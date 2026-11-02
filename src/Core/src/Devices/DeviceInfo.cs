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
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Devices;

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

    /// <summary>Decodes device-information TLVs, ignoring unknown tags.</summary>
    /// <remarks>
    ///     Absent optional fields use zero or null defaults. A supplied firmware version takes precedence
    ///     over tag 0x05, but a present firmware field must still have a valid encoding.
    /// </remarks>
    /// <exception cref="BadResponseException">A known field is malformed or no firmware version is available.</exception>
    public static DeviceInfo CreateFromTlvs(IReadOnlyCollection<Tlv> tlvs, FirmwareVersion? defaultVersion)
    {
        var tlvDict = tlvs.ToDictionary(tlv => tlv.Tag, tlv => tlv.Value);

        var snBytes = ReadFixedField(tlvDict, TAG_SERIAL_NUMBER, 4);
        int? serialNumber = snBytes.IsEmpty ? null : BinaryPrimitives.ReadInt32BigEndian(snBytes.Span);

        var isLocked = ReadByte(tlvDict, TAG_CONFIG_LOCKED) == 1;
        int formFactorTagData = ReadByte(tlvDict, TAG_FORMFACTOR);
        var isFips = (formFactorTagData & 0x80) != 0;
        var isSky = (formFactorTagData & 0x40) != 0;
        var formFactor = (FormFactor)formFactorTagData;

        var resetBlocked = ReadCapabilities(tlvDict, TAG_RESET_BLOCKED);
        var usbEnabled = ReadCapabilities(tlvDict, TAG_USB_ENABLED);
        var usbSupported = ReadCapabilities(tlvDict, TAG_USB_SUPPORTED);
        var nfcEnabled = ReadCapabilities(tlvDict, TAG_NFC_ENABLED);
        var nfcSupported = ReadCapabilities(tlvDict, TAG_NFC_SUPPORTED);
        var fipsCapabilities = ReadCapabilities(tlvDict, TAG_FIPS_CAPABLE);
        var fipsApprovedCapabilities = ReadCapabilities(tlvDict, TAG_FIPS_APPROVED);

        var isNfcRestricted = ReadByte(tlvDict, TAG_NFC_RESTRICTED) == 1;
        var hasPinComplexity = ReadByte(tlvDict, TAG_PIN_COMPLEXITY) == 1;

        string? partNumber = null;
        if (tlvDict.TryGetSpan(TAG_PART_NUMBER, out var partNumberBytes))
            partNumber = GetPartNumber(partNumberBytes);

        var autoEjectBytes = ReadFixedField(tlvDict, TAG_AUTO_EJECT_TIMEOUT, 2);
        var autoEjectTimeout = autoEjectBytes.IsEmpty ? (ushort)0 : BinaryPrimitives.ReadUInt16BigEndian(autoEjectBytes.Span);
        var challengeResponseTimeout = ReadFixedField(tlvDict, TAG_CHALLENGE_RESPONSE_TIMEOUT, 1);
        var deviceFlags = (DeviceFlags)ReadByte(tlvDict, TAG_DEVICE_FLAGS);

        var fpsVersion = ReadComponentVersion(tlvDict, TAG_FPS_VERSION);
        var stmVersion = ReadComponentVersion(tlvDict, TAG_STM_VERSION);

        var reportedVersion = ReadFirmwareVersion(tlvDict, TAG_FIRMWARE_VERSION);
        defaultVersion ??= reportedVersion ?? throw new BadResponseException($"Missing device info tag 0x{TAG_FIRMWARE_VERSION:X2}.");
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

    // Fixed widths follow the YubiKey configuration reference (serial/version/timeout/scalars).
    // Absence is not a malformed value: older firmware and non-NFC devices omit optional metadata.
    private static ReadOnlyMemory<byte> ReadFixedField(
        IDictionary<int, ReadOnlyMemory<byte>> data, int tag, int expectedLength)
    {
        if (!data.TryGetValue(tag, out var value))
            return default;
        if (value.Length != expectedLength)
            throw new BadResponseException($"Invalid device info tag 0x{tag:X2} length {value.Length}; expected {expectedLength}.");
        return value;
    }

    private static byte ReadByte(IDictionary<int, ReadOnlyMemory<byte>> data, int tag)
    {
        var value = ReadFixedField(data, tag, 1);
        return value.IsEmpty ? (byte)0 : value.Span[0];
    }

    private static FirmwareVersion? ReadFirmwareVersion(IDictionary<int, ReadOnlyMemory<byte>> data, int tag)
    {
        var value = ReadFixedField(data, tag, 3);
        return value.IsEmpty ? null : FirmwareVersion.FromBytes(value.Span);
    }

    private static FirmwareVersion? ReadComponentVersion(IDictionary<int, ReadOnlyMemory<byte>> data, int tag)
    {
        var version = ReadFirmwareVersion(data, tag);
        return version is { Major: 0, Minor: 0, Patch: 0 } ? null : version;
    }

    private static DeviceCapabilities ReadCapabilities(IDictionary<int, ReadOnlyMemory<byte>> data, int tag)
    {
        if (!data.TryGetValue(tag, out var value))
            return DeviceCapabilities.None;
        // Siblings accept variable-width capability integers; do not impose a two-byte maximum.
        if (value.IsEmpty)
            throw new BadResponseException($"Invalid device info tag 0x{tag:X2} length 0; expected a capability value.");
        return CapabilityMapper.FromApp(value);
    }

    private static (FirmwareVersion, VersionQualifier) DetermineFirmwareVersion(
        Dictionary<int, ReadOnlyMemory<byte>> dict,
        FirmwareVersion defaultFirmwareVersion)
    {
        if (!dict.TryGetValue(TAG_VERSION_QUALIFIER, out var versionQualifierBytes))
            return (defaultFirmwareVersion,
                new VersionQualifier(defaultFirmwareVersion, VersionQualifierType.Final, 0));

        const byte tagVersion = 0x01;
        const byte tagType = 0x02;
        const byte tagIteration = 0x03;

        IDictionary<int, ReadOnlyMemory<byte>> data;
        try
        {
            // The container is extensible. Validate its children, not the original 14-byte total.
            data = TlvHelper.DecodeDictionary(versionQualifierBytes.Span);
        }
        catch (ArgumentException)
        {
            throw new BadResponseException($"Invalid device info tag 0x{TAG_VERSION_QUALIFIER:X2} length {versionQualifierBytes.Length}; malformed qualifier TLVs.");
        }

        if (versionQualifierBytes.IsEmpty)
            throw new BadResponseException($"Invalid device info tag 0x{TAG_VERSION_QUALIFIER:X2} length 0; expected qualifier TLVs.");
        if (!data.ContainsKey(tagVersion))
            throw new BadResponseException($"Missing version qualifier tag 0x{tagVersion:X2}.");
        if (!data.ContainsKey(tagType))
            throw new BadResponseException($"Missing version qualifier tag 0x{tagType:X2}.");
        if (!data.TryGetValue(tagIteration, out var iterationBytes))
            throw new BadResponseException($"Missing version qualifier tag 0x{tagIteration:X2}.");

        var qualifierVersion = ReadFirmwareVersion(data, tagVersion)
            ?? throw new BadResponseException($"Missing version qualifier tag 0x{tagVersion:X2}.");
        var versionType = (VersionQualifierType)ReadByte(data, tagType);
        long iteration = ReadQualifierIteration(iterationBytes.Span, tagIteration);

        var versionQualifier = new VersionQualifier(
            qualifierVersion,
            versionType,
            iteration);

        var isFinalVersion = versionQualifier.Type == VersionQualifierType.Final;
        var finalVersion = isFinalVersion
            ? defaultFirmwareVersion
            : versionQualifier.FirmwareVersion;

        return (finalVersion, versionQualifier);
    }

    private static uint ReadQualifierIteration(ReadOnlySpan<byte> value, int tag)
    {
        if (value.IsEmpty)
            throw new BadResponseException($"Invalid version qualifier tag 0x{tag:X2} length 0; expected an iteration value.");

        // Python uses an integer, Rust fixtures also encode this in one byte. Accept leading-zero
        // extensions while retaining VersionQualifier's existing unsigned 32-bit numeric range.
        uint iteration = 0;
        foreach (var b in value)
        {
            if (iteration > (uint.MaxValue >> 8))
                throw new BadResponseException($"Invalid version qualifier tag 0x{tag:X2} length {value.Length}; iteration exceeds the supported numeric range.");
            iteration = (iteration << 8) | b;
        }
        return iteration;
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

internal static class CapabilityMapper
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