using System.Buffers.Binary;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class CapabilityMapperTests
{
    [Theory]
    [InlineData(0x00, DeviceCapabilities.None)]
    [InlineData(0x01, DeviceCapabilities.Fido2)]
    [InlineData(0x02, DeviceCapabilities.Piv)]
    [InlineData(0x04, DeviceCapabilities.OpenPgp)]
    [InlineData(0x08, DeviceCapabilities.Oath)]
    [InlineData(0x10, DeviceCapabilities.HsmAuth)]
    public void FromFips_SingleBit_MapsCorrectly(int fipsValue, DeviceCapabilities expected)
    {
        var buffer = CreateBigEndianBytes(fipsValue);

        var result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x03, DeviceCapabilities.Fido2 | DeviceCapabilities.Piv)]
    [InlineData(0x05, DeviceCapabilities.Fido2 | DeviceCapabilities.OpenPgp)]
    [InlineData(0x07, DeviceCapabilities.Fido2 | DeviceCapabilities.Piv | DeviceCapabilities.OpenPgp)]
    [InlineData(0x0F,
        DeviceCapabilities.Fido2 | DeviceCapabilities.Piv | DeviceCapabilities.OpenPgp | DeviceCapabilities.Oath)]
    [InlineData(0x1F,
        DeviceCapabilities.Fido2 | DeviceCapabilities.Piv | DeviceCapabilities.OpenPgp | DeviceCapabilities.Oath |
        DeviceCapabilities.HsmAuth)]
    public void FromFips_MultipleBits_MapsCorrectly(int fipsValue, DeviceCapabilities expected)
    {
        var buffer = CreateBigEndianBytes(fipsValue);

        var result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromFips_AllFipsCapabilities_ReturnsExpected()
    {
        var buffer = CreateBigEndianBytes(0x1F);

        var result = CapabilityMapper.FromFips(buffer);

        var expected =
            DeviceCapabilities.Fido2 |
            DeviceCapabilities.Piv |
            DeviceCapabilities.OpenPgp |
            DeviceCapabilities.Oath |
            DeviceCapabilities.HsmAuth;

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x20)]
    [InlineData(0x40)]
    [InlineData(0x80)]
    [InlineData(0xE0)]
    [InlineData(0xFF)]
    public void FromFips_UnknownBits_AreIgnored(int fipsValue)
    {
        var buffer = CreateBigEndianBytes(fipsValue);

        var result = CapabilityMapper.FromFips(buffer);

        var knownBits = (DeviceCapabilities)(fipsValue & 0x1F);
        var knownBuffer = CreateBigEndianBytes((int)knownBits);
        var expected = CapabilityMapper.FromFips(knownBuffer);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromFips_UnknownBitsCombinedWithKnown_MapsKnownOnly()
    {
        var buffer = CreateBigEndianBytes(0xE1);

        var result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(DeviceCapabilities.Fido2, result);
    }

    [Fact]
    public void FromFips_EmptyBuffer_ReturnsNone()
    {
        var buffer = Array.Empty<byte>();

        var result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(DeviceCapabilities.None, result);
    }

    [Fact]
    public void FromFips_HighByte_IsProcessed()
    {
        var buffer = new byte[] { 0x01, 0x00 };

        var result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(DeviceCapabilities.None, result);
    }

    [Theory]
    [InlineData(0x0001, DeviceCapabilities.Otp)]
    [InlineData(0x0002, DeviceCapabilities.U2f)]
    [InlineData(0x0008, DeviceCapabilities.OpenPgp)]
    [InlineData(0x0010, DeviceCapabilities.Piv)]
    [InlineData(0x0020, DeviceCapabilities.Oath)]
    [InlineData(0x0100, DeviceCapabilities.HsmAuth)]
    [InlineData(0x0200, DeviceCapabilities.Fido2)]
    public void FromApp_TwoBytes_DirectCast(int appValue, DeviceCapabilities expected)
    {
        var buffer = CreateBigEndianBytes(appValue);

        var result = CapabilityMapper.FromApp(buffer);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x01, DeviceCapabilities.Otp)]
    [InlineData(0x02, DeviceCapabilities.U2f)]
    [InlineData(0x08, DeviceCapabilities.OpenPgp)]
    [InlineData(0x10, DeviceCapabilities.Piv)]
    [InlineData(0x20, DeviceCapabilities.Oath)]
    public void FromApp_OneByte_DirectCast(byte appValue, DeviceCapabilities expected)
    {
        var buffer = new[] { appValue };

        var result = CapabilityMapper.FromApp(buffer);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromApp_MultipleCaps_DirectCast()
    {
        var allCaps = (int)DeviceCapabilities.All;
        var buffer = CreateBigEndianBytes(allCaps);

        var result = CapabilityMapper.FromApp(buffer);

        Assert.Equal(DeviceCapabilities.All, result);
    }

    [Fact]
    public void FromApp_EmptyBuffer_ReturnsNone()
    {
        var buffer = Array.Empty<byte>();

        var result = CapabilityMapper.FromApp(buffer);

        Assert.Equal(DeviceCapabilities.None, result);
    }

    [Fact]
    public void FromFips_DoesNotIncludeOtpOrU2f()
    {
        var buffer = CreateBigEndianBytes(0xFF);

        var result = CapabilityMapper.FromFips(buffer);

        Assert.False(result.HasFlag(DeviceCapabilities.Otp));
        Assert.False(result.HasFlag(DeviceCapabilities.U2f));
    }

    [Fact]
    public void FromApp_CanIncludeOtpAndU2f()
    {
        var appValue = (int)(DeviceCapabilities.Otp | DeviceCapabilities.U2f);
        var buffer = CreateBigEndianBytes(appValue);

        var result = CapabilityMapper.FromApp(buffer);

        Assert.True(result.HasFlag(DeviceCapabilities.Otp));
        Assert.True(result.HasFlag(DeviceCapabilities.U2f));
    }

    [Theory]
    [InlineData(DeviceCapabilities.Fido2)]
    [InlineData(DeviceCapabilities.Piv)]
    [InlineData(DeviceCapabilities.OpenPgp)]
    [InlineData(DeviceCapabilities.Oath)]
    [InlineData(DeviceCapabilities.HsmAuth)]
    public void FromFips_RoundTrip_EachFipsCapability(DeviceCapabilities capability)
    {
        var fipsBit = capability switch
        {
            DeviceCapabilities.Fido2 => 0x01,
            DeviceCapabilities.Piv => 0x02,
            DeviceCapabilities.OpenPgp => 0x04,
            DeviceCapabilities.Oath => 0x08,
            DeviceCapabilities.HsmAuth => 0x10,
            _ => throw new ArgumentException("Not a FIPS capability")
        };

        var buffer = CreateBigEndianBytes(fipsBit);

        var result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(capability, result);
    }

    [Fact]
    public void FromFips_BigEndianEncoding_IsRespected()
    {
        var buffer = new byte[] { 0x00, 0x1F };

        var result = CapabilityMapper.FromFips(buffer);

        var expected =
            DeviceCapabilities.Fido2 |
            DeviceCapabilities.Piv |
            DeviceCapabilities.OpenPgp |
            DeviceCapabilities.Oath |
            DeviceCapabilities.HsmAuth;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromApp_BigEndianEncoding_IsRespected()
    {
        var buffer = new byte[] { 0x03, 0x00 };

        var result = CapabilityMapper.FromApp(buffer);

        var expected = DeviceCapabilities.HsmAuth | DeviceCapabilities.Fido2;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromFips_NullMemory_ReturnsNone()
    {
        ReadOnlyMemory<byte> memory = default;

        var result = CapabilityMapper.FromFips(memory);

        Assert.Equal(DeviceCapabilities.None, result);
    }

    [Fact]
    public void FromApp_NullMemory_ReturnsNone()
    {
        ReadOnlyMemory<byte> memory = default;

        var result = CapabilityMapper.FromApp(memory);

        Assert.Equal(DeviceCapabilities.None, result);
    }

    [Theory]
    [InlineData(new byte[] { 0x00 })]
    [InlineData(new byte[] { 0x00, 0x00 })]
    [InlineData(new byte[] { 0x00, 0x00, 0x00 })]
    public void FromFips_ZeroValue_ReturnsNone(byte[] buffer)
    {
        var result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(DeviceCapabilities.None, result);
    }

    [Fact]
    public void FromFips_MaxInt16_ParsesCorrectly()
    {
        var buffer = new byte[] { 0x7F, 0xFF };

        var result = CapabilityMapper.FromFips(buffer);

        var expected =
            DeviceCapabilities.Fido2 |
            DeviceCapabilities.Piv |
            DeviceCapabilities.OpenPgp |
            DeviceCapabilities.Oath |
            DeviceCapabilities.HsmAuth;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromFips_NegativeInt16_ParsesCorrectly()
    {
        var buffer = new byte[] { 0x80, 0x00 };

        var result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(DeviceCapabilities.None, result);
    }

    private static byte[] CreateBigEndianBytes(int value)
    {
        var buffer = new byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, (short)value);
        return buffer;
    }
}