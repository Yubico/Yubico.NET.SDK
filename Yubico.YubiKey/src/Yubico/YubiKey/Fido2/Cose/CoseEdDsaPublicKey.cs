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
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2.Cose;

/// <summary>
///     A representation of an Elliptic Curve public key in COSE form.
/// </summary>
/// <remarks>
///     <para>
///         An ECC public key consists of a curve and public key data. In FIDO2, the curve is represented by the
///         <see cref="CoseAlgorithmIdentifier" /> .
///     </para>
///     <para>
///         The FIDO2 standard also specifies an encoding of the public key information. It uses the representation defined
///         in RFC8152: CBOR Object Signing and Encryption (COSE) standard. Supplementary information can be found in
///         section 6.5.6 of the CTAP2.1 specification (under the heading `getPublicKey()`).
///     </para>
/// </remarks>
public class CoseEdDsaPublicKey : CoseKey
{
    private const int TagCurve = -1;
    private const int TagPublicKey = -2;
    private const int Ed25519PublicKeyLength = 32;
    private CoseEcCurve _curve;
    private byte[] _publicKey = Array.Empty<byte>();

    /// <summary>
    ///     The Elliptic Curve that the key resides on.
    /// </summary>
    /// <exception cref="NotSupportedException">
    ///     On set, the curve specified is not supported.
    /// </exception>
    public CoseEcCurve Curve
    {
        get => _curve;
        set
        {
            ValidateCurve(value);
            _curve = value;
        }
    }

    /// <summary>
    ///     The public key data.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     On set, the key data is not the correct length.
    /// </exception>
    public ReadOnlyMemory<byte> PublicKey
    {
        get => _publicKey;
        set
        {
            if (value.Length != Ed25519PublicKeyLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData));
            }

            _publicKey = value.ToArray();
        }
    }

    /// <summary>
    ///     Construct a <see cref="CoseEdDsaPublicKey" /> based on public key data (x-coordinate of public key)
    /// </summary>
    /// <remarks>
    ///     The only valid DSA curve is ED25519.
    /// </remarks>
    /// <param name="publicKey">
    ///     The x-coordinate of the public point.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if the public key data is not the correct length.
    /// </exception>
    public static CoseEdDsaPublicKey CreateFromPublicKeyData(ReadOnlyMemory<byte> publicKey)
    {
        if (publicKey.Length != Ed25519PublicKeyLength)
        {
            throw new ArgumentException(ExceptionMessages.InvalidPublicKeyData);
        }

        return new CoseEdDsaPublicKey
        {
            PublicKey = publicKey,
            Curve = CoseEcCurve.Ed25519,
            Type = CoseKeyType.Okp,
            Algorithm = CoseAlgorithmIdentifier.EdDSA
        };
    }

    /// <summary>
    ///     Creates a new instance of <see cref="CoseEdDsaPublicKey" /> from the given encoded COSE key.
    /// </summary>
    /// <param name="encodedCoseKey">
    ///     The encoded COSE key in CBOR format.
    /// </param>
    /// <returns>
    ///     A <see cref="CoseEdDsaPublicKey" /> object initialized with the provided encoded key data.
    /// </returns>
    /// <exception cref="Ctap2DataException">
    ///     Thrown if the <paramref name="encodedCoseKey" /> is not a valid EdDSA Public Key encoding.
    /// </exception>
    public static CoseEdDsaPublicKey CreateFromEncodedKey(ReadOnlyMemory<byte> encodedCoseKey)
    {
        var map = new CborMap<int>(encodedCoseKey);
        return new CoseEdDsaPublicKey
        {
            PublicKey = map.ReadByteString(TagPublicKey),
            Curve = (CoseEcCurve)map.ReadInt32(TagCurve),
            Type = (CoseKeyType)map.ReadInt32(TagKeyType),
            Algorithm = (CoseAlgorithmIdentifier)map.ReadInt32(TagAlgorithm)
        };
    }

    /// <inheritdoc />
    public override byte[] Encode()
    {
        if (_publicKey.Length != Ed25519PublicKeyLength)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.NoDataToEncode));
        }

        return new CborMapWriter<int>()
            .Entry(TagKeyType, (int)CoseKeyType.Okp)
            .Entry(TagAlgorithm, (int)CoseAlgorithmIdentifier.EdDSA)
            .Entry(TagCurve, (int)CoseEcCurve.Ed25519)
            .Entry(TagPublicKey, PublicKey)
            .Encode();
    }

    private static void ValidateCurve(CoseEcCurve value)
    {
        if (value == CoseEcCurve.Ed25519)
        {
            return;
        }

        throw new ArgumentException(
            string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.UnsupportedAlgorithm));
    }
}
