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
    /// Represents the parameters for an Elliptic Curve (EC) public key.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the parameters specific to EC public keys,
    /// ensuring that the key only contains necessary public key components.
    /// It extends the base <see cref="ECKeyParameters"/> class with additional 
    /// validation to prevent the inclusion of private key data.
    /// </remarks>
    public class ECPublicKeyParameters : ECKeyParameters, IPublicKeyParameters
    {
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
        public ECPublicKeyParameters(ECParameters parameters) : base(parameters)
        {
            if (parameters.D != null)
            {
                throw new ArgumentException(
                    "Parameters must not contain private key data (D value)", nameof(parameters));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ECPublicKeyParameters"/> class.
        /// </summary>
        /// <param name="ecdsa"></param>
        public ECPublicKeyParameters(ECDsa ecdsa)
            : this(ecdsa?.ExportParameters(false) ?? throw new ArgumentNullException(nameof(ecdsa)))
        {

        }
        
        /// <summary>
        /// Gets the bytes representing the public key coordinates.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyMemory{T}"/> containing the public key bytes with the format 0x04 || X || Y.</returns>
        public ReadOnlyMemory<byte> GetBytes()
        {
            byte[] publicKeyRawData =
                new byte[] { 0x4 } // Format identifier (uncompressed point): 0x04
                    .Concat(Parameters.Q.X)
                    .Concat(Parameters.Q.Y)
                    .ToArray();

            return publicKeyRawData;
        }

        public ReadOnlyMemory<byte> GetPublicPoint() => GetBytes();
        public ReadOnlyMemory<byte> ExportSubjectPublicKeyInfo() => AsnPublicKeyWriter.EncodeToSpki(Parameters);

        public static ECPublicKeyParameters CreateFromParameters(ECParameters ecParameters) => new(ecParameters);
    }
}
