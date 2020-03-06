using Microsoft.Extensions.ObjectPool;

namespace IoUring.Internal
{
    internal class AsyncOperationPoolingStrategy : IPooledObjectPolicy<AsyncOperation>
    {
        private readonly AsyncOperationPool _pool;

        public AsyncOperationPoolingStrategy(AsyncOperationPool pool)
        {
            _pool = pool;
        }

        public AsyncOperation Create() => new AsyncOperation(_pool);

        public bool Return(AsyncOperation obj) => true;
    }

    internal sealed class AsyncOperationPool
    {
        private readonly ObjectPool<AsyncOperation> _pool;

        public AsyncOperationPool()
        {
            var poolingStrategy = new AsyncOperationPoolingStrategy(this);
            _pool = new DefaultObjectPool<AsyncOperation>(poolingStrategy);
        }

        public AsyncOperation Rent() => _pool.Get();

        public void Return(AsyncOperation op) => _pool.Return(op);
    }
}