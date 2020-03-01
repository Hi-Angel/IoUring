using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace IoUring.Internal
{
    internal sealed class AsyncOperationPool
    {
        private readonly Queue<AsyncOperation> _queue = new Queue<AsyncOperation>();

        public object? Gate { get; set; }

        public AsyncOperation Rent()
        {
            Debug.Assert(Gate != null);
            Debug.Assert(Monitor.IsEntered(Gate));

            if (_queue.TryDequeue(out var op)) return op;

            return new AsyncOperation(this);
        }

        public void Return(AsyncOperation op)
        {
            Debug.Assert(Gate != null);

            lock (Gate)
            {
                _queue.Enqueue(op);
            }
        }
    }
}