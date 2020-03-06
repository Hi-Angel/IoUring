using System;
using System.Threading;
using Tmds.Linux;

namespace IoUring.Internal
{
    internal sealed class AsyncOperation
    {
        private readonly AsyncOperationPool _pool;
        private int _res;

        public AsyncOperation(AsyncOperationPool pool) => _pool = pool;

        public object? State { get; set; }
        public Action<object?, int>? Callback { get; set; }
        public unsafe io_uring_sqe* Sqe { get; set; }
        public void RunInline(int res)
        {
            Callback!(State, res);
            Return();
        }

        public void ScheduleExecution(int res)
        {
            _res = res;
            ThreadPool.UnsafeQueueUserWorkItem(state => Complete(state), this, false);
        }

        private static void Complete(AsyncOperation state)
        {
            state.Callback!(state.State, state._res);
            state.Return();
        }

        private void Return() => _pool.Return(this);
    }
}