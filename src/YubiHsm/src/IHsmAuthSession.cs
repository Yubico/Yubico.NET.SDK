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
    Task<IReadOnlyList<HsmAuthCredential>> ListCredentialsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a symmetric (AES-128) credential in the YubiHSM Auth applet.
    /// </summary>
    /// <param name="managementKey">The 16-byte management key for authorization.</param>
    /// <param name="label">The credential label (1-64 UTF-8 bytes).</param>
    /// <param name="keyEnc">The 16-byte encryption key (K-ENC).</param>
    /// <param name="keyMac">The 16-byte MAC key (K-MAC).</param>
    /// <param name="credentialPassword">The credential password (string, padded to 16 bytes).</param>
    /// <param name="touchRequired">Whether touch is required to use this credential.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PutCredentialSymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> keyEnc,
        ReadOnlyMemory<byte> keyMac,
        string credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a symmetric credential derived from a password via PBKDF2-HMAC-SHA256.
    /// </summary>
    /// <param name="managementKey">The 16-byte management key for authorization.</param>
    /// <param name="label">The credential label (1-64 UTF-8 bytes).</param>
    /// <param name="derivationPassword">The password to derive keys from.</param>
    /// <param name="credentialPassword">The credential password (string, padded to 16 bytes).</param>
    /// <param name="touchRequired">Whether touch is required to use this credential.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PutCredentialDerivedAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        string derivationPassword,
        string credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a credential from the YubiHSM Auth applet.
    /// </summary>
    /// <param name="managementKey">The 16-byte management key for authorization.</param>
    /// <param name="label">The label of the credential to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteCredentialAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates session keys using a symmetric credential.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="context">The 16-byte host challenge concatenated with 16-byte HSM challenge.</param>
    /// <param name="credentialPassword">The credential password.</param>
    /// <param name="cardCryptogram">Optional card cryptogram for mutual authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session keys that must be disposed after use.</returns>
    Task<SessionKeys> CalculateSessionKeysSymmetricAsync(
        string label,
        ReadOnlyMemory<byte> context,
        string credentialPassword,
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
    /// <param name="currentManagementKey">The current 16-byte management key.</param>
    /// <param name="newManagementKey">The new 16-byte management key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PutManagementKeyAsync(
        ReadOnlyMemory<byte> currentManagementKey,
        ReadOnlyMemory<byte> newManagementKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the YubiHSM Auth applet to factory defaults.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates session keys using an asymmetric (EC P256) credential.
    ///     Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="context">The context data (EPK-OCE public key + EPK-SD public key, 130 bytes).</param>
    /// <param name="publicKey">
    ///     The uncompressed EC P256 public key of the YubiHSM 2 device (65 bytes: 0x04 || X || Y).
    /// </param>
    /// <param name="credentialPassword">The credential password.</param>
    /// <param name="cardCryptogram">
    ///     The card cryptogram from the YubiHSM 2 device. Required for asymmetric session key calculation
    ///     as it is used for mutual authentication between the YubiKey and the YubiHSM 2.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session keys that must be disposed after use.</returns>
    Task<SessionKeys> CalculateSessionKeysAsymmetricAsync(
        string label,
        ReadOnlyMemory<byte> context,
        ReadOnlyMemory<byte> publicKey,
        string credentialPassword,
        ReadOnlyMemory<byte> cardCryptogram,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the host challenge (symmetric) or ephemeral public key (asymmetric)
    ///     for a credential. Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="credentialPassword">
    ///     The credential password. Required for firmware &lt; 5.7.1;
    ///     optional for firmware &gt;= 5.7.1.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The challenge or ephemeral public key bytes.</returns>
    Task<ReadOnlyMemory<byte>> GetChallengeAsync(
        string label,
        string? credentialPassword = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores an asymmetric (EC P256) credential with an explicit private key.
    ///     Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="managementKey">The 16-byte management key for authorization.</param>
    /// <param name="label">The credential label (1-64 UTF-8 bytes).</param>
    /// <param name="privateKey">The 32-byte EC P256 private key.</param>
    /// <param name="credentialPassword">The credential password (string, padded to 16 bytes).</param>
    /// <param name="touchRequired">Whether touch is required to use this credential.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PutCredentialAsymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        ReadOnlyMemory<byte> privateKey,
        string credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates an asymmetric (EC P256) credential on-device.
    ///     The private key is generated by the YubiKey and never leaves the device.
    ///     Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="managementKey">The 16-byte management key for authorization.</param>
    /// <param name="label">The credential label (1-64 UTF-8 bytes).</param>
    /// <param name="credentialPassword">The credential password (string, padded to 16 bytes).</param>
    /// <param name="touchRequired">Whether touch is required to use this credential.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GenerateCredentialAsymmetricAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        string credentialPassword,
        bool touchRequired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the public key for an asymmetric credential.
    ///     Requires firmware 5.6.0+.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The 65-byte uncompressed EC point (0x04 + x[32] + y[32]).</returns>
    Task<ReadOnlyMemory<byte>> GetPublicKeyAsync(
        string label,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Changes the password for a credential using the current credential password.
    ///     Requires firmware 5.8.0+.
    /// </summary>
    /// <param name="label">The credential label.</param>
    /// <param name="currentPassword">The current credential password.</param>
    /// <param name="newPassword">The new credential password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ChangeCredentialPasswordAsync(
        string label,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Changes the password for a credential using the management key (admin override).
    ///     Requires firmware 5.8.0+.
    /// </summary>
    /// <param name="managementKey">The 16-byte management key for authorization.</param>
    /// <param name="label">The credential label.</param>
    /// <param name="newPassword">The new credential password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ChangeCredentialPasswordAdminAsync(
        ReadOnlyMemory<byte> managementKey,
        string label,
        string newPassword,
        CancellationToken cancellationToken = default);
}
