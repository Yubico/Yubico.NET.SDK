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
using System.Linq;
using System.Security.Cryptography;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Converters;

/// <summary>
/// This class converts from a Piv Encoded Key to either instances of the common IPublicKey and IPrivateKey
/// or concrete the concrete types that inherit these interfaces.
/// </summary>
internal partial class PivKeyConverter
{
    public static IPublicKey CreatePublicKey(ReadOnlyMemory<byte> pivEncodedKey, KeyType keyType) =>
        keyType switch
        {
            _ when keyType.IsCurve25519() => CreateCurve25519PublicKey(pivEncodedKey, keyType),
            _ when keyType.IsECDsa() => CreateECPublicKey(pivEncodedKey),
            _ when keyType.IsRSA() => CreateRSAPublicKey(pivEncodedKey),
            _ => throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidApduResponseData))
        };

    public static RSAPublicKey CreateRSAPublicKey(ReadOnlyMemory<byte> pivEncodedKey)
    {
        var (modulus, exponent) = PivEncodingReader.GetPublicRSAValues(pivEncodedKey);
        var rsaParameters = new RSAParameters { Modulus = modulus.ToArray(), Exponent = exponent.ToArray() };
        return RSAPublicKey.CreateFromParameters(rsaParameters);
    }

    public static ECPublicKey CreateECPublicKey(ReadOnlyMemory<byte> pivEncodedKey)
    {
        var publicPointData = PivEncodingReader.GetECPublicPointValues(pivEncodedKey);
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
            .Where(kd => kd.AlgorithmOid == Oids.ECDSA)
            .Single(kd => kd.LengthInBytes == coordinateLength);

        byte[]? x = publicPointData.Span.Slice(1, keyDefinition.LengthInBytes).ToArray();
        byte[]? y = publicPointData.Span.Slice(1 + keyDefinition.LengthInBytes, keyDefinition.LengthInBytes).ToArray();
        var parameters = new ECParameters
        {
            Q = new ECPoint { X = x, Y = y },
            Curve = ECCurve.CreateFromValue(keyDefinition.CurveOid)
        };

        return ECPublicKey.CreateFromParameters(parameters);
    }

    public static Curve25519PublicKey CreateCurve25519PublicKey(ReadOnlyMemory<byte> pivEncodedKey, KeyType keyType)
    {
        var publicPoint = PivEncodingReader.GetECPublicPointValues(pivEncodedKey);
        return Curve25519PublicKey.CreateFromValue(publicPoint, keyType);
    }

    /// <summary>
    /// Creates an instance of <see cref="IPrivateKey"/> from the
    /// given PIV-encoded key.
    /// </summary>
    /// <remarks>
    /// The created instance will be one of the following concrete types:
    /// <list type="bullet">
    /// <item><see cref="RSAPrivateKey"/></item>
    /// <item><see cref="ECPrivateKeyParameters"/></item>
    /// <item><see cref="Curve25519PrivateKey"/></item>
    /// </list>
    /// </remarks>
    /// <param name="pivEncodedKey">The PIV-encoded key.</param>
    /// <param name="keyType">The type of the key.</param>
    /// <returns>An instance of <see cref="IPrivateKey"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The key type is not supported.
    /// </exception>
    public static IPrivateKey CreatePrivateKey(ReadOnlyMemory<byte> pivEncodedKey, KeyType keyType) =>
        keyType switch
        {
            _ when keyType.IsCurve25519() => CreateCurve25519PrivateKey(pivEncodedKey, keyType),
            _ when keyType.IsECDsa() => CreateECPrivateKey(pivEncodedKey),
            _ when keyType.IsRSA() => CreateRSAPrivateKey(pivEncodedKey),
            _ => throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidApduResponseData))
        };

    public static Curve25519PrivateKey CreateCurve25519PrivateKey(
        ReadOnlyMemory<byte> pivEncodedKey,
        KeyType keyType)
    {
        if (!TlvObject.TryParse(pivEncodedKey.Span, out var tlv) || !PivConstants.IsValidPrivateECTag(tlv.Tag))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }

        using var privateValueHandle = new ZeroingMemoryHandle(tlv.Value.ToArray());

        return tlv.Tag switch
        {
            PivConstants.PrivateECEd25519Tag when keyType == KeyType.Ed25519 => Curve25519PrivateKey.CreateFromValue(
                privateValueHandle.Data, KeyType.Ed25519),

            PivConstants.PrivateECX25519Tag when keyType == KeyType.X25519 => Curve25519PrivateKey.CreateFromValue(
                privateValueHandle.Data, KeyType.X25519),

            _ => throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData))
        };
    }

    public static ECPrivateKey CreateECPrivateKey(ReadOnlyMemory<byte> pivEncodedKey)
    {
        if (!TlvObject.TryParse(pivEncodedKey.Span, out var tlv) || tlv.Tag != PivConstants.PrivateECDsaTag)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }

        var allowedKeyDefinitions = KeyDefinitions
            .GetEcKeyDefinitions()
            .Where(kd => kd.AlgorithmOid == Oids.ECDSA);

        try
        {
            var keyDefinition = allowedKeyDefinitions
                .Single(kd => kd.LengthInBytes == tlv.Value.Span.Length);

            using var privateValueHandle = new ZeroingMemoryHandle(tlv.Value.ToArray());
            return ECPrivateKey.CreateFromValue(privateValueHandle.Data, keyDefinition.KeyType);
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPrivateKeyData));
        }
    }

    public static RSAPrivateKey CreateRSAPrivateKey(ReadOnlyMemory<byte> pivEncodedKey)
    {
        var parameters = PivEncodingReader.GetRSAParameters(pivEncodedKey);
        return RSAPrivateKey.CreateFromParameters(parameters);
    }
}
