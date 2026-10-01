using System.Diagnostics;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;

internal static class SmartCardScenario
{
    internal static async Task<int> RunAsync(int serial)
    {
        try
        {
            IYubiKey? selected = await FindSelectedAsync(serial);
            if (selected is null)
                return 2;

            FirmwareVersion? expectedFirmware = null;
            for (int cycle = 1; cycle <= 3; cycle++)
                expectedFirmware = await ReadCycleAsync(selected, serial, cycle, expectedFirmware);

            Console.WriteLine($"PASS mode=smartcard serial={serial} cycles=3 deviceInfo=3 transactionEnd=3 reopen=2");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL mode=smartcard serial={serial}: {ex}");
            return 1;
        }
        finally
        {
            await YubiKeyManager.ShutdownAsync();
        }
    }

    private static async Task<IYubiKey?> FindSelectedAsync(int serial)
    {
        Console.WriteLine($"PHASE_SMARTCARD_DISCOVERY serial={serial} type=SmartCard");
        IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);
        IYubiKey[] matches = devices.Where(d => d.SerialNumber == serial &&
            d.SupportsConnection(ConnectionType.SmartCard)).ToArray();
        if (matches.Length != 1)
        {
            Console.Error.WriteLine($"BLOCKED: serial={serial} matched {matches.Length} smart-card devices; no target operation attempted");
            return null;
        }

        IYubiKey selected = matches[0];
        Console.WriteLine($"SELECTED serial={serial} id={selected.DeviceId} connections={selected.AvailableConnections}");
        return selected;
    }

    private static async Task<FirmwareVersion> ReadCycleAsync(IYubiKey selected, int serial, int cycle,
        FirmwareVersion? expectedFirmware)
    {
        Console.WriteLine($"PHASE_SMARTCARD_OPEN serial={serial} cycle={cycle}");
        var timer = Stopwatch.StartNew();
        Task<ISmartCardConnection> opening = selected.ConnectAsync<ISmartCardConnection>();
        Console.WriteLine($"PHASE_SMARTCARD_OPEN_INVOKED serial={serial} cycle={cycle} invocationMs={timer.Elapsed.TotalMilliseconds:F3} pending={!opening.IsCompleted}");
        FirmwareVersion firmware;
        await using (ISmartCardConnection connection = await opening)
        {
            Console.WriteLine($"PHASE_SMARTCARD_BEGIN serial={serial} cycle={cycle}");
            IDisposable transaction = await connection.BeginTransactionAsync();
            try
            {
                // Management creation selects the applet; both operations are read-only.
                Console.WriteLine($"PHASE_SMARTCARD_MANAGEMENT_SELECT_READ serial={serial} cycle={cycle}");
                await using ManagementSession session = await ManagementSession.CreateAsync(connection,
                    new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard });
                DeviceInfo info = await session.GetDeviceInfoAsync();
                if (info.SerialNumber != serial ||
                    (expectedFirmware is not null && info.FirmwareVersion != expectedFirmware))
                    throw new InvalidOperationException($"Smart-card info mismatch: selected serial={serial} baselineFirmware={expectedFirmware}; read serial={info.SerialNumber} firmware={info.FirmwareVersion}");
                firmware = info.FirmwareVersion;
                Console.WriteLine($"PHASE_SMARTCARD_INFO serial={info.SerialNumber} firmware={info.FirmwareVersion} cycle={cycle} transport=SmartCard");
            }
            finally
            {
                await ReleaseTransactionAsync(transaction, serial, cycle);
            }
        }
        Console.WriteLine($"PHASE_SMARTCARD_CLOSED serial={serial} cycle={cycle}");
        return firmware;
    }

    private static async Task ReleaseTransactionAsync(IDisposable transaction, int serial, int cycle)
    {
        Console.WriteLine($"PHASE_SMARTCARD_END serial={serial} cycle={cycle}");
        await ((IAsyncDisposable)transaction).DisposeAsync();
    }
}
