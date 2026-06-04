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
    /// WARNING: This code is for testing purposes only and is not intended to be a
    /// secure or complete implementation of ARKG.
    /// </para>
    /// <para>
    /// This corresponds to the <c>AuthenticationExtensionsSignGeneratedKey</c>
    /// output defined by the
    /// <see href="https://yubicolabs.github.io/webauthn-sign-extension/4/#dictdef-authenticationextensionssigngeneratedkey">
    /// previewSign extension specification</see>. The generated signing key is
    /// represented by an embedded attestation object whose attested credential
    /// data contains the signing key handle and public key.
    /// </para>
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
        /// This is the
        /// <see href="https://yubicolabs.github.io/webauthn-sign-extension/4/#dom-authenticationextensionssigngeneratedkey-publickey"><c>publicKey</c></see>
        /// field of the <c>AuthenticationExtensionsSignGeneratedKey</c> client
        /// extension output.
        /// </remarks>
        public ReadOnlyMemory<byte> PublicKey => _publicKey;

        /// <summary>
        /// Gets the signing algorithm chosen from the <c>algorithms</c>
        /// extension input.
        /// </summary>
        /// <remarks>
        /// This expresses how to communicate inputs to the authenticator during
        /// signing. Callers use the selected algorithm to decide how to prepare
        /// later signing input. This may be different from the <c>3 (alg)</c>
        /// attribute of the <see cref="PublicKey"/>, which expresses how third
        /// party consumers can use the public key.
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
