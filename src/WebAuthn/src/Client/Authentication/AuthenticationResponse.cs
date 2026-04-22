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

namespace Yubico.YubiKit.WebAuthn.Client.Authentication;

/// <summary>
/// Response from a WebAuthn authentication operation.
/// </summary>
public sealed record class AuthenticationResponse
{
    /// <summary>
    /// Gets the credential identifier used for this authentication.
    /// </summary>
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
    /// Gets the user information for discoverable credentials.
    /// </summary>
    /// <remarks>
    /// Only present for discoverable credentials when user verification is performed.
    /// </remarks>
    public WebAuthnUser? User { get; init; }

    /// <summary>
    /// Gets the signature counter value from the authenticator data.
    /// </summary>
    public required uint SignCount { get; init; }

    /// <summary>
    /// Gets the client data that was signed.
    /// </summary>
    public required WebAuthnClientData ClientData { get; init; }

    /// <summary>
    /// Gets the extension outputs.
    /// </summary>
    /// <remarks>
    /// Placeholder for Phase 6. Extension framework will replace this field.
    /// </remarks>
    public object? ClientExtensionResults { get; init; }
}
