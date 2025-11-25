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

using System;
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Management.Commands
{
    /// <summary>
    /// Base class for SetDeviceInfoCommand for Management, OTP and FIDO applications.
    /// </summary>
    public class SetDeviceInfoBaseCommand
    {
        private byte[]? _lockCode;
        private byte[]? _unlockCode;
        private ushort? _autoEjectTimeout;

        /// <summary>
        /// The length that a configuration lock code must be.
        /// </summary>
        public const byte LockCodeLength = 16;

        /// <summary>
        /// The features of the YubiKey that should be enabled over USB. <see langword="null"/>
        /// to leave unchanged.
        /// </summary>
        public YubiKeyCapabilities? EnabledUsbCapabilities { get; set; }

        /// <summary>
        /// The features of the YubiKey that should be enabled over NFC. <see langword="null"/>
        /// to leave unchanged.
        /// </summary>
        public YubiKeyCapabilities? EnabledNfcCapabilities { get; set; }

        /// <summary>
        /// The period of time (in seconds) after which the OTP challenge-response command should
        /// timeout.  <see langword="null"/> to leave unchanged.
        /// </summary>
        public byte? ChallengeResponseTimeout { get; set; }


        /// <summary>
        /// The CCID auto-eject timeout (in seconds). This field is only meaningful if the
        /// TouchEject flag in DeviceFlags is set. <see langword="null"/> to leave unchanged.
        /// </summary>
        /// <remarks>
        /// When setting, the value must be in the range <see cref="ushort.MinValue"/> through
        /// <see cref="ushort.MaxValue"/>. Otherwise an <see cref="ArgumentOutOfRangeException"/>
        /// will be thrown.
        /// </remarks>
        public int? AutoEjectTimeout
        {
            get => _autoEjectTimeout;

            set
            {
                if (value.HasValue && (value < ushort.MinValue || value > ushort.MaxValue))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _autoEjectTimeout = (ushort?)value;
            }
        }

        /// <summary>
        /// Device flags that can control device-global behavior.  <see langword="null"/> to leave
        /// unchanged.
        /// </summary>
        public DeviceFlags? DeviceFlags { get; set; }

        /// <summary>
        /// Resets (reboots) the YubiKey after the successful application of all configuration
        /// updates. Useful if enabling or disabling capabilities.
        /// </summary>
        public bool ResetAfterConfig { get; set; }

        /// <summary>
        /// Allows setting of the <see cref="YubiKeyDeviceInfo.IsNfcRestricted"/> property
        /// </summary>
        public bool RestrictNfc { get; set; }

        /// <summary>
        /// Temporarily set the threshold at which a capacitive touch should be considered active.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The field is using arbitrary units and has a default value of `6`. A higher value increases the sensor
        /// threshold which has the effect of decreasing the sensitivity of the sensor. Lower values increase the
        /// sensitivity, but callers cannot reduce the threshold below the default value of `6` which is locked in at
        /// manufacturing time.
        /// </para>
        /// <para>
        /// The value set here is only valid until the next time the YubiKey is power cycled. It does not persist.
        /// </para>
        /// <para>
        /// You should typically not ever need to adjust this value. This is primarily used in the context of automatic
        /// provisioning and testing where the YubiKey is being "touched" by electrically grounding the sensor.
        /// </para>
        /// </remarks>
        public int? TemporaryTouchThreshold { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDeviceInfoBaseCommand"/> class.
        /// </summary>
        protected SetDeviceInfoBaseCommand()
        {

        }

        protected SetDeviceInfoBaseCommand(SetDeviceInfoBaseCommand baseCommand)
        {
            if (baseCommand is null)
            {
                return;
            }

            EnabledUsbCapabilities = baseCommand.EnabledUsbCapabilities;
            EnabledNfcCapabilities = baseCommand.EnabledNfcCapabilities;
            ChallengeResponseTimeout = baseCommand.ChallengeResponseTimeout;
            AutoEjectTimeout = baseCommand.AutoEjectTimeout;
            DeviceFlags = baseCommand.DeviceFlags;
            ResetAfterConfig = baseCommand.ResetAfterConfig;
            RestrictNfc = baseCommand.RestrictNfc;
            TemporaryTouchThreshold = baseCommand.TemporaryTouchThreshold;

            _lockCode = baseCommand._lockCode;
            _unlockCode = baseCommand._unlockCode;
        }

        /// <summary>
        /// Locks the YubiKey's configuration with a code. Any subsequent calls to the
        /// <see cref="SetDeviceInfoBaseCommand"/> class will need to unlock the YubiKey using the
        /// <see cref="ApplyLockCode"/> method.
        /// </summary>
        /// <param name="lockCode">
        /// A 16-byte lock code. A value of all zeros (16 bytes) will clear the code.
        /// </param>
        public void SetLockCode(ReadOnlySpan<byte> lockCode)
        {
            if (lockCode.Length != LockCodeLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.LockCodeWrongLength,
                        LockCodeLength,
                        lockCode.Length),
                        nameof(lockCode));
            }

            if (_lockCode is byte[] clearMe)
            {
                CryptographicOperations.ZeroMemory(clearMe);
            }

            _lockCode = lockCode.ToArray();
        }

        /// <summary>
        /// Temporarily unlocks the YubiKey's configuration by applying the lock code.
        /// </summary>
        /// <param name="lockCode">The 16-byte lock code for this YubiKey.</param>
        public void ApplyLockCode(ReadOnlySpan<byte> lockCode)
        {
            if (lockCode.Length != LockCodeLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.LockCodeWrongLength,
                        LockCodeLength,
                        lockCode.Length),
                        nameof(lockCode));
            }

            if (_unlockCode is byte[] clearMe)
            {
                CryptographicOperations.ZeroMemory(clearMe);
            }

            _unlockCode = lockCode.ToArray();
        }

        /// <summary>
        /// Formats the data to be sent to the YubiKey.
        /// </summary>
        /// <returns>
        /// A formatted byte array.
        /// </returns>
        protected byte[] GetDataForApdu()
        {
            byte[] tlvData = GetTlvData();
            byte tlvDataLength = checked((byte)tlvData.Length);

            byte[] dataField = new byte[tlvDataLength + 1];
            dataField[0] = tlvDataLength;
            tlvData.CopyTo(dataField, 1);

            // tlvData may contain lock/unlock codes
            CryptographicOperations.ZeroMemory(tlvData);

            return dataField;
        }

        private byte[] GetTlvData()
        {
            var buffer = new TlvWriter();

            if (EnabledUsbCapabilities is YubiKeyCapabilities usbCapabilities)
            {
                buffer.WriteInt16(YubikeyDeviceManagementTags.UsbEnabledCapabilitiesTag, (short)usbCapabilities);
            }

            if (EnabledNfcCapabilities is YubiKeyCapabilities nfcCapabilities)
            {
                buffer.WriteInt16(YubikeyDeviceManagementTags.NfcEnabledCapabilitiesTag, (short)nfcCapabilities);
            }

            if (ChallengeResponseTimeout is byte crTimeout)
            {
                buffer.WriteByte(YubikeyDeviceManagementTags.ChallengeResponseTimeoutTag, crTimeout);
            }

            if (_autoEjectTimeout is ushort aeTimeout)
            {
                buffer.WriteUInt16(YubikeyDeviceManagementTags.AutoEjectTimeoutTag, aeTimeout);
            }

            if (DeviceFlags is DeviceFlags deviceFlags)
            {
                buffer.WriteByte(YubikeyDeviceManagementTags.DeviceFlagsTag, (byte)deviceFlags);
            }

            if (ResetAfterConfig)
            {
                buffer.WriteValue(YubikeyDeviceManagementTags.ResetAfterConfigTag, ReadOnlySpan<byte>.Empty);
            }

            if (_lockCode is byte[] lockCode)
            {
                buffer.WriteValue(YubikeyDeviceManagementTags.ConfigurationLockPresentTag, lockCode);
            }

            if (_unlockCode is byte[] unlockCode)
            {
                buffer.WriteValue(YubikeyDeviceManagementTags.ConfigurationUnlockPresentTag, unlockCode);
            }

            if (RestrictNfc)
            {
                buffer.WriteByte(YubikeyDeviceManagementTags.NfcRestrictedTag, 1);
            }

            if (TemporaryTouchThreshold.HasValue)
            {
                buffer.WriteByte(YubikeyDeviceManagementTags.TempTouchThresholdTag, (byte)TemporaryTouchThreshold.Value);
            }

            return buffer.Encode();
        }
    }
}
