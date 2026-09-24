using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class MacOSHidIOFacadeTests
{
    [Fact]
    public void ReportsUseNativeDescriptorSizesAndFixedFidoPackets()
    {
        var bridge = new Bridge { InputSizeValue = 65, OutputSizeValue = 72 };
        using var connection = new MacOSHidIOReportConnection(42, bridge);
        Assert.Equal(65, connection.InputReportSize);
        Assert.Equal(72, connection.OutputReportSize);
        var output = new byte[72]; output[0] = 23;
        connection.SetReport(output);
        Assert.Equal((byte)23, bridge.Output?.Span[0]);
        Assert.Equal(72, bridge.Output?.Length);
        connection.SetReport([]);
        Assert.Equal(0, bridge.Output?.Length);
        var input = new byte[65]; input[1] = 19;
        bridge.Emit(input);
        var received = connection.GetReport();
        Assert.Equal(64, received.Length);
        Assert.Equal((byte)19, received[0]);
    }

    [Fact]
    public void FailedOpenReleasesDeviceWithoutStartingInput()
    {
        var bridge = new Bridge { OpenStatus = -1 };
        var failure = Assert.Throws<Yubico.YubiKit.Core.Native.PlatformApiException>(() => new MacOSHidIOReportConnection(42, bridge));
        Assert.IsType<InvalidOperationException>(failure.InnerException);
        Assert.Equal(1, bridge.ReleaseCount);
        Assert.Equal(0, bridge.StartCount);
    }

    [Fact]
    public void DisposalTwiceReleasesOnlyOnce()
    {
        var bridge = new Bridge();
        var connection = new MacOSHidIOReportConnection(42, bridge);
        connection.Dispose();
        connection.Dispose();
        Assert.Equal(1, bridge.ReleaseCount);
    }

    private sealed class Bridge : IHidInputBridge
    {
        private Action<ReadOnlyMemory<byte>>? _report;
        public int InputSizeValue = 64;
        public int OutputSizeValue = 64;
        public int OpenStatus;
        public int StartCount;
        public int ReleaseCount;
        public ReadOnlyMemory<byte>? Output;
        public void Emit(byte[] packet) => (_report ?? throw new InvalidOperationException())(packet);
        public nint CreateDevice(long entryId) => 1;
        public void OpenDevice(nint device) { }
        public int OpenDeviceResult(nint device) => OpenStatus;
        public int InputSize(nint device) => InputSizeValue;
        public int OutputSize(nint device) => OutputSizeValue;
        public bool CloseUnstarted(nint device) => true;
        public void ReleaseDevice(nint device) => Interlocked.Increment(ref ReleaseCount);
        public nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
        { _report = report; return 2; }
        public int Start(nint owner) { StartCount++; return 0; }
        public void Cancel(nint owner) { }
        public int WaitShutdown(nint owner) => 0;
        public int Destroy(nint owner) => 0;
        public void SetReport(nint device, byte[] report) => Output = report.AsMemory().ToArray();
    }
}