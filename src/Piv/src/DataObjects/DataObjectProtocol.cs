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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;

namespace Yubico.YubiKit.Piv.DataObjects;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

internal static class DataObjectProtocol
{
    /// <summary>
    /// Reads a PIV data object.
    /// </summary>
    /// <param name="objectId">The object ID (e.g., from <see cref="DataObject"/>).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The object data, or empty if the object doesn't exist.</returns>
    internal static async Task<byte[]> GetObjectAsync(
        IBackend backend,
        int objectId,
        CancellationToken cancellationToken = default)
    {
        // INS 0xCB (GET DATA)
        var command = new ApduCommand(0x00, 0xCB, 0x3F, 0xFF, EncodeObjectId(objectId));
        var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

        try
        {
            if (response.SW == SWConstants.FileOrApplicationNotFound)
                return [];
            if (!response.IsOK())
                throw ApduException.FromStatusWord(response.SW, $"Failed to read data object 0x{objectId:X6}");
            return UnwrapDataObjectResponse(response.Data.Span);
        }
        finally
        {
            response.Clear();
        }
    }

    /// <summary>
    /// Unwraps a GET DATA response that is wrapped in TAG 0x53.
    /// </summary>
    private static byte[] UnwrapDataObjectResponse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return [];
        if (data[0] != 0x53)
            return data.ToArray();
        using var wrapper = Tlv.Create(data);
        return wrapper.Value.ToArray();
    }

    /// <summary>
    /// Writes a PIV data object.
    /// </summary>
    /// <param name="objectId">The object ID.</param>
    /// <param name="data">The data to write, or null to delete the object.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    internal static async Task PutObjectAsync(
        IBackend backend,
        int objectId,
        ReadOnlyMemory<byte>? data,
        CancellationToken cancellationToken = default)
    {
        // Build command data: TAG 0x5C [ object ID ] + TAG 0x53 [ data ]
        var objectIdBytes = EncodeObjectId(objectId);
        int dataLen = data.HasValue && !data.Value.IsEmpty ? data.Value.Length : 0;
        int dataLenSize = BerLength.EncodingSize(dataLen);
        byte[] encoded = new byte[objectIdBytes.Length + 1 + dataLenSize + dataLen];
        try
        {
            objectIdBytes.CopyTo(encoded, 0);
            int offset = objectIdBytes.Length;
            encoded[offset++] = 0x53;
            offset += BerLength.Write(encoded.AsSpan(offset), dataLen);
            if (data is { } value)
                value.Span.CopyTo(encoded.AsSpan(offset));
            var command = new ApduCommand(0x00, 0xDB, 0x3F, 0xFF, encoded);
            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);

            if (!response.IsOK())
            {
                throw ApduException.FromStatusWord(response.SW,
                    $"Failed to write data object 0x{objectId:X6}");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encoded);
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