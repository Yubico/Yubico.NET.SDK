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
/// Options for the MakeCredential operation.
/// </summary>
/// <remarks>
/// <para>
/// These options correspond to CTAP2 authenticatorMakeCredential parameters:
/// - excludeList (0x05): List of credentials to exclude
/// - extensions (0x06): Extension inputs
/// - options (0x07): Boolean options (rk, up, uv)
/// - pinUvAuthParam (0x08): PIN/UV auth parameter
/// - pinUvAuthProtocol (0x09): PIN/UV protocol version
/// - enterpriseAttestation (0x0A): Enterprise attestation type
/// </para>
/// <para>
/// This mutable options object retains supplied lists and byte memory, not immutable snapshots.
/// Keep it and its borrowed storage unchanged through the awaited command. The caller owns and clears
/// auth-parameter/secret-bearing input buffers after completion, including failure; request cleanup
/// does not clear them. These options encode requests, not capability negotiation or RP verification.
/// Token authorization depends on authenticator state/permissions, not a persistent logged-in state
/// established by setting these properties.
/// </para>
/// </remarks>
public sealed class MakeCredentialOptions
{
    /// <summary>
    /// Gets or sets the list of credentials to exclude.
    /// </summary>
    /// <remarks>
    /// Requests exclusion of these credentials under the authenticator's RP and authorization checks.
    /// This is not a global duplicate-user guarantee. Null/empty lists are omitted from the request;
    /// the supplied collection and descriptor IDs are borrowed.
    /// </remarks>
    public IReadOnlyList<PublicKeyCredentialDescriptor>? ExcludeList { get; set; }

    /// <summary>
    /// Gets or sets whether to create a discoverable credential (resident key).
    /// </summary>
    /// <remarks>
    /// True requests a credential discoverable by RP ID without supplying its ID. Support, storage
    /// capacity, authorization and credential-protection policy still apply. Null omits rk and false
    /// explicitly requests non-discoverable creation; neither describes where all key material is stored.
    /// </remarks>
    public bool? ResidentKey { get; set; }

    /// <summary>
    /// Gets or sets the "up" option key.
    /// </summary>
    /// <remarks>
    /// Defaults to omitted. Set <see langword="true"/> only when the key must be present on the wire
    /// (CTAP 2.1+ authenticators accept it; CTAP 2.0 may not). <see langword="false"/> is rejected with
    /// <see cref="ArgumentException"/>: CTAP 2.1-2.3 §6.1 says the value "MUST be true if present" and
    /// false yields CTAP2_ERR_INVALID_OPTION regardless of authenticator version. For a silent probe use
    /// getAssertion with up=false (§5 "pre-flight"), as the WebAuthn client does.
    /// </remarks>
    public bool? UserPresence { get; set; }

    /// <summary>
    /// Gets or sets the CTAP uv option requesting built-in user verification.
    /// </summary>
    /// <remarks>
    /// Null omits the uv key; false still sends it. CTAP 2.1/2.3 §6.1 require platforms to omit uv if
    /// built-in UV is unsupported and to omit it whenever PinUvAuthParam is supplied, even when false.
    /// PIN authorization uses a token/auth parameter, not this option. The SDK does not enforce these
    /// combinations or obtain a PIN here. Inspect the returned UV bit, then apply RP verification policy.
    /// </remarks>
    public bool? UserVerification { get; set; }

    /// <summary>
    /// Gets or sets the PIN/UV auth parameter for authorization.
    /// </summary>
    /// <remarks>
    /// Authenticate the exact 32-byte clientDataHash with a valid PIN/UV token and the matching protocol.
    /// Permission-bearing tokens require MakeCredential permission and the RP scope; authenticator
    /// state/lifetime still govern authorization. Omit UserVerification when using this parameter.
    /// Null/empty memory is omitted, so this surface does not encode a zero-length auth probe. The SDK
    /// does not validate tag length or token permissions here. The supplied memory remains caller-owned.
    /// </remarks>
    public ReadOnlyMemory<byte>? PinUvAuthParam { get; set; }

    /// <summary>
    /// Gets or sets the PIN/UV auth protocol version.
    /// </summary>
    /// <remarks>
    /// Match the protocol used for PinUvAuthParam. With nonempty auth bytes, null encodes version 2;
    /// this is an encoding default, not negotiation. Without auth bytes, this field is omitted.
    /// </remarks>
    public int? PinUvAuthProtocol { get; set; }

    /// <summary>
    /// Gets or sets the enterprise attestation type.
    /// </summary>
    /// <remarks>
    /// Values: 1 = vendor-facilitated, 2 = platform-managed.
    /// Requires advertised support, enabled configuration and applicable RP authorization. These
    /// options do not validate those conditions or the integer range; the response field reports use.
    /// </remarks>
    public int? EnterpriseAttestation { get; set; }

    /// <summary>
    /// Gets or sets extension inputs as a CBOR-encoded map.
    /// </summary>
    /// <remarks>
    /// Borrowed encoded bytes are written as supplied, without extension capability/policy validation.
    /// Null/zero-byte input is omitted; an encoded empty CBOR map is still present on the wire.
    /// </remarks>
    public ReadOnlyMemory<byte>? Extensions { get; set; }

    /// <summary>
    /// Creates default options.
    /// </summary>
    public MakeCredentialOptions()
    {
    }

    /// <summary>
    /// Sets the discoverable credential (resident key) option.
    /// </summary>
    /// <param name="value">Whether to create a discoverable credential.</param>
    /// <returns>This options instance for chaining.</returns>
    public MakeCredentialOptions WithResidentKey(bool value)
    {
        ResidentKey = value;
        return this;
    }

    /// <summary>
    /// Sets the user verification option.
    /// </summary>
    /// <param name="value">The built-in UV request value; this setter does not clear existing token authentication.</param>
    /// <returns>This options instance for chaining.</returns>
    public MakeCredentialOptions WithUserVerification(bool value)
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
    public MakeCredentialOptions WithPinUvAuth(ReadOnlyMemory<byte> param, int protocol)
    {
        PinUvAuthParam = param;
        PinUvAuthProtocol = protocol;
        return this;
    }

    /// <summary>
    /// Sets the exclude list.
    /// </summary>
    /// <param name="excludeList">The credentials to exclude.</param>
    /// <returns>This options instance for chaining.</returns>
    public MakeCredentialOptions WithExcludeList(IReadOnlyList<PublicKeyCredentialDescriptor> excludeList)
    {
        ExcludeList = excludeList;
        return this;
    }
}