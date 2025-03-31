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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv;

public static class PivEncodingReader
{
    
    // Will read PIV public key data and return the modulus and exponent
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
}
