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
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.YubiKey;

/// <summary>
/// Tests for <see cref="CompositeYubiKeyFactory"/>.
/// </summary>
public class CompositeYubiKeyFactoryTests
{
    [Fact]
    public async Task CreateCompositesAsync_WithEmptyReferences_ReturnsEmptyList()
    {
        // Arrange
        var factory = new CompositeYubiKeyFactory();
        IReadOnlyList<IYubiKeyReference> references = [];

        // Act
        var composites = await factory.CreateCompositesAsync(
            references,
            (_, _) => Task.FromResult<IDeviceIdentity>(null!));

        // Assert
        Assert.Empty(composites);
    }

    [Fact]
    public async Task CreateCompositesAsync_WithSingleReference_CreatesSingleComposite()
    {
        // Arrange
        var factory = new CompositeYubiKeyFactory();
        var identity = CreateTestIdentity(serialNumber: 12345678);
        var reference = CreateMockReference(ConnectionType.SmartCard);

        // Act
        var composites = await factory.CreateCompositesAsync(
            [reference],
            (_, _) => Task.FromResult(identity));

        // Assert
        Assert.Single(composites);
        Assert.Equal("12345678", composites[0].DeviceId);
    }

    [Fact]
    public async Task CreateCompositesAsync_WithMatchingReferences_CreatesSingleComposite()
    {
        // Arrange
        var factory = new CompositeYubiKeyFactory();
        var identity = CreateTestIdentity(serialNumber: 12345678);
        var smartCardRef = CreateMockReference(ConnectionType.SmartCard);
        var fidoRef = CreateMockReference(ConnectionType.HidFido);

        // Act
        var composites = await factory.CreateCompositesAsync(
            [smartCardRef, fidoRef],
            (_, _) => Task.FromResult(identity));

        // Assert - same serial/identity should correlate to one composite
        Assert.Single(composites);
        Assert.Contains(ConnectionType.SmartCard, composites[0].AvailableConnections);
        Assert.Contains(ConnectionType.HidFido, composites[0].AvailableConnections);
    }

    [Fact]
    public async Task CreateCompositesAsync_WithDifferentSerials_CreatesMultipleComposites()
    {
        // Arrange
        var factory = new CompositeYubiKeyFactory();
        var identity1 = CreateTestIdentity(serialNumber: 12345678);
        var identity2 = CreateTestIdentity(serialNumber: 87654321);
        var ref1 = CreateMockReference(ConnectionType.SmartCard);
        var ref2 = CreateMockReference(ConnectionType.HidFido);

        // Return different identities for different references
        var identityMap = new Dictionary<IYubiKeyReference, IDeviceIdentity>
        {
            [ref1] = identity1,
            [ref2] = identity2
        };

        // Act
        var composites = await factory.CreateCompositesAsync(
            [ref1, ref2],
            (r, _) => Task.FromResult(identityMap[r]));

        // Assert - different serials should not correlate
        Assert.Equal(2, composites.Count);
    }

    [Fact]
    public async Task CreateCompositesAsync_WithFailedIdentityRead_CreatesUncorrelatableComposite()
    {
        // Arrange
        var factory = new CompositeYubiKeyFactory();
        var reference = CreateMockReference(ConnectionType.SmartCard, "failed-device");

        // Act
        var composites = await factory.CreateCompositesAsync(
            [reference],
            (_, _) => throw new InvalidOperationException("Identity read failed"));

        // Assert - should still create a composite (uncorrelatable)
        Assert.Single(composites);
        Assert.StartsWith("fp:", composites[0].DeviceId);
    }

    [Fact]
    public async Task CreateCompositesAsync_WithNullSerial_CorrelatesByFingerprint()
    {
        // Arrange
        var factory = new CompositeYubiKeyFactory();
        // Same config = same fingerprint
        var identity = CreateTestIdentity(serialNumber: null);
        var smartCardRef = CreateMockReference(ConnectionType.SmartCard);
        var fidoRef = CreateMockReference(ConnectionType.HidFido);

        // Act
        var composites = await factory.CreateCompositesAsync(
            [smartCardRef, fidoRef],
            (_, _) => Task.FromResult(identity));

        // Assert - same fingerprint should correlate
        Assert.Single(composites);
        Assert.StartsWith("fp:", composites[0].DeviceId);
    }

    #region Test Helpers

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

    private static MockYubiKeyReference CreateMockReference(ConnectionType type, string? deviceId = null)
    {
        return new MockYubiKeyReference(type, deviceId);
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

    private sealed class MockYubiKeyReference(ConnectionType type, string? deviceId = null) : IYubiKeyReference
    {
        public string DeviceId => deviceId ?? $"mock-{type}";
        public ConnectionType ConnectionType => type;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            throw new NotSupportedException("Mock does not support connection");
        }
    }

    #endregion
}
