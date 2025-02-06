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

using System.Linq;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography
{

    /// <summary>
    /// Helper extensions for parameter copying
    /// </summary>
    public static class ECParametersExtensions
    {
        /// <summary>
        /// Performs a deep copy of the EC parameters.
        /// </summary>
        /// <param name="original">The original ECParameters to copy.</param>
        /// <returns>A new ECParameters with the same values as the original.</returns>
        public static ECParameters DeepCopy(this ECParameters original)
        {
            var copy = new ECParameters
            {
                Curve = original.Curve,
                Q = new ECPoint
                {
                    X = original.Q.X?.ToArray(),
                    Y = original.Q.Y?.ToArray()
                },
                D = original.D?.ToArray()
            };

            return copy;
        }
    }
}
