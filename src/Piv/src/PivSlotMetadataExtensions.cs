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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Piv;

/// <summary>
/// Extension methods for <see cref="PivSlotMetadata"/> to simplify public key extraction.
/// </summary>
public static class PivSlotMetadataExtensions
{
    /// <summary>
    /// Gets the RSA public key from slot metadata.
    /// </summary>
    /// <param name="metadata">The slot metadata containing the public key.</param>
    /// <returns>An <see cref="RSA"/> instance with the public key imported.</returns>
    /// <exception cref="InvalidOperationException">The slot does not contain an RSA key.</exception>
    /// <remarks>
    /// <para>
    /// The returned RSA instance can be used with <c>CertificateRequest</c> to generate
    /// certificate signing requests or self-signed certificates.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
    /// using var rsa = metadata.Value.GetRsaPublicKey();
    /// var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    /// </code>
    /// </example>
    public static RSA GetRsaPublicKey(this PivSlotMetadata metadata)
    {
        if (!metadata.Algorithm.IsRsa())
        {
            throw new InvalidOperationException(
                $"Slot contains {metadata.Algorithm} key, not an RSA key. Use GetECDsaPublicKey() for ECC keys.");
        }

        // PivSlotMetadata.PublicKey stores the raw PIV TLV-encoded RSA public key:
        //   { tag 0x81: modulus bytes, tag 0x82: exponent bytes }
        // We parse it the same way PivSession.ParseRsaPublicKey does.
        var rawSpan = metadata.PublicKey.Span;
        var tlvDict = TlvHelper.DecodeDictionary(rawSpan);

        if (!tlvDict.TryGetValue(0x81, out var modulusMemory))
        {
            throw new InvalidOperationException(
                "Public key metadata is missing RSA modulus (tag 0x81).");
        }

        if (!tlvDict.TryGetValue(0x82, out var exponentMemory))
        {
            throw new InvalidOperationException(
                "Public key metadata is missing RSA public exponent (tag 0x82).");
        }

        var parameters = new RSAParameters
        {
            Modulus = modulusMemory.ToArray(),
            Exponent = exponentMemory.ToArray()
        };

        return RSA.Create(parameters);
    }

    /// <summary>
    /// Gets the ECDSA public key from slot metadata.
    /// </summary>
    /// <param name="metadata">The slot metadata containing the public key.</param>
    /// <returns>An <see cref="ECDsa"/> instance with the public key imported.</returns>
    /// <exception cref="InvalidOperationException">The slot does not contain an ECC key supported by ECDsa.</exception>
    /// <remarks>
    /// <para>
    /// This method supports P-256 and P-384 curves. Ed25519 and X25519 keys are not supported
    /// by .NET's <see cref="ECDsa"/> class and will throw an exception.
    /// </para>
    /// <para>
    /// The returned ECDsa instance can be used with <c>CertificateRequest</c> to generate
    /// certificate signing requests or self-signed certificates.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
    /// using var ecdsa = metadata.Value.GetECDsaPublicKey();
    /// var request = new CertificateRequest("CN=Test", ecdsa, HashAlgorithmName.SHA256);
    /// </code>
    /// </example>
    public static ECDsa GetECDsaPublicKey(this PivSlotMetadata metadata)
    {
        if (!metadata.Algorithm.IsEcc())
        {
            throw new InvalidOperationException(
                $"Slot contains {metadata.Algorithm} key, not an ECC key. Use GetRsaPublicKey() for RSA keys.");
        }

        if (metadata.Algorithm is PivAlgorithm.Ed25519 or PivAlgorithm.X25519)
        {
            throw new InvalidOperationException(
                $"Slot contains {metadata.Algorithm} key which is not supported by .NET ECDsa. " +
                "Curve25519 keys (Ed25519, X25519) require specialized handling.");
        }

        // PivSlotMetadata.PublicKey stores the raw PIV TLV-encoded EC point:
        //   [0x86][length][0x04][X bytes][Y bytes]
        // We need to strip the TLV header and build ECParameters from the raw point.
        var rawBytes = metadata.PublicKey.Span;
        ReadOnlySpan<byte> ecPoint = rawBytes;

        // Strip the outer TLV tag/length if present (tag 0x86 = PIV EC point)
        if (rawBytes.Length > 2 && rawBytes[0] == 0x86)
        {
            int headerLength = rawBytes[1] < 0x80 ? 2 : 2 + (rawBytes[1] & 0x7F);
            ecPoint = rawBytes[headerLength..];
        }

        // ecPoint should now be [0x04][X][Y] (uncompressed format)
        if (ecPoint.Length < 3 || ecPoint[0] != 0x04)
        {
            throw new InvalidOperationException(
                "Public key is not in expected uncompressed EC point format (0x04 prefix).");
        }

        int coordinateSize = (ecPoint.Length - 1) / 2;
        var x = ecPoint[1..(1 + coordinateSize)].ToArray();
        var y = ecPoint[(1 + coordinateSize)..].ToArray();

        var curve = metadata.Algorithm == PivAlgorithm.EccP384
            ? ECCurve.NamedCurves.nistP384
            : ECCurve.NamedCurves.nistP256;

        var ecParams = new ECParameters
        {
            Curve = curve,
            Q = new ECPoint { X = x, Y = y }
        };

        return ECDsa.Create(ecParams);
    }
}

/// <summary>
/// Extension methods for <see cref="PivAlgorithm"/> to check algorithm type.
/// </summary>
public static class PivAlgorithmExtensions
{
    /// <summary>
    /// Determines if the algorithm is an RSA algorithm.
    /// </summary>
    /// <param name="algorithm">The algorithm to check.</param>
    /// <returns><c>true</c> if the algorithm is RSA (1024, 2048, 3072, or 4096 bits); otherwise, <c>false</c>.</returns>
    public static bool IsRsa(this PivAlgorithm algorithm) =>
        algorithm is PivAlgorithm.Rsa1024 or PivAlgorithm.Rsa2048 or 
                     PivAlgorithm.Rsa3072 or PivAlgorithm.Rsa4096;

    /// <summary>
    /// Determines if the algorithm is an elliptic curve algorithm.
    /// </summary>
    /// <param name="algorithm">The algorithm to check.</param>
    /// <returns><c>true</c> if the algorithm is ECC (P-256, P-384, Ed25519, or X25519); otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// Note that Ed25519 and X25519 are Curve25519-based algorithms that are not compatible with
    /// .NET's <see cref="ECDsa"/> class. Use this method to check for any elliptic curve algorithm,
    /// but be aware that only P-256 and P-384 work with <see cref="ECDsa"/>.
    /// </para>
    /// </remarks>
    public static bool IsEcc(this PivAlgorithm algorithm) =>
        algorithm is PivAlgorithm.EccP256 or PivAlgorithm.EccP384 or 
                     PivAlgorithm.Ed25519 or PivAlgorithm.X25519;
}
