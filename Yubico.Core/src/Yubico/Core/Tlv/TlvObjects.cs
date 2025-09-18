using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Yubico.Core.Tlv;

/// <summary>
///     Utility methods to encode and decode BER-TLV data.
/// </summary>
public static class TlvObjects
{
    /// <summary>
    ///     Decodes a sequence of BER-TLV encoded data into a list of Tlvs.
    /// </summary>
    /// <param name="data">Sequence of TLV encoded data</param>
    /// <returns>List of <see cref="TlvObject" /></returns>
    public static IReadOnlyList<TlvObject> DecodeList(ReadOnlySpan<byte> data)
    {
        var tlvs = new List<TlvObject>();
        ReadOnlySpan<byte> buffer = data;
        while (!buffer.IsEmpty)
        {
            var tlv = TlvObject.ParseFrom(ref buffer);
            tlvs.Add(tlv);
        }

        return tlvs.AsReadOnly();
    }

    /// <summary>
    ///     Decodes a sequence of BER-TLV encoded data into a mapping of Tag-Value pairs.
    ///     Iteration order is preserved. If the same tag occurs more than once only the latest will be kept.
    /// </summary>
    /// <param name="data">Sequence of TLV encoded data</param>
    /// <returns>Dictionary of Tag-Value pairs</returns>
    public static IReadOnlyDictionary<int, ReadOnlyMemory<byte>> DecodeDictionary(ReadOnlySpan<byte> data)
    {
        var tlvs = new Dictionary<int, ReadOnlyMemory<byte>>();
        ReadOnlySpan<byte> buffer = data;
        while (!buffer.IsEmpty)
        {
            var tlv = TlvObject.ParseFrom(ref buffer);
            tlvs[tlv.Tag] = tlv.Value;
        }

        return tlvs;
    }

    /// <summary>
    ///     Encodes a list of Tlvs into a sequence of BER-TLV encoded data.
    /// </summary>
    /// <param name="list">List of Tlvs to encode</param>
    /// <returns>BER-TLV encoded list</returns>
    public static byte[] EncodeList(IEnumerable<TlvObject> list)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (TlvObject? tlv in list)
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
    public static byte[] EncodeMany(params TlvObject[] tlvs) => EncodeList(tlvs);

    /// <summary>
    ///     Decode a single TLV encoded object, returning only the value.
    /// </summary>
    /// <param name="expectedTag">The expected tag value of the given TLV data</param>
    /// <param name="tlvData">The TLV data</param>
    /// <returns>The value of the TLV</returns>
    /// <exception cref="InvalidOperationException">If the TLV tag differs from expectedTag</exception>
    public static Memory<byte> UnpackValue(int expectedTag, ReadOnlySpan<byte> tlvData)
    {
        var tlv = TlvObject.Parse(tlvData);
        if (tlv.Tag != expectedTag)
        {
            throw new InvalidOperationException($"Expected tag: {expectedTag:X2}, got {tlv.Tag:X2}");
        }

        return tlv.Value.ToArray();
    }

    public static Memory<byte> EncodeDictionary(IReadOnlyDictionary<int, byte[]> map)
    {
        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        int totalSize = 0;
        foreach (KeyValuePair<int, byte[]> entry in map)
        {
            var tlv = new TlvObject(entry.Key, entry.Value ?? Array.Empty<byte>());
            ReadOnlyMemory<byte> bytes = tlv.GetBytes();
            totalSize += bytes.Length;
        }

        byte[] result = new byte[totalSize];
        int position = 0;

        try
        {
            foreach (KeyValuePair<int, byte[]> entry in map)
            {
                var tlv = new TlvObject(entry.Key, entry.Value ?? Array.Empty<byte>());
                byte[] tlvBytes = tlv.GetBytes().ToArray();

                try
                {
                    Buffer.BlockCopy(tlvBytes, 0, result, position, tlvBytes.Length);
                    position += tlvBytes.Length;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(tlvBytes);
                }
            }

            return result.AsMemory(0, position);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(result);
            throw;
        }
    }
}
