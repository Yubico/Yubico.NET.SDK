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
    /// <para>
    /// This class contains the key handle and public key components needed to
    /// perform offline ARKG (Asynchronous Remote Key Generation) key derivation.
    /// Relying-party-side derivation and verification are the consuming
    /// application's responsibility and are not exposed by this SDK.
    /// </para>
    /// <para>
    /// Instances of this class are obtained by calling
    /// <see cref="MakeCredentialData.GetPreviewSignGeneratedKey"/> after creating
    /// a credential with the previewSign extension enabled via
    /// <see cref="MakeCredentialParameters.AddPreviewSignGenerateKeyExtension"/>.
    /// </para>
    /// <para>
    /// The generated key material enables offline derivation of multiple public
    /// keys from a single credential, each identified by a unique context string.
    /// The YubiKey can sign with any derived key when provided the corresponding
    /// ARKG key handle and context.
    /// </para>
    /// </remarks>
    public sealed class PreviewSignGeneratedKey
    {
        private readonly byte[] _blindingPublicKey;
        private readonly byte[] _kemPublicKey;

        /// <summary>
        /// Gets the key handle for the generated credential.
        /// </summary>
        public ReadOnlyMemory<byte> KeyHandle { get; init; }

        /// <summary>
        /// Gets the blinding public key component.
        /// </summary>
        public ReadOnlyMemory<byte> BlindingPublicKey => _blindingPublicKey;

        /// <summary>
        /// Gets the KEM (Key Encapsulation Mechanism) public key component.
        /// </summary>
        public ReadOnlyMemory<byte> KemPublicKey => _kemPublicKey;

        /// <summary>
        /// Gets the algorithm identifier for the derived key.
        /// </summary>
        public CoseAlgorithmIdentifier DerivedKeyAlgorithm { get; init; }

        /// <summary>
        /// Gets the attestation object returned by the authenticator during MakeCredential.
        /// </summary>
        public AttestationObject AttestationObject { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewSignGeneratedKey"/> class.
        /// </summary>
        /// <param name="keyHandle">The key handle.</param>
        /// <param name="blindingPublicKey">The blinding public key.</param>
        /// <param name="kemPublicKey">The KEM public key.</param>
        /// <param name="derivedKeyAlgorithm">The derived key algorithm.</param>
        /// <param name="attestationObject">The attestation object returned by the authenticator during MakeCredential.</param>
        internal PreviewSignGeneratedKey(
            ReadOnlyMemory<byte> keyHandle,
            ReadOnlyMemory<byte> blindingPublicKey,
            ReadOnlyMemory<byte> kemPublicKey,
            CoseAlgorithmIdentifier derivedKeyAlgorithm,
            AttestationObject attestationObject)
        {
            KeyHandle = keyHandle;
            _blindingPublicKey = blindingPublicKey.ToArray();
            _kemPublicKey = kemPublicKey.ToArray();
            DerivedKeyAlgorithm = derivedKeyAlgorithm;
            AttestationObject = attestationObject;
        }
    }
}
