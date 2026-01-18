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
public sealed partial class PivSession : ApplicationSession, IPivSession, IAsyncDisposable
{
    private static readonly byte[] PivAid = ApplicationIds.Piv;
    
    private readonly IConnection _connection;
    private readonly ScpKeyParameters? _scpKeyParams;
    private ISmartCardProtocol? _protocol;
    
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

            // Store the smart card protocol for APDU operations
            _protocol = Protocol as ISmartCardProtocol;
            if (_protocol is null)
            {
                throw new NotSupportedException("PIV session requires SmartCard protocol");
            }

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
        
        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }
        
        Logger.LogDebug("PIV: Getting YubiKey serial number");
        
        var command = new ApduCommand(0x00, 0xF8, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);
        var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
        var response = new ApduResponse(responseData);
        
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
        
        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }
        
        Logger.LogDebug("PIV: Resetting PIV application");
        
        // TODO: Check bio not configured (Phase 7)
        
        // Step 1: Block PIN by trying wrong PIN repeatedly  
        var wrongPin = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };
        for (int i = 0; i < 5; i++) // Try more than 3 times to ensure blocked
        {
            try
            {
                var pinCommand = new ApduCommand(0x00, 0x20, 0x00, 0x80, wrongPin);
                await _protocol.TransmitAndReceiveAsync(pinCommand, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors - we expect this to fail
            }
        }
        
        // Step 2: Block PUK by trying wrong PUK repeatedly
        var wrongPuk = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        for (int i = 0; i < 5; i++) // Try more than 3 times to ensure blocked  
        {
            try
            {
                var pukCommand = new ApduCommand(0x00, 0x2C, 0x00, 0x81, wrongPuk);
                await _protocol.TransmitAndReceiveAsync(pukCommand, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors - we expect this to fail
            }
        }
        
        // Step 3: Send RESET command
        var resetCommand = new ApduCommand(0x00, 0xFB, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);
        var responseData = await _protocol.TransmitAndReceiveAsync(resetCommand, cancellationToken).ConfigureAwait(false);
        var response = new ApduResponse(responseData);
        
        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "PIV reset failed");
        }
        
        // Reset authentication state
        _isAuthenticated = false;
        ManagementKeyType = PivManagementKeyType.TripleDes; // Reset to default
        
        Logger.LogDebug("PIV: Reset completed successfully");
    }

    // All other IPivSession methods would be implemented as placeholders for now...
    // This is Phase 2 - just the core session structure.

    #endregion

    #region Not Yet Implemented Placeholder Methods

    public Task<ReadOnlyMemory<byte>?> VerifyUvAsync(bool requestTemporaryPin = false, bool checkOnly = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task VerifyTemporaryPinAsync(ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task ChangePukAsync(ReadOnlyMemory<byte> oldPuk, ReadOnlyMemory<byte> newPuk, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task UnblockPinAsync(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <summary>
    /// Gets metadata about the PIV PIN.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>PIN metadata including retry counts and status.</returns>
    /// <exception cref="NotSupportedException">Thrown on firmware older than 5.3.0.</exception>
    public async Task<PivPinMetadata> GetPinMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        EnsureSupports(PivFeatures.Metadata);
        
        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }
        
        Logger.LogDebug("PIV: Getting PIN metadata");
        
        var command = new ApduCommand(0x00, 0xF7, 0x00, 0x80, ReadOnlyMemory<byte>.Empty);
        var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
        var response = new ApduResponse(responseData);
        
        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to get PIN metadata");
        }
        
        if (response.Data.IsEmpty)
        {
            throw new ApduException("Empty PIN metadata response");
        }
        
        var data = response.Data.Span;
        
        // Parse metadata TLV structure (simplified for now)
        var totalRetries = data[0];
        var retriesRemaining = data[1];
        var isDefault = data[2] == 0x01;
        
        return new PivPinMetadata(isDefault, totalRetries, retriesRemaining);
    }



    public Task<PivPukMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PivManagementKeyMetadata> GetManagementKeyMetadataAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PivSlotMetadata?> GetSlotMetadataAsync(PivSlot slot, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PivBioMetadata> GetBioMetadataAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

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