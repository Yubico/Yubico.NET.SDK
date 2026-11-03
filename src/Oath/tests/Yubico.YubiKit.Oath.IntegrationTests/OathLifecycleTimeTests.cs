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

using System.Security.Cryptography;
using Xunit.Abstractions;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests;

public sealed class OathLifecycleTimeTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.3")]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, CustomFilter = typeof(OathProtocolResponseTests.Before563))]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task Timestamps_ExactRfcVectorsAndTerminalReset(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await WithCleanupAsync(state, () => state.WithOathSessionAsync(async session =>
        {
            output.WriteLine($"Executed device serial={state.SerialNumber}, firmware={state.FirmwareVersion}");
            byte[] seed = "12345678901234567890"u8.ToArray();
            try
            {
                using var data = new CredentialData
                {
                    Name = "time",
                    OathType = OathType.Totp,
                    HashAlgorithm = OathHashAlgorithm.Sha1,
                    Secret = seed,
                    Digits = 8
                };
                await session.PutCredentialAsync(data, timeout.Token);
                var credential = Assert.Single(await session.ListCredentialsAsync(timeout.Token));
                // RFC 6238 Appendix B, plus epoch and exact step boundaries from RFC 4226.
                foreach ((long timestamp, string expected, long from, long to) in new (long, string, long, long)[]
                {
                    (0, "84755224", 0, 30), (29, "84755224", 0, 30),
                    (30, "94287082", 30, 60), (59, "94287082", 30, 60),
                    (60, "37359152", 60, 90), (20000000000, "65353130", 19999999980, 20000000010)
                })
                {
                    var single = await session.CalculateCodeAsync(credential, timestamp, timeout.Token);
                    var aggregate = Assert.Single(await session.CalculateAllAsync(timestamp, timeout.Token));
                    Assert.Equal(expected, single.Value);
                    Assert.Equal(from, single.ValidFrom);
                    Assert.Equal(to, single.ValidTo);
                    Assert.Equal(single, aggregate.Value);
                }
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.CalculateCodeAsync(credential, -1, timeout.Token));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.CalculateAllAsync(long.MaxValue, timeout.Token));
                string before = session.DeviceId;
                await session.ResetAsync(timeout.Token);
                Assert.False(session.IsInitialized);
                await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListCredentialsAsync(timeout.Token));
                // This convenience-created session closed its owned connection during RESET.
                var reopened = await state.Device.CreateOathSessionAsync(cancellationToken: timeout.Token);
                await WithDisposedSessionAsync(reopened, async () =>
                {
                    Assert.NotEqual(before, reopened.DeviceId);
                    Assert.False(reopened.IsPasswordProtected);
                    Assert.Empty(await reopened.ListCredentialsAsync(timeout.Token));
                });
                output.WriteLine("Epoch, step boundaries, future RFC vector, aggregate equality and LIST on a fresh connection/session passed.");
            }
            finally { CryptographicOperations.ZeroMemory(seed); }
        }, cancellationToken: timeout.Token));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.3")]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ResetWithScp03_DefaultKeys_EndsSessionAndRequiresExplicitReopen(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var parameters = Scp03KeyParameters.Default;
        output.WriteLine($"SCP03 probe device serial={state.SerialNumber}, firmware={state.FirmwareVersion}; no Security Domain reset or key changes.");
        // Establish SCP before any reset. INITIALIZE UPDATE returning 6A88 leaves the
        // cause unresolved and provides no evidence about reset recovery.
        OathSession session;
        try
        {
            session = await state.Device.CreateOathSessionAsync(
                new SessionCreationOptions { ScpKeyParameters = parameters }, timeout.Token);
        }
        catch (ApduException exception) when (exception.SW == unchecked((short)0x6A88) &&
                                             exception.Cla == 0x80 && exception.Ins == 0x50)
        {
            output.WriteLine("INITIALIZE UPDATE returned 6A88 before RESET; cause unresolved. Reset recovery was not exercised.");
            Skip.If(true, "INITIALIZE UPDATE returned 6A88 before RESET; cause unresolved. SCP reset recovery not exercised.");
            throw;
        }
        await WithSessionCleanupAsync(state, session, async () =>
        {
            Assert.True(session.IsAuthenticated);
            await session.ListCredentialsAsync(timeout.Token);
            output.WriteLine("Default SCP03 establishment and initial protected LIST passed.");
            await session.ResetAsync(timeout.Token);
            Assert.False(session.IsInitialized);
            Assert.False(session.IsAuthenticated);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ListCredentialsAsync(timeout.Token));
            // RESET disposed this convenience-owned connection. Explicit creation opens a new one,
            // borrowing still-live parameters only for the new handshake, not from the old session.
            var reopened = await state.Device.CreateOathSessionAsync(
                new SessionCreationOptions { ScpKeyParameters = parameters }, timeout.Token);
            await WithDisposedSessionAsync(reopened, async () =>
            {
                Assert.True(reopened.IsAuthenticated);
                Assert.Empty(await reopened.ListCredentialsAsync(timeout.Token));
            });
            output.WriteLine("SCP03 RESET ended the old session; explicit reopen/handshake and protected LIST passed.");
        });
    }

    private async Task WithSessionCleanupAsync(YubiKeyTestState state, OathSession session, Func<Task> operation)
    {
        // The outer scope also captures disposal as the first failure before independent cleanup.
        // This is the only disposal owner, including failures in the initial assertion or LIST.
        await WithCleanupAsync(state, () => WithDisposedSessionAsync(session, operation));
    }

    private async Task WithDisposedSessionAsync(OathSession session, Func<Task> operation)
    {
        Exception? operationFailure = null;
        try { await operation(); }
        catch (Exception exception)
        {
            operationFailure = exception;
            throw;
        }
        finally
        {
            try { await session.DisposeAsync(); }
            catch (Exception exception) when (operationFailure is not null)
            {
                output.WriteLine($"OATH session teardown also failed ({exception.GetType().Name}); preserving the primary failure.");
            }
        }
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
    public async Task SessionCleanup_OwnedSession_PreservesFirstFailure(bool operationFails, bool disposalFails, bool cleanupFails)
    {
        var operationFailure = new InvalidOperationException("Initial LIST failed.");
        var disposalFailure = new InvalidOperationException("Owned connection disposal failed.");
        var cleanupFailure = new InvalidOperationException("Independent OATH RESET failed.");
        var connection = new LifecycleConnection(SelectResponse(), [0x90, 0])
        {
            ListFailure = operationFails ? operationFailure : null,
            DisposeFailure = disposalFails ? disposalFailure : null
        };
        var cleanupConnection = new LifecycleConnection(SelectResponse(), [0x90, 0], SelectResponse())
        {
            ResetFailure = cleanupFails ? cleanupFailure : null
        };
        var cleanupBody = new LifecycleConnection(SelectResponse());
        var device = new LifecycleDevice(connection, cleanupConnection, cleanupBody);
        var info = DeviceInfo.CreateFromTlvs([], new FirmwareVersion(5, 7, 0));
        var state = new YubiKeyTestState(device, info, ConnectionType.SmartCard);
        var session = await device.CreateOathSessionAsync();

        var failure = await Record.ExceptionAsync(() => WithSessionCleanupAsync(state, session,
            () => session.ListCredentialsAsync()));

        Exception? expected = operationFails ? operationFailure : disposalFails ? disposalFailure : cleanupFails ? cleanupFailure : null;
        Assert.Same(expected, failure);
        Assert.False(session.IsInitialized);
        Assert.Equal(1, connection.Inner.DisposeCount);
        Assert.Equal(cleanupFails ? 2 : 3, device.ConnectCount);
        Assert.True(device.CleanupToken.CanBeCanceled);
        Assert.False(device.CleanupToken.IsCancellationRequested);
        Assert.Equal(1, cleanupConnection.ResetCount);
        Assert.Equal(1, cleanupConnection.Inner.DisposeCount);
        // Core shares a failed disposal completion. A second owner would replay this failure.
        if (disposalFails)
            Assert.Same(disposalFailure, await Record.ExceptionAsync(() => session.DisposeAsync().AsTask()));
        Assert.Equal(1, connection.Inner.DisposeCount);
    }

    private static byte[] SelectResponse() => [0x79, 3, 5, 7, 0, 0x71, 1, 1, 0x90, 0];

    [Fact]
    public async Task SetupReset_ClosesResetConnectionBeforeOpeningFreshBodySession()
    {
        var reset = new LifecycleConnection(SelectResponse(), [0x90, 0], SelectResponse(), [0x90, 0]);
        var body = new LifecycleConnection(SelectResponse(), [0x90, 0]);
        var device = new LifecycleDevice(reset, body);
        var state = new YubiKeyTestState(device, DeviceInfo.CreateFromTlvs([], new FirmwareVersion(5, 7, 0)), ConnectionType.SmartCard);

        await state.WithOathSessionAsync(async session =>
        {
            Assert.Equal(1, reset.Inner.DisposeCount);
            Assert.Equal(2, device.ConnectCount);
            Assert.True(session.IsInitialized);
            Assert.Empty(await session.ListCredentialsAsync());
        });

        Assert.Equal(2, reset.Inner.TransmittedCommands.Count);
        Assert.Equal(1, reset.ResetCount);
        Assert.Equal(1, reset.Inner.DisposeCount);
        Assert.Equal(1, body.Inner.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetupReset_FailurePreventsBodyOpenAndPreservesFirstFailure(bool sendFails)
    {
        var sendFailure = new InvalidOperationException("RESET transport failed; mutation unknown.");
        var disposalFailure = new InvalidOperationException("RESET connection teardown failed.");
        var reset = new LifecycleConnection(SelectResponse(), [0x90, 0])
        {
            ResetFailure = sendFails ? sendFailure : null,
            DisposeFailure = disposalFailure
        };
        var device = new LifecycleDevice(reset);
        var state = new YubiKeyTestState(device, DeviceInfo.CreateFromTlvs([], new FirmwareVersion(5, 7, 0)), ConnectionType.SmartCard);

        var failure = await Record.ExceptionAsync(() => state.WithOathSessionAsync(_ =>
            throw new InvalidOperationException("The test body must not run.")));

        Assert.Same(sendFails ? sendFailure : disposalFailure, failure);
        Assert.Equal(1, device.ConnectCount);
        Assert.Equal(1, reset.ResetCount);
        Assert.Equal(1, reset.Inner.DisposeCount);
    }

    [Fact]
    public async Task SetupBody_OperationAndOwnedDisposalFail_PreservesOperationFailure()
    {
        var operationFailure = new InvalidOperationException("Body LIST failed.");
        var body = new LifecycleConnection(SelectResponse())
        {
            ListFailure = operationFailure,
            DisposeFailure = new InvalidOperationException("Body connection teardown failed.")
        };
        var device = new LifecycleDevice(body);
        var state = new YubiKeyTestState(device, DeviceInfo.CreateFromTlvs([], new FirmwareVersion(5, 7, 0)), ConnectionType.SmartCard);

        var failure = await Record.ExceptionAsync(() => state.WithOathSessionAsync(
            session => session.ListCredentialsAsync(), resetBeforeUse: false));

        Assert.Same(operationFailure, failure);
        Assert.Equal(1, body.Inner.DisposeCount);
    }

    private sealed class LifecycleDevice(params LifecycleConnection[] connections) : IYubiKey
    {
        public string DeviceId => "oath-lifecycle-cleanup-fake";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public int ConnectCount { get; private set; }
        public CancellationToken CleanupToken { get; private set; }
        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            ConnectCount++;
            if (ConnectCount > 1)
            {
                Assert.Equal(1, connections[ConnectCount - 2].Inner.DisposeCount);
                CleanupToken = cancellationToken;
            }
            return Task.FromResult(Assert.IsAssignableFrom<TConnection>(connections[ConnectCount - 1]));
        }
    }

    private sealed class LifecycleConnection(params byte[][] responses) : ISmartCardConnection
    {
        public RecordingSmartCardConnection Inner { get; } = new(responses);
        public Exception? ListFailure { get; init; }
        public Exception? ResetFailure { get; init; }
        public Exception? DisposeFailure { get; init; }
        public int ResetCount { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            if (command.Span[1] == 0xA1 && ListFailure is { } listFailure)
                throw listFailure;
            if (command.Span[1] == 0x04)
            {
                ResetCount++;
                if (ResetFailure is { } resetFailure)
                    throw resetFailure;
            }
            return Inner.TransmitAndReceiveAsync(command, cancellationToken);
        }
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => Inner.BeginTransaction(cancellationToken);
        public bool SupportsExtendedApdu() => false;
        public void Dispose()
        {
            Inner.Dispose();
            if (DisposeFailure is { } failure)
                throw failure;
        }
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private async Task WithCleanupAsync(YubiKeyTestState state, Func<Task> operation)
    {
        Exception? primaryFailure = null;
        try { await operation(); }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally { await CleanupAsync(state, primaryFailure); }
    }

    private async Task CleanupAsync(YubiKeyTestState state, Exception? primaryFailure)
    {
        // A cancelled operation can leave OATH changed. Use a separate 15-second admission
        // token, not a wall-clock deadline: admitted exchanges and disposal do not take cancellation.
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await state.WithOathSessionAsync(_ => Task.CompletedTask, cancellationToken: cleanup.Token); }
        catch (Exception exception) when (primaryFailure is not null)
        {
            output.WriteLine($"OATH cleanup also failed ({exception.GetType().Name}); preserving the primary failure.");
        }
    }
}