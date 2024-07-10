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
using System.Globalization;

namespace Yubico.YubiKey
{
    /// <summary>
    ///     A static class containing the <see cref="HasFeature" /> extension method.
    /// </summary>
    public static class YubiKeyFeatureExtensions
    {
        /// <summary>
        ///     Feature presence detection. Used to query whether or not a particular YubiKey feature is available for this
        ///     device.
        /// </summary>
        /// <param name="yubiKeyDevice">The YubiKey device to query.</param>
        /// <param name="feature">The name of the feature that you wish to query.</param>
        /// <returns>`true` if the feature is present on the YubiKey, `false` otherwise.</returns>
        /// <exception cref="ArgumentNullException">The YubiKey device was null.</exception>
        /// <exception cref="ArgumentException">An unknown feature was queried.</exception>
        /// <remarks>
        ///     Use this API to programmatically detect if a particular feature is present on a YubiKey. If your application
        ///     needs to deal with a heterogeneous deployment of YubiKeys across major releases, this method allows you to
        ///     switch on whether a certain feature is present or not. This method is meant to be more accurate than querying
        ///     firmware versions directly, as it will be kept up to date with all YubiKey releases.
        /// </remarks>
        /// <example>
        ///     Get the number of total tries a user has to enter the PIN of the PIV application:
        ///     <code language="csharp">
        /// public int GetPinTries(YubiKeyDevice yubiKeyDevice)
        /// {
        ///   if (!yubiKeyDevice.HasFeature(YubiKeyFeature.PivMetadata))
        ///     throw new NotSupportedException("The PIV metadata command is not supported on this YubiKey.");
        /// 
        ///   using PivSession piv = new PivSession(yubiKeyDevice);
        ///   var metadata = piv.GetMetadata(PivSlot.Pin);
        ///   return metadata.RetryCount;
        /// }
        /// </code>
        /// </example>
        public static bool HasFeature(this IYubiKeyDevice yubiKeyDevice, YubiKeyFeature feature)
        {
            if (yubiKeyDevice is null)
            {
                throw new ArgumentNullException(nameof(yubiKeyDevice));
            }

            return feature switch
            {
                // General YubiKey features

                YubiKeyFeature.OtpApplication =>
                    HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OathApplication =>
                    HasApplication(yubiKeyDevice, YubiKeyCapabilities.Oath),

                YubiKeyFeature.PivApplication =>
                    HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.U2fApplication =>
                    HasApplication(yubiKeyDevice, YubiKeyCapabilities.FidoU2f),

                YubiKeyFeature.Fido2Application =>
                    HasApplication(yubiKeyDevice, YubiKeyCapabilities.Fido2),

                YubiKeyFeature.ManagementApplication =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_0_0,

                YubiKeyFeature.ManagementNfcRestricted =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_7_0,

                YubiKeyFeature.SerialNumberVisibilityControls =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_2_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.Scp03 =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_3_0
                    && (HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv)
                    || HasApplication(yubiKeyDevice, YubiKeyCapabilities.Oath)
                    || HasApplication(yubiKeyDevice, YubiKeyCapabilities.OpenPgp)),

                YubiKeyFeature.FastUsbReclaim =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_6_0,

                YubiKeyFeature.DeviceReset =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_6_0,

                YubiKeyFeature.TemporaryTouchThreshold =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_3_1,

                YubiKeyFeature.YubiHsmAuthApplication =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_4_3
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.YubiHsmAuth),

                // OTP application features

                YubiKeyFeature.OtpOathHotpMode =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_1_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpProtectedLongPressSlot =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_0_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpNumericKeypad =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_3_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpFastTrigger =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_3_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpUpdatableSlots =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_3_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpDormantSlots =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_3_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpInvertLed =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_4_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpShortTickets =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_0_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpStaticPasswordMode =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_0_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpVariableSizeHmac =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_2_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpButtonTrigger =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_2_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpMixedCasePasswords =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_0_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpFixedModhex =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_1_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpChallengeResponseMode =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_2_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpAlphaNumericPasswords =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_0_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                YubiKeyFeature.OtpPasswordManualUpdates =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V2_0_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Otp),

                // PIV application features

                YubiKeyFeature.PivAttestation =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V4_3_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivAesManagementKey =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_4_2 ||
                    yubiKeyDevice.FirmwareVersion == new FirmwareVersion(major: 0, minor: 8, patch: 8)
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivMetadata =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_3_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivRsa1024 =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V3_1_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivRsa2048 =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V3_1_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivRsa3072 =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_7_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivRsa4096 =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_7_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivEccP256 =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V4_2_4
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivEccP384 =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V4_2_4
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivMoveOrDeleteKey =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_7_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivManagementKeyTouchPolicy =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V4_0_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivTouchPolicyCached =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V4_3_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                YubiKeyFeature.PivPrivateKeyTouchPolicyCached =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V4_3_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Piv),

                // OATH application features

                YubiKeyFeature.OathRenameCredential =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_3_0
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Oath),

                YubiKeyFeature.OathTouchCredential =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V4_2_4
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Oath),

                YubiKeyFeature.OathSha512 =>
                    yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V4_3_4
                    && HasApplication(yubiKeyDevice, YubiKeyCapabilities.Oath),

                _ => throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.UnknownYubiKeyFeature))
            };
        }

        /// <summary>
        ///     Throws a <see cref="NotSupportedException" /> if the YubiKey doesn't support the requested feature.
        /// </summary>
        /// <param name="yubiKeyDevice"></param>
        /// <param name="feature"></param>
        /// <exception cref="NotSupportedException"></exception>
        public static void ThrowOnMissingFeature(this IYubiKeyDevice yubiKeyDevice, YubiKeyFeature feature)
        {
            if (!HasFeature(yubiKeyDevice, feature))
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NotSupportedByYubiKeyVersion));
            }
        }

        // Checks to see if a particular application is available (meaning: paid-for, instead of simply enabled) on the
        // YubiKey over either USB or NFC.
        private static bool HasApplication(IYubiKeyDevice yubiKeyDevice, YubiKeyCapabilities capability) =>
            yubiKeyDevice.AvailableNfcCapabilities.HasFlag(capability) ||
            yubiKeyDevice.AvailableUsbCapabilities.HasFlag(capability);
    }
}
