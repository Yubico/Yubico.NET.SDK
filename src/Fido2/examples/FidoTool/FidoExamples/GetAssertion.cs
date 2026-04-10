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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

/// <summary>
/// Authenticates using an existing FIDO2 credential on the authenticator.
/// </summary>
public static class GetAssertion
{
    /// <summary>
    /// Result of a GetAssertion operation.
    /// </summary>
    public sealed record GetAssertionResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public GetAssertionResponse? Assertion { get; init; }
        public int TotalCredentials { get; init; }

        public static GetAssertionResult Succeeded(
            GetAssertionResponse assertion,
            int totalCredentials) =>
            new()
            {
                Success = true,
                Assertion = assertion,
                TotalCredentials = totalCredentials
            };

        public static GetAssertionResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Gets an assertion for the specified relying party using a discoverable credential.
    /// </summary>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="rpId">The relying party identifier (e.g., "example.com").</param>
    /// <param name="pinUtf8">The PIN as UTF-8 bytes for user verification, or null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result containing the assertion response.</returns>
    public static async Task<GetAssertionResult> AssertAsync(
        IYubiKey yubiKey,
        string rpId,
        ReadOnlyMemory<byte>? pinUtf8 = null,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            // Generate random clientDataHash (32 bytes) for demo/testing purposes
            var clientDataHash = new byte[32];
            RandomNumberGenerator.Fill(clientDataHash);

            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            var options = new GetAssertionOptions();

            // If PIN is provided, get a PIN token for user verification
            if (pinUtf8 is not null)
            {
                using var protocol = new PinUvAuthProtocolV2();
                using var clientPin = new ClientPin(session, protocol);

                pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    pinUtf8.Value,
                    PinUvAuthTokenPermissions.GetAssertion,
                    rpId,
                    cancellationToken);

                var authParam = protocol.Authenticate(pinToken, clientDataHash);
                options.WithPinUvAuth(authParam, protocol.Version);
                options.WithUserVerification(true);
            }

            var response = await session.GetAssertionAsync(
                rpId,
                clientDataHash,
                options,
                cancellationToken);

            var totalCredentials = response.NumberOfCredentials ?? 1;

            return GetAssertionResult.Succeeded(response, totalCredentials);
        }
        catch (CtapException ex)
        {
            return GetAssertionResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return GetAssertionResult.Failed($"Failed to get assertion: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    private static string MapCtapError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.NoCredentials =>
                "No credentials found for this relying party on the authenticator.",
            CtapStatus.UserActionTimeout =>
                "Operation timed out. Please try again and touch your YubiKey when prompted.",
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinNotSet => "No PIN is set. Set a PIN first.",
            CtapStatus.NotAllowed => "Operation not allowed in the current state.",
            CtapStatus.OperationDenied => "The operation was denied by the authenticator.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}
