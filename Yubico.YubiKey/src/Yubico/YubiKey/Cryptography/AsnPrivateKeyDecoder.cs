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

using System;
using System.Formats.Asn1;
using System.Globalization;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

/// <summary>
/// A class that converts ASN.1 DER encoded private keys to parameters and values.
/// </summary>
internal class AsnPrivateKeyDecoder
{
    /// <summary>
    /// Creates an instance of <see cref="IPrivateKey"/> from a PKCS#8
    /// ASN.1 DER-encoded private key.
    /// </summary>
    /// <param name="pkcs8EncodedKey">
    /// The ASN.1 DER-encoded private key.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="IPrivateKey"/>.
    /// </returns>
    /// <exception cref="CryptographicException">Thrown if privateKey does not match expected format.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the algorithm is not supported</exception>
    public static IPrivateKey CreatePrivateKey(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        var reader = new AsnReader(pkcs8EncodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();

        // PKCS#8 starts with a version (integer 0)
        var version = seqPrivateKeyInfo.ReadInteger();
        if (version != 0)
        {
            throw new CryptographicException("Invalid PKCS#8 private key format: unexpected version");
        }

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();
        string oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        switch (oidAlgorithm)
        {
            case Oids.RSA:
                {
                    if (seqAlgorithmIdentifier.HasData)
                    {
                        seqAlgorithmIdentifier.ReadNull();
                        seqAlgorithmIdentifier.ThrowIfNotEmpty();
                    }

                    var rsaParameters = CreateRSAParameters(pkcs8EncodedKey);
                    return RSAPrivateKey.CreateFromParameters(rsaParameters);
                }
            case Oids.ECDSA:
                {
                    var ecParams = CreateECParameters(pkcs8EncodedKey);
                    return ECPrivateKey.CreateFromParameters(ecParams);
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
                ExceptionMessages.UnsupportedAlgorithm));
    }

    /// <summary>
    /// Creates an instance of <see cref="Curve25519PrivateKey"/> from a PKCS#8
    /// ASN.1 DER-encoded private key.
    /// </summary>
    /// <param name="pkcs8EncodedKey">
    /// The ASN.1 DER-encoded private key.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="Curve25519PrivateKey"/>.
    /// </returns>
    /// <exception cref="CryptographicException">Thrown if privateKey does not match expected format.</exception>
    /// <exception cref="ArgumentException">Thrown if the algorithm is not <see cref="Oids.X25519"/> or 
    /// <see cref="Oids.Ed25519"/></exception>
    public static Curve25519PrivateKey CreateCurve25519Key(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        (byte[] privateKey, var keyType) = GetCurve25519PrivateKeyData(pkcs8EncodedKey);
        using var privateKeyHandle = new ZeroingMemoryHandle(privateKey);
        return Curve25519PrivateKey.CreateFromValue(privateKeyHandle.Data, keyType);
    }

    public static (byte[] privateKey, KeyType keyType) GetCurve25519PrivateKeyData(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        var reader = new AsnReader(pkcs8EncodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();
        var version = seqPrivateKeyInfo.ReadInteger();
        if (version != 0)
        {
            throw new CryptographicException("Invalid PKCS#8 private key format: unexpected version");
        }

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();
        string algorithmOid = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (!Oids.IsCurve25519Algorithm(algorithmOid))
        {
            throw new ArgumentException(
                "Invalid curve OID. Must be: " + Oids.X25519 + " or " +
                Oids.Ed25519);
        }

        using var privateKeyDataHandle = new ZeroingMemoryHandle(seqPrivateKeyInfo.ReadOctetString());
        var seqPrivateKey = new AsnReader(privateKeyDataHandle.Data, AsnEncodingRules.DER);
        var tag = seqPrivateKey.PeekTag();
        if (tag.TagValue != 4 || tag.TagClass != TagClass.Universal)
        {
            throw new CryptographicException("Invalid Curve25519 private key");
        }

        byte[] privateKey = seqPrivateKey.ReadOctetString();
        if (privateKey.Length != 32)
        {
            throw new CryptographicException("Invalid Curve25519 private key: incorrect length");
        }

        seqPrivateKeyInfo.ThrowIfNotEmpty();

        var keyDefinition = KeyDefinitions.GetByOid(algorithmOid);
        return (privateKey, keyDefinition.KeyType);
    }

    public static ECParameters CreateECParameters(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        var reader = new AsnReader(pkcs8EncodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();

        // PKCS#8 starts with a version (integer 0)
        var version = seqPrivateKeyInfo.ReadInteger();
        if (version != 0)
        {
            throw new CryptographicException("Invalid PKCS#8 private key format: unexpected version");
        }

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();
        string oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (oidAlgorithm != Oids.ECDSA)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        string curveOid = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (curveOid is not (
            Oids.ECP256 or
            Oids.ECP384 or
            Oids.ECP521))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        using var privateKeyInfoHandle = new ZeroingMemoryHandle(seqPrivateKeyInfo.ReadOctetString());
        seqPrivateKeyInfo.ThrowIfNotEmpty();

        var privateKeyReader = new AsnReader(privateKeyInfoHandle.Data, AsnEncodingRules.BER);
        var seqEcPrivateKey = privateKeyReader.ReadSequence();

        // EC private key sequence: Version, privateKey, [0] parameters (optional), [1] publicKey (optional)
        var ecVersion = seqEcPrivateKey.ReadInteger();
        if (ecVersion != 1)
        {
            throw new CryptographicException("Invalid EC private key format: unexpected version");
        }

        using var privateKeyHandle = new ZeroingMemoryHandle(seqEcPrivateKey.ReadOctetString());

        // Check for optional parameters and public key
        ECPoint point = default;
        while (seqEcPrivateKey.HasData)
        {
            var tag = seqEcPrivateKey.PeekTag();
            if (tag is { TagValue: 1, TagClass: TagClass.ContextSpecific })
            {
                ReadOnlyMemory<byte> publicKeyBytes = seqEcPrivateKey.ReadBitString(out int unusedBits, tag);
                if (unusedBits != 0)
                {
                    throw new CryptographicException("Invalid EC public key encoding");
                }

                // Process the public key point
                if (publicKeyBytes.Span[0] == 0x04) // Uncompressed point format
                {
                    int coordinateSize = AsnUtilities.GetCoordinateSizeFromCurve(curveOid);
                    bool sizeIsValid = publicKeyBytes.Length == (2 * coordinateSize) + 1;
                    if (sizeIsValid) // Format: 0x04 + X + Y
                    {
                        byte[] xCoordinate = new byte[coordinateSize];
                        byte[] yCoordinate = new byte[coordinateSize];

                        publicKeyBytes.Slice(1, coordinateSize).CopyTo(xCoordinate);
                        publicKeyBytes.Slice(1 + coordinateSize, coordinateSize).CopyTo(yCoordinate);

                        point = new ECPoint
                        {
                            X = xCoordinate,
                            Y = yCoordinate
                        };
                    }
                }
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

    public static RSAParameters CreateRSAParameters(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        var reader = new AsnReader(pkcs8EncodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();

        // PKCS#8 starts with a version (integer 0)
        var version = seqPrivateKeyInfo.ReadInteger();
        if (version != 0)
        {
            throw new CryptographicException("Invalid PKCS#8 private key format: unexpected version");
        }

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();
        string oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (oidAlgorithm != Oids.RSA)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        using var privateKeyDataHandle = new ZeroingMemoryHandle(seqPrivateKeyInfo.ReadOctetString());
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

        return rsaParameters.NormalizeParameters();
    }
}
