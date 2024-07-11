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
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2.PinProtocols
{
    /// <summary>
    /// This class contains methods that perform the platform operations of
    /// FIDO2's PIN/UV auth protocol two.
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

        private bool _disposed;

        private readonly byte[] _aesKey = new byte[KeyLength];
        private readonly byte[] _hmacKey = new byte[KeyLength];

        /// <summary>
        /// Constructs a new instance of <see cref="PinUvAuthProtocolTwo"/>.
        /// </summary>
        public PinUvAuthProtocolTwo()
        {
            Protocol = PinUvAuthProtocol.ProtocolTwo;
        }

        /// <inheritdoc />
        public override byte[] Encrypt(byte[] plaintext, int offset, int length)
        {
            if (EncryptionKey is null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidCallOrder));
            }

            if (plaintext is null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            if (length == 0 || length % BlockSize != 0 || offset + length > plaintext.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectPlaintextLength));
            }

            // For protocol 2, generate a 16-byte, random IV, encrypt, then
            // return a buffer containing IV || ciphertext.

            byte[] initVector = new byte[BlockSize];
            using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            randomObject.GetBytes(initVector);

            using Aes aes = CryptographyProviders.AesCreator();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.IV = initVector;
            aes.Key = _aesKey;
            using ICryptoTransform aesTransform = aes.CreateEncryptor();

            byte[] encryptedData = new byte[BlockSize + length];
            Array.Copy(initVector, 0, encryptedData, 0, BlockSize);
            _ = aesTransform.TransformBlock(plaintext, offset, length, encryptedData, BlockSize);

            return encryptedData;
        }

        /// <inheritdoc />
        public override byte[] Decrypt(byte[] ciphertext, int offset, int length)
        {
            if (EncryptionKey is null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidCallOrder));
            }

            if (ciphertext is null)
            {
                throw new ArgumentNullException(nameof(ciphertext));
            }

            // The first BlockSize bytes are the IV, so there should be at least
            // 2 blocks.
            if (length < 2 * BlockSize || length % BlockSize != 0 || offset + length > ciphertext.Length)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectCiphertextLength));
            }

            // The first BlockSize bytes are the IV, decrypt the rest.
            byte[] initVector = new byte[BlockSize];
            Array.Copy(ciphertext, offset, initVector, 0, BlockSize);

            using Aes aes = CryptographyProviders.AesCreator();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.IV = initVector;
            aes.Key = _aesKey;
            using ICryptoTransform aesTransform = aes.CreateDecryptor();

            byte[] decryptedData = new byte[length - BlockSize];
            _ = aesTransform.TransformBlock(ciphertext, BlockSize + offset, length - BlockSize, decryptedData, 0);

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

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return Authenticate(_hmacKey, message);
        }

        /// <inheritdoc />
        protected override byte[] Authenticate(byte[] keyData, byte[] message)
        {
            using HMAC hmacSha256 = CryptographyProviders.HmacCreator("HMACSHA256");
            hmacSha256.Key = keyData;
            return hmacSha256.ComputeHash(message);
        }

        /// <inheritdoc />
        protected override void DeriveKeys(byte[] buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // Derive 64 bytes.
            // Call HKDF-SHA-256 twice, each time producing 32 bytes.

            // HKDF is two steps:
            //  Extract, where HMAC-SHA-256(salt, IKM) produces the PRK.
            //    salt is a 32-byte buffer containing only 00 bytes
            //    and IKM is the input, in this case buffer.
            //    For this round, the salt is the HMAC key
            //  Expand, where a sequence of HMAC operations will produce OKM.
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

            try
            {
                // Extract.
                byte[] salt = new byte[SaltLength];
                using HMAC hmacSha256 = CryptographyProviders.HmacCreator("HMACSHA256");
                hmacSha256.Key = salt;
                prk = hmacSha256.ComputeHash(buffer);

                // Expand (Aes key)
                hmacSha256.Key = prk;
                byte[] infoAes = Encoding.ASCII.GetBytes(InfoAes);
                _ = hmacSha256.TransformBlock(infoAes, 0, infoAes.Length, null, 0);
                infoAes[0] = TrailingByte;
                _ = hmacSha256.TransformFinalBlock(infoAes, 0, TrailingByteCount);

                Array.Copy(hmacSha256.Hash, _aesKey, KeyLength);

                // Expand (HMAC key)
                byte[] infoHmac = Encoding.ASCII.GetBytes(InfoHmac);
                _ = hmacSha256.TransformBlock(infoHmac, 0, infoHmac.Length, null, 0);
                infoHmac[0] = TrailingByte;
                _ = hmacSha256.TransformFinalBlock(infoHmac, 0, TrailingByteCount);

                Array.Copy(hmacSha256.Hash, _hmacKey, KeyLength);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(prk);
            }

            EncryptionKey = new ReadOnlyMemory<byte>(_aesKey);
            AuthenticationKey = new ReadOnlyMemory<byte>(_hmacKey);
        }

        /// <summary>
        /// Release resources, overwrite sensitive data.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CryptographicOperations.ZeroMemory(_aesKey);
                    CryptographicOperations.ZeroMemory(_hmacKey);
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
