using Xunit.Sdk;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

public class ResponsivenessProbeTests
{
    [Fact]
    public async Task SynchronousExpertMacOpen_BlocksOnlyItsCallerWhileWorkerOpens()
    {
        using var probe = new WithheldNativeProbe();
        using var native = new BlockingOtpLifetime(probe) { HoldOpen = true };
        // The direct report constructor is synchronous by contract; native open belongs to the worker.
        probe.Start(() => Task.FromResult(new MacOSHidFeatureReportConnection(42, native)));
        try
        {
            Assert.True(native.OpenEntered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.NotEqual(probe.CallerThread, native.OpenThread);
            Assert.False(probe.Returned.IsSet);
        }
        finally
        {
            probe.Release.Set();
            // Drain and dispose a late-opened connection even when the assertion fails.
            var opening = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            using var connection = await ((Task<MacOSHidFeatureReportConnection>)opening)
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    [Fact]
    public async Task DirectNativeOpenUnderTaskReturningEntry_FailsResponsivenessGate()
    {
        using var probe = new WithheldNativeProbe();
        probe.Start(() => { probe.EnterNative(); return Task.CompletedTask; });
        try
        {
            Assert.Throws<NotEqualException>(probe.AssertResponsive);
            Assert.False(probe.Returned.IsSet);
        }
        finally { await probe.ReleaseAndDrainAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacySynchronousOtpReport_IsRejectedByTheResponsivenessGate(bool send)
    {
        using var probe = new WithheldNativeProbe();
        using var connection = new OtpHidConnection(new BlockingHidConnection(probe));
        probe.Start(() => send
            ? connection.SendAsync(new byte[8], TestContext.Current.CancellationToken)
            : connection.ReceiveAsync(TestContext.Current.CancellationToken));

        // The real legacy task-returning adapter calls GetReport/SetReport before returning a Task.
        try
        {
            Assert.Throws<NotEqualException>(probe.AssertResponsive);
            Assert.False(probe.Returned.IsSet);
        }
        finally { await probe.ReleaseAndDrainAsync(); }
        TestContext.Current.TestOutputHelper?.WriteLine($"legacy-otp send={send}: {probe.DescribeDurations()}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacySynchronousFidoReport_IsRejectedByTheResponsivenessGate(bool send)
    {
        using var probe = new WithheldNativeProbe();
        using var connection = new FidoHidConnection(new BlockingHidConnection(probe, 64));
        probe.Start(() => send
            ? connection.SendAsync(new byte[64], TestContext.Current.CancellationToken)
            : connection.ReceiveAsync(TestContext.Current.CancellationToken));
        try
        {
            Assert.Throws<NotEqualException>(probe.AssertResponsive);
            Assert.False(probe.Returned.IsSet);
        }
        finally
        {
            await probe.ReleaseAndDrainAsync();
        }
        TestContext.Current.TestOutputHelper?.WriteLine($"legacy-fido send={send}: {probe.DescribeDurations()}");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OffCallerReportControl_ReturnsTaskButDoesNotCompleteWhileHeld(bool fido, bool send)
    {
        using var probe = new WithheldNativeProbe();
        using var report = new BlockingHidConnection(probe, fido ? 64 : 8);
        using var fidoConnection = new FidoHidConnection(report);
        using var otpConnection = new OtpHidConnection(report);
        var cancellationToken = TestContext.Current.CancellationToken;
        Task InvokeAdapter() => (fido, send) switch
        {
            (true, true) => fidoConnection.SendAsync(new byte[64], cancellationToken),
            (true, false) => fidoConnection.ReceiveAsync(cancellationToken),
            (false, true) => otpConnection.SendAsync(new byte[8], cancellationToken),
            (false, false) => otpConnection.ReceiveAsync(cancellationToken)
        };

        // Test-only control, not a proposed production scheduler or Linux owner.
        probe.Start(() => Task.Run(InvokeAdapter, cancellationToken));
        try
        {
            probe.AssertResponsive();
            var operation = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            Assert.False(operation.IsCompleted);
        }
        finally
        {
            await probe.ReleaseAndDrainAsync();
        }
        TestContext.Current.TestOutputHelper?.WriteLine($"off-caller-control fido={fido} send={send}: {probe.DescribeDurations()}");
    }

    [Fact]
    public async Task CompletedTaskWhileReportIsHeld_IsRejectedByTheResponsivenessGate()
    {
        using var probe = new WithheldNativeProbe();
        using var connection = new OtpHidConnection(new BlockingHidConnection(probe));
        Task? report = null;
        probe.Start(() =>
        {
            // Deliberately broken: dispatch the real adapter but discard its completion.
            report = Task.Run(() => connection.ReceiveAsync(TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken);
            return Task.CompletedTask;
        });
        try
        {
            Assert.Throws<FalseException>(probe.AssertResponsive);
        }
        finally
        {
            probe.Release.Set();
            // Drain even after a failed assertion; cancellation must not abandon the held fake.
            await (await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None))
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            Assert.NotNull(report);
            await report.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    [Fact]
    public async Task BuiltInMacFidoOpen_ReturnsWhileNativeOpenIsWithheld()
    {
        using var probe = new WithheldNativeProbe();
        var bridge = new BlockingFidoBridge(probe);
        probe.Start(() => MacOSFidoHidConnection.OpenAsync(42, bridge, TestContext.Current.CancellationToken));

        try { probe.AssertResponsive(); }
        finally
        {
            probe.Release.Set();
            // Drain and dispose a late-opened connection even when the assertion fails.
            var opening = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            await using var connection = await ((Task<MacOSFidoHidConnection>)opening)
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    [Fact]
    public async Task BuiltInMacOtpOpen_ReturnsWhileNativeOpenIsWithheld()
    {
        using var probe = new WithheldNativeProbe();
        using var native = new BlockingOtpLifetime(probe) { HoldOpen = true };
        probe.Start(() => MacOSOtpHidConnection.OpenAsync(42, native, TestContext.Current.CancellationToken));

        try { probe.AssertResponsive(); }
        finally
        {
            probe.Release.Set();
            // Drain and dispose a late-opened connection even when the assertion fails.
            var opening = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            await using var connection = await ((Task<MacOSOtpHidConnection>)opening)
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    [Fact]
    public async Task BuiltInMacOtpReceive_ReturnsWhileNativeGetIsWithheld()
    {
        using var probe = new WithheldNativeProbe();
        using var native = new BlockingOtpLifetime(probe) { HoldGet = true };
        await using var connection = await MacOSOtpHidConnection.OpenAsync(42, native, TestContext.Current.CancellationToken);
        probe.Start(() => connection.ReceiveAsync(TestContext.Current.CancellationToken));

        try { probe.AssertResponsive(); }
        finally { await probe.ReleaseAndDrainAsync(); }
        var receiving = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(8, (await ((Task<ReadOnlyMemory<byte>>)receiving).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Length);
    }

    private sealed class BlockingHidConnection(WithheldNativeProbe probe, int reportSize = 8) : IHidConnection
    {
        public ConnectionType Type => ConnectionType.Hid;
        public int InputReportSize => reportSize;
        public int OutputReportSize => reportSize;
        public byte[] GetReport() { probe.EnterNative(); return new byte[reportSize]; }
        public void SetReport(byte[] report) => probe.EnterNative();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class BlockingOtpLifetime(WithheldNativeProbe probe) : IIOKitDeviceLifetime, IDisposable
    {
        public bool HoldGet { get; init; }
        public bool HoldOpen { get; init; }
        public ManualResetEventSlim OpenEntered { get; } = new();
        public int OpenThread { get; private set; }
        public nint CreateDevice(long id) { if (!HoldGet && !HoldOpen) probe.EnterNative(); return 1; }
        public void OpenDevice(nint device) { if (HoldOpen) probe.EnterNative(); }
        public int OpenDeviceResult(nint device)
        {
            OpenThread = Environment.CurrentManagedThreadId;
            OpenEntered.Set();
            OpenDevice(device);
            return 0;
        }
        public void CloseDevice(nint device) { }
        public bool CloseDeviceChecked(nint device) => true;
        public void ReleaseCFObject(nint device) { }
        public int GetFeatureReport(nint device, byte[] buffer, ref long length)
        { probe.EnterNative(); length = 8; return 0; }
        public int SetFeatureReport(nint device, byte[] buffer) => throw new NotSupportedException();
        public nint CreateRunLoopMode(string name) => throw new NotSupportedException();
        public int GetIntProperty(nint device, string name) => 8;
        public void RegisterInputReportCallback(nint device, byte[] buffer, int length, nint callback, nint context) { }
        public void RegisterRemovalCallback(nint device, nint callback, nint context) { }
        public void Dispose() => OpenEntered.Dispose();
    }

    private sealed class BlockingFidoBridge(WithheldNativeProbe probe) : IHidInputBridge
    {
        public nint CreateDevice(long id) => 1;
        public void OpenDevice(nint device) => probe.EnterNative();
        public int InputSize(nint device) => 64;
        public bool CloseUnstarted(nint device) => true;
        public void ReleaseDevice(nint device) { }
        public nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal) => 2;
        public int Start(nint owner) => 0;
        public void Cancel(nint owner) { }
        public int WaitShutdown(nint owner) => 0;
        public int Destroy(nint owner) => 0;
        public void SetReport(nint device, byte[] report) => throw new NotSupportedException();
    }
}