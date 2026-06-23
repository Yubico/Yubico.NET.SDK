// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu;

public class ApduFormatterTests
{
    [Fact]
    public void ShortFormatter_WithDataAndNoLe_AppendsZeroLe()
    {
        var formatter = new ApduFormatterShort();

        var formatted = formatter.Format(0x00, 0xCA, 0x00, 0x00, new byte[] { 0x5F, 0xC1 }, le: 0);

        Assert.Equal(new byte[] { 0x00, 0xCA, 0x00, 0x00, 0x02, 0x5F, 0xC1, 0x00 }, formatted.ToArray());
    }

    [Fact]
    public void ShortFormatter_WithoutDataAndNoLe_AppendsZeroLe()
    {
        var formatter = new ApduFormatterShort();

        var formatted = formatter.Format(0x00, 0xCA, 0x00, 0x00, ReadOnlyMemory<byte>.Empty, le: 0);

        Assert.Equal(new byte[] { 0x00, 0xCA, 0x00, 0x00, 0x00 }, formatted.ToArray());
    }

    [Fact]
    public void ShortFormatter_WithExplicitLe_UsesCallerLe()
    {
        var formatter = new ApduFormatterShort();

        var formatted = formatter.Format(0x00, 0xCA, 0x00, 0x00, new byte[] { 0x5F, 0xC1 }, le: 0x10);

        Assert.Equal(new byte[] { 0x00, 0xCA, 0x00, 0x00, 0x02, 0x5F, 0xC1, 0x10 }, formatted.ToArray());
    }

    [Fact]
    public void ExtendedFormatter_WithDataAndNoLe_AppendsExtendedZeroLe()
    {
        var formatter = new ApduFormatterExtended(SmartCardMaxApduSizes.Yk43);

        var formatted = formatter.Format(0x00, 0xCA, 0x00, 0x00, new byte[] { 0x5F, 0xC1 }, le: 0);

        Assert.Equal(new byte[] { 0x00, 0xCA, 0x00, 0x00, 0x00, 0x00, 0x02, 0x5F, 0xC1, 0x00, 0x00 }, formatted.ToArray());
    }

    [Fact]
    public void ExtendedFormatter_WithoutDataAndNoLe_AppendsExtendedZeroLe()
    {
        var formatter = new ApduFormatterExtended(SmartCardMaxApduSizes.Yk43);

        var formatted = formatter.Format(0x00, 0xCA, 0x00, 0x00, ReadOnlyMemory<byte>.Empty, le: 0);

        Assert.Equal(new byte[] { 0x00, 0xCA, 0x00, 0x00, 0x00, 0x00, 0x00 }, formatted.ToArray());
    }

    [Fact]
    public void ExtendedFormatter_WithoutDataAndExplicitLe_UsesExtendedCallerLe()
    {
        var formatter = new ApduFormatterExtended(SmartCardMaxApduSizes.Yk43);

        var formatted = formatter.Format(0x00, 0xCA, 0x00, 0x00, ReadOnlyMemory<byte>.Empty, le: 0x0100);

        Assert.Equal(new byte[] { 0x00, 0xCA, 0x00, 0x00, 0x00, 0x01, 0x00 }, formatted.ToArray());
    }

    [Fact]
    public void ExtendedFormatter_WithLargePayloadAndNoLe_AppendsExtendedZeroLe()
    {
        var formatter = new ApduFormatterExtended(SmartCardMaxApduSizes.Yk43);
        var payload = Enumerable.Range(0, 300).Select(i => (byte)i).ToArray();

        var formatted = formatter.Format(0x00, 0xDB, 0x3F, 0xFF, payload, le: 0);

        var expected = new byte[4 + 1 + 2 + payload.Length + 2];
        expected[0] = 0x00;
        expected[1] = 0xDB;
        expected[2] = 0x3F;
        expected[3] = 0xFF;
        expected[4] = 0x00;
        expected[5] = 0x01;
        expected[6] = 0x2C;
        payload.CopyTo(expected.AsSpan(7));
        expected[^2] = 0x00;
        expected[^1] = 0x00;
        Assert.Equal(expected, formatted.ToArray());
    }

    [Fact]
    public void ExtendedFormatter_WithLeOutOfRange_ThrowsArgumentException()
    {
        var formatter = new ApduFormatterExtended(SmartCardMaxApduSizes.Yk43);

        Assert.Throws<ArgumentException>(() => formatter.Format(0x00, 0xCA, 0x00, 0x00, ReadOnlyMemory<byte>.Empty, le: -1));
        Assert.Throws<ArgumentException>(() => formatter.Format(0x00, 0xCA, 0x00, 0x00, ReadOnlyMemory<byte>.Empty, le: 65537));
    }
}
