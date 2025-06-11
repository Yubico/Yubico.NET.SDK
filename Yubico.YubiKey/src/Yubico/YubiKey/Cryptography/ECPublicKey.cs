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

/// <summary>
/// Represents an Elliptic Curve (EC) public key.
/// </summary>
/// <remarks>
/// This class encapsulates EC public key parameters and provides cryptographic operations
/// for NIST elliptic curves and provides factory methods for creating instances from EC parameters or DER-encoded data.
/// </remarks>
public class ECPublicKey : PublicKey
{
    private readonly byte[] _publicPointBytes;

    /// <summary>
    /// Gets the key definition associated with this RSA private key.
    /// </summary>
    /// <value>
    /// A <see cref="KeyDefinition"/> object that describes the key's properties, including its type and length.
    /// </value>
    public KeyDefinition KeyDefinition { get; }

    /// <summary>
    /// Gets the Elliptic Curve parameters associated with this instance.
    /// </summary>
    /// <value>
    /// An <see cref="ECParameters"/> structure containing the curve parameters, key, and other
    /// cryptographic elements needed for EC operations.
    /// </value>
    public ECParameters Parameters { get; }

    /// <summary>
    /// Gets the bytes representing the public key coordinates.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the public key bytes with the format 0x04 || X || Y.</returns>
    public ReadOnlyMemory<byte> PublicPoint => _publicPointBytes;

    /// <inheritdoc />
    public override KeyType KeyType => KeyDefinition.KeyType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ECPublicKey"/> class.
    /// It is a wrapper for the <see cref="ECParameters"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is used to create an instance from a <see cref="ECParameters"/> object.
    /// It will deep copy the parameters from the <see cref="ECParameters"/> object.
    /// </remarks>
    /// <param name="parameters"></param>
    /// <exception cref="ArgumentException">Thrown when the parameters contain private key data (D value).</exception>
    protected ECPublicKey(ECParameters parameters)
    {
        if (parameters.D != null)
        {
            throw new ArgumentException(
                "Parameters must not contain private key data (D value)", nameof(parameters));
        }

        Parameters = parameters.DeepCopy();
        KeyDefinition = KeyDefinitions.GetByOid(Parameters.Curve.Oid);

        // Format identifier (uncompressed point): 0x04
        _publicPointBytes = [0x4, .. Parameters.Q.X, .. Parameters.Q.Y];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ECPublicKey"/> class.
    /// </summary>
    /// <param name="ecdsa"></param>
    protected ECPublicKey(ECDsa ecdsa)
    {
        if (ecdsa == null)
        {
            throw new ArgumentNullException(nameof(ecdsa));
        }

        Parameters = ecdsa.ExportParameters(false);
        KeyDefinition = KeyDefinitions.GetByOid(Parameters.Curve.Oid);

        // Format identifier (uncompressed point): 0x04
        _publicPointBytes = [0x4, .. Parameters.Q.X, .. Parameters.Q.Y];
    }

    /// <inheritdoc />
    public override byte[] ExportSubjectPublicKeyInfo() => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(Parameters);
    
    /// <summary>
    /// Creates an instance of <see cref="ECPublicKey"/> from the given <paramref name="parameters"/>.
    /// </summary>
    /// <param name="parameters">The parameters to create the key from.</param>
    /// <returns>An instance of <see cref="ECPublicKey"/>.</returns>
    public static ECPublicKey CreateFromParameters(ECParameters parameters) => new(parameters);

    /// <summary>
    /// Creates an instance of <see cref="ECPublicKey"/> from the given
    /// <paramref name="publicPoint"/> and <paramref name="keyType"/>.
    /// </summary>
    /// <param name="publicPoint">The raw public key data, formatted as an compressed point.</param>
    /// <param name="keyType">The type of key this is.</param>
    /// <returns>An instance of <see cref="ECPublicKey"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the key type is not a valid EC key.
    /// </exception>
    public static ECPublicKey CreateFromValue(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        if (keyDefinition.AlgorithmOid is not Oids.ECDSA)
        {
            throw new ArgumentException("Only P-256, P-384 and P-521 are supported.", nameof(keyType));
        }

        int coordinateLength = keyDefinition.LengthInBytes;
        var curve = ECCurve.CreateFromValue(keyDefinition.CurveOid);
        var ecParameters = new ECParameters
        {
            Curve = curve,
            Q = new ECPoint
            {
                X = publicPoint.Slice(1, coordinateLength).ToArray(),
                Y = publicPoint.Slice(coordinateLength + 1, coordinateLength).ToArray()
            }
        };

        return CreateFromParameters(ecParameters);
    }

    /// <summary>
    /// Creates an instance of <see cref="ECPublicKey"/> from a DER-encoded public key.
    /// </summary>
    /// <param name="encodedKey">The DER-encoded public key.</param>
    /// <returns>An instance of <see cref="IPublicKey"/>.</returns>
    /// <exception cref="CryptographicException">
    /// Thrown if the public key is invalid.
    /// </exception>
    public static ECPublicKey CreateFromSubjectPublicKeyInfo(ReadOnlyMemory<byte> encodedKey) =>
        AsnPublicKeyDecoder
            .CreatePublicKey(encodedKey)
            .Cast<ECPublicKey>();

    [Obsolete("Use CreateFromSubjectPublicKeyInfo instead", false)]
    public static ECPublicKey CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey) =>
    CreateFromSubjectPublicKeyInfo(encodedKey);
}
