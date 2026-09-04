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
    ///     A caller-owned buffer containing the BER-TLV encoded list, or an empty buffer if
    ///     <paramref name="tlvData" /> contains no elements. The caller must securely clear the returned
    ///     buffer when it contains sensitive data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="tlvData" /> is <see langword="null" />, or contains a <see langword="null" /> element.
    /// </exception>
    /// <remarks>
    ///     This method does not dispose the input <see cref="Tlv" /> objects. The caller remains responsible
    ///     for disposing them. Source buffers used to construct the TLVs also remain caller-owned and are not
    ///     cleared by this method.
    /// </remarks>
    public static Memory<byte> EncodeList(Tlv[] tlvData)
    {
        ArgumentNullException.ThrowIfNull(tlvData);

        var tlvSpan = tlvData.AsSpan();

        // TotalLength is the exact encoded span length. Direct assembly avoids abandoning a second
        // complete copy of potentially sensitive encoded data on the managed heap.
        var totalLength = 0;
        foreach (var tlv in tlvSpan)
        {
            ArgumentNullException.ThrowIfNull(tlv, nameof(tlvData));
            totalLength += tlv.TotalLength;
        }

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
    /// <param name="tlvData">TLVs to encode and dispose. Each TLV is disposed even if encoding fails.</param>
    /// <returns>
    ///     A caller-owned buffer containing the BER-TLV encoded list, or an empty buffer if
    ///     <paramref name="tlvData" /> contains no elements. The caller must securely clear the returned
    ///     buffer when it contains sensitive data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="tlvData" /> is <see langword="null" />, or contains a <see langword="null" /> element.
    /// </exception>
    /// <remarks>
    ///     Source buffers used to construct the TLVs remain caller-owned and are not cleared by this method.
    ///     Disposing the input TLVs does not clear the returned buffer.
    /// </remarks>
    public static Memory<byte> EncodeAndDisposeList(params Tlv[] tlvData)
    {
        ArgumentNullException.ThrowIfNull(tlvData);

        try
        {
            return EncodeList(tlvData);
        }
        finally
        {
            foreach (var tlv in tlvData)
            {
                tlv?.Dispose();
            }
        }
    }

    /// <summary>
    ///     Decode a single TLV encoded object, returning only the value.
    /// </summary>
    /// <param name="expectedTag">The expected tag value of the given TLV data</param>
    /// <param name="tlvData">The TLV data</param>
    /// <returns>
    ///     A caller-owned buffer containing the TLV value. The caller must securely clear the returned
    ///     buffer when it contains sensitive data.
    /// </returns>
    /// <exception cref="InvalidOperationException">If the TLV tag differs from expectedTag</exception>
    /// <remarks>
    ///     The returned buffer is a copy and remains valid after the internal <see cref="Tlv" /> is disposed.
    /// </remarks>
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
    /// <remarks>
    ///     When this method returns <see langword="true" />, <paramref name="value" /> is a caller-owned buffer.
    ///     The caller must securely clear it when it contains sensitive data.
    /// </remarks>
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

    /// <summary>
    ///     Encodes a mapping of tag-value pairs into BER-TLV data ordered by ascending tag.
    /// </summary>
    /// <param name="tlvData">Dictionary of TLV tag-value pairs.</param>
    /// <returns>
    ///     A caller-owned buffer containing the BER-TLV encoded dictionary, or an empty buffer if
    ///     <paramref name="tlvData" /> contains no elements. The caller must securely clear the returned
    ///     buffer when it contains sensitive data.
    /// </returns>
    /// <remarks>
    ///     Source buffers used as dictionary values remain caller-owned and are not cleared by this method.
    /// </remarks>
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