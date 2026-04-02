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

using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Parsed cardholder related data (DO 0x65) containing name, language, and sex.
/// </summary>
public sealed class CardholderRelatedData
{
    /// <summary>
    ///     The cardholder name bytes (tag 0x5B).
    /// </summary>
    public ReadOnlyMemory<byte> Name { get; init; }

    /// <summary>
    ///     The language preference bytes (tag 0x5F2D), typically 2 bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Language { get; init; }

    /// <summary>
    ///     Sex of cardholder per ISO 5218 (tag 0x5F35).
    ///     0x30 = not known, 0x31 = male, 0x32 = female, 0x39 = not applicable.
    /// </summary>
    public byte Sex { get; init; }

    /// <summary>
    ///     Parses cardholder related data from the TLV-encoded value of DO 0x65.
    /// </summary>
    /// <param name="encoded">The inner value of the 0x65 TLV (without the outer tag/length).</param>
    public static CardholderRelatedData Parse(ReadOnlySpan<byte> encoded)
    {
        var data = TlvHelper.DecodeDictionary(encoded);

        return new CardholderRelatedData
        {
            Name = data.TryGetValue((int)DataObject.Name, out var name) ? name : ReadOnlyMemory<byte>.Empty,
            Language = data.TryGetValue((int)DataObject.Language, out var lang) ? lang : ReadOnlyMemory<byte>.Empty,
            Sex = data.TryGetValue((int)DataObject.Sex, out var sex) && sex.Length > 0 ? sex.Span[0] : (byte)0x30,
        };
    }
}