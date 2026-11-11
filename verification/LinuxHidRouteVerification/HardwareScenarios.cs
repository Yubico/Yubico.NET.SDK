using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Management;

namespace LinuxHidRouteVerification;

internal static class HardwareScenarios
{
    internal static async Task<int> RunAsync(string mode, int serial, Evidence evidence)
    {
        if (mode is "--touch" or "--removal" or "--active-cancel" &&
            Environment.GetEnvironmentVariable("LINUX_VERIFY_OPERATOR_COORDINATED") != "1")
        {
            Console.Error.WriteLine($"BLOCKED: {mode} requires separately coordinated operator actions; no command sent");
            return 2;
        }
        // FindAllAsync owns a fresh one-shot discovery scope; no global manager shutdown is needed.
        IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync();
        foreach (IYubiKey key in devices)
            Console.WriteLine($"FIXTURE serial={key.SerialNumber} id={key.DeviceId} connections={key.AvailableConnections}");
        if (mode == "--list")
            return List(devices, evidence);
        IYubiKey[] matches = [.. devices.Where(device => device.SerialNumber == serial)];
        if (matches.Length != 1)
        {
            Console.Error.WriteLine($"BLOCKED: positive serial={serial} matched {matches.Length} devices; no arbitrary selection");
            return 2;
        }
        IYubiKey selected = matches[0];
        return await DispatchAsync(mode, selected, devices.Count, serial, evidence);
    }

    private static int List(IReadOnlyList<IYubiKey> devices, Evidence evidence)
    {
        Console.WriteLine($"discovered={devices.Count}");
        if (devices.Count == 0) { Console.Error.WriteLine("BLOCKED: no YubiKey discovered"); return 2; }
        evidence.Check(devices.Count > 0, "physical-device-enumeration-nonzero");
        if (devices.Count == 1 && devices[0].SerialNumber is > 0)
            Console.WriteLine($"UNIQUE_SELECTED_SERIAL={devices[0].SerialNumber}");
        return 0;
    }

    private static Task<int> DispatchAsync(string mode, IYubiKey selected, int keyCount, int serial, Evidence evidence) => mode switch
    {
        "--fido" => FidoAsync(selected, evidence),
        "--otp" => OtpAsync(selected, serial, evidence),
        "--raw" => RawAsync(selected, evidence),
        "--expert-io" => ExpertIoAsync(selected, keyCount, evidence),
        "--expert-feature" => ExpertFeatureAsync(selected, keyCount, serial, evidence),
        "--touch" or "--active-cancel" or "--removal" => OperatorScenarios.RunAsync(mode, selected, serial, evidence),
        _ => Task.FromResult(2)
    };

    private static async Task<int> FidoAsync(IYubiKey selected, Evidence evidence)
    {
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            IFidoHidConnection connection = await evidence.MeasureAsync($"fido-open-{cycle}", () => selected.ConnectAsync<IFidoHidConnection>());
            try
            {
                evidence.Check(connection is LinuxFidoHidConnection, $"fido-cycle-{cycle}-production-Linux-owner");
                await BorrowAsync(connection, evidence, $"fido-cycle-{cycle}");
            }
            finally { await evidence.MeasureAsync($"fido-dispose-{cycle}", () => connection.DisposeAsync().AsTask()); }
        }
        await using (IFidoHidConnection borrowed = await selected.ConnectAsync<IFidoHidConnection>())
        {
            await BorrowAsync(borrowed, evidence, "borrowed-session-1");
            await Task.Delay(100);
            await BorrowAsync(borrowed, evidence, "borrowed-session-2-after-idle");
        }
        IFidoHidConnection pending = await selected.ConnectAsync<IFidoHidConnection>();
        try
        {
            Task<ReadOnlyMemory<byte>> reading = pending.ReceiveAsync();
            evidence.Check(!reading.IsCompleted, "physical-raw-receive-pending-before-dispose");
            await evidence.MeasureAsync("physical-pending-read-dispose", () => pending.DisposeAsync().AsTask());
            evidence.Check(reading.IsCompleted, "physical-raw-receive-terminal-before-dispose-return");
            bool terminal = false;
            try { _ = await reading; }
            catch (ObjectDisposedException) { terminal = true; }
            catch (InvalidOperationException) { terminal = true; }
            evidence.Check(terminal, "physical-raw-pending-read-ended-with-disposal");
        }
        finally { await pending.DisposeAsync(); }
        await using IFidoHidConnection reopened = await selected.ConnectAsync<IFidoHidConnection>();
        await BorrowAsync(reopened, evidence, "physical-pending-read-reopen");
        return 0;
    }

    private static async Task BorrowAsync(IFidoHidConnection connection, Evidence evidence, string name)
    {
        Ctap2Session session = await evidence.MeasureAsync($"{name}-init-getinfo", () => Ctap2Session.CreateAsync(connection));
        try
        {
            AuthenticatorInfo info = await evidence.MeasureAsync($"{name}-getinfo", () => session.GetInfoAsync());
            evidence.Check(info.Versions.Count > 0, $"{name}-CTAPHID_INIT-authenticatorGetInfo");
            Console.WriteLine($"FIRMWARE route=LinuxFidoHidConnection session={session.FirmwareVersion} response={info.FirmwareVersion} versions={info.Versions.Count}");
        }
        finally { await evidence.MeasureAsync($"{name}-idle-session-dispose", () => session.DisposeAsync().AsTask()); }
    }

    private static async Task<int> OtpAsync(IYubiKey selected, int serial, Evidence evidence)
    {
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            IOtpHidConnection connection = await evidence.MeasureAsync($"otp-open-{cycle}", () => selected.ConnectAsync<IOtpHidConnection>());
            try
            {
                evidence.Check(connection is LinuxOtpHidConnection, $"otp-cycle-{cycle}-production-Linux-owner");
                ReadOnlyMemory<byte> status = await evidence.MeasureAsync($"otp-feature-get-{cycle}", () => connection.ReceiveAsync());
                evidence.Check(status.Length == 8, $"otp-cycle-{cycle}-typed-feature-GET8");
                await using ManagementSession session = await ManagementSession.CreateAsync(connection,
                    new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
                DeviceInfo info = await evidence.MeasureAsync($"otp-deviceinfo-{cycle}", () => session.GetDeviceInfoAsync());
                evidence.Check(info.SerialNumber == serial && session.ConnectionType == ConnectionType.HidOtp,
                    $"otp-cycle-{cycle}-selected-device-info");
                Console.WriteLine($"FIRMWARE route=LinuxOtpHidConnection firmware={info.FirmwareVersion} serial={info.SerialNumber}");
            }
            finally { await evidence.MeasureAsync($"otp-dispose-{cycle}", () => connection.DisposeAsync().AsTask()); }
        }
        return 0;
    }

    private static async Task<int> RawAsync(IYubiKey selected, Evidence evidence)
    {
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            await using RawCtapHidSession raw = await selected.CreateRawCtapHidSessionAsync();
            await RawInfoAsync(raw, evidence, $"public-raw-selected-getinfo-reopen-{cycle}");
        }
        return 0;
    }

    private static async Task RawInfoAsync(RawCtapHidSession raw, Evidence evidence, string name)
    {
        ReadOnlyMemory<byte> response = await evidence.MeasureAsync(name, () => raw.SendAndReceiveAsync(0x10, new byte[] { 0x04 }));
        evidence.Check(response.Length > 1 && response.Span[0] == 0, name);
    }

    private static async Task<IHidInterface?> InterfaceAsync(int keyCount, HidInterfaceType type)
    {
        IReadOnlyList<IHidInterface> interfaces = await FindHidInterfaces.Create().FindAllAsync();
        IHidInterface[] matches = [.. interfaces.Where(item => item.InterfaceType == type)];
        if (keyCount != 1 || matches.Length != 1)
        {
            Console.Error.WriteLine($"BLOCKED: direct tier requires uniquely associable selected physical key; keys={keyCount} interfaces={matches.Length}");
            return null;
        }
        Console.WriteLine($"DIRECT interface={matches[0].ReaderName} usagePage={matches[0].DescriptorInfo.UsagePage} usage={matches[0].DescriptorInfo.Usage}");
        return matches[0];
    }

    private static async Task<int> ExpertIoAsync(IYubiKey selected, int keyCount, Evidence evidence)
    {
        _ = selected; // Serial was matched before associating the unique public interface.
        IHidInterface? hid = await InterfaceAsync(keyCount, HidInterfaceType.Fido);
        if (hid is null) return 2;
        IHidConnection pending = hid.ConnectToIOReports();
        try
        {
            evidence.Check(pending is LinuxHidIOReportConnection, "direct-IO-production-Linux-facade");
            using var invoked = new ManualResetEventSlim();
            var completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            var reader = new Thread(() =>
            {
                invoked.Set();
                try { completion.SetResult(pending.GetReport()); }
                catch (Exception error) { completion.SetException(error); }
            })
            { IsBackground = true, Name = "Linux direct indefinite GetReport verification" };
            reader.Start();
            if (!invoked.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Reader did not enter");
            await Task.Delay(100);
            evidence.Check(!completion.Task.IsCompleted, "direct-IO-indefinite-read-pending-no-macOS-timeout-assumption");
            await evidence.MeasureAsync("direct-IO-dispose-wake", () => pending.DisposeAsync().AsTask());
            bool terminal = false;
            try { _ = await completion.Task; }
            catch (ObjectDisposedException) { terminal = true; }
            catch (InvalidOperationException) { terminal = true; }
            evidence.Check(terminal && reader.Join(TimeSpan.FromSeconds(5)), "direct-IO-separate-thread-dispose-wakes-GetReport");
        }
        finally { await pending.DisposeAsync(); }
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            await using IHidConnection native = hid.ConnectToIOReports();
            evidence.Check(native.InputReportSize == 64 && native.OutputReportSize == 64,
                $"direct-IO-descriptor-metadata-{cycle}");
            // Deliberately use the production retained synchronous adapter only in this direct tier.
            await using var adapter = new FidoHidConnection(native);
            await using RawCtapHidSession raw = await RawCtapHidSession.CreateAsync(adapter);
            await RawInfoAsync(raw, evidence, $"direct-IO-INIT-getinfo-reopen-{cycle}");
        }
        return 0;
    }

    private static async Task<int> ExpertFeatureAsync(IYubiKey selected, int keyCount, int serial, Evidence evidence)
    {
        _ = selected;
        IHidInterface? hid = await InterfaceAsync(keyCount, HidInterfaceType.Otp);
        if (hid is null) return 2;
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            await using IHidConnection native = hid.ConnectToFeatureReports();
            evidence.Check(native is LinuxHidFeatureReportConnection && native.InputReportSize == 8 && native.OutputReportSize == 8,
                $"direct-feature-Linux-metadata-{cycle}");
            byte[] report = native.GetReport();
            evidence.Check(report.Length == 8, $"direct-feature-GET8-{cycle}");
            await using var adapter = new OtpHidConnection(native);
            await using ManagementSession session = await ManagementSession.CreateAsync(adapter,
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
            DeviceInfo info = await session.GetDeviceInfoAsync();
            evidence.Check(info.SerialNumber == serial, $"direct-feature-selected-info-{cycle}");
            Console.WriteLine($"FIRMWARE route=LinuxHidFeatureReportConnection firmware={info.FirmwareVersion}");
        }
        return 0;
    }
}