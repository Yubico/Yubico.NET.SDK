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
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    public sealed partial class Fido2Session
    {
        private PinUvAuthProtocolBase? _selectedPinProtocol;
        private Memory<byte>? _pinUvAuthToken;

        private const int PinMinimumByteLength = 4;
        private const int PinMaximumByteLength = 63;

        public void SetPin(PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
        {
            if (TrySetPin(protocol))
            {
                return;
            }

            throw new OperationCanceledException("The user cancelled the PIN collection operation.");
        }

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

                throw new InvalidOperationException("PIN is already set.");
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }
        }

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


        public void ChangePin(PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
        {
            if (TryChangePin(protocol))
            {
                return;
            }

            throw new OperationCanceledException("The user cancelled the PIN collection operation.");
        }

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

            throw new OperationCanceledException("The user cancelled the PIN collection operation.");
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

            SetAuthToken(response.GetData());

            if (response.Status == ResponseStatus.Success)
            {
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
                throw new ArgumentException("PIN too short.");
            }

            if (newPin.Length > PinMaximumByteLength)
            {
                throw new ArgumentException("PIN too long.");
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
                _ => throw new NotSupportedException($"PIN Protocol {protocol} is not supported.")
            };
        }

        private PinUvAuthProtocolBase GetCurrentPinProtocol() =>
            _selectedPinProtocol ?? throw new InvalidOperationException("No PIN protocol in use.");

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
