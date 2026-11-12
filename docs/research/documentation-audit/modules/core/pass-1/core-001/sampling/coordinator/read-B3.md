PROPOSED SAMPLE — no verdict: core-001:unit:f8412d69f0903d78 / B3 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs:1–43
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.MacOS.IOKitFramework;
using CFNativeMethods = Yubico.YubiKit.Core.Native.MacOS.CoreFoundation.NativeMethods;
using IOKitNativeMethods = Yubico.YubiKit.Core.Native.MacOS.IOKitFramework.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>
///     The IOKit and CoreFoundation calls that govern the <em>lifetime</em> of a macOS HID connection:
///     creating the device, opening and closing it, creating and releasing CoreFoundation objects, and
///     registering or clearing the IOKit callbacks, and OTP feature-report I/O.
/// </summary>
/// <remarks>
///     <para>
///         This is a seam, not an abstraction layer. It exists so the constructor-failure and disposal
///         paths of <see cref="MacOSHidFeatureReportConnection" />,
///         <see cref="MacOSHidIOReportConnection" />, and <see cref="MacOSOtpHidConnection" />
///         can be tested without macOS hardware — the same
///         reason <c>IWindowsHidReportAccess</c> exists on the Windows side. It deliberately mirrors the native calls
///         one-for-one and adds no policy.
///     </para>
///     <para>
///         OTP feature-report I/O and checked close verify that accepted operations drain before the
///         physical claim is released. FIDO report I/O uses a separate bridge; this seam also exposes
///         the existing feature-report and callback calls used by the macOS HID connections.
///     </para>
/// </remarks>
[SupportedOSPlatform("macos")]



PROPOSED SAMPLE — no verdict: core-001:unit:663f44912ef5c3b2 / B3 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs:83–89
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Releases a CoreFoundation object previously created by <see cref="CreateDevice" /> or
    ///     <see cref="CreateRunLoopMode" />.
    /// </summary>
    void ReleaseCFObject(nint cfObject);


PROPOSED SAMPLE — no verdict: core-001:unit:68257c2609ad2cf4 / B3 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    KERN_FAILURE = 5,


PROPOSED SAMPLE — no verdict: core-001:unit:598fb7688002ea28 / B3 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:30–31
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    KERN_MEMORY_ERROR = 10,


PROPOSED SAMPLE — no verdict: core-001:unit:c697deea597fd88d / B3 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:48–49
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    KERN_INVALID_POLICY = 28,


PROPOSED SAMPLE — no verdict: core-001:unit:b2c3553b04c1bb00 / B3 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:53–54
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    KERN_INVALID_LEDGER = 33,


PROPOSED SAMPLE — no verdict: core-001:unit:8cc4e387d857f6a0 / B3 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:59–60
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    KERN_LOCK_UNSTABLE = 39,


PROPOSED SAMPLE — no verdict: core-001:unit:9303921f0cddea50 / B3 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:63–64
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    KERN_RPC_SERVER_TERMINATED = 43,


PROPOSED SAMPLE — no verdict: core-001:unit:38f5dfa3e4d48985 / B3 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:65–66
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    KERN_RPC_CONTINUE_ORPHAN = 45,


PROPOSED SAMPLE — no verdict: core-001:unit:77aebb8c72bc4f8b / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidBoundaryComparison/CurrentProfile.cs:274–325
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<Row> MeasureNormal(IYubiKey device)
    {
        using var process = Process.GetCurrentProcess();
        var watch = Stopwatch.StartNew();
        var allocated = GC.GetTotalAllocatedBytes(false);
        var cpu = process.TotalProcessorTime;
        var threadsBefore = process.Threads.Count;
        double returned, total;
        int threadsAtReturn;
        string connectionType;
        double closeCpu;
        long? closeAllocated;
        double disposal, taskAt;
        TimeSpan closeCpuStart;
        long closeAllocatedStart;
        await using (var connection = await device.ConnectAsync<IFidoHidConnection>())
        {
            connectionType = connection.GetType().FullName ?? "unknown";
            if (connection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Unexpected FIDO connection type");
            await using (var session = await RawCtapHidSession.CreateAsync(connection))
            {
                var invocationStart = watch.Elapsed.TotalMilliseconds;
                var pending = session.SendAndReceiveAsync(0x10, new byte[] { 0x04 });
                returned = watch.Elapsed.TotalMilliseconds - invocationStart;
                threadsAtReturn = process.Threads.Count;
                var response = await pending;
                if (response.IsEmpty || response.Span[0] != 0)
                    throw new InvalidDataException("GetInfo did not succeed");
                taskAt = watch.Elapsed.TotalMilliseconds;
                total = taskAt - invocationStart;
            }
            closeCpuStart = process.TotalProcessorTime;
            closeAllocatedStart = GC.GetTotalAllocatedBytes(false);
            // The session is already disposed: these snapshots isolate connection disposal.
        }
        closeCpu = (process.TotalProcessorTime - closeCpuStart).TotalMilliseconds;
        closeAllocated = AllocationDelta(closeAllocatedStart, GC.GetTotalAllocatedBytes(false));
        disposal = watch.Elapsed.TotalMilliseconds - taskAt;
        var closeAt = watch.Elapsed.TotalMilliseconds;
        await using var opened = await device.ConnectAsync<IFidoHidConnection>();
        var reopen = watch.Elapsed.TotalMilliseconds - closeAt;
        var threadsAfter = process.Threads.Count;
        return new Row("normal", "completed", returned, total, disposal, reopen, null,
            AllocationDelta(allocated, GC.GetTotalAllocatedBytes(false)), threadsAtReturn, threadsAfter,
            (process.TotalProcessorTime - cpu).TotalMilliseconds, null, NativeUnavailable, null, PendingUnavailable,
            CloseCpuMs: closeCpu, CloseAllocatedBytes: closeAllocated, ConnectionTypeName: connectionType,
            DeviceSerial: device.SerialNumber, FixtureFirmwareUnavailableReason: "IYubiKey does not expose firmware version; GetInfo is not parsed",
            ThreadDelta: threadsAfter - threadsBefore);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6a56c4d4ce9e07f2 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj:1–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsYubiKitSdkLibrary>false</IsYubiKitSdkLibrary>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <Compile Remove="obj/**" />
  </ItemGroup>
  <PropertyGroup>
    <BoundaryCorePath Condition="'$(BoundaryCorePath)' == ''">$(BoundarySourceRoot)/src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll</BoundaryCorePath>
    <BoundaryNativeVersion Condition="'$(BoundaryNativeVersion)' == ''">1.18.1-async.8</BoundaryNativeVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Yubico.YubiKit.Core">
      <HintPath>$(BoundaryCorePath)</HintPath>
    </Reference>
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging" />
    <PackageReference Include="Yubico.NativeShims" VersionOverride="$(BoundaryNativeVersion)" />
  </ItemGroup>
  <Target Name="CopyBoundaryCore" AfterTargets="Publish">
    <Copy SourceFiles="$(BoundaryCorePath)" DestinationFiles="$(PublishDir)Yubico.YubiKit.Core.dll" />
  </Target>
</Project>



PROPOSED SAMPLE — no verdict: core-001:unit:c8d2fe15b70e0f86 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidBoundaryComparison/Program.cs:14–15
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private const int TimeoutMs = 10000;


PROPOSED SAMPLE — no verdict: core-001:unit:8a664f6d0d04c1b2 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidBoundaryComparison/Program.cs:304–306
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));


PROPOSED SAMPLE — no verdict: core-001:unit:81efac9d6d487eb2 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidBoundaryComparison/Program.cs:306–307
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static string HashText(string text) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));


PROPOSED SAMPLE — no verdict: core-001:unit:bb7b5bb06936d638 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidBoundaryComparison/Program.cs:314–343
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static void CheckPair(string beforePath, string afterPath)
    {
        using var before = JsonDocument.Parse(File.ReadAllText(beforePath));
        using var after = JsonDocument.Parse(File.ReadAllText(afterPath));
        var b = before.RootElement;
        var a = after.RootElement;
        static string Value(JsonElement element, string key) => element.GetProperty(key).GetString() ?? "";
        if (Value(b, "variant") != "before" || Value(a, "variant") != "after" ||
            Value(b, "sourceCommit") == Value(a, "sourceCommit") ||
            Value(b, "coreSha256") == Value(a, "coreSha256") ||
            Value(b.GetProperty("nativeShims"), "version") != Value(a.GetProperty("nativeShims"), "version") ||
            Value(b.GetProperty("nativeShims"), "packageSha256") != Value(a.GetProperty("nativeShims"), "packageSha256") ||
            Value(b.GetProperty("nativeShims"), "deployedNativeSha256") != Value(a.GetProperty("nativeShims"), "deployedNativeSha256") ||
            Value(b.GetProperty("machine"), "runtime") != Value(a.GetProperty("machine"), "runtime") ||
            Value(b.GetProperty("machine"), "os") != Value(a.GetProperty("machine"), "os") ||
            b.GetProperty("fixtureSerial").GetInt32() != a.GetProperty("fixtureSerial").GetInt32())
            throw new InvalidDataException("Mismatched comparison provenance");
        foreach (var dataset in new[] { b, a })
        {
            var samples = dataset.GetProperty("samples").EnumerateArray().ToArray();
            if (dataset.GetProperty("lifecycleWarmups").GetInt32() != 2 ||
                dataset.GetProperty("lifecycleSamples").GetInt32() != 10 || samples.Length < 3 ||
                samples.Take(samples.Length - 1).Any(s => s.GetProperty("scenario").GetString() != "lifecycle") ||
                samples[^1].GetProperty("scenario").GetString() != "no-input-return")
                throw new InvalidDataException("Incomplete comparison sample structure");
        }
        Console.WriteLine($"Compatible pair: {Path.GetFileName(beforePath)} / {Path.GetFileName(afterPath)} (distinct Core hashes; same native package, asset, runtime, OS, fixture)");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:872e42063988d971 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidBoundaryComparison/Program.cs:343–366
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static void CheckPairSummary(string beforePath, string afterPath)
    {
        CheckPair(beforePath, afterPath);
        using var before = JsonDocument.Parse(File.ReadAllText(beforePath));
        using var after = JsonDocument.Parse(File.ReadAllText(afterPath));
        foreach (var dataset in new[] { before.RootElement, after.RootElement })
        {
            var measured = dataset.GetProperty("samples").EnumerateArray()
                .Skip(dataset.GetProperty("lifecycleWarmups").GetInt32())
                .Take(dataset.GetProperty("lifecycleSamples").GetInt32()).ToArray();
            var completed = measured.Where(s => s.GetProperty("classification").GetString() == "completed").ToArray();
            Console.WriteLine($"{dataset.GetProperty("variant").GetString()}: measured lifecycle {completed.Length}/{measured.Length} completed; no-input {dataset.GetProperty("samples").EnumerateArray().Last().GetProperty("classification").GetString()}");
            foreach (var field in new[] { "invocationReturnMs", "taskTerminalMs", "lifecycleCompleteMs" })
            {
                var values = completed.Select(s => s.GetProperty(field).GetDouble()).Order().ToArray();
                if (values.Length == 0) continue;
                var median = values.Length % 2 == 0 ? (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2 : values[values.Length / 2];
                var p95 = values[(int)Math.Ceiling(values.Length * 0.95) - 1];
                Console.WriteLine($"  {field}: median {median:F2} ms; nearest-rank p95 {p95:F2} ms (descriptive only, n={values.Length})");
            }
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a8935af0aa96ef61 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidInputVerification/Program.cs:32–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [LibraryImport(Library, EntryPoint = "hidinput_test_ack")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void Ack(nint owner);


PROPOSED SAMPLE — no verdict: core-001:unit:5b36bb754879dcba / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidInputVerification/Program.cs:48–49
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal byte[]? Bytes;


PROPOSED SAMPLE — no verdict: core-001:unit:be7c5a145c84bea2 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/AcceptanceScenarios.cs:213–283
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<int> RemovalAsync(int serial)
    {
        var selected = await SelectedAsync(serial, ConnectionType.HidFido);
        IFidoHidConnection connection = await selected.ConnectAsync<IFidoHidConnection>();
        bool observedAbsent = false;
        try
        {
            if (connection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Selected FIDO route is not built-in macOS");
            // No command was sent on this fresh connection; this is a pending native input wait.
            Task<ReadOnlyMemory<byte>> reading = connection.ReceiveAsync();
            if (reading.IsCompleted)
                throw new InvalidOperationException("Raw receive completed before removal checkpoint");
            Console.WriteLine($"READY_UNPLUG serial={serial} utc={DateTimeOffset.UtcNow:O}");
            try
            {
                _ = await reading.WaitAsync(TimeSpan.FromSeconds(60));
                throw new InvalidOperationException("Raw receive returned a report instead of terminal removal");
            }
            catch (InvalidOperationException ex) when (ex.Message == "FIDO input terminated: 1")
            {
                // A disposal-induced terminal is not evidence of physical removal.
                var absent = await YubiKeyManager.FindAllAsync();
                if (absent.Any(d => d.SerialNumber == serial))
                    throw new InvalidOperationException("Selected device was not observed absent after terminal input");
                observedAbsent = true;
                Console.WriteLine($"PHASE_REMOVAL_TERMINAL serial={serial} exception={ex.GetType().Name} utc={DateTimeOffset.UtcNow:O}");
            }
        }
        finally
        {
            // A failed native close retains the claim; never treat that failure as safe reuse.
            await connection.DisposeAsync();
        }
        Console.WriteLine($"PHASE_REMOVAL_DISPOSED serial={serial}");

        // Require an observed absent snapshot before accepting a replacement, not just a cached device.
        if (!observedAbsent)
            throw new InvalidOperationException("Physical absence was not observed");
        Console.WriteLine($"READY_REPLUG serial={serial} utc={DateTimeOffset.UtcNow:O}");

        IYubiKey? replacement = null;
        using var rescan = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (!rescan.IsCancellationRequested)
        {
            var found = await YubiKeyManager.FindAllAsync(cancellationToken: rescan.Token);
            var matches = found.Where(d => d.SerialNumber == serial && d.SupportsConnection(ConnectionType.HidFido)).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException("More than one matching replacement device");
            if (matches.Length == 1)
            {
                replacement = matches[0];
                break;
            }
            await Task.Delay(250, rescan.Token);
        }
        if (replacement is null || ReferenceEquals(replacement, selected))
            throw new InvalidOperationException("Fresh replacement generation was not observed");
        await using (var reopened = await replacement.ConnectAsync<IFidoHidConnection>())
        {
            if (reopened.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Replacement route is not built-in macOS");
            await using var session = await Ctap2Session.CreateAsync(reopened);
            if ((await session.GetInfoAsync()).Versions.Count == 0)
                throw new InvalidOperationException("Replacement getInfo returned no versions");
        }
        Console.WriteLine($"PASS mode=removal serial={serial} terminal=1 nativeDispose=1 freshGeneration=1 getInfo=1");
        return 0;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:bdb6c944a5756859 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/AcceptanceScenarios.cs:289–290
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public Task CancellationTask { get; private set; } = Task.CompletedTask;


PROPOSED SAMPLE — no verdict: core-001:unit:5a01a3b881ea5b76 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/AcceptanceScenarios.cs:318–319
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private long _cancelInvocation;


PROPOSED SAMPLE — no verdict: core-001:unit:c272a9fb00347790 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/AcceptanceScenarios.cs:334–338
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public (long Request, long Terminal, long Resolved, long Reuse) Snapshot() =>
            (Volatile.Read(ref _cancelInvocation), Volatile.Read(ref _terminalCatch),
                Volatile.Read(ref _resolved), Volatile.Read(ref _sameSessionInfo));


PROPOSED SAMPLE — no verdict: core-001:unit:5c2a9d17d0a6d264 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/ExpertFeatureScenario.cs:57–59
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // The session borrows the public native connection; the outer scope owns its disposal.



PROPOSED SAMPLE — no verdict: core-001:unit:290fea93ad2bc0b6 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/ExpertFeatureScenario.cs:75–84
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] report = native.GetReport();
            if (report.Length != FeatureReportSize)
                throw new InvalidOperationException($"Expected 8-byte OTP report, got {report.Length}");
            return Task.FromResult<ReadOnlyMemory<byte>>(report);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:b9573e58de539bae / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/ListenerDrainScenario.cs:109–153
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static async Task<int> RunRemovalAsync(int serial)
    {
        var native = new RecordingListenerNative { HoldRemoval = true };
        MacOSHidDeviceListener? listener = null;
        try
        {
            long? target = await ResolveRemovalTargetAsync(serial);
            if (target is null) return 2;
            long entryId = target.Value;
            native.RemovalTarget = entryId;
            listener = new MacOSHidDeviceListener(native, TimeSpan.FromSeconds(4));
            var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            // The hint is payload-less, so gate on the native wrapper's selected entry: it records the ID
            // immediately before forwarding the selected removal. Hints from unrelated keys do not count.
            listener.Changed = () =>
            {
                if (native.EntryId == entryId) delivered.TrySetResult();
            };
            listener.Start();
            if (listener.Status != DeviceListenerStatus.Started)
                throw new InvalidOperationException($"Listener did not start: {listener.Status}");
            if (!native.Scheduled.Wait(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("Listener did not schedule");
            if (!native.OpenReturned.Wait(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("Listener did not return from manager open");
            if (!native.InitialEntryIds.Contains(entryId))
            {
                Console.Error.WriteLine($"BLOCKED: selected serial={serial} entryId={entryId} absent from listener initial devices");
                return 2;
            }
            return await DrainRemovalAsync(serial, entryId, listener, native, delivered.Task);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL listener removal serial={serial}: {ex}");
            return 1;
        }
        finally
        {
            native.ReleaseCallback.Set();
            CleanupRemovalListener(listener, native);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:befc7b7f5b2de383 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/ListenerDrainScenario.cs:254–255
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private int _first;


PROPOSED SAMPLE — no verdict: core-001:unit:beef240f1f71761d / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/ListenerDrainScenario.cs:256–257
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private int _released;


PROPOSED SAMPLE — no verdict: core-001:unit:6c685970d91a4bff / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/ListenerDrainScenario.cs:258–259
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private int _stopOrder;


PROPOSED SAMPLE — no verdict: core-001:unit:fd8af1cc08c0c3c6 / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/ListenerDrainScenario.cs:268–269
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal readonly ManualResetEventSlim ReleaseCallback = new();


PROPOSED SAMPLE — no verdict: core-001:unit:9a36a7aeb6f4830e / B3 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/ListenerDrainScenario.cs:295–296
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public long[] GetInitialEntryIds(nint manager) => InitialEntryIds = _real.GetInitialEntryIds(manager);


PROPOSED SAMPLE — no verdict: core-001:unit:b0c302f97bbe0fad / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs:63–71
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // OTP needs the return code: exclusive access still establishes an open that must be closed.
    // Apple IOHIDDeviceClass.m sets _opened for both success and kIOReturnExclusiveAccess.
    int OpenDeviceResult(nint device)
    {
        OpenDevice(device);
        return 0;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:2766051dada6b6ce / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs:121–122
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public static readonly IOKitDeviceLifetime Instance = new();


PROPOSED SAMPLE — no verdict: core-001:unit:6861fce129a5a2ee / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs:179–181
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public nint CreateRunLoopMode(string name) => CoreFoundationString.Create(name);


PROPOSED SAMPLE — no verdict: core-001:unit:51d6d7f344f7adb1 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSFidoHidConnection.cs:28–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal);


PROPOSED SAMPLE — no verdict: core-001:unit:47f51cfacf863097 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSFidoHidConnection.cs:37–38
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed unsafe class NativeHidInputBridge : IHidInputBridge
{


PROPOSED SAMPLE — no verdict: core-001:unit:0b8eeb5f60672804 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSFidoHidConnection.cs:64–86
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal void Report(nint data, nuint length)
        {
            try
            {
                if (length is 0 or > MacOSFidoHidConnection.MaxInputReportLength)
                {
                    Terminal(HidInputTerminalReason.Fault);
                    return;
                }
                var copy = new byte[(int)length];
                try
                {
                    Marshal.Copy(data, copy, 0, copy.Length);
                    Deliver(copy);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(copy);
                }
            }
            catch { Terminal(HidInputTerminalReason.Fault); }
        }


PROPOSED SAMPLE — no verdict: core-001:unit:c2658cfe37339eeb / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSFidoHidConnection.cs:240–241
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly long _entryId;


PROPOSED SAMPLE — no verdict: core-001:unit:4d22a8bffe9fe670 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSFidoHidConnection.cs:254–255
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private TaskCompletionSource<ReadOnlyMemory<byte>>? _reader;


PROPOSED SAMPLE — no verdict: core-001:unit:858a29174246303a / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSFidoHidConnection.cs:257–258
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private bool _openAttempted;


PROPOSED SAMPLE — no verdict: core-001:unit:e638b0aaec64484d / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSFidoHidConnection.cs:258–259
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private bool _started;


PROPOSED SAMPLE — no verdict: core-001:unit:c1c79d3db2b973cf / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSFidoHidConnection.cs:480–487
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        private static async Task<ReadOnlyMemory<byte>> AwaitReadAsync(
            Task<ReadOnlyMemory<byte>> task, CancellationTokenRegistration registration)
        {
            try { return await task.ConfigureAwait(false); }
            finally { registration.Dispose(); }
        }


PROPOSED SAMPLE — no verdict: core-001:unit:2fec353019aca6e1 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSFidoHidConnection.cs:602–608
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        private async Task DrainShutdownAsync(Task[] reads)
        {
            await Task.WhenAll(reads).ConfigureAwait(false);
            await _closed.Task.ConfigureAwait(false);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:2dd5d34bf274ae46 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:27–28
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly TimeSpan CheckForChangesWaitTime = TimeSpan.FromMilliseconds(100);


PROPOSED SAMPLE — no verdict: core-001:unit:927379e43c7baa9f / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:34–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private Generation? _generation;


PROPOSED SAMPLE — no verdict: core-001:unit:5c067a5cdaa61349 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:35–36
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private bool _disposed;


PROPOSED SAMPLE — no verdict: core-001:unit:214769af2d1fac51 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:323–324
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public nint RunLoop;


PROPOSED SAMPLE — no verdict: core-001:unit:a9a38fdd04e31b55 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:326–327
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public bool OpenAttempted;


PROPOSED SAMPLE — no verdict: core-001:unit:d3adfb07c2ac4b74 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:349–350
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    void SetDeviceMatching(nint manager, int vendorId);


PROPOSED SAMPLE — no verdict: core-001:unit:fde61d2d34e07c11 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:351–352
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    long GetEntryId(nint device);


PROPOSED SAMPLE — no verdict: core-001:unit:b6ebc2963508d4b8 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:354–355
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    nint RetainCurrentRunLoop();


PROPOSED SAMPLE — no verdict: core-001:unit:1bdbae7c14c6022e / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidDeviceListener.cs:432–433
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<MacOSHidListenerNative>();


PROPOSED SAMPLE — no verdict: core-001:unit:0d4fc0b1ead2d657 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidFeatureReportConnection.cs:12–13
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class MacOSHidFeatureReportConnection : IHidConnection
{


PROPOSED SAMPLE — no verdict: core-001:unit:dd724f79368a79ca / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidFeatureReportConnection.cs:14–16
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public MacOSHidFeatureReportConnection(long entryId) : this(entryId, IOKitDeviceLifetime.Instance) { }


PROPOSED SAMPLE — no verdict: core-001:unit:8501aefe4f069add / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidIOReportConnection.cs:16–17
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly MacOSFidoHidConnection _connection;


PROPOSED SAMPLE — no verdict: core-001:unit:dc5d2dbbbf97c4a3 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSHidInterface.cs:29–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly long _entryId;


PROPOSED SAMPLE — no verdict: core-001:unit:56f503f8264cee44 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSOtpHidConnection.cs:50–51
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal Task<ReadOnlyMemory<byte>> GetReportAsync() => _owner.ReceiveAsync(CancellationToken.None, expert: true);


PROPOSED SAMPLE — no verdict: core-001:unit:d31af590d030d9cd / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSOtpHidConnection.cs:76–77
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private GCHandle _root;


PROPOSED SAMPLE — no verdict: core-001:unit:11fb36bc205db285 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSOtpHidConnection.cs:162–167
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        private bool HasMoreWork()
        {
            lock (_sync) return !_stopping || _work is not null;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:a46ae51a7fd8c746 / B3 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/MacOS/MacOSOtpHidConnection.cs:189–196
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        private static Exception NotifyFailure(Action<Exception>? onFailure, Exception failure)
        {
            try { onFailure?.Invoke(failure); }
            catch (Exception completionFailure) { return new AggregateException(failure, completionFailure); }
            return failure;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:51fddcc95d7f7156 / B3 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/CLAUDE.md:133–147
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Built-in macOS FIDO lifetime

The built-in macOS FIDO report connection opens asynchronously through
`MacOSFidoHidConnection` and its internal `IHidInputBridge`. A persistent native IOKit
input owner copies bounded reports; blocking output and checked shutdown run on the
connection's worker. Acknowledgment and accepted-report drain precede release; a failed
close retains the owner and physical claim rather than allowing unsafe reopen. Terminal
wake and registry/discovery claim transfer belong to this same connection lifetime.
`FidoHidProtocol.Configure` performs local setup only; `ApplicationSession` awaits FIDO
channel initialization. Public lower-level `IHidConnection` compatibility remains
synchronous; the typed macOS OTP path uses a separate worker. Linux has its own FIDO/OTP
owners described below; Windows/custom HID retains the compatibility adapters. See
[retained synchronous compatibility paths](../../docs/architecture/raw-access-tiers.md#retained-synchronous-compatibility-paths)
for public entry points and known limits.




PROPOSED SAMPLE — no verdict: core-001:unit:afc3343865d97e1d / B3 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/CoreFoundation/CoreFoundation.Interop.cs:19–20
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal static partial class NativeMethods
{


PROPOSED SAMPLE — no verdict: core-001:unit:37c554d3522ecd2e / B3 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/HidInput/HidInput.Interop.cs:37–41
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [LibraryImport(CoreLibraries.NativeShims, EntryPoint = "Native_HidInputCancel")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial void HidInputCancel(nint owner);


PROPOSED SAMPLE — no verdict: core-001:unit:8e8e6149ecfdedba / B3 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/HidInput/HidInput.Interop.cs:58–59
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal const int Fault = 4;


PROPOSED SAMPLE — no verdict: core-001:unit:8f93cc403a2ac53e / B3 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:173–185
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 /* Returns retained */

    /*!
        @function IOServiceGetMatchingService
        @abstract Look up a registered IOService object that matches a matching dictionary.
        @discussion This is the preferred method of finding IOService objects currently registered by IOKit (that is, objects that have had their registerService() methods invoked). To find IOService objects that aren't yet registered, use an iterator as created by IORegistryEntryCreateIterator(). IOServiceAddMatchingNotification can also supply this information and install a notification of new IOServices. The matching information used in the matching dictionary may vary depending on the class of service being looked up.
        @param masterPort The master port obtained from IOMasterPort(). Pass kIOMasterPortDefault to look up the default master port.
        @param matching A CF dictionary containing matching information, of which one reference is always consumed by this function (Note prior to the Tiger release there was a small chance that the dictionary might not be released if there was an error attempting to serialize the dictionary). IOKitLib can construct matching dictionaries for common criteria with helper functions such as IOServiceMatching, IOServiceNameMatching, IOBSDNameMatching.
        @result The first service matched is returned on success. The service must be released by the caller.
      */
    [LibraryImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial int IOServiceGetMatchingService(int masterPort, IntPtr matching /* Releases arg */);


PROPOSED SAMPLE — no verdict: core-001:unit:bcd58ec6f480d9d7 / B3 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitHid.Interop.cs:96–115
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 /* OS >= 10.5 */

    /*! @function   IOHIDManagerRegisterDeviceMatchingCallback
        @abstract   Registers a callback to be used a device is enumerated.
        @discussion Only device matching the set criteria will be enumerated.
                    If a dispatch queue is set, this call must occur before activation.
                    Devices provided in the callback will be scheduled with the same
                    runloop/dispatch queue as the IOHIDManagerRef, and should not be
                    rescheduled.
        @param      manager Reference to an IOHIDManagerRef.
        @param      callback Pointer to a callback method of type
                    IOHIDDeviceCallback.
        @param      context Pointer to data to be passed to the callback.
    */
    [LibraryImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial void IOHIDManagerRegisterDeviceMatchingCallback(
        IntPtr manager,
        IntPtr callback, // IOHIDDeviceCallback (void*, IOReturn, void*, IOHIDDeviceRef) -> void
        IntPtr context);


PROPOSED SAMPLE — no verdict: core-001:unit:58c17de2a80aaa55 / B3 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitHid.Interop.cs:115–132
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 /* OS >= 10.5 */

    /*! @function   IOHIDManagerRegisterDeviceRemovalCallback
        @abstract   Registers a callback to be used when any enumerated device is
                    removed.
        @discussion In most cases this occurs when a device is unplugged.
                    If a dispatch queue is set, this call must occur before activation.
        @param      manager Reference to an IOHIDManagerRef.
        @param      callback Pointer to a callback method of type
                    IOHIDDeviceCallback.
        @param      context Pointer to data to be passed to the callback.
    */
    [LibraryImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial void IOHIDManagerRegisterDeviceRemovalCallback(
        IntPtr manager,
        IntPtr callback, // IOHIDDeviceCallback (void*, IOReturn, void*, IOHIDDeviceRef) -> void
        IntPtr context);


PROPOSED SAMPLE — no verdict: core-001:unit:8f380e0a9029c637 / B3 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitHid.Interop.cs:132–151
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 /* OS >= 10.5 */

    /*! @function   IOHIDManagerScheduleWithRunLoop
        @abstract   Schedules HID manager with run loop.
        @discussion Formally associates manager with client's run loop. Scheduling
                    this device with the run loop is necessary before making use of
                    any asynchronous APIs.  This will propagate to current and
                    future devices that are enumerated.
        @param      manager Reference to an IOHIDManager.
        @param      runLoop RunLoop to be used when scheduling any asynchronous
                    activity.
        @param      runLoopMode Run loop mode to be used when scheduling any
                    asynchronous activity.
    */
    [LibraryImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial void IOHIDManagerScheduleWithRunLoop(
        IntPtr manager,
        IntPtr runLoop,
        IntPtr runLoopMode);


PROPOSED SAMPLE — no verdict: core-001:unit:4f3c03787d8628e8 / B3 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitHid.Interop.cs:151–168
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 /* OS >= 10.5 */

    /*! @function   IOHIDManagerUnscheduleFromRunLoop
        @abstract   Unschedules HID manager with run loop.
        @discussion Formally disassociates device with client's run loop. This will
                    propagate to current devices that are enumerated.
        @param      manager Reference to an IOHIDManager.
        @param      runLoop RunLoop to be used when unscheduling any asynchronous
                    activity.
        @param      runLoopMode Run loop mode to be used when unscheduling any
                    asynchronous activity.
    */
    [LibraryImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial void IOHIDManagerUnscheduleFromRunLoop(
        IntPtr manager,
        IntPtr runLoop,
        IntPtr runLoopMode);


PROPOSED SAMPLE — no verdict: core-001:unit:fa67b2ef0b63e729 / B3 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/MacOS/IOKitFramework/IOKitHid.Interop.cs:168–181
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
 /* OS >= 10.5 */

    /*!
        @function   IOHIDDeviceCreate
        @abstract   Creates an element from an io_service_t.
        @discussion The io_service_t passed in this method must reference an object
                    in the kernel of type IOHIDDevice.
        @param      allocator Allocator to be used during creation.
        @param      service Reference to service object in the kernel.
        @result     Returns a new IOHIDDeviceRef.
    */
    [LibraryImport(Libraries.IOKitFramework)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial IntPtr IOHIDDeviceCreate(IntPtr allocator, int service);


PROPOSED SAMPLE — no verdict: core-001:unit:e06e64880354c5ab / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidConnectionLifetimeTests.cs:1–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
///     Constructor and disposal lifetime of the macOS feature-report connection.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:6c8b14a3d805da68 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidConnectionLifetimeTests.cs:57–59
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public List<nint> Released { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:8c66398d0c0e3a14 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidConnectionLifetimeTests.cs:89–97
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void RegisterInputReportCallback(
            nint device,
            byte[] buffer,
            int bufferLength,
            nint callback,
            nint context)
        { }


PROPOSED SAMPLE — no verdict: core-001:unit:b7af6d77a34a17d0 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFeatureCompatibilityTests.cs:96–97
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private void Record(string call) { Calls.Add(call); Threads.Add(Environment.CurrentManagedThreadId); }


PROPOSED SAMPLE — no verdict: core-001:unit:c266fb276b6dd6b3 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFeatureCompatibilityTests.cs:108–114
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int SetFeatureReport(nint device, byte[] buffer)
        {
            Record("set"); SetLength = buffer.Length; InSet.Set();
            if (HoldSet) AllowSet.Wait();
            return 0;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:9bcd2e564aa0699e / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFeatureCompatibilityTests.cs:120–121
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void RegisterInputReportCallback(nint device, byte[] buffer, int length, nint callback, nint context) { }


PROPOSED SAMPLE — no verdict: core-001:unit:0997450c56dcfb2d / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFeatureFacadeTests.cs:8–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [Fact]
    public async Task DescriptorAndExpertLength_ArePreservedWhileTypedOtpRemainsEightBytes()
    {
        var native = new Lifetime();
        using (var expert = new MacOSHidFeatureReportConnection(42, native))
        {
            Assert.Equal(ConnectionType.Hid, expert.Type);
            Assert.Equal(65, expert.InputReportSize);
            Assert.Equal(72, expert.OutputReportSize);
            expert.SetReport(new byte[72]);
            Assert.Equal(72, native.SentLength);
            Assert.Equal(8, expert.GetReport().Length);
        }

        var typed = await MacOSOtpHidConnection.OpenAsync(42, native, TestContext.Current.CancellationToken);
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() => typed.SendAsync(new byte[72], TestContext.Current.CancellationToken));
            Assert.Equal(72, native.SentLength);
        }
        finally { await typed.DisposeAsync(); }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5d65f12b3be49266 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFeatureFacadeTests.cs:140–144
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int SetFeatureReport(nint device, byte[] buffer)
        {
            Record(); SentLength = buffer.Length; InReport.Set(); if (HoldReport) AllowReport.Wait(); return 0;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:96e61791ebd8cf9c / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFeatureFacadeTests.cs:149–150
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void RegisterInputReportCallback(nint device, byte[] buffer, int length, nint callback, nint context) { }


PROPOSED SAMPLE — no verdict: core-001:unit:6fd77f3b1c5816c9 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoReadCancellationTests.cs:87–104
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposalCompletesAdmittedPublicReadBeforeReturning(bool cancellable)
    {
        var bridge = new ControlledBridge();
        var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var read = connection.ReceiveAsync(cancellable ? cancellation.Token : CancellationToken.None);

        await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(read.IsCompleted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => read);
        Assert.Equal(1, bridge.ReleaseCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:53dfaebec8fd2987 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoReadCancellationTests.cs:128–129
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public bool HoldAck;


PROPOSED SAMPLE — no verdict: core-001:unit:bac4e0d807932738 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoReadCancellationTests.cs:150–151
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void Cancel(nint owner) => Interlocked.Increment(ref CancelCount);


PROPOSED SAMPLE — no verdict: core-001:unit:09bed7b2bd549db9 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoRouteLifetimeTests.cs:120–136
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task DisposalWakesReceiveAndWaitsForCancelAcknowledgmentBeforeRelease()
    {
        var bridge = new ControlledBridge { HoldAck = true }; bridge.ReleaseOpen.SetResult();
        var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        var read = connection.ReceiveAsync(TestContext.Current.CancellationToken);
        var disposal = connection.DisposeAsync().AsTask();
        await Assert.ThrowsAnyAsync<Exception>(() => read);
        await bridge.AckEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(disposal.IsCompleted);
        Assert.Equal(0, bridge.ReleaseCount);
        bridge.ReleaseAck.SetResult();
        await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, bridge.ReleaseCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c021d4413b6368fc / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoRouteLifetimeTests.cs:189–201
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task FailedOpenBeforeNativeOwnershipReleasesDeviceAndClaim()
    {
        var bridge = new ControlledBridge { ThrowOnOpen = true }; bridge.ReleaseOpen.SetResult();
        var claim = new RecordingClaim();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken, claim));
        Assert.Equal(1, bridge.ReleaseCount);
        Assert.True(claim.Released);
        Assert.Equal(0, bridge.CloseUnstartedCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7b14a961bb8e9a15 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoRouteLifetimeTests.cs:311–312
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class RecordingClaim : IDisposable
    {


PROPOSED SAMPLE — no verdict: core-001:unit:8a16cadbb9ef1cc6 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoRouteLifetimeTests.cs:318–319
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public readonly TaskCompletionSource OpenEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:d79a6f5b7ba68aba / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoRouteLifetimeTests.cs:319–320
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public readonly TaskCompletionSource ReleaseOpen = new(TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:c8dd39b1de1bd87a / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoRouteLifetimeTests.cs:322–323
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public readonly TaskCompletionSource AckEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:face888566b0b08e / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidFidoRouteLifetimeTests.cs:327–328
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public CancellationTokenSource? CancelAfterKeepalive;


PROPOSED SAMPLE — no verdict: core-001:unit:d429ff5fa192d1d5 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOCompatibilityTests.cs:72–95
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void QueuedInputReport_TransfersTheOwnersOriginalArray()
    {
        var bridge = new Bridge();
        using var connection = new MacOSHidIOReportConnection(1, bridge);
        bridge.Emit(42);

        // The owner queues a fresh array; inspecting that queue before dequeue pins the ownership contract.
        var facadeField = typeof(MacOSHidIOReportConnection).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Missing FIDO connection field");
        var owner = facadeField.GetValue(connection) ?? throw new InvalidOperationException("Missing FIDO connection");
        var ownerField = owner.GetType().GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Missing input owner field");
        var inputOwner = ownerField.GetValue(owner) ?? throw new InvalidOperationException("Missing input owner");
        var queueField = inputOwner.GetType().GetField("_reports", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Missing input queue field");
        var queue = (Queue<byte[]>)(queueField.GetValue(inputOwner) ?? throw new InvalidOperationException("Missing input queue"));
        byte[] owned = queue.Peek();

        Assert.Same(owned, connection.GetReport());
        Assert.Equal((byte)42, owned[0]);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:0a86e492926a3a86 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOCompatibilityTests.cs:171–172
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public TaskCompletionSource ReleaseSend { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:3b28c151ff467096 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOCompatibilityTests.cs:172–173
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public byte ObservedFirstByte;


PROPOSED SAMPLE — no verdict: core-001:unit:b2ecfc8df07e5b7a / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOCompatibilityTests.cs:189–195
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
        {
            _report = report;
            InputReady.SetResult();
            return 2;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:e60607159debe2ce / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOCompatibilityTests.cs:195–196
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int Start(nint owner) => 0;


PROPOSED SAMPLE — no verdict: core-001:unit:e20e6746acbe1eb6 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOCompatibilityTests.cs:203–204
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int Destroy(nint owner) { Interlocked.Increment(ref DestroyCount); return 0; }


PROPOSED SAMPLE — no verdict: core-001:unit:9213753eaf596d93 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOFacadeTests.cs:6–25
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [Fact]
    public void ReportsUseNativeDescriptorSizesAndFixedFidoPackets()
    {
        var bridge = new Bridge { InputSizeValue = 65, OutputSizeValue = 72 };
        using var connection = new MacOSHidIOReportConnection(42, bridge);
        Assert.Equal(65, connection.InputReportSize);
        Assert.Equal(72, connection.OutputReportSize);
        var output = new byte[72]; output[0] = 23;
        connection.SetReport(output);
        Assert.Equal((byte)23, bridge.Output?.Span[0]);
        Assert.Equal(72, bridge.Output?.Length);
        connection.SetReport([]);
        Assert.Equal(0, bridge.Output?.Length);
        var input = new byte[65]; input[1] = 19;
        bridge.Emit(input);
        var received = connection.GetReport();
        Assert.Equal(64, received.Length);
        Assert.Equal((byte)19, received[0]);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:edf39a10a89938ca / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOFacadeTests.cs:35–45
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void DisposalTwiceReleasesOnlyOnce()
    {
        var bridge = new Bridge();
        var connection = new MacOSHidIOReportConnection(42, bridge);
        connection.Dispose();
        connection.Dispose();
        Assert.Equal(1, bridge.ReleaseCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5eec5b0c7e037930 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOFacadeTests.cs:56–57
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public nint CreateDevice(long entryId) => 1;


PROPOSED SAMPLE — no verdict: core-001:unit:475d2d7317d0d9f8 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidIOFacadeTests.cs:62–63
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void ReleaseDevice(nint device) => Interlocked.Increment(ref ReleaseCount);


PROPOSED SAMPLE — no verdict: core-001:unit:b3004d28acdb5408 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidListenerLifetimeTests.cs:242–243
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public bool ThrowOnOpen { get; set; }


PROPOSED SAMPLE — no verdict: core-001:unit:18d9dfbaae9f910d / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSHidListenerLifetimeTests.cs:294–301
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void Unschedule(nint manager, nint loop, nint mode)
        {
            if (FailUnschedule) throw new InvalidOperationException("unschedule failed");
            Unscheduled.Add(manager);
            Cleanup.Add("unschedule");
            Calls.Add("unschedule");
        }


PROPOSED SAMPLE — no verdict: core-001:unit:b911881e7931c49d / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSOtpRecoveryTests.cs:65–81
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public TouchThenDataLifetime()
        {
            ExpectedResponse = [0x03, 0x01, 0x01, 0x02, 0, 0];
            ushort fcs = (ushort)~ChecksumUtils.CalculateCrc(ExpectedResponse.AsSpan(0, 4), 4);
            BinaryPrimitives.WriteUInt16LittleEndian(ExpectedResponse.AsSpan(4), fcs);
            _reads.Enqueue(Status(0)); // initialization
            QueuePrelude();
            _reads.Enqueue([0, 5, 4, 3, 1, 0, 0, OtpConstants.ResponseTimeoutWaitFlag]);
            QueuePrelude();
            byte[] data = new byte[8];
            ExpectedResponse.CopyTo(data, 0);
            data[7] = OtpConstants.ResponsePendingFlag;
            _reads.Enqueue(data);
            _reads.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:68016c9957dcce0f / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSOtpRecoveryTests.cs:81–87
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        private void QueuePrelude()
        {
            _reads.Enqueue(Status(1));
            for (int i = 0; i < 10; i++) _reads.Enqueue(Status(1));
        }


PROPOSED SAMPLE — no verdict: core-001:unit:b6e6f0bfde4e8c82 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSOtpRouteTests.cs:100–131
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task CancelBeforeDispatchDoesNotSubmit_AndCancelDuringNativeWaitDrains()
    {
        var native = new Lifetime();
        using var openCancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        openCancel.Cancel();
        var opening = MacOSOtpHidConnection.OpenAsync(42, native, openCancel.Token);
        native.AllowOpen.Set();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => opening);
        Assert.Empty(native.Calls);

        var connection = await MacOSOtpHidConnection.OpenAsync(42, native, TestContext.Current.CancellationToken);
        using var sendCancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task send = Task.CompletedTask;
        try
        {
            send = connection.SendAsync(new byte[8], sendCancel.Token);
            Assert.True(native.InSet.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            sendCancel.Cancel();
            Assert.False(send.IsCompleted);
            native.AllowSet.Set();
            await send;
            await connection.DisposeAsync();
        }
        finally
        {
            native.AllowSet.Set();
            await DrainAndDisposeAsync(send, connection);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:fc111c86c44a0782 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSOtpRouteTests.cs:131–145
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task CloseFailureRetainsClaimAndSharesDisposalFailure()
    {
        var native = new Lifetime { FailClose = true };
        native.AllowOpen.Set();
        var claim = new Claim();
        var connection = await MacOSOtpHidConnection.OpenAsync(42, native, TestContext.Current.CancellationToken, claim);
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        Assert.False(claim.Disposed);
        Assert.DoesNotContain("release", native.Calls);
        Assert.Single(native.Calls, call => call == "close");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d1153e63b1b0f30f / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSOtpRouteTests.cs:325–326
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void Dispose() => Disposed = true;


PROPOSED SAMPLE — no verdict: core-001:unit:2f91cfd2d58f9bfe / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSOtpRouteTests.cs:331–332
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ManualResetEventSlim InSet { get; } = new(false);


PROPOSED SAMPLE — no verdict: core-001:unit:a9fe1f2c18c24f0e / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSOtpRouteTests.cs:339–340
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public byte[]? LastReport { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:e1f124257be7dd22 / B3 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/MacOSOtpRouteTests.cs:351–356
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public int OpenDeviceResult(nint device)
        {
            Record("open");
            return ExclusiveOpen ? unchecked((int)0xE00002C5) : FailOpen ? -1 : 0;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:37739f3b2014b5d7 / B3 / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidBoundaryComparison/CurrentProfile.cs:18–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    internal sealed record Row(string Scenario, string Classification, double? CallerReturnMs,
        double? OperationCompleteMs, double? DisposalMs, double? ReopenMs, double? IdleMs,
        long? ProcessAllocatedBytes, int? ThreadCountAtReturn, int? ThreadCountAtEnd,
        double? CpuDeltaMs, double? NativeDurationMs, string NativeDurationUnavailableReason,
        int? PendingOrdinaryCount, string PendingOrdinaryCountUnavailableReason, string? ErrorType = null,
        double? IdleCpuMs = null, long? IdleAllocatedBytes = null,
        double? CloseCpuMs = null, long? CloseAllocatedBytes = null,
        string? ConnectionTypeName = null, int? DeviceSerial = null,
        string? FixtureFirmware = null, string? FixtureFirmwareUnavailableReason = "Not recorded",
        int? ThreadDelta = null);


PROPOSED SAMPLE — no verdict: core-001:unit:f42685023c336760 / B3 / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/MacOSHidRouteVerification/ExpertFeatureScenario.cs:9–10
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal static class ExpertFeatureScenario
{
