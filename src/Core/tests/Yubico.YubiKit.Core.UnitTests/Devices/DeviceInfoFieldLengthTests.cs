using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class DeviceInfoFieldLengthTests
{
    public static TheoryData<int, int> InvalidFixedFieldLengths
    {
        get
        {
            var cases = new TheoryData<int, int>();
            (int Tag, int Length)[] fields =
            [
                (0x02, 4), (0x04, 1), (0x05, 3), (0x06, 2), (0x07, 1),
                (0x08, 1), (0x0A, 1), (0x16, 1), (0x17, 1), (0x20, 3), (0x21, 3)
            ];
            foreach (var (tag, length) in fields)
            {
                for (var actualLength = 0; actualLength < length; ++actualLength)
                    cases.Add(tag, actualLength);
                cases.Add(tag, length + 1);
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(InvalidFixedFieldLengths))]
    public void CreateFromTlvs_MalformedFixedField_ReportsTagAndLength(int tag, int length)
    {
        using var tlvs = DecodeFixture(tag, new byte[length]);

        var ex = Assert.Throws<BadResponseException>(() => DeviceInfo.CreateFromTlvs(tlvs, null));

        Assert.Contains($"tag 0x{tag:X2}", ex.Message);
        Assert.Contains($"length {length}", ex.Message);
    }

    [Theory]
    [InlineData(0x01)]
    [InlineData(0x03)]
    [InlineData(0x0D)]
    [InlineData(0x0E)]
    [InlineData(0x14)]
    [InlineData(0x15)]
    [InlineData(0x18)]
    public void CreateFromTlvs_EmptyCapabilityField_IsNotTreatedAsAbsent(int tag)
    {
        using var tlvs = DecodeFixture(tag, []);

        var ex = Assert.Throws<BadResponseException>(() => DeviceInfo.CreateFromTlvs(tlvs, null));

        Assert.Contains($"tag 0x{tag:X2}", ex.Message);
        Assert.Contains("length 0", ex.Message);
    }

    [Theory]
    [InlineData(0x01, 0)]
    [InlineData(0x01, 1)]
    [InlineData(0x01, 2)]
    [InlineData(0x01, 4)]
    [InlineData(0x02, 0)]
    [InlineData(0x02, 2)]
    [InlineData(0x03, 0)]
    public void CreateFromTlvs_MalformedQualifierChild_ReportsChildTagAndLength(int tag, int length)
    {
        var qualifier = EncodeQualifier(tag, new byte[length]);
        using var tlvs = DecodeFixture(0x19, qualifier.Span);

        var ex = Assert.Throws<BadResponseException>(() => DeviceInfo.CreateFromTlvs(tlvs, null));

        Assert.Contains($"tag 0x{tag:X2}", ex.Message);
        Assert.Contains($"length {length}", ex.Message);
    }

    [Theory]
    [InlineData(0x01)]
    [InlineData(0x02)]
    [InlineData(0x03)]
    public void CreateFromTlvs_MissingQualifierChild_ReportsMissingTag(int tag)
    {
        var qualifier = EncodeQualifier(tag, [], omit: true);
        using var tlvs = DecodeFixture(0x19, qualifier.Span);

        var ex = Assert.Throws<BadResponseException>(() => DeviceInfo.CreateFromTlvs(tlvs, null));

        Assert.Contains($"tag 0x{tag:X2}", ex.Message);
        Assert.Contains("Missing", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void CreateFromTlvs_MalformedQualifierEncoding_ReportsContainerWithoutPayload(int length)
    {
        byte[] invalid = [0x01, 0x03, 0xDE];
        using var tlvs = DecodeFixture(0x19, invalid.AsSpan(0, length));

        var ex = Assert.Throws<BadResponseException>(() => DeviceInfo.CreateFromTlvs(tlvs, null));

        Assert.Contains("tag 0x19", ex.Message);
        Assert.Contains($"length {length}", ex.Message);
        Assert.DoesNotContain("DE", ex.Message);
    }

    [Fact]
    public void CreateFromTlvs_MalformedFirmwareWithFallback_IsStillRejected()
    {
        using var tlvs = DecodeFixture(0x05, [5, 7]);

        var ex = Assert.Throws<BadResponseException>(() =>
            DeviceInfo.CreateFromTlvs(tlvs, new FirmwareVersion(5, 7, 2)));

        Assert.Contains("tag 0x05", ex.Message);
        Assert.Contains("length 2", ex.Message);
    }

    [Fact]
    public void CreateFromTlvs_OptionalOmissions_UseMetadataDefaults()
    {
        using var tlvs = TlvHelper.DecodeList(new byte[] { 0x01, 0x01, 0x01, 0x05, 0x03, 5, 7, 2 }.AsSpan());

        var info = DeviceInfo.CreateFromTlvs(tlvs, null);

        Assert.Equal(new FirmwareVersion(5, 7, 2), info.FirmwareVersion);
        Assert.Equal(VersionQualifierType.Final, info.VersionQualifier.Type);
        Assert.False(info.IsLocked);
        Assert.Equal(FormFactor.Unknown, info.FormFactor);
        Assert.Null(info.SerialNumber);
        Assert.Equal(DeviceCapabilities.Otp, info.UsbSupported);
        Assert.Equal(DeviceCapabilities.None, info.UsbEnabled);
        Assert.Equal(DeviceCapabilities.None, info.NfcSupported);
        Assert.Equal(DeviceCapabilities.None, info.NfcEnabled);
        Assert.Equal(DeviceCapabilities.None, info.ResetBlocked);
        Assert.Equal(DeviceCapabilities.None, info.FipsCapabilities);
        Assert.Equal(DeviceCapabilities.None, info.FipsApproved);
        Assert.False(info.IsNfcRestricted);
        Assert.False(info.HasPinComplexity);
        Assert.Null(info.PartNumber);
        Assert.Null(info.FpsVersion);
        Assert.Null(info.StmVersion);
        Assert.Equal(0, info.AutoEjectTimeout);
        Assert.True(info.ChallengeResponseTimeout.IsEmpty);
        Assert.Equal((DeviceFlags)0, info.DeviceFlags);
    }

    [Fact]
    public void CreateFromTlvs_OmittedFirmware_UsesSuppliedFallback()
    {
        using var tlvs = TlvHelper.DecodeList(new byte[] { 0x01, 0x01, 0x01 }.AsSpan());
        var fallback = new FirmwareVersion(4, 3, 7);

        var info = DeviceInfo.CreateFromTlvs(tlvs, fallback);

        Assert.Equal(fallback, info.FirmwareVersion);
        Assert.Equal(fallback, info.VersionQualifier.FirmwareVersion);
    }

    [Fact]
    public void CreateFromTlvs_OmittedFirmwareWithoutFallback_ReportsMissingTag()
    {
        using var tlvs = TlvHelper.DecodeList(new byte[] { 0x01, 0x01, 0x01 }.AsSpan());

        var ex = Assert.Throws<BadResponseException>(() => DeviceInfo.CreateFromTlvs(tlvs, null));

        Assert.Contains("Missing", ex.Message);
        Assert.Contains("tag 0x05", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public void CreateFromTlvs_ExtensibleQualifierAndUnknownFields_AcceptsIntegerEncoding(int iterationLength)
    {
        var iteration = new byte[iterationLength];
        iteration[^1] = 42;
        var qualifier = EncodeQualifier(0x03, iteration);
        using var tlvs = DecodeFixture(0x19, qualifier.Span);

        var info = DeviceInfo.CreateFromTlvs(tlvs, null);

        Assert.Equal(new FirmwareVersion(6, 2, 1), info.FirmwareVersion);
        Assert.Equal(VersionQualifierType.Alpha, info.VersionQualifier.Type);
        Assert.Equal(42, info.VersionQualifier.Iteration);
        Assert.Contains(tlvs, tlv => tlv.Tag == 0x70 && tlv.Value.Span.SequenceEqual(new byte[] { 0xDE, 0xAD }));
    }

    [Fact]
    public void CreateFromTlvs_ZeroComponentVersions_StayAbsent()
    {
        using var tlvs = DecodeFixture(0x20, [0, 0, 0]);
        using var stm = new Tlv(0x21, [0, 0, 0]);

        var info = DeviceInfo.CreateFromTlvs([.. tlvs, stm], null);

        Assert.Null(info.FpsVersion);
        Assert.Null(info.StmVersion);
    }

    [Fact]
    public void CreateFromTlvs_OptionalFields_DecodeMetadata()
    {
        using var tlvs = DecodeFixture(0x16, [1]);
        using var nfcRestricted = new Tlv(0x17, [1]);
        using var partNumber = new Tlv(0x13, "YK5C"u8);
        using var fps = new Tlv(0x20, [1, 2, 3]);
        using var stm = new Tlv(0x21, [4, 5, 6]);

        var info = DeviceInfo.CreateFromTlvs([.. tlvs, nfcRestricted, partNumber, fps, stm], null);

        Assert.True(info.HasPinComplexity);
        Assert.True(info.IsNfcRestricted);
        Assert.Equal("YK5C", info.PartNumber);
        Assert.Equal(new FirmwareVersion(1, 2, 3), info.FpsVersion);
        Assert.Equal(new FirmwareVersion(4, 5, 6), info.StmVersion);
    }

    [Fact]
    public void CreateFromTlvs_QualifierIteration_UsesFullUnsignedWidth()
    {
        var qualifier = EncodeQualifier(0x03, [0xFF, 0xFF, 0xFF, 0xFF]);
        using var tlvs = DecodeFixture(0x19, qualifier.Span);

        var info = DeviceInfo.CreateFromTlvs(tlvs, null);

        Assert.Equal(uint.MaxValue, info.VersionQualifier.Iteration);
    }

    [Fact]
    public void CreateFromTlvs_UnrepresentableQualifierIteration_ReportsTagAndLength()
    {
        var qualifier = EncodeQualifier(0x03, [1, 0, 0, 0, 0]);
        using var tlvs = DecodeFixture(0x19, qualifier.Span);

        var ex = Assert.Throws<BadResponseException>(() => DeviceInfo.CreateFromTlvs(tlvs, null));

        Assert.Contains("tag 0x03", ex.Message);
        Assert.Contains("length 5", ex.Message);
    }

    [Fact]
    public void CreateFromTlvs_UnknownFlagBits_AreNotRejected()
    {
        using var tlvs = DecodeFixture(0x08, [0xFF]);

        var info = DeviceInfo.CreateFromTlvs(tlvs, null);

        Assert.Equal((DeviceFlags)0xFF, info.DeviceFlags);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(8)]
    public void CreateFromTlvs_ExtensibleCapabilityLength_IsAccepted(int length)
    {
        using var tlvs = DecodeFixture(0x01, new byte[length]);

        var info = DeviceInfo.CreateFromTlvs(tlvs, null);

        Assert.Equal(DeviceCapabilities.None, info.UsbSupported);
    }

    private static DisposableTlvList DecodeFixture(int tag, ReadOnlySpan<byte> value)
    {
        var fields = new List<Tlv>
        {
            new(0x0A, [0]), new(0x04, [1]), new(0x05, [5, 7, 2]),
            new(0x06, [0, 30]), new(0x07, [15]), new(0x08, [0]),
            new(0x01, [0x02, 0x3B]), new(0x70, [0xDE, 0xAD])
        };
        var replaced = fields.FindIndex(tlv => tlv.Tag == tag);
        if (replaced >= 0)
        {
            fields[replaced].Dispose();
            fields.RemoveAt(replaced);
        }
        fields.Add(new Tlv(tag, value));
        return TlvHelper.DecodeList(TlvHelper.EncodeAndDisposeList([.. fields]));
    }

    private static Memory<byte> EncodeQualifier(int tag, ReadOnlySpan<byte> value, bool omit = false)
    {
        var fields = new List<Tlv>
        {
            new(0x01, [6, 2, 1]), new(0x02, [0]), new(0x03, [0, 0, 0, 42]),
            new(0x70, [0xDE, 0xAD])
        };
        var replaced = fields.FindIndex(tlv => tlv.Tag == tag);
        fields[replaced].Dispose();
        fields.RemoveAt(replaced);
        if (!omit)
            fields.Add(new Tlv(tag, value));
        return TlvHelper.EncodeAndDisposeList([.. fields]);
    }
}