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
using Yubico.YubiKit.Piv.Backends;
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
    private const byte Cla = 0x00;
    private const byte UnusedParameter = 0x00;

    // PIV instruction bytes
    private const byte InsReset = 0xFB;

    private IBackend? _backend;
    private bool _isAuthenticated;
    private FirmwareVersion _detectedFirmwareVersion = new(0, 0, 0);

    /// <inheritdoc />
    public KeyType? ManagementKeyType { get; private set; } = KeyType.TripleDes;

    /// <summary>
    /// Gets the well-known 24-byte factory-default PIV management key value.
    /// </summary>
    /// <remarks>
    /// The same bytes are used with Triple-DES and AES-192 defaults. Use
    /// <see cref="ManagementKeyType"/> to determine the active algorithm.
    /// </remarks>
    public static ReadOnlySpan<byte> DefaultManagementKey => AuthenticationProtocol.DefaultManagementKey;

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
    ///     Creation goes through
    ///     <see cref="CreateAsync(ISmartCardConnection, SessionCreationOptions?, CancellationToken)" />,
    ///     which uses <c>ApplicationSession.ConstructInitializedAsync</c> to bind the session to its
    ///     connection before initialization and enforce one live session per connection.
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
    /// <remarks>
    /// The caller retains the connection and disposes it after session teardown. The factory binds before
    /// SELECT and registers protocol disposal before setup. It reads the PIV version, configures the effective
    /// override-or-detected version and optional SCP, then resolves the management-key type before publishing
    /// readiness. Failed initialization disposes/detaches the session without closing this borrowed connection.
    /// Keep borrowed SCP inputs alive through factory completion. The session owns its protocol; its backend
    /// borrows that protocol, and its user-presence prompt remains caller-owned.
    /// </remarks>
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
        var backend = new SmartCardBackend(protocol);

        try
        {
            // Read the PIV applet's version before SCP. Core treats major-zero versions optimistically;
            // that compatibility policy establishes no independently identified physical firmware version.
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
                backend = new SmartCardBackend(effectiveProtocol);
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
        return MetadataProtocol.GetSerialNumberAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        Logger.LogDebug("PIV: Resetting PIV application");

        // TODO: Add Bio preflight before consuming retries; Bio MPE requires its device-reset route.

        // Step 1: Block PIN by verifying with empty PIN until blocked
        // Empty PIN encodes as 8 bytes of 0xFF per PIV spec
        await BlockPinAsync(cancellationToken).ConfigureAwait(false);

        // Step 2: Block PUK using RESET RETRY with empty credentials
        await BlockPukAsync(cancellationToken).ConfigureAwait(false);

        // Step 3: Send RESET command
        var resetCommand = new ApduCommand(Cla, InsReset, UnusedParameter, UnusedParameter, ReadOnlyMemory<byte>.Empty);
        var response = await _backend.SendAsync(resetCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "PIV reset failed");
        }

        // Reset management authentication state. This workflow retains its backend/protocol and performs
        // no terminal session disposal, applet reselection or fresh SCP handshake.
        SetManagementKeyAuthenticationState(false);

        // RESET changed the physical applet even if the metadata refresh below fails. Establish a
        // conservative post-reset type before querying: a >=5.7 version defaults to AES-192, and an
        // major-zero development version follows the SDK's modern-default compatibility convention.
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
    ///     beta version under the SDK development-firmware convention, selecting the AES-192 fallback.
    ///     Authoritative management metadata still takes precedence when available.
    /// </remarks>
    private static KeyType GetConservativeDefaultManagementKeyType(FirmwareVersion firmwareVersion) =>
        firmwareVersion.IsAtLeast(5, 7, 0)
            ? KeyType.Aes192
            : KeyType.TripleDes;

    private async Task<KeyType?> ResolveManagementKeyTypeAsync(IBackend backend, CancellationToken cancellationToken)
    {
        try
        {
            return (await MetadataProtocol.GetManagementKeyMetadataAsync(backend, cancellationToken)
                .ConfigureAwait(false)).KeyType;
        }
        catch (NotSupportedException)
        {
            return GetConservativeDefaultManagementKeyType(_detectedFirmwareVersion);
        }
        catch (ApduException ex) when (ex.SW == SWConstants.DataNotFound)
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
            // Older firmware uses a bounded 15-attempt budget, matching the status-word count width.
            retriesRemaining = 15;
        }

        await AuthenticationProtocol.BlockPinAsync(_backend, retriesRemaining, cancellationToken).ConfigureAwait(false);

        Logger.LogDebug("PIV: PIN blocked");
    }

    /// <summary>
    /// Blocks the PUK by repeatedly calling RESET RETRY with empty credentials until blocked.
    /// </summary>
    private Task BlockPukAsync(CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
        return AuthenticationProtocol.BlockPukAsync(_backend, cancellationToken);
    }

    /// <summary>
    /// Gets metadata about the PIV PIN.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>PIN metadata including retry counts and status.</returns>
    /// <exception cref="NotSupportedException">Thrown on firmware older than 5.3.0.</exception>
    public Task<PinMetadata> GetPinMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        EnsureSupports(Features.Metadata);

        return MetadataProtocol.GetPinMetadataAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AuthenticateAsync(ReadOnlyMemory<byte> managementKey, CancellationToken cancellationToken = default)
    {
        // Readiness/disposal admission precedes state mutation; cancellation is an attempted
        // authentication failure and must not leave a previous success published.
        EnsureBackend(CancellationToken.None);

        SetManagementKeyAuthenticationState(false);
        cancellationToken.ThrowIfCancellationRequested();
        await AuthenticationProtocol.AuthenticateAsync(_backend, RequireManagementKeyType(), managementKey, cancellationToken)
            .ConfigureAwait(false);
        SetManagementKeyAuthenticationState(true);
    }

    /// <inheritdoc />
    public Task VerifyPinAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return AuthenticationProtocol.VerifyPinAsync(_backend, pin, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetPinAttemptsAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        if (IsSupported(Features.Metadata))
        {
            // Choose between supported implementations, rather than requiring metadata: older keys
            // still support the no-data VERIFY retry query. GetPinMetadataAsync itself enforces metadata.
            try
            {
                return (await MetadataProtocol.GetPinMetadataAsync(_backend, cancellationToken)
                    .ConfigureAwait(false)).RetriesRemaining;
            }
            catch (NotSupportedException)
            {
                // Only an unsupported metadata instruction permits the no-data VERIFY fallback.
            }
        }
        return await AuthenticationProtocol.GetPinAttemptsAsync(_backend, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task ChangePinAsync(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return AuthenticationProtocol.ChangePinAsync(_backend, currentPin, newPin, cancellationToken);
    }

    /// <inheritdoc />
    public Task ChangePukAsync(ReadOnlyMemory<byte> currentPuk, ReadOnlyMemory<byte> newPuk, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return AuthenticationProtocol.ChangePukAsync(_backend, currentPuk, newPuk, cancellationToken);
    }

    /// <inheritdoc />
    public Task UnblockPinAsync(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return AuthenticationProtocol.UnblockPinAsync(_backend, puk, newPin, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        return AuthenticationProtocol.SetPinAttemptsAsync(_backend, pinAttempts, pukAttempts, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IPublicKey> GenerateKeyAsync(
        Slot slot,
        KeyType keyType,
        KeyCreationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        var pinPolicy = options?.PinPolicy ?? PinPolicy.Default;
        var touchPolicy = options?.TouchPolicy ?? TouchPolicy.Default;

        EnsureKeyOperationSupported(keyType, pinPolicy, touchPolicy, generate: true);
        await EnsureBiometricPolicySupportedAsync(pinPolicy, cancellationToken).ConfigureAwait(false);
        EnsureManagementKeyAuthenticated();
        return await KeyProtocol.GenerateKeyAsync(_backend, slot, keyType, pinPolicy, touchPolicy, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ImportKeyAsync(
        Slot slot,
        IPrivateKey privateKey,
        KeyCreationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        var pinPolicy = options?.PinPolicy ?? PinPolicy.Default;
        var touchPolicy = options?.TouchPolicy ?? TouchPolicy.Default;

        ArgumentNullException.ThrowIfNull(privateKey);
        EnsureKeyOperationSupported(privateKey.KeyType, pinPolicy, touchPolicy, generate: false);
        await EnsureBiometricPolicySupportedAsync(pinPolicy, cancellationToken).ConfigureAwait(false);
        EnsureManagementKeyAuthenticated();
        await KeyProtocol.ImportKeyAsync(_backend, slot, privateKey, pinPolicy, touchPolicy, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task MoveKeyAsync(Slot sourceSlot, Slot destinationSlot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(sourceSlot);
        ValidateSlot(destinationSlot);
        EnsureSupports(Features.MoveKey);

        EnsureManagementKeyAuthenticated();
        return KeyProtocol.MoveKeyAsync(_backend, sourceSlot, destinationSlot, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteKeyAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        EnsureSupports(Features.MoveKey);

        EnsureManagementKeyAuthenticated();
        return KeyProtocol.DeleteKeyAsync(_backend, slot, cancellationToken);
    }

    /// <inheritdoc />
    public Task<X509Certificate2> AttestKeyAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        EnsureSupports(Features.Attestation);

        return KeyProtocol.AttestKeyAsync(_backend, slot, cancellationToken);
    }

    // Both overloads are established alpha entry points: one explicit algorithm and one metadata-driven.
#pragma warning disable RS0026
    /// <inheritdoc />
    public async Task<Memory<byte>> SignOrDecryptAsync(
        Slot slot,
        KeyType keyType,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        EnsureKeyOperationSupported(keyType, PinPolicy.Default, TouchPolicy.Default, generate: false);

        UserPresenceNotification userPresenceNotification = CreateUserPresenceNotification(
            await GetUserPresenceContextAsync(slot, UserPresenceOperations.Piv.SignOrDecrypt, cancellationToken).ConfigureAwait(false));
        return await SignOrDecryptWithUserPresenceAsync(
                slot,
                keyType,
                data,
                userPresenceNotification,
                cancellationToken)
            .ConfigureAwait(false);
    }
#pragma warning restore RS0026

    // Metadata-based algorithm discovery is intentionally a second established alpha overload.
#pragma warning disable RS0026
    /// <inheritdoc />
    public async Task<Memory<byte>> SignOrDecryptAsync(
        Slot slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        Logger.LogDebug("PIV: SignOrDecryptAsync auto-detecting key type for slot 0x{Slot:X2}", (byte)slot);

        if (!IsSupported(Features.Metadata))
        {
            throw new NotSupportedException(
                $"Auto-detecting key type requires YubiKey firmware 5.3 or later. " +
                $"Current firmware: {FirmwareVersion}. Use the overload that accepts an explicit keyType parameter.");
        }

        var metadata = await GetSlotMetadataAsync(slot, cancellationToken).ConfigureAwait(false);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                $"Slot 0x{(byte)slot:X2} is empty. Generate or import a key before signing/decrypting.");
        }

        var slotMetadata = metadata.Value;
        var keyType = slotMetadata.RequireKeyType();
        EnsureKeyOperationSupported(keyType, PinPolicy.Default, TouchPolicy.Default, generate: false);

        UserPresenceNotification userPresenceNotification =
            CreateUserPresenceNotification(CreateUserPresenceContext(slot, slotMetadata, UserPresenceOperations.Piv.SignOrDecrypt));
        return await SignOrDecryptWithUserPresenceAsync(
                slot,
                keyType,
                data,
                userPresenceNotification,
                cancellationToken)
            .ConfigureAwait(false);
    }
#pragma warning restore RS0026

    /// <inheritdoc />
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
            EnsureKeyOperationSupported(slotMetadata.RequireKeyType(), PinPolicy.Default, TouchPolicy.Default, generate: false);

        return await RunWithUserPresenceResolutionAsync(
                userPresenceNotification,
                token => PrivateKeyOperations.DecryptAsync(
                    _backend,

                    metadata,
                    slot,
                    cipherText,
                    padding,
                    userPresenceNotification,
                    token),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Memory<byte>> CalculateSecretAsync(
        Slot slot,
        IPublicKey peerPublicKey,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        ArgumentNullException.ThrowIfNull(peerPublicKey);
        EnsureKeyOperationSupported(peerPublicKey.KeyType, PinPolicy.Default, TouchPolicy.Default, generate: false);

        UserPresenceNotification userPresenceNotification = CreateUserPresenceNotification(
            await GetUserPresenceContextAsync(slot, UserPresenceOperations.Piv.CalculateSecret, cancellationToken).ConfigureAwait(false));
        return await RunWithUserPresenceResolutionAsync(
                userPresenceNotification,
                token => PrivateKeyOperations.CalculateSecretAsync(
                    _backend,

                    slot,
                    peerPublicKey,
                    userPresenceNotification,
                    token),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<X509Certificate2?> GetCertificateAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        return CertificateProtocol.GetCertificateAsync(_backend, slot, cancellationToken);
    }

    /// <inheritdoc />
    public Task StoreCertificateAsync(
        Slot slot,
        X509Certificate2 certificate,
        CertificateCompression compression = CertificateCompression.Automatic,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        EnsureManagementKeyAuthenticated();
        return CertificateProtocol.StoreCertificateAsync(_backend, slot, certificate, compression, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteCertificateAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        EnsureManagementKeyAuthenticated();
        return CertificateProtocol.DeleteCertificateAsync(_backend, slot, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PukMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        EnsureSupports(Features.Metadata);

        return MetadataProtocol.GetPukMetadataAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ManagementKeyMetadata> GetManagementKeyMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        EnsureSupports(Features.Metadata);

        return MetadataProtocol.GetManagementKeyMetadataAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SlotMetadata?> GetSlotMetadataAsync(Slot slot, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);
        EnsureSupports(Features.Metadata);

        return MetadataProtocol.GetSlotMetadataAsync(_backend, slot, cancellationToken);
    }

    /// <inheritdoc />
    public Task<BioMetadata> GetBioMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return BioProtocol.GetBioMetadataAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Memory<byte>> GetObjectAsync(int objectId, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return await DataObjectProtocol.GetObjectAsync(_backend, objectId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task PutObjectAsync(int objectId, ReadOnlyMemory<byte>? data, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        return DataObjectProtocol.PutObjectAsync(_backend, objectId, data, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CardholderUniqueId> GetCardholderUniqueIdAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return TypedDataObjectProtocol.GetCardholderUniqueIdAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetCardholderUniqueIdAsync(CardholderUniqueId cardholderUniqueId, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        return TypedDataObjectProtocol.SetCardholderUniqueIdAsync(_backend, cardholderUniqueId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CardCapabilityContainer> GetCardCapabilityContainerAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return TypedDataObjectProtocol.GetCardCapabilityContainerAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetCardCapabilityContainerAsync(CardCapabilityContainer cardCapabilityContainer, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        return TypedDataObjectProtocol.SetCardCapabilityContainerAsync(_backend, cardCapabilityContainer, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AdminData> GetAdminDataAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return TypedDataObjectProtocol.GetAdminDataAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetAdminDataAsync(AdminData adminData, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        return TypedDataObjectProtocol.SetAdminDataAsync(_backend, adminData, cancellationToken);
    }

    /// <inheritdoc />
    public Task<KeyHistory> GetKeyHistoryAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return TypedDataObjectProtocol.GetKeyHistoryAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetKeyHistoryAsync(KeyHistory keyHistory, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        EnsureManagementKeyAuthenticated();
        return TypedDataObjectProtocol.SetKeyHistoryAsync(_backend, keyHistory, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PinOnlyMode> GetPinOnlyModeAsync(CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return PinOnlyProtocol.GetPinOnlyModeAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PinOnlyMode> RecoverPinOnlyModeAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        return RecoverPinOnlyModeCoreAsync(pin, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetPinOnlyModeAsync(
        PinOnlyMode pinOnlyMode,
        ReadOnlyMemory<byte> pin,
        ReadOnlyMemory<byte>? managementKey = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        EnsureManagementKeyAuthenticated();

        // Only the protected-mode bit may be set; zero disables PIN-only mode. Derived/unavailable
        // bits describe existing state and are not legal requests for creating a new mode.
        if ((pinOnlyMode & ~(PinOnlyMode.None | PinOnlyMode.PinProtected)) != 0)
        {
            throw new ArgumentException(
                "Only PinOnlyMode.None (disable) and PinOnlyMode.PinProtected (enable) are " +
                "supported. PIN-derived management keys are deprecated and cannot be enabled through " +
                "this API.",
                nameof(pinOnlyMode));
        }

        if (pinOnlyMode == PinOnlyMode.PinProtected)
        {
            if (pin.Length is < 6 or > 8)
                throw new ArgumentException("PIN must be 6-8 bytes.", nameof(pin));
            if (managementKey is not { } keyToProtect)
            {
                throw new ArgumentNullException(nameof(managementKey), "A management key is required to enable PIN-protected mode.");
            }

            int expectedKeyLength = RequireManagementKeyType().GetKeySizeBytes();
            if (keyToProtect.Length != expectedKeyLength)
            {
                throw new ArgumentException(
                    $"Invalid management key length {keyToProtect.Length} for {ManagementKeyType}. Expected {expectedKeyLength} bytes.",
                    nameof(managementKey));
            }

            await EnablePinProtectedModeAsync(pin, keyToProtect, cancellationToken).ConfigureAwait(false);
            return;
        }
        await DisablePinOnlyModeAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetManagementKeyAsync(
        KeyType keyType,
        ReadOnlyMemory<byte> newKey,
        bool requireTouch = false,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        _ = KeyTypeEncoding.EncodeManagement(keyType);
        if (keyType != KeyType.TripleDes)
            EnsureSupports(Features.AesKey);
        if (requireTouch)
            EnsureSupports(Features.UsagePolicy);

        EnsureManagementKeyAuthenticated();
        try
        {
            ManagementKeyType = await AuthenticationProtocol.SetManagementKeyAsync(
                _backend,

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

    /// <inheritdoc />
    public async Task<Memory<byte>?> VerifyUvAsync(
        UserVerification userVerification = UserVerification.Verify,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        var temporaryPin = await BioProtocol.VerifyUvAsync(_backend, userVerification, cancellationToken).ConfigureAwait(false);
        return temporaryPin is null ? null : temporaryPin.AsMemory();
    }

    /// <inheritdoc />
    public Task VerifyTemporaryPinAsync(ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);

        return BioProtocol.VerifyTemporaryPinAsync(_backend, temporaryPin, cancellationToken);
    }

    private Task<byte[]> SignOrDecryptWithUserPresenceAsync(
        Slot slot,
        KeyType keyType,
        ReadOnlyMemory<byte> data,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);

        return RunWithUserPresenceResolutionAsync(
            userPresenceNotification,
            token => PrivateKeyOperations.SignOrDecryptAsync(
                _backend,

                slot,
                keyType,
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
        SlotMetadata? metadata,
        string operation)
    {
        if (metadata is null)
            return null;

        UserPresenceBasis? basis = metadata.Value.TouchPolicy switch
        {
            TouchPolicy.Always => UserPresenceBasis.PolicyRequires,
            TouchPolicy.Cached => UserPresenceBasis.PolicyMayRequire,
            TouchPolicy.Never or TouchPolicy.Default => null,
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

    private KeyType RequireManagementKeyType()
    {
        var known = ManagementKeyType ?? throw new NotSupportedException("The management-key algorithm is not recognized. Inspect its raw metadata identifier.");
        _ = KeyTypeEncoding.EncodeManagement(known);
        return known;
    }

    private void EnsureKeyOperationSupported(KeyType keyType, PinPolicy pinPolicy, TouchPolicy touchPolicy, bool generate)
    {
        _ = KeyTypeEncoding.EncodeAsymmetric(keyType);
        Feature? feature = keyType switch
        {
            KeyType.P384 => Features.P384,
            KeyType.Ed25519 or KeyType.X25519 => Features.Cv25519,
            KeyType.Rsa3072 or KeyType.Rsa4096 => Features.Rsa3072Rsa4096,
            _ => null
        };
        if (feature is { } required)
            EnsureSupports(required);
        EnsureKeyPoliciesSupported(pinPolicy, touchPolicy);
        if (generate && keyType.IsRsa() && !Features.SupportsRsaGeneration(FirmwareVersion))
            throw new NotSupportedException("RSA key generation is disabled on ROCA-affected firmware 4.2.6–4.3.4.");
        EnsureFipsKeyPolicySupported(keyType, pinPolicy);
    }

    private void EnsureKeyPoliciesSupported(PinPolicy pinPolicy, TouchPolicy touchPolicy)
    {
        if (!Enum.IsDefined(pinPolicy) || !Enum.IsDefined(touchPolicy))
            throw new ArgumentException("Unknown PIV key policy.");
        if (pinPolicy != PinPolicy.Default || touchPolicy != TouchPolicy.Default)
            EnsureSupports(Features.UsagePolicy);
        if (touchPolicy == TouchPolicy.Cached)
            EnsureSupports(Features.TouchCached);
    }

    private async Task EnsureBiometricPolicySupportedAsync(PinPolicy policy, CancellationToken cancellationToken)
    {
        // Probe the Bio metadata capability for MATCH policies. A successful response proves support,
        // not fingerprint enrollment: intentionally do not reject IsConfigured == false (Python parity).
        if (policy is PinPolicy.MatchOnce or PinPolicy.MatchAlways)
            _ = await GetBioMetadataAsync(cancellationToken).ConfigureAwait(false);
    }

    private void EnsureFipsKeyPolicySupported(KeyType keyType, PinPolicy pinPolicy)
    {
        if (!FirmwareVersion.IsAlphaOrBeta && FirmwareVersion.IsAtLeast(4, 4, 0) && !FirmwareVersion.IsAtLeast(4, 5, 0)
            && (keyType == KeyType.Rsa1024 || pinPolicy == PinPolicy.Never))
            throw new NotSupportedException("RSA1024 and PIN policy Never are not supported on YubiKey 4 FIPS.");
    }

    private async Task<PinOnlyMode> RecoverPinOnlyModeCoreAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
        Logger.LogDebug("PIV: Attempting to recover PIN-only management key authentication");

        var mode = PinOnlyMode.None;
        var (printedRaw, pinVerified) = await ReadPrintedForRecoveryAsync(pin, cancellationToken).ConfigureAwait(false);

        Memory<byte> storedKey = Memory<byte>.Empty;
        bool pinProtectedAuthenticated = false;
        try
        {
            if (PinOnlyProtocol.TryDecodePinProtectedManagementKey(printedRaw, out storedKey))
            {
                pinProtectedAuthenticated = await TryAuthenticateCandidateAsync(storedKey, cancellationToken).ConfigureAwait(false);
                if (pinProtectedAuthenticated)
                    mode |= PinOnlyMode.PinProtected;
            }

            // Try PIN-derived: derive a candidate management key from the PIN and the ADMIN DATA salt.
            var adminRaw = await DataObjectProtocol.GetObjectAsync(_backend, DataObject.AdminData, cancellationToken).ConfigureAwait(false);

            if (AdminData.TryDecode(adminRaw, out var adminData) && adminData.Salt is { } salt)
            {
                if (!pinVerified)
                    await VerifyPinAsync(pin, cancellationToken).ConfigureAwait(false);
                if (await TryAuthenticateDerivedCandidateAsync(pin, salt, cancellationToken).ConfigureAwait(false))
                    mode |= PinOnlyMode.PinDerived;
                else if (pinProtectedAuthenticated && !await TryAuthenticateCandidateAsync(storedKey, cancellationToken).ConfigureAwait(false))
                    mode &= ~PinOnlyMode.PinProtected;
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
            return (await DataObjectProtocol.GetObjectAsync(_backend, DataObject.PrintedInformation, cancellationToken)
                .ConfigureAwait(false), false);
        }
        catch (ApduException ex) when (ex.SW == SWConstants.SecurityStatusNotSatisfied)
        {
            await VerifyPinAsync(pin, cancellationToken).ConfigureAwait(false);
            return (await DataObjectProtocol.GetObjectAsync(_backend, DataObject.PrintedInformation, cancellationToken)
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
        int length = RequireManagementKeyType().GetKeySizeBytes();
        byte[] candidate = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            PinOnlyProtocol.DeriveManagementKey(pin.Span, salt.Span, candidate.AsSpan(0, length));
            return await TryAuthenticateCandidateAsync(candidate.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(candidate.AsSpan(0, length));
            ArrayPool<byte>.Shared.Return(candidate);
        }
    }

    private async Task EnablePinProtectedModeAsync(ReadOnlyMemory<byte> pin, ReadOnlyMemory<byte> keyToProtect, CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
        Logger.LogDebug("PIV: Enabling PIN-protected management key mode");

        await AuthenticateAsync(keyToProtect, cancellationToken).ConfigureAwait(false);
        await VerifyPinAsync(pin, cancellationToken).ConfigureAwait(false);
        await PinOnlyProtocol.StorePinProtectedManagementKeyAsync(_backend, keyToProtect, cancellationToken).ConfigureAwait(false);
        await AuthenticationProtocol.BlockPukAsync(_backend, cancellationToken).ConfigureAwait(false);

        var currentAdminRaw = await DataObjectProtocol.GetObjectAsync(_backend, DataObject.AdminData, cancellationToken)
            .ConfigureAwait(false);
        _ = AdminData.TryDecode(currentAdminRaw, out var currentAdmin);
        var updatedAdmin = AdminData.Create(pukBlocked: true, pinProtected: true, currentAdmin.Salt, currentAdmin.PinLastUpdated);
        EnsureManagementKeyAuthenticated();
        await TypedDataObjectProtocol.SetAdminDataAsync(_backend, updatedAdmin, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task DisablePinOnlyModeAsync(CancellationToken cancellationToken)
    {
        EnsureBackend(cancellationToken);
        // pinOnlyMode == None: disable, but only if a PIN-only mode is actually currently set.
        var currentMode = await GetPinOnlyModeAsync(cancellationToken).ConfigureAwait(false);
        if (!currentMode.HasFlag(PinOnlyMode.PinProtected) && !currentMode.HasFlag(PinOnlyMode.PinDerived))
        {
            Logger.LogDebug("PIV: PIN-only mode is already disabled; no changes made");
            return;
        }

        Logger.LogDebug("PIV: Disabling PIN-only management key mode");
        var keyType = RequireManagementKeyType();
        // Borrow the read-only, nonsecret factory pattern. The setter owns/clears its encoded copy,
        // never this shared default; only the type-appropriate slice is transmitted.
        await SetManagementKeyAsync(keyType, AuthenticationProtocol.GetDefaultManagementKey(keyType), false, cancellationToken)
            .ConfigureAwait(false);

        EnsureManagementKeyAuthenticated();
        await DataObjectProtocol.PutObjectAsync(_backend, DataObject.PrintedInformation, null, cancellationToken)
            .ConfigureAwait(false);
        EnsureManagementKeyAuthenticated();
        await DataObjectProtocol.PutObjectAsync(_backend, DataObject.AdminData, null, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void ValidateSlot(Slot slot)
    {
        if (!Enum.IsDefined(slot))
            throw new ArgumentOutOfRangeException(nameof(slot), "Unknown PIV asymmetric key reference.");
    }
}
