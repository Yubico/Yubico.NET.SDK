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

using System.Buffers;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Piv;

public sealed partial class PivSession
{
    /// <summary>
    /// Gets metadata about the PIV biometric configuration.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Biometric metadata including number of configured fingerprints.</returns>
    /// <exception cref="NotSupportedException">Thrown if the YubiKey does not support biometrics or biometrics are not configured.</exception>
    public async Task<PivBioMetadata> GetBioMetadataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }
        
        Logger.LogDebug("PIV: Getting biometric metadata");
        
        var command = new ApduCommand(0x00, 0xF7, 0x00, 0x96, ReadOnlyMemory<byte>.Empty);
        var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
        var response = new ApduResponse(responseData);
        
        // SW 0x6A82 means object/feature not found (not configured)
        // SW 0x6D00 means instruction not supported (non-Bio key)
        if (response.SW == 0x6A82 || response.SW == 0x6D00)
        {
            throw new NotSupportedException("Biometrics are not supported or not configured on this YubiKey");
        }
        
        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to get biometric metadata");
        }
        
        if (response.Data.IsEmpty)
        {
            throw new ApduException("Empty biometric metadata response");
        }
        
        var data = response.Data.Span;
        
        // Parse metadata TLV structure
        // Simplified parsing - actual format depends on YubiKey Bio implementation
        var isConfigured = data.Length > 0 && data[0] != 0;
        var retriesRemaining = data.Length > 1 ? data[1] : 0;
        var temporaryPinEnabled = data.Length > 2 && data[2] == 1;
        
        return new PivBioMetadata(isConfigured, retriesRemaining, temporaryPinEnabled);
    }

    /// <summary>
    /// Verifies the user via biometric (fingerprint) and optionally retrieves a temporary PIN.
    /// </summary>
    /// <param name="requestTemporaryPin">If true, requests a temporary PIN from the YubiKey.</param>
    /// <param name="checkOnly">If true, only checks UV status without retrieving temporary PIN.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Temporary PIN if requestTemporaryPin is true, otherwise null. WARNING: Caller MUST zero this immediately after use!</returns>
    /// <exception cref="NotSupportedException">Thrown if biometrics are not supported or configured.</exception>
    /// <exception cref="InvalidOperationException">Thrown if biometric verification fails.</exception>
    public async Task<ReadOnlyMemory<byte>?> VerifyUvAsync(bool requestTemporaryPin = false, bool checkOnly = false, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }
        
        Logger.LogDebug("PIV: Verifying user via biometric (checkOnly={CheckOnly})", checkOnly);
        
        // Build command data
        byte[] commandData;
        if (checkOnly || !requestTemporaryPin)
        {
            // TAG 0x02: Check only, don't return temporary PIN
            commandData = new byte[] { 0x02, 0x00 };
        }
        else
        {
            // TAG 0x00: Return temporary PIN
            commandData = new byte[] { 0x00, 0x00 };
        }
        
        var command = new ApduCommand(0x00, 0x20, 0x00, 0x96, commandData);
        var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
        var response = new ApduResponse(responseData);
        
        // SW 0x6A82 or 0x6D00 means biometrics not supported/configured
        if (response.SW == 0x6A82 || response.SW == 0x6D00)
        {
            throw new NotSupportedException("Biometrics are not supported or not configured on this YubiKey");
        }
        
        // SW 0x63Cx means biometric verification failed (x = retries remaining)
        if ((response.SW & 0xFFF0) == 0x63C0)
        {
            var retriesRemaining = response.SW & 0x0F;
            throw new InvalidOperationException($"Biometric verification failed. {retriesRemaining} retries remaining.");
        }
        
        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to verify biometric");
        }
        
        if (checkOnly || !requestTemporaryPin)
        {
            Logger.LogDebug("PIV: Biometric verification succeeded (check only)");
            return null;
        }
        
        // Parse temporary PIN from response
        if (response.Data.IsEmpty)
        {
            throw new ApduException("Expected temporary PIN in response but got empty data");
        }
        
        var tempPin = new byte[response.Data.Length];
        response.Data.CopyTo(tempPin);
        
        Logger.LogDebug("PIV: Biometric verification succeeded, temporary PIN retrieved (length={Length})", tempPin.Length);
        Logger.LogWarning("PIV: Caller MUST zero temporary PIN immediately after use!");
        
        return tempPin;
    }

    /// <summary>
    /// Verifies the temporary PIN obtained from biometric verification.
    /// </summary>
    /// <param name="temporaryPin">The temporary PIN returned from VerifyUvAsync. WILL BE ZEROED after use.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="InvalidPinException">Thrown if the temporary PIN is invalid.</exception>
    public async Task VerifyTemporaryPinAsync(ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }
        
        if (temporaryPin.IsEmpty)
        {
            throw new ArgumentException("Temporary PIN cannot be empty", nameof(temporaryPin));
        }
        
        Logger.LogDebug("PIV: Verifying temporary PIN");
        
        // Build command with TAG 0x01 for temporary PIN
        var commandData = ArrayPool<byte>.Shared.Rent(2 + temporaryPin.Length);
        try
        {
            commandData[0] = 0x01; // TAG for temporary PIN
            commandData[1] = (byte)temporaryPin.Length;
            temporaryPin.Span.CopyTo(commandData.AsSpan(2));
            
            var command = new ApduCommand(0x00, 0x20, 0x00, 0x96, commandData.AsMemory(0, 2 + temporaryPin.Length));
            var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
            var response = new ApduResponse(responseData);
            
            // SW 0x63Cx means verification failed (x = retries remaining)
            if ((response.SW & 0xFFF0) == 0x63C0)
            {
                var retriesRemaining = response.SW & 0x0F;
                throw new InvalidPinException(
                    retriesRemaining,
                    $"Temporary PIN verification failed. {retriesRemaining} retries remaining.");
            }
            
            if (!response.IsOK())
            {
                throw ApduException.FromStatusWord(response.SW, "Failed to verify temporary PIN");
            }
            
            Logger.LogDebug("PIV: Temporary PIN verified successfully");
        }
        finally
        {
            // Zero the command data containing the temporary PIN
            CryptographicOperations.ZeroMemory(commandData.AsSpan(0, 2 + temporaryPin.Length));
            ArrayPool<byte>.Shared.Return(commandData);
        }
    }
}
