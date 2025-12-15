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
    ///     Decodes a sequence of BER-TLV encoded data into a disposable collection of Tlvs.
    /// </summary>
    /// <param name="tlvData">Sequence of TLV encoded data</param>
    /// <returns>A disposable collection of <see cref="Tlv" /> objects that must be disposed to securely clear sensitive data.</returns>
    /// <remarks>
    ///     The returned collection must be disposed using a <c>using</c> declaration to ensure
    ///     all TLV objects are properly disposed and their sensitive data is securely zeroed.
    /// </remarks>
    public static DisposableTlvList Decode(ReadOnlySpan<byte> tlvData)
    {
        var tlvs = new List<Tlv>();
        var buffer = tlvData;
        while (!buffer.IsEmpty)
        {
            var tlv = Tlv.Create(buffer);
            tlvs.Add(tlv);
        }

        return new DisposableTlvList(tlvs);
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
            var tlv = Tlv.ParseData(ref buffer);
            tlvs[tlv.Tag] = tlv.Value;
        }

        return tlvs;
    }

    // /// <summary>
    // ///     Decodes a sequence of BER-TLV encoded data into a mapping of Tag-Value pairs.
    // ///     Iteration order is preserved. If the same tag occurs more than once only the latest will be kept.
    // /// </summary>
    // /// <param name="tlvData">Sequence of TLV encoded data</param>
    // /// <returns>Dictionary of Tag-Value pairs</returns>
    // public static DisposableTlvDictionary DecodeDictionary2(ReadOnlySpan<byte> tlvData)
    // {
    //     var tlvs = new DisposableTlvDictionary();
    //     var buffer = tlvData;
    //     while (!buffer.IsEmpty)
    //     {
    //         var tlv = Tlv.ParseData(ref buffer);
    //         tlvs.Add(tlv.Tag, tlv.Value);
    //     }
    //
    //     return tlvs;
    // }

    /// <summary>
    ///     Encodes a list of Tlvs into a sequence of BER-TLV encoded data.
    /// </summary>
    /// <param name="tlvData">List of Tlvs to encode</param>
    /// <returns>BER-TLV encoded list</returns>
    public static Memory<byte> EncodeList(ReadOnlySpan<Tlv> tlvData)
    {
        using var stream = new MemoryStream(); // todo rewrite, allocs
        using var writer = new BinaryWriter(stream);
        foreach (var tlv in tlvData)
        {
            ReadOnlyMemory<byte> bytes = tlv.AsMemory();
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
        using var tlv = Tlv.Create(tlvData);
        if (tlv.Tag != expectedTag)
            throw new InvalidOperationException($"Expected tag: {expectedTag:X2}, got {tlv.Tag:X2}");
        return tlv.Value.ToArray();
    }

    public static Memory<byte> EncodeDictionary(IReadOnlyDictionary<int, byte[]?> tlvData)
    {
        if (tlvData.Count == 0) return Array.Empty<byte>();

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