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

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Output from the previewSign extension registration.
/// </summary>
/// <remarks>
/// Contains the generated signing key information returned by the authenticator.
/// </remarks>
public sealed class PreviewSignRegistrationOutput
{
    /// <summary>
    /// Gets the key handle of the generated signing key.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; init; }

    /// <summary>
    /// Gets the COSE public key of the generated signing key.
    /// </summary>
    public ReadOnlyMemory<byte> PublicKey { get; init; }

    /// <summary>
    /// Gets the COSE algorithm identifier of the generated key.
    /// </summary>
    public int Algorithm { get; init; }

    /// <summary>
    /// Gets the attestation object containing the signing key.
    /// </summary>
    /// <remarks>
    /// May be null if authenticator did not provide unsigned extension outputs.
    /// </remarks>
    public ReadOnlyMemory<byte>? AttestationObject { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignRegistrationOutput"/>.
    /// </summary>
    public PreviewSignRegistrationOutput(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> publicKey,
        int algorithm,
        ReadOnlyMemory<byte>? attestationObject = null)
    {
        KeyHandle = keyHandle;
        PublicKey = publicKey;
        Algorithm = algorithm;
        AttestationObject = attestationObject;
    }

    /// <summary>
    /// Attempts to extract a typed <see cref="PreviewSignGeneratedKey"/> from the
    /// registration output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method decodes the <see cref="PublicKey"/> CBOR and returns a
    /// <see cref="PreviewSignGeneratedKey"/> if the key is an ARKG-P256 seed-key
    /// (COSE_Key with alg -65700). Returns <c>null</c> if the public key is not
    /// an ARKG seed-key variant.
    /// </para>
    /// <para>
    /// This is a backwards-compatible accessor: the raw <see cref="PublicKey"/>
    /// field remains available for direct CBOR processing.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A <see cref="PreviewSignGeneratedKey"/> if the public key is an ARKG-P256 seed-key;
    /// otherwise, <c>null</c>.
    /// </returns>
    public PreviewSignGeneratedKey? TryGetGeneratedKey()
    {
        try
        {
            var coseKey = Cose.CoseKey.Decode(PublicKey);

            if (coseKey is Cose.CoseArkgP256SeedKey arkgKey)
            {
                return new PreviewSignGeneratedKey(
                    KeyHandle,
                    arkgKey.BlPublicKey,
                    arkgKey.KemPublicKey,
                    arkgKey.Algorithm);
            }

            return null;
        }
        catch
        {
            // CBOR decode failure or invalid shape - return null
            return null;
        }
    }
}