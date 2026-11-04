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
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
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
    private const byte InsReset = 0xFB;

    private IPivBackend? _backend;
    private bool _isAuthenticated;
    private FirmwareVersion _detectedFirmwareVersion = new(0, 0, 0);

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
    /// <remarks>
    /// This is distinct from <see cref="ApplicationSession.IsAuthenticated"/>, which reports
    /// application-protocol authentication such as SCP. Returns <c>false</c> once disposal begins.
    /// </remarks>
    public bool IsManagementKeyAuthenticated => !IsDisposalStarted && Volatile.Read(ref _isAuthenticated);

    /// <summary>
    /// Initializes a new PivSession with the specified connection.
    /// </summary>
    /// <remarks>
    ///     Not public: construction must go through
    ///     <see cref="CreateAsync(ISmartCardConnection, SessionCreationOptions?, CancellationToken)" />,
    ///     which routes through <c>ApplicationSession.Construct</c> so the session is bound to its
    ///     connection and the one-live-session-per-connection rule is enforced. PivSession was the only
    ///     one of the eight applet sessions exposing a public constructor, and that door let a caller
    ///     create an unbound session that bypassed the guard.
    /// </remarks>
    /// <param name="connection">The connection to use for PIV operations.</param>
    /// <param name="userPresencePrompt">Optional user-presence notification service.</param>
    internal PivSession(
        IConnection connection,
        IUserPresencePrompt? userPresencePrompt = null)
        : base(connection, userPresencePrompt)
    {
    }

    /// <summary>
    /// Creates and initializes a new PIV session.
    /// </summary>
    /// <param name="connection">SmartCard connection to the YubiKey.</param>
    /// <param name="options">Optional cross-cutting session creation settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized PIV session.</returns>
    /// <exception cref="ArgumentNullException">If connection is null.</exception>
    /// <exception cref="ApduException">If PIV application selection fails.</exception>
    public static Task<PivSession> CreateAsync(
        ISmartCardConnection connection,
        SessionCreationOptions? options = null,
        CancellationToken cancellationToken = default) =>
        ConstructInitializedAsync(
            connection,
            options,
            () => new PivSession(connection, options?.UserPresencePrompt),
            (session, token) => session.InitializeAsync(
                options?.ProtocolConfiguration, options?.FirmwareVersionOverride, options?.ScpKeyParameters, token),
            cancellationToken);

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        FirmwareVersion? firmwareVersionOverride,
        ScpKeyParameters? scpKeyParams,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;

        var protocol = (ISmartCardProtocol)await PrepareProtocolAsync(scpKeyParams, cancellationToken)
            .ConfigureAwait(false);
        var backend = new PivBackend(protocol);

        try
        {
            // Production applet versions match firmware. Major zero is development firmware;
            // Core's feature evaluation deliberately treats it as current.
            var initialization = await backend.ReadInitialStateAsync(cancellationToken).ConfigureAwait(false);
            var detectedFirmwareVersion = initialization.FirmwareVersion;
            _detectedFirmwareVersion = detectedFirmwareVersion;
            var effectiveFirmwareVersion = firmwareVersionOverride ?? detectedFirmwareVersion;
            Logger.LogDebug("PIV firmware version: {Version}", detectedFirmwareVersion);

            // Initialize base session
            var effectiveProtocol = (ISmartCardProtocol)await ConfigureProtocolAsync(
                    protocol,
                    effectiveFirmwareVersion,
                    configuration,
                    scpKeyParams,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!ReferenceEquals(protocol, effectiveProtocol))
            {
                backend = new PivBackend(effectiveProtocol);
            }

            _backend = backend;

            // Detect management key type from device metadata (firmware 5.3+)
            // This is critical for YubiKey 5.7+ which defaults to AES-192 instead of 3DES
            ManagementKeyType = await ResolveManagementKeyTypeAsync(backend, cancellationToken).ConfigureAwait(false);

            Logger.LogInformation("PIV session initialized successfully. Version: {Version}", detectedFirmwareVersion);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize PIV session");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<int> GetSerialNumberAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        EnsureSupports(Features.Serial);
        return PivMetadataProtocol.GetSerialNumberAsync(_backend, Logger, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

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
        SetManagementKeyAuthenticationState(false);

        // RESET changed the physical applet even if the metadata refresh below fails. Establish a
        // conservative post-reset type before querying: a >=5.7 version defaults to AES-192, and an
        // alpha/beta version counts as >=5.7 because such a key is at least 5.8.0.
        ManagementKeyType = GetConservativeDefaultManagementKeyType(_detectedFirmwareVersion);

        // Update management key type from metadata (firmware 5.3+)
        ManagementKeyType = await ResolveManagementKeyTypeAsync(_backend, cancellationToken).ConfigureAwait(false);

        Logger.LogDebug("PIV: Reset completed successfully");
    }

    /// <summary>
    ///     Returns the management key type to assume before metadata has been read.
    /// </summary>
    /// <remarks>
    ///     <see cref="FirmwareVersion.IsAtLeast(int,int,int)" /> reports <see langword="true" /> for an alpha or
    ///     beta version, which is intended: such a key is at least 5.8.0 and therefore defaults to AES-192.
    ///     Excluding it here would assume Triple-DES for a development key that does not use it.
    /// </remarks>
    private static PivManagementKeyType GetConservativeDefaultManagementKeyType(FirmwareVersion firmwareVersion) =>
        firmwareVersion.IsAtLeast(5, 7, 0)
            ? PivManagementKeyType.Aes192
            : PivManagementKeyType.TripleDes;

    private async Task<PivManagementKeyType> ResolveManagementKeyTypeAsync(IPivBackend backend, CancellationToken cancellationToken)
    {
        try
        {
            return (await PivMetadataProtocol.GetManagementKeyMetadataAsync(backend, Logger, cancellationToken)
                .ConfigureAwait(false)).KeyType;
        }
        catch (NotSupportedException)
        {
            return GetConservativeDefaultManagementKeyType(_detectedFirmwareVersion);
        }
        catch (ApduException ex) when (ex.SW == 0x6A88)
        {
            return GetConservativeDefaultManagementKeyType(_detectedFirmwareVersion);
        }
    }

    /// <summary>
    /// Blocks the PIN by repeatedly verifying with an empty PIN until blocked.
    /// </summary>
    private async Task BlockPinAsync(CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
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

        await PivAuthenticationProtocol.BlockPinAsync(_backend, retriesRemaining, cancellationToken).ConfigureAwait(false);

        Logger.LogDebug("PIV: PIN blocked");
    }

    /// <summary>
    /// Blocks the PUK by repeatedly calling RESET RETRY with empty credentials until blocked.
    /// </summary>
    private Task BlockPukAsync(CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
        return PivAuthenticationProtocol.BlockPukAsync(_backend, Logger, cancellationToken);
    }

    /// <summary>
    /// Gets metadata about the PIV PIN.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>PIN metadata including retry counts and status.</returns>
    /// <exception cref="NotSupportedException">Thrown on firmware older than 5.3.0.</exception>
    public async Task<PivPinMetadata> GetPinMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        EnsureSupports(Features.Metadata);

        return await PivMetadataProtocol.GetPinMetadataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task AuthenticateAsync(ReadOnlyMemory<byte> managementKey, CancellationToken cancellationToken = default)
    {
        // Readiness/disposal admission precedes state mutation; cancellation is an attempted
        // authentication failure and must not leave a previous success published.
        EnsureBackend(CancellationToken.None);

        SetManagementKeyAuthenticationState(false);
        cancellationToken.ThrowIfCancellationRequested();
        await PivAuthenticationProtocol.AuthenticateAsync(_backend, Logger, ManagementKeyType, managementKey, cancellationToken)
            .ConfigureAwait(false);
        SetManagementKeyAuthenticationState(true);
    }

    public async Task VerifyPinAsync(ReadOnlyMemory<byte> pinUtf8, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        await PivAuthenticationProtocol.VerifyPinAsync(_backend, Logger, pinUtf8, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetPinAttemptsAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        if (IsSupported(Features.Metadata))
        {
            try
            {
                return (await PivMetadataProtocol.GetPinMetadataAsync(_backend, Logger, cancellationToken)
                    .ConfigureAwait(false)).RetriesRemaining;
            }
            catch (NotSupportedException)
            {
                // Only an unsupported metadata instruction permits the no-data VERIFY fallback.
            }
        }
        return await PivAuthenticationProtocol.GetPinAttemptsAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangePinAsync(ReadOnlyMemory<byte> currentPinUtf8, ReadOnlyMemory<byte> newPinUtf8, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        await PivAuthenticationProtocol.ChangePinAsync(_backend, Logger, currentPinUtf8, newPinUtf8, cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangePukAsync(ReadOnlyMemory<byte> currentPukUtf8, ReadOnlyMemory<byte> newPukUtf8, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        await PivAuthenticationProtocol.ChangePukAsync(_backend, Logger, currentPukUtf8, newPukUtf8, cancellationToken).ConfigureAwait(false);
    }

    public async Task UnblockPinAsync(ReadOnlyMemory<byte> pukUtf8, ReadOnlyMemory<byte> newPinUtf8, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        await PivAuthenticationProtocol.UnblockPinAsync(_backend, Logger, pukUtf8, newPinUtf8, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        await PivAuthenticationProtocol.SetPinAttemptsAsync(_backend, Logger, pinAttempts, pukAttempts, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IPublicKey> GenerateKeyAsync(
        Slot slot,
        KeyType algorithm,
        PivKeyCreationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        var pinPolicy = options?.PinPolicy ?? PivPinPolicy.Default;
        var touchPolicy = options?.TouchPolicy ?? PivTouchPolicy.Default;

        EnsureKeyOperationSupported(algorithm, pinPolicy, touchPolicy, generate: true);
        await EnsureBiometricPolicySupportedAsync(pinPolicy, cancellationToken).ConfigureAwait(false);
        EnsureManagementKeyAuthenticated();
        return await PivKeyProtocol.GenerateKeyAsync(_backend, Logger, slot, algorithm, pinPolicy, touchPolicy, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<KeyType> ImportKeyAsync(
        Slot slot,
        IPrivateKey privateKey,
        PivKeyCreationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        var pinPolicy = options?.PinPolicy ?? PivPinPolicy.Default;
        var touchPolicy = options?.TouchPolicy ?? PivTouchPolicy.Default;

        ArgumentNullException.ThrowIfNull(privateKey);
        EnsureKeyOperationSupported(privateKey.KeyType, pinPolicy, touchPolicy, generate: false);
        await EnsureBiometricPolicySupportedAsync(pinPolicy, cancellationToken).ConfigureAwait(false);
        EnsureManagementKeyAuthenticated();
        return await PivKeyProtocol.ImportKeyAsync(_backend, Logger, slot, privateKey, pinPolicy, touchPolicy, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MoveKeyAsync(Slot sourceSlot, Slot destinationSlot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(sourceSlot);
        ValidateSlot(destinationSlot);
        EnsureSupports(Features.MoveKey);

        EnsureManagementKeyAuthenticated();
        await PivKeyProtocol.MoveKeyAsync(_backend, Logger, sourceSlot, destinationSlot, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteKeyAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        EnsureSupports(Features.MoveKey);

        EnsureManagementKeyAuthenticated();
        await PivKeyProtocol.DeleteKeyAsync(_backend, Logger, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<X509Certificate2> AttestKeyAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        EnsureSupports(Features.Attestation);

        return await PivKeyProtocol.AttestKeyAsync(_backend, Logger, slot, cancellationToken).ConfigureAwait(false);
    }

    // Both overloads are established alpha entry points: one explicit algorithm and one metadata-driven.
#pragma warning disable RS0026
    public async Task<Memory<byte>> SignOrDecryptAsync(
        Slot slot,
        KeyType algorithm,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        EnsureKeyOperationSupported(algorithm, PivPinPolicy.Default, PivTouchPolicy.Default, generate: false);

        UserPresenceNotification userPresenceNotification = CreateUserPresenceNotification(
            await GetUserPresenceContextAsync(slot, UserPresenceOperations.Piv.SignOrDecrypt, cancellationToken).ConfigureAwait(false));
        return await SignOrDecryptWithUserPresenceAsync(
                slot,
                algorithm,
                data,
                userPresenceNotification,
                cancellationToken)
            .ConfigureAwait(false);
    }
#pragma warning restore RS0026

    // Metadata-based algorithm discovery is intentionally a second established alpha overload.
#pragma warning disable RS0026
    public async Task<Memory<byte>> SignOrDecryptAsync(
        Slot slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        Logger.LogDebug("PIV: SignOrDecryptAsync auto-detecting algorithm for slot 0x{Slot:X2}", (byte)slot);

        if (!IsSupported(Features.Metadata))
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
        var algorithm = slotMetadata.RequireKeyType();
        EnsureKeyOperationSupported(algorithm, PivPinPolicy.Default, PivTouchPolicy.Default, generate: false);

        UserPresenceNotification userPresenceNotification =
            CreateUserPresenceNotification(CreateUserPresenceContext(slot, slotMetadata, UserPresenceOperations.Piv.SignOrDecrypt));
        return await SignOrDecryptWithUserPresenceAsync(
                slot,
                algorithm,
                data,
                userPresenceNotification,
                cancellationToken)
            .ConfigureAwait(false);
    }
#pragma warning restore RS0026

    public async Task<Memory<byte>> DecryptAsync(
        Slot slot,
        ReadOnlyMemory<byte> cipherText,
        RSAEncryptionPadding padding,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        var metadata = await GetSlotMetadataAsync(slot, cancellationToken).ConfigureAwait(false);
        UserPresenceNotification userPresenceNotification =
            CreateUserPresenceNotification(CreateUserPresenceContext(slot, metadata, UserPresenceOperations.Piv.Decrypt));

        if (metadata is { } slotMetadata)
            EnsureKeyOperationSupported(slotMetadata.RequireKeyType(), PivPinPolicy.Default, PivTouchPolicy.Default, generate: false);

        return await RunWithUserPresenceResolutionAsync(
                userPresenceNotification,
                token => PivCryptographicOperations.DecryptAsync(
                    _backend,
                    Logger,
                    metadata,
                    slot,
                    cipherText,
                    padding,
                    userPresenceNotification,
                    token),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Memory<byte>> CalculateSecretAsync(
        Slot slot,
        IPublicKey peerPublicKey,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        ArgumentNullException.ThrowIfNull(peerPublicKey);
        EnsureKeyOperationSupported(peerPublicKey.KeyType, PivPinPolicy.Default, PivTouchPolicy.Default, generate: false);

        UserPresenceNotification userPresenceNotification = CreateUserPresenceNotification(
            await GetUserPresenceContextAsync(slot, UserPresenceOperations.Piv.CalculateSecret, cancellationToken).ConfigureAwait(false));
        return await RunWithUserPresenceResolutionAsync(
                userPresenceNotification,
                token => PivCryptographicOperations.CalculateSecretAsync(
                    _backend,
                    Logger,
                    slot,
                    peerPublicKey,
                    userPresenceNotification,
                    token),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<X509Certificate2?> GetCertificateAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        return await PivCertificateProtocol.GetCertificateAsync(_backend, Logger, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task StoreCertificateAsync(
        Slot slot,
        X509Certificate2 certificate,
        PivCertificateCompression compression = PivCertificateCompression.Automatic,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        EnsureManagementKeyAuthenticated();
        await PivCertificateProtocol.StoreCertificateAsync(_backend, Logger, slot, certificate, compression, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteCertificateAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        EnsureManagementKeyAuthenticated();
        await PivCertificateProtocol.DeleteCertificateAsync(_backend, Logger, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivPukMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        EnsureSupports(Features.Metadata);

        return await PivMetadataProtocol.GetPukMetadataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivManagementKeyMetadata> GetManagementKeyMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        EnsureSupports(Features.Metadata);

        return await PivMetadataProtocol.GetManagementKeyMetadataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivSlotMetadata?> GetSlotMetadataAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        EnsureSupports(Features.Metadata);

        return await PivMetadataProtocol.GetSlotMetadataAsync(_backend, Logger, slot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivBioMetadata> GetBioMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return await PivBioProtocol.GetBioMetadataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Memory<byte>> GetObjectAsync(int objectId, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return await PivDataObjectProtocol.GetObjectAsync(_backend, objectId, cancellationToken).ConfigureAwait(false);
    }

    public async Task PutObjectAsync(int objectId, ReadOnlyMemory<byte>? data, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        await PivDataObjectProtocol.PutObjectAsync(_backend, objectId, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivCardholderUniqueId> GetCardholderUniqueIdAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return await PivTypedDataObjectProtocol.GetCardholderUniqueIdAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetCardholderUniqueIdAsync(PivCardholderUniqueId cardholderUniqueId, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        await PivTypedDataObjectProtocol.SetCardholderUniqueIdAsync(_backend, Logger, cardholderUniqueId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PivCardCapabilityContainer> GetCardCapabilityContainerAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return await PivTypedDataObjectProtocol.GetCardCapabilityContainerAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetCardCapabilityContainerAsync(PivCardCapabilityContainer cardCapabilityContainer, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        await PivTypedDataObjectProtocol.SetCardCapabilityContainerAsync(_backend, Logger, cardCapabilityContainer, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PivAdminData> GetAdminDataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return await PivTypedDataObjectProtocol.GetAdminDataAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetAdminDataAsync(PivAdminData adminData, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        await PivTypedDataObjectProtocol.SetAdminDataAsync(_backend, Logger, adminData, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivKeyHistory> GetKeyHistoryAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return await PivTypedDataObjectProtocol.GetKeyHistoryAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetKeyHistoryAsync(PivKeyHistory keyHistory, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        await PivTypedDataObjectProtocol.SetKeyHistoryAsync(_backend, Logger, keyHistory, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PivPinOnlyMode> GetPinOnlyModeAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return await PivPinOnlyProtocol.GetPinOnlyModeAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);
    }

    public Task<PivPinOnlyMode> RecoverPinOnlyModeAsync(ReadOnlyMemory<byte> pinUtf8, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        return RecoverPinOnlyModeCoreAsync(pinUtf8, cancellationToken);
    }

    public async Task SetPinOnlyModeAsync(
        PivPinOnlyMode pinOnlyMode,
        ReadOnlyMemory<byte> pinUtf8,
        ReadOnlyMemory<byte>? managementKey = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        EnsureManagementKeyAuthenticated();

        if ((pinOnlyMode & ~(PivPinOnlyMode.None | PivPinOnlyMode.PinProtected)) != 0)
        {
            throw new ArgumentException(
                "Only PivPinOnlyMode.None (disable) and PivPinOnlyMode.PinProtected (enable) are " +
                "supported. PIN-derived management keys are deprecated and cannot be enabled through " +
                "this API.",
                nameof(pinOnlyMode));
        }

        if (pinOnlyMode == PivPinOnlyMode.PinProtected)
        {
            if (pinUtf8.Length is < 6 or > 8)
                throw new ArgumentException("PIN must be 6-8 bytes.", nameof(pinUtf8));
            if (managementKey is not { } keyToProtect)
            {
                throw new ArgumentNullException(nameof(managementKey), "A management key is required to enable PIN-protected mode.");
            }

            int expectedKeyLength = PivPinOnlyProtocol.GetManagementKeyLength(ManagementKeyType);
            if (keyToProtect.Length != expectedKeyLength)
            {
                throw new ArgumentException(
                    $"Invalid management key length {keyToProtect.Length} for {ManagementKeyType}. Expected {expectedKeyLength} bytes.",
                    nameof(managementKey));
            }

            await EnablePinProtectedModeAsync(pinUtf8, keyToProtect, cancellationToken).ConfigureAwait(false);
            return;
        }
        await DisablePinOnlyModeAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetManagementKeyAsync(
        PivManagementKeyType keyType,
        ReadOnlyMemory<byte> newKey,
        bool requireTouch = false,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        if (keyType != PivManagementKeyType.TripleDes)
            EnsureSupports(Features.AesKey);
        if (requireTouch)
            EnsureSupports(Features.UsagePolicy);

        EnsureManagementKeyAuthenticated();
        try
        {
            ManagementKeyType = await PivAuthenticationProtocol.SetManagementKeyAsync(
                _backend,
                Logger,
                keyType,
                newKey,
                requireTouch,
                cancellationToken).ConfigureAwait(false);
        }
        catch (ApduException exception) when (exception.SW == SWConstants.SecurityStatusNotSatisfied)
        {
            SetManagementKeyAuthenticationState(false);
            throw;
        }
    }

    public async Task<Memory<byte>?> VerifyUvAsync(
        PivUserVerification userVerification = PivUserVerification.Verify,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        var temporaryPin = await PivBioProtocol.VerifyUvAsync(_backend, Logger, userVerification, cancellationToken).ConfigureAwait(false);
        return temporaryPin is null ? null : temporaryPin.AsMemory();
    }

    public async Task VerifyTemporaryPinAsync(ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        await PivBioProtocol.VerifyTemporaryPinAsync(_backend, Logger, temporaryPin, cancellationToken).ConfigureAwait(false);
    }

    private Task<byte[]> SignOrDecryptWithUserPresenceAsync(
        Slot slot,
        KeyType algorithm,
        ReadOnlyMemory<byte> data,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);

        return RunWithUserPresenceResolutionAsync(
            userPresenceNotification,
            token => PivCryptographicOperations.SignOrDecryptAsync(
                _backend,
                Logger,
                slot,
                algorithm,
                data,
                userPresenceNotification,
                token),
            cancellationToken);
    }

    private async Task<UserPresenceContext?> GetUserPresenceContextAsync(
        Slot slot,
        string operation,
        CancellationToken cancellationToken)
    {
        if (!IsUserPresenceNotificationEnabled)
            return null;

        if (!IsSupported(Features.Metadata))
            return CreateUserPresenceContext(slot, UserPresenceBasis.PolicyMayRequire, operation);

        try
        {
            var metadata = await GetSlotMetadataAsync(slot, cancellationToken).ConfigureAwait(false);
            return CreateUserPresenceContext(slot, metadata, operation);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogDebug(ex, "PIV: Failed to query slot metadata for touch policy, notifying conservatively");
            return CreateUserPresenceContext(slot, UserPresenceBasis.PolicyMayRequire, operation);
        }
    }

    private static UserPresenceContext? CreateUserPresenceContext(
        Slot slot,
        PivSlotMetadata? metadata,
        string operation)
    {
        if (metadata is null)
            return null;

        UserPresenceBasis? basis = metadata.Value.TouchPolicy switch
        {
            PivTouchPolicy.Always => UserPresenceBasis.PolicyRequires,
            PivTouchPolicy.Cached => UserPresenceBasis.PolicyMayRequire,
            PivTouchPolicy.Never or PivTouchPolicy.Default => null,
            _ => UserPresenceBasis.PolicyMayRequire
        };

        return basis is { } value ? CreateUserPresenceContext(slot, value, operation) : null;
    }

    private static UserPresenceContext CreateUserPresenceContext(Slot slot, UserPresenceBasis basis, string operation) =>
        new()
        {
            Basis = basis,
            Application = UserPresenceApplications.Piv,
            Operation = operation,
            Scope = slot.ToString()
        };

    protected override void Dispose(bool disposing)
    {
        SetManagementKeyAuthenticationState(false);
        base.Dispose(disposing);
    }

    private void SetManagementKeyAuthenticationState(bool isAuthenticated) =>
        Volatile.Write(ref _isAuthenticated, isAuthenticated);

    [MemberNotNull(nameof(_backend))]
    private void EnsureBackend(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (!IsInitialized)
            throw new InvalidOperationException("Session is not initialized. Use PivSession.CreateAsync() to create a session.");
        cancellationToken.ThrowIfCancellationRequested();

        if (_backend is null)
        {
            throw new InvalidOperationException("PIV session backend is unavailable.");
        }
    }

    private void EnsureManagementKeyAuthenticated()
    {
        if (!IsManagementKeyAuthenticated)
            throw new InvalidOperationException("Management key authentication required.");
    }

    private void EnsureKeyOperationSupported(KeyType keyType, PivPinPolicy pinPolicy, PivTouchPolicy touchPolicy, bool generate)
    {
        _ = PivAlgorithmId.Encode(keyType);
        Feature? feature = keyType switch
        {
            KeyType.ECP384 => Features.P384,
            KeyType.Ed25519 or KeyType.X25519 => Features.Cv25519,
            KeyType.RSA3072 or KeyType.RSA4096 => Features.Rsa3072Rsa4096,
            _ => null
        };
        if (feature is { } required)
            EnsureSupports(required);
        EnsureKeyPoliciesSupported(pinPolicy, touchPolicy);
        if (generate && keyType.IsRSA() && !Features.SupportsRsaGeneration(FirmwareVersion))
            throw new NotSupportedException("RSA key generation is disabled on ROCA-affected firmware 4.2.6–4.3.4.");
        EnsureFipsKeyPolicySupported(keyType, pinPolicy);
    }

    private void EnsureKeyPoliciesSupported(PivPinPolicy pinPolicy, PivTouchPolicy touchPolicy)
    {
        if (!Enum.IsDefined(pinPolicy) || !Enum.IsDefined(touchPolicy))
            throw new ArgumentException("Unknown PIV key policy.");
        if (pinPolicy != PivPinPolicy.Default || touchPolicy != PivTouchPolicy.Default)
            EnsureSupports(Features.UsagePolicy);
        if (touchPolicy == PivTouchPolicy.Cached)
            EnsureSupports(Features.TouchCached);
    }

    private async Task EnsureBiometricPolicySupportedAsync(PivPinPolicy policy, CancellationToken cancellationToken)
    {
        if (policy is PivPinPolicy.MatchOnce or PivPinPolicy.MatchAlways)
            _ = await GetBioMetadataAsync(cancellationToken).ConfigureAwait(false);
    }

    private void EnsureFipsKeyPolicySupported(KeyType keyType, PivPinPolicy pinPolicy)
    {
        if (!FirmwareVersion.IsAlphaOrBeta && FirmwareVersion.IsAtLeast(4, 4, 0) && !FirmwareVersion.IsAtLeast(4, 5, 0)
            && (keyType == KeyType.RSA1024 || pinPolicy == PivPinPolicy.Never))
            throw new NotSupportedException("RSA1024 and PIN policy Never are not supported on YubiKey 4 FIPS.");
    }

    private async Task<PivPinOnlyMode> RecoverPinOnlyModeCoreAsync(ReadOnlyMemory<byte> pinUtf8, CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
        Logger.LogDebug("PIV: Attempting to recover PIN-only management key authentication");

        var mode = PivPinOnlyMode.None;
        var (printedRaw, pinVerified) = await ReadPrintedForRecoveryAsync(pinUtf8, cancellationToken).ConfigureAwait(false);

        Memory<byte> storedKey = Memory<byte>.Empty;
        bool pinProtectedAuthenticated = false;
        try
        {
            if (PivPinOnlyProtocol.TryDecodePinProtectedManagementKey(printedRaw, out storedKey))
            {
                pinProtectedAuthenticated = await TryAuthenticateCandidateAsync(storedKey, cancellationToken).ConfigureAwait(false);
                if (pinProtectedAuthenticated)
                    mode |= PivPinOnlyMode.PinProtected;
            }

            // Try PIN-derived: derive a candidate management key from the PIN and the ADMIN DATA salt.
            var adminRaw = await PivDataObjectProtocol.GetObjectAsync(_backend, PivDataObject.AdminData, cancellationToken).ConfigureAwait(false);

            if (PivAdminData.TryDecode(adminRaw, out var adminData) && adminData.Salt is { } salt)
            {
                if (!pinVerified)
                    await VerifyPinAsync(pinUtf8, cancellationToken).ConfigureAwait(false);
                if (await TryAuthenticateDerivedCandidateAsync(pinUtf8, salt, cancellationToken).ConfigureAwait(false))
                    mode |= PivPinOnlyMode.PinDerived;
                else if (pinProtectedAuthenticated && !await TryAuthenticateCandidateAsync(storedKey, cancellationToken).ConfigureAwait(false))
                    mode &= ~PivPinOnlyMode.PinProtected;
            }

            return mode;
        }
        finally
        {
            if (!storedKey.IsEmpty)
            {
                CryptographicOperations.ZeroMemory(storedKey.Span);
            }
        }
    }

    private async Task<(byte[] Data, bool PinVerified)> ReadPrintedForRecoveryAsync(
        ReadOnlyMemory<byte> pin, CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
        try
        {
            return (await PivDataObjectProtocol.GetObjectAsync(_backend, PivDataObject.PrintedInformation, cancellationToken)
                .ConfigureAwait(false), false);
        }
        catch (ApduException ex) when (ex.SW == SWConstants.SecurityStatusNotSatisfied)
        {
            await VerifyPinAsync(pin, cancellationToken).ConfigureAwait(false);
            return (await PivDataObjectProtocol.GetObjectAsync(_backend, PivDataObject.PrintedInformation, cancellationToken)
                .ConfigureAwait(false), true);
        }
    }

    private async Task<bool> TryAuthenticateCandidateAsync(ReadOnlyMemory<byte> candidate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await AuthenticateAsync(candidate, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (ApduException ex) when (ex.SW == SWConstants.SecurityStatusNotSatisfied)
        {
            // A rejected management-key witness/challenge is not a transport or encoding failure.
            return false;
        }
    }

    private async Task<bool> TryAuthenticateDerivedCandidateAsync(ReadOnlyMemory<byte> pin, ReadOnlyMemory<byte> salt, CancellationToken cancellationToken)
    {
        int length = PivPinOnlyProtocol.GetManagementKeyLength(ManagementKeyType);
        byte[] candidate = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            PivPinOnlyProtocol.DeriveManagementKey(pin.Span, salt.Span, candidate.AsSpan(0, length));
            return await TryAuthenticateCandidateAsync(candidate.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(candidate.AsSpan(0, length));
            ArrayPool<byte>.Shared.Return(candidate);
        }
    }

    private async Task EnablePinProtectedModeAsync(ReadOnlyMemory<byte> pinUtf8, ReadOnlyMemory<byte> keyToProtect, CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
        Logger.LogDebug("PIV: Enabling PIN-protected management key mode");

        await AuthenticateAsync(keyToProtect, cancellationToken).ConfigureAwait(false);
        await VerifyPinAsync(pinUtf8, cancellationToken).ConfigureAwait(false);
        await PivPinOnlyProtocol.StorePinProtectedManagementKeyAsync(_backend, Logger, keyToProtect, cancellationToken).ConfigureAwait(false);
        await PivAuthenticationProtocol.BlockPukAsync(_backend, Logger, cancellationToken).ConfigureAwait(false);

        var currentAdminRaw = await PivDataObjectProtocol.GetObjectAsync(_backend, PivDataObject.AdminData, cancellationToken)
            .ConfigureAwait(false);
        _ = PivAdminData.TryDecode(currentAdminRaw, out var currentAdmin);
        var updatedAdmin = PivAdminData.Create(pukBlocked: true, pinProtected: true, currentAdmin.Salt, currentAdmin.PinLastUpdated);
        EnsureManagementKeyAuthenticated();
        await PivTypedDataObjectProtocol.SetAdminDataAsync(_backend, Logger, updatedAdmin, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task DisablePinOnlyModeAsync(CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
        // pinOnlyMode == None: disable, but only if a PIN-only mode is actually currently set.
        var currentMode = await GetPinOnlyModeAsync(cancellationToken).ConfigureAwait(false);
        if (!currentMode.HasFlag(PivPinOnlyMode.PinProtected) && !currentMode.HasFlag(PivPinOnlyMode.PinDerived))
        {
            Logger.LogDebug("PIV: PIN-only mode is already disabled; no changes made");
            return;
        }

        Logger.LogDebug("PIV: Disabling PIN-only management key mode");
        int defaultKeyLength = PivPinOnlyProtocol.GetManagementKeyLength(ManagementKeyType);
        byte[] defaultKey = ArrayPool<byte>.Shared.Rent(defaultKeyLength);
        try
        {
            PivPinOnlyProtocol.FillRepeatingDefaultKeyPattern(defaultKey.AsSpan(0, defaultKeyLength));
            await SetManagementKeyAsync(ManagementKeyType, defaultKey.AsMemory(0, defaultKeyLength), false, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(defaultKey.AsSpan(0, defaultKeyLength));
            ArrayPool<byte>.Shared.Return(defaultKey);
        }

        EnsureManagementKeyAuthenticated();
        await PivDataObjectProtocol.PutObjectAsync(_backend, PivDataObject.PrintedInformation, null, cancellationToken)
            .ConfigureAwait(false);
        EnsureManagementKeyAuthenticated();
        await PivDataObjectProtocol.PutObjectAsync(_backend, PivDataObject.AdminData, null, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void ValidateSlot(Slot slot)
    {
        if (!Enum.IsDefined(slot))
            throw new ArgumentOutOfRangeException(nameof(slot), "Unknown PIV asymmetric key reference.");
    }
}