using System;
using System.Runtime.InteropServices;

namespace appbox.Server
{
    // https://blog.csdn.net/anonymalias/article/details/9219945
    // Linux ls /dev/shm 查看named semaphore

    public sealed class PosixSemaphore : IDisposable
    {
        private string name;
        private IntPtr sem;
        private bool own;

        private PosixSemaphore() { }

        public static PosixSemaphore Create(string name)
        {
            IntPtr semPtr = sem_open(name, (int)OpenFlags.O_CREAT,
                           (uint)(FilePermissions.S_IRUSR | FilePermissions.S_IWUSR), 0);
            if (semPtr == IntPtr.Zero)
            {
                throw new Exception($"Create PosixSemaphore failed. error = {Marshal.GetLastWin32Error()}");
            }

            var ps = new PosixSemaphore();
            ps.name = name;
            ps.sem = semPtr;
            ps.own = true;
            return ps;
        }

        public static PosixSemaphore Open(string name)
        {
            IntPtr semPtr = sem_open(name, (int)OpenFlags.O_CREAT, //TODO: fix OFlag
                            (uint)(FilePermissions.S_IRUSR | FilePermissions.S_IWUSR), 0);
            if (semPtr == IntPtr.Zero)
            {
                throw new Exception($"Open PosixSemaphore failed. error = {Marshal.GetLastWin32Error()}");
            }

            var ps = new PosixSemaphore();
            ps.name = name;
            ps.sem = semPtr;
            ps.own = false;
            return ps;
        }

        public void Wait()
        {
            if (sem == IntPtr.Zero)
                throw new Exception("Create or Open first.");

            sem_wait(sem);
        }

        public bool WaitOne(int timeout)
        {
            //TODO:
            Wait();
            return true;
        }

        public void Post()
        {
            if (sem == IntPtr.Zero)
                throw new Exception("Create or Open first.");

            sem_post(sem);
        }

        public int GetValue()
        {
            unsafe
            {
                int value = 0;
                sem_getvalue(sem, &value);
                return value;
            }
        }

        #region ====IDisposable====
        private bool disposedValue;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (sem != IntPtr.Zero)
                {
                    sem_close(sem);
                    if (own)
                        sem_unlink(name);
                    sem = IntPtr.Zero;
                }

                disposedValue = true;
            }
        }

        ~PosixSemaphore()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region ====Posix api====
        internal const string LIBRT = "librt";

        [DllImport(LIBRT, SetLastError = true)]
        private static extern IntPtr sem_open([MarshalAs(UnmanagedType.LPUTF8Str)]string name, int oflag);

        [DllImport(LIBRT, SetLastError = true)]
        private static extern IntPtr sem_open([MarshalAs(UnmanagedType.LPUTF8Str)]string name, int oflag, uint mode, uint value);

        [DllImport(LIBRT, SetLastError = true)]
        private static extern int sem_close(IntPtr sem);

        [DllImport(LIBRT, SetLastError = true)]
        private static extern int sem_unlink([MarshalAs(UnmanagedType.LPUTF8Str)]string name);

        [DllImport(LIBRT, SetLastError = true)]
        private static unsafe extern int sem_getvalue(IntPtr sem, int* value);

        [DllImport(LIBRT, SetLastError = true)]
        private static extern int sem_post(IntPtr sem);

        [DllImport(LIBRT, SetLastError = true)]
        private static extern int sem_wait(IntPtr sem);
        #endregion
    }

    #region ====Unix====
    [Flags]
    enum OpenFlags
    {
        //
        // One of these
        //
        O_RDONLY = 0x00000000,
        O_WRONLY = 0x00000001,
        O_RDWR = 0x00000002,

        //
        // Or-ed with zero or more of these
        //
        O_CREAT = 0x00000040,
        O_EXCL = 0x00000080,
        O_NOCTTY = 0x00000100,
        O_TRUNC = 0x00000200,
        O_APPEND = 0x00000400,
        O_NONBLOCK = 0x00000800,
        O_SYNC = 0x00001000,

        //
        // These are non-Posix.  Using them will result in errors/exceptions on
        // non-supported platforms.
        //
        // (For example, "C-wrapped" system calls -- calls with implementation in
        // MonoPosixHelper -- will return -1 with errno=EINVAL.  C#-wrapped system
        // calls will generate an exception in NativeConvert, as the value can't be
        // converted on the target platform.)
        //

        O_NOFOLLOW = 0x00020000,
        O_DIRECTORY = 0x00010000,
        O_DIRECT = 0x00004000,
        O_ASYNC = 0x00002000,
        O_LARGEFILE = 0x00008000,
        O_CLOEXEC = 0x00080000,
        O_PATH = 0x00200000
    }

    [Flags]
    enum FilePermissions : uint
    {
        S_ISUID = 0x0800, // Set user ID on execution
        S_ISGID = 0x0400, // Set group ID on execution
        S_ISVTX = 0x0200, // Save swapped text after use (sticky).
        S_IRUSR = 0x0100, // Read by owner
        S_IWUSR = 0x0080, // Write by owner
        S_IXUSR = 0x0040, // Execute by owner
        S_IRGRP = 0x0020, // Read by group
        S_IWGRP = 0x0010, // Write by group
        S_IXGRP = 0x0008, // Execute by group
        S_IROTH = 0x0004, // Read by other
        S_IWOTH = 0x0002, // Write by other
        S_IXOTH = 0x0001, // Execute by other

        S_IRWXG = (S_IRGRP | S_IWGRP | S_IXGRP),
        S_IRWXU = (S_IRUSR | S_IWUSR | S_IXUSR),
        S_IRWXO = (S_IROTH | S_IWOTH | S_IXOTH),
        ACCESSPERMS = (S_IRWXU | S_IRWXG | S_IRWXO), // 0777
        ALLPERMS = (S_ISUID | S_ISGID | S_ISVTX | S_IRWXU | S_IRWXG | S_IRWXO), // 07777
        DEFFILEMODE = (S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH), // 0666

        // Device types
        // Why these are held in "mode_t" is beyond me...
        S_IFMT = 0xF000, // Bits which determine file type
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Timespec
    {
        public long tv_nsec;
        public long tv_sec;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Timeval
    {
        public long tv_sec;
        public long tv_usec;

        public static Timeval FromTimeSpan(TimeSpan span)
        {
            Timeval tv = new Timeval();
            long nanoseconds;

            /* make sure we're dealing with a positive TimeSpan */
            span = span.Duration();

            nanoseconds = span.Ticks * 100;

            tv.tv_sec = (long)(nanoseconds / 1E+09);
            tv.tv_usec = (long)((nanoseconds % 1E+09) / 1000);

            return tv;
        }
    }
    #endregion
}
