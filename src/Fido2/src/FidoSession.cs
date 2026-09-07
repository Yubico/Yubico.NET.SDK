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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Fido2.Backend;
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Provides FIDO2/CTAP2 session operations for YubiKey authenticators.
/// </summary>
/// <remarks>
/// <para>
/// Implements CTAP 2.1/2.3 specification. Supports both SmartCard (CCID) 
/// and FIDO HID transports.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html
/// </para>
/// </remarks>
public sealed class FidoSession : ApplicationSession, IFidoSession
{
    /// <summary>
    /// Feature flag for FIDO2 support (requires firmware 5.0+).
    /// </summary>
    public static readonly Feature FeatureFido2 = new("FIDO2", 5, 0, 0);

    /// <summary>
    /// Feature flag for Bio Enrollment support (requires firmware 5.2+).
    /// </summary>
    public static readonly Feature FeatureBioEnrollment = new("Bio Enrollment", 5, 2, 0);

    /// <summary>
    /// Feature flag for Credential Management support (requires firmware 5.2+).
    /// </summary>
    public static readonly Feature FeatureCredentialManagement = new("Credential Management", 5, 2, 0);

    /// <summary>
    /// Feature flag for hmac-secret-mc extension (requires firmware 5.4+).
    /// </summary>
    public static readonly Feature FeatureHmacSecretMc = new("hmac-secret-mc", 5, 4, 0);

    /// <summary>
    /// Feature flag for Authenticator Config support (requires firmware 5.4+).
    /// </summary>
    public static readonly Feature FeatureAuthenticatorConfig = new("Authenticator Config", 5, 4, 0);

    /// <summary>
    /// Feature flag for credBlob extension (requires firmware 5.5+).
    /// </summary>
    public static readonly Feature FeatureCredBlob = new("credBlob", 5, 5, 0);

    /// <summary>
    /// Feature flag for Encrypted Identifier support (requires firmware 5.7+).
    /// </summary>
    public static readonly Feature FeatureEncIdentifier = new("Encrypted Identifier", 5, 7, 0);

    private static readonly Feature FeatureFido2UsbSmartCard = new("FIDO2 over USB SmartCard", 5, 8, 0);

    private readonly ScpKeyParameters? _scpKeyParams;
    private readonly ILogger _logger;

    private IFidoBackend _backend = null!;

    private FidoSession(IConnection connection, ScpKeyParameters? scpKeyParams = null)
        : base(connection)
    {
        _scpKeyParams = scpKeyParams;
        _logger = Logger;
    }

    /// <summary>
    /// Creates and initializes a FIDO session from a connection.
    /// </summary>
    /// <param name="connection">The connection to the YubiKey (SmartCard or FIDO HID).</param>
    /// <param name="options">Optional cross-cutting session creation settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized FidoSession.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="connection"/> is null.</exception>
    /// <exception cref="NotSupportedException">If the connection type is not supported.</exception>
    public static async Task<FidoSession> CreateAsync(
        IConnection connection,
        SessionCreationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var configuration = options?.ProtocolConfiguration;
        var scpKeyParams = options?.ScpKeyParameters;
        var firmwareVersionOverride = options?.FirmwareVersionOverride;

        ValidatePreferredConnectionType(connection, options);

        // A session that fails to initialize must not keep its claim on the connection: the connection
        // outlives it, and the next session over it would otherwise be refused forever.
        var session = Construct(connection, () => new FidoSession(connection, scpKeyParams));
        try
        {
            await session.InitializeAsync(configuration, firmwareVersionOverride, cancellationToken).ConfigureAwait(false);
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
        FirmwareVersion? firmwareVersionOverride,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;

        var protocol = ProtocolFactory.Create(Connection);
        Protocol = protocol;
        var backend = CreateBackend(protocol);
        await backend.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // Get firmware version from authenticator info
        var info = await GetInfoCoreAsync(backend, cancellationToken).ConfigureAwait(false);
        var detectedFirmwareVersion = info.FirmwareVersion ?? new FirmwareVersion();

        if (Connection is ISmartCardConnection smartCardConnection)
        {
            EnsureSmartCardTransportSupported(smartCardConnection.Transport, detectedFirmwareVersion);
        }

        // Initialize base class
        var effectiveProtocol = await InitializeProtocolAsync(
                protocol,
                firmwareVersionOverride ?? detectedFirmwareVersion,
                configuration,
                _scpKeyParams,
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
        await SendCborAsync(CtapCommand.Selection, null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await SendCborAsync(CtapCommand.Reset, null, cancellationToken).ConfigureAwait(false);
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

        EnsureInitialized();

        byte[]? request = null;
        ReadOnlyMemory<byte> response;
        try
        {
            request = FidoSessionRequestEncoding.BuildMakeCredentialRequest(
                clientDataHash, rp, user, pubKeyCredParams, options);

            _logger.LogDebug("MakeCredential for RP: {RpId}", rp.Id);

            response = await _backend.SendCborAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (request is not null)
            {
                CryptographicOperations.ZeroMemory(request);
            }
        }

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
            request = FidoSessionRequestEncoding.BuildGetAssertionRequest(rpId, clientDataHash, options);

            _logger.LogDebug("GetAssertion for RP: {RpId}", rpId);

            response = await _backend.SendCborAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (request is not null)
            {
                CryptographicOperations.ZeroMemory(request);
            }
        }

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

        var request = CtapRequestBuilder.Create(CtapCommand.GetNextAssertion).Build();

        var response = await _backend.SendCborAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return GetAssertionResponse.Decode(response);
    }

    /// <summary>
    /// Sends a CTAP CBOR command to the authenticator.
    /// </summary>
    /// <param name="command">The CTAP command byte.</param>
    /// <param name="payload">Optional CBOR-encoded payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CBOR-encoded response data.</returns>
    internal async Task<ReadOnlyMemory<byte>> SendCborAsync(
        byte command,
        ReadOnlyMemory<byte>? payload,
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

        return await _backend.SendCborAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> SendCborRequestAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        return await _backend.SendCborAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<AuthenticatorInfo> GetInfoCoreAsync(
        IFidoBackend backend,
        CancellationToken cancellationToken)
    {
        var request = CtapRequestBuilder.Create(CtapCommand.GetInfo).Build();
        var response = await backend.SendCborAsync(request, cancellationToken).ConfigureAwait(false);
        return AuthenticatorInfo.Decode(response);
    }

    private void EnsureInitialized()
    {
        ThrowIfDisposed();

        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                "Session is not initialized. Use FidoSession.CreateAsync() to create a session.");
        }
    }

    private static IFidoBackend CreateBackend(IProtocol protocol) =>
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