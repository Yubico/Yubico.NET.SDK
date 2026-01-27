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

using System.Diagnostics;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

/// <summary>
/// Demonstrates PIV RSA decryption operations using the YubiKey.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples of decrypting data with RSA private keys stored in PIV slots.
/// Only RSA keys support decryption; ECC keys cannot be used for this operation.
/// </para>
/// </remarks>
public static class Decryption
{
    /// <summary>
    /// Decrypts data using the RSA private key in a PIV slot.
    /// </summary>
    /// <param name="session">An authenticated PIV session with PIN already verified if required.</param>
    /// <param name="slot">The slot containing the RSA decryption key.</param>
    /// <param name="encryptedData">The data encrypted with the corresponding public key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing decrypted data or error information.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// await session.VerifyPinAsync(pin, ct);
    /// 
    /// // Set touch callback on PivSession if needed
    /// if (session is PivSession pivSession)
    ///     pivSession.OnTouchRequired = () => Console.WriteLine("Touch required");
    /// 
    /// var result = await Decryption.DecryptDataAsync(
    ///     session,
    ///     PivSlot.KeyManagement,
    ///     encryptedBytes,
    ///     ct);
    /// 
    /// if (result.Success)
    /// {
    ///     var plaintext = result.DecryptedData;
    /// }
    /// </code>
    /// </example>
    public static async Task<DecryptionResult> DecryptDataAsync(
        IPivSession session,
        PivSlot slot,
        ReadOnlyMemory<byte> encryptedData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            // Check if slot has an RSA key
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            if (metadata is null)
            {
                return DecryptionResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
            }

            if (!metadata.Value.Algorithm.IsRsa())
            {
                return DecryptionResult.Failed("Decryption requires an RSA key. Selected slot has an ECC key.");
            }

            var stopwatch = Stopwatch.StartNew();

            // Decrypt using simplified API
            var decrypted = await session.SignOrDecryptAsync(slot, encryptedData, cancellationToken);

            stopwatch.Stop();
            return DecryptionResult.Succeeded(decrypted, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            return DecryptionResult.Failed("Touch timeout. Please touch the YubiKey when prompted.");
        }
        catch (Exception ex)
        {
            return DecryptionResult.Failed($"Decryption failed: {ex.Message}");
        }
    }
}
