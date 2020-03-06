using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Tmds.Linux;
using IoUring.Internal;
using static Tmds.Linux.LibC;

namespace IoUring
{
    public unsafe partial class ConcurrentRing
    {
        private readonly ConcurrentSubmissionQueue _sq;
        private readonly UnmapHandle _sqHandle;
        private readonly UnmapHandle _sqeHandle;
        private readonly AsyncOperationPool _operationPool;

        /// <summary>
        /// Submits a NOP to the kernel for execution.
        /// </summary>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitNop(Action<object?, int> callback, object? state, SubmissionOption options = SubmissionOption.None)
        {
            io_uring_sqe sqe = default;

            sqe.opcode = IORING_OP_NOP;
            sqe.flags = (byte) options;

            return SubmitNextEntry(&sqe, callback, state);
        }

        /// <summary>
        /// Submits a readv, preadv or preadv2 to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="iov">I/O vectors to read to</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitReadV(int fd, iovec* iov, int count, Action<object?, int> callback, object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_READV, fd, iov, count, default, 0, callback, state, options);
        }

        /// <summary>
        /// Submits a readv, preadv or preadv2 to the kernel for execution.
        /// </summary>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="iov">I/O vectors to read to</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per preadv)</param>
        /// <param name="flags">Flags for the I/O (as per preadv2)</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitReadV(Action<object?, int> callback, object? state,
            int fd, iovec* iov, int count, off_t offset, int flags, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_READV, fd, iov, count, offset, flags, callback, state, options);
        }

        /// <summary>
        /// Submits a writev, pwritev or pwritev2 to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="iov">I/O vectors to write</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitWriteV(int fd, iovec* iov, int count, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_WRITEV, fd, iov, count, default, 0, callback, state, options);
        }

        /// <summary>
        /// Submits a writev, pwritev or pwritev2 to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="iov">I/O vectors to write</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per pwritev)</param>
        /// <param name="flags">Flags for the I/O (as per pwritev2)</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitWriteV(int fd, iovec* iov, int count, off_t offset, int flags, Action<object?, int> callback,
            object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_WRITEV, fd, iov, count, offset, flags, callback, state, options);
        }

        /// <summary>
        /// Submits a fsync to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to synchronize</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitFsync(int fd, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            io_uring_sqe sqe = default;

            sqe.opcode = IORING_OP_FSYNC;
            sqe.flags = (byte) options;
            sqe.fd = fd;
            sqe.fsync_flags = (uint) FsyncOption.FileIntegrity;

            return SubmitNextEntry(&sqe, callback, state);
        }

        /// <summary>
        /// Submits a fsync to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to synchronize</param>
        /// <param name="fsyncOptions">Integrity options</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitFsync(int fd, FsyncOption fsyncOptions, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            io_uring_sqe sqe = default;

            sqe.opcode = IORING_OP_FSYNC;
            sqe.flags = (byte) options;
            sqe.fd = fd;
            sqe.fsync_flags = (uint) fsyncOptions;

            return SubmitNextEntry(&sqe, callback, state);
        }

        /// <summary>
        /// Submits a read using a registered buffer/file to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="buf">Buffers to read from</param>
        /// <param name="count">Number of buffers</param>
        /// <param name="index"></param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitReadFixed(int fd, void* buf, size_t count, int index, Action<object?, int> callback,
            object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWriteFixed(IORING_OP_READ_FIXED, fd, buf, count, index, default, callback, state, options);
        }

        /// <summary>
        /// Submits a read using a registered buffer/file to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="buf">Buffers to read from</param>
        /// <param name="count">Number of buffers</param>
        /// <param name="index"></param>
        /// <param name="offset">Offset in bytes into buffer (as per preadv)</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitReadFixed(int fd, void* buf, size_t count, int index, off_t offset,
            Action<object?, int> callback, object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWriteFixed(IORING_OP_READ_FIXED, fd, buf, count, index, offset, callback, state, options);
        }

        /// <summary>
        /// Submits a write using a registered buffer/file to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="buf">Buffers to write</param>
        /// <param name="count">Number of buffers</param>
        /// <param name="index"></param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitWriteFixed(int fd, void* buf, size_t count, int index, Action<object?, int> callback,
            object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWriteFixed(IORING_OP_WRITE_FIXED, fd, buf, count, index, default, callback, state, options);
        }

        /// <summary>
        /// Submits a write using a registered buffer/file to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="buf">Buffers to write</param>
        /// <param name="count">Number of buffers</param>
        /// <param name="index"></param>
        /// <param name="offset">Offset in bytes into buffer (as per pwritev)</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitWriteFixed(int fd, void* buf, size_t count, int index, off_t offset,
            Action<object?, int> callback, object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWriteFixed(IORING_OP_WRITE_FIXED, fd, buf, count, index, offset, callback, state, options);
        }

        /// <summary>
        /// Submits a one-shot poll of the file descriptor to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to poll</param>
        /// <param name="pollEvents">Events to poll for</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitPollAdd(int fd, ushort pollEvents, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            io_uring_sqe sqe = default;

            sqe.opcode = IORING_OP_POLL_ADD;
            sqe.flags = (byte) options;
            sqe.fd = fd;
            sqe.poll_events = pollEvents;

            return SubmitNextEntry(&sqe, callback, state);
        }

        /// <summary>
        /// Submits a request for removal of a previously added poll request to the kernel for execution.
        /// </summary>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitPollRemove(Action<object?, int> callback, object? state, SubmissionOption options = SubmissionOption.None)
        {
            io_uring_sqe sqe = default;

            sqe.opcode = IORING_OP_POLL_REMOVE;
            sqe.flags = (byte) options;

            return SubmitNextEntry(&sqe, callback, state);
        }

        /// <summary>
        /// Submits a sync_file_range to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to sync</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="count">Number of bytes to sync</param>
        /// <param name="flags">Flags for the operation (as per sync_file_range)</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitSyncFileRange(int fd, off_t offset, off_t count, uint flags, Action<object?, int> callback,
            object? state, SubmissionOption options = SubmissionOption.None)
        {
            io_uring_sqe sqe = default;

            unchecked
            {
                sqe.opcode = IORING_OP_SYNC_FILE_RANGE;
                sqe.flags = (byte) options;
                sqe.fd = fd;
                sqe.off = (ulong) (long) offset;
                sqe.len = (uint) count;
                sqe.sync_range_flags = flags;
            }

            return SubmitNextEntry(&sqe, callback, state);
        }

        /// <summary>
        /// Submits a sendmsg to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to send to</param>
        /// <param name="msg">Message to send</param>
        /// <param name="flags">Flags for the operator (as per sendmsg)</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitSendMsg(int fd, msghdr* msg, int flags, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            return SubmitSendRecvMsg(IORING_OP_SENDMSG, fd, msg, flags, callback, state, options);
        }

        /// <summary>
        /// Submits a recvmsg to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to receive from</param>
        /// <param name="msg">Message to read to</param>
        /// <param name="flags">Flags for the operator (as per recvmsg)</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitRecvMsg(int fd, msghdr* msg, int flags, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            return SubmitSendRecvMsg(IORING_OP_RECVMSG, fd, msg, flags, callback, state, options);
        }

        /// <summary>
        /// Submits a timeout to the kernel for execution.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger if less than one submissions completed.</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitTimeout(timespec* ts, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_TIMEOUT, -1, ts, 1, 1, (int) TimeoutOptions.Relative, callback, state, options);
        }

        /// <summary>
        /// Submits a timeout to the kernel for execution.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger if less than <paramref name="count"/> submissions completed.</param>
        /// <param name="count">The amount of completed submissions after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitTimeout(timespec* ts, uint count, TimeoutOptions timeoutOptions,
            Action<object?, int> callback, object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_TIMEOUT, -1, ts, 1, count, (int) timeoutOptions, callback, state, options);
        }

        /// <summary>
        /// Submits the removal of a timeout to the kernel for execution.
        /// </summary>
        /// <param name="timeoutRef">Reference to the timeout submission that should be removed</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitTimeoutRemove(ulong timeoutRef, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_TIMEOUT_REMOVE, -1, (void*) timeoutRef, 0, 0, 0, callback, state, options);
        }

        /// <summary>
        /// Submits an accept to the kernel for execution.
        /// </summary>
        /// <param name="fd">File descriptor to accept on</param>
        /// <param name="addr">(out) the address of the connected client.</param>
        /// <param name="addrLen">(out) the length of the address</param>
        /// <param name="flags">Flags as per accept4</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitAccept(int fd, sockaddr* addr, socklen_t* addrLen, int flags, Action<object?, int> callback,
            object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_ACCEPT, fd, addr, 0, (long) addrLen, flags, callback, state, options);
        }

        /// <summary>
        /// Submits the cancellation of a previously submitted item to the kernel for execution.
        /// </summary>
        /// <param name="operationRef">Reference to the operation to cancel</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitCancel(ulong operationRef, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_ASYNC_CANCEL, -1, (void*) operationRef, 0, 0, 0, callback, state, options);
        }

        /// <summary>
        /// Submits a connect to the kernel for execution.
        /// </summary>
        /// <param name="fd">The socket to connect on</param>
        /// <param name="addr">The address to connect to</param>
        /// <param name="addrLen">The length of the address</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitConnect(int fd, sockaddr* addr, socklen_t addrLen, Action<object?, int> callback,
            object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_CONNECT, fd, addr, 0, (uint) addrLen, 0, callback, state, options);
        }

        /// <summary>
        /// Submits a timeout to a previously prepared linked item to the kernel for execution.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitLinkTimeout(timespec* ts, Action<object?, int> callback, object? state,
            SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_LINK_TIMEOUT, -1, ts, 1, 0, (int) TimeoutOptions.Relative, callback, state, options);
        }

        /// <summary>
        /// Submits a timeout to a previously prepared linked item to the kernel for execution.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="callback">Callback to be invoked once the operation completes</param>
        /// <param name="state">State to be passed to the callback</param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns>A reference to the submitted operation</returns>
        public ulong SubmitLinkTimeout(timespec* ts, TimeoutOptions timeoutOptions, Action<object?, int> callback,
            object? state, SubmissionOption options = SubmissionOption.None)
        {
            return SubmitReadWrite(IORING_OP_LINK_TIMEOUT, -1, ts, 1, 0, (int) timeoutOptions, callback, state, options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong SubmitReadWrite(byte op, int fd, void* iov, int count, off_t offset, int flags,
            Action<object?, int> callback, object? state, SubmissionOption options)
        {
            io_uring_sqe sqe = default;

            unchecked
            {
                sqe.opcode = op;
                sqe.flags = (byte) options;
                sqe.fd = fd;
                sqe.off = (ulong) (long) offset;
                sqe.addr = (ulong) iov;
                sqe.len = (uint) count;
                sqe.rw_flags = flags;
            }

            return SubmitNextEntry(&sqe, callback, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong SubmitReadWriteFixed(byte op, int fd, void* buf, size_t count, int index, off_t offset,
            Action<object?, int> callback, object? state, SubmissionOption options)
        {
            io_uring_sqe sqe = default;

            unchecked
            {
                sqe.opcode = op;
                sqe.flags = (byte) options;
                sqe.fd = fd;
                sqe.off = (ulong) (long) offset;
                sqe.addr = (ulong) buf;
                sqe.len = (uint) count;
                sqe.buf_index = (ushort) index;
            }

            return SubmitNextEntry(&sqe, callback, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong SubmitSendRecvMsg(byte op, int fd, msghdr* msg, int flags,
            Action<object?, int> callback, object? state, SubmissionOption options)
        {
            io_uring_sqe sqe;

            unchecked
            {
                sqe.opcode = op;
                sqe.flags = (byte) options;
                sqe.fd = fd;
                sqe.addr = (ulong) msg;
                sqe.len = 1;
                sqe.msg_flags = (uint) flags;
            }

            return SubmitNextEntry(&sqe, callback, state);
        }

        private ulong SubmitNextEntry(io_uring_sqe* sqe, Action<object?, int> callback, object? state)
        {
            AsyncOperation op = _operationPool.Rent();
            op.Sqe = sqe;
            op.Callback = callback;
            op.State = state;

            if (!_sq.SubmitNextEntry(op, out var userData))
            {
                ThrowHelper.ThrowSubmissionQueueFullException(); // TODO: Grow instead
            }

            return userData;
        }

        public void SubmitMultiple(
            Submission submission1, Action<object?, int> callback1, object? state1,
            Submission submission2, Action<object?, int> callback2, object? state2,
            Span<ulong> userData)
        {
            var op1 = _operationPool.Rent();
            op1.Sqe = (io_uring_sqe*) (&submission1);
            op1.Callback = callback1;
            op1.State = state1;

            var op2 = _operationPool.Rent();
            op2.Sqe = (io_uring_sqe*) (&submission2);
            op2.Callback = callback2;
            op2.State = state2;

            AsyncOperation[] ops = ArrayPool<AsyncOperation>.Shared.Rent(2);
            ops[0] = op1;
            ops[1] = op2;

            if (!_sq.SubmitNextEntries(ops.AsSpan().Slice(0,2 ), userData))
            {
                ThrowHelper.ThrowSubmissionQueueFullException(); // TODO: Grow instead
            }

            ArrayPool<AsyncOperation>.Shared.Return(ops);
        }

        public void SubmitMultiple(
            Submission submission1, Action<object?, int> callback1, object? state1,
            Submission submission2, Action<object?, int> callback2, object? state2,
            Submission submission3, Action<object?, int> callback3, object? state3,
            Span<ulong> userData)
        {
            var op1 = _operationPool.Rent();
            op1.Sqe = (io_uring_sqe*) (&submission1);
            op1.Callback = callback1;
            op1.State = state1;

            var op2 = _operationPool.Rent();
            op2.Sqe = (io_uring_sqe*) (&submission2);
            op2.Callback = callback2;
            op2.State = state2;

            var op3 = _operationPool.Rent();
            op3.Sqe = (io_uring_sqe*) (&submission3);
            op3.Callback = callback3;
            op3.State = state3;

            AsyncOperation[] ops = ArrayPool<AsyncOperation>.Shared.Rent(3);
            ops[0] = op1;
            ops[1] = op2;
            ops[2] = op3;

            if (!_sq.SubmitNextEntries(ops.AsSpan().Slice(0,3 ), userData))
            {
                ThrowHelper.ThrowSubmissionQueueFullException(); // TODO: Grow instead
            }

            ArrayPool<AsyncOperation>.Shared.Return(ops);
        }
    }
}
