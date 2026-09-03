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

using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

/// <summary>Tests for <see cref="AsnUtilities"/>.</summary>
/// <remarks>
/// A positive ASN.1 INTEGER needs a leading <c>0x00</c> content octet only when the high bit of
/// its first significant content octet is set; otherwise the signed value would be negative.
/// </remarks>
public class AsnUtilitiesTests
{
    #region TrimLeadingZeroes(ReadOnlySpan<byte>)

    [Fact]
    public void TrimLeadingZeroes_ReadOnlySpan_NoLeadingZeroes_ReturnsUnchanged()
    {
        byte[] data = [0x01, 0x02, 0x03];

        var result = AsnUtilities.TrimLeadingZeroes((ReadOnlySpan<byte>)data);

        Assert.Equal(data, result.ToArray());
    }

    [Fact]
    public void TrimLeadingZeroes_ReadOnlySpan_OneLeadingZero_IsRemoved()
    {
        byte[] data = [0x00, 0x80, 0x01];

        var result = AsnUtilities.TrimLeadingZeroes((ReadOnlySpan<byte>)data);

        Assert.Equal<byte>([0x80, 0x01], result.ToArray());
    }

    [Fact]
    public void TrimLeadingZeroes_ReadOnlySpan_MultipleLeadingZeroes_AreAllRemoved()
    {
        byte[] data = [0x00, 0x00, 0x00, 0x7F];

        var result = AsnUtilities.TrimLeadingZeroes((ReadOnlySpan<byte>)data);

        Assert.Equal<byte>([0x7F], result.ToArray());
    }

    [Fact]
    public void TrimLeadingZeroes_ReadOnlySpan_AllZeroes_LeavesSingleZeroByte()
    {
        byte[] data = [0x00, 0x00, 0x00];

        var result = AsnUtilities.TrimLeadingZeroes((ReadOnlySpan<byte>)data);

        Assert.Equal<byte>([0x00], result.ToArray());
    }

    [Fact]
    public void TrimLeadingZeroes_ReadOnlySpan_EmptyInput_ReturnsEmpty()
    {
        var result = AsnUtilities.TrimLeadingZeroes(ReadOnlySpan<byte>.Empty);

        Assert.Equal(0, result.Length);
    }

    #endregion

    #region TrimLeadingZeroes(Span<byte>)

    [Fact]
    public void TrimLeadingZeroes_Span_NoLeadingZeroes_ReturnsUnchanged()
    {
        byte[] data = [0x05, 0x06];

        var result = AsnUtilities.TrimLeadingZeroes(data.AsSpan());

        Assert.Equal<byte>([0x05, 0x06], result.ToArray());
    }

    [Fact]
    public void TrimLeadingZeroes_Span_LeadingZeroes_AreRemoved()
    {
        byte[] data = [0x00, 0x00, 0x01, 0x02];

        var result = AsnUtilities.TrimLeadingZeroes(data.AsSpan());

        Assert.Equal<byte>([0x01, 0x02], result.ToArray());
    }

    [Fact]
    public void TrimLeadingZeroes_Span_AllZeroes_LeavesSingleZeroByte()
    {
        byte[] data = [0x00, 0x00];

        var result = AsnUtilities.TrimLeadingZeroes(data.AsSpan());

        Assert.Equal<byte>([0x00], result.ToArray());
    }

    [Fact]
    public void TrimLeadingZeroes_Span_EmptyInput_ReturnsEmpty()
    {
        var result = AsnUtilities.TrimLeadingZeroes(Span<byte>.Empty);

        Assert.Equal(0, result.Length);
    }

    #endregion

    #region GetCoordinateSizeFromCurve

    [Theory]
    [InlineData(Oids.ECP256, 32)]
    [InlineData(Oids.ECP384, 48)]
    [InlineData(Oids.ECP521, 66)]
    public void GetCoordinateSizeFromCurve_SupportedCurve_ReturnsExpectedSize(string curveOid, int expectedSize)
    {
        var size = AsnUtilities.GetCoordinateSizeFromCurve(curveOid);

        Assert.Equal(expectedSize, size);
    }

    [Fact]
    public void GetCoordinateSizeFromCurve_UnsupportedOid_ThrowsNotSupportedException() =>
        Assert.Throws<NotSupportedException>(() => AsnUtilities.GetCoordinateSizeFromCurve("1.2.3.4.5.6.7"));

    #endregion

    #region EnsurePositive

    [Fact]
    public void EnsurePositive_NullInput_ReturnsEmptyArray()
    {
        var result = AsnUtilities.EnsurePositive(null);

        Assert.Empty(result);
    }

    [Fact]
    public void EnsurePositive_EmptyInput_ReturnsEmptyArray()
    {
        var result = AsnUtilities.EnsurePositive([]);

        Assert.Empty(result);
    }

    [Fact]
    public void EnsurePositive_HighBitNotSet_ReturnsUnchanged()
    {
        byte[] value = [0x7F, 0x01];

        var result = AsnUtilities.EnsurePositive(value);

        Assert.Equal<byte>([0x7F, 0x01], result);
    }

    [Fact]
    public void EnsurePositive_HighBitSet_PrependsLeadingZero()
    {
        byte[] value = [0x80, 0x01];

        var result = AsnUtilities.EnsurePositive(value);

        Assert.Equal<byte>([0x00, 0x80, 0x01], result);
    }

    [Fact]
    public void EnsurePositive_DoesNotStripExtraLeadingZeroes_UnlikeGetIntegerBytes()
    {
        byte[] value = [0x00, 0x00, 0x7F];

        var ensurePositiveResult = AsnUtilities.EnsurePositive(value);
        var getIntegerBytesResult = AsnUtilities.GetIntegerBytes(value.AsSpan()).ToArray();

        Assert.Equal<byte>([0x00, 0x00, 0x7F], ensurePositiveResult);
        Assert.Equal<byte>([0x7F], getIntegerBytesResult);
    }

    #endregion

    #region GetIntegerBytes

    [Fact]
    public void GetIntegerBytes_EmptyInput_ReturnsSingleZeroByte()
    {
        var result = AsnUtilities.GetIntegerBytes(Span<byte>.Empty);

        Assert.Equal<byte>([0x00], result.ToArray());
    }

    [Fact]
    public void GetIntegerBytes_NoLeadingZeroesHighBitClear_ReturnsUnchanged()
    {
        byte[] value = [0x7F, 0x01];

        var result = AsnUtilities.GetIntegerBytes(value.AsSpan());

        Assert.Equal<byte>([0x7F, 0x01], result.ToArray());
    }

    [Fact]
    public void GetIntegerBytes_HighBitSetAfterTrim_PrependsLeadingZero()
    {
        byte[] value = [0x80, 0x01];

        var result = AsnUtilities.GetIntegerBytes(value.AsSpan());

        Assert.Equal<byte>([0x00, 0x80, 0x01], result.ToArray());
    }

    [Fact]
    public void GetIntegerBytes_LeadingZeroesThenHighBitSet_TrimsThenPrependsOneZero()
    {
        byte[] value = [0x00, 0x00, 0x80];

        var result = AsnUtilities.GetIntegerBytes(value.AsSpan());

        Assert.Equal<byte>([0x00, 0x80], result.ToArray());
    }

    #endregion
}