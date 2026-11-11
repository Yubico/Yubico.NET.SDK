using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Transports.Hid.Linux;

namespace LinuxHidRouteVerification;

// Every call delegates to real production libc: counts are observation, not simulated readiness.
internal sealed class ObservedReadinessNative : ILinuxHidReadinessNative
{
    private readonly LinuxHidReadinessNative _real = new();
    private int _polls;
    private int _writes;
    private int _reads;
    internal int Polls => Volatile.Read(ref _polls);
    internal int Writes => Volatile.Read(ref _writes);
    internal int Reads => Volatile.Read(ref _reads);
    public LinuxFileSafeHandle CreateWake() => _real.CreateWake();
    public LinuxIoResult Poll(NativeMethods.PollFd[] descriptors)
    {
        _ = Interlocked.Increment(ref _polls);
        return _real.Poll(descriptors);
    }
    public LinuxIoResult WriteWake(LinuxFileSafeHandle wake)
    {
        _ = Interlocked.Increment(ref _writes);
        return _real.WriteWake(wake);
    }
    public LinuxIoResult ReadWake(LinuxFileSafeHandle wake)
    {
        _ = Interlocked.Increment(ref _reads);
        return _real.ReadWake(wake);
    }
}