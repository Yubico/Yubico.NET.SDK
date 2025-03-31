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

    private static Memory<byte> EncodeRSAPublicKeyParameters(RSAPublicKeyParameters parameters)
    {
        var rsaParameters = parameters.Parameters;
        var tlvs = new Dictionary<int, byte[]>
        {
            { PivConstants.PublicRSAModulusTag, rsaParameters.Modulus },
            { PivConstants.PublicRSAExponentTag, rsaParameters.Exponent }
        };

        return new TlvObject(
                PivConstants.PublicKeyTag,
                TlvObjects.EncodeDictionary(tlvs).Span)
            .GetBytes();
    }

    private static Memory<byte> EncodeCurve25519PublicKeyParameters(Curve25519PublicKeyParameters parameters) =>
        new TlvObject(PivConstants.PublicECTag, parameters.PublicPoint.Span).GetBytes();

    private static Memory<byte> EncodeECPublicKeyParameters(ECPublicKeyParameters parameters) =>
        new TlvObject(PivConstants.PublicECTag, parameters.PublicPoint.Span).GetBytes();

    private static Memory<byte> EncodeECPrivateKeyParameters(ECPrivateKeyParameters parameters)
    {
        // byte[] tlv = new byte[1 + parameters.PrivateKey.Length];
        // tlv[0] = PivConstants.PrivateECDsaTag;
        // tlv[1] = (byte)parameters.PrivateKey.Length;
        // parameters.PrivateKey.CopyTo(tlv.AsMemory()[2..]);
        // return tlv;

        var tlvWriter = new TlvWriter();
        tlvWriter.WriteValue(PivConstants.PrivateECDsaTag, parameters.PrivateKey.Span);
        return tlvWriter.Encode();

        // var tlvObject = new TlvObject(PivConstants.PrivateECDsaTag, parameters.PrivateKey.Span);
        // return tlvObject.GetBytes();
    }

    private static Memory<byte> EncodeRSAPrivateKeyParameters(RSAPrivateKeyParameters parameters)
    {
        var rsaParameters = parameters.Parameters;

        // Verify all RSA parameters are of correct length
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

        // var tlvWriter = new TlvWriter();
        // tlvWriter.WriteValue(PivConstants.PrivateRSAPrimePTag, parameters.P);
        // tlvWriter.WriteValue(PivConstants.PrivateRSAPrimeQTag, parameters.Q);
        // tlvWriter.WriteValue(PivConstants.PrivateRSAExponentPTag, parameters.DP);
        // tlvWriter.WriteValue(PivConstants.PrivateRSAExponentQTag, parameters.DQ);
        // tlvWriter.WriteValue(PivConstants.PrivateRSACoefficientTag, parameters.InverseQ);
        // return tlvWriter.Encode();

        var tlvs = new Dictionary<int, byte[]>
        {
            { PivConstants.PrivateRSAPrimePTag, rsaParameters.P },
            { PivConstants.PrivateRSAPrimeQTag, rsaParameters.Q },
            { PivConstants.PrivateRSAExponentPTag, rsaParameters.DP },
            { PivConstants.PrivateRSAExponentQTag, rsaParameters.DQ },
            { PivConstants.PrivateRSACoefficientTag, rsaParameters.InverseQ }
        };

        return TlvObjects.EncodeDictionary(tlvs);
    }

    private static Memory<byte> EncodeCurve25519PrivateKeyParameters(Curve25519PrivateKeyParameters parameters)
    {
        // byte[] tlv = new byte[1 + parameters.PrivateKey.Length];
        // tlv[0] = (byte)(parameters.KeyType == KeyType.Ed25519 ? PivConstants.PrivateECEd25519Tag : PivConstants.PrivateECX25519Tag);
        // tlv[1] = (byte)parameters.PrivateKey.Length;
        // parameters.PrivateKey.CopyTo(tlv.AsMemory()[2..]);
        // return tlv;

        var tlvWriter = new TlvWriter();
        int typeTag = parameters.KeyType == KeyType.Ed25519
            ? PivConstants.PrivateECEd25519Tag
            : PivConstants.PrivateECX25519Tag;

        tlvWriter.WriteValue(typeTag, parameters.PrivateKey.Span);

        return tlvWriter.Encode();
    }
}
