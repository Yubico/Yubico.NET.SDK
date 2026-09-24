using System.Security.Cryptography;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;

internal static class ExpertFeatureScenario
{
    internal static async Task<int> RunAsync(int serial)
    {
        try
        {
            IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync();
            IYubiKey[] selected = devices.Where(d => d.SerialNumber == serial &&
                d.SupportsConnection(ConnectionType.HidOtp)).ToArray();
            IReadOnlyList<IHidDevice> hidDevices = await FindHidDevices.Create().FindAllAsync();
            IHidDevice[] otpInterfaces = hidDevices.Where(d => d.InterfaceType == HidInterfaceType.Otp &&
                d.DescriptorInfo.UsagePage == 1 && d.DescriptorInfo.Usage == 6).ToArray();
            if (devices.Count != 1 || selected.Length != 1 || otpInterfaces.Length != 1 ||
                hidDevices.Count(d => d.InterfaceType == HidInterfaceType.Otp) != 1)
            {
                Console.Error.WriteLine($"BLOCKED: serial={serial} discovered {devices.Count} keys, matched {selected.Length} OTP keys, {otpInterfaces.Length} keyboard OTP interfaces; cannot uniquely associate interface");
                return 2;
            }

            Console.WriteLine($"SELECTED serial={serial} id={selected[0].DeviceId} interface={otpInterfaces[0].ReaderName} usagePage=1 usage=6");
            for (int cycle = 1; cycle <= 3; cycle++)
            {
                await using IHidConnection native = otpInterfaces[0].ConnectToFeatureReports();
                if (native.GetType().Name != "MacOSHidFeatureReportConnection")
                    throw new InvalidOperationException("Expert feature route did not use the built-in macOS facade");
                byte[] report = native.GetReport();
                if (report.Length != 8)
                    throw new InvalidOperationException($"Expected 8-byte feature GET, got {report.Length}");
                Console.WriteLine($"PHASE_EXPERT_FEATURE_GET serial={serial} cycle={cycle} length={report.Length}");

                await using var session = await ManagementSession.CreateAsync(new ExpertFeatureConnection(native),
                    new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
                if (session.ConnectionType != ConnectionType.HidOtp)
                    throw new InvalidOperationException("Management selected a different transport");
                DeviceInfo info = await session.GetDeviceInfoAsync();
                if (info.SerialNumber != serial)
                    throw new InvalidOperationException("Expert feature device-info serial does not match selected serial");
                Console.WriteLine($"PHASE_EXPERT_FEATURE_INFO serial={serial} cycle={cycle} transport=HidOtp");
            }

            Console.WriteLine($"PASS mode=expert-feature serial={serial} cycles=3 featureGet=3 deviceInfo=3");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL mode=expert-feature serial={serial}: {ex}");
            return 1;
        }
        finally
        {
            await YubiKeyManager.ShutdownAsync();
        }
    }

    // The session borrows the public native connection; the outer scope owns its disposal.
    private sealed class ExpertFeatureConnection(IHidConnection native) : IOtpHidConnection
    {
        public ConnectionType Type => ConnectionType.HidOtp;
        public int FeatureReportSize => 8;

        public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (report.Length != FeatureReportSize)
                throw new ArgumentException("Expected 8-byte OTP report", nameof(report));
            byte[] copy = new byte[FeatureReportSize];
            report.CopyTo(copy);
            try { native.SetReport(copy); }
            finally { CryptographicOperations.ZeroMemory(copy); }
            return Task.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] report = native.GetReport();
            if (report.Length != FeatureReportSize)
                throw new InvalidOperationException($"Expected 8-byte OTP report, got {report.Length}");
            return Task.FromResult<ReadOnlyMemory<byte>>(report);
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
