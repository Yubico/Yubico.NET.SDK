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
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Piv;

public sealed partial class PivSession
{
    /// <summary>
    /// Gets the factory default PIV management key (3DES).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>⚠️ SECURITY WARNING:</b> This is the well-known default management key shipped with all YubiKeys.
    /// You SHOULD change this key after initial device setup to protect against unauthorized management operations.
    /// </para>
    /// <para>
    /// The default key is: 01 02 03 04 05 06 07 08 01 02 03 04 05 06 07 08 01 02 03 04 05 06 07 08 (24 bytes, 3DES).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Authenticate with default key (for initial setup only)
    /// await session.AuthenticateAsync(PivSession.DefaultManagementKey);
    /// 
    /// // Generate a new secure key and change it immediately
    /// byte[] newKey = RandomNumberGenerator.GetBytes(24);
    /// await session.ChangeManagementKeyAsync(newKey);
    /// </code>
    /// </example>
    public static ReadOnlySpan<byte> DefaultManagementKey =>
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private bool _isAuthenticated;

    /// <summary>
    /// Gets whether the session has been authenticated with the management key.
    /// </summary>
    // TODO Disambiguate with IsAuthenticated
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
        EnsureProtocol();

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
            var witnessBytes = ParseAuthResponse(witnessResponse.Data.Span, TagAuthWitness, challengeLength);

            // Step 2: Decrypt witness with our key
            byte[]? decryptedWitness = null;
            byte[]? challenge = null;
            byte[]? responseBuffer = null;
            byte[]? expectedResponse = null;
            
            try
            {
                decryptedWitness = ArrayPool<byte>.Shared.Rent(challengeLength);
                CryptoBlock(keyBytes.AsSpan(0, expectedKeyLength), witnessBytes, decryptedWitness.AsSpan(0, challengeLength), ManagementKeyType, encrypt: false);

                // Step 3: Generate our challenge
                challenge = ArrayPool<byte>.Shared.Rent(challengeLength);
                RandomNumberGenerator.Fill(challenge.AsSpan(0, challengeLength));

                // Step 4: Build and send response with decrypted witness and our challenge
                // Send: 7C [len] 80 [len] [decrypted witness] 81 [len] [challenge]
                // Max size: 1 (tag) + 3 (BER len) + 1 (tag) + 3 (BER len) + challengeLength + 1 (tag) + 3 (BER len) + challengeLength
                int responseSize = 12 + (2 * challengeLength);
                responseBuffer = ArrayPool<byte>.Shared.Rent(responseSize);
                int bytesWritten = BuildAuthResponse(decryptedWitness.AsSpan(0, challengeLength), challenge.AsSpan(0, challengeLength), responseBuffer.AsSpan(0, responseSize));
                var challengeCommand = new ApduCommand(0x00, InsAuthenticate, algorithmCode, SlotCardManagement, responseBuffer.AsMemory(0, bytesWritten));
                var challengeResponse = await _protocol.TransmitAndReceiveAsync(challengeCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);
            
                if (!challengeResponse.IsOK())
                {
                    throw ApduException.FromStatusWord(challengeResponse.SW, "Management key authentication failed - challenge response");
                }

                // Step 5: Verify device's response (encrypted challenge)
                // Response: 7C [len] 82 [len] [encrypted challenge]
                var encryptedChallenge = ParseAuthResponse(challengeResponse.Data.Span, 0x82, challengeLength);

                // Encrypt our challenge and compare
                expectedResponse = ArrayPool<byte>.Shared.Rent(challengeLength);
                CryptoBlock(keyBytes.AsSpan(0, expectedKeyLength), challenge.AsSpan(0, challengeLength), expectedResponse.AsSpan(0, challengeLength), ManagementKeyType, encrypt: true);

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
                if (responseBuffer is not null)
                {
                    int responseSize = 12 + (2 * challengeLength);
                    CryptographicOperations.ZeroMemory(responseBuffer.AsSpan(0, responseSize));
                    ArrayPool<byte>.Shared.Return(responseBuffer);
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
    /// Parses an authentication response TLV: 7C [len] [expectedInnerTag] [len] [data].
    /// Used for both witness (tag 0x80) and challenge (tag 0x82) responses.
    /// </summary>
    private static byte[] ParseAuthResponse(ReadOnlySpan<byte> response, byte expectedInnerTag, int expectedLength)
    {
        using var outer = Tlv.Create(response);
        if (outer.Tag != 0x7C)
        {
            throw new ApduException($"Invalid auth response - expected TAG 0x7C, got 0x{outer.Tag:X2}");
        }

        using var inner = Tlv.Create(outer.Value.Span);
        if (inner.Tag != expectedInnerTag)
        {
            throw new ApduException($"Invalid auth response - expected TAG 0x{expectedInnerTag:X2}, got 0x{inner.Tag:X2}");
        }

        if (inner.Length != expectedLength)
        {
            throw new ApduException($"Invalid auth response length - expected {expectedLength}, got {inner.Length}");
        }

        return inner.Value.Span.ToArray();
    }

    /// <summary>
    /// Builds the authentication response: 7C [len] 80 [len] [decrypted witness] 81 [len] [challenge]
    /// </summary>
    /// <param name="decryptedWitness">The decrypted witness data.</param>
    /// <param name="challenge">The challenge data.</param>
    /// <param name="destination">Destination buffer, must be at least 6 + witness.Length + challenge.Length bytes.</param>
    /// <returns>Number of bytes written.</returns>
    private static int BuildAuthResponse(ReadOnlySpan<byte> decryptedWitness, ReadOnlySpan<byte> challenge, Span<byte> destination)
    {
        int witnessLen = decryptedWitness.Length;
        int challengeLen = challenge.Length;

        // Inner: 80 [len] [witness] 81 [len] [challenge]
        int witnessLenSize = BerLength.EncodingSize(witnessLen);
        int challengeLenSize = BerLength.EncodingSize(challengeLen);
        int innerLen = 1 + witnessLenSize + witnessLen + 1 + challengeLenSize + challengeLen;
        int outerLenSize = BerLength.EncodingSize(innerLen);
        int totalLen = 1 + outerLenSize + innerLen;

        // Outer: 7C [len] [inner]
        int offset = 0;

        destination[offset++] = 0x7C;
        offset += BerLength.Write(destination[offset..], innerLen);
        destination[offset++] = 0x80;
        offset += BerLength.Write(destination[offset..], witnessLen);
        decryptedWitness.CopyTo(destination[offset..]);
        offset += witnessLen;
        destination[offset++] = 0x81;
        offset += BerLength.Write(destination[offset..], challengeLen);
        challenge.CopyTo(destination[offset..]);

        return totalLen;
    }

    /// <summary>
    /// Encrypts or decrypts a single block using ECB mode with the specified key type.
    /// </summary>
    private static void CryptoBlock(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input, Span<byte> output, PivManagementKeyType keyType, bool encrypt)
    {
        byte[]? keyBuffer = null;
        byte[]? inputBuffer = null;
        byte[]? outputBuffer = null;
        try
        {
            keyBuffer = ArrayPool<byte>.Shared.Rent(key.Length);
            inputBuffer = ArrayPool<byte>.Shared.Rent(input.Length);
            outputBuffer = ArrayPool<byte>.Shared.Rent(input.Length);

            key.CopyTo(keyBuffer);
            input.CopyTo(inputBuffer);

            if (keyType == PivManagementKeyType.TripleDes)
            {
                if (encrypt)
                {
                    TripleDesEncryptManual(keyBuffer.AsSpan(0, key.Length), inputBuffer.AsSpan(0, input.Length), outputBuffer.AsSpan(0, input.Length));
                }
                else
                {
                    TripleDesDecryptManual(keyBuffer.AsSpan(0, key.Length), inputBuffer.AsSpan(0, input.Length), outputBuffer.AsSpan(0, input.Length));
                }
            }
            else
            {
                byte[]? aesKeyArray = null;
                try
                {
                    using var aes = Aes.Create();
                    aesKeyArray = keyBuffer.AsSpan(0, key.Length).ToArray();
                    aes.Key = aesKeyArray;
                    aes.Mode = CipherMode.ECB;
                    aes.Padding = PaddingMode.None;
                    using var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
                    transform.TransformBlock(inputBuffer, 0, input.Length, outputBuffer, 0);
                }
                finally
                {
                    if (aesKeyArray is not null) CryptographicOperations.ZeroMemory(aesKeyArray);
                }
            }

            outputBuffer.AsSpan(0, input.Length).CopyTo(output);
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
                CryptographicOperations.ZeroMemory(inputBuffer.AsSpan(0, input.Length));
                ArrayPool<byte>.Shared.Return(inputBuffer);
            }
            if (outputBuffer is not null)
            {
                CryptographicOperations.ZeroMemory(outputBuffer.AsSpan(0, input.Length));
                ArrayPool<byte>.Shared.Return(outputBuffer);
            }
        }
    }

    /// <summary>
    /// Manually implements 3DES-ECB decryption using individual DES operations.
    /// This avoids .NET's TripleDES weak key rejection, which blocks the default
    /// PIV management key (0102030405060708 repeated) mandated by the PIV spec.
    /// 3DES Decrypt = DES-Decrypt(K1) ∘ DES-Encrypt(K2) ∘ DES-Decrypt(K3)
    /// </summary>
    private static void TripleDesDecryptManual(ReadOnlySpan<byte> key24, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> temp1 = stackalloc byte[8];
        Span<byte> temp2 = stackalloc byte[8];
        try
        {
            DesBlockOperation(key24[16..24], input, temp1, encrypt: false);
            DesBlockOperation(key24[8..16], temp1, temp2, encrypt: true);
            DesBlockOperation(key24[..8], temp2, output, encrypt: false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(temp1);
            CryptographicOperations.ZeroMemory(temp2);
        }
    }

    /// <summary>
    /// Manually implements 3DES-ECB encryption using individual DES operations.
    /// 3DES Encrypt = DES-Encrypt(K3) ∘ DES-Decrypt(K2) ∘ DES-Encrypt(K1)
    /// </summary>
    private static void TripleDesEncryptManual(ReadOnlySpan<byte> key24, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> temp1 = stackalloc byte[8];
        Span<byte> temp2 = stackalloc byte[8];
        try
        {
            DesBlockOperation(key24[..8], input, temp1, encrypt: true);
            DesBlockOperation(key24[8..16], temp1, temp2, encrypt: false);
            DesBlockOperation(key24[16..24], temp2, output, encrypt: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(temp1);
            CryptographicOperations.ZeroMemory(temp2);
        }
    }

    /// <summary>
    /// Performs a single DES block encrypt or decrypt operation (ECB, no padding).
    /// </summary>
    /// <remarks>
    /// .NET rejects DES weak keys (e.g., all-zeros). The default PIV management key
    /// (0102030405060708) is not a DES weak key, so this is not an issue in practice.
    /// If a weak key is encountered, the CryptographicException will propagate.
    /// </remarks>
    private static void DesBlockOperation(ReadOnlySpan<byte> key8, ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        byte[] keyArr = key8.ToArray();
        byte[] inputArr = input.ToArray();
        byte[] outputArr = new byte[8];
        try
        {
            using var des = DES.Create();
            des.Key = keyArr;
            des.Mode = CipherMode.ECB;
            des.Padding = PaddingMode.None;
            using var transform = encrypt ? des.CreateEncryptor() : des.CreateDecryptor();
            transform.TransformBlock(inputArr, 0, 8, outputArr, 0);
            outputArr.AsSpan().CopyTo(output);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyArr);
            CryptographicOperations.ZeroMemory(inputArr);
            CryptographicOperations.ZeroMemory(outputArr);
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
        EnsureProtocol();

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

            if (SWConstants.ExtractRetryCount(response.SW) is { } retriesRemaining)
            {
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
        EnsureProtocol();

        // Try metadata approach first if supported
        if (IsSupported(PivFeatures.Metadata))
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

        if (SWConstants.ExtractRetryCount(response.SW) is { } retriesRemaining)
        {
            return retriesRemaining;
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
        EnsureProtocol();

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

            if (SWConstants.ExtractRetryCount(response.SW) is { } retriesRemaining)
            {
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