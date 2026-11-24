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

using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Text;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>Device metadata observed by discovery or read through a device-information protocol.</summary>
/// <remarks>
///     Supported applications describe device capability; enabled applications describe configuration on each
///     transport. These values describe an observation, not current presence or operation success. Decoding
///     supplies defaults for absent optional fields, so false, zero, or null can mean unreported, depending
///     on the field.
/// </remarks>
public readonly record struct DeviceInfo
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<DeviceInfo>();

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

    /// <summary>Whether the reported form-factor byte has the Security Key product flag (0x40).</summary>
    public required bool IsSky { get; init; }
    /// <summary>Whether the reported form-factor byte has the FIPS product flag (0x80).</summary>
    /// <remarks>This manufacturing flag is separate from application approval or operation-specific FIPS requirements.</remarks>
    public required bool IsFips { get; init; }
    /// <summary>The low nibble of the form-factor byte, preserving unnamed base values.</summary>
    public required FormFactor FormFactor { get; init; }
    /// <summary>The complete form-factor byte, including known and unknown product flags.</summary>
    public byte RawFormFactor { get; init; }
    /// <summary>The serial decoded from four big-endian bytes, or null when the response omits it.</summary>
    public int? SerialNumber { get; init; }
    /// <summary>Whether the configuration-lock field equals 1; false when absent.</summary>
    public required bool IsLocked { get; init; }
    /// <summary>The applications reported enabled over USB; <see cref="DeviceCapabilities.None"/> when absent.</summary>
    public required DeviceCapabilities UsbEnabled { get; init; }
    /// <summary>The applications reported supported over USB; <see cref="DeviceCapabilities.None"/> when absent.</summary>
    public required DeviceCapabilities UsbSupported { get; init; }
    /// <summary>The applications reported enabled over NFC; <see cref="DeviceCapabilities.None"/> when absent.</summary>
    public required DeviceCapabilities NfcEnabled { get; init; }
    /// <summary>The applications reported supported over NFC; <see cref="DeviceCapabilities.None"/> when absent.</summary>
    public required DeviceCapabilities NfcSupported { get; init; }
    /// <summary>The decoded reset-blocked application field; <see cref="DeviceCapabilities.None"/> when absent.</summary>
    public required DeviceCapabilities ResetBlocked { get; init; }
    /// <summary>The decoded FIPS-capable application field; <see cref="DeviceCapabilities.None"/> when absent.</summary>
    /// <remarks>Interpretation depends on the response encoding; this value alone establishes no profile conformance.</remarks>
    public required DeviceCapabilities FipsCapabilities { get; init; }
    /// <summary>The decoded FIPS-approved application field; <see cref="DeviceCapabilities.None"/> when absent.</summary>
    /// <remarks>Apply the application's device, configuration, and operation requirements separately.</remarks>
    public required DeviceCapabilities FipsApproved { get; init; }
    /// <summary>Whether the PIN-complexity field equals 1; false when absent.</summary>
    public required bool HasPinComplexity { get; init; }
    /// <summary>The UTF-8 part number, or null when absent, empty, or invalid UTF-8.</summary>
    public required string? PartNumber { get; init; }
    /// <summary>Whether the NFC-restriction field equals 1; false when absent.</summary>
    public required bool IsNfcRestricted { get; init; }
    /// <summary>The CCID inactivity timeout in seconds, decoded from two big-endian bytes; zero when absent.</summary>
    /// <remarks>The timeout is meaningful with <see cref="DeviceFlags.TouchEject"/> enabled.</remarks>
    public required ushort AutoEjectTimeout { get; init; }
    /// <summary>The reported OTP challenge-response timeout in seconds, or null when absent.</summary>
    /// <remarks>A reported byte is preserved as-is, including zero; omission does not imply the hardware default.</remarks>
    public required byte? ChallengeResponseTimeout { get; init; }
    /// <summary>The reported device-wide configuration flags, or <see cref="DeviceFlags.None"/> when absent.</summary>
    public required DeviceFlags DeviceFlags { get; init; }
    /// <summary>The effective firmware version used for compatibility checks.</summary>
    /// <remarks>A non-final qualifier supplies this version; otherwise the supplied default or reported firmware is used.</remarks>
    public required FirmwareVersion FirmwareVersion { get; init; }
    /// <summary>The decoded release qualifier, or a final qualifier with iteration zero for the effective default version.</summary>
    public required VersionQualifier VersionQualifier { get; init; }
    /// <summary>The reported FPS component version, or null when absent or 0.0.0.</summary>
    public FirmwareVersion? FpsVersion { get; init; }
    /// <summary>The reported STM component version, or null when absent or 0.0.0.</summary>
    public FirmwareVersion? StmVersion { get; init; }

    /// <summary>The firmware's three-part version for a final release, otherwise the qualifier's formatted version.</summary>
    public string VersionName => VersionQualifier.Type == VersionQualifierType.Final
        ? FirmwareVersion.ToString()
        : VersionQualifier.ToString();

    /// <summary>Compares every field by value, including the reported timeout and its absence.</summary>
    /// <remarks>
    ///     Discovery relies on this comparison to publish <see cref="DeviceUpdated"/> only when metadata actually changed.
    /// </remarks>
    public bool Equals(DeviceInfo other) =>
        IsSky == other.IsSky &&
        IsFips == other.IsFips &&
        FormFactor == other.FormFactor &&
        RawFormFactor == other.RawFormFactor &&
        SerialNumber == other.SerialNumber &&
        IsLocked == other.IsLocked &&
        UsbEnabled == other.UsbEnabled &&
        UsbSupported == other.UsbSupported &&
        NfcEnabled == other.NfcEnabled &&
        NfcSupported == other.NfcSupported &&
        ResetBlocked == other.ResetBlocked &&
        FipsCapabilities == other.FipsCapabilities &&
        FipsApproved == other.FipsApproved &&
        HasPinComplexity == other.HasPinComplexity &&
        PartNumber == other.PartNumber &&
        IsNfcRestricted == other.IsNfcRestricted &&
        AutoEjectTimeout == other.AutoEjectTimeout &&
        ChallengeResponseTimeout == other.ChallengeResponseTimeout &&
        DeviceFlags == other.DeviceFlags &&
        Equals(FirmwareVersion, other.FirmwareVersion) &&
        Equals(VersionQualifier, other.VersionQualifier) &&
        Equals(FpsVersion, other.FpsVersion) &&
        Equals(StmVersion, other.StmVersion);

    /// <summary>Hashes every field, including <see cref="ChallengeResponseTimeout"/>.</summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IsSky);
        hash.Add(IsFips);
        hash.Add(FormFactor);
        hash.Add(RawFormFactor);
        hash.Add(SerialNumber);
        hash.Add(IsLocked);
        hash.Add(UsbEnabled);
        hash.Add(UsbSupported);
        hash.Add(NfcEnabled);
        hash.Add(NfcSupported);
        hash.Add(ResetBlocked);
        hash.Add(FipsCapabilities);
        hash.Add(FipsApproved);
        hash.Add(HasPinComplexity);
        hash.Add(PartNumber);
        hash.Add(IsNfcRestricted);
        hash.Add(AutoEjectTimeout);
        hash.Add(ChallengeResponseTimeout);
        hash.Add(DeviceFlags);
        hash.Add(FirmwareVersion);
        hash.Add(VersionQualifier);
        hash.Add(FpsVersion);
        hash.Add(StmVersion);
        return hash.ToHashCode();
    }

    /// <summary>Decodes device-information TLVs, ignoring unknown tags.</summary>
    /// <remarks>
    ///     Absent optional fields use false, zero, or null defaults. A supplied firmware version takes precedence
    ///     over tag 0x05, but a present firmware field must still have a valid encoding.
    ///     A qualifier whose type differs from <see cref="VersionQualifierType.Final"/> supplies the effective
    ///     firmware version, including an unnamed qualifier type. An omitted challenge-response timeout is null.
    ///     Ordinary capability masks are unsigned big-endian integers. Leading zero bytes are accepted and
    ///     unnamed bits are preserved up to 32 bits; values outside this SDK range are rejected.
    /// </remarks>
    /// <param name="tlvs">Decoded device-information fields. Unknown tags are ignored, even when repeated.</param>
    /// <param name="defaultVersion">Version used in preference to tag 0x05, or null to use that tag.</param>
    /// <returns>The decoded metadata.</returns>
    /// <exception cref="ArgumentException">The collection repeats a known top-level device-information tag.</exception>
    /// <exception cref="BadResponseException">A known field is malformed or no firmware version is available.</exception>
    public static DeviceInfo CreateFromTlvs(IReadOnlyCollection<Tlv> tlvs, FirmwareVersion? defaultVersion)
    {
        ArgumentNullException.ThrowIfNull(tlvs);

        var tlvDict = IndexKnownFields(tlvs);

        var snBytes = ReadFixedField(tlvDict, TAG_SERIAL_NUMBER, 4);
        int? serialNumber = snBytes.IsEmpty ? null : BinaryPrimitives.ReadInt32BigEndian(snBytes.Span);

        var isLocked = ReadByte(tlvDict, TAG_CONFIG_LOCKED) == 1;
        int formFactorTagData = ReadByte(tlvDict, TAG_FORMFACTOR);
        var isFips = (formFactorTagData & 0x80) != 0;
        var isSky = (formFactorTagData & 0x40) != 0;
        var formFactor = (FormFactor)(formFactorTagData & 0x0F);

        var resetBlocked = ReadCapabilities(tlvDict, TAG_RESET_BLOCKED);
        var usbEnabled = ReadCapabilities(tlvDict, TAG_USB_ENABLED);
        var usbSupported = ReadCapabilities(tlvDict, TAG_USB_SUPPORTED);
        var nfcEnabled = ReadCapabilities(tlvDict, TAG_NFC_ENABLED);
        var nfcSupported = ReadCapabilities(tlvDict, TAG_NFC_SUPPORTED);
        var fipsCapabilities = ReadFipsCapabilities(tlvDict, TAG_FIPS_CAPABLE);
        var fipsApprovedCapabilities = ReadFipsCapabilities(tlvDict, TAG_FIPS_APPROVED);

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
            RawFormFactor = (byte)formFactorTagData,
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
            ChallengeResponseTimeout = challengeResponseTimeout.IsEmpty ? null : challengeResponseTimeout.Span[0],
            DeviceFlags = deviceFlags,
            FirmwareVersion = firmwareVersion,
            VersionQualifier = versionQualifier,
            FpsVersion = fpsVersion,
            StmVersion = stmVersion
        };
    }

    private static Dictionary<int, ReadOnlyMemory<byte>> IndexKnownFields(IReadOnlyCollection<Tlv> tlvs)
    {
        var fields = new Dictionary<int, ReadOnlyMemory<byte>>();
        foreach (var tlv in tlvs)
        {
            if (!IsKnownDeviceInfoTag(tlv.Tag))
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("Ignoring unknown device-info tag 0x{Tag:X2} ({Length} bytes).", tlv.Tag, tlv.Length);
                continue;
            }

            if (!fields.TryAdd(tlv.Tag, tlv.Value))
                throw new ArgumentException($"Duplicate device info tag 0x{tlv.Tag:X2}.", nameof(tlvs));
        }
        return fields;
    }

    // Keep this list in sync with the top-level fields consumed by the decoder.
    private static bool IsKnownDeviceInfoTag(int tag) => tag is
        TAG_USB_SUPPORTED or TAG_SERIAL_NUMBER or TAG_USB_ENABLED or TAG_FORMFACTOR or
        TAG_FIRMWARE_VERSION or TAG_AUTO_EJECT_TIMEOUT or TAG_CHALLENGE_RESPONSE_TIMEOUT or TAG_DEVICE_FLAGS or
        TAG_NFC_SUPPORTED or TAG_NFC_ENABLED or TAG_CONFIG_LOCKED or TAG_PART_NUMBER or
        TAG_FIPS_CAPABLE or TAG_FIPS_APPROVED or TAG_PIN_COMPLEXITY or TAG_NFC_RESTRICTED or
        TAG_RESET_BLOCKED or TAG_VERSION_QUALIFIER or TAG_FPS_VERSION or TAG_STM_VERSION;

    // Fixed widths follow the YubiKey configuration reference (serial/version/timeout/scalars).
    // Absence is not a malformed value: older firmware and non-NFC devices omit optional metadata.
    private static ReadOnlyMemory<byte> ReadFixedField(
        IDictionary<int, ReadOnlyMemory<byte>> data, int tag, int expectedLength)
    {
        if (!data.TryGetValue(tag, out var value))
            return default;
        return RequireFieldLength(value, tag, expectedLength);
    }

    private static ReadOnlyMemory<byte> RequireFieldLength(ReadOnlyMemory<byte> value, int tag, int expectedLength)
    {
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
        if (value.IsEmpty)
            throw new BadResponseException($"Invalid device info tag 0x{tag:X2} length 0; expected a capability value.");
        if (!CapabilityMapper.TryFromApp(value.Span, out var capabilities))
            throw new BadResponseException($"Device info tag 0x{tag:X2} exceeds the SDK's 32-bit capability range.");
        return capabilities;
    }

    private static DeviceCapabilities ReadFipsCapabilities(IDictionary<int, ReadOnlyMemory<byte>> data, int tag)
    {
        if (!data.TryGetValue(tag, out var value))
            return DeviceCapabilities.None;
        if (value.Length is not (1 or 2))
            throw new BadResponseException($"Invalid device info tag 0x{tag:X2} length {value.Length}; expected 1 or 2 bytes.");
        return CapabilityMapper.FromFips(value);
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

        IDictionary<int, Memory<byte>> data;
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
        if (!data.TryGetValue(tagVersion, out var versionBytes))
            throw new BadResponseException($"Missing version qualifier tag 0x{tagVersion:X2}.");
        if (!data.TryGetValue(tagType, out var typeBytes))
            throw new BadResponseException($"Missing version qualifier tag 0x{tagType:X2}.");
        if (!data.TryGetValue(tagIteration, out var iterationBytes))
            throw new BadResponseException($"Missing version qualifier tag 0x{tagIteration:X2}.");

        var qualifierVersion = FirmwareVersion.FromBytes(RequireFieldLength(versionBytes, tagVersion, 3).Span);
        var versionType = (VersionQualifierType)RequireFieldLength(typeBytes, tagType, 1).Span[0];
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

        // Accept leading-zero extensions while retaining VersionQualifier's unsigned 32-bit numeric range.
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
        if (value.IsEmpty)
            return DeviceCapabilities.None;

        int fips = value.Length == 1 ? value.Span[0] : BinaryPrimitives.ReadUInt16BigEndian(value.Span);
        DeviceCapabilities capabilities = 0;

        foreach (var (bit, cap) in FipsMapping)
            if ((fips & bit) != 0)
                capabilities |= cap;

        return capabilities;
    }

    public static bool TryFromApp(ReadOnlySpan<byte> appData, out DeviceCapabilities capabilities)
    {
        uint mask = 0;
        foreach (byte b in appData)
        {
            if (mask > (uint.MaxValue >> 8))
            {
                capabilities = DeviceCapabilities.None;
                return false;
            }

            mask = (mask << 8) | b;
        }

        capabilities = (DeviceCapabilities)mask;
        return true;
    }
}