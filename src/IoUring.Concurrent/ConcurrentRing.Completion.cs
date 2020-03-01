using IoUring.Internal;

namespace IoUring
{
    public partial class ConcurrentRing
    {
        private readonly ConcurrentCompletionQueue _cq;
        private readonly UnmapHandle _cqHandle;

        /// <summary>
        /// Checks whether a Completion Queue Event is available.
        /// </summary>
        /// <param name="result">The data from the observed Completion Queue Event if any</param>
        /// <returns>Whether a Completion Queue Event was observed</returns>
        /// <exception cref="ErrnoException">If a syscall failed</exception>
        /// <exception cref="CompletionQueueOverflowException">If an overflow in the Completion Queue occurred</exception>
        internal bool TryRead(out Completion result)
            => _cq.TryRead(_ringFd.DangerousGetHandle().ToInt32(), out result);

        /// <summary>
        /// Reads, blocking if required, for a Completion Queue Event.
        /// </summary>
        /// <returns>The read Completion Queue Event</returns>
        /// <exception cref="ErrnoException">If a syscall failed</exception>
        /// <exception cref="CompletionQueueOverflowException">If an overflow in the Completion Queue occurred</exception>
        internal Completion Read()
            => _cq.Read(_ringFd.DangerousGetHandle().ToInt32());
    }
}