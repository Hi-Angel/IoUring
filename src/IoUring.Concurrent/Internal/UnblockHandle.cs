using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal sealed unsafe class UnblockHandle : IDisposable
    {
        private readonly ConcurrentRing _ring;
        private int _eventfd;
        private GCHandle _eventfdBytes;
        private GCHandle _eventfdIoVecHandle;
        private iovec* _eventfdIoVec;

        public UnblockHandle(ConcurrentRing ring)
        {
            _ring = ring;

            int res = eventfd(0, EFD_CLOEXEC);
            if (res == -1) throw new ErrnoException(errno);
            _eventfd = res;

            // Pin buffer for eventfd reads via io_uring
            byte[] bytes = new byte[sizeof(ulong)];
            _eventfdBytes = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            // Pin iovec used for eventfd reads via io_uring
            var eventfdIoVec = new iovec
            {
                iov_base = (void*) _eventfdBytes.AddrOfPinnedObject(),
                iov_len = bytes.Length
            };
            _eventfdIoVecHandle = GCHandle.Alloc(eventfdIoVec, GCHandleType.Pinned);
            _eventfdIoVec = (iovec*) _eventfdIoVecHandle.AddrOfPinnedObject();
        }

        public void Register()
        {
            var eventfd = Volatile.Read(ref _eventfd);
            if (eventfd == 0) return; // race with dispose -> ignore

            _ring.SubmitReadWrite(IORING_OP_READV, eventfd, _eventfdIoVec, 1, default, 0, (state, res) =>
            {
                Debug.Assert(state is UnblockHandle);
                if (res == sizeof(long) || res == -EINTR)
                {
                    ((UnblockHandle)state!).Register();
                }
                else if (res == -EBADFD && Volatile.Read(ref  ((UnblockHandle)state!)._eventfd) == 0)
                {
                    // interrupted by dispose -> ignore
                }
                else
                {
                    ThrowErrnoException(-res);
                }
            }, this, SubmissionOption.None);
        }

        public void Unblock()
        {
            var eventfd = Volatile.Read(ref _eventfd);
            if (eventfd == 0) return; // race with dispose -> ignore

            byte* val = stackalloc byte[sizeof(ulong)];
            Unsafe.WriteUnaligned(val, 1UL);
            int rv;
            do
            {
                rv = (int) write(eventfd, val, sizeof(ulong));
            } while (rv == -1 && errno == EINTR);

            if (rv == -1)
            {
                if (errno == EBADFD && Volatile.Read(ref _eventfd) == 0) return; // race with dispose -> ignore
                ThrowErrnoException(errno);
            }

            Debug.Assert(rv == 8);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _eventfd, 0) == 0)
                return; // Already disposed

            close(_eventfd);
            _eventfdIoVec = (iovec*) 0;
            _eventfdIoVecHandle.Free();
            _eventfdBytes.Free();
        }
    }
}