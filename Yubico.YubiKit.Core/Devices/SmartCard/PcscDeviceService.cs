using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Devices.SmartCard;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

public interface IPcscDeviceService
{
    public Task<IReadOnlyList<IPcscDevice>> GetAllAsync( CancellationToken cancellationToken = default );
    public IReadOnlyList<IPcscDevice> GetAll();
}

public class PcscDeviceService : IPcscDeviceService
{
    private readonly ILogger<PcscDeviceService> _logger;

    public PcscDeviceService(ILogger<PcscDeviceService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<IPcscDevice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(GetAll, cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyList<IPcscDevice> GetAll()
    {
        _logger.LogInformation("Getting list of PC/SC devices");

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
            return [.. from reader in readerStates
                where (reader.GetEventState() & SCARD_STATE.PRESENT) != 0
                where ProductAtrs.AllYubiKeys.Contains(reader.GetAtr())
                select new PcscDevice
                {
                    ReaderName = reader.GetReaderName(), Atr = reader.GetAtr(), Kind = SmartCardConnectionKind.Usb
                }
            ];
        }
        finally
        {
            context.Dispose();
        }
    }
}
