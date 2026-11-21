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

using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.WebAuthn.Client.Authentication;

/// <summary>
/// Internal helper for matching credentials during authentication.
/// </summary>
/// <remarks>
/// Collects final assertions starting with the successful GetAssertion response. CredentialFilter performs
/// allow-list probing before this helper receives the narrowed request.
/// </remarks>
internal sealed class CredentialMatcher
{
    private const uint MaxAssertionsPerCeremony = 1024;

    /// <summary>
    /// Matches credentials for an authentication request.
    /// </summary>
    /// <param name="backend">The WebAuthn backend to use.</param>
    /// <param name="firstResponse">The successful GetAssertion response.</param>
    /// <param name="hasAllowList">Whether the final request included an allow list.</param>
    /// <param name="selected">The real descriptor selected by probing, never the dummy descriptor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of tuples containing credential ID, optional user, and the GetAssertionResponse.
    /// May be empty if no credentials match.
    /// </returns>
    internal static async Task<IReadOnlyList<(ReadOnlyMemory<byte> CredentialId, PublicKeyCredentialUserEntity? User, GetAssertionResponse Response)>>
        MatchAsync(
            IWebAuthnBackend backend,
            GetAssertionResponse firstResponse,
            bool hasAllowList,
            PublicKeyCredentialDescriptor? selected,
            CancellationToken cancellationToken)
    {
        var results = new List<(ReadOnlyMemory<byte>, PublicKeyCredentialUserEntity?, GetAssertionResponse)>();

        uint? numberOfCredentials = CheckAssertionCount(firstResponse);

        try
        {
            var firstCredId = ResolveCredentialId(firstResponse, selected, hasAllowList);
            results.Add((firstCredId, firstResponse.User, firstResponse));

            for (uint i = 1; i < numberOfCredentials; i++)
            {
                var nextResponse = await GetContinuationAsync(backend, cancellationToken).ConfigureAwait(false);
                var nextCredId = ResolveCredentialId(nextResponse, selected, hasAllowList, allowOmission: false);
                results.Add((nextCredId, nextResponse.User, nextResponse));
            }
            return results;
        }
        catch
        {
            foreach (var (_, _, response) in results)
                ClearUnreturnedAssertion(response);
            throw;
        }
    }

    private static async Task<GetAssertionResponse> GetContinuationAsync(IWebAuthnBackend backend, CancellationToken ct)
    {
        try
        {
            return await backend.GetNextAssertionAsync(ct).ConfigureAwait(false);
        }
        catch (CtapException ex)
        {
            throw WebAuthnClient.MapCtapStatusToWebAuthnError(ex);
        }
    }

    private static ReadOnlyMemory<byte> ResolveCredentialId(
        GetAssertionResponse response, PublicKeyCredentialDescriptor? selected, bool hasAllowList, bool allowOmission = true)
    {
        if (response.Credential is null && allowOmission && selected is not null)
            return selected.Id;

        if (response.Credential is { Id.IsEmpty: false } descriptor
            && (!hasAllowList || (selected is not null && descriptor.Id.Span.SequenceEqual(selected.Id.Span))))
            return descriptor.Id;

        ClearUnreturnedAssertion(response);
        throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState,
            "The assertion did not identify a credential matching the request.");
    }

    private static uint? CheckAssertionCount(GetAssertionResponse response)
    {
        uint? count = response.NumberOfCredentials;
        if (count is not > MaxAssertionsPerCeremony)
            return count;

        // These decoded buffers belong to this ceremony and won't be returned to the caller.
        ClearUnreturnedAssertion(response);
        throw new WebAuthnClientError(WebAuthnClientErrorCode.Constraint,
            $"Authenticator reported {count} assertions; the SDK supports at most {MaxAssertionsPerCeremony} per ceremony.");
    }

    private static void ClearUnreturnedAssertion(GetAssertionResponse response)
    {
        CryptographicOperations.ZeroMemory(response.AuthenticatorDataRaw.Span);
        if (response.LargeBlobKey is { } key)
            CryptographicOperations.ZeroMemory(key.Span);
    }

    internal static bool IsNoCredentialsError(CtapStatus status)
    {
        // NotAllowed (0x30) is "device denied the operation" (user cancel, policy reject) —
        // semantically distinct from "no matching credential" and must propagate so callers
        // map it to WebAuthnClientErrorCode.NotAllowed instead of treating as empty match.
        return status == CtapStatus.NoCredentials
            || status == CtapStatus.InvalidCredential;
    }
}