// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>Structural key families, independent of operations and protocol identifiers.</summary>
public enum KeyFamily
{
    /// <summary>Elliptic-curve keys; the curve form and concrete construction are separate metadata.</summary>
    EllipticCurve,
    /// <summary>RSA keys identified by their modulus size.</summary>
    Rsa,
    /// <summary>Symmetric AES keys.</summary>
    Aes,
    /// <summary>Three-key Triple DES, including stored parity bits.</summary>
    TripleDes
}

/// <summary>Immutable metadata for a concrete key representation. Contains no key material.</summary>
public sealed record KeyInfo
{
    internal KeyInfo(KeyType keyType, string displayName, KeyFamily family, int keySizeBits,
        int keySizeBytes, CurveInfo? curve = null, Asn1KeyInfo? asn1 = null, CoseKeyInfo? cose = null,
        int? privateValueLengthBytes = null, int? publicValueLengthBytes = null)
    {
        KeyType = keyType;
        DisplayName = displayName;
        Family = family;
        KeySizeBits = keySizeBits;
        KeySizeBytes = keySizeBytes;
        Curve = curve;
        Asn1 = asn1;
        Cose = cose;
        PrivateValueLengthBytes = privateValueLengthBytes;
        PublicValueLengthBytes = publicValueLengthBytes;
    }

    /// <summary>The concrete catalog identity, not an operation or protocol identifier.</summary>
    public KeyType KeyType { get; }
    /// <summary>A human-readable label; use <see cref="KeyType"/> for identity.</summary>
    public string DisplayName { get; }
    /// <summary>The structural family, independent of operation selection.</summary>
    public KeyFamily Family { get; }

    /// <summary>
    /// Nominal size: RSA modulus bits, ordinary EC field bits, or symmetric stored key bits (including
    /// Triple DES parity). For 25519 representations this retains the 256 stored-bit size, not the
    /// 255-bit field size or security strength. See <see cref="Curve"/> for mathematical sizes.
    /// </summary>
    public int KeySizeBits { get; }

    /// <summary>
    /// RSA modulus width, ordinary EC coordinate width, or symmetric/25519 stored key width, in bytes.
    /// This is not the encoded public point length or a security-strength estimate. Use
    /// <see cref="PrivateValueLengthBytes"/> for scalar or seed widths, which can differ from coordinate widths.
    /// </summary>
    public int KeySizeBytes { get; }

    /// <summary>Raw private scalar, seed, or symmetric value width; null for structured RSA keys.</summary>
    public int? PrivateValueLengthBytes { get; }
    /// <summary>Uncompressed ordinary EC point or RFC 8410 public value width; null for other families.</summary>
    public int? PublicValueLengthBytes { get; }
    /// <summary>The mathematical curve, or null for RSA and symmetric families.</summary>
    public CurveInfo? Curve { get; }
    /// <summary>ASN.1 key representation, or null when no bare-key representation is modeled.</summary>
    public Asn1KeyInfo? Asn1 { get; }
    /// <summary>Registered COSE kty/crv metadata, or null when no mapping is modeled; never selects an alg.</summary>
    public CoseKeyInfo? Cose { get; }
    /// <summary>Whether this key belongs to the elliptic-curve family, regardless of curve form.</summary>
    public bool IsEllipticCurve => Family == KeyFamily.EllipticCurve;
    /// <summary>Whether this key belongs to the RSA family.</summary>
    public bool IsRsa => Family == KeyFamily.Rsa;
    /// <summary>Whether this key belongs to an asymmetric family (RSA or elliptic curve).</summary>
    public bool IsAsymmetric => IsEllipticCurve || IsRsa;
    /// <summary>Whether this key belongs to the AES or Triple DES family.</summary>
    public bool IsSymmetric => Family is KeyFamily.Aes or KeyFamily.TripleDes;
    /// <summary>Whether the mathematical curve has short-Weierstrass form; does not select ECDSA over ECDH.</summary>
    public bool IsShortWeierstrass => Curve?.Form == CurveForm.ShortWeierstrass;
    /// <summary>Returns the display label and nominal size for diagnostics, not a persistence format.</summary>
    public override string ToString() => $"{DisplayName} ({KeySizeBits} bits)";
}