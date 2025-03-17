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

public class AsnPublicKeyReader
{
    public static IPublicKeyParameters DecodeFromSpki(ReadOnlyMemory<byte> encodedKey)
    {
        var reader = new AsnReader(encodedKey, AsnEncodingRules.DER);
        var seqSubjectPublicKeyInfo = reader.ReadSequence();
        var seqAlgorithmIdentifier = seqSubjectPublicKeyInfo.ReadSequence();
        
        string oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        switch (oidAlgorithm)
        {
            case KeyDefinitions.KeyOids.Algorithm.Rsa:
                {
                    if (seqAlgorithmIdentifier.HasData)
                    {
                        seqAlgorithmIdentifier.ReadNull();
                        seqAlgorithmIdentifier.ThrowIfNotEmpty();
                    }
                    return CreateRsaPublicKeyParameters(seqSubjectPublicKeyInfo);
                }
            case KeyDefinitions.KeyOids.Algorithm.EllipticCurve:
                {
                    string oidCurve = seqAlgorithmIdentifier.ReadObjectIdentifier();
                    byte[] bitString = seqSubjectPublicKeyInfo.ReadBitString(out int unusedBitCount);
                    if (unusedBitCount != 0)
                    {
                        throw new CryptographicException("Invalid RSA public key encoding");
                    }

                    return CreateEcPublicKeyParameters(oidCurve, bitString);
                }
            case KeyDefinitions.KeyOids.Curve.X25519:
                {
                    byte[] subjectPublicKey = seqSubjectPublicKeyInfo.ReadBitString(out int unusedBitCount);
                    if (unusedBitCount != 0)
                    {
                        throw new CryptographicException("Invalid RSA public key encoding");
                    }

                    var keyDefinition = KeyDefinitions.GetByOid(KeyDefinitions.KeyOids.Curve.X25519, OidType.CurveOid);
                    return new ECX25519PublicKeyParameters(encodedKey, subjectPublicKey, keyDefinition);
                }
            case KeyDefinitions.KeyOids.Curve.Ed25519:
                {
                    byte[] subjectPublicKey = seqSubjectPublicKeyInfo.ReadBitString(out int unusedBitCount);
                    if (unusedBitCount != 0)
                    {
                        throw new CryptographicException("Invalid RSA public key encoding");
                    }
                    
                    var keyDefinition = KeyDefinitions.GetByOid(KeyDefinitions.KeyOids.Curve.Ed25519, OidType.CurveOid);
                    return new EDsaPublicKeyParameters(encodedKey, subjectPublicKey, keyDefinition);
                }
        }

        throw new NotSupportedException(
            string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.UnsupportedAlgorithm));
    }

    private static IPublicKeyParameters CreateRsaPublicKeyParameters(AsnReader seqSubjectPublicKeyInfo)
    {
        byte[] subjectPublicKey = seqSubjectPublicKeyInfo.ReadBitString(out int unusedBitCount);
        if (unusedBitCount != 0)
        {
            throw new CryptographicException("Invalid RSA public key encoding");
        }

        var subjectPublicKeyReader = new AsnReader(subjectPublicKey, AsnEncodingRules.DER);
        var seqSubjectPublicKey = subjectPublicKeyReader.ReadSequence();
        var modulusBigInt = seqSubjectPublicKey.ReadIntegerBytes();
        var exponentBigInt = seqSubjectPublicKey.ReadIntegerBytes();
        seqSubjectPublicKey.ThrowIfNotEmpty();

        byte[] modulus = TrimLeadingZero(modulusBigInt).ToArray();
        byte[] exponent = TrimLeadingZero(exponentBigInt).ToArray();

        var rsaParameters = new RSAParameters
            { Modulus = modulus, Exponent = exponent };
        
        return new RSAPublicKeyParameters(rsaParameters);
    }

    private static ECPublicKeyParameters CreateEcPublicKeyParameters(string oidCurve, byte[] bitString)
    {
        if (oidCurve is not (KeyDefinitions.KeyOids.Curve.P256 or KeyDefinitions.KeyOids.Curve.P384
            or KeyDefinitions.KeyOids.Curve.P521))
        {
            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        var curve = GetCurveFromOid(oidCurve);

        // For PKCS EC keys, the bit string contains the EC point in uncompressed form
        // Format is typically: 0x04 + X coordinate + Y coordinate
        if (bitString[0] != 0x04)
        {
            throw new CryptographicException("Unsupported EC point format");
        }

        // Split coordinates - for P-256, each coordinate is 32 bytes
        int coordinateSize = GetCoordinateSizeFromCurve(oidCurve);

        byte[] xCoordinate = new byte[coordinateSize];
        byte[] yCoordinate = new byte[coordinateSize];

        // Skip the first byte (0x04 indicating uncompressed point)
        Buffer.BlockCopy(bitString, 1, xCoordinate, 0, coordinateSize);
        Buffer.BlockCopy(bitString, 1 + coordinateSize, yCoordinate, 0, coordinateSize);

        var ecParams = new ECParameters
        {
            Curve = curve,
            Q = new ECPoint
            {
                X = xCoordinate,
                Y = yCoordinate
            }
        };

        return new ECPublicKeyParameters(ecParams);
    }

    private static ECCurve GetCurveFromOid(string oidCurve)
    {
        switch (oidCurve)
        {
            case KeyDefinitions.KeyOids.Curve.P256:
                return ECCurve.NamedCurves.nistP256;
            case KeyDefinitions.KeyOids.Curve.P384:
                return ECCurve.NamedCurves.nistP384;
            case KeyDefinitions.KeyOids.Curve.P521:
                return ECCurve.NamedCurves.nistP521;
            default:
                throw new NotSupportedException($"Curve OID {oidCurve} is not supported");
        }
    }

    private static int GetCoordinateSizeFromCurve(string oidCurve)
    {
        var keyDef = KeyDefinitions.GetByOid(oidCurve, OidType.CurveOid);
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
