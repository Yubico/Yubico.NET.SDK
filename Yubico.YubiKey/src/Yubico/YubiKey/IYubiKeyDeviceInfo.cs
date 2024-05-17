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

namespace Yubico.YubiKey
{
    /// <summary>
    /// Detailed device and capabilities information for a YubiKey.
    /// </summary>
    public interface IYubiKeyDeviceInfo
    {
        /// <summary>
        /// The paid-for YubiKey features that are available over USB (and Lightning).
        /// </summary>
        public YubiKeyCapabilities AvailableUsbCapabilities { get; }

        /// <summary>
        /// The USB features that are currently enabled over USB (and Lightning).
        /// </summary>
        public YubiKeyCapabilities EnabledUsbCapabilities { get; }

        /// <summary>
        /// The paid-for YubiKey features that are available over NFC.
        /// </summary>
        public YubiKeyCapabilities AvailableNfcCapabilities { get; }

        /// <summary>
        /// The NFC features that are currently enabled over NFC.
        /// </summary>
        public YubiKeyCapabilities EnabledNfcCapabilities { get; }

        /// <summary>
        /// The serial number of the YubiKey, if one is present.
        /// </summary>
        public int? SerialNumber { get; }

        /// <summary>
        /// Indicates whether or not the YubiKey is a FIPS Series device.
        /// </summary>
        /// <remarks>
        /// When using a YubiKey FIPS Series device as an authenticator in a FIPS environment,
        /// all of the sub-modules must be in a FIPS approved mode of operation for the
        /// YubiKey FIPS Series device as a whole to be considered as operating in a FIPS
        /// approved mode. This value does not determine whether the YubiKey is in a FIPS
        /// approved mode.
        /// </remarks>
        public bool IsFipsSeries { get; }

        /// <summary>
        /// Indicates whether or not this device is a "Security Key by Yubico" series device.
        /// </summary>
        /// <remarks>
        /// Security Key Series devices only support the U2F and FIDO2 applications. This property helps differentiate
        /// these devices from a standard YubiKey that only has these two applications enabled.
        /// </remarks>
        public bool IsSkySeries { get; }

        /// <summary>
        /// The form-factor of the YubiKey.
        /// </summary>
        public FormFactor FormFactor { get; }

        /// <summary>
        /// The version of the firmware currently running on the YubiKey.
        /// </summary>
        public FirmwareVersion FirmwareVersion { get; }

        /// <summary>
        /// The version of the chip/firmware storing the fingerprints (the second
        /// secure element). If there is no template storage chip, this will be
        /// null.
        /// </summary>
        public TemplateStorageVersion? TemplateStorageVersion { get; }

        /// <summary>
        /// The version of the chip/firmware performing the image processing. If
        /// there is no image processing chip, this will be null.
        /// </summary>
        public ImageProcessorVersion? ImageProcessorVersion { get; }

        /// <summary>
        /// The CCID auto-eject timeout (in seconds).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This field is only meaningful if <see cref="Yubico.YubiKey.DeviceFlags.TouchEject"/> in
        /// <see cref="DeviceFlags"/> is set. A value of <c>0</c> means that the timeout
        /// is disabled (the smart card will not be ejected automatically).
        /// </para>
        /// <para>
        /// The range is <see cref="ushort.MinValue"/> through <see cref="ushort.MaxValue"/>.
        /// </para>
        /// </remarks>
        public int AutoEjectTimeout { get; }

        /// <summary>
        /// The period of time (in seconds) after which the OTP challenge-response command
        /// should timeout.
        /// </summary>
        /// <remarks>
        /// The default value for the timeout is 15 seconds.
        /// </remarks>
        public byte ChallengeResponseTimeout { get; }

        /// <summary>
        /// Device flags that can control device-global behavior.
        /// </summary>
        public DeviceFlags DeviceFlags { get; }

        /// <summary>
        /// Indicates whether or not the YubiKey's configuration has been locked by the user.
        /// </summary>
        public bool ConfigurationLocked { get; }
        
        /// <summary>
        /// Indicates if this device has temporarily disabled NFC.
        /// </summary>
        public bool IsNfcRestricted { get; }
    }
}
