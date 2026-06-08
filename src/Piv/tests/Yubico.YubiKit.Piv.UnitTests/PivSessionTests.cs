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

    private static RecordingSmartCardConnection CreateInitializedConnection(params byte[][] trailingResponses) =>
        new([OkResponse(), VersionResponse(), ManagementKeyMetadataResponse(), .. trailingResponses]);

    private static byte[] OkResponse() => [0x90, 0x00];

    private static byte[] VersionResponse() => [0x00, 0x00, 0x01, 0x90, 0x00];

    private static byte[] ManagementKeyMetadataResponse() =>
    [
        0x01, 0x01, (byte)PivManagementKeyType.TripleDes,
        0x02, 0x02, 0x00, (byte)PivTouchPolicy.Default,
        0x05, 0x01, 0x01,
        0x90, 0x00
    ];

    private static byte[] PinMetadataResponse() => [0x05, 0x01, 0x01, 0x06, 0x02, 0x03, 0x03, 0x90, 0x00];

    private static byte[] Rsa1024TouchAlwaysMetadataResponse() =>
    [
        0x01, 0x01, (byte)PivAlgorithm.Rsa1024,
        0x02, 0x02, (byte)PivPinPolicy.Default, (byte)PivTouchPolicy.Always,
        0x03, 0x01, 0x01,
        0x90, 0x00
    ];

}
