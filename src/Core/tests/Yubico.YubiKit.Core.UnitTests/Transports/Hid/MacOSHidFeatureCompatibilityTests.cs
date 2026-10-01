using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class MacOSHidFeatureCompatibilityTests
{
    [Fact]
    public void ExpertReportsUseWorkerWithNativeMetadataAndArbitrarySetLength()
    {
        var native = new FeatureLifetime();
        using var connection = new MacOSHidFeatureReportConnection(42, native);
        Assert.Equal(17, connection.InputReportSize);
        Assert.Equal(23, connection.OutputReportSize);
        connection.SetReport([1, 2, 3, 4, 5]);
        Assert.Equal(new byte[8], connection.GetReport());
        Assert.Equal(new[] { "create", "open", "input", "output", "set", "get" }, native.Calls);
        Assert.Single(native.Threads.Distinct());
        Assert.NotEqual(Environment.CurrentManagedThreadId, native.Threads[0]);
        Assert.Equal(5, native.SetLength);
        Assert.Equal(8, native.GetBufferLength);
    }

    [Fact]
    public async Task PendingExpertSetDrainsBeforeAsyncDispose_AndConcurrentDisposeSharesClose()
    {
        var native = new FeatureLifetime { HoldSet = true };
        var connection = new MacOSHidFeatureReportConnection(42, native);
        var sending = Task.Run(() => connection.SetReport([1, 2, 3]));
        Assert.True(native.InSet.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        var disposal = connection.DisposeAsync().AsTask();
        var second = connection.DisposeAsync().AsTask();
        Assert.False(disposal.IsCompleted);
        Assert.False(second.IsCompleted);
        Assert.DoesNotContain("close", native.Calls);
        native.AllowSet.Set();
        await sending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await Task.WhenAll(disposal, second).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        connection.Dispose();
        Assert.Equal(new[] { "create", "open", "input", "output", "set", "close", "release" }, native.Calls);
    }

    [Fact]
    public void GetNativeFailurePreservesPlatformErrorAndCheckedClose()
    {
        var native = new FeatureLifetime { GetStatus = -99 };
        using var connection = new MacOSHidFeatureReportConnection(42, native);
        var error = Assert.Throws<PlatformApiException>(connection.GetReport);
        Assert.Contains("FFFFFF9D", error.Message, StringComparison.Ordinal);
        connection.Dispose();
        Assert.Equal(new[] { "create", "open", "input", "output", "get", "close", "release" }, native.Calls);
    }

    [Fact]
    public void UnprovenCloseRetainsNativeObjectAndSharesFailure()
    {
        var native = new FeatureLifetime { FailClose = true };
        var connection = new MacOSHidFeatureReportConnection(42, native);
        Assert.Throws<Yubico.YubiKit.Core.Devices.UnrecoveredConnectionException>(connection.Dispose);
        Assert.Throws<Yubico.YubiKit.Core.Devices.UnrecoveredConnectionException>(connection.Dispose);
        Assert.DoesNotContain("release", native.Calls);
        Assert.Single(native.Calls, call => call == "close");
    }

    private sealed class FeatureLifetime : IIOKitDeviceLifetime
    {
        public List<string> Calls { get; } = [];
        public List<int> Threads { get; } = [];
        public ManualResetEventSlim InSet { get; } = new();
        public ManualResetEventSlim AllowSet { get; } = new();
        public bool HoldSet { get; init; }
        public bool FailClose { get; init; }
        public int GetStatus { get; init; }
        public int SetLength { get; private set; }
        public int GetBufferLength { get; private set; }
        private void Record(string call) { Calls.Add(call); Threads.Add(Environment.CurrentManagedThreadId); }
        public nint CreateDevice(long id) { Record("create"); return 42; }
        public void OpenDevice(nint device) => throw new NotSupportedException();
        public int OpenDeviceResult(nint device) { Record("open"); return 0; }
        public void CloseDevice(nint device) => throw new NotSupportedException();
        public bool CloseDeviceChecked(nint device) { Record("close"); return !FailClose; }
        public void ReleaseCFObject(nint device) => Record("release");
        public int GetIntProperty(nint device, string property)
        {
            if (property.Contains("Input", StringComparison.Ordinal)) { Record("input"); return 17; }
            Record("output"); return 23;
        }
        public int SetFeatureReport(nint device, byte[] buffer)
        {
            Record("set"); SetLength = buffer.Length; InSet.Set();
            if (HoldSet) AllowSet.Wait();
            return 0;
        }
        public int GetFeatureReport(nint device, byte[] buffer, ref long length)
        {
            Record("get"); GetBufferLength = buffer.Length; length = 8;
            return GetStatus;
        }
        public nint CreateRunLoopMode(string name) => throw new NotSupportedException();
        public void RegisterInputReportCallback(nint device, byte[] buffer, int length, nint callback, nint context) { }
        public void RegisterRemovalCallback(nint device, nint callback, nint context) { }
    }
}
