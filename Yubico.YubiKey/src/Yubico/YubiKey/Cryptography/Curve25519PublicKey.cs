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
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

/// <summary>
/// Represents a Curve25519 public key.
/// </summary>
/// <remarks>
/// This sealed class encapsulates Curve25519 public key data as a compressed point
/// and supports both Ed25519 and X25519 key types.
/// It also provides factory methods for creating instances from public point values or DER-encoded data.
/// </remarks>
public sealed class Curve25519PublicKey : PublicKey
{
    private readonly Memory<byte> _publicPoint;

    /// <summary>
    /// Gets the key definition associated with this RSA private key.
    /// </summary>
    /// <value>
    /// A <see cref="KeyDefinition"/> object that describes the key's properties, including its type and length.
    /// </value>
    public KeyDefinition KeyDefinition { get; }

    /// <inheritdoc />
    public override KeyType KeyType => KeyDefinition.KeyType;

    /// <summary>
    /// Gets the bytes representing the public key coordinates as a compressed point.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the public key bytes.</returns>
    /// <remarks>
    /// The public key is represented as a byte array, which is the raw public key data.
    /// </remarks>
    public ReadOnlyMemory<byte> PublicPoint => _publicPoint;

    private Curve25519PublicKey(
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
    public override byte[] ExportSubjectPublicKeyInfo() =>
        AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(_publicPoint, KeyDefinition.KeyType);

    /// <summary>
    /// Creates a new instance of <see cref="Curve25519PublicKey"/> from a DER-encoded SubjectPublicKeyInfo.
    /// </summary>
    /// <param name="subjectPublicKeyInfo">
    /// The DER-encoded SubjectPublicKeyInfo.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="Curve25519PublicKey"/>.
    /// </returns>
    /// <exception cref="CryptographicException">
    /// Thrown if the subjectPublicKeyInfo is invalid.
    /// </exception>
    public static Curve25519PublicKey CreateFromSubjectPublicKeyInfo(ReadOnlyMemory<byte> subjectPublicKeyInfo) =>
        AsnPublicKeyDecoder
            .CreatePublicKey(subjectPublicKeyInfo)
            .Cast<Curve25519PublicKey>();

    /// <summary>
    /// Creates an instance of <see cref="Curve25519PublicKey"/> from the given
    /// <paramref name="publicPoint"/> and <paramref name="keyType"/>.
    /// </summary>
    /// <param name="publicPoint">The raw public key data, formatted as an compressed point.</param>
    /// <param name="keyType">The type of key this is.</param>
    /// <returns>An instance of <see cref="Curve25519PublicKey"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the public key data length is not valid.
    /// </exception>
    public static Curve25519PublicKey CreateFromValue(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        if (publicPoint.Length != keyDefinition.LengthInBytes)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPublicKeyData));
        }

        return new Curve25519PublicKey(publicPoint, keyDefinition);
    }

    [Obsolete("Use CreateFromSubjectPublicKeyInfo instead", false)]
    public static Curve25519PublicKey CreateFromPkcs8(ReadOnlyMemory<byte> subjectPublicKeyInfo) =>
        CreateFromSubjectPublicKeyInfo(subjectPublicKeyInfo);
}
