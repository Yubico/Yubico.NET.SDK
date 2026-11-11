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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Ctap2.Examples.FidoTool.FidoExamples;

/// <summary>
/// Factory reset of the FIDO application on the authenticator.
/// Invalidates generated credentials (including non-discoverable/U2F), erases discoverable credentials
/// and clears the PIN; named storage/configuration effects depend on the applicable protocol/product.
/// </summary>
/// <remarks>
/// This helper does not power-cycle, verify target continuity, enforce a local reset timer or roll back.
/// CTAP 2.1/2.3 require receipt within ten seconds of power-up for displayless devices; Yubico documents
/// five seconds before firmware 5.5.2 and ten on 5.5.4+, with 5.5.2/5.5.3 unspecified in that passage.
/// Separate transport/product presence requirements apply. Current executable timing/error prompts are
/// overgeneralized and remain deferred; see the sample README's qualified reset procedure. General catches
/// flatten cancellation/error provenance. A failed result is not evidence that no destructive action occurred.
/// </remarks>
public static class ResetAuthenticator
{
    /// <summary>
    /// Result of a reset operation.
    /// </summary>
    public sealed record ResetResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public static ResetResult Succeeded() => new() { Success = true };
        public static ResetResult Failed(string error) => new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Pre-reset information queried from the authenticator to guide user prompts.
    /// </summary>
    public sealed record ResetPreflightInfo
    {
        /// <summary>
        /// The parsed long-touch option, defaulting absent metadata to false.
        /// CTAP 2.3's enabled option requires at least five seconds; false is not proof a short tap suffices.
        /// </summary>
        public bool LongTouchForReset { get; init; }

        /// <summary>
        /// Transports the authenticator allows for reset (e.g. "usb", "nfc").
        /// Empty metadata does not prove that all transports are allowed.
        /// </summary>
        public IReadOnlyList<string> TransportsForReset { get; init; } = [];

        /// <summary>
        /// Returns the sample's current touch message, not a qualified per-product timing contract.
        /// </summary>
        public string TouchMessage => LongTouchForReset
            ? "Press and hold the YubiKey button for 10 seconds to confirm."
            : "Touch the YubiKey to confirm.";
    }

    /// <summary>
    /// Queries the authenticator for reset-related information before performing
    /// the actual reset. Missing fields are defaulted and all failures/cancellation return null;
    /// the result does not validate reset timing, target identity or transport restrictions.
    /// </summary>
    public static async Task<ResetPreflightInfo?> GetPreflightInfoAsync(
        IYubiKey yubiKey,
        CancellationToken cancellationToken) =>
        await GetPreflightInfoAsync(yubiKey, null, cancellationToken).ConfigureAwait(false);

    public static async Task<ResetPreflightInfo?> GetPreflightInfoAsync(
        IYubiKey yubiKey,
        ConnectionType? preferredConnection = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateCtap2SessionAsync(
                new SessionCreationOptions { PreferredConnectionType = preferredConnection },
                cancellationToken: cancellationToken);

            var info = await session.GetInfoAsync(cancellationToken);

            return new ResetPreflightInfo
            {
                LongTouchForReset = info.LongTouchForReset ?? false,
                TransportsForReset = info.TransportsForReset
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Performs a factory reset of the FIDO application.
    /// The YubiKey must have been recently re-inserted before calling this method,
    /// and the user must meet its separate presence/long-touch requirements. Firmware alone is not a
    /// universal ten-second-touch rule, and session initialization consumes the receipt window too.
    /// </summary>
    public static async Task<ResetResult> ResetAsync(
        IYubiKey yubiKey,
        CancellationToken cancellationToken) =>
        await ResetAsync(yubiKey, null, cancellationToken).ConfigureAwait(false);

    public static async Task<ResetResult> ResetAsync(
        IYubiKey yubiKey,
        ConnectionType? preferredConnection = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateCtap2SessionAsync(
                new SessionCreationOptions { PreferredConnectionType = preferredConnection },
                cancellationToken: cancellationToken);

            await session.ResetAsync(cancellationToken);

            return ResetResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return ResetResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return ResetResult.Failed($"Failed to reset authenticator: {ex.Message}");
        }
    }

    private static string MapCtapError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.UserActionTimeout =>
                "Reset timed out. You need to touch your YubiKey to confirm the reset.",
            CtapStatus.NotAllowed =>
                "Reset not allowed. Reset must be triggered within 5 seconds after the YubiKey is inserted.",
            CtapStatus.OperationDenied =>
                "Reset was denied. Remove the YubiKey, re-insert it, and try again within 5 seconds.",
            CtapStatus.PinAuthBlocked =>
                "Reset not allowed. Remove the YubiKey, re-insert it, and try again within 5 seconds.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}