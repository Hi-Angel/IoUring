using IoUring.Internal;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.ThrowHelper;

namespace IoUring
{
    public unsafe partial class Ring
    {
        /// <summary>
        /// Adds a NOP to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareNop(ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareNopInternal(userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a NOP to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareNop(ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareNopInternal(userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareNopInternal(ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_NOP;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a readv, preadv or preadv2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="iov">I/O vectors to read to</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per preadv)</param>
        /// <param name="flags">Flags for the I/O (as per preadv2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareReadV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareReadVInternal(fd, iov, count, offset, flags, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a readv, preadv or preadv2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="iov">I/O vectors to read to</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per preadv)</param>
        /// <param name="flags">Flags for the I/O (as per preadv2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareReadV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareReadVInternal(fd, iov, count, offset, flags, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareReadVInternal(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_READV;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) iov;
                sqe->len = (uint) count;
                sqe->rw_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a writev, pwritev or pwritev2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="iov">I/O vectors to write</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per pwritev)</param>
        /// <param name="flags">Flags for the I/O (as per pwritev2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareWriteV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareWriteVInternal(fd, iov, count, offset, flags, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a writev, pwritev or pwritev2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="iov">I/O vectors to write</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per pwritev)</param>
        /// <param name="flags">Flags for the I/O (as per pwritev2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareWriteV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareWriteVInternal(fd, iov, count, offset, flags, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareWriteVInternal(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_WRITEV;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) iov;
                sqe->len = (uint) count;
                sqe->rw_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a fsync to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to synchronize</param>
        /// <param name="fsyncOptions">Integrity options</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareFsync(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareFsyncInternal(fd, fsyncOptions, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a fsync to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to synchronize</param>
        /// <param name="fsyncOptions">Integrity options</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareFsync(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareFsyncInternal(fd, fsyncOptions, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareFsyncInternal(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_FSYNC;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->fsync_flags = (uint) fsyncOptions;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a read using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="buf">Buffer/file to read to</param>
        /// <param name="count">Number of bytes to read</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per preadv)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareReadFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareReadFixedInternal(fd, buf, count, index, offset, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a read using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="buf">Buffer/file to read to</param>
        /// <param name="count">Number of bytes to read</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per preadv)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareReadFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareReadFixedInternal(fd, buf, count, index, offset, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareReadFixedInternal(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_READ_FIXED;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) buf;
                sqe->len = (uint) count;
                sqe->user_data = userData;
                sqe->buf_index = (ushort) index;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a write using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="buf">Buffer/file to write</param>
        /// <param name="count">Number of bytes to write</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per pwritev)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareWriteFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareWriteFixedInternal(fd, buf, count, index, offset, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a write using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="buf">Buffer/file to write</param>
        /// <param name="count">Number of bytes to write</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per pwritev)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareWriteFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareWriteFixedInternal(fd, buf, count, index, offset, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareWriteFixedInternal(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_WRITE_FIXED;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) buf;
                sqe->len = (uint) count;
                sqe->user_data = userData;
                sqe->buf_index = (ushort) index;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a one-shot poll of the file descriptor to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to poll</param>
        /// <param name="pollEvents">Events to poll for</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PreparePollAdd(int fd, ushort pollEvents, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PreparePollAddInternal(fd, pollEvents, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a one-shot poll of the file descriptor to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to poll</param>
        /// <param name="pollEvents">Events to poll for</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPreparePollAdd(int fd, ushort pollEvents, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PreparePollAddInternal(fd, pollEvents, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PreparePollAddInternal(int fd, ushort pollEvents, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_POLL_ADD;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->poll_events = pollEvents;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a request for removal of a previously added poll request to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="pollUserData">userData of the poll submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PreparePollRemove(ulong pollUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PreparePollRemoveInternal(pollUserData, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a request for removal of a previously added poll request to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="pollUserData">userData of the poll submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPreparePollRemove(ulong pollUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PreparePollRemoveInternal(pollUserData, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PreparePollRemoveInternal(ulong pollUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_POLL_REMOVE;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->addr = pollUserData;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a sync_file_range to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to sync</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="count">Number of bytes to sync</param>
        /// <param name="flags">Flags for the operation (as per sync_file_range)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareSyncFileRange(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareSyncFileRangeInternal(fd, offset, count, flags, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a sync_file_range to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to sync</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="count">Number of bytes to sync</param>
        /// <param name="flags">Flags for the operation (as per sync_file_range)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareSyncFileRange(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareSyncFileRangeInternal(fd, offset, count, flags, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareSyncFileRangeInternal(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_SYNC_FILE_RANGE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->len = (uint) count;
                sqe->sync_range_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a sendmsg to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to send to</param>
        /// <param name="msg">Message to send</param>
        /// <param name="flags">Flags for the operation (as per sendmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareSendMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareSendMsgInternal(fd, msg, flags, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a sendmsg to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to send to</param>
        /// <param name="msg">Message to send</param>
        /// <param name="flags">Flags for the operation (as per sendmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareSendMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareSendMsgInternal(fd, msg, flags, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareSendMsgInternal(int fd, msghdr* msg, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_SENDMSG;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->addr = (ulong) msg;
                sqe->len = 1;
                sqe->msg_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a recvmsg to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to receive from</param>
        /// <param name="msg">Message to read to</param>
        /// <param name="flags">Flags for the operation (as per recvmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareRecvMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareRecvMsgInternal(fd, msg, flags, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a recvmsg to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to receive from</param>
        /// <param name="msg">Message to read to</param>
        /// <param name="flags">Flags for the operation (as per recvmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareRecvMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareRecvMsgInternal(fd, msg, flags, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareRecvMsgInternal(int fd, msghdr* msg, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_RECVMSG;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->addr = (ulong) msg;
                sqe->len = 1;
                sqe->msg_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a timeout to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger if less than <paramref name="count"/> submissions completed.</param>
        /// <param name="count">The amount of completed submissions after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareTimeout(timespec* ts, uint count = 1, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareTimeoutInternal(ts, count, timeoutOptions, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a timeout to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger if less than <paramref name="count"/> submissions completed.</param>
        /// <param name="count">The amount of completed submissions after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareTimeout(timespec* ts, uint count = 1, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareTimeoutInternal(ts, count, timeoutOptions, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareTimeoutInternal(timespec* ts, uint count = 1, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_TIMEOUT;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->off = count;
                sqe->addr = (ulong) ts;
                sqe->len = 1;
                sqe->timeout_flags = (uint) timeoutOptions;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds the removal of a timeout to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="timeoutUserData">userData of the timeout submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareTimeoutRemove(ulong timeoutUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareTimeoutRemoveInternal(timeoutUserData, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add the removal of a timeout to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="timeoutUserData">userData of the timeout submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareTimeoutRemove(ulong timeoutUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareTimeoutRemoveInternal(timeoutUserData, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareTimeoutRemoveInternal(ulong timeoutUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_TIMEOUT_REMOVE;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->addr = timeoutUserData;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds an accept to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to accept from</param>
        /// <param name="addr">(out) the address of the connected client.</param>
        /// <param name="addrLen">(out) the length of the address</param>
        /// <param name="flags">Flags as per accept4</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareAccept(int fd, sockaddr* addr, socklen_t* addrLen, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareAcceptInternal(fd, addr, addrLen, flags, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add an accept to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to accept from</param>
        /// <param name="addr">(out) the address of the connected client.</param>
        /// <param name="addrLen">(out) the length of the address</param>
        /// <param name="flags">Flags as per accept4</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareAccept(int fd, sockaddr* addr, socklen_t* addrLen, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareAcceptInternal(fd, addr, addrLen, flags, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareAcceptInternal(int fd, sockaddr* addr, socklen_t* addrLen, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_ACCEPT;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) addrLen;
                sqe->addr = (ulong) addr;
                sqe->accept_flags = (uint) flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds the cancellation of a previously submitted item to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="opUserData">userData of the operation to cancel</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareCancel(ulong opUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareCancelInternal(opUserData, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add the cancellation of a previously submitted item to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="opUserData">userData of the operation to cancel</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareCancel(ulong opUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareCancelInternal(opUserData, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareCancelInternal(ulong opUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_ASYNC_CANCEL;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->addr = opUserData;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a timeout to a previously prepared linked item to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareLinkTimeout(timespec* ts, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareLinkTimeoutInternal(ts, timeoutOptions, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a timeout to a previously prepared linked item to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareLinkTimeout(timespec* ts, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareLinkTimeoutInternal(ts, timeoutOptions, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareLinkTimeoutInternal(timespec* ts, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_LINK_TIMEOUT;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->addr = (ulong) ts;
                sqe->len = 1;
                sqe->timeout_flags = (uint) timeoutOptions;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a connect to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">The socket to connect on</param>
        /// <param name="addr">The address to connect to</param>
        /// <param name="addrLen">The length of the address</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareConnect(int fd, sockaddr* addr, socklen_t addrLen, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareConnectInternal(fd, addr, addrLen, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a connect to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">The socket to connect on</param>
        /// <param name="addr">The address to connect to</param>
        /// <param name="addrLen">The length of the address</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareConnect(int fd, sockaddr* addr, socklen_t addrLen, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareConnectInternal(fd, addr, addrLen, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareConnectInternal(int fd, sockaddr* addr, socklen_t addrLen, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_CONNECT;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = addrLen;
                sqe->addr = (ulong) addr;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a fallocate to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">The file to manipulate the allocated disk space for</param>
        /// <param name="mode">The operation to be performed</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="len">Number of bytes to operate on</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareFallocate(int fd, int mode, off_t offset, off_t len, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareFallocateInternal(fd, mode, offset, len, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a fallocate to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">The file to manipulate the allocated disk space for</param>
        /// <param name="mode">The operation to be performed</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="len">Number of bytes to operate on</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareFallocate(int fd, int mode, off_t offset, off_t len, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareFallocateInternal(fd, mode, offset, len, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareFallocateInternal(int fd, int mode, off_t offset, off_t len, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_FALLOCATE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) (long) len;
                sqe->len = (uint) mode;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a closeat to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="flags">Flags for the open operation (e.g. access mode)</param>
        /// <param name="mode">File mode bits</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareOpenAt(int dfd, byte* path, int flags, mode_t mode = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareOpenAtInternal(dfd, path, flags, mode, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a closeat to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="flags">Flags for the open operation (e.g. access mode)</param>
        /// <param name="mode">File mode bits</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareOpenAt(int dfd, byte* path, int flags, mode_t mode = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareOpenAtInternal(dfd, path, flags, mode, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareOpenAtInternal(int dfd, byte* path, int flags, mode_t mode = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_OPENAT;
                sqe->flags = (byte) options;
                sqe->fd = dfd;
                sqe->addr = (ulong) path;
                sqe->len = (uint) mode;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a close to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to close</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareClose(int fd, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareCloseInternal(fd, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a close to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to close</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareClose(int fd, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareCloseInternal(fd, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareCloseInternal(int fd, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_CLOSE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds an update to the registered files to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fds">File descriptors to add / -1 to remove</param>
        /// <param name="nrFds">Number of changing file descriptors</param>
        /// <param name="offset">Offset into the previously registered files</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareFilesUpdate(int* fds, int nrFds, int offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareFilesUpdateInternal(fds, nrFds, offset, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add an update to the registered files to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fds">File descriptors to add / -1 to remove</param>
        /// <param name="nrFds">Number of changing file descriptors</param>
        /// <param name="offset">Offset into the previously registered files</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareFilesUpdate(int* fds, int nrFds, int offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareFilesUpdateInternal(fds, nrFds, offset, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareFilesUpdateInternal(int* fds, int nrFds, int offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_CLOSE;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) fds;
                sqe->len = (uint) nrFds;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a statx to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="dfd">Directory file descriptor for relative paths</param>
        /// <param name="path">Absolute or relative path</param>
        /// <param name="flags">Influence pathname-based lookup</param>
        /// <param name="mask">Identifies the required fields</param>
        /// <param name="statxbuf">Buffer for the required information</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareStatx(int dfd, byte* path, int flags, uint mask, statx* statxbuf, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareStatxInternal(dfd, path, flags, mask, statxbuf, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a statx to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="dfd">Directory file descriptor for relative paths</param>
        /// <param name="path">Absolute or relative path</param>
        /// <param name="flags">Influence pathname-based lookup</param>
        /// <param name="mask">Identifies the required fields</param>
        /// <param name="statxbuf">Buffer for the required information</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareStatx(int dfd, byte* path, int flags, uint mask, statx* statxbuf, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareStatxInternal(dfd, path, flags, mask, statxbuf, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareStatxInternal(int dfd, byte* path, int flags, uint mask, statx* statxbuf, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_STATX;
                sqe->flags = (byte) options;
                sqe->fd = dfd;
                sqe->off = (ulong) statxbuf;
                sqe->addr = (ulong) path;
                sqe->len = mask;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a read to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="nbytes">Number of bytes to read</param>
        /// <param name="offset">File offset to read at</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareRead(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareReadInternal(fd, buf, nbytes, offset, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a read to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="nbytes">Number of bytes to read</param>
        /// <param name="offset">File offset to read at</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareRead(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareReadInternal(fd, buf, nbytes, offset, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareReadInternal(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_READ;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) buf;
                sqe->len = nbytes;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a write to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to write</param>
        /// <param name="nbytes">Number of bytes to write</param>
        /// <param name="offset">File offset to write at</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareWrite(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareWriteInternal(fd, buf, nbytes, offset, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a write to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to write</param>
        /// <param name="nbytes">Number of bytes to write</param>
        /// <param name="offset">File offset to write at</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareWrite(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareWriteInternal(fd, buf, nbytes, offset, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareWriteInternal(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_WRITE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) buf;
                sqe->len = nbytes;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a posix_fadvise to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="offset">Offset into the file</param>
        /// <param name="len">Length of the file range</param>
        /// <param name="advice">Advice for the file range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareFadvise(int fd, off_t offset, off_t len, int advice, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareFadviseInternal(fd, offset, len, advice, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a posix_fadvise to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="offset">Offset into the file</param>
        /// <param name="len">Length of the file range</param>
        /// <param name="advice">Advice for the file range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareFadvise(int fd, off_t offset, off_t len, int advice, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareFadviseInternal(fd, offset, len, advice, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareFadviseInternal(int fd, off_t offset, off_t len, int advice, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_FADVISE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->len = (uint) len;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds an madvise to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="addr">Start of address range</param>
        /// <param name="len">Length of address range</param>
        /// <param name="advice">Advice for address range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareMadvise(void* addr, off_t len, int advice, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareMadviseInternal(addr, len, advice, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add an madvise to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="addr">Start of address range</param>
        /// <param name="len">Length of address range</param>
        /// <param name="advice">Advice for address range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareMadvise(void* addr, off_t len, int advice, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareMadviseInternal(addr, len, advice, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareMadviseInternal(void* addr, off_t len, int advice, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_MADVISE;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->len = (uint) len;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a send to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to send</param>
        /// <param name="len">Length of buffer to send</param>
        /// <param name="flags">Flags for the send</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareSend(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareSendInternal(sockfd, buf, len, flags, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a send to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to send</param>
        /// <param name="len">Length of buffer to send</param>
        /// <param name="flags">Flags for the send</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareSend(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareSendInternal(sockfd, buf, len, flags, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareSendInternal(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_SEND;
                sqe->flags = (byte) options;
                sqe->fd = sockfd;
                sqe->addr = (ulong) buf;
                sqe->len = (uint) len;
                sqe->msg_flags = (uint) flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds a recv to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="len">Length of buffer to read to</param>
        /// <param name="flags">Flags for the recv</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareRecv(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareRecvInternal(sockfd, buf, len, flags, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add a recv to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="len">Length of buffer to read to</param>
        /// <param name="flags">Flags for the recv</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareRecv(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareRecvInternal(sockfd, buf, len, flags, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareRecvInternal(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_RECV;
                sqe->flags = (byte) options;
                sqe->fd = sockfd;
                sqe->addr = (ulong) buf;
                sqe->len = (uint) len;
                sqe->msg_flags = (uint) flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds an openat2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="how">How pat should be opened</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareOpenAt2(int dfd, byte* path, open_how* how, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareOpenAt2Internal(dfd, path, how, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add an openat2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="how">How pat should be opened</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareOpenAt2(int dfd, byte* path, open_how* how, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareOpenAt2Internal(dfd, path, how, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareOpenAt2Internal(int dfd, byte* path, open_how* how, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_OPENAT2;
                sqe->flags = (byte) options;
                sqe->fd = dfd;
                sqe->addr = (ulong) path;
                sqe->len = SizeOf.open_how;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

        /// <summary>
        /// Adds  to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="epfd">epoll instance file descriptor</param>
        /// <param name="fd">File descriptor</param>
        /// <param name="op">Operation to be performed for the file descriptor</param>
        /// <param name="ev">Settings</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        /// <exception cref="TooManyOperationsInFlightException">If <see cref="Ring.SupportsNoDrop"/> is false and too many operations are currently in flight</exception>
        public void PrepareEpollCtl(int epfd, int fd, int op, epoll_event* ev, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var result = PrepareEpollCtlInternal(epfd, fd, op, ev, userData, options, personality);
            if (result != SubmissionAcquireResult.SubmissionAcquired)
            {
                ThrowSubmissionAcquisitionException(result);
            }
        }

        /// <summary>
        /// Attempts to add  to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="epfd">epoll instance file descriptor</param>
        /// <param name="fd">File descriptor</param>
        /// <param name="op">Operation to be performed for the file descriptor</param>
        /// <param name="ev">Settings</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareEpollCtl(int epfd, int fd, int op, epoll_event* ev, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            return PrepareEpollCtlInternal(epfd, fd, op, ev, userData, options, personality) == SubmissionAcquireResult.SubmissionAcquired;
        }

        private SubmissionAcquireResult PrepareEpollCtlInternal(int epfd, int fd, int op, epoll_event* ev, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var acquireResult = NextSubmissionQueueEntry(out var sqe);
            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;

            unchecked
            {
                sqe->opcode = IORING_OP_EPOLL_CTL;
                sqe->flags = (byte) options;
                sqe->fd = epfd;
                sqe->addr = (ulong) ev;
                sqe->len = (uint) op;
                sqe->user_data = userData;
                sqe->personality = personality;
            }

            return SubmissionAcquireResult.SubmissionAcquired;
        }

    }
}
