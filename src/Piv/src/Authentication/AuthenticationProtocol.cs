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

using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;

namespace Yubico.YubiKit.Piv.Authentication;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

internal static class AuthenticationProtocol
{
    private const byte Cla = 0x00;
    private const byte InsAuthenticate = 0x87;
    private const byte InsVerify = 0x20;
    private const byte InsChangeReference = 0x24;
    private const byte InsResetRetry = 0x2C;
    private const byte InsSetRetries = 0xFA;
    private const byte TagDynAuth = 0x7C;
    private const byte TagAuthWitness = 0x80;
    private const byte TagAuthChallenge = 0x81;
    private const byte TagAuthResponse = 0x82;
    private const byte InsSetManagementKey = 0xFF;
    private const byte SetManagementKeyParameter = 0xFF;
    private const byte TouchRequiredParameter = 0xFE;
    private const byte NoTouchParameter = 0xFF;
    private const byte ShortTlvHeaderLength = 2;
    private const int AuthenticationHeaderLength = 6;
    private const int ManagementKeyHeaderLength = 3;
    private const int PinPairLength = PinUtilities.PinLength * 2;
    private const int DesBlockSize = 8;
    private const int AesBlockSize = 16;
    private static readonly int DesKeyComponentLength = KeyType.TripleDes.GetKeyInfo().KeySizeBytes / 3;

    private static ILogger Logger => YubiKitLogging.CreateLogger(nameof(AuthenticationProtocol));

    /// <summary>
    /// Gets the well-known 24-byte factory-default PIV management key value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>⚠️ SECURITY WARNING:</b> This is the well-known default management key shipped with all YubiKeys.
    /// You SHOULD change this key after initial device setup to protect against unauthorized management operations.
    /// </para>
    /// <para>
    /// The default key is: 01 02 03 04 05 06 07 08 01 02 03 04 05 06 07 08 01 02 03 04 05 06 07 08.
    /// These 24 bytes are used with Triple-DES and AES-192 defaults; the session's management-key
    /// type determines the algorithm.
    /// </para>
    /// </remarks>
    private static readonly byte[] DefaultManagementKeyBytes =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    internal static ReadOnlySpan<byte> DefaultManagementKey =>
        DefaultManagementKeyBytes.AsSpan(0, KeyType.TripleDes.GetKeyInfo().KeySizeBytes);

    internal static ReadOnlyMemory<byte> GetDefaultManagementKey(KeyType keyType)
    {
        _ = KeyTypeEncoding.EncodeManagement(keyType);
        return DefaultManagementKeyBytes.AsMemory(0, keyType.GetKeyInfo().KeySizeBytes);
    }

    /// <summary>
    /// Authenticates the session using the PIV management key.
    /// </summary>
    /// <param name="managementKey">The management key bytes. Length must match <paramref name="managementKeyType"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown if the management key is the wrong length.</exception>
    /// <exception cref="ApduException">Thrown if authentication fails.</exception>
    /// <remarks>
    /// <para>
    /// Authentication uses mutual challenge-response to prove knowledge of the management key
    /// without transmitting it. Only SDK-owned working copies are cleared; caller input remains unchanged.
    /// </para>
    /// <para>
    /// Default management key bytes (Triple-DES or AES-192): 01 02 03 04 05 06 07 08 01 02 03 04 05 06 07 08 01 02 03 04 05 06 07 08
    /// </para>
    /// </remarks>
    internal static async Task AuthenticateAsync(
        IBackend backend,
        KeyType managementKeyType,
        ReadOnlyMemory<byte> managementKey,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Starting management key authentication with {KeyType}", managementKeyType);

        // Validate key length based on management key type
        byte algorithmId = KeyTypeEncoding.EncodeManagement(managementKeyType);
        int expectedKeyLength = managementKeyType.GetKeyInfo().KeySizeBytes;

        if (managementKey.Length != expectedKeyLength)
        {
            throw new ArgumentException(
                $"Invalid management key length: {managementKey.Length}. Expected {expectedKeyLength} bytes for {managementKeyType}.",
                nameof(managementKey));
        }

        // Challenge length: 8 bytes for 3DES, 16 bytes for AES
        int challengeLength = managementKeyType == KeyType.TripleDes ? DesBlockSize : AesBlockSize;


        var keyBytes = ArrayPool<byte>.Shared.Rent(expectedKeyLength);
        try
        {
            managementKey.CopyTo(keyBytes);

            // Step 1: Request witness from device
            // Send: 7C 02 80 00 (empty witness request)
            byte[] witnessRequest = [TagDynAuth, ShortTlvHeaderLength, TagAuthWitness, 0];
            var witnessCommand = new ApduCommand(Cla, InsAuthenticate, algorithmId, KeyReference.Management, witnessRequest);
            var witnessResponse = await backend.SendAsync(witnessCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);

            // Parse witness response: 7C [len] 80 [len] [witness data]
            var witnessBytes = ParseAndClearAuthResponse(witnessResponse, TagAuthWitness, challengeLength);

            // Step 2: Decrypt witness with our key
            byte[]? decryptedWitness = null;
            byte[]? challenge = null;
            byte[]? responseBuffer = null;
            byte[]? expectedResponse = null;
            byte[]? encryptedChallenge = null;

            try
            {
                decryptedWitness = ArrayPool<byte>.Shared.Rent(challengeLength);
                DecryptBlock(keyBytes.AsSpan(0, expectedKeyLength), witnessBytes, decryptedWitness.AsSpan(0, challengeLength), managementKeyType);

                // Step 3: Generate our challenge
                challenge = ArrayPool<byte>.Shared.Rent(challengeLength);
                RandomNumberGenerator.Fill(challenge.AsSpan(0, challengeLength));

                // Step 4: Build and send response with decrypted witness and our challenge
                // Send: 7C [len] 80 [len] [decrypted witness] 81 [len] [challenge]
                // Max size: 1 (tag) + 3 (BER len) + 1 (tag) + 3 (BER len) + challengeLength + 1 (tag) + 3 (BER len) + challengeLength
                int responseSize = AuthenticationHeaderLength + (2 * challengeLength);
                responseBuffer = ArrayPool<byte>.Shared.Rent(responseSize);
                int bytesWritten = BuildAuthResponse(decryptedWitness.AsSpan(0, challengeLength), challenge.AsSpan(0, challengeLength), responseBuffer.AsSpan(0, responseSize));
                var challengeCommand = new ApduCommand(Cla, InsAuthenticate, algorithmId, KeyReference.Management, responseBuffer.AsMemory(0, bytesWritten));
                var challengeResponse = await backend.SendAsync(challengeCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);

                // Step 5: Verify device's response (encrypted challenge)
                // Response: 7C [len] 82 [len] [encrypted challenge]
                encryptedChallenge = ParseAndClearAuthResponse(challengeResponse, TagAuthResponse, challengeLength);

                // Encrypt our challenge and compare
                expectedResponse = ArrayPool<byte>.Shared.Rent(challengeLength);
                EncryptBlock(keyBytes.AsSpan(0, expectedKeyLength), challenge.AsSpan(0, challengeLength), expectedResponse.AsSpan(0, challengeLength), managementKeyType);

                if (!CryptographicOperations.FixedTimeEquals(encryptedChallenge, expectedResponse.AsSpan(0, challengeLength)))
                {
                    throw new ApduException("Management key authentication failed - device response mismatch");
                }

                Logger.LogDebug("PIV: Management key authentication succeeded");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(witnessBytes);
                if (encryptedChallenge is not null)
                    CryptographicOperations.ZeroMemory(encryptedChallenge);
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
                    int responseSize = AuthenticationHeaderLength + (2 * challengeLength);
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

    private static byte[] ParseAndClearAuthResponse(ApduResponse response, byte expectedTag, int expectedLength)
    {
        try
        {
            if (!response.IsOK())
                throw ApduException.FromStatusWord(response.SW, "Management key authentication failed");
            return ParseAuthResponse(response.Data.Span, expectedTag, expectedLength);
        }
        finally
        {
            response.Clear();
        }
    }

    /// <summary>
    /// Parses an authentication response TLV: 7C [len] [expectedInnerTag] [len] [data].
    /// Used for both witness (tag 0x80) and challenge (tag 0x82) responses.
    /// </summary>
    private static byte[] ParseAuthResponse(ReadOnlySpan<byte> response, byte expectedInnerTag, int expectedLength)
    {
        using var outer = Tlv.Create(response);
        if (outer.Tag != TagDynAuth)
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

        destination[offset++] = TagDynAuth;
        offset += BerLength.Write(destination[offset..], innerLen);
        destination[offset++] = TagAuthWitness;
        offset += BerLength.Write(destination[offset..], witnessLen);
        decryptedWitness.CopyTo(destination[offset..]);
        offset += witnessLen;
        destination[offset++] = TagAuthChallenge;
        offset += BerLength.Write(destination[offset..], challengeLen);
        challenge.CopyTo(destination[offset..]);

        return totalLen;
    }

    /// <summary>
    /// Encrypts a single block using ECB mode with the specified key type.
    /// </summary>
    private static void EncryptBlock(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, KeyType keyType) =>
        CryptoBlockCore(key, plaintext, ciphertext, keyType, encrypt: true);

    /// <summary>
    /// Decrypts a single block using ECB mode with the specified key type.
    /// </summary>
    private static void DecryptBlock(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext, KeyType keyType) =>
        CryptoBlockCore(key, ciphertext, plaintext, keyType, encrypt: false);

    /// <summary>
    /// Shared implementation for single-block ECB encrypt/decrypt with buffer management.
    /// </summary>
    private static void CryptoBlockCore(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input, Span<byte> output, KeyType keyType, bool encrypt)
    {
        if (keyType == KeyType.TripleDes)
        {
            // Preserve explicit EDE for the public factory default, which .NET rejects as a
            // weak TripleDES key. Yubico uses this factory pattern; each DES component is still valid.
            if (encrypt)
                TripleDesEncryptManual(key, input, output);
            else
                TripleDesDecryptManual(key, input, output);
            return;
        }
        byte[] keyCopy = key.ToArray();
        try
        {
            using var aes = Aes.Create();
            aes.Key = keyCopy;
            if (encrypt)
                _ = aes.EncryptEcb(input, output, PaddingMode.None);
            else
                _ = aes.DecryptEcb(input, output, PaddingMode.None);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyCopy);
        }
    }

    /// <summary>
    /// Manually implements 3DES-ECB decryption using individual DES operations.
    /// This avoids .NET's TripleDES weak key rejection, which blocks the default
    /// Yubico factory management key (0102030405060708 repeated).
    /// 3DES Decrypt = DES-Decrypt(K1) ∘ DES-Encrypt(K2) ∘ DES-Decrypt(K3)
    /// </summary>
    private static void TripleDesDecryptManual(ReadOnlySpan<byte> key24, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> temp1 = stackalloc byte[DesBlockSize];
        Span<byte> temp2 = stackalloc byte[DesBlockSize];
        try
        {
            DesBlockOperation(key24[(2 * DesKeyComponentLength)..], input, temp1, encrypt: false);
            DesBlockOperation(key24[DesKeyComponentLength..(2 * DesKeyComponentLength)], temp1, temp2, encrypt: true);
            DesBlockOperation(key24[..DesKeyComponentLength], temp2, output, encrypt: false);
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
        Span<byte> temp1 = stackalloc byte[DesBlockSize];
        Span<byte> temp2 = stackalloc byte[DesBlockSize];
        try
        {
            DesBlockOperation(key24[..DesKeyComponentLength], input, temp1, encrypt: true);
            DesBlockOperation(key24[DesKeyComponentLength..(2 * DesKeyComponentLength)], temp1, temp2, encrypt: false);
            DesBlockOperation(key24[(2 * DesKeyComponentLength)..], temp2, output, encrypt: true);
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
        byte[] keyCopy = key8.ToArray();
        try
        {
            using var des = DES.Create();
            des.Key = keyCopy;
            if (encrypt)
                _ = des.EncryptEcb(input, output, PaddingMode.None);
            else
                _ = des.DecryptEcb(input, output, PaddingMode.None);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyCopy);
        }
    }

    /// <summary>
    /// Verifies the user PIN.
    /// </summary>
    /// <param name="pin">The caller-owned PIN as 6-8 UTF-8 encoded bytes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="InvalidPinException">Thrown if PIN verification fails.</exception>
    /// <exception cref="ArgumentException">Thrown if PIN is invalid format.</exception>
    /// <remarks>
    /// <para>
    /// PIN verification is required for certain operations like signing and decryption.
    /// The card controls verification lifetime according to the key policy and card/application state.
    /// </para>
    /// <para>
    /// Failed attempts consume the configured retry budget (factory default three). A usable PUK
    /// can unblock the PIN; when both credentials are blocked, recovery needs a separate reset decision.
    /// </para>
    /// <para>
    /// Default PIN: 123456
    /// </para>
    /// </remarks>
    internal static async Task VerifyPinAsync(
        IBackend backend,
        ReadOnlyMemory<byte> pin,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Verifying PIN");

        if (pin.Length is < 6 or > PinUtilities.PinLength)
        {
            throw new ArgumentException("PIN must be 6-8 bytes", nameof(pin));
        }

        var paddedPin = ArrayPool<byte>.Shared.Rent(PinUtilities.PinLength);
        try
        {
            // Pad PIN to 8 bytes with 0xFF
            pin.Span.CopyTo(paddedPin);
            Array.Fill(paddedPin, PinUtilities.PaddingByte, pin.Length, PinUtilities.PinLength - pin.Length);

            var command = new ApduCommand(Cla, InsVerify, 0, KeyReference.Pin, paddedPin.AsMemory(0, PinUtilities.PinLength));
            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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
            CryptographicOperations.ZeroMemory(paddedPin.AsSpan(0, PinUtilities.PinLength));
            ArrayPool<byte>.Shared.Return(paddedPin);
        }
    }

    /// <summary>
    /// Gets the number of PIN verification attempts remaining.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A status-word retry count from 0–15. Success without a count raises InvalidOperationException.</returns>
    /// <remarks>
    /// This fallback sends VERIFY with absent data, which queries state without consuming retries.
    /// It differs from submitting a padded empty credential while deliberately blocking the PIN.
    /// </remarks>
    internal static async Task<int> GetPinAttemptsAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting PIN attempt count");

        cancellationToken.ThrowIfCancellationRequested();
        // Fallback: absent-data VERIFY queries retry state without presenting a credential.
        var command = new ApduCommand(Cla, InsVerify, 0, KeyReference.Pin, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        if (SWConstants.ExtractRetryCount(response.SW) is { } retriesRemaining)
        {
            return retriesRemaining;
        }

        // PIN is blocked
        if (response.SW == SWConstants.AuthenticationMethodBlocked)
        {
            return 0;
        }

        if (response.IsOK())
            throw new InvalidOperationException("The PIN is already verified; the device did not report a retry count.");
        throw ApduException.FromStatusWord(response.SW, "Unable to determine PIN attempts remaining");
    }

    /// <summary>
    /// Changes the PIN.
    /// </summary>
    /// <param name="currentPin">The current PIN.</param>
    /// <param name="newPin">The caller-owned new PIN as 6-8 UTF-8 encoded bytes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="InvalidPinException">Thrown if current PIN is incorrect.</exception>
    /// <exception cref="ArgumentException">Thrown if PIN format is invalid.</exception>
    /// <remarks>
    /// SDK-owned encoded working copies are cleared after use; caller input buffers are never cleared.
    /// </remarks>
    internal static async Task ChangePinAsync(
        IBackend backend,
        ReadOnlyMemory<byte> currentPin,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Changing PIN");

        if (currentPin.Length is < 6 or > PinUtilities.PinLength)
        {
            throw new ArgumentException("Current PIN must be 6-8 bytes", nameof(currentPin));
        }

        if (newPin.Length is < 6 or > PinUtilities.PinLength)
        {
            throw new ArgumentException("New PIN must be 6-8 bytes", nameof(newPin));
        }

        var pinData = ArrayPool<byte>.Shared.Rent(PinPairLength);
        try
        {
            // Format: [current PIN padded to 8][new PIN padded to 8]
            currentPin.Span.CopyTo(pinData);
            Array.Fill(pinData, PinUtilities.PaddingByte, currentPin.Length, PinUtilities.PinLength - currentPin.Length);

            newPin.Span.CopyTo(pinData.AsSpan(PinUtilities.PinLength));
            Array.Fill(pinData, PinUtilities.PaddingByte, PinUtilities.PinLength + newPin.Length, PinUtilities.PinLength - newPin.Length);

            var command = new ApduCommand(Cla, InsChangeReference, 0, KeyReference.Pin, pinData.AsMemory(0, PinPairLength));
            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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
            CryptographicOperations.ZeroMemory(pinData.AsSpan(0, PinPairLength));
            ArrayPool<byte>.Shared.Return(pinData);
        }
    }

    /// <summary>
    /// Sets the management key (requires authentication with old key).
    /// </summary>
    internal static async Task<KeyType> SetManagementKeyAsync(
        IBackend backend,
        KeyType keyType,
        ReadOnlyMemory<byte> newKey,
        bool requireTouch = false,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Setting management key, type {KeyType}", keyType);

        // Validate key length
        byte algorithmId = KeyTypeEncoding.EncodeManagement(keyType);
        int expectedLength = keyType.GetKeyInfo().KeySizeBytes;

        if (newKey.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Invalid key length {newKey.Length} for {keyType}. Expected {expectedLength} bytes.",
                nameof(newKey));
        }

        // Build data: [algorithm] [slot 0x9B] [length] [key data]
        // Format per Yubico PIV spec: algorithm byte, slot byte, length byte, key bytes
        var dataLength = ManagementKeyHeaderLength + newKey.Length; // header (3 bytes) + key
        var data = new byte[dataLength];
        try
        {
            data[0] = algorithmId;
            data[1] = KeyReference.Management;
            data[2] = (byte)newKey.Length;
            newKey.Span.CopyTo(data.AsSpan(ManagementKeyHeaderLength));

            // The boolean API selects P2 FF (no touch) or FE (touch required).
            byte p2 = requireTouch ? TouchRequiredParameter : NoTouchParameter;
            var command = new ApduCommand(Cla, InsSetManagementKey, SetManagementKeyParameter, p2, data);
            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

            if (!response.IsOK())
            {
                throw ApduException.FromStatusWord(response.SW, "Failed to set management key");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }

        return keyType;
    }

    /// <summary>
    /// Changes the PUK from old PUK to new PUK.
    /// </summary>
    internal static async Task ChangePukAsync(
        IBackend backend,
        ReadOnlyMemory<byte> oldPuk,
        ReadOnlyMemory<byte> newPuk,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Changing PUK");

        // Build data: [8-byte old PUK padded with 0xFF] [8-byte new PUK padded with 0xFF]
        byte[] pukPair = PinUtilities.EncodePinPairBytes(oldPuk.Span, newPuk.Span);
        try
        {
            var command = new ApduCommand(Cla, InsChangeReference, 0, KeyReference.Puk, pukPair);
            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
            if (response.SW == SWConstants.InsNotSupported)
                throw new NotSupportedException("Changing the PUK is not supported by this YubiKey.");

            if (!response.IsOK())
            {
                int retriesRemaining = PinUtilities.GetRetriesFromStatusWord(response.SW);
                if (retriesRemaining >= 0)
                {
                    throw new InvalidPinException(retriesRemaining, "Invalid PUK");
                }
                throw ApduException.FromStatusWord(response.SW, "Failed to change PUK");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pukPair);
        }
    }

    /// <summary>
    /// Unblocks the PIN using the PUK and sets a new PIN.
    /// </summary>
    internal static async Task UnblockPinAsync(
        IBackend backend,
        ReadOnlyMemory<byte> puk,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Unblocking PIN with PUK");

        // Build data: [8-byte PUK padded with 0xFF] [8-byte new PIN padded with 0xFF]
        byte[] pukPinPair = PinUtilities.EncodePinPairBytes(puk.Span, newPin.Span);
        try
        {
            var command = new ApduCommand(Cla, InsResetRetry, 0, KeyReference.Pin, pukPinPair);
            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
            if (response.SW == SWConstants.InsNotSupported)
                throw new NotSupportedException("Unblocking the PIN is not supported by this YubiKey.");

            if (!response.IsOK())
            {
                // 0x6983 = PUK blocked
                if (response.SW == SWConstants.AuthenticationMethodBlocked)
                {
                    throw new InvalidPinException(0, "PUK is blocked");
                }

                int retriesRemaining = PinUtilities.GetRetriesFromStatusWord(response.SW);
                if (retriesRemaining >= 0)
                {
                    throw new InvalidPinException(retriesRemaining, "Invalid PUK");
                }
                throw ApduException.FromStatusWord(response.SW, "Failed to unblock PIN");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pukPinPair);
        }
    }

    /// <summary>
    /// Blocks the PUK by repeatedly calling RESET RETRY with empty credentials until blocked.
    /// </summary>
    /// <remarks>
    /// Shared by <see cref="PivSession.ResetAsync"/> and PIN-only mode setup (enabling a PIN-only
    /// mode requires the PUK to be blocked, since it can no longer be used to unblock a PIN that
    /// is protected by a management key the PUK holder may not know).
    /// </remarks>
    internal static async Task BlockPukAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Blocking PUK");

        // PUK blocking uses INS_RESET_RETRY (0x2C) with P2=0x80 (PIN_P2, not PUK_P2!)
        // Data is 16 bytes: 8-byte empty PUK + 8-byte empty PIN (both all 0xFF)
        byte[] emptyPukPin = PinUtilities.EncodePinPair(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty);
        try
        {
            int retriesRemaining = 1; // Start with 1 to enter loop
            while (retriesRemaining > 0)
            {
                var pukCommand = new ApduCommand(Cla, InsResetRetry, 0, KeyReference.Pin, emptyPukPin);
                var response = await backend.SendAsync(pukCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);

                retriesRemaining = PinUtilities.GetRetriesFromStatusWord(response.SW);
                if (retriesRemaining < 0)
                {
                    throw ApduException.FromStatusWord(response.SW, "Failed to block PUK");
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(emptyPukPin);
        }

        Logger.LogDebug("PIV: PUK blocked");
    }

    /// <summary>
    /// Sets the PIN and PUK retry limits.
    /// </summary>
    /// <remarks>
    /// The session checks management key authentication here; the card also requires a verified PIN,
    /// which the SDK cannot observe. See <see cref="IPivSession.SetPinAttemptsAsync" /> for the
    /// credential reset this command performs.
    /// </remarks>
    internal static async Task SetPinAttemptsAsync(
        IBackend backend,
        int pinAttempts,
        int pukAttempts,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Setting PIN attempts to {PinAttempts}, PUK attempts to {PukAttempts}", pinAttempts, pukAttempts);

        if (pinAttempts is < 1 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(pinAttempts), "PIN attempts must be 1-255");
        }

        if (pukAttempts is < 1 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(pukAttempts), "PUK attempts must be 1-255");
        }

        var command = new ApduCommand(Cla, InsSetRetries, (byte)pinAttempts, (byte)pukAttempts);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
        if (response.SW == SWConstants.InsNotSupported)
            throw new NotSupportedException("Setting PIN/PUK retries is not supported by this YubiKey.");

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to set PIN/PUK retry limits");
        }
    }

    internal static async Task BlockPinAsync(IBackend backend, int retriesRemaining, CancellationToken cancellationToken)
    {
        // Empty PIN encodes as 8 bytes of 0xFF
        byte[] emptyPin = PinUtilities.EncodePinBytes(ReadOnlySpan<char>.Empty);
        try
        {
            while (retriesRemaining > 0)
            {
                var pinCommand = new ApduCommand(Cla, InsVerify, 0, KeyReference.Pin, emptyPin);
                var response = await backend.SendAsync(pinCommand, throwOnError: false, cancellationToken).ConfigureAwait(false);

                retriesRemaining = PinUtilities.GetRetriesFromStatusWord(response.SW);
                if (retriesRemaining < 0)
                {
                    throw ApduException.FromStatusWord(response.SW, "Failed to block PIN");
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(emptyPin);
        }

    }
}