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
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Defines the public contract for interacting with the OpenPGP application on a YubiKey.
/// </summary>
public interface IOpenPgpSession : IApplicationSession
{
    // ── Core Data Access ──────────────────────────────────────────────

    /// <summary>
    ///     Reads the Application Related Data (DO 0x6E) from the card.
    /// </summary>
    Task<ApplicationRelatedData> GetApplicationRelatedDataAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reads a raw Data Object from the card.
    /// </summary>
    Task<ReadOnlyMemory<byte>> GetDataAsync(
        DataObject dataObject,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Writes a raw Data Object to the card.
    /// </summary>
    Task PutDataAsync(
        DataObject dataObject,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets random bytes from the card.
    /// </summary>
    Task<ReadOnlyMemory<byte>> GetChallengeAsync(
        int length,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the number of signatures performed with the Signature key.
    /// </summary>
    Task<int> GetSignatureCounterAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the current PIN status (attempts remaining, max lengths, policy).
    /// </summary>
    Task<PwStatus> GetPinStatusAsync(
        CancellationToken cancellationToken = default);

    // ── PIN Operations ────────────────────────────────────────────────

    /// <summary>
    ///     Verifies the User PIN. If KDF is configured, the PIN is derived before sending.
    /// </summary>
    /// <remarks>
    ///     <b>Breaking change:</b> PIN parameters changed from <c>string</c> to
    ///     <c>ReadOnlyMemory&lt;byte&gt;</c> (UTF-8 encoded) to allow callers to zero
    ///     sensitive material after use. Pass <c>Encoding.UTF8.GetBytes(pin)</c> and zero
    ///     the resulting array when finished.
    /// </remarks>
    /// <param name="pinUtf8">The User PIN as UTF-8 encoded bytes.</param>
    /// <param name="extended">
    ///     If <c>true</c>, verifies for extended operations (decrypt/authenticate, P2=0x82).
    ///     If <c>false</c>, verifies for signature operations (P2=0x81). Defaults to <c>false</c>.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task VerifyPinAsync(
        ReadOnlyMemory<byte> pinUtf8,
        bool extended = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Verifies the Admin PIN. If KDF is configured, the PIN is derived before sending.
    /// </summary>
    Task VerifyAdminAsync(
        ReadOnlyMemory<byte> pinUtf8,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears the User PIN verification state. Requires firmware 5.6.0+.
    /// </summary>
    Task UnverifyPinAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Changes the User PIN from <paramref name="currentPinUtf8" /> to <paramref name="newPinUtf8" />.
    /// </summary>
    Task ChangePinAsync(
        ReadOnlyMemory<byte> currentPinUtf8,
        ReadOnlyMemory<byte> newPinUtf8,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Changes the Admin PIN from <paramref name="currentPinUtf8" /> to <paramref name="newPinUtf8" />.
    /// </summary>
    Task ChangeAdminAsync(
        ReadOnlyMemory<byte> currentPinUtf8,
        ReadOnlyMemory<byte> newPinUtf8,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the Reset Code used for resetting the User PIN without the Admin PIN.
    /// </summary>
    Task SetResetCodeAsync(
        ReadOnlyMemory<byte> resetCodeUtf8,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the User PIN using either the Reset Code or Admin PIN privilege.
    /// </summary>
    /// <remarks>
    ///     When <paramref name="useAdmin" /> is <c>true</c>, the caller must have already
    ///     verified the Admin PIN via <see cref="VerifyAdminAsync" /> before calling this method.
    ///     The <paramref name="resetCodeUtf8" /> parameter is ignored in admin mode; only the
    ///     <paramref name="newPinUtf8" /> is sent to the card.
    /// </remarks>
    /// <param name="resetCodeUtf8">
    ///     The Reset Code as UTF-8 bytes (when <paramref name="useAdmin" /> is <c>false</c>).
    ///     Ignored when <paramref name="useAdmin" /> is <c>true</c>.
    /// </param>
    /// <param name="newPinUtf8">The new User PIN as UTF-8 encoded bytes.</param>
    /// <param name="useAdmin">
    ///     If <c>true</c>, assumes Admin PIN (PW3) has been verified and sends
    ///     RESET RETRY COUNTER with P1=0x02. If <c>false</c>, uses the Reset Code with P1=0x00.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ResetPinAsync(
        ReadOnlyMemory<byte> resetCodeUtf8,
        ReadOnlyMemory<byte> newPinUtf8,
        bool useAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the retry attempt counts for User PIN, Reset Code, and Admin PIN.
    /// </summary>
    Task SetPinAttemptsAsync(
        int userAttempts,
        int resetAttempts,
        int adminAttempts,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the signature PIN policy (whether PIN is required once per session or per signature).
    /// </summary>
    Task SetSignaturePinPolicyAsync(
        PinPolicy policy,
        CancellationToken cancellationToken = default);

    // ── Key Operations ────────────────────────────────────────────────

    /// <summary>
    ///     Generates an RSA key pair in the specified slot.
    /// </summary>
    Task GenerateRsaKeyAsync(
        KeyRef keyRef,
        RsaSize size = RsaSize.Rsa2048,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates an EC key pair in the specified slot.
    /// </summary>
    Task GenerateEcKeyAsync(
        KeyRef keyRef,
        CurveOid curve,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Imports a private key into the specified slot using a key template.
    /// </summary>
    /// <param name="keyRef">The target key slot.</param>
    /// <param name="template">The private key template containing key material.</param>
    /// <param name="attributes">
    ///     Optional algorithm attributes to set before import. If null, existing attributes are used.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task PutKeyAsync(
        KeyRef keyRef,
        PrivateKeyTemplate template,
        AlgorithmAttributes? attributes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes the key in the specified slot.
    /// </summary>
    Task DeleteKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reads the public key from the specified slot via GENERATE ASYMMETRIC KEY PAIR (read mode).
    /// </summary>
    Task<ReadOnlyMemory<byte>> GetPublicKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets an attestation certificate for the specified key slot. Requires firmware 5.2.0+.
    /// </summary>
    Task<X509Certificate2> AttestKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets key status information (none/generated/imported) for all slots.
    /// </summary>
    Task<KeyInformation> GetKeyInformationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the fingerprints for all key slots.
    /// </summary>
    Task<Fingerprints> GetFingerprintsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the generation timestamps for all key slots.
    /// </summary>
    Task<GenerationTimes> GetGenerationTimesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the fingerprint for the specified key slot.
    /// </summary>
    Task SetFingerprintAsync(
        KeyRef keyRef,
        ReadOnlyMemory<byte> fingerprint,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the generation timestamp for the specified key slot.
    /// </summary>
    Task SetGenerationTimeAsync(
        KeyRef keyRef,
        int timestamp,
        CancellationToken cancellationToken = default);

    // ── Certificate Operations ────────────────────────────────────────

    /// <summary>
    ///     Gets the certificate stored for the specified key slot.
    /// </summary>
    Task<X509Certificate2?> GetCertificateAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a certificate for the specified key slot.
    /// </summary>
    Task PutCertificateAsync(
        KeyRef keyRef,
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes the certificate for the specified key slot.
    /// </summary>
    Task DeleteCertificateAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    // ── Configuration ─────────────────────────────────────────────────

    /// <summary>
    ///     Gets the User Interaction Flag for the specified key slot.
    /// </summary>
    Task<Uif> GetUifAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the User Interaction Flag for the specified key slot.
    /// </summary>
    Task SetUifAsync(
        KeyRef keyRef,
        Uif uif,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the algorithm attributes for the specified key slot.
    /// </summary>
    Task<AlgorithmAttributes> GetAlgorithmAttributesAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the algorithm attributes for the specified key slot.
    /// </summary>
    Task SetAlgorithmAttributesAsync(
        KeyRef keyRef,
        AlgorithmAttributes attributes,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the list of supported algorithm attributes per key slot.
    ///     Requires firmware 5.2.0+.
    /// </summary>
    Task<IReadOnlyList<(KeyRef KeyRef, AlgorithmAttributes Attributes)>> GetAlgorithmInformationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the current KDF configuration.
    /// </summary>
    Task<Kdf> GetKdfAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the KDF configuration. Requires Admin PIN verification.
    /// </summary>
    Task SetKdfAsync(
        Kdf kdf,
        CancellationToken cancellationToken = default);

    // ── Cryptographic Operations ──────────────────────────────────────

    /// <summary>
    ///     Signs a message using the Signature key.
    /// </summary>
    Task<ReadOnlyMemory<byte>> SignAsync(
        ReadOnlyMemory<byte> message,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Decrypts a ciphertext using the Decryption key.
    /// </summary>
    Task<ReadOnlyMemory<byte>> DecryptAsync(
        ReadOnlyMemory<byte> ciphertext,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs internal authentication using the Authentication key.
    /// </summary>
    Task<ReadOnlyMemory<byte>> AuthenticateAsync(
        ReadOnlyMemory<byte> data,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default);

    // ── Factory Reset ─────────────────────────────────────────────────

    /// <summary>
    ///     Performs a factory reset of the OpenPGP application.
    ///     Blocks both PINs, terminates, and reactivates the applet.
    ///     Requires firmware 1.0.6+.
    /// </summary>
    Task ResetAsync(
        CancellationToken cancellationToken = default);
}