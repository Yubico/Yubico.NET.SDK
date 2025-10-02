using Yubico.YubiKit.Core.Core.Iso7816;

namespace Yubico.YubiKit.Core.Core.Devices.SmartCard;

/// <summary>
///     Represents an ISO 7816 compliant smart card, visible either through CCID or NFC.
/// </summary>
public interface IPcscDevice : IDevice
{
    /// <summary>
    ///     The "answer to reset" (ATR) for the smart card.
    /// </summary>
    /// <value>
    ///     The ATR.
    /// </value>
    /// <remarks>
    ///     The ATR for a smart card can act as an identifier for the type of card that is inserted.
    /// </remarks>
    AnswerToReset? Atr { get; }

    /// <summary>
    ///     Gets the smart card's connection type.
    /// </summary>
    PscsConnectionKind Kind { get; }
}

public readonly struct PcscDevice : IPcscDevice
{
    public required string ReaderName { get; init; }
    public required AnswerToReset? Atr { get; init; }
    public PscsConnectionKind Kind { get; init; }
}