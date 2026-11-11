using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

internal static class PcscConnectionKindDetector
{
    public static PscsConnectionKind Detect(AnswerToReset? atr)
    {
        if (atr is null || atr.Bytes.Length <= 1)
        {
            return PscsConnectionKind.Unknown;
        }

        return (atr.Bytes[1] & 0xF0) == 0xF0
            ? PscsConnectionKind.Usb
            : PscsConnectionKind.Nfc;
    }
}