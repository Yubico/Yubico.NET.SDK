// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.YubiHsm;

/// <summary>
///     Defines the public contract for interacting with the YubiHSM Auth applet on a YubiKey.
///     This applet stores credentials used to authenticate to YubiHSM 2 hardware security modules.
/// </summary>
public interface IHsmAuthSession : IApplicationSession
{
    /// <summary>
    ///     Lists all credentials stored in the YubiHSM Auth applet.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of stored credentials.</returns>
    /// <remarks>
    ///     Listing requires no authentication or touch. Results are metadata snapshots: unknown algorithm
    ///     identifiers are preserved and unknown touch values become <c>null</c>. Select a supported
    ///     algorithm explicitly before using a credential.
    /// </remarks>
    Task<IReadOnlyList<HsmAuthCredential>> ListCredentialsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a symmetric (AES-128) credential in the YubiHSM Auth applet.
    /// </summary>
    /// <param name="managementKey">The borrowed 16-byte management key. The caller must clear it after use.</param>
    /// <param name="label">The credential label (1-64 UTF-8 bytes).</param>
    /// <param name="keyEnc">The borrowed 16-byte encryption key (K-ENC). The caller must clear it after enrollment.</param>
    /// <param name="keyMac">The borrowed 16-byte MAC key (K-MAC). The caller must clear it after enrollment.</param>
    /// <param name="credentialPassword">
    ///     The borrowed UTF-8 encoded credential password, at most 16 bytes. Shorter values are
    ///     null-padded to 16 bytes in an internal copy. The caller must clear the input after use.
    /// </param>
    /// <param name="touchRequired">Whether touch is required to use this credential.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    ///     Provision the same K-ENC and K-MAC in the intended YubiHSM 2 authentication-key object through
    ///     your HSM library. The YubiKey label and HSM object identifier are separate application mappings.
    ///     The credential password protects use on the YubiKey; the management key authorizes enrollment.
    /// </remarks>
    /// <exception cref="HsmAuthRetryException">
    ///     The management key is incorrect. <see cref="HsmAuthRetryException.RetriesRemaining" />
    ///     reports the remaining management key attempts.
    /// </exception>
    Task PutCredentialSymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> keyEnc,
        ReadOnlyMemory<byte> keyMac,
        ReadOnlyMemory<byte> credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a symmetric credential derived from a password via PBKDF2-HMAC-SHA256.
    /// </summary>
    /// <remarks>
    ///     Derivation uses salt <c>Yubico</c>, 10,000 iterations and a 32-byte result split into K-ENC
    ///     then K-MAC. Provision the matching HSM authentication-key object with the same derived keys.
    ///     The derivation password supplies the long-lived HSM keys; the separate credential password
    ///     protects their use on the YubiKey.
    /// </remarks>
    /// <param name="managementKey">The borrowed 16-byte management key. The caller must clear it after use.</param>
    /// <param name="label">The credential label (1-64 UTF-8 bytes).</param>
    /// <param name="derivationPassword">
    ///     The borrowed UTF-8 encoded password used to derive K-ENC and K-MAC. It must not be
    ///     empty. The caller must clear it after use.
    /// </param>
    /// <param name="credentialPassword">
    ///     The borrowed UTF-8 encoded credential password, at most 16 bytes. Shorter values are
    ///     null-padded to 16 bytes in an internal copy. The caller must clear the input after use.
    /// </param>
    /// <param name="touchRequired">Whether touch is required to use this credential.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HsmAuthRetryException">
    ///     The management key is incorrect. <see cref="HsmAuthRetryException.RetriesRemaining" />
    ///     reports the remaining management key attempts.
    /// </exception>
    Task PutCredentialDerivedAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> derivationPassword,
        ReadOnlyMemory<byte> credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a credential from the YubiHSM Auth applet.
    /// </summary>
    /// <param name="managementKey">The borrowed 16-byte management key. The caller must clear it after use.</param>
    /// <param name="label">The label of the credential to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HsmAuthRetryException">
    ///     The management key is incorrect. <see cref="HsmAuthRetryException.RetriesRemaining" />
    ///     reports the remaining management key attempts.
    /// </exception>
    Task DeleteCredentialAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates session keys using a symmetric credential.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="context">
    ///     The borrowed 16-byte context: the 8-byte host challenge obtained from
    ///     <see cref="GetChallengeAsync" /> on firmware 5.6.0 and later, or freshly generated by
    ///     the caller on supported firmware, followed by the actual 8-byte HSM challenge
    ///     returned by the YubiHSM connector. The caller must clear it after use.
    /// </param>
    /// <param name="credentialPassword">
    ///     The borrowed UTF-8 encoded credential password, at most 16 bytes. Shorter values are
    ///     null-padded to 16 bytes in an internal copy. The caller must clear the input after use.
    /// </param>
    /// <param name="cardCryptogram">
    ///     The optional borrowed card cryptogram generated by the HSM and relayed by the connector.
    ///     Supply it to have the applet verify the HSM's cryptogram during calculation. Omitting it
    ///     derives keys without that verification; complete and authenticate the HSM handshake through
    ///     your HSM library before sending application commands.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session keys that must be disposed after use.</returns>
    /// <remarks>
    ///     Successful result parsing requires exactly 48 response bytes. Supplying a symmetric card
    ///     cryptogram can produce a 56-byte response that this calculation path rejects with
    ///     <see cref="ArgumentException" />. Vendor host code has handled 56-byte responses since 2023;
    ///     the affected firmware range and additional bytes' meaning remain unqualified here. The
    ///     rejection was observed on a development YubiKey. Verify the complete HSM authentication flow
    ///     on your target applet before relying on this path. Omitting the cryptogram skips applet
    ///     verification and is not an authenticated HSM workaround; the HSM integration must verify
    ///     its peer and complete the handshake.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="context" /> is not exactly 16 bytes.</exception>
    /// <exception cref="ArgumentException">The CALCULATE response is not exactly 48 bytes.</exception>
    /// <exception cref="HsmAuthRetryException">
    ///     The credential password is incorrect. <see cref="HsmAuthRetryException.RetriesRemaining" />
    ///     reports the remaining credential password attempts.
    /// </exception>
    Task<SessionKeys> CalculateSessionKeysSymmetricAsync(
        string label,
        ReadOnlyMemory<byte> context,
        ReadOnlyMemory<byte> credentialPassword,
        ReadOnlyMemory<byte>? cardCryptogram = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the number of remaining management key retries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of remaining retries.</returns>
    Task<int> GetManagementKeyRetriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Changes the management key.
    /// </summary>
    /// <remarks>
    ///     Persist a recoverable copy of the replacement in protected storage before installing it.
    ///     Keep the current key until the result is established. If communication fails, reconcile
    ///     the active key without repeated guesses; losing it requires a reset that erases credentials.
    /// </remarks>
    /// <param name="currentManagementKey">The borrowed current 16-byte management key. The caller must clear it after use.</param>
    /// <param name="newManagementKey">The borrowed new 16-byte management key. The caller must clear it after use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HsmAuthRetryException">
    ///     The current management key is incorrect. <see cref="HsmAuthRetryException.RetriesRemaining" />
    ///     reports the remaining management key attempts.
    /// </exception>
    Task PutManagementKeyAsync(
        ReadOnlyMemory<byte> currentManagementKey,
        ReadOnlyMemory<byte> newManagementKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the YubiHSM Auth applet to factory defaults.
    /// </summary>
    /// <remarks>
    ///     Reset permanently erases all credentials, restores the 16-zero-byte management key and resets
    ///     retry counters. The session reselects the applet over its existing protocol and retains its
    ///     resolved feature-version policy, including any caller-supplied firmware override.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates session keys using an asymmetric (EC P256) credential.
    ///     Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="context">
    ///     The borrowed 130-byte context: the 65-byte uncompressed EPK-OCE returned by
    ///     <see cref="GetChallengeAsync" />, followed by the actual 65-byte uncompressed EPK-SD
    ///     returned by the YubiHSM connector. The caller must clear it after use.
    /// </param>
    /// <param name="publicKey">
    ///     The borrowed, trusted static EC P256 public key of the YubiHSM 2 device (65 bytes:
    ///     0x04 || X || Y), obtained and authenticated through your HSM provisioning process.
    ///     <see cref="GetPublicKeyAsync" /> returns the YubiKey credential's public key instead;
    ///     use that key for enrollment in the HSM.
    /// </param>
    /// <param name="credentialPassword">
    ///     The borrowed UTF-8 encoded credential password, at most 16 bytes. Shorter values are
    ///     null-padded to 16 bytes in an internal copy. The caller must clear the input after use.
    /// </param>
    /// <param name="cardCryptogram">
    ///     The borrowed card cryptogram returned by the YubiHSM connector for mutual
    ///     authentication.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session keys that must be disposed after use.</returns>
    /// <exception cref="ArgumentException"><paramref name="context" /> is not exactly 130 bytes.</exception>
    /// <exception cref="ArgumentException">The CALCULATE response is not exactly 48 bytes.</exception>
    /// <exception cref="HsmAuthRetryException">
    ///     The credential password is incorrect. <see cref="HsmAuthRetryException.RetriesRemaining" />
    ///     reports the remaining credential password attempts.
    /// </exception>
    Task<SessionKeys> CalculateSessionKeysAsymmetricAsync(
        string label,
        ReadOnlyMemory<byte> context,
        ReadOnlyMemory<byte> publicKey,
        ReadOnlyMemory<byte> credentialPassword,
        ReadOnlyMemory<byte> cardCryptogram,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the host challenge (symmetric) or ephemeral public key (asymmetric)
    ///     for a credential. Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="credentialPassword">
    ///     The borrowed UTF-8 encoded credential password, at most 16 bytes. Shorter values are
    ///     null-padded to 16 bytes before transmission when the session supports the 5.7.1 password
    ///     feature gate. Supply this password for an asymmetric challenge on supported firmware.
    ///     On versions 5.6.0 through 5.7.0 the SDK omits a supplied password, so that argument provides
    ///     no password authentication. The caller must clear the input after use.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The applet response, expected to be an 8-byte host challenge or 65-byte uncompressed EPK-OCE.</returns>
    /// <remarks>
    ///     The session returns response bytes without validating their length or EC point encoding.
    ///     Check the shape required by your HSM handshake before constructing its context.
    ///     Protocol failures follow the general APDU error path, including password failures.
    /// </remarks>
    /// <exception cref="Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.ApduException">
    ///     The applet rejects the command. Inspect the status word for retry information before
    ///     deciding on another attempt; this operation uses the general exception rather than
    ///     <see cref="HsmAuthRetryException" />.
    /// </exception>
    Task<ReadOnlyMemory<byte>> GetChallengeAsync(
        string label,
        ReadOnlyMemory<byte>? credentialPassword = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores an asymmetric (EC P256) credential with an explicit private key.
    ///     Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="managementKey">The borrowed 16-byte management key. The caller must clear it after use.</param>
    /// <param name="label">The credential label (1-64 UTF-8 bytes).</param>
    /// <param name="privateKey">The borrowed 32-byte EC P256 private key. The caller must clear it after use.</param>
    /// <param name="credentialPassword">
    ///     The borrowed UTF-8 encoded credential password, at most 16 bytes. Shorter values are
    ///     null-padded to 16 bytes in an internal copy. The caller must clear the input after use.
    /// </param>
    /// <param name="touchRequired">Whether touch is required to use this credential.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HsmAuthRetryException">
    ///     The management key is incorrect. <see cref="HsmAuthRetryException.RetriesRemaining" />
    ///     reports the remaining management key attempts.
    /// </exception>
    Task PutCredentialAsymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> privateKey,
        ReadOnlyMemory<byte> credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates an asymmetric (EC P256) credential on-device.
    ///     The private key is generated by the YubiKey and never leaves the device.
    ///     Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="managementKey">The borrowed 16-byte management key. The caller must clear it after use.</param>
    /// <param name="label">The credential label (1-64 UTF-8 bytes).</param>
    /// <param name="credentialPassword">
    ///     The borrowed UTF-8 encoded credential password, at most 16 bytes. Shorter values are
    ///     null-padded to 16 bytes in an internal copy. The caller must clear the input after use.
    /// </param>
    /// <param name="touchRequired">Whether touch is required to use this credential.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HsmAuthRetryException">
    ///     The management key is incorrect. <see cref="HsmAuthRetryException.RetriesRemaining" />
    ///     reports the remaining management key attempts.
    /// </exception>
    Task GenerateCredentialAsymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the public key for an asymmetric credential.
    ///     Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The 65-byte uncompressed EC point (0x04 + x[32] + y[32]).</returns>
    /// <remarks>
    ///     This is the credential's long-lived PK-OCE for enrollment in the HSM authentication object.
    ///     The session checks response length; the integration layer validates point encoding as needed.
    ///     Obtain the HSM's own trusted static public key separately for asymmetric calculation.
    /// </remarks>
    Task<ReadOnlyMemory<byte>> GetPublicKeyAsync(
        string label,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Changes the password for a credential using the current credential password.
    ///     Requires firmware 5.8.0+.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="currentPassword">
    ///     The borrowed UTF-8 encoded current credential password, at most 16 bytes. Shorter values
    ///     are null-padded in an internal copy. The caller must clear the input after use.
    /// </param>
    /// <param name="newPassword">
    ///     The borrowed UTF-8 encoded new credential password, at most 16 bytes. Shorter values are
    ///     null-padded in an internal copy. The caller must clear the input after use.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HsmAuthRetryException">
    ///     The current credential password is incorrect.
    ///     <see cref="HsmAuthRetryException.RetriesRemaining" /> reports the remaining attempts.
    /// </exception>
    Task ChangeCredentialPasswordAsync(
        string label,
        ReadOnlyMemory<byte> currentPassword,
        ReadOnlyMemory<byte> newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Changes the password for a credential using the management key (admin override).
    ///     Requires firmware 5.8.0+.
    /// </summary>
    /// <param name="managementKey">The borrowed 16-byte management key. The caller must clear it after use.</param>
    /// <param name="label">The credential label.</param>
    /// <param name="newPassword">
    ///     The borrowed UTF-8 encoded new credential password, at most 16 bytes. Shorter values are
    ///     null-padded in an internal copy. The caller must clear the input after use.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HsmAuthRetryException">
    ///     The management key is incorrect. <see cref="HsmAuthRetryException.RetriesRemaining" />
    ///     reports the remaining management key attempts.
    /// </exception>
    Task ChangeCredentialPasswordAdminAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> newPassword,
        CancellationToken cancellationToken = default);
}