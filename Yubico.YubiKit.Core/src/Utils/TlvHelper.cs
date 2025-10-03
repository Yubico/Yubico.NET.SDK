// Copyright 2025 Yubico AB
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
using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Utils;

/// <summary>
///     Utility methods to encode and decode BER-TLV data.
/// </summary>
public static class TlvHelper
{
    /// <summary>
    ///     Decodes a sequence of BER-TLV encoded data into a list of Tlvs.
    /// </summary>
    /// <param name="tlvData">Sequence of TLV encoded data</param>
    /// <returns>List of <see cref="Tlv" /></returns>
    public static IEnumerable<Tlv> Decode(ReadOnlySpan<byte> tlvData)
    {
        var tlvs = new HashSet<Tlv>();
        var buffer = tlvData;
        while (!buffer.IsEmpty)
        {
            var tlv = Tlv.ParseFrom(ref buffer);
            tlvs.Add(tlv);
        }

        return tlvs;
    }

    /// <summary>
    ///     Decodes a sequence of BER-TLV encoded data into a mapping of Tag-Value pairs.
    ///     Iteration order is preserved. If the same tag occurs more than once only the latest will be kept.
    /// </summary>
    /// <param name="tlvData">Sequence of TLV encoded data</param>
    /// <returns>Dictionary of Tag-Value pairs</returns>
    public static IDictionary<int, ReadOnlyMemory<byte>> DecodeDictionary(ReadOnlySpan<byte> tlvData)
    {
        var tlvs = new Dictionary<int, ReadOnlyMemory<byte>>();
        var buffer = tlvData;
        while (!buffer.IsEmpty)
        {
            var tlv = Tlv.ParseFrom(ref buffer);
            tlvs[tlv.Tag] = tlv.Value;
        }

        return tlvs;
    }

    /// <summary>
    ///     Encodes a list of Tlvs into a sequence of BER-TLV encoded data.
    /// </summary>
    /// <param name="tlvData">List of Tlvs to encode</param>
    /// <returns>BER-TLV encoded list</returns>
    public static Memory<byte> EncodeList(ReadOnlySpan<Tlv> tlvData)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var tlv in tlvData)
        {
            ReadOnlyMemory<byte> bytes = tlv.GetBytes();
            writer.Write(bytes.Span.ToArray());
        }

        return stream.ToArray();
    }

    /// <summary>
    ///     Encodes an array of Tlvs into a sequence of BER-TLV encoded data.
    /// </summary>
    /// <param name="tlvs">Array of Tlvs to encode</param>
    /// <returns>BER-TLV encoded array</returns>
    public static Memory<byte> EncodeMany(params ReadOnlySpan<Tlv> tlvs) => EncodeList(tlvs);

    /// <summary>
    ///     Decode a single TLV encoded object, returning only the value.
    /// </summary>
    /// <param name="expectedTag">The expected tag value of the given TLV data</param>
    /// <param name="tlvData">The TLV data</param>
    /// <returns>The value of the TLV</returns>
    /// <exception cref="InvalidOperationException">If the TLV tag differs from expectedTag</exception>
    public static Memory<byte> GetValue(int expectedTag, ReadOnlySpan<byte> tlvData)
    {
        var tlv = Tlv.Parse(tlvData);
        return tlv.Tag != expectedTag
            ? throw new InvalidOperationException($"Expected tag: {expectedTag:X2}, got {tlv.Tag:X2}")
            : tlv.Value.ToArray();
    }

    public static Memory<byte> EncodeDictionary(IReadOnlyDictionary<int, byte[]?> tlvData)
    {
        if (tlvData.Count == 0)
        {
            return Array.Empty<byte>();
        }

        int estimatedSize = tlvData.Sum(kvp => 2 + (kvp.Value?.Length ?? 0));
        byte[] rented = ArrayPool<byte>.Shared.Rent(estimatedSize);

        try
        {
            int position = 0;
            Span<byte> buffer = rented.AsSpan();

            foreach (var (tag, value) in tlvData.OrderBy(kvp => kvp.Key))
            {
                var tlv = new Tlv(tag, value ?? []);
                var tlvBytes = tlv.GetBytes().Span;
                tlvBytes.CopyTo(buffer[position..]);
                position += tlvBytes.Length;
            }

            // Copy only the written portion
            byte[] result = new byte[position];
            buffer[..position].CopyTo(result);
            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rented);
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}