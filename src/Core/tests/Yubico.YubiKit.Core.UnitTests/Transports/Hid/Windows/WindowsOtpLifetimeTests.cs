// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid.Windows;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;
using static Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows.WindowsOtpHidNativeOwnerTests;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

[Trait("Category", "RuntimeResilience")]
public class WindowsOtpLifetimeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkerFailure_TerminalizesActualAcceptedTaskBeforeHeldCloseAndPreservesPrimary(bool send)
    {
        using var release = new ManualResetEventSlim();
        var closeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new WindowsOtpReportApi();
        var claim = new WindowsOtpClaim();
        var connection = await OpenAsync(api, claim);
        var owner = Field<WindowsOtpHidNativeOwner>(connection, "_owner");
        var primary = new IOException("injected loop failure");
        Task operation;
        byte[]? copy = null;
        api.Before = name =>
        {
            if (name != "close") return;
            closeEntered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10), Token)) throw new TimeoutException();
        };
        using (Field<Lock>(owner, "_sync").EnterScope())
        {
            operation = send ? connection.SendAsync(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, Token) : connection.ReceiveAsync(Token);
            if (send) copy = Field<byte[]>(owner, "_outputCopy");
            // Fail the dispatch loop independently of the accepted public completion slot.
            WorkField.SetValue(owner, (Action)(() => throw primary));
        }
        Task closing = owner.ShutdownAsync();
        try
        {
            await closeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.Same(primary, await Assert.ThrowsAsync<IOException>(() => operation));
            Assert.False(closing.IsCompleted);
            Assert.Equal(0, claim.Releases);
            if (copy is not null) Assert.All(copy, value => Assert.Equal(0, value));
            Assert.Throws<ObjectDisposedException>(() => { _ = connection.ReceiveAsync(Token); });
        }
        finally
        {
            release.Set();
            // Drain despite runner cancellation before retiring the fake gate.
            Assert.Same(primary, await Assert.ThrowsAsync<IOException>(() => closing.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)));
            Assert.True((api.Worker ?? throw new InvalidOperationException()).Join(TimeSpan.FromSeconds(5)));
        }
        Assert.Same(closing, owner.ShutdownAsync());
        Assert.Same(primary, Assert.Throws<IOException>(connection.Dispose));
        Assert.Equal(0, api.Gets + api.Sets);
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
        owner.RequestShutdown(); // Event has retired; finalizer request must remain safe.
        GC.SuppressFinalize(connection);
    }

    [Fact]
    public async Task PublicCompletions_UseAsynchronousContinuationsAndSharedActualDisposalTask()
    {
        var api = new WindowsOtpReportApi();
        var connection = await OpenAsync(api);
        var send = connection.SendAsync(new byte[8], Token);
        Assert.True(send.CreationOptions.HasFlag(TaskCreationOptions.RunContinuationsAsynchronously));
        await send;
        var receive = connection.ReceiveAsync(Token);
        Assert.True(receive.CreationOptions.HasFlag(TaskCreationOptions.RunContinuationsAsynchronously));
        await receive;
        var closing = connection.DisposeAsync().AsTask();
        Assert.Same(closing, connection.DisposeAsync().AsTask());
        Assert.True(closing.CreationOptions.HasFlag(TaskCreationOptions.RunContinuationsAsynchronously));
        await closing;
        connection.Dispose();
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public async Task ProvenCloseDiagnostic_ReleasesClaimAndSharesNativeError()
    {
        var api = new WindowsOtpReportApi { Release = new(true, 5) };
        var claim = new WindowsOtpClaim();
        var connection = await OpenAsync(api, claim);
        var failure = await Assert.ThrowsAsync<Win32Exception>(() => connection.DisposeAsync().AsTask());
        Assert.Equal(5, failure.NativeErrorCode);
        Assert.Same(failure, await Assert.ThrowsAsync<Win32Exception>(() => connection.DisposeAsync().AsTask()));
        Assert.Equal(1, claim.Releases);
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public Task UnknownRelease_QuarantinesOriginalRegistryClaimAndRetainsRoot() =>
        PcscIsolatedProbe.RunAsync("windows-otp-quarantine", typeof(WindowsOtpLifetimeTests),
            nameof(UnknownRelease_QuarantinesOriginalRegistryClaimAndRetainsRoot), UnknownReleaseAsync);

    private static async Task UnknownReleaseAsync()
    {
        string id = $"windows-otp-{Guid.NewGuid()}";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id], Token);
        var api = new WindowsOtpReportApi { Release = new(false, 5) };
        var connection = await OpenAsync(api, claim);
        var owner = Field<WindowsOtpHidNativeOwner>(connection, "_owner");
        var failure = await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        Assert.Same(failure, await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask()));
        Assert.Same(failure, Assert.Throws<UnrecoveredConnectionException>(connection.Dispose));
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([id], Token).AsTask());
        Assert.True(Root(owner).IsAllocated);
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public Task FailedOpenAndUnknownClose_PreservesBothErrorsAndClaim() =>
        PcscIsolatedProbe.RunAsync("windows-otp-open-quarantine", typeof(WindowsOtpLifetimeTests),
            nameof(FailedOpenAndUnknownClose_PreservesBothErrorsAndClaim), async () =>
            {
                var primary = new IOException("metadata failure after acquisition");
                var api = new WindowsOtpReportApi { OpenError = primary, Release = new(false, 6) };
                var claim = new WindowsOtpClaim();
                var failure = await Assert.ThrowsAsync<AggregateException>(() => OpenAsync(api, claim));
                Assert.Same(primary, failure.InnerExceptions[0]);
                Assert.IsType<UnrecoveredConnectionException>(failure.InnerExceptions[1]);
                Assert.Equal(0, claim.Releases);
                Assert.Equal(1, api.Closes);
            });

    [Fact]
    public Task Finalizer_RequestsShutdownWithoutWaitingForHeldCloseAndRetainsUnknownOwner() =>
        PcscIsolatedProbe.RunAsync("windows-otp-finalizer", typeof(WindowsOtpLifetimeTests),
            nameof(Finalizer_RequestsShutdownWithoutWaitingForHeldCloseAndRetainsUnknownOwner), FinalizerAsync);

    private static async Task FinalizerAsync()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new WindowsOtpReportApi { Release = new(false, 5) };
        var claim = new WindowsOtpClaim();
        api.Before = name =>
        {
            if (name != "close") return;
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10), Token)) throw new TimeoutException();
        };
        var (facade, owner) = await AbandonAsync(api, claim);
        await Task.Yield();
        try
        {
            for (int i = 0; i < 50 && facade.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Yield();
            }
            Assert.False(facade.IsAlive);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.Equal(0, claim.Releases);
            Assert.True(owner.IsAlive);
        }
        finally
        {
            release.Set();
            // Finalizer-requested worker must leave the held fake before its gate is disposed.
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => ObserveShutdown(owner).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            Assert.True((api.Worker ?? throw new InvalidOperationException()).Join(TimeSpan.FromSeconds(5)));
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.True(owner.IsAlive);
        Assert.Equal(0, claim.Releases);
        Assert.Equal(1, api.Closes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Facade, WeakReference Owner)> AbandonAsync(WindowsOtpReportApi api, WindowsOtpClaim claim)
    {
        var connection = await OpenAsync(api, claim);
        return (new WeakReference(connection), new WeakReference(Field<WindowsOtpHidNativeOwner>(connection, "_owner")));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task ObserveShutdown(WeakReference reference) =>
        Assert.IsType<WindowsOtpHidNativeOwner>(reference.Target).ShutdownAsync();

    private static System.Runtime.InteropServices.GCHandle Root(WindowsOtpHidNativeOwner owner) =>
        Assert.IsType<System.Runtime.InteropServices.GCHandle>(typeof(WindowsOtpHidNativeOwner)
            .GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner));

    private static FieldInfo WorkField => typeof(WindowsOtpHidNativeOwner).GetField("_work", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Missing owner work slot.");
}