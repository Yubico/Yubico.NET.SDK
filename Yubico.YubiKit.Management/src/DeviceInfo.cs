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

using System.Buffers.Binary;
using System.Text;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management;

public readonly record struct DeviceInfo
{
    private const int TAG_USB_SUPPORTED = 0x01;
    private const int TAG_SERIAL_NUMBER = 0x02;
    private const int TAG_USB_ENABLED = 0x03;
    private const int TAG_FORMFACTOR = 0x04;
    private const int TAG_FIRMWARE_VERSION = 0x05;
    private const int TAG_AUTO_EJECT_TIMEOUT = 0x06;
    private const int TAG_CHALLENGE_RESPONSE_TIMEOUT = 0x07;
    private const int TAG_DEVICE_FLAGS = 0x08;
    private const int TAG_NFC_SUPPORTED = 0x0d;
    private const int TAG_NFC_ENABLED = 0x0e;
    private const int TAG_CONFIG_LOCKED = 0x0a;
    private const int TAG_PART_NUMBER = 0x13;
    private const int TAG_FIPS_CAPABLE = 0x14;
    private const int TAG_FIPS_APPROVED = 0x15;
    private const int TAG_PIN_COMPLEXITY = 0x16;
    private const int TAG_NFC_RESTRICTED = 0x17;
    private const int TAG_RESET_BLOCKED = 0x18;
    private const int TAG_VERSION_QUALIFIER = 0x19;
    private const int TAG_FPS_VERSION = 0x20;
    private const int TAG_STM_VERSION = 0x21;

    public required bool IsSky { get; init; }
    public required bool IsFips { get; init; }
    public required FormFactor FormFactor { get; init; }
    public int? SerialNumber { get; init; }
    public required bool IsLocked { get; init; }
    public required DeviceCapabilities UsbEnabled { get; init; }
    public required DeviceCapabilities UsbSupported { get; init; }
    public required DeviceCapabilities NfcEnabled { get; init; }
    public required DeviceCapabilities NfcSupported { get; init; }
    public required DeviceCapabilities ResetBlocked { get; init; }
    public required DeviceCapabilities FipsCapabilities { get; init; }
    public required DeviceCapabilities FipsApproved { get; init; }
    public required bool HasPinComplexity { get; init; }
    public required string? PartNumber { get; init; }
    public required bool IsNfcRestricted { get; init; }
    public required ushort AutoEjectTimeout { get; init; }
    public required ReadOnlyMemory<byte> ChallengeResponseTimeout { get; init; }
    public required DeviceFlags DeviceFlags { get; init; }
    public required FirmwareVersion FirmwareVersion { get; init; }
    public required VersionQualifier VersionQualifier { get; init; }
    public FirmwareVersion? FpsVersion { get; init; }
    public FirmwareVersion? StmVersion { get; init; }

    public string VersionName => VersionQualifier.Type == VersionQualifierType.Final
        ? FirmwareVersion.ToString()
        : VersionQualifier.ToString();

    public static DeviceInfo CreateFromTlvs(IReadOnlyCollection<Tlv> tlvs, FirmwareVersion? defaultVersion)
    {
        var tlvDict = tlvs.ToDictionary(tlv => tlv.Tag, tlv => tlv.Value);

        int? serialNumber = null;
        if (tlvDict.TryGetValue(TAG_SERIAL_NUMBER, out var snBytes))
            serialNumber = BinaryPrimitives.ReadInt32BigEndian(snBytes.Span);

        var isLocked = tlvDict.GetSpan(TAG_CONFIG_LOCKED)[0] == 1;
        int formFactorTagData = tlvDict.GetSpan(TAG_FORMFACTOR)[0];
        var isFips = (formFactorTagData & 0x80) != 0;
        var isSky = (formFactorTagData & 0x40) != 0;
        var formFactor = (FormFactor)formFactorTagData;

        var resetBlocked = CapabilityMapper.FromApp(tlvDict.GetMemory(TAG_RESET_BLOCKED));
        var usbEnabled = CapabilityMapper.FromApp(tlvDict.GetMemory(TAG_USB_ENABLED));
        var usbSupported = CapabilityMapper.FromApp(tlvDict.GetMemory(TAG_USB_SUPPORTED));
        var nfcEnabled = CapabilityMapper.FromApp(tlvDict.GetMemory(TAG_NFC_ENABLED));
        var nfcSupported = CapabilityMapper.FromApp(tlvDict.GetMemory(TAG_NFC_SUPPORTED));
        var fipsCapabilities = CapabilityMapper.FromApp(tlvDict.GetMemory(TAG_FIPS_CAPABLE));
        var fipsApprovedCapabilities = CapabilityMapper.FromApp(tlvDict.GetMemory(TAG_FIPS_APPROVED));

        var isNfcRestricted = tlvDict.TryGetValue(TAG_NFC_RESTRICTED, out var nfcRestrictedBytes) &&
                              nfcRestrictedBytes.Span[0] == 1;
        var hasPinComplexity = tlvDict.TryGetValue(TAG_PIN_COMPLEXITY, out var hasPinComplexityBytes) &&
                               hasPinComplexityBytes.Span[0] == 1;

        string? partNumber = null;
        if (tlvDict.TryGetSpan(TAG_PART_NUMBER, out var partNumberBytes))
            partNumber = GetPartNumber(partNumberBytes);

        var autoEjectTimeout = BinaryPrimitives.ReadUInt16BigEndian(tlvDict.GetSpan(TAG_AUTO_EJECT_TIMEOUT));
        var challengeResponseTimeout = tlvDict.GetMemory(TAG_CHALLENGE_RESPONSE_TIMEOUT);
        var deviceFlags = (DeviceFlags)tlvDict.GetSpan(TAG_DEVICE_FLAGS)[0];

        FirmwareVersion? fpsVersion = null;
        if (tlvDict.TryGetSpan(TAG_FPS_VERSION, out var fpsVersionBytes) &&
            !fpsVersionBytes.SequenceEqual(new byte[3]))
        {
            var fpsVersionMajor = fpsVersionBytes[0];
            var fpsVersionMinor = fpsVersionBytes[1];
            var fpsVersionMPatch = fpsVersionBytes[2];
            fpsVersion = new FirmwareVersion(fpsVersionMajor, fpsVersionMinor, fpsVersionMPatch);
        }

        FirmwareVersion? stmVersion = null;
        if (tlvDict.TryGetSpan(TAG_STM_VERSION, out var stmVersionBytes) &&
            !stmVersionBytes.SequenceEqual(new byte[3]))
        {
            var stmVersionMajor = stmVersionBytes[0];
            var stmVersionMinor = stmVersionBytes[1];
            var stmVersionMPatch = stmVersionBytes[2];
            stmVersion = new FirmwareVersion(stmVersionMajor, stmVersionMinor, stmVersionMPatch);
        }

        var firmwareVersionMajor = tlvDict.GetSpan(TAG_FIRMWARE_VERSION)[0];
        var firmwareVersionMinor = tlvDict.GetSpan(TAG_FIRMWARE_VERSION)[1];
        var firmwareVersionMPatch = tlvDict.GetSpan(TAG_FIRMWARE_VERSION)[2];
        defaultVersion ??= new FirmwareVersion(firmwareVersionMajor, firmwareVersionMinor, firmwareVersionMPatch);
        var (firmwareVersion, versionQualifier) = DetermineFirmwareVersion(tlvDict, defaultVersion);

        return new DeviceInfo
        {
            IsLocked = isLocked,
            SerialNumber = serialNumber,
            FormFactor = formFactor,
            IsFips = isFips,
            IsSky = isSky,
            ResetBlocked = resetBlocked,
            UsbEnabled = usbEnabled,
            UsbSupported = usbSupported,
            NfcEnabled = nfcEnabled,
            NfcSupported = nfcSupported,
            FipsCapabilities = fipsCapabilities,
            FipsApproved = fipsApprovedCapabilities,
            HasPinComplexity = hasPinComplexity,
            PartNumber = partNumber,
            IsNfcRestricted = isNfcRestricted,
            AutoEjectTimeout = autoEjectTimeout,
            ChallengeResponseTimeout = challengeResponseTimeout,
            DeviceFlags = deviceFlags,
            FirmwareVersion = firmwareVersion,
            VersionQualifier = versionQualifier,
            FpsVersion = fpsVersion,
            StmVersion = stmVersion
        };
    }

    private static (FirmwareVersion, VersionQualifier) DetermineFirmwareVersion(
        Dictionary<int, ReadOnlyMemory<byte>> dict,
        FirmwareVersion defaultFirmwareVersion)
    {
        if (!dict.TryGetValue(TAG_VERSION_QUALIFIER, out var versionQualifierBytes))
            return (defaultFirmwareVersion,
                new VersionQualifier(defaultFirmwareVersion, VersionQualifierType.Final, 0));

        if (versionQualifierBytes.Length != 0x0E) throw new ArgumentException("Invalid data length.");

        const byte tagVersion = 0x01;
        const byte tagType = 0x02;
        const byte tagIteration = 0x03;

        var data = TlvHelper.DecodeDictionary(versionQualifierBytes.Span);
        if (!data.TryGetSpan(tagVersion, out var firmwareVersionBytes))
            throw new ArgumentException("Missing TLV field: TAG_VERSION.");
        if (!data.TryGetSpan(tagType, out var versionTypeBytes))
            throw new ArgumentException("Missing TLV field: TAG_TYPE.");
        if (!data.TryGetSpan(tagIteration, out var iterationBytes))
            throw new ArgumentException("Missing TLV field: TAG_ITERATION.");

        var qualifierVersion = FirmwareVersion.FromBytes(firmwareVersionBytes);
        var versionType = (VersionQualifierType)versionTypeBytes[0];
        long iteration = BinaryPrimitives.ReadUInt32BigEndian(iterationBytes);

        var versionQualifier = new VersionQualifier(
            qualifierVersion,
            versionType,
            iteration);

        var isFinalVersion = versionQualifier.Type == VersionQualifierType.Final;
        if (!isFinalVersion)
        {
            // TODO
            // var logger = Log.GetLogger<YubiKeyDeviceInfo>();
            // logger.LogDebug("Overriding behavioral version with {FirmwareString}",
            //     deviceInfo.VersionQualifier.FirmwareVersion);
        }

        var finalVersion = isFinalVersion
            ? defaultFirmwareVersion
            : versionQualifier.FirmwareVersion;

        return (finalVersion, versionQualifier);
    }

    private static string? GetPartNumber(ReadOnlySpan<byte> valueSpan)
    {
        if (valueSpan.Length == 0) return null;
        try
        {
            // .NET defaults to decode without error detection, this is to detect an error in the decoding when
            // invalid bytes are found and allows us to return null, similar to the other Yubikey SDK's
            var encoding = new UTF8Encoding(false, true);
            return encoding.GetString(valueSpan);
        }
        catch (DecoderFallbackException)
        {
            // Handle similar to other SDK's by setting the unparseable part number to null
            return null;
        }
    }
}

public static class CapabilityMapper // TODO internal
{
    private static readonly (int Bit, DeviceCapabilities Cap)[] FipsMapping =
    [
        (0x01, DeviceCapabilities.Fido2),
        (0x02, DeviceCapabilities.Piv),
        (0x04, DeviceCapabilities.OpenPgp),
        (0x08, DeviceCapabilities.Oath),
        (0x10, DeviceCapabilities.HsmAuth)
    ];

    public static DeviceCapabilities FromFips(ReadOnlyMemory<byte> value)
    {
        if (value.IsEmpty) return 0;
        if (value.Length == 1 && value.Span[0] == 0) return DeviceCapabilities.None;

        int fips = BinaryPrimitives.ReadInt16BigEndian(value.Span);
        DeviceCapabilities capabilities = 0;

        foreach (var (bit, cap) in FipsMapping)
            if ((fips & bit) != 0)
                capabilities |= cap;

        return capabilities;
    }

    public static DeviceCapabilities FromApp(ReadOnlyMemory<byte> appData)
    {
        if (appData.IsEmpty) return DeviceCapabilities.None;

        return appData.Length == 1
            ? (DeviceCapabilities)appData.Span[0]
            : (DeviceCapabilities)BinaryPrimitives.ReadInt16BigEndian(appData.Span);
    }
}