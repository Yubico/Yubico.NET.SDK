using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

namespace Yubico.YubiKit.Core.SmartCard;

public interface IFindPcscDevices
{
    Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default);
}

public class FindPcscDevices(ILogger<FindPcscDevices> logger) : IFindPcscDevices
{

    public async Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
        await Task.Run(FindAll, cancellationToken).ConfigureAwait(false);


    private IReadOnlyList<IPcscDevice> FindAll()
    {
        logger.LogDebug("Getting list of PC/SC devices");

        uint establishResult;
        SCardContext context;
        try
        {
            establishResult = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out context);
        }
        catch (DllNotFoundException ex)
        {
            logger.LogWarning("PC/SC native library not available, returning no devices: {Message}", ex.Message);
            return [];
        }

        if (establishResult != ErrorCode.SCARD_S_SUCCESS)
        {
            logger.LogWarning("PC/SC service not available (0x{Code:X8}), returning no devices", establishResult);
            return [];
        }

        var result = NativeMethods.SCardListReaders(context, null, out var readerNames);
        if (result != ErrorCode.SCARD_S_SUCCESS || readerNames.Length == 0) return [];

        var readerStates = SCARD_READER_STATE.CreateMany(readerNames);
        result = NativeMethods.SCardGetStatusChange(
            context,
            0,
            readerStates,
            readerStates.Length);

        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new PlatformInteropException($"PC/SC device enumeration failed: SCardGetStatusChange returned error 0x{(uint)result:X8}");

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