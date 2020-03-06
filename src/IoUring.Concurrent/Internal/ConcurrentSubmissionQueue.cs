using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal unsafe class ConcurrentSubmissionQueue
    {
        /// <summary>
        /// Incremented by the kernel to let the application know, another element was consumed.
        /// </summary>
        private readonly uint* _head;

        /// <summary>
        /// Incremented by the application to let the kernel know, another element was submitted.
        /// </summary>
        private readonly uint* _tail;

        /// <summary>
        /// Mask to apply to potentially overflowing tail counter to get a valid index within the ring
        /// </summary>
        private readonly uint _ringMask;

        /// <summary>
        /// Number of entries in the ring
        /// </summary>
        private readonly uint _ringEntries;

        /// <summary>
        /// Set to IORING_SQ_NEED_WAKEUP by the kernel, if the Submission Queue polling thread is idle and needs
        /// a call to io_uring_enter with the IORING_ENTER_SQ_WAKEUP flag set.
        /// </summary>
        private readonly uint* _flags;

        /// <summary>
        /// Incremented by the kernel on each invalid submission.
        /// </summary>
        private readonly uint* _dropped;

        /// <summary>
        /// Array of indices within the <see cref="_sqes"/>
        /// </summary>
        private readonly uint* _array;

        /// <summary>
        /// Submission Queue Entries to be filled by the application
        /// </summary>
        private readonly io_uring_sqe* _sqes;

        /// <summary>
        /// Index of the last Submission Queue Entry handed out to the application (to be filled).
        /// This is typically behind <see cref="_tail"/> as the kernel must not yet know about bumps of the internal index, before the Entry is fully prepped.
        /// </summary>
        private uint _tailInternal;

        /// <summary>
        /// Index of the last Submission Queue Entry handed over to the kernel.
        /// This is typically ahead of <see cref="_head"/> as the kernel might not have had the chance to consume the item at the given index.
        /// </summary>
        private uint _headInternal;

        /// <summary>
        /// Whether the kernel is polling the Submission Queue.
        /// </summary>
        private readonly bool _sqPolled;

        /// <summary>
        /// Whether the kernel is polling I/O
        /// </summary>
        internal bool IoPolled { get; }

        private readonly ConcurrentDictionary<ulong, AsyncOperation> _asyncOperations;

        private readonly UnblockHandle _unblockHandle;
        internal object Gate => this;

        internal bool ShouldUnblock { get; set; }

        private ConcurrentSubmissionQueue(
            uint* head, uint* tail, uint ringMask, uint ringEntries, uint* flags, uint* dropped, uint* array, io_uring_sqe* sqes,
            bool sqPolled, bool ioPolled, ConcurrentDictionary<ulong, AsyncOperation> asyncOperations,
            UnblockHandle unblockHandle)
        {
            _head = head;
            _tail = tail;
            _ringMask = ringMask;
            _ringEntries = ringEntries;
            _flags = flags;
            _dropped = dropped;
            _array = array;
            _sqes = sqes;
            _tailInternal = 0;
            _headInternal = 0;
            _sqPolled = sqPolled;
            IoPolled = ioPolled;
            _asyncOperations = asyncOperations;
            _unblockHandle = unblockHandle;
        }

        public static ConcurrentSubmissionQueue CreateSubmissionQueue(
            void* ringBase, io_sqring_offsets* offsets, io_uring_sqe* elements,
            bool sqPolled, bool ioPolled, ConcurrentDictionary<ulong, AsyncOperation> asyncOperations,
            UnblockHandle unblockHandle)
            => new ConcurrentSubmissionQueue(
                Add<uint>(ringBase, offsets->head),
                Add<uint>(ringBase, offsets->tail),
                *Add<uint>(ringBase, offsets->ring_mask),
                *Add<uint>(ringBase, offsets->ring_entries),
                Add<uint>(ringBase, offsets->flags),
                Add<uint>(ringBase, offsets->dropped),
                Add<uint>(ringBase, offsets->array),
                elements, sqPolled, ioPolled, asyncOperations, unblockHandle
            );

        /// <summary>
        /// Returns the number of entries in the Submission Queue.
        /// </summary>
        public uint Entries => _ringEntries;

        /// <summary>
        /// Determines the number of entries in the Submission Queue that can be used to prepare new submissions
        /// prior to the next <see cref="SubmitAndWait"/>.
        /// </summary>
        public uint EntriesToPrepare
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ringEntries - EntriesToSubmit;
        }

        /// <summary>
        /// Calculates the number of prepared Submission Queue Entries that will be submitted to the kernel during
        /// the next <see cref="SubmitAndWait"/>.
        /// </summary>
        public uint EntriesToSubmit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                lock (Gate)
                {
                    uint head = _sqPolled ? *_head : _headInternal;
                    return unchecked(_tailInternal - head);
                }
            }
        }

        /// <summary>
        /// Adds the provided async operation to the Submission Queue.
        /// </summary>
        /// <returns>Whether the operation could be added</returns>
        public bool SubmitNextEntry(AsyncOperation op, out ulong userData)
        {
            io_uring_sqe* sqeData = op.Sqe;
            bool unblock;

            lock (Gate)
            {
                uint tailInternal = _tailInternal;
                uint head = _sqPolled ? *_head : _headInternal;
                uint next = unchecked(tailInternal + 1);
                uint pendingItems = unchecked(next - head);

                userData = CalculateUserData(sqeData->fd, tailInternal);

                if (pendingItems > _ringEntries)
                {
                    return false;
                }

                sqeData->user_data = userData;
                var sqe = &_sqes[tailInternal & _ringMask];

                Unsafe.CopyBlockUnaligned(sqe, sqeData, (uint) sizeof(io_uring_sqe));

                _asyncOperations[userData] = op;
                _tailInternal = next;

                unblock = ShouldUnblock;
                if (unblock) ShouldUnblock = false;
            }

            if (unblock)
            {
                 _unblockHandle.Unblock();
            }

            return true;
        }

        /// <summary>
        /// Adds the provided async operations to the Submission Queue.
        /// </summary>
        /// <returns>Whether the operations could be added</returns>
        public bool SubmitNextEntries(ReadOnlySpan<AsyncOperation> ops, Span<ulong> userData)
        {
            Debug.Assert(ops.Length == userData.Length);
            bool unblock;

            lock (Gate)
            {
                uint tailInternal = _tailInternal;
                uint head = _sqPolled ? *_head : _headInternal;

                if (tailInternal + ops.Length - head > _ringEntries)
                {
                    return false;
                }

                for (int i = 0; i < ops.Length; i++)
                {
                    var op = ops[i];

                    io_uring_sqe* sqeData = op.Sqe;
                    var currentUserData =  CalculateUserData(sqeData->fd, tailInternal);
                    userData[i] = currentUserData;
                    sqeData->user_data = currentUserData;

                    var sqe = &_sqes[tailInternal & _ringMask];
                    Unsafe.CopyBlockUnaligned(sqe, sqeData, (uint) sizeof(io_uring_sqe));

                    _asyncOperations[currentUserData] = op;
                    tailInternal = unchecked(tailInternal + 1);
                }

                _tailInternal = tailInternal;
                unblock = ShouldUnblock;
                if (unblock) ShouldUnblock = false;
            }

            if (unblock)
            {
                _unblockHandle.Unblock();
            }

            return true;
        }

        /// <summary>
        /// Make prepared Submission Queue Entries visible to the kernel.
        /// </summary>
        /// <returns>
        /// The number Submission Queue Entries that can be submitted.
        /// This may include Submission Queue Entries previously ignored by the kernel.</returns>
        internal uint Notify()
        {
            lock (Gate)
            {
                uint tail = *_tail;
                uint tailInternal = _tailInternal;
                uint headInternal = _headInternal;
                if (headInternal == tailInternal)
                {
                    return tail - *_head;
                }

                uint mask = _ringMask;
                uint* array = _array;
                uint toSubmit = unchecked(tailInternal - headInternal);
                while (toSubmit-- != 0)
                {
                    array[tail & mask] = headInternal & mask;
                    tail = unchecked(tail + 1);
                    headInternal = unchecked(headInternal + 1);
                }

                _headInternal = headInternal;

                *_tail = tail;

                return tail - *_head;
            }
        }

        internal bool ShouldEnter(out uint enterFlags)
        {
            enterFlags = 0;
            if (!_sqPolled) return true;
            if ((*_flags & IORING_SQ_NEED_WAKEUP) != 0)
            {
                // Kernel is polling but transitioned to idle (IORING_SQ_NEED_WAKEUP)
                enterFlags |= IORING_ENTER_SQ_WAKEUP;
                return true;
            }

            // Kernel is still actively polling
            return false;
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void CheckNoSubmissionsDropped() => Debug.Assert(Volatile.Read(ref *_dropped) == 0);

        public SubmitResult SubmitAndWait(int ringFd, uint minComplete, out uint operationsSubmitted)
        {
            uint toSubmit = Notify();

            if (!ShouldEnter(out uint enterFlags))
            {
                CheckNoSubmissionsDropped();

                // Assume all Entries are already known to the kernel via Notify above
                goto SkipSyscall;
            }

            // For minComplete to take effect or if the kernel is polling for I/O, we must set IORING_ENTER_GETEVENTS
            if (minComplete > 0 || IoPolled)
            {
                enterFlags |= IORING_ENTER_GETEVENTS;
            }
            else if (toSubmit == 0)
            {
                // There are no submissions, we don't have to wait for completions and don't have to reap polled I/O completions
                // --> We can skip the syscall and return directly.
                goto SkipSyscall;
            }

            int res;
            int err = default;
            do
            {
                res = io_uring_enter(ringFd, toSubmit, minComplete, enterFlags, (sigset_t*) NULL);
            } while (res == -1 && (err = errno) == EINTR);

            if (res == -1)
            {
                if (err == EAGAIN || err == EBUSY)
                {
                    operationsSubmitted = default;
                    return SubmitResult.AwaitCompletions;
                }

                ThrowErrnoException(res);
            }

            CheckNoSubmissionsDropped();

            return (operationsSubmitted = (uint) res) >= toSubmit ?
                SubmitResult.SubmittedSuccessfully :
                SubmitResult.SubmittedPartially;

        SkipSyscall:
            operationsSubmitted = toSubmit;
            return SubmitResult.SubmittedSuccessfully;
        }

        private static ulong CalculateUserData(int fd, uint tail)
        {
            ulong lFd = unchecked((ulong)fd);
            ulong lTail = tail;

            return (lFd << 32) | lTail;
        }
    }
}
