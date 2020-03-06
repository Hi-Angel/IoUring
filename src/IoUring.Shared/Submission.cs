using System.Runtime.InteropServices;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Submission
    {
        [FieldOffset(0)]
        private  byte opcode;
        [FieldOffset(1)]
        private byte flags;
        [FieldOffset(2)]
        private ushort ioprio;
        [FieldOffset(4)]
        private int fd;
        [FieldOffset(8)]
        private ulong off;
        [FieldOffset(8)]
        private ulong addr2;
        [FieldOffset(16)]
        private ulong addr;
        [FieldOffset(24)]
        private uint len;
        [FieldOffset(28)]
        private int rw_flags;
        [FieldOffset(28)]
        private uint fsync_flags;
        [FieldOffset(28)]
        private ushort poll_events;
        [FieldOffset(28)]
        private uint sync_range_flags;
        [FieldOffset(28)]
        private uint msg_flags;
        [FieldOffset(28)]
        private uint timeout_flags;
        [FieldOffset(28)]
        private uint accept_flags;
        [FieldOffset(28)]
        private uint cancel_flags;
        [FieldOffset(32)]
        private ulong user_data;
        [FieldOffset(40)]
        private ushort buf_index;
        [FieldOffset(40)]
        private ulong __pad2;
        [FieldOffset(48)]
        private ulong __pad3;
        [FieldOffset(56)]
        private ulong __pad4;

        public static Submission Nop(ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_NOP;
                s.flags = (byte) options;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission ReadV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_READV;
                s.flags = (byte) options;
                s.fd = fd;
                s.off = (ulong) (long) offset;
                s.addr = (ulong) iov;
                s.len = (uint) count;
                s.rw_flags = flags;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission WriteV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_WRITEV;
                s.flags = (byte) options;
                s.fd = fd;
                s.off = (ulong) (long) offset;
                s.addr = (ulong) iov;
                s.len = (uint) count;
                s.rw_flags = flags;
                s.user_data = userData;
            }

            return s;
        }

        public static Submission Fsync(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_FSYNC;
                s.flags = (byte) options;
                s.fd = fd;
                s.fsync_flags = (uint) fsyncOptions;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission ReadFixed(int fd, void* buf, size_t count, int index, off_t offset = default,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_READ_FIXED;
                s.flags = (byte) options;
                s.fd = fd;
                s.off = (ulong) (long) offset;
                s.addr = (ulong) buf;
                s.len = (uint) count;
                s.buf_index = (ushort) index;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission WriteFixed(int fd, void* buf, size_t count, int index, off_t offset = default,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_WRITE_FIXED;
                s.flags = (byte) options;
                s.fd = fd;
                s.off = (ulong) (long) offset;
                s.addr = (ulong) buf;
                s.len = (uint) count;
                s.buf_index = (ushort) index;
                s.user_data = userData;
            }

            return s;
        }

        public static Submission PollAdd(int fd, ushort pollEvents, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_POLL_ADD;
                s.flags = (byte) options;
                s.fd = fd;
                s.poll_events = pollEvents;
                s.user_data = userData;
            }

            return s;
        }

        public static Submission PollRemove(ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_POLL_REMOVE;
                s.flags = (byte) options;
                s.user_data = userData;
            }

            return s;
        }

        public static Submission SyncFileRange(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_SYNC_FILE_RANGE;
                s.flags = (byte) options;
                s.fd = fd;
                s.off = (ulong) (long) offset;
                s.len = (uint) count;
                s.sync_range_flags = flags;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission SendMsg(int fd, msghdr* msg, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_SENDMSG;
                s.flags = (byte) options;
                s.fd = fd;
                s.addr = (ulong) msg;
                s.len = 1;
                s.msg_flags = (uint) flags;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission RecvMsg(int fd, msghdr* msg, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_RECVMSG;
                s.flags = (byte) options;
                s.fd = fd;
                s.addr = (ulong) msg;
                s.len = 1;
                s.msg_flags = (uint) flags;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission Timeout(timespec *ts, uint count = 1, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_TIMEOUT;
                s.flags = (byte) options;
                s.fd = -1;
                s.off = count;
                s.addr = (ulong) ts;
                s.len = 1;
                s.timeout_flags = (uint) timeoutOptions;
                s.user_data = userData;
            }

            return s;
        }

        public static Submission TimeoutRemove(ulong timeoutUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_TIMEOUT_REMOVE;
                s.flags = (byte) options;
                s.fd = -1;
                s.addr = timeoutUserData;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission Accept(int fd, sockaddr *addr, socklen_t *addrLen, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_ACCEPT;
                s.flags = (byte) options;
                s.fd = fd;
                s.off = (ulong) (long) addrLen;
                s.addr = (ulong) addr;
                s.rw_flags = flags;
                s.user_data = userData;
            }

            return s;
        }

        public static Submission Cancel(ulong opUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_ASYNC_CANCEL;
                s.flags = (byte) options;
                s.fd = -1;
                s.addr = opUserData;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission Connect(int fd, sockaddr* addr, socklen_t addrLen, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_CONNECT;
                s.flags = (byte) options;
                s.fd = fd;
                s.off = addrLen;
                s.addr = (ulong) addr;
                s.user_data = userData;
            }

            return s;
        }

        public static unsafe Submission LinkTimeout(timespec* ts, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            Submission s = default;

            unchecked
            {
                s.opcode = IORING_OP_LINK_TIMEOUT;
                s.flags = (byte) options;
                s.fd = -1;
                s.addr = (ulong) ts;
                s.len = 1;
                s.timeout_flags = (uint) timeoutOptions;
                s.user_data = userData;
            }

            return s;
        }
    }
}