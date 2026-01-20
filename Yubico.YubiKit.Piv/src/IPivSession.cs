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

using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

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
public interface IPivSession : IApplicationSession, IAsyncDisposable
{
    /// <summary>PIV management key type currently in use.</summary>
    PivManagementKeyType ManagementKeyType { get; }

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
    /// <param name="pin">PIN as UTF-8 bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">PIN incorrect. Check RetriesRemaining property.</exception>
    Task VerifyPinAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify biometric authentication and optionally get temporary PIN.
    /// </summary>
    /// <param name="requestTemporaryPin">Request temporary PIN for subsequent operations.</param>
    /// <param name="checkOnly">Only check biometric without authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>16-byte temporary PIN if requested; null otherwise. Caller must zero returned bytes.</returns>
    /// <exception cref="NotSupportedException">Biometric authentication not available.</exception>
    Task<ReadOnlyMemory<byte>?> VerifyUvAsync(
        bool requestTemporaryPin = false,
        bool checkOnly = false,
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
    /// <param name="oldPin">Current PIN as UTF-8 bytes.</param>
    /// <param name="newPin">New PIN as UTF-8 bytes (6-8 ASCII characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">Old PIN incorrect.</exception>
    Task ChangePinAsync(ReadOnlyMemory<byte> oldPin, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change PUK from old PUK to new PUK.
    /// </summary>
    /// <param name="oldPuk">Current PUK as UTF-8 bytes.</param>
    /// <param name="newPuk">New PUK as UTF-8 bytes (6-8 ASCII characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">Old PUK incorrect.</exception>
    Task ChangePukAsync(ReadOnlyMemory<byte> oldPuk, ReadOnlyMemory<byte> newPuk, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblock PIN using PUK and set new PIN.
    /// </summary>
    /// <param name="puk">PUK as UTF-8 bytes.</param>
    /// <param name="newPin">New PIN as UTF-8 bytes (6-8 ASCII characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">PUK incorrect.</exception>
    Task UnblockPinAsync(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default);

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
    /// <param name="pinPolicy">PIN verification policy for key usage.</param>
    /// <param name="touchPolicy">Touch requirement policy for key usage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated public key.</returns>
    /// <exception cref="NotSupportedException">Algorithm not supported on this YubiKey version.</exception>
    Task<IPublicKey> GenerateKeyAsync(
        PivSlot slot,
        PivAlgorithm algorithm,
        PivPinPolicy pinPolicy = PivPinPolicy.Default,
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import private key into specified slot.
    /// </summary>
    /// <remarks>
    /// Requires management key authentication. Private key is NOT zeroed by this method.
    /// </remarks>
    /// <param name="slot">PIV slot for key storage.</param>
    /// <param name="privateKey">Private key to import.</param>
    /// <param name="pinPolicy">PIN verification policy for key usage.</param>
    /// <param name="touchPolicy">Touch requirement policy for key usage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Algorithm of imported key.</returns>
    Task<PivAlgorithm> ImportKeyAsync(
        PivSlot slot,
        IPrivateKey privateKey,
        PivPinPolicy pinPolicy = PivPinPolicy.Default,
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default,
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
    Task<ReadOnlyMemory<byte>> SignOrDecryptAsync(
        PivSlot slot,
        PivAlgorithm algorithm,
        ReadOnlyMemory<byte> data,
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
    /// automatically compressed unless compress=false is specified.
    /// </remarks>
    /// <param name="slot">PIV slot for certificate storage.</param>
    /// <param name="certificate">X.509 certificate to store.</param>
    /// <param name="compress">Force gzip compression for large certificates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreCertificateAsync(
        PivSlot slot,
        X509Certificate2 certificate,
        bool compress = false,
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

    // Management key

    /// <summary>
    /// Set new management key.
    /// </summary>
    /// <remarks>
    /// Requires current management key authentication. New key is NOT zeroed by this method.
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