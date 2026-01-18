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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Piv;

/// <summary>
/// PIV (Personal Identity Verification) session for YubiKey operations.
/// </summary>
public sealed class PivSession : ApplicationSession, IPivSession, IAsyncDisposable
{
    private static readonly byte[] PivAid = ApplicationIds.Piv;
    
    private readonly IConnection _connection;
    private readonly ScpKeyParameters? _scpKeyParams;
    
    /// <inheritdoc />
    public PivManagementKeyType ManagementKeyType { get; private set; } = PivManagementKeyType.TripleDes;

    /// <summary>
    /// Initializes a new PivSession with the specified connection.
    /// </summary>
    /// <remarks>
    /// This constructor should typically not be used directly. Use 
    /// <see cref="CreateAsync(ISmartCardConnection, ProtocolConfiguration?, ScpKeyParameters?, CancellationToken)"/> 
    /// to create initialized sessions.
    /// </remarks>
    /// <param name="connection">The connection to use for PIV operations.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
    public PivSession(IConnection connection, ScpKeyParameters? scpKeyParams)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
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
        
        var session = new PivSession(connection, scpKeyParams);
        await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);
        return session;
    }

    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;

        // Create SmartCard protocol 
        var protocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create((ISmartCardConnection)_connection);

        var smartCardProtocol = protocol as ISmartCardProtocol
            ?? throw new InvalidOperationException("Failed to create SmartCard protocol.");

        try
        {
            // Select PIV application
            await smartCardProtocol.SelectAsync(PivAid, cancellationToken)
                .ConfigureAwait(false);
            
            Logger.LogDebug("PIV application selected successfully");

            // Get firmware version using GET VERSION command (INS 0xFD)
            var versionCommand = new ApduCommand(0x00, 0xFD, 0x00, 0x00);
            var versionResponse = await smartCardProtocol.TransmitAndReceiveAsync(versionCommand, cancellationToken)
                .ConfigureAwait(false);
            
            var firmwareVersion = ParseVersionResponse(versionResponse.Span);
            Logger.LogDebug("PIV firmware version: {Version}", firmwareVersion);

            // Initialize base session
            await InitializeCoreAsync(
                    protocol,
                    firmwareVersion,
                    configuration,
                    _scpKeyParams,
                    cancellationToken)
                .ConfigureAwait(false);

            // Default management key type is 3DES unless we determine otherwise
            ManagementKeyType = PivManagementKeyType.TripleDes;

            Logger.LogInformation("PIV session initialized successfully. Version: {Version}", firmwareVersion);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize PIV session");
            protocol.Dispose();
            throw;
        }
    }

    private static FirmwareVersion ParseVersionResponse(ReadOnlySpan<byte> response)
    {
        // Expected format: [major, minor, patch, 0x90, 0x00]
        if (response.Length < 5 || response[3] != 0x90 || response[4] != 0x00)
        {
            throw new ApduException("Invalid firmware version response from PIV application");
        }

        return new FirmwareVersion(response[0], response[1], response[2]);
    }

    #region IPivSession Implementation (Placeholder methods for now)
    
    /// <inheritdoc />
    public async Task<int> GetSerialNumberAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        EnsureSupports(PivFeatures.Serial);
        
        // GET SERIAL command (INS 0xF8) - placeholder implementation
        throw new NotImplementedException("GetSerialNumberAsync not yet implemented");
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        // PIV RESET command - placeholder implementation  
        throw new NotImplementedException("ResetAsync not yet implemented");
    }

    /// <inheritdoc />
    public async Task AuthenticateAsync(ReadOnlyMemory<byte> managementKey, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        // PIV AUTHENTICATE command - placeholder implementation
        throw new NotImplementedException("AuthenticateAsync not yet implemented");
    }

    /// <inheritdoc />
    public async Task VerifyPinAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        // PIV VERIFY command - placeholder implementation
        throw new NotImplementedException("VerifyPinAsync not yet implemented");
    }

    // All other IPivSession methods would be implemented as placeholders for now...
    // This is Phase 2 - just the core session structure.

    #endregion

    #region Not Yet Implemented Placeholder Methods

    public Task<ReadOnlyMemory<byte>?> VerifyUvAsync(bool requestTemporaryPin = false, bool checkOnly = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task VerifyTemporaryPinAsync(ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task ChangePinAsync(ReadOnlyMemory<byte> oldPin, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task ChangePukAsync(ReadOnlyMemory<byte> oldPuk, ReadOnlyMemory<byte> newPuk, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task UnblockPinAsync(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<int> GetPinAttemptsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IPublicKey> GenerateKeyAsync(PivSlot slot, PivAlgorithm algorithm, PivPinPolicy pinPolicy = PivPinPolicy.Default, PivTouchPolicy touchPolicy = PivTouchPolicy.Default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PivAlgorithm> ImportKeyAsync(PivSlot slot, IPrivateKey privateKey, PivPinPolicy pinPolicy = PivPinPolicy.Default, PivTouchPolicy touchPolicy = PivTouchPolicy.Default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task MoveKeyAsync(PivSlot sourceSlot, PivSlot destinationSlot, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DeleteKeyAsync(PivSlot slot, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<X509Certificate2> AttestKeyAsync(PivSlot slot, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ReadOnlyMemory<byte>> SignOrDecryptAsync(PivSlot slot, PivAlgorithm algorithm, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ReadOnlyMemory<byte>> CalculateSecretAsync(PivSlot slot, IPublicKey peerPublicKey, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<X509Certificate2?> GetCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task StoreCertificateAsync(PivSlot slot, X509Certificate2 certificate, bool compress = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DeleteCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PivPinMetadata> GetPinMetadataAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PivPukMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PivManagementKeyMetadata> GetManagementKeyMetadataAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PivSlotMetadata?> GetSlotMetadataAsync(PivSlot slot, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PivBioMetadata> GetBioMetadataAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ReadOnlyMemory<byte>> GetObjectAsync(int objectId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task PutObjectAsync(int objectId, ReadOnlyMemory<byte>? data, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task SetManagementKeyAsync(PivManagementKeyType keyType, ReadOnlyMemory<byte> newKey, bool requireTouch = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    #endregion

    private void EnsureInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Session is not initialized. Use PivSession.CreateAsync() to create a session.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask.ConfigureAwait(false);
    }
}