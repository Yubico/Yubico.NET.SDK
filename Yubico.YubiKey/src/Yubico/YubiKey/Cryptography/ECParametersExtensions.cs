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
    /// Helper extensions for parameter copying
    /// </summary>
    internal static class ECParametersExtensions
    {
        public static ECParameters DeepCopy(this ECParameters parameters)
        {
            var copy = new ECParameters
            {
                Curve = parameters.Curve,
                Q = new ECPoint
                {
                    X = parameters.Q.X?.ToArray(),
                    Y = parameters.Q.Y?.ToArray()
                }
            };

            if (parameters.D != null)
            {
                copy.D = parameters.D.ToArray();
            }

            return copy;
        }
        
        /// <summary>
        /// Creates an instance from a byte array.
        /// </summary>
        /// <remarks>
        /// The byte array is expected to be in the format 0x04 || X || Y
        /// where X and Y are the uncompressed coordinates of the point.
        /// </remarks>
        /// <param name="bytes">The byte array.</param>
        /// <returns>An instance of EccPrivateKeyParameters with the nistP256 curve.</returns>
        public static ECPublicKeyParameters CreateEcPublicKeyFromBytes(this ReadOnlySpan<byte> bytes)
        {
            var ecParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = bytes.Slice(1, 32).ToArray(),
                    Y = bytes.Slice(33, 32).ToArray()
                }
            };
            
            return new ECPublicKeyParameters(ecParameters);
        }
    }
}
