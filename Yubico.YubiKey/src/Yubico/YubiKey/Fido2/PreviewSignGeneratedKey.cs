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
    /// This corresponds to the <c>AuthenticationExtensionsSignGeneratedKey</c>
    /// output defined by the
    /// <see href="https://yubicolabs.github.io/webauthn-sign-extension/4/#dictdef-authenticationextensionssigngeneratedkey">
    /// previewSign extension specification</see>. The generated signing key is
    /// represented by an embedded attestation object whose attested credential
    /// data contains the signing key handle and public key.
    /// </remarks>
    public sealed class PreviewSignGeneratedKey
    {
        private readonly byte[] _publicKey;

        /// <summary>
        /// Gets the key handle used to request signatures from this generated
        /// signing key.
        /// </summary>
        /// <remarks>
        /// This is auxiliary information the authenticator may need to look up
        /// or derive the signing private key. It is copied from the credential
        /// ID in the embedded attestation object's attested credential data and
        /// can be empty.
        /// </remarks>
        public ReadOnlyMemory<byte> KeyHandle { get; init; }

        /// <summary>
        /// Gets the CBOR-encoded COSE public key for the generated signing key.
        /// </summary>
        /// <remarks>
        /// This is the generated signing public key in COSE_Key format, copied
        /// from the embedded attestation object's attested credential data.
        /// </remarks>
        public ReadOnlyMemory<byte> PublicKey => _publicKey;

        /// <summary>
        /// Gets the signature algorithm chosen for the generated signing key.
        /// </summary>
        /// <remarks>
        /// This is the algorithm chosen from the requested algorithm list. It
        /// can differ from the COSE_Key <c>alg</c> attribute for split signing
        /// algorithms.
        /// </remarks>
        public CoseAlgorithmIdentifier Algorithm { get; init; }

        /// <summary>
        /// Gets the attestation object for the generated signing key pair.
        /// </summary>
        /// <remarks>
        /// The previewSign specification carries this object in the unsigned
        /// extension output during registration. It has the same structure as
        /// the top-level attestation object, but attests the generated signing
        /// public key.
        /// </remarks>
        public AttestationObject AttestationObject { get; init; }

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
