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
/// Queries authenticator capabilities via the CTAP2 authenticatorGetInfo command.
/// </summary>
public static class GetAuthenticatorInfo
{
    /// <summary>
    /// Result of an authenticator info query.
    /// </summary>
    public sealed record AuthenticatorInfoResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public AuthenticatorInfo? Info { get; init; }

        public static AuthenticatorInfoResult Succeeded(AuthenticatorInfo info) =>
            new() { Success = true, Info = info };

        public static AuthenticatorInfoResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Queries the authenticator for its capabilities and supported features.
    /// </summary>
    /// <param name="yubiKey">The YubiKey device to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticator info result.</returns>
    public static async Task<AuthenticatorInfoResult> QueryAsync(
        IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            var info = await session.GetInfoAsync(cancellationToken);

            return AuthenticatorInfoResult.Succeeded(info);
        }
        catch (CtapException ex)
        {
            return AuthenticatorInfoResult.Failed($"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})");
        }
        catch (Exception ex)
        {
            return AuthenticatorInfoResult.Failed($"Failed to query authenticator: {ex.Message}");
        }
    }
}
