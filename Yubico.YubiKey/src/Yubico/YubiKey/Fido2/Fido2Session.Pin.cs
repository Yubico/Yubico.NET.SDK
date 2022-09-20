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
        /// This property's value will always point to the currently used auth protocol that is currently being
        /// used by the session.
        /// </para>
        /// </remarks>
        public PinUvAuthProtocolBase AuthProtocol { get; private set; }

        // Likewise, the auth token is our ticket to successfully authenticating
        // other session operations.
        private ReadOnlyMemory<byte>? _pinUvAuthToken;

        private const int PinMinimumByteLength = 4;
        private const int PinMaximumByteLength = 63;

        /// <summary>
        /// Sets the initial FIDO2 PIN using the <c>KeyCollector</c>. To change an existing PIN, use
        /// the <see cref="ChangePin"/> function.
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
        /// Several considerations must be made when collecting the PIN. It must be encoded in UTF-8 with Normalization
        /// Form C. It must be at least 4 Unicode code points in length, and not to exceed 64 bytes in encoded length.
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
        /// Several considerations must be made when collecting the PIN. It must be encoded in UTF-8 with Normalization
        /// Form C. It must be at least 4 Unicode code points in length, and not to exceed 64 bytes in encoded length.
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
        /// at least 4 Unicode code points in length, and not to exceed 64 bytes in encoded length. Read more about
        /// PINs <xref href="TheFido2Pin">here</xref>.
        /// </param>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the YubiKey has a PIN already set, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The YubiKey already has a PIN set. This function cannot be used to change the PIN.
        /// </exception>
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
        /// the PIN on first use, and use this method to change the PIN after that. In order to change the PIN both
        /// the current and the desired new PIN must be supplied. A PIN cannot be removed from FIDO2. The only way
        /// to clear the PIN is to reset the entire FIDO2 application, which will result in all credentials being
        /// removed.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN. It must be encoded in UTF-8 with Normalization
        /// Form C. It must be at least 4 Unicode code points in length, and not to exceed 64 bytes in encoded length.
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
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
        /// the PIN on first use, and use this method to change the PIN after that. In order to change the PIN both
        /// the current and the desired new PIN must be supplied. A PIN cannot be removed from FIDO2. The only way
        /// to clear the PIN is to reset the entire FIDO2 application, which will result in all credentials being
        /// removed.
        /// </para>
        /// <para>
        /// Several considerations must be made when collecting the PIN. It must be encoded in UTF-8 with Normalization
        /// Form C. It must be at least 4 Unicode code points in length, and not to exceed 64 bytes in encoded length.
        /// Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
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
        /// the PIN on first use, and use this method to change the PIN after that. In order to change the PIN both
        /// the current and the desired new PIN must be supplied. A PIN cannot be removed from FIDO2. The only way
        /// to clear the PIN is to reset the entire FIDO2 application, which will result in all credentials being
        /// removed.
        /// </para>
        /// <para>
        /// The PIN is binary data and must be at least 4 and no more than 63 bytes long. The encoding process for PINs
        /// is described in detail in the <xref href="TheFido2Pin">user's manual</xref>.
        /// </para>
        /// </remarks>
        /// <param name="currentPin">
        /// The existing PIN encoded using UTF-8 in Normalization Form C.
        /// </param>
        /// <param name="newPin">
        /// The PIN to program onto the YubiKey. It must be encoded in UTF-8 with Normalization Form C. It must be
        /// at least 4 Unicode code points in length, and not to exceed 64 bytes in encoded length. Read more about
        /// PINs <xref href="TheFido2Pin">here</xref>.
        /// </param>
        /// <returns></returns>
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
        /// can perform most FIDO2 operations. After a PIN has been set, it must be Verified against the YubiKey
        /// before privileged operations can occur. This method will perform that verification.
        /// </para>
        /// <para>
        /// The SDK will automatically verify the PIN when the YubiKey requests it, so in many circumstances, your
        /// app many not need to call this method directly. It can be advantageous to preempt the verification -
        /// for example, if it would provide a better user experience in your application to do so sooner. This
        /// method is available for those sorts of scenarios.
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
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        public void VerifyPin()
        {
            if (TryVerifyPin())
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
        /// can perform most FIDO2 operations. After a PIN has been set, it must be Verified against the YubiKey
        /// before privileged operations can occur. This method will perform that verification.
        /// </para>
        /// <para>
        /// The SDK will automatically verify the PIN when the YubiKey requests it, so in many circumstances, your
        /// app many not need to call this method directly. It can be advantageous to preempt the verification -
        /// for example, if it would provide a better user experience in your application to do so sooner. This
        /// method is available for those sorts of scenarios.
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
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
        public bool TryVerifyPin()
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
                    if (TryVerifyPin(keyEntryData.GetCurrentValue()))
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
        /// Verifies the PIN against the YubiKey.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A YubiKey is manufactured with no PIN set on the FIDO2 application. A PIN must be set before a user
        /// can perform most FIDO2 operations. After a PIN has been set, it must be Verified against the YubiKey
        /// before privileged operations can occur. This method will perform that verification.
        /// </para>
        /// <para>
        /// The SDK will automatically verify the PIN when the YubiKey requests it, so in many circumstances, your
        /// app many not need to call this method directly. It can be advantageous to preempt the verification -
        /// for example, if it would provide a better user experience in your application to do so sooner. This
        /// method is available for those sorts of scenarios.
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
        /// <returns>
        /// <c>True</c> if the PIN successfully verified, <c>False</c> if the PIN was incorrect, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the PIN verification request could not be completed.
        /// </exception>
        public bool TryVerifyPin(ReadOnlyMemory<byte> currentPin)
        {
            ObtainSharedSecret();

            GetPinUvAuthTokenResponse response = Connection.SendCommand(
                new GetPinTokenCommand(AuthProtocol, currentPin));

            if (response.Status == ResponseStatus.Success)
            {
                _pinUvAuthToken = response.GetData();
                return true;
            }

            if (GetCtapError(response) == CtapStatus.PinInvalid)
            {
                return false; // PIN is invalid
            }

            throw new Fido2Exception(response.StatusMessage);
        }

        /// <summary>
        /// Overrides the default PIN / UV Auth protocol determined by the YubiKey and SDK.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Call this method with an instance of a class that derives <see cref="PinUvAuthProtocolBase"/> - either
        /// <see cref="PinUvAuthProtocolOne"/> or <seealso cref="PinUvAuthProtocolTwo"/>. When called, this method
        /// will dispose of the instance referred to by the <see cref="AuthProtocol"/> property. The session will
        /// then take a reference to the instance provided by <paramref name="authProtocol"/> by setting <c>AuthProtocol</c>
        /// to this value. Finally, it will call the <c>Initialize</c> method on the <c>AuthProtocol</c>.
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

            AuthProtocol.Dispose();
            AuthProtocol = authProtocol;
            AuthProtocol.Initialize();
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
