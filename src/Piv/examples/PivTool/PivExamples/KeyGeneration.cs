// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

/// <summary>
/// Demonstrates PIV key generation operations using the YubiKey.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples for generating new key pairs in PIV slots.
/// Key generation requires management key authentication.
/// </para>
/// <para>
/// RSA 4096 key generation may take 30+ seconds. Generated private keys
/// cannot be exported but the public key is returned.
/// </para>
/// </remarks>
public static class KeyGeneration
{
    /// <summary>
    /// Generates a new key pair in the specified PIV slot.
    /// </summary>
    /// <param name="session">An authenticated PIV session (management key verified).</param>
    /// <param name="slot">The slot to generate the key in.</param>
    /// <param name="algorithm">The key algorithm and size.</param>
    /// <param name="pinPolicy">PIN verification policy for key usage.</param>
    /// <param name="touchPolicy">Touch requirement policy for key usage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the public key or error information.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// await session.AuthenticateAsync(managementKey, ct);
    /// 
    /// var result = await KeyGeneration.GenerateKeyAsync(
    ///     session,
    ///     PivSlot.Authentication,
    ///     PivAlgorithm.EccP256,
    ///     PivPinPolicy.Once,
    ///     PivTouchPolicy.Cached,
    ///     ct);
    /// 
    /// if (result.Success)
    /// {
    ///     var publicKeyBytes = result.PublicKey;
    /// }
    /// </code>
    /// </example>
    public static async Task<KeyGenerationResult> GenerateKeyAsync(
        IPivSession session,
        PivSlot slot,
        PivAlgorithm algorithm,
        PivPinPolicy pinPolicy = PivPinPolicy.Default,
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var publicKey = await session.GenerateKeyAsync(
                slot,
                algorithm,
                pinPolicy,
                touchPolicy,
                cancellationToken);

            var spki = publicKey.ExportSubjectPublicKeyInfo();
            return KeyGenerationResult.Succeeded(slot, algorithm, spki);
        }
        catch (NotSupportedException ex)
        {
            return KeyGenerationResult.Failed($"Algorithm {algorithm} not supported: {ex.Message}");
        }
        catch (Exception ex)
        {
            return KeyGenerationResult.Failed($"Key generation failed: {ex.Message}");
        }
    }
}
