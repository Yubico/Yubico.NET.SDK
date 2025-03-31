// // Copyright 2024 Yubico AB
// // 
// // Licensed under the Apache License, Version 2.0 (the "License").
// // You may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// // 
// //     http://www.apache.org/licenses/LICENSE-2.0
// // 
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

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
    /// It extends the base <see cref="ECKeyParameters"/> class with additional 
    /// validation to prevent the inclusion of private key data.
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

        public byte[] ExportSubjectPublicKeyInfo() => AsnPublicKeyWriter.EncodeToSubjectPublicKeyInfo(Parameters);

        /// <summary>
        /// Gets the bytes representing the public key coordinates.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the public key bytes with the format 0x04 || X || Y.</returns>
        [Obsolete("Use PublicPoint instead")]
        public ReadOnlyMemory<byte> GetBytes() => _publicPointBytes;
        
        public static ECPublicKeyParameters CreateFromParameters(ECParameters ecParameters) => new(ecParameters);

        public static IPublicKeyParameters CreateFromValue(ReadOnlyMemory<byte> publicPoint, KeyType keyType)
        {
            if (!keyType.IsEcKey() ||
                keyType == KeyType.X25519 ||
                keyType == KeyType.Ed25519)
            {
                throw new ArgumentException("Only P-256, P-384 and P-521 are supported.", nameof(keyType));
            }

            var keyDef = KeyDefinitions.GetByKeyType(keyType);
            int coordinateLength = keyDef.LengthInBytes;
            var curve = ECCurve.CreateFromValue(keyDef.CurveOid);
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

        public static IPublicKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey) =>
            AsnPublicKeyReader.CreateKeyParameters(encodedKey);
    }
}
