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
using System.Security.Cryptography;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv;

public static class PivEncodingReader
{
    const int EccTag = 0x86;
    const int ModulusTag = 0x81;
    const int ExponentTag = 0x82;
    const int PublicKeyTag = 0x7F49;
    const int PublicComponentCount = 2;

    public static (ReadOnlyMemory<byte> Modulus, ReadOnlyMemory<byte> Exponent) GetPublicRsaValues(
        ReadOnlyMemory<byte> encodedPublicKey)
    {
        var tlvReader = new TlvReader(encodedPublicKey);
        int tag = tlvReader.PeekTag(2);
        if (tag == PublicKeyTag)
        {
            tlvReader = tlvReader.ReadNestedTlv(tag);
        }

        var valueArray = new ReadOnlyMemory<byte>[PublicComponentCount];

        while (tlvReader.HasData)
        {
            tag = tlvReader.PeekTag();
            int valueIndex = tag switch
            {
                ModulusTag => 0,
                ExponentTag => 1,
                _ => throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidPublicKeyData))
            };

            if (!valueArray[valueIndex].IsEmpty)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData)
                    );
            }

            valueArray[valueIndex] = tlvReader.ReadValue(tag);
        }

        return (valueArray[0], valueArray[1]);
    }

    public static ReadOnlyMemory<byte> GetPublicECValues(ReadOnlyMemory<byte> encodedPublicKey)
    {
        var tlvReader = new TlvReader(encodedPublicKey);

        int tag = tlvReader.PeekTag(2);
        if (tag == PublicKeyTag)
        {
            tlvReader = tlvReader.ReadNestedTlv(tag);
        }

        tag = tlvReader.PeekTag();
        if (tag != EccTag)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData)
                );
        }

        var publicPoint = tlvReader.ReadValue(EccTag);
        return publicPoint;
    }
}

public static class KeyParametersPivHelper
{
    const int PrimePTag = 0x01;
    const int PrimeQTag = 0x02;
    const int ExponentPTag = 0x03;
    const int ExponentQTag = 0x04;
    const int CoefficientTag = 0x05;
    const int CrtComponentCount = 5;

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
            _ when PivPrivateKey.IsValidEccTag(tag) => CreatePrivateEcFromPivEncoding(pivEncodingBytes),
            _ when PivPrivateKey.IsValidRsaTag(tag) => CreatePrivateRsaFromPivEncoding(pivEncodingBytes),
            _ => throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidPrivateKeyData))
        };

        return (T)pkp;
    }

    public static T CreatePublicParametersFromPivEncoding<T>(ReadOnlyMemory<byte> pivEncodingBytes)
        where T : IPublicKeyParameters
    {
        if (pivEncodingBytes.IsEmpty)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }

        int tag = GetKeyTag(pivEncodingBytes);
        IPublicKeyParameters pkp = tag switch
        {
            _ when PivPublicKey.IsValidEccTag(tag) => CreatePublicEcFromPivEncoding(pivEncodingBytes),
            _ when PivPublicKey.IsValidRsaTag(tag) => CreatePublicRsaFromPivEncoding(pivEncodingBytes),
            _ => throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ExceptionMessages.InvalidPublicKeyData))
        };

        return (T)pkp;
    }

    private static int GetKeyTag(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        var tlvReader = new TlvReader(pivEncodingBytes);
        int tag = tlvReader.PeekTag(2);
        tlvReader = tlvReader.ReadNestedTlv(tag);
        tag = tlvReader.PeekTag();
        return tag;
    }

    private static RSAPublicKeyParameters CreatePublicRsaFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        var (modulus, exponent) = PivEncodingReader.GetPublicRsaValues(pivEncodingBytes);
        var rsaParameters = new RSAParameters { Modulus = modulus.ToArray(), Exponent = exponent.ToArray() };
        return RSAPublicKeyParameters.CreateFromParameters(rsaParameters);
    }

    private static ECPublicKeyParameters CreatePublicEcFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        var publicPointData = PivEncodingReader.GetPublicECValues(pivEncodingBytes);

        // Determine size of array
        // Determine size of coordinate
        // Match against keydefinition EC lengths
        // Get the x y values
        // Set the curve oid

        if (publicPointData.Span[0] != 0x4)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData)
                );
        }

        var publicKeyData = publicPointData.Span[1..];
        var coordinateLength = publicKeyData.Length / 2;

        var keyDefintion = KeyDefinitions
            .GetEcKeyDefinitions()
            .Where(kd => kd.AlgorithmOid == KeyDefinitions.KeyOids.Algorithm.EllipticCurve)
            .Single(kd => kd.LengthInBytes == coordinateLength);

        var x = publicPointData.Span.Slice(1, keyDefintion.LengthInBytes).ToArray();
        var y = publicPointData.Span.Slice(1 + keyDefintion.LengthInBytes, keyDefintion.LengthInBytes).ToArray();
        var rsaParameters = new ECParameters
            { Q = new ECPoint() { X = x, Y = y }, Curve = ECCurve.CreateFromValue(keyDefintion.CurveOid) };

        // var x = publicPoint.Span.Slice(1, 32).ToArray();
        // var y = publicPoint.Span.Slice(33, 32).ToArray();
        // var rsaParameters = new ECParameters { Q = new ECPoint(){ X = x, Y = y}, Curve = ECCurve.CreateFromValue(KeyDefinitions.KeyOids.Curve.P256)};
        return ECPublicKeyParameters.CreateFromParameters(rsaParameters);
    }

    private static ECPrivateKeyParameters CreatePrivateEcFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        if (TlvObject.TryParse(pivEncodingBytes.Span, out var tlv) && PivPrivateKey.IsValidEccTag(tlv.Tag))
        {
            switch (tlv.Tag)
            {
                case PivPrivateKey.EccTag:
                    List<KeyDefinitions.KeyDefinition> allowed =
                        [KeyDefinitions.P256, KeyDefinitions.P384, KeyDefinitions.P521];

                    var keyDefinition = allowed.Single(kd => kd.LengthInBytes == tlv.Value.Span.Length);
                    return ECPrivateKeyParameters.CreateFromValue(tlv.Value.Span.ToArray(), keyDefinition.KeyType);
                case PivPrivateKey.EccEd25519Tag:
                    return ECPrivateKeyParameters.CreateFromValue(tlv.Value.ToArray(), KeyDefinitions.KeyType.Ed25519);
                case PivPrivateKey.EccX25519Tag:
                    return ECPrivateKeyParameters.CreateFromValue(tlv.Value.ToArray(), KeyDefinitions.KeyType.X25519);
            }
        }

        throw new ArgumentException(
            string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.InvalidPrivateKeyData));
    }

    private static RSAPrivateKeyParameters CreatePrivateRsaFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
    {
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

        var primeP = valueArray[PrimePTag - 1].Span;
        var primeQ = valueArray[PrimeQTag - 1].Span;
        var exponentP = valueArray[ExponentPTag - 1].Span;
        var exponentQ = valueArray[ExponentQTag - 1].Span;
        var coefficient = valueArray[CoefficientTag - 1].Span;

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
