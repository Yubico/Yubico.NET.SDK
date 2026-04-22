// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.WebAuthn.Client.Authentication;

/// <summary>
/// Internal helper for matching credentials during authentication.
/// </summary>
/// <remarks>
/// Handles both allow-list probing and discoverable credential enumeration
/// via GetAssertion + GetNextAssertion.
/// </remarks>
internal sealed class CredentialMatcher
{
    /// <summary>
    /// Matches credentials for an authentication request.
    /// </summary>
    /// <param name="backend">The WebAuthn backend to use.</param>
    /// <param name="request">The backend GetAssertion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of tuples containing credential ID, optional user, and the GetAssertionResponse.
    /// May be empty if no credentials match.
    /// </returns>
    internal static async Task<IReadOnlyList<(ReadOnlyMemory<byte> CredentialId, WebAuthnUser? User, GetAssertionResponse Response)>>
        MatchAsync(
            IWebAuthnBackend backend,
            BackendGetAssertionRequest request,
            CancellationToken cancellationToken)
    {
        GetAssertionResponse firstResponse;

        try
        {
            firstResponse = await backend.GetAssertionAsync(request, progress: null, cancellationToken);
        }
        catch (CtapException ex) when (IsNoCredentialsError(ex.Status))
        {
            // Authenticator has no matching credentials - return empty list
            return Array.Empty<(ReadOnlyMemory<byte>, WebAuthnUser?, GetAssertionResponse)>();
        }

        var results = new List<(ReadOnlyMemory<byte>, WebAuthnUser?, GetAssertionResponse)>();

        // Add the first response
        var firstCredId = firstResponse.Credential?.Id ?? ReadOnlyMemory<byte>.Empty;
        results.Add((firstCredId, MapUser(firstResponse.User), firstResponse));

        // Check if there are more credentials to enumerate
        int? numberOfCredentials = firstResponse.NumberOfCredentials;
        if (numberOfCredentials.HasValue && numberOfCredentials.Value > 1)
        {
            // Enumerate remaining credentials via GetNextAssertion
            int remaining = numberOfCredentials.Value - 1;

            for (int i = 0; i < remaining; i++)
            {
                var nextResponse = await backend.GetNextAssertionAsync(cancellationToken);
                var nextCredId = nextResponse.Credential?.Id ?? ReadOnlyMemory<byte>.Empty;
                results.Add((nextCredId, MapUser(nextResponse.User), nextResponse));
            }
        }

        return results;
    }

    private static bool IsNoCredentialsError(CtapStatus status)
    {
        return status == CtapStatus.NoCredentials
            || status == CtapStatus.InvalidCredential
            || status == CtapStatus.NotAllowed;
    }

    private static WebAuthnUser? MapUser(PublicKeyCredentialUserEntity? fidoUser)
    {
        if (fidoUser is null)
            return null;

        return new WebAuthnUser
        {
            Id = fidoUser.Id,
            Name = fidoUser.Name ?? string.Empty,
            DisplayName = fidoUser.DisplayName ?? fidoUser.Name ?? string.Empty
        };
    }
}
