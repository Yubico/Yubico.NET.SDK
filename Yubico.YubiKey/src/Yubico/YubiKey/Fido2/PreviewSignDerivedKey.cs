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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents a derived public key produced by ARKG-P256 derivation.
    /// </summary>
    /// <remarks>
    /// This class contains the derived public key and handles needed for
    /// authentication via the previewSign extension.
    /// </remarks>
    public sealed class PreviewSignDerivedKey
    {
        /// <summary>
        /// Gets the derived public key.
        /// </summary>
        public ReadOnlyMemory<byte> PublicKey { get; init; }

        /// <summary>
        /// Gets the ARKG key handle.
        /// </summary>
        public ReadOnlyMemory<byte> ArkgKeyHandle { get; init; }

        /// <summary>
        /// Gets the device key handle from the original registration.
        /// </summary>
        public ReadOnlyMemory<byte> DeviceKeyHandle { get; init; }

        /// <summary>
        /// Gets the context string used for derivation.
        /// </summary>
        public ReadOnlyMemory<byte> Context { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewSignDerivedKey"/> class.
        /// </summary>
        /// <param name="publicKey">The derived public key.</param>
        /// <param name="arkgKeyHandle">The ARKG key handle.</param>
        /// <param name="deviceKeyHandle">The device key handle.</param>
        /// <param name="context">The context string.</param>
        internal PreviewSignDerivedKey(
            ReadOnlyMemory<byte> publicKey,
            ReadOnlyMemory<byte> arkgKeyHandle,
            ReadOnlyMemory<byte> deviceKeyHandle,
            ReadOnlyMemory<byte> context)
        {
            PublicKey = publicKey;
            ArkgKeyHandle = arkgKeyHandle;
            DeviceKeyHandle = deviceKeyHandle;
            Context = context;
        }

        /// <summary>
        /// Verifies a signature against the derived public key.
        /// </summary>
        /// <param name="message">The message that was signed.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns><c>true</c> if the signature is valid; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public bool VerifySignature(byte[] message, byte[] signature)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (signature is null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            // PublicKey is SEC1 uncompressed: 0x04 || X(32) || Y(32).
            if (PublicKey.Length != 65 || PublicKey.Span[0] != 0x04)
            {
                return false;
            }

            var verifier = new EcdsaVerify(PublicKey);
            return verifier.VerifyData(message, signature, isStandardSignature: true);
        }
    }
}
