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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
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

        if (FirmwareVersion < new FirmwareVersion(5, 3, 0))
        {
            throw new NotSupportedException("Slot metadata requires firmware 5.3.0 or later");
        }

        // INS 0xF7 (GET METADATA), P2 = slot
        var command = new ApduCommand(0x00, 0xF7, 0x00, (byte)slot, ReadOnlyMemory<byte>.Empty);
        var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
        var response = new ApduResponse(responseData);

        if (response.SW == 0x6A82) // Not found - slot is empty
        {
            return null;
        }

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, 
                $"Failed to get metadata for slot 0x{(byte)slot:X2}");
        }

        // Parse metadata TLV
        var span = response.Data.Span;
        PivAlgorithm algorithm = 0;
        PivPinPolicy pinPolicy = PivPinPolicy.Default;
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default;
        bool isGenerated = false;
        bool isDefault = false;
        ReadOnlyMemory<byte>? publicKey = null;

        int offset = 0;
        while (offset < span.Length)
        {
            byte tag = span[offset++];
            if (offset >= span.Length) break;
            
            int length = span[offset++];
            if (offset + length > span.Length) break;

            switch (tag)
            {
                case 0x01: // Algorithm
                    if (length > 0)
                    {
                        algorithm = (PivAlgorithm)span[offset];
                    }
                    break;
                case 0x02: // Policy
                    if (length >= 2)
                    {
                        pinPolicy = (PivPinPolicy)span[offset];
                        touchPolicy = (PivTouchPolicy)span[offset + 1];
                    }
                    break;
                case 0x03: // Origin
                    if (length > 0)
                    {
                        isGenerated = span[offset] == 0x01;
                    }
                    break;
                case 0x04: // Public key
                    publicKey = response.Data.Slice(offset, length);
                    break;
                case 0x05: // Default value
                    if (length > 0)
                    {
                        isDefault = span[offset] == 0x01;
                    }
                    break;
            }

            offset += length;
        }

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

        // Build TLV: TAG 0x03 [ algorithm ] + TAG 0x9B [ key ]
        var dataList = new List<byte>();
        
        // TAG 0x03 (Algorithm)
        dataList.Add(0x03);
        dataList.Add(0x01);
        dataList.Add((byte)keyType);

        // TAG 0x9B (Key)
        dataList.Add(0x9B);
        dataList.Add((byte)newKey.Length);
        dataList.AddRange(newKey.ToArray());

        // INS 0xFF (SET MANAGEMENT KEY), P1 = 0xFF, P2 = 0xFE (set) or 0xFF (set with touch)
        byte p2 = (byte)(requireTouch ? 0xFE : 0xFF);
        var command = new ApduCommand(0x00, 0xFF, 0xFF, p2, dataList.ToArray());
        var responseData = await _protocol.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
        var response = new ApduResponse(responseData);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to set management key");
        }

        // Update cached key type
        ManagementKeyType = keyType;
    }

    // Stub implementations for less critical metadata methods
    public Task<PivPukMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement PUK metadata retrieval (INS 0xF7, P2=0x81)
        throw new NotImplementedException("GetPukMetadataAsync not yet implemented");
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
        var response = await _protocol.TransmitAsync(command, cancellationToken).ConfigureAwait(false);

        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == 0x6D00)
        {
            throw new NotSupportedException("Management key metadata requires firmware 5.3.0 or later");
        }

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to get management key metadata");
        }

        // Parse metadata TLV
        // TAG 0x01 = algorithm (ManagementKeyType)
        // TAG 0x02 = policy (byte 0 = unused, byte 1 = touch policy)
        // TAG 0x05 = isDefault (1 = default, 0 = not default)
        var span = response.Data.Span;
        PivManagementKeyType keyType = PivManagementKeyType.TripleDes;
        PivTouchPolicy touchPolicy = PivTouchPolicy.Default;
        bool isDefault = false;

        int offset = 0;
        while (offset < span.Length)
        {
            byte tag = span[offset++];
            if (offset >= span.Length) break;
            
            int length = span[offset++];
            if (offset + length > span.Length) break;

            switch (tag)
            {
                case 0x01: // Algorithm
                    if (length > 0)
                    {
                        keyType = (PivManagementKeyType)span[offset];
                    }
                    break;
                case 0x02: // Policy (2 bytes: pin policy, touch policy)
                    if (length >= 2)
                    {
                        // Index 0 = pin policy (unused for mgmt key)
                        // Index 1 = touch policy
                        touchPolicy = (PivTouchPolicy)span[offset + 1];
                    }
                    break;
                case 0x05: // Default value
                    if (length > 0)
                    {
                        isDefault = span[offset] != 0;
                    }
                    break;
            }

            offset += length;
        }

        Logger.LogDebug("PIV: Management key metadata: type={KeyType}, isDefault={IsDefault}, touchPolicy={TouchPolicy}", 
            keyType, isDefault, touchPolicy);

        return new PivManagementKeyMetadata(
            KeyType: keyType,
            IsDefault: isDefault,
            TouchPolicy: touchPolicy
        );
    }

    public Task ChangePukAsync(ReadOnlyMemory<byte> oldPuk, ReadOnlyMemory<byte> newPuk, CancellationToken cancellationToken = default)
    {
        // TODO: Implement PUK change (INS 0x24, P2=0x81)
        throw new NotImplementedException("ChangePukAsync not yet implemented");
    }

    public Task UnblockPinAsync(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default)
    {
        // TODO: Implement PIN unblock with PUK (INS 0x2C, P2=0x80)
        throw new NotImplementedException("UnblockPinAsync not yet implemented");
    }

    public Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default)
    {
        // TODO: Implement set PIN/PUK retry counts (INS 0xFA)
        throw new NotImplementedException("SetPinAttemptsAsync not yet implemented");
    }
}
