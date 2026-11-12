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
/// Provides PIV smart-card operations and Yubico extensions, including key generation,
/// certificate management and cryptographic operations. Algorithm availability alone is not
/// a claim of conformance to a federal PIV deployment profile.
/// </para>
/// <para>Credential inputs are caller-owned buffers. PIN/PUK inputs are UTF-8 bytes; byte-length
/// checks do not imply ASCII-only, digit-only, or UTF-8-validity checks. Returned mutable memory is
/// transferred to the caller, who must clear plaintext, shared secrets, and temporary PINs after use.</para>
/// <para>
/// Await and serialize complete public tasks before another operation or disposal. Core guards one logical
/// APDU exchange, including chaining/SCP, rather than a complete management-authentication, PIN-only or reset
/// workflow. Parsing, presence callbacks and local RSA unpadding can continue after that exchange leaves the
/// guard. Keep borrowed inputs, keys and prompt services alive through actual public-task completion.
/// Cancellation is checked before each exchange is admitted; admitted constituent traffic uses
/// CancellationToken.None and may succeed after the caller cancels. An outer canceled wait is not operation
/// completion or permission to clear inputs. Await actual completion/teardown before inspecting or retrying
/// a possibly persistent change.
/// </para>
/// </remarks>
public interface IPivSession : IApplicationSession
{
    /// <summary>Recognized management key type, or null for an unmodeled algorithm; inspect raw management-key metadata.</summary>
    KeyType? ManagementKeyType { get; }

    /// <summary>
    ///     Gets a value indicating whether the session has authenticated the PIV management key.
    /// </summary>
    /// <remarks>
    ///     This is distinct from the inherited <see cref="IApplicationSession.IsAuthenticated"/>, which
    ///     represents application-protocol authentication such as SCP. Returns <c>false</c> once disposal begins.
    ///     It is a local observation; the card owns current administration and PIN state. SCP setup grants
    ///     neither PIN verification nor management-key authorization.
    /// </remarks>
    bool IsManagementKeyAuthenticated { get; }

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
    /// The SDK blocks the PIN and PUK, then resets normal key slots, certificates, objects and
    /// credentials. The F9 attestation key and certificate survive normal PIV reset. Management
    /// authentication is unnecessary. Failure or cancellation before RESET can leave retries consumed.
    /// Bio MPE requires its device-reset procedure; the PIV Bio preflight is unfinished.
    /// </para>
    /// <para>
    /// A successful reset clears management-key authentication and refreshes
    /// <see cref="ManagementKeyType"/> from post-reset metadata. If metadata is unavailable, the
    /// session uses detected firmware (not a compatibility override): AES-192 for 5.7+ and development
    /// versions, or Triple-DES for older released versions. Unexpected metadata errors propagate,
    /// including after the physical reset has succeeded. Inspect fresh device state before retrying.
    /// The method retains the existing backend/protocol, without reselection or a new SCP handshake;
    /// its authentication flag alone establishes no post-reset secure-channel restoration.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ApduException">The device rejects reset.</exception>
    Task ResetAsync(CancellationToken cancellationToken = default);

    // Authentication

    /// <summary>
    /// Authenticate with PIV management key to enable privileged operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The caller retains ownership of the management-key buffer and clears it after use.
    /// Authentication enables key generation, import, certificate storage, and management operations.
    /// A failed authentication attempt clears any previously recorded management authentication state.
    /// The witness/challenge exchange uses two separately guarded sends. Keep this buffer valid and
    /// serialize the full method, rather than interleaving another session operation between them.
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
    /// Supply 6–8 encoded bytes. SDK validation checks length; the device/product profile governs
    /// credential content. The caller retains the input buffer and clears it after use. The referenced
    /// SP 800-73-4 Part 2 §2.4.3 profile specifies ASCII decimal PINs; UTF-8 byte acceptance is an SDK mechanism.
    /// PIN verification is card-controlled state, not a managed session flag. Disposing a session that
    /// borrows a connection does not close that connection or explicitly clear the card's PIN state.
    /// </para>
    /// </remarks>
    /// <param name="pin">PIN as UTF-8 encoded bytes. The caller owns and must clear this input buffer; the SDK never clears it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">PIN incorrect. Check RetriesRemaining property.</exception>
    Task VerifyPinAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify biometric authentication and optionally get temporary PIN.
    /// </summary>
    /// <param name="userVerification">The user-verification mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>16-byte temporary PIN if requested; null otherwise. Caller must zero returned bytes.</returns>
    /// <exception cref="NotSupportedException">Biometric authentication not available.</exception>
    Task<Memory<byte>?> VerifyUvAsync(
        UserVerification userVerification = UserVerification.Verify,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify temporary PIN from biometric authentication.
    /// </summary>
    /// <remarks>
    /// The caller owns the temporary-PIN buffer and clears it after the awaited operation.
    /// </remarks>
    /// <param name="temporaryPin">16-byte temporary PIN from VerifyUvAsync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task VerifyTemporaryPinAsync(ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default);

    // PIN/PUK management

    /// <summary>
    /// Change PIN from old PIN to new PIN.
    /// </summary>
    /// <remarks>Both inputs must contain 6–8 encoded bytes. Changing the PIN leaves legacy
    /// PIN-derived management-key recovery data unchanged; coordinate that migration separately.</remarks>
    /// <param name="currentPin">Current PIN as UTF-8 encoded bytes. The caller owns and must clear this input buffer; the SDK never clears it.</param>
    /// <param name="newPin">New PIN as UTF-8 encoded bytes. The caller owns and must clear this input buffer; the SDK never clears it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">Old PIN incorrect.</exception>
    Task ChangePinAsync(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change PUK from old PUK to new PUK.
    /// </summary>
    /// <remarks>The pair encoder accepts up to eight bytes per input; the device enforces further
    /// policy. A blocked PUK requires a separate recovery/reset decision.</remarks>
    /// <param name="currentPuk">Current PUK as UTF-8 encoded bytes. The caller owns and must clear this input buffer; the SDK never clears it.</param>
    /// <param name="newPuk">New PUK as UTF-8 encoded bytes. The caller owns and must clear this input buffer; the SDK never clears it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">Old PUK incorrect.</exception>
    Task ChangePukAsync(ReadOnlyMemory<byte> currentPuk, ReadOnlyMemory<byte> newPuk, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblock PIN using PUK and set new PIN.
    /// </summary>
    /// <remarks>Requires a usable PUK. The pair encoder accepts up to eight bytes per input;
    /// the device enforces further policy. This updates the PIN without synchronizing PIN-only objects.</remarks>
    /// <param name="puk">PUK as UTF-8 encoded bytes. The caller owns and must clear this input buffer; the SDK never clears it.</param>
    /// <param name="newPin">New PIN as UTF-8 encoded bytes. The caller owns and must clear this input buffer; the SDK never clears it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidPinException">PUK incorrect.</exception>
    Task UnblockPinAsync(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the PIN and PUK retry limits (Yubico SET PIN RETRIES, INS FA). Requires management key
    ///     authentication (<see cref="AuthenticateAsync" />) and a verified PIN (<see cref="VerifyPinAsync" />);
    ///     the card refuses the command without both.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>This resets both credentials.</b> The YubiKey sets the PIN back to <c>123456</c> and the PUK
    ///         to <c>12345678</c> every time this command runs, even when the limits do not change. Follow it
    ///         with <see cref="ChangePinAsync" /> and <see cref="ChangePukAsync" /> to set real values. The
    ///         three commands are separate. A failure during the subsequent changes can leave one or
    ///         both credentials at their defaults; inspect state and complete the remaining changes
    ///         before deployment.
    ///     </para>
    /// </remarks>
    /// <param name="pinAttempts">PIN retry limit (1-255).</param>
    /// <param name="pukAttempts">PUK retry limit (1-255).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get remaining PIN attempts.
    /// </summary>
    /// <remarks>Uses metadata when available, falling back only when the metadata instruction is
    /// unsupported. The no-data VERIFY query consumes no retries. Its 63CX status carries at most 15;
    /// success without a count raises <see cref="InvalidOperationException"/>. Metadata can report larger counts.</remarks>
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
    /// The device retains the private key and returns its public key for enrollment.
    /// Generating a replacement key leaves any separately stored certificate unchanged.
    /// </para>
    /// </remarks>
    /// <param name="slot">PIV slot for key storage.</param>
    /// <param name="keyType">Shared key type, including key size.</param>
    /// <param name="options">Optional key-use policies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated public key.</returns>
    /// <exception cref="NotSupportedException">Key type not supported on this YubiKey version.</exception>
    Task<IPublicKey> GenerateKeyAsync(
        Slot slot,
        KeyType keyType,
        KeyCreationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import private key into specified slot.
    /// </summary>
    /// <remarks>
    /// Requires management-key authentication. The caller keeps <paramref name="privateKey"/> alive
    /// through the awaited operation and disposes it afterward. RSA import requires exponent 65537.
    /// Key import leaves separately stored certificates unchanged.
    /// </remarks>
    /// <param name="slot">PIV slot for key storage.</param>
    /// <param name="privateKey">Private key to import.</param>
    /// <param name="options">Optional key-use policies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes after the device imports the key. Its type is already available on the caller's private key.</returns>
    Task ImportKeyAsync(
        Slot slot,
        IPrivateKey privateKey,
        KeyCreationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move key from source slot to destination slot.
    /// </summary>
    /// <remarks>Requires YubiKey 5.7+ and management-key authentication. Move between distinct slots
    /// with an empty destination; F9 is excluded. Certificates remain in their original storage locations.</remarks>
    /// <param name="sourceSlot">Source slot containing key to move.</param>
    /// <param name="destinationSlot">Destination slot (must be empty).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotSupportedException">YubiKey does not support key movement.</exception>
    Task MoveKeyAsync(Slot sourceSlot, Slot destinationSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete key from specified slot.
    /// </summary>
    /// <remarks>Requires YubiKey 5.7+ and management-key authentication. The separately stored
    /// certificate remains. Deleting F9 material is a separate, potentially irreversible provisioning decision.</remarks>
    /// <param name="slot">PIV slot to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotSupportedException">YubiKey does not support key deletion.</exception>
    Task DeleteKeyAsync(Slot slot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate attestation certificate for key in specified slot.
    /// </summary>
    /// <remarks>
    /// Requires YubiKey 4.3+ and an on-device-generated key. The caller validates the signer chain
    /// against its trust policy and binds the attested public key to the intended slot. Retrieval alone
    /// supplies no trust decision. The caller owns and disposes the returned certificate.
    /// </remarks>
    /// <param name="slot">PIV slot containing key to attest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>X.509 attestation certificate.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support attestation.</exception>
    Task<X509Certificate2> AttestKeyAsync(Slot slot, CancellationToken cancellationToken = default);

    // Cryptographic operations

    /// <summary>
    /// Sign data or decrypt data using private key in specified slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The YubiKey performs the raw private-key operation; what <paramref name="data" /> must contain
    /// depends on the algorithm:
    /// </para>
    /// <list type="bullet">
    /// <item><description>RSA: a complete representative, at most the modulus length. For signing,
    /// prepare EMSA-PKCS1-v1_5 or EMSA-PSS on the host; for decryption, pass the ciphertext.
    /// The SDK left-pads shorter inputs with zeros and rejects oversized inputs. It returns a
    /// modulus-sized raw result; use <see cref="DecryptAsync"/> for host encryption-padding removal.</description></item>
    /// <item><description>ECDSA: a digest. Longer digests use the leftmost curve-order bytes
    /// (FIPS 186-5 §6.4.1); shorter digests are zero-left-padded to preserve their integer value.
    /// The result is ASN.1 DER SEQUENCE(r,s), suitable for Rfc3279DerSequence host verification.</description></item>
    /// <item><description>Ed25519: the original message, passed through unchanged (RFC 8032 PureEdDSA).
    /// The device governs message limits. The result is a raw 64-byte signature.</description></item>
    /// <item><description>X25519 agreement uses <see cref="CalculateSecretAsync"/>.</description></item>
    /// </list>
    /// <para>
    /// May require PIN verification or touch based on key policy.
    /// </para>
    /// </remarks>
    /// <param name="slot">PIV slot containing private key.</param>
    /// <param name="keyType">Shared key type (must match slot contents).</param>
    /// <param name="data">Data to sign or decrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Caller-owned mutable signature or raw RSA output. Clear sensitive results after use.</returns>
    // Both declarations are established alpha entry points: explicit and metadata-driven algorithms.
#pragma warning disable RS0026
    Task<Memory<byte>> SignOrDecryptAsync(
        Slot slot,
        KeyType keyType,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign data or decrypt data using private key in specified slot, auto-detecting the key type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Requires YubiKey firmware 5.3+.</b> This overload queries slot metadata to determine
    /// the key type automatically, eliminating the need to track key types separately.
    /// </para>
    /// <para>
    /// For YubiKeys with firmware older than 5.3, use the overload that accepts an explicit
    /// keyType parameter.
    /// </para>
    /// <para>
    /// May require PIN verification or touch based on key policy.
    /// </para>
    /// </remarks>
    /// <param name="slot">PIV slot containing private key.</param>
    /// <param name="data">Data to sign or decrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Caller-owned mutable signature or raw RSA output. Formats follow the explicit-key-type
    /// overload: ECDSA digest/DER, Ed25519 message/raw signature, RSA complete representative/raw result.</returns>
    /// <exception cref="NotSupportedException">YubiKey firmware is older than 5.3 and does not support metadata retrieval.</exception>
    /// <exception cref="InvalidOperationException">Slot is empty (no key present).</exception>
    Task<Memory<byte>> SignOrDecryptAsync(
        Slot slot,
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
    /// Select <see cref="RSAEncryptionPadding.Pkcs1"/> or the OAEP hash matching the sender.
    /// Slot metadata is queried first, requiring firmware 5.3+. A successful device/touch completion
    /// can precede a local padding error. The caller owns and clears the mutable plaintext after use.
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
    Task<Memory<byte>> DecryptAsync(
        Slot slot,
        ReadOnlyMemory<byte> cipherText,
        RSAEncryptionPadding padding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate ECDH shared secret with peer public key.
    /// </summary>
    /// <remarks>
    /// Slot must contain a P-256, P-384 or X25519 private key matching the peer's type.
    /// May require PIN verification or touch. The caller authenticates the peer and applies its
    /// protocol's KDF to the raw result, then clears the caller-owned mutable secret.
    /// </remarks>
    /// <param name="slot">PIV slot containing EC private key.</param>
    /// <param name="peerPublicKey">Peer's public key for ECDH.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw shared secret (x-coordinate for ECDH, 32 bytes for X25519).</returns>
    Task<Memory<byte>> CalculateSecretAsync(
        Slot slot,
        IPublicKey peerPublicKey,
        CancellationToken cancellationToken = default);

    // Certificate management

    /// <summary>
    /// Get certificate stored in specified slot.
    /// </summary>
    /// <param name="slot">PIV slot to read certificate from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A caller-owned certificate to dispose, or null when usable certificate data is absent.
    /// Private-key occupancy is independent of certificate storage.</returns>
    Task<X509Certificate2?> GetCertificateAsync(Slot slot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store certificate in specified slot.
    /// </summary>
    /// <remarks>
    /// Requires management-key authentication. Automatic compression uses GZip when DER exceeds
    /// 1856 bytes; this SDK threshold is a recommended PIV length, not a hard storage maximum.
    /// The caller retains the certificate. Validate its public-key binding to the slot before enrollment;
    /// storage itself performs no binding check.
    /// </remarks>
    /// <param name="slot">PIV slot for certificate storage.</param>
    /// <param name="certificate">X.509 certificate to store.</param>
    /// <param name="compression">Certificate compression policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreCertificateAsync(
        Slot slot,
        X509Certificate2 certificate,
        CertificateCompression compression = CertificateCompression.Automatic,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete certificate from specified slot.
    /// </summary>
    /// <remarks>Requires management-key authentication. Deleting absent certificate data succeeds.
    /// The private key remains in the slot.</remarks>
    /// <param name="slot">PIV slot to clear certificate from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteCertificateAsync(Slot slot, CancellationToken cancellationToken = default);

    // Metadata (YubiKey 5.3+)

    /// <summary>
    /// Get PIN metadata information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PIN metadata.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support metadata retrieval.</exception>
    Task<PinMetadata> GetPinMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get PUK metadata information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PUK metadata.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support metadata retrieval.</exception>
    Task<PukMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get management key metadata information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Management key metadata.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support metadata retrieval.</exception>
    Task<ManagementKeyMetadata> GetManagementKeyMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get slot metadata information.
    /// </summary>
    /// <param name="slot">PIV slot to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Slot metadata or null if slot is empty.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support metadata retrieval.</exception>
    Task<SlotMetadata?> GetSlotMetadataAsync(Slot slot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get biometric metadata information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Biometric metadata.</returns>
    /// <exception cref="NotSupportedException">YubiKey does not support biometrics.</exception>
    Task<BioMetadata> GetBioMetadataAsync(CancellationToken cancellationToken = default);

    // Data objects

    /// <summary>
    /// Read PIV data object.
    /// </summary>
    /// <param name="objectId">PIV object identifier. Prefer defined DataObject values;
    /// arbitrary integer requests currently reach the encoder without a range guard.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Caller-owned mutable inner object bytes, excluding the outer 53 wrapper, or empty
    /// if absent. Clear sensitive contents after use; public provisioning data needs no secret cleanup.</returns>
    Task<Memory<byte>> GetObjectAsync(int objectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write PIV data object.
    /// </summary>
    /// <remarks>Requires management-key authentication. Pass null data to delete an object.
    /// The caller retains the input buffer through the awaited operation and clears sensitive contents afterward.
    /// Prefer defined DataObject identifiers; the current encoder has no arbitrary-integer range guard.</remarks>
    /// <param name="objectId">PIV data object identifier.</param>
    /// <param name="data">Object data or null to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PutObjectAsync(int objectId, ReadOnlyMemory<byte>? data, CancellationToken cancellationToken = default);

    // Typed data objects

    /// <summary>
    /// Get the typed CHUID (CardHolder Unique Identifier) object.
    /// </summary>
    /// <remarks>Reads the SDK's fixed provisioning shape. Inspect other valid encodings with GetObjectAsync.</remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CHUID, or <see cref="CardholderUniqueId.Empty"/> if not present.</returns>
    /// <exception cref="ApduException">The stored object is not encoded as a valid CHUID.</exception>
    Task<CardholderUniqueId> GetCardholderUniqueIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the typed CHUID (CardHolder Unique Identifier) object.
    /// </summary>
    /// <remarks>Requires management key authentication.</remarks>
    /// <param name="cardholderUniqueId">The CHUID to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetCardholderUniqueIdAsync(CardholderUniqueId cardholderUniqueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the typed CCC (Card Capability Container) object.
    /// </summary>
    /// <remarks>Reads the SDK's fixed provisioning shape. Inspect other valid encodings with GetObjectAsync.</remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CCC, or <see cref="CardCapabilityContainer.Empty"/> if not present.</returns>
    /// <exception cref="ApduException">The stored object is not encoded as a valid CCC.</exception>
    Task<CardCapabilityContainer> GetCardCapabilityContainerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the typed CCC (Card Capability Container) object.
    /// </summary>
    /// <remarks>Requires management key authentication.</remarks>
    /// <param name="cardCapabilityContainer">The CCC to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetCardCapabilityContainerAsync(CardCapabilityContainer cardCapabilityContainer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the typed ADMIN DATA object, which records PIN-only mode state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ADMIN DATA, or <see cref="AdminData.Empty"/> if not present.</returns>
    /// <exception cref="ApduException">The stored object is not encoded as valid ADMIN DATA.</exception>
    Task<AdminData> GetAdminDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the typed ADMIN DATA object.
    /// </summary>
    /// <remarks>
    /// Requires management key authentication. Most callers should use
    /// <see cref="SetPinOnlyModeAsync"/> instead of writing ADMIN DATA directly.
    /// </remarks>
    /// <param name="adminData">The ADMIN DATA to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAdminDataAsync(AdminData adminData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the typed Key History object.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Key History, or <see cref="KeyHistory.Empty"/> if not present.</returns>
    /// <exception cref="ApduException">The stored object is not encoded as a valid Key History.</exception>
    Task<KeyHistory> GetKeyHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the typed Key History object.
    /// </summary>
    /// <remarks>Requires management key authentication.</remarks>
    /// <param name="keyHistory">The Key History to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetKeyHistoryAsync(KeyHistory keyHistory, CancellationToken cancellationToken = default);

    // PIN-only mode

    /// <summary>
    /// Detect whether the management key is currently PIN-protected and/or PIN-derived, based on
    /// the contents of the ADMIN DATA object.
    /// </summary>
    /// <remarks>
    /// This inspects ADMIN DATA while leaving authentication unchanged. If ADMIN DATA
    /// is present but not encoded as expected (for example, overwritten by another application),
    /// both <see cref="PinOnlyMode.PinProtectedUnavailable"/> and
    /// <see cref="PinOnlyMode.PinDerivedUnavailable"/> are set.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detected PIN-only mode.</returns>
    Task<PinOnlyMode> GetPinOnlyModeAsync(CancellationToken cancellationToken = default);

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
    /// the method returns <see cref="PinOnlyMode.None"/> and leaves the session unauthenticated.
    /// </para>
    /// <para>
    /// The PIN-derived candidate key is discarded and zeroed after each attempt; it is never
    /// retained by the session.
    /// When no candidate is available, a None result can preserve preexisting authentication.
    /// Only a security-status 6982 response rejects a candidate; unrelated failures propagate.
    /// </para>
    /// </remarks>
    /// <param name="pin">PIN as UTF-8 encoded bytes. The caller owns and must clear this input buffer; the SDK never clears it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PIN-only mode(s) successfully authenticated. <see cref="PinOnlyMode.None"/> if neither succeeded.</returns>
    /// <exception cref="InvalidPinException">PIN verification is required and the supplied PIN is incorrect.</exception>
    Task<PinOnlyMode> RecoverPinOnlyModeAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable or disable PIN-protected management key mode.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires the management key to already be authenticated (<see cref="AuthenticateAsync"/>).
    /// Only <see cref="PinOnlyMode.None"/> (disable) and <see cref="PinOnlyMode.PinProtected"/>
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
    /// <para>These writes are separate commands with no rollback. Failure can leave PRINTED or ADMIN
    /// updated and the PUK blocked. Disabling leaves the PUK blocked. Inspect the objects and active
    /// credentials and reauthenticate before retrying. Keep an independent secure backup of the active key.
    /// The caller owns and clears <paramref name="managementKey"/> and the PIN buffer after use.</para>
    /// </remarks>
    /// <param name="pinOnlyMode"><see cref="PinOnlyMode.None"/> to disable, or <see cref="PinOnlyMode.PinProtected"/> to enable.</param>
    /// <param name="pin">PIN as UTF-8 encoded bytes. The caller owns and must clear this input buffer; the SDK never clears it.</param>
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
        PinOnlyMode pinOnlyMode,
        ReadOnlyMemory<byte> pin,
        ReadOnlyMemory<byte>? managementKey = null,
        CancellationToken cancellationToken = default);

    // Management key

    /// <summary>
    /// Set new management key.
    /// </summary>
    /// <remarks>
    /// Requires current management key authentication. After success, <see cref="ManagementKeyType"/>
    /// reflects <paramref name="keyType"/> and management authentication remains active for the newly
    /// installed key in the card session. The caller owns, backs up and clears the new-key buffer;
    /// the session retains no key bytes. Existing PIN-only objects require separate coordination.
    /// If the device reports that the security status is not satisfied, the session
    /// clears its recorded management authentication state; other command failures preserve it.
    /// </remarks>
    /// <param name="keyType">A symmetric management key type: TripleDES, AES128, AES192, or AES256.</param>
    /// <param name="newKey">New management key bytes.</param>
    /// <param name="requireTouch">Require touch for management key operations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetManagementKeyAsync(
        KeyType keyType,
        ReadOnlyMemory<byte> newKey,
        bool requireTouch = false,
        CancellationToken cancellationToken = default);
}
