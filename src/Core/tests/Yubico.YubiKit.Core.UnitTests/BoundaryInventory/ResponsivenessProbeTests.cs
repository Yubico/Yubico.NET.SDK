using Xunit.Sdk;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

public class ResponsivenessProbeTests
{
    [Fact]
    public async Task LegacySynchronousMacOpen_IsRejectedByTheResponsivenessGate()
    {
        using var probe = new Probe();
        var native = new BlockingOtpLifetime(probe) { HoldOpen = true };
        // Models restoring the legacy synchronous constructor under a task-returning open entry.
        probe.Start(() => Task.FromResult(new MacOSHidFeatureReportConnection(42, native)));
        try
        {
            Assert.Throws<NotEqualException>(probe.AssertResponsive);
            Assert.False(probe.Returned.IsSet);
        }
        finally { probe.Release.Set(); }
        var opening = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        using var connection = await ((Task<MacOSHidFeatureReportConnection>)opening).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacySynchronousOtpReport_IsRejectedByTheResponsivenessGate(bool send)
    {
        using var probe = new Probe();
        using var connection = new OtpHidConnection(new BlockingHidConnection(probe));
        probe.Start(() => send ? connection.SendAsync(new byte[8]) : connection.ReceiveAsync());

        // The real legacy task-returning adapter calls GetReport/SetReport before returning a Task.
        try
        {
            Assert.Throws<NotEqualException>(probe.AssertResponsive);
            Assert.False(probe.Returned.IsSet);
        }
        finally { probe.Release.Set(); }
        await (await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BuiltInMacFidoOpen_ReturnsWhileNativeOpenIsWithheld()
    {
        using var probe = new Probe();
        var bridge = new BlockingFidoBridge(probe);
        probe.Start(() => MacOSFidoHidConnection.OpenAsync(42, bridge, CancellationToken.None));

        try { probe.AssertResponsive(); }
        finally { probe.Release.Set(); }
        var opening = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await using var connection = await ((Task<MacOSFidoHidConnection>)opening).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BuiltInMacOtpOpen_ReturnsWhileNativeOpenIsWithheld()
    {
        using var probe = new Probe();
        var native = new BlockingOtpLifetime(probe) { HoldOpen = true };
        probe.Start(() => MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None));

        try { probe.AssertResponsive(); }
        finally { probe.Release.Set(); }
        var opening = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await using var connection = await ((Task<MacOSOtpHidConnection>)opening).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BuiltInMacOtpReceive_ReturnsWhileNativeGetIsWithheld()
    {
        using var probe = new Probe();
        var native = new BlockingOtpLifetime(probe) { HoldGet = true };
        await using var connection = await MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None);
        probe.Start(() => connection.ReceiveAsync());

        try { probe.AssertResponsive(); }
        finally { probe.Release.Set(); }
        var receiving = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(8, (await ((Task<ReadOnlyMemory<byte>>)receiving).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Length);
    }

    private sealed class Probe : IDisposable
    {
        private readonly ManualResetEventSlim _entered = new();
        private Thread? _caller;
        private int _nativeThread;
        private int _callerThread;
        internal ManualResetEventSlim Release { get; } = new();
        internal ManualResetEventSlim Returned { get; } = new();
        internal TaskCompletionSource<Task> Invocation { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal void Start(Func<Task> invoke)
        {
            _caller = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                _callerThread = Environment.CurrentManagedThreadId;
                try { Invocation.TrySetResult(invoke()); }
                catch (Exception ex) { Invocation.TrySetException(ex); }
                finally { Returned.Set(); }
            })
            { IsBackground = true };
            _caller.Start();
        }

        internal void EnterNative()
        {
            Volatile.Write(ref _nativeThread, Environment.CurrentManagedThreadId);
            _entered.Set();
            Release.Wait();
        }

        internal void AssertResponsive()
        {
            Assert.True(_entered.Wait(TimeSpan.FromSeconds(5)), "native operation was never entered");
            Assert.NotEqual(_callerThread, Volatile.Read(ref _nativeThread));
            Assert.True(Returned.Wait(TimeSpan.FromSeconds(5)), "invocation did not return before native release");
            Assert.False(Release.IsSet);
        }

        public void Dispose()
        {
            Release.Set();
            if (_caller?.Join(TimeSpan.FromSeconds(5)) != false)
            {
                _entered.Dispose();
                Release.Dispose();
                Returned.Dispose();
            }
        }
    }

    private sealed class BlockingHidConnection(Probe probe) : IHidConnection
    {
        public ConnectionType Type => ConnectionType.Hid;
        public int InputReportSize => 8;
        public int OutputReportSize => 8;
        public byte[] GetReport() { probe.EnterNative(); return new byte[8]; }
        public void SetReport(byte[] report) => probe.EnterNative();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class BlockingOtpLifetime(Probe probe) : IIOKitDeviceLifetime
    {
        public bool HoldGet { get; init; }
        public bool HoldOpen { get; init; }
        public nint CreateDevice(long id) { if (!HoldGet && !HoldOpen) probe.EnterNative(); return 1; }
        public void OpenDevice(nint device) { if (HoldOpen) probe.EnterNative(); }
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
    }

    private sealed class BlockingFidoBridge(Probe probe) : IHidInputBridge
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