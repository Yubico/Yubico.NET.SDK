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
    public static IPublicKeyParameters CreatePublicKeyParameters(ReadOnlyMemory<byte> pivEncodedKey, KeyType keyType) =>
        keyType switch
        {
            KeyType.Ed25519 or KeyType.X25519 => KeyParametersPivHelper
                .CreatePublicCurve25519FromPivEncoding(pivEncodedKey, keyType),
            KeyType.P256 or KeyType.P384 or KeyType.P521 => KeyParametersPivHelper
                .CreatePublicEcFromPivEncoding(pivEncodedKey),
            KeyType.RSA1024 or KeyType.RSA2048 or KeyType.RSA3072 or KeyType.RSA4096 => KeyParametersPivHelper
                .CreatePublicRsaFromPivEncoding(pivEncodedKey),
            _ => throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidApduResponseData))
        };
    
    // TODO Is this needed?
    public static IPrivateKeyParameters CreatePrivateKeyParameters(ReadOnlyMemory<byte> value, KeyType keyType) =>
        keyType switch
        {
            KeyType.Ed25519 or KeyType.X25519 => KeyParametersPivHelper
                .CreatePrivateCurve25519FromPivEncoding(value, keyType),
            KeyType.P256 or KeyType.P384 or KeyType.P521 => KeyParametersPivHelper
                .CreatePrivateEcFromPivEncoding(value),
            KeyType.RSA1024 or KeyType.RSA2048 or KeyType.RSA3072 or KeyType.RSA4096 => KeyParametersPivHelper
                .CreatePrivateRsaFromPivEncoding(value),
            _ => throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidApduResponseData))
        };

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
    
    public static Curve25519PrivateKeyParameters CreatePrivateCurve25519FromPivEncoding(
        ReadOnlyMemory<byte> pivEncodingBytes, 
        KeyType keyType)
    {
        if (!TlvObject.TryParse(pivEncodingBytes.Span, out var tlv) || !PivConstants.IsValidPrivateECTag(tlv.Tag))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }

        using var privateValueHandle = new ZeroingMemoryHandle(tlv.Value.ToArray());
    
        return tlv.Tag switch
        {
            PivConstants.PrivateECEd25519Tag when keyType == KeyType.Ed25519 => Curve25519PrivateKeyParameters.CreateFromValue(
                privateValueHandle.Data, KeyType.Ed25519),
            
            PivConstants.PrivateECX25519Tag when keyType == KeyType.X25519 => Curve25519PrivateKeyParameters.CreateFromValue(
                privateValueHandle.Data, KeyType.X25519),
            
            _ => throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData))
        };
    }

    public static ECPrivateKeyParameters CreatePrivateEcFromPivEncoding(ReadOnlyMemory<byte> pivEncodingBytes)
    {
        if (!TlvObject.TryParse(pivEncodingBytes.Span, out var tlv) || tlv.Tag != PivConstants.PrivateECDsaTag)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }
    
        var allowedKeyDefinitions = KeyDefinitions
            .GetEcKeyDefinitions()
            .Where(kd => kd.AlgorithmOid == KeyDefinitions.CryptoOids.ECDSA);
        try
        {
            var keyDefinition = allowedKeyDefinitions
                .Single(kd => kd.LengthInBytes == tlv.Value.Span.Length);
            ReadOnlyMemory<byte> value = tlv.Value;
            return ECPrivateKeyParameters.CreateFromValue(value, keyDefinition.KeyType);
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }
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

        var parameters = new RSAParameters
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

        return RSAPrivateKeyParameters.CreateFromParameters(parameters);
    }
}
