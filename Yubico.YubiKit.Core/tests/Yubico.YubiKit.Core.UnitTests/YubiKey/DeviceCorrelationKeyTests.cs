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
/// Tests for <see cref="DeviceCorrelationKey"/> record struct.
/// </summary>
public class DeviceCorrelationKeyTests
{
    [Fact]
    public void From_WithSerialNumber_IncludesSerialInKey()
    {
        // Arrange
        var identity = CreateTestIdentity(serialNumber: 12345678);

        // Act
        var key = DeviceCorrelationKey.From(identity);

        // Assert - key should include serial number
        Assert.Equal(12345678, key.SerialNumber);
        Assert.NotNull(key.SerialNumber);
    }

    [Fact]
    public void From_WithoutSerialNumber_UsesNullSerial()
    {
        // Arrange - Security Key without serial
        var identity = CreateTestIdentity(serialNumber: null);

        // Act
        var key = DeviceCorrelationKey.From(identity);

        // Assert
        Assert.Null(key.SerialNumber);
    }

    [Fact]
    public void From_IncludesVersionAndFormFactor()
    {
        // Arrange
        var identity = CreateTestIdentity(
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            formFactor: FormFactor.UsbCKeychain);

        // Act
        var key = DeviceCorrelationKey.From(identity);

        // Assert
        Assert.Equal(new FirmwareVersion(5, 4, 3), key.FirmwareVersion);
        Assert.Equal(FormFactor.UsbCKeychain, key.FormFactor);
    }

    [Fact]
    public void From_IncludesSupportedCapabilities()
    {
        // Arrange
        var identity = CreateTestIdentity(
            usbSupported: DeviceCapabilities.Piv | DeviceCapabilities.Fido2,
            nfcSupported: DeviceCapabilities.Oath);

        // Act
        var key = DeviceCorrelationKey.From(identity);

        // Assert - should be union of USB and NFC
        Assert.Equal(
            DeviceCapabilities.Piv | DeviceCapabilities.Fido2 | DeviceCapabilities.Oath,
            key.SupportedCapabilities);
    }

    [Fact]
    public void From_IncludesConfigFingerprint()
    {
        // Arrange
        var identity = CreateTestIdentity(
            usbEnabled: DeviceCapabilities.Piv,
            nfcEnabled: DeviceCapabilities.Oath);

        // Act
        var key = DeviceCorrelationKey.From(identity);

        // Assert - fingerprint should match
        Assert.Equal(identity.ComputeConfigFingerprint(), key.ConfigFingerprint);
    }

    [Fact]
    public void Equality_SameIdentity_ProducesSameKey()
    {
        // Arrange
        var identity1 = CreateTestIdentity(serialNumber: 12345678);
        var identity2 = CreateTestIdentity(serialNumber: 12345678);

        // Act
        var key1 = DeviceCorrelationKey.From(identity1);
        var key2 = DeviceCorrelationKey.From(identity2);

        // Assert - record struct equality
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void Equality_DifferentSerial_ProducesDifferentKey()
    {
        // Arrange
        var identity1 = CreateTestIdentity(serialNumber: 12345678);
        var identity2 = CreateTestIdentity(serialNumber: 87654321);

        // Act
        var key1 = DeviceCorrelationKey.From(identity1);
        var key2 = DeviceCorrelationKey.From(identity2);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void Equality_DifferentVersion_ProducesDifferentKey()
    {
        // Arrange
        var identity1 = CreateTestIdentity(firmwareVersion: new FirmwareVersion(5, 4, 3));
        var identity2 = CreateTestIdentity(firmwareVersion: new FirmwareVersion(5, 7, 0));

        // Act
        var key1 = DeviceCorrelationKey.From(identity1);
        var key2 = DeviceCorrelationKey.From(identity2);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GetHashCode_SameKey_ProducesSameHash()
    {
        // Arrange
        var identity1 = CreateTestIdentity(serialNumber: 12345678);
        var identity2 = CreateTestIdentity(serialNumber: 12345678);

        // Act
        var key1 = DeviceCorrelationKey.From(identity1);
        var key2 = DeviceCorrelationKey.From(identity2);

        // Assert - record struct generates GetHashCode
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    /// Creates a test implementation of IDeviceIdentity.
    /// </summary>
    private static IDeviceIdentity CreateTestIdentity(
        int? serialNumber = 12345678,
        FirmwareVersion? firmwareVersion = null,
        FormFactor formFactor = FormFactor.UsbAKeychain,
        DeviceCapabilities usbSupported = DeviceCapabilities.All,
        DeviceCapabilities nfcSupported = DeviceCapabilities.None,
        DeviceCapabilities usbEnabled = DeviceCapabilities.All,
        DeviceCapabilities nfcEnabled = DeviceCapabilities.None,
        ushort autoEjectTimeout = 0,
        byte[]? challengeResponseTimeout = null,
        DeviceFlags deviceFlags = DeviceFlags.None,
        bool isNfcRestricted = false)
    {
        return new TestDeviceIdentity
        {
            SerialNumber = serialNumber,
            FirmwareVersion = firmwareVersion ?? new FirmwareVersion(5, 4, 3),
            FormFactor = formFactor,
            UsbSupported = usbSupported,
            NfcSupported = nfcSupported,
            UsbEnabled = usbEnabled,
            NfcEnabled = nfcEnabled,
            AutoEjectTimeout = autoEjectTimeout,
            ChallengeResponseTimeout = challengeResponseTimeout ?? [],
            DeviceFlags = deviceFlags,
            IsNfcRestricted = isNfcRestricted
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
}
