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
    public static (ReadOnlyMemory<byte> Modulus, ReadOnlyMemory<byte> Exponent) GetPublicRSAValues(
        ReadOnlyMemory<byte> encodedPublicKey)
    {
        var tlvReader = new TlvReader(encodedPublicKey);
        int tag = tlvReader.PeekTag(2);
        if (tag == PivConstants.PublicKeyTag)
        {
            tlvReader = tlvReader.ReadNestedTlv(tag);
        }

        var valueArray = new ReadOnlyMemory<byte>[2];

        while (tlvReader.HasData)
        {
            tag = tlvReader.PeekTag();
            int valueIndex = tag switch
            {
                PivConstants.PublicRSAModulusTag => 0,
                PivConstants.PublicRSAExponentTag => 1,
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

    // Will read PIV public key data and return the public point
    public static ReadOnlyMemory<byte> GetECPublicPointValues(ReadOnlyMemory<byte> encodedPublicKey)
    {
        var tlvReader = new TlvReader(encodedPublicKey);

        int tag = tlvReader.PeekTag(2);
        if (tag == PivConstants.PublicKeyTag)
        {
            tlvReader = tlvReader.ReadNestedTlv(tag);
        }

        tag = tlvReader.PeekTag();
        if (tag != PivConstants.PublicECTag)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData)
                );
        }

        var publicPoint = tlvReader.ReadValue(PivConstants.PublicECTag);
        return publicPoint;
    }

    public static RSAParameters GetRSAParameters(ReadOnlyMemory<byte> pivEncodedKey)
    {
        const int CrtComponentCount = 5;

        var tlvReader = new TlvReader(pivEncodedKey);
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
