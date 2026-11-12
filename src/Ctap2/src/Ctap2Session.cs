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

    private ICtap2Backend _backend = null!;

    private Ctap2Session(
        IConnection connection,
        IUserPresencePrompt? userPresencePrompt = null)
        : base(connection, userPresencePrompt)
    {
        _logger = Logger;
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
        ConstructInitializedAsync(
            connection,
            options,
            () => new Ctap2Session(connection, options?.UserPresencePrompt),
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

        var protocol = await PrepareProtocolAsync(scpKeyParams, cancellationToken).ConfigureAwait(false);
        var backend = CreateBackend(protocol);
        if (backend is SmartCardBackend smartCardBackend)
            await smartCardBackend.SelectApplicationAsync(cancellationToken).ConfigureAwait(false);

        // The transport gate uses detected firmware; an override only affects later configuration.
        var info = await GetInfoCoreAsync(backend, cancellationToken).ConfigureAwait(false);
        var detectedFirmwareVersion = info.FirmwareVersion ?? new FirmwareVersion();

        if (Connection is ISmartCardConnection smartCardConnection)
        {
            EnsureSmartCardTransportSupported(smartCardConnection.Transport, detectedFirmwareVersion);
        }

        // Configure the session firmware snapshot, then establish SCP and rebind the backend if needed.
        var effectiveProtocol = await ConfigureProtocolAsync(
                protocol,
                firmwareVersionOverride ?? detectedFirmwareVersion,
                configuration,
                scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        if (!ReferenceEquals(protocol, effectiveProtocol))
        {
            backend = CreateBackend(effectiveProtocol);
        }

        _backend = backend;

        _logger.LogDebug(
            "FIDO session initialized. Firmware: {Version}, Versions: [{Versions}]",
            detectedFirmwareVersion,
            string.Join(", ", info.Versions));
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
        await SendCborAsync(
                CtapCommand.Reset,
                CreateUserPresenceNotification(ConnectionType is ConnectionType.HidFido
                    ? new UserPresenceContext
                    {
                        Basis = UserPresenceBasis.PolicyMayRequire,
                        Application = UserPresenceApplications.Fido2,
                        Operation = UserPresenceOperations.Ctap2.Reset
                    }
                    : null),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        _logger.LogInformation("FIDO application reset completed");
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

            response = await _backend.SendCborAsync(request, userPresenceNotification, cancellationToken)
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
        var result = MakeCredentialResponse.Decode(response);

        _logger.LogInformation(
            "Credential created. Format: {Format}, CredentialId length: {Length}",
            result.Format,
            result.GetCredentialId().Length);

        return result;
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

            response = await _backend.SendCborAsync(request, userPresenceNotification, cancellationToken)
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
        var result = GetAssertionResponse.Decode(response);

        _logger.LogInformation(
            "Assertion obtained. NumberOfCredentials: {Count}",
            result.NumberOfCredentials ?? 1);

        return result;
    }

    /// <inheritdoc />
    public async Task<GetAssertionResponse> GetNextAssertionAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        // The caller preserves device sequence adjacency and tracks the first response's count.
        var request = CtapRequestBuilder.Create(CtapCommand.GetNextAssertion).Build();

        var response = await _backend.SendCborAsync(
                request,
                UserPresenceNotification.None,
                cancellationToken)
            .ConfigureAwait(false);

        return GetAssertionResponse.Decode(response);
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

        return await _backend.SendCborAsync(request, userPresenceNotification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> SendCborRequestAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        return await _backend.SendCborAsync(request, UserPresenceNotification.None, cancellationToken)
            .ConfigureAwait(false);
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
        FirmwareVersion firmwareVersion)
    {
        if (transport == Transport.Nfc)
        {
            return;
        }

        if (FeatureFido2UsbSmartCard.IsSupportedByFirmware(firmwareVersion))
        {
            return;
        }

        throw new NotSupportedException(
            "FIDO2 over USB SmartCard requires firmware 5.8.0 or later. " +
            "For older USB-connected YubiKeys, use IFidoHidConnection instead.");
    }

}