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

namespace Yubico.YubiKey.Cryptography;

public static class RSAParametersExtensions
{
    /// <summary>
    /// Performs a deep copy of the EC parameters.
    /// </summary>
    /// <param name="original">The original RSAParameters to copy.</param>
    /// <returns>A new RSAParameters with the same values as the original.</returns>
    public static RSAParameters DeepCopy(this RSAParameters original)
    {
        var copy = new RSAParameters
        {
            D = original.D?.ToArray(),
            DP = original.DP?.ToArray(),
            DQ = original.DQ?.ToArray(),
            Exponent = original.Exponent?.ToArray(),
            InverseQ = original.InverseQ?.ToArray(),
            Modulus = original.Modulus?.ToArray(),
            P = original.P?.ToArray(),
            Q = original.Q?.ToArray()
        };

        return copy;
    }
}
