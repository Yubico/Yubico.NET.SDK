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
    private KeyDefinition _keyDefinition { get; }
    private readonly Memory<byte> _privateKey;
    private Curve25519PrivateKeyParameters(
        ReadOnlyMemory<byte> privateKey,
        KeyDefinition keyDefinition)
    {
        if (keyDefinition.AlgorithmOid == KeyDefinitions.CryptoOids.X25519)
        {
            AsnUtilities.VerifyX25519PrivateKey(privateKey.Span);
        }

        _privateKey = new byte[privateKey.Length];
        _keyDefinition = keyDefinition;

        privateKey.CopyTo(_privateKey);
    }
    
    public byte[] ExportPkcs8PrivateKey() => AsnPrivateKeyWriter.EncodeToPkcs8(_privateKey, KeyType);
    public KeyDefinition KeyDefinition => _keyDefinition;
    public KeyType KeyType => _keyDefinition.KeyType;
    public ReadOnlyMemory<byte> PrivateKey => _privateKey;
    public void Clear() => CryptographicOperations.ZeroMemory(_privateKey.Span);

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
        if (oidAlgorithm != KeyDefinitions.CryptoOids.X25519 &&
            oidAlgorithm != KeyDefinitions.CryptoOids.Ed25519)
        {
            throw new ArgumentException(
                "Invalid curve OID. Must be: " + KeyDefinitions.CryptoOids.X25519 + " or " +
                KeyDefinitions.CryptoOids.Ed25519);
        }

        var seqPrivateKey = new AsnReader(seqPrivateKeyInfo.ReadOctetString(), AsnEncodingRules.DER);
        var tag = seqPrivateKey.PeekTag();
        if (tag.TagValue != 4 || tag.TagClass != TagClass.Universal)
        {
            throw new CryptographicException("Invalid Curve25519 private key");
        }

        using var privateKeyHandle = new ZeroingMemoryHandle(seqPrivateKey.ReadOctetString());
        seqPrivateKeyInfo.ThrowIfNotEmpty();
        if (privateKeyHandle.Data.Length != 32)
        {
            throw new CryptographicException("Invalid Curve25519 private key: incorrect length");
        }

        var keyDefinition = KeyDefinitions.GetByOid(oidAlgorithm);
        return new Curve25519PrivateKeyParameters(privateKeyHandle.Data, keyDefinition);

    }
    public static Curve25519PrivateKeyParameters CreateFromValue(ReadOnlyMemory<byte> privateKey, KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        return new Curve25519PrivateKeyParameters(privateKey, keyDefinition);
    }
}
