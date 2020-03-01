using System.Threading;
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
        [InlineData(4096, 1, false)]
        [InlineData(4096, 1, true)]
        [InlineData(4096, 4, false)]
        [InlineData(4096, 4, true)]
        public void SmokeTest(int ringSize, int threadCount, bool asyncContinuations)
        {
            var barrier = new CountdownEvent(ringSize - 1);
            using var ring = new ConcurrentRing(ringSize, threadCount, asyncContinuations);

            for (int i = 0; i < ringSize - 1; i++)
            {
                ring.SubmitNop((state, result) =>
                {
                    ((CountdownEvent) state).Signal();
                    Assert.Equal(0, result);
                }, barrier);
            }

            uint submitted;
            do
            {
                ring.SubmitAndWait(0, out submitted);
            } while (submitted != 0);

            barrier.Wait();
        }
    }
}
