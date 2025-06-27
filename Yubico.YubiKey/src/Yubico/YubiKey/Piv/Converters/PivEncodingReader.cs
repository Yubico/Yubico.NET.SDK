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
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Converters;

internal static class PivEncodingReader
{
    public static (ReadOnlyMemory<byte> Modulus, ReadOnlyMemory<byte> Exponent) GetPublicRSAValues(
        ReadOnlyMemory<byte> encodedPublicKey)
    {
        var keyEncoding = GetKeyEncoding(encodedPublicKey);
        var publicKeyValues = TlvObjects.DecodeDictionary(keyEncoding.Span);
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

    public static ReadOnlyMemory<byte> GetECPublicPointValues(ReadOnlyMemory<byte> pivEncodedPublicKey)
    {
        var keyEncoding = GetKeyEncoding(pivEncodedPublicKey);
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

    public static RSAParameters CreateRSAParameters(ReadOnlyMemory<byte> pivEncodedPrivateKey)
    {
        var keyEncoding = GetKeyEncoding(pivEncodedPrivateKey);
        var rsaValues = TlvObjects.DecodeDictionary(keyEncoding.Span);
        if (!rsaValues.ContainsKey(PivConstants.PrivateRSAPrimePTag) ||
            !rsaValues.ContainsKey(PivConstants.PrivateRSAPrimeQTag) ||
            !rsaValues.ContainsKey(PivConstants.PrivateRSAExponentPTag) ||
            !rsaValues.ContainsKey(PivConstants.PrivateRSAExponentQTag) ||
            !rsaValues.ContainsKey(PivConstants.PrivateRSACoefficientTag))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData
                    ));
        }
        
        var primeP = rsaValues[PivConstants.PrivateRSAPrimePTag].Span;
        var primeQ = rsaValues[PivConstants.PrivateRSAPrimeQTag].Span;
        var exponentP = rsaValues[PivConstants.PrivateRSAExponentPTag].Span;
        var exponentQ = rsaValues[PivConstants.PrivateRSAExponentQTag].Span;
        var coefficient = rsaValues[PivConstants.PrivateRSACoefficientTag].Span;

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
    
    /// <summary>
    /// Handles both PivEncoding and YubiKey GetMetadata encoding. 
    /// </summary>
    /// <param name="pivEncodedPublicKey"></param>
    /// <returns>Returns the portion of the bytes that contains the key data</returns>
    private static ReadOnlyMemory<byte> GetKeyEncoding(ReadOnlyMemory<byte> pivEncodedPublicKey)
    {
        var tlvObject = TlvObject.Parse(pivEncodedPublicKey.Span);
        // If leading byte is 0x7F49, then it is a PIV encoded key, otherwise it is a GetMetaData encoded key
        // which means we can decode the tlvs directly instead of reading from the nested value.
        var rsaKeyEncoding = tlvObject.Tag == PivConstants.PublicKeyTag 
            ? tlvObject.Value 
            : pivEncodedPublicKey;
        return rsaKeyEncoding;
    }
}
