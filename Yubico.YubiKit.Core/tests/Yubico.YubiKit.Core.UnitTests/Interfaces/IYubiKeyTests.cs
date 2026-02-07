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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.Interfaces;

/// <summary>
/// Tests for <see cref="IYubiKey"/> interface contract.
/// </summary>
public class IYubiKeyTests
{
    [Fact]
    public void IYubiKey_HasDeviceIdProperty()
    {
        // Arrange
        var yubiKey = CreateTestYubiKey(deviceId: "12345678");

        // Act & Assert
        Assert.Equal("12345678", yubiKey.DeviceId);
    }

    [Fact]
    public void IYubiKey_HasIdentityProperty()
    {
        // Arrange
        var identity = CreateTestIdentity(serialNumber: 12345678);
        var yubiKey = CreateTestYubiKey(identity: identity);

        // Act & Assert
        Assert.NotNull(yubiKey.Identity);
        Assert.Equal(12345678, yubiKey.Identity.SerialNumber);
    }

    [Fact]
    public void IYubiKey_HasAvailableConnectionsProperty()
    {
        // Arrange
        var connections = new[] { ConnectionType.SmartCard, ConnectionType.HidFido };
        var yubiKey = CreateTestYubiKey(availableConnections: connections);

        // Act
        var available = yubiKey.AvailableConnections;

        // Assert
        Assert.Contains(ConnectionType.SmartCard, available);
        Assert.Contains(ConnectionType.HidFido, available);
    }

    [Fact]
    public void SupportsConnection_WhenAvailable_ReturnsTrue()
    {
        // Arrange
        var yubiKey = CreateTestYubiKey(
            availableConnections: [ConnectionType.SmartCard]);

        // Act & Assert
        Assert.True(yubiKey.SupportsConnection<ISmartCardConnection>());
    }

    [Fact]
    public void SupportsConnection_WhenNotAvailable_ReturnsFalse()
    {
        // Arrange - only SmartCard, no HidFido
        var yubiKey = CreateTestYubiKey(
            availableConnections: [ConnectionType.SmartCard]);

        // Act & Assert - asking for a connection type not available
        Assert.False(yubiKey.SupportsConnection<IConnection>());
    }

    [Fact]
    public async Task ConnectAsync_ReturnsConnection()
    {
        // Arrange
        var yubiKey = CreateTestYubiKey(
            availableConnections: [ConnectionType.SmartCard]);

        // Act
        var connection = await yubiKey.ConnectAsync<ISmartCardConnection>();

        // Assert
        Assert.NotNull(connection);
    }

    /// <summary>
    /// Creates a test IYubiKey implementation.
    /// </summary>
    private static IYubiKey CreateTestYubiKey(
        string? deviceId = null,
        IDeviceIdentity? identity = null,
        IReadOnlyList<ConnectionType>? availableConnections = null)
    {
        return new TestYubiKey
        {
            DeviceId = deviceId ?? "test-device",
            Identity = identity ?? CreateTestIdentity(),
            AvailableConnections = availableConnections ?? [ConnectionType.SmartCard]
        };
    }

    private static IDeviceIdentity CreateTestIdentity(
        int? serialNumber = 12345678)
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

    private sealed class TestYubiKey : IYubiKey
    {
        public required string DeviceId { get; init; }
        public required IDeviceIdentity Identity { get; init; }
        public required IReadOnlyList<ConnectionType> AvailableConnections { get; init; }

        public bool SupportsConnection<TConnection>() where TConnection : class, IConnection
        {
            // Simple implementation - only supports SmartCardConnection
            return typeof(TConnection) == typeof(ISmartCardConnection) &&
                   AvailableConnections.Contains(ConnectionType.SmartCard);
        }

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            // Return a mock connection for testing
            return Task.FromResult((TConnection)(object)new MockSmartCardConnection());
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
}
