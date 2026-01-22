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
        Logger.LogDebug("PIV: Starting management key authentication with {KeyType}", ManagementKeyType);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // Validate key length based on management key type
        int expectedKeyLength = ManagementKeyType switch
        {
            PivManagementKeyType.TripleDes => 24,
            PivManagementKeyType.Aes128 => 16,
            PivManagementKeyType.Aes192 => 24,
            PivManagementKeyType.Aes256 => 32,
            _ => throw new ArgumentException($"Unsupported management key type: {ManagementKeyType}")
        };

        if (managementKey.Length != expectedKeyLength)
        {
            throw new ArgumentException(
                $"Invalid management key length: {managementKey.Length}. Expected {expectedKeyLength} bytes for {ManagementKeyType}.",
                nameof(managementKey));
        }

        // Challenge length: 8 bytes for 3DES, 16 bytes for AES
        int challengeLength = ManagementKeyType == PivManagementKeyType.TripleDes ? 8 : 16;
        
        // Algorithm code for P1
        byte algorithmCode = (byte)ManagementKeyType;
        
        const byte InsAuthenticate = 0x87;
        const byte SlotCardManagement = 0x9B;
        const byte TagDynAuth = 0x7C;
        const byte TagAuthWitness = 0x80;

        var keyBytes = ArrayPool<byte>.Shared.Rent(expectedKeyLength);
        try
        {
            managementKey.CopyTo(keyBytes);

            // Step 1: Request witness from device
            // Send: 7C 02 80 00 (empty witness request)
            byte[] witnessRequest = [TagDynAuth, 0x02, TagAuthWitness, 0x00];
            var witnessCommand = new ApduCommand(0x00, InsAuthenticate, algorithmCode, SlotCardManagement, witnessRequest);
            var witnessResponse = await _protocol.TransmitAndReceiveAsync(witnessCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);
            
            if (!witnessResponse.IsOK())
            {
                throw ApduException.FromStatusWord(witnessResponse.SW, "Management key authentication failed - witness request");
            }

            // Parse witness response: 7C [len] 80 [len] [witness data]
            var witnessBytes = ParseWitnessResponse(witnessResponse.Data.Span, challengeLength);

            // Step 2: Decrypt witness with our key
            byte[]? decryptedWitness = null;
            byte[]? challenge = null;
            byte[]? responseData = null;
            byte[]? expectedResponse = null;
            
            try
            {
                decryptedWitness = ArrayPool<byte>.Shared.Rent(challengeLength);
                DecryptBlock(keyBytes.AsSpan(0, expectedKeyLength), witnessBytes, decryptedWitness.AsSpan(0, challengeLength), ManagementKeyType);

                // Step 3: Generate our challenge
                challenge = ArrayPool<byte>.Shared.Rent(challengeLength);
                RandomNumberGenerator.Fill(challenge.AsSpan(0, challengeLength));

                // Step 4: Build and send response with decrypted witness and our challenge
                // Send: 7C [len] 80 [len] [decrypted witness] 81 [len] [challenge]
                responseData = BuildAuthResponse(decryptedWitness.AsSpan(0, challengeLength), challenge.AsSpan(0, challengeLength));
                var challengeCommand = new ApduCommand(0x00, InsAuthenticate, algorithmCode, SlotCardManagement, responseData);
                var challengeResponse = await _protocol.TransmitAndReceiveAsync(challengeCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);
            
                if (!challengeResponse.IsOK())
                {
                    throw ApduException.FromStatusWord(challengeResponse.SW, "Management key authentication failed - challenge response");
                }

                // Step 5: Verify device's response (encrypted challenge)
                // Response: 7C [len] 82 [len] [encrypted challenge]
                var encryptedChallenge = ParseChallengeResponse(challengeResponse.Data.Span, challengeLength);

                // Encrypt our challenge and compare
                expectedResponse = ArrayPool<byte>.Shared.Rent(challengeLength);
                EncryptBlock(keyBytes.AsSpan(0, expectedKeyLength), challenge.AsSpan(0, challengeLength), expectedResponse.AsSpan(0, challengeLength), ManagementKeyType);

                if (!CryptographicOperations.FixedTimeEquals(encryptedChallenge, expectedResponse.AsSpan(0, challengeLength)))
                {
                    throw new ApduException("Management key authentication failed - device response mismatch");
                }

                _isAuthenticated = true;
                Logger.LogDebug("PIV: Management key authentication succeeded");
            }
            finally
            {
                // Zero all sensitive intermediate buffers
                if (decryptedWitness is not null)
                {
                    CryptographicOperations.ZeroMemory(decryptedWitness.AsSpan(0, challengeLength));
                    ArrayPool<byte>.Shared.Return(decryptedWitness);
                }
                if (challenge is not null)
                {
                    CryptographicOperations.ZeroMemory(challenge.AsSpan(0, challengeLength));
                    ArrayPool<byte>.Shared.Return(challenge);
                }
                if (responseData is not null)
                {
                    CryptographicOperations.ZeroMemory(responseData);
                }
                if (expectedResponse is not null)
                {
                    CryptographicOperations.ZeroMemory(expectedResponse.AsSpan(0, challengeLength));
                    ArrayPool<byte>.Shared.Return(expectedResponse);
                }
            }
        }
        finally
        {
            // Zero sensitive data
            CryptographicOperations.ZeroMemory(keyBytes.AsSpan(0, expectedKeyLength));
            ArrayPool<byte>.Shared.Return(keyBytes);
        }
    }

    /// <summary>
    /// Parses the witness response TLV: 7C [len] 80 [len] [witness data]
    /// </summary>
    private static ReadOnlySpan<byte> ParseWitnessResponse(ReadOnlySpan<byte> response, int expectedLength)
    {
        // Minimum: 7C 02 80 00 for empty, but we expect: 7C [len] 80 [len] [data]
        if (response.Length < 4)
        {
            throw new ApduException("Invalid witness response - too short");
        }

        if (response[0] != 0x7C)
        {
            throw new ApduException($"Invalid witness response - expected TAG 0x7C, got 0x{response[0]:X2}");
        }

        int outerLen = response[1];
        if (response.Length < 2 + outerLen)
        {
            throw new ApduException("Invalid witness response - truncated outer TLV");
        }

        var inner = response.Slice(2, outerLen);
        if (inner.Length < 2 || inner[0] != 0x80)
        {
            throw new ApduException($"Invalid witness response - expected TAG 0x80, got 0x{inner[0]:X2}");
        }

        int witnessLen = inner[1];
        if (witnessLen != expectedLength)
        {
            throw new ApduException($"Invalid witness length - expected {expectedLength}, got {witnessLen}");
        }

        return inner.Slice(2, witnessLen);
    }

    /// <summary>
    /// Parses the challenge response TLV: 7C [len] 82 [len] [encrypted data]
    /// </summary>
    private static ReadOnlySpan<byte> ParseChallengeResponse(ReadOnlySpan<byte> response, int expectedLength)
    {
        if (response.Length < 4)
        {
            throw new ApduException("Invalid challenge response - too short");
        }

        if (response[0] != 0x7C)
        {
            throw new ApduException($"Invalid challenge response - expected TAG 0x7C, got 0x{response[0]:X2}");
        }

        int outerLen = response[1];
        if (response.Length < 2 + outerLen)
        {
            throw new ApduException("Invalid challenge response - truncated outer TLV");
        }

        var inner = response.Slice(2, outerLen);
        if (inner.Length < 2 || inner[0] != 0x82)
        {
            throw new ApduException($"Invalid challenge response - expected TAG 0x82, got 0x{inner[0]:X2}");
        }

        int responseLen = inner[1];
        if (responseLen != expectedLength)
        {
            throw new ApduException($"Invalid challenge response length - expected {expectedLength}, got {responseLen}");
        }

        return inner.Slice(2, responseLen);
    }

    /// <summary>
    /// Builds the authentication response: 7C [len] 80 [len] [decrypted witness] 81 [len] [challenge]
    /// </summary>
    private static byte[] BuildAuthResponse(ReadOnlySpan<byte> decryptedWitness, ReadOnlySpan<byte> challenge)
    {
        int witnessLen = decryptedWitness.Length;
        int challengeLen = challenge.Length;
        
        // Inner: 80 [len] [witness] 81 [len] [challenge]
        int innerLen = 2 + witnessLen + 2 + challengeLen;
        
        // Outer: 7C [len] [inner]
        byte[] result = new byte[2 + innerLen];
        int offset = 0;
        
        result[offset++] = 0x7C;
        result[offset++] = (byte)innerLen;
        result[offset++] = 0x80;
        result[offset++] = (byte)witnessLen;
        decryptedWitness.CopyTo(result.AsSpan(offset));
        offset += witnessLen;
        result[offset++] = 0x81;
        result[offset++] = (byte)challengeLen;
        challenge.CopyTo(result.AsSpan(offset));
        
        return result;
    }

    /// <summary>
    /// Decrypts a single block using ECB mode with the specified key type.
    /// </summary>
    private static void DecryptBlock(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input, Span<byte> output, PivManagementKeyType keyType)
    {
        byte[]? keyBuffer = null;
        byte[]? inputBuffer = null;
        byte[]? decryptedBuffer = null;
        try
        {
            keyBuffer = ArrayPool<byte>.Shared.Rent(key.Length);
            inputBuffer = ArrayPool<byte>.Shared.Rent(input.Length);
            decryptedBuffer = ArrayPool<byte>.Shared.Rent(input.Length);
            
            key.CopyTo(keyBuffer);
            input.CopyTo(inputBuffer);
            
            if (keyType == PivManagementKeyType.TripleDes)
            {
                using var des = TripleDES.Create();
                des.Key = keyBuffer.AsSpan(0, key.Length).ToArray();
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;
                using var decryptor = des.CreateDecryptor();
                decryptor.TransformBlock(inputBuffer, 0, input.Length, decryptedBuffer, 0);
            }
            else
            {
                using var aes = Aes.Create();
                aes.Key = keyBuffer.AsSpan(0, key.Length).ToArray();
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using var decryptor = aes.CreateDecryptor();
                decryptor.TransformBlock(inputBuffer, 0, input.Length, decryptedBuffer, 0);
            }
            
            decryptedBuffer.AsSpan(0, input.Length).CopyTo(output);
        }
        finally
        {
            if (keyBuffer is not null)
            {
                CryptographicOperations.ZeroMemory(keyBuffer.AsSpan(0, key.Length));
                ArrayPool<byte>.Shared.Return(keyBuffer);
            }
            if (inputBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(inputBuffer);
            }
            if (decryptedBuffer is not null)
            {
                CryptographicOperations.ZeroMemory(decryptedBuffer.AsSpan(0, input.Length));
                ArrayPool<byte>.Shared.Return(decryptedBuffer);
            }
        }
    }

    /// <summary>
    /// Encrypts a single block using ECB mode with the specified key type.
    /// </summary>
    private static void EncryptBlock(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input, Span<byte> output, PivManagementKeyType keyType)
    {
        byte[]? keyBuffer = null;
        byte[]? inputBuffer = null;
        byte[]? encryptedBuffer = null;
        try
        {
            keyBuffer = ArrayPool<byte>.Shared.Rent(key.Length);
            inputBuffer = ArrayPool<byte>.Shared.Rent(input.Length);
            encryptedBuffer = ArrayPool<byte>.Shared.Rent(input.Length);
            
            key.CopyTo(keyBuffer);
            input.CopyTo(inputBuffer);
            
            if (keyType == PivManagementKeyType.TripleDes)
            {
                using var des = TripleDES.Create();
                des.Key = keyBuffer.AsSpan(0, key.Length).ToArray();
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;
                using var encryptor = des.CreateEncryptor();
                encryptor.TransformBlock(inputBuffer, 0, input.Length, encryptedBuffer, 0);
            }
            else
            {
                using var aes = Aes.Create();
                aes.Key = keyBuffer.AsSpan(0, key.Length).ToArray();
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using var encryptor = aes.CreateEncryptor();
                encryptor.TransformBlock(inputBuffer, 0, input.Length, encryptedBuffer, 0);
            }
            
            encryptedBuffer.AsSpan(0, input.Length).CopyTo(output);
        }
        finally
        {
            if (keyBuffer is not null)
            {
                CryptographicOperations.ZeroMemory(keyBuffer.AsSpan(0, key.Length));
                ArrayPool<byte>.Shared.Return(keyBuffer);
            }
            if (inputBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(inputBuffer);
            }
            if (encryptedBuffer is not null)
            {
                CryptographicOperations.ZeroMemory(encryptedBuffer.AsSpan(0, input.Length));
                ArrayPool<byte>.Shared.Return(encryptedBuffer);
            }
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
            var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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
            var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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