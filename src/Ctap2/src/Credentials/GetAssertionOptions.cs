// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKit.Ctap2.Credentials;

/// <summary>
/// Options for the GetAssertion operation.
/// </summary>
/// <remarks>
/// <para>
/// These options correspond to CTAP2 authenticatorGetAssertion parameters:
/// - allowList (0x03): List of allowed credentials
/// - extensions (0x04): Extension inputs
/// - options (0x05): Boolean options (up, uv)
/// - pinUvAuthParam (0x06): PIN/UV auth parameter
/// - pinUvAuthProtocol (0x07): PIN/UV protocol version
/// </para>
/// <para>
/// The mutable object retains supplied lists and byte memory. Keep it and borrowed input storage
/// unchanged through the awaited command; read-only views are not immutable snapshots. The caller
/// clears auth parameters and secret-bearing inputs after completion, including failure. This type
/// does not negotiate capabilities, choose RP account policy or verify returned assertions.
/// Setting auth properties establishes no persistent logged-in state; the authenticator governs token
/// scope, permission consumption and expiry for the request.
/// </para>
/// </remarks>
public sealed class GetAssertionOptions
{
    /// <summary>
    /// Gets or sets the list of allowed credentials.
    /// </summary>
    /// <remarks>
    /// A nonempty list restricts the authenticator's credential search to these descriptors for the RP.
    /// Null/empty lists are omitted and request discovery by RP ID, subject to authorization and
    /// credential-protection policy. The list and descriptor ID storage are borrowed.
    /// </remarks>
    public IReadOnlyList<PublicKeyCredentialDescriptor>? AllowList { get; set; }

    /// <summary>
    /// Gets or sets whether user presence is required.
    /// </summary>
    /// <remarks>
    /// Null omits up (CTAP defaults to true); false explicitly requests no user-presence check, for
    /// example a preflight. Authenticator/extension policy can still reject it. This is not a promise
    /// of successful silent authentication; WebAuthn RP assertions ordinarily require UP.
    /// </remarks>
    public bool? UserPresence { get; set; }

    /// <summary>
    /// Gets or sets the CTAP uv option requesting built-in user verification.
    /// </summary>
    /// <remarks>
    /// Null omits uv; false still sends the key. CTAP 2.1/2.3 §6.2 prohibit uv alongside PinUvAuthParam,
    /// even false; omit it for token authorization and when built-in UV is unsupported. This setter
    /// neither obtains a PIN nor enforces the combination. Check received UV and perform RP verification.
    /// </remarks>
    public bool? UserVerification { get; set; }

    /// <summary>
    /// Gets or sets the PIN/UV auth parameter for authorization.
    /// </summary>
    /// <remarks>
    /// Authenticate the exact clientDataHash with a valid token and matching protocol. Permission-bearing
    /// tokens need GetAssertion permission and RP scope; actual state/lifetime is enforced by the
    /// authenticator. Omit UserVerification. Null/empty auth bytes are omitted, not encoded as a
    /// zero-length probe; tag size/permissions are not checked here. The memory remains caller-owned.
    /// </remarks>
    public ReadOnlyMemory<byte>? PinUvAuthParam { get; set; }

    /// <summary>
    /// Gets or sets the PIN/UV auth protocol version.
    /// </summary>
    /// <remarks>
    /// Match the auth parameter's protocol. Null defaults to encoded version 2 only with nonempty auth
    /// bytes; this is not protocol negotiation. A protocol without auth bytes is omitted.
    /// </remarks>
    public int? PinUvAuthProtocol { get; set; }

    /// <summary>
    /// Gets or sets extension inputs as a CBOR-encoded map.
    /// </summary>
    /// <remarks>
    /// Borrowed bytes are emitted without extension capability/policy validation. Null/zero-byte input
    /// is omitted, whereas an encoded empty CBOR map is present.
    /// </remarks>
    public ReadOnlyMemory<byte>? Extensions { get; set; }

    /// <summary>
    /// Creates default options.
    /// </summary>
    public GetAssertionOptions()
    {
    }

    /// <summary>
    /// Sets the user verification option.
    /// </summary>
    /// <param name="value">The built-in UV request value; this setter does not clear token authentication.</param>
    /// <returns>This options instance for chaining.</returns>
    public GetAssertionOptions WithUserVerification(bool value)
    {
        UserVerification = value;
        return this;
    }

    /// <summary>
    /// Sets the PIN/UV authentication parameters.
    /// </summary>
    /// <param name="param">Caller-owned auth parameter over clientDataHash; keep stable through the awaited command.</param>
    /// <param name="protocol">The protocol version.</param>
    /// <returns>This options instance for chaining.</returns>
    public GetAssertionOptions WithPinUvAuth(ReadOnlyMemory<byte> param, int protocol)
    {
        PinUvAuthParam = param;
        PinUvAuthProtocol = protocol;
        return this;
    }

    /// <summary>
    /// Sets the allow list.
    /// </summary>
    /// <param name="allowList">The allowed credentials.</param>
    /// <returns>This options instance for chaining.</returns>
    public GetAssertionOptions WithAllowList(IReadOnlyList<PublicKeyCredentialDescriptor> allowList)
    {
        AllowList = allowList;
        return this;
    }

    /// <summary>
    /// Sets the allow list from credential IDs.
    /// </summary>
    /// <param name="credentialIds">The allowed IDs; the new descriptor list still borrows each ID's byte storage.</param>
    /// <returns>This options instance for chaining.</returns>
    public GetAssertionOptions WithAllowList(params ReadOnlyMemory<byte>[] credentialIds)
    {
        AllowList = credentialIds
            .Select(id => PublicKeyCredentialDescriptor.FromCredentialId(id))
            .ToList();
        return this;
    }
}