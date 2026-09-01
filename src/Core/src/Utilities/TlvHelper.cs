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

namespace Yubico.YubiKit.Core.Utilities;

/// <summary>
///     Utility methods to encode and decode BER-TLV data.
/// </summary>
public static class TlvHelper
{
    /// <summary>
    ///     Decodes a sequence of BER-TLV encoded data into a disposable collection of Tlvs.
    /// </summary>
    /// <param name="tlvData">Sequence of TLV encoded data</param>
    /// <returns>A disposable collection of <see cref="Tlv" /> objects that must be disposed to securely clear sensitive data.</returns>
    /// <remarks>
    ///     The returned collection must be disposed using a <c>using</c> declaration to ensure
    ///     all TLV objects are properly disposed and their sensitive data is securely zeroed.
    /// </remarks>
    public static DisposableTlvList DecodeList(ReadOnlySpan<byte> tlvData)
    {
        var tlvs = new List<Tlv>();
        var buffer = tlvData;
        while (!buffer.IsEmpty)
        {
            // Parse and advance the buffer to avoid infinite loop
            var (tag, _, value) = Tlv.ParseData(ref buffer);
            tlvs.Add(new Tlv(tag, value));
        }

        return new DisposableTlvList(tlvs);
    }

    public static DisposableTlvList DecodeList(ReadOnlyMemory<byte> tlvData) => DecodeList(tlvData.Span);

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
            var tlv = Tlv.ParseData(ref buffer);
            tlvs[tlv.Tag] = tlv.Value;
        }

        return tlvs;
    }

    public static IDictionary<int, ReadOnlyMemory<byte>> DecodeDictionary(ReadOnlyMemory<byte> tlvData) =>
        DecodeDictionary(tlvData.Span);

    /// <summary>
    ///     Encodes a list of Tlvs into a sequence of BER-TLV encoded data.
    /// </summary>
    /// <param name="tlvData">List of Tlvs to encode</param>
    /// <returns>
    ///     BER-TLV encoded list, or an empty buffer if <paramref name="tlvData" /> contains no elements.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="tlvData" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     A disposed <see cref="Tlv" /> still reports its original <see cref="Tlv.TotalLength" /> while
    ///     exposing zeroed bytes, so passing one encodes a correctly sized run of <c>0x00</c> rather than
    ///     throwing. Encode before disposing, or use <see cref="EncodeAndDisposeList" />.
    /// </remarks>
    public static Memory<byte> EncodeList(Tlv[] tlvData)
    {
        ArgumentNullException.ThrowIfNull(tlvData);

        var tlvSpan = tlvData.AsSpan();

        // Exact, not an estimate: Tlv.TotalLength is the length of the buffer Tlv.AsSpan() exposes,
        // so the sum is the precise output size and no growth step is possible. The header
        // arithmetic behind it is pinned by TlvHelperTests.Tlv_TotalLength_IsHeaderPlusValue.
        var totalLength = 0;
        foreach (var tlv in tlvSpan)
            totalLength += tlv.TotalLength;

        // Assemble directly into the array that is returned. Staging through a separate writer
        // would leave a second, complete copy of every encoded value - including PINs, keys and
        // other secret material - on the heap, abandoned to the GC without being zeroed.
        var encoded = new byte[totalLength];
        var position = 0;
        foreach (var tlv in tlvSpan)
        {
            var tlvBytes = tlv.AsSpan();
            tlvBytes.CopyTo(encoded.AsSpan(position));
            position += tlvBytes.Length;
        }

        return encoded;
    }

    /// <summary>
    ///     Encodes a collection of TLV objects into a single byte sequence, then disposes each TLV.
    ///     Use this when the TLV objects are created inline and not needed after encoding.
    /// </summary>
    public static Memory<byte> EncodeAndDisposeList(params Tlv[] tlvData)
    {
        try
        {
            return EncodeList(tlvData);
        }
        finally
        {
            foreach (var tlv in tlvData)
            {
                tlv.Dispose();
            }
        }
    }

    // /// <summary>
    // ///     Encodes an array of Tlvs into a sequence of BER-TLV encoded data.
    // /// </summary>
    // /// <param name="tlvs">Array of Tlvs to encode</param>
    // /// <returns>BER-TLV encoded array</returns>
    // public static Memory<byte> EncodeMany(params Tlv[] tlvs) => EncodeList(tlvs);

    /// <summary>
    ///     Decode a single TLV encoded object, returning only the value.
    /// </summary>
    /// <param name="expectedTag">The expected tag value of the given TLV data</param>
    /// <param name="tlvData">The TLV data</param>
    /// <returns>The value of the TLV</returns>
    /// <exception cref="InvalidOperationException">If the TLV tag differs from expectedTag</exception>
    public static Memory<byte> GetValue(int expectedTag, ReadOnlySpan<byte> tlvData)
    {
        using var tlv = Tlv.Create(tlvData);
        if (tlv.Tag != expectedTag)
            throw new InvalidOperationException($"Expected tag: {expectedTag:X2}, got {tlv.Tag:X2}");
        return tlv.Value.ToArray();
    }

    /// <summary>
    ///     Searches a sequence of TLV encoded data for a specific tag and returns its value.
    /// </summary>
    /// <param name="tag">The tag to search for</param>
    /// <param name="tlvData">Sequence of TLV encoded data</param>
    /// <param name="value">The value of the first TLV with the matching tag, or default if not found</param>
    /// <returns>True if the tag was found, false otherwise</returns>
    public static bool TryFindValue(int tag, ReadOnlySpan<byte> tlvData, out Memory<byte> value)
    {
        var buffer = tlvData;
        while (!buffer.IsEmpty)
        {
            var (currentTag, _, currentValue) = Tlv.ParseData(ref buffer);
            if (currentTag == tag)
            {
                value = currentValue.ToArray();
                return true;
            }
        }

        value = default;
        return false;
    }

    public static Memory<byte> EncodeDictionary(IReadOnlyDictionary<int, byte[]?> tlvData)
    {
        if (tlvData.Count == 0) return Memory<byte>.Empty;

        var estimatedSize = tlvData.Sum(kvp => 2 + (kvp.Value?.Length ?? 0));
        var rented = ArrayPool<byte>.Shared.Rent(estimatedSize);

        try
        {
            var position = 0;
            var buffer = rented.AsSpan();

            foreach (var (tag, value) in tlvData.OrderBy(kvp => kvp.Key))
            {
                using var tlv = new Tlv(tag, value ?? []);
                var tlvBytes = tlv.AsMemory().Span;
                tlvBytes.CopyTo(buffer[position..]);
                position += tlvBytes.Length;
            }

            // Copy only the written portion
            var result = new byte[position];
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