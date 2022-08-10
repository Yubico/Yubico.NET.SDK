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
using System.IO;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class PinUvAuthProtocolOne : PinUvAuthProtocolBase
    {
        public PinUvAuthProtocolOne()
        {
            Protocol = PinUvAuthProtocol.ProtocolOne;
        }

        /// <inheritdoc />
        public override byte[] Encrypt(byte[] key, byte[] plaintext)
        {
            using Aes aes = CryptographyProviders.AesCreator();
            aes.Key = key;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var bwEncrypt = new BinaryWriter(csEncrypt);

            bwEncrypt.Write(plaintext);
            return msEncrypt.ToArray();
        }

        /// <inheritdoc />
        public override byte[] Decrypt(byte[] key, byte[] ciphertext)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (ciphertext is null)
            {
                throw new ArgumentNullException(nameof(ciphertext));
            }

            using Aes aes = CryptographyProviders.AesCreator();
            aes.Key = key;
            aes.IV = new byte[16];

            if (ciphertext.Length % aes.BlockSize != 0)
            {
                throw new ArgumentException("The cipherText is not a multiple of the AES block size.");
            }

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(ciphertext);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var brDecrypt = new BinaryReader(csDecrypt);

            return brDecrypt.ReadBytes(ciphertext.Length);
        }

        /// <inheritdoc />
        public override byte[] Authenticate(byte[] key, byte[] message)
        {
            using HMAC hmacSha256 = CryptographyProviders.HmacCreator("HMACSHA256");
            hmacSha256.Key = key;
            return hmacSha256.ComputeHash(message).AsMemory(0, 16).ToArray();
        }

        protected override byte[] DeriveKey(byte[] buffer)
        {
            using SHA256 sha256 = CryptographyProviders.Sha256Creator();
            return sha256.ComputeHash(buffer);
        }
    }
}
