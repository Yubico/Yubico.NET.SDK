using Yubico.YubiKit.Core.Core.Iso7816;

namespace Yubico.YubiKit.Core.Core.Devices.SmartCard;

public readonly struct PcscDevice : ISmartCardDevice
{
    public required string ReaderName { get; init; }
    public required AnswerToReset? Atr { get; init; }
    public SmartCardConnectionKind Kind { get; init; }
}
