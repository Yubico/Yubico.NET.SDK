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
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;

namespace Yubico.YubiKit.Ctap2.Examples.FidoTool.FidoExamples;

/// <summary>
/// Creates a discoverable CTAP2 credential on the authenticator.
/// </summary>
/// <remarks>
/// This is a persistent protocol demo, not RP account registration: it fabricates the hash and user
/// handle. Production ceremonies need fresh exact client-data bytes, RP options and a stable RP account
/// handle, followed by independent RP verification. The PIN branch currently combines uv with token auth,
/// which CTAP forbids; executable correction is pending. Protocol 2 is selected without negotiation.
/// General exception handling converts cancellation/transport/decode failures into failed results;
/// failure does not establish that the authenticator created nothing. PIN input is borrowed, and token
/// finally-zeroing does not clear all auth-param/request/response copies.
/// </remarks>
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
    /// <param name="pin">Optional PIN as UTF-8 bytes for user verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result containing the credential response.</returns>
    public static async Task<MakeCredentialResult> CreateAsync(
        IYubiKey yubiKey,
        string rpId,
        string userName,
        string? displayName,
        ReadOnlyMemory<byte>? pin = null,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            // Stand-in hash, not SHA-256 of any collected client data; not an RP-verifiable ceremony.
            var clientDataHash = new byte[32];
            RandomNumberGenerator.Fill(clientDataHash);

            // Demo account handle; production uses the RP's stable handle, not a new random account each call.
            var userId = new byte[32];
            RandomNumberGenerator.Fill(userId);

            await using var session = await yubiKey.CreateCtap2SessionAsync(
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
                using var protocol = new PinUvAuthProtocolV2();
                using var clientPin = new ClientPin(session, protocol);

                pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    pin.Value,
                    PinUvAuthTokenPermissions.MakeCredential,
                    rpId,
                    cancellationToken);

                var authParam = protocol.Authenticate(pinToken, clientDataHash);
                options.WithPinUvAuth(authParam, protocol.Version);
                // Known sample defect: uv must be omitted beside pinUvAuthParam (even uv:false).
                // Preserve this executable call for the separately scoped sample fix.
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