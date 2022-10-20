// Copyright 2022 Yubico AB
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
using System.Security;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class contains code for PIN operations.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// The PIN protocol to use for all operations on this session instance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By default, the SDK will ask the YubiKey for its preferred protocol and will instantiate the appropriate
        /// protocol object. A user can also override the YubiKey's choice and provide their own protocol instance
        /// by calling the <see cref="SetAuthProtocol"/> method on the session class.
        /// </para>
        /// <para>
        /// This property's value will always point to the auth protocol that is currently being used by the session.
        /// </para>
        /// </remarks>
        public PinUvAuthProtocolBase AuthProtocol { get; private set; }

        // We need to dispose the automatically created AuthProtocol. If it's overridden by SetAuthProtocol, then
        // that method will dispose the original, set this to false, and update the reference to the new protocol.
        private bool _disposeAuthProtocol = true;

        /// <summary>
        /// The current PIN / UV Auth token, if present.
        /// </summary>
        /// <remarks>
        /// The PIN / UV Auth Token, or auth token for short, is created when (Try)VerifyPin or (Try)VerifyUv is called.
        /// The auth token may also have a set of permissions that restrict the use of the token. These permissions
        /// are specified when verifying the PIN or UV and are shown in the <see cref="AuthTokenPermissions"/> property.
        /// </remarks>
        public ReadOnlyMemory<byte>? AuthToken { get; private set; }

        /// <summary>
        /// The set of permissions assigned to the <see cref="AuthToken"/> that indicate which operations will be
        /// allowed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The permissions for an auth token are set when PIN or UV verification occur. Check this property
        /// to determine if you are able to perform a certain FIDO2 operation or not. If the auth token does not include
        /// the permission you require, you will need to perform PIN or UV verification again with the new permission
        /// set.
        /// </para>
        /// <para>
        /// Not all YubiKeys support auth tokens with permissions. To determine if this feature is available, check
        /// if the `pinUvAuthToken` option is present and `true` in <see cref="AuthenticatorInfo.Options"/>. If
        /// permissions are not supported, do not specify any permissions when verifying the PIN. Once the PIN has been
        /// verified, this property will be set to `null`.
        /// </para>
        /// </remarks>
        public PinUvAuthTokenPermissions? AuthTokenPermissions { get; private set; }

        private const int PinMinimumByteLength = 4;
        private const int PinMaximumByteLength = 63;

        /// <summary>
        /// Sets the initial FIDO2 PIN using the <c>KeyCollector</c>. To change an existing PIN, use
        /// the <see cref="ChangePin"/> function.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Yubikey is manufactured with no default PIN set on the FIDO2 application. To configure a YubiKey's
        /// initial PIN, use this function. This function will only succeed if no PIN is currently configured. To change
        /// an existing PIN, use the <see cref="ChangePin"/> method instead. Once set, a PIN cannot be removed
        /// without resetting the FIDO2 application. The reset operation will remove the PIN and clear all registered
        /// credentials.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN.
        /// <list type="bullet">
        /// <item><description>It must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>It must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>It must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <exception cref="SecurityException">
        /// The YubiKey already has a PIN set. This function cannot be used to change the PIN.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        public void SetPin()
        {
            if (TrySetPin())
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.PinCollectionCancelled);
        }

        /// <summary>
        /// Tries to set the initial FIDO2 PIN using the <c>KeyCollector</c>. To change an existing PIN, use
        /// the <see cref="TryChangePin()"/> function.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Yubikey is manufactured with no default PIN set on the FIDO2 application. To initially configure
        /// a PIN, use this function. This function will only succeed if no PIN is currently configured. To change
        /// an already set PIN, use the <see cref="ChangePin"/> method instead. Once set, a PIN cannot be removed
        /// without resetting the FIDO2 application. This operation will unset the PIN as well as clear all registered
        /// credentials.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN.
        /// <list type="bullet">
        /// <item><description>It must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>It must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>It must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The YubiKey already has a PIN set. This function cannot be used to change the PIN.
        /// </exception>
        public bool TrySetPin()
        {
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.SetFido2Pin,
            };

            try
            {
                if (!keyCollector(keyEntryData))
                {
                    return false; // User cancellation
                }

                if (TrySetPin(keyEntryData.GetCurrentValue()))
                {
                    return true;
                }

                throw new SecurityException(ExceptionMessages.PinAlreadySet);
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }
        }

        /// <summary>
        /// Tries to set the initial FIDO2 PIN. To change an existing PIN, use the
        /// <see cref="TryChangePin()"/> function.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Yubikey is manufactured with no default PIN set on the FIDO2 application. To initially configure
        /// a PIN, use this function. This function will only succeed if no PIN is currently configured. To change
        /// an already set PIN, use the <see cref="ChangePin"/> method instead. Once set, a PIN cannot be removed
        /// without resetting the FIDO2 application. This operation will unset the PIN as well as clear all registered
        /// credentials.
        /// </para>
        /// </remarks>
        /// <param name="newPin">
        /// The PIN to program onto the YubiKey. It must be encoded in UTF-8 with Normalization Form C. It must be
        /// at least 4 Unicode code points in length, and not to exceed 63 bytes in encoded length. Read more about
        /// PINs <xref href="TheFido2Pin">here</xref>.
        /// </param>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the YubiKey has a PIN already set, and an exception for all
        /// other kinds of failures.
        /// </returns>
        public bool TrySetPin(ReadOnlyMemory<byte> newPin)
        {
            AuthenticatorInfo info = GetAuthenticatorInfo();

            VerifyPinLengthRequirements(info, newPin);

            ObtainSharedSecret();

            SetPinResponse result = Connection.SendCommand(new SetPinCommand(AuthProtocol, newPin));

            if (result.Status == ResponseStatus.Success)
            {
                return true;
            }

            // Spec says "PinAuthInvalid" for PIN already set. YubiKey says "NotAllowed".
            if (GetCtapError(result) == CtapStatus.PinAuthInvalid ||
                GetCtapError(result) == CtapStatus.NotAllowed)
            {
                return false; // PIN is already set.
            }

            throw new Fido2Exception(result.StatusMessage);
        }

        /// <summary>
        /// Changes the PIN using the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// FIDO2 separates the action of setting the initial PIN from changing it. Use <see cref="SetPin"/> to set
        /// the first PIN, and use this method to change the PIN after that. In order to change the PIN, both
        /// the current PIN and the new PIN must be supplied. A PIN cannot be removed from FIDO2. The only way
        /// to clear the PIN is to reset the entire FIDO2 application, which will result in all credentials being
        /// removed.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN.
        /// <list type="bullet">
        /// <item><description>It must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>It must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>It must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the change PIN request could not be completed.
        /// </exception>
        public void ChangePin()
        {
            if (TryChangePin())
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.PinCollectionCancelled);
        }

        /// <summary>
        /// Tries to change the PIN using the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// FIDO2 separates the action of setting the initial PIN from changing it. Use <see cref="SetPin"/> to set
        /// the first PIN, and use this method to change the PIN after that. In order to change the PIN, both
        /// the current PIN and the new PIN must be supplied. A PIN cannot be removed from FIDO2. The only way
        /// to clear the PIN is to reset the entire FIDO2 application, which will result in all credentials being
        /// removed.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN.
        /// <list type="bullet">
        /// <item><description>It must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>It must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>It must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the change PIN request could not be completed.
        /// </exception>
        public bool TryChangePin()
        {
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.ChangeFido2Pin
            };

            try
            {
                while (keyCollector(keyEntryData))
                {
                    if (TryChangePin(keyEntryData.GetCurrentValue(), keyEntryData.GetNewValue()))
                    {
                        return true;
                    }

                    keyEntryData.IsRetry = true;
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }

            return false;
        }

        /// <summary>
        /// Tries to change the PIN.
        /// </summary>
        /// <remarks>
        /// <para>
        /// FIDO2 separates the action of setting the initial PIN from changing it. Use <see cref="SetPin"/> to set
        /// the first PIN, and use this method to change the PIN after that. In order to change the PIN, both
        /// the current PIN and the new PIN must be supplied. A PIN cannot be removed from FIDO2. The only way
        /// to clear the PIN is to reset the entire FIDO2 application, which will result in all credentials being
        /// removed.
        /// </para>
        /// </remarks>
        /// <param name="currentPin">
        /// The existing PIN encoded using UTF-8 in Normalization Form C.
        /// </param>
        /// <param name="newPin">
        /// The new PIN to program onto the YubiKey. The new PIN:
        /// <list type="bullet">
        /// <item><description>Must be encoded in UTF-8 with Normalization Form C.</description></item>
        /// <item><description>Must be at least 4 Unicode code points in length.</description></item>
        /// <item><description>Must not exceed 63 bytes in encoded length.</description></item>
        /// </list>
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </param>
        /// <returns>True, if successful. Otherwise false.</returns>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the change PIN request could not be completed.
        /// </exception>
        public bool TryChangePin(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin)
        {
            AuthenticatorInfo info = GetAuthenticatorInfo();

            VerifyPinLengthRequirements(info, newPin);

            ObtainSharedSecret();

            ChangePinResponse result = Connection.SendCommand(new ChangePinCommand(
                AuthProtocol,
                currentPin,
                newPin));

            if (result.Status == ResponseStatus.Success)
            {
                return true;
            }

            if (GetCtapError(result) == CtapStatus.PinInvalid)
            {
                return false; // PIN is invalid
            }

            throw new Fido2Exception(result.StatusMessage);
        }

        /// <summary>
        /// Verifies the PIN against the YubiKey using the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A YubiKey is manufactured with no PIN set on the FIDO2 application. A PIN must be set before a user
        /// can perform most FIDO2 operations. After a PIN has been set, it must be verified against the YubiKey
        /// before privileged operations can occur. This method will perform that verification.
        /// </para>
        /// <para>
        /// Unlike other applications in this SDK (such as PIV and OATH), the SDK will not automatically verify PIN or
        /// UV using the KeyCollector in methods like <see cref="MakeCredential"/> due to FIDO2's complex user
        /// verification process. Your application must call this method explicitly before attempting to perform a FIDO2
        /// operation that requires verification.
        /// </para>
        /// <para>
        /// This version of VerifyPin uses the <see cref="KeyCollector"/> delegate. You can read about key collectors
        /// in much more detail in the <xref href="UsersManualKeyCollector">user's manual entry</xref>.
        /// </para>
        /// <para>
        /// If the PIN was incorrectly entered, the SDK will automatically retry. The key collector will be called
        /// again allowing for another attempt at entry. Each time the key collector is called, the <c>IsRetry</c>
        /// member will be set to <c>true</c> and the <c>RetryCount</c> will be updated to reflect the number of
        /// retries left before the YubiKey blocks further PIN attempts. To cancel pin collection operations, simply
        /// return <c>false</c> in the handler for the key collector.
        /// </para>
        /// <para>
        /// The PIN, while often comprised of ASCII values, can in fact contain most Unicode characters. The PIN
        /// must be encoded as a byte array using a UTF-8 encoding in Normalized Form C. See the
        /// <xref href="TheFido2Pin">user's manual entry</xref> on FIDO2 PINs for more information.
        /// </para>
        /// </remarks>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do. This parameter is allowed only if the
        /// YubiKey contains the `pinUvAuthToken` option in <see cref="AuthenticatorInfo.Options"/>. If the YubiKey
        /// does not support this, leave the parameter `null`; the legacy <see cref="GetPinTokenCommand"/> will be used
        /// as a fallback.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional.
        /// </param>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        /// <exception cref="SecurityException">
        /// There are no retries remaining.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey does not have a PIN set.
        /// --- or ---
        /// This YubiKey does not support permissions on PIN / UV auth tokens.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the PIN verification request could not be completed.
        /// </exception>
        public void VerifyPin(PinUvAuthTokenPermissions? permissions = null, string? relyingPartyId = null)
        {
            if (TryVerifyPin(permissions, relyingPartyId))
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.PinCollectionCancelled);
        }

        /// <summary>
        /// Tries to verify the PIN against the YubiKey using the <c>KeyCollector</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A YubiKey is manufactured with no PIN set on the FIDO2 application. A PIN must be set before a user
        /// can perform most FIDO2 operations. After a PIN has been set, it must be verified against the YubiKey
        /// before privileged operations can occur. This method will perform that verification.
        /// </para>
        /// <para>
        /// Unlike other applications in this SDK (such as PIV and OATH), the SDK will not automatically verify PIN or
        /// UV using the KeyCollector in methods like <see cref="MakeCredential"/> due to FIDO2's complex user
        /// verification process. Your application must call this method explicitly before attempting to perform a FIDO2
        /// operation that requires verification.
        /// </para>
        /// <para>
        /// This version of TryVerifyPin uses the <see cref="KeyCollector"/> delegate. You can read about key collectors
        /// in much more detail in the <xref href="UsersManualKeyCollector">user's manual entry</xref>.
        /// </para>
        /// <para>
        /// If the PIN was incorrectly entered, the SDK will automatically retry. The key collector will be called
        /// again allowing for another attempt at entry. Each time the key collector is called, the <c>IsRetry</c>
        /// member will be set to <c>true</c> and the <c>RetryCount</c> will be updated to reflect the number of
        /// retries left before the YubiKey blocks further PIN attempts. To cancel pin collection operations, simply
        /// return <c>false</c> in the handler for the key collector.
        /// </para>
        /// <para>
        /// The PIN, while often comprised of ASCII values, can in fact contain most Unicode characters. The PIN
        /// must be encoded as a byte array using a UTF-8 encoding in Normalized Form C. See the
        /// <xref href="TheFido2Pin">user's manual entry</xref> on FIDO2 PINs for more information.
        /// </para>
        /// </remarks>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do. This parameter is allowed only if the
        /// YubiKey contains the `pinUvAuthToken` option in <see cref="AuthenticatorInfo.Options"/>. If the YubiKey
        /// does not support this, leave the parameter `null` and the legacy <see cref="GetPinTokenCommand"/> will be used
        /// as a fallback.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional.
        /// </param>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="SecurityException">
        /// There are no retries remaining.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey does not have a PIN set.
        /// --- or ---
        /// This YubiKey does not support permissions on PIN / UV auth tokens.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the PIN verification request could not be completed.
        /// </exception>
        public bool TryVerifyPin(PinUvAuthTokenPermissions? permissions = null, string? relyingPartyId = null)
        {
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyFido2Pin
            };

            try
            {
                while (keyCollector(keyEntryData))
                {
                    if (TryVerifyPin(keyEntryData.GetCurrentValue(), permissions, relyingPartyId, out int? retriesRemaining, out _))
                    {
                        return true;
                    }

                    keyEntryData.IsRetry = true;
                    keyEntryData.RetriesRemaining = retriesRemaining!; // If we are retrying, we know this won't be null.

                    if (keyEntryData.RetriesRemaining == 0)
                    {
                        throw new SecurityException(ExceptionMessages.Fido2NoMoreRetries);
                    }
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }

            return false;
        }

        /// <summary>
        /// Tries to verify the PIN against the YubiKey.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A YubiKey is manufactured with no PIN set on the FIDO2 application. A PIN must be set before a user
        /// can perform most FIDO2 operations. After a PIN has been set, it must be verified against the YubiKey
        /// before privileged operations can occur. This method will perform that verification.
        /// </para>
        /// <para>
        /// Unlike other applications in this SDK (such as PIV and OATH), the SDK will not automatically verify PIN or
        /// UV using the KeyCollector in methods like <see cref="MakeCredential"/> due to FIDO2's complex user
        /// verification process. Your application must call this method explicitly before attempting to perform a FIDO2
        /// operation that requires verification.
        /// </para>
        /// <para>
        /// This version of TryVerifyPin does not use the key collector. This method will only attempt to verify a
        /// single PIN and will not automatically retry. In this case, the method will return <c>false</c> if the
        /// PIN was incorrect. It will throw an exception in all other failure cases.
        /// </para>
        /// <para>
        /// The PIN, while often comprised of ASCII values, can in fact contain most Unicode characters. The PIN
        /// must be encoded as a byte array using a UTF-8 encoding in Normalized Form C. See the
        /// <xref href="TheFido2Pin">user's manual entry</xref> on FIDO2 PINs for more information.
        /// </para>
        /// </remarks>
        /// <param name="currentPin">
        /// The FIDO2 PIN that you wish to verify.
        /// </param>
        /// <param name="retriesRemaining">
        /// The number of PIN retries remaining before the FIDO2 application becomes locked.
        /// </param>
        /// <param name="rebootRequired">
        /// Indicates whether a reboot of the YubiKey (unplug and re-insert) is required before further PIN retries are
        /// allowed.
        /// </param>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do. This parameter is allowed only if the
        /// YubiKey contains the `pinUvAuthToken` option in <see cref="AuthenticatorInfo.Options"/>. If the YubiKey
        /// does not support this, this parameter must be `null` and the legacy <see cref="GetPinTokenCommand"/> will be
        /// used as a fallback.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional. If <paramref name="permissions"/> is `null`, this parameter must also be `null`.
        /// </param>
        /// <returns>
        /// <c>True</c> if the PIN successfully verified, <c>False</c> if the PIN was incorrect, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey does not have a PIN set.
        /// --- or ---
        /// This YubiKey does not support permissions on PIN / UV auth tokens.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the PIN verification request could not be completed.
        /// </exception>
        public bool TryVerifyPin(
            ReadOnlyMemory<byte> currentPin,
            PinUvAuthTokenPermissions? permissions,
            string? relyingPartyId,
            out int? retriesRemaining,
            out bool? rebootRequired)
        {
            AuthenticatorInfo info = GetAuthenticatorInfo();

            IYubiKeyCommand<GetPinUvAuthTokenResponse> command;

            if (!OptionEnabled(info, "clientPin"))
            {
                throw new InvalidOperationException(ExceptionMessages.Fido2NoPin);
            }

            ObtainSharedSecret();

            if (!permissions.HasValue)
            {
                command = new GetPinTokenCommand(AuthProtocol, currentPin);
            }
            else
            {
                if (!OptionEnabled(info, "pinUvAuthToken"))
                {
                    throw new InvalidOperationException(ExceptionMessages.Fido2PermsNotSupported);
                }

                command = new GetPinUvAuthTokenUsingPinCommand(
                    AuthProtocol,
                    currentPin,
                    permissions.Value,
                    relyingPartyId);
            }

            GetPinUvAuthTokenResponse response = Connection.SendCommand(command);

            if (response.Status == ResponseStatus.Success)
            {
                AuthToken = response.GetData();
                AuthTokenPermissions = permissions;

                retriesRemaining = null;
                rebootRequired = null;

                return true;
            }

            if (GetCtapError(response) == CtapStatus.PinInvalid)
            {
                (retriesRemaining, rebootRequired) = Connection.SendCommand(new GetPinRetriesCommand()).GetData();

                return false; // PIN is invalid
            }

            throw new Fido2Exception(response.StatusMessage);
        }

        /// <summary>
        /// Performs a User Verification (UV) check on the YubiKey using the onboard biometric sensor. This method is
        /// only supported on YubiKey Bio Series devices. Uses the KeyCollector for touch prompting.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A YubiKey is manufactured with no PIN and no biometric templates set. A PIN must be set before a user
        /// can register fingerprints. After a PIN has been set, a user can enroll one or more fingers using their
        /// platform or operating system's built in registration mechanism.
        /// </para>
        /// <para>
        /// Once both a PIN has been set and a fingerprint has been registered, a user can perform verification. This
        /// method initiates the biometric (or user verification) process. If the user cannot match a valid finger within
        /// the allowed number of retries, it is best practice to fall back to PIN verification.
        /// </para>
        /// <para>
        /// Unlike other applications in this SDK (such as PIV and OATH), the SDK will not automatically verify PIN or
        /// UV using the KeyCollector in methods like <see cref="MakeCredential"/> due to FIDO2's complex user
        /// verification process. Your application must call this method explicitly before attempting to perform a FIDO2
        /// operation that requires verification.
        /// </para>
        /// <para>
        /// If the YubiKey was unable to verify a registered fingerprint, the SDK will automatically retry. The key
        /// collector will be called again to notify your app that touch is required. Each time the key collector is
        /// called, the <c>IsRetry</c> member will be set to <c>true</c> and the <c>RetryCount</c> will be updated to
        /// reflect the number of retries left before the YubiKey blocks further UV attempts. To cancel UV collection
        /// operations, simply return <c>false</c> in the handler for the key collector. When the retries have been
        /// exhausted, a `SecurityException` will be thrown. This, along with user cancellation, are indicators that
        /// your application should switch to verification with PIN.
        /// </para>
        /// </remarks>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do. This parameter is allowed only if the
        /// YubiKey contains the `pinUvAuthToken` option in <see cref="AuthenticatorInfo.Options"/>. If the YubiKey
        /// does not support this, leave the parameter `null` and the legacy <see cref="GetPinTokenCommand"/> will be used
        /// as a fallback.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional.
        /// </param>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled UV collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        public void VerifyUv(PinUvAuthTokenPermissions? permissions, string? relyingPartyId = null)
        {
            if (TryVerifyPin(permissions, relyingPartyId))
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.PinCollectionCancelled);
        }

        /// <summary>
        /// Tries to Perform a User Verification (UV) check on the YubiKey using the onboard biometric sensor. This
        /// method is only supported on YubiKey Bio Series devices. Uses the KeyCollector for touch prompting.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A YubiKey is manufactured with no PIN and no biometric templates set. A PIN must be set before a user
        /// can register fingerprints. After a PIN has been set, a user can enroll one or more fingers using their
        /// platform or operating system's built in registration mechanism.
        /// </para>
        /// <para>
        /// Once both a PIN has been set and a fingerprint has been registered, a user can perform verification. This
        /// method initiates the biometric (or user verification) process. If the user cannot match a valid finger within
        /// the allowed number of retries, it is best practice to fall back to PIN verification.
        /// </para>
        /// <para>
        /// Unlike other applications in this SDK (such as PIV and OATH), the SDK will not automatically verify PIN or
        /// UV using the KeyCollector in methods like <see cref="MakeCredential"/> due to FIDO2's complex user
        /// verification process. Your application must call this method explicitly before attempting to perform a FIDO2
        /// operation thatwhen requires verification.
        /// </para>
        /// <para>
        /// If the YubiKey was unable to verify a registered fingerprint, the SDK will automatically retry. The key
        /// collector will be called again to notify your app that touch is required. Each time the key collector is
        /// called, the <c>IsRetry</c> member will be set to <c>true</c> and the <c>RetryCount</c> will be updated to
        /// reflect the number of retries left before the YubiKey blocks further UV attempts. To cancel UV collection
        /// operations, simply return <c>false</c> in the handler for the key collector. When the retries have been
        /// exhausted, a `SecurityException` will be thrown. This, along with user cancellation, are indicators that
        /// your application should switch to verification with PIN.
        /// </para>
        /// </remarks>
        /// <param name="permissions">
        /// The set of operations that this auth token should be permitted to do. This parameter is allowed only if the
        /// YubiKey contains the `pinUvAuthToken` option in <see cref="AuthenticatorInfo.Options"/>. If the YubiKey
        /// does not support this, leave the parameter `null` and the legacy <see cref="GetPinTokenCommand"/> will be used
        /// as a fallback.
        /// </param>
        /// <param name="relyingPartyId">
        /// Some <paramref name="permissions"/> require the qualification of a relying party ID. This parameter should
        /// only be specified when a permission requires it, otherwise it should be left null. See
        /// <see cref="PinUvAuthTokenPermissions"/> for more details on which permissions require the RP ID and for which
        /// it is optional.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey does not support onboard user-verification.
        /// </exception>
        public bool TryVerifyUv(PinUvAuthTokenPermissions permissions, string? relyingPartyId = null)
        {
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyFido2Uv
            };

            AuthenticatorInfo info = GetAuthenticatorInfo();

            if (!OptionEnabled(info, "pinUvAuthToken") || !OptionEnabled(info, "uv"))
            {
                throw new InvalidOperationException(ExceptionMessages.Fido2UvNotSupported);
            }

            try
            {
                // Inform key collector that we're asking for UV.
                while (keyCollector(keyEntryData))
                {
                    GetPinUvAuthTokenResponse response = Connection.SendCommand(
                        new GetPinUvAuthTokenUsingUvCommand(AuthProtocol, permissions, relyingPartyId));

                    if (response.Status == ResponseStatus.Success)
                    {
                        AuthToken = response.GetData();
                        AuthTokenPermissions = permissions;

                        return true;
                    }

                    if (GetCtapError(response) == CtapStatus.UvInvalid)
                    {
                        keyEntryData.IsRetry = true;
                        keyEntryData.RetriesRemaining = Connection.SendCommand(new GetUvRetriesCommand()).GetData();

                        if (keyEntryData.RetriesRemaining == 0)
                        {
                            throw new SecurityException(ExceptionMessages.Fido2NoMoreRetries);
                        }
                    }
                }
            }
            finally
            {
                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }

            return false;
        }

        /// <summary>
        /// Overrides the default PIN/UV Auth protocol (which is determined by the YubiKey and SDK).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Call this method with an instance of a class that derives <see cref="PinUvAuthProtocolBase"/> - either
        /// <see cref="PinUvAuthProtocolOne"/> or <seealso cref="PinUvAuthProtocolTwo"/>. When called, this method
        /// will replace the existing reference stored as <see cref="AuthProtocol"/> with the one that was passed
        /// to this function. The SDK will automatically dispose of the previous protocol if it was the default
        /// protocol created by the SDK. Otherwise, it will not. The caller of this function owns the lifetime of any
        /// protocol passed into the session - the SDK will not dispose of any user-set auth protocol.
        /// </para>
        /// <para>
        /// The auth protocol is not remembered across sessions. That is - if you call this method on session A, and
        /// later create a session B, the session B will be using the default auth protocol. You would need to call this
        /// method on both session instances to override the default behavior.
        /// </para>
        /// </remarks>
        /// <param name="authProtocol">
        /// The PIN/UV auth protocol instance to use for this session class.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The value specified by <c>authProtocol</c> is null.
        /// </exception>
        public void SetAuthProtocol(PinUvAuthProtocolBase authProtocol)
        {
            if (authProtocol is null)
            {
                throw new ArgumentNullException(nameof(authProtocol));
            }

            if (_disposeAuthProtocol)
            {
                AuthProtocol.Dispose();
                _disposeAuthProtocol = false; // Dispose no longer needed as caller now owns AuthProtocol.
            }

            AuthProtocol = authProtocol;
        }

        private static void VerifyPinLengthRequirements(AuthenticatorInfo info, ReadOnlyMemory<byte> newPin)
        {
            int minPinLengthInCodePoints = info.MinimumPinLength ?? PinMinimumByteLength;

            // Assumption - newPIN is already normalized
            if (newPin.Length < minPinLengthInCodePoints)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.PinTooShort,
                        minPinLengthInCodePoints,
                        "bytes",
                        newPin.Length));
            }

            if (newPin.Length > PinMaximumByteLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.PinTooLong,
                        PinMaximumByteLength,
                        "bytes",
                        newPin.Length));
            }
        }

        private PinUvAuthProtocolBase GetPreferredPinProtocol()
        {
            AuthenticatorInfo info = GetAuthenticatorInfo();
            PinUvAuthProtocol protocol = info.PinUvAuthProtocols?[0] ?? PinUvAuthProtocol.ProtocolOne;

            return protocol switch
            {
                PinUvAuthProtocol.ProtocolOne => new PinUvAuthProtocolOne(),
                PinUvAuthProtocol.ProtocolTwo => new PinUvAuthProtocolTwo(),
                _ => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.PinProtocolNotSupported,
                        protocol))
            };
        }

        private CoseEcPublicKey GetPeerCoseKey()
        {
            GetKeyAgreementResponse keyAgreementResponse =
                Connection.SendCommand(new GetKeyAgreementCommand(AuthProtocol.Protocol));

            CoseEcPublicKey peerCoseKey = keyAgreementResponse.GetData();
            return peerCoseKey;
        }


        private void ObtainSharedSecret()
        {
            if (AuthProtocol.PlatformPublicKey is null)
            {
                CoseEcPublicKey peerCoseKey = GetPeerCoseKey();
                AuthProtocol.Encapsulate(peerCoseKey);
            }
        }

        private Func<KeyEntryData, bool> EnsureKeyCollector()
        {
            if (KeyCollector is null)
            {
                throw new InvalidOperationException(ExceptionMessages.MissingKeyCollector);
            }

            return KeyCollector;
        }
    }
}
