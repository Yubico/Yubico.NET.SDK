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

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
///     Represents an Elliptic Curve (EC) public key.
/// </summary>
/// <remarks>
///     This class encapsulates EC public key parameters and provides cryptographic operations
///     for NIST elliptic curves and provides factory methods for creating instances from EC parameters or DER-encoded
///     data.
/// </remarks>
public class ECPublicKey : PublicKey
{
    private readonly byte[] _publicPointBytes;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ECPublicKey" /> class.
    ///     It is a wrapper for the <see cref="ECParameters" /> class.
    /// </summary>
    /// <remarks>
    ///     This constructor is used to create an instance from a <see cref="ECParameters" /> object.
    ///     It will deep copy the parameters from the <see cref="ECParameters" /> object.
    /// </remarks>
    /// <param name="parameters">The EC parameters containing the public key data.</param>
    /// <exception cref="ArgumentException">Thrown when the parameters contain private key data (D value).</exception>
    private ECPublicKey(ECParameters parameters)
    {
        if (parameters.D is not null)
            throw new ArgumentException(
                "Parameters must not contain private key data (D value)", nameof(parameters));

        if (parameters.Q.X is null || parameters.Q.Y is null)
            throw new ArgumentException(
                "Parameters must contain public key data (Q.X and Q.Y values)", nameof(parameters));

        Parameters = parameters.DeepCopy();
        KeyDefinition = KeyDefinitions.GetByOid(Parameters.Curve.Oid);

        // Format identifier (uncompressed point): 0x04
        _publicPointBytes = [0x4, .. Parameters.Q.X!, .. Parameters.Q.Y!];
    }

    /// <summary>
    ///     Gets the Elliptic Curve parameters associated with this instance.
    /// </summary>
    /// <value>
    ///     An <see cref="ECParameters" /> structure containing the curve parameters, key, and other
    ///     cryptographic elements needed for EC operations.
    /// </value>
    public ECParameters Parameters { get; }

    /// <summary>
    ///     Gets the key definition associated with this EC public key.
    /// </summary>
    /// <value>
    ///     A <see cref="KeyDefinition" /> object that describes the key's properties, including its type and length.
    /// </value>
    public KeyDefinition KeyDefinition { get; }

    /// <summary>
    ///     Gets the bytes representing the public key coordinates.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}" /> containing the public key bytes with the format 0x04 || X || Y.</returns>
    public ReadOnlyMemory<byte> PublicPoint => _publicPointBytes;

    /// <inheritdoc />
    public override KeyType KeyType => KeyDefinition.KeyType;

    /// <inheritdoc />
    public override byte[] ExportSubjectPublicKeyInfo() => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(Parameters);

    /// <summary>
    ///     Creates an instance of <see cref="ECPublicKey" /> from the given <paramref name="parameters" />.
    /// </summary>
    /// <param name="parameters">The parameters to create the key from.</param>
    /// <returns>An instance of <see cref="ECPublicKey" />.</returns>
    public static ECPublicKey CreateFromParameters(ECParameters parameters) => new(parameters);

    /// <summary>
    ///     Creates an instance of <see cref="ECPublicKey" /> from an <see cref="ECDiffieHellman" /> instance.
    /// </summary>
    /// <param name="ecdh">The ECDiffieHellman instance containing the public key.</param>
    /// <returns>An instance of <see cref="ECPublicKey" />.</returns>
    /// <remarks>
    ///     This method extracts the public key from the <see cref="ECDiffieHellman" /> instance
    ///     and creates a new <see cref="ECPublicKey" /> wrapper. This is useful when working with ephemeral
    ///     keys generated via <see cref="ECDiffieHellman.Create(ECCurve)" /> for protocols like SCP11.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ecdh" /> is null.</exception>
    public static ECPublicKey CreateFromEcdh(ECDiffieHellman ecdh)
    {
        ArgumentNullException.ThrowIfNull(ecdh);
        return CreateFromParameters(ecdh.PublicKey.ExportParameters());
    }

    /// <summary>
    ///     Creates an instance of <see cref="ECPublicKey" /> from an <see cref="ECDiffieHellmanPublicKey" /> instance.
    /// </summary>
    /// <param name="publicKey">The ECDiffieHellmanPublicKey instance.</param>
    /// <returns>An instance of <see cref="ECPublicKey" />.</returns>
    /// <remarks>
    ///     This method exports the public key parameters from the <see cref="ECDiffieHellmanPublicKey" /> instance
    ///     and creates a new <see cref="ECPublicKey" /> wrapper. This is useful when receiving public keys
    ///     from key agreement operations or certificate imports.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publicKey" /> is null.</exception>
    public static ECPublicKey CreateFromEcdh(ECDiffieHellmanPublicKey publicKey)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        return CreateFromParameters(publicKey.ExportParameters());
    }

    /// <summary>
    ///     Creates an instance of <see cref="ECPublicKey" /> from the given
    ///     <paramref name="publicPoint" /> and <paramref name="keyType" />.
    /// </summary>
    /// <param name="publicPoint">The raw public key data, formatted as an uncompressed point (0x04 || X || Y).</param>
    /// <param name="keyType">The type of key this is.</param>
    /// <returns>An instance of <see cref="ECPublicKey" />.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the key type is not a valid EC key.
    /// </exception>
    public static ECPublicKey CreateFromValue(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
    {
        var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
        if (keyDefinition.AlgorithmOid is not Oids.ECDSA)
            throw new ArgumentException("Only P-256, P-384 and P-521 are supported.", nameof(keyType));

        var coordinateLength = keyDefinition.LengthInBytes;
        var curve = ECCurve.CreateFromValue(keyDefinition.CurveOid!);
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
    ///     Creates an instance of <see cref="ECPublicKey" /> from a DER-encoded SubjectPublicKeyInfo.
    /// </summary>
    /// <param name="subjectPublicKeyInfo">The DER-encoded SubjectPublicKeyInfo.</param>
    /// <returns>An instance of <see cref="IPublicKey" />.</returns>
    /// <exception cref="CryptographicException">
    ///     Thrown if the subjectPublicKeyInfo is invalid.
    /// </exception>
    public static ECPublicKey CreateFromSubjectPublicKeyInfo(ReadOnlyMemory<byte> subjectPublicKeyInfo) =>
        AsnPublicKeyDecoder
            .CreatePublicKey(subjectPublicKeyInfo)
            .Cast<ECPublicKey>();

    /// <summary>
    ///     Converts this EC public key to an <see cref="ECDiffieHellmanPublicKey" /> for use in key agreement operations.
    /// </summary>
    /// <returns>An <see cref="ECDiffieHellmanPublicKey" /> instance that can be used with <see cref="ECDiffieHellman" />.</returns>
    /// <remarks>
    ///     This method creates a new <see cref="ECDiffieHellman" /> instance from the stored EC parameters
    ///     and returns its public key. This is useful for ECDH key agreement operations such as those
    ///     used in SCP11 (Secure Channel Protocol 11).
    /// </remarks>
    public ECDiffieHellmanPublicKey ToECDiffieHellmanPublicKey()
    {
        using var ecdh = ECDiffieHellman.Create(Parameters);
        return ecdh.PublicKey;
    }

    /// <summary>
    ///     Performs Elliptic Curve Diffie-Hellman (ECDH) key agreement using this public key
    ///     and the provided private key.
    /// </summary>
    /// <param name="privateKey">The ECDH private key to use for key agreement.</param>
    /// <returns>The derived key material as a byte array.</returns>
    /// <remarks>
    ///     This method derives shared secret key material using ECDH. The private key must be
    ///     compatible with this public key's curve parameters. The derived key material can then
    ///     be used with key derivation functions (KDFs) to generate session keys.
    ///     This operation is commonly used in protocols like SCP11 where both parties
    ///     derive a shared secret from their ephemeral/static key pairs.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="privateKey" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the curves are incompatible.</exception>
    public byte[] DeriveKeyMaterial(ECDiffieHellman privateKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        return privateKey.DeriveKeyMaterial(ToECDiffieHellmanPublicKey());
    }
}