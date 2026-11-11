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

using System.Globalization;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Represents a Curve25519 public key.
/// </summary>
/// <remarks>
/// Stores the RFC 8410 public value: an Ed25519 compressed Edwards point or an X25519
/// little-endian Montgomery u-coordinate. These are distinct constructions, not interchangeable points.
/// It also provides factory methods for creating instances from public point values or DER-encoded data.
/// </remarks>
public sealed class Curve25519PublicKey : PublicKey
{
    private readonly Memory<byte> _publicPoint;

    /// <summary>
    /// Gets the metadata associated with this 25519 public key.
    /// </summary>
    /// <value>
    /// A <see cref="KeyInfo"/> object that describes the key's representation.
    /// </value>
    public override KeyInfo KeyInfo { get; }

    /// <inheritdoc />
    public override KeyType KeyType => KeyInfo.KeyType;

    /// <summary>
    /// Gets the RFC 8410 public value for the concrete construction.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the public key bytes.</returns>
    /// <remarks>
    /// The public key is represented as a byte array, which is the raw public key data.
    /// </remarks>
    public ReadOnlyMemory<byte> PublicPoint => _publicPoint;

    private Curve25519PublicKey(
        ReadOnlyMemory<byte> publicPoint,
        KeyInfo keyInfo)
    {
        KeyInfo = keyInfo;
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
        AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(_publicPoint, KeyInfo.KeyType);

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
    /// <param name="publicPoint">The raw 32-byte RFC 8410 public value for the concrete construction.</param>
    /// <param name="keyType">The type of key this is.</param>
    /// <returns>An instance of <see cref="Curve25519PublicKey"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the public key data length is not valid.
    /// </exception>
    public static Curve25519PublicKey CreateFromValue(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
    {
        var keyInfo = keyType.GetKeyInfo();
        if (!keyType.IsCurve25519())
        {
            throw new ArgumentException("Only X25519 and Ed25519 representations are supported.", nameof(keyType));
        }
        if (publicPoint.Length != keyInfo.PublicValueLengthBytes)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Invalid 25519 public value: expected {0} bytes.", keyInfo.PublicValueLengthBytes), nameof(publicPoint));
        }

        return new Curve25519PublicKey(publicPoint, keyInfo);
    }
}