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

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Core.Cryptography;

public static partial class CryptographicDefinitions
{
    /// <summary>Concrete key representations. Initialization depends only on curves, never algorithms.</summary>
    public static class Keys
    {
        /// <summary>Ordinary EC key representation on NIST P-256; does not select ECDSA over ECDH.</summary>
        public static KeyInfo P256 { get; } = Ec(KeyType.P256, Curves.P256, CoseEcCurve.P256);
        /// <summary>Ordinary EC key representation on NIST P-384.</summary>
        public static KeyInfo P384 { get; } = Ec(KeyType.P384, Curves.P384, CoseEcCurve.P384);
        /// <summary>Ordinary EC key representation on NIST P-521, with 66-byte coordinates and scalars.</summary>
        public static KeyInfo P521 { get; } = Ec(KeyType.P521, Curves.P521, CoseEcCurve.P521);
        /// <summary>X25519 construction on Curve25519, with 32-byte private and public values.</summary>
        public static KeyInfo X25519 { get; } = new(KeyType.X25519, "X25519", KeyFamily.EllipticCurve, 256, 32,
            Curves.Curve25519, new(Oids.X25519, Asn1ParameterKind.Absent), new(CoseKeyType.Okp, CoseEcCurve.X25519), 32, 32);
        /// <summary>Ed25519 construction on Edwards25519, with a 32-byte private seed and public value.</summary>
        public static KeyInfo Ed25519 { get; } = new(KeyType.Ed25519, "Ed25519", KeyFamily.EllipticCurve, 256, 32,
            Curves.Edwards25519, new(Oids.Ed25519, Asn1ParameterKind.Absent), new(CoseKeyType.Okp, CoseEcCurve.Ed25519), 32, 32);
        /// <summary>RSA with a 1024-bit modulus; historical metadata, not a security recommendation.</summary>
        public static KeyInfo Rsa1024 { get; } = Rsa(KeyType.Rsa1024, 1024);
        /// <summary>RSA with a 2048-bit modulus; no signature padding or operation is implied.</summary>
        public static KeyInfo Rsa2048 { get; } = Rsa(KeyType.Rsa2048, 2048);
        /// <summary>RSA with a 3072-bit modulus.</summary>
        public static KeyInfo Rsa3072 { get; } = Rsa(KeyType.Rsa3072, 3072);
        /// <summary>RSA with a 4096-bit modulus.</summary>
        public static KeyInfo Rsa4096 { get; } = Rsa(KeyType.Rsa4096, 4096);
        /// <summary>Three-key Triple DES with 24 stored bytes including parity; not a recommendation.</summary>
        public static KeyInfo TripleDes { get; } = new(KeyType.TripleDes, "Triple DES", KeyFamily.TripleDes, 192, 24, privateValueLengthBytes: 24);
        /// <summary>AES with a 128-bit key; no block mode is implied.</summary>
        public static KeyInfo Aes128 { get; } = Aes(KeyType.Aes128, 128);
        /// <summary>AES with a 192-bit key; no block mode is implied.</summary>
        public static KeyInfo Aes192 { get; } = Aes(KeyType.Aes192, 192);
        /// <summary>AES with a 256-bit key; no block mode is implied.</summary>
        public static KeyInfo Aes256 { get; } = Aes(KeyType.Aes256, 256);
        /// <summary>Ordinary EC representation on secp256k1, distinct from NIST P-256.</summary>
        public static KeyInfo Secp256k1 { get; } = Ec(KeyType.Secp256k1, Curves.Secp256k1, CoseEcCurve.Secp256k1);
        /// <summary>Ordinary EC representation on brainpoolP256r1.</summary>
        public static KeyInfo BrainpoolP256r1 { get; } = Ec(KeyType.BrainpoolP256r1, Curves.BrainpoolP256r1, CoseEcCurve.BrainpoolP256r1);
        /// <summary>Ordinary EC representation on brainpoolP384r1.</summary>
        public static KeyInfo BrainpoolP384r1 { get; } = Ec(KeyType.BrainpoolP384r1, Curves.BrainpoolP384r1, CoseEcCurve.BrainpoolP384r1);
        /// <summary>Ordinary EC representation on brainpoolP512r1.</summary>
        public static KeyInfo BrainpoolP512r1 { get; } = Ec(KeyType.BrainpoolP512r1, Curves.BrainpoolP512r1, CoseEcCurve.BrainpoolP512r1);

        /// <summary>All concrete keys in stable KeyType numeric order; None is not a key.</summary>
        public static ImmutableList<KeyInfo> All { get; } =
            [P256, P384, P521, X25519, Ed25519, Rsa1024, Rsa2048, Rsa3072, Rsa4096, TripleDes, Aes128, Aes192, Aes256,
                Secp256k1, BrainpoolP256r1, BrainpoolP384r1, BrainpoolP512r1];

        private static readonly FrozenDictionary<KeyType, KeyInfo> ByKeyType = CreateIndex(All, keyInfo => keyInfo.KeyType);
        private static readonly FrozenDictionary<string, ImmutableList<KeyInfo>> ByCurveId = CreateCurveIndex(All);

        /// <summary>Gets immutable metadata for a concrete key identity.</summary>
        /// <param name="keyType">The concrete key identity, not an operation or protocol number.</param>
        /// <returns>The catalog's shared metadata instance.</returns>
        /// <exception cref="ArgumentException">The identity is None or is not in the catalog.</exception>
        public static KeyInfo Get(KeyType keyType) => TryGet(keyType, out var keyInfo) ? keyInfo
            : throw new ArgumentException($"Unsupported concrete key type '{keyType}'.", nameof(keyType));
        /// <summary>Tries to find immutable metadata for a concrete key identity.</summary>
        /// <param name="keyType">The concrete key identity.</param>
        /// <param name="keyInfo">The shared metadata instance on success; null on a miss.</param>
        /// <returns>False for None or an unknown identity, without throwing.</returns>
        public static bool TryGet(KeyType keyType, [NotNullWhen(true)] out KeyInfo? keyInfo) => ByKeyType.TryGetValue(keyType, out keyInfo);

        /// <summary>Finds all concrete constructions for a mathematical curve, in catalog order.</summary>
        /// <param name="curveInfo">The curve to match by its case-sensitive catalog identity.</param>
        /// <returns>An immutable collection, empty if no construction is modeled.</returns>
        public static ImmutableList<KeyInfo> FindByCurve(CurveInfo curveInfo) =>
            ByCurveId.TryGetValue(curveInfo.Id, out var matches) ? matches : [];

        /// <summary>Gets the sole modeled construction for a mathematical curve.</summary>
        /// <param name="curveInfo">The curve to match by its case-sensitive catalog identity.</param>
        /// <returns>The unique concrete key; never an arbitrary first match.</returns>
        /// <exception cref="NotSupportedException">No construction is modeled for the curve.</exception>
        /// <exception cref="InvalidOperationException">Multiple constructions are modeled; use <see cref="FindByCurve"/>.</exception>
        public static KeyInfo GetByCurve(CurveInfo curveInfo) => ResolveByCurve(curveInfo, FindByCurve(curveInfo));

        // ECParameters represents id-ecPublicKey, not every construction on the same curve.
        internal static KeyInfo GetNamedEcByCurve(CurveInfo curveInfo) =>
            ResolveByCurve(curveInfo, FindByCurve(curveInfo), Oids.EcPublicKey);

        internal static FrozenDictionary<string, ImmutableList<KeyInfo>> CreateCurveIndex(IEnumerable<KeyInfo> entries) => entries
            .Where(keyInfo => keyInfo.Curve is not null)
            .GroupBy(keyInfo => keyInfo.Curve?.Id ?? throw new InvalidOperationException())
            .ToFrozenDictionary(group => group.Key, group => group.ToImmutableList());

        internal static KeyInfo ResolveByCurve(CurveInfo curveInfo, ImmutableList<KeyInfo> matches, string? keyAlgorithmOid = null)
        {
            if (keyAlgorithmOid is not null)
            {
                matches = matches.Where(keyInfo => keyInfo.Asn1?.AlgorithmOid == keyAlgorithmOid).ToImmutableList();
            }
            return matches.Count switch
            {
                0 => throw new NotSupportedException($"No matching concrete key is modeled for curve '{curveInfo.Id}'."),
                1 => matches[0],
                _ => throw new InvalidOperationException($"Multiple concrete keys are modeled for curve '{curveInfo.Id}'; select a construction explicitly.")
            };
        }

        /// <summary>Resolves an RSA modulus size, retaining the existing one-bit size tolerance.</summary>
        /// <param name="keySizeBits">The RSA modulus length in bits, not bytes or security strength.</param>
        /// <returns>The modeled RSA key whose nominal size is within one bit of the supplied length.</returns>
        /// <exception cref="NotSupportedException">No modeled RSA size is within that tolerance.</exception>
        public static KeyInfo GetByRsaLength(int keySizeBits) => All.FirstOrDefault(
            keyInfo => keyInfo.IsRsa && Math.Abs((long)keySizeBits - keyInfo.KeySizeBits) <= 1)
            ?? throw new NotSupportedException($"Unsupported RSA modulus length '{keySizeBits}' bits.");

        private static KeyInfo Ec(KeyType keyType, CurveInfo curveInfo, CoseEcCurve? coseCurve = null) => new(
            keyType, curveInfo.DisplayName, KeyFamily.EllipticCurve, curveInfo.FieldSizeBits, curveInfo.CoordinateLengthBytes,
            curveInfo, new(Oids.EcPublicKey, Asn1ParameterKind.NamedCurve, curveInfo.NamedCurveOid),
            coseCurve is null ? null : new(CoseKeyType.Ec2, coseCurve),
            (curveInfo.OrderSizeBits + 7) / 8, 1 + 2 * curveInfo.CoordinateLengthBytes);

        private static KeyInfo Rsa(KeyType keyType, int keySizeBits) => new(keyType, $"RSA-{keySizeBits}", KeyFamily.Rsa,
            keySizeBits, keySizeBits / 8, asn1: new(Oids.Rsa, Asn1ParameterKind.Null), cose: new(CoseKeyType.Rsa));
        private static KeyInfo Aes(KeyType keyType, int keySizeBits) => new(keyType, $"AES-{keySizeBits}", KeyFamily.Aes,
            keySizeBits, keySizeBits / 8, cose: new(CoseKeyType.Symmetric), privateValueLengthBytes: keySizeBits / 8);
    }
}