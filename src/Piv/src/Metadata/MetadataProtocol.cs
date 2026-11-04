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
using System.Buffers.Binary;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;

namespace Yubico.YubiKit.Piv.Metadata;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

internal static class MetadataProtocol
{
    private const byte Cla = 0x00;
    private const byte UnusedParameter = 0x00;
    private const byte InsGetMetadata = 0xF7;
    private const byte InsGetSerial = 0xF8;
    private const byte TagAlgorithm = 0x01;
    private const byte TagPolicy = 0x02;
    private const byte TagOrigin = 0x03;
    private const byte OriginGenerated = 0x01;
    private const byte TagPublicKey = 0x04;
    private const byte TagIsDefault = 0x05;
    private const byte TagRetries = 0x06;

    private static ILogger Logger => YubiKitLogging.CreateLogger("Yubico.YubiKit.Piv.Metadata.MetadataProtocol");

    internal static async Task<PinMetadata> GetPinMetadataAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting PIN metadata");

        var command = new ApduCommand(Cla, InsGetMetadata, UnusedParameter, KeyReference.Pin, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == SWConstants.InsNotSupported)
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
        var defaultValue = RequiredField(fields, TagIsDefault, 1);
        var retries = RequiredField(fields, TagRetries, 2);
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

        var command = new ApduCommand(Cla, InsGetMetadata, UnusedParameter, (byte)slot, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == SWConstants.InsNotSupported)
        {
            throw new NotSupportedException("Slot metadata requires firmware 5.3.0 or later");
        }

        // Check for empty slot - 0x6A82 "File not found" or 0x6A88 "Referenced data not found"
        if (response.SW is SWConstants.FileOrApplicationNotFound or SWConstants.DataNotFound)
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

        byte algorithmId = RequiredField(tlvDict, TagAlgorithm, 1).Span[0];
        var policy = RequiredField(tlvDict, TagPolicy, 2);
        var pinPolicy = (PinPolicy)policy.Span[0];
        var touchPolicy = (TouchPolicy)policy.Span[1];
        var origin = RequiredField(tlvDict, TagOrigin, 1);
        var isGenerated = origin.Span[0] == OriginGenerated;

        ReadOnlyMemory<byte>? publicKey = tlvDict.TryGetValue(TagPublicKey, out var pk) ? pk : null;

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

        var command = new ApduCommand(Cla, InsGetMetadata, UnusedParameter, KeyReference.Puk, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == SWConstants.InsNotSupported)
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




        var command = new ApduCommand(Cla, InsGetMetadata, UnusedParameter, KeyReference.Management, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // Check for "instruction not supported" which indicates firmware < 5.3
        if (response.SW == SWConstants.InsNotSupported)
        {
            throw new NotSupportedException("Management key metadata requires firmware 5.3.0 or later");
        }

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, "Failed to get management key metadata");
        }

        // Parse metadata TLV using TlvHelper
        var tlvDict = DecodeMetadataFields(response.Data);

        // Early metadata-capable firmware may omit the algorithm, meaning Triple-DES.
        // Present known fields must still have their exact protocol lengths.
        byte algorithmId = tlvDict.ContainsKey(TagAlgorithm)
            ? RequiredField(tlvDict, TagAlgorithm, 1).Span[0]
            : KeyTypeEncoding.EncodeManagement(KeyType.TripleDes);
        var keyType = KeyTypeEncoding.Decode(algorithmId);

        var touchPolicy = (TouchPolicy)RequiredField(tlvDict, TagPolicy, 2).Span[1];
        var defaultValue = RequiredField(tlvDict, TagIsDefault, 1).Span[0];
        if (defaultValue > 1)
            throw new ApduException("Invalid management-key default-state metadata.");
        var isDefault = defaultValue != 0;

        Logger.LogDebug("PIV: Management key metadata: type={KeyType}, isDefault={IsDefault}, touchPolicy={TouchPolicy}",
            keyType, isDefault, touchPolicy);

        return new ManagementKeyMetadata(
            KeyType: keyType,
            IsDefault: isDefault,
            TouchPolicy: touchPolicy
        )
        { RawAlgorithmId = algorithmId, RawData = response.Data.ToArray() };
    }

    internal static async Task<int> GetSerialNumberAsync(IBackend backend, CancellationToken cancellationToken)
    {

        Logger.LogDebug("PIV: Getting YubiKey serial number");

        var command = new ApduCommand(Cla, InsGetSerial, UnusedParameter, UnusedParameter, ReadOnlyMemory<byte>.Empty);
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        // 0x6D00 means INS not supported (firmware < 5.0.0)
        if (response.SW == SWConstants.InsNotSupported)
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
        var serial = BinaryPrimitives.ReadInt32BigEndian(serialBytes);

        Logger.LogDebug("PIV: Retrieved serial number: {Serial}", serial);
        return serial;
    }
}