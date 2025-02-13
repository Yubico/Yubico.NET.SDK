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

namespace Yubico.YubiKey.Scp03
{
    [Obsolete("Use new SessionKeys class instead.")]
    internal class SessionKeys : IDisposable
    {
        private readonly byte[] _sessionMacKey;
        private readonly byte[] _sessionEncryptionKey;
        private readonly byte[] _sessionRmacKey;

        private bool _disposed;

        // This copies a reference to the input keys and will clear them when
        // done.
        // Callers should not do anything with the buffers after a successful
        // instantiation.
        public SessionKeys(byte[] sessionMacKey, byte[] sessionEncryptionKey, byte[] sessionRmacKey)
        {
            _sessionMacKey = sessionMacKey;
            _sessionEncryptionKey = sessionEncryptionKey;
            _sessionRmacKey = sessionRmacKey;
            _disposed = false;
        }

        // Return a reference to the byte array containing the session Mac Key.
        public byte[] GetSessionMacKey() => _sessionMacKey;

        // Return a reference to the byte array containing the session Enc Key.
        public byte[] GetSessionEncKey() => _sessionEncryptionKey;

        // Return a reference to the byte array containing the session Rmac Key.
        public byte[] GetSessionRmacKey() => _sessionRmacKey;

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // Overwrite the memory of the keys
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CryptographicOperations.ZeroMemory(_sessionMacKey.AsSpan());
                    CryptographicOperations.ZeroMemory(_sessionEncryptionKey.AsSpan());
                    CryptographicOperations.ZeroMemory(_sessionRmacKey.AsSpan());

                    _disposed = true;
                }
            }
        }
    }
}
