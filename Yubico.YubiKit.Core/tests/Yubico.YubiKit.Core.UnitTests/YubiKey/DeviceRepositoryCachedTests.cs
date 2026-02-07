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

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.YubiKey;

/// <summary>
/// Tests for <see cref="DeviceRepositoryCached"/> verifying composite device caching behavior.
/// </summary>
public class DeviceRepositoryCachedTests
{
    [Fact]
    public async Task FindAllAsync_ReturnsIYubiKey_NotIYubiKeyReference()
    {
        // Arrange
        var reference1 = new MockYubiKeyReference("pcsc:Test_Reader_1", ConnectionType.SmartCard);
        var reference2 = new MockYubiKeyReference("hid:vid_1050_pid_0407", ConnectionType.HidFido);

        var identity = CreateTestIdentity(12345678);
        var composite = new MockYubiKey("12345678", identity, [ConnectionType.SmartCard, ConnectionType.HidFido]);

        var findYubiKeys = new MockFindYubiKeys([reference1, reference2]);
        var compositeFactory = new MockCompositeYubiKeyFactory([composite]);

        var repo = CreateRepository(findYubiKeys, compositeFactory);

        // Act
        var result = await repo.FindAllAsync(ConnectionType.All);

        // Assert
        Assert.Single(result);
        Assert.IsAssignableFrom<IYubiKey>(result[0]);
        Assert.Equal("12345678", result[0].DeviceId);
    }

    [Fact]
    public async Task FindAllAsync_FiltersCompositesByConnectionType()
    {
        // Arrange
        var smartCardRef = new MockYubiKeyReference("pcsc:Test_Reader_1", ConnectionType.SmartCard);
        var fidoRef = new MockYubiKeyReference("hid:vid_1050_pid_0407", ConnectionType.HidFido);

        // Two composites - one with SmartCard, one with FIDO
        var smartCardComposite = new MockYubiKey("12345678", CreateTestIdentity(12345678), [ConnectionType.SmartCard]);
        var fidoComposite = new MockYubiKey("fido-only", CreateTestIdentity(null), [ConnectionType.HidFido]);

        var findYubiKeys = new MockFindYubiKeys([smartCardRef, fidoRef]);
        var compositeFactory = new MockCompositeYubiKeyFactory([smartCardComposite, fidoComposite]);

        var repo = CreateRepository(findYubiKeys, compositeFactory);

        // Act - filter to SmartCard only
        var result = await repo.FindAllAsync(ConnectionType.SmartCard);

        // Assert - should only return the SmartCard-capable composite
        Assert.Single(result);
        Assert.Equal("12345678", result[0].DeviceId);
    }

    [Fact]
    public async Task FindAllAsync_UsesCompositeFactory_WithIdentityReader()
    {
        // Arrange
        var reference = new MockYubiKeyReference("pcsc:Test_Reader_1", ConnectionType.SmartCard);
        var identity = CreateTestIdentity(12345678);
        var composite = new MockYubiKey("12345678", identity, [ConnectionType.SmartCard]);

        var findYubiKeys = new MockFindYubiKeys([reference]);
        var compositeFactory = new MockCompositeYubiKeyFactory([composite]);

        Func<IYubiKeyReference, CancellationToken, Task<IDeviceIdentity?>> identityReader =
            (_, _) => Task.FromResult<IDeviceIdentity?>(identity);

        var repo = CreateRepository(findYubiKeys, compositeFactory, identityReader);

        // Act
        await repo.FindAllAsync(ConnectionType.All);

        // Assert - factory was called
        Assert.Equal(1, compositeFactory.CallCount);
        Assert.Single(compositeFactory.LastReferences!);
        Assert.Equal("pcsc:Test_Reader_1", compositeFactory.LastReferences![0].DeviceId);
    }

    [Fact]
    public async Task FindAllAsync_CachesComposites_ByDeviceId()
    {
        // Arrange
        var reference = new MockYubiKeyReference("pcsc:Test_Reader_1", ConnectionType.SmartCard);
        var composite = new MockYubiKey("12345678", CreateTestIdentity(12345678), [ConnectionType.SmartCard]);

        var findYubiKeys = new MockFindYubiKeys([reference]);
        var compositeFactory = new MockCompositeYubiKeyFactory([composite]);

        var repo = CreateRepository(findYubiKeys, compositeFactory);

        // Act - first call
        var result1 = await repo.FindAllAsync(ConnectionType.All);

        // Act - second call (should use cache, not call factory again)
        var result2 = await repo.FindAllAsync(ConnectionType.All);

        // Assert - factory should only be called once (first time populates cache)
        Assert.Equal(1, compositeFactory.CallCount);
        Assert.Equal(result1.Count, result2.Count);
    }

    #region Test Helpers

    private static DeviceRepositoryCached CreateRepository(
        IFindYubiKeys findYubiKeys,
        ICompositeYubiKeyFactory compositeFactory,
        Func<IYubiKeyReference, CancellationToken, Task<IDeviceIdentity?>>? identityReader = null)
    {
        return new DeviceRepositoryCached(
            findYubiKeys,
            compositeFactory,
            identityReader ?? ((_, _) => Task.FromResult<IDeviceIdentity?>(null)));
    }

    private static TestDeviceIdentity CreateTestIdentity(int? serialNumber)
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

    private sealed class MockYubiKeyReference(string deviceId, ConnectionType connectionType) : IYubiKeyReference
    {
        public string DeviceId => deviceId;
        public ConnectionType ConnectionType => connectionType;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new NotSupportedException("Test mock does not support connections");
    }

    private sealed class MockYubiKey(
        string deviceId,
        IDeviceIdentity identity,
        IReadOnlyList<ConnectionType> availableConnections) : IYubiKey
    {
        public string DeviceId => deviceId;
        public IDeviceIdentity Identity => identity;
        public IReadOnlyList<ConnectionType> AvailableConnections => availableConnections;

        public bool SupportsConnection<TConnection>() where TConnection : class, IConnection =>
            false; // Not needed for these tests

        public bool SupportsConnection(ConnectionType connectionType) =>
            availableConnections.Contains(connectionType);

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new NotSupportedException("Test mock does not support connections");
    }

    private sealed class MockFindYubiKeys(IReadOnlyList<IYubiKeyReference> references) : IFindYubiKeys
    {
        public Task<IReadOnlyList<IYubiKeyReference>> FindAllAsync(
            ConnectionType connectionType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(references);
    }

    private sealed class MockCompositeYubiKeyFactory(IReadOnlyList<IYubiKey> composites) : ICompositeYubiKeyFactory
    {
        public int CallCount { get; private set; }
        public IReadOnlyList<IYubiKeyReference>? LastReferences { get; private set; }

        public Task<IReadOnlyList<IYubiKey>> CreateCompositesAsync(
            IReadOnlyList<IYubiKeyReference> references,
            Func<IYubiKeyReference, CancellationToken, Task<IDeviceIdentity>> identityReader,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastReferences = references;
            return Task.FromResult(composites);
        }
    }

    #endregion
}
