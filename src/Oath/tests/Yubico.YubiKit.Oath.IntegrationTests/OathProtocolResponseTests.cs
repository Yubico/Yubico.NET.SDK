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

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit.Abstractions;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.IntegrationTests;

public sealed class OathProtocolResponseTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.3")]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, CustomFilter = typeof(Before563))]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ProtocolResponses_KnownHmacVectorsAndLifecycle(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var token = timeout.Token;
        output.WriteLine($"Executed device serial={state.SerialNumber}, firmware={state.FirmwareVersion}");
        await RunWithOathCleanupAsync(state, () => state.WithOathSessionAsync(async session =>
        {
            Assert.NotEmpty(session.Salt.ToArray());
            Assert.False(session.IsLocked);
            byte[] seed = new byte[20];
            Array.Fill(seed, (byte)0x0B);
            try
            {
                foreach (var algorithm in new[] { OathHashAlgorithm.Sha1, OathHashAlgorithm.Sha256, OathHashAlgorithm.Sha512 })
                {
                    using var data = new CredentialData
                    {
                        Name = algorithm.ToString(),
                        OathType = OathType.Totp,
                        HashAlgorithm = algorithm,
                        Secret = seed
                    };
                    await session.PutCredentialAsync(data, token);
                    var credential = Assert.Single(await session.ListCredentialsAsync(token));
                    ReadOnlyMemory<byte> actual = await session.CalculateAsync(credential, "Hi There"u8.ToArray(), token);
                    byte[] expected = Convert.FromHexString(algorithm switch
                    {
                        OathHashAlgorithm.Sha1 => "B617318655057264E28BC0B6FB378C8EF146BE00",
                        OathHashAlgorithm.Sha256 => "B0344C61D8DB38535CA8AFCEAF0BF12B881DC200C9833DA726E9376C2E32CFF7",
                        OathHashAlgorithm.Sha512 => "87AA7CDEA5EF619D4FF0B4241A1D6CB02379F4E2CE4EC2787AD0B30545E17CDEDAA833B7D6B8A702038B274EAEA3F4E4BE9D914EEB61F1702E696C203A126854",
                        _ => throw new InvalidOperationException()
                    });
                    try
                    {
                        Assert.True(CryptographicOperations.FixedTimeEquals(expected, actual.Span), "Full HMAC must match the RFC vector without a digit header.");
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(expected);
                        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(actual).Span);
                    }
                    var single = await session.CalculateCodeAsync(credential, 30, token);
                    var aggregate = Assert.Single(await session.CalculateAllAsync(30, token));
                    Assert.Equal(single, aggregate.Value);
                    await session.DeleteCredentialAsync(credential, token);
                    Assert.Empty(await session.ListCredentialsAsync(token));
                    output.WriteLine($"{algorithm}: RFC full-HMAC vector and PUT/LIST/single/aggregate/DELETE passed");
                }

                foreach (var type in new[] { OathType.Hotp, OathType.Totp })
                {
                    using var data = new CredentialData
                    {
                        Name = type.ToString(),
                        OathType = type,
                        HashAlgorithm = OathHashAlgorithm.Sha1,
                        Secret = seed,
                        RequireTouch = true
                    };
                    await session.PutCredentialAsync(data, token);
                }
                var markers = await session.CalculateAllAsync(30, token);
                Assert.Equal(2, markers.Count);
                Assert.All(markers, item => Assert.Null(item.Value));
                var hotp = markers.Keys.Single(c => c.Name == "Hotp");
                Assert.Equal(OathType.Hotp, hotp.OathType);
                Assert.False(hotp.TouchRequired);
                var totp = markers.Keys.Single(c => c.Name == "Totp");
                Assert.Equal(OathType.Totp, totp.OathType);
                Assert.True(totp.TouchRequired);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(seed);
            }
        }, cancellationToken: token));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.3")]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, CustomFilter = typeof(Before563))]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    public async Task ProtocolResponses_SelectAndWrongKeyStatus(YubiKeyTestState state)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = timeout.Token;
        await VerifySelectAndWrongKeyStatusAsync(state, token);
    }

    private async Task VerifySelectAndWrongKeyStatusAsync(YubiKeyTestState state, CancellationToken token)
    {
        output.WriteLine($"Executed device serial={state.SerialNumber}, firmware={state.FirmwareVersion}");
        await RunWithOathCleanupAsync(state, async () =>
        {
            await state.WithOathSessionAsync(async session =>
            {
                byte[] key = new byte[16];
                Array.Fill(key, (byte)0xA5);
                try { await session.SetKeyAsync(key, token); }
                finally { CryptographicOperations.ZeroMemory(key); }
            }, cancellationToken: token);

            await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>(token);
            await using (var raw = await RawSmartCardSession.CreateAsync(connection, token))
            {
                ReadOnlyMemory<byte> selected = await raw.SelectAsync(ApplicationIds.Oath, token);
                using var tlvs = TlvHelper.DecodeList(selected.Span);
                Assert.Equal(3, Assert.Single(tlvs, t => t.Tag == 0x79).Value.Length);
                Assert.NotEmpty(Assert.Single(tlvs, t => t.Tag == 0x71).Value.ToArray());
                Assert.Equal(8, Assert.Single(tlvs, t => t.Tag == 0x74).Value.Length);
                output.WriteLine("Protected SELECT: version length=3; device ID present; challenge length=8");
            }
            await using var locked = await OathSession.CreateAsync(connection, cancellationToken: token);
            byte[] wrong = new byte[16];
            byte[] correct = new byte[16];
            Array.Fill(correct, (byte)0xA5);
            try
            {
                var exception = await Assert.ThrowsAsync<OathException>(() => locked.ValidateAsync(wrong, token));
                output.WriteLine($"Wrong-key VALIDATE status=0x{(ushort)(exception.StatusWord ?? 0):X4}; reason={exception.Reason}");
                Assert.True(locked.IsLocked);
                await locked.ValidateAsync(correct, token);
                Assert.False(locked.IsLocked);
                Assert.Empty(await locked.ListCredentialsAsync(token));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(wrong);
                CryptographicOperations.ZeroMemory(correct);
            }
        });
    }

    private async Task RunWithOathCleanupAsync(YubiKeyTestState state, Func<Task> operation)
    {
        Exception? operationFailure = null;
        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            operationFailure = exception;
            throw;
        }
        finally
        {
            // The operation may have expired after changing OATH. Teardown needs a separate
            // admission timeout; admitted exchanges/disposal are not wall-clock bounded.
            using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                await state.WithOathSessionAsync(_ => Task.CompletedTask, cancellationToken: cleanupTimeout.Token);
            }
            catch (Exception cleanupFailure) when (operationFailure is not null)
            {
                output.WriteLine($"OATH cleanup also failed ({cleanupFailure.GetType().Name}); preserving the operation failure.");
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cleanup_ExpiredOperationToken_StillResetsAndPreservesFailure(bool cleanupFails)
    {
        using var operation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var device = new CancelAfterMutationDevice(operation, cleanupFails);
        var info = DeviceInfo.CreateFromTlvs([], new FirmwareVersion(5, 7, 0));
        var state = new YubiKeyTestState(device, info, ConnectionType.SmartCard);

        var failure = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            VerifySelectAndWrongKeyStatusAsync(state, operation.Token));

        Assert.Same(device.OperationFailure, failure);
        Assert.Equal(cleanupFails ? 4 : 5, device.ConnectCount);
        Assert.True(device.CleanupToken.CanBeCanceled);
        Assert.False(device.CleanupToken.IsCancellationRequested);
        Assert.Contains(device.CleanupConnection.TransmittedCommands, command => command[1] == 0x04);
    }

    private static byte[] SelectResponse() => [0x79, 3, 5, 7, 0, 0x71, 1, 1, 0x90, 0];

    private sealed class CancelAfterMutationDevice(CancellationTokenSource operation, bool cleanupFails) : IYubiKey
    {
        public string DeviceId => "oath-cleanup-fake";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public int ConnectCount { get; private set; }
        public OperationCanceledException? OperationFailure { get; private set; }
        public CancellationToken CleanupToken { get; private set; }
        public RecordingSmartCardConnection CleanupConnection { get; } = new(
            SelectResponse(), cleanupFails ? [0x6A, 0x80] : [0x90, 0]);

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            ConnectCount++;
            if (cancellationToken.IsCancellationRequested)
            {
                OperationFailure = new OperationCanceledException("Operation token expired after SET CODE.", cancellationToken);
                throw OperationFailure;
            }

            IConnection connection;
            if (ConnectCount == 1)
                connection = new RecordingSmartCardConnection(SelectResponse(), [0x90, 0]);
            else if (ConnectCount == 2)
                connection = new CancelAfterSetCodeConnection(operation);
            else
            {
                CleanupToken = cancellationToken;
                connection = ConnectCount == 4 ? CleanupConnection : new RecordingSmartCardConnection(SelectResponse());
            }
            return Task.FromResult(Assert.IsAssignableFrom<TConnection>(connection));
        }
    }

    private sealed class CancelAfterSetCodeConnection(CancellationTokenSource operation) : ISmartCardConnection
    {
        private readonly RecordingSmartCardConnection _inner = new(
            SelectResponse(), [0x90, 0]);
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;
        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            var response = await _inner.TransmitAndReceiveAsync(command, cancellationToken);
            if (command.Span[1] == 0x03)
                operation.Cancel();
            return response;
        }
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => _inner.BeginTransaction(cancellationToken);
        public bool SupportsExtendedApdu() => false;
        public void Dispose()
        {
            foreach (byte[] command in _inner.TransmittedCommands)
                CryptographicOperations.ZeroMemory(command);
            _inner.Dispose();
        }
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    public sealed class Before563 : IYubiKeyFilter
    {
        public bool Matches(YubiKeyTestState device) => !device.FirmwareVersion.IsAtLeast(5, 6, 3);
        public string GetDescription() => "Firmware before 5.6.3";
    }
}