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
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Finds which of a list of credential descriptors the authenticator holds, without a touch.
/// </summary>
/// <remarks>
/// <para>
/// The one implementation of python-fido2's <c>_filter_creds</c> and yubikit-android's
/// <c>Utils.filterCreds</c>, used for the registration exclude-list pre-flight and for allow-list
/// selection before an assertion (see docs/architecture/webauthn-extension-processing.md).
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
    /// recognises, or null when it recognises none.
    /// </summary>
    /// <param name="backend">The WebAuthn backend for CTAP commands.</param>
    /// <param name="rpId">The relying party identifier.</param>
    /// <param name="candidates">The descriptors to probe, in the relying party's order.</param>
    /// <param name="info">The authenticator info (<c>maxCredentialCountInList</c>, <c>maxCredentialIdLength</c>).</param>
    /// <param name="token">
    /// The PIN/UV auth token for the ceremony, or null when the ceremony has none; the probe then
    /// carries no <c>pinUvAuthParam</c>, as in python-fido2.
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

        if (candidates.Count == 0)
        {
            return null;
        }

        // CTAP 2.1-2.3 §6.4 maxCredentialIdLength: an id longer than the authenticator can produce
        // cannot match, and sending it risks CTAP2_ERR_REQUEST_TOO_LARGE (python-fido2 _filter_creds).
        IReadOnlyList<PublicKeyCredentialDescriptor> remaining = info.MaxCredentialIdLength is { } maxIdLength
            ? candidates.Where(descriptor => descriptor.Id.Length <= maxIdLength).ToList()
            : candidates;
        if (remaining.Count == 0)
        {
            return null;
        }

        // CTAP 2.1-2.3 §6.4: maxCredentialCountInList "MUST be greater than zero if present." A
        // non-positive value would make the chunk empty: the loop would never advance, or an empty
        // allowList probe could succeed against a discoverable credential. Refuse it instead of
        // clamping so faulty firmware stays visible (maintainer decision, 25g).
        int maxCreds = info.MaxCredentialCountInList ?? 1;
        if (maxCreds <= 0)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                $"Authenticator reported maxCredentialCountInList = {maxCreds}; CTAP §6.4 requires a value greater than zero, so the credential list cannot be checked.");
        }

        byte[] zeroClientDataHash = new byte[ClientDataHashLength];
        byte[]? pinUvAuthParam = token is null ? null : token.Protocol.Authenticate(token.Token.Span, zeroClientDataHash);

        try
        {
            int offset = 0;
            while (offset < remaining.Count)
            {
                int chunkSize = Math.Min(maxCreds, remaining.Count - offset);
                var chunk = remaining.Skip(offset).Take(chunkSize).ToList();

                GetAssertionResponse response;
                try
                {
                    response = await backend.GetAssertionAsync(new BackendGetAssertionRequest
                    {
                        ClientDataHash = zeroClientDataHash,
                        RpId = rpId,
                        AllowList = chunk,
                        Options = new Dictionary<string, bool> { ["up"] = false },
                        PinUvAuthParam = pinUvAuthParam is null ? null : (ReadOnlyMemory<byte>?)pinUvAuthParam,
                        PinUvAuthProtocol = token is null ? null : (byte)token.Protocol.Version
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
                {
                    offset += chunkSize;
                    continue;
                }
                catch (CtapException ex) when (ex.Status == CtapStatus.RequestTooLarge && maxCreds > 1)
                {
                    // The message was too large for this authenticator; try smaller chunks at the same offset.
                    maxCreds--;
                    continue;
                }

                // A one-descriptor chunk is its own answer: the authenticator may omit the credential
                // from the response when the allowList had a single entry (CTAP §6.2.2).
                if (chunk.Count == 1)
                {
                    return chunk[0];
                }

                var matchedId = response.GetCredentialId();
                return chunk.FirstOrDefault(descriptor => descriptor.Id.Span.SequenceEqual(matchedId.Span));
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
}