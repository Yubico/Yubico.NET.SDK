
using System.Buffers.Binary;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Core.UnitTests;
public class CapabilityMapperTests
{
    [Theory]
    [InlineData(0x00, YubiKeyCapabilities.None)]
    [InlineData(0x01, YubiKeyCapabilities.Fido2)]
    [InlineData(0x02, YubiKeyCapabilities.Piv)]
    [InlineData(0x04, YubiKeyCapabilities.OpenPgp)]
    [InlineData(0x08, YubiKeyCapabilities.Oath)]
    [InlineData(0x10, YubiKeyCapabilities.HsmAuth)]
    public void FromFips_SingleBit_MapsCorrectly(int fipsValue, YubiKeyCapabilities expected)
    {
        byte[] buffer = CreateBigEndianBytes(fipsValue);

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x03, YubiKeyCapabilities.Fido2 | YubiKeyCapabilities.Piv)]
    [InlineData(0x05, YubiKeyCapabilities.Fido2 | YubiKeyCapabilities.OpenPgp)]
    [InlineData(0x07, YubiKeyCapabilities.Fido2 | YubiKeyCapabilities.Piv | YubiKeyCapabilities.OpenPgp)]
    [InlineData(0x0F, YubiKeyCapabilities.Fido2 | YubiKeyCapabilities.Piv | YubiKeyCapabilities.OpenPgp | YubiKeyCapabilities.Oath)]
    [InlineData(0x1F, YubiKeyCapabilities.Fido2 | YubiKeyCapabilities.Piv | YubiKeyCapabilities.OpenPgp | YubiKeyCapabilities.Oath | YubiKeyCapabilities.HsmAuth)]
    public void FromFips_MultipleBits_MapsCorrectly(int fipsValue, YubiKeyCapabilities expected)
    {
        byte[] buffer = CreateBigEndianBytes(fipsValue);

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromFips_AllFipsCapabilities_ReturnsExpected()
    {
        byte[] buffer = CreateBigEndianBytes(0x1F); // All 5 FIPS bits set

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        YubiKeyCapabilities expected =
            YubiKeyCapabilities.Fido2 |
            YubiKeyCapabilities.Piv |
            YubiKeyCapabilities.OpenPgp |
            YubiKeyCapabilities.Oath |
            YubiKeyCapabilities.HsmAuth;

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x20)]  // Bit 5 - undefined
    [InlineData(0x40)]  // Bit 6 - undefined
    [InlineData(0x80)]  // Bit 7 - undefined
    [InlineData(0xE0)]  // Bits 5-7 all set
    [InlineData(0xFF)]  // All bits including undefined
    public void FromFips_UnknownBits_AreIgnored(int fipsValue)
    {
        byte[] buffer = CreateBigEndianBytes(fipsValue);

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        // Should only include known FIPS capabilities (bits 0-4)
        YubiKeyCapabilities knownBits = (YubiKeyCapabilities)(fipsValue & 0x1F);
        byte[] knownBuffer = CreateBigEndianBytes((int)knownBits);
        YubiKeyCapabilities expected = CapabilityMapper.FromFips(knownBuffer);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromFips_UnknownBitsCombinedWithKnown_MapsKnownOnly()
    {
        byte[] buffer = CreateBigEndianBytes(0xE1); // Bits 7,6,5 (unknown) + bit 0 (Fido2)

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(YubiKeyCapabilities.Fido2, result);
    }

    [Fact]
    public void FromFips_EmptyBuffer_ReturnsNone()
    {
        byte[] buffer = Array.Empty<byte>();

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(YubiKeyCapabilities.None, result);
    }

    [Fact]
    public void FromFips_HighByte_IsProcessed()
    {
        // Test that high byte is properly handled (16-bit value)
        byte[] buffer = new byte[] { 0x01, 0x00 }; // 0x0100 big-endian

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        // Bit 8 should be ignored (undefined in FIPS mapping)
        Assert.Equal(YubiKeyCapabilities.None, result);
    }

    [Theory]
    [InlineData(0x0001, YubiKeyCapabilities.Otp)]
    [InlineData(0x0002, YubiKeyCapabilities.U2f)]
    [InlineData(0x0008, YubiKeyCapabilities.OpenPgp)]
    [InlineData(0x0010, YubiKeyCapabilities.Piv)]
    [InlineData(0x0020, YubiKeyCapabilities.Oath)]
    [InlineData(0x0100, YubiKeyCapabilities.HsmAuth)]
    [InlineData(0x0200, YubiKeyCapabilities.Fido2)]
    public void FromApp_TwoBytes_DirectCast(int appValue, YubiKeyCapabilities expected)
    {
        byte[] buffer = CreateBigEndianBytes(appValue);

        YubiKeyCapabilities result = CapabilityMapper.FromApp(buffer);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x01, YubiKeyCapabilities.Otp)]
    [InlineData(0x02, YubiKeyCapabilities.U2f)]
    [InlineData(0x08, YubiKeyCapabilities.OpenPgp)]
    [InlineData(0x10, YubiKeyCapabilities.Piv)]
    [InlineData(0x20, YubiKeyCapabilities.Oath)]
    public void FromApp_OneByte_DirectCast(byte appValue, YubiKeyCapabilities expected)
    {
        byte[] buffer = new byte[] { appValue };

        YubiKeyCapabilities result = CapabilityMapper.FromApp(buffer);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromApp_MultipleCaps_DirectCast()
    {
        int allCaps = (int)YubiKeyCapabilities.All;
        byte[] buffer = CreateBigEndianBytes(allCaps);

        YubiKeyCapabilities result = CapabilityMapper.FromApp(buffer);

        Assert.Equal(YubiKeyCapabilities.All, result);
    }

    [Fact]
    public void FromApp_EmptyBuffer_ReturnsNone()
    {
        byte[] buffer = Array.Empty<byte>();

        YubiKeyCapabilities result = CapabilityMapper.FromApp(buffer);

        Assert.Equal(YubiKeyCapabilities.None, result);
    }

    [Fact]
    public void FromFips_DoesNotIncludeOtpOrU2f()
    {
        // FIPS should never map to Otp or U2f (not FIPS-approved)
        byte[] buffer = CreateBigEndianBytes(0xFF);

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        Assert.False(result.HasFlag(YubiKeyCapabilities.Otp));
        Assert.False(result.HasFlag(YubiKeyCapabilities.U2f));
    }

    [Fact]
    public void FromApp_CanIncludeOtpAndU2f()
    {
        int appValue = (int)(YubiKeyCapabilities.Otp | YubiKeyCapabilities.U2f);
        byte[] buffer = CreateBigEndianBytes(appValue);

        YubiKeyCapabilities result = CapabilityMapper.FromApp(buffer);

        Assert.True(result.HasFlag(YubiKeyCapabilities.Otp));
        Assert.True(result.HasFlag(YubiKeyCapabilities.U2f));
    }

    [Theory]
    [InlineData(YubiKeyCapabilities.Fido2)]
    [InlineData(YubiKeyCapabilities.Piv)]
    [InlineData(YubiKeyCapabilities.OpenPgp)]
    [InlineData(YubiKeyCapabilities.Oath)]
    [InlineData(YubiKeyCapabilities.HsmAuth)]
    public void FromFips_RoundTrip_EachFipsCapability(YubiKeyCapabilities capability)
    {
        // Find which FIPS bit corresponds to this capability
        int fipsBit = capability switch
        {
            YubiKeyCapabilities.Fido2 => 0x01,
            YubiKeyCapabilities.Piv => 0x02,
            YubiKeyCapabilities.OpenPgp => 0x04,
            YubiKeyCapabilities.Oath => 0x08,
            YubiKeyCapabilities.HsmAuth => 0x10,
            _ => throw new ArgumentException("Not a FIPS capability")
        };

        byte[] buffer = CreateBigEndianBytes(fipsBit);

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(capability, result);
    }

    [Fact]
    public void FromFips_BigEndianEncoding_IsRespected()
    {
        // Manually construct big-endian bytes
        byte[] buffer = new byte[] { 0x00, 0x1F }; // Big-endian 0x001F

        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        YubiKeyCapabilities expected =
            YubiKeyCapabilities.Fido2 |
            YubiKeyCapabilities.Piv |
            YubiKeyCapabilities.OpenPgp |
            YubiKeyCapabilities.Oath |
            YubiKeyCapabilities.HsmAuth;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromApp_BigEndianEncoding_IsRespected()
    {
        // Manually construct big-endian bytes for HsmAuth | Fido2
        byte[] buffer = new byte[] { 0x03, 0x00 }; // Big-endian 0x0300

        YubiKeyCapabilities result = CapabilityMapper.FromApp(buffer);

        YubiKeyCapabilities expected = YubiKeyCapabilities.HsmAuth | YubiKeyCapabilities.Fido2;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromFips_NullMemory_ReturnsNone()
    {
        ReadOnlyMemory<byte> memory = default;

        YubiKeyCapabilities result = CapabilityMapper.FromFips(memory);

        Assert.Equal(YubiKeyCapabilities.None, result);
    }

    [Fact]
    public void FromApp_NullMemory_ReturnsNone()
    {
        ReadOnlyMemory<byte> memory = default;

        YubiKeyCapabilities result = CapabilityMapper.FromApp(memory);

        Assert.Equal(YubiKeyCapabilities.None, result);
    }

    [Theory]
    [InlineData(new byte[] { 0x00 })]
    [InlineData(new byte[] { 0x00, 0x00 })]
    [InlineData(new byte[] { 0x00, 0x00, 0x00 })]
    public void FromFips_ZeroValue_ReturnsNone(byte[] buffer)
    {
        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(YubiKeyCapabilities.None, result);
    }

    [Fact]
    public void FromApp_OneByte_HighBitsIgnored()
    {
        // Single byte can only represent lower 8 capabilities
        byte[] buffer = [0xFF];

        YubiKeyCapabilities result = CapabilityMapper.FromApp(buffer);

        // Should only have Otp, U2f, OpenPgp, Piv, Oath (not HsmAuth or Fido2)
        YubiKeyCapabilities expected =
            YubiKeyCapabilities.Otp |
            YubiKeyCapabilities.U2f |
            YubiKeyCapabilities.OpenPgp |
            YubiKeyCapabilities.Piv |
            YubiKeyCapabilities.Oath;

        Assert.Equal((int)expected, (int)result);
    }

    [Fact]
    public void FromFips_MaxInt16_ParsesCorrectly()
    {
        byte[] buffer = new byte[] { 0x7F, 0xFF }; // Max positive int16

        // Should ignore all high bits, only process bits 0-4
        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        YubiKeyCapabilities expected =
            YubiKeyCapabilities.Fido2 |
            YubiKeyCapabilities.Piv |
            YubiKeyCapabilities.OpenPgp |
            YubiKeyCapabilities.Oath |
            YubiKeyCapabilities.HsmAuth;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromFips_NegativeInt16_ParsesCorrectly()
    {
        byte[] buffer = new byte[] { 0x80, 0x00 }; // -32768 as int16, 0x8000 as uint16

        // Should ignore high bits beyond bit 4
        YubiKeyCapabilities result = CapabilityMapper.FromFips(buffer);

        Assert.Equal(YubiKeyCapabilities.None, result);
    }

    private static byte[] CreateBigEndianBytes(int value)
    {
        byte[] buffer = new byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, (short)value);
        return buffer;
    }


}

// Add this to your enum if not present
public static class YubiKeyCapabilitiesExtensions
{
    public const YubiKeyCapabilities None = 0;
}