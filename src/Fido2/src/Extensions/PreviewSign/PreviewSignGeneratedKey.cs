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

using Yubico.YubiKit.Fido2.Arkg;
using Yubico.YubiKit.Fido2.Cose;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Represents the generated key material returned by the YubiKey during
/// previewSign extension registration.
/// </summary>
/// <remarks>
/// <para>
/// This class contains the key handle and public key components needed to
/// perform offline ARKG (Asynchronous Remote Key Generation) key derivation
/// via <see cref="DerivePublicKey"/>.
/// </para>
/// <para>
/// Instances of this class are obtained by calling
/// <see cref="PreviewSignRegistrationOutput.TryGetGeneratedKey"/> after creating
/// a credential with the previewSign extension enabled.
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
    /// <summary>
    /// Gets the key handle for the generated credential.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; init; }

    /// <summary>
    /// Gets the blinding public key component (65-byte SEC1 uncompressed point).
    /// </summary>
    public ReadOnlyMemory<byte> BlindingPublicKey { get; init; }

    /// <summary>
    /// Gets the KEM (Key Encapsulation Mechanism) public key component (65-byte SEC1 uncompressed point).
    /// </summary>
    public ReadOnlyMemory<byte> KemPublicKey { get; init; }

    /// <summary>
    /// Gets the algorithm identifier for the derived key.
    /// </summary>
    public CoseAlgorithm DerivedKeyAlgorithm { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewSignGeneratedKey"/> class.
    /// </summary>
    /// <param name="keyHandle">The key handle.</param>
    /// <param name="blindingPublicKey">The blinding public key (65-byte SEC1 uncompressed point).</param>
    /// <param name="kemPublicKey">The KEM public key (65-byte SEC1 uncompressed point).</param>
    /// <param name="derivedKeyAlgorithm">The derived key algorithm.</param>
    internal PreviewSignGeneratedKey(
        ReadOnlyMemory<byte> keyHandle,
        ReadOnlyMemory<byte> blindingPublicKey,
        ReadOnlyMemory<byte> kemPublicKey,
        CoseAlgorithm derivedKeyAlgorithm)
    {
        KeyHandle = keyHandle;
        BlindingPublicKey = blindingPublicKey;
        KemPublicKey = kemPublicKey;
        DerivedKeyAlgorithm = derivedKeyAlgorithm;
    }

    /// <summary>
    /// Derives a public key using the ARKG-P256 algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs offline key derivation using the ARKG-P256 algorithm.
    /// The derived public key can be used to verify signatures created by the
    /// YubiKey when provided with the corresponding ARKG key handle and context.
    /// </para>
    /// <para>
    /// Multiple independent public keys can be derived from the same generated
    /// key by using different context strings. Each context produces a unique
    /// derived key pair.
    /// </para>
    /// <para>
    /// To use the derived key for signing, include the returned
    /// <see cref="PreviewSignDerivedKey"/> in the previewSign authentication extension
    /// input. The YubiKey will produce a signature that can be verified using
    /// <see cref="PreviewSignDerivedKey.VerifySignature"/>.
    /// </para>
    /// </remarks>
    /// <param name="ikm">
    /// Input keying material for HKDF derivation. This should be random data
    /// unique to the derivation context.
    /// </param>
    /// <param name="ctx">
    /// Context string for domain separation. Different contexts produce
    /// different derived keys from the same input keying material.
    /// </param>
    /// <returns>
    /// A <see cref="PreviewSignDerivedKey"/> containing the derived public key,
    /// ARKG key handle, device key handle, and context.
    /// </returns>
    public PreviewSignDerivedKey DerivePublicKey(ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> ctx)
    {
        (byte[] derivedPk, byte[] arkgKeyHandle) = ArkgP256.DerivePublicKey(
            BlindingPublicKey.Span,
            KemPublicKey.Span,
            ikm,
            ctx);

        return new PreviewSignDerivedKey(
            derivedPk,
            arkgKeyHandle,
            KeyHandle,
            ctx.ToArray());
    }
}
