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
using System.Security.Cryptography;
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
    
    // PIV instruction bytes
    private const byte InsVerify = 0x20;
    private const byte InsResetRetry = 0x2C;
    private const byte InsReset = 0xFB;
    
    // PIV P2 parameter bytes
    private const byte P2Pin = 0x80;
    private const byte P2Puk = 0x81;
    
    private readonly IConnection _connection;
    private readonly ScpKeyParameters? _scpKeyParams;
    private ISmartCardProtocol? _protocol;
    
    /// <inheritdoc />
    public PivManagementKeyType ManagementKeyType { get; private set; } = PivManagementKeyType.TripleDes;

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
            var versionResponse = await smartCardProtocol.TransmitAndReceiveAsync(versionCommand, throwOnError: true, cancellationToken)
                .ConfigureAwait(false);

            // Note: PIV GET VERSION returns the PIV application version (often 0.0.1),
            // not the YubiKey firmware version. Feature detection should use metadata
            // commands rather than version comparisons.
            var firmwareVersion = ParseVersionResponse(versionResponse.Data.Span);
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

            // Detect management key type from device metadata (firmware 5.3+)
            // This is critical for YubiKey 5.7+ which defaults to AES-192 instead of 3DES
            try
            {
                var metadata = await GetManagementKeyMetadataAsync(cancellationToken).ConfigureAwait(false);
                ManagementKeyType = metadata.KeyType;
                Logger.LogDebug("Management key type detected from metadata: {KeyType}", ManagementKeyType);
            }
            catch (NotSupportedException)
            {
                // Firmware < 5.3 doesn't support metadata - default to 3DES
                ManagementKeyType = PivManagementKeyType.TripleDes;
                Logger.LogDebug("Management key metadata not supported, defaulting to TripleDes");
            }
            catch (ApduException ex) when (ex.SW == 0x6A88 || ex.SW == 0x6D00)
            {
                // 0x6A88 = Referenced data not found, 0x6D00 = Instruction not supported
                // Older firmware or metadata not available - default to 3DES
                ManagementKeyType = PivManagementKeyType.TripleDes;
                Logger.LogDebug("Management key metadata query failed (SW={SW:X4}), defaulting to TripleDes", ex.SW);
            }

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
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
        
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
        
        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }
        
        Logger.LogDebug("PIV: Resetting PIV application");
        
        // TODO: Check bio not configured (Phase 7)
        
        // Step 1: Block PIN by verifying with empty PIN until blocked
        // Empty PIN encodes as 8 bytes of 0xFF per PIV spec
        await BlockPinAsync(cancellationToken).ConfigureAwait(false);
        
        // Step 2: Block PUK using RESET RETRY with empty credentials
        await BlockPukAsync(cancellationToken).ConfigureAwait(false);
        
        // Step 3: Send RESET command
        var resetCommand = new ApduCommand(0x00, InsReset, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);
        var response = await _protocol.TransmitAndReceiveAsync(resetCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);
        
        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "PIV reset failed");
        }
        
        // Reset authentication state
        _isAuthenticated = false;
        
        // Update management key type from metadata (firmware 5.3+)
        try
        {
            var metadata = await GetManagementKeyMetadataAsync(cancellationToken).ConfigureAwait(false);
            ManagementKeyType = metadata.KeyType;
            Logger.LogDebug("PIV: Reset - management key type is {KeyType}", ManagementKeyType);
        }
        catch (NotSupportedException)
        {
            // Firmware < 5.3 doesn't support metadata - default to 3DES
            // Note: PIV version is often 0.0.1, so we can't reliably detect 5.7.0+ for AES default
            ManagementKeyType = PivManagementKeyType.TripleDes;
            Logger.LogDebug("PIV: Reset - metadata not supported, defaulting to TripleDes");
        }
        
        Logger.LogDebug("PIV: Reset completed successfully");
    }

    /// <summary>
    /// Blocks the PIN by repeatedly verifying with an empty PIN until blocked.
    /// </summary>
    private async Task BlockPinAsync(CancellationToken cancellationToken)
    {
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
                var response = await _protocol!.TransmitAndReceiveAsync(pinCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);
            
                retriesRemaining = PivPinUtilities.GetRetriesFromStatusWord(response.SW);
                if (retriesRemaining < 0)
                {
                    // Unexpected response - break to avoid infinite loop
                    break;
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
    private async Task BlockPukAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("PIV: Blocking PUK");
        
        // PUK blocking uses INS_RESET_RETRY (0x2C) with P2=0x80 (PIN_P2, not PUK_P2!)
        // Data is 16 bytes: 8-byte empty PUK + 8-byte empty PIN (both all 0xFF)
        byte[] emptyPukPin = PivPinUtilities.EncodePinPair(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty);
        try
        {
            int retriesRemaining = 1; // Start with 1 to enter loop
            while (retriesRemaining > 0)
            {
                var pukCommand = new ApduCommand(0x00, InsResetRetry, 0x00, P2Pin, emptyPukPin);
                var response = await _protocol!.TransmitAndReceiveAsync(pukCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);
            
                retriesRemaining = PivPinUtilities.GetRetriesFromStatusWord(response.SW);
                if (retriesRemaining < 0)
                {
                    // Unexpected response - break to avoid infinite loop
                    break;
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(emptyPukPin);
        }
        
        Logger.LogDebug("PIV: PUK blocked");
    }

    // All other IPivSession methods would be implemented as placeholders for now...
    // This is Phase 2 - just the core session structure.

    #endregion

    #region Not Yet Implemented Placeholder Methods



    /// <summary>
    /// Gets metadata about the PIV PIN.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>PIN metadata including retry counts and status.</returns>
    /// <exception cref="NotSupportedException">Thrown on firmware older than 5.3.0.</exception>
    public async Task<PivPinMetadata> GetPinMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }
        
        Logger.LogDebug("PIV: Getting PIN metadata");
        
        var command = new ApduCommand(0x00, 0xF7, 0x00, 0x80, ReadOnlyMemory<byte>.Empty);
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
        
        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == 0x6D00)
        {
            throw new NotSupportedException("PIN metadata requires firmware 5.3.0 or later");
        }
        
        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to get PIN metadata");
        }
        
        if (response.Data.IsEmpty)
        {
            throw new ApduException("Empty PIN metadata response");
        }
        
        // Parse TLV structure
        // TAG 0x05 = isDefault, TAG 0x06 = retries [total, remaining]
        var span = response.Data.Span;
        bool isDefault = false;
        int totalRetries = 0;
        int retriesRemaining = 0;
        
        int offset = 0;
        while (offset < span.Length)
        {
            byte tag = span[offset++];
            if (offset >= span.Length) break;
            
            int length = span[offset++];
            if (offset + length > span.Length) break;
            
            switch (tag)
            {
                case 0x05: // IsDefault
                    if (length > 0)
                    {
                        isDefault = span[offset] != 0;
                    }
                    break;
                case 0x06: // Retries [total, remaining]
                    if (length >= 2)
                    {
                        totalRetries = span[offset];
                        retriesRemaining = span[offset + 1];
                    }
                    break;
            }
            
            offset += length;
        }
        
        return new PivPinMetadata(isDefault, totalRetries, retriesRemaining);
    }





    #endregion

    #region Touch Notification

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
        if (FirmwareVersion >= PivFeatures.Metadata.Version)
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
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "PIV: Failed to query slot metadata for touch policy, notifying conservatively");
            }
        }

        // Fallback: On old firmware or metadata query failure, notify conservatively
        Logger.LogDebug("PIV: Notifying touch conservatively (metadata unavailable)");
        OnTouchRequired.Invoke();
    }

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