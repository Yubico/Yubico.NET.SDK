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
using System.Linq;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// Represents the parameters for an Elliptic Curve (EC) private key.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the parameters specific to EC private keys and
    /// contains the necessary private key data.
    /// </remarks>
    public class ECPrivateKeyParameters : IPrivateKeyParameters
    {
        public ECParameters Parameters { get; }
        public KeyDefinition KeyDefinition { get; }
        public KeyType KeyType => KeyDefinition.KeyType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ECPrivateKeyParameters"/> class.
        /// It is a wrapper for the <see cref="ECParameters"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is used to create an instance from a <see cref="ECParameters"/> object. It will deep copy 
        /// the parameters from the ECParameters object.
        /// </remarks>
        /// <param name="parameters">The EC parameters.</param>
        [Obsolete("Use factory methods instead")]
        public ECPrivateKeyParameters(ECParameters parameters)
        {
            if (parameters.D == null)
            {
                throw new ArgumentException("Parameters must contain private key data (D value)", nameof(parameters));
            }

            Parameters = parameters.DeepCopy();
            KeyDefinition = KeyDefinitions.GetByOid(parameters.Curve.Oid);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ECPrivateKeyParameters"/> class using a <see cref="ECDsa"/> object.
        /// </summary>
        /// <remarks>
        /// It exports the parameters from the ECDsa object and deep copy the parameters from the ECParameters object.
        /// </remarks>
        /// <param name="ecdsaObject">The ECDsa object.</param>
        [Obsolete("Use factory methods instead")]
        public ECPrivateKeyParameters(ECDsa ecdsaObject)
        {
            if (ecdsaObject == null)
            {
                throw new ArgumentNullException(nameof(ecdsaObject));
            }

            Parameters = ecdsaObject.ExportParameters(true);
            KeyDefinition = KeyDefinitions.GetByOid(Parameters.Curve.Oid);
        }

        public byte[] ExportPkcs8PrivateKey() => AsnPrivateKeyWriter.EncodeToPkcs8(Parameters);

        public void Clear()
        {
            CryptographicOperations.ZeroMemory(Parameters.Q.Y);
            CryptographicOperations.ZeroMemory(Parameters.Q.X);
            CryptographicOperations.ZeroMemory(Parameters.D);
        }

        /// <summary>
        /// Creates a new instance of <see cref="ECPrivateKeyParameters"/> from a DER-encoded private key.
        /// </summary>
        /// <param name="encodedKey">
        /// The DER-encoded private key.
        /// </param>
        /// <returns>
        /// A new instance of <see cref="ECPrivateKeyParameters"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// Thrown if the private key is invalid.
        /// </exception>
        public static ECPrivateKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
        {
            var parameters = AsnPrivateKeyReader.CreateECParameters(encodedKey);
            return CreateFromParameters(parameters);
        }
        
        #pragma warning disable CS0618 // Type or member is obsolete.
        public static ECPrivateKeyParameters CreateFromParameters(ECParameters parameters) => new(parameters);
        #pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// Creates a new instance of <see cref="ECPrivateKeyParameters"/> from the given
        /// <paramref name="privateValue"/> and <paramref name="keyType"/>.
        /// </summary>
        /// <remarks>
        /// The <paramref name="privateValue"/> is taken as the raw private key data (scalar value).
        /// </remarks>
        /// <param name="privateValue">The raw private key data.</param>
        /// <param name="keyType">The type of key this is.</param>
        /// <returns>A new instance of <see cref="ECPrivateKeyParameters"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the key type is not a valid EC key.
        /// </exception>
        public static ECPrivateKeyParameters CreateFromValue(
            ReadOnlyMemory<byte> privateValue,
            KeyType keyType)
        {
            var keyDefinition = keyType.GetKeyDefinition();
            if (keyDefinition.AlgorithmOid is not KeyDefinitions.CryptoOids.ECDSA)
            {
                throw new ArgumentException("Only P-256, P-384 and P-521 are supported.", nameof(keyType));
            }

            string curveOid = keyDefinition.CurveOid ??
                throw new ArgumentException("The key definition for this key type has no Curve OID is null.");

            var curve = ECCurve.CreateFromValue(curveOid);
            var parameters = new ECParameters
            {
                Curve = curve,
                D = privateValue.ToArray(),
            };

            return CreateFromParameters(parameters);
        }
    }
}
