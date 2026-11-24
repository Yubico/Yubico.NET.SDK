// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Ctap2.Cose;

namespace Yubico.YubiKit.Ctap2.Extensions;

/// <summary>
/// Represents the generated signing key from previewSign registration.
/// </summary>
/// <remarks>
/// Corresponds to <see href="https://yubicolabs.github.io/webauthn-sign-extension/4/#dictdef-authenticationextensionssigngeneratedkey">
/// AuthenticationExtensionsSignGeneratedKey</see> in the experimental previewSign draft.
/// Use <see cref="PreviewSignCbor.TryExtractGeneratedKey"/> to extract it from a registration response.
/// Extraction does not verify attestation, device identity or key binding.
/// </remarks>
[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
public record class PreviewSignGeneratedKey
{
    /// <summary>Initializes the common fields of a generated signing key.</summary>
    private protected PreviewSignGeneratedKey(
        ReadOnlyMemory<byte> keyHandle,
        CoseKey publicKey,
        CoseAlgorithm algorithm,
        ReadOnlyMemory<byte> attestationObject)
    {
        if (attestationObject.IsEmpty)
        {
            throw new ArgumentException("The generated-key attestation object is required.", nameof(attestationObject));
        }

        KeyHandle = keyHandle;
        PublicKey = publicKey;
        Algorithm = algorithm;
        AttestationObject = attestationObject;
    }

    /// <summary>Gets the generated signing-key handle. It may be empty.</summary>
    public ReadOnlyMemory<byte> KeyHandle { get; }

    /// <summary>Gets the decoded signing public key from the inner attested credential data.</summary>
    public CoseKey PublicKey { get; }

    /// <summary>Gets the signing algorithm reported in the outer authenticator data.</summary>
    /// <remarks>This can differ from the algorithm identifier in <see cref="PublicKey"/>.</remarks>
    public CoseAlgorithm Algorithm { get; }

    /// <summary>Gets the generated-key attestation object as WebAuthn-shaped CBOR with text keys.</summary>
    public ReadOnlyMemory<byte> AttestationObject { get; }

    internal static PreviewSignGeneratedKey Create(
        ReadOnlyMemory<byte> keyHandle,
        CoseKey publicKey,
        CoseAlgorithm algorithm,
        ReadOnlyMemory<byte> attestationObject) => publicKey switch
        {
            CoseArkgP256SeedKey seedKey => new ArkgP256GeneratedKey(keyHandle, seedKey, algorithm, attestationObject),
            _ => new PreviewSignGeneratedKey(keyHandle, publicKey, algorithm, attestationObject)
        };
}