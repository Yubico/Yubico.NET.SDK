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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Diagnostics;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2.PinProtocols;

/// <summary>
///     This class contains methods that perform the platform operations of
///     FIDO2's PIN/UV auth protocol two.
/// </summary>
public class PinUvAuthProtocolTwo : PinUvAuthProtocolBase
{
    private const int KeyLength = 32;
    private const int BlockSize = 16;
    private const int SaltLength = 32;
    private const int TrailingByteCount = 1;
    private const byte TrailingByte = 0x01;
    private const string InfoAes = "CTAP2 AES key";
    private const string InfoHmac = "CTAP2 HMAC key";
    private readonly byte[] _aesEncKey = new byte[KeyLength];
    private readonly byte[] _hmacAuthKey = new byte[KeyLength];
    private bool _disposed;

    /// <summary>
    ///     Constructs a new instance of <see cref="PinUvAuthProtocolTwo" />.
    /// </summary>
    public PinUvAuthProtocolTwo()
    {
        Protocol = PinUvAuthProtocol.ProtocolTwo;
    }

    /// <inheritdoc />
    public override byte[] Encrypt(byte[] plaintext, int offset, int length)
    {
        Guard.IsNotNull(plaintext, nameof(plaintext));

        return Encrypt(plaintext.AsMemory(offset, length));
    }

    /// <inheritdoc />
    public override byte[] Encrypt(ReadOnlyMemory<byte> plaintext)
    {
        if (EncryptionKey is null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidCallOrder));
        }

        if (plaintext.Length == 0 || plaintext.Length % BlockSize != 0)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.IncorrectPlaintextLength));
        }

        using var randomObject = CryptographyProviders.RngCreator();
        byte[] initVector = new byte[BlockSize];
        randomObject.GetBytes(initVector);

        using var aes = CryptographyProviders.AesCreator();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.IV = initVector;
        aes.Key = _aesEncKey;

        using var aesTransform = aes.CreateEncryptor();
        byte[] encryptedData = new byte[BlockSize + plaintext.Length];
        Array.Copy(initVector, 0, encryptedData, 0, BlockSize);
        _ = aesTransform.TransformBlock(plaintext.ToArray(), 0, plaintext.Length, encryptedData, BlockSize);

        return encryptedData;
    }

    /// <inheritdoc />
    public override byte[] Decrypt(byte[] ciphertext, int offset, int length)
    {
        Guard.IsNotNull(ciphertext, nameof(ciphertext));

        return Decrypt(ciphertext.AsMemory(offset, length));
    }

    /// <inheritdoc />
    public override byte[] Decrypt(ReadOnlyMemory<byte> ciphertext)
    {
        if (EncryptionKey is null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidCallOrder));
        }

        // The first BlockSize bytes are the IV, so there should be at least
        // 2 blocks.
        if (ciphertext.Length < 2 * BlockSize || ciphertext.Length % BlockSize != 0)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.IncorrectCiphertextLength));
        }

        byte[] initVector = new byte[BlockSize];
        ciphertext[..BlockSize].CopyTo(initVector);

        using var aes = CryptographyProviders.AesCreator();
        int length = ciphertext.Length;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.IV = initVector;
        aes.Key = _aesEncKey;

        using var aesTransform = aes.CreateDecryptor();
        byte[] decryptedData = new byte[length - BlockSize];
        _ = aesTransform.TransformBlock(ciphertext.ToArray(), BlockSize, length - BlockSize, decryptedData, 0);

        return decryptedData;
    }

    /// <inheritdoc />
    public override byte[] Authenticate(byte[] message)
    {
        if (AuthenticationKey is null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidCallOrder));
        }

        Guard.IsNotNull(message, nameof(message));

        return Authenticate(_hmacAuthKey, message);
    }

    /// <inheritdoc />
    public override byte[] Authenticate(byte[] keyData, byte[] message) =>
        Authenticate(keyData.AsMemory(), message.AsMemory());

    /// <inheritdoc />
    public override byte[] Authenticate(ReadOnlyMemory<byte> keyData, ReadOnlyMemory<byte> message)
    {
        Guard.IsNotNull(message, nameof(message));
        Guard.IsNotNull(keyData, nameof(keyData));

        byte[] keyBytes = keyData.ToArray();
        byte[] messageBytes = message.ToArray();

        using var hmacSha256 = CryptographyProviders.HmacCreator("HMACSHA256");

        hmacSha256.Key = keyBytes;
        return hmacSha256.ComputeHash(messageBytes);
    }

    /// <inheritdoc />
    protected override void DeriveKeys(byte[] sharedSecret)
    {
        Guard.IsNotNull(sharedSecret, nameof(sharedSecret));
        Guard.IsEqualTo(sharedSecret.Length, KeyLength, nameof(sharedSecret.Length));

        // Derive 64 bytes.
        // Call HKDF-SHA-256 twice, each time producing 32 bytes.

        // HKDF is two steps:
        //  Extract, where HMAC-SHA-256(salt, input keying material (IKM)) produces the pseudorandom key (PRK).
        //    salt is a 32-byte buffer containing only 00 bytes
        //    and IKM is the input, in this case buffer.
        //    For this round, the salt is the HMAC key
        //  Expand, where a sequence of HMAC operations will produce the output keying material (OKM).
        //    in this case, because the output of HMAC-SHA-256 is 32 bytes
        //    long and the requested length is 32, there will be only one
        //    HMAC operation:
        //    HMAC-SHA-256(PRK, info || 0x01)
        //    where info is one of the following values
        //      CTAP2 HMAC key   (0x43 54 41 ...)
        //      CTAP2 AES key
        // Perform HKDF twice,
        //  with the "HMAC" info to get the _hmacKey and
        //  with the "AES" info to get the _aesKey.

        byte[] prk = Array.Empty<byte>();
        byte[] salt = new byte[SaltLength];

        try
        {
            // Extract.
            using var hmacSha256 = CryptographyProviders.HmacCreator("HMACSHA256");

            hmacSha256.Key = salt;
            prk = hmacSha256.ComputeHash(sharedSecret);

            // Expand (Aes key)
            hmacSha256.Key = prk;
            byte[] infoAes = Encoding.ASCII.GetBytes(InfoAes);
            _ = hmacSha256.TransformBlock(infoAes, 0, infoAes.Length, null, 0);
            infoAes[0] = TrailingByte;
            _ = hmacSha256.TransformFinalBlock(infoAes, 0, TrailingByteCount);

            // Save the AES key.
            Array.Copy(hmacSha256.Hash, _aesEncKey, KeyLength);

            // Expand (HMAC key)
            byte[] infoHmac = Encoding.ASCII.GetBytes(InfoHmac);
            _ = hmacSha256.TransformBlock(infoHmac, 0, infoHmac.Length, null, 0);
            infoHmac[0] = TrailingByte;
            _ = hmacSha256.TransformFinalBlock(infoHmac, 0, TrailingByteCount);

            // Save the HMAC key.
            Array.Copy(hmacSha256.Hash, _hmacAuthKey, KeyLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(prk);
        }

        EncryptionKey = _aesEncKey;
        AuthenticationKey = _hmacAuthKey;
    }

    /// <summary>
    ///     Release resources, overwrite sensitive data.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                CryptographicOperations.ZeroMemory(_aesEncKey);
                CryptographicOperations.ZeroMemory(_hmacAuthKey);
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
