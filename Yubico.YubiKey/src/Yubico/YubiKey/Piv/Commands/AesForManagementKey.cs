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
using Yubico.YubiKey.Cryptography;


namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Perform AES operations.
    /// &gt; [!WARNING]
    /// &gt; This is not a general purpose class and is specifically tailored for
    /// &gt; PIV management key operations. Do not use this class anywhere else.
    /// </summary>
    /// <remarks>
    /// The reason this class exists is because we need a special Triple-DES for
    /// the PIV management key. Rather than using if clauses everywhere, we have
    /// an interface to define the symmetric algorithm and implement one for 3DES
    /// and another for AES.
    /// </remarks>
    [Obsolete("This should only be used for PIV management key operations and nowhere else.")]
    internal sealed class AesForManagementKey : ISymmetricForManagementKey
    {
        private const int AesBlockSize = 16;

        private readonly ICryptoTransform _cryptoTransform;
        private bool _disposed;

        /// <inheritdoc/>
        public bool IsEncrypting { get; }

        /// <inheritdoc/>
        public int BlockSize { get; }

        /// <summary>
        /// Create a new instance of <c>AesForManagementKey</c> using the given
        /// management key. Make sure the key is the same length as
        /// <c>expectedKeyLength</c>.
        /// </summary>
        /// <param name="managementKey">
        /// The bytes of the management key. This key must be
        /// <c>expectedKeyLength</c> bytes long.
        /// </param>
        /// <param name="expectedKeyLength">
        /// How long the key should be, in bytes.
        /// </param>
        /// <param name="isEncrypting">
        /// Indicates whether the object should be built to encrypt (if it is
        /// <c>true</c>), or to decrypt (if it is <c>false</c>).
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <c>managementKey</c> argument is not <c>expectedKeyLength</c>
        /// bytes long.
        /// </exception>
        public AesForManagementKey(ReadOnlySpan<byte> managementKey, int expectedKeyLength, bool isEncrypting)
        {
            IsEncrypting = isEncrypting;
            BlockSize = AesBlockSize;
            _disposed = false;

            if (managementKey.Length != expectedKeyLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectAesKeyLength));
            }

            using var aesObject = CryptographyProviders.AesCreator();
#pragma warning disable CA5358 // Allow the usage of cipher mode 'ECB' per the standard
            aesObject.Mode = CipherMode.ECB;
#pragma warning restore CA5358
            aesObject.Padding = PaddingMode.None;

            byte[] keyData = Array.Empty<byte>();

            try
            {
                keyData = managementKey.ToArray();
#pragma warning disable CA5401 // Allow null IV because we're in ECB
                _cryptoTransform = isEncrypting ?
                    aesObject.CreateEncryptor(keyData, null) :
                    aesObject.CreateDecryptor(keyData, null);
#pragma warning restore CA5401
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyData);
            }
        }

        /// <inheritdoc/>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputBuffer is null)
            {
                throw new ArgumentNullException(nameof(inputBuffer));
            }
            if (outputBuffer is null)
            {
                throw new ArgumentNullException(nameof(outputBuffer));
            }
            if (inputCount == 0 || (inputCount & 7) != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectPlaintextLength));
            }
            if (inputOffset < 0 || inputBuffer.Length - inputOffset < inputCount ||
                outputOffset < 0 || outputBuffer.Length - outputOffset < inputCount)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidOutputBuffer));
            }
            EnsureNotDisposed();

            _ = _cryptoTransform.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);

            return inputCount;
        }

        /// <summary>
        /// When the object goes out of scope, this method is called. It will
        /// dispose local objects.
        /// </summary>
        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        //
        // However, that does not apply to sealed classes.
        // So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cryptoTransform.Dispose();
            _disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TripleDesForManagementKey));
            }
        }
    }
}
