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

using System.Linq;
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

        public static byte[] Derive(byte dataDerivationConstant, byte outputLen, byte[] kdfKey, byte[] hostChallenge, byte[] cardChallenge)
        {
            byte[] macInp = new byte[32];
            macInp[11] = dataDerivationConstant;

            if (outputLen != 0x40 && outputLen != 0x80)
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectDerivationLength);
            }

            macInp[14] = outputLen;
            macInp[15] = 1;
            hostChallenge.CopyTo(macInp, 16);
            cardChallenge.CopyTo(macInp, 24);

            return Cmac.AesCmac(kdfKey, macInp);
        }

        public static byte[] DeriveCryptogram(byte dataDerivationConstant, byte[] key, byte[] hostChallenge, byte[] cardChallenge) =>
            Derive(dataDerivationConstant, 0x40, key, hostChallenge, cardChallenge);

        public static SessionKeys DeriveSessionKeysFromStaticKeys(StaticKeys staticKeys, byte[] hostChallenge, byte[] cardChallenge)
        {
            byte[] SMAC = Derive(DDC_SMAC, 0x80, staticKeys.ChannelMacKey.ToArray(), hostChallenge, cardChallenge);
            byte[] SENC = Derive(DDC_SENC, 0x80, staticKeys.ChannelEncryptionKey.ToArray(), hostChallenge, cardChallenge);
            byte[] SRMAC = Derive(DDC_SRMAC, 0x80, staticKeys.ChannelMacKey.ToArray(), hostChallenge, cardChallenge);
            return new SessionKeys(SMAC, SENC, SRMAC);
        }
    }
}
