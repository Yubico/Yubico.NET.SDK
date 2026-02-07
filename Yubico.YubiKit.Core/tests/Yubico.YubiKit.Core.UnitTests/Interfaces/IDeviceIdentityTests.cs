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

namespace Yubico.YubiKit.Core.UnitTests.Interfaces;

/// <summary>
/// Tests for <see cref="IDeviceIdentity"/> interface contract.
/// </summary>
public class IDeviceIdentityTests
{
    [Fact]
    public void IDeviceIdentity_HasRequiredProperties()
    {
        // Arrange & Act - verify interface contract exists with expected properties
        var identity = CreateTestIdentity(
            serialNumber: 12345678,
            firmwareVersion: new FirmwareVersion(5, 4, 3),
            formFactor: FormFactor.UsbCKeychain,
            usbSupported: DeviceCapabilities.All,
            nfcSupported: DeviceCapabilities.Piv | DeviceCapabilities.Oath);

        // Assert - verify all properties are accessible
        Assert.Equal(12345678, identity.SerialNumber);
        Assert.Equal(new FirmwareVersion(5, 4, 3), identity.FirmwareVersion);
        Assert.Equal(FormFactor.UsbCKeychain, identity.FormFactor);
        Assert.Equal(DeviceCapabilities.All, identity.UsbSupported);
        Assert.Equal(DeviceCapabilities.Piv | DeviceCapabilities.Oath, identity.NfcSupported);
    }

    [Fact]
    public void IDeviceIdentity_SerialNumber_CanBeNull()
    {
        // Security Keys without serial number should return null
        var identity = CreateTestIdentity(serialNumber: null);

        Assert.Null(identity.SerialNumber);
    }

    [Fact]
    public void IDeviceIdentity_SupportedCapabilities_ReturnsUnionOfUsbAndNfc()
    {
        // Arrange
        var identity = CreateTestIdentity(
            usbSupported: DeviceCapabilities.Piv | DeviceCapabilities.Fido2,
            nfcSupported: DeviceCapabilities.Oath | DeviceCapabilities.OpenPgp);

        // Act
        var supported = identity.SupportedCapabilities;

        // Assert - should be union of USB and NFC capabilities
        Assert.Equal(
            DeviceCapabilities.Piv | DeviceCapabilities.Fido2 | DeviceCapabilities.Oath | DeviceCapabilities.OpenPgp,
            supported);
    }

    [Fact]
    public void IDeviceIdentity_ConfigFingerprint_Properties_AreAccessible()
    {
        // Properties used for configuration fingerprint should be accessible
        var identity = CreateTestIdentity(
            usbEnabled: DeviceCapabilities.Piv,
            nfcEnabled: DeviceCapabilities.Oath,
            autoEjectTimeout: 30,
            challengeResponseTimeout: [0x0F],
            deviceFlags: DeviceFlags.TouchEject,
            isNfcRestricted: true);

        Assert.Equal(DeviceCapabilities.Piv, identity.UsbEnabled);
        Assert.Equal(DeviceCapabilities.Oath, identity.NfcEnabled);
        Assert.Equal((ushort)30, identity.AutoEjectTimeout);
        Assert.Equal<byte>([0x0F], identity.ChallengeResponseTimeout.ToArray());
        Assert.Equal(DeviceFlags.TouchEject, identity.DeviceFlags);
        Assert.True(identity.IsNfcRestricted);
    }

    [Fact]
    public void ComputeConfigFingerprint_ReturnsDeterministicHash()
    {
        // Arrange - two identities with same config
        var identity1 = CreateTestIdentity(
            usbEnabled: DeviceCapabilities.Piv,
            nfcEnabled: DeviceCapabilities.Oath,
            autoEjectTimeout: 30,
            challengeResponseTimeout: [0x0F],
            deviceFlags: DeviceFlags.TouchEject,
            isNfcRestricted: true);

        var identity2 = CreateTestIdentity(
            usbEnabled: DeviceCapabilities.Piv,
            nfcEnabled: DeviceCapabilities.Oath,
            autoEjectTimeout: 30,
            challengeResponseTimeout: [0x0F],
            deviceFlags: DeviceFlags.TouchEject,
            isNfcRestricted: true);

        // Act
        var fingerprint1 = identity1.ComputeConfigFingerprint();
        var fingerprint2 = identity2.ComputeConfigFingerprint();

        // Assert - same config produces same fingerprint
        Assert.Equal(fingerprint1, fingerprint2);
        Assert.NotEmpty(fingerprint1);
    }

    [Fact]
    public void ComputeConfigFingerprint_DifferentConfig_ReturnsDifferentHash()
    {
        // Arrange - two identities with different config
        var identity1 = CreateTestIdentity(
            usbEnabled: DeviceCapabilities.Piv,
            nfcEnabled: DeviceCapabilities.Oath);

        var identity2 = CreateTestIdentity(
            usbEnabled: DeviceCapabilities.Fido2,  // Different
            nfcEnabled: DeviceCapabilities.Oath);

        // Act
        var fingerprint1 = identity1.ComputeConfigFingerprint();
        var fingerprint2 = identity2.ComputeConfigFingerprint();

        // Assert - different config produces different fingerprint
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public void ComputeConfigFingerprint_ReturnsHexString()
    {
        // Arrange
        var identity = CreateTestIdentity();

        // Act
        var fingerprint = identity.ComputeConfigFingerprint();

        // Assert - should be hex string (SHA256 truncated to 8 chars)
        Assert.Matches("^[0-9a-f]{8}$", fingerprint);
    }

    /// <summary>
    /// Creates a test implementation of IDeviceIdentity for testing the interface contract.
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

    /// <summary>
    /// Test implementation of IDeviceIdentity for verifying interface contract.
    /// </summary>
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
