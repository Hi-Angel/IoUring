using System.Diagnostics;
using System.Threading;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal unsafe class ConcurrentCompletionQueue
    {
        /// <summary>
        /// Incremented by the application to let the kernel know, which Completion Queue Events were already consumed.
        /// </summary>
        internal readonly uint* _head;

        /// <summary>
        /// Incremented by the kernel to let the application know about another Completion Queue Event.
        /// </summary>
        internal readonly uint* _tail;

        /// <summary>
        /// Mask to apply to potentially overflowing head counter to get a valid index within the ring.
        /// </summary>
        internal readonly uint _ringMask;

        /// <summary>
        /// Number of entries in the ring.
        /// </summary>
        internal readonly uint _ringEntries;

        /// <summary>
        /// Incremented by the kernel on each overwritten Completion Queue Event.
        /// This is a sign, that the application is producing Submission Queue Events faster as it handles the corresponding Completion Queue Events.
        /// </summary>
        internal readonly uint* _overflow;

        /// <summary>
        /// Completion Queue Events filled by the kernel.
        /// </summary>
        internal readonly io_uring_cqe* _cqes;

        /// <summary>
        /// Whether the kernel polls for I/O
        /// </summary>
        internal readonly bool _ioPolled;

        internal object Gate => this;

        private ConcurrentCompletionQueue(uint* head, uint* tail, uint ringMask, uint ringEntries, uint* overflow, io_uring_cqe* cqes, bool ioPolled)
        {
            _head = head;
            _tail = tail;
            _ringMask = ringMask;
            _ringEntries = ringEntries;
            _overflow = overflow;
            _cqes = cqes;
            _ioPolled = ioPolled;
        }

        public static ConcurrentCompletionQueue CreateCompletionQueue(void* ringBase, io_cqring_offsets* offsets, bool ioPolled) =>
            new ConcurrentCompletionQueue(
                head: Add<uint>(ringBase, offsets->head),
                tail: Add<uint>(ringBase, offsets->tail),
                ringMask: *Add<uint>(ringBase, offsets->ring_mask),
                ringEntries: *Add<uint>(ringBase, offsets->ring_entries),
                overflow: Add<uint>(ringBase, offsets->overflow),
                cqes: Add<io_uring_cqe>(ringBase, offsets->cqes),
                ioPolled: ioPolled
            );

        /// <summary>
        /// Returns the number of entries in the Completion Queue.
        /// </summary>
        public uint Entries => _ringEntries;

        /// <summary>
        /// Returns whether events are currently available to be read
        /// </summary>
        internal bool EventsAvailable
        {
            get
            {
                Debug.Assert(Monitor.IsEntered(Gate));
                return *_head != *_tail;
            }
        }

        public bool TryRead(int ringFd, out Completion result)
        {
            lock (Gate)
            {
                uint head = *_head;
                bool eventsAvailable = head != *_tail;

                if (!eventsAvailable && _ioPolled)
                {
                    // If the kernel is polling I/O, we must reap completions.
                    // We are not expected to block if no completions are available, so min_complete is set to 0.
                    SafeEnter(ringFd, 0, 0, IORING_ENTER_GETEVENTS);
                    // double-check with a memory barrier to ensure we see everything the kernel manipulated prior to the tail bump
                    eventsAvailable = head != Volatile.Read(ref *_tail);
                }

                if (!eventsAvailable)
                {
                    result = default;
                    return false;
                }

                // Check overflow now that we are sure there are events available... without events we assume no overflow happened
                uint overflow = *_overflow;
                if (overflow > 0)
                {
                    ThrowOverflowException(overflow);
                }

                var index = head & _ringMask;
                var cqe = &_cqes[index];

                result = new Completion(cqe->res, cqe->user_data);
                *_head = unchecked(head + 1);

                return true;
            }
        }

        public Completion Read(int ringFd)
        {
            lock (Gate)
            {
                while (true)
                {
                    if (TryRead(ringFd, out var completion))
                    {
                        return completion;
                    }

                    SafeEnter(ringFd, 0, 1, IORING_ENTER_GETEVENTS);
                }
            }
        }
    }
}
