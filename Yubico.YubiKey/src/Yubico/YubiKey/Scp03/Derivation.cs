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
using Yubico.PlatformInterop;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp03
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
        // The result (output) will be 8 bytes (outputLenBits = 0x40 = 64 bits)
        // or 16 bytes (outputLen = 0x80 = 128 bits).
        public static byte[] Derive(
            byte dataDerivationConstant,
            byte outputLenBits,
            byte[] kdfKey,
            byte[] hostChallenge,
            byte[] cardChallenge)
        {
            if (outputLenBits != 0x40 && outputLenBits != 0x80)
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectDerivationLength);
            }

            if (hostChallenge.Length != 8 || cardChallenge.Length != 8)
            {
                throw new SecureChannelException(ExceptionMessages.InvalidChallengeLength);
            }

            byte[] macInp = new byte[32];
            macInp[11] = dataDerivationConstant;

            // This is the output length.
            macInp[14] = outputLenBits;
            macInp[15] = 1;
            hostChallenge.CopyTo(macInp, 16);
            cardChallenge.CopyTo(macInp, 24);

            byte[] cmac = new byte[16];
            using ICmacPrimitives cmacObj =
                CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);

            cmacObj.CmacInit(kdfKey);
            cmacObj.CmacUpdate(macInp);
            cmacObj.CmacFinal(cmac);

            if (outputLenBits == 0x80)
            {
                return cmac;
            }

            byte[] smallerResult = new byte[8];
            Array.Copy(cmac, 0, smallerResult, 0, 8);
            CryptographicOperations.ZeroMemory(cmac.AsSpan());
            return smallerResult;
        }

        public static byte[] DeriveCryptogram(
            byte dataDerivationConstant,
            byte[] key,
            byte[] hostChallenge,
            byte[] cardChallenge) =>
            Derive(dataDerivationConstant, 0x40, key, hostChallenge, cardChallenge);

        public static SessionKeys DeriveSessionKeysFromStaticKeys(
            StaticKeys staticKeys,
            byte[] hostChallenge,
            byte[] cardChallenge)
        {
            byte[] macKey = staticKeys.ChannelMacKey.ToArray();
            byte[] encKey = staticKeys.ChannelEncryptionKey.ToArray();

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
                byte[] SMAC = Derive(DDC_SMAC, 0x80, macKey, hostChallenge, cardChallenge);
                byte[] SENC = Derive(DDC_SENC, 0x80, encKey, hostChallenge, cardChallenge);
                byte[] SRMAC = Derive(DDC_SRMAC, 0x80, macKey, hostChallenge, cardChallenge);

                return new SessionKeys(SMAC, SENC, SRMAC);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(macKey.AsSpan());
                CryptographicOperations.ZeroMemory(encKey.AsSpan());
            }
        }
    }
}
