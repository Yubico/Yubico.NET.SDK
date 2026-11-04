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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;

namespace Yubico.YubiKit.Piv.Metadata;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

internal static class MetadataProtocol
{
    private static ILogger Logger => YubiKitLogging.CreateLogger("Yubico.YubiKit.Piv.Metadata.MetadataProtocol");

    internal static async Task<PinMetadata> GetPinMetadataAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting PIN metadata");

        var command = new ApduCommand(0x00, 0xF7, 0x00, 0x80, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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

        return ParsePinMetadata(response.Data);
    }

    private static PinMetadata ParsePinMetadata(ReadOnlyMemory<byte> data)
    {
        var fields = DecodeMetadataFields(data);
        var defaultValue = RequiredField(fields, 0x05, 1);
        var retries = RequiredField(fields, 0x06, 2);
        if (defaultValue.Span[0] > 1 || retries.Span[1] > retries.Span[0])
            throw new ApduException("Invalid PIN/PUK metadata values.");
        return new PinMetadata(defaultValue.Span[0] != 0, retries.Span[0], retries.Span[1]);
    }

    private static ReadOnlyMemory<byte> RequiredField(
        IDictionary<int, ReadOnlyMemory<byte>> fields, int tag, int length)
    {
        if (!fields.TryGetValue(tag, out var value) || value.Length != length)
            throw new ApduException($"Missing or invalid metadata field 0x{tag:X2}.");
        return value;
    }

    private static IDictionary<int, ReadOnlyMemory<byte>> DecodeMetadataFields(ReadOnlyMemory<byte> data)
    {
        try
        {
            return TlvHelper.DecodeDictionary(data);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            throw new ApduException("Malformed PIV metadata response.", ex);
        }
    }

    /// <summary>
    /// Gets metadata about a key slot (requires firmware 5.3+).
    /// </summary>
    internal static async Task<SlotMetadata?> GetSlotMetadataAsync(
        IBackend backend,
        Slot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting slot metadata for 0x{Slot:X2}", (byte)slot);

        // INS 0xF7 (GET METADATA), P2 = slot
        var command = new ApduCommand(0x00, 0xF7, 0x00, (byte)slot, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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
        var tlvDict = DecodeMetadataFields(response.Data);

        byte algorithmId = RequiredField(tlvDict, 0x01, 1).Span[0];
        var policy = RequiredField(tlvDict, 0x02, 2);
        var pinPolicy = (PinPolicy)policy.Span[0];
        var touchPolicy = (TouchPolicy)policy.Span[1];
        var origin = RequiredField(tlvDict, 0x03, 1);
        var isGenerated = origin.Span[0] == 0x01;

        ReadOnlyMemory<byte>? publicKey = tlvDict.TryGetValue(0x04, out var pk) ? pk : null;

        return new SlotMetadata(
            KeyType: KeyTypeEncoding.Decode(algorithmId),
            PinPolicy: pinPolicy,
            TouchPolicy: touchPolicy,
            IsGenerated: isGenerated,
            PublicKey: publicKey ?? ReadOnlyMemory<byte>.Empty
        )
        { RawAlgorithmId = algorithmId, RawData = response.Data.ToArray() };
    }

    /// <summary>
    /// Gets metadata about the PIV PUK.
    /// </summary>
    internal static async Task<PukMetadata> GetPukMetadataAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting PUK metadata");

        // INS 0xF7 (GET METADATA), P2 = 0x81 (PUK slot)
        var command = new ApduCommand(0x00, 0xF7, 0x00, 0x81, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == 0x6D00)
        {
            throw new NotSupportedException("PUK metadata requires firmware 5.3.0 or later");
        }

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to get PUK metadata");
        }

        var metadata = ParsePinMetadata(response.Data);
        return new PukMetadata(metadata.IsDefault, metadata.TotalRetries, metadata.RetriesRemaining);
    }

    internal static async Task<ManagementKeyMetadata> GetManagementKeyMetadataAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting management key metadata");

        // INS 0xF7 (GET METADATA), P2 = 0x9B (SLOT_CARD_MANAGEMENT)
        const byte InsGetMetadata = 0xF7;
        const byte SlotCardManagement = 0x9B;

        var command = new ApduCommand(0x00, InsGetMetadata, 0x00, SlotCardManagement, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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
        var tlvDict = DecodeMetadataFields(response.Data);

        // Early metadata-capable firmware may omit the algorithm, meaning Triple-DES.
        // Present known fields must still have their exact protocol lengths.
        var keyType = tlvDict.ContainsKey(0x01)
            ? (PivManagementKeyType)RequiredField(tlvDict, 0x01, 1).Span[0]
            : PivManagementKeyType.TripleDes;

        var touchPolicy = (TouchPolicy)RequiredField(tlvDict, 0x02, 2).Span[1];
        var defaultValue = RequiredField(tlvDict, 0x05, 1).Span[0];
        if (defaultValue > 1)
            throw new ApduException("Invalid management-key default-state metadata.");
        var isDefault = defaultValue != 0;

        Logger.LogDebug("PIV: Management key metadata: type={KeyType}, isDefault={IsDefault}, touchPolicy={TouchPolicy}",
            keyType, isDefault, touchPolicy);

        return new ManagementKeyMetadata(
            KeyType: keyType,
            IsDefault: isDefault,
            TouchPolicy: touchPolicy
        );
    }

    internal static async Task<int> GetSerialNumberAsync(IBackend backend, CancellationToken cancellationToken)
    {

        Logger.LogDebug("PIV: Getting YubiKey serial number");

        var command = new ApduCommand(0x00, 0xF8, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

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
}