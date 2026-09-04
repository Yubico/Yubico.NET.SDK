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

using System.Formats.Asn1;
using System.Globalization;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// A class that converts ASN.1 DER encoded private keys to parameters and values.
/// </summary>
internal class AsnPrivateKeyDecoder
{
    private const string Rfc5958Version1UnsupportedMessage =
        "RFC 5958 version value 1 OneAsymmetricKey is valid but unsupported.";

    /// <summary>
    /// Creates an instance of <see cref="IPrivateKey"/> from a PKCS#8
    /// ASN.1 DER-encoded private key.
    /// </summary>
    /// <param name="pkcs8EncodedKey">
    /// The borrowed ASN.1 DER-encoded private key. This method does not modify or clear the input.
    /// </param>
    /// <returns>
    /// A new disposable private-key object that owns its decoded key material.
    /// </returns>
    /// <exception cref="CryptographicException">Thrown if privateKey does not match expected format.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the algorithm is not supported</exception>
    public static IPrivateKey CreatePrivateKey(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        var reader = new AsnReader(pkcs8EncodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();

        // PKCS#8 starts with a version (integer 0)
        ReadAndValidatePkcs8Version(seqPrivateKeyInfo);

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();
        var oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        switch (oidAlgorithm)
        {
            case Oids.RSA:
                {
                    if (seqAlgorithmIdentifier.HasData)
                    {
                        seqAlgorithmIdentifier.ReadNull();
                        seqAlgorithmIdentifier.ThrowIfNotEmpty();
                    }

                    return RSAPrivateKey.CreateFromPkcs8(pkcs8EncodedKey);
                }
            case Oids.ECDSA:
                {
                    return ECPrivateKey.CreateFromPkcs8(pkcs8EncodedKey);
                }
            case Oids.X25519:
            case Oids.Ed25519:
                {
                    return Curve25519PrivateKey.CreateFromPkcs8(pkcs8EncodedKey);
                }
        }

        throw new InvalidOperationException(
            string.Format(
                CultureInfo.CurrentCulture,
                "Unsupported private key algorithm OID '{0}'.",
                oidAlgorithm));
    }

    /// <summary>
    /// Creates an instance of <see cref="Curve25519PrivateKey"/> from a PKCS#8
    /// ASN.1 DER-encoded private key.
    /// </summary>
    /// <param name="pkcs8EncodedKey">
    /// The borrowed ASN.1 DER-encoded private key. This method does not modify or clear the input.
    /// </param>
    /// <returns>
    /// A new disposable private-key object that owns its decoded key material.
    /// </returns>
    /// <exception cref="CryptographicException">Thrown if privateKey does not match expected format.</exception>
    /// <exception cref="ArgumentException">Thrown if the algorithm is not <see cref="Oids.X25519"/> or 
    /// <see cref="Oids.Ed25519"/></exception>
    public static Curve25519PrivateKey CreateCurve25519Key(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        (var privateKey, var keyType) = GetCurve25519PrivateKeyData(pkcs8EncodedKey);
        using var privateKeyHandle = new DisposableBufferHandle(privateKey);
        return Curve25519PrivateKey.CreateFromValue(privateKeyHandle.Data, keyType);
    }

    /// <summary>
    /// Decodes a Curve25519 private value and its key type from a PKCS#8 private key.
    /// </summary>
    /// <param name="pkcs8EncodedKey">
    /// The borrowed ASN.1 DER-encoded private key. This method does not modify or clear the input.
    /// </param>
    /// <returns>
    /// A caller-owned 32-byte private-value array and the decoded key type. The caller must clear
    /// the returned array when it is no longer needed.
    /// </returns>
    /// <exception cref="ArgumentException">The algorithm is not X25519 or Ed25519.</exception>
    /// <exception cref="CryptographicException">The private-key encoding or length is invalid.</exception>
    public static (byte[] privateKey, KeyType keyType) GetCurve25519PrivateKeyData(ReadOnlyMemory<byte> pkcs8EncodedKey) =>
        GetCurve25519PrivateKeyData(pkcs8EncodedKey, privateKeyDecoded: null);

    // Test observation hook. The callback must not retain or mutate the decoded private value.
    internal static (byte[] privateKey, KeyType keyType) GetCurve25519PrivateKeyData(
        ReadOnlyMemory<byte> pkcs8EncodedKey,
        Action<byte[]>? privateKeyDecoded)
    {
        var reader = new AsnReader(pkcs8EncodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();
        ReadAndValidatePkcs8Version(seqPrivateKeyInfo);

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();
        var algorithmOid = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (!Oids.IsCurve25519Algorithm(algorithmOid))
        {
            throw new ArgumentException(
                "Invalid curve OID. Must be: " + Oids.X25519 + " or " +
                Oids.Ed25519);
        }

        using var privateKeyDataHandle = new DisposableBufferHandle(seqPrivateKeyInfo.ReadOctetString());
        var seqPrivateKey = new AsnReader(privateKeyDataHandle.Data, AsnEncodingRules.DER);
        var tag = seqPrivateKey.PeekTag();
        if (tag.TagValue != 4 || tag.TagClass != TagClass.Universal)
        {
            throw new CryptographicException("Invalid Curve25519 private key");
        }

        var privateKey = seqPrivateKey.ReadOctetString();
        try
        {
            privateKeyDecoded?.Invoke(privateKey);
            if (privateKey.Length != 32)
            {
                throw new CryptographicException("Invalid Curve25519 private key: incorrect length");
            }

            seqPrivateKeyInfo.ThrowIfNotEmpty();

            var keyDefinition = KeyDefinitions.GetByOid(algorithmOid);
            return (privateKey, keyDefinition.KeyType);
        }
        catch
        {
            // This method owns the array unless it returns it successfully. On any exception the
            // array is about to become unreachable, so no caller-owned buffer is cleared here.
            CryptographicOperations.ZeroMemory(privateKey);
            throw;
        }
    }

    /// <summary>
    /// Decodes elliptic-curve parameters from a PKCS#8 private key.
    /// </summary>
    /// <param name="pkcs8EncodedKey">
    /// The borrowed ASN.1 DER-encoded private key. This method does not modify or clear the input.
    /// </param>
    /// <returns>
    /// Parameters containing caller-owned arrays. The caller must clear <see cref="ECParameters.D"/>
    /// when the parameters are no longer needed.
    /// </returns>
    /// <exception cref="CryptographicException">The private-key encoding is invalid.</exception>
    /// <exception cref="InvalidOperationException">The algorithm or curve is unsupported.</exception>
    public static ECParameters CreateECParameters(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        var reader = new AsnReader(pkcs8EncodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();

        // PKCS#8 starts with a version (integer 0)
        ReadAndValidatePkcs8Version(seqPrivateKeyInfo);

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();
        var oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (oidAlgorithm != Oids.ECDSA)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Expected an EC private key using id-ecPublicKey, but the algorithm OID was '{0}'.",
                    oidAlgorithm));
        }

        var curveOid = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (curveOid is not (
            Oids.ECP256 or
            Oids.ECP384 or
            Oids.ECP521))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Unsupported EC curve OID '{0}'. Supported curves are P-256, P-384 and P-521.",
                    curveOid));
        }

        using var privateKeyInfoHandle = new DisposableBufferHandle(seqPrivateKeyInfo.ReadOctetString());
        seqPrivateKeyInfo.ThrowIfNotEmpty();

        var privateKeyReader = new AsnReader(privateKeyInfoHandle.Data, AsnEncodingRules.BER);
        var seqEcPrivateKey = privateKeyReader.ReadSequence();

        // EC private key sequence: Version, privateKey, [0] parameters (optional), [1] publicKey (optional)
        var ecVersion = seqEcPrivateKey.ReadInteger();
        if (ecVersion != 1)
        {
            throw new CryptographicException("Invalid EC private key format: unexpected version");
        }

        using var privateKeyHandle = new DisposableBufferHandle(seqEcPrivateKey.ReadOctetString());

        // Check for optional parameters and public key
        ECPoint point = default;
        while (seqEcPrivateKey.HasData)
        {
            var tag = seqEcPrivateKey.PeekTag();
            if (tag is { TagValue: 1, TagClass: TagClass.ContextSpecific })
            {
                ReadOnlyMemory<byte> publicKeyBytes = seqEcPrivateKey.ReadBitString(out var unusedBits, tag);
                if (unusedBits != 0)
                {
                    throw new CryptographicException("Invalid EC public key encoding");
                }

                // Process the public key point
                if (publicKeyBytes.Length == 0)
                {
                    throw new CryptographicException("Invalid EC public key encoding");
                }

                if (publicKeyBytes.Span[0] != 0x04) // Uncompressed point format
                {
                    throw new CryptographicException("Unsupported EC point format");
                }

                var coordinateSize = AsnUtilities.GetCoordinateSizeFromCurve(curveOid);
                if (publicKeyBytes.Length != (2 * coordinateSize) + 1) // Format: 0x04 + X + Y
                {
                    throw new CryptographicException("Invalid EC public key encoding");
                }

                var xCoordinate = new byte[coordinateSize];
                var yCoordinate = new byte[coordinateSize];

                publicKeyBytes.Slice(1, coordinateSize).CopyTo(xCoordinate);
                publicKeyBytes.Slice(1 + coordinateSize, coordinateSize).CopyTo(yCoordinate);

                point = new ECPoint
                {
                    X = xCoordinate,
                    Y = yCoordinate
                };
            }
            else
            {
                // Skip other optional fields
                _ = seqEcPrivateKey.ReadEncodedValue();
            }
        }

        return new ECParameters
        {
            Curve = ECCurve.CreateFromValue(curveOid),
            D = privateKeyHandle.Data.ToArray(),
            Q = point
        };
    }

    /// <summary>
    /// Decodes RSA parameters from a PKCS#8 private key.
    /// </summary>
    /// <param name="pkcs8EncodedKey">
    /// The borrowed ASN.1 DER-encoded private key. This method does not modify or clear the input.
    /// </param>
    /// <returns>
    /// Parameters containing caller-owned arrays. The caller must clear the private parameter
    /// arrays when they are no longer needed.
    /// </returns>
    /// <exception cref="CryptographicException">The private-key encoding is invalid.</exception>
    /// <exception cref="InvalidOperationException">The algorithm is unsupported.</exception>
    public static RSAParameters CreateRSAParameters(ReadOnlyMemory<byte> pkcs8EncodedKey) =>
        CreateRSAParameters(pkcs8EncodedKey, parametersDecoded: null);

    // Test observation hook. The callback must not retain or mutate the decoded private arrays.
    internal static RSAParameters CreateRSAParameters(
        ReadOnlyMemory<byte> pkcs8EncodedKey,
        Action<RSAParameters>? parametersDecoded)
    {
        var reader = new AsnReader(pkcs8EncodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();

        // PKCS#8 starts with a version (integer 0)
        ReadAndValidatePkcs8Version(seqPrivateKeyInfo);

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();
        var oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (oidAlgorithm != Oids.RSA)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Expected an RSA private key, but the algorithm OID was '{0}'.",
                    oidAlgorithm));
        }

        using var privateKeyDataHandle = new DisposableBufferHandle(seqPrivateKeyInfo.ReadOctetString());
        seqPrivateKeyInfo.ThrowIfNotEmpty();

        var privateKeyReader = new AsnReader(privateKeyDataHandle.Data, AsnEncodingRules.DER);
        var seqRsaPrivateKey = privateKeyReader.ReadSequence();

        // RSA private key sequence: Version, modulus, publicExponent, privateExponent, prime1, prime2, exponent1, exponent2, coefficient
        var rsaVersion = seqRsaPrivateKey.ReadInteger();
        if (rsaVersion != 0)
        {
            throw new CryptographicException("Invalid RSA private key format: unexpected version");
        }

        var modulus = AsnUtilities.TrimLeadingZeroes(seqRsaPrivateKey.ReadIntegerBytes().Span);
        var publicExponent = AsnUtilities.TrimLeadingZeroes(seqRsaPrivateKey.ReadIntegerBytes().Span);
        var privateExponent = AsnUtilities.TrimLeadingZeroes(seqRsaPrivateKey.ReadIntegerBytes().Span);
        var prime1 = AsnUtilities.TrimLeadingZeroes(seqRsaPrivateKey.ReadIntegerBytes().Span);
        var prime2 = AsnUtilities.TrimLeadingZeroes(seqRsaPrivateKey.ReadIntegerBytes().Span);
        var exponent1 = AsnUtilities.TrimLeadingZeroes(seqRsaPrivateKey.ReadIntegerBytes().Span);
        var exponent2 = AsnUtilities.TrimLeadingZeroes(seqRsaPrivateKey.ReadIntegerBytes().Span);
        var coefficient = AsnUtilities.TrimLeadingZeroes(seqRsaPrivateKey.ReadIntegerBytes().Span);

        var rsaParameters = new RSAParameters
        {
            Modulus = modulus.ToArray(),
            Exponent = publicExponent.ToArray(),
            D = privateExponent.ToArray(),
            P = prime1.ToArray(),
            Q = prime2.ToArray(),
            DP = exponent1.ToArray(),
            DQ = exponent2.ToArray(),
            InverseQ = coefficient.ToArray()
        };

        try
        {
            parametersDecoded?.Invoke(rsaParameters);
            return rsaParameters.NormalizeParameters();
        }
        finally
        {
            // NormalizeParameters returns a deep copy on success. On failure this decoder is
            // unwinding. Either way, these locally allocated private arrays are not returned.
            CryptographicOperations.ZeroMemory(rsaParameters.D);
            CryptographicOperations.ZeroMemory(rsaParameters.P);
            CryptographicOperations.ZeroMemory(rsaParameters.Q);
            CryptographicOperations.ZeroMemory(rsaParameters.DP);
            CryptographicOperations.ZeroMemory(rsaParameters.DQ);
            CryptographicOperations.ZeroMemory(rsaParameters.InverseQ);
        }
    }

    private static void ReadAndValidatePkcs8Version(AsnReader privateKeyInfo)
    {
        var version = privateKeyInfo.ReadInteger();
        if (version != 0)
        {
            throw new CryptographicException(version == 1
                ? Rfc5958Version1UnsupportedMessage
                : "Invalid PKCS#8 private key format: unexpected version");
        }
    }
}