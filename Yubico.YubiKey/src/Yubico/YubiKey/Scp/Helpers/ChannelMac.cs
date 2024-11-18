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
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp.Helpers
{
    internal static class ChannelMac
    {
        public static (CommandApdu macdApdu, byte[] newMacChainingValue) MacApdu(
            CommandApdu apdu,
            ReadOnlySpan<byte> macKey,
            ReadOnlySpan<byte> macChainingValue)
        {
            if (macChainingValue.Length != 16)
            {
                throw new ArgumentException(ExceptionMessages.UnknownScpError, nameof(macChainingValue));
            }

            var apduWithLongerLen = AddDataToApdu(apdu, new byte[8]);
            byte[] apduBytesWithZeroMac = apduWithLongerLen.AsByteArray();

            byte[] macInp = new byte[16 + apduBytesWithZeroMac.Length - 8];
            macChainingValue.CopyTo(macInp);
            apduBytesWithZeroMac.AsSpan(0, apduBytesWithZeroMac.Length - 8).CopyTo(macInp.AsSpan(16));

            byte[] newMacChainingValue = new byte[16];
            using var cmacObj = CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);
            cmacObj.CmacInit(macKey);
            cmacObj.CmacUpdate(macInp);
            cmacObj.CmacFinal(newMacChainingValue);

            var macdApdu = AddDataToApdu(apdu, newMacChainingValue.AsSpan(0, 8));
            return (macdApdu, newMacChainingValue);
        }

        public static void VerifyRmac(ReadOnlySpan<byte> response, ReadOnlySpan<byte> rmacKey, ReadOnlySpan<byte> macChainingValue)
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
            var recvdRmac = response[^8..];

            Span<byte> macInp = stackalloc byte[16 + respDataLen + 2];
            macChainingValue.CopyTo(macInp);
            response[..respDataLen].CopyTo(macInp[16..]);

            macInp[16 + respDataLen] = SW1Constants.Success;
            macInp[16 + respDataLen + 1] = SWConstants.Success & 0xFF;

            using var cmacObj = CryptographyProviders.CmacPrimitivesCreator(CmacBlockCipherAlgorithm.Aes128);
            Span<byte> cmac = stackalloc byte[16];
            cmacObj.CmacInit(rmacKey);
            cmacObj.CmacUpdate(macInp);
            cmacObj.CmacFinal(cmac);
    
            if (!CryptographicOperations.FixedTimeEquals(recvdRmac, cmac.Slice(0, 8)))
            {
                throw new SecureChannelException(ExceptionMessages.IncorrectRmac);
            }
        }

        private static CommandApdu AddDataToApdu(CommandApdu apdu, ReadOnlySpan<byte> data)
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
                apdu.Data.Span.CopyTo(newData);
            }

            data.CopyTo(newData.AsSpan(currentDataLength));
            newApdu.Data = newData;
            return newApdu;
        }
    }
}
