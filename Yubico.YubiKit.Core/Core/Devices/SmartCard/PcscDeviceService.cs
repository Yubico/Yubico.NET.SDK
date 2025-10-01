using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Core.PlatformInterop.Desktop.SCard;

namespace Yubico.YubiKit.Core.Core.Devices.SmartCard;

public interface IPcscDeviceService
{
    Task<IReadOnlyList<IPcscDevice>> GetAllAsync(CancellationToken cancellationToken = default);
}

public class PcscDeviceService(ILogger<PcscDeviceService> logger) : IPcscDeviceService
{
    #region IPcscDeviceService Members

    public async Task<IReadOnlyList<IPcscDevice>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Task.Run(GetAll, cancellationToken).ConfigureAwait(false);

    #endregion

    private IReadOnlyList<IPcscDevice> GetAll()
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
}