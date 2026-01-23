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
/// Demonstrates PIV PIN, PUK, and management key operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples for PIN management operations including
/// verification, changing PIN/PUK, unblocking PIN with PUK, and
/// changing the management key.
/// </para>
/// <para>
/// All PIN/PUK/key data should be zeroed from memory after use.
/// Callers are responsible for secure handling of credentials.
/// </para>
/// </remarks>
public static class PinManagement
{
    /// <summary>
    /// Verifies the PIN.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="pin">PIN as UTF-8 bytes (6-8 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with retries remaining.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// 
    /// var pin = Encoding.UTF8.GetBytes("123456");
    /// try
    /// {
    ///     var result = await PinManagement.VerifyPinAsync(session, pin, ct);
    ///     if (result.Success)
    ///     {
    ///         Console.WriteLine("PIN verified");
    ///     }
    /// }
    /// finally
    /// {
    ///     CryptographicOperations.ZeroMemory(pin);
    /// }
    /// </code>
    /// </example>
    public static async Task<PinOperationResult> VerifyPinAsync(
        IPivSession session,
        ReadOnlyMemory<byte> pin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            await session.VerifyPinAsync(pin, cancellationToken);
            return PinOperationResult.Succeeded();
        }
        catch (InvalidPinException ex)
        {
            return PinOperationResult.Failed(
                $"Invalid PIN. {ex.RetriesRemaining} retries remaining.",
                ex.RetriesRemaining);
        }
        catch (Exception ex)
        {
            return PinOperationResult.Failed($"PIN verification failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Changes the PIN.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="oldPin">Current PIN as UTF-8 bytes.</param>
    /// <param name="newPin">New PIN as UTF-8 bytes (6-8 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <example>
    /// <code>
    /// var oldPin = Encoding.UTF8.GetBytes("123456");
    /// var newPin = Encoding.UTF8.GetBytes("654321");
    /// try
    /// {
    ///     var result = await PinManagement.ChangePinAsync(session, oldPin, newPin, ct);
    /// }
    /// finally
    /// {
    ///     CryptographicOperations.ZeroMemory(oldPin);
    ///     CryptographicOperations.ZeroMemory(newPin);
    /// }
    /// </code>
    /// </example>
    public static async Task<PinOperationResult> ChangePinAsync(
        IPivSession session,
        ReadOnlyMemory<byte> oldPin,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            await session.ChangePinAsync(oldPin, newPin, cancellationToken);
            return PinOperationResult.Succeeded();
        }
        catch (InvalidPinException ex)
        {
            return PinOperationResult.Failed(
                $"Invalid current PIN. {ex.RetriesRemaining} retries remaining.",
                ex.RetriesRemaining);
        }
        catch (Exception ex)
        {
            return PinOperationResult.Failed($"PIN change failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Changes the PUK.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="oldPuk">Current PUK as UTF-8 bytes.</param>
    /// <param name="newPuk">New PUK as UTF-8 bytes (6-8 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<PinOperationResult> ChangePukAsync(
        IPivSession session,
        ReadOnlyMemory<byte> oldPuk,
        ReadOnlyMemory<byte> newPuk,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            await session.ChangePukAsync(oldPuk, newPuk, cancellationToken);
            return PinOperationResult.Succeeded();
        }
        catch (InvalidPinException ex)
        {
            return PinOperationResult.Failed(
                $"Invalid current PUK. {ex.RetriesRemaining} retries remaining.",
                ex.RetriesRemaining);
        }
        catch (Exception ex)
        {
            return PinOperationResult.Failed($"PUK change failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Unblocks PIN using PUK.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="puk">PUK as UTF-8 bytes.</param>
    /// <param name="newPin">New PIN as UTF-8 bytes (6-8 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <example>
    /// <code>
    /// var puk = Encoding.UTF8.GetBytes("12345678");
    /// var newPin = Encoding.UTF8.GetBytes("123456");
    /// try
    /// {
    ///     var result = await PinManagement.UnblockPinAsync(session, puk, newPin, ct);
    /// }
    /// finally
    /// {
    ///     CryptographicOperations.ZeroMemory(puk);
    ///     CryptographicOperations.ZeroMemory(newPin);
    /// }
    /// </code>
    /// </example>
    public static async Task<PinOperationResult> UnblockPinAsync(
        IPivSession session,
        ReadOnlyMemory<byte> puk,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            await session.UnblockPinAsync(puk, newPin, cancellationToken);
            return PinOperationResult.Succeeded();
        }
        catch (InvalidPinException ex)
        {
            return PinOperationResult.Failed(
                $"Invalid PUK. {ex.RetriesRemaining} retries remaining.",
                ex.RetriesRemaining);
        }
        catch (Exception ex)
        {
            return PinOperationResult.Failed($"PIN unblock failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the number of PIN retries remaining.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the retry count.</returns>
    public static async Task<PinOperationResult> GetPinRetriesAsync(
        IPivSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var retries = await session.GetPinAttemptsAsync(cancellationToken);
            return PinOperationResult.Succeeded(retries);
        }
        catch (Exception ex)
        {
            return PinOperationResult.Failed($"Failed to get PIN retries: {ex.Message}");
        }
    }

    /// <summary>
    /// Authenticates with the management key.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="managementKey">Management key bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <example>
    /// <code>
    /// var mgmtKey = new byte[] { /* 24 bytes for 3DES, or 16/24/32 for AES */ };
    /// try
    /// {
    ///     var result = await PinManagement.AuthenticateAsync(session, mgmtKey, ct);
    /// }
    /// finally
    /// {
    ///     CryptographicOperations.ZeroMemory(mgmtKey);
    /// }
    /// </code>
    /// </example>
    public static async Task<PinOperationResult> AuthenticateAsync(
        IPivSession session,
        ReadOnlyMemory<byte> managementKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            await session.AuthenticateAsync(managementKey, cancellationToken);
            return PinOperationResult.Succeeded();
        }
        catch (Exception ex)
        {
            return PinOperationResult.Failed($"Authentication failed: {ex.Message}");
        }
    }
}
