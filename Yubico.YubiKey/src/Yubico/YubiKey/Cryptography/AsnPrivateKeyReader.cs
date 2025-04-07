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
            case KeyDefinitions.Oids.RSA:
                {
                    if (seqAlgorithmIdentifier.HasData)
                    {
                        seqAlgorithmIdentifier.ReadNull();
                        seqAlgorithmIdentifier.ThrowIfNotEmpty();
                    }

                    var rsaParameters = CreateRSAParameters(pkcs8EncodedKey);
                    return RSAPrivateKeyParameters.CreateFromParameters(rsaParameters);
                }
            case KeyDefinitions.Oids.ECDSA:
                {
                    var ecParams = CreateECParameters(pkcs8EncodedKey);
                    return ECPrivateKeyParameters.CreateFromParameters(ecParams);
                }
            case KeyDefinitions.Oids.X25519:
            case KeyDefinitions.Oids.Ed25519:
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
        if (oidAlgorithm != KeyDefinitions.Oids.ECDSA)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.UnsupportedAlgorithm));
        }

        string curveOid = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (curveOid is not (
            KeyDefinitions.Oids.P256 or
            KeyDefinitions.Oids.P384 or
            KeyDefinitions.Oids.P521))
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

        var curve = ECCurve.CreateFromValue(curveOid);
        byte[] privateKey = privateKeyHandle.Data.ToArray();
        var ecParams = new ECParameters
        {
            Curve = curve,
            D = privateKey,
            Q = point
        };

        return ecParams;
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
        if (oidAlgorithm != KeyDefinitions.Oids.RSA)
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
