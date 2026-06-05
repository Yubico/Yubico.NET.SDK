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

using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.Internal;

/// <summary>
/// Pre-flight exclude list filtering to avoid authenticator processing limits.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors yubikit-android's Ctap2Client.filterCreds algorithm (Ctap2Client.java:860-938).
/// When an excludeList exceeds the authenticator's MaxCredentialCountInList, the list
/// must be chunked and probed via GetAssertion(up=false) to identify the first matching
/// credential (if any). Only that matched credential (or an empty list if no match) is
/// then sent to MakeCredential, avoiding firmware processing limits.
/// </para>
/// <para>
/// This is the client-layer orchestration that sits between WebAuthn API and raw CTAP.
/// The Fido2 layer stays CTAP-direct and does not implement this pre-flight logic.
/// </para>
/// </remarks>
internal static class ExcludeListPreflight
{
    /// <summary>
    /// Finds the first credential in excludeCredentials that exists on the authenticator.
    /// </summary>
    /// <param name="backend">The WebAuthn backend for CTAP commands.</param>
    /// <param name="rpId">The relying party identifier.</param>
    /// <param name="excludeCredentials">The full exclude list to probe.</param>
    /// <param name="info">The authenticator info (for MaxCredentialCountInList).</param>
    /// <param name="pinUvAuthToken">The PIN/UV auth token (must have GetAssertion permission).</param>
    /// <param name="protocol">The PIN/UV auth protocol used to acquire the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The first matching <see cref="PublicKeyCredentialDescriptor"/> from the exclude list,
    /// or null if no credentials match (or if excludeCredentials is empty).
    /// </returns>
    /// <remarks>
    /// <para>
    /// Algorithm (from yubikit-android Ctap2Client.java:860-938):
    /// 1. If excludeCredentials is empty, return null immediately (short-circuit).
    /// 2. Determine chunk size from info.MaxCredentialCountInList (default 1 if null).
    /// 3. For each chunk of up to maxCreds descriptors:
    ///    - Invoke GetAssertion(rpId, dummyClientDataHash, chunk, up=false) with pinUvAuthParam.
    ///    - If NoCredentials: continue to next chunk.
    ///    - If success: return the matched credential from chunk (identified by response.Credential.Id).
    ///    - Other errors: propagate.
    /// 4. If no chunk matched, return null.
    /// </para>
    /// <para>
    /// The dummyClientDataHash is a 32-byte zero array (Java line 883-884).
    /// The pinUvAuthParam is computed as protocol.Authenticate(pinUvAuthToken, dummyClientDataHash) (line 889).
    /// </para>
    /// </remarks>
    public static async Task<PublicKeyCredentialDescriptor?> FindFirstMatchAsync(
        IWebAuthnBackend backend,
        string rpId,
        IReadOnlyList<PublicKeyCredentialDescriptor> excludeCredentials,
        AuthenticatorInfo info,
        ReadOnlyMemory<byte> pinUvAuthToken,
        IPinUvAuthProtocol protocol,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(rpId);
        ArgumentNullException.ThrowIfNull(excludeCredentials);
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(protocol);

        // Short-circuit if no excludeList
        if (excludeCredentials.Count == 0)
        {
            return null;
        }

        // MaxCredentialCountInList defaults to 1 if not present (Java line 880-881)
        int maxCreds = info.MaxCredentialCountInList ?? 1;

        // Dummy client data hash: 32 zero bytes (Java line 883-884)
        byte[] dummyClientDataHash = new byte[32];

        // Compute pinUvAuthParam for GetAssertion (Java line 889)
        byte[] pinUvAuthParam = protocol.Authenticate(pinUvAuthToken.Span, dummyClientDataHash);

        // Chunk the list and probe each chunk
        int offset = 0;
        while (offset < excludeCredentials.Count)
        {
            int chunkSize = Math.Min(maxCreds, excludeCredentials.Count - offset);
            var chunk = excludeCredentials.Skip(offset).Take(chunkSize).ToList();

            try
            {
                // Build GetAssertion request with up=false (Java line 899-907)
                var request = new BackendGetAssertionRequest
                {
                    ClientDataHash = dummyClientDataHash,
                    RpId = rpId,
                    AllowList = chunk,
                    Options = new Dictionary<string, bool> { ["up"] = false },
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = (byte)protocol.Version
                };

                var response = await backend.GetAssertionAsync(request, progress: null, cancellationToken);

                // Match found - return the credential that matched
                // Java lines 909-916: if chunk.size == 1, return chunk[0]; else extract from response.credentialId
                if (chunk.Count == 1)
                {
                    return chunk[0];
                }

                // Multiple creds in chunk - identify which one matched from response
                var matchedId = response.GetCredentialId();
                return chunk.FirstOrDefault(desc => desc.Id.Span.SequenceEqual(matchedId.Span));
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
            {
                // No match in this chunk - continue to next (Java line 920-923)
                offset += chunkSize;
            }
            // Other CtapExceptions propagate (Java line 933 "throw ctapException")
        }

        // No match found in any chunk
        return null;
    }
}
