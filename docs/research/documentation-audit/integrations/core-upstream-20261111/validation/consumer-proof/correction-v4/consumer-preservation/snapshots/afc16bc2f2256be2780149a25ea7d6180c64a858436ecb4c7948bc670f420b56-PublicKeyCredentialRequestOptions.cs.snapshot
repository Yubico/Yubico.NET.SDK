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
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Client.Authentication;

/// <summary>
/// The relying party's options for an authentication ceremony: WebAuthn L3 §5.5
/// <c>PublicKeyCredentialRequestOptions</c>, the dictionary passed to <c>navigator.credentials.get()</c>.
/// Consumed by <see cref="WebAuthnClient.GetAssertionAsync"/>, which returns the matching credentials.
/// </summary>
public sealed record class PublicKeyCredentialRequestOptions
{
    /// <summary>
    /// Gets the cryptographic challenge from the relying party.
    /// </summary>
    /// <remarks>
    /// The RP must generate an unpredictable challenge, retain it for the ceremony, and verify its
    /// freshness in the response. WebAuthn L3 §13.4.3 requires enough entropy to make guessing infeasible
    /// and recommends at least 16 bytes. The SDK checks only that this value is nonempty.
    /// </remarks>
    public required ReadOnlyMemory<byte> Challenge { get; init; }

    /// <summary>
    /// Gets the relying party identifier.
    /// </summary>
    /// <remarks>
    /// The application must validate this RP ID's syntax and authorize it for the configured origin through
    /// <see cref="WebAuthnClientOptions.RpIdValidator"/> before authentication device I/O.
    /// The SDK checks only nonempty, faithfully UTF-8 encodable RP text; it imposes no domain/URI
    /// syntax or domain-suffix policy. Approval is an application decision, and the RP ID is transmitted unchanged.
    /// </remarks>
    public required string RpId { get; init; }

    /// <summary>
    /// Gets the list of credential descriptors to try for authentication.
    /// </summary>
    /// <remarks>
    /// If null or empty, the authenticator will search for discoverable credentials
    /// matching the <see cref="RpId"/>.
    /// </remarks>
    public IReadOnlyList<PublicKeyCredentialDescriptor>? AllowCredentials { get; init; }

    /// <summary>
    /// Gets the user verification requirement for this authentication.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="UserVerificationPreference.Preferred"/>.
    /// </remarks>
    public UserVerificationPreference UserVerification { get; init; } = UserVerificationPreference.Preferred;

    /// <summary>
    /// Gets the timeout for the authentication ceremony.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Starts after local validation and the RP validation callback. Expiry signals cancellation of
    /// authenticator work, PIN prompting, allow-list selection, and extension processing. FIDO HID sends
    /// <c>CTAPHID_CANCEL</c> at the next keep-alive; a PC/SC exchange already in flight runs to its answer.
    /// If this produces an <see cref="OperationCanceledException"/> and the caller has not cancelled,
    /// the client reports <see cref="WebAuthnClientErrorCode.NotAllowed"/>. Successful completion can
    /// win the cancellation race; expiry is not a guarantee of failure or absence of extension side effects.
    /// </para>
    /// <para>
    /// <c>null</c> starts no timer; L3 §15.1 recommends a 300 s default, which this client does not apply.
    /// Values are not clamped to the L3 range. A non-positive value is rejected with
    /// <see cref="WebAuthnClientErrorCode.InvalidRequest"/>; a positive value exceeding the delay supported
    /// by <see cref="CancellationTokenSource"/> throws <see cref="ArgumentOutOfRangeException"/>.
    /// This cooperative timer is not a complete implementation of L3 §5.1.4's lifetime timer.
    /// A resulting <see cref="OperationCanceledException"/> propagates unchanged if the caller's token
    /// is cancelled, rather than being wrapped as a timeout.
    /// </para>
    /// </remarks>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets whether this is a cross-origin request.
    /// </summary>
    /// <remarks>
    /// The client data JSON always carries <c>crossOrigin</c>; null is serialized as <c>false</c>
    /// (WebAuthn L3 §5.8.1.1).
    /// </remarks>
    public bool? CrossOrigin { get; init; }

    /// <summary>
    /// Gets the top-level origin for iframe (cross-origin) scenarios.
    /// </summary>
    /// <remarks>
    /// Only valid together with <see cref="CrossOrigin"/> = true (WebAuthn L3 §5.8.1); the client rejects
    /// the options with <see cref="WebAuthnClientErrorCode.InvalidRequest"/> otherwise.
    /// </remarks>
    public string? TopOrigin { get; init; }

    /// <summary>
    /// Gets the WebAuthn extension inputs for this authentication.
    /// </summary>
    public WebAuthn.Extensions.AuthenticationExtensionInputs? Extensions { get; init; }
}