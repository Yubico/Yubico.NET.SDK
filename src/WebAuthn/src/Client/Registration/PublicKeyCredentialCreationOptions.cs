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

using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
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
    /// Gets the challenge bytes (provided by relying party).
    /// </summary>
    public required ReadOnlyMemory<byte> Challenge { get; init; }

    /// <summary>
    /// Gets the relying party information.
    /// </summary>
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
    public AttestationPreference Attestation { get; init; } = AttestationPreference.None;

    /// <summary>
    /// Gets the timeout for the registration ceremony.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The WebAuthn L3 §5.1.3 <c>lifetimeTimer</c>. When set, the client cancels the pending authenticator
    /// request (FIDO HID: <c>CTAPHID_CANCEL</c> at the next keep-alive), the PIN prompt and the exclude-list
    /// pre-flight, and the call fails with <see cref="WebAuthnClientErrorCode.NotAllowed"/>. The deadline is a
    /// signal: a PC/SC exchange already in flight runs to its answer first.
    /// </para>
    /// <para>
    /// <c>null</c> starts no timer; L3 §15.1 recommends a 300 s default, which this client does not apply.
    /// Values are not clamped to the L3 range. A non-positive value is rejected with
    /// <see cref="WebAuthnClientErrorCode.InvalidRequest"/>. Cancelling the caller's token still surfaces as
    /// <see cref="OperationCanceledException"/>.
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