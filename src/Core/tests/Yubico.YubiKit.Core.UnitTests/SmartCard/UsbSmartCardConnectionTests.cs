using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard;

public class UsbSmartCardConnectionTests
{
    [Theory]
    [InlineData(PscsConnectionKind.Nfc, Transport.Nfc)]
    [InlineData(PscsConnectionKind.Usb, Transport.Usb)]
    [InlineData(PscsConnectionKind.Unknown, Transport.Usb)]
    [InlineData(PscsConnectionKind.Any, Transport.Usb)]
    public void Transport_MapsPcscConnectionKind(PscsConnectionKind kind, Transport expectedTransport)
    {
        using var connection = new UsbSmartCardConnection(new FakePcscDevice(kind));

        Assert.Equal(expectedTransport, connection.Transport);
    }

    [Fact]
    public void Transport_WhenPcscDeviceKindIsDefaultAny_FailsClosedToUsb()
    {
        using var connection = new UsbSmartCardConnection(new FakePcscDevice());

        Assert.Equal(Transport.Usb, connection.Transport);
    }

    private sealed class FakePcscDevice(PscsConnectionKind kind = default) : IPcscDevice
    {
        public string ReaderName => "fake";
        public AnswerToReset? Atr => ProductAtrs.YubiKey5Usb;
        public PscsConnectionKind Kind => kind;
    }
}