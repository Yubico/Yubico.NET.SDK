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

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public class PrivateKeyTemplateTests
{
    /// <summary>
    ///     Parses tag+length descriptor pairs from an OpenPGP 0x7F48 header block.
    ///     These are NOT standard TLVs — they contain only tag and BER-TLV length,
    ///     no value bytes. The actual values live in the 0x5F48 block.
    /// </summary>
    private static List<(int Tag, int Length)> ParseHeaderDescriptors(ReadOnlySpan<byte> headers)
    {
        var result = new List<(int, int)>();
        var offset = 0;
        while (offset < headers.Length)
        {
            // Single-byte tag (0x91-0x99 range used by OpenPGP)
            var tag = headers[offset++];

            // BER-TLV length encoding
            int length;
            if (headers[offset] < 0x80)
            {
                length = headers[offset++];
            }
            else if (headers[offset] == 0x81)
            {
                offset++;
                length = headers[offset++];
            }
            else if (headers[offset] == 0x82)
            {
                offset++;
                length = (headers[offset] << 8) | headers[offset + 1];
                offset += 2;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported length encoding: 0x{headers[offset]:X2}");
            }

            result.Add((tag, length));
        }

        return result;
    }

    [Fact]
    public void RsaKeyTemplate_ToBytes_ContainsExpectedStructure()
    {
        byte[] e = [0x01, 0x00, 0x01]; // 65537
        byte[] p = [0xAA, 0xBB];
        byte[] q = [0xCC, 0xDD];

        var template = new RsaKeyTemplate(KeyRef.Sig, e, p, q);
        var result = template.ToBytes();

        // Outer tag must be 0x4D
        using var outer = Tlv.Create(result);
        Assert.Equal(0x4D, outer.Tag);

        // Inner structure: CRT + TLV(0x7F48, headers) + TLV(0x5F48, values)
        var innerData = outer.Value.Span;
        var dict = TlvHelper.DecodeDictionary(innerData);

        // Must contain header TLV (0x7F48) and value TLV (0x5F48)
        Assert.True(dict.ContainsKey(0x7F48));
        Assert.True(dict.ContainsKey(0x5F48));

        // Values should be concatenated: e + p + q
        var values = dict[0x5F48].ToArray();
        Assert.Equal(e.Length + p.Length + q.Length, values.Length);
        Assert.Equal(e, values[..e.Length]);
        Assert.Equal(p, values[e.Length..(e.Length + p.Length)]);
        Assert.Equal(q, values[(e.Length + p.Length)..]);
    }

    [Fact]
    public void RsaCrtKeyTemplate_ToBytes_ContainsAllComponents()
    {
        byte[] e = [0x01, 0x00, 0x01];
        byte[] p = [0xAA];
        byte[] q = [0xBB];
        byte[] iqmp = [0xCC];
        byte[] dmp1 = [0xDD];
        byte[] dmq1 = [0xEE];
        byte[] n = [0xFF];

        var template = new RsaCrtKeyTemplate(KeyRef.Sig, e, p, q, iqmp, dmp1, dmq1, n);
        var result = template.ToBytes();

        using var outer = Tlv.Create(result);
        Assert.Equal(0x4D, outer.Tag);

        var dict = TlvHelper.DecodeDictionary(outer.Value.Span);

        // Values = e + p + q + iqmp + dmp1 + dmq1 + n
        var values = dict[0x5F48].ToArray();
        var expectedLen = e.Length + p.Length + q.Length + iqmp.Length + dmp1.Length + dmq1.Length + n.Length;
        Assert.Equal(expectedLen, values.Length);

        // Headers should contain tags 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97
        var headers = dict[0x7F48].Span;
        var headerDescriptors = ParseHeaderDescriptors(headers);
        var tags = headerDescriptors.Select(h => h.Tag).ToArray();
        Assert.Equal([0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97], tags);
    }

    [Fact]
    public void EcKeyTemplate_WithoutPublicKey_ContainsOnlyPrivateKey()
    {
        byte[] privateKey = [0x01, 0x02, 0x03, 0x04];

        var template = new EcKeyTemplate(KeyRef.Sig, privateKey);
        var result = template.ToBytes();

        using var outer = Tlv.Create(result);
        var dict = TlvHelper.DecodeDictionary(outer.Value.Span);

        // Headers should only have tag 0x92
        var headers = dict[0x7F48].Span;
        var headerDescriptors = ParseHeaderDescriptors(headers);
        Assert.Single(headerDescriptors);
        Assert.Equal(0x92, headerDescriptors[0].Tag);

        // Value should be just the private key
        Assert.Equal(privateKey, dict[0x5F48].ToArray());
    }

    [Fact]
    public void EcKeyTemplate_WithPublicKey_ContainsBothKeys()
    {
        byte[] privateKey = [0x01, 0x02, 0x03, 0x04];
        byte[] publicKey = [0x04, 0x05, 0x06, 0x07, 0x08];

        var template = new EcKeyTemplate(KeyRef.Sig, privateKey, publicKey);
        var result = template.ToBytes();

        using var outer = Tlv.Create(result);
        var dict = TlvHelper.DecodeDictionary(outer.Value.Span);

        // Headers should have tags 0x92 and 0x99
        var headers = dict[0x7F48].Span;
        var headerDescriptors = ParseHeaderDescriptors(headers);
        Assert.Equal(2, headerDescriptors.Count);
        Assert.Equal(0x92, headerDescriptors[0].Tag);
        Assert.Equal(0x99, headerDescriptors[1].Tag);

        // Values = privateKey + publicKey
        var values = dict[0x5F48].ToArray();
        Assert.Equal(privateKey.Length + publicKey.Length, values.Length);
    }

    [Fact]
    public void RsaKeyTemplate_HeaderLengths_MatchValueLengths()
    {
        byte[] e = [0x01, 0x00, 0x01]; // 3 bytes
        byte[] p = new byte[128]; // 128 bytes
        byte[] q = new byte[128]; // 128 bytes

        var template = new RsaKeyTemplate(KeyRef.Dec, e, p, q);
        var result = template.ToBytes();

        using var outer = Tlv.Create(result);
        var dict = TlvHelper.DecodeDictionary(outer.Value.Span);

        // Parse headers to verify lengths match
        var headers = dict[0x7F48].Span;
        var headerDescriptors = ParseHeaderDescriptors(headers);

        Assert.Equal(3, headerDescriptors.Count);
        Assert.Equal(e.Length, headerDescriptors[0].Length);
        Assert.Equal(p.Length, headerDescriptors[1].Length);
        Assert.Equal(q.Length, headerDescriptors[2].Length);
    }
}
