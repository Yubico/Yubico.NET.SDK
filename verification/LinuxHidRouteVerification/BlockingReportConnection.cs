using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;

namespace LinuxHidRouteVerification;

/// <summary>Controlled report connection for the caller-blocking negative control; not native evidence.</summary>
/// <remarks>
/// The verifier wraps this fake in the retained synchronous
/// <see cref="Yubico.YubiKit.Core.Protocols.Fido.Hid.FidoHidConnection" /> adapter,
/// not the built-in Linux async connection. Holding <see cref="GetReport" /> proves the responsiveness
/// gate detects a blocking adapter. The adapter remains supported for custom report providers;
/// built-in Windows bypasses it. This fixture is not obsolete production Linux code to remove.
/// </remarks>
internal sealed class BlockingReportConnection : IHidConnection
{
    private int _disposed;
    internal ManualResetEventSlim Entered { get; } = new();
    internal ManualResetEventSlim Release { get; } = new();
    internal int EntryThread { get; private set; }
    public int InputReportSize => 64;
    public int OutputReportSize => 64;
    public ConnectionType Type => ConnectionType.Hid;
    public byte[] GetReport()
    {
        EntryThread = Environment.CurrentManagedThreadId;
        Entered.Set();
        Release.Wait();
        return new byte[64];
    }
    public void SetReport(byte[] report) => throw new NotSupportedException();
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Release.Set();
        Entered.Dispose();
        Release.Dispose();
    }
    public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
}