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

namespace Yubico.YubiKit.Core.Utils;

/// <summary>
///     Class for encoding and decoding bytes into BCD (binary coded decimal) format.
/// </summary>
/// <remarks>
///     BCD is a way to represent decimal data in a format that is both easy to parse
///     and easily human-readable. Each four bits is one decimal digit. The obvious
///     disadvantage is that the data is far less dense. Each byte can only contain
///     less than seven full bits of data.
/// </remarks>
public class Bcd : Base16
{
    // Base16 does things we don't need, like checking case, and Bcd needs
    // things that Base16 doesn't, like checking for digits out of range.
    // It seems worth the slight mismatch to not have to duplicate a bunch
    // of code.
    private readonly Memory<char> _characterSet = "0123456789".ToCharArray();

    /// <inheritdoc />
    protected override Span<char> CharacterSet => _characterSet.Span;

    #region Static Version

    /// <inheritdoc />
    public static new void EncodeBytes(ReadOnlySpan<byte> data, Span<char> encoded) => new Bcd().Encode(data, encoded);

    /// <inheritdoc />
    public static new string EncodeBytes(ReadOnlySpan<byte> data) => new Bcd().Encode(data);

    /// <inheritdoc />
    public static new void DecodeText(ReadOnlySpan<char> encoded, Span<byte> data) => new Bcd().Decode(encoded, data);

    /// <inheritdoc />
    public static new byte[] DecodeText(string encoded) => new Bcd().Decode(encoded);

    #endregion
}