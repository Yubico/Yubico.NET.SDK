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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Abstractions;

using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Defines the public contract for interacting with the OpenPGP application on a YubiKey.
/// </summary>
/// <remarks>
///     Use a SmartCard session and await each operation before starting the next. Administrative writes
///     generally require prior Admin PIN verification; the card enforces access conditions and advertised
///     capabilities. PIN inputs are borrowed UTF-8 bytes. Keep them valid until the awaited call completes,
///     then clear caller-owned buffers. Core guards one logical exchange, including APDU chaining and SCP,
///     rather than the whole public method. Certificate occurrence selection, key/cache updates and PIN/KDF
///     workflows span separate exchanges. Await the complete public task before another call or disposal,
///     and keep borrowed keys, input memory and prompt services alive through their last use.
///     Cancellation is checked before each exchange is admitted; an admitted SmartCard exchange runs with
///     CancellationToken.None. Ending an outer wait does not complete that task or permit input cleanup.
///     Await actual completion/teardown and inspect fresh state before retrying a persistent mutation.
///     Inherited IsAuthenticated describes SCP setup, while the card separately enforces User/Admin PIN access.
/// </remarks>
public interface IOpenPgpSession : IApplicationSession
{
    // ── Core Data Access ──────────────────────────────────────────────

    /// <summary>
    ///     Reads the Application Related Data (DO 0x6E) from the card.
    /// </summary>
    /// <remarks>
    ///     Refreshes cached algorithm attributes and UIF values without requiring PIN verification.
    ///     The KDF cache is separate and remains unchanged. Parsing requires recognized algorithms in
    ///     mandatory fields; use raw data access to inspect response fields outside the model.
    /// </remarks>
    Task<ApplicationRelatedData> GetApplicationRelatedDataAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reads a raw Data Object from the card.
    /// </summary>
    /// <remarks>
    ///     Returns the card's value bytes, including any container TLV supplied by that object. The card
    ///     decides access and tag support. Use the typed certificate methods for occurrence selection.
    ///     The caller owns the returned storage; clear sensitive values after use.
    /// </remarks>
    Task<ReadOnlyMemory<byte>> GetDataAsync(
        DataObject dataObject,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Writes a raw Data Object to the card.
    /// </summary>
    /// <remarks>
    ///     The caller supplies correctly encoded value bytes and any required authentication. This method
    ///     borrows <paramref name="data" /> until completion; clear sensitive input afterward. Writes can
    ///     persistently change credentials, keys or configuration. Cached algorithms, UIF and KDF remain
    ///     unchanged: refresh application data for algorithms/UIF, and open a fresh session after raw KDF writes.
    /// </remarks>
    Task PutDataAsync(
        DataObject dataObject,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets random bytes from the card.
    /// </summary>
    /// <param name="length">Requested positive byte count, within the card's advertised challenge limit.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Check Extended Capabilities before requesting a challenge. The SDK validates positivity and
    ///     sends the request; capability and maximum-length rejection are left to the card/protocol.
    ///     The caller owns the returned bytes and clears them if used as secret material.
    /// </remarks>
    Task<ReadOnlyMemory<byte>> GetChallengeAsync(
        int length,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the number of signatures performed with the Signature key.
    /// </summary>
    /// <remarks>
    ///     The card resets this counter on application reset and Signature key generation or import.
    ///     Read it as operational metadata rather than independent evidence of key provenance.
    /// </remarks>
    Task<int> GetSignatureCounterAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the current PIN status (attempts remaining, max lengths, policy).
    /// </summary>
    /// <remarks>Reads DO C4 without PIN verification. User PIN modes share the User retry counter.</remarks>
    Task<PwStatus> GetPinStatusAsync(
        CancellationToken cancellationToken = default);

    // ── PIN Operations ────────────────────────────────────────────────

    /// <summary>
    ///     Verifies the User PIN. If KDF is configured, the PIN is derived before sending.
    /// </summary>
    /// <remarks>
    ///     Both modes verify the same User PIN (PW1), with separate signature (P2=81) and other-operation
    ///     (P2=82) access states. The Reset Code is a separate recovery credential. The current extended-mode
    ///     implementation incorrectly selects Reset Code KDF salt and, on SW 6982, its retry counter.
    ///     With distinct User/Reset salts this can reject a correct PIN and consume a User retry; avoid
    ///     extended-mode use with that configuration until corrected. Treat its 6982 retry count as uncertain.
    ///     After an extended-mode failure, query fresh <see cref="GetPinStatusAsync" /> and inspect
    ///     <see cref="PwStatus.AttemptsUser" />; a typed zero can describe Reset Code attempts instead of a
    ///     blocked User PIN. Diagnose the cause before another verification attempt.
    /// </remarks>
    /// <param name="pin">
    ///     The borrowed UTF-8 encoded User PIN. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="extended">
    ///     If <c>true</c>, verifies for extended operations (decrypt/authenticate, P2=0x82).
    ///     If <c>false</c>, verifies for signature operations (P2=0x81). Defaults to <c>false</c>.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="OpenPgpInvalidPinException">
    ///     Thrown for every unsuccessful VERIFY response, including failures unrelated to a wrong PIN.
    ///     <see cref="OpenPgpInvalidPinException.RetriesRemaining" /> reports the available extracted or
    ///     queried count, or <c>null</c> when unavailable. Extended-mode 6982 fallback currently selects
    ///     Reset Code attempts; query fresh <see cref="GetPinStatusAsync" /> and inspect
    ///     <see cref="PwStatus.AttemptsUser" /> before deciding whether User PIN recovery is needed.
    ///     Diagnose the failure before retrying; a reported zero alone does not establish User blocking.
    /// </exception>
    Task VerifyPinAsync(
        ReadOnlyMemory<byte> pin,
        bool extended = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Verifies the Admin PIN. If KDF is configured, the PIN is derived before sending.
    /// </summary>
    /// <param name="pin">
    ///     The borrowed UTF-8 encoded Admin PIN. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="OpenPgpInvalidPinException">
    ///     Thrown for every unsuccessful VERIFY response, including failures unrelated to a wrong PIN.
    ///     <see cref="OpenPgpInvalidPinException.RetriesRemaining" /> reports the remaining
    ///     attempts (<c>0</c> if the PIN is now blocked), or <c>null</c> if the count could not
    ///     be determined.
    /// </exception>
    Task VerifyAdminAsync(
        ReadOnlyMemory<byte> pin,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears the User PIN verification state. Requires firmware 5.6.0+.
    /// </summary>
    /// <remarks>
    ///     Sends separate empty VERIFY commands for signature and other-operation states. A failure
    ///     between commands can leave only one state cleared. Admin authentication is unchanged.
    /// </remarks>
    Task UnverifyPinAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Changes the User PIN from <paramref name="currentPin" /> to <paramref name="newPin" />.
    /// </summary>
    /// <remarks>
    ///     The card checks the current PIN; prior verification is unnecessary. The session derives both
    ///     inputs with its current KDF. This is a persistent credential change; a rejected command propagates
    ///     an APDU exception rather than the retry-aware VERIFY exception.
    /// </remarks>
    /// <param name="currentPin">
    ///     The borrowed UTF-8 encoded current User PIN. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="newPin">
    ///     The borrowed UTF-8 encoded new User PIN. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ChangePinAsync(
        ReadOnlyMemory<byte> currentPin,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Changes the Admin PIN from <paramref name="currentPin" /> to <paramref name="newPin" />.
    /// </summary>
    /// <remarks>
    ///     The card checks the current PIN; prior verification is unnecessary. The session derives both
    ///     inputs with its current KDF. Rejection propagates an APDU exception. Retain a recovery plan before
    ///     changing administrator credentials; a blocked Admin PIN requires application reset for recovery.
    /// </remarks>
    /// <param name="currentPin">
    ///     The borrowed UTF-8 encoded current Admin PIN. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="newPin">
    ///     The borrowed UTF-8 encoded new Admin PIN. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ChangeAdminAsync(
        ReadOnlyMemory<byte> currentPin,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the Reset Code used for resetting the User PIN without the Admin PIN.
    /// </summary>
    /// <remarks>
    ///     Verify the Admin PIN first. The session derives the code with the Reset salt when KDF is active.
    ///     An empty value removes the code with KDF disabled; this method derives even an empty input with
    ///     an active KDF, so use an explicitly planned credential transition for that configuration.
    /// </remarks>
    /// <param name="resetCode">
    ///     The borrowed UTF-8 encoded Reset Code. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SetResetCodeAsync(
        ReadOnlyMemory<byte> resetCode,
        CancellationToken cancellationToken = default);

    /// <summary>Resets the User PIN using the configured Reset Code.</summary>
    /// <remarks>
    ///     Recovers a blocked User PIN and restores its retry counter. The card checks the Reset Code;
    ///     failed attempts can block that recovery route. The session derives each credential using its
    ///     own KDF salt. Rejection propagates an APDU exception; inspect status before another attempt.
    /// </remarks>
    /// <param name="resetCode">
    ///     The borrowed UTF-8 encoded Reset Code. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="newPin">
    ///     The borrowed UTF-8 encoded new User PIN. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ResetPinUsingResetCodeAsync(
        ReadOnlyMemory<byte> resetCode,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default);

    /// <summary>Resets the User PIN after the caller has authenticated the administrator PIN.</summary>
    /// <remarks>
    ///     The caller must have already verified the Admin PIN via <see cref="VerifyAdminAsync" />
    ///     before calling this method. No Reset Code is involved; only the new PIN is sent to the card.
    ///     The session applies the User KDF to the new PIN. Success restores the User retry counter;
    ///     rejection propagates an APDU exception.
    /// </remarks>
    /// <param name="newPin">
    ///     The borrowed UTF-8 encoded new User PIN. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ResetPinUsingAdminAuthenticationAsync(
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the retry attempt counts for User PIN, Reset Code, and Admin PIN.
    /// </summary>
    /// <remarks>
    ///     Verify the Admin PIN first. Each count is 1–255. This vendor command changes persistent retry
    ///     settings; the SDK leaves family/firmware support to the card. NEO firmware can also restore default
    ///     PIN values, so qualify that device's behavior and recovery credentials before changing counts.
    /// </remarks>
    Task SetPinAttemptsAsync(
        int userAttempts,
        int resetAttempts,
        int adminAttempts,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets whether signature authorization is consumed after each signature or retained while
    ///     the card access state remains valid.
    /// </summary>
    /// <remarks>
    ///     Verify the Admin PIN first. <see cref="PinPolicy.Always" /> consumes signature authorization
    ///     after each signature; <see cref="PinPolicy.Once" /> retains it while the card access state remains
    ///     valid. Selection, reset and explicit unverification can end that state independently of host lifetime.
    /// </remarks>
    Task SetSignaturePinPolicyAsync(
        PinPolicy policy,
        CancellationToken cancellationToken = default);

    // ── Key Operations ────────────────────────────────────────────────

    /// <summary>Generates a key pair with the supplied algorithm attributes in the specified slot.</summary>
    /// <remarks>
    ///     Verify the Admin PIN first and choose attributes advertised for the slot. Attribute writing precedes
    ///     generation and can invalidate an existing key. Later failure/cancellation can leave partial changes;
    ///     inspect fresh application data/public key before retrying. Successful generation refreshes the cache,
    ///     but refresh itself can fail after key creation. Manage certificates and fingerprint/time metadata separately.
    ///     EC generation uses the SDK's 5.2.0 check. Avoid on-device RSA generation on ROCA-affected firmware
    ///     (vendor advisory YSA-2017-01: 4.2.6–4.3.4); this method currently has no ROCA guard.
    ///     ATT provisioning uses a malformed CRT in this release; use ordinary slots until that encoder is corrected.
    /// </remarks>
    Task GenerateKeyAsync(
        KeyRef keyRef,
        AlgorithmAttributes attributes,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Imports a private key into the specified slot. Requires Admin PIN verification.
    /// </summary>
    /// <remarks>
    ///     The algorithm attributes for the slot are derived from the key and written first (when the card
    ///     allows changing them; otherwise only RSA 2048 can be imported). Modeled representations include
    ///     RSA 2048, 3072 and 4096 with public exponent 65537; P-256, P-384, P-521, secp256k1,
    ///     Brainpool P256r1/P384r1/P512r1, Ed25519 and X25519. Check host-provider, card and slot support
    ///     separately: catalog representation alone establishes none of them. The private key is not cleared
    ///     by this method.
    ///     The caller retains and disposes <paramref name="privateKey" />. Attribute writes can invalidate the
    ///     previous key before import succeeds; cancellation or refresh failure can follow a persistent change.
    ///     Inspect a fresh read before retrying. Certificates and fingerprint/time metadata are separate.
    ///     EC uses the SDK's 5.2.0 check. Historical NEO import encoding remains unqualified; ATT CRT encoding
    ///     is currently malformed, so this release provides no reliable ATT provisioning path.
    /// </remarks>
    /// <param name="keyRef">The target key slot.</param>
    /// <param name="privateKey">The private key to import.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="NotSupportedException">
    ///     The SDK rejects the key type, size, exponent or EC firmware threshold, or a card with fixed
    ///     attributes accepts only RSA 2048. These checks occur before an attribute/import command;
    ///     other failures can occur after persistent mutation.
    /// </exception>
    Task PutKeyAsync(
        KeyRef keyRef,
        IPrivateKey privateKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes the key in the specified slot.
    /// </summary>
    /// <remarks>
    ///     Verify the Admin PIN first. The method writes RSA 4096 attributes followed by RSA 2048 and
    ///     refreshes application data. This destructive sequence has no rollback and remains unqualified
    ///     on NEO, whose vendor deletion path differs. Certificates and host-written metadata are separate.
    /// </remarks>
    Task DeleteKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reads the public key from the specified slot via GENERATE ASYMMETRIC KEY PAIR (read mode).
    /// </summary>
    /// <returns>
    ///     The card public-key template (7F49): RSA fields 81 modulus/82 exponent or EC field 86 point.
    ///     Decode the template before importing it into a host cryptography API; it is not SubjectPublicKeyInfo.
    /// </returns>
    /// <remarks>
    ///     Ordinary-slot reads need no PIN verification. ATT reads currently use a malformed CRT and can fail;
    ///     this is independent of generating a target-key attestation certificate.
    /// </remarks>
    Task<ReadOnlyMemory<byte>> GetPublicKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates an attestation certificate for the key in <paramref name="keyRef" /> and returns it.
    ///     Requires firmware 5.2.0+ and User PIN verification (<see cref="VerifyPinAsync" />).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>This replaces the cardholder certificate for that key.</b> The YubiKey writes the
    ///         attestation statement into the key's cardholder certificate slot (DO 7F21) and discards
    ///         whatever was stored there. The SDK keeps no copy: read it first with
    ///         <see cref="GetCertificateAsync" /> if you need it, and note that putting it back with
    ///         <see cref="PutCertificateAsync" /> needs Admin PIN verification.
    ///     </para>
    ///     <para>
    ///         Use Sig, Dec or Aut containing an on-device-generated key. Ordinary User PIN verification is
    ///         used by the vendor CLI and SDK tests; ATT touch policy controls the attesting operation.
    ///         Certificate retrieval may fail after the overwrite has occurred. The caller disposes the returned
    ///         certificate and verifies its key binding and trusted CA chain; parsing alone establishes no trust.
    ///         Mutable name, fingerprint and date fields require their own interpretation.
    ///     </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///     Thrown when the device firmware does not support key attestation (requires 5.2.0+).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the device does not return an attestation certificate for
    ///     <paramref name="keyRef" /> after a successful GET_ATTESTATION, indicating unexpected
    ///     device behavior rather than a caller input error.
    /// </exception>
    Task<X509Certificate2> AttestKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Refreshes application data and gets key status information (none/generated/imported) for reported slots.
    /// </summary>
    /// <remarks>Slot presence varies by card profile. Use these statuses rather than fingerprint length to inspect keys.</remarks>
    Task<KeyInformation> ListKeyInformationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Refreshes application data and gets the card's stored 20-byte fingerprints for reported slots.
    /// </summary>
    /// <remarks>Zero-filled fingerprints can represent unset metadata even when a key exists.</remarks>
    Task<KeyFingerprints> GetKeyFingerprintsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Refreshes application data and gets the card's stored generation-time metadata for reported slots.
    /// </summary>
    /// <remarks>
    ///     Zero denotes an unspecified time. The card stores unsigned four-byte seconds since 1970-01-01 UTC;
    ///     this API currently exposes those bytes as a signed int. Values above int.MaxValue therefore appear
    ///     negative. Preserve the original four-byte representation when inspecting the full unsigned range,
    ///     rather than interpreting negative API values as dates before 1970. See <see cref="GenerationTimes" />.
    /// </remarks>
    Task<GenerationTimes> GetGenerationTimesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the fingerprint for the specified key slot.
    /// </summary>
    /// <param name="keyRef">The target slot.</param>
    /// <param name="fingerprint">Exactly 20 borrowed bytes calculated by the host for the card's key profile.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Verify the Admin PIN first. The SDK writes the supplied metadata without calculating a fingerprint
    ///     or checking its correspondence to the key. Generation/import leaves that calculation to the host.
    /// </remarks>
    Task SetKeyFingerprintAsync(
        KeyRef keyRef,
        ReadOnlyMemory<byte> fingerprint,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the generation timestamp for the specified key slot.
    /// </summary>
    /// <param name="keyRef">The target slot.</param>
    /// <param name="timestamp">
    ///     The signed int carrying the four bytes to write in big-endian order. Pass zero to mark the
    ///     generation time unspecified. Card time uses unsigned seconds since 1970-01-01 UTC; preserve
    ///     that four-byte representation when handling values above int.MaxValue, which appear negative here.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Verify the Admin PIN first. The method writes the supplied int's four-byte representation,
    ///     including negative values, without validating a date range or applying a special zero policy.
    ///     The card interprets zero as unspecified; other values are unsigned seconds, rather than dates
    ///     before 1970. This host-supplied metadata is independent of key creation.
    /// </remarks>
    Task SetGenerationTimeAsync(
        KeyRef keyRef,
        int timestamp,
        CancellationToken cancellationToken = default);

    // ── Certificate Operations ────────────────────────────────────────

    /// <summary>
    ///     Gets the certificate stored for the specified key slot.
    /// </summary>
    /// <returns>
    ///     The certificate, which the caller must dispose, or <see langword="null" /> when a successful
    ///     read returns empty data. For the Sig, Dec, and Aut cardholder slots, a GET DATA response of
    ///     0x6A88 (referenced data or DO not found) also returns <see langword="null" />.
    /// </returns>
    /// <remarks>
    ///     Certificate-slot selection failures and other read failures are propagated, not treated as
    ///     certificate absence. For the attestation certificate (Att, DO 0xFC), all APDU failures are
    ///     propagated; only a successful empty read returns <see langword="null" />.
    ///     Ordinary cardholder reads use the SDK's 5.2.0 check and occurrence selection. ATT uses its
    ///     separate data object directly. Reading/decoding a certificate performs no trust-chain validation.
    /// </remarks>
    /// <exception cref="ApduException">
    ///     Certificate-slot selection failed, or the certificate read failed with a status other than
    ///     the cardholder-slot absence response described above.
    /// </exception>
    /// <exception cref="CryptographicException">
    ///     The returned non-empty data cannot be decoded as an X.509 certificate.
    /// </exception>
    Task<X509Certificate2?> GetCertificateAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a certificate for the specified key slot.
    /// </summary>
    /// <remarks>
    ///     Verify the Admin PIN first. Replaces certificate bytes independently of the slot's private key;
    ///     the caller retains and disposes the supplied certificate. Ordinary slots use the 5.2.0 SDK check
    ///     and SELECT DATA; ATT writes its separate data object. Respect the advertised certificate-size limit.
    /// </remarks>
    Task PutCertificateAsync(
        KeyRef keyRef,
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes the certificate for the specified key slot.
    /// </summary>
    /// <remarks>
    ///     Verify the Admin PIN first. Writes an empty certificate value while retaining the private key.
    ///     ATT certificate deletion removes public attestation material that application reset preserves;
    ///     save a copy first if needed. Ordinary slots use the SDK's 5.2.0 certificate check.
    /// </remarks>
    Task DeleteCertificateAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    // ── Configuration ─────────────────────────────────────────────────

    /// <summary>
    ///     Gets the User Interaction Flag for the specified key slot.
    /// </summary>
    /// <remarks>Reads the slot directly using the SDK's 4.2.0 UIF check. Card support varies by slot and policy.</remarks>
    Task<Uif> GetUifAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the User Interaction Flag for the specified key slot.
    /// </summary>
    /// <remarks>
    ///     Verify the Admin PIN first. Updates persistent policy and the session's UIF override cache after
    ///     success. The SDK checks 4.2.0; cached and ATT policies require additional card support. A fixed
    ///     ordinary-slot policy clears on key replacement/deletion or application reset. Fixed ATT policy
    ///     survives reset and requires ATT key replacement, which loses factory private-key provenance;
    ///     ATT provisioning is currently affected by malformed CRT encoding.
    /// </remarks>
    Task SetUifAsync(
        KeyRef keyRef,
        Uif uif,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the algorithm attributes for the specified key slot.
    /// </summary>
    /// <remarks>Refreshes application data and selects the slot's parsed attributes from it.</remarks>
    /// <exception cref="NotSupportedException">
    ///     Thrown when <paramref name="keyRef" /> is <see cref="KeyRef.Att" /> and the device
    ///     does not have an Attestation key slot.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="keyRef" /> is not a defined value.
    /// </exception>
    Task<AlgorithmAttributes> GetAlgorithmAttributesAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the algorithm attributes for the specified key slot.
    /// </summary>
    /// <remarks>
    ///     Verify the Admin PIN first and check advertised changeable-attribute support. A changed algorithm
    ///     can erase or disable the existing key. This method leaves cached crypto attributes unchanged;
    ///     call <see cref="GetApplicationRelatedDataAsync" /> before a subsequent crypto operation.
    /// </remarks>
    Task SetAlgorithmAttributesAsync(
        KeyRef keyRef,
        AlgorithmAttributes attributes,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the list of supported algorithm attributes per key slot.
    ///     Requires firmware 5.2.0+.
    /// </summary>
    /// <remarks>
    ///     Preserves order and repeated slots. Entries with unknown tags or algorithm encodings rejected by
    ///     the parser are skipped; known Curve25519 entries receive a pre-5.6.1 workaround. Treat the result
    ///     as the SDK's interpreted list and use raw FA bytes when inspecting omitted entries.
    /// </remarks>
    Task<IReadOnlyList<SupportedAlgorithm>> GetSupportedAlgorithmsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the current KDF configuration.
    /// </summary>
    /// <remarks>
    ///     Lazily reads F9 and returns the same cached instance on later calls. An APDU read error currently
    ///     caches KdfNone. Treat the result as session-retained: keep it undisposed while the session uses it.
    ///     The session disposes its current cached KDF during disposal. Refreshing application data leaves it unchanged.
    ///     Synchronous disposal clears that object before protocol drain; asynchronous disposal clears it after
    ///     Core teardown, including on failure. Await complete PIN/KDF tasks before either disposal path.
    /// </remarks>
    Task<Kdf> GetKdfAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the KDF configuration. Requires Admin PIN verification.
    /// </summary>
    /// <remarks>
    ///     This persistent credential transition writes F9 and retains the supplied instance as the session's
    ///     cache. Keep that object undisposed until session disposal. Replacement/reset currently drops the
    ///     previous reference without disposing it; retain an explicit cleanup scope for such objects after
    ///     the session ends. The card profile requires terminal software to align credentials and KDF data;
    ///     YubiKey's vendor transition restores initial User/Admin values and invalidates the Reset Code.
    ///     Subsequent PIN changes derive both current and new inputs with this KDF. Qualify enable/change/disable
    ///     and recovery on a dedicated device before provisioning production credentials. Configure KDF before
    ///     ordinary keys; the card can refuse F9 with conditions-not-satisfied depending on application state.
    /// </remarks>
    Task SetKdfAsync(
        Kdf kdf,
        CancellationToken cancellationToken = default);

    // ── Cryptographic Operations ──────────────────────────────────────

    /// <summary>
    ///     Signs a message using the Signature key.
    /// </summary>
    /// <remarks>
    ///     Verify the User PIN in signature mode first; policy may require verification for every signature.
    ///     RSA/ECDSA hash the borrowed message with the named algorithm; RSA supplies DigestInfo for card
    ///     PKCS#1 v1.5 padding. EdDSA sends the raw message and ignores hashAlgorithm (ordinary Ed25519).
    ///     RSA returns modulus-sized raw bytes, ECDSA a DER (r,s) sequence, and Ed25519 64 raw bytes.
    ///     The caller owns returned storage and clears it after use. RSA/EdDSA transfer the response backing
    ///     storage; ECDSA allocates DER output and clears its raw response. All use cached algorithm attributes.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///     Thrown when <paramref name="hashAlgorithm" /> is not one of SHA1, SHA256, SHA384, or
    ///     SHA512 for RSA/ECDSA. The EdDSA branch ignores this parameter.
    /// </exception>
    Task<ReadOnlyMemory<byte>> SignAsync(
        ReadOnlyMemory<byte> message,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs raw RSA deciphering or ECDH agreement using the Decryption key.
    /// </summary>
    /// <param name="ciphertext">
    ///     Borrowed raw RSAES-PKCS1-v1_5 ciphertext block (one modulus length), or an uncompressed EC point
    ///     (04 || X || Y) for ECDH. The SDK supplies the RSA indicator or EC cipher-data wrapper.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Caller-owned plaintext for RSA, or raw shared-secret material for ECDH; clear it after use.</returns>
    /// <remarks>
    ///     Verify the User PIN in extended mode first, subject to that method's current distinct-salt KDF defect.
    ///     The host handles OpenPGP packets, content encryption/decryption, ECDH KDF and protocol validation.
    ///     A complete .gpg file or OAEP block is outside this input contract. Current X25519 formatting omits
    ///     the required EC wrapper; use a qualified NIST-curve/RSA path until corrected. The method uses cached
    ///     algorithm attributes, copies the successful result for the caller and clears the raw response.
    /// </remarks>
    Task<ReadOnlyMemory<byte>> DecryptAsync(
        ReadOnlyMemory<byte> ciphertext,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs internal authentication using the Authentication key.
    /// </summary>
    /// <remarks>
    ///     Verify the User PIN in extended mode first, subject to its distinct-salt KDF defect. RSA/ECDSA hash
    ///     the borrowed input; RSA uses PKCS#1 v1.5 DigestInfo. EdDSA sends the raw input and ignores the hash
    ///     parameter. Results are raw modulus-sized RSA, DER ECDSA (r,s), or raw 64-byte Ed25519 signatures.
    ///     The caller owns and clears the returned storage. RSA/EdDSA transfer response backing storage;
    ///     ECDSA converts to DER and clears the raw response. The host verifies the result against its challenge.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///     Thrown when <paramref name="hashAlgorithm" /> is not one of SHA1, SHA256, SHA384, or
    ///     SHA512 for RSA/ECDSA. The EdDSA branch ignores this parameter.
    /// </exception>
    Task<ReadOnlyMemory<byte>> AuthenticateAsync(
        ReadOnlyMemory<byte> data,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default);

    // ── Factory Reset ─────────────────────────────────────────────────

    /// <summary>
    ///     Attempts the OpenPGP application reset sequence: PIN blocking, TERMINATE and ACTIVATE.
    ///     Requires firmware 1.0.6+.
    /// </summary>
    /// <remarks>
    ///     Successful device reset irreversibly removes ordinary keys/certificates and restores application
    ///     defaults. YubiKey ATT key/certificate survive, as does a fixed ATT touch policy. A public certificate
    ///     backup cannot recover a replaced factory ATT private key or its manufacturer trust.
    ///     The current method ignores TERMINATE/ACTIVATE status words and checks no complete reset postcondition;
    ///     normal return alone is insufficient to establish erasure. Read back keys, certificates, PIN/retry,
    ///     KDF and policy state independently. Failure/cancellation can leave PINs blocked or state partly changed.
    ///     The cached KDF reference is dropped without disposal; retain its cleanup scope through session teardown.
    ///     Reset retains the effective version and any existing SCP processor graph. Reselection sends a plain
    ///     SELECT through that graph, then uses its retained secure state for subsequent commands; it performs
    ///     no fresh SCP handshake. The inherited authentication flag does not establish channel restoration.
    ///     After uncertain reset/recovery, dispose and reopen, then inspect persistent state before retrying.
    /// </remarks>
    Task ResetAsync(
        CancellationToken cancellationToken = default);
}