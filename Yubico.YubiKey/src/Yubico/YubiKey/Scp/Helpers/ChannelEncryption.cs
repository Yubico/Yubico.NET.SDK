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
using System.Buffers.Binary;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp.Helpers
{
    internal static class ChannelEncryption
    {
        /// <summary>
        /// Encrypts the provided data using AES CBC mode with the given key and encryption counter.
        /// </summary>
        /// <param name="dataToEncrypt">The data to be encrypted.</param>
        /// <param name="encryptionKey">The AES key to use for encryption.</param>
        /// <param name="encryptionCounter">
        /// A counter used to generate the initialization vector (IV) for encryption.
        /// </param>
        /// <returns>
        /// A <see cref="Memory{T}"/> containing the encrypted data.
        /// </returns>
        public static ReadOnlyMemory<byte> EncryptData(
            ReadOnlySpan<byte> dataToEncrypt,
            ReadOnlySpan<byte> encryptionKey,
            int encryptionCounter)
        {
            // NB: Could skip this if the payload is empty (rather than sending a 16-byte encrypted '0x800000...' payload
            byte[] countBytes = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(countBytes, encryptionCounter);

            byte[] ivInput = new byte[16];
            countBytes.CopyTo(ivInput, 16 - countBytes.Length); // copy to rightmost part of block
            var iv = AesUtilities.BlockCipher(encryptionKey, ivInput);

            var paddedPayload = Padding.PadToBlockSize(dataToEncrypt);
            var encryptedData = AesUtilities.AesCbcEncrypt(encryptionKey, iv.Span, paddedPayload.Span);

            return encryptedData;
        }

        /// <summary>
        /// Decrypts the provided data using AES CBC mode with the given key and encryption counter.
        /// </summary>
        /// <param name="dataToDecrypt">The encrypted data to be decrypted.</param>
        /// <param name="key">The AES key to use for decryption.</param>
        /// <param name="encryptionCounter">
        /// A counter used to generate the initialization vector (IV) for decryption.
        /// </param>
        /// <returns>
        /// A <see cref="Memory{T}"/> containing the decrypted data with padding removed.
        /// </returns>
        public static ReadOnlyMemory<byte> DecryptData(
            ReadOnlySpan<byte> dataToDecrypt,
            ReadOnlySpan<byte> key,
            int encryptionCounter)
        {
            byte[] countBytes = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(countBytes, encryptionCounter);

            byte[] ivInput = new byte[16];
            countBytes.CopyTo(ivInput, 16 - countBytes.Length); // copy to rightmost part of block
            ivInput[0] = 0x80; // to mark as RMAC calculation

            var iv = AesUtilities.BlockCipher(key, ivInput);
            var decryptedData = AesUtilities.AesCbcDecrypt(key, iv.Span, dataToDecrypt);

            return Padding.RemovePadding(decryptedData.Span);
        }
    }
}
