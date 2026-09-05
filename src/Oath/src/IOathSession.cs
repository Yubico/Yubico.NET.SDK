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
    ///     Gets whether the <b>current session</b> is locked and must call <see cref="ValidateAsync" />
    ///     before protected operations can be performed.
    /// </summary>
    /// <remarks>
    ///     This reflects only the current session's unlock state, not whether the device has a
    ///     password configured at all: it starts <see langword="true" /> when the device has a
    ///     password configured, and becomes <see langword="false" /> once <see cref="ValidateAsync" />
    ///     succeeds — even though the device remains password-protected for the next session. To
    ///     check whether the device has a password configured, independent of this session's unlock
    ///     state, use <see cref="IsPasswordProtected" /> instead.
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
    Task<IReadOnlyList<Credential>> ListCredentialsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a new credential on the device.
    /// </summary>
    Task PutCredentialAsync(CredentialData credentialData, bool requireTouch = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a credential from the device.
    /// </summary>
    Task DeleteCredentialAsync(Credential credential, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Renames a credential on the device. Requires firmware 5.3.1+.
    /// </summary>
    Task<Credential> RenameCredentialAsync(Credential credential, string? newIssuer, string newName,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates the full HMAC response for a single credential.
    /// </summary>
    Task<ReadOnlyMemory<byte>> CalculateAsync(Credential credential, ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates a formatted OTP code for a single credential.
    /// </summary>
    Task<Code> CalculateCodeAsync(Credential credential, long? timestamp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates codes for all credentials on the device.
    ///     HOTP and touch-required credentials return <c>null</c> codes.
    /// </summary>
    Task<Dictionary<Credential, Code?>> CalculateAllAsync(long? timestamp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the OATH application, removing all credentials and the access key.
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Derives a key from a password using PBKDF2-HMAC-SHA1 with the device salt.
    /// </summary>
    /// <param name="password">
    ///     The borrowed UTF-8 encoded password. The caller owns the buffer and must clear it
    ///     after use.
    /// </param>
    /// <returns>
    ///     The derived 16-byte access key. The caller owns the returned array and must clear it
    ///     after use.
    /// </returns>
    /// <remarks>
    ///     <b>Breaking change:</b> The <c>password</c> parameter changed from <c>string</c>
    ///     to <c>ReadOnlyMemory&lt;byte&gt;</c> (UTF-8 encoded) to allow callers to zero
    ///     sensitive material after use. Pass <c>Encoding.UTF8.GetBytes(password)</c> and
    ///     zero the resulting array when finished.
    /// </remarks>
    byte[] DeriveKey(ReadOnlyMemory<byte> password);

    /// <summary>
    ///     Validates the access key using mutual HMAC-SHA1 challenge-response authentication.
    /// </summary>
    Task ValidateAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets or changes the access key for the OATH applet.
    /// </summary>
    Task SetKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes the access key from the OATH applet.
    /// </summary>
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
    ///     (not yet PBKDF2-derived) password bytes; this method derives the access key internally via
    ///     <see cref="DeriveKey" /> and zeroes the derived key after the authentication attempt completes.
    ///     The caller remains responsible for the lifetime of the bytes it returns.
    /// </param>
    /// <param name="cancellationToken">
    ///     Propagated to <paramref name="operation" />, <paramref name="passwordProvider" />, and the
    ///     internal <see cref="ValidateAsync" /> call. Checked before authenticating so a cancellation
    ///     requested between the locked failure and the retry stops promptly.
    /// </param>
    /// <returns>The result of the (possibly retried) <paramref name="operation" /> call.</returns>
    /// <exception cref="OathException">
    ///     <paramref name="passwordProvider" /> supplied a password that failed validation
    ///     (<see cref="OathFailureReason.WrongPassword" />), or <paramref name="operation" /> failed
    ///     again after a successful authentication.
    /// </exception>
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
}