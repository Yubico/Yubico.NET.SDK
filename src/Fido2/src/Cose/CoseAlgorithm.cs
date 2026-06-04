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

namespace Yubico.YubiKit.Fido2.Cose;

/// <summary>
/// COSE algorithm identifier.
/// </summary>
/// <param name="Value">The COSE algorithm integer value.</param>
/// <remarks>
/// This is a value type carrier for COSE algorithm identifiers. Unlike an enum,
/// it can represent unknown algorithm values. See
/// <see href="https://www.iana.org/assignments/cose/cose.xhtml">COSE Algorithm Registry</see>.
/// </remarks>
public readonly record struct CoseAlgorithm(int Value) : IEquatable<CoseAlgorithm>
{
    /// <summary>
    /// ECDSA with SHA-256 (P-256 curve).
    /// </summary>
    public static readonly CoseAlgorithm Es256 = new(-7);

    /// <summary>
    /// EdDSA (Ed25519 curve).
    /// </summary>
    public static readonly CoseAlgorithm EdDsa = new(-8);

    /// <summary>
    /// ECDSA with SHA-256 (secp256k1 curve).
    /// </summary>
    public static readonly CoseAlgorithm Esp256 = new(-9);

    /// <summary>
    /// ECDSA with SHA-384 (P-384 curve).
    /// </summary>
    public static readonly CoseAlgorithm Es384 = new(-35);

    /// <summary>
    /// RSASSA-PKCS1-v1_5 with SHA-256.
    /// </summary>
    public static readonly CoseAlgorithm Rs256 = new(-257);

    /// <summary>
    /// ESP256 split-key ARKG placeholder for the previewSign extension.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WARNING -- EXPERIMENTAL --</b> ARKG previewSign identifiers are not ready for production use and must
    /// not be treated as production cryptographic guidance.
    /// </para>
    /// <para>
    /// This is the wire-level COSE algorithm identifier for the ARKG-P256-ESP256 signing operation,
    /// written under key 3 of a <c>COSE_Sign_Args</c> map (previewSign authentication request).
    /// Do NOT confuse with the seed-key COSE-key alg identifier <c>-65700</c>
    /// (<c>ARKG_P256_PLACEHOLDER.ALGORITHM</c> in python-fido2) which lives at a different protocol layer.
    /// </para>
    /// </remarks>
    public static readonly CoseAlgorithm Esp256SplitArkgPlaceholder = new(-65539);

    /// <summary>
    /// ARKG-P256-ESP256 signing-operation algorithm identifier (alias of
    /// <see cref="Esp256SplitArkgPlaceholder"/>; value <c>-65539</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WARNING -- EXPERIMENTAL --</b> ARKG previewSign helpers and identifiers are not ready for production use
    /// and must not be treated as production cryptographic guidance.
    /// </para>
    /// <para>
    /// Stable, intent-revealing alias for the ARKG-P256 signing-operation alg ID. Use this on
    /// <c>ArkgP256SignArgs.Algorithm</c> and any caller-facing API that names the request alg.
    /// The underlying value is intentionally identical to <see cref="Esp256SplitArkgPlaceholder"/>;
    /// when Yubico finalises the alg ID we can rename one without churning consumers of the other.
    /// </para>
    /// <para>
    /// Wire value: <c>-65539</c>. This is the request-side signing algorithm and goes on the wire
    /// at <c>COSE_Sign_Args</c> key 3. It is NOT the seed-key COSE-key alg (which is <c>-65700</c>
    /// in python-fido2's <c>ARKG_P256_PLACEHOLDER</c>).
    /// </para>
    /// </remarks>
    public static readonly CoseAlgorithm ArkgP256 = Esp256SplitArkgPlaceholder;

    /// <summary>
    /// ARKG-P256 seed-key COSE algorithm identifier (value <c>-65700</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WARNING -- EXPERIMENTAL --</b> ARKG previewSign seed-key handling is not ready for production use and
    /// must not be treated as production cryptographic guidance.
    /// </para>
    /// <para>
    /// This is the seed-key COSE-key <c>alg</c> parameter (key 3) identifying an ARKG-P256
    /// placeholder key structure, per python-fido2's <c>ARKG_P256_PLACEHOLDER.ALGORITHM</c>.
    /// It marks a COSE_Key as containing KEM and blinding public keys (at parameters -1/-2)
    /// instead of standard EC2 curve coordinates.
    /// </para>
    /// <para>
    /// Do NOT confuse with the signing-operation algorithm <see cref="ArkgP256"/> (<c>-65539</c>),
    /// which is written in <c>COSE_Sign_Args</c> key 3 during previewSign authentication requests.
    /// The two constants live at different protocol layers and are NOT interchangeable.
    /// </para>
    /// </remarks>
    public static readonly CoseAlgorithm ArkgP256SeedKey = new(-65700);

    /// <summary>
    /// Gets a value indicating whether this is a known algorithm.
    /// </summary>
    public bool IsKnown => Value switch
    {
        -7 or -8 or -9 or -35 or -257 or -65539 or -65700 => true,
        _ => false
    };

    /// <summary>
    /// Creates a COSE algorithm from an arbitrary integer value.
    /// </summary>
    /// <param name="value">The COSE algorithm value.</param>
    /// <returns>A <see cref="CoseAlgorithm"/> with the specified value.</returns>
    public static CoseAlgorithm Other(int value) => new(value);

    /// <summary>
    /// Returns the algorithm name if known, otherwise "COSE(value)".
    /// </summary>
    public override string ToString() => Value switch
    {
        -7 => "ES256",
        -8 => "EdDSA",
        -9 => "ESP256",
        -35 => "ES384",
        -257 => "RS256",
        -65539 => "ESP256_SPLIT_ARKG_PLACEHOLDER",
        -65700 => "ARKG_P256_SEED_KEY",
        _ => $"COSE({Value})"
    };
}
