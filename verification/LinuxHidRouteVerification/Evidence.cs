using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinuxHidRouteVerification;

internal sealed record Observation(string Name, double? CallerReturnMs, double? TotalMs, long? AllocatedBytes);
internal sealed record ProbeObservation(string Name, int ReadCalls, int WriteCalls, int PendingAtBarrier,
    int OutputWorker, int CloseWorker, double ReportWriteCallMs);
internal sealed record IdleObservation(string Scope, double WallMs, double CpuMs, int ThreadDelta, long AllocatedBytes);

internal sealed class Evidence(string mode)
{
    public string Mode { get; } = mode;
    public string Baseline { get; } = "3de42bb54c5e8c986364f0f04398d4e85a66a596";
    public string SourceHash { get; } = Environment.GetEnvironmentVariable("LINUX_VERIFY_SOURCEHASH") ?? "UNRECORDED";
    public string Runtime { get; } = RuntimeInformation.FrameworkDescription;
    public string Kernel { get; } = RuntimeInformation.OSDescription;
    public string KernelRelease { get; } = File.ReadAllText("/proc/sys/kernel/osrelease").Trim();
    public bool DynamicCode { get; } = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;
    public List<string> Checks { get; } = [];
    public List<Observation> Measurements { get; } = [];
    public List<ProbeObservation> NativeProbes { get; } = [];
    public List<IdleObservation> Idle { get; } = [];
    public string? Failure { get; set; }
    public string Outcome { get; set; } = "FAILED";

    internal void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Checks.Add(name);
        Console.WriteLine($"PASS check={name}");
    }

    internal async Task<T> MeasureAsync<T>(string name, Func<Task<T>> invoke)
    {
        long allocated = GC.GetTotalAllocatedBytes();
        long start = Stopwatch.GetTimestamp();
        Task<T> operation = invoke();
        double caller = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        T value = await operation;
        Measurements.Add(new(name, caller, Stopwatch.GetElapsedTime(start).TotalMilliseconds,
            GC.GetTotalAllocatedBytes() - allocated));
        return value;
    }

    internal async Task MeasureAsync(string name, Func<Task> invoke) =>
        await MeasureAsync(name, async () => { await invoke(); return 0; });

    internal void Save(string? output)
    {
        string json = JsonSerializer.Serialize(this, EvidenceJson.Default.Evidence);
        Console.WriteLine(json);
        if (output is not null) File.WriteAllText(output, json + Environment.NewLine);
    }
}

[JsonSerializable(typeof(Evidence))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class EvidenceJson : JsonSerializerContext;