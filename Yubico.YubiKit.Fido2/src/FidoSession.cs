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
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Backend;
using Yubico.YubiKit.Fido2.Cbor;
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
public sealed class FidoSession : ApplicationSession, IFidoSession, IAsyncDisposable
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
    
    private readonly IConnection _connection;
    private readonly ScpKeyParameters? _scpKeyParams;
    private readonly ILogger _logger;
    
    private IFidoBackend? _backend;
    private bool _disposed;
    
    private FidoSession(IConnection connection, ScpKeyParameters? scpKeyParams = null)
    {
        _connection = connection;
        _scpKeyParams = scpKeyParams;
        _logger = Logger;
    }
    
    /// <summary>
    /// Creates and initializes a FIDO session from a connection.
    /// </summary>
    /// <param name="connection">The connection to the YubiKey (SmartCard or FIDO HID).</param>
    /// <param name="configuration">Optional protocol configuration.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for secure channel (SmartCard only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized FidoSession.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="connection"/> is null.</exception>
    /// <exception cref="NotSupportedException">If the connection type is not supported.</exception>
    public static async Task<FidoSession> CreateAsync(
        IConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        
        var session = new FidoSession(connection, scpKeyParams);
        await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);
        return session;
    }
    
    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;
        
        // Create backend based on connection type
        var (backend, protocol) = _connection switch
        {
            ISmartCardConnection sc => await CreateSmartCardBackendAsync(sc, cancellationToken)
                .ConfigureAwait(false),
            IFidoHidConnection fido => CreateFidoHidBackend(fido),
            _ => throw new NotSupportedException(
                $"Connection type {_connection.GetType().Name} is not supported. " +
                "Use ISmartCardConnection or IFidoHidConnection.")
        };
        
        _backend = backend;
        
        // Get firmware version from authenticator info
        var info = await GetInfoCoreAsync(backend, cancellationToken).ConfigureAwait(false);
        var firmwareVersion = info.FirmwareVersion ?? new FirmwareVersion();
        
        // Initialize base class
        await InitializeCoreAsync(
                protocol,
                firmwareVersion,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);
        
        // If SCP was established, recreate backend with wrapped protocol
        if (IsAuthenticated && Protocol is ISmartCardProtocol scpProtocol)
        {
            _backend?.Dispose();
            _backend = new SmartCardFidoBackend(scpProtocol);
        }
        
        _logger.LogDebug(
            "FIDO session initialized. Firmware: {Version}, Versions: [{Versions}]",
            firmwareVersion,
            string.Join(", ", info.Versions));
    }
    
    /// <inheritdoc />
    public Task<AuthenticatorInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        return GetInfoCoreAsync(_backend!, cancellationToken);
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
        
        return await _backend!.SendCborAsync(request, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Sends a CTAP CBOR request built with <see cref="CtapRequestBuilder"/>.
    /// </summary>
    /// <param name="request">The serialized request (command byte + CBOR).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CBOR-encoded response data.</returns>
    internal async Task<ReadOnlyMemory<byte>> SendCborRequestAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        return await _backend!.SendCborAsync(request, cancellationToken).ConfigureAwait(false);
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                "Session is not initialized. Use FidoSession.CreateAsync() to create a session.");
        }
    }
    
    private static async Task<(IFidoBackend backend, IProtocol protocol)> CreateSmartCardBackendAsync(
        ISmartCardConnection connection,
        CancellationToken cancellationToken)
    {
        var protocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create(connection);
        
        var smartCardProtocol = protocol as ISmartCardProtocol
            ?? throw new InvalidOperationException("Failed to create SmartCard protocol.");
        
        // Select the FIDO2 application
        await smartCardProtocol.SelectAsync(ApplicationIds.Fido2, cancellationToken)
            .ConfigureAwait(false);
        
        var backend = new SmartCardFidoBackend(smartCardProtocol);
        return (backend, protocol);
    }
    
    private static (IFidoBackend backend, IProtocol protocol) CreateFidoHidBackend(
        IFidoHidConnection connection)
    {
        var protocol = FidoProtocolFactory
            .Create()
            .Create(connection);
        
        var backend = new FidoHidBackend(protocol);
        return (backend, protocol);
    }
    
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        
        if (disposing)
        {
            _backend?.Dispose();
            _backend = null;
        }
        
        _disposed = true;
        base.Dispose(disposing);
    }
    
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        
        _backend?.Dispose();
        _backend = null;
        _disposed = true;
        
        // Dispose base class synchronously (it doesn't have async dispose)
        base.Dispose(true);
        GC.SuppressFinalize(this);
        
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
