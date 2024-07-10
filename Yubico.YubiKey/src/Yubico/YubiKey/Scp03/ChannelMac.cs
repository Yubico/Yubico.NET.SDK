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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Yubico.Core.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp03
{
    internal static class ChannelMac
    {
        public static (CommandApdu macdApdu, byte[] newMacChainingValue) MacApdu(
            CommandApdu apdu,
            byte[] macKey,
            byte[] macChainingValue)
        {
            if (macChainingValue.Length != 16)
            {
                throw new ArgumentException(ExceptionMessages.UnknownScp03Error, nameof(macChainingValue));
            }

            CommandApdu apduWithLongerLen = AddDataToApdu(apdu, new byte[8]);
            byte[] apduBytesWithZeroMac = ApduToBytes(apduWithLongerLen);
            byte[] apduBytes = apduBytesWithZeroMac.Take(apduBytesWithZeroMac.Length - 8).ToArray();
            byte[] macInp = new byte[16 + apduBytes.Length];
            macChainingValue.CopyTo(macInp, index: 0);
            apduBytes.CopyTo(macInp, index: 16);

            using ICmacPrimitives cmacObj =
                CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);

            cmacObj.CmacInit(macKey);
            cmacObj.CmacUpdate(macInp);
            cmacObj.CmacFinal(macChainingValue);

            return (AddDataToApdu(apdu, macChainingValue.Take(8).ToArray()), macChainingValue);
        }

        public static void VerifyRmac(byte[] response, byte[] rmacKey, byte[] macChainingValue)
        {
            if (response.Length < 8)
            {
                throw new SecureChannelException(ExceptionMessages.InsufficientResponseLengthToVerifyRmac);
            }

            if ((response.Length - 8) % 16 != 0)
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectResponseLengthToDecrypt);
            }

            int respDataLen = response.Length - 8;
            byte[] recvdRmac = response.Skip(response.Length - 8).ToArray();
            byte[] macInp = new byte[16 + respDataLen + 2];
            macChainingValue.CopyTo(macInp, index: 0);
            response.Take(respDataLen).ToArray().CopyTo(macInp, index: 16);

            // NB: this could support more status words, but devices only give RMACs w/ SW=0x9000
            macInp[16 + respDataLen] = SW1Constants.Success;
            macInp[16 + respDataLen + 1] = SWConstants.Success & 0xFF;

            using ICmacPrimitives cmacObj =
                CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);

            byte[] cmac = new byte[16];
            cmacObj.CmacInit(rmacKey);
            cmacObj.CmacUpdate(macInp);
            cmacObj.CmacFinal(cmac);
            Span<byte> calculatedRmac = cmac.AsSpan(start: 0, length: 8);

            if (!CryptographicOperations.FixedTimeEquals(recvdRmac, calculatedRmac))
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectRmac);
            }
        }

        private static byte[] ApduToBytes(CommandApdu apdu)
        {
            byte[] data = apdu.Data.ToArray();
            byte[] header = { apdu.Cla, apdu.Ins, apdu.P1, apdu.P2 };
            byte[] encodedLen = EncodeLen(data.Length);
            using var s = new MemoryStream();
            s.Write(header, offset: 0, header.Length);
            s.Write(encodedLen, offset: 0, encodedLen.Length);
            s.Write(data, offset: 0, data.Length);
            return s.ToArray();
        }

        private static CommandApdu AddDataToApdu(CommandApdu apdu, byte[] data)
        {
            var newApdu = new CommandApdu
            {
                Cla = apdu.Cla,
                Ins = apdu.Ins,
                P1 = apdu.P1,
                P2 = apdu.P2
            };

            int currentDataLength = apdu.Data.Length;
            byte[] newData = new byte[currentDataLength + data.Length];

            if (!apdu.Data.IsEmpty)
            {
                apdu.Data.ToArray().CopyTo(newData, index: 0);
            }

            data.CopyTo(newData, currentDataLength);
            newApdu.Data = newData;
            return newApdu;
        }

        private static byte[] EncodeLen(int len)
        {
            if (len <= 0xFF)
            {
                return new[] { (byte)len };
            }

            byte lenUpper = (byte)(len >> 8);
            byte lenLower = (byte)(len & 0xFF);
            return new byte[] { 0x00, lenUpper, lenLower };
        }
    }
}
