// Copyright 2026 Yubico AB
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

using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Piv;

public sealed partial class PivSession
{
    /// <summary>
    /// Gets metadata about a key slot (requires firmware 5.3+).
    /// </summary>
    public async Task<PivSlotMetadata?> GetSlotMetadataAsync(
        PivSlot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting slot metadata for 0x{Slot:X2}", (byte)slot);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // INS 0xF7 (GET METADATA), P2 = slot
        var command = new ApduCommand(0x00, 0xF7, 0x00, (byte)slot, ReadOnlyMemory<byte>.Empty);
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == 0x6D00)
        {
            throw new NotSupportedException("Slot metadata requires firmware 5.3.0 or later");
        }

        // Check for empty slot - 0x6A82 "File not found" or 0x6A88 "Referenced data not found"
        if (response.SW is 0x6A82 or 0x6A88)
        {
            return null;
        }

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, 
                $"Failed to get metadata for slot 0x{(byte)slot:X2}");
        }

        // Parse metadata TLV using TlvHelper
        var tlvDict = TlvHelper.DecodeDictionary(response.Data.Span);

        var algorithm = tlvDict.TryGetValue(0x01, out var alg) && alg.Length > 0
            ? (PivAlgorithm)alg.Span[0]
            : (PivAlgorithm)0;

        var (pinPolicy, touchPolicy) = tlvDict.TryGetValue(0x02, out var policy) && policy.Length >= 2
            ? ((PivPinPolicy)policy.Span[0], (PivTouchPolicy)policy.Span[1])
            : (PivPinPolicy.Default, PivTouchPolicy.Default);

        var isGenerated = tlvDict.TryGetValue(0x03, out var origin) && origin.Length > 0
            && origin.Span[0] == 0x01;

        ReadOnlyMemory<byte>? publicKey = tlvDict.TryGetValue(0x04, out var pk) ? pk : null;

        var isDefault = tlvDict.TryGetValue(0x05, out var def) && def.Length > 0
            && def.Span[0] == 0x01;

        return new PivSlotMetadata(
            Algorithm: algorithm,
            PinPolicy: pinPolicy,
            TouchPolicy: touchPolicy,
            IsGenerated: isGenerated,
            PublicKey: publicKey ?? ReadOnlyMemory<byte>.Empty
        );
    }

    /// <summary>
    /// Sets the management key (requires authentication with old key).
    /// </summary>
    public async Task SetManagementKeyAsync(
        PivManagementKeyType keyType,
        ReadOnlyMemory<byte> newKey,
        bool requireTouch = false,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Setting management key, type {KeyType}", keyType);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Current management key authentication required");
        }

        // Validate key length
        int expectedLength = keyType switch
        {
            PivManagementKeyType.TripleDes => 24,
            PivManagementKeyType.Aes128 => 16,
            PivManagementKeyType.Aes192 => 24,
            PivManagementKeyType.Aes256 => 32,
            _ => throw new ArgumentException($"Unsupported key type: {keyType}", nameof(keyType))
        };

        if (newKey.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Invalid key length {newKey.Length} for {keyType}. Expected {expectedLength} bytes.",
                nameof(newKey));
        }

        // Build data: [algorithm] [slot 0x9B] [length] [key data]
        // Format per Yubico PIV spec: algorithm byte, slot byte, length byte, key bytes
        var dataList = new List<byte>
        {
            (byte)keyType,      // Algorithm byte (0x03=3DES, 0x08=AES128, 0x0A=AES192, 0x0C=AES256)
            0x9B,               // Slot 9B (management key slot)
            (byte)newKey.Length // Key length
        };
        dataList.AddRange(newKey.ToArray());

        // INS 0xFF (SET MANAGEMENT KEY), P1 = 0xFF
        // P2 = touch policy: 0xFF (no touch), 0xFE (touch required), 0xFD (cached touch)
        byte p2 = (byte)(requireTouch ? 0xFE : 0xFF);
        var command = new ApduCommand(0x00, 0xFF, 0xFF, p2, dataList.ToArray());
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to set management key");
        }

        // Update cached key type
        ManagementKeyType = keyType;
    }

    /// <summary>
    /// Gets metadata about the PIV PUK.
    /// </summary>
    public async Task<PivPukMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting PUK metadata");

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // INS 0xF7 (GET METADATA), P2 = 0x81 (PUK slot)
        var command = new ApduCommand(0x00, 0xF7, 0x00, 0x81, ReadOnlyMemory<byte>.Empty);
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == 0x6D00)
        {
            throw new NotSupportedException("PUK metadata requires firmware 5.3.0 or later");
        }

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to get PUK metadata");
        }

        // Parse TLV structure using TlvHelper
        // TAG 0x05 = isDefault, TAG 0x06 = retries [total, remaining]
        var tlvDict = TlvHelper.DecodeDictionary(response.Data);
        
        bool isDefault = tlvDict.TryGetValue(0x05, out var defaultValue) && 
                         defaultValue.Length > 0 && defaultValue.Span[0] != 0;
        
        int totalRetries = 0;
        int retriesRemaining = 0;
        if (tlvDict.TryGetValue(0x06, out var retriesValue) && retriesValue.Length >= 2)
        {
            totalRetries = retriesValue.Span[0];
            retriesRemaining = retriesValue.Span[1];
        }

        return new PivPukMetadata(isDefault, totalRetries, retriesRemaining);
    }

    public async Task<PivManagementKeyMetadata> GetManagementKeyMetadataAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting management key metadata");

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // INS 0xF7 (GET METADATA), P2 = 0x9B (SLOT_CARD_MANAGEMENT)
        const byte InsGetMetadata = 0xF7;
        const byte SlotCardManagement = 0x9B;
        
        var command = new ApduCommand(0x00, InsGetMetadata, 0x00, SlotCardManagement, ReadOnlyMemory<byte>.Empty);
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == 0x6D00)
        {
            throw new NotSupportedException("Management key metadata requires firmware 5.3.0 or later");
        }

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to get management key metadata");
        }

        // Parse metadata TLV using TlvHelper
        // TAG 0x01 = algorithm (ManagementKeyType)
        // TAG 0x02 = policy (byte 0 = unused, byte 1 = touch policy)
        // TAG 0x05 = isDefault (1 = default, 0 = not default)
        var tlvDict = TlvHelper.DecodeDictionary(response.Data.Span);

        var keyType = tlvDict.TryGetValue(0x01, out var alg) && alg.Length > 0
            ? (PivManagementKeyType)alg.Span[0]
            : PivManagementKeyType.TripleDes;

        var touchPolicy = tlvDict.TryGetValue(0x02, out var policy) && policy.Length >= 2
            ? (PivTouchPolicy)policy.Span[1]  // Index 1 = touch policy
            : PivTouchPolicy.Default;

        var isDefault = tlvDict.TryGetValue(0x05, out var def) && def.Length > 0
            && def.Span[0] != 0;

        Logger.LogDebug("PIV: Management key metadata: type={KeyType}, isDefault={IsDefault}, touchPolicy={TouchPolicy}", 
            keyType, isDefault, touchPolicy);

        return new PivManagementKeyMetadata(
            KeyType: keyType,
            IsDefault: isDefault,
            TouchPolicy: touchPolicy
        );
    }

    /// <summary>
    /// Changes the PUK from old PUK to new PUK.
    /// </summary>
    public async Task ChangePukAsync(ReadOnlyMemory<byte> oldPuk, ReadOnlyMemory<byte> newPuk, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Changing PUK");

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // Build data: [8-byte old PUK padded with 0xFF] [8-byte new PUK padded with 0xFF]
        byte[] pukPair = PivPinUtilities.EncodePinPairBytes(oldPuk.Span, newPuk.Span);
        try
        {
            // INS 0x24 (CHANGE REFERENCE DATA), P2=0x81 (PUK reference)
            var command = new ApduCommand(0x00, 0x24, 0x00, 0x81, pukPair);
            var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

            if (!response.IsOK())
            {
                int retriesRemaining = PivPinUtilities.GetRetriesFromStatusWord(response.SW);
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
    public async Task UnblockPinAsync(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Unblocking PIN with PUK");

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // Build data: [8-byte PUK padded with 0xFF] [8-byte new PIN padded with 0xFF]
        byte[] pukPinPair = PivPinUtilities.EncodePinPairBytes(puk.Span, newPin.Span);
        try
        {
            // INS 0x2C (RESET RETRY COUNTER), P2=0x80 (PIN reference)
            var command = new ApduCommand(0x00, 0x2C, 0x00, 0x80, pukPinPair);
            var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

            if (!response.IsOK())
            {
                // 0x6983 = PUK blocked
                if (response.SW == 0x6983)
                {
                    throw new InvalidPinException(0, "PUK is blocked");
                }
                
                int retriesRemaining = PivPinUtilities.GetRetriesFromStatusWord(response.SW);
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
    /// Sets the PIN and PUK retry limits.
    /// </summary>
    /// <remarks>
    /// Requires management key authentication. This command also resets PIN and PUK to defaults.
    /// </remarks>
    public async Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Setting PIN attempts to {PinAttempts}, PUK attempts to {PukAttempts}", pinAttempts, pukAttempts);

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required");
        }

        if (pinAttempts is < 1 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(pinAttempts), "PIN attempts must be 1-255");
        }

        if (pukAttempts is < 1 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(pukAttempts), "PUK attempts must be 1-255");
        }

        // INS 0xFA (SET PIN RETRIES), P1=PIN retries, P2=PUK retries, no data
        var command = new ApduCommand(0x00, 0xFA, (byte)pinAttempts, (byte)pukAttempts);
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to set PIN/PUK retry limits");
        }
    }
}
