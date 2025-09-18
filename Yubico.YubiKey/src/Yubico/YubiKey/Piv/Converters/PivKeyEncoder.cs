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
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Converters;

/// <summary>
///     This class converts from IPublicKey, IPrivateKey and implementations of either to a PIV-encoded key.
/// </summary>
internal static class PivKeyEncoder
{
    [Obsolete("KeyExtensions instead", false)]
    public static Memory<byte> EncodePublicKey(IPublicKey publicKey)
    {
        return publicKey switch
        {
            Curve25519PublicKey curve25519PublicKey => EncodeCurve25519PublicKey(curve25519PublicKey),
            ECPublicKey ecPublicKey => EncodeECPublicKey(ecPublicKey),
            RSAPublicKey rsaPublicKey => EncodeRSAPublicKey(rsaPublicKey),
            _ => throw new ArgumentException("Unsupported public key type.")
        };
    }

    public static Memory<byte> EncodeRSAPublicKey(RSAPublicKey publicKey)
    {
        var rsaParameters = publicKey.Parameters;
        var tlvWriter = new TlvWriter();
        using (tlvWriter.WriteNestedTlv(PivConstants.PublicKeyTag))
        {
            tlvWriter.WriteValue(PivConstants.PublicRSAModulusTag, rsaParameters.Modulus);
            tlvWriter.WriteValue(PivConstants.PublicRSAExponentTag, rsaParameters.Exponent);
        }

        return tlvWriter.Encode();
    }

    public static Memory<byte> EncodeCurve25519PublicKey(Curve25519PublicKey publicKey)
    {
        var tlvWriter = new TlvWriter();
        using (tlvWriter.WriteNestedTlv(PivConstants.PublicKeyTag))
        {
            tlvWriter.WriteValue(PivConstants.PublicECTag, publicKey.PublicPoint.Span);
        }

        return tlvWriter.Encode();
    }

    public static Memory<byte> EncodeECPublicKey(ECPublicKey publicKey)
    {
        var tlvWriter = new TlvWriter();
        using (tlvWriter.WriteNestedTlv(PivConstants.PublicKeyTag))
        {
            tlvWriter.WriteValue(PivConstants.PublicECTag, publicKey.PublicPoint.Span);
        }

        return tlvWriter.Encode();
    }

    [Obsolete("KeyExtensions instead", false)]
    public static Memory<byte> EncodePrivateKey(IPrivateKey publicKey) =>
        publicKey switch
        {
            Curve25519PrivateKey curve25519PrivateKey => EncodeCurve25519PrivateKey(curve25519PrivateKey),
            ECPrivateKey ecPrivateKey => EncodeECPrivateKey(ecPrivateKey),
            RSAPrivateKey rsaPrivateKey => EncodeRSAPrivateKey(rsaPrivateKey),

            _ => throw new ArgumentException("Unsupported public key type.")
        };

    public static Memory<byte> EncodeECPrivateKey(ECPrivateKey privateKey)
    {
        var tlvWriter = new TlvWriter();
        tlvWriter.WriteValue(PivConstants.PrivateECDsaTag, privateKey.Parameters.D);
        return tlvWriter.Encode();
    }

    public static Memory<byte> EncodeRSAPrivateKey(RSAPrivateKey privateKey)
    {
        var rsaParameters = privateKey.Parameters;
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

    public static Memory<byte> EncodeCurve25519PrivateKey(Curve25519PrivateKey privateKey)
    {
        var tlvWriter = new TlvWriter();
        int typeTag = privateKey.KeyType == KeyType.Ed25519
            ? PivConstants.PrivateECEd25519Tag
            : PivConstants.PrivateECX25519Tag;

        tlvWriter.WriteValue(typeTag, privateKey.PrivateKey.Span);
        return tlvWriter.Encode();
    }
}
