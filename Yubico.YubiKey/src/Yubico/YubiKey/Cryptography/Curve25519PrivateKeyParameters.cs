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
    private readonly Memory<byte> _privateKey;
    
    public KeyDefinition KeyDefinition { get; }
    public KeyType KeyType => KeyDefinition.KeyType;
    public ReadOnlyMemory<byte> PrivateKey => _privateKey;

    private Curve25519PrivateKeyParameters(
        ReadOnlyMemory<byte> privateKey,
        KeyType keyType)
    {
        var keyDefinition = keyType.GetKeyDefinition();
        if (keyDefinition.AlgorithmOid == KeyDefinitions.Oids.X25519)
        {
            AsnUtilities.VerifyX25519PrivateKey(privateKey.Span);
        }

        _privateKey = new byte[privateKey.Length];
        KeyDefinition = keyDefinition;

        privateKey.CopyTo(_privateKey);
    }
    
    /// <summary>
    /// Exports the private key in PKCS#8 DER encoded format.
    /// </summary>
    /// <returns>The DER encoded private key.</returns>
    public byte[] ExportPkcs8PrivateKey() => AsnPrivateKeyWriter.EncodeToPkcs8(_privateKey, KeyType);

    /// <summary>
    /// Clears the private key.
    /// </summary>
    /// <remarks>
    /// This method securely zeroes out the private key data.
    /// </remarks>
    public void Clear() => CryptographicOperations.ZeroMemory(_privateKey.Span);

    /// <summary>
    /// Creates an instance of <see cref="Curve25519PrivateKeyParameters"/> from a PKCS#8
    /// DER-encoded private key.
    /// </summary>
    /// <param name="encodedKey">
    /// The DER-encoded private key.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="Curve25519PrivateKeyParameters"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the algorithm OID is not X25519 or Ed25519.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown if the private key is invalid.
    /// </exception>
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
        string algorithmOid = seqAlgorithmIdentifier.ReadObjectIdentifier();
        if (algorithmOid != KeyDefinitions.Oids.X25519 &&
            algorithmOid != KeyDefinitions.Oids.Ed25519)
        {
            throw new ArgumentException(
                "Invalid curve OID. Must be: " + KeyDefinitions.Oids.X25519 + " or " +
                KeyDefinitions.Oids.Ed25519);
        }

        using var privateKeyDataHandle = new ZeroingMemoryHandle(seqPrivateKeyInfo.ReadOctetString());
        var seqPrivateKey = new AsnReader(privateKeyDataHandle.Data, AsnEncodingRules.DER);
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

        var keyDefinition = KeyDefinitions.GetByOid(algorithmOid);
        return new Curve25519PrivateKeyParameters(privateKeyHandle.Data, keyDefinition.KeyType);

    }
    
    /// <summary>
    /// Creates an instance of <see cref="Curve25519PrivateKeyParameters"/> from the given
    /// <paramref name="privateKey"/> and <paramref name="keyType"/>.
    /// </summary>
    /// <param name="privateKey">The raw private key data.</param>
    /// <param name="keyType">The type of key this is.</param>
    /// <returns>An instance of <see cref="Curve25519PrivateKeyParameters"/>.</returns>
    public static Curve25519PrivateKeyParameters CreateFromValue(ReadOnlyMemory<byte> privateKey, KeyType keyType) => new(privateKey, keyType);
}
