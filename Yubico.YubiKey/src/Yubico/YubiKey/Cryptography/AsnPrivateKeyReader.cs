// Copyright 2024 Yubico AB
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

public class AsnPrivateKeyReader
{
    public static ECParameters CreateECParameters(ReadOnlyMemory<byte> encodedKey)
    {
        var reader = new AsnReader(encodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();

        // PKCS#8 starts with a version (integer 0)
        var version = seqPrivateKeyInfo.ReadInteger();
        if (version != 0)
        {
            throw new CryptographicException("Invalid PKCS#8 private key format: unexpected version");
        }

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();

        string oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (oidAlgorithm != KeyDefinitions.CryptoOids.EC)
        {
            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        string oidCurve = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (oidCurve is not (
            KeyDefinitions.CryptoOids.P256 or
            KeyDefinitions.CryptoOids.P384 or
            KeyDefinitions.CryptoOids.P521))
        {
            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        return CreateEcPrivateKeyParameters(seqPrivateKeyInfo, oidCurve);
    }

    public static RSAParameters CreateRSAParameters(ReadOnlyMemory<byte> encodedKey)
    {
        var reader = new AsnReader(encodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();

        // PKCS#8 starts with a version (integer 0)
        var version = seqPrivateKeyInfo.ReadInteger();
        if (version != 0)
        {
            throw new CryptographicException("Invalid PKCS#8 private key format: unexpected version");
        }

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();

        string oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (oidAlgorithm != KeyDefinitions.CryptoOids.RSA)
        {
            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }
        
        return CreateRsaPrivateKeyParameters(seqPrivateKeyInfo);
    }

    public static IPrivateKeyParameters DecodePkcs8EncodedKey(ReadOnlyMemory<byte> encodedKey)
    {
        var reader = new AsnReader(encodedKey, AsnEncodingRules.DER);
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
            case KeyDefinitions.CryptoOids.RSA:
                {
                    if (seqAlgorithmIdentifier.HasData)
                    {
                        seqAlgorithmIdentifier.ReadNull();
                        seqAlgorithmIdentifier.ThrowIfNotEmpty();
                    }

                    var rsaParameters= CreateRsaPrivateKeyParameters(seqPrivateKeyInfo);
                    return new RSAPrivateKeyParameters(rsaParameters);
                }
            case KeyDefinitions.CryptoOids.EC:
                {
                    string oidCurve = seqAlgorithmIdentifier.ReadObjectIdentifier();

                    if (oidCurve is not (KeyDefinitions.CryptoOids.P256 or KeyDefinitions.CryptoOids.P384
                        or KeyDefinitions.CryptoOids.P521))
                    {
                        throw new NotSupportedException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.UnsupportedAlgorithm));
                    }

                    var ecParams = CreateEcPrivateKeyParameters(seqPrivateKeyInfo, oidCurve);
                    return new ECPrivateKeyParameters(ecParams);
                }

            // case KeyDefinitions.KeyOids.X25519:
            //     {
            //         return CreateX25519PrivateKeyParameters(seqPrivateKeyInfo, encodedKey);
            //     }
            // case KeyDefinitions.KeyOids.Ed25519:
            //     {
            //         return CreateEd25519PrivateKeyParameters(seqPrivateKeyInfo, encodedKey);
            //     }
            case KeyDefinitions.CryptoOids.X25519:
            case KeyDefinitions.CryptoOids.Ed25519:
                {
                    return Curve25519PrivateKeyParameters.CreateFromPkcs8(encodedKey);
                }
        }

        throw new NotSupportedException(
            string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.UnsupportedAlgorithm));
    }

    private static RSAParameters CreateRsaPrivateKeyParameters(AsnReader seqPrivateKeyInfo)
    {
        // Read private key octet string
        ReadOnlyMemory<byte> privateKeyData = seqPrivateKeyInfo.ReadOctetString();
        seqPrivateKeyInfo.ThrowIfNotEmpty();

        // Parse the RSA private key structure
        var privateKeyReader = new AsnReader(privateKeyData, AsnEncodingRules.DER);
        var seqRsaPrivateKey = privateKeyReader.ReadSequence();

        // RSA private key sequence: Version, modulus, publicExponent, privateExponent, prime1, prime2, exponent1, exponent2, coefficient
        var rsaVersion = seqRsaPrivateKey.ReadInteger();
        if (rsaVersion != 0)
        {
            throw new CryptographicException("Invalid RSA private key format: unexpected version");
        }

        var modulus = seqRsaPrivateKey.ReadIntegerBytes();
        var publicExponent = seqRsaPrivateKey.ReadIntegerBytes();
        var privateExponent = seqRsaPrivateKey.ReadIntegerBytes();
        var prime1 = seqRsaPrivateKey.ReadIntegerBytes();
        var prime2 = seqRsaPrivateKey.ReadIntegerBytes();
        var exponent1 = seqRsaPrivateKey.ReadIntegerBytes();
        var exponent2 = seqRsaPrivateKey.ReadIntegerBytes();
        var coefficient = seqRsaPrivateKey.ReadIntegerBytes();

        // Remove leading zeros where needed
        byte[] modulusBytes = TrimLeadingZero(modulus).ToArray();
        byte[] exponentBytes = TrimLeadingZero(publicExponent).ToArray();
        byte[] dBytes = TrimLeadingZero(privateExponent).ToArray();
        byte[] p = TrimLeadingZero(prime1).ToArray();
        byte[] q = TrimLeadingZero(prime2).ToArray();
        byte[] dp = TrimLeadingZero(exponent1).ToArray();
        byte[] dq = TrimLeadingZero(exponent2).ToArray();
        byte[] inverseQ = TrimLeadingZero(coefficient).ToArray();

        var rsaParameters = new RSAParameters
        {
            Modulus = modulusBytes,
            Exponent = exponentBytes,
            D = dBytes,
            P = p,
            Q = q,
            DP = dp,
            DQ = dq,
            InverseQ = inverseQ
        };

        return rsaParameters;
    }

    private static ECParameters CreateEcPrivateKeyParameters(AsnReader seqPrivateKeyInfo, string oidCurve)
    {
        // Read private key octet string
        ReadOnlyMemory<byte> privateKeyData = seqPrivateKeyInfo.ReadOctetString();
        seqPrivateKeyInfo.ThrowIfNotEmpty();

        // Parse the EC private key structure
        var privateKeyReader = new AsnReader(privateKeyData, AsnEncodingRules.BER);
        var seqEcPrivateKey = privateKeyReader.ReadSequence();

        // EC private key sequence: Version, privateKey, [0] parameters (optional), [1] publicKey (optional)
        var ecVersion = seqEcPrivateKey.ReadInteger();
        if (ecVersion != 1)
        {
            throw new CryptographicException("Invalid EC private key format: unexpected version");
        }

        byte[] dValue = seqEcPrivateKey.ReadOctetString();

        // Get the public key if present (optional [1] tag)
        ECPoint point = default;

        // Check for optional parameters and public key
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
                if (publicKeyBytes.Length > 0 && publicKeyBytes.Span[0] == 0x04) // Uncompressed point format
                {
                    int coordinateSize = GetCoordinateSizeFromCurve(oidCurve);

                    if (publicKeyBytes.Length == (2 * coordinateSize) + 1) // Format: 0x04 + X + Y
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

        // Create ECParameters
        var curve = GetCurveFromOid(oidCurve);
        var ecParams = new ECParameters
        {
            Curve = curve,
            D = dValue,
            Q = point
        };

        return ecParams;
    }

    // private static IPrivateKeyParameters CreateX25519PrivateKeyParameters(
    //     AsnReader seqPrivateKeyInfo,
    //     ReadOnlyMemory<byte> encodedKey)
    // {
    //     var seqPrivateKey = new AsnReader(seqPrivateKeyInfo.ReadOctetString(), AsnEncodingRules.DER);
    //     var tag = seqPrivateKey.PeekTag();
    //     if (tag.TagValue != 4 || tag.TagClass != TagClass.Universal)
    //     {
    //         throw new CryptographicException("Invalid X25519 private key");
    //     }
    //     byte[] privateKeyData = seqPrivateKey.ReadOctetString();
    //     seqPrivateKeyInfo.ThrowIfNotEmpty();
    //     if (privateKeyData.Length != 32)
    //     {
    //         throw new CryptographicException("Invalid X25519 private key: incorrect length");
    //     }
    //
    //     var keyDefinition = KeyDefinitions.GetByOid(KeyDefinitions.KeyOids.X25519, OidType.AlgorithmId);
    //     return new ECX25519PrivateKeyParameters(encodedKey, privateKeyData, keyDefinition);
    // }

    // private static IPrivateKeyParameters CreateEd25519PrivateKeyParameters(
    //     AsnReader seqPrivateKeyInfo,
    //     ReadOnlyMemory<byte> encodedKey)
    // {
    //     var seqPrivateKey = new AsnReader(seqPrivateKeyInfo.ReadOctetString(), AsnEncodingRules.DER);
    //     var tag = seqPrivateKey.PeekTag();
    //     if (tag.TagValue != 4 || tag.TagClass != TagClass.Universal)
    //     {
    //         throw new CryptographicException("Invalid Ed25519 private key");
    //     }
    //     byte[] privateKeyData = seqPrivateKey.ReadOctetString();
    //     seqPrivateKeyInfo.ThrowIfNotEmpty();
    //     if (privateKeyData.Length != 32)
    //     {
    //         throw new CryptographicException("Invalid Ed25519 private key: incorrect length");
    //     }
    //
    //     var keyDefinition = KeyDefinitions.GetByOid(KeyDefinitions.KeyOids.Ed25519, OidType.AlgorithmId);
    //     return new EDsaPrivateKeyParameters(encodedKey, privateKeyData, keyDefinition);
    // }

    private static ECCurve GetCurveFromOid(string oidCurve)
    {
        switch (oidCurve)
        {
            case KeyDefinitions.CryptoOids.P256:
                return ECCurve.NamedCurves.nistP256;
            case KeyDefinitions.CryptoOids.P384:
                return ECCurve.NamedCurves.nistP384;
            case KeyDefinitions.CryptoOids.P521:
                return ECCurve.NamedCurves.nistP521;
            default:
                throw new NotSupportedException($"Curve OID {oidCurve} is not supported");
        }
    }

    private static int GetCoordinateSizeFromCurve(string oidCurve)
    {
        var keyDef = KeyDefinitions.GetByOid(oidCurve);
        return keyDef.LengthInBytes;
    }

    private static ReadOnlyMemory<byte> TrimLeadingZero(ReadOnlyMemory<byte> data)
    {
        if (data.Length > 1 && data.Span[0] == 0)
        {
            byte[] result = new byte[data.Length - 1];
            data[1..].CopyTo(result);
            return result;
        }

        return data;
    }
}
