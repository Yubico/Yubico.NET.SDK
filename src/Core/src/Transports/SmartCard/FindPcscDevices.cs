using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

public interface IFindPcscDevices
{
    Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default);
}

public class FindPcscDevices : IFindPcscDevices
{
    /// <summary>
    ///     Message of the <see cref="InvalidOperationException" /> thrown when no discovery worker slot is free.
    /// </summary>
    internal const string WorkerSaturationMessage =
        "PC/SC device enumeration could not start because discovery worker capacity is saturated; retry the scan.";

    private readonly ILogger<FindPcscDevices> _logger;
    private readonly ISCardApi _sCardApi;

    public FindPcscDevices(ILogger<FindPcscDevices> logger)
        : this(logger, NativeSCardApi.Instance)
    {
    }

    internal FindPcscDevices(ILogger<FindPcscDevices> logger, ISCardApi sCardApi)
    {
        _logger = logger;
        _sCardApi = sCardApi;
    }

    public async Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!DiscoveryWorkerAdmission.TryAcquire(out var admission))
            throw new InvalidOperationException(WorkerSaturationMessage);

        Task<IReadOnlyList<IPcscDevice>> worker;
        try
        {
            worker = Task.Factory.StartNew(
                () =>
                {
                    using (admission)
                    {
                        return FindAll();
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
        catch
        {
            admission.Dispose();
            throw;
        }

        return await worker.ConfigureAwait(false);
    }


    private IReadOnlyList<IPcscDevice> FindAll()
    {
        _logger.LogDebug("Getting list of PC/SC devices");

        uint establishResult;
        SCardContext context;
        try
        {
            establishResult = _sCardApi.SCardEstablishContext(SCARD_SCOPE.USER, out context);
        }
        catch (DllNotFoundException ex)
        {
            _logger.LogWarning("PC/SC native library not available, returning no devices: {Message}", ex.Message);
            return [];
        }

        if (establishResult != ErrorCode.SCARD_S_SUCCESS)
        {
            _logger.LogWarning("PC/SC service not available (0x{Code:X8}), returning no devices", establishResult);
            return [];
        }

        var result = _sCardApi.SCardListReaders(context, null, out var readerNames);
        if (result != ErrorCode.SCARD_S_SUCCESS || readerNames.Length == 0) return [];

        try
        {
            // A USB YubiKey's integrated CCID reader disappears with the device and its name encodes a
            // known Yubico PID. Avoid querying its card status: macOS PC/SC can block this zero-timeout call
            // behind an in-flight transaction. Generic/NFC readers still require ATR-based status probing.
            var devices = readerNames
                .Where(IsIntegratedSmartCardYubiKeyReader)
                .Select(readerName => (IPcscDevice)new PcscDevice
                {
                    ReaderName = readerName,
                    Atr = null,
                    Kind = PscsConnectionKind.Usb
                })
                .ToList();
            var readersRequiringStatus = readerNames
                .Where(readerName => !IsIntegratedSmartCardYubiKeyReader(readerName))
                .ToArray();

            if (readersRequiringStatus.Length == 0)
                return devices;

            var readerStates = SCARD_READER_STATE.CreateMany(readersRequiringStatus);
            result = _sCardApi.SCardGetStatusChange(
                context,
                0,
                readerStates,
                readerStates.Length);

            if (result != ErrorCode.SCARD_S_SUCCESS)
                throw new PlatformInteropException($"PC/SC device enumeration failed: SCardGetStatusChange returned error 0x{(uint)result:X8}");

            devices.AddRange(
                from reader in readerStates
                where (reader.GetEventState() & SCARD_STATE.PRESENT) != 0
                let atr = reader.GetAtr()
                where ProductAtrs.AllYubiKeys.Contains(atr)
                select (IPcscDevice)new PcscDevice
                {
                    ReaderName = reader.GetReaderName(),
                    Atr = atr,
                    Kind = PcscConnectionKindDetector.Detect(atr)
                });
            return devices;
        }
        finally
        {
            context.Dispose();
        }
    }

    private static bool IsIntegratedSmartCardYubiKeyReader(string readerName)
    {
        var pid = ReaderNamePidParser.FromReaderName(readerName);
        return pid is { } value
            && ReaderNamePidParser.ExpectedConnectionsForPid(value).SupportsConnection(ConnectionType.SmartCard);
    }

    public static FindPcscDevices Create(ILogger<FindPcscDevices>? logger = null) =>
        new(logger ?? NullLogger<FindPcscDevices>.Instance);
}