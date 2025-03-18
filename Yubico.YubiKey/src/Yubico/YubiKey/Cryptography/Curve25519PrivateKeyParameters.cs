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
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

public class Curve25519PrivateKeyParameters : IPrivateKeyParameters
{
    private readonly KeyDefinitions.KeyDefinition _keyDefinition;
    private readonly Memory<byte> _privateKeyData;
    private readonly Memory<byte> _encodedKey;

    private Curve25519PrivateKeyParameters(
        ReadOnlyMemory<byte> encodedKey,
        ReadOnlyMemory<byte> privateKeyData,
        KeyDefinitions.KeyDefinition keyDefinition)
    {
        _keyDefinition = keyDefinition;
        _privateKeyData = new byte[privateKeyData.Length];
        _encodedKey = new byte[encodedKey.Length];

        privateKeyData.CopyTo(_privateKeyData);
        encodedKey.CopyTo(_encodedKey);
    }
    
    public static Curve25519PrivateKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
    {
        var reader = new AsnReader(encodedKey, AsnEncodingRules.DER);
        var seqPrivateKeyInfo = reader.ReadSequence();

        var version = seqPrivateKeyInfo.ReadInteger();
        if (version != 0)
        {
            throw new CryptographicException("Invalid PKCS#8 private key format: unexpected version");
        }

        var seqAlgorithmIdentifier = seqPrivateKeyInfo.ReadSequence();
        
        string oidAlgorithm = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if(oidAlgorithm != KeyDefinitions.KeyOids.Algorithm.X25519 && oidAlgorithm != KeyDefinitions.KeyOids.Algorithm.Ed25519)
        {
            throw new ArgumentException("Invalid curve OID. Must be: " + KeyDefinitions.KeyOids.Algorithm.X25519 + " or " + KeyDefinitions.KeyOids.Algorithm.Ed25519);
        }
        
        var seqPrivateKey = new AsnReader(seqPrivateKeyInfo.ReadOctetString(), AsnEncodingRules.DER);
        var tag = seqPrivateKey.PeekTag();
        if (tag.TagValue != 4 || tag.TagClass != TagClass.Universal)
        {
            throw new CryptographicException("Invalid Curve25519 private key");
        }
        // TOdo optionaly verify clamping and structure of X25519 or Ed25519 key
        
        byte[] privateKeyData = seqPrivateKey.ReadOctetString();
        seqPrivateKeyInfo.ThrowIfNotEmpty();
        if (privateKeyData.Length != 32)
        {
            throw new CryptographicException("Invalid Curve25519 private key: incorrect length");
        }
        
        var keyDefinition = KeyDefinitions.GetByOid(oidAlgorithm, OidType.AlgorithmOid);
        return new Curve25519PrivateKeyParameters(encodedKey, privateKeyData, keyDefinition);
    }

    public static Curve25519PrivateKeyParameters CreateFromValue(ReadOnlyMemory<byte> privateKey, KeyDefinitions.KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        byte[] encodedPkcs8 = AsnPrivateKeyWriter.EncodeToPkcs8(privateKey, keyType);
        return new Curve25519PrivateKeyParameters(encodedPkcs8, privateKey, keyDefinition);
    }
    
    public ReadOnlyMemory<byte> ExportPkcs8PrivateKey() => _encodedKey;

    public KeyDefinitions.KeyDefinition GetKeyDefinition() => _keyDefinition;
    public KeyDefinitions.KeyType GetKeyType() => _keyDefinition.KeyType;

    public ReadOnlyMemory<byte> GetPrivateKey() => _privateKeyData;
}
