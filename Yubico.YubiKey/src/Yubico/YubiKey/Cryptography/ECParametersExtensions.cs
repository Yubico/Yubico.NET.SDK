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
    public static class ECParametersExtensions
    {
        public static ECParameters DeepCopy(this ECParameters original)
        {
            if (original.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            {
                throw new NotSupportedException("Key must be of type NIST P-256");
            }

            var copy = new ECParameters
            {
                Curve = original.Curve,
                Q = new ECPoint
                {
                    X = original.Q.X?.ToArray(),
                    Y = original.Q.Y?.ToArray()
                },
                D = original.D?.ToArray() ?? Array.Empty<byte>()
            };
            
            return copy;
        }

        /// <summary>
        /// Creates an instance from a byte array.
        /// </summary>
        /// <remarks>
        /// The byte array is expected to be in the format 0x04 || X || Y
        /// where X and Y are the uncompressed (32 bit) coordinates of the point.
        /// </remarks>
        /// <param name="bytes">The byte array.</param>
        /// <returns>An instance of EcPrivateKeyParameters with the nistP256 curve.</returns>
        /// <exception cref="ArgumentException">Thrown when the byte array is not in the expected format.
        /// Either the first byte is not 0x04, or the byte array is not 65 bytes long (Key must be of type NIST P-256).</exception>
        public static ECPublicKeyParameters CreateECPublicKeyFromBytes(this ReadOnlySpan<byte> bytes)
        {
            if (bytes[0] != 0x04)
            {
                throw new ArgumentException("The byte array must start with 0x04", nameof(bytes));
            }

            if (bytes.Length != 65)
            {
                throw new ArgumentException("The byte array must be 65 bytes long (Key must be of type NIST P-256)", nameof(bytes));
            }

            var ecParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = bytes.Slice(1, 32).ToArray(), // Starts at 1 because the first byte is 0x04, indicating that it is an uncompressed point
                    Y = bytes.Slice(33, 32).ToArray()
                }
            };

            return new ECPublicKeyParameters(ecParameters);
        }
    }
}
