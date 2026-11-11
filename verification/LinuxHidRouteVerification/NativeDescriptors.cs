using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Linux.Libc;

namespace LinuxHidRouteVerification;

// Host fixture only: real Linux datagram sockets, not hidraw or a physical authenticator.
internal sealed partial class NativeDescriptors : IDisposable
{
    internal LinuxFileSafeHandle Input { get; }
    internal LinuxFileSafeHandle Peer { get; }

    internal NativeDescriptors()
    {
        int[] descriptors = new int[2];
        if (SocketPair(1, 2 | 0x800 | 0x80000, 0, descriptors) != 0)
            throw new IOException($"socketpair errno={Marshal.GetLastPInvokeError()}");
        Input = new();
        Input.Initialize(descriptors[0]);
        Peer = new();
        Peer.Initialize(descriptors[1]);
    }

    [LibraryImport("libc.so.6", EntryPoint = "socketpair", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int SocketPair(int domain, int type, int protocol, [Out] int[] descriptors);

    internal void Send(byte value)
    {
        Span<byte> report = stackalloc byte[64];
        report.Fill(value);
        if (NativeMethods.Write(Peer, report) != 64) throw new IOException("socket write failed");
    }

    public void Dispose()
    {
        Input.Dispose();
        Peer.Dispose();
    }
}