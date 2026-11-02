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

namespace Yubico.YubiKit.Oath;

/// <summary>
///     Interface for interacting with the OATH application on a YubiKey.
/// </summary>
public interface IOathSession : IApplicationSession
{
    /// <summary>
    ///     Gets the stable device identifier, computed as <c>Base64(SHA256(salt)[:16])</c> with padding stripped.
    ///     Changes on factory reset.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    ///     Gets the raw salt bytes from the OATH applet SELECT response.
    /// </summary>
    ReadOnlyMemory<byte> Salt { get; }

    /// <summary>
    ///     Gets whether the current session must be unlocked before performing protected operations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When a password is configured, the session starts locked. Call <see cref="ValidateAsync" />
    ///         to unlock it. Successful validation sets this property to <see langword="false" />
    ///         without removing the password.
    ///     </para>
    ///     <para>To check whether a password is configured, use <see cref="IsPasswordProtected" />.</para>
    /// </remarks>
    bool IsLocked { get; }

    /// <summary>
    ///     Gets whether the OATH applet has a password configured, independent of whether the
    ///     current session has already been unlocked via <see cref="ValidateAsync" />.
    /// </summary>
    /// <remarks>
    ///     Unlike <see cref="IsLocked" />, which becomes <see langword="false" /> once the current
    ///     session successfully validates, this property stays <see langword="true" /> for the
    ///     lifetime of the session as long as a password is configured on the device. Use this to
    ///     distinguish "no password configured" from "password already unlocked this session" —
    ///     for example, to decide whether a "remove password" UI action should be shown, or whether
    ///     a key-change flow needs to prompt for the current password.
    /// </remarks>
    bool IsPasswordProtected { get; }

    /// <summary>
    ///     Lists all credentials stored on the device.
    /// </summary>
    /// <remarks>
    ///     A TOTP credential with a recognized ASCII-digit period prefix that is zero or exceeds
    ///     <see cref="int.MaxValue" /> is omitted and logged at warning level. This does not imply
    ///     general validation of raw credential IDs.
    /// </remarks>
    /// <exception cref="Core.BadResponseException">
    ///     A recognized LIST entry lacks its type/algorithm byte or a nonempty credential ID.
    /// </exception>
    Task<IReadOnlyList<Credential>> ListCredentialsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a credential or replaces an existing credential with the same raw credential ID.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The raw ID is produced by <see cref="CredentialData.GetId" />. The SDK sends PUT without
    ///         checking whether that ID already exists on the device.
    ///     </para>
    ///     <para>
    ///         Borrows <paramref name="credentialData" /> until the operation completes. PUT uses independent
    ///         temporary secret storage and does not clear the credential's owned seed, including on failure.
    ///         The same instance can be used for authentication retry; dispose it only after all uses complete.
    ///     </para>
    /// </remarks>
    Task PutCredentialAsync(CredentialData credentialData,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a credential from the device.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     <paramref name="credential" /> was read from a different device (its <see cref="Credential.DeviceId" />
    ///     is not this session's <see cref="DeviceId" />). Nothing is sent.
    /// </exception>
    Task DeleteCredentialAsync(Credential credential, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Renames a credential on the device. Requires firmware 5.3.1+.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     <paramref name="credential" /> was read from a different device (its <see cref="Credential.DeviceId" />
    ///     is not this session's <see cref="DeviceId" />). Nothing is sent.
    /// </exception>
    Task<Credential> RenameCredentialAsync(Credential credential, string? newIssuer, string newName,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates the full HMAC response for a single credential.
    /// </summary>
    /// <param name="credential">The borrowed credential identifying the device entry to calculate.</param>
    /// <param name="challenge">
    ///     The caller-owned challenge, borrowed until the operation completes. Keep its storage unchanged
    ///     during the operation; the SDK does not modify or clear it.
    /// </param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>
    ///     Independent, caller-owned HMAC storage, excluding the protocol's leading digit-count byte.
    ///     The returned memory remains valid after session disposal.
    /// </returns>
    /// <remarks>
    ///     The SDK does not modify the credential or its ID. It requires a response tag whose value
    ///     contains a digit header and a nonempty HMAC payload before reporting successful user presence;
    ///     this structural check does not verify a particular digest length. Clear the returned bytes
    ///     when no longer needed, including when later processing fails. For a result named <c>hmac</c>,
    ///     use <c>CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(hmac).Span)</c> in a
    ///     <c>finally</c> block. These types are in <c>System.Security.Cryptography</c> and
    ///     <c>System.Runtime.InteropServices</c>, respectively. The caller also remains responsible
    ///     for clearing its challenge storage when appropriate, after the operation completes.
    /// </remarks>
    /// <exception cref="ArgumentException">
    ///     <paramref name="credential" /> was read from a different device (its <see cref="Credential.DeviceId" />
    ///     is not this session's <see cref="DeviceId" />). Nothing is sent.
    /// </exception>
    /// <exception cref="Core.BadResponseException">
    ///     The required full-response tag is missing, or its value lacks the digit header or HMAC payload.
    /// </exception>
    Task<ReadOnlyMemory<byte>> CalculateAsync(Credential credential, ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates a formatted OTP code for a single credential (RFC 4226 for HOTP, RFC 6238 for TOTP).
    /// </summary>
    /// <param name="credential">The credential to calculate.</param>
    /// <param name="timestamp">
    ///     Unix time in seconds, defaulting to the current UTC time. For TOTP, this determines the
    ///     time-step challenge and nominal generation window. For HOTP, it only sets <see cref="Code.ValidFrom" />;
    ///     the device uses its stored counter, not this timestamp, to calculate the code.
    /// </param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <exception cref="ArgumentException">
    ///     <paramref name="credential" /> is TOTP with a period that is not positive, or was read from a
    ///     different device (its <see cref="Credential.DeviceId" /> is not this session's <see cref="DeviceId" />).
    ///     Nothing is sent.
    /// </exception>
    /// <exception cref="Core.BadResponseException">
    ///     The required truncated-response tag is missing, or its value does not contain five bytes
    ///     with a digit count from six through eight.
    /// </exception>
    Task<Code> CalculateCodeAsync(Credential credential, long? timestamp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates codes for all credentials on the device (YKOATH CALCULATE ALL).
    ///     HOTP and touch-required credentials return <c>null</c> codes.
    /// </summary>
    /// <param name="timestamp">
    ///     Unix time in seconds, defaulting to the current UTC time. Determines the TOTP time-step
    ///     challenges and nominal generation windows.
    /// </param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <remarks>
    ///     The device computes every TOTP response with the default 30-second time step; credentials with
    ///     another period are recalculated individually. Entries with invalid recognized TOTP period
    ///     prefixes are omitted and logged at warning level, as described in <see cref="ListCredentialsAsync" />.
    ///     Known response values are validated before that host-side omission policy or any recalculation.
    /// </remarks>
    /// <exception cref="Core.BadResponseException">
    ///     An entry lacks required name/response structure, uses a known non-result tag or a full-response
    ///     tag for the truncated request, or has a truncated value other than five bytes with a digit count
    ///     from six through eight.
    /// </exception>
    Task<IReadOnlyDictionary<Credential, Code?>> CalculateAllAsync(long? timestamp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the OATH application, removing all credentials and the access key.
    /// </summary>
    /// <remarks>
    ///     Reset and the subsequent session-state refresh are separate exchanges. If the refresh fails
    ///     or is cancelled, the device may already have erased the data. Do not assume rollback;
    ///     dispose the session and reopen it rather than relying on its cached state.
    /// </remarks>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Derives a key from a password using PBKDF2-HMAC-SHA1 with the device salt.
    /// </summary>
    /// <param name="password">
    ///     The borrowed UTF-8-encoded password, not a derived access key. The caller owns the buffer
    ///     and must keep it unchanged until this method returns.
    /// </param>
    /// <returns>
    ///     The derived 16-byte access key. The caller owns the returned array and must clear it
    ///     after use.
    /// </returns>
    /// <remarks>
    ///     Clear the password buffer and returned key with
    ///     <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory" /> when each
    ///     is no longer needed, including when an operation fails. Wait for any operation borrowing
    ///     the key to complete before clearing it.
    /// </remarks>
    byte[] DeriveKey(ReadOnlyMemory<byte> password);

    /// <summary>
    ///     Validates the access key using mutual HMAC-SHA1 challenge-response authentication.
    /// </summary>
    /// <param name="key">The borrowed access key, normally obtained from <see cref="DeriveKey" />.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <remarks>
    ///     <para>
    ///         Requires an outstanding challenge from the OATH applet SELECT response. Call this method
    ///         only while <see cref="IsLocked" /> is <see langword="true" />, not for an already unlocked
    ///         or unprotected session. Successful validation consumes the challenge and unlocks the session.
    ///     </para>
    ///     <para>
    ///         Pass the access key, not raw password bytes. The caller retains ownership of <paramref name="key" />;
    ///         keep its buffer unchanged until the operation completes, then clear it with
    ///         <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory" /> when no longer
    ///         needed, including if validation fails.
    ///     </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">The session has no outstanding challenge.</exception>
    Task ValidateAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets or changes the access key for the OATH applet.
    /// </summary>
    /// <param name="key">The borrowed new access key, normally obtained from <see cref="DeriveKey" />.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <remarks>
    ///     <para>
    ///         If <see cref="IsLocked" /> is <see langword="true" />, first unlock the session with
    ///         <see cref="ValidateAsync" /> using the current access key. Then derive the new access key
    ///         from the new UTF-8-encoded password and pass it here, not the raw password bytes.
    ///     </para>
    ///     <para>
    ///         The caller retains ownership of <paramref name="key" />. Keep its buffer unchanged until
    ///         the operation completes, then clear it with
    ///         <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory" /> when no longer
    ///         needed, including if setting the key fails.
    ///     </para>
    /// </remarks>
    Task SetKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes the access key from the OATH applet.
    /// </summary>
    /// <remarks>
    ///     If <see cref="IsLocked" /> is <see langword="true" />, first unlock the session with
    ///     <see cref="ValidateAsync" /> using the current access key. Removing the access key removes
    ///     password protection without deleting stored credentials.
    /// </remarks>
    Task UnsetKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Runs <paramref name="operation" />, and if it fails because the OATH applet is locked,
    ///     collects a password via <paramref name="passwordProvider" />, validates it, and retries
    ///     <paramref name="operation" /> exactly once.
    /// </summary>
    /// <typeparam name="T">The result type produced by <paramref name="operation" />.</typeparam>
    /// <param name="operation">
    ///     The operation to run. Receives the same <paramref name="cancellationToken" /> passed to this method.
    /// </param>
    /// <param name="passwordProvider">
    ///     Invoked only if <paramref name="operation" /> fails with an <see cref="OathException" /> whose
    ///     <see cref="OathException.Reason" /> is <see cref="OathFailureReason.Locked" />. Must return the raw
    ///     UTF-8-encoded password bytes, not a derived access key; this method derives the access key internally via
    ///     <see cref="DeriveKey" /> and zeroes the derived key after the authentication attempt completes.
    ///     The caller owns the returned password buffer and must keep it unchanged until this method completes,
    ///     then clear it with <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory" />
    ///     when no longer needed, including if authentication or the retried operation fails.
    /// </param>
    /// <param name="cancellationToken">
    ///     Propagated to <paramref name="operation" />, <paramref name="passwordProvider" />, and the
    ///     internal <see cref="ValidateAsync" /> call. Checked before authenticating so a cancellation
    ///     requested between the locked failure and the retry stops promptly.
    /// </param>
    /// <returns>The result of the (possibly retried) <paramref name="operation" /> call.</returns>
    /// <remarks>
    ///     Authentication uses this session's outstanding challenge, as required by <see cref="ValidateAsync" />.
    /// </remarks>
    /// <exception cref="OathException">
    ///     <paramref name="passwordProvider" /> supplied a password that failed validation
    ///     (<see cref="OathFailureReason.WrongPassword" />), or <paramref name="operation" /> failed
    ///     again after a successful authentication.
    /// </exception>
    // Generic and non-generic forms preserve the established task-operation ergonomics.
#pragma warning disable RS0026
    Task<T> AuthenticateAndRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<CancellationToken, Task<ReadOnlyMemory<byte>>> passwordProvider,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Runs <paramref name="operation" />, and if it fails because the OATH applet is locked,
    ///     collects a password via <paramref name="passwordProvider" />, validates it, and retries
    ///     <paramref name="operation" /> exactly once.
    /// </summary>
    /// <remarks>See the generic overload for full parameter and exception documentation.</remarks>
    Task AuthenticateAndRetryAsync(
        Func<CancellationToken, Task> operation,
        Func<CancellationToken, Task<ReadOnlyMemory<byte>>> passwordProvider,
        CancellationToken cancellationToken = default);
#pragma warning restore RS0026
}