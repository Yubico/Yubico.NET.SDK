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
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class contains code for PIN operations.
    public sealed partial class Fido2Session
    {
        // The PIN protocol classes contain some state that will be useful
        // for other session operations.
        private PinUvAuthProtocolBase? _selectedPinProtocol;

        // Likewise, the auth token is our ticket to successfully authenticating
        // other session operations. This field should be cleared during the
        // disposal of the Fido2Session class.
        private Memory<byte>? _pinUvAuthToken;

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
        /// The PIN is binary data and must be at least 4 and no more than 63 bytes long. The encoding process for PINs
        /// is described in detail in the <xref href="TheFido2Pin">user's manual</xref>.
        /// </para>
        /// </remarks>
        /// <param name="protocol">
        /// The preferred PIN/UV authentication protocol to use. Leaving this parameter unspecified or passing in
        /// `None` will have the same effect - it will use the YubiKey's preferred protocol.
        /// </param>
        /// <exception cref="SecurityException">
        /// The YubiKey already has a PIN set. This function cannot be used to change the PIN.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        public void SetPin(PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
        {
            if (TrySetPin(protocol))
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.PinCollectionCancelled);
        }

        /// <summary>
        /// Tries to set the initial FIDO2 PIN using the <c>KeyCollector</c>. To change an existing PIN, use
        /// the <see cref="TryChangePin(Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocol)"/> function.
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
        /// The PIN is binary data and must be at least 4 and no more than 63 bytes long. The encoding process for PINs
        /// is described in detail in the <xref href="TheFido2Pin">user's manual</xref>.
        /// </para>
        /// </remarks>
        /// <param name="protocol">
        /// The preferred PIN/UV authentication protocol to use. Leaving this parameter unspecified or passing in
        /// `None` will have the same effect - it will use the YubiKey's preferred protocol.
        /// </param>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The YubiKey already has a PIN set. This function cannot be used to change the PIN.
        /// </exception>
        public bool TrySetPin(PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
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

                if (TrySetPin(keyEntryData.GetCurrentValue(), protocol))
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
        /// <see cref="TryChangePin(Yubico.YubiKey.Fido2.PinProtocols.PinUvAuthProtocol)"/> function.
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
        /// The PIN is binary data and must be at least 4 and no more than 63 bytes long. The encoding process for PINs
        /// is described in detail in the <xref href="TheFido2Pin">user's manual</xref>.
        /// </para>
        /// </remarks>
        /// <param name="newPin">
        /// The PIN to program onto the YubiKey. It must be encoded in UTF-8 with Normalization Form C. It must also
        /// be between 4 and 63 bytes long. Read more about PINs <xref href="TheFido2Pin">here</xref>.
        /// </param>
        /// <param name="protocol">
        /// The preferred PIN/UV authentication protocol to use. Leaving this parameter unspecified or passing in
        /// `None` will have the same effect - it will use the YubiKey's preferred protocol.
        /// </param>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the YubiKey has a PIN already set, and an exception for all
        /// other kinds of failures.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The YubiKey already has a PIN set. This function cannot be used to change the PIN.
        /// </exception>
        public bool TrySetPin(ReadOnlyMemory<byte> newPin, PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
        {
            AuthenticatorInfo info = GetAuthenticatorInfo();

            VerifyPinLengthRequirements(info, newPin);

            ObtainSharedSecret(info, protocol);

            SetPinResponse result = Connection.SendCommand(new SetPinCommand(GetCurrentPinProtocol(), newPin));

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
        /// The PIN is binary data and must be at least 4 and no more than 63 bytes long. The encoding process for PINs
        /// is described in detail in the <xref href="TheFido2Pin">user's manual</xref>.
        /// </para>
        /// </remarks>
        /// <param name="protocol">
        /// The preferred PIN/UV authentication protocol to use. Leaving this parameter unspecified or passing in
        /// `None` will have the same effect - it will use the YubiKey's preferred protocol.
        /// </param>
        /// <exception cref="OperationCanceledException">
        /// The user cancelled PIN collection. This happens when the application returns <c>false</c>
        /// in the <c>KeyCollector</c>.
        /// </exception>
        public void ChangePin(PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
        {
            if (TryChangePin(protocol))
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
        /// The PIN is binary data and must be at least 4 and no more than 63 bytes long. The encoding process for PINs
        /// is described in detail in the <xref href="TheFido2Pin">user's manual</xref>.
        /// </para>
        /// </remarks>
        /// <param name="protocol">
        /// The preferred PIN/UV authentication protocol to use. Leaving this parameter unspecified or passing in
        /// `None` will have the same effect - it will use the YubiKey's preferred protocol.
        /// </param>
        /// <returns>
        /// <c>True</c> on success, <c>False</c> if the user cancelled PIN collection, and an exception for all
        /// other kinds of failures.
        /// </returns>
        public bool TryChangePin(PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
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
                    if (TryChangePin(keyEntryData.GetCurrentValue(), keyEntryData.GetNewValue(), protocol))
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
        /// The new value for PIN to set on the YubiKey, encoded using UTF-8 in Normalization Form C.
        /// </param>
        /// <param name="protocol">
        /// The preferred PIN/UV authentication protocol to use. Leaving this parameter unspecified or passing in
        /// `None` will have the same effect - it will use the YubiKey's preferred protocol.
        /// </param>
        /// <returns></returns>
        /// <exception cref="Fido2Exception">
        /// The YubiKey returned an error indicating that the change PIN request could not be completed.
        /// </exception>
        public bool TryChangePin(
            ReadOnlyMemory<byte> currentPin,
            ReadOnlyMemory<byte> newPin,
            PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
        {
            AuthenticatorInfo info = GetAuthenticatorInfo();

            VerifyPinLengthRequirements(info, newPin);

            ObtainSharedSecret(info, protocol);

            ChangePinResponse result = Connection.SendCommand(new ChangePinCommand(
                GetCurrentPinProtocol(),
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

        public void VerifyPin(PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
        {
            if (TryVerifyPin(protocol))
            {
                return;
            }

            throw new OperationCanceledException(ExceptionMessages.PinCollectionCancelled);
        }

        public bool TryVerifyPin(PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
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
                    if (TryVerifyPin(keyEntryData.GetCurrentValue(), protocol))
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

        public bool TryVerifyPin(ReadOnlyMemory<byte> currentPin, PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
        {
            AuthenticatorInfo info = GetAuthenticatorInfo();

            ObtainSharedSecret(info, protocol);

            GetPinUvAuthTokenResponse response = Connection.SendCommand(
                new GetPinTokenCommand(GetCurrentPinProtocol(), currentPin));

            if (response.Status == ResponseStatus.Success)
            {
                SetAuthToken(response.GetData());
                return true;
            }

            if (GetCtapError(response) == CtapStatus.PinInvalid)
            {
                return false; // PIN is invalid
            }

            throw new Fido2Exception(response.StatusMessage);
        }

        private void SetAuthToken(Memory<byte>? token)
        {
            if (_pinUvAuthToken.HasValue)
            {
                CryptographicOperations.ZeroMemory(_pinUvAuthToken.Value.Span);
            }

            if (token.HasValue)
            {
                _pinUvAuthToken = token.Value;
            }
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

        private static PinUvAuthProtocolBase GetPreferredPinProtocol(AuthenticatorInfo info, PinUvAuthProtocol protocol)
        {
            if (protocol == PinUvAuthProtocol.None)
            {
                protocol = info.PinUvAuthProtocols != null
                    ? info.PinUvAuthProtocols[0]
                    : PinUvAuthProtocol.ProtocolTwo;
            }

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

        private PinUvAuthProtocolBase GetCurrentPinProtocol() =>
            _selectedPinProtocol ?? throw new InvalidOperationException(ExceptionMessages.NoActivePinProtocol);

        private CoseEcPublicKey GetPeerCoseKey()
        {
            GetKeyAgreementResponse keyAgreementResponse =
                Connection.SendCommand(new GetKeyAgreementCommand(GetCurrentPinProtocol().Protocol));

            CoseEcPublicKey peerCoseKey = keyAgreementResponse.GetData();
            return peerCoseKey;
        }


        private void ObtainSharedSecret(AuthenticatorInfo info, PinUvAuthProtocol preferredProtocol)
        {
            _selectedPinProtocol?.Dispose();
            _selectedPinProtocol = GetPreferredPinProtocol(info, preferredProtocol);
            _selectedPinProtocol.Initialize();

            CoseEcPublicKey peerCoseKey = GetPeerCoseKey();

            _selectedPinProtocol.Encapsulate(peerCoseKey);
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
