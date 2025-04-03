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
using System.Linq;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

internal class AsnPrivateKeyReader
{
    public static IPrivateKeyParameters CreateKeyParameters(ReadOnlyMemory<byte> pkcs8EncodedKey)
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
            case KeyDefinitions.CryptoOids.RSA:
                {
                    if (seqAlgorithmIdentifier.HasData)
                    {
                        seqAlgorithmIdentifier.ReadNull();
                        seqAlgorithmIdentifier.ThrowIfNotEmpty();
                    }

                    var rsaParameters = CreateRSAPrivateKeyParameters(seqPrivateKeyInfo);
                    return RSAPrivateKeyParameters.CreateFromParameters(rsaParameters);
                }
            case KeyDefinitions.CryptoOids.ECDSA:
                {
                    string oidCurve = seqAlgorithmIdentifier.ReadObjectIdentifier();

                    var ecParams = CreateECPrivateKeyParameters(seqPrivateKeyInfo, oidCurve);
                    return new ECPrivateKeyParameters(ecParams);
                }
            case KeyDefinitions.CryptoOids.X25519:
            case KeyDefinitions.CryptoOids.Ed25519:
                {
                    return Curve25519PrivateKeyParameters.CreateFromPkcs8(pkcs8EncodedKey);
                }
        }

        throw new InvalidOperationException(
            string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.UnsupportedAlgorithm));
    }

    public static Curve25519PrivateKeyParameters CreateCurve25519Parameters(ReadOnlyMemory<byte> pkcs8EncodedKey) =>
        Curve25519PrivateKeyParameters.CreateFromPkcs8(pkcs8EncodedKey);

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
        if (oidAlgorithm != KeyDefinitions.CryptoOids.ECDSA)
        {
            throw new InvalidOperationException(
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
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        return CreateECPrivateKeyParameters(seqPrivateKeyInfo, oidCurve);
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
        if (oidAlgorithm != KeyDefinitions.CryptoOids.RSA)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        return CreateRSAPrivateKeyParameters(seqPrivateKeyInfo);
    }

    private static RSAParameters CreateRSAPrivateKeyParameters(AsnReader seqPrivateKeyInfo)
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
        var modulusBytes = AsnUtilities.TrimLeadingZeroes(modulus.Span);
        var exponentBytes = AsnUtilities.TrimLeadingZeroes(publicExponent.Span);
        var dBytes = AsnUtilities.TrimLeadingZeroes(privateExponent.Span);
        var p = AsnUtilities.TrimLeadingZeroes(prime1.Span);
        var q = AsnUtilities.TrimLeadingZeroes(prime2.Span);
        var dp = AsnUtilities.TrimLeadingZeroes(exponent1.Span);
        var dq = AsnUtilities.TrimLeadingZeroes(exponent2.Span);
        var inverseQ = AsnUtilities.TrimLeadingZeroes(coefficient.Span);

        var rsaParameters = new RSAParameters
        {
            Modulus = modulusBytes.ToArray(),
            Exponent = exponentBytes.ToArray(),
            D = dBytes.ToArray(),
            P = p.ToArray(),
            Q = q.ToArray(),
            DP = dp.ToArray(),
            DQ = dq.ToArray(),
            InverseQ = inverseQ.ToArray()
        };

        // Apply normalization for cross-platform compatibility
        return rsaParameters.NormalizeParameters();
    }

    private static ECParameters CreateECPrivateKeyParameters(AsnReader seqPrivateKeyInfo, string curveOid)
    {
        if (curveOid is not (KeyDefinitions.CryptoOids.P256 or KeyDefinitions.CryptoOids.P384
            or KeyDefinitions.CryptoOids.P521))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        // Read private key octet string
        using var privateKeyHandle = new ZeroingMemoryHandle(seqPrivateKeyInfo.ReadOctetString());
        seqPrivateKeyInfo.ThrowIfNotEmpty();

        // Parse the EC private key structure
        var privateKeyReader = new AsnReader(privateKeyHandle.Data, AsnEncodingRules.BER);
        var seqEcPrivateKey = privateKeyReader.ReadSequence();

        // EC private key sequence: Version, privateKey, [0] parameters (optional), [1] publicKey (optional)
        var ecVersion = seqEcPrivateKey.ReadInteger();
        if (ecVersion != 1)
        {
            throw new CryptographicException("Invalid EC private key format: unexpected version");
        }

        using var dValueHandle = new ZeroingMemoryHandle(seqEcPrivateKey.ReadOctetString());

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

        var curve = ECCurve.CreateFromValue(curveOid);
        byte[] dValue = dValueHandle.Data.ToArray();
        var ecParams = new ECParameters
        {
            Curve = curve,
            D = dValue,
            Q = point
        };

        return ecParams;
    }
}
