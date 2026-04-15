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

using System.Buffers;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Piv;

public sealed partial class PivSession
{
    /// <summary>
    /// Reads a PIV data object.
    /// </summary>
    /// <param name="objectId">The object ID (e.g., from <see cref="PivDataObject"/>).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The object data, or empty if the object doesn't exist.</returns>
    public async Task<ReadOnlyMemory<byte>> GetObjectAsync(
        int objectId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting data object 0x{ObjectId:X6}", objectId);
        EnsureProtocol();

        // INS 0xCB (GET DATA)
        var command = new ApduCommand(0x00, 0xCB, 0x3F, 0xFF, EncodeObjectId(objectId));
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        if (response.SW == 0x6A82) // File not found
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, 
                $"Failed to read data object 0x{objectId:X6}");
        }

        // Response is wrapped in TAG 0x53 (Discretionary data)
        // Format: 53 LL [data] - unwrap and return inner data
        return UnwrapDataObjectResponse(response.Data);
    }
    
    /// <summary>
    /// Unwraps a GET DATA response that is wrapped in TAG 0x53.
    /// </summary>
    private static ReadOnlyMemory<byte> UnwrapDataObjectResponse(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
            return data;

        var span = data.Span;

        // Check for TAG 0x53 (Discretionary data)
        if (span[0] != 0x53)
        {
            // Not wrapped, return as-is
            return data;
        }

        // Use Tlv to parse the wrapper
        var wrapper = Tlv.Create(span);
        return wrapper.Value;
    }

    /// <summary>
    /// Writes a PIV data object.
    /// </summary>
    /// <param name="objectId">The object ID.</param>
    /// <param name="data">The data to write, or null to delete the object.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task PutObjectAsync(
        int objectId,
        ReadOnlyMemory<byte>? data,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Putting data object 0x{ObjectId:X6}", objectId);
        EnsureProtocol();

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required to write data objects");
        }

        // Build command data: TAG 0x5C [ object ID ] + TAG 0x53 [ data ]
        var objectIdBytes = EncodeObjectId(objectId);
        int dataLen = data.HasValue && !data.Value.IsEmpty ? data.Value.Length : 0;
        int dataLenSize = dataLen > 0 ? BerLength.EncodingSize(dataLen) : 1; // 1 byte for 0x00 (empty)
        int estimatedSize = objectIdBytes.Length + 1 + dataLenSize + dataLen;
        var writer = new ArrayBufferWriter<byte>(estimatedSize);

        writer.Write(objectIdBytes);

        // TAG 0x53 (Data) - always required, even if empty for delete
        ReadOnlySpan<byte> dataTag = [0x53];
        writer.Write(dataTag);
        if (data.HasValue && !data.Value.IsEmpty)
        {
            var lenSpan = writer.GetSpan(dataLenSize);
            BerLength.Write(lenSpan, dataLen);
            writer.Advance(dataLenSize);
            writer.Write(data.Value.Span);
        }
        else
        {
            // Empty data for delete operations
            ReadOnlySpan<byte> emptyLen = [0x00];
            writer.Write(emptyLen);
        }

        // INS 0xDB (PUT DATA)
        var command = new ApduCommand(0x00, 0xDB, 0x3F, 0xFF, writer.WrittenMemory);
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW,
                $"Failed to write data object 0x{objectId:X6}");
        }
    }

    /// <summary>
    /// Encodes an object ID as a TLV with tag 0x5C.
    /// </summary>
    private static byte[] EncodeObjectId(int objectId)
    {
        if (objectId <= 0xFF)
        {
            return [0x5C, 0x01, (byte)objectId];
        }

        if (objectId <= 0xFFFF)
        {
            return [0x5C, 0x02, (byte)(objectId >> 8), (byte)(objectId & 0xFF)];
        }

        return [0x5C, 0x03, (byte)(objectId >> 16), (byte)((objectId >> 8) & 0xFF), (byte)(objectId & 0xFF)];
    }
}
