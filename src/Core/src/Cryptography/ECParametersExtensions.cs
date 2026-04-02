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
///     Helper extensions for parameter copying
/// </summary>
public static class ECParametersExtensions
{
    #region Nested type: $extension

    extension(ECParameters original)
    {
        /// <summary>
        ///     Performs a deep copy of the EC parameters.
        /// </summary>
        /// <returns>A new ECParameters with the same values as the original.</returns>
        public ECParameters DeepCopy()
        {
            var copy = new ECParameters
            {
                Curve = original.Curve,
                Q = new ECPoint { X = original.Q.X?.ToArray(), Y = original.Q.Y?.ToArray() },
                D = original.D?.ToArray()
            };

            return copy;
        }

        public byte[] ToUncompressedPoint()
        {
            if (original.Q.X is null || original.Q.Y is null)
                throw new ArgumentException(
                    "ECParameters must contain public key data (Q.X and Q.Y values)", nameof(original));

            // Format identifier (uncompressed point): 0x04 + X + Y
            var publicPointBytes = new byte[1 + original.Q.X.Length + original.Q.Y.Length];

            publicPointBytes[0] = 0x4;
            original.Q.X.AsSpan().CopyTo(publicPointBytes.AsSpan(1));
            original.Q.Y.AsSpan().CopyTo(publicPointBytes.AsSpan(1 + original.Q.X.Length));

            return publicPointBytes;
        }
    }

    #endregion
}