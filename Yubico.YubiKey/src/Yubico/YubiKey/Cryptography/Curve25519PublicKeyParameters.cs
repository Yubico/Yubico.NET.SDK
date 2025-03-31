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
    private KeyDefinition _keyDefinition { get; }
    private readonly Memory<byte> _publicPoint;
    public KeyType KeyType => _keyDefinition.KeyType;
    public KeyDefinition KeyDefinition => _keyDefinition;
    public ReadOnlyMemory<byte> PublicPoint => _publicPoint;

    private Curve25519PublicKeyParameters(
        ReadOnlyMemory<byte> publicPoint,
        KeyDefinition keyDefinition)
    {
        _keyDefinition = keyDefinition;
        _publicPoint = new byte[publicPoint.Length];

        publicPoint.CopyTo(_publicPoint);
    }

    public byte[] ExportSubjectPublicKeyInfo() =>
        AsnPublicKeyWriter.EncodeToSubjectPublicKeyInfo(_publicPoint, _keyDefinition.KeyType);

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

    public static Curve25519PublicKeyParameters CreateFromValue(ReadOnlyMemory<byte> publicKey, KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        if (publicKey.Length != keyDefinition.LengthInBytes)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData));
        }

        return new Curve25519PublicKeyParameters(publicKey, keyDefinition);
    }
}
