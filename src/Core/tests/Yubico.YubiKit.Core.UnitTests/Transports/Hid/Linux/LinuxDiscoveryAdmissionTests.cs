// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Devices;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;
using FidoApi = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.ReportApi;
using OtpApi = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxOtpHidConnectionTests.ReportApi;
using Reactor = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.Reactor;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

[SupportedOSPlatform("linux")]
[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class LinuxDiscoveryAdmissionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public Task FidoDirect_TimeoutKeepsPidAdmissionThroughOpenAndClose() =>
        RunHeldOpenProbeAsync(false, false, false, 0, nameof(FidoDirect_TimeoutKeepsPidAdmissionThroughOpenAndClose));

    [Fact]
    public Task FidoComposite_CanceledWaitKeepsPidAdmissionUntilEintrClose() =>
        RunHeldOpenProbeAsync(false, true, true, 4, nameof(FidoComposite_CanceledWaitKeepsPidAdmissionUntilEintrClose));

    [Fact]
    public Task OtpDirect_CanceledWaitKeepsPidAdmissionUntilEioClose() =>
        RunHeldOpenProbeAsync(true, false, true, 5, nameof(OtpDirect_CanceledWaitKeepsPidAdmissionUntilEioClose));

    [Fact]
    public Task OtpComposite_TimeoutKeepsPidAdmissionThroughOpenAndClose() =>
        RunHeldOpenProbeAsync(true, true, false, 0, nameof(OtpComposite_TimeoutKeepsPidAdmissionThroughOpenAndClose));

    private static Task RunHeldOpenProbeAsync(bool otp, bool composite, bool cancel, int closeError, string methodName) =>
        // A failed barrier watchdog must not quarantine a process-wide PID in the parent runner either.
        PcscIsolatedProbe.RunAsync($"linux-discovery-held-{otp}-{composite}", typeof(LinuxDiscoveryAdmissionTests), methodName,
            () => AssertHeldOpenAsync(otp, composite, cancel, closeError));

    private static async Task AssertHeldOpenAsync(
        bool otp, bool composite, bool cancel, int closeError)
    {
        using var openEntered = new ManualResetEventSlim();
        using var releaseOpen = new ManualResetEventSlim();
        using var closeEntered = new ManualResetEventSlim();
        using var releaseClose = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using var siblingCancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var fido = new FidoApi { MetadataError = new IOException("Metadata after open"), CloseError = closeError };
        var keyboard = new OtpApi { GetError = new IOException("Report after open"), CloseError = closeError };
        // These native-seam barriers outlive caller/runner cancellation until finally releases them.
        // The child-process watchdog isolates even a failed barrier or unproven fake close.
        void before(string operation)
        {
            if (operation == "open")
            {
                openEntered.Set();
                if (!releaseOpen.Wait(TimeSpan.FromSeconds(10), CancellationToken.None)) throw new TimeoutException("Open not released.");
            }
            else if (operation == "close")
            {
                closeEntered.Set();
                if (!releaseClose.Wait(TimeSpan.FromSeconds(10), CancellationToken.None)) throw new TimeoutException("Close not released.");
            }
        }
        fido.Before = keyboard.Before = before;
        var slot = LinuxHidRoutingTests.Slot(otp, otp ? keyboard : fido, new Reactor());
        var device = LinuxHidRoutingTests.Device(otp ? null : slot, otp ? slot : null);
        var scope = new DiscoveryReadScope();
        scope.CoordinateUsbRead(slot.InterfaceId, 0x0407);
        Task<DiscoveryReadAdmission>? sibling = null;
        Task recovery = Task.CompletedTask;
        var read = composite
            ? DiscoveryDeviceInfoReader.ReadBoundedAsync(device, slot.ConnectionType,
                cancel ? TimeSpan.FromSeconds(5) : TimeSpan.FromMilliseconds(100), NullLogger.Instance, cancellation.Token, true, scope)
            : DiscoveryDeviceInfoReader.ReadSlotBoundedAsync(slot, slot.ConnectionType,
                cancel ? TimeSpan.FromSeconds(5) : TimeSpan.FromMilliseconds(100), NullLogger.Instance, cancellation.Token, true, scope);
        try
        {
            Assert.True(openEntered.Wait(TimeSpan.FromSeconds(5), Token));
            sibling = DiscoveryReadAdmission.AcquireAsync(DiscoveryReadAdmission.GatesFor(0x0407), siblingCancellation.Token);
            Assert.False(sibling.IsCompleted);
            if (cancel)
            {
                await cancellation.CancelAsync();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
            }
            else
            {
                await Assert.ThrowsAsync<TimeoutException>(() => read);
            }

            // Caller cancellation and scope retirement abandon discovery, not the native open.
            scope.Retire();
            recovery = DeviceConnectionRegistry.WaitForRecoveryForTest(slot.InterfaceId);
            Assert.False(recovery.IsCompleted);
            Assert.False(sibling.IsCompleted);
            releaseOpen.Set();
            Assert.True(closeEntered.Wait(TimeSpan.FromSeconds(5), Token));
            Assert.False(recovery.IsCompleted);
            Assert.False(sibling.IsCompleted);
            Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(slot.InterfaceId));

            releaseClose.Set();
            using var nextAdmission = await sibling.WaitAsync(TimeSpan.FromSeconds(5), Token);
            await recovery.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
            Assert.Equal(1, otp ? keyboard.Closes : fido.Closes);
        }
        finally
        {
            scope.Retire();
            releaseOpen.Set();
            releaseClose.Set();
            await cancellation.CancelAsync();
            await siblingCancellation.CancelAsync();
            // Keep barriers alive until detached native cleanup and any admitted sibling have drained,
            // even after runner cancellation. Every cleanup wait has a finite watchdog.
            _ = await Record.ExceptionAsync(() => read.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            if (sibling is not null)
            {
                try { (await sibling.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)).Dispose(); }
                catch (OperationCanceledException) when (siblingCancellation.IsCancellationRequested) { }
            }
            await recovery.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    [Fact]
    public Task FidoRead_FailedRemovalKeepsPidAdmissionThroughActualQuiescenceAndClose() =>
        PcscIsolatedProbe.RunAsync("linux-discovery-held-quiescence", typeof(LinuxDiscoveryAdmissionTests),
            nameof(FidoRead_FailedRemovalKeepsPidAdmissionThroughActualQuiescenceAndClose), AssertHeldQuiescenceAsync);

    private static async Task AssertHeldQuiescenceAsync()
    {
        using var closeEntered = new ManualResetEventSlim();
        using var releaseClose = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var api = new FidoApi { WriteError = new IOException("INIT write failed") };
        var reactor = new Reactor { HoldDrain = true, RemovalError = new IOException("Removal wake failed") };
        api.Before = operation =>
        {
            if (operation != "close") return;
            closeEntered.Set();
            // Native-seam teardown must wait for finally's release even after runner cancellation.
            if (!releaseClose.Wait(TimeSpan.FromSeconds(10), CancellationToken.None)) throw new TimeoutException("Close not released.");
        };
        var slot = LinuxHidRoutingTests.Slot(false, api, reactor);
        var scope = new DiscoveryReadScope();
        scope.CoordinateUsbRead(slot.InterfaceId, 0x0407);
        var read = DiscoveryDeviceInfoReader.ReadSlotBoundedAsync(slot, slot.ConnectionType,
            TimeSpan.FromMilliseconds(100), NullLogger.Instance, Token, true, scope);
        Task<DiscoveryReadAdmission>? sibling = null;
        try
        {
            await reactor.Removal.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            await Assert.ThrowsAsync<TimeoutException>(() => read);
            scope.Retire();
            sibling = DiscoveryReadAdmission.AcquireAsync(DiscoveryReadAdmission.GatesFor(0x0407), cancellation.Token);
            Assert.False(sibling.IsCompleted);
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, reactor.ReferenceReleases);
            reactor.Drain.TrySetResult();
            Assert.True(closeEntered.Wait(TimeSpan.FromSeconds(5), Token));
            Assert.Equal(1, reactor.ReferenceReleases);
            Assert.False(sibling.IsCompleted);
            Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(slot.InterfaceId));

            releaseClose.Set();
            using var nextAdmission = await sibling.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
            Assert.Equal(1, api.Closes);
        }
        finally
        {
            scope.Retire();
            reactor.Drain.TrySetResult();
            releaseClose.Set();
            await cancellation.CancelAsync();
            // Actual drain and checked close must finish before the fake barriers are disposed,
            // including runner cancellation. Do not turn a watchdog into native release evidence.
            _ = await Record.ExceptionAsync(() => read.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            if (sibling is not null)
            {
                try { (await sibling.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)).Dispose(); }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            }
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    [Fact]
    public Task FidoDirect_UnprovenCloseQuarantinesPidAdmission() =>
        RunQuarantineProbeAsync(false, false, nameof(FidoDirect_UnprovenCloseQuarantinesPidAdmission));

    [Fact]
    public Task FidoComposite_UnprovenCloseQuarantinesPidAdmission() =>
        RunQuarantineProbeAsync(false, true, nameof(FidoComposite_UnprovenCloseQuarantinesPidAdmission));

    [Fact]
    public Task OtpDirect_UnprovenCloseQuarantinesPidAdmission() =>
        RunQuarantineProbeAsync(true, false, nameof(OtpDirect_UnprovenCloseQuarantinesPidAdmission));

    [Fact]
    public Task OtpComposite_UnprovenCloseQuarantinesPidAdmission() =>
        RunQuarantineProbeAsync(true, true, nameof(OtpComposite_UnprovenCloseQuarantinesPidAdmission));

    private static Task RunQuarantineProbeAsync(bool otp, bool composite, string methodName) =>
        // Permanent PID quarantine must never poison the parent runner's global admission gates.
        PcscIsolatedProbe.RunAsync($"linux-discovery-{otp}-{composite}", typeof(LinuxDiscoveryAdmissionTests), methodName,
            () => AssertQuarantineAsync(otp, composite));

    private static async Task AssertQuarantineAsync(bool otp, bool composite)
    {
        var fido = new FidoApi { MetadataError = new IOException("Metadata after open"), CloseError = 9 };
        var keyboard = new OtpApi { GetError = new IOException("Report after open"), CloseError = 9 };
        var slot = LinuxHidRoutingTests.Slot(otp, otp ? keyboard : fido, new Reactor());
        var device = LinuxHidRoutingTests.Device(otp ? null : slot, otp ? slot : null);
        var scope = new DiscoveryReadScope();
        scope.CoordinateUsbRead(slot.InterfaceId, 0x0407);
        var read = composite
            ? DiscoveryDeviceInfoReader.ReadBoundedAsync(device, slot.ConnectionType,
                TimeSpan.FromSeconds(5), NullLogger.Instance, Token, true, scope)
            : DiscoveryDeviceInfoReader.ReadSlotBoundedAsync(slot, slot.ConnectionType,
                TimeSpan.FromSeconds(5), NullLogger.Instance, Token, true, scope);
        if (otp)
        {
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => read);
        }
        else
        {
            var failure = await Assert.ThrowsAsync<AggregateException>(() => read);
            Assert.IsType<IOException>(failure.InnerExceptions[0]);
            Assert.IsType<UnrecoveredConnectionException>(failure.InnerExceptions[1]);
        }
        scope.Retire();

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var sibling = DiscoveryReadAdmission.AcquireAsync(DiscoveryReadAdmission.GatesFor(0x0407), cancellation.Token);
        try
        {
            // A completed failed-disposal task and retirement are not native release evidence.
            Assert.False(sibling.IsCompleted);
            Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(slot.InterfaceId));
            Assert.False(DeviceConnectionRegistry.WaitForRecoveryForTest(slot.InterfaceId).IsCompleted);
            Assert.Equal(1, otp ? keyboard.Closes : fido.Closes);
            using var unrelated = await DiscoveryReadAdmission.AcquireAsync(DiscoveryReadAdmission.GatesFor(0x0404), Token)
                .WaitAsync(TimeSpan.FromSeconds(5), Token);
        }
        finally
        {
            await cancellation.CancelAsync();
            try { (await sibling.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)).Dispose(); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
    }
}