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

namespace Yubico.YubiKit.WebAuthn.Extensions.Inputs;

/// <summary>
/// The WebAuthn largeBlob client extension input (WebAuthn L3 §10.1.5
/// <c>AuthenticationExtensionsLargeBlobInputs</c>): <c>support</c> at registration, <c>read</c> or
/// <c>write</c> at authentication.
/// </summary>
/// <remarks>
/// <para>
/// The SDK supports CTAP's <c>largeBlobKey</c> extension (§12.3) with authenticatorLargeBlobs (§6.10),
/// not the alternative direct <c>largeBlob</c> extension (§12.4 in CTAP 2.2–2.3). This supported route
/// requires both advertised <c>largeBlobKey</c> and <c>largeBlobs: true</c>. It needs a discoverable
/// credential: the client requests the key only when the effective registration option is <c>rk: true</c>.
/// Use <c>ResidentKey = Required</c> when blob support is essential, and inspect the returned
/// <c>LargeBlob.Supported</c>. The current <see cref="LargeBlobSupport.Required"/> handling does not
/// guarantee failure whenever the created credential lacks blob support.
/// </para>
/// <para>
/// The SDK currently requires a PIN/UV token with the LargeBlobWrite permission for every write.
/// It requests that permission, which may require user verification despite <c>discouraged</c> when
/// verification is configured. This is stricter than CTAP §6.10.2, which permits unauthenticated writes
/// on an authenticator without configured verification when <c>alwaysUv</c> is not enabled; that path
/// is not supported by this SDK.
/// </para>
/// </remarks>
public sealed record class LargeBlobInput
{
    /// <summary>Registration: whether large-blob support is <c>required</c> or <c>preferred</c>.</summary>
    public LargeBlobSupport? Support { get; init; }

    /// <summary>Authentication: read the blob stored for the asserted credential.</summary>
    public bool Read { get; init; }

    /// <summary>Authentication: store this blob for the asserted credential.</summary>
    /// <remarks>
    /// Supply exactly one entry in the original <c>AllowCredentials</c> list, as WebAuthn requires for
    /// writes. The current client does not enforce this constraint; later credential selection does
    /// not satisfy it. The memory remains caller-owned and must stay unchanged until the ceremony,
    /// including storage work, completes. Clear sensitive caller-owned data when no longer needed.
    /// </remarks>
    public ReadOnlyMemory<byte>? Write { get; init; }
}