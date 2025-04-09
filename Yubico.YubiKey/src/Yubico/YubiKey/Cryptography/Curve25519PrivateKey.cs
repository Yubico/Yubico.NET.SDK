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
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

public sealed class Curve25519PrivateKey : PrivateKey
{
    private readonly Memory<byte> _privateKey;

    /// <inheritdoc />
    public override KeyType KeyType => KeyDefinition.KeyType;
    
    /// <summary>
    /// Gets the key definition associated with this RSA private key.
    /// </summary>
    /// <value>
    /// A <see cref="KeyDefinition"/> object that describes the key's properties, including its type and length.
    /// </value>
    public KeyDefinition KeyDefinition { get; }

    /// <summary>
    /// Gets the bytes representing the private scalar value.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the private scalar value.</returns>
    public ReadOnlyMemory<byte> PrivateKey => _privateKey;
    
    private Curve25519PrivateKey(
        ReadOnlyMemory<byte> privateKey,
        KeyType keyType)
    {
        var keyDefinition = keyType.GetKeyDefinition();
        if (keyDefinition.AlgorithmOid == Oids.X25519)
        {
            AsnUtilities.VerifyX25519PrivateKey(privateKey.Span);
        }

        _privateKey = new byte[privateKey.Length];
        KeyDefinition = keyDefinition;

        privateKey.CopyTo(_privateKey);
    }
    
    /// <inheritdoc />
    public override byte[] ExportPkcs8PrivateKey() 
    {
        ThrowIfDisposed();
        return AsnPrivateKeyWriter.EncodeToPkcs8(_privateKey, KeyType);
    }
    /// <summary>
    /// Clears the private key.
    /// </summary>
    /// <remarks>
    /// This method securely zeroes out the private key data.
    /// </remarks>
    public override void Clear() => CryptographicOperations.ZeroMemory(_privateKey.Span);



    /// <summary>
    /// Creates an instance of <see cref="Curve25519PrivateKey"/> from a PKCS#8
    /// DER-encoded private key.
    /// </summary>
    /// <param name="pkcs8EncodedKey">
    /// The DER-encoded private key.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="Curve25519PrivateKey"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the algorithm OID is not X25519 or Ed25519.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown if privateKey does not match expected format.</exception>
    public static Curve25519PrivateKey CreateFromPkcs8(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        (byte[] privateKey, var keyType) = AsnPrivateKeyReader.GetCurve25519PrivateKeyData(pkcs8EncodedKey);
        using var privateKeyHandle = new ZeroingMemoryHandle(privateKey);
        return new Curve25519PrivateKey(privateKeyHandle.Data, keyType);
    }
    
    /// <summary>
    /// Creates an instance of <see cref="Curve25519PrivateKey"/> from the given
    /// <paramref name="privateKey"/> and <paramref name="keyType"/>.
    /// </summary>
    /// <param name="privateKey">The raw private key data. This is copied internally.</param>
    /// <param name="keyType">The type of key this is.</param>
    /// <returns>An instance of <see cref="Curve25519PrivateKey"/>.</returns>
    /// <exception cref="CryptographicException">Thrown if privateKey does not match expected format.</exception>
    public static Curve25519PrivateKey CreateFromValue(ReadOnlyMemory<byte> privateKey, KeyType keyType) => new(privateKey, keyType);

}
