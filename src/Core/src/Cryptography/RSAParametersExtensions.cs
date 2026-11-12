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
    /// <returns>Parameters with independent arrays. The caller must clear copied private components after use.</returns>
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
    /// Copies the parameters and, when all private components and Modulus are present, left-pads
    /// shorter D to the modulus width and shorter CRT components to half that width (rounded up).
    /// Oversized arrays are preserved. Missing components leave the copy unpadded; this helper does
    /// not establish mathematical validity or acceptance by every platform provider.
    /// </summary>
    /// <param name="parameters">The RSA parameters to normalize</param>
    /// <returns>A new (copied) RSAParameters with normalized values</returns>
    internal static RSAParameters NormalizeParameters(this RSAParameters parameters) =>
        NormalizeParameters(parameters, parametersCopied: null);

    // Test-only observers may retain aliases to inspect clearing after the owner exits; never mutate live arrays.
    internal static RSAParameters NormalizeParameters(
        this RSAParameters parameters,
        Action<RSAParameters>? parametersCopied)
    {
        var normalized = parameters.DeepCopy();
        try
        {
            parametersCopied?.Invoke(normalized);
            if (normalized.D is null || normalized.P is null || normalized.Q is null ||
                normalized.DP is null || normalized.DQ is null || normalized.InverseQ is null ||
                normalized.Modulus is null)
            {
                return normalized; // Can't normalize if missing required components
            }

            // For private key, we need D to be same length as Modulus,
            // and P, Q, DP, DQ, InverseQ to be half the length of Modulus (rounded up)
            var modulusLength = normalized.Modulus.Length;
            var halfLength = (modulusLength + 1) / 2; // Round up

            normalized.D = PadToLengthAndClear(normalized.D, modulusLength);
            normalized.P = PadToLengthAndClear(normalized.P, halfLength);
            normalized.Q = PadToLengthAndClear(normalized.Q, halfLength);
            normalized.DP = PadToLengthAndClear(normalized.DP, halfLength);
            normalized.DQ = PadToLengthAndClear(normalized.DQ, halfLength);
            normalized.InverseQ = PadToLengthAndClear(normalized.InverseQ, halfLength);

            return normalized;
        }
        catch
        {
            ClearPrivateParameters(normalized);
            throw;
        }
    }

    private static byte[] PadToLengthAndClear(byte[] data, int targetLength)
    {
        var padded = PadToLength(data, targetLength);
        if (!ReferenceEquals(data, padded))
        {
            CryptographicOperations.ZeroMemory(data);
        }

        return padded;
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

    private static void ClearPrivateParameters(RSAParameters parameters)
    {
        CryptographicOperations.ZeroMemory(parameters.D);
        CryptographicOperations.ZeroMemory(parameters.P);
        CryptographicOperations.ZeroMemory(parameters.Q);
        CryptographicOperations.ZeroMemory(parameters.DP);
        CryptographicOperations.ZeroMemory(parameters.DQ);
        CryptographicOperations.ZeroMemory(parameters.InverseQ);
    }

}