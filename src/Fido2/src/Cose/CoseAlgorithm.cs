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
    /// ESP256 split-key ARKG placeholder (CTAP v4 draft previewSign extension).
    /// </summary>
    /// <remarks>
    /// This is the wire-level COSE algorithm identifier for the ARKG-P256-ESP256 signing operation,
    /// written under key 3 of a <c>COSE_Sign_Args</c> map (previewSign authentication request).
    /// Do NOT confuse with the seed-key COSE-key alg identifier <c>-65700</c>
    /// (<c>ARKG_P256_PLACEHOLDER.ALGORITHM</c> in python-fido2) which lives at a different protocol layer.
    /// </remarks>
    public static readonly CoseAlgorithm Esp256SplitArkgPlaceholder = new(-65539);

    /// <summary>
    /// ARKG-P256-ESP256 signing-operation algorithm identifier (alias of
    /// <see cref="Esp256SplitArkgPlaceholder"/>; value <c>-65539</c>).
    /// </summary>
    /// <remarks>
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
    /// Gets a value indicating whether this is a known algorithm.
    /// </summary>
    public bool IsKnown => Value switch
    {
        -7 or -8 or -9 or -35 or -257 or -65539 => true,
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
        _ => $"COSE({Value})"
    };
}