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

using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.WebAuthn.Attestation;

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Represents the generated key material returned by the YubiKey during
/// previewSign extension registration.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <see href="https://yubicolabs.github.io/webauthn-sign-extension/4/#dictdef-authenticationextensionssigngeneratedkey">
/// <c>AuthenticationExtensionsSignGeneratedKey</c></see> output defined by the previewSign
/// extension specification. The generated signing key is represented by an embedded attestation
/// object whose attested credential data contains the signing key handle and public key.
/// </para>
/// </remarks>
/// <param name="KeyHandle">
/// The key handle used to request signatures from this generated signing key. This is copied from
/// the credential ID in the embedded attestation object's attested credential data and can be empty.
/// </param>
/// <param name="PublicKey">
/// The COSE public key for the generated signing key. This is the
/// <see href="https://yubicolabs.github.io/webauthn-sign-extension/4/#dom-authenticationextensionssigngeneratedkey-publickey">
/// <c>publicKey</c></see> field of the <c>AuthenticationExtensionsSignGeneratedKey</c> client
/// extension output.
/// </param>
/// <param name="Algorithm">
/// The signing algorithm chosen from the <c>algorithms</c> extension input.
/// </param>
/// <param name="AttestationObject">
/// The attestation object for the generated signing key pair.
/// </param>
public sealed record class GeneratedSigningKey(
    ReadOnlyMemory<byte> KeyHandle,
    CoseKey PublicKey,
    CoseAlgorithm Algorithm,
    WebAuthnAttestationObject? AttestationObject);
