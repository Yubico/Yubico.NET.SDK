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

using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Piv.Authentication;
using Yubico.YubiKit.Piv.Backend;
using Yubico.YubiKit.Piv.Bio;
using Yubico.YubiKit.Piv.Certificates;
using Yubico.YubiKit.Piv.Cryptography;
using Yubico.YubiKit.Piv.DataObjects;
using Yubico.YubiKit.Piv.Keys;
using Yubico.YubiKit.Piv.Metadata;

namespace Yubico.YubiKit.Piv;

/// <summary>
/// PIV (Personal Identity Verification) session for YubiKey operations.
/// </summary>
public sealed class PivSession : ApplicationSession, IPivSession
{
    // PIV instruction bytes
    private const byte InsVerify = 0x20;
    private const byte InsReset = 0xFB;

    // PIV P2 parameter bytes
    private const byte P2Pin = 0x80;
    private const byte P2Puk = 0x81;

    private readonly ScpKeyParameters? _scpKeyParams;
    private IPivBackend? _backend;
    private bool _isAuthenticated;

    /// <inheritdoc />
    public PivManagementKeyType ManagementKeyType { get; private set; } = PivManagementKeyType.TripleDes;

    /// <summary>
    /// Gets the well-known 24-byte factory-default PIV management key value.
    /// </summary>
    /// <remarks>
    /// The same bytes are used with Triple-DES and AES-192 defaults. Use
    /// <see cref="ManagementKeyType"/> to determine the active algorithm.
    /// </remarks>
    public static ReadOnlySpan<byte> DefaultManagementKey => PivAuthenticationProtocol.DefaultManagementKey;

    /// <summary>
    /// Gets whether the session has been authenticated with the management key.
    /// </summary>
    // TODO Disambiguate with IsAuthenticated
    public new bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Gets or sets the callback invoked when a YubiKey operation may require physical touch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set this property to receive notifications before operations that may require touch.
    /// The callback will be invoked for keys with <see cref="PivTouchPolicy.Always"/> or
    /// <see cref="PivTouchPolicy.Cached"/> touch policies.
    /// </para>
    /// <para>
    /// For <see cref="PivTouchPolicy.Cached"/>, the callback fires conservatively because
    /// the 15-second cache expiry timing cannot be determined from the API.
    /// </para>
    /// <para>
    /// On firmware older than 5.3 (no metadata support), the callback fires conservatively
    /// for all cryptographic operations as the touch policy cannot be queried.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// session.OnTouchRequired = () => Console.WriteLine("Touch your YubiKey now...");
    /// await session.SignOrDecryptAsync(PivSlot.Authentication, data);
    /// </code>
    /// </example>
    public TouchNotificationCallback? OnTouchRequired { get; set; }

    /// <summary>
    /// Initializes a new PivSession with the specified connection.
    /// </summary>
    /// <remarks>
    ///     Not public: construction must go through
    ///     <see cref="CreateAsync(ISmartCardConnection, ProtocolConfiguration?, ScpKeyParameters?, CancellationToken)" />,
    ///     which routes through <c>ApplicationSession.Construct</c> so the session is bound to its
    ///     connection and the one-live-session-per-connection rule is enforced. PivSession was the only
    ///     one of the eight applet sessions exposing a public constructor, and that door let a caller
    ///     create an unbound session that bypassed the guard.
    /// </remarks>
    /// <param name="connection">The connection to use for PIV operations.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
    internal PivSession(IConnection connection, ScpKeyParameters? scpKeyParams)
        : base(connection)
    {
        _scpKeyParams = scpKeyParams;
    }

    /// <summary>
    /// Creates and initializes a new PIV session.
    /// </summary>
    /// <param name="connection">SmartCard connection to the YubiKey.</param>
    /// <param name="configuration">Optional protocol configuration.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized PIV session.</returns>
    /// <exception cref="ArgumentNullException">If connection is null.</exception>
    /// <exception cref="ApduException">If PIV application selection fails.</exception>
    public static async Task<PivSession> CreateAsync(
        ISmartCardConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        // A session that fails to initialize must not keep its claim on the connection: the connection
        // outlives it, and the next session over it would otherwise be refused forever.
        var session = Construct(connection, () => new PivSession(connection, scpKeyParams));
        try
        {
            await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);
            return session;
        }
        catch
        {
            session.DisposeAfterInitializationFailure();
            throw;
        }
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;

        var protocol = ProtocolFactory.Create((ISmartCardConnection)Connection);
        Protocol = protocol;
        IPivBackend backend = new PivBackend(protocol);

        try
        {
            // Note: PIV GET VERSION returns the PIV application version (often 0.0.1),
            // not the YubiKey firmware version. Feature detection should use metadata
            // commands rather than version comparisons.
            var initialization = await backend.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var firmwareVersion = initialization.FirmwareVersion;
            Logger.LogDebug("PIV firmware version: {Version}", firmwareVersion);

            // Initialize base session
            var effectiveProtocol = (ISmartCardProtocol)await InitializeProtocolAsync(
                    protocol,
                    firmwareVersion,
                    configuration,
                    _scpKeyParams,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!ReferenceEquals(protocol, effectiveProtocol))
            {
                backend = new PivBackend(effectiveProtocol);
            }

            _backend = backend;

            // Detect management key type from device metadata (firmware 5.3+)
            // This is critical for YubiKey 5.7+ which defaults to AES-192 instead of 3DES
            ManagementKeyType = GetConservativeDefaultManagementKeyType(firmwareVersion);
            try
            {
                var metadata = await GetManagementKeyMetadataAsync(cancellationToken).ConfigureAwait(false);
                ManagementKeyType = metadata.KeyType;
                Logger.LogDebug("Management key type detected from metadata: {KeyType}", ManagementKeyType);
            }
            catch (NotSupportedException)
            {
                Logger.LogDebug(
                    "Management key metadata not supported, using conservative {KeyType} fallback",
                    ManagementKeyType);
            }
            catch (ApduException ex) when (ex.SW == 0x6A88 || ex.SW == 0x6D00)
            {
                // 0x6A88 = Referenced data not found, 0x6D00 = Instruction not supported
                Logger.LogDebug(
                    "Management key metadata query failed (SW={SW:X4}), using conservative {KeyType} fallback",
                    ex.SW,
                    ManagementKeyType);
            }

            Logger.LogInformation("PIV session initialized successfully. Version: {Version}", firmwareVersion);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize PIV session");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetSerialNumberAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        EnsureSupports(PivFeatures.Serial);
        EnsureBackend();

        Logger.LogDebug("PIV: Getting YubiKey serial number");

        var command = new ApduCommand(0x00, 0xF8, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);
        var response = await _backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // 0x6D00 means INS not supported (firmware < 5.0.0)
        if (response.SW == 0x6D00)
        {
            throw new NotSupportedException("Serial number retrieval requires firmware 5.0.0 or later");
        }

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to get serial number");
        }

        if (response.Data.Length != 4)
        {
            throw new ApduException("Invalid serial number response length");
        }

        // Serial is returned as big-endian 4-byte integer
        var serialBytes = response.Data.Span;
        var serial = (serialBytes[0] << 24) | (serialBytes[1] << 16) | (serialBytes[2] << 8) | serialBytes[3];

        Logger.LogDebug("PIV: Retrieved serial number: {Serial}", serial);
        return serial;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        EnsureBackend();

        Logger.LogDebug("PIV: Resetting PIV application");

        // TODO: Check bio not configured (Phase 7)

        // Step 1: Block PIN by verifying with empty PIN until blocked
        // Empty PIN encodes as 8 bytes of 0xFF per PIV spec
        await BlockPinAsync(cancellationToken).ConfigureAwait(false);

        // Step 2: Block PUK using RESET RETRY with empty credentials
        await BlockPukAsync(cancellationToken).ConfigureAwait(false);

        // Step 3: Send RESET command
        var resetCommand = new ApduCommand(0x00, InsReset, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);
        var response = await _backend.SendAsync(resetCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "PIV reset failed");
        }

        // Reset authentication state
        _isAuthenticated = false;

        // RESET changed the physical applet even if the metadata refresh below fails. Establish a
        // conservative post-reset type before querying: only a non-sentinel >=5.7 version reliably
        // identifies the AES-192 default; unknown/alpha/beta PIV versions fall back to TripleDes.
        ManagementKeyType = GetConservativeDefaultManagementKeyType(FirmwareVersion);

        // Update management key type from metadata (firmware 5.3+)
        try
        {
            var metadata = await GetManagementKeyMetadataAsync(cancellationToken).ConfigureAwait(false);
            ManagementKeyType = metadata.KeyType;
            Logger.LogDebug("PIV: Reset - management key type is {KeyType}", ManagementKeyType);
        }
        catch (NotSupportedException)
        {
            Logger.LogDebug("PIV: Reset - metadata not supported, using conservative {KeyType} fallback", ManagementKeyType);
        }

        Logger.LogDebug("PIV: Reset completed successfully");
    }

    private static PivManagementKeyType GetConservativeDefaultManagementKeyType(FirmwareVersion firmwareVersion) =>
        !firmwareVersion.IsAlphaOrBeta && firmwareVersion.IsAtLeast(5, 7, 0)
            ? PivManagementKeyType.Aes192
            : PivManagementKeyType.TripleDes;

    /// <summary>
    /// Blocks the PIN by repeatedly verifying with an empty PIN until blocked.
    /// </summary>
    private async Task BlockPinAsync(CancellationToken cancellationToken)
    {
        EnsureBackend();
        Logger.LogDebug("PIV: Blocking PIN");

        // Get initial retry count
        int retriesRemaining;
        try
        {
            var metadata = await GetPinMetadataAsync(cancellationToken).ConfigureAwait(false);
            retriesRemaining = metadata.RetriesRemaining;
        }
        catch (NotSupportedException)
        {
            // Firmware < 5.3 - assume max retries
            retriesRemaining = 15;
        }

        // Empty PIN encodes as 8 bytes of 0xFF
        byte[] emptyPin = PivPinUtilities.EncodePinBytes(ReadOnlySpan<char>.Empty);
        try
        {
            while (retriesRemaining > 0)
            {
                var pinCommand = new ApduCommand(0x00, InsVerify, 0x00, P2Pin, emptyPin);
                var response = await _backend.SendAsync(pinCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);

                retriesRemaining = PivPinUtilities.GetRetriesFromStatusWord(response.SW);
                if (retriesRemaining < 0)
                {
                    throw ApduException.FromStatusWord(response.SW, "Failed to block PIN");
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(emptyPin);
        }

        Logger.LogDebug("PIV: PIN blocked");
    }

    /// <summary>
    /// Blocks the PUK by repeatedly calling RESET RETRY with empty credentials until blocked.
    /// </summary>
    private Task BlockPukAsync(CancellationToken cancellationToken)
    {
        EnsureBackend();
        return PivMetadataProtocol.BlockPukAsync(_backend, Logger, cancellationToken);
    }

    /// <summary>
    /// Gets metadata about the PIV PIN.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>PIN metadata including retry counts and status.</returns>
    /// <exception cref="NotSupportedException">Thrown on firmware older than 5.3.0.</exception>
    public async Task<PivPinMetadata> GetPinMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        EnsureBackend();

        return await PivMetadataProtocol.GetPinMetadataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task AuthenticateAsync(ReadOnlyMemory<byte> managementKey, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        _isAuthenticated = false;
        await PivAuthenticationProtocol.AuthenticateAsync(_backend, Logger, ManagementKeyType, managementKey, cancellationToken)
            .ConfigureAwait(false);
        _isAuthenticated = true;
    }

    public async Task VerifyPinAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivAuthenticationProtocol.VerifyPinAsync(_backend, Logger, pin, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetPinAttemptsAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivAuthenticationProtocol.GetPinAttemptsAsync(
            _backend,
            Logger,
            IsSupported(PivFeatures.Metadata),
            GetPinMetadataAsync,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangePinAsync(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivAuthenticationProtocol.ChangePinAsync(_backend, Logger, currentPin, newPin, cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangePukAsync(ReadOnlyMemory<byte> oldPuk, ReadOnlyMemory<byte> newPuk, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivMetadataProtocol.ChangePukAsync(_backend, Logger, oldPuk, newPuk, cancellationToken).ConfigureAwait(false);
    }

    public async Task UnblockPinAsync(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivMetadataProtocol.UnblockPinAsync(_backend, Logger, puk, newPin, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivMetadataProtocol.SetPinAttemptsAsync(_backend, Logger, _isAuthenticated, pinAttempts, pukAttempts, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IPublicKey> GenerateKeyAsync(
        PivSlot slot,
        PivAlgorithm algorithm,
        PivPinPolicy pinPolicy = PivPinPolicy.Default,
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivKeyProtocol.GenerateKeyAsync(_backend, Logger, _isAuthenticated, slot, algorithm, pinPolicy, touchPolicy, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PivAlgorithm> ImportKeyAsync(
        PivSlot slot,
        IPrivateKey privateKey,
        PivPinPolicy pinPolicy = PivPinPolicy.Default,
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivKeyProtocol.ImportKeyAsync(_backend, Logger, _isAuthenticated, slot, privateKey, pinPolicy, touchPolicy, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MoveKeyAsync(PivSlot sourceSlot, PivSlot destinationSlot, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivKeyProtocol.MoveKeyAsync(_backend, Logger, _isAuthenticated, sourceSlot, destinationSlot, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteKeyAsync(PivSlot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivKeyProtocol.DeleteKeyAsync(_backend, Logger, _isAuthenticated, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<X509Certificate2> AttestKeyAsync(PivSlot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivKeyProtocol.AttestKeyAsync(_backend, Logger, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReadOnlyMemory<byte>> SignOrDecryptAsync(
        PivSlot slot,
        PivAlgorithm algorithm,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await NotifyTouchIfRequiredAsync(slot, cancellationToken).ConfigureAwait(false);
        return await PivCryptographicOperations.SignOrDecryptAsync(_backend, Logger, slot, algorithm, data, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ReadOnlyMemory<byte>> SignOrDecryptAsync(
        PivSlot slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: SignOrDecryptAsync auto-detecting algorithm for slot 0x{Slot:X2}", (byte)slot);

        if (!IsSupported(PivFeatures.Metadata))
        {
            throw new NotSupportedException(
                $"Auto-detecting algorithm requires YubiKey firmware 5.3 or later. " +
                $"Current firmware: {FirmwareVersion}. Use the overload that accepts an explicit algorithm parameter.");
        }

        var metadata = await GetSlotMetadataAsync(slot, cancellationToken).ConfigureAwait(false);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                $"Slot 0x{(byte)slot:X2} is empty. Generate or import a key before signing/decrypting.");
        }

        var slotMetadata = metadata.Value;
        Logger.LogDebug("PIV: Auto-detected algorithm {Algorithm} for slot 0x{Slot:X2}", slotMetadata.Algorithm, (byte)slot);

        return await SignOrDecryptAsync(slot, slotMetadata.Algorithm, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReadOnlyMemory<byte>> DecryptAsync(
        PivSlot slot,
        ReadOnlyMemory<byte> cipherText,
        RSAEncryptionPadding padding,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivCryptographicOperations.DecryptAsync(
            _backend,
            Logger,
            GetSlotMetadataAsync,
            NotifyTouchIfRequiredAsync,
            slot,
            cipherText,
            padding,
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ReadOnlyMemory<byte>> CalculateSecretAsync(
        PivSlot slot,
        IPublicKey peerPublicKey,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await NotifyTouchIfRequiredAsync(slot, cancellationToken).ConfigureAwait(false);
        return await PivCryptographicOperations.CalculateSecretAsync(_backend, Logger, slot, peerPublicKey, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<X509Certificate2?> GetCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivCertificateProtocol.GetCertificateAsync(_backend, Logger, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task StoreCertificateAsync(
        PivSlot slot,
        X509Certificate2 certificate,
        bool compress = false,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivCertificateProtocol.StoreCertificateAsync(_backend, Logger, _isAuthenticated, slot, certificate, compress, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivCertificateProtocol.DeleteCertificateAsync(_backend, Logger, _isAuthenticated, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivPukMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivMetadataProtocol.GetPukMetadataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivManagementKeyMetadata> GetManagementKeyMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivMetadataProtocol.GetManagementKeyMetadataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivSlotMetadata?> GetSlotMetadataAsync(PivSlot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivMetadataProtocol.GetSlotMetadataAsync(_backend, Logger, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivBioMetadata> GetBioMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        EnsureBackend();

        return await PivBioProtocol.GetBioMetadataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReadOnlyMemory<byte>> GetObjectAsync(int objectId, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivDataObjectProtocol.GetObjectAsync(_backend, objectId, cancellationToken).ConfigureAwait(false);
    }

    public async Task PutObjectAsync(int objectId, ReadOnlyMemory<byte>? data, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivDataObjectProtocol.PutObjectAsync(_backend, _isAuthenticated, objectId, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivCardholderUniqueId> GetCardholderUniqueIdAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivTypedDataObjectProtocol.GetCardholderUniqueIdAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetCardholderUniqueIdAsync(PivCardholderUniqueId cardholderUniqueId, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivTypedDataObjectProtocol.SetCardholderUniqueIdAsync(_backend, Logger, _isAuthenticated, cardholderUniqueId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PivCardCapabilityContainer> GetCardCapabilityContainerAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivTypedDataObjectProtocol.GetCardCapabilityContainerAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetCardCapabilityContainerAsync(PivCardCapabilityContainer cardCapabilityContainer, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivTypedDataObjectProtocol.SetCardCapabilityContainerAsync(_backend, Logger, _isAuthenticated, cardCapabilityContainer, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PivAdminData> GetAdminDataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivTypedDataObjectProtocol.GetAdminDataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetAdminDataAsync(PivAdminData adminData, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivTypedDataObjectProtocol.SetAdminDataAsync(_backend, Logger, _isAuthenticated, adminData, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivKeyHistory> GetKeyHistoryAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivTypedDataObjectProtocol.GetKeyHistoryAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetKeyHistoryAsync(PivKeyHistory keyHistory, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivTypedDataObjectProtocol.SetKeyHistoryAsync(_backend, Logger, _isAuthenticated, keyHistory, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivPinOnlyMode> GetPinOnlyModeAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivPinOnlyProtocol.GetPinOnlyModeAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivPinOnlyMode> RecoverPinOnlyModeAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        return await PivPinOnlyProtocol.RecoverPinOnlyModeAsync(
            _backend,
            Logger,
            ManagementKeyType,
            pin,
            (key, ct) => AuthenticateAsync(key, ct),
            (p, ct) => VerifyPinAsync(p, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SetPinOnlyModeAsync(
        PivPinOnlyMode pinOnlyMode,
        ReadOnlyMemory<byte> pin,
        ReadOnlyMemory<byte>? managementKey = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        await PivPinOnlyProtocol.SetPinOnlyModeAsync(
            _backend,
            Logger,
            _isAuthenticated,
            ManagementKeyType,
            pinOnlyMode,
            pin,
            managementKey,
            (key, ct) => AuthenticateAsync(key, ct),
            (p, ct) => VerifyPinAsync(p, ct),
            (type, key, touch, ct) => SetManagementKeyAsync(type, key, touch, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SetManagementKeyAsync(
        PivManagementKeyType keyType,
        ReadOnlyMemory<byte> newKey,
        bool requireTouch = false,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend();

        try
        {
            ManagementKeyType = await PivMetadataProtocol.SetManagementKeyAsync(
                _backend,
                Logger,
                _isAuthenticated,
                keyType,
                newKey,
                requireTouch,
                cancellationToken).ConfigureAwait(false);
        }
        catch (ApduException exception) when (exception.SW == SWConstants.SecurityStatusNotSatisfied)
        {
            _isAuthenticated = false;
            throw;
        }
    }

    public async Task<ReadOnlyMemory<byte>?> VerifyUvAsync(bool requestTemporaryPin = false, bool checkOnly = false, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        EnsureBackend();

        return await PivBioProtocol.VerifyUvAsync(_backend, Logger, requestTemporaryPin, checkOnly, cancellationToken).ConfigureAwait(false);
    }

    public async Task VerifyTemporaryPinAsync(ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        EnsureBackend();

        await PivBioProtocol.VerifyTemporaryPinAsync(_backend, Logger, temporaryPin, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Notifies the user if touch may be required for the operation on the specified slot.
    /// </summary>
    /// <param name="slot">The slot to check for touch policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// This method queries slot metadata (if supported) to determine the touch policy.
    /// For <see cref="PivTouchPolicy.Always"/> or <see cref="PivTouchPolicy.Cached"/>,
    /// the callback is invoked.
    /// </para>
    /// <para>
    /// On older firmware (&lt; 5.3), metadata is not available and the callback is invoked
    /// conservatively for all operations.
    /// </para>
    /// </remarks>
    private async Task NotifyTouchIfRequiredAsync(PivSlot slot, CancellationToken cancellationToken)
    {
        // Short-circuit if no callback registered
        if (OnTouchRequired is null)
        {
            return;
        }

        // Try to query slot metadata for touch policy
        if (IsSupported(PivFeatures.Metadata))
        {
            try
            {
                var metadata = await GetSlotMetadataAsync(slot, cancellationToken).ConfigureAwait(false);
                if (metadata is null)
                {
                    // Slot is empty - no touch needed
                    return;
                }

                var touchPolicy = metadata.Value.TouchPolicy;
                if (touchPolicy is PivTouchPolicy.Always or PivTouchPolicy.Cached)
                {
                    Logger.LogDebug("PIV: Touch may be required (policy: {Policy})", touchPolicy);
                    OnTouchRequired.Invoke();
                }

                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogDebug(ex, "PIV: Failed to query slot metadata for touch policy, notifying conservatively");
            }
        }

        // Fallback: On old firmware or metadata query failure, notify conservatively
        Logger.LogDebug("PIV: Notifying touch conservatively (metadata unavailable)");
        OnTouchRequired.Invoke();
    }


    private void EnsureInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Session is not initialized. Use PivSession.CreateAsync() to create a session.");
    }

    [MemberNotNull(nameof(_backend))]
    private void EnsureBackend()
    {
        if (_backend is null)
        {
            throw new InvalidOperationException("PIV session is not initialized. Call InitializeAsync first.");
        }
    }

}