using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class DeviceInfoTests
{
    [Fact]
    public void CreateFromTlvs_RequiredTlvs_ParsesDeviceMetadata()
    {
        using var tlvs = CreateRequiredDeviceInfoTlvs();
        var info = DeviceInfo.CreateFromTlvs(tlvs, defaultVersion: null);

        Assert.False(info.IsLocked);
        Assert.False(info.IsFips);
        Assert.False(info.IsSky);
        Assert.Equal(FormFactor.UsbAKeychain, info.FormFactor);
        Assert.Equal(0x01020304, info.SerialNumber);
        Assert.Equal(DeviceCapabilities.Otp | DeviceCapabilities.Piv, info.UsbEnabled);
        Assert.Equal(DeviceCapabilities.All, info.UsbSupported);
        Assert.Equal(DeviceCapabilities.Piv, info.NfcEnabled);
        Assert.Equal(DeviceCapabilities.Piv | DeviceCapabilities.Oath, info.NfcSupported);
        Assert.Equal(DeviceCapabilities.None, info.ResetBlocked);
        Assert.Equal(DeviceCapabilities.Fido2, info.FipsCapabilities);
        Assert.Equal(DeviceCapabilities.Piv, info.FipsApproved);
        Assert.Equal(30, info.AutoEjectTimeout);
        Assert.Equal(new byte[] { 0x0F }, info.ChallengeResponseTimeout.ToArray());
        Assert.Equal(DeviceFlags.RemoteWakeup, info.DeviceFlags);
        Assert.Equal(new FirmwareVersion(5, 7, 2), info.FirmwareVersion);
        Assert.Equal(VersionQualifierType.Final, info.VersionQualifier.Type);
        Assert.Equal("5.7.2", info.VersionName);
    }

    [Fact]
    public void CreateFromTlvs_AlphaVersionQualifier_UsesQualifiedVersionName()
    {
        using var tlvs = CreateRequiredDeviceInfoTlvs([
            new Tlv(0x19,
            [
                0x01, 0x03, 0x06, 0x02, 0x01,
                0x02, 0x01, 0x00,
                0x03, 0x04, 0x00, 0x00, 0x00, 0x2A
            ])
        ]);

        var info = DeviceInfo.CreateFromTlvs(tlvs, new FirmwareVersion(5, 7, 2));

        Assert.Equal(new FirmwareVersion(6, 2, 1), info.FirmwareVersion);
        Assert.Equal(new FirmwareVersion(6, 2, 1), info.VersionQualifier.FirmwareVersion);
        Assert.Equal(VersionQualifierType.Alpha, info.VersionQualifier.Type);
        Assert.Equal(42, info.VersionQualifier.Iteration);
        Assert.Equal("6.2.1.alpha.42", info.VersionName);
    }

    [Fact]
    public void CreateFromTlvs_InvalidVersionQualifierLength_ThrowsBadResponseException()
    {
        using var tlvs = CreateRequiredDeviceInfoTlvs([new Tlv(0x19, [0x01, 0x03, 0x05])]);

        var ex = Assert.Throws<BadResponseException>(() =>
            DeviceInfo.CreateFromTlvs(tlvs, new FirmwareVersion(5, 7, 2)));

        Assert.Contains("tag 0x19", ex.Message);
        Assert.Contains("length 3", ex.Message);
    }

    [Fact]
    public void CreateFromTlvs_VersionQualifierMissingType_ThrowsBadResponseException()
    {
        using var tlvs = CreateRequiredDeviceInfoTlvs([
            new Tlv(0x19,
            [
                0x01, 0x03, 0x05, 0x07, 0x02,
                0x03, 0x04, 0x00, 0x00, 0x00, 0x01,
                0x04, 0x01, 0x00
            ])
        ]);

        var ex = Assert.Throws<BadResponseException>(() =>
            DeviceInfo.CreateFromTlvs(tlvs, new FirmwareVersion(5, 7, 2)));

        Assert.Contains("tag 0x02", ex.Message);
    }

    [Fact]
    public void CreateFromTlvs_InvalidPartNumberUtf8_ReturnsNullPartNumber()
    {
        using var tlvs = CreateRequiredDeviceInfoTlvs([new Tlv(0x13, [0xC3, 0x28])]);

        var info = DeviceInfo.CreateFromTlvs(tlvs, defaultVersion: null);

        Assert.Null(info.PartNumber);
    }

    private static DisposableTlvList CreateRequiredDeviceInfoTlvs(params Tlv[] additionalTlvs) => new(
    [
        new(0x0A, [0x00]),
        new(0x04, [(byte)FormFactor.UsbAKeychain]),
        new(0x18, [0x00]),
        new(0x03, [0x00, 0x11]),
        new(0x01, [0x03, 0x3B]),
        new(0x0E, [0x10]),
        new(0x0D, [0x30]),
        new(0x14, [0x02, 0x00]),
        new(0x15, [0x00, 0x10]),
        new(0x06, [0x00, 0x1E]),
        new(0x07, [0x0F]),
        new(0x08, [(byte)DeviceFlags.RemoteWakeup]),
        new(0x05, [0x05, 0x07, 0x02]),
        new(0x02, [0x01, 0x02, 0x03, 0x04]),
        .. additionalTlvs
    ]);
}