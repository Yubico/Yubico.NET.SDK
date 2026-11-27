using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard;

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

    [Fact]
    public void AnswerToReset_WhenBytesMatch_HashesEquallyAndSupportsHashSetLookup()
    {
        byte[] firstBytes = [0x3B, 0x00];
        byte[] equalBytes = [0x3B, 0x00];
        var first = new AnswerToReset(firstBytes);
        var equal = new AnswerToReset(equalBytes);
        var different = new AnswerToReset([0x3B, 0x01]);
        var set = new HashSet<AnswerToReset> { first };

        Assert.Equal(first, equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.True(set.TryGetValue(equal, out var stored));
        Assert.Same(first, stored);
        Assert.False(set.TryGetValue(different, out _));
    }
}