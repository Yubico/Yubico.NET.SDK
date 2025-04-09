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
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Converters;

internal static class PivEncodingReader
{
    // Works with both Piv Encoded and GetMetaData encoded keys (with or without leading Public Key Tag)
    // public static (ReadOnlyMemory<byte> Modulus, ReadOnlyMemory<byte> Exponent) GetPublicRSAValues(
    //     ReadOnlyMemory<byte> encodedPublicKey)
    // {
    //     var tlvReader = new TlvReader(encodedPublicKey);
    //     int tag = tlvReader.PeekTag(2);
    //     var rsaKeyEncoding = tag == PivConstants.PublicKeyTag 
    //         ? tlvReader.ReadValue(PivConstants.PublicKeyTag) 
    //         : encodedPublicKey;
    //     
    //     var publicKeyValues = TlvObjects.DecodeDictionary(rsaKeyEncoding.Span);
    //     bool hasModulus = publicKeyValues.TryGetValue(PivConstants.PublicRSAModulusTag, out var modulus);
    //     bool hasExponent = publicKeyValues.TryGetValue(PivConstants.PublicRSAExponentTag, out var exponent);
    //     if (!hasModulus || !hasExponent)
    //     {
    //         throw new ArgumentException(
    //             string.Format(
    //                 CultureInfo.CurrentCulture,
    //                 ExceptionMessages.InvalidPublicKeyData
    //                 ));
    //     }
    //     return (modulus, exponent);
    // }
    
    public static (ReadOnlyMemory<byte> Modulus, ReadOnlyMemory<byte> Exponent) GetPublicRSAValues(
        ReadOnlyMemory<byte> encodedPublicKey)
    {
        var tlvObject = TlvObject.Parse(encodedPublicKey.Span);
        var rsaKeyEncoding = tlvObject.Tag == PivConstants.PublicKeyTag 
            ? tlvObject.Value 
            : encodedPublicKey;
        
        var publicKeyValues = TlvObjects.DecodeDictionary(rsaKeyEncoding.Span);
        bool hasModulus = publicKeyValues.TryGetValue(PivConstants.PublicRSAModulusTag, out var modulus);
        bool hasExponent = publicKeyValues.TryGetValue(PivConstants.PublicRSAExponentTag, out var exponent);
        if (!hasModulus || !hasExponent)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData
                    ));
        }
        return (modulus, exponent);
    }

    // Will read PIV public key data and return the public point
    public static ReadOnlyMemory<byte> GetECPublicPointValues(ReadOnlyMemory<byte> encodedPublicKey)
    {
        var tlvObject = TlvObject.Parse(encodedPublicKey.Span);
        var keyEncoding = tlvObject.Tag == PivConstants.PublicKeyTag
            ? tlvObject.Value
            : encodedPublicKey;
        
        var publicKeyValues = TlvObjects.DecodeDictionary(keyEncoding.Span);
        bool hasPublicPoint = publicKeyValues.TryGetValue(PivConstants.PublicECTag, out var publicPoint);
        if (!hasPublicPoint)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData
                    ));
        }
        return publicPoint;
    }

    public static RSAParameters GetRSAParameters(ReadOnlyMemory<byte> pivEncodedKey)
    {
        var tlvObject = TlvObject.Parse(pivEncodedKey.Span);
        var keyEncoding = tlvObject.Tag == PivConstants.PublicKeyTag
            ? tlvObject.Value
            : pivEncodedKey;
        
        var rsaTlvValues = TlvObjects.DecodeDictionary(keyEncoding.Span);
        if (!rsaTlvValues.ContainsKey(PivConstants.PrivateRSAPrimePTag) ||
            !rsaTlvValues.ContainsKey(PivConstants.PrivateRSAPrimeQTag) ||
            !rsaTlvValues.ContainsKey(PivConstants.PrivateRSAExponentPTag) ||
            !rsaTlvValues.ContainsKey(PivConstants.PrivateRSAExponentQTag) ||
            !rsaTlvValues.ContainsKey(PivConstants.PrivateRSACoefficientTag))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData
                    ));
        }
        
        var primeP = rsaTlvValues[PivConstants.PrivateRSAPrimePTag].Span;
        var primeQ = rsaTlvValues[PivConstants.PrivateRSAPrimeQTag].Span;
        var exponentP = rsaTlvValues[PivConstants.PrivateRSAExponentPTag].Span;
        var exponentQ = rsaTlvValues[PivConstants.PrivateRSAExponentQTag].Span;
        var coefficient = rsaTlvValues[PivConstants.PrivateRSACoefficientTag].Span;

        return new RSAParameters
        {
            P = primeP.ToArray(),
            Q = primeQ.ToArray(),
            DP = exponentP.ToArray(),
            DQ = exponentQ.ToArray(),
            InverseQ = coefficient.ToArray(),

            // The YubiKey only works with the CRT components of the private RSA key,
            // that's why we set these values as empty.
            D = Array.Empty<byte>(),
            Modulus = Array.Empty<byte>(),
            Exponent = Array.Empty<byte>(),
        };
    }
}
