using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal unsafe class OneShotEvent : IDisposable
    {
        private readonly ConcurrentRing _ring;
        private int _eventfd;
        private GCHandle _eventfdBytes;
        private GCHandle _eventfdIoVecHandle;
        private iovec* _eventfdIoVec;

        public OneShotEvent(ConcurrentRing ring)
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

        public void Read()
        {
            if (_eventfd == 0) throw new ObjectDisposedException(nameof(OneShotEvent));

            _ring.SubmitReadWrite(IORING_OP_READV, _eventfd, _eventfdIoVec, 1, default, 0, (state, res) =>
            {
                if (res == sizeof(long))
                {
                    ((OneShotEvent)state).Dispose();
                }
                else if (res == -EINTR)
                {
                    ((OneShotEvent)state).Read();
                }
                else
                {
                    ThrowErrnoException(-res);
                }
            }, this, SubmissionOption.None);
        }

        public void Write()
        {
            if (_eventfd == 0) throw new ObjectDisposedException(nameof(OneShotEvent));

            byte* val = stackalloc byte[sizeof(ulong)];
            Unsafe.WriteUnaligned(val, 1UL);
            int rv;
            do
            {
                rv = (int) write(_eventfd, val, sizeof(ulong));
            } while (rv == -1 && errno == EINTR);

            if (rv == -1) ThrowErrnoException(errno);
        }

        public void Dispose()
        {
            close(_eventfd);
            _eventfd = 0;
            _eventfdIoVec = (iovec*) 0;
            if (_eventfdIoVecHandle.IsAllocated)
                _eventfdIoVecHandle.Free();
            if (_eventfdBytes.IsAllocated)
                _eventfdBytes.Free();
        }
    }
}