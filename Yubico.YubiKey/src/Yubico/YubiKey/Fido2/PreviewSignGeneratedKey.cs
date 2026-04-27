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
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents the generated key material returned by the YubiKey during
    /// previewSign extension registration.
    /// </summary>
    /// <remarks>
    /// This class contains the key handle and public key components needed to
    /// perform offline ARKG key derivation via <see cref="DerivePublicKey"/>.
    /// </remarks>
    public sealed class PreviewSignGeneratedKey
    {
        /// <summary>
        /// Gets the key handle for the generated credential.
        /// </summary>
        public ReadOnlyMemory<byte> KeyHandle { get; init; }

        /// <summary>
        /// Gets the blinding public key component.
        /// </summary>
        public ReadOnlyMemory<byte> BlindingPublicKey { get; init; }

        /// <summary>
        /// Gets the KEM (Key Encapsulation Mechanism) public key component.
        /// </summary>
        public ReadOnlyMemory<byte> KemPublicKey { get; init; }

        /// <summary>
        /// Gets the algorithm identifier for the derived key.
        /// </summary>
        public CoseAlgorithmIdentifier DerivedKeyAlgorithm { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewSignGeneratedKey"/> class.
        /// </summary>
        /// <param name="keyHandle">The key handle.</param>
        /// <param name="blindingPublicKey">The blinding public key.</param>
        /// <param name="kemPublicKey">The KEM public key.</param>
        /// <param name="derivedKeyAlgorithm">The derived key algorithm.</param>
        internal PreviewSignGeneratedKey(
            ReadOnlyMemory<byte> keyHandle,
            ReadOnlyMemory<byte> blindingPublicKey,
            ReadOnlyMemory<byte> kemPublicKey,
            CoseAlgorithmIdentifier derivedKeyAlgorithm)
        {
            KeyHandle = keyHandle;
            BlindingPublicKey = blindingPublicKey;
            KemPublicKey = kemPublicKey;
            DerivedKeyAlgorithm = derivedKeyAlgorithm;
        }

        /// <summary>
        /// Derives a public key using the ARKG-P256 algorithm.
        /// </summary>
        /// <param name="ikm">Input keying material for derivation.</param>
        /// <param name="ctx">Context string for derivation.</param>
        /// <returns>A <see cref="PreviewSignDerivedKey"/> containing the derived public key and handles.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public PreviewSignDerivedKey DerivePublicKey(byte[] ikm, byte[] ctx)
        {
            throw new NotImplementedException();
        }
    }
}
