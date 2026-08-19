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

using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests.Authentication;

/// <summary>
/// End-to-end (public API) coverage proving <see cref="PivSession"/> wires PIN-only mode through
/// to the underlying protocol helpers (ISC-14, 14.1, 15, 15.1).
/// </summary>
public class PivPinOnlyModeSessionTests
{
    [Fact]
    public async Task GetPinOnlyModeAsync_NoAdminData_ReturnsNone()
    {
        var connection = CreateInitializedConnection([0x6A, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var mode = await session.GetPinOnlyModeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.None, mode);
        Assert.Contains(connection.TransmittedCommands, c => c[1] == 0xCB); // GET DATA
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_NotAuthenticated_ThrowsInvalidOperationException()
    {
        var connection = CreateInitializedConnection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SetPinOnlyModeAsync(
            PivPinOnlyMode.PinProtected,
            "123456"u8.ToArray(),
            new byte[24],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_PinDerivedRequested_ThrowsArgumentException()
    {
        var connection = CreateInitializedConnection();
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);

        await Assert.ThrowsAsync<ArgumentException>(() => session.SetPinOnlyModeAsync(
            PivPinOnlyMode.PinDerived,
            "123456"u8.ToArray(),
            new byte[24],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetPinOnlyModeAsync_AfterSuccessfulAuthentication_WhenSuppliedKeyFailsAuthentication_ClearsSessionStateBeforeMutation()
    {
        var connection = CreateInitializedConnection([0x69, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        // Represent a prior successful management-key authentication. The subsequent authentication
        // attempt runs through the real public-session and protocol flow using the queued APDU response.
        MarkAuthenticated(session);
        Assert.True(session.IsManagementKeyAuthenticated);
        int initializationCommandCount = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ApduException>(() => session.SetPinOnlyModeAsync(
            PivPinOnlyMode.PinProtected,
            "123456"u8.ToArray(),
            new byte[24],
            TestContext.Current.CancellationToken));

        Assert.True(exception.SW == 0x6982);
        Assert.False(session.IsManagementKeyAuthenticated);

        var operationCommands = connection.TransmittedCommands.Skip(initializationCommandCount).ToList();
        Assert.Single(operationCommands);
        Assert.Equal(0x87, operationCommands[0][1]); // GENERAL AUTHENTICATE
        Assert.DoesNotContain(operationCommands, command => command[1] == 0x20); // no VERIFY PIN
        Assert.DoesNotContain(operationCommands, command => command[1] is 0x2C or 0xDB or 0xFF); // no persistent mutation
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_NoPinOnlyDataPresent_ReturnsNoneWithoutAuthenticating()
    {
        // Neither PRINTED nor ADMIN DATA present -> Recover should short-circuit before ever
        // attempting management-key authentication (no GENERAL AUTHENTICATE APDU transmitted).
        var connection = CreateInitializedConnection([0x6A, 0x82], [0x6A, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var mode = await session.RecoverPinOnlyModeAsync("123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.None, mode);
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x87); // no GENERAL AUTHENTICATE
        Assert.DoesNotContain(connection.TransmittedCommands, c => c[1] == 0x20); // no VERIFY
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_WhenDerivedCandidateFails_RestoresProtectedAuthenticationBeforeReturningSuccess()
    {
        await using var connection = new MixedPinOnlyRecoveryConnection(restoreProtectedAuthentication: true);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var mode = await session.RecoverPinOnlyModeAsync("123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.PinProtected, mode);
        Assert.True(session.IsManagementKeyAuthenticated);
        Assert.Equal(3, connection.AuthenticationAttempts);
    }

    [Fact]
    public async Task RecoverPinOnlyModeAsync_WhenDerivedAndProtectedRestorationFail_ReturnsNoneWithUnauthenticatedSession()
    {
        await using var connection = new MixedPinOnlyRecoveryConnection(restoreProtectedAuthentication: false);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var mode = await session.RecoverPinOnlyModeAsync("123456"u8.ToArray(), TestContext.Current.CancellationToken);

        Assert.Equal(PivPinOnlyMode.None, mode);
        Assert.False(session.IsManagementKeyAuthenticated);
        Assert.Equal(3, connection.AuthenticationAttempts);
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ManagementKeyMetadataResponse(), .. trailingResponses]);

    private static void MarkAuthenticated(PivSession session) =>
        typeof(PivSession)
            .GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, true);

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] VersionResponse() => [0x00, 0x00, 0x01, 0x90, 0x00];

    private static byte[] ManagementKeyMetadataResponse() =>
    [
        0x01, 0x01, (byte)PivManagementKeyType.TripleDes,
        0x02, 0x02, 0x00, (byte)PivTouchPolicy.Default,
        0x05, 0x01, 0x01,
        0x90, 0x00
    ];

    private sealed class MixedPinOnlyRecoveryConnection(bool restoreProtectedAuthentication) : ISmartCardConnection
    {
        private readonly byte[] _managementKey =
        [
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37
        ];

        private static readonly byte[] Salt =
        [
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F
        ];

        private readonly bool _restoreProtectedAuthentication = restoreProtectedAuthentication;

        public int AuthenticationAttempts { get; private set; }

        public Transport Transport => Transport.Usb;

        public ConnectionType Type => ConnectionType.SmartCard;

        public List<byte[]> TransmittedCommands { get; } = [];

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] commandBytes = command.ToArray();
            TransmittedCommands.Add(commandBytes);

            byte[] response = commandBytes[1] switch
            {
                0xA4 => OkResponse(),
                0xFD => VersionResponse(),
                0xF7 => ManagementKeyMetadataResponse(),
                0xCB => GetDataResponse(commandBytes),
                0x20 => OkResponse(),
                0x87 => AuthenticateResponse(commandBytes),
                _ => throw new InvalidOperationException($"Unexpected PIV command INS 0x{commandBytes[1]:X2}.")
            };

            return Task.FromResult((ReadOnlyMemory<byte>)response);
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => NullDisposable.Instance;

        public bool SupportsExtendedApdu() => false;

        public void Dispose()
        {
            ClearSensitiveState();
        }

        public ValueTask DisposeAsync()
        {
            ClearSensitiveState();
            return default;
        }

        private byte[] GetDataResponse(byte[] command)
        {
            ReadOnlySpan<byte> commandData = command.AsSpan(5, command[4]);
            if (commandData.IndexOf(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x09 }) >= 0)
            {
                return [0x53, 0x1C, 0x88, 0x1A, 0x89, 0x18, .. _managementKey, 0x90, 0x00];
            }

            if (commandData.IndexOf(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x00 }) >= 0)
            {
                byte[] adminData = [0x80, 0x15, 0x81, 0x01, 0x02, 0x82, 0x10, .. Salt];
                return [0x53, (byte)adminData.Length, .. adminData, 0x90, 0x00];
            }

            throw new InvalidOperationException("Unexpected PIV data object request.");
        }

        private byte[] AuthenticateResponse(byte[] command)
        {
            ReadOnlySpan<byte> commandData = command.AsSpan(5, command[4]);
            if (commandData.SequenceEqual(new byte[] { 0x7C, 0x02, 0x80, 0x00 }))
            {
                AuthenticationAttempts++;
                bool succeeds = AuthenticationAttempts == 1 ||
                    (AuthenticationAttempts == 3 && _restoreProtectedAuthentication);
                if (!succeeds)
                {
                    return [0x69, 0x82];
                }

                ReadOnlySpan<byte> witness = [0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58];
                byte[] encryptedWitness = EncryptBlock(witness);
                return [0x7C, 0x0A, 0x80, 0x08, .. encryptedWitness, 0x90, 0x00];
            }

            ReadOnlySpan<byte> challenge = commandData.Slice(14, 8);
            byte[] encryptedChallenge = EncryptBlock(challenge);
            return [0x7C, 0x0A, 0x82, 0x08, .. encryptedChallenge, 0x90, 0x00];
        }

        private byte[] EncryptBlock(ReadOnlySpan<byte> input)
        {
            using var tripleDes = TripleDES.Create();
            tripleDes.Key = _managementKey;
            tripleDes.Mode = CipherMode.ECB;
            tripleDes.Padding = PaddingMode.None;
            using var encryptor = tripleDes.CreateEncryptor();

            byte[] inputBytes = input.ToArray();
            byte[] output = new byte[8];
            try
            {
                _ = encryptor.TransformBlock(inputBytes, 0, inputBytes.Length, output, 0);
                return output;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(inputBytes);
            }
        }

        private void ClearSensitiveState()
        {
            CryptographicOperations.ZeroMemory(_managementKey);
            foreach (byte[] command in TransmittedCommands)
            {
                CryptographicOperations.ZeroMemory(command);
            }
        }

        private sealed class NullDisposable : IDisposable
        {
            public static NullDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
