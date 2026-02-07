// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.YubiKey;

/// <summary>
/// Tests for <see cref="CompositeYubiKey"/> class.
/// </summary>
public class CompositeYubiKeyTests
{
    [Fact]
    public void DeviceId_WithSerialNumber_ReturnsSerialAsString()
    {
        // Arrange
        var identity = CreateTestIdentity(serialNumber: 12345678);
        var composite = CreateCompositeYubiKey(identity);

        // Act
        var deviceId = composite.DeviceId;

        // Assert
        Assert.Equal("12345678", deviceId);
    }

    [Fact]
    public void DeviceId_WithoutSerialNumber_ReturnsFingerprintFormat()
    {
        // Arrange
        var identity = CreateTestIdentity(serialNumber: null);
        var composite = CreateCompositeYubiKey(identity);

        // Act
        var deviceId = composite.DeviceId;

        // Assert - should be "fp:" prefix with 8-char hex
        Assert.StartsWith("fp:", deviceId);
        Assert.Matches(@"^fp:[0-9a-f]{8}$", deviceId);
    }

    [Fact]
    public void Identity_ReturnsProvidedIdentity()
    {
        // Arrange
        var identity = CreateTestIdentity(serialNumber: 12345678);
        var composite = CreateCompositeYubiKey(identity);

        // Act & Assert
        Assert.Same(identity, composite.Identity);
    }

    [Fact]
    public void AvailableConnections_ReturnsRegisteredConnectionTypes()
    {
        // Arrange
        var identity = CreateTestIdentity();
        var smartCardRef = CreateMockReference(ConnectionType.SmartCard);
        var fidoRef = CreateMockReference(ConnectionType.HidFido);

        var composite = CreateCompositeYubiKey(
            identity,
            references: [smartCardRef, fidoRef]);

        // Act
        var connections = composite.AvailableConnections;

        // Assert
        Assert.Contains(ConnectionType.SmartCard, connections);
        Assert.Contains(ConnectionType.HidFido, connections);
        Assert.Equal(2, connections.Count);
    }

    [Fact]
    public void SupportsConnection_ForSmartCard_WhenAvailable_ReturnsTrue()
    {
        // Arrange
        var smartCardRef = CreateMockReference(ConnectionType.SmartCard);
        var composite = CreateCompositeYubiKey(
            CreateTestIdentity(),
            references: [smartCardRef]);

        // Act & Assert
        Assert.True(composite.SupportsConnection<ISmartCardConnection>());
    }

    [Fact]
    public void SupportsConnection_ForSmartCard_WhenNotAvailable_ReturnsFalse()
    {
        // Arrange - only FIDO available
        var fidoRef = CreateMockReference(ConnectionType.HidFido);
        var composite = CreateCompositeYubiKey(
            CreateTestIdentity(),
            references: [fidoRef]);

        // Act & Assert
        Assert.False(composite.SupportsConnection<ISmartCardConnection>());
    }

    [Fact]
    public void SupportsConnection_ForFido_WhenAvailable_ReturnsTrue()
    {
        // Arrange
        var fidoRef = CreateMockReference(ConnectionType.HidFido);
        var composite = CreateCompositeYubiKey(
            CreateTestIdentity(),
            references: [fidoRef]);

        // Act & Assert
        Assert.True(composite.SupportsConnection<IFidoHidConnection>());
    }

    [Fact]
    public async Task ConnectAsync_ForSmartCard_RoutesToCorrectReference()
    {
        // Arrange
        var mockConnection = new MockSmartCardConnection();
        var smartCardRef = CreateMockReference(ConnectionType.SmartCard, mockConnection);
        var composite = CreateCompositeYubiKey(
            CreateTestIdentity(),
            references: [smartCardRef]);

        // Act
        var connection = await composite.ConnectAsync<ISmartCardConnection>();

        // Assert
        Assert.Same(mockConnection, connection);
    }

    [Fact]
    public async Task ConnectAsync_ForUnsupportedType_ThrowsNotSupportedException()
    {
        // Arrange - only FIDO available
        var fidoRef = CreateMockReference(ConnectionType.HidFido);
        var composite = CreateCompositeYubiKey(
            CreateTestIdentity(),
            references: [fidoRef]);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => composite.ConnectAsync<ISmartCardConnection>());
    }

    #region Test Helpers

    private static CompositeYubiKey CreateCompositeYubiKey(
        IDeviceIdentity identity,
        IReadOnlyList<IYubiKeyReference>? references = null)
    {
        references ??= [CreateMockReference(ConnectionType.SmartCard)];
        return new CompositeYubiKey(identity, references);
    }

    private static IDeviceIdentity CreateTestIdentity(int? serialNumber = 12345678)
    {
        return new TestDeviceIdentity
        {
            SerialNumber = serialNumber,
            FirmwareVersion = new FirmwareVersion(5, 4, 3),
            FormFactor = FormFactor.UsbAKeychain,
            UsbSupported = DeviceCapabilities.All,
            NfcSupported = DeviceCapabilities.None,
            UsbEnabled = DeviceCapabilities.All,
            NfcEnabled = DeviceCapabilities.None,
            AutoEjectTimeout = 0,
            ChallengeResponseTimeout = ReadOnlyMemory<byte>.Empty,
            DeviceFlags = DeviceFlags.None,
            IsNfcRestricted = false
        };
    }

    private static MockYubiKeyReference CreateMockReference(
        ConnectionType type,
        IConnection? connectionToReturn = null)
    {
        return new MockYubiKeyReference(type, connectionToReturn);
    }

    private sealed class TestDeviceIdentity : IDeviceIdentity
    {
        public required int? SerialNumber { get; init; }
        public required FirmwareVersion FirmwareVersion { get; init; }
        public required FormFactor FormFactor { get; init; }
        public required DeviceCapabilities UsbSupported { get; init; }
        public required DeviceCapabilities NfcSupported { get; init; }
        public required DeviceCapabilities UsbEnabled { get; init; }
        public required DeviceCapabilities NfcEnabled { get; init; }
        public required ushort AutoEjectTimeout { get; init; }
        public required ReadOnlyMemory<byte> ChallengeResponseTimeout { get; init; }
        public required DeviceFlags DeviceFlags { get; init; }
        public required bool IsNfcRestricted { get; init; }
    }

    private sealed class MockYubiKeyReference(ConnectionType type, IConnection? connection = null) : IYubiKeyReference
    {
        public string DeviceId => $"mock-{type}";
        public ConnectionType ConnectionType => type;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            if (connection is TConnection typed)
            {
                return Task.FromResult(typed);
            }
            throw new NotSupportedException($"Connection type {typeof(TConnection)} not supported");
        }
    }

    private sealed class MockSmartCardConnection : ISmartCardConnection
    {
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;
        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
            => Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 0x90, 0x00 });
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => new NoOpDisposable();
        public bool SupportsExtendedApdu() => true;
        private sealed class NoOpDisposable : IDisposable { public void Dispose() { } }
    }

    #endregion
}
