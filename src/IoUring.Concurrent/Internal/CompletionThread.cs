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
        private readonly OneShotEvent? _unblockEvent;
        private readonly ConcurrentDictionary<ulong, AsyncOperation> _asyncOperations;
        private readonly bool _runContinuationsAsynchronously;
        private Thread? _thread;
        private volatile bool _isDisposed;

        public CompletionThread(ConcurrentRing ring, Barrier barrier, bool isBoss, OneShotEvent? unblockEvent, ConcurrentDictionary<ulong, AsyncOperation> asyncOperations, bool runContinuationsAsynchronously)
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
                RunCompletions(_ring.Read());
            }
            catch (ErrnoException)
            {
                lock (_barrier)
                {
                    _barrier.Dispose();
                }
                throw;
            }
        }

        private void WaitForOtherThreads()
        {
            try
            {
                // ReSharper disable once InconsistentlySynchronizedField - only Barrier.Dispose() is not thread safe.
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
                try
                {
                    operation.RunInline(result);
                }
                catch (Exception)
                {
                    /* swallow all errors */
                }
                finally
                {
                    operation.Return();
                }
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            _unblockEvent?.Write();
            _thread?.Join();
            _barrier.Dispose();
        }
    }
}