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
    /// Performs a factory reset of the FIDO2 application.
    /// The YubiKey must be re-inserted within 5 seconds before calling this method,
    /// and the user must touch the key when prompted.
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
                "Reset timed out. The YubiKey must be touched within 5 seconds of insertion.",
            CtapStatus.NotAllowed =>
                "Reset not allowed. Remove the YubiKey, re-insert it, and try again within 5 seconds.",
            CtapStatus.OperationDenied =>
                "Reset was denied. Remove the YubiKey, re-insert it, and try again within 5 seconds.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}
