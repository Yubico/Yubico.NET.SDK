using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard;

public class PcscConnectionKindDetectorTests
{
    [Fact]
    public void Detect_WhenAtrHasUsbHighNibble_ReturnsUsb()
    {
        Assert.Equal(PscsConnectionKind.Usb, PcscConnectionKindDetector.Detect(ProductAtrs.YubiKeyNeoUsb));
        Assert.Equal(PscsConnectionKind.Usb, PcscConnectionKindDetector.Detect(ProductAtrs.YubiKey4Usb));
        Assert.Equal(PscsConnectionKind.Usb, PcscConnectionKindDetector.Detect(ProductAtrs.YubiKey5Usb));
    }

    [Fact]
    public void Detect_WhenAtrDoesNotHaveUsbHighNibble_ReturnsNfc()
    {
        Assert.Equal(PscsConnectionKind.Nfc, PcscConnectionKindDetector.Detect(ProductAtrs.YubiKeyNeoNfc));
        Assert.Equal(PscsConnectionKind.Nfc, PcscConnectionKindDetector.Detect(ProductAtrs.YubiKey5Nfc));
    }

    [Fact]
    public void Detect_WhenAtrIsTooShort_ReturnsUnknown()
    {
        Assert.Equal(PscsConnectionKind.Unknown, PcscConnectionKindDetector.Detect(new AnswerToReset([0x3B])));
    }

    [Fact]
    public void Detect_WhenAtrIsNull_ReturnsUnknown()
    {
        Assert.Equal(PscsConnectionKind.Unknown, PcscConnectionKindDetector.Detect(null));
    }
}