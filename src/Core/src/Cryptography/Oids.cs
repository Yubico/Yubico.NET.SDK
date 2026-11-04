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

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Contains standard ASN.1 key algorithm, named-curve and operation OID constants.
/// </summary>
/// <remarks>
/// These are different identifier domains. Use <see cref="CryptographicDefinitions"/> for metadata and
/// domain-specific lookups. A key algorithm OID alone does not select an RSA size or an ordinary EC curve.
/// </remarks>
public static class Oids
{
    /// <summary>
    /// The rsaEncryption key algorithm identifier (PKCS#1), not a size or signature-padding selection.
    /// </summary>
    public const string Rsa = "1.2.840.113549.1.1.1";

    /// <summary>
    /// Represents the id-ecPublicKey algorithm identifier for elliptic-curve keys (ANSI X9.62).
    /// </summary>
    public const string EcPublicKey = "1.2.840.10045.2.1";

    /// <summary>
    /// The RFC 8410 X25519 key/agreement algorithm identifier, with absent ASN.1 parameters.
    /// </summary>
    public const string X25519 = "1.3.101.110";

    /// <summary>
    /// The RFC 8410 Ed25519 key/signature algorithm identifier, with absent ASN.1 parameters.
    /// </summary>
    public const string Ed25519 = "1.3.101.112";

    /// <summary>
    /// Represents the OID for NIST P-256 curve (also known as secp256r1)
    /// </summary>
    public const string P256 = "1.2.840.10045.3.1.7";

    /// <summary>
    /// Represents the OID for NIST P-384 curve (also known as secp384r1)
    /// </summary>
    public const string P384 = "1.3.132.0.34";

    /// <summary>
    /// Represents the OID for NIST P-521 curve (also known as secp521r1)
    /// </summary>
    public const string P521 = "1.3.132.0.35";

    /// <summary>
    /// The AES-128-CBC operation OID, not a mode-independent AES key identifier.
    /// </summary>
    public const string Aes128Cbc = "2.16.840.1.101.3.4.1.2";

    /// <summary>
    /// The AES-192-CBC operation OID, not a mode-independent AES key identifier.
    /// </summary>
    public const string Aes192Cbc = "2.16.840.1.101.3.4.1.22";

    /// <summary>
    /// The AES-256-CBC operation OID, not a mode-independent AES key identifier.
    /// </summary>
    public const string Aes256Cbc = "2.16.840.1.101.3.4.1.42";

    /// <summary>
    /// The Triple DES CBC operation OID, not a mode-independent key identifier.
    /// </summary>
    public const string TripleDesCbc = "1.2.840.113549.3.7";

    /// <summary>The secp256k1 named-curve OID (SEC 2).</summary>
    public const string Secp256k1 = "1.3.132.0.10";
    /// <summary>The brainpoolP256r1 named-curve OID (RFC 5639).</summary>
    public const string BrainpoolP256r1 = "1.3.36.3.3.2.8.1.1.7";
    /// <summary>The brainpoolP384r1 named-curve OID (RFC 5639).</summary>
    public const string BrainpoolP384r1 = "1.3.36.3.3.2.8.1.1.11";
    /// <summary>The brainpoolP512r1 named-curve OID (RFC 5639).</summary>
    public const string BrainpoolP512r1 = "1.3.36.3.3.2.8.1.1.13";
    /// <summary>The ecdsa-with-SHA1 signature operation OID, not id-ecPublicKey (RFC 3279).</summary>
    public const string EcdsaSha1 = "1.2.840.10045.4.1";
    /// <summary>The ecdsa-with-SHA256 signature operation OID (RFC 5758).</summary>
    public const string EcdsaSha256 = "1.2.840.10045.4.3.2";
    /// <summary>The ecdsa-with-SHA384 signature operation OID (RFC 5758).</summary>
    public const string EcdsaSha384 = "1.2.840.10045.4.3.3";
    /// <summary>The ecdsa-with-SHA512 signature operation OID (RFC 5758).</summary>
    public const string EcdsaSha512 = "1.2.840.10045.4.3.4";
    /// <summary>The RSASSA-PSS signature OID; hash, MGF1 and salt parameters select a concrete operation.</summary>
    public const string RsaPss = "1.2.840.113549.1.1.10";
    /// <summary>The sha1WithRSAEncryption PKCS#1 v1.5 signature operation OID.</summary>
    public const string RsaSha1 = "1.2.840.113549.1.1.5";
    /// <summary>The sha256WithRSAEncryption PKCS#1 v1.5 signature operation OID.</summary>
    public const string RsaSha256 = "1.2.840.113549.1.1.11";
    /// <summary>The sha384WithRSAEncryption PKCS#1 v1.5 signature operation OID.</summary>
    public const string RsaSha384 = "1.2.840.113549.1.1.12";
    /// <summary>The sha512WithRSAEncryption PKCS#1 v1.5 signature operation OID.</summary>
    public const string RsaSha512 = "1.2.840.113549.1.1.13";
}