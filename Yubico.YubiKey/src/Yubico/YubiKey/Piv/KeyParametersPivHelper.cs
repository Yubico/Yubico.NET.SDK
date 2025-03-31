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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv;

public static class KeyParametersPivHelper
{
    // PIV TLV Encoding ---> IPrivateKeyParameters
    public static T CreatePrivateParametersFromPivEncoding<T>(ReadOnlyMemory<byte> pivEncodingBytes)
        where T : IPrivateKeyParameters
    {
        if (pivEncodingBytes.IsEmpty)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }

        byte tag = pivEncodingBytes.Span[0];
        IPrivateKeyParameters pkp = tag switch
        {
            _ when PivConstants.IsValidPrivateECTag(tag) => CreatePrivateEcFromPivEncoding(pivEncodingBytes),
            _ when PivConstants.IsValidPrivateRSATag(tag) => CreatePrivateRsaFromPivEncoding(pivEncodingBytes),
            _ => throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidPrivateKeyData))
        };

        return (T)pkp;
    }

    // PIV TLV Encoding ---> IPublicKeyParameters
    
    // Problem with this method is that we dont know if its a x25519 or ed25519 (or p256) as they share length
    // public static T CreatePublicParametersFromPivEncoding<T>(ReadOnlyMemory<byte> pivEncodingBytes)
    //     where T : IPublicKeyParameters
    // {
    //     if (pivEncodingBytes.IsEmpty)
    //     {
    //         throw new ArgumentException(
    //             string.Format(
    //                 CultureInfo.CurrentCulture,
    //                 ExceptionMessages.InvalidPrivateKeyData));
    //     }
    //
    //     // KeyType keyType = GetKeyType(pivEncodingBytes);
    //     int tag = GetKeyTag(pivEncodingBytes);
    //     IPublicKeyParameters pkp = tag switch
    //     {
    //         _ when PivConstants.IsValidPublicECTag(tag) && pivEncodingBytes.Length == 34 => CreatePublicCurve25519FromPivEncoding(pivEncodingBytes, KeyType.X25519), // temp
    //         _ when PivConstants.IsValidPublicECTag(tag) => CreatePublicEcFromPivEncoding(pivEncodingBytes),
    //         _ when PivConstants.IsValidPublicRSATag(tag) => CreatePublicRsaFromPivEncoding(pivEncodingBytes),
    //         _ => throw new ArgumentException(
    //             string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidPublicKeyData))
    //     };
    //
    //     return (T)pkp;
    // }

    public static IPublicKeyParameters CreatePublicParameters(ReadOnlyMemory<byte> value, KeyType keyType)
    {
        return keyType switch
        {
            KeyType.Ed25519 or KeyType.X25519 => KeyParametersPivHelper
                .CreatePublicCurve25519FromPivEncoding(value, keyType),
            KeyType.P256 or KeyType.P384 or KeyType.P521 => KeyParametersPivHelper
                .CreatePublicEcFromPivEncoding(value),
            KeyType.RSA1024 or KeyType.RSA2048 or KeyType.RSA3072 or KeyType.RSA4096 => KeyParametersPivHelper
                .CreatePublicRsaFromPivEncoding(value),
            _ => throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidApduResponseData))
        };
    }

    private static int GetKeyTag(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        var tlvReader = new TlvReader(pivEncodingBytes);
        int tag = tlvReader.PeekTag(2);
        if (tag == PivConstants.PublicKeyTag)
        {
            tlvReader = tlvReader.ReadNestedTlv(tag);
            return tlvReader.PeekTag();
        }

        return pivEncodingBytes.Span[0];
    }

    public static RSAPublicKeyParameters CreatePublicRsaFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        var (modulus, exponent) = PivEncodingReader.GetPublicRSAValues(pivEncodingBytes);
        var rsaParameters = new RSAParameters { Modulus = modulus.ToArray(), Exponent = exponent.ToArray() };
        return RSAPublicKeyParameters.CreateFromParameters(rsaParameters);
    }

    public static ECPublicKeyParameters CreatePublicEcFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        var publicPointData = PivEncodingReader.GetECPublicPointValues(pivEncodingBytes);
        if (publicPointData.Span[0] != 0x4)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData)
                );
        }

        var publicKeyData = publicPointData.Span[1..];
        int coordinateLength = publicKeyData.Length / 2;
        var keyDefinition = KeyDefinitions
            .GetEcKeyDefinitions()
            .Where(kd => kd.AlgorithmOid == KeyDefinitions.CryptoOids.ECDSA)
            .Single(kd => kd.LengthInBytes == coordinateLength);

        byte[]? x = publicPointData.Span.Slice(1, keyDefinition.LengthInBytes).ToArray();
        byte[]? y = publicPointData.Span.Slice(1 + keyDefinition.LengthInBytes, keyDefinition.LengthInBytes).ToArray();
        var parameters = new ECParameters
        {
            Q = new ECPoint { X = x, Y = y },
            Curve = ECCurve.CreateFromValue(keyDefinition.CurveOid)
        };

        return ECPublicKeyParameters.CreateFromParameters(parameters);
    }

    public static Curve25519PublicKeyParameters CreatePublicCurve25519FromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes, KeyType keyType)
    {
        var publicPoint = PivEncodingReader.GetECPublicPointValues(pivEncodingBytes);
        return Curve25519PublicKeyParameters.CreateFromValue(publicPoint, keyType);
    }

    public static ECPrivateKeyParameters CreatePrivateEcFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        if (TlvObject.TryParse(pivEncodingBytes.Span, out var tlv) && PivConstants.IsValidPrivateECTag(tlv.Tag))
        {
            switch (tlv.Tag)
            {
                case PivConstants.PrivateECDsaTag:
                    List<KeyDefinition> allowed =
                        [KeyDefinitions.P256, KeyDefinitions.P384, KeyDefinitions.P521];

                    var keyDefinition = allowed.Single(kd => kd.LengthInBytes == tlv.Value.Span.Length);
                    return ECPrivateKeyParameters.CreateFromValue(tlv.Value.Span.ToArray(), keyDefinition.KeyType);
                case PivConstants.PrivateECEd25519Tag:
                    return ECPrivateKeyParameters.CreateFromValue(tlv.Value.ToArray(), KeyType.Ed25519);
                case PivConstants.PrivateECX25519Tag:
                    return ECPrivateKeyParameters.CreateFromValue(tlv.Value.ToArray(), KeyType.X25519);
            }
        }

        throw new ArgumentException(
            string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.InvalidPrivateKeyData));
    }

    public static RSAPrivateKeyParameters CreatePrivateRsaFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        const int CrtComponentCount = 5;

        var tlvReader = new TlvReader(pivEncodingBytes);
        var valueArray = new ReadOnlyMemory<byte>[CrtComponentCount];

        int index = 0;
        for (; index < CrtComponentCount; index++)
        {
            valueArray[index] = ReadOnlyMemory<byte>.Empty;
        }

        index = 0;
        while (index < CrtComponentCount)
        {
            if (tlvReader.HasData == false)
            {
                break;
            }

            int tag = tlvReader.PeekTag();
            var temp = tlvReader.ReadValue(tag);
            if (tag <= 0 || tag > CrtComponentCount)
            {
                continue;
            }

            if (valueArray[tag - 1].IsEmpty == false)
            {
                continue;
            }

            index++;
            valueArray[tag - 1] = temp;
        }

        var primeP = valueArray[PivConstants.PrivateRSAPrimePTag - 1].Span;
        var primeQ = valueArray[PivConstants.PrivateRSAPrimeQTag - 1].Span;
        var exponentP = valueArray[PivConstants.PrivateRSAExponentPTag - 1].Span;
        var exponentQ = valueArray[PivConstants.PrivateRSAExponentQTag - 1].Span;
        var coefficient = valueArray[PivConstants.PrivateRSACoefficientTag - 1].Span;

        var rsaParameters = new RSAParameters
        {
            // D = privateExponent,      // Private exponent
            // Modulus = modulus,        // Modulus (n)
            // Exponent = publicExponent, // Public exponent (e)
            P = primeP.ToArray(), // First prime factor
            Q = primeQ.ToArray(), // Second prime factor
            DP = exponentP.ToArray(), // d mod (p-1)
            DQ = exponentQ.ToArray(), // d mod (q-1)
            InverseQ = coefficient.ToArray() // (q^-1) mod p
        };

        return new RSAPrivateKeyParameters(rsaParameters);
    }
}
