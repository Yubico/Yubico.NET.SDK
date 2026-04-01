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

using System.Buffers.Binary;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Parsed Security Support Template (DO 0x7A) containing the digital signature counter.
/// </summary>
public sealed class SecuritySupportTemplate
{
    /// <summary>
    ///     The number of signatures performed with the Signature key since last reset.
    /// </summary>
    public int SignatureCounter { get; init; }

    /// <summary>
    ///     Parses the Security Support Template from the inner value of DO 0x7A.
    /// </summary>
    /// <param name="encoded">The inner TLV data of the 0x7A template.</param>
    public static SecuritySupportTemplate Parse(ReadOnlySpan<byte> encoded)
    {
        var data = TlvHelper.DecodeDictionary(encoded);

        var counter = 0;
        if (data.TryGetValue((int)DataObject.SignatureCounter, out var counterBytes))
        {
            var span = counterBytes.Span;
            // Counter is encoded as a 3-byte big-endian integer
            counter = span.Length switch
            {
                3 => (span[0] << 16) | (span[1] << 8) | span[2],
                2 => BinaryPrimitives.ReadUInt16BigEndian(span),
                1 => span[0],
                _ when span.Length >= 4 => (int)BinaryPrimitives.ReadUInt32BigEndian(span),
                _ => 0,
            };
        }

        return new SecuritySupportTemplate { SignatureCounter = counter };
    }
}
