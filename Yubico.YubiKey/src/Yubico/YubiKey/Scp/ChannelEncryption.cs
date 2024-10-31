﻿// Copyright 2021 Yubico AB
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
using System.Buffers.Binary;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp
{
    internal static class ChannelEncryption
    {
        public static Memory<byte> EncryptData(ReadOnlySpan<byte> dataToEncrypt, ReadOnlySpan<byte> encryptionKey, int encryptionCounter)
        {
            // NB: Could skip this if the payload is empty (rather than sending a 16-byte encrypted '0x800000...' payload
            byte[] countBytes = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(countBytes, encryptionCounter);

            byte[] ivInput = new byte[16];
            countBytes.CopyTo(ivInput, 16 - countBytes.Length); // copy to rightmost part of block
            byte[] iv = AesUtilities.BlockCipher(encryptionKey.ToArray(), ivInput); //todo toarry

            var paddedPayload = Padding.PadToBlockSize(dataToEncrypt);
            var encryptedData = AesUtilities.AesCbcEncrypt(encryptionKey, iv, paddedPayload.Span);

            return encryptedData;
        }

        public static Memory<byte> DecryptData(ReadOnlySpan<byte> dataToDecrypt, ReadOnlySpan<byte> key, int encryptionCounter)
        {
            byte[] countBytes = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(countBytes, encryptionCounter);

            byte[] ivInput = new byte[16];
            countBytes.CopyTo(ivInput, 16 - countBytes.Length); // copy to rightmost part of block
            ivInput[0] = 0x80; // to mark as RMAC calculation
            byte[] iv = AesUtilities.BlockCipher(key, ivInput);

            var decryptedData = AesUtilities.AesCbcDecrypt(key, iv, dataToDecrypt);

            return Padding.RemovePadding(decryptedData.Span);
        }
    }
}
