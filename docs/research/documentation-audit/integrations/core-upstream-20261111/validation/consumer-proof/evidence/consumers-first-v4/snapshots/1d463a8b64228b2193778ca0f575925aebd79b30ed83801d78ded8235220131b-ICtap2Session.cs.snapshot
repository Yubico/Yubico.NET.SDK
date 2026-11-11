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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2;

/// <summary>
/// Interface for CTAP2 session operations on a YubiKey authenticator.
/// </summary>
/// <remarks>
/// <para>
/// Exposes CTAP2 operations and selected optional features. Availability depends on the
/// authenticator's advertised capabilities, current configuration, product, firmware and transport;
/// advertisement alone does not grant PIN/UV permissions or establish support for every operation.
/// CTAP2.1 reference: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html
/// </para>
/// <para>
/// Await each call before starting another exchange. The session refuses overlapping exchanges,
/// rather than queuing them, and does not reserve the authenticator across a multi-command sequence.
/// A pre-canceled exchange is refused before wire admission.
/// Cancellation is cooperative: admitted SmartCard exchanges complete, while HID cancellation is
/// observed during keep-alive processing and the terminal response is drained. Cancellation can race
/// a successful return and does not impose a host deadline or undo device work. A transport,
/// notification callback or decoder failure is not proof that the authenticator made no change.
/// </para>
/// <para>
/// User-presence notifications describe SDK policy or an observed HID wait. Their completion reflects
/// the CTAP exchange before typed decoding, not verified UP/UV flags or relying-party authentication.
/// The relying party remains responsible for challenge, origin, account binding, signature,
/// attestation and requested UP/UV verification, including its counter policy.
/// </para>
/// </remarks>
public interface ICtap2Session : IApplicationSession
{
    /// <summary>
    /// Sends a raw CTAP CBOR request to the authenticator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a low-level method for advanced scenarios. Most users should use the
    /// higher-level methods like <see cref="MakeCredentialAsync"/> and <see cref="GetAssertionAsync"/>.
    /// </para>
    /// <para>
    /// The request must include the CTAP command byte as the first byte, followed by
    /// the CBOR-encoded parameters (if any). The caller selects the command schema, authorization
    /// and stateful sequence, and must meet CTAP canonical encoding, nesting and device message-size
    /// limits. This method does not perform capability, PIN/UV or command-schema preflight and does
    /// not infer user-presence notifications. Transport framing and CTAP status handling still apply.
    /// CTAP2.1 §8 limits nesting to four levels and messages to 1024 bytes unless GetInfo maxMsgSize
    /// advertises a larger limit; the session does not enforce that device limit at this boundary.
    /// </para>
    /// <para>
    /// Keep request storage valid and unchanged until the call completes, and clear caller-owned
    /// secrets when no longer needed. The returned read-only view has no disposable or automatic
    /// zeroing contract; clearing a copy does not clear the backend's response storage.
    /// </para>
    /// </remarks>
    /// <param name="request">The serialized CTAP request (command byte + CBOR payload).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The successful response payload without the CTAP status byte; it can be empty.</returns>
    /// <exception cref="ArgumentException">The request has no command byte.</exception>
    /// <exception cref="CtapException">A nonzero CTAP status is received, or Other is synthesized
    /// for a response with no status byte. Transport/APDU errors have separate origins.</exception>
    Task<ReadOnlyMemory<byte>> SendCborRequestAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the authenticator for information at the time of this call.
    /// </summary>
    /// <remarks>
    /// Each call sends GetInfo; options may change after PIN or configuration operations.
    /// This read does not refresh the session's construction-time firmware or protocol configuration.
    /// Do not insert GetInfo into a stateful assertion or administrative continuation sequence.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticator information.</returns>
    Task<AuthenticatorInfo> GetInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests user presence to select this authenticator.
    /// </summary>
    /// <remarks>
    /// This command is useful when multiple authenticators are present and the user
    /// needs to indicate which one they want to use. Presence depends on the transport and device.
    /// The caller coordinates other authenticators and cancels their outstanding selection requests.
    /// HID can report a device-wait notification; SmartCard does not predict a touch notification here.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SelectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the FIDO application to factory defaults.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reset invalidates generated credentials, including non-discoverable and CTAP1/U2F credentials,
    /// erases discoverable credentials and clears the FIDO PIN. Storage and configuration reset
    /// effects depend on the applicable protocol revision and product; this is not a device-wide reset.
    /// YubiKey Bio Multi-protocol Edition requires device-wide reset instead when individual FIDO reset
    /// is blocked; this method does not choose or perform that alternate product procedure.
    /// </para>
    /// <para>
    /// Send reset promptly after power-up. CTAP2.1 and CTAP2.3 §6.6 specify command receipt within
    /// ten seconds for displayless authenticators, separately from the subsequent presence wait.
    /// Yubico documents ten seconds for firmware 5.5.4 and later and five seconds before 5.5.2;
    /// that product guidance does not classify 5.5.2/5.5.3. Its USB touch wait is separately thirty
    /// seconds. Meet the product's transport and any enabled long-touch requirements; inspect
    /// <see cref="AuthenticatorInfo.TransportsForReset"/> and <see cref="AuthenticatorInfo.LongTouchForReset"/>
    /// where advertised. The authenticator enforces these conditions; this method does not arrange
    /// power cycling, enforce a local clock, refresh session state or roll back an attempted reset.
    /// Reset is destructive and cannot be undone.
    /// </para>
    /// <para>
    /// Product procedure: https://docs.yubico.com/yesdk/users-manual/application-fido2/fido2-reset.html
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="CtapException">
    /// Thrown with <see cref="CtapStatus.NotAllowed"/> if reset is attempted too long after
    /// power-up; other reset refusals and presence timeouts can have different CTAP statuses.
    /// </exception>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new credential on the authenticator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the CTAP2 authenticatorMakeCredential command.
    /// User presence is requested; <see cref="MakeCredentialOptions.UserPresence"/> cannot be false.
    /// The device's presence gesture depends on transport and policy, not always a physical touch.
    /// </para>
    /// <para>
    /// For discoverable credentials, set <see cref="MakeCredentialOptions.ResidentKey"/> to true.
    /// The caller constructs serialized client data and supplies its SHA-256 hash, not the raw RP
    /// challenge. The session checks the hash length, not its contents or the RP ID's origin authority.
    /// Preserve credential ID and user/account mapping and have the relying party verify the response.
    /// </para>
    /// <para>
    /// Built-in UV is requested with UserVerification only on a capable authenticator. When supplying
    /// PinUvAuthParam, omit UserVerification entirely (including false), use the appropriate protocol
    /// and authenticate this exact hash with a token authorized for MakeCredential and this RP.
    /// Legacy PIN-token fallback does not request explicit permissions/RP scope; select that route
    /// only when the authenticator's applicable legacy authorization rules permit the operation.
    /// The encoder does not negotiate capabilities or reject every invalid UV/token combination.
    /// Keep borrowed entity IDs, option buffers and collections stable through completion. The session
    /// clears its encoded request in finally, not caller inputs or decoded secret-bearing responses;
    /// session disposal is not sensitive-result cleanup. CTAP success precedes decoding and is not
    /// complete WebAuthn registration verification.
    /// </para>
    /// </remarks>
    /// <param name="clientDataHash">The 32-byte SHA-256 hash of serialized client data.</param>
    /// <param name="rp">Relying party entity.</param>
    /// <param name="user">User entity.</param>
    /// <param name="pubKeyCredParams">Supported credential parameters in preference order.</param>
    /// <param name="options">Optional parameters including exclude list, extensions, etc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created credential response with attestation.</returns>
    /// <exception cref="ArgumentException">The hash is not 32 bytes, UserPresence is false,
    /// or the credential parameter list is empty or contains duplicate type/algorithm pairs.</exception>
    /// <exception cref="CtapException">
    /// Thrown with <see cref="CtapStatus.CredentialExcluded"/> when the authenticator's applicable
    /// RP, credential and authorization processing finds an excluded credential, not a global duplicate-user check.
    /// </exception>
    Task<MakeCredentialResponse> MakeCredentialAsync(
        ReadOnlyMemory<byte> clientDataHash,
        PublicKeyCredentialRpEntity rp,
        PublicKeyCredentialUserEntity user,
        IReadOnlyList<PublicKeyCredentialParameters> pubKeyCredParams,
        MakeCredentialOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an assertion for authentication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the CTAP2 authenticatorGetAssertion command.
    /// User presence defaults to requested. Setting <see cref="GetAssertionOptions.UserPresence"/>
    /// to false sends a silent request and suppresses the SDK notification, subject to device policy;
    /// neither a notification nor its absence verifies the returned UP/UV flags.
    /// </para>
    /// <para>
    /// If multiple credentials match, use <see cref="GetNextAssertionAsync"/> to retrieve
    /// additional assertions according to the returned count. The caller supplies the SHA-256 hash
    /// of serialized client data, retains credential/account associations and has the relying party
    /// verify the proof. This method does not construct client data, authorize the RP ID against an
    /// origin or verify signatures. When supplying PinUvAuthParam, omit UserVerification entirely
    /// and use the matching protocol and GetAssertion permission/RP scope for the exact hash.
    /// Legacy token fallback has no explicit permission/RP request; applicable device authorization
    /// rules still govern its use. With no allow list, the request asks for applicable discoverable
    /// credentials for this RP, not a global credential or account search.
    /// </para>
    /// <para>
    /// Keep borrowed option buffers and collections stable through completion. Only the session's
    /// encoded request is cleared in finally; caller inputs and decoded response copies are not
    /// cleared by this method or session disposal. Callback or decode failure after CTAP success
    /// does not imply the device did nothing.
    /// </para>
    /// </remarks>
    /// <param name="rpId">Relying party identifier.</param>
    /// <param name="clientDataHash">The 32-byte SHA-256 hash of serialized client data.</param>
    /// <param name="options">Optional parameters including allow list, extensions, etc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first assertion response.</returns>
    /// <exception cref="ArgumentException">The RP ID is null/empty or the hash is not 32 bytes.</exception>
    /// <exception cref="CtapException">
    /// Thrown with <see cref="CtapStatus.NoCredentials"/> if no matching credential is found.
    /// </exception>
    Task<GetAssertionResponse> GetAssertionAsync(
        string rpId,
        ReadOnlyMemory<byte> clientDataHash,
        GetAssertionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next assertion when multiple credentials match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method when <see cref="GetAssertionResponse.NumberOfCredentials"/> is greater than 1.
    /// Collect N−1 additional responses promptly after the first count N. An absent count means one
    /// at the protocol level. Do not interpose GetInfo, PIN, administrative, raw or other authenticator
    /// operations, including on another channel. This method sends no new presence or PIN request
    /// and tracks no local remaining count, timer or sequence reservation. CTAP2.1 §6.3 allows
    /// NotAllowed for missing, exhausted or expired state (more than thirty seconds between calls);
    /// its timeout step is optional over NFC. An exchange guard is not a lease for the whole sequence.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next assertion response.</returns>
    Task<GetAssertionResponse> GetNextAssertionAsync(CancellationToken cancellationToken = default);
}