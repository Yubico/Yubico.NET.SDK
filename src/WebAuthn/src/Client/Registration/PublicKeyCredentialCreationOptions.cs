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

using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Client.Registration;

/// <summary>
/// The relying party's options for a registration ceremony: WebAuthn L3 §5.4
/// <c>PublicKeyCredentialCreationOptions</c>, the dictionary passed to <c>navigator.credentials.create()</c>.
/// Consumed by <see cref="WebAuthnClient.MakeCredentialAsync"/>, which returns a <see cref="RegistrationResponse"/>.
/// </summary>
public sealed record class PublicKeyCredentialCreationOptions
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
    /// Gets the relying party information.
    /// </summary>
    /// <remarks>
    /// The application must validate the RP ID's syntax and authorize it for the configured origin
    /// through <see cref="WebAuthnClientOptions.RpIdValidator"/> before registration device I/O.
    /// The SDK checks only nonempty, faithfully UTF-8 encodable RP text and transmits the approved ID unchanged.
    /// </remarks>
    public required PublicKeyCredentialRpEntity Rp { get; init; }

    /// <summary>
    /// Gets the user information.
    /// </summary>
    public required PublicKeyCredentialUserEntity User { get; init; }

    /// <summary>
    /// Gets the list of supported public key credential algorithms in preference order.
    /// </summary>
    public required IReadOnlyList<CoseAlgorithm> PubKeyCredParams { get; init; }

    /// <summary>
    /// Gets the list of credentials to exclude (already registered).
    /// </summary>
    public IReadOnlyList<PublicKeyCredentialDescriptor>? ExcludeCredentials { get; init; }

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
    /// <remarks>
    /// None removes non-self attestation statements. Other preferences pass the returned statement
    /// through: Indirect does not use an anonymization service, and Enterprise does not currently
    /// send a CTAP enterprise-attestation request. See <see cref="AttestationPreference"/>.
    /// </remarks>
    public AttestationPreference Attestation { get; init; } = AttestationPreference.None;

    /// <summary>
    /// Gets the timeout for the registration ceremony.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Starts after local validation and the RP validation callback. Expiry signals cancellation of
    /// authenticator work, PIN prompting, exclude-list pre-flight, and extension processing. FIDO HID sends
    /// <c>CTAPHID_CANCEL</c> at the next keep-alive; a PC/SC exchange already in flight runs to its answer.
    /// If this produces an <see cref="OperationCanceledException"/> and the caller has not cancelled,
    /// the client reports <see cref="WebAuthnClientErrorCode.NotAllowed"/>. Successful completion can
    /// win the cancellation race; expiry does not prove that no credential or extension side effect exists.
    /// </para>
    /// <para>
    /// <c>null</c> starts no timer; L3 §15.1 recommends a 300 s default, which this client does not apply.
    /// Values are not clamped to the L3 range. A non-positive value is rejected with
    /// <see cref="WebAuthnClientErrorCode.InvalidRequest"/>; a positive value exceeding the delay supported
    /// by <see cref="CancellationTokenSource"/> throws <see cref="ArgumentOutOfRangeException"/>.
    /// This cooperative timer is not a complete implementation of L3 §5.1.3's lifetime timer.
    /// A resulting <see cref="OperationCanceledException"/> propagates unchanged if the caller's token
    /// is cancelled, rather than being wrapped as a timeout.
    /// </para>
    /// </remarks>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets the WebAuthn extension inputs for this registration.
    /// </summary>
    public WebAuthn.Extensions.RegistrationExtensionInputs? Extensions { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a cross-origin request.
    /// </summary>
    /// <remarks>
    /// The client data JSON always carries <c>crossOrigin</c>; null is serialized as <c>false</c>
    /// (WebAuthn L3 §5.8.1.1).
    /// </remarks>
    public bool? CrossOrigin { get; init; }

    /// <summary>
    /// Gets the top-level origin for nested (cross-origin) contexts.
    /// </summary>
    /// <remarks>
    /// Only valid together with <see cref="CrossOrigin"/> = true (WebAuthn L3 §5.8.1); the client rejects
    /// the options with <see cref="WebAuthnClientErrorCode.InvalidRequest"/> otherwise.
    /// </remarks>
    public string? TopOrigin { get; init; }
}