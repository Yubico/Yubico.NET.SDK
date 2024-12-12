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
    /// Represents the parameters for an Elliptic Curve (EC) private key.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the parameters specific to EC private keys and
    /// contains the necessary private key data.
    /// It extends the base <see cref="ECKeyParameters"/> class with additional validation for private key components.
    /// </remarks>

    public class ECPrivateKeyParameters : ECKeyParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ECPrivateKeyParameters"/> class.
        /// It is a wrapper for the <see cref="ECParameters"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is used to create an instance from a <see cref="ECParameters"/> object. It will deep copy 
        /// the parameters from the ECParameters object.
        /// </remarks>
        /// <param name="parameters">The EC parameters.</param>
        public ECPrivateKeyParameters(ECParameters parameters) : base(parameters)
        {
            if (parameters.D == null)
            {
                throw new ArgumentException("Parameters must contain private key data (D value)", nameof(parameters));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ECPrivateKeyParameters"/> class using a <see cref="ECDsa"/> object.
        /// </summary>
        /// <remarks>
        /// It exports the parameters from the ECDsa object and deep copy the parameters from the ECParameters object.
        /// </remarks>
        /// <param name="ecdsaObject">The ECDsa object.</param>
        public ECPrivateKeyParameters(ECDsa ecdsaObject)
            : base(ecdsaObject?.ExportParameters(true) ?? throw new ArgumentNullException())
        {
            
        }
    }
}
