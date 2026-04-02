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
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

/// <summary>
/// Creates a discoverable FIDO2 credential on the authenticator.
/// </summary>
public static class MakeCredential
{
    /// <summary>
    /// Result of a MakeCredential operation.
    /// </summary>
    public sealed record MakeCredentialResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public MakeCredentialResponse? Credential { get; init; }
        public ReadOnlyMemory<byte> ClientDataHash { get; init; }

        public static MakeCredentialResult Succeeded(
            MakeCredentialResponse credential,
            ReadOnlyMemory<byte> clientDataHash) =>
            new()
            {
                Success = true,
                Credential = credential,
                ClientDataHash = clientDataHash
            };

        public static MakeCredentialResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Creates a discoverable credential with sensible defaults (ES256, rk=true).
    /// </summary>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="rpId">The relying party identifier (e.g., "example.com").</param>
    /// <param name="userName">The user name (e.g., "user@example.com").</param>
    /// <param name="displayName">The user display name.</param>
    /// <param name="pin">Optional PIN for user verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result containing the credential response.</returns>
    public static async Task<MakeCredentialResult> CreateAsync(
        IYubiKey yubiKey,
        string rpId,
        string userName,
        string? displayName,
        string? pin = null,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinBytes = null;
        byte[]? pinToken = null;
        try
        {
            // Generate random clientDataHash (32 bytes) for demo/testing purposes
            var clientDataHash = new byte[32];
            RandomNumberGenerator.Fill(clientDataHash);

            // Generate random user ID (32 bytes)
            var userId = new byte[32];
            RandomNumberGenerator.Fill(userId);

            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            var rp = new PublicKeyCredentialRpEntity(rpId, rpId);
            var user = new PublicKeyCredentialUserEntity(
                userId,
                userName,
                displayName ?? userName);

            List<PublicKeyCredentialParameters> pubKeyCredParams =
            [
                PublicKeyCredentialParameters.CreateES256()
            ];

            var options = new MakeCredentialOptions
            {
                ResidentKey = true
            };

            // If PIN is provided, get a PIN token for user verification
            if (pin is not null)
            {
                pinBytes = Encoding.UTF8.GetBytes(pin);

                using var protocol = new PinUvAuthProtocolV2();
                using var clientPin = new ClientPin(session, protocol);

                pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    pin,
                    PinUvAuthTokenPermissions.MakeCredential,
                    rpId,
                    cancellationToken);

                var authParam = protocol.Authenticate(pinToken, clientDataHash);
                options.WithPinUvAuth(authParam, protocol.Version);
                options.WithUserVerification(true);
            }

            var response = await session.MakeCredentialAsync(
                clientDataHash,
                rp,
                user,
                pubKeyCredParams,
                options,
                cancellationToken);

            return MakeCredentialResult.Succeeded(response, clientDataHash);
        }
        catch (CtapException ex)
        {
            return MakeCredentialResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return MakeCredentialResult.Failed($"Failed to create credential: {ex.Message}");
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

    private static string MapCtapError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.CredentialExcluded =>
                "A credential for this relying party already exists on the authenticator.",
            CtapStatus.KeyStoreFull =>
                "The authenticator's credential storage is full. Delete unused credentials first.",
            CtapStatus.UserActionTimeout =>
                "Operation timed out. Please try again and touch your YubiKey when prompted.",
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinNotSet =>
                "No PIN is set. A PIN is required for discoverable credentials. Set a PIN first.",
            CtapStatus.NotAllowed => "Operation not allowed in the current state.",
            CtapStatus.OperationDenied => "The operation was denied by the authenticator.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}
