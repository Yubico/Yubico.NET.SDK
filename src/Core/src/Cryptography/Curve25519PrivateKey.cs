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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Represents a Curve25519 private key.
/// </summary>
/// <remarks>
/// This sealed class encapsulates Curve25519 private key data and supports
/// both Ed25519 and X25519 cryptographic operations.
/// It also provides factory methods for creating instances from private key values or DER-encoded data.
/// Imported private key bytes are copied and preserved unchanged. In particular, X25519 masking occurs
/// when a cryptographic operation decodes the scalar as specified by RFC 7748, not when key bytes are
/// imported, encoded, or exported.
/// </remarks>
public sealed class Curve25519PrivateKey : PrivateKey
{
    private readonly Memory<byte> _privateKey;

    /// <inheritdoc />
    public override KeyType KeyType => KeyDefinition.KeyType;

    /// <summary>
    /// Gets the key definition associated with this Curve25519 private key.
    /// </summary>
    /// <value>
    /// A <see cref="KeyDefinition"/> object that describes the key's properties, including its type and length.
    /// </value>
    public KeyDefinition KeyDefinition { get; }

    /// <summary>
    /// Gets the raw private key bytes exactly as imported.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the raw private key value.</returns>
    public ReadOnlyMemory<byte> PrivateKey
    {
        get
        {
            ThrowIfDisposed();
            return _privateKey;
        }
    }

    private Curve25519PrivateKey(
        ReadOnlyMemory<byte> privateKey,
        KeyType keyType)
    {
        if (keyType is not (KeyType.X25519 or KeyType.Ed25519))
        {
            throw new ArgumentException("Only X25519 and Ed25519 are supported.", nameof(keyType));
        }

        var keyDefinition = keyType.GetKeyDefinition();

        if (privateKey.Length != 32)
        {
            throw new ArgumentException("Curve25519 private keys must be exactly 32 bytes.", nameof(privateKey));
        }

        _privateKey = new byte[privateKey.Length];
        KeyDefinition = keyDefinition;

        privateKey.CopyTo(_privateKey);
    }

    /// <inheritdoc />
    public override byte[] ExportPkcs8PrivateKey()
    {
        ThrowIfDisposed();
        return AsnPrivateKeyEncoder.EncodeToPkcs8(_privateKey, KeyType);
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
    /// ASN.1 DER-encoded private key.
    /// </summary>
    /// <param name="pkcs8EncodedKey">
    /// The ASN.1 DER-encoded private key.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="Curve25519PrivateKey"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the algorithm OID is not X25519 or Ed25519.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown if privateKey does not match expected format.</exception>
    /// <remarks>
    /// This method accepts version 0 PKCS#8 <c>PrivateKeyInfo</c>. RFC 5958 version value 1
    /// <c>OneAsymmetricKey</c> is valid but unsupported.
    /// </remarks>
    public static Curve25519PrivateKey CreateFromPkcs8(ReadOnlyMemory<byte> pkcs8EncodedKey)
    {
        (var privateKey, var keyType) = AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(pkcs8EncodedKey);
        using var privateKeyHandle = new DisposableBufferHandle(privateKey);
        return new Curve25519PrivateKey(privateKeyHandle.Data, keyType);
    }

    /// <summary>
    /// Creates an instance of <see cref="Curve25519PrivateKey"/> from the given
    /// <paramref name="privateKey"/> and <paramref name="keyType"/>.
    /// </summary>
    /// <param name="privateKey">The 32 raw private key bytes. These are copied and preserved unchanged.</param>
    /// <param name="keyType">The type of key this is.</param>
    /// <returns>An instance of <see cref="Curve25519PrivateKey"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="keyType"/> is not X25519 or Ed25519, or if <paramref name="privateKey"/> is not exactly 32 bytes.</exception>
    public static Curve25519PrivateKey CreateFromValue(ReadOnlyMemory<byte> privateKey, KeyType keyType) => new(privateKey, keyType);

}