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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2.PinProtocols
{
    /// <summary>
    /// This class contains methods that perform the platform operations of
    /// FIDO2's PIN/UV auth protocol one.
    /// </summary>
    public class PinUvAuthProtocolOne : PinUvAuthProtocolBase
    {
        private const int KeyLength = 32;
        private const int BlockSize = 16;

        private bool _disposed;

        private readonly byte[] _keyData = new byte[KeyLength];

        /// <summary>
        /// Constructs a new instance of <see cref="PinUvAuthProtocolOne"/>.
        /// </summary>
        public PinUvAuthProtocolOne()
        {
            Protocol = PinUvAuthProtocol.ProtocolOne;
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
            if ((length < BlockSize) || ((length % BlockSize) != 0) || ((offset + length) > plaintext.Length))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectPlaintextLength));
            }

            using Aes aes = CryptographyProviders.AesCreator();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.IV = new byte[BlockSize];
            aes.Key = _keyData;
            using ICryptoTransform aesTransform = aes.CreateEncryptor();

            byte[] encryptedData = new byte[length];
            _ = aesTransform.TransformBlock(plaintext, offset, length, encryptedData, 0);

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
            if ((length == 0) || (length % BlockSize != 0) || (offset + length > ciphertext.Length))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectCiphertextLength));
            }

            using Aes aes = CryptographyProviders.AesCreator();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.IV = new byte[BlockSize];
            aes.Key = _keyData;
            using ICryptoTransform aesTransform = aes.CreateDecryptor();

            byte[] decryptedData = new byte[length];
            _ = aesTransform.TransformBlock(ciphertext, offset, length, decryptedData, 0);

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

            return Authenticate(_keyData, message);
        }

        /// <inheritdoc />
        protected override byte[] Authenticate(byte[] keyData, byte[] message)
        {
            using HMAC hmacSha256 = CryptographyProviders.HmacCreator("HMACSHA256");
            hmacSha256.Key = keyData;
            return hmacSha256.ComputeHash(message).AsMemory(0, 16).ToArray();
        }

        /// <inheritdoc />
        protected override void DeriveKeys(byte[] buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            using SHA256 sha256 = CryptographyProviders.Sha256Creator();
            _ = sha256.TransformFinalBlock(buffer, 0, buffer.Length);
            if (sha256.Hash.Length != KeyLength)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.CryptographyProviderFailure));
            }

            Array.Copy(sha256.Hash, _keyData, KeyLength);
            EncryptionKey = new ReadOnlyMemory<byte>(_keyData);
            AuthenticationKey = new ReadOnlyMemory<byte>(_keyData);
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
                    CryptographicOperations.ZeroMemory(_keyData);
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
