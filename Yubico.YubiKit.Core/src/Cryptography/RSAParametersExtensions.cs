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

public static class RSAParametersExtensions
{
    /// <summary>
    /// Performs a deep copy of the RSA parameters.
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
    
    /// <summary>
    /// Normalizes the RSA parameters to ensure consistent cross-platform behavior:
    /// - If D is present, it must have the same length as Modulus
    /// - If D is present, P, Q, DP, DQ, and InverseQ are required and must have half the length of Modulus (rounded up)
    /// 
    /// This ensures compatibility with stricter platforms like Windows BCrypt while preserving the original key data.
    /// </summary>
    /// <param name="parameters">The RSA parameters to normalize</param>
    /// <returns>A new (copied) RSAParameters with normalized values</returns>
    internal static RSAParameters NormalizeParameters(this RSAParameters parameters)
    {
        var normalized = parameters.DeepCopy();
        if (normalized.D == null || normalized.P == null || normalized.Q == null || 
            normalized.DP == null || normalized.DQ == null || normalized.InverseQ == null ||
            normalized.Modulus == null)
        {
            return normalized; // Can't normalize if missing required components
        }
        
        // For private key, we need D to be same length as Modulus,
        // and P, Q, DP, DQ, InverseQ to be half the length of Modulus (rounded up)
        var modulusLength = normalized.Modulus.Length;
        var halfLength = (modulusLength + 1) / 2; // Round up
        
        normalized.D = PadToLength(normalized.D, modulusLength);
        normalized.P = PadToLength(normalized.P, halfLength);
        normalized.Q = PadToLength(normalized.Q, halfLength);
        normalized.DP = PadToLength(normalized.DP, halfLength);
        normalized.DQ = PadToLength(normalized.DQ, halfLength);
        normalized.InverseQ = PadToLength(normalized.InverseQ, halfLength);
        
        return normalized;
    }
    
    /// <summary>
    /// Pads a byte array to the specified length by adding leading zeros.
    /// This preserves the value of the array while ensuring it meets length requirements.
    /// Leading zeros do not change the mathematical value of the integer.
    /// </summary>
    private static byte[] PadToLength(byte[] data, int targetLength)
    {
        if (data.Length == targetLength)
        {
            return data;
        }
        
        if (data.Length > targetLength)
        {
            return data; 
        }
        
        // Pad with zeros at the beginning (most significant bytes)
        var result = new byte[targetLength];
        var padding = targetLength - data.Length;
        System.Buffer.BlockCopy(data, 0, result, padding, data.Length);
        
        return result;
    }
    
}
