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
using System.Text;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

/// <summary>
/// PIN management operations: set, change, and query retry counts.
/// </summary>
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
    /// <param name="newPin">The PIN to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    public static async Task<PinResult> SetPinAsync(
        IYubiKey yubiKey,
        string newPin,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinBytes = null;
        try
        {
            pinBytes = Encoding.UTF8.GetBytes(newPin);

            await using var session = await yubiKey.CreateFidoSessionAsync(
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
        finally
        {
            if (pinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(pinBytes);
            }
        }
    }

    /// <summary>
    /// Changes the existing PIN on the authenticator.
    /// </summary>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="currentPin">The current PIN.</param>
    /// <param name="newPin">The new PIN to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    public static async Task<PinResult> ChangePinAsync(
        IYubiKey yubiKey,
        string currentPin,
        string newPin,
        CancellationToken cancellationToken = default)
    {
        byte[]? currentPinBytes = null;
        byte[]? newPinBytes = null;
        try
        {
            currentPinBytes = Encoding.UTF8.GetBytes(currentPin);
            newPinBytes = Encoding.UTF8.GetBytes(newPin);

            await using var session = await yubiKey.CreateFidoSessionAsync(
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
        finally
        {
            if (currentPinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(currentPinBytes);
            }

            if (newPinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(newPinBytes);
            }
        }
    }

    /// <summary>
    /// Verifies the PIN by attempting to obtain a PIN token.
    /// </summary>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="pin">The PIN to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    public static async Task<PinResult> VerifyPinAsync(
        IYubiKey yubiKey,
        string pin,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinBytes = null;
        byte[]? pinToken = null;
        try
        {
            pinBytes = Encoding.UTF8.GetBytes(pin);

            await using var session = await yubiKey.CreateFidoSessionAsync(
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
            if (pinBytes is not null)
            {
                CryptographicOperations.ZeroMemory(pinBytes);
            }

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
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            var (retries, powerCycleRequired) = await clientPin.GetPinRetriesAsync(cancellationToken);

            return PinRetriesResult.Succeeded(retries, powerCycleRequired);
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
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UV retries result.</returns>
    public static async Task<PinRetriesResult> GetUvRetriesAsync(
        IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            var (retries, powerCycleRequired) = await clientPin.GetUvRetriesAsync(cancellationToken);

            return PinRetriesResult.Succeeded(retries, powerCycleRequired);
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
