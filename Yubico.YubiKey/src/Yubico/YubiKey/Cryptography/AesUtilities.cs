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
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography
{
    internal static class AesUtilities
    {
        public const int BlockSizeBytes = 16;
        public const int BlockSizeBits = 128;

        /// <summary>
        /// Computes the raw AES128 encryption of the input block using the specified key.
        /// </summary>
        /// <remarks>
        /// This is not a secure authenticated encryption scheme.
        /// </remarks>
        /// <param name="key">16-byte AES128 key</param>
        /// <param name="plaintext">16-byte input block</param>
        /// <returns>The 16-byte AES128 ciphertext</returns>
        public static byte[] BlockCipher(byte[] key, ReadOnlySpan<byte> plaintext)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key.Length != BlockSizeBytes)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectAesKeyLength, nameof(key));
            }

            if (plaintext.Length != BlockSizeBytes)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectPlaintextLength, nameof(plaintext));
            }

            byte[] ciphertext;

            using (Aes aesObj = CryptographyProviders.AesCreator())
            {
                #pragma warning disable CA5358 // Allow the usage of cipher mode 'ECB'
                aesObj.Mode = CipherMode.ECB;
                #pragma warning restore CA5358
                aesObj.KeySize = BlockSizeBits;
                aesObj.BlockSize = BlockSizeBits;
                aesObj.Key = key;
                aesObj.IV = new byte[BlockSizeBytes];
                aesObj.Padding = PaddingMode.None;
                #pragma warning disable CA5401 // Justification: Allow the symmetric encryption to use

                // a non-default initialization vector
                ICryptoTransform encryptor = aesObj.CreateEncryptor();
                #pragma warning restore CA5401
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plaintext.ToArray(), offset: 0, plaintext.Length);
                        ciphertext = msEncrypt.ToArray();
                    }
                }
            }

            return ciphertext;
        }

        /// <summary>
        /// Computes the AES-CBC encryption of the input blocks using the specified key.
        /// </summary>
        /// <remarks>
        /// This is not a secure authenticated encryption scheme. No padding occurs.
        /// </remarks>
        /// <param name="key">16-byte AES128 key</param>
        /// <param name="iv">16-byte initialization vector (IV)</param>
        /// <param name="plaintext">Input blocks; must be a non-zero multiple of 16 bytes long</param>
        /// <returns>Ciphertext of the same length as the plaintext</returns>
        public static byte[] AesCbcEncrypt(byte[] key, byte[] iv, ReadOnlySpan<byte> plaintext)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv is null)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            if (key.Length != BlockSizeBytes)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectAesKeyLength, nameof(key));
            }

            if (iv.Length != BlockSizeBytes)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectIVLength, nameof(iv));
            }

            if (plaintext.Length > 0 && plaintext.Length % BlockSizeBytes != 0)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectCiphertextLength, nameof(plaintext));
            }

            byte[] ciphertext;

            using (Aes aesObj = CryptographyProviders.AesCreator())
            {
                aesObj.Mode = CipherMode.CBC;
                aesObj.KeySize = BlockSizeBits;
                aesObj.BlockSize = BlockSizeBits;
                aesObj.Key = key;
                aesObj.IV = iv;
                aesObj.Padding = PaddingMode.None;
                #pragma warning disable CA5401 // Justification: Allow the symmetric encryption to use

                // a non-default initialization vector
                ICryptoTransform encryptor = aesObj.CreateEncryptor();
                #pragma warning restore CA5401
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plaintext.ToArray(), offset: 0, plaintext.Length);
                        ciphertext = msEncrypt.ToArray();
                    }
                }
            }

            return ciphertext;
        }

        /// <summary>
        /// Computes the AES-CBC decryption of the input blocks using the specified key.
        /// </summary>
        /// <remarks>
        /// This is not a secure authenticated encryption scheme. No padding occurs.
        /// </remarks>
        /// <param name="key">16-byte AES128 key</param>
        /// <param name="iv">16-byte initialization vector (IV)</param>
        /// <param name="ciphertext">Input blocks; must be a non-zero multiple of 16 bytes long</param>
        /// <returns>Plaintext of the same length as the ciphertext</returns>
        public static byte[] AesCbcDecrypt(byte[] key, byte[] iv, ReadOnlySpan<byte> ciphertext)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv is null)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            if (key.Length != BlockSizeBytes)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectAesKeyLength, nameof(key));
            }

            if (iv.Length != BlockSizeBytes)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectIVLength, nameof(iv));
            }

            if (ciphertext.Length > 0 && ciphertext.Length % BlockSizeBytes != 0)
            {
                throw new ArgumentException(ExceptionMessages.IncorrectCiphertextLength, nameof(ciphertext));
            }

            byte[] plaintext;

            using (Aes aesObj = CryptographyProviders.AesCreator())
            {
                aesObj.Mode = CipherMode.CBC;
                aesObj.KeySize = BlockSizeBits;
                aesObj.BlockSize = BlockSizeBits;
                aesObj.Key = key;
                aesObj.IV = iv;
                aesObj.Padding = PaddingMode.None;

                ICryptoTransform decryptor = aesObj.CreateDecryptor();

                using (var msDecrypt = new MemoryStream())
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(ciphertext.ToArray(), offset: 0, ciphertext.Length);
                        plaintext = msDecrypt.ToArray();
                    }
                }
            }

            return plaintext;
        }
    }
}
