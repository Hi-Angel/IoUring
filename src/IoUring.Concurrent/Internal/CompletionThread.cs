using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace IoUring.Internal
{
    internal sealed class CompletionThread : IDisposable
    {
        private static int _threadId = -1;
        private readonly ConcurrentRing _ring;
        private readonly Barrier _barrier;
        private readonly bool _isBoss;
        private readonly UnblockHandle? _unblockEvent;
        private readonly ConcurrentDictionary<ulong, AsyncOperation> _asyncOperations;
        private readonly bool _runContinuationsAsynchronously;
        private Thread? _thread;
        private volatile bool _isDisposed;

        public CompletionThread(ConcurrentRing ring, Barrier barrier, bool isBoss, UnblockHandle? unblockEvent, ConcurrentDictionary<ulong, AsyncOperation> asyncOperations, bool runContinuationsAsynchronously)
        {
            _ring = ring;
            _barrier = barrier;
            _isBoss = isBoss;
            _unblockEvent = unblockEvent;
            _asyncOperations = asyncOperations;
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
        }

        public void Run()
        {
            Thread thread = new Thread(o => ((CompletionThread) o!).Loop())
            {
                Name = $"IoUring Completion Thread - {Interlocked.Increment(ref _threadId)}",
                IsBackground = true
            };

            thread.Start(this);
            _thread = thread;
        }

        private void Loop()
        {
            var isBoss = _isBoss;

            while (!_isDisposed)
            {
                if (isBoss) ReapCompletion();

                WaitForOtherThreads();

                while (_ring.TryRead(out var completion))
                    RunCompletions(completion);
            }
        }

        private void ReapCompletion()
        {
            try
            {
                _ring.Synchronize();
            }
            catch (ErrnoException)
            {
                _barrier.Dispose();
                throw;
            }
        }

        private void WaitForOtherThreads()
        {
            try
            {
                _barrier.SignalAndWait();
            }
            catch (ObjectDisposedException)
            {
                _isDisposed = true;
            }
        }

        private void RunCompletions(Completion completion)
        {
            var (result, reference) = completion;

            if (_asyncOperations.Remove(reference, out var operation))
                CompleteAsyncOperation(operation, result);
        }

        private void CompleteAsyncOperation(AsyncOperation operation, int result)
        {
            if (_runContinuationsAsynchronously)
            {
                operation.ScheduleExecution(result);
            }
            else
            {
                operation.RunInline(result);
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            _unblockEvent?.Unblock();
            _thread?.Join();
            _barrier.Dispose();
        }
    }
}