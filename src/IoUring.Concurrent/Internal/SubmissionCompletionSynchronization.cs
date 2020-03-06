using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;

namespace IoUring.Internal
{
    internal static class SubmissionCompletionSynchronization
    {
        public static void Synchronize(int ringFd, ConcurrentSubmissionQueue sq, ConcurrentCompletionQueue cq)
        {
            uint toSubmit;
            bool shouldEnterToSubmit;
            uint enterFlags;
            bool block;
            bool ioPolled = sq.IoPolled;

            lock (sq.Gate)
            {
                toSubmit = sq.Notify();

                shouldEnterToSubmit = sq.ShouldEnter(out enterFlags);

                block = toSubmit == 0; // block until something happens (unblock)
                if (block)
                {
                    sq.ShouldUnblock = true;
                }
            }

            bool enter = (toSubmit > 0 && shouldEnterToSubmit) || // enter to submit pending entries
                         block ||                                 // enter to block
                         ioPolled;                                // enter to reap polled I/O completions

            if (!enter)
            {
                return;
            }

            if (block || ioPolled)
            {
                enterFlags |= IORING_ENTER_GETEVENTS;
            }

            uint minComplete = 0;
            if (block)
            {
                lock (cq.Gate)
                {
                    if (!cq.EventsAvailable)
                    {
                        minComplete = 1;
                    }
                    SafeEnter(ringFd, toSubmit, minComplete, enterFlags);
                }
            }
            else
            {
                SafeEnter(ringFd, toSubmit, minComplete, enterFlags);
            }
        }
    }
}