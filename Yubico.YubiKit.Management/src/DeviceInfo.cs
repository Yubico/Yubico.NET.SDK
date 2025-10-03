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
    public required int SerialNumber { get; init; }
    public required bool IsLocked { get; init; }
    public required YubiKeyCapabilities UsbEnabled { get; init; }
    public required YubiKeyCapabilities UsbSupported { get; init; }
    public required YubiKeyCapabilities NfcEnabled { get; init; }
    public required YubiKeyCapabilities NfcSupported { get; init; }
    public required YubiKeyCapabilities ResetBlocked { get; init; }
    public required YubiKeyCapabilities FipsCapabilities { get; init; }
    public required YubiKeyCapabilities FipsApproved { get; init; }
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


    public static DeviceInfo CreateFromTlvs(IReadOnlyCollection<Tlv> tlvPages, FirmwareVersion? defaultVersion)
    {
        var dict = new Dictionary<int, ReadOnlyMemory<byte>>(tlvPages.Count);
        foreach (var tlv in tlvPages)
            dict.Add(tlv.Tag, tlv.Value);

        var isLocked = dict.GetSpan(TAG_CONFIG_LOCKED)[0] == 1;
        var serialNumber = BinaryPrimitives.ReadInt32BigEndian(dict.GetSpan(TAG_SERIAL_NUMBER));

        int formFactorTagData = dict.GetSpan(TAG_FORMFACTOR)[0];
        var isFips = (formFactorTagData & 0x80) != 0;
        var isSky = (formFactorTagData & 0x40) != 0;
        var formFactor = (FormFactor)formFactorTagData;

        var resetBlocked = GetYubiKeyCapabilities(dict.GetMemory(TAG_RESET_BLOCKED));
        var usbEnabled = GetYubiKeyCapabilities(dict.GetMemory(TAG_USB_ENABLED));
        var usbSupported = GetYubiKeyCapabilities(dict.GetMemory(TAG_USB_SUPPORTED));
        var nfcEnabled = GetYubiKeyCapabilities(dict.GetMemory(TAG_NFC_ENABLED));
        var nfcSupported = GetYubiKeyCapabilities(dict.GetMemory(TAG_NFC_SUPPORTED));
        var isNfcRestricted = dict.TryGetValue(TAG_NFC_RESTRICTED, out var nfcRestrictedBytes) &&
                              nfcRestrictedBytes.Span[0] == 1;

        var fipsCapabilities = GetFipsCapabilities(dict.GetMemory(TAG_FIPS_CAPABLE));
        var fipsApprovedCapabilities = GetFipsCapabilities(dict.GetMemory(TAG_FIPS_APPROVED));
        var hasPinComplexity = dict.TryGetValue(TAG_PIN_COMPLEXITY, out var hasPinComplexityBytes) &&
                               hasPinComplexityBytes.Span[0] == 1;

        string? partNumber = null;
        if (dict.TryGetSpan(TAG_PART_NUMBER, out var partNumberBytes))
            partNumber = GetPartNumber(partNumberBytes);

        var autoEjectTimeout = BinaryPrimitives.ReadUInt16BigEndian(dict.GetSpan(TAG_AUTO_EJECT_TIMEOUT));
        var challengeResponseTimeout = dict.GetSpan(TAG_CHALLENGE_RESPONSE_TIMEOUT);
        var deviceFlags = (DeviceFlags)dict.GetSpan(TAG_DEVICE_FLAGS)[0];
        FirmwareVersion? fpsVersion = null;
        if (dict.TryGetSpan(TAG_FPS_VERSION, out var fpsVersionBytes) &&
            !fpsVersionBytes.SequenceEqual(new byte[3]))
        {
            var fpsVersionMajor = fpsVersionBytes[0];
            var fpsVersionMinor = fpsVersionBytes[1];
            var fpsVersionMPatch = fpsVersionBytes[2];
            fpsVersion = new FirmwareVersion(fpsVersionMajor, fpsVersionMinor, fpsVersionMPatch);
        }

        FirmwareVersion? stmVersion = null;
        if (dict.TryGetSpan(TAG_STM_VERSION, out var stmVersionBytes) &&
            !stmVersionBytes.SequenceEqual(new byte[3]))
        {
            var stmVersionMajor = stmVersionBytes[0];
            var stmVersionMinor = stmVersionBytes[1];
            var stmVersionMPatch = stmVersionBytes[2];
            stmVersion = new FirmwareVersion(stmVersionMajor, stmVersionMinor, stmVersionMPatch);
        }

        var firmwareVersionMajor = dict.GetSpan(TAG_FIRMWARE_VERSION)[0];
        var firmwareVersionMinor = dict.GetSpan(TAG_FIRMWARE_VERSION)[1];
        var firmwareVersionMPatch = dict.GetSpan(TAG_FIRMWARE_VERSION)[2];
        defaultVersion ??= new FirmwareVersion(firmwareVersionMajor, firmwareVersionMinor, firmwareVersionMPatch);
        var (firmwareVersion, versionQualifier) = DetermineFirmwareVersion(dict, defaultVersion);

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
            ChallengeResponseTimeout = challengeResponseTimeout.ToArray(),
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

    private static YubiKeyCapabilities GetFipsCapabilities(ReadOnlyMemory<byte> value)
    {
        if (value.IsEmpty) return 0;

        YubiKeyCapabilities capabilities = 0;
        int fips = BinaryPrimitives.ReadInt16BigEndian(value.Span);
        if ((fips & 0b0000_0001) != 0) capabilities |= YubiKeyCapabilities.Fido2;
        if ((fips & 0b0000_0010) != 0) capabilities |= YubiKeyCapabilities.Piv;
        if ((fips & 0b0000_0100) != 0) capabilities |= YubiKeyCapabilities.OpenPgp;
        if ((fips & 0b0000_1000) != 0) capabilities |= YubiKeyCapabilities.Oath;
        if ((fips & 0b0001_0000) != 0) capabilities |= YubiKeyCapabilities.HsmAuth;

        return capabilities;
    }

    private static YubiKeyCapabilities GetYubiKeyCapabilities(ReadOnlyMemory<byte> value)
    {
        if (value.IsEmpty) return 0;
        return value.Length == 1
            ? (YubiKeyCapabilities)value.Span[0]
            : (YubiKeyCapabilities)BinaryPrimitives.ReadInt16BigEndian(value.Span);
    }
}

[Flags]
public enum YubiKeyCapabilities
{
    /// <summary>
    ///     Identifies the YubiOTP application.
    /// </summary>
    Otp = 0x0001,

    /// <summary>
    ///     Identifies the U2F (CTAP1) portion of the FIDO application.
    /// </summary>
    U2f = 0x0002,

    /// <summary>
    ///     Identifies the OpenPGP application, implementing the OpenPGP Card protocol.
    /// </summary>
    OpenPgp = 0x0008,

    /// <summary>
    ///     Identifies the PIV application, implementing the PIV protocol.
    /// </summary>
    Piv = 0x0010,

    /// <summary>
    ///     Identifies the OATH application, implementing the YKOATH protocol.
    /// </summary>
    Oath = 0x0020,

    /// <summary>
    ///     Identifies the HSMAUTH application.
    /// </summary>
    HsmAuth = 0x0100,

    /// <summary>
    ///     Identifies the FIDO2  = CTAP2 portion of the FIDO application.
    /// </summary>
    Fido2 = 0x0200,
    
    All = Otp | U2f | OpenPgp | Piv | Oath | HsmAuth | Fido2
}

/// <summary>
///     Miscellaneous flags representing various settings available on the YubiKey.
/// </summary>
[Flags]
public enum DeviceFlags
{
    /// <summary>
    ///     No device flags are set.
    /// </summary>
    None = 0x00,

    /// <summary>
    ///     USB remote wakeup is enabled.
    /// </summary>
    RemoteWakeup = 0x40,

    /// <summary>
    ///     The CCID touch-eject feature is enabled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For the CCID connection, the YubiKey behaves as a smart card reader and smart
    ///         card. When this flag is disabled, the smart card is always present in the smart
    ///         card reader. When enabled, the smart card will be ejected by default,
    ///         and the user is required to touch the YubiKey to insert the smart card. For
    ///         this to take effect, all <see cref="YubiKeyCapabilities" /> which do not depend
    ///         on the CCID connection (such as <c>Fido2</c>, <c>FidoU2f</c>, and <c>Otp</c>)
    ///         must be disabled.
    ///     </para>
    ///     <para>
    ///         To automatically eject the smart card following a touch, see
    ///         <see cref="IYubiKeyDeviceInfo.AutoEjectTimeout" />.
    ///     </para>
    /// </remarks>
    TouchEject = 0x80
}