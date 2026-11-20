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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Ctap2.Pin;

namespace Yubico.YubiKit.Ctap2.Examples.FidoTool.FidoExamples;

/// <summary>
/// PIN management operations: set, change, and query retry counts.
/// </summary>
/// <remarks>
/// These examples select protocol 2 without negotiation and borrow caller PIN bytes; keep them valid
/// through the await, then clear them. Local 4–63-byte validation is not complete UTF-8/NFC/code-point/
/// device-policy validation. General catches turn cancellation/transport/decode failures into failed
/// results; failure does not establish unchanged PIN state or safe retry. Temporary PinAuthBlocked
/// differs from exhausted PinBlocked. Returned token finally-zeroing is not end-to-end request cleanup.
/// </remarks>
public static class PinManagement
{
    /// <summary>
    /// Result of a PIN operation.
    /// </summary>
    public sealed record PinResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public static PinResult Succeeded() => new() { Success = true };
        public static PinResult Failed(string error) => new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of a PIN retries query.
    /// </summary>
    public sealed record PinRetriesResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public int Retries { get; init; }
        public bool PowerCycleRequired { get; init; }

        public static PinRetriesResult Succeeded(int retries, bool powerCycleRequired) =>
            new() { Success = true, Retries = retries, PowerCycleRequired = powerCycleRequired };

        public static PinRetriesResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Sets the initial PIN on the authenticator.
    /// </summary>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="newPin">The PIN as UTF-8 bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    public static async Task<PinResult> SetPinAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateCtap2SessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            await clientPin.SetPinAsync(newPin, cancellationToken);

            return PinResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return PinResult.Failed(MapCtapPinError(ex));
        }
        catch (ArgumentException ex)
        {
            return PinResult.Failed(ex.Message);
        }
        catch (Exception ex)
        {
            return PinResult.Failed($"Failed to set PIN: {ex.Message}");
        }
    }

    /// <summary>
    /// Changes the existing PIN on the authenticator.
    /// </summary>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="currentPin">The current PIN as UTF-8 bytes.</param>
    /// <param name="newPin">The new PIN as UTF-8 bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    public static async Task<PinResult> ChangePinAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> currentPin,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateCtap2SessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            await clientPin.ChangePinAsync(currentPin, newPin, cancellationToken);

            return PinResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return PinResult.Failed(MapCtapPinError(ex));
        }
        catch (ArgumentException ex)
        {
            return PinResult.Failed(ex.Message);
        }
        catch (Exception ex)
        {
            return PinResult.Failed($"Failed to change PIN: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies the PIN by attempting to obtain a PIN token.
    /// </summary>
    /// <remarks>
    /// The current permission-token request asks for GetAssertion without its required RP ID; permission-
    /// capable devices can reject it for request scope, not a wrong PIN. Legacy fallback drops permissions.
    /// Executable request correction is deferred. This method does not perform an RP assertion/verification.
    /// </remarks>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="pin">The PIN as UTF-8 bytes to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    public static async Task<PinResult> VerifyPinAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pin,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateCtap2SessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin,
                PinUvAuthTokenPermissions.GetAssertion,
                cancellationToken: cancellationToken);

            return PinResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return PinResult.Failed(MapCtapPinError(ex));
        }
        catch (ArgumentException ex)
        {
            return PinResult.Failed(ex.Message);
        }
        catch (Exception ex)
        {
            return PinResult.Failed($"Failed to verify PIN: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Gets the number of PIN retries remaining.
    /// </summary>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PIN retries result.</returns>
    public static async Task<PinRetriesResult> GetPinRetriesAsync(
        IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateCtap2SessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            var status = await clientPin.GetPinRetriesAsync(cancellationToken);

            return PinRetriesResult.Succeeded(status.RetriesRemaining, status.PowerCycleRequired);
        }
        catch (CtapException ex)
        {
            return PinRetriesResult.Failed($"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})");
        }
        catch (Exception ex)
        {
            return PinRetriesResult.Failed($"Failed to get PIN retries: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the number of UV (user verification) retries remaining.
    /// </summary>
    /// <remarks>
    /// The result copies the parser's defaulted count/boolean. CTAP 2.1 provides no UV power-cycle field;
    /// false does not prove a cycle is unnecessary, and missing counts are not known zero-retry evidence.
    /// </remarks>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UV retries result.</returns>
    public static async Task<PinRetriesResult> GetUvRetriesAsync(
        IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateCtap2SessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            var status = await clientPin.GetUvRetriesAsync(cancellationToken);

            return PinRetriesResult.Succeeded(status.RetriesRemaining, status.PowerCycleRequired);
        }
        catch (CtapException ex)
        {
            return PinRetriesResult.Failed($"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})");
        }
        catch (Exception ex)
        {
            return PinRetriesResult.Failed($"Failed to get UV retries: {ex.Message}");
        }
    }

    private static string MapCtapPinError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinAuthBlocked =>
                "PIN authentication is blocked. Remove and re-insert the YubiKey.",
            CtapStatus.PinNotSet => "No PIN is currently set on this authenticator.",
            CtapStatus.PinPolicyViolation => "The PIN does not meet the authenticator's policy requirements.",
            CtapStatus.NotAllowed =>
                "Operation not allowed. A PIN may already be set (use 'change' instead of 'set').",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}