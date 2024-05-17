// Copyright 2021 Yubico AB
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

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Yubico.Core.Logging;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey
{
    /// <summary>
    /// This class can be used in commands to set and retrieve
    /// detailed device and capabilities information for a YubiKey.
    /// </summary>
    public class YubiKeyDeviceInfo : IYubiKeyDeviceInfo
    {
        private const byte UsbPrePersCapabilitiesTag = 0x01;
        private const byte SerialNumberTag = 0x02;
        private const byte UsbEnabledCapabilitiesTag = 0x03;
        private const byte FormFactorTag = 0x04;
        private const byte FirmwareVersionTag = 0x05;
        private const byte AutoEjectTimeoutTag = 0x06;
        private const byte ChallengeResponseTimeoutTag = 0x07;
        private const byte DeviceFlagsTag = 0x08;
        private const byte ConfigurationLockPresentTag = 0x0a;
        private const byte NfcPrePersCapabilitiesTag = 0x0d;
        private const byte NfcEnabledCapabilitiesTag = 0x0e;
        private const byte MoreDataTag = 0x10;
        // private const byte PartNumberTag = 0x13;
        // private const byte FipsCapableTag = 0x14;
        // private const byte FipsApprovedTag = 0x15;
        // private const byte PinComplexityTag = 0x16;
        private const byte NfcRestrictedTag = 0x17;
        // private const byte ResetBlockedTag = 0x18;
        private const byte TemplateStorageVersionTag = 0x20;
        private const byte ImageProcessorVersionTag = 0x21;

        // The IapFlags tag may be returned by the device, but it should be ignored.
        private const byte IapDetectionTag = 0x0f;

        private const byte FipsMask = 0b1000_0000;
        private const byte SkyMask = 0b0100_0000;
        private const byte FormFactorMask = unchecked((byte)~(FipsMask | SkyMask));

        private static readonly FirmwareVersion _fipsInclusiveLowerBound =
            FirmwareVersion.V4_4_0;

        private static readonly FirmwareVersion _fipsExclusiveUpperBound =
            FirmwareVersion.V4_5_0;

        private static readonly FirmwareVersion _fipsFlagInclusiveLowerBound =
            FirmwareVersion.V5_4_2;

        /// <inheritdoc />
        public YubiKeyCapabilities AvailableUsbCapabilities { get; set; }

        /// <inheritdoc />
        public YubiKeyCapabilities EnabledUsbCapabilities { get; set; }

        /// <inheritdoc />
        public YubiKeyCapabilities AvailableNfcCapabilities { get; set; }

        /// <inheritdoc />
        public YubiKeyCapabilities EnabledNfcCapabilities { get; set; }

        /// <inheritdoc />
        public int? SerialNumber { get; set; }

        /// <inheritdoc />
        public bool IsFipsSeries { get; set; }

        /// <inheritdoc />
        public bool IsSkySeries { get; set; }

        /// <inheritdoc />
        public FormFactor FormFactor { get; set; }

        /// <inheritdoc />
        public FirmwareVersion FirmwareVersion { get; set; }

        /// <inheritdoc />
        public TemplateStorageVersion? TemplateStorageVersion { get; set; }

        /// <inheritdoc />
        public ImageProcessorVersion? ImageProcessorVersion { get; set; }

        /// <inheritdoc />
        public int AutoEjectTimeout { get; set; }

        /// <inheritdoc />
        public byte ChallengeResponseTimeout { get; set; }

        /// <inheritdoc />
        public DeviceFlags DeviceFlags { get; set; }

        /// <inheritdoc />
        public bool ConfigurationLocked { get; set; }

        /// <inheritdoc />
        public bool IsNfcRestricted { get; set; }

        /// <summary>
        /// Constructs a default instance of YubiKeyDeviceInfo.
        /// </summary>
        public YubiKeyDeviceInfo()
        {
            FirmwareVersion = new FirmwareVersion();
        }

        /// <summary>
        /// Gets the YubiKey's device configuration details.
        /// </summary>
        /// <param name="responseApduData">
        /// The ResponseApdu data as byte array returned by the YubiKey. The first byte
        /// is the overall length of the TLV data, followed by the TLV data.
        /// </param>
        /// <param name="deviceInfo">
        /// On success, the <see cref="YubiKeyDeviceInfo"/> is returned using this out parameter.
        /// </param>
        /// <returns>
        /// True if the parsing and construction was successful, and false if
        /// <paramref name="responseApduData"/> did not meet formatting requirements.
        /// </returns>
        [Obsolete("This has been replaced by CreateFromResponseData")]
        internal static bool TryCreateFromResponseData(
            ReadOnlyMemory<byte> responseApduData,
            [MaybeNullWhen(returnValue: false)] out YubiKeyDeviceInfo deviceInfo)
        {
            Logger log = Log.GetLogger();

            // Certain transports (such as OTP keyboard) may return a buffer that is larger than the
            // overall TLV size. We want to make sure we're only parsing over real TLV data here, so
            // check the first byte to get the overall TLV length and slice accordingly.

            if (responseApduData.IsEmpty)
            {
                log.LogWarning("ResponseAPDU data was empty!");
                deviceInfo = null;
                return false;
            }

            int tlvDataLength = responseApduData.Span[0];

            if (tlvDataLength == 0 || 1 + tlvDataLength > responseApduData.Length)
            {
                log.LogWarning("TLV Data length was out of expected ranges. {Length}", tlvDataLength);
                deviceInfo = null;
                return false;
            }

            deviceInfo = new YubiKeyDeviceInfo();
            bool fipsSeriesFlag = false;
            bool skySeriesFlag = false;

            var tlvReader = new TlvReader(responseApduData.Slice(1, tlvDataLength));
            while (tlvReader.HasData)
            {
                switch (tlvReader.PeekTag())
                {
                    case UsbPrePersCapabilitiesTag:
                        ReadOnlySpan<byte> usbValue = tlvReader.ReadValue(UsbPrePersCapabilitiesTag).Span;
                        deviceInfo.AvailableUsbCapabilities = GetYubiKeyCapabilities(usbValue);
                        log.SensitiveLogInformation("Available capabilities (USB): {Capabilities}", deviceInfo.AvailableUsbCapabilities);
                        break;

                    case SerialNumberTag:
                        deviceInfo.SerialNumber = tlvReader.ReadInt32(SerialNumberTag, true);
                        log.SensitiveLogInformation("SerialNumber: {SerialNumber}", deviceInfo.SerialNumber);
                        break;

                    case UsbEnabledCapabilitiesTag:
                        ReadOnlySpan<byte> usbEnabledValue = tlvReader.ReadValue(UsbEnabledCapabilitiesTag).Span;
                        deviceInfo.EnabledUsbCapabilities = GetYubiKeyCapabilities(usbEnabledValue);
                        log.SensitiveLogInformation("Enabled capabilities (USB): {Capabilities}", deviceInfo.EnabledUsbCapabilities);
                        break;

                    case FormFactorTag:
                        byte formFactorValue = tlvReader.ReadByte(FormFactorTag);
                        deviceInfo.FormFactor = (FormFactor)(formFactorValue & FormFactorMask);
                        fipsSeriesFlag = (formFactorValue & FipsMask) == FipsMask;
                        skySeriesFlag = (formFactorValue & SkyMask) == SkyMask;
                        log.SensitiveLogInformation("FormFactor {FormFactor}, FIPS {Fips}, SKY {Sky}", deviceInfo.FormFactor, fipsSeriesFlag, skySeriesFlag);
                        break;

                    case FirmwareVersionTag:
                        ReadOnlySpan<byte> firmwareValue = tlvReader.ReadValue(FirmwareVersionTag).Span;
                        deviceInfo.FirmwareVersion = new FirmwareVersion
                        {
                            Major = firmwareValue[0],
                            Minor = firmwareValue[1],
                            Patch = firmwareValue[2]
                        };
                        log.SensitiveLogInformation("FirmwareVersion: {FirmwareVersion}", deviceInfo.FirmwareVersion.ToString());
                        break;

                    case AutoEjectTimeoutTag:
                        deviceInfo.AutoEjectTimeout = tlvReader.ReadUInt16(AutoEjectTimeoutTag);
                        log.SensitiveLogInformation("AutoEjectTimeout: {AutoEjectTimeout}", deviceInfo.AutoEjectTimeout);
                        break;

                    case ChallengeResponseTimeoutTag:
                        deviceInfo.ChallengeResponseTimeout =
                            tlvReader.ReadByte(ChallengeResponseTimeoutTag);
                        log.SensitiveLogInformation("ChallengeResponseTimeout: {ChallengeResponseTimeout}", deviceInfo.ChallengeResponseTimeout);
                        break;

                    case DeviceFlagsTag:
                        deviceInfo.DeviceFlags = (DeviceFlags)tlvReader.ReadByte(DeviceFlagsTag);
                        log.SensitiveLogInformation("DeviceFlags: {DeviceFlags}", deviceInfo.DeviceFlags);
                        break;

                    case ConfigurationLockPresentTag:
                        deviceInfo.ConfigurationLocked =
                            tlvReader.ReadByte(ConfigurationLockPresentTag) == 1;
                        log.SensitiveLogInformation("ConfigurationLocked: {ConfigurationLocked}", deviceInfo.ConfigurationLocked);
                        break;

                    case NfcPrePersCapabilitiesTag:
                        ReadOnlySpan<byte> nfcValue = tlvReader.ReadValue(NfcPrePersCapabilitiesTag).Span;
                        deviceInfo.AvailableNfcCapabilities = GetYubiKeyCapabilities(nfcValue);
                        log.SensitiveLogInformation("AvailableNfcCapabilities: {AvailableNfcCapabilities}", deviceInfo.AvailableNfcCapabilities);
                        break;

                    case NfcEnabledCapabilitiesTag:
                        ReadOnlySpan<byte> nfcEnabledValue = tlvReader.ReadValue(NfcEnabledCapabilitiesTag).Span;
                        deviceInfo.EnabledNfcCapabilities = GetYubiKeyCapabilities(nfcEnabledValue);
                        log.SensitiveLogInformation("EnabledNfcCapabilities: {EnabledNfcCapabilities}", deviceInfo.EnabledNfcCapabilities);
                        break;

                    case IapDetectionTag:
                        // The YubiKey may provide a TLV that represents the state of the iAP flags.
                        // This data is reserved for future use, and is not meant to be used.
                        // Therefore we will swallow this value, advancing the reader to the next TLV.
                        _ = tlvReader.ReadByte(IapDetectionTag);
                        break;

                    case MoreDataTag:
                        // Ignore this tag for now.
                        _ = (int)tlvReader.ReadByte(MoreDataTag);
                        break;

                    case TemplateStorageVersionTag:
                        ReadOnlySpan<byte> fpChipVersion = tlvReader.ReadValue(TemplateStorageVersionTag).Span;
                        deviceInfo.TemplateStorageVersion = new TemplateStorageVersion()
                        {
                            Major = fpChipVersion[0],
                            Minor = fpChipVersion[1],
                            Patch = fpChipVersion[2]
                        };
                        log.SensitiveLogInformation("TemplateStorageVersion: {TemplateStorageVersion}", deviceInfo.TemplateStorageVersion.ToString());
                        break;

                    case ImageProcessorVersionTag:
                        ReadOnlySpan<byte> ipChipVersion = tlvReader.ReadValue(ImageProcessorVersionTag).Span;
                        deviceInfo.ImageProcessorVersion = new ImageProcessorVersion()
                        {
                            Major = ipChipVersion[0],
                            Minor = ipChipVersion[1],
                            Patch = ipChipVersion[2]
                        };
                        log.SensitiveLogInformation("ImageProcessorVersion: {ImageProcessorVersion}", deviceInfo.ImageProcessorVersion.ToString());
                        break;

                    case NfcRestrictedTag:
                        deviceInfo.IsNfcRestricted = tlvReader.ReadByte(NfcRestrictedTag) == 1;
                        break;

                    default:
                        Debug.Assert(false, "Encountered an unrecognized tag in DeviceInfo. Ignoring.");
                        break;
                }
            }

            deviceInfo.IsFipsSeries =
                deviceInfo.FirmwareVersion >= _fipsFlagInclusiveLowerBound
                ? fipsSeriesFlag
                : deviceInfo.IsFipsVersion;

            deviceInfo.IsSkySeries |= skySeriesFlag;

            return true;
        }

        /// <summary>
        /// Gets the YubiKey's device configuration details.
        /// </summary>
        /// <param name="responseApduData">
        /// The ResponseApdu data as byte array returned by the YubiKey. The first byte
        /// is the overall length of the TLV data, followed by the TLV data.
        /// </param>
        /// <returns>
        /// On success, the <see cref="YubiKeyDeviceInfo"/> is returned using this out parameter.
        /// <paramref name="responseApduData"/> did not meet formatting requirements.
        /// </returns>
        internal static YubiKeyDeviceInfo CreateFromResponseData(
            Dictionary<int, ReadOnlyMemory<byte>> responseApduData)
        {
            bool fipsSeriesFlag = false;
            bool skySeriesFlag = false;

            var deviceInfo = new YubiKeyDeviceInfo();
            foreach (KeyValuePair<int, ReadOnlyMemory<byte>> tagValuePair in responseApduData)
            {
                switch (tagValuePair.Key)
                {
                    case UsbPrePersCapabilitiesTag:
                        deviceInfo.AvailableUsbCapabilities = GetYubiKeyCapabilities(tagValuePair.Value.Span);
                        break;

                    case SerialNumberTag:
                        deviceInfo.SerialNumber = BinaryPrimitives.ReadInt32BigEndian(tagValuePair.Value.Span);
                        break;

                    case UsbEnabledCapabilitiesTag:
                        deviceInfo.EnabledUsbCapabilities = GetYubiKeyCapabilities(tagValuePair.Value.Span);
                        break;

                    case FormFactorTag:
                        byte formFactorValue = tagValuePair.Value.Span[0];
                        deviceInfo.FormFactor = (FormFactor)(formFactorValue & FormFactorMask);
                        fipsSeriesFlag = (formFactorValue & FipsMask) == FipsMask;
                        skySeriesFlag = (formFactorValue & SkyMask) == SkyMask;
                        break;

                    case FirmwareVersionTag:
                        ReadOnlySpan<byte> firmwareValue = tagValuePair.Value.Span;
                        deviceInfo.FirmwareVersion = new FirmwareVersion
                        {
                            Major = firmwareValue[0],
                            Minor = firmwareValue[1],
                            Patch = firmwareValue[2]
                        };
                        break;

                    case AutoEjectTimeoutTag:
                        deviceInfo.AutoEjectTimeout = BinaryPrimitives.ReadUInt16BigEndian(tagValuePair.Value.Span);
                        break;

                    case ChallengeResponseTimeoutTag:
                        deviceInfo.ChallengeResponseTimeout = tagValuePair.Value.Span[0];
                        break;

                    case DeviceFlagsTag:
                        deviceInfo.DeviceFlags = (DeviceFlags)tagValuePair.Value.Span[0];
                        break;

                    case ConfigurationLockPresentTag:
                        deviceInfo.ConfigurationLocked = tagValuePair.Value.Span[0] == 1;
                        break;

                    case NfcPrePersCapabilitiesTag:
                        deviceInfo.AvailableNfcCapabilities = GetYubiKeyCapabilities(tagValuePair.Value.Span);
                        break;

                    case NfcEnabledCapabilitiesTag:
                        deviceInfo.EnabledNfcCapabilities = GetYubiKeyCapabilities(tagValuePair.Value.Span);
                        break;

                    case TemplateStorageVersionTag:
                        ReadOnlySpan<byte> fpChipVersion = tagValuePair.Value.Span;
                        deviceInfo.TemplateStorageVersion = new TemplateStorageVersion
                        {
                            Major = fpChipVersion[0],
                            Minor = fpChipVersion[1],
                            Patch = fpChipVersion[2]
                        };
                        break;

                    case ImageProcessorVersionTag:
                        ReadOnlySpan<byte> ipChipVersion = tagValuePair.Value.Span;
                        deviceInfo.ImageProcessorVersion = new ImageProcessorVersion
                        {
                            Major = ipChipVersion[0],
                            Minor = ipChipVersion[1],
                            Patch = ipChipVersion[2]
                        };
                        break;

                    case NfcRestrictedTag:
                        deviceInfo.IsNfcRestricted = tagValuePair.Value.Span[0] == 1;
                        break;
                    case IapDetectionTag:
                    case MoreDataTag:
                        // Ignore these tags for now
                        break;

                    //Todo add more cases, needs run test
                    default:
                        Debug.Assert(false, "Encountered an unrecognized tag in DeviceInfo. Ignoring.");
                        break;
                }
            }

            deviceInfo.IsFipsSeries = deviceInfo.FirmwareVersion >= _fipsFlagInclusiveLowerBound
                    ? fipsSeriesFlag
                    : deviceInfo.IsFipsVersion;

            deviceInfo.IsSkySeries |= skySeriesFlag;

            return deviceInfo;
        }

        internal YubiKeyDeviceInfo Merge(YubiKeyDeviceInfo? second)
        {
            second ??= new YubiKeyDeviceInfo();

            return new YubiKeyDeviceInfo
            {
                AvailableUsbCapabilities = AvailableUsbCapabilities | second.AvailableUsbCapabilities,

                EnabledUsbCapabilities = EnabledUsbCapabilities | second.EnabledUsbCapabilities,

                AvailableNfcCapabilities = AvailableNfcCapabilities | second.AvailableNfcCapabilities,

                EnabledNfcCapabilities = EnabledNfcCapabilities | second.EnabledNfcCapabilities,

                SerialNumber = SerialNumber ?? second.SerialNumber,

                IsFipsSeries = IsFipsSeries || second.IsFipsSeries,

                FormFactor = FormFactor != FormFactor.Unknown
                    ? FormFactor
                    : second.FormFactor,

                FirmwareVersion = FirmwareVersion != new FirmwareVersion()
                    ? FirmwareVersion
                    : second.FirmwareVersion,

                AutoEjectTimeout = DeviceFlags.HasFlag(DeviceFlags.TouchEject)
                        ? AutoEjectTimeout
                        : second.DeviceFlags.HasFlag(DeviceFlags.TouchEject)
                            ? second.AutoEjectTimeout
                            : default,

                ChallengeResponseTimeout = ChallengeResponseTimeout != default
                    ? ChallengeResponseTimeout
                    : second.ChallengeResponseTimeout,

                DeviceFlags = DeviceFlags | second.DeviceFlags,

                ConfigurationLocked = ConfigurationLocked != default
                    ? ConfigurationLocked
                    : second.ConfigurationLocked,

                IsNfcRestricted = IsNfcRestricted || second.IsNfcRestricted
            };
        }

        /// <summary>
        /// The firmware version is known to be a FIPS Series device.
        /// </summary>
        private bool IsFipsVersion =>
            FirmwareVersion >= _fipsInclusiveLowerBound
            && FirmwareVersion < _fipsExclusiveUpperBound;

        private static YubiKeyCapabilities GetYubiKeyCapabilities(ReadOnlySpan<byte> value) =>
            // Older firmware versions only encode this enumeration with one byte. If we see a single
            // byte in this element, it's OK. We will handle it here.
            value.Length == 1
                ? (YubiKeyCapabilities)value[0]
                : (YubiKeyCapabilities)BinaryPrimitives.ReadInt16BigEndian(value);

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(AvailableUsbCapabilities)}: {AvailableUsbCapabilities}, {nameof(EnabledUsbCapabilities)}: {EnabledUsbCapabilities}, {nameof(AvailableNfcCapabilities)}: {AvailableNfcCapabilities}, {nameof(EnabledNfcCapabilities)}: {EnabledNfcCapabilities}, {nameof(AutoEjectTimeout)}: {AutoEjectTimeout}, {nameof(DeviceFlags)}: {DeviceFlags}, {nameof(ConfigurationLocked)}: {ConfigurationLocked}";
    }
}
