// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKey.Cryptography;

internal static class AesUtilities
{
    private const int BlockSizeBytes = 16;
    private const int BlockSizeBits = 128;

    /// <summary>
    ///     Computes the raw AES128 encryption of the input block using the specified key.
    /// </summary>
    /// <remarks>
    ///     This is not a secure authenticated encryption scheme.
    /// </remarks>
    /// <param name="encryptionKey">16-byte AES128 key</param>
    /// <param name="plaintext">16-byte input block</param>
    /// <returns>The 16-byte AES128 ciphertext</returns>
    public static Memory<byte> BlockCipher(ReadOnlySpan<byte> encryptionKey, ReadOnlySpan<byte> plaintext)
    {
        if (encryptionKey.IsEmpty)
        {
            throw new ArgumentNullException(nameof(encryptionKey));
        }

        if (encryptionKey.Length != BlockSizeBytes)
        {
            throw new ArgumentException(ExceptionMessages.IncorrectAesKeyLength, nameof(encryptionKey));
        }

        if (plaintext.Length != BlockSizeBytes)
        {
            throw new ArgumentException(ExceptionMessages.IncorrectPlaintextLength, nameof(plaintext));
        }

        byte[] aesObjKey = encryptionKey.ToArray();

        try
        {
            using var aesObj = CryptographyProviders.AesCreator();
            #pragma warning disable CA5358 // Allow the usage of cipher mode 'ECB'
            aesObj.Mode = CipherMode.ECB;
            #pragma warning restore CA5358
            aesObj.KeySize = BlockSizeBits;
            aesObj.BlockSize = BlockSizeBits;
            aesObj.Key = aesObjKey;
            aesObj.IV = new byte[BlockSizeBytes];
            aesObj.Padding = PaddingMode.None;
            #pragma warning disable CA5401 // Justification: Allow the symmetric encryption to use

            // a non-default initialization vector
            var encryptor = aesObj.CreateEncryptor();
            #pragma warning restore CA5401

            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            csEncrypt.Write(plaintext.ToArray(), 0, plaintext.Length);
            return msEncrypt.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesObjKey);
        }
    }

    /// <inheritdoc cref="BlockCipher(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    public static byte[] BlockCipher(byte[] encryptionKey, ReadOnlySpan<byte> plaintext) =>
        BlockCipher(encryptionKey.AsSpan(), plaintext).ToArray();

    /// <summary>
    ///     Computes the AES-CBC encryption of the input blocks using the specified key.
    /// </summary>
    /// <remarks>
    ///     This is not a secure authenticated encryption scheme. No padding occurs.
    /// </remarks>
    /// <param name="encryptionKey">16-byte AES128 key</param>
    /// <param name="iv">16-byte initialization vector (IV)</param>
    /// <param name="plaintext">Input blocks; must be a non-zero multiple of 16 bytes long</param>
    /// <returns>Ciphertext of the same length as the plaintext</returns>
    public static ReadOnlyMemory<byte> AesCbcEncrypt(
        ReadOnlySpan<byte> encryptionKey,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> plaintext)
    {
        if (encryptionKey.IsEmpty)
        {
            throw new ArgumentNullException(nameof(encryptionKey));
        }

        if (iv.IsEmpty)
        {
            throw new ArgumentNullException(nameof(iv));
        }

        if (encryptionKey.Length != BlockSizeBytes)
        {
            throw new ArgumentException(ExceptionMessages.IncorrectAesKeyLength, nameof(encryptionKey));
        }

        if (iv.Length != BlockSizeBytes)
        {
            throw new ArgumentException(ExceptionMessages.IncorrectIVLength, nameof(iv));
        }

        if (plaintext.Length > 0 && plaintext.Length % BlockSizeBytes != 0)
        {
            throw new ArgumentException(ExceptionMessages.IncorrectCiphertextLength, nameof(plaintext));
        }

        byte[] aesObjKey = encryptionKey.ToArray();
        byte[] aesObjIv = iv.ToArray();

        try
        {
            using var aesObj = CryptographyProviders.AesCreator();

            aesObj.Mode = CipherMode.CBC;
            aesObj.KeySize = BlockSizeBits;
            aesObj.BlockSize = BlockSizeBits;
            aesObj.Key = aesObjKey;
            aesObj.IV = aesObjIv;
            aesObj.Padding = PaddingMode.None;
            #pragma warning disable CA5401 // Justification: Allow the symmetric encryption to use a non-default initialization vector
            var encryptor = aesObj.CreateEncryptor();
            #pragma warning restore CA5401
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            csEncrypt.Write(plaintext.ToArray(), 0, plaintext.Length);
            return msEncrypt.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesObjKey);
            CryptographicOperations.ZeroMemory(aesObjIv);
        }
    }

    /// <inheritdoc cref="AesCbcEncrypt(ReadOnlySpan{byte}, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    public static byte[] AesCbcEncrypt(byte[] encryptionKey, byte[] iv, ReadOnlySpan<byte> plaintext) =>
        AesCbcEncrypt(encryptionKey.AsSpan(), iv, plaintext).ToArray();

    /// <summary>
    ///     Computes the AES-CBC decryption of the input blocks using the specified key.
    /// </summary>
    /// <remarks>
    ///     This is not a secure authenticated encryption scheme. No padding occurs.
    /// </remarks>
    /// <param name="decryptionKey">16-byte AES128 key</param>
    /// <param name="iv">16-byte initialization vector (IV)</param>
    /// <param name="ciphertext">Input blocks; must be a non-zero multiple of 16 bytes long</param>
    /// <returns>Plaintext of the same length as the ciphertext</returns>
    public static ReadOnlyMemory<byte> AesCbcDecrypt(
        ReadOnlySpan<byte> decryptionKey,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> ciphertext)
    {
        if (decryptionKey.IsEmpty)
        {
            throw new ArgumentNullException(nameof(decryptionKey));
        }

        if (iv.IsEmpty)
        {
            throw new ArgumentNullException(nameof(iv));
        }

        if (decryptionKey.Length != BlockSizeBytes)
        {
            throw new ArgumentException(ExceptionMessages.IncorrectAesKeyLength, nameof(decryptionKey));
        }

        if (iv.Length != BlockSizeBytes)
        {
            throw new ArgumentException(ExceptionMessages.IncorrectIVLength, nameof(iv));
        }

        if (ciphertext.Length > 0 && ciphertext.Length % BlockSizeBytes != 0)
        {
            throw new ArgumentException(ExceptionMessages.IncorrectCiphertextLength, nameof(ciphertext));
        }

        byte[] aesObjKey = decryptionKey.ToArray();
        byte[] aesObjIv = iv.ToArray();

        try
        {
            using var aesObj = CryptographyProviders.AesCreator();

            aesObj.Mode = CipherMode.CBC;
            aesObj.KeySize = BlockSizeBits;
            aesObj.BlockSize = BlockSizeBits;

            aesObj.Key = aesObjKey;
            aesObj.IV = aesObjIv;
            aesObj.Padding = PaddingMode.None;

            var decryptor = aesObj.CreateDecryptor();

            using var msDecrypt = new MemoryStream();
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write);

            csDecrypt.Write(ciphertext.ToArray(), 0, ciphertext.Length);
            return msDecrypt.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesObjKey);
            CryptographicOperations.ZeroMemory(aesObjIv);
        }
    }

    /// <inheritdoc cref="AesCbcDecrypt(ReadOnlySpan{byte}, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    public static byte[] AesCbcDecrypt(byte[] decryptionKey, byte[] iv, ReadOnlySpan<byte> ciphertext) =>
        AesCbcDecrypt(decryptionKey.AsSpan(), iv.AsSpan(), ciphertext).ToArray();
}
