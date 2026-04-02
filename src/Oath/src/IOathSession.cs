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

using Yubico.YubiKit.Core.Interfaces;

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
    ///     Gets whether the OATH applet is password-protected and requires validation.
    /// </summary>
    bool IsLocked { get; }

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
    Task<byte[]> CalculateAsync(Credential credential, ReadOnlyMemory<byte> challenge,
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
    byte[] DeriveKey(string password);

    /// <summary>
    ///     Validates the access key using mutual HMAC-SHA1 challenge-response authentication.
    /// </summary>
    Task ValidateAsync(byte[] key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets or changes the access key for the OATH applet.
    /// </summary>
    Task SetKeyAsync(byte[] key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes the access key from the OATH applet.
    /// </summary>
    Task UnsetKeyAsync(CancellationToken cancellationToken = default);
}
