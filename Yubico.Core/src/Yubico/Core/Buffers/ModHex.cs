// Copyright 2021 Yubico AB
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

namespace Yubico.Core.Buffers
{
    /// <summary>
    ///     Class for encoding and decoding byte collections into MODHEX.
    /// </summary>
    public class ModHex : Base16
    {
        private readonly Memory<char> _characterSet = "cbdefghijklnrtuv".ToCharArray();

        /// <inheritdoc />
        protected override Span<char> CharacterSet => _characterSet.Span;

        /// <inheritdoc />
        protected override bool DefaultLowerCase => true;

        #region Static Version

        /// <inheritdoc />
        public static new void EncodeBytes(ReadOnlySpan<byte> data, Span<char> encoded) =>
            new ModHex().Encode(data, encoded);

        /// <inheritdoc />
        public static new string EncodeBytes(ReadOnlySpan<byte> data) => new ModHex().Encode(data);

        /// <inheritdoc />
        public static new void DecodeText(ReadOnlySpan<char> encoded, Span<byte> data) =>
            new ModHex().Decode(encoded, data);

        /// <inheritdoc />
        public static new byte[] DecodeText(string encoded) => new ModHex().Decode(encoded);

        #endregion
    }
}
