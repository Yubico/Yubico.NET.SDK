// Copyright 2021 Yubico AB
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

using System;
using System.Buffers;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Piv;

public sealed partial class PivSession
{
    private bool _isAuthenticated;

    /// <summary>
    /// Gets whether the session has been authenticated with the management key.
    /// </summary>
    public new bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Authenticates the session using the PIV management key.
    /// </summary>
    /// <param name="managementKey">The management key bytes. Must be 24 bytes (3DES).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown if the management key is the wrong length.</exception>
    /// <exception cref="ApduException">Thrown if authentication fails.</exception>
    /// <remarks>
    /// <para>
    /// Authentication uses mutual challenge-response to prove knowledge of the management key
    /// without transmitting it. The key is automatically zeroed after use for security.
    /// </para>
    /// <para>
    /// Default management key (3DES): 01 02 03 04 05 06 07 08 01 02 03 04 05 06 07 08 01 02 03 04 05 06 07 08
    /// </para>
    /// </remarks>
    public async Task AuthenticateAsync(ReadOnlyMemory<byte> managementKey, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Starting management key authentication");

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // For now, only support 3DES keys (24 bytes)
        if (managementKey.Length != 24)
        {
            throw new ArgumentException($"Invalid management key length: {managementKey.Length}. Expected 24 bytes for 3DES.", nameof(managementKey));
        }

        var keyBytes = ArrayPool<byte>.Shared.Rent(24);
        try
        {
            managementKey.CopyTo(keyBytes);

            // Step 1: Get witness from device (empty challenge)  
            var witnessCommand = new ApduCommand(0x00, 0x87, 0x03, 0x9B, ReadOnlyMemory<byte>.Empty);
            var witnessData = await _protocol.TransmitAndReceiveAsync(witnessCommand, cancellationToken).ConfigureAwait(false);
            var witnessResponse = new ApduResponse(witnessData);
            
            if (!witnessResponse.IsOK())
            {
                throw ApduException.FromStatusWord(witnessResponse.SW, "Management key authentication failed - witness request");
            }

            // Parse witness response (TAG 0x80)
            if (witnessResponse.Data.Length < 10 || witnessResponse.Data.Span[0] != 0x80 || witnessResponse.Data.Span[1] != 0x08)
            {
                throw new ApduException("Invalid witness response format");
            }

            var witness = witnessResponse.Data.Slice(2, 8);

            // Step 2: Generate challenge for device
            var challenge = new byte[8];
            RandomNumberGenerator.Fill(challenge);

            // Step 3: Encrypt witness and challenge with 3DES
            var responseData = new byte[18]; // TAG + LEN + 16 bytes encrypted
            responseData[0] = 0x80;
            responseData[1] = 0x10; // 16 bytes

            using (var des = TripleDES.Create())
            {
                des.Key = keyBytes.AsSpan(0, 24).ToArray();
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using var encryptor = des.CreateEncryptor();
                encryptor.TransformBlock(witness.ToArray(), 0, 8, responseData, 2);
                encryptor.TransformBlock(challenge, 0, 8, responseData, 10);
            }

            // Step 4: Send encrypted response and get challenge response
            var challengeCommand = new ApduCommand(0x00, 0x87, 0x03, 0x9B, responseData.AsMemory());
            var challengeData = await _protocol.TransmitAndReceiveAsync(challengeCommand, cancellationToken).ConfigureAwait(false);
            var challengeResponse = new ApduResponse(challengeData);
            
            if (!challengeResponse.IsOK())
            {
                throw ApduException.FromStatusWord(challengeResponse.SW, "Management key authentication failed - challenge response");
            }

            // Step 5: Verify device encrypted our challenge correctly
            if (challengeResponse.Data.Length < 10 || challengeResponse.Data.Span[0] != 0x80 || challengeResponse.Data.Span[1] != 0x08)
            {
                throw new ApduException("Invalid challenge response format");
            }

            var deviceResponse = challengeResponse.Data.Slice(2, 8);
            var expectedResponse = new byte[8];

            using (var des = TripleDES.Create())
            {
                des.Key = keyBytes.AsSpan(0, 24).ToArray();
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using var encryptor = des.CreateEncryptor();
                encryptor.TransformBlock(challenge, 0, 8, expectedResponse, 0);
            }

            if (!deviceResponse.Span.SequenceEqual(expectedResponse))
            {
                throw new ApduException("Management key authentication failed - device response incorrect");
            }

            _isAuthenticated = true;
            Logger.LogDebug("PIV: Management key authentication succeeded");
        }
        finally
        {
            // Zero sensitive data
            CryptographicOperations.ZeroMemory(keyBytes.AsSpan(0, 24));
            ArrayPool<byte>.Shared.Return(keyBytes);
        }
    }

    /// <summary>
    /// Verifies the user PIN.
    /// </summary>
    /// <param name="pin">The PIN to verify. Must be 6-8 digits/characters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="InvalidPinException">Thrown if PIN verification fails.</exception>
    /// <exception cref="ArgumentException">Thrown if PIN is invalid format.</exception>
    /// <remarks>
    /// <para>
    /// PIN verification is required for certain operations like signing and decryption.
    /// The verification persists until the session ends or another application is selected.
    /// </para>
    /// <para>
    /// After 3 consecutive failed attempts, the PIN becomes blocked and must be unblocked
    /// with the PUK (PIN Unblocking Key).
    /// </para>
    /// <para>
    /// Default PIN: 123456
    /// </para>
    /// </remarks>
    public async Task VerifyPinAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Verifying PIN");

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (pin.Length is < 6 or > 8)
        {
            throw new ArgumentException("PIN must be 6-8 bytes", nameof(pin));
        }

        var paddedPin = ArrayPool<byte>.Shared.Rent(8);
        try
        {
            // Pad PIN to 8 bytes with 0xFF
            pin.Span.CopyTo(paddedPin);
            Array.Fill(paddedPin, (byte)0xFF, pin.Length, 8 - pin.Length);

            var command = new ApduCommand(0x00, 0x20, 0x00, 0x80, paddedPin.AsMemory(0, 8));
            var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
            var response = new ApduResponse(responseData);

            if (response.IsOK())
            {
                Logger.LogDebug("PIV: PIN verification succeeded");
                return;
            }

            // Parse retry count from status word (0x63Cx where x is attempts remaining)
            if ((response.SW & 0xFFF0) == SWConstants.VerifyFail)
            {
                var retriesRemaining = (int)(response.SW & 0x0F);
                throw new InvalidPinException(retriesRemaining);
            }

            // PIN is blocked
            if (response.SW == SWConstants.AuthenticationMethodBlocked)
            {
                throw new InvalidPinException(0, "PIN is blocked. Use PUK to unblock.");
            }

            throw ApduException.FromStatusWord(response.SW, "PIN verification failed");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(paddedPin.AsSpan(0, 8));
            ArrayPool<byte>.Shared.Return(paddedPin);
        }
    }

    /// <summary>
    /// Gets the number of PIN verification attempts remaining.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Number of attempts remaining (0-3).</returns>
    /// <remarks>
    /// This method performs an empty PIN verify as fallback for older firmware.
    /// </remarks>
    public async Task<int> GetPinAttemptsAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting PIN attempt count");

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // Try metadata approach first if supported
        if (FirmwareVersion >= PivFeatures.Metadata.Version)
        {
            try
            {
                var metadata = await GetPinMetadataAsync(cancellationToken).ConfigureAwait(false);
                return metadata.RetriesRemaining;
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "PIV: Metadata approach failed, falling back to empty verify");
            }
        }

        // Fallback: empty PIN verify to get retry count
        var command = new ApduCommand(0x00, 0x20, 0x00, 0x80, ReadOnlyMemory<byte>.Empty);
        var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
        var response = new ApduResponse(responseData);

        // Parse retry count from status word (0x63Cx where x is attempts remaining)
        if ((response.SW & 0xFFF0) == SWConstants.VerifyFail)
        {
            return (int)(response.SW & 0x0F);
        }

        // PIN is blocked
        if (response.SW == SWConstants.AuthenticationMethodBlocked)
        {
            return 0;
        }

        // Default to 3 if we can't determine
        return 3;
    }

    /// <summary>
    /// Changes the PIN.
    /// </summary>
    /// <param name="currentPin">The current PIN.</param>
    /// <param name="newPin">The new PIN (6-8 digits/characters).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="InvalidPinException">Thrown if current PIN is incorrect.</exception>
    /// <exception cref="ArgumentException">Thrown if PIN format is invalid.</exception>
    /// <remarks>
    /// Both PINs are automatically zeroed after use for security.
    /// </remarks>
    public async Task ChangePinAsync(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Changing PIN");

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (currentPin.Length is < 6 or > 8)
        {
            throw new ArgumentException("Current PIN must be 6-8 bytes", nameof(currentPin));
        }

        if (newPin.Length is < 6 or > 8)
        {
            throw new ArgumentException("New PIN must be 6-8 bytes", nameof(newPin));
        }

        var pinData = ArrayPool<byte>.Shared.Rent(16);
        try
        {
            // Format: [current PIN padded to 8][new PIN padded to 8]
            currentPin.Span.CopyTo(pinData);
            Array.Fill(pinData, (byte)0xFF, currentPin.Length, 8 - currentPin.Length);
            
            newPin.Span.CopyTo(pinData.AsSpan(8));
            Array.Fill(pinData, (byte)0xFF, 8 + newPin.Length, 8 - newPin.Length);

            var command = new ApduCommand(0x00, 0x24, 0x00, 0x80, pinData.AsMemory(0, 16));
            var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
            var response = new ApduResponse(responseData);

            if (response.IsOK())
            {
                Logger.LogDebug("PIV: PIN change succeeded");
                return;
            }

            // Parse retry count from status word (0x63Cx where x is attempts remaining)
            if ((response.SW & 0xFFF0) == SWConstants.VerifyFail)
            {
                var retriesRemaining = (int)(response.SW & 0x0F);
                throw new InvalidPinException(retriesRemaining, "PIN change failed - current PIN incorrect");
            }

            // PIN is blocked
            if (response.SW == SWConstants.AuthenticationMethodBlocked)
            {
                throw new InvalidPinException(0, "PIN is blocked. Use PUK to unblock.");
            }

            throw ApduException.FromStatusWord(response.SW, "PIN change failed");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pinData.AsSpan(0, 16));
            ArrayPool<byte>.Shared.Return(pinData);
        }
    }
}