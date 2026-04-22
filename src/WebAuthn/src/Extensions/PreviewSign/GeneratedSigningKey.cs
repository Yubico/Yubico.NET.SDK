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

using Yubico.YubiKit.WebAuthn.Attestation;
using Yubico.YubiKit.WebAuthn.Cose;

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Represents a generated signing key from the previewSign registration ceremony.
/// </summary>
/// <remarks>
/// <para>
/// The signing key is separate from the WebAuthn credential authentication key pair.
/// It is used for signing arbitrary data via the previewSign extension during authentication.
/// </para>
/// <para>
/// Per CTAP v4 draft specification, the attestation object is the authoritative source
/// for the public key and flags. The loose KeyHandle and PublicKey fields are provided
/// for convenience but should be verified against the attestation object.
/// </para>
/// </remarks>
/// <param name="KeyHandle">
/// The key handle for the signing private key. May be empty if the authenticator stores
/// the key internally. Used during authentication to identify which signing key to use.
/// </param>
/// <param name="PublicKey">
/// The COSE-encoded public key. Relying Parties should prefer extracting this from
/// the verified attestation object.
/// </param>
/// <param name="Algorithm">
/// The COSE algorithm chosen by the authenticator from the provided list.
/// May differ from the algorithm in PublicKey if using split-signing algorithms.
/// </param>
/// <param name="AttestationObject">
/// The attestation object for the signing key pair. This contains the authoritative
/// public key, authenticator data, and embedded previewSign extension output including flags.
/// </param>
/// <param name="Flags">
/// The user presence/verification policy for this signing key. Extracted from the
/// attestation object's embedded previewSign extension output.
/// </param>
public sealed record class GeneratedSigningKey(
    ReadOnlyMemory<byte> KeyHandle,
    CoseKey PublicKey,
    CoseAlgorithm Algorithm,
    WebAuthnAttestationObject AttestationObject,
    PreviewSignFlags Flags);
