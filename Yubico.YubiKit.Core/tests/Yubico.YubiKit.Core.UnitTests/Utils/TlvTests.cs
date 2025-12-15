// Copyright 2025 Yubico AB
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

using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.UnitTests.Utils;

public class TlvTests
{
    [Fact]
    public void Constructor_ShortFormLength_EncodesCorrectly()
    {
        // Tag 0x5A, length 3, value 0x01 02 03
        using var tlv = new Tlv(0x5A, new byte[] { 0x01, 0x02, 0x03 });
        var encoded = tlv.AsSpan();

        Assert.Equal(0x5A, encoded[0]);
        Assert.Equal(0x03, encoded[1]);
        Assert.True(encoded[2..].SequenceEqual(new byte[] { 0x01, 0x02, 0x03 }));

        Assert.Equal(0x5A, tlv.Tag);
        Assert.Equal(3, tlv.Length);
        Assert.True(tlv.Value.Span.SequenceEqual(new byte[] { 0x01, 0x02, 0x03 }));
    }

    [Fact]
    public void Constructor_LongFormLength_130Bytes_EncodesWith0x81()
    {
        var value = Enumerable.Range(0, 130).Select(i => (byte)i).ToArray();
        using var tlv = new Tlv(0x5A, value);
        var encoded = tlv.AsSpan();

        // Expect: 0x5A 0x81 0x82 <130 bytes>
        Assert.Equal(0x5A, encoded[0]);
        Assert.Equal(0x81, encoded[1]);
        Assert.Equal(0x82, encoded[2]);
        Assert.True(encoded[3..].SequenceEqual(value));

        Assert.Equal(130, tlv.Length);
    }

    [Fact]
    public void Constructor_LongFormLength_256Bytes_EncodesWith0x82()
    {
        var value = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        using var tlv = new Tlv(0x6F, value);
        var encoded = tlv.AsSpan();

        // Expect: 0x6F 0x82 0x01 0x00 <256 bytes>
        Assert.Equal(0x6F, encoded[0]);
        Assert.Equal(0x82, encoded[1]);
        Assert.Equal(0x01, encoded[2]);
        Assert.Equal(0x00, encoded[3]);
        Assert.True(encoded[4..].SequenceEqual(value));
    }

    [Fact]
    public void Constructor_MultiByteTag_0x9F33_EncodesCorrectly()
    {
        using var tlv = new Tlv(0x9F33, new byte[] { 0xDE, 0xAD });
        var encoded = tlv.AsSpan();

        Assert.Equal(0x9F, encoded[0]);
        Assert.Equal(0x33, encoded[1]);
        Assert.Equal(0x02, encoded[2]);
        Assert.Equal(0xDE, encoded[3]);
        Assert.Equal(0xAD, encoded[4]);

        Assert.Equal(0x9F33, tlv.Tag);
        Assert.Equal(2, tlv.Length);
    }

    [Fact]
    public void Create_Parses_ShortForm()
    {
        // 5A 03 01 02 03
        ReadOnlySpan<byte> bytes = new byte[] { 0x5A, 0x03, 0x01, 0x02, 0x03 };
        using var tlv = Tlv.Create(bytes);

        Assert.Equal(0x5A, tlv.Tag);
        Assert.Equal(3, tlv.Length);
        Assert.True(tlv.Value.Span.SequenceEqual(new byte[] { 0x01, 0x02, 0x03 }));
    }

    [Fact]
    public void Create_Parses_LongForm_0x81()
    {
        // 5A 81 82 <130 bytes of 0..129>
        var value = Enumerable.Range(0, 130).Select(i => (byte)i).ToArray();
        var encoded = new byte[3 + value.Length];
        encoded[0] = 0x5A;
        encoded[1] = 0x81;
        encoded[2] = 0x82;
        Array.Copy(value, 0, encoded, 3, value.Length);

        using var tlv = Tlv.Create(encoded);
        Assert.Equal(0x5A, tlv.Tag);
        Assert.True(tlv.Value.Span.SequenceEqual(value));
    }

    [Fact]
    public void Create_Parses_MultiByteTag()
    {
        // 9F 33 01 FF
        ReadOnlySpan<byte> bytes = new byte[] { 0x9F, 0x33, 0x01, 0xFF };
        using var tlv = Tlv.Create(bytes);
        Assert.Equal(0x9F33, tlv.Tag);
        Assert.Equal(1, tlv.Length);
        Assert.Equal(0xFF, tlv.Value.Span[0]);
    }

    [Fact]
    public void AsMemory_AsSpan_RoundTrip_Equals_EncodedBytes()
    {
        using var tlv = new Tlv(0x7F49, new byte[] { 0x00, 0x01, 0x02, 0x03 });
        var encoded = tlv.AsSpan().ToArray();
        var mem = tlv.AsMemory().ToArray();
        Assert.True(encoded.AsSpan().SequenceEqual(mem));
    }

    [Fact]
    public void Dispose_Zeros_Buffer_And_Resets_Properties()
    {
        var tlv = new Tlv(0x5A, new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var before = tlv.AsMemory().ToArray();
        Assert.Contains((byte)0x5A, before);

        tlv.Dispose();

        var after = tlv.AsMemory().ToArray();
        Assert.All(after, b => Assert.Equal(0, b));
        Assert.Equal(0, tlv.Tag);
        Assert.Equal(0, tlv.Length);
    }

    [Fact]
    public void Create_Throws_On_Empty_Input() =>
        Assert.Throws<ArgumentException>(() => Tlv.Create(Array.Empty<byte>()));

    [Fact]
    public void Create_Throws_On_Truncated_Length() =>
        // Tag present, but length byte missing
        Assert.Throws<ArgumentException>(() => Tlv.Create(new byte[] { 0x5A }));

    [Fact]
    public void ToString_Contains_Tag_Length_And_Value()
    {
        using var tlv = new Tlv(0x5A, new byte[] { 0xAA, 0xBB });
        var s = tlv.ToString();
        Assert.Contains("0x5A", s, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2", s, StringComparison.Ordinal);
        Assert.Contains("AABB", s, StringComparison.OrdinalIgnoreCase);
    }
}