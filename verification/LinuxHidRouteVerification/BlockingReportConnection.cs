using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;

namespace LinuxHidRouteVerification;

// Negative control only: this is not native evidence. It executes the real legacy sync adapter.
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