using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Core.PlatformInterop.Desktop.SCard;

namespace Yubico.YubiKit.Core.Core.Devices.SmartCard;

public interface IFindPcscDevices
{
    Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default);
}

public class FindPcscDevices(ILogger<FindPcscDevices> logger) : IFindPcscDevices
{
    #region IFindPcscDevices Members

    public async Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
        await Task.Run(FindAll, cancellationToken).ConfigureAwait(false);

    #endregion

    private IReadOnlyList<IPcscDevice> FindAll()
    {
        logger.LogInformation("Getting list of PC/SC devices");

        var result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new InvalidOperationException("Can't establish context with PC/SC service.");

        result = NativeMethods.SCardListReaders(context, null, out var readerNames);
        if (result != ErrorCode.SCARD_S_SUCCESS || readerNames.Length == 0) return [];

        var readerStates = SCARD_READER_STATE.CreateMany(readerNames);
        result = NativeMethods.SCardGetStatusChange(
            context,
            0,
            readerStates,
            readerStates.Length);

        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new InvalidOperationException($"SCardGetStatusChange failed: 0x{result:X8}");

        try
        {
            return
            [
                .. from reader in readerStates
                where (reader.GetEventState() & SCARD_STATE.PRESENT) != 0
                where ProductAtrs.AllYubiKeys.Contains(reader.GetAtr())
                select new PcscDevice
                {
                    ReaderName = reader.GetReaderName(), Atr = reader.GetAtr(), Kind = PscsConnectionKind.Usb
                }
            ];
        }
        finally
        {
            context.Dispose();
        }
    }

    public static FindPcscDevices Create(ILogger<FindPcscDevices>? logger = null) =>
        new(logger ?? NullLogger<FindPcscDevices>.Instance);
}