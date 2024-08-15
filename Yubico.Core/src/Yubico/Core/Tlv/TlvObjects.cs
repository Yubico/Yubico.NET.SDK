using System;
using System.Collections.Generic;
using System.IO;
using Yubico.Core.Tlv;

namespace Yubico.YubiKit.Core.Util
{
    /// <summary>
    /// Utility methods to encode and decode BER-TLV data.
    /// </summary>
    public static class TlvObjects
    {
        /// <summary>
        /// Decodes a sequence of BER-TLV encoded data into a list of Tlvs.
        /// </summary>
        /// <param name="data">Sequence of TLV encoded data</param>
        /// <returns>List of Tlvs</returns>
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
        /// Decodes a sequence of BER-TLV encoded data into a mapping of Tag-Value pairs.
        /// Iteration order is preserved. If the same tag occurs more than once only the latest will be kept.
        /// </summary>
        /// <param name="data">Sequence of TLV encoded data</param>
        /// <returns>Dictionary of Tag-Value pairs</returns>
        public static IReadOnlyDictionary<int, ReadOnlyMemory<byte>> DecodeMap(ReadOnlySpan<byte> data)
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
        
        public static byte[] EncodeList(IEnumerable<TlvObject> list)
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            
            using var stream = new MemoryStream();
            foreach (TlvObject? tlv in list)
            {
                ReadOnlyMemory<byte> bytes = tlv.GetBytes();
#if NETSTANDARD2_1_OR_GREATER
                stream.Write(bytes.Span);
#else
                stream.Write(bytes.Span.ToArray(), 0, bytes.Length);
#endif
            }
            return stream.ToArray();
        }

        public static byte[] EncodeMany(params TlvObject[] tlvs) => EncodeList(tlvs);


        //Todo keep?
        public static byte[] EncodeMap(IReadOnlyDictionary<int, ReadOnlyMemory<byte>> map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }
            
            using var stream = new MemoryStream();
            foreach (KeyValuePair<int, ReadOnlyMemory<byte>> entry in map)
            {
                var tlv = new TlvObject(entry.Key, entry.Value.ToArray());
                ReadOnlyMemory<byte> bytes = tlv.GetBytes();
                stream.Write(bytes.ToArray(), 0,bytes.Length);;
            }
            return stream.ToArray();
        }

        /// <summary>
        /// Decode a single TLV encoded object, returning only the value.
        /// </summary>
        /// <param name="expectedTag">The expected tag value of the given TLV data</param>
        /// <param name="tlvData">The TLV data</param>
        /// <returns>The value of the TLV</returns>
        /// <exception cref="InvalidOperationException">If the TLV tag differs from expectedTag</exception>
        public static ReadOnlyMemory<byte> UnpackValue(int expectedTag, ReadOnlySpan<byte> tlvData)
        {
            var tlv = TlvObject.Parse(tlvData);
            if (tlv.Tag != expectedTag)
            {
                throw new InvalidOperationException($"Expected tag: {expectedTag:X2}, got {tlv.Tag:X2}");
            }
            return tlv.Value;
        }
    }
}
