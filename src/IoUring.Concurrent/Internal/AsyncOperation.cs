using System;
using System.Threading;

namespace IoUring.Internal
{
    internal sealed class AsyncOperation
    {
        private readonly AsyncOperationPool _pool;
        private int _res;

        public AsyncOperation(AsyncOperationPool pool) => _pool = pool;

        public object? State { get; set; }
        public Action<object, int>? Callback { get; set; }

        public void RunInline(int res) => Callback!(State!, res);

        public void ScheduleExecution(int res)
        {
            _res = res;
            ThreadPool.UnsafeQueueUserWorkItem(state => Complete(state), this, false);
            Return();
        }

        private static void Complete(AsyncOperation state)
        {
            state.Callback!(state.State!, state._res);
            state.Return();
        }

        public void Return()
        {
            _pool.Return(this);
        }
    }
}