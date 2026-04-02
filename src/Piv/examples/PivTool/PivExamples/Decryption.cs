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
    /// <param name="padding">RSA padding scheme used when encrypting. Defaults to PKCS#1 v1.5.</param>
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
        RSAEncryptionPadding? padding = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // session.DecryptAsync handles the raw RSA operation and strips padding,
            // returning clean plaintext — matching the Python yubikey-manager PivSession.decrypt() API.
            var plaintext = await session.DecryptAsync(
                slot, encryptedData, padding ?? RSAEncryptionPadding.Pkcs1, cancellationToken);

            stopwatch.Stop();
            return DecryptionResult.Succeeded(plaintext, stopwatch.ElapsedMilliseconds);
        }
        catch (ArgumentException ex)
        {
            return DecryptionResult.Failed(ex.Message);
        }
        catch (CryptographicException ex)
        {
            return DecryptionResult.Failed($"Decryption failed: {ex.Message}");
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