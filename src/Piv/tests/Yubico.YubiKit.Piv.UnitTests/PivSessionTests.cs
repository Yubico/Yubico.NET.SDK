// Copyright 2024 Yubico AB
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

using NSubstitute;
using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests;

public class PivSessionTests
{
    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = new RecordingSmartCardConnection();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // Borrowed: the session did not create this connection, so disposal is the caller's.
        // Upstream asserted 1 here because its protocols disposed the connection; this branch
        // deliberately removed that (see ProtocolConnectionOwnershipTests).
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task CreateAsync_WithValidConnection_ReturnsInitializedSession()
    {
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);

        // This will likely fail during actual PIV selection since it's a mock,
        // but it tests that the CreateAsync method exists and accepts the right parameters
        var exception = await Record.ExceptionAsync(() =>
            PivSession.CreateAsync(mockConnection, cancellationToken: TestContext.Current.CancellationToken));

        // We expect this to fail with an ApduException since the mock doesn't implement real protocol
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task CreateAsync_WithNullConnection_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => PivSession.CreateAsync((ISmartCardConnection)null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Constructor_WithValidConnection_CreatesSession()
    {
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);

        var session = new PivSession(mockConnection, null);

        Assert.NotNull(session);
        // Before initialization, session should not be initialized
        Assert.False(session.IsInitialized);
    }

    [Fact]
    public void ManagementKeyType_DefaultsToTripleDes()
    {
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);

        var session = new PivSession(mockConnection, null);

        // Default management key type should be 3DES
        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);

        var session = new PivSession(mockConnection, null);

        var exception = Record.Exception(() => session.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void DefaultManagementKey_Returns24ByteDefaultValue()
    {
        // Default PIV management key is 0x010203040506070801020304050607080102030405060708 (24 bytes)
        ReadOnlySpan<byte> expected = [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        ];

        ReadOnlySpan<byte> actual = PivSession.DefaultManagementKey;

        Assert.Equal(24, actual.Length);
        Assert.True(expected.SequenceEqual(actual));
    }

    [Fact]
    public async Task SignOrDecryptAsync_WithoutAlgorithm_OnOldFirmware_ThrowsNotSupportedException()
    {
        // Arrange: Create session with firmware < 5.3
        var mockConnection = Substitute.For<ISmartCardConnection>();
        mockConnection.Transport.Returns(Transport.Usb);

        var session = new PivSession(mockConnection, null);

        // Default FirmwareVersion is 0.0.0 which is treated as alpha/beta (latest).
        // Set an explicit old firmware version via the protected setter to simulate old hardware.
        typeof(PivSession).BaseType!
            .GetProperty(nameof(session.FirmwareVersion))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(session, [new FirmwareVersion(4, 0, 0)]);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => session.SignOrDecryptAsync(PivSlot.Authentication, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));

        Assert.Contains("5.3", exception.Message);
        Assert.Contains("firmware", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_TransmitsSelectVersionAndManagementMetadata()
    {
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            VersionResponse(),
            ManagementKeyMetadataResponse());

        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
        Assert.True(connection.TransmittedCommands.Count >= 3);
        Assert.Contains(connection.TransmittedCommands, command => command[1] == 0xA4); // SELECT
        Assert.Contains(connection.TransmittedCommands, command => command[1] == 0xFD); // GET VERSION
        Assert.Contains(connection.TransmittedCommands, command => command[1] == 0xF7 && command[3] == 0x9B); // Management metadata
    }

    [Theory]
    [InlineData(0x6A, 0x88)]
    [InlineData(0x6D, 0x00)]
    public async Task CreateAsync_WhenReliableFirmwareManagementMetadataUnavailable_UsesAes192Fallback(
        int statusHigh,
        int statusLow)
    {
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            [0x05, 0x07, 0x00, 0x90, 0x00],
            [(byte)statusHigh, (byte)statusLow]);

        await using var session = await PivSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PivManagementKeyType.Aes192, session.ManagementKeyType);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 9, 9)]
    public async Task CreateAsync_WhenSentinelFirmwareManagementMetadataUnavailable_UsesTripleDesFallback(
        int major,
        int minor,
        int patch)
    {
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            [(byte)major, (byte)minor, (byte)patch, 0x90, 0x00],
            [0x6A, 0x88]);

        await using var session = await PivSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
    }

    [Fact]
    public async Task GetPinMetadataAsync_TransmitsGetMetadataForPinSlot()
    {
        var connection = CreateInitializedConnection(PinMetadataResponse());
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var metadata = await session.GetPinMetadataAsync(TestContext.Current.CancellationToken);

        Assert.True(metadata.IsDefault);
        Assert.Contains(connection.TransmittedCommands, command => command[1] == 0xF7 && command[3] == 0x80);
    }

    [Fact]
    public async Task GetManagementKeyMetadataAsync_TransmitsGetMetadataForManagementKeySlot()
    {
        var connection = CreateInitializedConnection(ManagementKeyMetadataResponse());
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var metadata = await session.GetManagementKeyMetadataAsync(TestContext.Current.CancellationToken);

        Assert.Equal(PivManagementKeyType.TripleDes, metadata.KeyType);
        Assert.Contains(connection.TransmittedCommands, command => command[1] == 0xF7 && command[3] == 0x9B);
    }

    [Fact]
    public async Task GetSlotMetadataAsync_TransmitsGetMetadataForRequestedSlot()
    {
        var connection = CreateInitializedConnection([0x6A, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication, TestContext.Current.CancellationToken);

        Assert.Null(metadata);
        Assert.Contains(connection.TransmittedCommands, command => command[1] == 0xF7 && command[3] == (byte)PivSlot.Authentication);
    }

    [Fact]
    public async Task GetObjectAsync_TransmitsGetDataWithObjectIdTlv()
    {
        var connection = CreateInitializedConnection([0x53, 0x01, 0xAA, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var data = await session.GetObjectAsync(0x5FC105, TestContext.Current.CancellationToken);

        Assert.Equal([0xAA], data.ToArray());
        Assert.Contains(connection.TransmittedCommands, command =>
            command[1] == 0xCB &&
            command[2] == 0x3F &&
            command[3] == 0xFF &&
            command.AsSpan().IndexOf((byte)0x5C) >= 0);
    }

    [Fact]
    public async Task DecryptAsync_WithTouchPolicyAlways_NotifiesBeforePrivateKeyOperation()
    {
        var connection = CreateInitializedConnection(
            Rsa1024TouchAlwaysMetadataResponse(),
            Rsa1024TouchAlwaysMetadataResponse(),
            [0x7C, 0x02, 0x82, 0x00, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        var callbackCount = 0;
        session.OnTouchRequired = () => callbackCount++;

        var exception = await Record.ExceptionAsync(() => session.DecryptAsync(
            PivSlot.Authentication,
            new byte[128],
            RSAEncryptionPadding.Pkcs1,
            TestContext.Current.CancellationToken));

        Assert.NotNull(exception);
        Assert.Equal(1, callbackCount);
        Assert.Contains(connection.TransmittedCommands, command => command[1] == 0x87);
    }

    [Fact]
    public async Task GenerateKeyAsync_WithPolicies_TransmitsGenerateAsymmetricCommand()
    {
        var connection = CreateInitializedConnection(EccP256PublicKeyResponse());
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);

        _ = await session.GenerateKeyAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP256,
            PivPinPolicy.Once,
            PivTouchPolicy.Never,
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        // APDU header: INS=Generate Asymmetric, P1=0, P2=target slot.
        Assert.Equal(0x47, command[1]);
        Assert.Equal(0x00, command[2]);
        Assert.Equal((byte)PivSlot.Signature, command[3]);
        // Data: AC template containing algorithm(80), PIN policy(AA), and touch policy(AB) TLVs.
        Assert.Equal([
            0xAC, 0x09,
            0x80, 0x01, (byte)PivAlgorithm.EccP256,
            0xAA, 0x01, (byte)PivPinPolicy.Once,
            0xAB, 0x01, (byte)PivTouchPolicy.Never
        ], CommandData(command).ToArray());
    }

    [Fact]
    public async Task SignOrDecryptAsync_TransmitsAuthenticateTemplateWithChallenge()
    {
        // Response data: dynamic-auth template(7C) containing one-byte result in response tag(82), then SW 9000.
        var connection = CreateInitializedConnection([0x7C, 0x03, 0x82, 0x01, 0xAA, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        // ECC P-256 sign/decrypt input is 32 bytes; 0xCC is a sentinel proving the payload survives encoding.
        var data = new byte[32];
        data[31] = 0xCC;

        var result = await session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            data,
            TestContext.Current.CancellationToken);

        Assert.Equal([0xAA], result.ToArray());
        var command = LastCommand(connection);
        // APDU header: INS=GENERAL AUTHENTICATE, P1=algorithm, P2=target slot.
        Assert.Equal(0x87, command[1]);
        Assert.Equal((byte)PivAlgorithm.EccP256, command[2]);
        Assert.Equal((byte)PivSlot.Authentication, command[3]);
        var commandData = CommandData(command);
        // Short APDU data length: 7C template + empty 82 response tag + 32-byte 81 challenge.
        Assert.Equal(0x26, commandData.Length);
        // Data: dynamic-auth template(7C), expected response(82), challenge(81) with 32-byte P-256 input.
        AssertStartsWith(commandData, [0x7C, 0x24, 0x82, 0x00, 0x81, 0x20]);
        Assert.Equal(0xCC, commandData[^1]);
    }

    [Fact]
    public async Task SignOrDecryptAsync_WhenSecurityStatusNotSatisfied_ThrowsInvalidOperationException()
    {
        // SW 6982 is returned without response data when the key requires prior PIN verification.
        var connection = CreateInitializedConnection([0x69, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            new byte[32],
            TestContext.Current.CancellationToken));

        // SW 6982 is the PIV security-status-not-satisfied response.
        Assert.Contains("Security status", exception.Message);
        var command = LastCommand(connection);
        Assert.Equal(0x87, command[1]);
        Assert.Equal((byte)PivAlgorithm.EccP256, command[2]);
        Assert.Equal((byte)PivSlot.Authentication, command[3]);
    }

    [Fact]
    public async Task CalculateSecretAsync_TransmitsAuthenticateTemplateWithPeerPublicKey()
    {
        // Response data: dynamic-auth template(7C) containing 32-byte shared secret in response tag(82), then SW 9000.
        var connection = CreateInitializedConnection([0x7C, 0x22, 0x82, 0x20, .. new byte[32], 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        using var peer = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerPublicKey = ECPublicKey.CreateFromParameters(peer.PublicKey.ExportParameters());

        _ = await session.CalculateSecretAsync(
            PivSlot.KeyManagement,
            peerPublicKey,
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        // APDU header: INS=GENERAL AUTHENTICATE, P1=algorithm, P2=target slot.
        Assert.Equal(0x87, command[1]);
        Assert.Equal((byte)PivAlgorithm.EccP256, command[2]);
        Assert.Equal((byte)PivSlot.KeyManagement, command[3]);
        var commandData = CommandData(command);
        // Short APDU data length: 7C template + empty 82 response tag + 65-byte 85 public key.
        Assert.Equal(0x47, commandData.Length);
        // Data: dynamic-auth template(7C), expected response(82), peer public key(85) as 65-byte P-256 point.
        AssertStartsWith(commandData, [0x7C, 0x45, 0x82, 0x00, 0x85, 0x41]);
        Assert.Equal(peerPublicKey.PublicPoint.ToArray(), commandData[6..].ToArray());
    }

    [Fact]
    public async Task SetManagementKeyAsync_WhenSuccessful_UpdatesTypeAndPreservesAuthentication()
    {
        var connection = CreateInitializedConnection([0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        byte[] newKey = new byte[16];

        try
        {
            await session.SetManagementKeyAsync(
                PivManagementKeyType.Aes128,
                newKey,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(newKey);
        }

        Assert.Equal(PivManagementKeyType.Aes128, session.ManagementKeyType);
        Assert.True(session.IsAuthenticated);
        Assert.Equal(0xFF, LastCommand(connection)[1]);
    }

    [Fact]
    public async Task SetManagementKeyAsync_WhenNonAuthenticationCommandFailureOccurs_PreservesTypeAndAuthentication()
    {
        var connection = CreateInitializedConnection([0x6A, 0x80]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        byte[] newKey = new byte[16];

        try
        {
            await Assert.ThrowsAsync<ApduException>(() => session.SetManagementKeyAsync(
                PivManagementKeyType.Aes128,
                newKey,
                cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(newKey);
        }

        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
        Assert.True(session.IsAuthenticated);
    }

    [Fact]
    public async Task SetManagementKeyAsync_SecurityStatusNotSatisfied_ClearsAuthenticationAndPreservesType()
    {
        var connection = CreateInitializedConnection([0x69, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        byte[] newKey = new byte[16];

        try
        {
            var exception = await Assert.ThrowsAsync<ApduException>(() => session.SetManagementKeyAsync(
                PivManagementKeyType.Aes128,
                newKey,
                cancellationToken: TestContext.Current.CancellationToken));
            Assert.True(exception.SW == SWConstants.SecurityStatusNotSatisfied);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(newKey);
        }

        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenAttemptFails_ClearsPriorAuthentication()
    {
        var connection = CreateInitializedConnection([0x69, 0x82]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        byte[] managementKey = new byte[24];

        try
        {
            var exception = await Assert.ThrowsAsync<ApduException>(() => session.AuthenticateAsync(
                managementKey,
                TestContext.Current.CancellationToken));
            Assert.True(exception.SW == SWConstants.SecurityStatusNotSatisfied);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(managementKey);
        }

        Assert.False(session.IsAuthenticated);
        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
        Assert.Equal(0x87, LastCommand(connection)[1]);
    }

    [Fact]
    public async Task ResetAsync_BlocksPinAndPukThenResets()
    {
        // Regression test for the BlockPukAsync -> PivMetadataProtocol.BlockPukAsync extraction:
        // the APDU sequence (PIN metadata, VERIFY-until-blocked, RESET RETRY-until-blocked P2=0x80,
        // RESET, re-fetch management key metadata) must be unchanged.
        var connection = CreateInitializedConnection(
            [0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x01, 0x90, 0x00], // PIN metadata: 1 retry remaining
            [0x63, 0xC0], // VERIFY empty PIN -> blocked (0 retries)
            [0x63, 0xC0], // RESET RETRY empty PUK/PIN -> blocked (0 retries)
            [0x90, 0x00], // RESET
            ManagementKeyMetadataResponse());
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        await session.ResetAsync(TestContext.Current.CancellationToken);

        Assert.Contains(connection.TransmittedCommands, c => c[1] == 0x20 && c[3] == 0x80); // VERIFY PIN
        Assert.Contains(connection.TransmittedCommands, c => c[1] == 0x2C && c[3] == 0x80); // RESET RETRY, PUK-blocking quirk
        Assert.Contains(connection.TransmittedCommands, c => c[1] == 0xFB); // RESET
    }

    [Fact]
    public async Task ResetAsync_WhenSuccessful_RefreshesManagementKeyTypeAndClearsAuthentication()
    {
        var connection = CreateInitializedConnection(
            [0x90, 0x00], // SET MANAGEMENT KEY -> AES256
            [0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x01, 0x90, 0x00], // PIN metadata
            [0x63, 0xC0], // PIN blocked
            [0x63, 0xC0], // PUK blocked
            [0x90, 0x00], // RESET
            ManagementKeyMetadataResponse(PivManagementKeyType.Aes128)); // authoritative post-reset metadata
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        await SetManagementKeyForTestAsync(session, PivManagementKeyType.Aes256);

        await session.ResetAsync(TestContext.Current.CancellationToken);

        Assert.False(session.IsAuthenticated);
        Assert.Equal(PivManagementKeyType.Aes128, session.ManagementKeyType);
    }

    [Fact]
    public async Task ResetAsync_WhenMetadataUnsupported_UsesTripleDesFallbackAndClearsAuthentication()
    {
        var connection = CreateInitializedConnection(
            [0x90, 0x00], // SET MANAGEMENT KEY -> AES128
            [0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x01, 0x90, 0x00], // PIN metadata
            [0x63, 0xC0], // PIN blocked
            [0x63, 0xC0], // PUK blocked
            [0x90, 0x00], // RESET
            [0x6D, 0x00]); // GET METADATA unsupported
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        await SetManagementKeyForTestAsync(session, PivManagementKeyType.Aes128);

        await session.ResetAsync(TestContext.Current.CancellationToken);

        Assert.False(session.IsAuthenticated);
        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
    }

    [Fact]
    public async Task ResetAsync_WhenReliableFirmwareMetadataUnsupported_UsesAes192FallbackAndClearsAuthentication()
    {
        var connection = CreateInitializedConnectionWithVersion(
            [0x05, 0x07, 0x00, 0x90, 0x00],
            [0x90, 0x00], // SET MANAGEMENT KEY -> AES256
            [0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x01, 0x90, 0x00], // PIN metadata
            [0x63, 0xC0], // PIN blocked
            [0x63, 0xC0], // PUK blocked
            [0x90, 0x00], // RESET
            [0x6D, 0x00]); // GET METADATA unsupported
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        await SetManagementKeyForTestAsync(session, PivManagementKeyType.Aes256);

        await session.ResetAsync(TestContext.Current.CancellationToken);

        Assert.False(session.IsAuthenticated);
        Assert.Equal(PivManagementKeyType.Aes192, session.ManagementKeyType);
    }

    [Theory]
    [InlineData(0x6A80)]
    [InlineData(0x6A88)]
    public async Task ResetAsync_WhenMetadataRefreshUnexpectedlyFails_UsesReliableFirmwareFallbackBeforePropagating(
        int statusWord)
    {
        var connection = CreateInitializedConnectionWithVersion(
            [0x05, 0x07, 0x00, 0x90, 0x00],
            [0x90, 0x00], // SET MANAGEMENT KEY -> AES256
            [0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x01, 0x90, 0x00], // PIN metadata
            [0x63, 0xC0], // PIN blocked
            [0x63, 0xC0], // PUK blocked
            [0x90, 0x00], // RESET succeeded
            [(byte)(statusWord >> 8), (byte)statusWord]); // unexpected metadata refresh failure
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        await SetManagementKeyForTestAsync(session, PivManagementKeyType.Aes256);

        var exception = await Assert.ThrowsAsync<ApduException>(() =>
            session.ResetAsync(TestContext.Current.CancellationToken));

        Assert.True(exception.SW == statusWord);
        Assert.False(session.IsAuthenticated);
        Assert.Equal(PivManagementKeyType.Aes192, session.ManagementKeyType);
    }

    [Fact]
    public async Task ResetAsync_WhenMetadataRefreshUnexpectedlyFailsWithSentinelVersion_UsesTripleDesFallbackBeforePropagating()
    {
        var connection = CreateInitializedConnection(
            [0x90, 0x00], // SET MANAGEMENT KEY -> AES256
            [0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x01, 0x90, 0x00], // PIN metadata
            [0x63, 0xC0], // PIN blocked
            [0x63, 0xC0], // PUK blocked
            [0x90, 0x00], // RESET succeeded
            [0x6A, 0x80]); // unexpected metadata refresh failure
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        MarkAuthenticated(session);
        await SetManagementKeyForTestAsync(session, PivManagementKeyType.Aes256);

        await Assert.ThrowsAsync<ApduException>(() => session.ResetAsync(TestContext.Current.CancellationToken));

        Assert.False(session.IsAuthenticated);
        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
    }

    [Fact]
    public async Task ResetAsync_WhenPinBlockingReturnsUnexpectedStatus_ThrowsAndDoesNotSendReset()
    {
        var connection = CreateInitializedConnection(
            [0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x01, 0x90, 0x00], // PIN metadata: 1 retry remaining
            [0x6A, 0x80], // VERIFY empty PIN -> unexpected status
            [0x63, 0xC0], // Would block PUK if ResetAsync continued
            [0x90, 0x00], // Would reset PIV if ResetAsync continued
            ManagementKeyMetadataResponse());
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApduException>(() =>
            session.ResetAsync(TestContext.Current.CancellationToken));

        Assert.True(exception.SW == 0x6A80);
        Assert.DoesNotContain(connection.TransmittedCommands, command => command[1] == 0xFB);
    }

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ManagementKeyMetadataResponse(), .. trailingResponses]);

    private static RecordingSmartCardConnection CreateInitializedConnectionWithVersion(
        byte[] versionResponse,
        params byte[][] trailingResponses) =>
        new([OkResponse(), versionResponse, ManagementKeyMetadataResponse(), .. trailingResponses]);

    private static byte[] LastCommand(RecordingSmartCardConnection connection) =>
        connection.TransmittedCommands[^1];

    private static void MarkAuthenticated(PivSession session) =>
        typeof(PivSession)
            .GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, true);

    private static async Task SetManagementKeyForTestAsync(PivSession session, PivManagementKeyType keyType)
    {
        int keyLength = keyType switch
        {
            PivManagementKeyType.Aes128 => 16,
            PivManagementKeyType.Aes256 => 32,
            _ => 24
        };
        byte[] key = new byte[keyLength];
        try
        {
            await session.SetManagementKeyAsync(
                keyType,
                key,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static ReadOnlySpan<byte> CommandData(byte[] command) =>
        // Short APDU format: CLA INS P1 P2 Lc Data; the recorder reports SupportsExtendedApdu=false.
        command.AsSpan(5, command[4]);

    private static void AssertStartsWith(ReadOnlySpan<byte> actual, byte[] expectedPrefix) =>
        Assert.True(
            actual.Length >= expectedPrefix.Length && actual[..expectedPrefix.Length].SequenceEqual(expectedPrefix),
            $"Expected command data to start with {Convert.ToHexString(expectedPrefix)}.");

    // SW 9000: successful APDU response with no data.
    private static byte[] OkResponse() => [0x90, 0x00];

    // PIV version response: 0.0.1 followed by SW 9000.
    private static byte[] VersionResponse() => [0x00, 0x00, 0x01, 0x90, 0x00];

    // Metadata TLVs: key type(01), touch/default policy(02), generated/default flag(05), then SW 9000.
    private static byte[] ManagementKeyMetadataResponse(
        PivManagementKeyType keyType = PivManagementKeyType.TripleDes) =>
    [
        0x01, 0x01, (byte)keyType,
        0x02, 0x02, 0x00, (byte)PivTouchPolicy.Default,
        0x05, 0x01, 0x01,
        0x90, 0x00
    ];

    // PIN metadata TLVs: default flag(05) and retry counts(06), then SW 9000.
    private static byte[] PinMetadataResponse() => [0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x03, 0x90, 0x00];

    // Slot metadata TLVs: algorithm(01), PIN/touch policy(02), generated flag(03), then SW 9000.
    private static byte[] Rsa1024TouchAlwaysMetadataResponse() =>
    [
        0x01, 0x01, (byte)PivAlgorithm.Rsa1024,
        0x02, 0x02, (byte)PivPinPolicy.Default, (byte)PivTouchPolicy.Always,
        0x03, 0x01, 0x01,
        0x90, 0x00
    ];

    private static byte[] EccP256PublicKeyResponse()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(false);
        var x = parameters.Q.X!;
        var y = parameters.Q.Y!;

        return [
            // Public key response: 7F49 template, 86 public-point tag, uncompressed EC point, SW 9000.
            0x7F, 0x49, 0x43,
            0x86, 0x41,
            0x04,
            .. x,
            .. y,
            0x90, 0x00
        ];
    }

}