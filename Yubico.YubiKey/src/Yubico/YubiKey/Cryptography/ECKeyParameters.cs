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
    /// Base class for EC key parameters
    /// </summary>
    public abstract class ECKeyParameters
    {
        /// <summary>
        /// Gets the EC parameters associated with this key.
        /// </summary>
        /// <remarks>
        /// These parameters include the curve information and key data.
        /// For NIST P-256 keys, this includes the curve definition and public point coordinates.
        /// </remarks>
        public ECParameters Parameters { get; }

        protected ECKeyParameters(ECParameters parameters)
        {
            if (parameters.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            {
                throw new NotSupportedException("Key must be of type NIST P-256");
            }

            Parameters = parameters.DeepCopy();
        }
    }
}
