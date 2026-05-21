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
    /// Instances of this class are obtained by decoding previewSign extension
    /// output after creating a credential with previewSign enabled.
    /// </remarks>
    public sealed class PreviewSignGeneratedKey
    {
        private readonly byte[] _publicKey;

        /// <summary>
        /// Gets the key handle for the generated signing key.
        /// </summary>
        public ReadOnlyMemory<byte> KeyHandle { get; init; }

        /// <summary>
        /// Gets the CBOR-encoded COSE public key returned by the authenticator.
        /// Relying parties decode this to obtain the generated signing key's public key material.
        /// </summary>
        public ReadOnlyMemory<byte> PublicKey => _publicKey;

        /// <summary>
        /// Gets the algorithm identifier for the generated key.
        /// </summary>
        public CoseAlgorithmIdentifier Algorithm { get; init; }

        /// <summary>
        /// Gets the attestation object returned by the authenticator during MakeCredential.
        /// </summary>
        public AttestationObject AttestationObject { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewSignGeneratedKey"/> class.
        /// </summary>
        /// <param name="keyHandle">The key handle.</param>
        /// <param name="publicKey">The CBOR-encoded COSE public key.</param>
        /// <param name="algorithm">The algorithm identifier for the generated key.</param>
        /// <param name="attestationObject">The attestation object returned by the authenticator during MakeCredential.</param>
        internal PreviewSignGeneratedKey(
            ReadOnlyMemory<byte> keyHandle,
            ReadOnlyMemory<byte> publicKey,
            CoseAlgorithmIdentifier algorithm,
            AttestationObject attestationObject)
        {
            KeyHandle = keyHandle;
            _publicKey = publicKey.ToArray();
            Algorithm = algorithm;
            AttestationObject = attestationObject;
        }
    }
}
