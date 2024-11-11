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
using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKey.Scp.Commands;

namespace Yubico.YubiKey.Scp
{
    internal class Scp03State : ScpState
    {
        private readonly ReadOnlyMemory<byte> _hostCryptogram;

        public Scp03State(
            SessionKeys sessionKeys,
            Memory<byte> hostCryptogram)
            : base(sessionKeys, new Memory<byte>(new byte[16]))
        {
            _hostCryptogram = hostCryptogram;
        }

        internal static Scp03State CreateScpState(
            IApduTransform pipeline,
            Scp03KeyParameters keyParameters,
            ReadOnlyMemory<byte> hostChallenge)
        {
            var (cardChallenge, cardCryptogram) = PerformInitializeUpdate(pipeline, keyParameters, hostChallenge);
            var state = CreateScpState(keyParameters, hostChallenge, cardChallenge, cardCryptogram);
            state.PerformExternalAuthenticate(pipeline);

            return state;
        }

        private static Scp03State CreateScpState(
            Scp03KeyParameters keyParameters,
            ReadOnlyMemory<byte> hostChallenge,
            ReadOnlyMemory<byte> cardChallenge,
            ReadOnlyMemory<byte> cardCryptogram)
        {
            // Derive session keys
            var sessionKeys = Derivation.DeriveSessionKeysFromStaticKeys(
                keyParameters.StaticKeys,
                hostChallenge.Span,
                cardChallenge.Span);

            // Check supplied card cryptogram
            var calculatedCardCryptogram = Derivation.DeriveCryptogram(
                Derivation.DDC_CARD_CRYPTOGRAM,
                sessionKeys.MacKey.Span,
                hostChallenge.Span,
                cardChallenge.Span);

            if (!CryptographicOperations.FixedTimeEquals(cardCryptogram.Span, calculatedCardCryptogram.Span))
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectCardCryptogram);
            }

            // Calculate host cryptogram
            var hostCryptogram = Derivation.DeriveCryptogram(
                Derivation.DDC_HOST_CRYPTOGRAM,
                sessionKeys.MacKey.Span,
                hostChallenge.Span,
                cardChallenge.Span);

            return new Scp03State(sessionKeys, hostCryptogram);
        }

        private static (ReadOnlyMemory<byte> cardChallenge, ReadOnlyMemory<byte> cardCryptogram)
            PerformInitializeUpdate(
            IApduTransform pipeline,
            Scp03KeyParameters keyParameters,
            ReadOnlyMemory<byte> hostChallenge)
        {
            var initializeUpdateCommand = new InitializeUpdateCommand(
                keyParameters.KeyReference.VersionNumber, hostChallenge);

            var initializeUpdateResponseApdu = pipeline.Invoke(
                initializeUpdateCommand.CreateCommandApdu(),
                typeof(InitializeUpdateCommand),
                typeof(InitializeUpdateResponse));

            var initializeUpdateResponse = initializeUpdateCommand.CreateResponseForApdu(initializeUpdateResponseApdu);
            initializeUpdateResponse.ThrowIfFailed($"Error when performing {initializeUpdateCommand.GetType().Name}: {initializeUpdateResponse.StatusMessage}");

            var cardChallenge = initializeUpdateResponse.CardChallenge.ToArray().AsMemory();
            var cardCryptogram = initializeUpdateResponse.CardCryptogram.ToArray().AsMemory();

            return (cardChallenge, cardCryptogram);
        }

        private void
            PerformExternalAuthenticate(
            IApduTransform pipeline)
        {
            // Create a MAC:ed APDU
            var eaCommandPlain = new ExternalAuthenticateCommand(_hostCryptogram);
            (var macdApdu, byte[] newMacChainingValue) = MacApdu(
                eaCommandPlain.CreateCommandApdu(),
                SessionKeys.MacKey.ToArray(),
                MacChainingValue.ToArray()
                );

            // Update the states MacChainingValue
            MacChainingValue = newMacChainingValue;

            // Send command
            var eaCommandMaced = new ExternalAuthenticateCommand(macdApdu.Data.ToArray());
            var eaResponseApdu = pipeline.Invoke(
                eaCommandMaced.CreateCommandApdu(),
                typeof(ExternalAuthenticateCommand),
                typeof(ExternalAuthenticateResponse));

            var externalAuthenticateResponse = eaCommandMaced.CreateResponseForApdu(eaResponseApdu);
            externalAuthenticateResponse.ThrowIfFailed();
        }
    }
}
