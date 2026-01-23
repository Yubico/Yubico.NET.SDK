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
    /// PIV TLV tag for RSA modulus (n).
    /// </summary>
    private const int RsaModulusTag = 0x81;

    /// <summary>
    /// PIV TLV tag for RSA public exponent (e).
    /// </summary>
    private const int RsaExponentTag = 0x82;

    /// <summary>
    /// PIV TLV tag for ECC public point.
    /// </summary>
    private const int EccPointTag = 0x86;

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

        // PIV returns public key in TLV format:
        // Tag 0x81 = modulus (n)
        // Tag 0x82 = public exponent (e)
        var publicKeyBytes = metadata.PublicKey.Span;
        var tlvDict = TlvHelper.DecodeDictionary(publicKeyBytes);

        if (!tlvDict.TryGetValue(RsaModulusTag, out var modulusMemory))
        {
            throw new InvalidOperationException("RSA public key data is missing modulus (tag 0x81).");
        }

        if (!tlvDict.TryGetValue(RsaExponentTag, out var exponentMemory))
        {
            throw new InvalidOperationException("RSA public key data is missing exponent (tag 0x82).");
        }

        var rsaParams = new RSAParameters
        {
            Modulus = modulusMemory.ToArray(),
            Exponent = exponentMemory.ToArray()
        };

        var rsa = RSA.Create();
        try
        {
            rsa.ImportParameters(rsaParams);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
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

        // PIV returns public key in TLV format:
        // Tag 0x86 = EC point in uncompressed format (04 || X || Y)
        var publicKeyBytes = metadata.PublicKey.Span;
        var tlvDict = TlvHelper.DecodeDictionary(publicKeyBytes);

        if (!tlvDict.TryGetValue(EccPointTag, out var pointMemory))
        {
            throw new InvalidOperationException("ECC public key data is missing EC point (tag 0x86).");
        }

        var pointBytes = pointMemory.Span;

        // Uncompressed point format: 04 || X || Y
        if (pointBytes.Length == 0 || pointBytes[0] != 0x04)
        {
            throw new InvalidOperationException("ECC public key is not in uncompressed point format (expected 0x04 prefix).");
        }

        // Determine curve based on algorithm
        ECCurve curve = metadata.Algorithm switch
        {
            PivAlgorithm.EccP256 => ECCurve.NamedCurves.nistP256,
            PivAlgorithm.EccP384 => ECCurve.NamedCurves.nistP384,
            _ => throw new InvalidOperationException($"Unsupported ECC algorithm: {metadata.Algorithm}")
        };

        // Point format is 04 || X || Y, where X and Y are same length
        var coordinateLength = (pointBytes.Length - 1) / 2;
        var ecParams = new ECParameters
        {
            Curve = curve,
            Q = new ECPoint
            {
                X = pointBytes.Slice(1, coordinateLength).ToArray(),
                Y = pointBytes.Slice(1 + coordinateLength, coordinateLength).ToArray()
            }
        };

        var ecdsa = ECDsa.Create();
        try
        {
            ecdsa.ImportParameters(ecParams);
            return ecdsa;
        }
        catch
        {
            ecdsa.Dispose();
            throw;
        }
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
