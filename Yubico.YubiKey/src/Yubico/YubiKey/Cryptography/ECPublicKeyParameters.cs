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

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// Represents the parameters for an Elliptic Curve (EC) public key.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the parameters specific to EC public keys,
    /// ensuring that the key only contains necessary public key components.
    /// </remarks>
    public class ECPublicKeyParameters : IPublicKeyParameters
    {
        private readonly byte[] _publicPointBytes;
        public KeyDefinition KeyDefinition { get; }
        public ECParameters Parameters { get; }

        /// <summary>
        /// Gets the bytes representing the public key coordinates.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the public key bytes with the format 0x04 || X || Y.</returns>
        public ReadOnlyMemory<byte> PublicPoint => _publicPointBytes;

        public KeyType KeyType => KeyDefinition.KeyType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ECPublicKeyParameters"/> class.
        /// It is a wrapper for the <see cref="ECParameters"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is used to create an instance from a <see cref="ECParameters"/> object.
        /// It will deep copy the parameters from the <see cref="ECParameters"/> object.
        /// </remarks>
        /// <param name="parameters"></param>
        /// <exception cref="ArgumentException">Thrown when the parameters contain private key data (D value).</exception>
        [Obsolete("Use factory methods instead")]
        public ECPublicKeyParameters(ECParameters parameters)
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
        /// Initializes a new instance of the <see cref="ECPublicKeyParameters"/> class.
        /// </summary>
        /// <param name="ecdsa"></param>
        [Obsolete("Use factory methods instead")]
        // TODO The constructor should be private, but not possible to have to constructors with the same signature
        public ECPublicKeyParameters(ECDsa ecdsa)
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

        /// <summary>
        /// Converts this public key to an ASN.1 DER encoded format (X.509 SubjectPublicKeyInfo).
        /// </summary>
        /// <returns>
        /// A byte array containing the ASN.1 DER encoded public key.
        /// </returns>
        public byte[] ExportSubjectPublicKeyInfo() => AsnPublicKeyWriter.EncodeToSubjectPublicKeyInfo(Parameters);

        /// <summary>
        /// Gets the bytes representing the public key coordinates.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the public key bytes with the format 0x04 || X || Y.</returns>
        [Obsolete("Use PublicPoint instead")]
        public ReadOnlyMemory<byte> GetBytes() => _publicPointBytes;
        
        #pragma warning disable CS0618 // Type or member is obsolete
        public static ECPublicKeyParameters CreateFromParameters(ECParameters parameters) => new(parameters);
        #pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// Creates an instance of <see cref="ECPublicKeyParameters"/> from the given
        /// <paramref name="publicPoint"/> and <paramref name="keyType"/>.
        /// </summary>
        /// <param name="publicPoint">The raw public key data, formatted as an compressed point.</param>
        /// <param name="keyType">The type of key this is.</param>
        /// <returns>An instance of <see cref="ECPublicKeyParameters"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the key type is not a valid EC key.
        /// </exception>
        public static IPublicKeyParameters CreateFromValue(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
        {
            var keyDefinition = KeyDefinitions.GetByKeyType(keyType);
            if (keyDefinition.AlgorithmOid is not KeyDefinitions.CryptoOids.ECDSA)
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
        /// Creates an instance of <see cref="IPublicKeyParameters"/> from a DER-encoded public key.
        /// </summary>
        /// <param name="encodedKey">The DER-encoded public key.</param>
        /// <returns>An instance of <see cref="IPublicKeyParameters"/>.</returns>
        /// <exception cref="CryptographicException">
        /// Thrown if the public key is invalid.
        /// </exception>
        public static IPublicKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey) =>
            AsnPublicKeyReader.CreateKeyParameters(encodedKey);
    }
}
