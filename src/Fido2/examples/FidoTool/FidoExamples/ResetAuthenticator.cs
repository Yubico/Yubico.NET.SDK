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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

/// <summary>
/// Factory reset of the FIDO2 application on the authenticator.
/// This permanently deletes ALL stored credentials, PINs, and settings.
/// </summary>
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
        /// Whether the authenticator requires a long (10-second) touch for reset.
        /// When false, a short tap is sufficient.
        /// </summary>
        public bool LongTouchForReset { get; init; }

        /// <summary>
        /// Transports the authenticator allows for reset (e.g. "usb", "nfc").
        /// Empty means all transports are allowed.
        /// </summary>
        public IReadOnlyList<string> TransportsForReset { get; init; } = [];

        /// <summary>
        /// Returns the appropriate touch prompt message for the user.
        /// </summary>
        public string TouchMessage => LongTouchForReset
            ? "Press and hold the YubiKey button for 10 seconds to confirm."
            : "Touch the YubiKey to confirm.";
    }

    /// <summary>
    /// Queries the authenticator for reset-related information before performing
    /// the actual reset. Use this to display the correct touch message and validate
    /// transport restrictions.
    /// </summary>
    public static async Task<ResetPreflightInfo?> GetPreflightInfoAsync(
        IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
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
    /// Performs a factory reset of the FIDO2 application.
    /// The YubiKey must have been recently re-inserted before calling this method,
    /// and the user must touch (or hold for 10 seconds on newer firmware) when prompted.
    /// </summary>
    public static async Task<ResetResult> ResetAsync(
        IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
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
