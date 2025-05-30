﻿// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.Scp
{
    internal class SessionKeys : IDisposable
    {
        /// <summary>
        /// Gets the session MAC key.
        /// </summary>
        public ReadOnlyMemory<byte> MacKey => _macKey;

        /// <summary>
        /// Gets the session encryption key.    
        /// </summary>
        public ReadOnlyMemory<byte> EncKey => _encryptionKey;

        /// <summary>
        /// Gets the session RMAC key.
        /// </summary>
        public ReadOnlyMemory<byte> RmacKey => _rmacKey;

        /// <summary>
        /// Gets the session data encryption key (DEK).
        /// </summary>
        public ReadOnlyMemory<byte>? DataEncryptionKey => _dataEncryptionKey;

        private const byte KeySizeBytes = 16;
        private readonly Memory<byte> _macKey = new byte[KeySizeBytes];
        private readonly Memory<byte> _encryptionKey = new byte[KeySizeBytes];
        private readonly Memory<byte> _rmacKey = new byte[KeySizeBytes];
        private readonly Memory<byte> _dataEncryptionKey = new byte[KeySizeBytes];
        private bool _disposed;

        /// <summary>
        /// Session keys for Secure Channel Protocol (SCP).
        /// </summary>
        /// <param name="macKey">The session MAC key.</param>
        /// <param name="encryptionKey">The session encryption key.</param>
        /// <param name="rmacKey">The session RMAC key.</param>
        /// <param name="dataEncryptionKey">The session data encryption key. Optional.</param>
        public SessionKeys(
            ReadOnlyMemory<byte> macKey,
            ReadOnlyMemory<byte> encryptionKey,
            ReadOnlyMemory<byte> rmacKey,
            ReadOnlyMemory<byte> dataEncryptionKey)
        {
            ValidateKeyLength(macKey, nameof(macKey));
            ValidateKeyLength(encryptionKey, nameof(encryptionKey));
            ValidateKeyLength(rmacKey, nameof(rmacKey));
            ValidateKeyLength(dataEncryptionKey, nameof(dataEncryptionKey));

            macKey.CopyTo(_macKey);
            encryptionKey.CopyTo(_encryptionKey);
            rmacKey.CopyTo(_rmacKey);
            dataEncryptionKey.CopyTo(_dataEncryptionKey);
        }

        private static void ValidateKeyLength(ReadOnlyMemory<byte> key, string paramName)
        {
            if (key.Length != KeySizeBytes)
            {
                throw new ArgumentException("Incorrect session key length. Must be 16.", paramName);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // Overwrite the memory of the keys
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (!disposing)
            {
                return;
            }

            CryptographicOperations.ZeroMemory(_macKey.Span);
            CryptographicOperations.ZeroMemory(_encryptionKey.Span);
            CryptographicOperations.ZeroMemory(_rmacKey.Span);
            CryptographicOperations.ZeroMemory(_dataEncryptionKey.Span);

            _disposed = true;
        }
    }
}
