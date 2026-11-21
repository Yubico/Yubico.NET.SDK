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

using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.WebAuthn.Client.Authentication;

/// <summary>
/// Response from a WebAuthn authentication operation.
/// </summary>
public sealed record class AuthenticationResponse
{
    /// <summary>
    /// Gets the credential identifier used for this authentication.
    /// </summary>
    /// <remarks>
    /// Currently empty if the authenticator omits the credential descriptor. CTAP2.0 §5.2 permits
    /// omission for a single-descriptor request; CTAP2.1/2.3 §6.2.2 require the descriptor. Failure to
    /// recover an omitted identifier is a client limitation, not a usable empty credential ID.
    /// An original single-entry allow list can identify the requested credential application-side;
    /// the first entry of a multi-entry list cannot. The RP must verify the response and account binding.
    /// </remarks>
    public required ReadOnlyMemory<byte> CredentialId { get; init; }

    /// <summary>
    /// Gets the parsed authenticator data.
    /// </summary>
    public required WebAuthnAuthenticatorData AuthenticatorData { get; init; }

    /// <summary>
    /// Gets the raw authenticator data bytes.
    /// </summary>
    public required ReadOnlyMemory<byte> RawAuthenticatorData { get; init; }

    /// <summary>
    /// Gets the assertion signature.
    /// </summary>
    /// <remarks>
    /// This signature can be verified using the credential's public key
    /// over the concatenation of <see cref="RawAuthenticatorData"/> and
    /// the client data hash.
    /// </remarks>
    public required ReadOnlyMemory<byte> Signature { get; init; }

    /// <summary>
    /// Gets the user entity, when provided by the authenticator.
    /// </summary>
    /// <remarks>
    /// The user handle identifies the account; display fields may be absent, especially without user
    /// verification. CTAP can also return this entity for a non-discoverable credential. Use the handle
    /// and the RP's verified credential/account binding for authorization, not a display name.
    /// </remarks>
    public PublicKeyCredentialUserEntity? User { get; init; }

    /// <summary>
    /// Gets the signature counter value from the authenticator data.
    /// </summary>
    public required uint SignCount { get; init; }

    /// <summary>
    /// Gets the client data that was signed.
    /// </summary>
    public required WebAuthnClientData ClientData { get; init; }

    /// <summary>
    /// Gets the client extension results.
    /// </summary>
    public WebAuthn.Extensions.AuthenticationExtensionOutputs? ClientExtensionResults { get; init; }
}