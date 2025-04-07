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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv;

public static class KeyParametersPivExtensions
{
    public static Memory<byte> ToPivEncodedPrivateKey(this IPrivateKeyParameters parameters)
    {
        return parameters switch
        {
            ECPrivateKeyParameters p => EncodeECPrivateKeyParameters(p),
            RSAPrivateKeyParameters p => EncodeRSAPrivateKeyParameters(p),
            Curve25519PrivateKeyParameters p => EncodeCurve25519PrivateKeyParameters(p),
            _ => throw new ArgumentException("Unsupported key type.", nameof(parameters))
        };
    }

    public static Memory<byte> ToPivEncodedPublicKey(this IPublicKeyParameters parameters)
    {
        return parameters switch
        {
            ECPublicKeyParameters p => EncodeECPublicKeyParameters(p),
            RSAPublicKeyParameters p => EncodeRSAPublicKeyParameters(p),
            Curve25519PublicKeyParameters p => EncodeCurve25519PublicKeyParameters(p),
            _ => throw new ArgumentException("Unsupported key type.", nameof(parameters))
        };
    }

    public static PivPublicKey ToPivPublicKey(this IPublicKeyParameters parameters) {
        return PivPublicKey.Create(parameters.ToPivEncodedPublicKey(), parameters.KeyType.GetPivAlgorithm());
    }

    private static Memory<byte> EncodeRSAPublicKeyParameters(RSAPublicKeyParameters parameters)
    {
        var rsaParameters = parameters.Parameters;
        var tlvWriter = new TlvWriter();
        using (tlvWriter.WriteNestedTlv(PivConstants.PublicKeyTag))
        {
            tlvWriter.WriteValue(PivConstants.PublicRSAModulusTag, rsaParameters.Modulus);
            tlvWriter.WriteValue(PivConstants.PublicRSAExponentTag, rsaParameters.Exponent);
        }

        return tlvWriter.Encode();
    }

    private static Memory<byte> EncodeCurve25519PublicKeyParameters(Curve25519PublicKeyParameters parameters) =>
        new TlvObject(PivConstants.PublicECTag, parameters.PublicPoint.Span).GetBytes();

    private static Memory<byte> EncodeECPublicKeyParameters(ECPublicKeyParameters parameters) =>
        new TlvObject(PivConstants.PublicECTag, parameters.PublicPoint.Span).GetBytes();

    private static Memory<byte> EncodeECPrivateKeyParameters(ECPrivateKeyParameters parameters)
    {
        var tlvWriter = new TlvWriter();
        tlvWriter.WriteValue(PivConstants.PrivateECDsaTag, parameters.Parameters.D);
        return tlvWriter.Encode();
    }

    private static Memory<byte> EncodeRSAPrivateKeyParameters(RSAPrivateKeyParameters parameters)
    {
        var rsaParameters = parameters.Parameters;
        if (rsaParameters.P.Length != rsaParameters.Q.Length ||
            rsaParameters.DP.Length != rsaParameters.P.Length ||
            rsaParameters.DQ.Length != rsaParameters.P.Length ||
            rsaParameters.InverseQ.Length != rsaParameters.P.Length)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }

        var tlvWriter = new TlvWriter();
        tlvWriter.WriteValue(PivConstants.PrivateRSAPrimePTag, rsaParameters.P);
        tlvWriter.WriteValue(PivConstants.PrivateRSAPrimeQTag, rsaParameters.Q);
        tlvWriter.WriteValue(PivConstants.PrivateRSAExponentPTag, rsaParameters.DP);
        tlvWriter.WriteValue(PivConstants.PrivateRSAExponentQTag, rsaParameters.DQ);
        tlvWriter.WriteValue(PivConstants.PrivateRSACoefficientTag, rsaParameters.InverseQ);
        return tlvWriter.Encode();
    }

    private static Memory<byte> EncodeCurve25519PrivateKeyParameters(Curve25519PrivateKeyParameters parameters)
    {
        var tlvWriter = new TlvWriter();
        int typeTag = parameters.KeyType == KeyType.Ed25519
            ? PivConstants.PrivateECEd25519Tag
            : PivConstants.PrivateECX25519Tag;

        tlvWriter.WriteValue(typeTag, parameters.PrivateKey.Span);
        return tlvWriter.Encode();
    }
}
