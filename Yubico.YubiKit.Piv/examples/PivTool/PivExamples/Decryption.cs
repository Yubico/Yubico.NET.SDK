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

            // Decrypt using explicit algorithm overload.
            // The auto-detect overload checks PIV app version (0.0.1), not device firmware.
            var algorithm = metadata.Value.Algorithm;
            var rawDecrypted = await session.SignOrDecryptAsync(slot, algorithm, encryptedData, cancellationToken);

            stopwatch.Stop();

            // The YubiKey returns the raw RSA decryption block including PKCS#1 v1.5 padding:
            // [0x00][0x02][non-zero padding bytes][0x00][plaintext]
            // Strip the padding to return the actual plaintext.
            var plaintext = StripPkcs1v15Padding(rawDecrypted.Span);
            if (plaintext.IsEmpty)
            {
                return DecryptionResult.Failed("Decryption succeeded but PKCS#1 v1.5 padding is malformed.");
            }

            return DecryptionResult.Succeeded(plaintext, stopwatch.ElapsedMilliseconds);
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

    /// <summary>
    /// Strips PKCS#1 v1.5 type-2 padding from a raw RSA decryption block.
    /// Format: [0x00][0x02][8+ non-zero padding bytes][0x00][plaintext]
    /// </summary>
    /// <returns>The plaintext slice, or empty if padding is malformed.</returns>
    private static ReadOnlyMemory<byte> StripPkcs1v15Padding(ReadOnlySpan<byte> block)
    {
        // Must start with 0x00 0x02
        if (block.Length < 11 || block[0] != 0x00 || block[1] != 0x02)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        // Find the 0x00 separator (must be at position 10 or later — minimum 8 padding bytes)
        int separatorIndex = -1;
        for (int i = 2; i < block.Length; i++)
        {
            if (block[i] == 0x00)
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex < 10 || separatorIndex == block.Length - 1)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        return block[(separatorIndex + 1)..].ToArray();
    }
}