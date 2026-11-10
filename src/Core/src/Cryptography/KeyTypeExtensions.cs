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

/// <summary>Queries shared metadata and structural classifications for concrete key identities.</summary>
/// <remarks>
/// These queries do not establish operation, codec, provider, hardware, slot or protocol-profile support.
/// Metadata/size/mapping lookups reject <see cref="KeyType.None"/> and unknown values; classification
/// predicates return false for them instead.
/// </remarks>
public static class KeyTypeExtensions
{
    /// <summary>Gets the catalog's immutable metadata instance for a concrete key.</summary>
    /// <param name="keyType">The concrete key identity, not a protocol identifier or operation algorithm.</param>
    /// <returns>The same instance as <see cref="CryptographicDefinitions.Keys.Get"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="keyType"/> is None or is not in the catalog.</exception>
    public static KeyInfo GetKeyInfo(this KeyType keyType) => CryptographicDefinitions.Keys.Get(keyType);

    /// <summary>Gets the nominal key size in bits, not a security-strength estimate.</summary>
    /// <param name="keyType">The concrete key identity.</param>
    /// <returns>
    /// RSA modulus bits, ordinary EC field bits, or stored symmetric/25519 key bits. The 25519 stored
    /// size is 256 bits despite its 255-bit field; Triple DES includes parity bits.
    /// </returns>
    /// <remarks>Use <see cref="KeyInfo.Curve"/> for field/order sizes and representation-specific properties for encoded lengths.</remarks>
    /// <exception cref="ArgumentException"><paramref name="keyType"/> is None or is not in the catalog.</exception>
    public static int GetKeySizeBits(this KeyType keyType) => keyType.GetKeyInfo().KeySizeBits;

    /// <summary>Gets the key representation's nominal component width in bytes.</summary>
    /// <param name="keyType">The concrete key identity.</param>
    /// <returns>RSA modulus width, ordinary EC coordinate width, or stored symmetric/25519 key width, in bytes.</returns>
    /// <remarks>This is not the encoded public point, SPKI or PKCS#8 length. See <see cref="KeyInfo.KeySizeBytes"/>.</remarks>
    /// <exception cref="ArgumentException"><paramref name="keyType"/> is None or is not in the catalog.</exception>
    public static int GetKeySizeBytes(this KeyType keyType) => keyType.GetKeyInfo().KeySizeBytes;

    /// <summary>Determines whether a known concrete key has registered COSE representation metadata.</summary>
    /// <param name="keyType">The concrete key identity.</param>
    /// <returns>True when kty/crv metadata is modeled; false for a known unmapped key such as Triple DES.</returns>
    /// <remarks>A mapping does not choose an alg or establish codec, operation or profile support.</remarks>
    /// <exception cref="ArgumentException"><paramref name="keyType"/> is None or is not in the catalog.</exception>
    public static bool HasCoseMapping(this KeyType keyType) => keyType.GetCoseKeyType() is not null;

    /// <summary>Gets a known key's registered COSE key family (kty), when modeled.</summary>
    /// <param name="keyType">The concrete key identity, never a numeric COSE kty or crv.</param>
    /// <returns>The COSE family, or null for a known key without a modeled mapping.</returns>
    /// <exception cref="ArgumentException"><paramref name="keyType"/> is None or is not in the catalog.</exception>
    public static CoseKeyType? GetCoseKeyType(this KeyType keyType) => keyType.GetKeyInfo().Cose?.Type;

    /// <summary>Determines whether the key uses a modeled ordinary short-Weierstrass curve.</summary>
    /// <param name="keyType">The concrete key identity.</param>
    /// <returns>True for that mathematical form; false for other forms, other families, None or unknown values.</returns>
    /// <remarks>This structural predicate does not select ECDSA over ECDH or imply protocol support.</remarks>
    public static bool IsShortWeierstrass(this KeyType keyType) => CryptographicDefinitions.Keys.TryGet(keyType, out var keyInfo) && keyInfo.IsShortWeierstrass;

    /// <summary>Determines whether the key uses the modeled Curve25519 or Edwards25519 construction.</summary>
    /// <param name="keyType">The concrete key identity.</param>
    /// <returns>True for X25519 or Ed25519 representations; false for other keys, None or unknown values.</returns>
    /// <remarks>The Montgomery and Edwards forms and their public encodings remain distinct.</remarks>
    public static bool IsCurve25519(this KeyType keyType) => CryptographicDefinitions.Keys.TryGet(keyType, out var keyInfo)
        && (keyInfo.Curve == CryptographicDefinitions.Curves.Curve25519 || keyInfo.Curve == CryptographicDefinitions.Curves.Edwards25519);

    /// <summary>Determines whether the key belongs to a modeled asymmetric family.</summary>
    /// <param name="keyType">The concrete key identity.</param>
    /// <returns>True for RSA or elliptic-curve keys; false for other families, None or unknown values.</returns>
    public static bool IsAsymmetric(this KeyType keyType) => CryptographicDefinitions.Keys.TryGet(keyType, out var keyInfo) && keyInfo.IsAsymmetric;

    /// <summary>Determines whether the key belongs to a modeled symmetric family.</summary>
    /// <param name="keyType">The concrete key identity.</param>
    /// <returns>True for AES or Triple DES keys; false for other families, None or unknown values.</returns>
    public static bool IsSymmetric(this KeyType keyType) => CryptographicDefinitions.Keys.TryGet(keyType, out var keyInfo) && keyInfo.IsSymmetric;

    /// <summary>Determines whether the key belongs to the modeled elliptic-curve family.</summary>
    /// <param name="keyType">The concrete key identity.</param>
    /// <returns>True for short-Weierstrass, Montgomery or Edwards keys; false for other families, None or unknown values.</returns>
    public static bool IsEllipticCurve(this KeyType keyType) => CryptographicDefinitions.Keys.TryGet(keyType, out var keyInfo) && keyInfo.IsEllipticCurve;

    /// <summary>Determines whether the key belongs to the modeled RSA family.</summary>
    /// <param name="keyType">The concrete key identity.</param>
    /// <returns>True for RSA keys; false for other families, None or unknown values.</returns>
    public static bool IsRsa(this KeyType keyType) => CryptographicDefinitions.Keys.TryGet(keyType, out var keyInfo) && keyInfo.IsRsa;
}