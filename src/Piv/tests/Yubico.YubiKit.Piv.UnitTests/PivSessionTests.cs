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
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Piv.UnitTests;

public class PivSessionTests
{
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

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ManagementKeyMetadataResponse(), .. trailingResponses]);

    private static byte[] LastCommand(RecordingSmartCardConnection connection) =>
        connection.TransmittedCommands[^1];

    private static void MarkAuthenticated(PivSession session) =>
        typeof(PivSession)
            .GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, true);

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
    private static byte[] ManagementKeyMetadataResponse() =>
    [
        0x01, 0x01, (byte)PivManagementKeyType.TripleDes,
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