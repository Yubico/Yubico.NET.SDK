// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Reflection;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.UnitTests.BoundaryInventory;
using Claim = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.Claim;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

public class LinuxOtpHidConnectionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("open")]
    [InlineData("get")]
    [InlineData("set")]
    public async Task HeldNativeCall_ReturnsPendingTaskAndDrainsBeforeRelease(string held)
    {
        using var probe = new WithheldNativeProbe();
        var api = new ReportApi { Before = operation => { if (operation == held) probe.EnterNative(); } };
        var claim = new Claim();
        IOtpHidConnection? connection = held == "open" ? null : await OpenAsync(api, Token, claim);
        Task<IOtpHidConnection>? opening = null;
        probe.Start(() => held == "open" ? opening = OpenAsync(api, Token, claim)
            : held == "get" ? connection!.ReceiveAsync(Token) : connection!.SendAsync(new byte[8], Token));
        try { probe.AssertResponsive(); }
        finally
        {
            await probe.ReleaseAndDrainAsync();
            if (opening is not null) connection = await opening;
            if (connection is not null) await connection.DisposeAsync();
        }
        Assert.Equal(1, claim.Releases);
        Assert.Equal(1, api.Closes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubmittedCancellation_DoesNotDetachOrPermitOverlapAndDisposalWaits(bool send)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using var probe = new WithheldNativeProbe();
        var api = new ReportApi();
        var claim = new Claim();
        var connection = await OpenAsync(api, Token, claim);
        api.Before = operation => { if (operation == (send ? "set" : "get")) probe.EnterNative(); };
        probe.Start(() => send ? connection.SendAsync(new byte[8], cancellation.Token) : connection.ReceiveAsync(cancellation.Token));
        Task? closing = null;
        Task? synchronousClosing = null;
        try
        {
            probe.AssertResponsive();
            cancellation.Cancel();
            var operation = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(operation.IsCompleted);
            Assert.Throws<InvalidOperationException>(() => { _ = connection.ReceiveAsync(Token); });
            closing = connection.DisposeAsync().AsTask();
            // Test-only caller thread; production disposal never queues a pool cleanup job.
            synchronousClosing = Task.Run(connection.Dispose, Token);
            Assert.False(closing.IsCompleted);
            Assert.False(synchronousClosing.IsCompleted);
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, claim.Releases);
        }
        finally
        {
            await probe.ReleaseAndDrainAsync();
            await (closing ?? connection.DisposeAsync().AsTask());
            if (synchronousClosing is not null) await synchronousClosing;
        }
        await connection.DisposeAsync();
        connection.Dispose();
        if (!send)
        {
            var actual = Assert.IsType<Task<ReadOnlyMemory<byte>>>(await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), Token));
            Assert.All((await actual).ToArray(), value => Assert.Equal(0xA7, value));
        }
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task FailedGet_ClearsPartialOwnedReportBeforeTaskFaults()
    {
        var api = new ReportApi { GetError = new IOException("partial GET") };
        await using var connection = await OpenAsync(api, Token);
        api.Before = operation => { if (operation == "get") api.GetBuffer = Field<byte[]>(Field<object>(connection, "_owner"), "_inputCopy"); };
        await Assert.ThrowsAsync<IOException>(() => connection.ReceiveAsync(Token));
        // The fake captures the production destination, not a second fake-owned copy.
        Assert.All(api.GetBuffer ?? throw new InvalidOperationException("Missing GET staging."), value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(9, false)]
    [InlineData(22, false)]
    public async Task CheckedClose_SharesOutcomeAndReleasesOnlyWithProof(int error, bool released)
    {
        var api = new ReportApi { CloseError = error };
        var claim = new Claim();
        var connection = await OpenAsync(api, Token, claim);
        if (error == 0) { await connection.DisposeAsync(); connection.Dispose(); }
        else
        {
            var failure = await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask());
            if (!released) Assert.IsType<UnrecoveredConnectionException>(failure);
            Assert.Same(failure, await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask()));
            Assert.Same(failure, Assert.ThrowsAny<Exception>(connection.Dispose));
        }
        Assert.Equal(released ? 1 : 0, claim.Releases);
        Assert.Equal(1, api.Closes);
        GC.SuppressFinalize(connection);
    }

    [Fact]
    public async Task PreCanceledOpen_AcquiresNoDescriptorAndReleasesClaim()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        cancellation.Cancel();
        var api = new ReportApi();
        var claim = new Claim();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => OpenAsync(api, cancellation.Token, claim));
        Assert.Equal(0, api.Opens);
        Assert.Equal(0, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelBeforeDispatch_SubmitsNoFeatureCall(bool send)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var api = new ReportApi();
        await using var connection = await OpenAsync(api, Token);
        var owner = Field<object>(connection, "_owner");
        Task operation;
        using (Field<Lock>(owner, "_sync").EnterScope())
        {
            operation = send ? connection.SendAsync(new byte[8], cancellation.Token) : connection.ReceiveAsync(cancellation.Token);
            cancellation.Cancel();
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(0, api.Gets);
        Assert.Equal(0, api.Sets);
        // Cancellation does not poison the reusable single operation slot.
        Assert.Equal(8, (await connection.ReceiveAsync(Token)).Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedSend_IsolatedFromCallerAndClearedBeforeSuccessOrFailure(bool fail)
    {
        using var probe = new WithheldNativeProbe();
        var api = new ReportApi { SetError = fail ? new IOException("SET failed") : null };
        await using var connection = await OpenAsync(api, Token);
        var source = Enumerable.Repeat((byte)0xA7, 8).ToArray();
        byte[]? copy = null;
        api.Before = operation =>
        {
            if (operation != "set") return;
            copy = Field<byte[]>(Field<object>(connection, "_owner"), "_outputCopy");
            probe.EnterNative();
        };
        probe.Start(() => connection.SendAsync(source, Token));
        try
        {
            probe.AssertResponsive();
            source.AsSpan().Clear();
            Assert.NotNull(copy);
            Assert.All(copy, value => Assert.Equal(0xA7, value));
        }
        finally
        {
            if (fail) await Assert.ThrowsAsync<IOException>(probe.ReleaseAndDrainAsync);
            else await probe.ReleaseAndDrainAsync();
        }
        Assert.NotNull(copy);
        Assert.All(copy, value => Assert.Equal(0, value));
        Assert.All(api.Sent ?? throw new InvalidOperationException("Missing SET."), value => Assert.Equal(0xA7, value));
        Assert.Equal(1, api.Sets);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public async Task CancelDuringOpen_RetainsClaimThroughLateCheckedCleanup(int closeError)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using var probe = new WithheldNativeProbe();
        var api = new ReportApi { CloseError = closeError, Before = operation => { if (operation == "open") probe.EnterNative(); } };
        var claim = new Claim();
        probe.Start(() => OpenAsync(api, cancellation.Token, claim));
        Exception? failure = null;
        try
        {
            probe.AssertResponsive();
            cancellation.Cancel();
            Assert.Equal(0, claim.Releases);
            Assert.Equal(0, api.Closes);
        }
        finally { failure = await Assert.ThrowsAnyAsync<Exception>(probe.ReleaseAndDrainAsync); }
        if (closeError == 0) Assert.IsType<OperationCanceledException>(failure, exactMatch: false);
        else
        {
            var combined = Assert.IsType<AggregateException>(failure);
            Assert.IsType<OperationCanceledException>(combined.InnerExceptions[0], exactMatch: false);
            if (closeError == 9) Assert.IsType<UnrecoveredConnectionException>(combined.InnerExceptions[1]);
            else Assert.IsType<Yubico.YubiKit.Core.Native.PlatformApiException>(combined.InnerExceptions[1]);
        }
        Assert.Equal(closeError == 9 ? 0 : 1, claim.Releases);
        Assert.Equal(1, api.Closes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOpenWithoutDescriptor_ReleasesClaimWithoutNativeClose(bool invalid)
    {
        var api = new ReportApi { InvalidHandle = invalid, OpenError = invalid ? null : new IOException("open failed") };
        var claim = new Claim();
        await Assert.ThrowsAsync<IOException>(() => OpenAsync(api, Token, claim));
        Assert.Equal(1, api.Opens);
        Assert.Equal(0, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [SupportedOSPlatform("linux")]
    [Fact]
    public void DirectFeatureFacade_PreservesNonEightSetLengthAndFixedGetMetadata()
    {
        var api = new ReportApi();
        using var connection = new LinuxHidFeatureReportConnection("fake", api);
        Assert.Equal(8, connection.InputReportSize);
        Assert.Equal(8, connection.OutputReportSize);
        connection.SetReport([1, 2, 3]);
        Assert.Equal<byte>([1, 2, 3], api.Sent ?? throw new InvalidOperationException("Missing SET."));
        Assert.Equal(8, connection.GetReport().Length);
        connection.Dispose();
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public async Task UnknownRelease_QuarantinesActualRegistryClaimAndCannotReopen()
    {
        string id = $"linux-otp-unknown-{Guid.NewGuid()}";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id], Token);
        var api = new ReportApi { CloseError = 9 };
        var connection = await OpenAsync(api, Token, claim);
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([id], Token).AsTask());
        GC.SuppressFinalize(connection);
    }

    [Fact]
    public async Task HeldCheckedClose_ReturnsPendingDisposalTaskAndRetainsClaim()
    {
        using var probe = new WithheldNativeProbe();
        var api = new ReportApi();
        var claim = new Claim();
        var connection = await OpenAsync(api, Token, claim);
        api.Before = operation => { if (operation == "close") probe.EnterNative(); };
        probe.Start(() => connection.DisposeAsync().AsTask());
        try
        {
            probe.AssertResponsive();
            Assert.Equal(0, claim.Releases);
            Assert.Throws<ObjectDisposedException>(() => { _ = connection.SendAsync(new byte[8], Token); });
        }
        finally { await probe.ReleaseAndDrainAsync(); }
        connection.Dispose();
        Assert.Equal(1, claim.Releases);
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public async Task OriginalRegistryClaim_IsHeldBeforeOpenAndUntilLateCanceledOpenCloses()
    {
        string id = $"linux-otp-late-open-{Guid.NewGuid()}";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id], Token);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using var probe = new WithheldNativeProbe();
        using var closeEntered = new ManualResetEventSlim();
        using var releaseClose = new ManualResetEventSlim();
        var api = new ReportApi
        {
            Before = operation =>
            {
                if (operation == "open") probe.EnterNative();
                if (operation != "close") return;
                closeEntered.Set();
                if (!releaseClose.Wait(TimeSpan.FromSeconds(5), Token)) throw new TimeoutException("Late close not released.");
            }
        };
        probe.Start(() => OpenAsync(api, cancellation.Token, claim));
        try
        {
            probe.AssertResponsive();
            await Assert.ThrowsAsync<ConnectionInUseException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([id], Token).AsTask());
            cancellation.Cancel();
            probe.Release.Set();
            Assert.True(closeEntered.Wait(TimeSpan.FromSeconds(5), Token));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([id], Token).AsTask());
            Assert.False((await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), Token)).IsCompleted);
        }
        finally
        {
            releaseClose.Set();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(probe.ReleaseAndDrainAsync);
        }
        using var successor = await DeviceConnectionRegistry.AcquireConnectionAsync([id], Token);
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public async Task ClaimReleaseFailure_IsSharedAndRetainsOwnerAfterProvenNativeClose()
    {
        var api = new ReportApi();
        var claim = new FailingClaim();
        var connection = await OpenAsync(api, Token, claim);
        var failure = await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        Assert.Same(failure, await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask()));
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Attempts);
        GC.SuppressFinalize(connection);
    }

    private sealed class FailingClaim : IDisposable
    {
        internal int Attempts;
        public void Dispose() { Attempts++; throw new IOException("Claim release failed."); }
    }

    internal static async Task<IOtpHidConnection> OpenAsync(ReportApi api, CancellationToken token, IDisposable? claim = null)
    {
        return await LinuxOtpHidConnection.OpenAsync("fake", api, token, claim);
    }

    internal static T Field<T>(object instance, string name) where T : class =>
        Assert.IsType<T>(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance), exactMatch: false);

    internal sealed class ReportApi : ILinuxHidReportApi
    {
        internal Action<string>? Before;
        internal Exception? OpenError { get; set; }
        internal Exception? GetError { get; set; }
        internal Exception? SetError { get; set; }
        internal int Opens, Gets, Sets, Closes, CloseError;
        internal bool InvalidHandle { get; set; }
        internal Thread? Worker { get; private set; }
        internal byte[]? GetBuffer;
        internal byte[]? Sent;
        internal Queue<byte[]> Reports { get; } = new();
        internal List<byte[]> SentReports { get; } = [];
        public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags)
        {
            Worker = Thread.CurrentThread;
            Opens++;
            Before?.Invoke("open");
            if (OpenError is not null) throw OpenError;
            var handle = new LinuxFileSafeHandle(_ => new(0, 0));
            if (!InvalidHandle) handle.Initialize(42);
            return handle;
        }
        public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload)
        {
            Assert.Same(Worker, Thread.CurrentThread);
            Gets++;
            Before?.Invoke("get");
            if (Reports.TryDequeue(out var report)) report.CopyTo(payload);
            else payload.Fill(0xA7);
            if (GetError is not null) throw GetError;
        }
        public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
        {
            Assert.Same(Worker, Thread.CurrentThread);
            Sets++;
            Before?.Invoke("set");
            Sent = payload.ToArray();
            SentReports.Add(Sent);
            if (SetError is not null) throw SetError;
        }
        public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle)
        {
            Assert.Same(Worker, Thread.CurrentThread);
            Closes++;
            Before?.Invoke("close");
            handle.Dispose();
            return new(true, CloseError is 0 or 4 or 5, CloseError == 0 ? 0 : -1, CloseError);
        }
        public (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle) => throw new NotSupportedException();
        public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload) => throw new NotSupportedException();
        public void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload) => throw new NotSupportedException();
    }
}