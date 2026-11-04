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
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Cryptography;

public static partial class CryptographicDefinitions
{
    /// <summary>Useful SDK operation definitions, not an exhaustive registry or a choice of defaults.</summary>
    public static class Algorithms
    {
        private static readonly KeyApplicability ShortWeierstrassKeys = new(KeyFamily.EllipticCurve, CurveForm.ShortWeierstrass);
        private static readonly KeyApplicability EdwardsSigning = new(KeyFamily.EllipticCurve, CurveForm.Edwards);
        private static readonly KeyApplicability MontgomeryAgreement = new(KeyFamily.EllipticCurve, CurveForm.Montgomery);
        private static readonly KeyApplicability RsaSigning = new(KeyFamily.Rsa);

        /// <summary>Generic ECDSA on short-Weierstrass keys, with no hash selected.</summary>
        public static AlgorithmInfo Ecdsa { get; } = new("ecdsa", "ECDSA (hash unspecified)", CryptographicOperation.Signature, [ShortWeierstrassKeys]);
        /// <summary>ECDSA with SHA-1; historical metadata, not a security recommendation.</summary>
        public static AlgorithmInfo EcdsaSha1 { get; } = EcdsaHash("ecdsa-sha1", "ECDSA with SHA-1", Oids.EcdsaSha1, HashAlgorithmName.SHA1);
        /// <summary>ECDSA with SHA-256 (COSE ES256); this operation does not itself require P-256.</summary>
        public static AlgorithmInfo EcdsaSha256 { get; } = EcdsaHash("ecdsa-sha256", "ECDSA with SHA-256", Oids.EcdsaSha256, HashAlgorithmName.SHA256, -7);
        /// <summary>ECDSA with SHA-384 (COSE ES384); no particular short-Weierstrass curve is selected.</summary>
        public static AlgorithmInfo EcdsaSha384 { get; } = EcdsaHash("ecdsa-sha384", "ECDSA with SHA-384", Oids.EcdsaSha384, HashAlgorithmName.SHA384, -35);
        /// <summary>ECDSA with SHA-512 (COSE ES512); no particular short-Weierstrass curve is selected.</summary>
        public static AlgorithmInfo EcdsaSha512 { get; } = EcdsaHash("ecdsa-sha512", "ECDSA with SHA-512", Oids.EcdsaSha512, HashAlgorithmName.SHA512, -36);
        /// <summary>Generic pure EdDSA on Edwards keys, with the curve unspecified (COSE EdDSA).</summary>
        public static AlgorithmInfo EdDsa { get; } = new("eddsa", "Pure EdDSA (curve unspecified)", CryptographicOperation.Signature, [EdwardsSigning], coseAlgorithmId: -8);
        /// <summary>Pure Ed25519 signatures using the concrete Ed25519 construction and its intrinsic hash.</summary>
        public static AlgorithmInfo Ed25519 { get; } = new("ed25519", "Ed25519", CryptographicOperation.Signature,
            [new(KeyFamily.EllipticCurve, curveId: Curves.Edwards25519.Id, keyType: KeyType.Ed25519)], Oids.Ed25519, coseAlgorithmId: -19);

        /// <summary>Generic RSA signatures with padding and hash unspecified.</summary>
        public static AlgorithmInfo RsaSignature { get; } = new("rsa-signature", "RSA signature (encoding unspecified)", CryptographicOperation.Signature, [RsaSigning]);
        /// <summary>RSASSA-PKCS1-v1_5 signatures with no hash selected.</summary>
        public static AlgorithmInfo RsaPkcs1 { get; } = new("rsa-pkcs1", "RSASSA-PKCS1-v1_5 (hash unspecified)", CryptographicOperation.Signature, [RsaSigning], signaturePadding: RSASignaturePaddingMode.Pkcs1);
        /// <summary>Generic RSASSA-PSS, with hash, MGF1 hash and salt length unspecified.</summary>
        public static AlgorithmInfo RsaPss { get; } = new("rsa-pss", "RSASSA-PSS (parameters unspecified)", CryptographicOperation.Signature, [RsaSigning], Oids.RsaPss, signaturePadding: RSASignaturePaddingMode.Pss);
        /// <summary>RSA PKCS#1 v1.5 with SHA-1; historical metadata, not a security recommendation.</summary>
        public static AlgorithmInfo RsaPkcs1Sha1 { get; } = RsaPkcs1Hash("rsa-pkcs1-sha1", "RSA PKCS#1 v1.5 with SHA-1", Oids.RsaSha1, HashAlgorithmName.SHA1, -65535);
        /// <summary>RSA PKCS#1 v1.5 with SHA-256 (COSE RS256).</summary>
        public static AlgorithmInfo RsaPkcs1Sha256 { get; } = RsaPkcs1Hash("rsa-pkcs1-sha256", "RSA PKCS#1 v1.5 with SHA-256", Oids.RsaSha256, HashAlgorithmName.SHA256, -257);
        /// <summary>RSA PKCS#1 v1.5 with SHA-384 (COSE RS384).</summary>
        public static AlgorithmInfo RsaPkcs1Sha384 { get; } = RsaPkcs1Hash("rsa-pkcs1-sha384", "RSA PKCS#1 v1.5 with SHA-384", Oids.RsaSha384, HashAlgorithmName.SHA384, -258);
        /// <summary>RSA PKCS#1 v1.5 with SHA-512 (COSE RS512).</summary>
        public static AlgorithmInfo RsaPkcs1Sha512 { get; } = RsaPkcs1Hash("rsa-pkcs1-sha512", "RSA PKCS#1 v1.5 with SHA-512", Oids.RsaSha512, HashAlgorithmName.SHA512, -259);
        /// <summary>RSA-PSS with SHA-256, MGF1-SHA-256 and a 32-byte salt (COSE PS256).</summary>
        public static AlgorithmInfo RsaPssSha256 { get; } = RsaPssHash("rsa-pss-sha256", "RSA-PSS with SHA-256", HashAlgorithmName.SHA256, 32, -37);
        /// <summary>RSA-PSS with SHA-384, MGF1-SHA-384 and a 48-byte salt (COSE PS384).</summary>
        public static AlgorithmInfo RsaPssSha384 { get; } = RsaPssHash("rsa-pss-sha384", "RSA-PSS with SHA-384", HashAlgorithmName.SHA384, 48, -38);
        /// <summary>RSA-PSS with SHA-512, MGF1-SHA-512 and a 64-byte salt; excludes RSA-1024 (COSE PS512).</summary>
        public static AlgorithmInfo RsaPssSha512 { get; } = RsaPssHash("rsa-pss-sha512", "RSA-PSS with SHA-512", HashAlgorithmName.SHA512, 64, -39);

        /// <summary>Raw ECDH agreement on short-Weierstrass keys, without an implicit KDF.</summary>
        public static AlgorithmInfo Ecdh { get; } = new("ecdh", "ECDH raw agreement", CryptographicOperation.KeyAgreement, [ShortWeierstrassKeys]);
        /// <summary>Raw X25519 agreement using the concrete X25519 construction, without an implicit KDF.</summary>
        public static AlgorithmInfo X25519 { get; } = new("x25519", "X25519", CryptographicOperation.KeyAgreement,
            [new(KeyFamily.EllipticCurve, curveId: Curves.Curve25519.Id, keyType: KeyType.X25519)], Oids.X25519);
        /// <summary>COSE ephemeral-static ECDH with HKDF-SHA-256, for short-Weierstrass or Montgomery keys.</summary>
        public static AlgorithmInfo EcdhEsHkdf256 { get; } = new("ecdh-es-hkdf-sha256", "ECDH-ES + HKDF-256", CryptographicOperation.KeyDerivation,
            [ShortWeierstrassKeys, MontgomeryAgreement], coseAlgorithmId: -25, hashAlgorithm: HashAlgorithmName.SHA256, derivation: "HKDF");
        /// <summary>AES-128-CBC; protocol padding and companion integrity requirements remain separate.</summary>
        public static AlgorithmInfo Aes128Cbc { get; } = Cbc("aes128-cbc", "AES-128-CBC", KeyFamily.Aes, 128, Oids.Aes128Cbc, -65531);
        /// <summary>AES-192-CBC; protocol padding and companion integrity requirements remain separate.</summary>
        public static AlgorithmInfo Aes192Cbc { get; } = Cbc("aes192-cbc", "AES-192-CBC", KeyFamily.Aes, 192, Oids.Aes192Cbc, -65530);
        /// <summary>AES-256-CBC; protocol padding and companion integrity requirements remain separate.</summary>
        public static AlgorithmInfo Aes256Cbc { get; } = Cbc("aes256-cbc", "AES-256-CBC", KeyFamily.Aes, 256, Oids.Aes256Cbc, -65529);
        /// <summary>Three-key Triple DES CBC; historical metadata, not a security recommendation.</summary>
        public static AlgorithmInfo TripleDesCbc { get; } = Cbc("tripledes-cbc", "Triple DES CBC", KeyFamily.TripleDes, 192, Oids.TripleDesCbc);

        /// <summary>All operations in declaration order. Includes historical algorithms, not recommendations.</summary>
        public static ImmutableList<AlgorithmInfo> All { get; } = [Ecdsa, EcdsaSha1, EcdsaSha256, EcdsaSha384, EcdsaSha512,
            EdDsa, Ed25519, RsaSignature, RsaPkcs1, RsaPss, RsaPkcs1Sha1, RsaPkcs1Sha256, RsaPkcs1Sha384, RsaPkcs1Sha512,
            RsaPssSha256, RsaPssSha384, RsaPssSha512, Ecdh, X25519, EcdhEsHkdf256, Aes128Cbc, Aes192Cbc, Aes256Cbc, TripleDesCbc];

        private static readonly FrozenDictionary<string, AlgorithmInfo> ById = CreateIndex(All, algorithmInfo => algorithmInfo.Id);
        private static readonly FrozenDictionary<string, ImmutableList<AlgorithmInfo>> ByOid = All
            .Where(algorithmInfo => algorithmInfo.AlgorithmOid is not null)
            .GroupBy(algorithmInfo => algorithmInfo.AlgorithmOid ?? throw new InvalidOperationException())
            .ToFrozenDictionary(group => group.Key, group => group.ToImmutableList());

        /// <summary>Gets an operation by its case-sensitive catalog identity, including selected parameters.</summary>
        /// <param name="algorithmId">An <see cref="AlgorithmInfo.Id"/>, not a display label, OID or COSE number.</param>
        /// <returns>The catalog's immutable operation instance.</returns>
        /// <exception cref="NotSupportedException">The identity is not in the catalog.</exception>
        public static AlgorithmInfo Get(string algorithmId) => TryGet(algorithmId, out var algorithmInfo) ? algorithmInfo
            : throw new NotSupportedException($"Unknown operation algorithm identity '{algorithmId}'.");
        /// <summary>Tries to find an operation by its case-sensitive catalog identity.</summary>
        /// <param name="algorithmId">An <see cref="AlgorithmInfo.Id"/>, not a display label, OID or COSE number.</param>
        /// <param name="algorithmInfo">The immutable operation instance on success; null on a miss.</param>
        /// <returns>False for an unknown identity, without throwing.</returns>
        public static bool TryGet(string algorithmId, [NotNullWhen(true)] out AlgorithmInfo? algorithmInfo) => ById.TryGetValue(algorithmId, out algorithmInfo);
        /// <summary>Finds operation definitions only. A shared OID (such as RSA-PSS) can have multiple results.</summary>
        /// <param name="algorithmOid">The dotted-decimal OID to match in operation metadata, even if it also identifies a key.</param>
        /// <returns>An immutable collection in catalog order, empty on a miss.</returns>
        public static ImmutableList<AlgorithmInfo> FindByOid(string algorithmOid) => ByOid.TryGetValue(algorithmOid, out var matches) ? matches : [];
        /// <summary>Structurally applicable operations, not provider/device support, profile policy or a default.</summary>
        /// <param name="keyInfo">The concrete key representation to match against the OR applicability alternatives.</param>
        /// <returns>All matching operations in catalog order; the immutable collection can be empty.</returns>
        public static ImmutableList<AlgorithmInfo> FindForKey(KeyInfo keyInfo) => All
            .Where(algorithmInfo => algorithmInfo.Applicability.Any(constraint => constraint.Matches(keyInfo))).ToImmutableList();

        private static AlgorithmInfo EcdsaHash(string algorithmId, string displayName, string algorithmOid, HashAlgorithmName hashAlgorithm, int? coseAlgorithmId = null) =>
            new(algorithmId, displayName, CryptographicOperation.Signature, [ShortWeierstrassKeys], algorithmOid, coseAlgorithmId, hashAlgorithm);
        private static AlgorithmInfo RsaPkcs1Hash(string algorithmId, string displayName, string algorithmOid, HashAlgorithmName hashAlgorithm, int coseAlgorithmId) =>
            new(algorithmId, displayName, CryptographicOperation.Signature, [RsaSigning], algorithmOid, coseAlgorithmId, hashAlgorithm, RSASignaturePaddingMode.Pkcs1);
        private static AlgorithmInfo RsaPssHash(string algorithmId, string displayName, HashAlgorithmName hashAlgorithm, int saltLengthBytes, int coseAlgorithmId) =>
            new(algorithmId, displayName, CryptographicOperation.Signature,
                // EMSA-PSS needs emLen >= hLen + sLen + 2; 1024-bit RSA cannot fit SHA-512 with a 64-byte salt.
                [new(KeyFamily.Rsa, minimumKeySizeBits: (saltLengthBytes * 2 + 2 - 1) * 8 + 2)],
                Oids.RsaPss, coseAlgorithmId, hashAlgorithm, RSASignaturePaddingMode.Pss, hashAlgorithm, saltLengthBytes);
        private static AlgorithmInfo Cbc(string algorithmId, string displayName, KeyFamily keyFamily, int keySizeBits, string algorithmOid, int? coseAlgorithmId = null) =>
            new(algorithmId, displayName, CryptographicOperation.Encryption, [new(keyFamily, minimumKeySizeBits: keySizeBits, maximumKeySizeBits: keySizeBits)], algorithmOid, coseAlgorithmId, cipherMode: CipherMode.CBC);
    }
}