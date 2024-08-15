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
using System.Security.Cryptography;
using Yubico.Core.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey.Scp
{
    internal static class Derivation
    {
        public const byte DDC_SENC = 0x04;
        public const byte DDC_SMAC = 0x06;
        public const byte DDC_SRMAC = 0x07;
        public const byte DDC_CARD_CRYPTOGRAM = 0x00;
        public const byte DDC_HOST_CRYPTOGRAM = 0x01;

        // Derive a key from the challenges.
        // This method only supports deriving a 64- or 128-bit result based on
        // challenges each of which must be 8 bytes.
        // The result (output) will be 8 bytes (outputLenBits = 64 bits)
        // or 16 bytes (outputLen = 128 bits).
        public static Memory<byte> Derive(
            byte dataDerivationConstant,
            byte outputLenBits,
            ReadOnlySpan<byte> kdfKey,
            ReadOnlySpan<byte> hostChallenge,
            ReadOnlySpan<byte> cardChallenge)
        {
            if (outputLenBits != 64 && outputLenBits != 128)
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectDerivationLength);
            }
            if (hostChallenge.Length != 8 || cardChallenge.Length != 8)
            {
                throw new SecureChannelException(ExceptionMessages.InvalidChallengeLength);
            }

            Span<byte> macInp = stackalloc byte[32];
            macInp[11] = dataDerivationConstant;

            // This is the output length.
            macInp[14] = outputLenBits;
            macInp[15] = 1;
            hostChallenge.CopyTo(macInp.Slice(16, 8));
            cardChallenge.CopyTo(macInp.Slice(24, 8));

            Span<byte> cmac = stackalloc byte[16];
            using var cmacObj = CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);
            cmacObj.CmacInit(kdfKey);
            cmacObj.CmacUpdate(macInp);
            cmacObj.CmacFinal(cmac);

            if (outputLenBits == 128) // Output is a 128 bit key
            {
                return cmac.ToArray();
            }

            // Output is a cryptogram
            byte[] smallerResult = new byte[8];
            cmac[..8].CopyTo(smallerResult);

            CryptographicOperations.ZeroMemory(cmac);

            return smallerResult;
        }

        public static Memory<byte> DeriveCryptogram(
            byte dataDerivationConstant,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> hostChallenge,
            ReadOnlySpan<byte> cardChallenge) => Derive(dataDerivationConstant, 64, key, hostChallenge, cardChallenge);

        public static SessionKeys DeriveSessionKeysFromStaticKeys(
            StaticKeys staticKeys,
            ReadOnlySpan<byte> hostChallenge,
            ReadOnlySpan<byte> cardChallenge)
        {
            Span<byte> macKey = stackalloc byte[staticKeys.ChannelMacKey.Length];
            Span<byte> encKey = stackalloc byte[staticKeys.ChannelEncryptionKey.Length];
            staticKeys.ChannelMacKey.Span.CopyTo(macKey);
            staticKeys.ChannelEncryptionKey.Span.CopyTo(encKey);

            try
            {
                // If these calls succeed, then control of the created buffers
                // will be given to the new SessionKeys object created. That is,
                // if these succeed, don't overwrite the returned sensitive data.
                // The Derive call can throw an exception. Normally, we would
                // want to catch that exception just in case at least one call
                // succeeded and we have some sensitive data to overwrite. But
                // the Derive call fails if either the host or card challenge
                // is not exactly 8 bytes. In that case, the first call would
                // fail before generating a result, so there will be no data to
                // overwrite.
                var SMAC = Derive(DDC_SMAC, 128, macKey, hostChallenge, cardChallenge);
                var SENC = Derive(DDC_SENC, 128, encKey, hostChallenge, cardChallenge);
                var SRMAC = Derive(DDC_SRMAC, 128, macKey, hostChallenge, cardChallenge);

                return new SessionKeys(SMAC, SENC, SRMAC);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(macKey);
                CryptographicOperations.ZeroMemory(encKey);
            }
        }
    }
}
