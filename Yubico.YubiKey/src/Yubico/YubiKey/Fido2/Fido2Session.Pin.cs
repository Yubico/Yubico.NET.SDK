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
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    public sealed partial class Fido2Session
    {
        private PinUvAuthProtocolBase? _selectedPinProtocol;

        private const int PinMinimumByteLength = 4;
        private const int PinMaximumByteLength = 63;

        public void SetPin(PinUvAuthProtocol protocol = PinUvAuthProtocol.None)
        {
            if (TrySetPin())
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

                if (TrySetPin(keyEntryData.GetCurrentValue()))
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

            ObtainSharedSecret(info, protocol);

            SetPinResponse result = Connection.SendCommand(new SetPinCommand(GetCurrentPinProtocol(), newPin));

            if (result.Status == ResponseStatus.Success)
            {
                return true;
            }

            if (GetFido2Status(result) == Fido2Status.Ctap2ErrPinAuthInvalid)
            {
                return false; // PIN is already set.
            }

            throw new Fido2Exception(result.StatusMessage);
        }


        public void ChangePin()
        {
            throw new NotImplementedException();
        }

        public bool TryChangePin()
        {
            throw new NotImplementedException();
        }

        public bool TryChangePin(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin)
        {

        }

        public void VerifyPin()
        {
            throw new NotImplementedException();
        }

        public bool TryVerifyPin()
        {
            throw new NotImplementedException();
        }

        public bool TryVerifyPin(ReadOnlyMemory<byte> currentPin)
        {

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
