using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;

var config = YubiKitBenchmarkConfig.Create();
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

internal sealed class YubiKitBenchmarkConfig : ManualConfig
{
    private YubiKitBenchmarkConfig()
    {
        var runId = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        ArtifactsPath = Path.Combine(FindRepoRoot(), "artifacts", "benchmarks", runId);

        AddJob(Job.ShortRun
            .WithRuntime(CoreRuntime.Core10_0)
            .WithId("net10-short"));

        AddColumnProvider(DefaultColumnProviders.Instance);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(ThreadingDiagnoser.Default);
        AddDiagnoser(ExceptionDiagnoser.Default);
        AddDiagnoser(new EventPipeProfiler(EventPipeProfile.CpuSampling));
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(HtmlExporter.Default);
        AddExporter(CsvExporter.Default);
        AddExporter(CsvMeasurementsExporter.Default);
        AddExporter(JsonExporter.Full);
        AddExporter(RPlotExporter.Default);
        AddValidator(JitOptimizationsValidator.FailOnError);
        AddLogger(DefaultConfig.Instance.GetLoggers().ToArray());
    }

    public static IConfig Create() => new YubiKitBenchmarkConfig();

    private static string FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}

public abstract class YubiKeyHardwareBenchmarkBase
{
    protected IYubiKey Device { get; private set; } = null!;

    protected void SetupDevice(ConnectionType requiredConnection)
    {
        Device = FindDevice(requiredConnection).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => YubiKeyManager.Shutdown();

    private static async Task<IYubiKey> FindDevice(ConnectionType requiredConnection)
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true)
            .ConfigureAwait(false);

        return devices.FirstOrDefault(device => device.SupportsConnection(requiredConnection))
            ?? throw new InvalidOperationException(
                $"No connected YubiKey exposes {requiredConnection}. Connect the YubiKey 5.8 device before running benchmarks.");
    }
}

[MemoryDiagnoser]
[ThreadingDiagnoser]
[ExceptionDiagnoser]
public class YubiKeyDiscoveryBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        _ = YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true)
            .GetAwaiter()
            .GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => YubiKeyManager.Shutdown();

    [Benchmark(Baseline = true)]
    public async Task<int> CachedFindAll()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: false)
            .ConfigureAwait(false);
        return devices.Count;
    }

    [Benchmark]
    public async Task<int> ForcedRescanFindAll()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true)
            .ConfigureAwait(false);
        return devices.Count;
    }

    [Benchmark]
    public async Task<int> ColdFindAll()
    {
        await YubiKeyManager.ShutdownAsync().ConfigureAwait(false);
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: false)
            .ConfigureAwait(false);
        return devices.Count;
    }
}

[MemoryDiagnoser]
[ThreadingDiagnoser]
[ExceptionDiagnoser]
public class SmartCardManagementBenchmarks : YubiKeyHardwareBenchmarkBase
{
    [GlobalSetup]
    public void Setup() => SetupDevice(ConnectionType.SmartCard);

    [Benchmark(Baseline = true)]
    public async Task<bool> ConnectSmartCard()
    {
        await using var connection = await Device.ConnectAsync<ISmartCardConnection>().ConfigureAwait(false);
        return connection.SupportsExtendedApdu();
    }

    [Benchmark]
    public async Task<int> SelectManagementOverSmartCard()
    {
        var connection = await Device.ConnectAsync<ISmartCardConnection>().ConfigureAwait(false);
        using var protocol = PcscProtocolFactory<ISmartCardConnection>.Create().Create(connection);
        var response = await protocol.SelectAsync(ApplicationIds.Management).ConfigureAwait(false);
        return response.Length;
    }

    [Benchmark]
    public async Task<int> GetDeviceInfoOverSmartCard()
    {
        await using var session = await Device.CreateManagementSessionAsync(
                preferredConnection: ConnectionType.SmartCard)
            .ConfigureAwait(false);

        var info = await session.GetDeviceInfoAsync().ConfigureAwait(false);
        return info.SerialNumber ?? 0;
    }
}

[MemoryDiagnoser]
[ThreadingDiagnoser]
[ExceptionDiagnoser]
public class FidoHidManagementBenchmarks : YubiKeyHardwareBenchmarkBase
{
    [GlobalSetup]
    public void Setup() => SetupDevice(ConnectionType.HidFido);

    [Benchmark(Baseline = true)]
    public async Task<int> ConnectFidoHid()
    {
        await using var connection = await Device.ConnectAsync<IFidoHidConnection>().ConfigureAwait(false);
        return connection.PacketSize;
    }

    [Benchmark]
    public async Task<int> CreateManagementSessionOverFidoHid()
    {
        await using var session = await Device.CreateManagementSessionAsync(
                preferredConnection: ConnectionType.HidFido)
            .ConfigureAwait(false);

        return session.FirmwareVersion.Major;
    }

    [Benchmark]
    public async Task<int> GetDeviceInfoOverFidoHid()
    {
        await using var session = await Device.CreateManagementSessionAsync(
                preferredConnection: ConnectionType.HidFido)
            .ConfigureAwait(false);

        var info = await session.GetDeviceInfoAsync().ConfigureAwait(false);
        return info.SerialNumber ?? 0;
    }
}

[MemoryDiagnoser]
[ThreadingDiagnoser]
[ExceptionDiagnoser]
public class OtpHidManagementBenchmarks : YubiKeyHardwareBenchmarkBase
{
    [GlobalSetup]
    public void Setup() => SetupDevice(ConnectionType.HidOtp);

    [Benchmark(Baseline = true)]
    public async Task<int> ConnectOtpHid()
    {
        await using var connection = await Device.ConnectAsync<IOtpHidConnection>().ConfigureAwait(false);
        return connection.FeatureReportSize;
    }

    [Benchmark]
    public async Task<int> CreateManagementSessionOverOtpHid()
    {
        await using var session = await Device.CreateManagementSessionAsync(
                preferredConnection: ConnectionType.HidOtp)
            .ConfigureAwait(false);

        return session.FirmwareVersion.Major;
    }

    [Benchmark]
    public async Task<int> GetDeviceInfoOverOtpHid()
    {
        await using var session = await Device.CreateManagementSessionAsync(
                preferredConnection: ConnectionType.HidOtp)
            .ConfigureAwait(false);

        var info = await session.GetDeviceInfoAsync().ConfigureAwait(false);
        return info.SerialNumber ?? 0;
    }
}
