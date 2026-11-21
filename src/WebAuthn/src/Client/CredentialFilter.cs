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
using Yubico.YubiKit.WebAuthn.Client.Authentication;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Finds which of a list of credential descriptors the authenticator holds, without a touch.
/// </summary>
/// <remarks>
/// <para>
/// Used for the registration exclude-list pre-flight and for allow-list selection before an assertion,
/// so that both ceremonies make the same decision from the same probe.
/// </para>
/// <para>
/// Descriptors are probed with <c>authenticatorGetAssertion</c>, <c>up: false</c> and a zero client
/// data hash, in chunks of <c>maxCredentialCountInList</c> (CTAP 2.1-2.3 §6.4). A chunk the
/// authenticator rejects with CTAP2_ERR_REQUEST_TOO_LARGE is retried smaller; CTAP2_ERR_NO_CREDENTIALS
/// moves to the next chunk; any other status propagates. Descriptors longer than
/// <c>maxCredentialIdLength</c> are skipped: the authenticator cannot have issued them.
/// </para>
/// </remarks>
internal static class CredentialFilter
{
    private const int ClientDataHashLength = 32;

    /// <summary>
    /// Returns the first descriptor in <paramref name="candidates"/> that the authenticator
    /// recognises, or null when no eligible credentials were sent or all eligible probes missed.
    /// A successful response that does not identify a credential in its probe chunk is an error.
    /// </summary>
    /// <param name="backend">The WebAuthn backend for CTAP commands.</param>
    /// <param name="rpId">The relying party identifier.</param>
    /// <param name="candidates">The descriptors to probe, in the relying party's order.</param>
    /// <param name="info">The authenticator info (<c>maxCredentialCountInList</c>, <c>maxCredentialIdLength</c>).</param>
    /// <param name="token">
    /// The PIN/UV auth token for the ceremony, or null when the ceremony has none; the probe then
    /// carries no <c>pinUvAuthParam</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<PublicKeyCredentialDescriptor?> SelectAsync(
        IWebAuthnBackend backend,
        string rpId,
        IReadOnlyList<PublicKeyCredentialDescriptor> candidates,
        AuthenticatorInfo info,
        PinUvAuthTokenSession? token,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(rpId);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(info);

        var eligible = Eligible(candidates, info);
        if (eligible.Count == 0)
            return null;

        byte[] zeroClientDataHash = new byte[ClientDataHashLength];
        byte[]? pinUvAuthParam = token is null ? null : token.Protocol.Authenticate(token.Token.Span, zeroClientDataHash);

        try
        {
            var result = await FindAsync(eligible, info, requireUvAndUp: false, chunk =>
                backend.GetAssertionAsync(new BackendGetAssertionRequest
                {
                    ClientDataHash = zeroClientDataHash,
                    RpId = rpId,
                    AllowList = chunk,
                    Options = new Dictionary<string, bool> { ["up"] = false },
                    PinUvAuthParam = pinUvAuthParam is null ? null : (ReadOnlyMemory<byte>?)pinUvAuthParam,
                    PinUvAuthProtocol = token is null ? null : (byte)token.Protocol.Version
                }, cancellationToken), cancellationToken).ConfigureAwait(false);
            if (result is { } match)
            {
                CredentialMatcher.ClearUnreturnedAssertion(match.Response);
                return match.Selected;
            }

            return null;
        }
        finally
        {
            if (pinUvAuthParam is not null)
            {
                CryptographicOperations.ZeroMemory(pinUvAuthParam);
            }
        }
    }

    // CTAP §6.4: IDs larger than the authenticator can issue cannot match.
    internal static IReadOnlyList<PublicKeyCredentialDescriptor> Eligible(
        IReadOnlyList<PublicKeyCredentialDescriptor> candidates, AuthenticatorInfo info) =>
        info.MaxCredentialIdLength is { } max
            ? candidates.Where(descriptor => descriptor.Id.Length <= max).ToList()
            : candidates;

    internal static int MaxListCount(AuthenticatorInfo info)
    {
        // CTAP §6.4 requires a positive count if present. Never send an empty chunk by clamping it.
        int maxCreds = info.MaxCredentialCountInList ?? 1;
        return maxCreds > 0 ? maxCreds : throw new WebAuthnClientError(WebAuthnClientErrorCode.NotSupported,
            $"Authenticator reported maxCredentialCountInList = {maxCreds}; CTAP §6.4 requires a value greater than zero, so the credential list cannot be checked.");
    }

    /// <summary>Checks eligible chunks in order and transfers ownership of only an associated success.</summary>
    internal static async Task<(PublicKeyCredentialDescriptor Selected, GetAssertionResponse Response)?> FindAsync(
        IReadOnlyList<PublicKeyCredentialDescriptor> eligible, AuthenticatorInfo info, bool requireUvAndUp,
        Func<IReadOnlyList<PublicKeyCredentialDescriptor>, Task<GetAssertionResponse>> getAssertion, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (eligible.Count == 0)
            return null;

        int limit = MaxListCount(info);
        int offset = 0;
        while (offset < eligible.Count)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = eligible.Skip(offset).Take(limit).ToList();
            GetAssertionResponse response;
            try
            {
                response = await getAssertion(chunk).ConfigureAwait(false);
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
            {
                offset += chunk.Count;
                continue;
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.RequestTooLarge && chunk.Count > 1)
            {
                limit = chunk.Count - 1; // Retry from the same offset without losing any candidates.
                continue;
            }

            return (Associate(response, chunk, requireUvAndUp, ct), response);
        }

        ct.ThrowIfCancellationRequested();
        return null;
    }

    internal static PublicKeyCredentialDescriptor Associate(GetAssertionResponse response,
        IReadOnlyList<PublicKeyCredentialDescriptor> chunk, bool requireUvAndUp, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            if (requireUvAndUp && (!response.AuthenticatorData.UserVerified || !response.AuthenticatorData.UserPresent))
                throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState,
                    "The authenticator did not return the requested user verification and presence.");

            // CTAP 2.0 §5.2 selects one credential; 2.1–2.3 §6.2.2 omit the count.
            if (response.NumberOfCredentials is > 1)
                throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState,
                    "The authenticator reported multiple assertions for a nonempty allow list.");

            // Only a wire singleton allows omission repair (CTAP 2.0 §5.2).
            if (response.Credential is null && chunk.Count == 1)
                return chunk[0];

            var id = response.GetCredentialId();
            if (!id.IsEmpty && chunk.FirstOrDefault(descriptor => descriptor.Id.Span.SequenceEqual(id.Span)) is { } matched)
                return matched;

            throw new WebAuthnClientError(WebAuthnClientErrorCode.InvalidState,
                "The successful assertion did not identify a credential in the submitted list.");
        }
        catch
        {
            CredentialMatcher.ClearUnreturnedAssertion(response);
            throw;
        }
    }
}