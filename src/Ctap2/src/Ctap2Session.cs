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

using Microsoft.Extensions.Logging;
using System.Formats.Cbor;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Ctap2.Backend;
using Yubico.YubiKit.Ctap2.Cbor;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2;

/// <summary>
/// Provides FIDO2/CTAP2 session operations for YubiKey authenticators.
/// </summary>
/// <remarks>
/// <para>
/// Exposes CTAP2 operations and selected optional features over FIDO HID and SmartCard (CCID).
/// GetInfo advertisement, current configuration, product, firmware, transport and PIN/UV permissions
/// determine availability; the firmware feature thresholds below are not complete support checks.
/// See <see cref="ICtap2Session"/> for caller verification, sequencing, cancellation and byte-lifetime
/// responsibilities. Secure-channel IsAuthenticated state is not CTAP PIN/UV authorization.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html
/// </para>
/// </remarks>
public sealed class Ctap2Session : ApplicationSession, ICtap2Session
{
    private const int DefaultMaxMessageSize = 1024;
    private int _maxMessageSize = DefaultMaxMessageSize;
    private int? _maxCredBlobLength;
    /// <summary>
    /// Nominal firmware threshold for FIDO2 (5.0+), not a per-device capability or authorization check.
    /// </summary>
    public static readonly Feature FeatureFido2 = new("FIDO2", 5, 0, 0);

    /// <summary>
    /// Nominal firmware threshold for Bio Enrollment (5.2+); product and GetInfo support still matter.
    /// </summary>
    public static readonly Feature FeatureBioEnrollment = new("Bio Enrollment", 5, 2, 0);

    /// <summary>
    /// Nominal firmware threshold for Credential Management (5.2+), not permission to manage credentials.
    /// </summary>
    public static readonly Feature FeatureCredentialManagement = new("Credential Management", 5, 2, 0);

    /// <summary>
    /// Nominal firmware threshold for hmac-secret-mc (5.4+); check extension advertisement separately.
    /// </summary>
    public static readonly Feature FeatureHmacSecretMc = new("hmac-secret-mc", 5, 4, 0);

    /// <summary>
    /// Nominal firmware threshold for Authenticator Config (5.4+), not a configured-state or permission check.
    /// </summary>
    public static readonly Feature FeatureAuthenticatorConfig = new("Authenticator Config", 5, 4, 0);

    /// <summary>
    /// Nominal firmware threshold for credBlob (5.5+); check extension advertisement and size limits separately.
    /// </summary>
    public static readonly Feature FeatureCredBlob = new("credBlob", 5, 5, 0);

    /// <summary>
    /// Nominal firmware threshold for Encrypted Identifier (5.7+), not a guarantee the field is returned.
    /// </summary>
    public static readonly Feature FeatureEncIdentifier = new("Encrypted Identifier", 5, 7, 0);

    private static readonly Feature FeatureFido2UsbSmartCard = new("FIDO2 over USB SmartCard", 5, 8, 0);

    private readonly ILogger _logger;
    private readonly ProtocolConfiguration? _configuration;
    private readonly FirmwareVersion? _firmwareVersionOverride;
    private readonly ScpKeyParameters? _scpKeyParameters;
    private readonly DeviceInfo? _cachedDeviceInfo;
    private Exception? _resetRefreshFailure;

    private ICtap2Backend _backend = null!;

    private Ctap2Session(
        IConnection connection,
        SessionCreationOptions? options,
        DeviceInfo? cachedDeviceInfo)
        : base(connection, options?.UserPresencePrompt)
    {
        _logger = Logger;
        _configuration = options?.ProtocolConfiguration;
        _firmwareVersionOverride = options?.FirmwareVersionOverride;
        _scpKeyParameters = options?.ScpKeyParameters;
        _cachedDeviceInfo = cachedDeviceInfo;
    }

    /// <summary>
    /// Creates and initializes a CTAP2 session using a caller-owned connection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The session borrows the connection; dispose the session before disposing or reusing the
    /// connection. PreferredConnectionType asserts the supplied connection's type, rather than opening
    /// or switching transports. The caller also owns supplied SCP key parameters and the presence prompt.
    /// After healthy sequential use an idle borrowed connection can host another session, but disposal
    /// during an active built-in macOS HID exchange can leave it unusable. After uncertain exchange or
    /// teardown state, dispose and reopen the connection rather than assuming recovery.
    /// </para>
    /// <para>
    /// HID channel preparation precedes GetInfo. SmartCard selects the FIDO AID and reads GetInfo
    /// before the USB firmware gate and SCP establishment; NFC skips that USB gate. The gate uses
    /// detected GetInfo firmware, not FirmwareVersionOverride. Missing firmware becomes 0.0.0 and
    /// Feature treats Major == 0 sentinel versions as supported; this is not proof of actual firmware.
    /// The override configures session/protocol firmware after that gate, and later GetInfo calls do
    /// not update it. SCP on HID is rejected before channel initialization or GetInfo.
    /// </para>
    /// <para>
    /// Initial SmartCard SELECT/GetInfo precede options-based configuration. For major-zero firmware,
    /// Core selects APDU format from connection support before ordinary ForceShortApdus handling.
    /// SCP setup rebinds the backend without another SELECT or protected CTAP probe; successful setup
    /// alone does not qualify applet-selection survival or FIDO-over-SCP interoperability.
    /// Await complete operations and helper workflows before disposal; the exchange drain is not a
    /// whole-workflow join. Keep caller-owned prompts usable through their final callbacks.
    /// </para>
    /// </remarks>
    /// <param name="connection">The connection to the YubiKey (SmartCard or FIDO HID).</param>
    /// <param name="options">Optional cross-cutting session creation settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized Ctap2Session.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="connection"/> is null.</exception>
    /// <exception cref="NotSupportedException">If the connection type is not supported.</exception>
    public static Task<Ctap2Session> CreateAsync(
        IConnection connection,
        SessionCreationOptions? options = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(connection, options, cachedDeviceInfo: null, cancellationToken);

    internal static Task<Ctap2Session> CreateAsync(
        IConnection connection,
        SessionCreationOptions? options,
        DeviceInfo? cachedDeviceInfo,
        CancellationToken cancellationToken) =>
        ConstructInitializedAsync(
            connection,
            options,
            () => new Ctap2Session(connection, options, cachedDeviceInfo),
            (session, token) => session.InitializeAsync(token),
            cancellationToken);

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;

        var protocol = await PrepareProtocolAsync(_scpKeyParameters, cancellationToken).ConfigureAwait(false);
        var backend = CreateBackend(protocol);
        if (backend is SmartCardBackend smartCardBackend)
            await smartCardBackend.SelectApplicationAsync(cancellationToken).ConfigureAwait(false);

        // The transport gate uses detected firmware; an override only affects later configuration.
        var info = await GetInfoCoreAsync(backend, cancellationToken).ConfigureAwait(false);
        var detectedFirmwareVersion = _cachedDeviceInfo?.FirmwareVersion ?? info.FirmwareVersion ?? new FirmwareVersion();

        if (Connection is ISmartCardConnection smartCardConnection)
        {
            EnsureSmartCardTransportSupported(
                smartCardConnection.Transport, detectedFirmwareVersion, _cachedDeviceInfo?.VersionQualifier);
        }

        var effectiveProtocol = await ConfigureProtocolAsync(
                protocol,
                GetConfigurationFirmwareVersion(detectedFirmwareVersion, info.FirmwareVersion),
                _configuration,
                _scpKeyParameters,
                cancellationToken)
            .ConfigureAwait(false);

        if (!ReferenceEquals(protocol, effectiveProtocol))
        {
            backend = CreateBackend(effectiveProtocol);
        }

        _backend = backend;
        _maxMessageSize = info.MaxMsgSize ?? DefaultMaxMessageSize;
        _maxCredBlobLength = info.MaxCredBlobLength;

        _logger.LogDebug(
            "FIDO session initialized. Firmware: {Version}, Versions: [{Versions}]",
            detectedFirmwareVersion,
            string.Join(", ", info.Versions));
    }

    private FirmwareVersion GetConfigurationFirmwareVersion(FirmwareVersion detected, FirmwareVersion? applet)
    {
        if (_firmwareVersionOverride is { } configured)
            return configured;

        // A major-zero cache can pass the USB gate without outranking numeric GetInfo for APDU framing.
        // A qualified prerelease with a numeric version keeps its numeric framing.
        return Connection is ISmartCardConnection && detected.IsAlphaOrBeta && applet is { Major: > 0 }
            ? applet
            : detected;
    }

    /// <inheritdoc />
    public Task<AuthenticatorInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        return GetInfoCoreAsync(_backend, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SelectionAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await SendCborAsync(
                CtapCommand.Selection,
                CreateUserPresenceNotification(ConnectionType is ConnectionType.HidFido
                    ? new UserPresenceContext
                    {
                        Basis = UserPresenceBasis.PolicyMayRequire,
                        Application = UserPresenceApplications.Fido2,
                        Operation = UserPresenceOperations.Ctap2.Selection
                    }
                    : null),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        var notification = CreateUserPresenceNotification(ConnectionType is ConnectionType.HidFido
            ? new UserPresenceContext
            {
                Basis = UserPresenceBasis.PolicyMayRequire,
                Application = UserPresenceApplications.Fido2,
                Operation = UserPresenceOperations.Ctap2.Reset
            }
            : null);
        bool confirmed = false;
        Exception? notificationFailure = null;
        try
        {
            await _backend.SendCborAsync(new byte[] { CtapCommand.Reset }, notification, cancellationToken,
                onSuccess: () => confirmed = true).ConfigureAwait(false);
        }
        catch (Exception ex) when (confirmed)
        {
            // A logger or terminal callback can fail after the successful CTAP status is known.
            notificationFailure = ex;
        }

        await RefreshAfterResetAsync().ConfigureAwait(false);
        if (notificationFailure is not null)
            ExceptionDispatchInfo.Throw(notificationFailure);
        _logger.LogInformation("FIDO application reset completed and session refreshed");
    }

    private async Task RefreshAfterResetAsync()
    {
        IsInitialized = false;
        IsAuthenticated = false;
        FirmwareVersion = new FirmwareVersion();
        _maxMessageSize = DefaultMaxMessageSize;
        _maxCredBlobLength = null;
        _backend = null!;

        try
        {
            var oldProtocol = Protocol;
            Protocol = null;
            if (oldProtocol is IAsyncDisposable asyncProtocol)
                await asyncProtocol.DisposeAsync().ConfigureAwait(false);
            else
                oldProtocol?.Dispose();

            // The authenticator has already reset. Complete or end the session even if the caller cancelled.
            await InitializeSessionAsync(InitializeAsync, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _resetRefreshFailure = ex;
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
        catch (Exception cleanupFailure) when (_resetRefreshFailure is not null)
        {
            // Don't cache a secondary failure which would be replayed by the caller's await using.
            _resetRefreshFailure.Data["ResetRefreshCleanupException"] = cleanupFailure;
        }
    }

    /// <inheritdoc />
    public async Task<MakeCredentialResponse> MakeCredentialAsync(
        ReadOnlyMemory<byte> clientDataHash,
        PublicKeyCredentialRpEntity rp,
        PublicKeyCredentialUserEntity user,
        IReadOnlyList<PublicKeyCredentialParameters> pubKeyCredParams,
        MakeCredentialOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(rp);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(pubKeyCredParams);
        if (options?.UserPresence is false)
        {
            // CTAP 2.1-2.3 §6.1: "its value MUST be true if present. The value false will cause a
            // CTAP2_ERR_INVALID_OPTION response regardless of authenticator version."
            throw new ArgumentException(
                "UserPresence cannot be false for makeCredential; use getAssertion with up=false for a silent probe.",
                nameof(options));
        }

        if (clientDataHash.Length != 32)
        {
            throw new ArgumentException(
                "Client data hash must be exactly 32 bytes (SHA-256).",
                nameof(clientDataHash));
        }

        if (pubKeyCredParams.Count == 0)
        {
            throw new ArgumentException(
                "At least one credential parameter must be specified.",
                nameof(pubKeyCredParams));
        }

        Ctap2SessionRequestEncoding.ThrowIfDuplicateCredentialParameters(pubKeyCredParams, nameof(pubKeyCredParams));

        EnsureInitialized();
        ValidateCredBlob(options);

        byte[]? request = null;
        ReadOnlyMemory<byte> response;
        try
        {
            request = Ctap2SessionRequestEncoding.BuildMakeCredentialRequest(
                clientDataHash, rp, user, pubKeyCredParams, options);

            _logger.LogDebug("MakeCredential for RP: {RpId}", rp.Id);

            // This is a presence-policy notification, not verification of the returned UP/UV flags.
            UserPresenceNotification userPresenceNotification = CreateUserPresenceNotification(
                new UserPresenceContext
                {
                    Basis = UserPresenceBasis.PolicyRequires,
                    Application = UserPresenceApplications.Fido2,
                    Operation = UserPresenceOperations.Ctap2.MakeCredential,
                    Scope = rp.Id
                });

            response = await SendWithinMessageLimitAsync(request, userPresenceNotification, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (request is not null)
            {
                // Clear our encoded request, not borrowed caller inputs or the transferred response.
                CryptographicOperations.ZeroMemory(request);
            }
        }

        // CTAP status and terminal notification resolution precede decode; failure here is not rollback.
        return DecodeCredentialResponse(response, MakeCredentialResponse.Decode);
    }

    /// <inheritdoc />
    public async Task<GetAssertionResponse> GetAssertionAsync(
        string rpId,
        ReadOnlyMemory<byte> clientDataHash,
        GetAssertionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentException.ThrowIfNullOrEmpty(rpId);

        if (clientDataHash.Length != 32)
        {
            throw new ArgumentException(
                "Client data hash must be exactly 32 bytes (SHA-256).",
                nameof(clientDataHash));
        }

        EnsureInitialized();

        byte[]? request = null;
        ReadOnlyMemory<byte> response;
        try
        {
            request = Ctap2SessionRequestEncoding.BuildGetAssertionRequest(rpId, clientDataHash, options);

            _logger.LogDebug("GetAssertion for RP: {RpId}", rpId);

            UserPresenceNotification userPresenceNotification = CreateUserPresenceNotification(
                options?.UserPresence is false
                    ? null
                    : new UserPresenceContext
                    {
                        Basis = UserPresenceBasis.PolicyRequires,
                        Application = UserPresenceApplications.Fido2,
                        Operation = UserPresenceOperations.Ctap2.GetAssertion,
                        Scope = rpId
                    });

            response = await SendWithinMessageLimitAsync(request, userPresenceNotification, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (request is not null)
            {
                // Clear our encoded request, not borrowed caller inputs or decoded response copies.
                CryptographicOperations.ZeroMemory(request);
            }
        }

        // Parsing is not relying-party verification and can fail after a successful device exchange.
        return DecodeCredentialResponse(response, GetAssertionResponse.Decode);
    }

    /// <inheritdoc />
    public async Task<GetAssertionResponse> GetNextAssertionAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        // The caller preserves device sequence adjacency and tracks the first response's count.
        var request = CtapRequestBuilder.Create(CtapCommand.GetNextAssertion).Build();

        var response = await SendWithinMessageLimitAsync(
                request,
                UserPresenceNotification.None,
                cancellationToken)
            .ConfigureAwait(false);

        return DecodeCredentialResponse(response, GetAssertionResponse.Decode);
    }

    private void ValidateCredBlob(MakeCredentialOptions? options)
    {
        if (options?.Extensions is not { Length: > 0 } extensions)
        {
            return;
        }

        var reader = new CborReader(extensions, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        while (reader.PeekState() is not CborReaderState.EndMap)
        {
            if (reader.ReadTextString() != Extensions.ExtensionIdentifiers.CredBlob)
            {
                reader.SkipValue();
                continue;
            }

            int length = reader.ReadDefiniteLengthByteString().Length;
            if (length == 0)
            {
                throw new ArgumentException("credBlob must not be empty.", nameof(options));
            }
            if (_maxCredBlobLength is not int limit)
            {
                throw new NotSupportedException("The authenticator did not advertise a credBlob capacity.");
            }
            if (length > limit)
            {
                throw new ArgumentException($"credBlob exceeds the authenticator's {limit}-byte capacity.", nameof(options));
            }
        }
        reader.ReadEndMap();
    }

    private static T DecodeCredentialResponse<T>(ReadOnlyMemory<byte> response, Func<ReadOnlyMemory<byte>, T> decode)
    {
        try
        {
            return decode(response);
        }
        finally
        {
            // Both backends transfer their writable response storage to this session.
            // Typed decoders no longer retain any view of it, even on success.
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
        }
    }

    /// <summary>
    /// Sends a CTAP CBOR command to the authenticator.
    /// </summary>
    /// <param name="command">The CTAP command byte.</param>
    /// <param name="userPresenceNotification">The non-null notification handle for this SDK operation.</param>
    /// <param name="payload">Optional CBOR-encoded payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CBOR-encoded response data.</returns>
    internal async Task<ReadOnlyMemory<byte>> SendCborAsync(
        byte command,
        UserPresenceNotification userPresenceNotification,
        ReadOnlyMemory<byte>? payload = null,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        ReadOnlyMemory<byte> request;
        if (payload.HasValue)
        {
            var requestArray = new byte[1 + payload.Value.Length];
            requestArray[0] = command;
            payload.Value.CopyTo(requestArray.AsMemory(1));
            request = requestArray;
        }
        else
        {
            request = new byte[] { command };
        }

        return await SendWithinMessageLimitAsync(request, userPresenceNotification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> SendCborRequestAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        return await SendWithinMessageLimitAsync(request, UserPresenceNotification.None, cancellationToken)
            .ConfigureAwait(false);
    }

    private Task<ReadOnlyMemory<byte>> SendWithinMessageLimitAsync(
        ReadOnlyMemory<byte> request,
        UserPresenceNotification notification,
        CancellationToken cancellationToken)
    {
        if (request.Length > _maxMessageSize)
            throw new ArgumentException($"The CTAP request exceeds the authenticator's {_maxMessageSize}-byte message limit.", nameof(request));

        return _backend.SendCborAsync(request, notification, cancellationToken);
    }

    private static async Task<AuthenticatorInfo> GetInfoCoreAsync(
        ICtap2Backend backend,
        CancellationToken cancellationToken)
    {
        var request = CtapRequestBuilder.Create(CtapCommand.GetInfo).Build();
        var response = await backend.SendCborAsync(request, UserPresenceNotification.None, cancellationToken)
            .ConfigureAwait(false);
        return AuthenticatorInfo.Decode(response);
    }

    private void EnsureInitialized()
    {
        ThrowIfDisposed();

        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                "Session is not initialized. Use Ctap2Session.CreateAsync() to create a session.");
        }
    }

    private ICtap2Backend CreateBackend(IProtocol protocol) =>
        protocol switch
        {
            ISmartCardProtocol smartCard => new SmartCardBackend(smartCard),
            IFidoHidProtocol fidoHid => new HidBackend(fidoHid),
            _ => throw new NotSupportedException(
                $"Protocol type {protocol.GetType().Name} is not supported. " +
                "Use ISmartCardConnection or IFidoHidConnection.")
        };

    internal static void EnsureSmartCardTransportSupported(
        Transport transport,
        FirmwareVersion firmwareVersion,
        VersionQualifier? qualifier = null)
    {
        if (transport == Transport.Nfc)
        {
            return;
        }

        if (qualifier?.Type is VersionQualifierType.Alpha or VersionQualifierType.Beta ||
            FeatureFido2UsbSmartCard.IsSupportedByFirmware(firmwareVersion))
        {
            return;
        }

        throw new NotSupportedException(
            "FIDO2 over USB SmartCard requires firmware 5.8.0 or later. " +
            "For older USB-connected YubiKeys, use IFidoHidConnection instead.");
    }

}