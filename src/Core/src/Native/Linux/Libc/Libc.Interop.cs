// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.Native.Linux.Libc;

// glibc libc.so.6 ABI. Raw descriptor ints are hidden behind retained SafeHandle
// wrappers; these synchronous entry points do not establish worker ownership.
internal static partial class NativeMethods
{

    [Flags]
    public enum OpenFlags
    {
        O_RDONLY = 0,
        O_WRONLY = 1,
        O_RDWR = 2,
        O_NONBLOCK = 0x800,
        O_CLOEXEC = 0x80000
    }



    /// <summary>
    /// Structure for poll() file descriptor monitoring.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PollFd
    {
        public int fd;
        public short events;
        public short revents;
    }

    /// <summary>There is data to read.</summary>
    public const short POLLIN = 0x0001;

    /// <summary>There is urgent data to read (e.g., out-of-band data on TCP socket).</summary>
    public const short POLLPRI = 0x0002;

    /// <summary>Writing is now possible.</summary>
    public const short POLLOUT = 0x0004;

    /// <summary>Error condition (only returned in revents; ignored in events).</summary>
    public const short POLLERR = 0x0008;

    /// <summary>Hang up (only returned in revents; ignored in events).</summary>
    public const short POLLHUP = 0x0010;

    /// <summary>Invalid request: fd not open (only returned in revents; ignored in events).</summary>
    public const short POLLNVAL = 0x0020;

    /// <summary>Close the descriptor on exec.</summary>
    public const int EFD_CLOEXEC = 0x80000;

    /// <summary>Open the event fd in non-blocking mode.</summary>
    public const int EFD_NONBLOCK = 0x800;

    /// <summary>EINTR: interrupted system call.</summary>
    public const int EINTR = 4;

    /// <summary>EAGAIN/EWOULDBLOCK: no data available on a non-blocking descriptor.</summary>
    public const int EAGAIN = 11;

    /// <summary>
    /// Wait for events on the specified file descriptors.
    /// </summary>
    /// <param name="fds">Array of PollFd structures describing the file descriptors to monitor.</param>
    /// <param name="nfds">Number of items in the fds array.</param>
    /// <param name="timeout">Timeout in milliseconds. -1 means infinite, 0 means return immediately.</param>
    /// <returns>Number of file descriptors with events, 0 on timeout, -1 on error (check errno).</returns>
    [LibraryImport(Libraries.LinuxKernelLib, EntryPoint = "poll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int PollNative([In, Out] PollFd[] fds, nuint nfds, int timeout);

    public static int Poll(PollFd[] fds, int nfds, int timeout)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nfds);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nfds, fds.Length);
        return PollNative(fds, (nuint)nfds, timeout);
    }

    [LibraryImport(Libraries.LinuxKernelLib, EntryPoint = "eventfd", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int EventFdNative(uint initval, int flags);

    public static LinuxEventFdSafeHandle EventFd(uint initval, int flags)
    {
        var result = new LinuxEventFdSafeHandle();
        int fd = EventFdNative(initval, flags);
        int error = Marshal.GetLastPInvokeError();
        result.Initialize(fd);
        Marshal.SetLastPInvokeError(error);
        return result;
    }


    public const long HIDIOCGRAWINFO = 0x0000000080084803;
    public const long HIDIOCGRDESCSIZE = 0x0000000080044801;

    public const long HIDIOCGRDESC = 0x0000000090044802;

    // The FEATURE flags need to be combined with the buffer size. With "CG"
    // (Get), the buffer size is the size of the buffer into which the result
    // will be placed. With "CS" (Set), the buffer size is the size of the
    // data being sent.
    // The generic Linux ioctl size field occupies bits 16 - 29.
    // For example, to build the ioctl flag for Get with a buffer of 256
    // bytes, use
    //   HIDIOCGFEATURE | (256 << 16);
    // Make sure the buffer size is not greater than the maximum.
    public const long HIDIOCGFEATURE = 0x00000000C0004807;
    public const long HIDIOCSFEATURE = 0x00000000C0004806;
    public const int MaxFeatureBufferSize = 256;
    public const int InfoSize = 8;
    public const int DescriptorSizeSize = 4;
    public const int DescriptorSize = 4100;

    // Where the fields in the hidraw_devinfo struct are, and how many bytes
    // they are.
    public const int OffsetInfoVendor = 4;
    public const int OffsetInfoProduct = 6;

    // Where the fields in the hidraw_report_descriptor struct are, how many
    // bytes make up the size field, and the number of bytes in the value
    // buffer.
    public const int OffsetDescSize = 0;
    public const int OffsetDescValue = 4;

    // Note that the DefaultDllImportSearchPaths attribute is a security best
    // practice on the Windows platform (and required by our analyzer
    // settings). It does not currently have any effect on platforms other
    // than Windows, but is included because of the analyzer and in the hope
    // that it will be supported by these platforms in the future.
    [LibraryImport(Libraries.LinuxKernelLib, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "open",
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int OpenNative(string filename, OpenFlags flag);

    public static LinuxFileSafeHandle Open(string filename, OpenFlags flag)
    {
        // Allocate before acquiring the fd. SafeHandle's default native return marshaller uses
        // IntPtr, which is not the ABI of an int-returning libc function on 64-bit Linux.
        var result = new LinuxFileSafeHandle();
        int fd = OpenNative(filename, flag | OpenFlags.O_CLOEXEC);
        int error = Marshal.GetLastPInvokeError();
        result.Initialize(fd);
        Marshal.SetLastPInvokeError(error);
        return result;
    }

    // This will be called from within the SafeHandle class, but should be
    // called by no one else.
    [LibraryImport(Libraries.LinuxKernelLib, EntryPoint = "close", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial int CloseNative(int fd);

    // Metadata requests (info, descriptor size, descriptor) use the legacy pointer
    // wrapper below; report feature requests use the span wrapper and include the ID.
    // Pass in one of the HIDIOCG values as the request. Pass in a buffer
    // that is the corresponding Size bytes long, that will accept the
    // result. For example, to get the RAWINFO, create an IntPtr with space
    // for the size of the data struct that is the output, call the method,
    // then copy the returned data into a managed buffer.
    // Parse the bytes after the call to extract the information, using the
    // Offset const values to find the locations of the data in the buffer
    // where the targets are located.
    [LibraryImport(Libraries.LinuxKernelLib, EntryPoint = "ioctl", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static unsafe partial int IoctlNative(int fd, nuint request, void* result);

    public static unsafe int Ioctl(LinuxFileSafeHandle handle, long request, IntPtr result)
    {
        using var lease = new LinuxDescriptorLease(handle);
        return IoctlNative(lease.Descriptor, checked((nuint)request), (void*)result);
    }

    internal static unsafe int Ioctl(LinuxFileSafeHandle handle, nuint request, Span<byte> buffer)
    {
        using var lease = new LinuxDescriptorLease(handle);
        fixed (byte* pointer = buffer)
            return IoctlNative(lease.Descriptor, request, pointer);
    }

    // Read count bytes. Place them into outputBuffer.
    [LibraryImport(Libraries.LinuxKernelLib, EntryPoint = "read", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static unsafe partial nint ReadNative(int fd, byte* outputBuffer, nuint count);

    public static int Read(LinuxFileSafeHandle handle, byte[] outputBuffer, int count) =>
        checked((int)Read(handle, outputBuffer.AsSpan(0, count)));

    internal static unsafe nint Read(LinuxFileSafeHandle handle, Span<byte> outputBuffer)
    {
        using var lease = new LinuxDescriptorLease(handle);
        fixed (byte* pointer = outputBuffer)
            return ReadNative(lease.Descriptor, pointer, (nuint)outputBuffer.Length);
    }

    // Write the count bytes in inputBuffer.
    [LibraryImport(Libraries.LinuxKernelLib, EntryPoint = "write", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static unsafe partial nint WriteNative(int fd, byte* inputBuffer, nuint count);

    public static int Write(LinuxFileSafeHandle handle, byte[] inputBuffer, int count) =>
        checked((int)Write(handle, inputBuffer.AsSpan(0, count)));

    internal static unsafe nint Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> inputBuffer)
    {
        using var lease = new LinuxDescriptorLease(handle);
        fixed (byte* pointer = inputBuffer)
            return WriteNative(lease.Descriptor, pointer, (nuint)inputBuffer.Length);
    }

    [LibraryImport(Libraries.LinuxKernelLib, EntryPoint = "fcntl", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int FcntlNative(int fd, int cmd, int flags);

    internal static int Fcntl(LinuxFileSafeHandle handle, int command, int flags = 0)
    {
        using var lease = new LinuxDescriptorLease(handle);
        return FcntlNative(lease.Descriptor, command, flags);
    }
}