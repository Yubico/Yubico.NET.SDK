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
/// Built on CTAP's <c>largeBlobKey</c> extension (§12.3) and authenticatorLargeBlobs (§6.10). Large blobs
/// exist only for discoverable credentials: the client requests the key only when the registration is
/// discoverable (CTAP §12.3 "If the options field ... does not map rk to true, return
/// CTAP2_ERR_INVALID_OPTION"), and otherwise reports <c>supported: false</c>.
/// </para>
/// <para>
/// A write needs a PIN/UV token with the LargeBlobWrite permission (CTAP §6.10.2); the client requests
/// it, which may require user verification even if the relying party asked for <c>discouraged</c>.
/// </para>
/// </remarks>
public sealed record class LargeBlobInput
{
    /// <summary>Registration: whether large-blob support is <c>required</c> or <c>preferred</c>.</summary>
    public LargeBlobSupport? Support { get; init; }

    /// <summary>Authentication: read the blob stored for the asserted credential.</summary>
    public bool Read { get; init; }

    /// <summary>Authentication: store this blob for the asserted credential.</summary>
    public ReadOnlyMemory<byte>? Write { get; init; }
}