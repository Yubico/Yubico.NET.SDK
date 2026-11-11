// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.ComponentModel;
using System.Reflection;
using Yubico.YubiKit.Core.Transports.Hid.Windows;
using Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

public class WindowsOtpHidNativeOwnerTests
{
    internal static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OpenCancellation_AfterSuccessPublicationBeforeAwait_KeepsConnectionUsable()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var api = new WindowsOtpReportApi();
        var claim = new WindowsOtpClaim();
        var owner = await WindowsOtpHidNativeOwner.OpenAsync("feature", api, cancellation.Token, claim,
            async opening =>
            {
                await opening.WaitAsync(TimeSpan.FromSeconds(5), Token);
                // The worker has published success, but the factory still owns its cancellation registration.
                cancellation.Cancel();
            });
        try
        {
            Assert.Equal(8, (await owner.ReceiveAsync(Token).WaitAsync(TimeSpan.FromSeconds(5), Token)).Length);
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, claim.Releases);
        }
        finally
        {
            // Drain the original worker before test-owned resources leave scope, even if the test was cancelled.
            await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Theory]
    [InlineData("open")]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("close")]
    public async Task HeldNativeCall_ReturnsPendingAndRetainsOriginalClaim(string held)
    {
        using var probe = new WithheldNativeProbe();
        var api = new WindowsOtpReportApi();
        var claim = new WindowsOtpClaim();
        WindowsOtpHidConnection? connection = held == "open" ? null : await OpenAsync(api, claim);
        Task<WindowsOtpHidConnection>? opening = null;
        api.Before = operation => { if (operation == held) probe.EnterNative(); };
        probe.Start(() => held == "open" ? opening = OpenAsync(api, claim)
            : held == "get" ? (connection ?? throw new InvalidOperationException()).ReceiveAsync(Token)
            : held == "set" ? (connection ?? throw new InvalidOperationException()).SendAsync(new byte[8], Token)
            : (connection ?? throw new InvalidOperationException()).DisposeAsync().AsTask());
        try
        {
            probe.AssertResponsive();
            Assert.Equal(0, claim.Releases);
        }
        finally
        {
            await probe.ReleaseAndDrainAsync();
            if (opening is not null) connection = await opening;
            if (connection is not null) await connection.DisposeAsync();
        }
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAfterDispatch_DrainsFeatureBeforePublicCompletionAndClose(bool send)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using var probe = new WithheldNativeProbe();
        var api = new WindowsOtpReportApi();
        var claim = new WindowsOtpClaim();
        var connection = await OpenAsync(api, claim);
        api.Before = operation => { if (operation == (send ? "set" : "get")) probe.EnterNative(); };
        probe.Start(() => send ? connection.SendAsync(new byte[8], cancellation.Token) : connection.ReceiveAsync(cancellation.Token));
        Task? closing = null;
        try
        {
            probe.AssertResponsive();
            var operation = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            cancellation.Cancel();
            Assert.False(operation.IsCompleted);
            Assert.Throws<InvalidOperationException>(() => { _ = connection.SendAsync(new byte[8], Token); });
            Assert.Throws<InvalidOperationException>(() => { _ = connection.ReceiveAsync(Token); });
            closing = connection.DisposeAsync().AsTask();
            Assert.False(closing.IsCompleted);
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, claim.Releases);
            api.Before = name => { if (name == "close") Assert.True(operation.IsCompleted); };
        }
        finally
        {
            await probe.ReleaseAndDrainAsync();
            await (closing ?? connection.DisposeAsync().AsTask());
        }
        Assert.Equal(1, claim.Releases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationBeforeDispatch_SkipsNativeAndRestoresAdmission(bool send)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var api = new WindowsOtpReportApi();
        await using var connection = await OpenAsync(api);
        var owner = Field<WindowsOtpHidNativeOwner>(connection, "_owner");
        Task operation;
        byte[]? copy = null;
        using (Field<Lock>(owner, "_sync").EnterScope())
        {
            operation = send ? connection.SendAsync(new byte[8], cancellation.Token) : connection.ReceiveAsync(cancellation.Token);
            if (send) copy = Field<byte[]>(owner, "_outputCopy");
            cancellation.Cancel();
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(0, api.Gets + api.Sets);
        if (copy is not null) Assert.All(copy, value => Assert.Equal(0, value));
        Assert.Equal(8, (await connection.ReceiveAsync(Token)).Length);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(5u)]
    public async Task Send_CopiesCallerAndClearsFramedStorageAfterNativeReturn(uint error)
    {
        using var probe = new WithheldNativeProbe();
        var api = new WindowsOtpReportApi { FeatureError = error };
        await using var connection = await OpenAsync(api);
        byte[] source = [1, 2, 3, 4, 5, 6, 7, 8];
        api.Before = name => { if (name == "set") probe.EnterNative(); };
        probe.Start(() => connection.SendAsync(source, Token));
        try
        {
            probe.AssertResponsive();
            source.AsSpan().Clear();
            Assert.Equal<byte>([0, 1, 2, 3, 4, 5, 6, 7, 8], api.Buffer ?? throw new InvalidOperationException());
        }
        finally
        {
            if (error == 0) await probe.ReleaseAndDrainAsync();
            else await Assert.ThrowsAnyAsync<Exception>(probe.ReleaseAndDrainAsync);
        }
        Assert.All(api.Buffer ?? throw new InvalidOperationException(), value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(5u)]
    public async Task Get_ClearsNativeStagingAndTransfersSeparateSuccessfulPayload(uint error)
    {
        var api = new WindowsOtpReportApi { FeatureError = error };
        var connection = await OpenAsync(api);
        ReadOnlyMemory<byte> payload = default;
        try
        {
            if (error == 0) payload = await connection.ReceiveAsync(Token);
            else await Assert.ThrowsAnyAsync<Exception>(() => connection.ReceiveAsync(Token));
            Assert.All(api.Buffer ?? throw new InvalidOperationException(), value => Assert.Equal(0, value));
        }
        finally { await connection.DisposeAsync(); }
        if (error == 0) Assert.Equal<byte>(Enumerable.Repeat((byte)0xA7, 8), payload.ToArray());
    }

    [Fact]
    public async Task DescriptorLength_DirectOwnerPreservesExactPayloadAndTypedOpenCleansMismatch()
    {
        var api = new WindowsOtpReportApi { Metadata = new(1, 6, 17) };
        var owner = await WindowsOtpHidNativeOwner.OpenAsync("feature", api, Token);
        try
        {
            Assert.Equal(17, owner.Metadata.FeatureReportByteLength);
            Assert.Throws<ArgumentException>(() => { _ = owner.SendAsync(new byte[8], Token); });
            Assert.Equal(0, api.Sets);
            await owner.SendAsync(new byte[16], Token);
            Assert.Equal(17, api.Buffer?.Length);
            Assert.Equal(16, (await owner.ReceiveAsync(Token)).Length);
        }
        finally { await owner.ShutdownAsync(); }
        var typedApi = new WindowsOtpReportApi { Metadata = api.Metadata };
        var claim = new WindowsOtpClaim();
        await Assert.ThrowsAsync<IOException>(() => OpenAsync(typedApi, claim));
        Assert.Equal(1, typedApi.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32768)]
    [InlineData(65535)]
    public async Task InvalidDescriptor_CleansAcquisitionBeforeFailure(int length)
    {
        var api = new WindowsOtpReportApi { Metadata = new(1, 6, (ushort)length) };
        var claim = new WindowsOtpClaim();
        await Assert.ThrowsAsync<IOException>(() => OpenAsync(api, claim));
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Theory]
    [InlineData(false, 0u)]
    [InlineData(true, 0u)]
    [InlineData(true, 5u)]
    public async Task FailedOrCancelledOpen_KeepsClaimThroughCheckedClose(bool cancel, uint closeError)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using var probe = new WithheldNativeProbe();
        using var releaseClose = new ManualResetEventSlim();
        var closeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new WindowsOtpReportApi
        {
            OpenError = cancel ? null : new IOException("open metadata failed"),
            Release = new(true, closeError)
        };
        var claim = new WindowsOtpClaim();
        api.Before = name =>
        {
            if (name == "open") probe.EnterNative();
            if (name != "close") return;
            closeEntered.SetResult();
            if (!releaseClose.Wait(TimeSpan.FromSeconds(10), Token)) throw new TimeoutException();
        };
        probe.Start(() => WindowsOtpHidConnection.OpenAsync("feature", api, cancellation.Token, claim));
        try
        {
            probe.AssertResponsive();
            if (cancel) cancellation.Cancel();
            probe.Release.Set();
            await closeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.Equal(0, claim.Releases);
            Assert.False((await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), Token)).IsCompleted);
        }
        finally
        {
            releaseClose.Set();
            if (closeError != 0)
            {
                var error = await Assert.ThrowsAsync<AggregateException>(probe.ReleaseAndDrainAsync);
                Assert.Collection(error.InnerExceptions,
                    primary => Assert.IsAssignableFrom<OperationCanceledException>(primary),
                    cleanup => Assert.Equal((int)closeError, Assert.IsType<Win32Exception>(cleanup).NativeErrorCode));
            }
            else if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(probe.ReleaseAndDrainAsync);
            else await Assert.ThrowsAsync<IOException>(probe.ReleaseAndDrainAsync);
        }
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task PreCancelledOpen_ReleasesTransferredClaimWithoutNativeAcquisition()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        cancellation.Cancel();
        var api = new WindowsOtpReportApi();
        var claim = new WindowsOtpClaim();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WindowsOtpHidConnection.OpenAsync("feature", api, cancellation.Token, claim));
        Assert.Equal(0, api.Opens);
        Assert.Equal(0, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task Worker_DoesNotCaptureAmbientContextAndRejectsSynchronousSelfDrain()
    {
        var ambient = new AsyncLocal<string?> { Value = "caller" };
        var api = new WindowsOtpReportApi { Before = _ => Assert.Null(ambient.Value) };
        await using var connection = await OpenAsync(api);
        api.Before = _ => Assert.Throws<InvalidOperationException>(connection.Dispose);
        await connection.SendAsync(new byte[8], Token);
        api.Before = null;
        Assert.Equal("caller", ambient.Value);
    }

    internal static Task<WindowsOtpHidConnection> OpenAsync(WindowsOtpReportApi api, IDisposable? claim = null) =>
        WindowsOtpHidConnection.OpenAsync("feature", api, Token, claim);

    internal static T Field<T>(object instance, string name) where T : class =>
        Assert.IsType<T>(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance), exactMatch: false);
}