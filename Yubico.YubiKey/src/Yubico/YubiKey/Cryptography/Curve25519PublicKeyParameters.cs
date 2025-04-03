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
using System.Formats.Asn1;
using System.Globalization;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

public class Curve25519PublicKeyParameters : IPublicKeyParameters
{
    private readonly Memory<byte> _publicPoint;

    public KeyDefinition KeyDefinition { get; }
    public KeyType KeyType => KeyDefinition.KeyType;
    public ReadOnlyMemory<byte> PublicPoint => _publicPoint;

    private Curve25519PublicKeyParameters(
        ReadOnlyMemory<byte> publicPoint,
        KeyDefinition keyDefinition)
    {
        KeyDefinition = keyDefinition;
        _publicPoint = new byte[publicPoint.Length];

        publicPoint.CopyTo(_publicPoint);
    }
    
    /// <summary>
    /// Converts this public key to an ASN.1 DER encoded format (X.509 SubjectPublicKeyInfo).
    /// </summary>
    /// <returns>
    /// A byte array containing the ASN.1 DER encoded public key.
    /// </returns>
    public byte[] ExportSubjectPublicKeyInfo() =>
        AsnPublicKeyWriter.EncodeToSubjectPublicKeyInfo(_publicPoint, KeyDefinition.KeyType);

    /// <summary>
    /// Creates a new instance of <see cref="Curve25519PublicKeyParameters"/> from a DER-encoded public key.
    /// </summary>
    /// <param name="encodedKey">
    /// The DER-encoded public key.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="Curve25519PublicKeyParameters"/>.
    /// </returns>
    /// <exception cref="CryptographicException">
    /// Thrown if the public key is invalid.
    /// </exception>
    public static Curve25519PublicKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
    {
        var reader = new AsnReader(encodedKey, AsnEncodingRules.DER);
        var seqSubjectPublicKeyInfo = reader.ReadSequence();
        var seqAlgorithmIdentifier = seqSubjectPublicKeyInfo.ReadSequence();

        string oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        byte[] subjectPublicKey = seqSubjectPublicKeyInfo.ReadBitString(out int unusedBitCount);
        if (unusedBitCount != 0)
        {
            throw new CryptographicException("Invalid public key encoding");
        }

        var keyType = KeyDefinitions.GetKeyTypeByOid(oidAlgorithm);
        return CreateFromValue(subjectPublicKey, keyType);
    }

    /// <summary>
    /// Creates an instance of <see cref="Curve25519PublicKeyParameters"/> from the given
    /// <paramref name="publicPoint"/> and <paramref name="keyType"/>.
    /// </summary>
    /// <param name="publicPoint">The raw public key data, formatted as an compressed point.</param>
    /// <param name="keyType">The type of key this is.</param>
    /// <returns>An instance of <see cref="Curve25519PublicKeyParameters"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the public key data length is not valid.
    /// </exception>
    public static Curve25519PublicKeyParameters CreateFromValue(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        if (publicPoint.Length != keyDefinition.LengthInBytes)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData));
        }

        return new Curve25519PublicKeyParameters(publicPoint, keyDefinition);
    }
}
