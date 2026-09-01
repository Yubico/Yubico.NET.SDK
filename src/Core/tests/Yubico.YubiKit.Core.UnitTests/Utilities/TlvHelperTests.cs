// Copyright (C) 2025 Yubico.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Utilities;

public class TlvHelperTests
{
    /// <summary>
    ///     Helper that invokes <see cref="Tlv.ParseData(ref ReadOnlySpan{byte})"/> on the
    ///     provided TLV-encoded bytes to force parsing and surface any thrown exceptions.
    /// </summary>
    /// <param name="data">A byte array containing a single BER‑TLV element.</param>
    /// <remarks>
    ///     This exists only to be used inside <c>Assert.Throws*</c> expressions in unit tests.
    ///     It avoids capturing a byref-like <see cref="ReadOnlySpan{T}"/> inside a lambda, which
    ///     is disallowed by the C# compiler. The method mirrors the production parser’s behavior
    ///     by creating a span over <paramref name="data"/>, calling <see cref="Tlv.ParseData"/>,
    ///     and discarding the result.
    /// </remarks>
    private static void ParseDataOrThrow(byte[] data)
    {
        ReadOnlySpan<byte> buffer = data;
        _ = Tlv.ParseData(ref buffer);
    }
    [Fact]
    public void Decode_WithTwoConcatenatedTlvs_ReturnsBoth()
    {
        // Arrange: 0x5A 01 AA | 0x9F33 01 FF
        var input = new byte[] { 0x5A, 0x01, 0xAA, 0x9F, 0x33, 0x01, 0xFF };

        // Act
        using var list = TlvHelper.DecodeList(input);

        // Assert
        Assert.Equal(2, list.Count);
        Assert.Equal(0x5A, list[0].Tag);
        Assert.True(list[0].Value.Span.SequenceEqual(new byte[] { 0xAA }));
        Assert.Equal(0x9F33, list[1].Tag);
        Assert.True(list[1].Value.Span.SequenceEqual(new byte[] { 0xFF }));
    }

    [Fact]
    public void DecodeDictionary_LastWins_OnDuplicateTags()
    {
        // Arrange: 5A 01 01, 5A 01 02, 9F33 01 FF
        var input = new byte[] { 0x5A, 0x01, 0x01, 0x5A, 0x01, 0x02, 0x9F, 0x33, 0x01, 0xFF };

        // Act
        var dict = TlvHelper.DecodeDictionary(input);

        // Assert
        Assert.Equal(2, dict.Count);
        Assert.True(dict[0x5A].Span.SequenceEqual(new byte[] { 0x02 }));
        Assert.True(dict[0x9F33].Span.SequenceEqual(new byte[] { 0xFF }));
    }

    [Fact]
    public void EncodeDictionary_OrdersByAscendingTag()
    {
        // Arrange (unordered input)
        var map = new Dictionary<int, byte[]?>
        {
            { 0x9F33, [0xFF] },
            { 0x5A, [0xAA] },
        };

        // Act
        var encoded = TlvHelper.EncodeDictionary(map).Span;

        // Assert: Expect 0x5A element first
        Assert.Equal(0x5A, encoded[0]);
        Assert.Equal(0x01, encoded[1]);
        Assert.Equal(0xAA, encoded[2]);
        Assert.Equal(0x9F, encoded[3]);
        Assert.Equal(0x33, encoded[4]);
        Assert.Equal(0x01, encoded[5]);
        Assert.Equal(0xFF, encoded[6]);
    }

    [Fact]
    public void ParseData_WithIndefiniteLength0x80_Throws()
    {
        // Arrange: Tag 0x5A, length 0x80 (indefinite - unsupported), no content
        var input = new byte[] { 0x5A, 0x80 };
        // Act + Assert: slicing should fail due to insufficient data
        Assert.ThrowsAny<Exception>(() => ParseDataOrThrow(input));
    }

    [Fact]
    public void ParseData_WithOversizedLength_Throws()
    {
        // Arrange: Tag 0x5A, length 0x82 0x01 0x00 (256) but only 1 byte of value present
        var input = new byte[] { 0x5A, 0x82, 0x01, 0x00, 0x00 };
        // Act + Assert
        Assert.ThrowsAny<Exception>(() => ParseDataOrThrow(input));
    }

    [Fact]
    public void Create_WithNonMinimalLongFormLength_ParsesSuccessfully()
    {
        // Arrange: Tag 0x5A, length 0x81 0x7F (127, but should be short-form)
        var value = new byte[127];
        for (var i = 0; i < value.Length; i++) value[i] = (byte)i;
        var input = new byte[3 + value.Length];
        input[0] = 0x5A;
        input[1] = 0x81; // long-form 1 byte follows
        input[2] = 0x7F; // 127
        value.AsSpan().CopyTo(input.AsSpan(3));

        // Act
        using var tlv = Tlv.Create(input);

        // Assert
        Assert.Equal(0x5A, tlv.Tag);
        Assert.Equal(127, tlv.Length);
        Assert.True(tlv.Value.Span.SequenceEqual(value));
    }

    [Fact]
    public void Create_TagBoundary1F_IsParsedAsTwoByteTag()
    {
        // Arrange: Tag bytes 0x1F 0x33 (treated as long-form tag), length 1, value 0xAA
        var input = new byte[] { 0x1F, 0x33, 0x01, 0xAA };

        // Act
        using var tlv = Tlv.Create(input);

        // Assert: Tag becomes 0x1F33
        Assert.Equal(0x1F33, tlv.Tag);
        Assert.Equal(1, tlv.Length);
        Assert.Equal(0xAA, tlv.Value.Span[0]);
    }

    /// <summary>
    ///     Pins the header arithmetic that makes <see cref="Tlv.TotalLength" /> trustworthy as the
    ///     sole size input to <see cref="TlvHelper.EncodeList" />. The expected size is spelled out
    ///     here rather than read back off the Tlv, so this fails if either the length-field encoding
    ///     in <see cref="Tlv" /> or the meaning of TotalLength changes.
    ///     An undersized TotalLength would make EncodeList throw; an oversized one would silently
    ///     zero-pad its output.
    /// </summary>
    [Theory]
    [InlineData(0x5A, 0x00, 2)]   // 1 tag + 1 short-form length
    [InlineData(0x5A, 0x7F, 2)]   // largest short-form length
    [InlineData(0x5A, 0x80, 3)]   // 1 tag + 0x81 0x80
    [InlineData(0x5A, 0xFF, 3)]
    [InlineData(0x5A, 0x100, 4)]  // 1 tag + 0x82 0x01 0x00
    [InlineData(0x9F33, 0x7F, 3)] // 2-byte tag + 1 short-form length
    [InlineData(0x9F33, 0x100, 5)]
    public void Tlv_TotalLength_IsHeaderPlusValue(int tag, int valueLength, int headerLength)
    {
        using var tlv = new Tlv(tag, new byte[valueLength]);

        Assert.Equal(headerLength + valueLength, tlv.TotalLength);
    }

    [Fact]
    public void EncodeList_EmitsExpectedBytes()
    {
        using var first = new Tlv(0x5A, new byte[] { 0xAA });
        using var second = new Tlv(0x9F33, new byte[] { 0xFF });

        var encoded = TlvHelper.EncodeList([first, second]);

        Assert.Equal(
            new byte[] { 0x5A, 0x01, 0xAA, 0x9F, 0x33, 0x01, 0xFF },
            encoded.ToArray());
    }

    [Fact]
    public void EncodeList_WithSingleElement_EmitsThatElementOnly()
    {
        using var only = new Tlv(0x5A, new byte[] { 0xAA, 0xBB });

        var encoded = TlvHelper.EncodeList([only]);

        Assert.Equal(new byte[] { 0x5A, 0x02, 0xAA, 0xBB }, encoded.ToArray());
    }

    [Fact]
    public void EncodeList_WithNoElements_ReturnsEmpty()
    {
        var encoded = TlvHelper.EncodeList([]);

        Assert.True(encoded.IsEmpty);
    }
}