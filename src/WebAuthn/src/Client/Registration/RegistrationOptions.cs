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

using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Client.Registration;

/// <summary>
/// Options for WebAuthn credential registration (MakeCredential).
/// </summary>
public sealed record class RegistrationOptions
{
    /// <summary>
    /// Gets the challenge bytes (provided by relying party).
    /// </summary>
    public required ReadOnlyMemory<byte> Challenge { get; init; }

    /// <summary>
    /// Gets the relying party information.
    /// </summary>
    public required WebAuthnRelyingParty Rp { get; init; }

    /// <summary>
    /// Gets the user information.
    /// </summary>
    public required WebAuthnUser User { get; init; }

    /// <summary>
    /// Gets the list of supported public key credential algorithms in preference order.
    /// </summary>
    public required IReadOnlyList<CoseAlgorithm> PubKeyCredParams { get; init; }

    /// <summary>
    /// Gets the list of credentials to exclude (already registered).
    /// </summary>
    public IReadOnlyList<WebAuthnCredentialDescriptor>? ExcludeCredentials { get; init; }

    /// <summary>
    /// Gets the resident key preference.
    /// </summary>
    public ResidentKeyPreference ResidentKey { get; init; } = ResidentKeyPreference.Discouraged;

    /// <summary>
    /// Gets the user verification preference.
    /// </summary>
    public UserVerificationPreference UserVerification { get; init; } = UserVerificationPreference.Preferred;

    /// <summary>
    /// Gets the attestation conveyance preference.
    /// </summary>
    public AttestationPreference Attestation { get; init; } = AttestationPreference.None;

    /// <summary>
    /// Gets the timeout for the operation.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets the raw CBOR extensions map (opaque passthrough for Phase 3, typed in Phase 6).
    /// </summary>
    public string? ExtensionsRaw { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a cross-origin request.
    /// </summary>
    public bool? CrossOrigin { get; init; }

    /// <summary>
    /// Gets the top-level origin for nested contexts.
    /// </summary>
    public string? TopOrigin { get; init; }
}
