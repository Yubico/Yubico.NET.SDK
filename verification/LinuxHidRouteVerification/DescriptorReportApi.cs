using System.Diagnostics;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Transports.Hid.Linux;

namespace LinuxHidRouteVerification;

// Narrow report seam: open transfers a real socket fd, metadata is synthetic FIDO64.
// Read and report framing/output still execute production L1 libc, readiness uses production L2.
internal sealed class DescriptorReportApi(NativeDescriptors descriptors) : ILinuxHidReportApi, IDisposable
{
    private readonly ILinuxHidReportApi _real = LinuxHidReportApi.Instance;
    internal ManualResetEventSlim OutputEntered { get; } = new();
    internal ManualResetEventSlim OutputRelease { get; } = new();
    internal bool HoldOutput { get; set; }
    internal int OutputThread { get; private set; }
    internal int CloseThread { get; private set; }
    internal long NativeWriteTicks { get; private set; }
    internal int ReadCalls { get; private set; }
    internal int WriteCalls { get; private set; }
    internal LinuxReleaseOutcome Release { get; private set; }
    internal int PendingOutput => OutputEntered.IsSet && !OutputRelease.IsSet && HoldOutput ? 1 : 0;

    public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags) => descriptors.Input;
    public (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle) => (64, 64);
    public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload)
    {
        ReadCalls++;
        return _real.TryRead(handle, payload);
    }
    public void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
    {
        OutputThread = Environment.CurrentManagedThreadId;
        WriteCalls++;
        OutputEntered.Set();
        if (HoldOutput) OutputRelease.Wait();
        long start = Stopwatch.GetTimestamp();
        _real.Write(handle, payload);
        NativeWriteTicks += Stopwatch.GetTimestamp() - start;
    }
    public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload) => throw new NotSupportedException("Not an OTP fixture");
    public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload) => throw new NotSupportedException("Not an OTP fixture");
    public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle)
    {
        CloseThread = Environment.CurrentManagedThreadId;
        Release = _real.CloseOnce(handle);
        return Release;
    }
    public void Dispose()
    {
        OutputRelease.Set();
        OutputEntered.Dispose();
        OutputRelease.Dispose();
    }
}