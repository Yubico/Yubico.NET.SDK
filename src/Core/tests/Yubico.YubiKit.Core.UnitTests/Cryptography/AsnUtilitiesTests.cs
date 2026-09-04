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

using System.Security.Cryptography;
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

    #region Curve resolution

    /// <summary>
    /// OIDs that <see cref="KeyDefinitions.GetByOid"/> happily resolves but that are not EC prime
    /// curves. Resolving through it would hand back a coordinate size for a non-EC algorithm.
    /// </summary>
    public static TheoryData<string> NonEcCurveOids => new()
    {
        Oids.X25519, // 32-byte definition, so it would masquerade as a P-256 coordinate size
        Oids.Ed25519,
        Oids.AES256Cbc,
        Oids.TripleDESCbc
    };

    /// <summary>Well-formed EC curve OIDs that this SDK does not support.</summary>
    public static TheoryData<string> UnsupportedCurveOids => new()
    {
        "1.3.132.0.10", // secp256k1
        "1.2.840.10045.3.1.1", // secp192r1 / P-192
        "1.2.3.4.5.6.7" // not an algorithm OID at all
    };

    [Theory]
    [InlineData(Oids.ECP256, 32)]
    [InlineData(Oids.ECP384, 48)]
    [InlineData(Oids.ECP521, 66)]
    public void GetDecodedCoordinateSize_SupportedCurve_ReturnsExpectedSize(string curveOid, int expectedSize)
    {
        var size = AsnUtilities.GetDecodedCoordinateSize(curveOid);

        Assert.Equal(expectedSize, size);
    }

    [Theory]
    [MemberData(nameof(NonEcCurveOids))]
    [MemberData(nameof(UnsupportedCurveOids))]
    public void GetDecodedCoordinateSize_NotASupportedPrimeCurve_ThrowsCryptographicException(string oid) =>
        Assert.Throws<CryptographicException>(() => AsnUtilities.GetDecodedCoordinateSize(oid));

    [Theory]
    [MemberData(nameof(NonEcCurveOids))]
    [MemberData(nameof(UnsupportedCurveOids))]
    public void ValidateDecodedEcPoint_NotASupportedPrimeCurve_ThrowsCryptographicException(string oid)
    {
        var point = new byte[65];
        point[0] = 0x04;

        Assert.Throws<CryptographicException>(() => AsnUtilities.ValidateDecodedEcPoint(point, oid));
    }

    [Theory]
    [MemberData(nameof(NonEcCurveOids))]
    [MemberData(nameof(UnsupportedCurveOids))]
    public void ValidateEcPointArgument_NotASupportedPrimeCurve_ThrowsArgumentException(string oid)
    {
        // A 65-byte uncompressed point is exactly right for P-256, and X25519's key definition is
        // also 32 bytes, so a size-only check would let this through and emit it as an EC key.
        var point = new byte[65];
        point[0] = 0x04;

        var exception = Assert.Throws<ArgumentException>(
            () => AsnUtilities.ValidateEcPointArgument(point, oid, "publicPoint"));

        Assert.Equal("publicPoint", exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(NonEcCurveOids))]
    [MemberData(nameof(UnsupportedCurveOids))]
    public void BuildUncompressedEcPoint_NotASupportedPrimeCurve_ThrowsArgumentException(string oid)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AsnUtilities.BuildUncompressedEcPoint(new byte[32], new byte[32], oid, "parameters"));

        Assert.Equal("parameters", exception.ParamName);
    }

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