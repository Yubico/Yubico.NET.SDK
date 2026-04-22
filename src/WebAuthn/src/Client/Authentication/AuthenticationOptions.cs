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

using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Client.Authentication;

/// <summary>
/// Options for WebAuthn authentication (GetAssertion) operations.
/// </summary>
public sealed record class AuthenticationOptions
{
    /// <summary>
    /// Gets the cryptographic challenge from the relying party.
    /// </summary>
    /// <remarks>
    /// Must be at least 16 bytes of random data.
    /// </remarks>
    public required ReadOnlyMemory<byte> Challenge { get; init; }

    /// <summary>
    /// Gets the relying party identifier.
    /// </summary>
    /// <remarks>
    /// Must be a registrable domain suffix of the origin's effective domain,
    /// or match an entry in the enterprise allow-list.
    /// </remarks>
    public required string RpId { get; init; }

    /// <summary>
    /// Gets the list of credential descriptors to try for authentication.
    /// </summary>
    /// <remarks>
    /// If null or empty, the authenticator will search for discoverable credentials
    /// matching the <see cref="RpId"/>.
    /// </remarks>
    public IReadOnlyList<WebAuthnCredentialDescriptor>? AllowCredentials { get; init; }

    /// <summary>
    /// Gets the user verification requirement for this authentication.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="UserVerificationPreference.Preferred"/>.
    /// </remarks>
    public UserVerificationPreference UserVerification { get; init; } = UserVerificationPreference.Preferred;

    /// <summary>
    /// Gets the timeout for this authentication operation.
    /// </summary>
    /// <remarks>
    /// If null, no timeout is enforced. The timeout applies to the entire operation,
    /// including user interaction.
    /// </remarks>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets whether this is a cross-origin request.
    /// </summary>
    /// <remarks>
    /// When true, the client data JSON will include the crossOrigin field.
    /// </remarks>
    public bool? CrossOrigin { get; init; }

    /// <summary>
    /// Gets the top-level origin for iframe scenarios.
    /// </summary>
    /// <remarks>
    /// When set, the client data JSON will include the topOrigin field.
    /// </remarks>
    public string? TopOrigin { get; init; }

    /// <summary>
    /// Gets the raw extension inputs JSON.
    /// </summary>
    /// <remarks>
    /// Placeholder for Phase 6. Extension framework will replace this field.
    /// </remarks>
    public string? ExtensionsRaw { get; init; }
}
