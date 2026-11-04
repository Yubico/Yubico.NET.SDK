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

using Yubico.YubiKit.Ctap2.Arkg;
using Yubico.YubiKit.Ctap2.Cose;

namespace Yubico.YubiKit.Ctap2.Extensions;

/// <summary>
/// Represents the generated key material returned by the YubiKey during
/// previewSign extension registration.
/// </summary>
/// <remarks>
/// <para>
/// <b>WARNING -- EXPERIMENTAL --</b> ARKG previewSign derivation helpers are not ready for production use and
/// must not be treated as production cryptographic guidance.
/// </para>
/// <para>
/// This class contains the key handle and public key components needed to
/// perform RP-side ARKG-P256 public key derivation via <see cref="DerivePublicKey"/>.
/// </para>
/// <para>
/// Instances of this class are obtained by calling
/// <see cref="PreviewSignRegistrationOutput.TryGetGeneratedKey"/> after creating
/// a credential with the previewSign extension enabled.
/// </para>
/// <para>
/// The public seed permits offline derivations using input keying material and a context. It is not
/// proof of attestation, device identity, key binding or uniqueness for every different context.
/// Memory supplied to this object is borrowed; derived points/handles and copied contexts have no
/// disposal owner. Public points are not secrets, but application-sensitive context/handle storage
/// should not be logged and must follow the application's lifetime policy.
/// </para>
/// </remarks>
public sealed class PreviewSignGeneratedKey
{
    /// <summary>
    /// Gets the key handle used to request signatures from this generated signing key.
    /// </summary>
    public ReadOnlyMemory<byte> KeyHandle { get; init; }

    /// <summary>
    /// Gets the blinding public key component.
    /// </summary>
    public ReadOnlyMemory<byte> BlindingPublicKey { get; init; }

    /// <summary>
    /// Gets the KEM public key component.
    /// </summary>
    public ReadOnlyMemory<byte> KemPublicKey { get; init; }

    /// <summary>
    /// Gets the algorithm identifier for the derived key.
    /// </summary>
    /// <remarks>
    /// Extraction paths currently disagree: TryGetGeneratedKey supplies seed marker -65700, while
    /// PreviewSignCbor.TryExtractGeneratedKey supplies the seed's derived algorithm (typically -9).
    /// DerivePublicKey does not dispatch on this property; its verifier is fixed to P-256/SHA-256.
    /// </remarks>
    public CoseAlgorithm DerivedKeyAlgorithm { get; init; }

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
    /// <b>WARNING -- EXPERIMENTAL --</b> This ARKG derivation helper is not ready for production use and must not
    /// be treated as production cryptographic guidance.
    /// </para>
    /// <para>
    /// This method performs offline key derivation using the ARKG-P256 algorithm.
    /// The derived public key can be used to verify signatures created by the
    /// YubiKey when provided with the corresponding ARKG key handle and context.
    /// </para>
    /// <para>
    /// Different input keying material/context values provide derivation/domain-separation inputs;
    /// this helper does not establish unconditional uniqueness or full conformance to the cited ARKG
    /// Internet-Draft. Context is limited to 64 bytes by the experimental algorithm's explicit bound.
    /// </para>
    /// <para>
    /// To request an ESP256-split-ARKG signature with this helper, pass the returned
    /// <see cref="PreviewSignDerivedKey.DeviceKeyHandle"/>,
    /// <see cref="PreviewSignDerivedKey.ArkgKeyHandle"/>, and
    /// <see cref="PreviewSignDerivedKey.Context"/> to the previewSign authentication extension input.
    /// The YubiKey will produce a signature that can be verified using
    /// <see cref="PreviewSignDerivedKey.VerifySignature"/>.
    /// </para>
    /// </remarks>
    /// <param name="ikm">
    /// Input keying material for HKDF derivation. This should be random data
    /// unique to the derivation context.
    /// </param>
    /// <param name="ctx">
    /// Borrowed context bytes for domain separation (0–64 bytes); copied into the returned result.
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