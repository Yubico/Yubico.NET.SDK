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

using System;
using System.Collections.Generic;

namespace Yubico.Core.Tlv
{
    /// <summary>
    /// A Tag-Length-Value that has sub-elements.
    /// </summary>
    /// <remarks>
    /// There are two types of Nested TLV. One, the sub-elements are presented as a
    /// concatenation:
    /// <code>
    ///   TLV || TLV || ... TLV
    ///   for example: 81 02 05 05 82 01 14 83 04 00 72 9A 1E
    ///              81 02 05 05   82 01 14   83 04 00 72 9A 1E
    /// </code>
    /// And two, a "tree" where the Nested TLV has a tag and the value is a
    /// collection of sub-elements:
    /// <code>
    ///   TL { TLV || TLV || ... TLV }
    ///   for example: 7C 0D 81 02 05 05 82 01 14 83 04 00 72 9A 1E
    ///     7C 0D
    ///        81 02
    ///           05 05
    ///        82 01
    ///           14
    ///        83 04
    ///           00 72 9A 1E
    /// </code>
    /// </remarks>
    internal class TlvNestedTlv : TlvEncoder
    {
        private int _encodedLength;

        /// <inheritdoc/>
        override public int EncodedLength => _encodedLength;

        private readonly List<TlvEncoder> _subElements = new List<TlvEncoder>();
        private int _subElementLength;
        private readonly int _tag;
        private byte[] _tagAndLength;

        /// <summary>
        /// Build a new NestedTlv that will organize as a concatenation.
        /// </summary>
        /// <remarks>
        /// The sub-elements added to this Nested TLV will be encoded as a
        /// concatenation:
        /// <code>
        ///   TLV || TLV || ... TLV
        ///   for example: 81 02 05 05 82 01 14 83 04 00 72 9A 1E
        ///              81 02 05 05   82 01 14   83 04 00 72 9A 1E
        /// </code>
        /// </remarks>
        public TlvNestedTlv()
        {
            _tagAndLength = Array.Empty<byte>();
            _encodedLength = 0;
        }

        /// <summary>
        /// Build a new NestedTlv that will organize as a tree with the given tag.
        /// </summary>
        /// <remarks>
        /// The sub-elements added to this Nested TLV will be encoded as a tree:
        /// <code>
        ///   var ykInfo = new TlvNestedTlv(0x7C);<br/>
        ///   TL { TLV || TLV || ... TLV }
        ///   for example: 7C 0D 81 02 05 05 82 01 14 83 04 00 72 9A 1E
        ///     7C 0D
        ///        81 02
        ///           05 05
        ///        82 01
        ///           14
        ///        83 04
        ///           00 72 9A 1E
        /// </code>
        /// </remarks>
        /// <exception cref="TlvException">
        /// The tag is unsupported.
        /// </exception>
        public TlvNestedTlv(int tag)
        {
            _tag = tag;
            _tagAndLength = BuildTagAndLength(tag, 0);
            _encodedLength = _tagAndLength.Length;
        }

        /// <summary>
        /// Add a new sub-element to this Nested TLV.
        /// </summary>
        /// <remarks>
        /// The subElement might be a TlvSubElement, it might be a TlvNestedTlv
        /// as well.
        /// </remarks>
        /// <param name="subElement">
        /// The sub-element to add.
        /// </param>
        /// <exception cref="TlvException">
        /// The tag or length is unsupported.
        /// </exception>
        public void AddSubElement(TlvEncoder subElement)
        {
            _subElements.Add(subElement);
            _subElementLength += subElement.EncodedLength;
            if (_tagAndLength.Length != 0)
            {
                _tagAndLength = BuildTagAndLength(_tag, _subElementLength);
            }
            _encodedLength = _tagAndLength.Length + _subElementLength;
        }

        /// <inheritdoc />
        override public bool TryEncode(Span<byte> encoding, int offset, out int bytesWritten)
        {
            bytesWritten = 0;
            if (encoding.Length < offset + _encodedLength)
            {
                return false;
            }

            if (_tagAndLength.Length != 0)
            {
                Span<byte> destination = encoding[offset..];
                var source = new Span<byte>(_tagAndLength);
                source.CopyTo(destination);
                offset += _tagAndLength.Length;
            }

            foreach (TlvEncoder element in _subElements)
            {
                if (element.TryEncode(encoding, offset, out int encodingLength) == false)
                {
                    return false;
                }
                offset += encodingLength;
            }

            bytesWritten = _encodedLength;
            return true;
        }

        /// <inheritdoc />
        override public void Clear()
        {
            foreach (TlvEncoder element in _subElements)
            {
                element.Clear();
            }
        }
    }
}
