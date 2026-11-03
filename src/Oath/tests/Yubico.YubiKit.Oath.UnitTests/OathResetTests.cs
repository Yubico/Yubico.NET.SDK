// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Oath.UnitTests;

public sealed class OathResetTests
{
    private static byte[] Select(byte salt = 1) => [0x79, 3, 5, 7, 0, 0x71, 1, salt, 0x90, 0];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetAsync_Success_IsTerminalAndLeavesBorrowedConnectionToCaller(bool cancel)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var connection = new ResetConnection(Select(), [0x90, 0]);
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: cancellation.Token);
        ReadOnlyMemory<byte> oldSalt = session.Salt;
        if (cancel)
            connection.AfterReset = cancellation.Cancel;

        await session.ResetAsync(cancellation.Token);

        int commandCount = connection.Inner.TransmittedCommands.Count;
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        Assert.All(oldSalt.ToArray(), value => Assert.Equal(0, value));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListCredentialsAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ResetAsync(TestContext.Current.CancellationToken));
        Assert.Throws<ObjectDisposedException>(() => session.DeriveKey(new byte[1]));
        Assert.Equal(commandCount, connection.Inner.TransmittedCommands.Count);
        Assert.Equal(0, connection.Inner.DisposeCount);

        Assert.Equal(2, commandCount);
        Assert.Equal(new byte[] { 0, 4, 0xDE, 0xAD, 0 }, connection.Inner.TransmittedCommands[1]);
        // The SDK detached the session, but the caller must close/reopen its borrowed connection.
        await connection.DisposeAsync();
        Assert.Equal(1, connection.Inner.DisposeCount);
        await using var reopened = new RecordingSmartCardConnection(Select(2));
        await using var replacement = await OathSession.CreateAsync(reopened, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(replacement.IsInitialized);
        Assert.Equal(new byte[] { 2 }, replacement.Salt.ToArray());
    }

    [Fact]
    public async Task ResetAsync_Success_DisposesOwnedConnectionOnceWithoutRefresh()
    {
        var connection = new ResetConnection(Select(), [0x90, 0]);
        var device = new ResetDevice(connection);
        var session = await device.CreateOathSessionAsync(
            new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 8, 0) }, TestContext.Current.CancellationToken);
        await session.ResetAsync(TestContext.Current.CancellationToken);
        Assert.Equal(new FirmwareVersion(5, 8, 0), session.FirmwareVersion);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        Assert.Equal(2, connection.Inner.TransmittedCommands.Count);
        Assert.Equal(1, connection.Inner.DisposeCount);
        await session.DisposeAsync();
        Assert.Equal(1, connection.Inner.DisposeCount);
    }

    [Fact]
    public async Task ResetAsync_CancelledBeforeAdmission_DoesNotInvalidateSession()
    {
        var connection = new RecordingSmartCardConnection(Select(), [0x90, 0]);
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.ResetAsync(cancellation.Token));
        Assert.True(session.IsInitialized);
        Assert.Single(connection.TransmittedCommands);
        Assert.Empty(await session.ListCredentialsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResetAsync_OverlapRefused_DoesNotTearDownAdmittedList()
    {
        var connection = new ResetConnection(Select(), [0x90, 0], [0x90, 0]);
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.BeforeList = async () =>
        {
            entered.SetResult();
            await release.Task;
        };
        var admitted = session.ListCredentialsAsync(TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResetAsync(TestContext.Current.CancellationToken));
            Assert.True(session.IsInitialized);
            Assert.Single(connection.Inner.TransmittedCommands);
        }
        finally
        {
            release.TrySetResult();
            // Drain the admitted fake exchange before disposing its session, even on assertion failure.
            await admitted.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        Assert.Empty(await admitted);
        connection.BeforeList = null;
        Assert.Empty(await session.ListCredentialsAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetAsync_SuccessButOwnedDisposalFails_PropagatesDisposalFailureAndRemainsTerminal(bool cancel)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var cleanupFailure = new InvalidOperationException("Owned connection teardown failed.");
        var connection = new ResetConnection(Select(), [0x90, 0]) { DisposeFailure = cleanupFailure };
        var device = new ResetDevice(connection);
        var session = await device.CreateOathSessionAsync(cancellationToken: cancellation.Token);
        if (cancel)
            connection.AfterReset = cancellation.Cancel;

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResetAsync(cancellation.Token));
        Assert.Same(cleanupFailure, failure);

        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
        Assert.Equal(2, connection.Inner.TransmittedCommands.Count);
        Assert.Equal(1, connection.Inner.DisposeCount);
        Assert.Same(cleanupFailure, Assert.Throws<InvalidOperationException>(session.Dispose));
        Assert.Equal(1, connection.Inner.DisposeCount);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListCredentialsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResetAsync_DeviceError_DoesNotClaimConfirmedSuccessOrDisposeSession()
    {
        var connection = new RecordingSmartCardConnection(Select(), [0x6A, 0x80], [0x90, 0]);
        await using var session = await OathSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<Core.Protocols.SmartCard.Apdu.ApduException>(() => session.ResetAsync(TestContext.Current.CancellationToken));
        Assert.True(session.IsInitialized);
        Assert.Equal(0, connection.DisposeCount);
        Assert.Empty(await session.ListCredentialsAsync(TestContext.Current.CancellationToken));
    }

    private sealed class ResetDevice(ResetConnection connection) : IYubiKey
    {
        public string DeviceId => "owned-reset-failure";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection => Task.FromResult(Assert.IsAssignableFrom<TConnection>(connection));
    }

    [Fact]
    public async Task ResetAsync_Success_AwaitsAsyncDisposalAndRefusesOperationsWhileTeardownDrains()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new ResetConnection(Select(), [0x90, 0])
        {
            BeforeAsyncDispose = async () =>
            {
                entered.SetResult();
                await release.Task;
            }
        };
        var session = await new ResetDevice(connection).CreateOathSessionAsync(cancellationToken: cancellation.Token);
        var reset = session.ResetAsync(cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            cancellation.Cancel();
            Assert.False(reset.IsCompleted);
            Assert.False(session.IsInitialized);
            Assert.False(session.IsAuthenticated);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListCredentialsAsync(TestContext.Current.CancellationToken));
            Assert.Equal(2, connection.Inner.TransmittedCommands.Count);
            Assert.Equal(0, connection.Inner.DisposeCount);
        }
        finally
        {
            release.TrySetResult();
            // Teardown owns the fake connection until it drains, even if the operation token expired.
            await reset.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        Assert.Equal(1, connection.Inner.DisposeCount);
    }

    private sealed class ResetConnection(params byte[][] responses) : ISmartCardConnection
    {
        public RecordingSmartCardConnection Inner { get; } = new(responses);
        public Action? AfterReset { get; set; }
        public Func<Task>? BeforeList { get; set; }
        public Exception? DisposeFailure { get; init; }
        public Func<Task>? BeforeAsyncDispose { get; init; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            if (command.Span[1] == 0xA1 && BeforeList is { } beforeList)
                await beforeList();
            var response = await Inner.TransmitAndReceiveAsync(command, cancellationToken);
            if (command.Span[1] == 4)
                AfterReset?.Invoke();
            return response;
        }
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => Inner.BeginTransaction(cancellationToken);
        public bool SupportsExtendedApdu() => false;
        public void Dispose()
        {
            Inner.Dispose();
            if (DisposeFailure is { } failure)
                throw failure;
        }
        public async ValueTask DisposeAsync()
        {
            if (BeforeAsyncDispose is { } beforeAsyncDispose)
                await beforeAsyncDispose();
            Dispose();
        }
    }
}