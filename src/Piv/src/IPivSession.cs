// Copyright 2024 Yubico AB
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
using Yubico.YubiKit.Piv.DataObjects;

namespace Yubico.YubiKit.Piv;

/// <summary>
/// Interface for PIV (Personal Identity Verification) session operations.
/// </summary>
/// <remarks>
/// <para>
/// Implements NIST SP 800-73 PIV application functionality for smart card operations
/// including key generation, certificate management, and cryptographic operations.
/// </para>
/// </remarks>
public interface IPivSession : IApplicationSession
{
    /// <summary>PIV management key type currently in use.</summary>
    PivManagementKeyType ManagementKeyType { get; }

    /// <summary>
    ///     Gets a value indicating whether the session has authenticated the PIV management key.
    /// </summary>
    /// <remarks>
    ///     This is distinct from the inherited <see cref="IApplicationSession.IsAuthenticated"/>, which
    ///     represents application-protocol authentication such as SCP. Returns <c>false</c> once disposal begins.
    /// </remarks>
    bool IsManagementKeyAuthenticated { get; }

    /// <summary>Gets or sets a parameterless callback invoked before an operation may require touch.</summary>
    /// <remarks>
    ///     The callback intentionally receives no operation context so it cannot disclose the slot, algorithm,
    ///     or data involved. It must not call back into this session.
    /// </remarks>
    Action? OnTouchRequired { get; set; }

    // Session management

    /// <summary>
    /// Get the YubiKey serial number.
    /// </summary>
    /// <remarks>Requires YubiKey 5.0+.</remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>YubiKey serial number.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support serial number retrieval.</exception>
    Task<int> GetSerialNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset PIV application to factory defaults.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WARNING: This permanently destroys all PIV data, keys, and certificates.
    /// The operation requires that biometrics are not configured.
    /// </para>
    /// <para>
    /// A successful reset clears management-key authentication and refreshes
    /// <see cref="ManagementKeyType"/> from post-reset metadata. If metadata is unavailable, the
    /// session uses AES-192 only for a reliable firmware version 5.7 or later and conservatively
    /// falls back to Triple-DES for sentinel or older versions. Unexpected metadata errors propagate.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Biometrics are configured - cannot reset.</exception>
    Task ResetAsync(CancellationToken cancellationToken = default);

    // Authentication

    /// <summary>
    /// Authenticate with PIV management key to enable privileged operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The management key is NOT zeroed by this method - caller is responsible for secure disposal.
    /// Authentication enables key generation, import, certificate storage, and management operations.
    /// A failed authentication attempt clears any previously recorded management authentication state.
    /// </para>
    /// </remarks>
    /// <param name="managementKey">Management key bytes (24 bytes for 3DES, 16/24/32 for AES).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ApduException">Authentication failed - invalid key.</exception>
    Task AuthenticateAsync(ReadOnlyMemory<byte> managementKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify PIN to enable PIN-protected operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PIN must be 6-8 ASCII characters. PIN is NOT zeroed by this method - caller is responsible
    /// for secure disposal of PIN data.
    /// </para>
    /// </remarks>
    /// <param name="pinUtf8">PIN as UTF-8 bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">PIN incorrect. Check RetriesRemaining property.</exception>
    Task VerifyPinAsync(ReadOnlyMemory<byte> pinUtf8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify biometric authentication and optionally get temporary PIN.
    /// </summary>
    /// <param name="userVerification">The user-verification mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>16-byte temporary PIN if requested; null otherwise. Caller must zero returned bytes.</returns>
    /// <exception cref="NotSupportedException">Biometric authentication not available.</exception>
    Task<ReadOnlyMemory<byte>?> VerifyUvAsync(
        PivUserVerification userVerification = PivUserVerification.Verify,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify temporary PIN from biometric authentication.
    /// </summary>
    /// <remarks>
    /// Temporary PIN is NOT zeroed by this method - caller must zero it after use.
    /// </remarks>
    /// <param name="temporaryPin">16-byte temporary PIN from VerifyUvAsync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task VerifyTemporaryPinAsync(ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default);

    // PIN/PUK management

    /// <summary>
    /// Change PIN from old PIN to new PIN.
    /// </summary>
    /// <param name="currentPinUtf8">Current PIN as UTF-8 bytes.</param>
    /// <param name="newPinUtf8">New PIN as UTF-8 bytes (6-8 ASCII characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">Old PIN incorrect.</exception>
    Task ChangePinAsync(ReadOnlyMemory<byte> currentPinUtf8, ReadOnlyMemory<byte> newPinUtf8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change PUK from old PUK to new PUK.
    /// </summary>
    /// <param name="currentPukUtf8">Current PUK as UTF-8 bytes.</param>
    /// <param name="newPukUtf8">New PUK as UTF-8 bytes (6-8 ASCII characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">Old PUK incorrect.</exception>
    Task ChangePukAsync(ReadOnlyMemory<byte> currentPukUtf8, ReadOnlyMemory<byte> newPukUtf8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblock PIN using PUK and set new PIN.
    /// </summary>
    /// <param name="pukUtf8">PUK as UTF-8 bytes.</param>
    /// <param name="newPinUtf8">New PIN as UTF-8 bytes (6-8 ASCII characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">PUK incorrect.</exception>
    Task UnblockPinAsync(ReadOnlyMemory<byte> pukUtf8, ReadOnlyMemory<byte> newPinUtf8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set PIN and PUK retry limits.
    /// </summary>
    /// <param name="pinAttempts">PIN retry limit (1-255).</param>
    /// <param name="pukAttempts">PUK retry limit (1-255).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get remaining PIN attempts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of PIN attempts remaining before lockout.</returns>
    Task<int> GetPinAttemptsAsync(CancellationToken cancellationToken = default);

    // Key operations

    /// <summary>
    /// Generate new key pair in specified slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires management key authentication. RSA 4096 generation may take 30+ seconds.
    /// Generated private key cannot be exported but public key is returned for certificate generation.
    /// </para>
    /// </remarks>
    /// <param name="slot">PIV slot for key storage.</param>
    /// <param name="algorithm">Key algorithm and size.</param>
    /// <param name="options">Optional key-use policies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated public key.</returns>
    /// <exception cref="NotSupportedException">Algorithm not supported on this YubiKey version.</exception>
    Task<IPublicKey> GenerateKeyAsync(
        PivSlot slot,
        PivAlgorithm algorithm,
        PivKeyCreationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import private key into specified slot.
    /// </summary>
    /// <remarks>
    /// Requires management key authentication. Private key is NOT zeroed by this method.
    /// </remarks>
    /// <param name="slot">PIV slot for key storage.</param>
    /// <param name="privateKey">Private key to import.</param>
    /// <param name="options">Optional key-use policies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Algorithm of imported key.</returns>
    Task<PivAlgorithm> ImportKeyAsync(
        PivSlot slot,
        IPrivateKey privateKey,
        PivKeyCreationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move key from source slot to destination slot.
    /// </summary>
    /// <remarks>Requires YubiKey 5.7+ and management key authentication.</remarks>
    /// <param name="sourceSlot">Source slot containing key to move.</param>
    /// <param name="destinationSlot">Destination slot (must be empty).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotSupportedException">YubiKey does not support key movement.</exception>
    Task MoveKeyAsync(PivSlot sourceSlot, PivSlot destinationSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete key from specified slot.
    /// </summary>
    /// <remarks>Requires YubiKey 5.7+ and management key authentication.</remarks>
    /// <param name="slot">PIV slot to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotSupportedException">YubiKey does not support key deletion.</exception>
    Task DeleteKeyAsync(PivSlot slot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate attestation certificate for key in specified slot.
    /// </summary>
    /// <remarks>
    /// Requires YubiKey 4.3+ and key must exist in slot.
    /// Attestation proves the key was generated on-device and provides key metadata.
    /// </remarks>
    /// <param name="slot">PIV slot containing key to attest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>X.509 attestation certificate.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support attestation.</exception>
    Task<X509Certificate2> AttestKeyAsync(PivSlot slot, CancellationToken cancellationToken = default);

    // Cryptographic operations

    /// <summary>
    /// Sign data or decrypt data using private key in specified slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Operation depends on key algorithm:
    /// - RSA: PKCS#1 v1.5 padding for both sign and decrypt
    /// - ECDSA: Sign hash directly (caller must hash data)
    /// - EdDSA: Sign message directly
    /// - X25519: Not supported (use CalculateSecretAsync)
    /// </para>
    /// <para>
    /// May require PIN verification or touch based on key policy.
    /// </para>
    /// </remarks>
    /// <param name="slot">PIV slot containing private key.</param>
    /// <param name="algorithm">Key algorithm (must match slot contents).</param>
    /// <param name="data">Data to sign or decrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Signature or decrypted data.</returns>
    // Both declarations are established alpha entry points: explicit and metadata-driven algorithms.
#pragma warning disable RS0026
    Task<ReadOnlyMemory<byte>> SignOrDecryptAsync(
        PivSlot slot,
        PivAlgorithm algorithm,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign data or decrypt data using private key in specified slot, auto-detecting the algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Requires YubiKey firmware 5.3+.</b> This overload queries slot metadata to determine
    /// the key algorithm automatically, eliminating the need to track algorithms separately.
    /// </para>
    /// <para>
    /// For YubiKeys with firmware older than 5.3, use the overload that accepts an explicit
    /// algorithm parameter.
    /// </para>
    /// <para>
    /// May require PIN verification or touch based on key policy.
    /// </para>
    /// </remarks>
    /// <param name="slot">PIV slot containing private key.</param>
    /// <param name="data">Data to sign or decrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Signature or decrypted data.</returns>
    /// <exception cref="NotSupportedException">YubiKey firmware is older than 5.3 and does not support metadata retrieval.</exception>
    /// <exception cref="InvalidOperationException">Slot is empty (no key present).</exception>
    Task<ReadOnlyMemory<byte>> SignOrDecryptAsync(
        PivSlot slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);
#pragma warning restore RS0026

    /// <summary>
    /// Decrypts RSA cipher text and removes padding, returning clean plaintext.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the high-level decrypt API, modelled after the Python yubikey-manager SDK.
    /// It performs the raw RSA private key operation via the YubiKey, then strips the
    /// specified padding scheme to return the original plaintext.
    /// </para>
    /// <para>
    /// Supported padding schemes:
    /// - <see cref="RSAEncryptionPadding.Pkcs1"/> — PKCS#1 v1.5 (most common)
    /// - <see cref="RSAEncryptionPadding.OaepSHA1"/> / OaepSHA256 etc. — OAEP
    /// </para>
    /// <para>
    /// Slot must contain an RSA key. May require PIN verification before calling.
    /// </para>
    /// </remarks>
    /// <param name="slot">PIV slot containing RSA private key.</param>
    /// <param name="cipherText">RSA-encrypted cipher text (length must match key size).</param>
    /// <param name="padding">Padding scheme used when encrypting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Decrypted plaintext with padding removed.</returns>
    /// <exception cref="ArgumentException">Cipher text length does not match RSA key size, or slot has no RSA key.</exception>
    /// <exception cref="CryptographicException">Padding is malformed or decryption failed.</exception>
    Task<ReadOnlyMemory<byte>> DecryptAsync(
        PivSlot slot,
        ReadOnlyMemory<byte> cipherText,
        RSAEncryptionPadding padding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate ECDH shared secret with peer public key.
    /// </summary>
    /// <remarks>
    /// Slot must contain EC or X25519 private key. May require PIN verification or touch.
    /// </remarks>
    /// <param name="slot">PIV slot containing EC private key.</param>
    /// <param name="peerPublicKey">Peer's public key for ECDH.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw shared secret (x-coordinate for ECDH, 32 bytes for X25519).</returns>
    Task<ReadOnlyMemory<byte>> CalculateSecretAsync(
        PivSlot slot,
        IPublicKey peerPublicKey,
        CancellationToken cancellationToken = default);

    // Certificate management

    /// <summary>
    /// Get certificate stored in specified slot.
    /// </summary>
    /// <param name="slot">PIV slot to read certificate from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>X.509 certificate or null if slot is empty.</returns>
    Task<X509Certificate2?> GetCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store certificate in specified slot.
    /// </summary>
    /// <remarks>
    /// Requires management key authentication. Certificates larger than 1856 bytes are
    /// automatically compressed.
    /// </remarks>
    /// <param name="slot">PIV slot for certificate storage.</param>
    /// <param name="certificate">X.509 certificate to store.</param>
    /// <param name="compression">Certificate compression policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreCertificateAsync(
        PivSlot slot,
        X509Certificate2 certificate,
        PivCertificateCompression compression = PivCertificateCompression.Automatic,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete certificate from specified slot.
    /// </summary>
    /// <remarks>Requires management key authentication. Idempotent - no error if slot already empty.</remarks>
    /// <param name="slot">PIV slot to clear certificate from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default);

    // Metadata (YubiKey 5.3+)

    /// <summary>
    /// Get PIN metadata information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PIN metadata.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support metadata retrieval.</exception>
    Task<PivPinMetadata> GetPinMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get PUK metadata information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PUK metadata.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support metadata retrieval.</exception>
    Task<PivPukMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get management key metadata information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Management key metadata.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support metadata retrieval.</exception>
    Task<PivManagementKeyMetadata> GetManagementKeyMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get slot metadata information.
    /// </summary>
    /// <param name="slot">PIV slot to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Slot metadata or null if slot is empty.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support metadata retrieval.</exception>
    Task<PivSlotMetadata?> GetSlotMetadataAsync(PivSlot slot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get biometric metadata information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Biometric metadata.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support biometrics.</exception>
    Task<PivBioMetadata> GetBioMetadataAsync(CancellationToken cancellationToken = default);

    // Data objects

    /// <summary>
    /// Read PIV data object.
    /// </summary>
    /// <param name="objectId">PIV data object identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Object data or empty if object does not exist.</returns>
    Task<ReadOnlyMemory<byte>> GetObjectAsync(int objectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write PIV data object.
    /// </summary>
    /// <remarks>Requires management key authentication. Pass null data to delete object.</remarks>
    /// <param name="objectId">PIV data object identifier.</param>
    /// <param name="data">Object data or null to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PutObjectAsync(int objectId, ReadOnlyMemory<byte>? data, CancellationToken cancellationToken = default);

    // Typed data objects

    /// <summary>
    /// Get the typed CHUID (CardHolder Unique Identifier) object.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CHUID, or <see cref="PivCardholderUniqueId.Empty"/> if not present.</returns>
    /// <exception cref="ApduException">The stored object is not encoded as a valid CHUID.</exception>
    Task<PivCardholderUniqueId> GetCardholderUniqueIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the typed CHUID (CardHolder Unique Identifier) object.
    /// </summary>
    /// <remarks>Requires management key authentication.</remarks>
    /// <param name="cardholderUniqueId">The CHUID to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetCardholderUniqueIdAsync(PivCardholderUniqueId cardholderUniqueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the typed CCC (Card Capability Container) object.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CCC, or <see cref="PivCardCapabilityContainer.Empty"/> if not present.</returns>
    /// <exception cref="ApduException">The stored object is not encoded as a valid CCC.</exception>
    Task<PivCardCapabilityContainer> GetCardCapabilityContainerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the typed CCC (Card Capability Container) object.
    /// </summary>
    /// <remarks>Requires management key authentication.</remarks>
    /// <param name="cardCapabilityContainer">The CCC to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetCardCapabilityContainerAsync(PivCardCapabilityContainer cardCapabilityContainer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the typed ADMIN DATA object, which records PIN-only mode state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ADMIN DATA, or <see cref="PivAdminData.Empty"/> if not present.</returns>
    /// <exception cref="ApduException">The stored object is not encoded as valid ADMIN DATA.</exception>
    Task<PivAdminData> GetAdminDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the typed ADMIN DATA object.
    /// </summary>
    /// <remarks>
    /// Requires management key authentication. Most callers should use
    /// <see cref="SetPinOnlyModeAsync"/> instead of writing ADMIN DATA directly.
    /// </remarks>
    /// <param name="adminData">The ADMIN DATA to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAdminDataAsync(PivAdminData adminData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the typed Key History object.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Key History, or <see cref="PivKeyHistory.Empty"/> if not present.</returns>
    /// <exception cref="ApduException">The stored object is not encoded as a valid Key History.</exception>
    Task<PivKeyHistory> GetKeyHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the typed Key History object.
    /// </summary>
    /// <remarks>Requires management key authentication.</remarks>
    /// <param name="keyHistory">The Key History to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetKeyHistoryAsync(PivKeyHistory keyHistory, CancellationToken cancellationToken = default);

    // PIN-only mode

    /// <summary>
    /// Detect whether the management key is currently PIN-protected and/or PIN-derived, based on
    /// the contents of the ADMIN DATA object.
    /// </summary>
    /// <remarks>
    /// This does not authenticate the management key; it only inspects ADMIN DATA. If ADMIN DATA
    /// is present but not encoded as expected (for example, overwritten by another application),
    /// both <see cref="PivPinOnlyMode.PinProtectedUnavailable"/> and
    /// <see cref="PivPinOnlyMode.PinDerivedUnavailable"/> are set.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detected PIN-only mode.</returns>
    Task<PivPinOnlyMode> GetPinOnlyModeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempt to authenticate the management key using PIN-protected and/or PIN-derived data
    /// already stored on the YubiKey.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This tries the PIN-protected management key stored in the PRINTED object first, verifying
    /// the PIN and retrying the read once if PRINTED is PIN-gated, then attempts to derive a
    /// management key from the given PIN and the salt stored in ADMIN DATA. A successful PIN
    /// verification is reused between both paths. The session's authenticated state is updated
    /// for whichever mode(s) succeed.
    /// </para>
    /// <para>
    /// If a later PIN-derived candidate fails after PIN-protected authentication succeeded, the
    /// PIN-protected key is reauthenticated before that success is returned. If restoration fails,
    /// the method returns <see cref="PivPinOnlyMode.None"/> and leaves the session unauthenticated.
    /// </para>
    /// <para>
    /// The PIN-derived candidate key is discarded and zeroed after each attempt; it is never
    /// retained by the session.
    /// </para>
    /// </remarks>
    /// <param name="pinUtf8">PIN as UTF-8 bytes, used if PRINTED is PIN-gated or PIN-derived data is present.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PIN-only mode(s) successfully authenticated. <see cref="PivPinOnlyMode.None"/> if neither succeeded.</returns>
    /// <exception cref="InvalidPinException">PIN verification is required and the supplied PIN is incorrect.</exception>
    Task<PivPinOnlyMode> RecoverPinOnlyModeAsync(ReadOnlyMemory<byte> pinUtf8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable or disable PIN-protected management key mode.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires the management key to already be authenticated (<see cref="AuthenticateAsync"/>).
    /// Only <see cref="PivPinOnlyMode.None"/> (disable) and <see cref="PivPinOnlyMode.PinProtected"/>
    /// (enable) are supported; PIN-derived management keys are a deprecated, weaker mechanism and
    /// cannot be enabled through this method (existing PIN-derived state can still be detected via
    /// <see cref="GetPinOnlyModeAsync"/> and recovered via <see cref="RecoverPinOnlyModeAsync"/>).
    /// </para>
    /// <para>
    /// Enabling PIN-protected mode first authenticates <paramref name="managementKey"/> as the
    /// active key for <see cref="ManagementKeyType"/>, then verifies the PIN, stores the key in the
    /// PRINTED object, blocks the PUK, and updates ADMIN DATA. Disabling first resets the management
    /// key to the type-appropriate well-known default, then clears PRINTED and ADMIN DATA in that
    /// order. Disabling is a no-op if neither PIN-protected nor PIN-derived mode is currently set.
    /// </para>
    /// <para><paramref name="managementKey"/> is NOT zeroed by this method - caller is responsible for secure disposal.</para>
    /// </remarks>
    /// <param name="pinOnlyMode"><see cref="PivPinOnlyMode.None"/> to disable, or <see cref="PivPinOnlyMode.PinProtected"/> to enable.</param>
    /// <param name="pinUtf8">PIN as UTF-8 bytes. Required (and verified) when enabling PIN-protected mode; ignored when disabling.</param>
    /// <param name="managementKey">
    /// The active management key to authenticate and protect. Required when enabling PIN-protected mode;
    /// its length must match <see cref="ManagementKeyType"/>. Ignored when disabling.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">The management key is not authenticated.</exception>
    /// <exception cref="ArgumentException"><paramref name="pinOnlyMode"/> requests an unsupported mode, or the management-key length does not match <see cref="ManagementKeyType"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="managementKey"/> is required but not supplied.</exception>
    /// <exception cref="InvalidPinException">The supplied PIN is incorrect.</exception>
    Task SetPinOnlyModeAsync(
        PivPinOnlyMode pinOnlyMode,
        ReadOnlyMemory<byte> pinUtf8,
        ReadOnlyMemory<byte>? managementKey = null,
        CancellationToken cancellationToken = default);

    // Management key

    /// <summary>
    /// Set new management key.
    /// </summary>
    /// <remarks>
    /// Requires current management key authentication. After success, <see cref="ManagementKeyType"/>
    /// reflects <paramref name="keyType"/> and management authentication remains active for the newly
    /// installed key in the card session. The session does not retain key bytes. New key is NOT zeroed
    /// by this method. If the device reports that the security status is not satisfied, the session
    /// clears its recorded management authentication state; other command failures preserve it.
    /// </remarks>
    /// <param name="keyType">Management key algorithm.</param>
    /// <param name="newKey">New management key bytes.</param>
    /// <param name="requireTouch">Require touch for management key operations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetManagementKeyAsync(
        PivManagementKeyType keyType,
        ReadOnlyMemory<byte> newKey,
        bool requireTouch = false,
        CancellationToken cancellationToken = default);
}