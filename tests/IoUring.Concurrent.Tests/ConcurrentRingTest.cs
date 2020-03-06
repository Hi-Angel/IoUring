using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IoUring.Concurrent.Tests
{
    public class ConcurrentRingTest
    {
        [Theory]
        [InlineData(8, 1, false)]
        [InlineData(8, 1, true)]
        [InlineData(8, 4, false)]
        [InlineData(8, 4, true)]
        [InlineData(4096 << 2, 1, false)]
        [InlineData(4096 << 2, 1, true)]
        [InlineData(4096 << 2, 4, false)]
        [InlineData(4096 << 2, 4, true)]
        public void SmokeTest(int ringSize, int threadCount, bool asyncContinuations)
        {
            const int preoccupiedSqElements = 2;
            var barrier = new CountdownEvent(ringSize - preoccupiedSqElements);
            using var ring = new ConcurrentRing(ringSize, threadCount, asyncContinuations);

            for (int i = 0; i < ringSize - preoccupiedSqElements; i++)
            {
                ring.SubmitNop((state, result) =>
                {
                    ((CountdownEvent) state!).Signal();
                    Assert.Equal(0, result);
                }, barrier);
            }

            Assert.True(barrier.Wait(TimeSpan.FromSeconds(2)));
            barrier.Reset();

            for (int i = 0; i < ringSize - preoccupiedSqElements; i++)
            {
                ring.SubmitNop((state, result) =>
                {
                    ((CountdownEvent) state!).Signal();
                    Assert.Equal(0, result);
                }, barrier);
            }

            Assert.True(barrier.Wait(TimeSpan.FromSeconds(2)));
            Assert.True(Task.Run(() => ring.Dispose()).Wait(TimeSpan.FromSeconds(2)));
        }

        [Theory]
        [InlineData(8, 1, false)]
        [InlineData(8, 1, true)]
        [InlineData(8, 4, false)]
        [InlineData(8, 4, true)]
        [InlineData(4096 << 2, 1, false)]
        [InlineData(4096 << 2, 1, true)]
        [InlineData(4096 << 2, 4, false)]
        [InlineData(4096 << 2, 4, true)]
        public void SubmitLinked(int ringSize, int threadCount, bool asyncContinuations)
        {
            const int preoccupiedSqElements = 2;
            var barrier = new CountdownEvent(ringSize - preoccupiedSqElements);
            using var ring = new ConcurrentRing(ringSize, threadCount, asyncContinuations);
            Span<ulong> userData = stackalloc ulong[2];

            Action<object?, int> callback = (state, result) =>
            {
                ((CountdownEvent) state!).Signal();
                Assert.Equal(0, result);
            };

            for (int i = 0; i < (ringSize - preoccupiedSqElements) / 2; i++)
            {
                ring.SubmitMultiple(
                    Submission.Nop(options: SubmissionOption.Link), callback, barrier,
                    Submission.Nop(), callback, barrier,
                    userData
                );
            }

            Assert.True(barrier.Wait(TimeSpan.FromSeconds(2)));
            barrier.Reset();

            for (int i = 0; i < (ringSize - preoccupiedSqElements) / 2; i++)
            {
                ring.SubmitMultiple(
                    Submission.Nop(options: SubmissionOption.Link), callback, barrier,
                    Submission.Nop(), callback, barrier,
                    userData
                );
            }

            Assert.True(Task.Run(() => ring.Dispose()).Wait(TimeSpan.FromSeconds(2)));
        }
    }
}
