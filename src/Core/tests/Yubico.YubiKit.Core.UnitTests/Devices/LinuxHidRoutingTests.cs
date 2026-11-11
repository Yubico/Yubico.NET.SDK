// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers.Binary;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.BoundaryInventory;
using FidoApi = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.ReportApi;
using OtpApi = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxOtpHidConnectionTests.ReportApi;
using Reactor = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.Reactor;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[SupportedOSPlatform("linux")]
[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class LinuxHidRoutingTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicOpen_HeldNativeOpenReturnsPendingAndClaimsEveryMember(bool otp)
    {
        using var probe = new WithheldNativeProbe();
        var fidoApi = new FidoApi();
        var otpApi = new OtpApi();
        void before(string operation) { if (operation == "open") probe.EnterNative(); }
        fidoApi.Before = before;
        otpApi.Before = before;
        var fido = Slot(false, fidoApi, new Reactor());
        var keyboard = Slot(true, otpApi);
        var device = new YubiKeyDevice($"l5-device-{Guid.NewGuid():N}", new SmartCardMember(), fido, keyboard, null);
        Task<IConnection>? opening = null;
        probe.Start(() => opening = OpenAsync(device, otp, Token));
        try
        {
            probe.AssertResponsive();
            Assert.All(device.InterfaceIds, id => Assert.True(DeviceConnectionRegistry.IsInUse(id)));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(device, !otp, Token));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => device.ConnectAsync<ISmartCardConnection>(Token));
            Assert.Equal(otp ? 0 : 1, fidoApi.Opens);
            Assert.Equal(otp ? 1 : 0, otpApi.Opens);
        }
        finally
        {
            await probe.ReleaseAndDrainAsync();
            if (opening is not null) await (await opening).DisposeAsync();
        }
        Assert.All(device.InterfaceIds, id => Assert.False(DeviceConnectionRegistry.IsInUse(id)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreCanceledPublicOpen_SubmitsNothingAndReleasesAllClaims(bool otp)
    {
        var fidoApi = new FidoApi();
        var otpApi = new OtpApi();
        var device = Device(Slot(false, fidoApi, new Reactor()), Slot(true, otpApi));
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(Token);
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => OpenAsync(device, otp, canceled.Token));
        Assert.Equal(0, fidoApi.Opens + otpApi.Opens);
        Assert.All(device.InterfaceIds, id => Assert.False(DeviceConnectionRegistry.IsInUse(id)));
        await using var retry = await OpenAsync(device, otp, Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicDisposal_HeldCheckedCloseRetainsAllMembers(bool otp)
    {
        using var probe = new WithheldNativeProbe();
        var fidoApi = new FidoApi();
        var otpApi = new OtpApi();
        var device = Device(Slot(false, fidoApi, new Reactor()), Slot(true, otpApi));
        var connection = await OpenAsync(device, otp, Token);
        void before(string operation) { if (operation == "close") probe.EnterNative(); }
        fidoApi.Before = before;
        otpApi.Before = before;
        probe.Start(() => connection.DisposeAsync().AsTask());
        try
        {
            probe.AssertResponsive();
            Assert.All(device.InterfaceIds, id => Assert.True(DeviceConnectionRegistry.IsInUse(id)));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(device, !otp, Token));
        }
        finally { await probe.ReleaseAndDrainAsync(); }
        await using var retry = await OpenAsync(device, !otp, Token);
    }

    [Theory]
    [InlineData(false, 4)]
    [InlineData(false, 5)]
    [InlineData(false, 9)]
    [InlineData(true, 4)]
    [InlineData(true, 5)]
    [InlineData(true, 9)]
    public async Task PublicClose_ReleaseEvidenceNotDisposalSuccessControlsReopen(bool otp, int error)
    {
        var fidoApi = new FidoApi { CloseError = error };
        var otpApi = new OtpApi { CloseError = error };
        var device = Device(Slot(false, fidoApi, new Reactor()), Slot(true, otpApi));
        var connection = await OpenAsync(device, otp, Token);
        var failure = await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask());
        Assert.Same(failure, await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask()));
        if (error == 9)
        {
            Assert.IsType<UnrecoveredConnectionException>(failure);
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => OpenAsync(device, !otp, Token));
            Assert.All(device.InterfaceIds, id => Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(id)));
            GC.SuppressFinalize(connection);
        }
        else
        {
            Assert.IsType<Yubico.YubiKit.Core.Native.PlatformApiException>(failure);
            Assert.All(device.InterfaceIds, id => Assert.False(DeviceConnectionRegistry.IsInUse(id)));
            fidoApi.CloseError = otpApi.CloseError = 0;
            await using var retry = await OpenAsync(device, !otp, Token);
        }
        Assert.Equal(1, otp ? otpApi.Closes : fidoApi.Closes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicFailedOpen_ProvenCleanupAllowsRetry(bool otp)
    {
        var fidoApi = new FidoApi { MetadataError = new IOException("metadata") };
        var otpApi = new OtpApi { OpenError = new IOException("open") };
        var device = Device(Slot(false, fidoApi, new Reactor()), Slot(true, otpApi));
        await Assert.ThrowsAsync<IOException>(() => OpenAsync(device, otp, Token));
        Assert.All(device.InterfaceIds, id => Assert.False(DeviceConnectionRegistry.IsInUse(id)));
        fidoApi.MetadataError = null;
        otpApi.OpenError = null;
        await using var retry = await OpenAsync(device, otp, Token);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DiscoveryTimeout_DirectAndCompositeRetainClaimThroughLateOpenAndCleanup(bool otp, bool composite)
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var fidoApi = new FidoApi();
        var otpApi = new OtpApi();
        void before(string operation)
        {
            if (operation != "open") return;
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5), Token)) throw new TimeoutException("Open not released.");
            throw new IOException("Late open failure");
        }
        fidoApi.Before = before;
        otpApi.Before = before;
        var slot = Slot(otp, otp ? otpApi : fidoApi, new Reactor());
        var device = Device(otp ? null : slot, otp ? slot : null);
        var scope = ProtocolDeviceInfo.CreateScope();
        try
        {
            var read = composite
                ? ProtocolDeviceInfo.ReadBoundedAsync(device, slot.ConnectionType, TimeSpan.FromMilliseconds(100), NullLogger.Instance, Token, true, scope)
                : ProtocolDeviceInfo.ReadSlotBoundedAsync(slot, slot.ConnectionType, TimeSpan.FromMilliseconds(100), NullLogger.Instance, Token, true, scope);
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), Token));
            await Assert.ThrowsAsync<TimeoutException>(() => read);
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => OpenAsync(device, otp, Token));
            var recovered = DeviceConnectionRegistry.WaitForRecoveryForTest(slot.InterfaceId);
            ProtocolDeviceInfo.Retire(scope);
            release.Set();
            await recovered.WaitAsync(TimeSpan.FromSeconds(5), Token);
            fidoApi.Before = otpApi.Before = null;
            await using var replacement = await OpenAsync(device, otp, Token);
            await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(device, otp, Token));
            Assert.Equal(2, otp ? otpApi.Opens : fidoApi.Opens);
        }
        finally { release.Set(); }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DiscoveryFailedClose_DirectAndCompositeCannotReleaseOwnersUnprovenClaim(bool otp, bool composite)
    {
        var api = new SessionApi();
        api.Fido.CloseError = api.Otp.CloseError = 9;
        api.Otp.GetError = new IOException("Discovery GET failed");
        var slot = Slot(otp, api, api.Reactor);
        var device = Device(otp ? null : slot, otp ? slot : null);
        var scope = ProtocolDeviceInfo.CreateScope();
        var read = composite
            ? ProtocolDeviceInfo.ReadBoundedAsync(device, slot.ConnectionType, TimeSpan.FromSeconds(5), NullLogger.Instance, Token, true, scope)
            : ProtocolDeviceInfo.ReadSlotBoundedAsync(slot, slot.ConnectionType, TimeSpan.FromSeconds(5), NullLogger.Instance, Token, true, scope);
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => read);
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => OpenAsync(device, otp, Token));
        Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(slot.InterfaceId));
        Assert.Equal(1, otp ? api.Otp.Closes : api.Fido.Closes);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 9)]
    [InlineData(true, 0)]
    [InlineData(true, 9)]
    public async Task PublicCanceledHeldOpen_DrainsOriginalClaimAndPreservesCleanupFailure(bool otp, int closeError)
    {
        using var probe = new WithheldNativeProbe();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var fidoApi = new FidoApi { CloseError = closeError };
        var otpApi = new OtpApi { CloseError = closeError };
        void before(string operation) { if (operation == "open") probe.EnterNative(); }
        fidoApi.Before = otpApi.Before = before;
        var device = Device(Slot(false, fidoApi, new Reactor()), Slot(true, otpApi));
        probe.Start(() => OpenAsync(device, otp, cancellation.Token));
        Exception? failure = null;
        try
        {
            probe.AssertResponsive();
            cancellation.Cancel();
            Assert.All(device.InterfaceIds, id => Assert.True(DeviceConnectionRegistry.IsInUse(id)));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(device, !otp, Token));
        }
        finally { failure = await Assert.ThrowsAnyAsync<Exception>(probe.ReleaseAndDrainAsync); }
        if (closeError == 9)
        {
            var aggregate = Assert.IsType<AggregateException>(failure);
            Assert.IsType<OperationCanceledException>(aggregate.InnerExceptions[0], exactMatch: false);
            Assert.IsType<UnrecoveredConnectionException>(aggregate.InnerExceptions[1]);
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => OpenAsync(device, !otp, Token));
        }
        else
        {
            Assert.IsType<OperationCanceledException>(failure, exactMatch: false);
            fidoApi.Before = otpApi.Before = null;
            await using var retry = await OpenAsync(device, !otp, Token);
        }
        Assert.Equal(1, otp ? otpApi.Closes : fidoApi.Closes);
    }

    [Fact]
    public async Task FailedFidoQuiescence_RetainsClaimUntilActualDrainAndOldCleanupCannotReleaseReplacement()
    {
        var api = new FidoApi();
        var reactor = new Reactor { HoldDrain = true, RemovalError = new IOException("wake") };
        var slot = Slot(false, api, reactor);
        var device = Device(slot, null);
        var connection = await OpenAsync(device, false, Token);
        var closing = connection.DisposeAsync().AsTask();
        try
        {
            await reactor.Removal.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(closing.IsCompleted);
            Assert.Equal(0, api.Closes);
            await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(device, false, Token));
        }
        finally { reactor.Drain.TrySetResult(); }
        await Assert.ThrowsAsync<IOException>(() => closing);
        var replacementSlot = Slot(false, new FidoApi(), new Reactor(), slot.InterfaceId.Split(':')[1]);
        var replacement = Device(replacementSlot, null);
        await using var next = await OpenAsync(replacement, false, Token);
        await Assert.ThrowsAsync<IOException>(() => connection.DisposeAsync().AsTask());
        reactor.Ready(reactor.Generation - 1);
        await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(replacement, false, Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DiscoveryTimeout_DuringReportCallRetainsClaimThroughHeldClose(bool otp)
    {
        using var reportEntered = new ManualResetEventSlim();
        using var releaseReport = new ManualResetEventSlim();
        using var closeEntered = new ManualResetEventSlim();
        using var releaseClose = new ManualResetEventSlim();
        var api = new SessionApi();
        void before(string operation)
        {
            if (operation == (otp ? "get" : "write"))
            {
                reportEntered.Set();
                if (!releaseReport.Wait(TimeSpan.FromSeconds(5), Token)) throw new TimeoutException("Report held.");
                throw new IOException("Report failure after timeout");
            }
            if (operation != "close") return;
            closeEntered.Set();
            if (!releaseClose.Wait(TimeSpan.FromSeconds(5), Token)) throw new TimeoutException("Close held.");
        }
        api.Fido.Before = api.Otp.Before = before;
        var slot = Slot(otp, api, api.Reactor);
        var device = Device(otp ? null : slot, otp ? slot : null);
        var read = ProtocolDeviceInfo.ReadBoundedAsync(device, slot.ConnectionType,
            TimeSpan.FromMilliseconds(100), NullLogger.Instance, Token, true, ProtocolDeviceInfo.CreateScope());
        Task recovered = Task.CompletedTask;
        try
        {
            Assert.True(reportEntered.Wait(TimeSpan.FromSeconds(5), Token));
            await Assert.ThrowsAsync<TimeoutException>(() => read);
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => OpenAsync(device, otp, Token));
            recovered = DeviceConnectionRegistry.WaitForRecoveryForTest(slot.InterfaceId);
            releaseReport.Set();
            Assert.True(closeEntered.Wait(TimeSpan.FromSeconds(5), Token));
            Assert.False(recovered.IsCompleted);
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => OpenAsync(device, otp, Token));
        }
        finally
        {
            releaseReport.Set();
            releaseClose.Set();
            // Drain late cleanup even when the runner has canceled; the fake gates must outlive it.
            await recovered.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        api.Fido.Before = api.Otp.Before = null;
        await using var retry = await OpenAsync(device, otp, Token);
    }

    [Fact]
    public async Task Finder_RetiredLinuxReadCompletesLateWithoutPublishingIntoReplacementEvidence()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var api = new SessionApi { Serial = 333 };
        var hid = new LinuxHidInterface(Descriptor(false, $"l5-epoch-{Guid.NewGuid():N}"));
        var slot = new HidConnectionSlot(hid, linuxReportApi: api, linuxReadReactor: api.Reactor);
        var finder = new FindYubiKeys(new EmptyPcsc(), new OneHid(hid), _ => slot);
        api.Fido.Before = operation =>
        {
            if (operation != "write" || api.Fido.Writes != 1) return; // Hold metadata, after channel INIT.
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5), Token)) throw new TimeoutException("Metadata held.");
        };
        var oldScan = finder.FindAllAsync(ConnectionType.All, Token);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), Token));
            finder.NotifyTransportActivity(ConnectionType.Hid);
        }
        finally { release.Set(); }
        Assert.Equal(333, Assert.Single(await oldScan).SerialNumber);
        api.Fido.Before = null;
        api.Serial = 999;
        var newScan = await finder.FindAllAsync(ConnectionType.All, Token);
        Assert.Equal(999, Assert.Single(newScan).SerialNumber);
        Assert.Equal(2, api.Fido.Opens);
        Assert.Equal(2, api.Fido.Closes);
        Assert.Equal(999, Assert.Single(await finder.FindAllAsync(ConnectionType.All, Token)).SerialNumber);
        Assert.Equal(2, api.Fido.Opens); // Only replacement evidence was cached.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicRawSessions_BorrowReuseAndOneSessionRuleSurviveLinuxRouting(bool otp)
    {
        var api = new SessionApi();
        var slot = Slot(otp, api, api.Reactor);
        var device = Device(otp ? null : slot, otp ? slot : null);
        await using var connection = await OpenAsync(device, otp, Token);
        if (otp)
        {
            var typed = Assert.IsType<IOtpHidConnection>(connection, exactMatch: false);
            await using (var idle = await RawOtpHidSession.CreateAsync(typed, Token))
            {
                Assert.True(idle.IsInitialized);
                await Assert.ThrowsAsync<ConnectionInUseException>(() => RawOtpHidSession.CreateAsync(typed, Token));
            }
            Assert.Equal(0, api.Otp.Gets);
            QueueOtpExchange(api.Otp);
            await using (var active = await RawOtpHidSession.CreateAsync(typed, Token))
            {
                var response = await active.SendAndReceiveAsync(OtpConstants.CmdYk4Capabilities, ReadOnlyMemory<byte>.Empty, Token);
                Assert.Equal(0xAC, response.Span[0]);
            }
            Assert.Equal(11, api.Otp.Sets); // Ten frames and completion reset.
            Assert.Equal(0, api.Otp.Closes);
            await using var next = await RawOtpHidSession.CreateAsync(typed, Token);
        }
        else
        {
            var typed = Assert.IsType<IFidoHidConnection>(connection, exactMatch: false);
            await using (var idle = await RawCtapHidSession.CreateAsync(typed, Token))
            {
                Assert.True(idle.IsInitialized);
                await Assert.ThrowsAsync<ConnectionInUseException>(() => RawCtapHidSession.CreateAsync(typed, Token));
            }
            Assert.Equal(0, api.Fido.Writes);
            for (int i = 0; i < 2; i++)
            {
                await using var active = await RawCtapHidSession.CreateAsync(typed, Token);
                Assert.Equal(0xAC, (await active.SendAndReceiveAsync(0x41, ReadOnlyMemory<byte>.Empty, Token)).Span[0]);
            }
            Assert.Equal(4, api.Fido.Writes); // Independent lazy INIT followed by command, twice.
            Assert.False(api.Reactor.RemovalRequested);
            Assert.Equal(0, api.Fido.Closes);
        }
        Assert.True(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(device, otp, Token));
    }

    [Fact]
    public async Task SamePidIndependentInterfaces_AreNotGloballyGroupedByFactory()
    {
        var first = Device(Slot(false, new FidoApi(), new Reactor()), null);
        var second = Device(Slot(false, new FidoApi(), new Reactor()), null);
        await using var a = await OpenAsync(first, false, Token);
        await using var b = await OpenAsync(second, false, Token);
        Assert.NotEqual(first.PhysicalIdentityKey, second.PhysicalIdentityKey);
        await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(first, false, Token));
        await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(second, false, Token));
    }

    [Fact]
    public async Task Finder_LinuxSamePidKeysGroupOnlyMatchingSerialsAndKeepNativeRoutesDistinct()
    {
        var interfaces = new List<IHidInterface>();
        var slots = new Dictionary<IDevice, IYubiKeyConnectionSlot>();
        foreach (int serial in new[] { 111, 222 })
        {
            foreach (bool otp in new[] { false, true })
            {
                var hid = new LinuxHidInterface(Descriptor(otp, $"l5-key-{serial}-{otp}-{Guid.NewGuid():N}"));
                var api = new SessionApi { Serial = serial };
                if (otp) QueueOtpMetadata(api.Otp, serial);
                interfaces.Add(hid);
                slots.Add(hid, new HidConnectionSlot(hid, linuxReportApi: api, linuxReadReactor: api.Reactor));
            }
        }
        var finder = new FindYubiKeys(new EmptyPcsc(), new ListedHids(interfaces), device => slots[device]);
        var found = await finder.FindAllAsync(ConnectionType.All, Token);
        Assert.Equal(2, found.Count);
        Assert.Equal<int?>([111, 222], found.Select(device => device.SerialNumber).Order());
        foreach (var device in found)
        {
            Assert.Equal(ConnectionType.HidFido | ConnectionType.HidOtp, device.AvailableConnections);
            var published = Assert.IsType<YubiKeyDevice>(device);
            Assert.Equal(2, published.InterfaceIds.Count);
            Assert.All(published.InterfaceIds, id => Assert.Contains($"l5-key-{device.SerialNumber}-", id));
            await using var connection = await device.ConnectAsync<IFidoHidConnection>(Token);
            await Assert.ThrowsAsync<ConnectionInUseException>(() => device.ConnectAsync<IOtpHidConnection>(Token));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MismatchedDiscoveryTransport_DoesNotOpenNativeOrLeakDiscoveryLease(bool otp)
    {
        var api = new SessionApi();
        var slot = Slot(otp, api, api.Reactor);
        await Assert.ThrowsAsync<NotSupportedException>(() => ProtocolDeviceInfo.ReadSlotBoundedAsync(slot,
            otp ? ConnectionType.HidFido : ConnectionType.HidOtp, TimeSpan.FromSeconds(5),
            NullLogger.Instance, Token, true, ProtocolDeviceInfo.CreateScope()));
        Assert.Equal(0, api.Fido.Opens + api.Otp.Opens);
        await using var connection = await slot.OpenRawConnectionAsync(slot.ConnectionType, Token);
        using var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([slot.InterfaceId], Token);
    }

    private static void QueueOtpExchange(OtpApi api)
    {
        api.Reports.Enqueue([0, 5, 8, 0, 0, 0, 0, 0]); // Lazy status initialization.
        for (int i = 0; i < 11; i++) api.Reports.Enqueue([0, 5, 8, 0, 1, 0, 0, 0]);
        api.Reports.Enqueue([0xAC, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
        api.Reports.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
    }

    private static void QueueOtpMetadata(OtpApi api, int serial)
    {
        api.Reports.Enqueue([0, 5, 8, 0, 0, 0, 0, 0]);
        for (int i = 0; i < 11; i++) api.Reports.Enqueue([0, 5, 8, 0, 1, 0, 0, 0]);
        byte[] response = [11, 5, 3, 5, 8, 0, 2, 4, 0, 0, 0, 0, 0, 0];
        BinaryPrimitives.WriteInt32BigEndian(response.AsSpan(8), serial);
        BinaryPrimitives.WriteUInt16LittleEndian(response.AsSpan(12), (ushort)~ChecksumUtils.CalculateCrc(response.AsSpan(0, 12), 12));
        for (int sequence = 0; sequence < 2; sequence++)
        {
            var report = new byte[8];
            response.AsSpan(sequence * 7, 7).CopyTo(report);
            report[7] = (byte)(OtpConstants.ResponsePendingFlag | sequence);
            api.Reports.Enqueue(report);
        }
        api.Reports.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
    }

    private sealed class SessionApi : ILinuxHidReportApi
    {
        internal FidoApi Fido { get; } = new();
        internal OtpApi Otp { get; } = new();
        internal Reactor Reactor { get; } = new();
        internal int Serial { get; set; } = 123;
        public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags) =>
            flags.HasFlag(NativeMethods.OpenFlags.O_NONBLOCK) ? Fido.Open(path, flags) : Otp.Open(path, flags);
        public (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle) => Fido.GetReportSizes(handle);
        public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload) => Fido.TryRead(handle, payload);
        public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload) => Otp.GetFeature(handle, payload);
        public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload) => Otp.SetFeature(handle, payload);
        public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle) =>
            Fido.Opens > 0 ? Fido.CloseOnce(handle) : Otp.CloseOnce(handle);
        public void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
        {
            Fido.Write(handle, payload);
            var response = new byte[64];
            payload[..5].CopyTo(response);
            if (payload[4] == 0x86)
            {
                response[6] = 17;
                payload.Slice(7, 8).CopyTo(response.AsSpan(7));
                BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(15), 0x01020304);
                response[19] = 2; response[20] = 5; response[21] = 8;
            }
            else if (payload[4] == 0xC2)
            {
                response[6] = 12;
                new byte[] { 11, 5, 3, 5, 8, 0, 2, 4 }.CopyTo(response, 7);
                BinaryPrimitives.WriteInt32BigEndian(response.AsSpan(15), Serial);
            }
            else { response[6] = 1; response[7] = 0xAC; }
            Fido.Reports.Enqueue(response);
            Reactor.Ready();
        }
    }

    private sealed class EmptyPcsc : IFindPcscDevices
    {
        public Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IPcscDevice>>([]);
    }

    private sealed class OneHid(IHidInterface hid) : IFindHidInterfaces
    {
        public Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IHidInterface>>([hid]);
    }

    private sealed class ListedHids(IReadOnlyList<IHidInterface> hids) : IFindHidInterfaces
    {
        public Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(hids);
    }

    private sealed class SmartCardMember : IYubiKeyConnectionSlot
    {
        public string InterfaceId { get; } = $"pcsc:l5-{Guid.NewGuid():N}";
        public ConnectionType ConnectionType => ConnectionType.SmartCard;
    }

    internal static HidConnectionSlot Slot(bool otp, ILinuxHidReportApi api, ILinuxHidReadReactor? reactor = null, string? path = null)
    {
        var descriptor = Descriptor(otp, path ?? $"l5-{Guid.NewGuid():N}");
        return new HidConnectionSlot(new LinuxHidInterface(descriptor), linuxReportApi: api, linuxReadReactor: reactor);
    }

    private static HidDescriptorInfo Descriptor(bool otp, string path) => new()
    {
        DevicePath = path,
        VendorId = 0x1050,
        ProductId = 0x0407,
        UsagePage = otp ? (ushort)1 : (ushort)0xF1D0,
        Usage = otp ? (ushort)6 : (ushort)1
    };

    internal static YubiKeyDevice Device(HidConnectionSlot? fido, HidConnectionSlot? otp) =>
        new($"l5-device-{Guid.NewGuid():N}", null, fido, otp, null);

    internal static async Task<IConnection> OpenAsync(YubiKeyDevice device, bool otp, CancellationToken token) => otp
        ? await device.ConnectAsync<IOtpHidConnection>(token)
        : await device.ConnectAsync<IFidoHidConnection>(token);
}