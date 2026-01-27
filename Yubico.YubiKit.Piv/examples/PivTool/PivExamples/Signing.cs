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
using System.Security.Cryptography;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

/// <summary>
/// Demonstrates PIV signing operations using the YubiKey.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples of signing data with private keys stored in PIV slots.
/// Keys must be generated or imported into the slot before signing.
/// </para>
/// <para>
/// PIN verification may be required depending on the slot's PIN policy.
/// Touch may be required depending on the slot's touch policy.
/// </para>
/// </remarks>
public static class Signing
{
    /// <summary>
    /// Signs data using the private key in a PIV slot.
    /// </summary>
    /// <param name="session">An authenticated PIV session with PIN already verified if required.</param>
    /// <param name="slot">The slot containing the signing key.</param>
    /// <param name="dataToSign">The data to sign (will be hashed).</param>
    /// <param name="hashAlgorithm">Hash algorithm to use (SHA256, SHA384, or SHA512).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing signature or error information.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// await session.VerifyPinAsync(pin, ct);
    /// 
    /// // Set touch callback on PivSession if needed
    /// if (session is PivSession pivSession)
    ///     pivSession.OnTouchRequired = () => Console.WriteLine("Touch required");
    /// 
    /// var result = await Signing.SignDataAsync(
    ///     session, 
    ///     PivSlot.Signature, 
    ///     dataBytes,
    ///     HashAlgorithmName.SHA256,
    ///     ct);
    /// 
    /// if (result.Success)
    /// {
    ///     var signature = result.Signature;
    /// }
    /// </code>
    /// </example>
    public static async Task<SigningResult> SignDataAsync(
        IPivSession session,
        PivSlot slot,
        ReadOnlyMemory<byte> dataToSign,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            // Check if slot has a key
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            if (metadata is null)
            {
                return SigningResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
            }

            var stopwatch = Stopwatch.StartNew();

            // Hash the data
            byte[] hash = hashAlgorithm.Name switch
            {
                "SHA256" => SHA256.HashData(dataToSign.Span),
                "SHA384" => SHA384.HashData(dataToSign.Span),
                "SHA512" => SHA512.HashData(dataToSign.Span),
                _ => SHA256.HashData(dataToSign.Span)
            };

            // Sign using simplified API (auto-detects algorithm from slot metadata)
            var signature = await session.SignOrDecryptAsync(slot, hash, cancellationToken);

            stopwatch.Stop();
            return SigningResult.Succeeded(signature, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            return SigningResult.Failed("Touch timeout. Please touch the YubiKey when prompted.");
        }
        catch (Exception ex)
        {
            return SigningResult.Failed($"Signing failed: {ex.Message}");
        }
    }
}
