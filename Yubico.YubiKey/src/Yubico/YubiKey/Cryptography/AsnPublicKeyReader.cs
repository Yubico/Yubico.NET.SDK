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

internal class AsnPublicKeyReader
{
    public static IPublicKey CreatePublicKey(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        var reader = new AsnReader(pkcs8EncodedKey, AsnEncodingRules.DER);
        var seqSubjectPublicKeyInfo = reader.ReadSequence();
        var seqAlgorithmIdentifier = seqSubjectPublicKeyInfo.ReadSequence();

        string oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        byte[] subjectPublicKey = seqSubjectPublicKeyInfo.ReadBitString(out int unusedBitCount);
        if (unusedBitCount != 0)
        {
            throw new CryptographicException("Invalid subject public key encoding");
        }

        switch (oidAlgorithm)
        {
            case Oids.RSA:
                {
                    if (seqAlgorithmIdentifier.HasData)
                    {
                        seqAlgorithmIdentifier.ReadNull();
                        seqAlgorithmIdentifier.ThrowIfNotEmpty();
                    }

                    return CreateRSAPublicKey(subjectPublicKey);
                }
            case Oids.ECDSA:
                {
                    string oidCurve = seqAlgorithmIdentifier.ReadObjectIdentifier();
                    return CreateECPublicKey(oidCurve, subjectPublicKey);
                }
            case Oids.X25519:
                {
                    return Curve25519PublicKey.CreateFromValue(subjectPublicKey, KeyType.X25519);
                }
            case Oids.Ed25519:
                {
                    return Curve25519PublicKey.CreateFromValue(subjectPublicKey, KeyType.Ed25519);
                }
        }

        throw new NotSupportedException(
            string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.UnsupportedAlgorithm));
    }

    private static RSAPublicKey CreateRSAPublicKey(byte[] subjectPublicKey)
    {
        var subjectPublicKeyReader = new AsnReader(subjectPublicKey, AsnEncodingRules.DER);
        var seqSubjectPublicKey = subjectPublicKeyReader.ReadSequence();
        var modulusBigInt = seqSubjectPublicKey.ReadIntegerBytes();
        var exponentBigInt = seqSubjectPublicKey.ReadIntegerBytes();

        seqSubjectPublicKey.ThrowIfNotEmpty();

        var modulus = AsnUtilities.TrimLeadingZeroes(modulusBigInt.Span);
        var exponent = AsnUtilities.TrimLeadingZeroes(exponentBigInt.Span);
        var rsaParameters = new RSAParameters
        {
            Modulus = modulus.ToArray(),
            Exponent = exponent.ToArray()
        };

        return RSAPublicKey.CreateFromParameters(rsaParameters);
    }

    private static ECPublicKey CreateECPublicKey(string curveOid, byte[] subjectPublicKey)
    {
        if (!Oids.IsECDsaCurve(curveOid))
        {
            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        // For PKCS EC keys, the bit string contains the EC point in uncompressed form
        // Format is typically: 0x04 + X coordinate + Y coordinate
        if (subjectPublicKey[0] != 0x04)
        {
            throw new CryptographicException("Unsupported EC point format");
        }

        int coordinateSize = AsnUtilities.GetCoordinateSizeFromCurve(curveOid);
        if (subjectPublicKey.Length != 1 + (2 * coordinateSize))
        {
            throw new CryptographicException("Invalid EC public key encoding");
        }
        byte[] xCoordinate = new byte[coordinateSize];
        byte[] yCoordinate = new byte[coordinateSize];

        // Skip the first byte (0x04 indicating uncompressed point)
        Buffer.BlockCopy(subjectPublicKey, 1, xCoordinate, 0, coordinateSize);
        Buffer.BlockCopy(subjectPublicKey, 1 + coordinateSize, yCoordinate, 0, coordinateSize);

        var curve = ECCurve.CreateFromValue(curveOid);
        var ecParams = new ECParameters
        {
            Curve = curve,
            Q = new ECPoint
            {
                X = xCoordinate,
                Y = yCoordinate
            }
        };

        return ECPublicKey.CreateFromParameters(ecParams);
    }
}
