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

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        // Build command data: TAG 0x5C [ object ID bytes ]
        var idBytes = new List<byte>();
        idBytes.Add(0x5C);
        
        if (objectId <= 0xFF)
        {
            idBytes.Add(0x01);
            idBytes.Add((byte)objectId);
        }
        else if (objectId <= 0xFFFF)
        {
            idBytes.Add(0x02);
            idBytes.Add((byte)(objectId >> 8));
            idBytes.Add((byte)(objectId & 0xFF));
        }
        else
        {
            idBytes.Add(0x03);
            idBytes.Add((byte)(objectId >> 16));
            idBytes.Add((byte)((objectId >> 8) & 0xFF));
            idBytes.Add((byte)(objectId & 0xFF));
        }

        // INS 0xCB (GET DATA)
        var command = new ApduCommand(0x00, 0xCB, 0x3F, 0xFF, idBytes.ToArray());
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

        return response.Data;
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

        if (_protocol is null)
        {
            throw new InvalidOperationException("Session not initialized");
        }

        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication required to write data objects");
        }

        // Build command data: TAG 0x5C [ object ID ] + TAG 0x53 [ data ]
        var cmdData = new List<byte>();
        
        // TAG 0x5C (Object ID)
        cmdData.Add(0x5C);
        if (objectId <= 0xFF)
        {
            cmdData.Add(0x01);
            cmdData.Add((byte)objectId);
        }
        else if (objectId <= 0xFFFF)
        {
            cmdData.Add(0x02);
            cmdData.Add((byte)(objectId >> 8));
            cmdData.Add((byte)(objectId & 0xFF));
        }
        else
        {
            cmdData.Add(0x03);
            cmdData.Add((byte)(objectId >> 16));
            cmdData.Add((byte)((objectId >> 8) & 0xFF));
            cmdData.Add((byte)(objectId & 0xFF));
        }

        // TAG 0x53 (Data) - always required, even if empty for delete
        cmdData.Add(0x53);
        if (data.HasValue && !data.Value.IsEmpty)
        {
            var dataSpan = data.Value.Span;
            if (dataSpan.Length > 127)
            {
                cmdData.Add(0x82);
                cmdData.Add((byte)(dataSpan.Length >> 8));
                cmdData.Add((byte)(dataSpan.Length & 0xFF));
            }
            else
            {
                cmdData.Add((byte)dataSpan.Length);
            }
            cmdData.AddRange(dataSpan.ToArray());
        }
        else
        {
            // Empty data for delete operations
            cmdData.Add(0x00);
        }

        // INS 0xDB (PUT DATA)
        var command = new ApduCommand(0x00, 0xDB, 0x3F, 0xFF, cmdData.ToArray());
        var response = await _protocol.TransmitAndReceiveAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
        {
            throw ApduException.FromStatusWord(response.SW, 
                $"Failed to write data object 0x{objectId:X6}");
        }
    }
}
