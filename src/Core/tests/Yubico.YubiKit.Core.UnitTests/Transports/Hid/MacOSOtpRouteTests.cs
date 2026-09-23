using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class MacOSOtpRouteTests
{
    [Fact]
    public async Task OpenSendGetAndClose_RunOnOneWorkerAndReleaseClaim()
    {
        var native = new Lifetime();
        native.AllowSet.Set();
        var claim = new Claim();
        var open = MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None, claim);
        Assert.False(open.IsCompleted);
        native.AllowOpen.Set();
        var connection = await open;
        await connection.SendAsync(new byte[8]).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(8, (await connection.ReceiveAsync()).Length);
        await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { "create", "open", "set", "get", "close", "release" }, native.Calls);
        Assert.Single(native.Threads.Distinct());
        Assert.True(claim.Disposed);
    }

    [Fact]
    public async Task HeldSend_RejectsOverlapAndDrainsBeforeAsyncDispose()
    {
        var native = new Lifetime();
        native.AllowOpen.Set();
        var connection = await MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None);
        var report = new byte[8];
        report[0] = 7;
        var send = connection.SendAsync(report);
        Assert.True(native.InSet.Wait(TimeSpan.FromSeconds(5)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => connection.ReceiveAsync());
        var dispose = connection.DisposeAsync();
        Assert.False(dispose.IsCompleted);
        Assert.False(send.IsCompleted);
        native.AllowSet.Set();
        await send;
        await dispose;
        Assert.Equal(new[] { "create", "open", "set", "close", "release" }, native.Calls);
        Assert.Equal((byte)7, native.CapturedReport?[0]);
        Assert.All(native.LastReport ?? [], value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task CancelBeforeDispatchDoesNotSubmit_AndCancelDuringNativeWaitDrains()
    {
        var native = new Lifetime();
        using var openCancel = new CancellationTokenSource();
        openCancel.Cancel();
        var opening = MacOSOtpHidConnection.OpenAsync(42, native, openCancel.Token);
        native.AllowOpen.Set();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => opening);
        Assert.Empty(native.Calls);

        var connection = await MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None);
        using var sendCancel = new CancellationTokenSource();
        var send = connection.SendAsync(new byte[8], sendCancel.Token);
        Assert.True(native.InSet.Wait(TimeSpan.FromSeconds(5)));
        sendCancel.Cancel();
        Assert.False(send.IsCompleted);
        native.AllowSet.Set();
        await send;
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task CloseFailureRetainsClaimAndSharesDisposalFailure()
    {
        var native = new Lifetime { FailClose = true };
        native.AllowOpen.Set();
        var claim = new Claim();
        var connection = await MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None, claim);
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        Assert.False(claim.Disposed);
        Assert.DoesNotContain("release", native.Calls);
        Assert.Single(native.Calls, call => call == "close");
    }

    [Fact]
    public async Task FailedOpenBeforeNativeOwnershipReleasesSameKey()
    {
        var native = new Lifetime { FailOpen = true, FailClose = true };
        native.AllowOpen.Set();
        const string id = "hid:otp-partial-42";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id]);
        await Assert.ThrowsAsync<PlatformApiException>(() =>
            MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None, claim));
        Assert.False(DeviceConnectionRegistry.IsInUse(id));
        Assert.Equal(new[] { "create", "open", "release" }, native.Calls);
    }

    [Fact]
    public async Task FailedOpenReleasesClaimForNextOpen()
    {
        var native = new Lifetime { FailOpen = true };
        native.AllowOpen.Set();
        const string id = "hid:otp-proven-partial-42";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id]);
        await Assert.ThrowsAsync<PlatformApiException>(() =>
            MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None, claim).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(new[] { "create", "open", "release" }, native.Calls);
        Assert.False(DeviceConnectionRegistry.IsInUse(id));
        using var next = await DeviceConnectionRegistry.AcquireConnectionAsync([id]);
    }

    [Fact]
    public async Task FailedNativeCloseAfterSuccessfulOpenQuarantinesRegistryClaim()
    {
        var native = new Lifetime { FailClose = true };
        native.AllowOpen.Set();
        const string id = "hid:otp-close-unproven-42";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id]);
        var connection = await MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None, claim);
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(DeviceConnectionRegistry.IsInUse(id));
        Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(id));
        Assert.DoesNotContain("release", native.Calls);
    }

    [Fact]
    public async Task CancelledActiveReceiveStillDrainsBeforeClose()
    {
        var native = new Lifetime { HoldGet = true };
        native.AllowOpen.Set();
        var connection = await MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var read = connection.ReceiveAsync(cancellation.Token);
        Assert.True(native.InGet.Wait(TimeSpan.FromSeconds(5)));
        cancellation.Cancel();
        var dispose = connection.DisposeAsync().AsTask();
        Assert.False(read.IsCompleted);
        Assert.False(dispose.IsCompleted);
        native.AllowGet.Set();
        Assert.Equal(8, (await read.WaitAsync(TimeSpan.FromSeconds(5))).Length);
        await dispose.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { "create", "open", "get", "close", "release" }, native.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidOrFailedGetClearsPartialReport(bool failGet)
    {
        var native = new Lifetime { FailGet = failGet, ShortGet = !failGet };
        native.AllowOpen.Set();
        var connection = await MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None);
        await Assert.ThrowsAnyAsync<Exception>(() => connection.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.NotNull(native.LastGetReport);
        Assert.All(native.LastGetReport, value => Assert.Equal(0, value));
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task FailedOpenAndFailedReleaseReportBothFailuresAndRetainClaim()
    {
        var native = new Lifetime { FailOpen = true, FailRelease = true };
        native.AllowOpen.Set();
        const string id = "hid:otp-failed-release-42";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id]);
        var failure = await Assert.ThrowsAsync<UnrecoveredConnectionException>(() =>
            MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None, claim).WaitAsync(TimeSpan.FromSeconds(5)));
        var both = Assert.IsType<AggregateException>(failure.InnerException);
        Assert.Contains(both.InnerExceptions, error => error is PlatformApiException);
        Assert.True(DeviceConnectionRegistry.IsInUse(id));
        Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(id));
    }

    [Fact]
    public async Task ExclusiveAccessOpenClosesBeforeReleaseEvenThoughOpenReportsFailure()
    {
        var native = new Lifetime { ExclusiveOpen = true };
        native.AllowOpen.Set();
        var claim = new Claim();
        await Assert.ThrowsAsync<PlatformApiException>(() =>
            MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None, claim).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(new[] { "create", "open", "close", "release" }, native.Calls);
        Assert.True(claim.Disposed);
    }

    [Fact]
    public async Task ExclusiveAccessOpenWithSuccessfulCloseReleasesPhysicalClaim()
    {
        var native = new Lifetime { ExclusiveOpen = true };
        native.AllowOpen.Set();
        const string id = "hid:otp-exclusive-open-42";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id]);
        await Assert.ThrowsAsync<PlatformApiException>(() =>
            MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None, claim).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(new[] { "create", "open", "close", "release" }, native.Calls);
        Assert.False(DeviceConnectionRegistry.IsInUse(id));
        using var next = await DeviceConnectionRegistry.AcquireConnectionAsync([id]);
    }

    [Fact]
    public async Task ThrowingGetClearsMutatedReportAndAllowsCheckedClose()
    {
        var native = new Lifetime { ThrowGet = true };
        native.AllowOpen.Set();
        var connection = await MacOSOtpHidConnection.OpenAsync(42, native, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            connection.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.NotNull(native.LastGetReport);
        Assert.All(native.LastGetReport, value => Assert.Equal(0, value));
        await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { "create", "open", "get", "close", "release" }, native.Calls);
    }

    [Fact]
    public async Task RegisteredOtpSlotTransfersClaimBeforeOpenAndReleasesAfterClose()
    {
        var native = new Lifetime();
        var descriptor = new HidDescriptorInfo { VendorId = 0x1050, ProductId = 0x0407,
            UsagePage = 1, Usage = 6 };
        var slot = new HidConnectionSlot(new MacOSHidDevice(42, descriptor), otpLifetime: native);
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([slot.InterfaceId]);
        var opening = slot.OpenRegisteredConnectionAsync(claim, CancellationToken.None);
        Assert.True(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        Assert.False(opening.IsCompleted);
        native.AllowOpen.Set();
        var connection = await opening.WaitAsync(TimeSpan.FromSeconds(5));
        await connection.DisposeAsync();
        Assert.Equal(new[] { "create", "open", "close", "release" }, native.Calls);
        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
    }

    private sealed class Claim : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private sealed class Lifetime : IIOKitDeviceLifetime
    {
        public ManualResetEventSlim AllowOpen { get; } = new(false);
        public ManualResetEventSlim InSet { get; } = new(false);
        public ManualResetEventSlim AllowSet { get; } = new(false);
        public ManualResetEventSlim InGet { get; } = new(false);
        public ManualResetEventSlim AllowGet { get; } = new(false);
        public bool HoldGet { get; init; }
        public List<string> Calls { get; } = [];
        public List<int> Threads { get; } = [];
        public byte[]? CapturedReport { get; private set; }
        public byte[]? LastReport { get; private set; }
        public bool FailClose { get; init; }
        public bool ExclusiveOpen { get; init; }
        public bool FailRelease { get; init; }
        public bool FailGet { get; init; }
        public bool ThrowGet { get; init; }
        public bool ShortGet { get; init; }
        public byte[]? LastGetReport { get; private set; }
        private void Record(string name) { Calls.Add(name); Threads.Add(Environment.CurrentManagedThreadId); }
        public nint CreateDevice(long id) { AllowOpen.Wait(); Record("create"); return 42; }
        public bool FailOpen { get; init; }
        public void OpenDevice(nint device) { Record("open"); if (FailOpen) throw new PlatformApiException("open failed"); }
        public int OpenDeviceResult(nint device)
        {
            Record("open");
            return ExclusiveOpen ? unchecked((int)0xE00002C5) : FailOpen ? -1 : 0;
        }
        public void CloseDevice(nint device) => Record("close");
        public bool CloseDeviceChecked(nint device) { Record("close"); return !FailClose; }
        public void ReleaseCFObject(nint device) { Record("release"); if (FailRelease) throw new InvalidOperationException("release failed"); }
        public int GetFeatureReport(nint device, byte[] buffer, ref long size)
        { Record("get"); LastGetReport = buffer; InGet.Set(); if (HoldGet) AllowGet.Wait(); buffer[1] = 5; if (ThrowGet) throw new InvalidOperationException("get failed after writing"); size = ShortGet ? 4 : 8; return FailGet ? -1 : 0; }
        public int SetFeatureReport(nint device, byte[] buffer)
        { Record("set"); LastReport = buffer; InSet.Set(); AllowSet.Wait(); CapturedReport = [.. buffer]; return 0; }
        public nint CreateRunLoopMode(string name) => 1;
        public int GetIntProperty(nint device, string name) => 8;
        public void RegisterInputReportCallback(nint device, byte[] buffer, int length, nint callback, nint context) { }
        public void RegisterRemovalCallback(nint device, nint callback, nint context) { }
    }
}
