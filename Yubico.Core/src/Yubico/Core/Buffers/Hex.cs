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
    /// <inheritdoc cref="Base16" path="/summary"/>
    /// <remarks>
    /// This class is an alias for <see cref="Base16"/>. New code should use that class
    /// and existing code should be changed over time. Putting the <see cref="ObsoleteAttribute"/>
    /// on this class is impractical.
    /// </remarks>
    public class Hex : Base16
    {
        /// <inheritdoc cref="Base16.EncodeBytes(ReadOnlySpan{byte})"/>
        public static string BytesToHex(ReadOnlySpan<byte> bytes) =>
            EncodeBytes(bytes);

        /// <inheritdoc cref="Base16.DecodeText(string)"/>
        public static byte[] HexToBytes(string encoded) =>
            DecodeText(encoded);
    }
}
