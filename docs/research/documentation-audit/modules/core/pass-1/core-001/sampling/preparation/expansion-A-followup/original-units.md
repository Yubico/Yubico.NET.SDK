# Additional A narrative/internal whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read complete units and relevant enclosing context.

## `core-001:unit:f24674b7449e617e`
`docs/migration/v1-to-v2-changelog.md:12–25`; narrative-examples-and-maps; original rank 9.

````markdown
## 2026-11-05 - Independent Find and explicitly owned monitoring

- Updated current guide/map/highlights for `Find…Async(predicate, interfaces, cancellationToken)` with fresh
  per-call evidence, and `CreateMonitorAsync` returning an initialized, disposable `YubiKeyMonitor`.
  Current API: `WatchAsync` supplies atomic `DeviceAdded` replay, `SnapshotComplete`, and live changes;
  consumers build membership projections rather than reading a monitor cache property.
- Documented earlier-prerelease migration away from `ScanAsync`, `forceRescan`, and static monitoring/
  shutdown. Old result filters need All plus local filtering where full composite slots are required.
  Overflow recovery re-enumerates the same monitor rather than pairing independent Find with an event tail.
- Clarified optional last-known metadata versus live Management reads and independent object lifetimes.
  Added targeted two-key read-only hardware coverage; attended removal/reinsertion acceptance remains a
  separate pending pass gate. Historical changelog entries and `.state.yml` are unchanged; this is not a
  claim that the follow-up feature has merged.


````

## `core-001:unit:47befc3dc2cc5dd5`
`docs/migration/v1-to-v2-map.yml:291–302`; narrative-examples-and-maps; original rank 10.

````markdown
  - id: recipe-device-discovery-find-by-transport
    status: assisted
    confidence: high
    migration_kind: behavior
    v1: YubiKeyDevice.FindByTransport(...) / YubiKeyDevice.FindAll()
    v2: YubiKeyManager.FindAllAsync(...)
    evidence:
      - develop:Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyDevice.Static.cs
      - develop:Yubico.YubiKey/examples/SharedSampleCode/YubiKeyOperations/ListYubiKeys.cs
      - yubikit:src/Core/src/Devices/YubiKeyManager.cs
    notes: V2 discovery is async and returns physical IYubiKey instances with explicit ConnectionType availability; review transport filters and cancellation behavior at each call site.
    last_reviewed_commit: 5a82db9bce05addc0385162e9f085adbc2366c5b

````

## `core-001:unit:572af9c8e29c74f9`
`src/Core/src/Devices/README.md:1–4`; narrative-examples-and-maps; original rank 11.

````markdown
# Device discovery and monitoring design

manager watch creates a producer; monitor watch subscribes to an existing one.


````

## `core-001:unit:eea6325dac26e880`
`docs/architecture/physical-device-model.md:129–137`; narrative-examples-and-maps; original rank 12.

````markdown
### Platform Support For HID Discovery

HID interface enumeration (HID FIDO, HID OTP) is implemented on **macOS, Linux, and Windows**. On Windows,
discovery uses ConfigMgr interface metadata and does not need to open report handles just to identify YubiKey
HID interfaces. Opening a HID report connection can still fail with `UnauthorizedAccessException` when Windows
denies access to the interface, for example because another process holds it exclusively or because the
current environment requires the process to run elevated as Administrator. The PC/SC SmartCard path works on
all platforms and is unaffected by HID report-handle access.


````

## `core-001:unit:e90e615ba927962a`
`docs/TESTING.md:279–316`; narrative-examples-and-maps; original rank 13.

````markdown
### ConnectionType Filtering

Use the `ConnectionType` property to require that the bound device exposes a connection type:

```csharp
// One device row; state.ConnectionType may contain multiple flags.
[SkippableTheory]
[WithYubiKey]
public async Task DefaultSelection(YubiKeyTestState state)
{
    var info = await state.Device.GetDeviceInfoAsync();
    Assert.Equal(state.SerialNumber, info.SerialNumber);
}

// One row whose device must expose SmartCard.
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task SmartCardOnly(YubiKeyTestState state)
{
    var info = await state.Device.GetDeviceInfoAsync();
    Assert.Equal(state.SerialNumber, info.SerialNumber);
}

// Three separate rows, one for each requested connection type.
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
[WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
public async Task EachManagementConnection(YubiKeyTestState state)
{
    // This demonstrates independent device-filter rows, not operation-transport coverage.
    var info = await state.Device.GetDeviceInfoAsync();
    Assert.Equal(state.SerialNumber, info.SerialNumber);
}
```

The filter alone does not pin Management to that connection. Pin the session explicitly when the test's meaning depends on the connection used.


````

## `core-001:unit:7b5daf3c140bfbc6`
`docs/migration/v1-to-v2-changelog.md:40–46`; narrative-examples-and-maps; original rank 14.

````markdown
## 2026-06-30 - Initial baseline snapshot

- Created the initial migration guide, mapping seed, and automation state for branch `yubikit` at commit `e348013685d92a6a665cd0b8bd7e8b05850fddd5`.
- Recorded high-confidence package and namespace split guidance from `Yubico.YubiKey.*` and `Yubico.Core` to `Yubico.YubiKit.*` packages.
- Added manual-review guidance for device discovery, transport selection, applet session lifecycle, and raw APDU or low-level command migrations.
- Established automation expectations: PR preview comments for pull requests targeting `yubikit`, post-merge documentation PRs for pushes to `yubikit`, and weekly reconciliation.


````

## `core-001:unit:b1b8f3f0d43abb21`
`src/Core/README.md:250–282`; narrative-examples-and-maps; original rank 15.

````markdown
### Watch for devices being added and removed

```csharp
await foreach (DeviceChange change in YubiKeyManager.WatchAsync())
{
    Console.WriteLine(change switch
    {
        DeviceAdded added => $"Added: serial {added.Device.SerialNumber}",
        DeviceRemoved removed => $"Removed: serial {removed.Device.SerialNumber}",
        DeviceUpdated updated => $"Updated: serial {updated.Device.SerialNumber}",
        SnapshotComplete => "Initial snapshot complete",
        _ => "Unknown change"
    });
}
```

`WatchAsync` yields `DeviceAdded`, `DeviceRemoved`, and `DeviceUpdated` records computed from observed scans,
not raw operating-system notifications. Subscription starts on first enumeration, atomically replaying current
membership followed by `SnapshotComplete` (even for an empty snapshot), then ordered changes. `Revision`
increments once per scan; changes sharing a revision belong to that scan. Metadata updates retain the
originally published device object. Maintain a membership projection from the
stream, or use `FindAllAsync` for independent fresh discovery. This enumeration owns its private monitor;
share `CreateMonitorAsync` when several consumers need one producer.

**v2 prerelease migration:** replace `ScanAsync` with `FindAllAsync(interfaces: ...)`; remove `forceRescan`.
Replace static Start/Stop/Watch/Shutdown calls with an owned monitor and `DisposeAsync`. Old `type` filters
selected full-device results; the new `interfaces` restricts discovery, so retain All plus local filtering
where later operations need composite connection slots. No compatibility aliases retain the old cache behavior.

For two-key read-only validation of the public factory, replay, snapshots, lifetime, and metadata, see the
[targeted discovery hardware checks](../../docs/usage/device-discovery.md#read-only-scoped-discovery-hardware-checks).
Those unattended tests do not satisfy the separate attended removal/reinsertion acceptance gate.


````

## `core-001:unit:13971cb61434ba4c`
`docs/architecture/device-discovery-guarantees.md:449–456`; narrative-examples-and-maps; original rank 16.

````markdown
## Firmware note: CCID is not independently switchable on 5.8.0+

Measured on firmware 5.8.0: FIDO2 and U2F are exposed over CCID as well as HID, so **disabling the
CCID-exclusive applications does not remove the CCID interface** while FIDO remains enabled. The
key continues to enumerate with its CCID interface present. Plan accordingly when reasoning about
reconfiguration: the enabled-capability set determines the USB PID, but the mapping is not one
application per interface.


````

## `core-001:unit:cec556dd8f62b863`
`scripts/toolchain/TestTargets.cs:58–95`; implementation-state-and-internal-contracts; original rank 12.

````csharp


    static ProjectResult RunTestProject(string? testFilter, TestProject project)
    {
        var projectName = ProjectDiscovery.ShortName(project.Path);
        PrintProjectHeader("Testing", projectName);

        string? command;
        try
        {
            command = BuildTestCommand(project, testFilter);
        }
        catch (Exception ex) when (ex is ArgumentException or ToolchainException)
        {
            // A bad --filter, or test discovery that failed: say what was wrong.
            PrintOutcome(Outcome.Failed, ex.Message);
            return new ProjectResult(projectName, Outcome.Failed, ex.Message);
        }

        if (command is null)
        {
            PrintOutcome(Outcome.NoTestsExecuted, "no test matches --filter");
            return new ProjectResult(projectName, Outcome.NoTestsExecuted, "no test matches --filter");
        }

        var run = Exec.Run("dotnet", command, Exec.Display.ErrorsOnly, $"test-{projectName}");
        var counts = TestCounts.Parse(run.Output);
        var failedTests = FailedTest.Parse(run.Output);
        var result = Classify(projectName, run.ExitCode, counts, failedTests) with
        {
            Counts = counts,
            FailedTests = failedTests,
            LogPath = run.LogPath,
        };

        PrintOutcome(result.Outcome, result.Reason);
        return result;
    }
````

## `core-001:unit:d0048ac2b4474b13`
`scripts/toolchain/BuildTargets.cs:109–150`; implementation-state-and-internal-contracts; original rank 13.

````csharp


    // Native AOT support covers the published SDK libraries, which all sit at src/<Module>/src/*.csproj.
    // Deriving the set from that layout rather than a hand-maintained list means a newly added module is
    // held to the contract automatically: ValidateNativeAotContract will name it and fail the build until
    // it carries the IsYubiKitSdkLibrary opt-in, is referenced by the verification host, and is anchored
    // there by an entry type.
    static (string[] SdkProjects, string[] ExcludedProjects, string[] UnclassifiedProjects) DiscoverNativeAotProjects(ToolchainContext ctx)
    {
        // Modules that sit at the shipped-library layout but are internal tooling (IsPackable=false,
        // never shipped), so they stay outside the Native AOT support envelope. This is a closed set,
        // not a "Cli.*" prefix rule: any other Cli.* module appearing at that layout is reported as
        // unclassified rather than silently skipped, so shipping one forces the in/out decision to be
        // made here. The exclusion is deliberately path-based rather than read from the project file:
        // a check that a project can opt itself out of is not a safety net.
        string[] excludedModules = ["Cli.Commands", "Cli.Shared"];

        // The slash count pins the depth and Contains("/src/") pins the middle segment. That second
        // predicate is load-bearing in a non-obvious way: it is the only thing excluding
        // src/Cli/YkTool/Yubico.YubiKit.Cli.YkTool.csproj, which also has three slashes and whose module
        // segment is "Cli", not "Cli.<something>", so neither the depth test nor the excluded-module set
        // would reject it.
        var candidates = Directory
            .GetFiles(Path.Combine(ctx.RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => ProjectDiscovery.RepoRelative(ctx.RepoRoot, path))
            .Where(path => path.Count(character => character == '/') == 3 &&
                           path.StartsWith("src/", StringComparison.Ordinal) &&
                           path.Contains("/src/", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        static string ModuleSegment(string path) => path.Split('/')[1];

        bool IsTooling(string path) => ModuleSegment(path).StartsWith("Cli.", StringComparison.Ordinal);
        bool IsKnownExclusion(string path) =>
            excludedModules.Contains(ModuleSegment(path), StringComparer.Ordinal);

        return (
            SdkProjects: candidates.Where(path => !IsTooling(path)).ToArray(),
            ExcludedProjects: candidates.Where(IsKnownExclusion).ToArray(),
            UnclassifiedProjects: candidates.Where(path => IsTooling(path) && !IsKnownExclusion(path)).ToArray());
    }
````

## `core-001:unit:ac046b142303c33e`
`src/Core/src/ProductAtrs.cs:68–77`; implementation-state-and-internal-contracts; original rank 14.

````csharp


    public static IList<AnswerToReset> AllYubiKeys => new List<AnswerToReset>
    {
        YubiKeyNeoNfc,
        YubiKeyNeoUsb,
        YubiKey4Usb,
        YubiKey5Nfc,
        YubiKey5Usb
    };
````

## `core-001:unit:3ccf06e816f277d2`
`src/Cli.Commands/src/OpenPgp/OpenPgpCertificatesCommands.cs:108–151`; implementation-state-and-internal-contracts; original rank 15.

````csharp


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, CertificatesImportSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var keyRef = ParseKeyRef(settings.Key);

        if (!File.Exists(settings.CertFile))
        {
            OutputHelpers.WriteError($"File not found: {settings.CertFile}");
            return ExitCode.GenericError;
        }

        X509Certificate2 cert;
        try
        {
            cert = X509CertificateLoader.LoadCertificateFromFile(settings.CertFile);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to load certificate: {ex.Message}");
            return ExitCode.GenericError;
        }

        OutputHelpers.WriteKeyValue("Subject", cert.Subject);
        OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
        OutputHelpers.WriteKeyValue("Thumbprint", cert.Thumbprint);

        using var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        if (adminPin is null)
        {
            OutputHelpers.WriteError("Admin PIN is required.");
            return ExitCode.GenericError;
        }

        await session.VerifyAdminAsync(adminPin.Memory);

        await session.PutCertificateAsync(keyRef, cert);

        OutputHelpers.WriteSuccess($"Certificate imported to {FormatKeyRef(keyRef)} slot.");
        return ExitCode.Success;
    }
````

## `core-001:unit:25673509b9d722bf`
`src/Core/src/Abstractions/IDevice.cs:21–22`; implementation-state-and-internal-contracts; original rank 16.

````csharp

    string ReaderName { get; }
````

## `core-001:unit:9c8ab813d3edc094`
`src/Cli.Commands/src/Oath/OathCommands.cs:187–207`; implementation-state-and-internal-contracts; original rank 17.

````csharp


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathResetSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDestructive(
                    "reset the OATH application, destroying all credentials and the access password"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreateOathSessionAsync();
        await session.ResetAsync();

        OutputHelpers.WriteSuccess("OATH application reset.");
        return ExitCode.Success;
    }
````

## `core-001:unit:297e68004991d4a8`
`src/Cli.Commands/src/OpenPgp/OpenPgpInfoCommand.cs:16–109`; implementation-state-and-internal-contracts; original rank 18.

````csharp


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var appData = await session.GetApplicationRelatedDataAsync();

        OutputHelpers.WriteHeader("OpenPGP Application");
        OutputHelpers.WriteKeyValue("AID Version",
            $"{appData.Aid.Version.Major}.{appData.Aid.Version.Minor}");
        OutputHelpers.WriteKeyValue("Manufacturer", $"0x{appData.Aid.Manufacturer:X4}");
        OutputHelpers.WriteKeyValue("Serial Number", appData.Aid.Serial.ToString());

        if (appData.HistoricalBytes.Length > 0)
        {
            OutputHelpers.WriteHex("Historical Bytes", appData.HistoricalBytes.Span);
        }

        var pinStatus = await session.GetPinStatusAsync();

        AnsiConsole.WriteLine();
        OutputHelpers.WriteHeader("PIN Status");
        OutputHelpers.WriteKeyValue("Signature PIN Policy",
            pinStatus.SignaturePinPolicy == PinPolicy.Always
                ? "Verify every signature"
                : "Verify once per session");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Remaining Attempts[/]");
        WriteAttempts("User PIN", pinStatus.AttemptsUser);
        WriteAttempts("Reset Code", pinStatus.AttemptsReset);
        WriteAttempts("Admin PIN", pinStatus.AttemptsAdmin);

        var sigCount = await session.GetSignatureCounterAsync();
        AnsiConsole.WriteLine();
        OutputHelpers.WriteKeyValue("Signature Counter", sigCount.ToString());

        AnsiConsole.WriteLine();
        OutputHelpers.WriteHeader("Keys");

        var disc = appData.Discretionary;
        var table = OutputHelpers.CreateTable("Slot", "Algorithm", "Status", "Fingerprint");

        KeyRef[] slots = [KeyRef.Sig, KeyRef.Dec, KeyRef.Aut, KeyRef.Att];
        foreach (var slot in slots)
        {
            var attrs = GetAlgorithmAttributes(disc, slot);
            var algString = FormatAlgorithm(attrs);
            var fingerprint = GetFingerprint(disc, slot);
            var fpString = fingerprint.Length > 0 ? Convert.ToHexString(fingerprint.Span) : "(none)";

            if (fpString.Length > 40)
            {
                fpString = fpString[..40] + "...";
            }

            var status = fingerprint.Length > 0 ? "[green]Present[/]" : "[grey]Empty[/]";
            table.AddRow(
                Markup.Escape(FormatSlotShort(slot)),
                Markup.Escape(algString),
                status,
                Markup.Escape(fpString));
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        OutputHelpers.WriteHeader("Touch Policies");
        foreach (var slot in slots)
        {
            try
            {
                var uif = await session.GetUifAsync(slot);
                var uifStr = uif switch
                {
                    Uif.Off => "[grey]Off[/]",
                    Uif.On => "[green]On[/]",
                    Uif.Fixed => "[yellow]On (fixed)[/]",
                    Uif.Cached => "[blue]Cached[/]",
                    Uif.CachedFixed => "[yellow]Cached (fixed)[/]",
                    _ => "[grey]Unknown[/]"
                };
                OutputHelpers.WriteKeyValueMarkup(FormatSlotShort(slot), uifStr);
            }
            catch (NotSupportedException)
            {
                OutputHelpers.WriteKeyValueMarkup(FormatSlotShort(slot), "[grey]N/A[/]");
            }
        }

        return ExitCode.Success;
    }
````

## `core-001:unit:ad16310453b46d29`
`scripts/toolchain/Discovery.cs:82–91`; implementation-state-and-internal-contracts; original rank 19.

````csharp


    static bool IsArchivedOrPlanningDoc(string relativePath)
    {
        string[] excludedPrefixes =
            ["docs/archive/", "docs/completed/", "docs/plans/", "docs/research/", "docs/reviews/", "docs/specs/", "docs/templates/"];

        var normalized = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        return excludedPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
````

## `core-001:unit:6e9c74a6b5e82dc5`
`src/Cli.Shared/src/Device/DeviceSelector.cs:25–26`; implementation-state-and-internal-contracts; original rank 20.

````csharp

    private readonly Func<IYubiKey, CancellationToken, Task<DeviceInfo?>> _readInfo;
````

## `core-001:unit:cf3538431462d4ab`
`src/Cli.Commands/src/OpenPgp/OpenPgpKeysCommands.cs:244–301`; implementation-state-and-internal-contracts; original rank 21.

````csharp


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, KeysGenerateSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var keyRef = ParseKeyRef(settings.Key);
        using var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        if (adminPin is null)
        {
            OutputHelpers.WriteError("Admin PIN is required.");
            return ExitCode.GenericError;
        }

        await session.VerifyAdminAsync(adminPin.Memory);

        var alg = settings.Algorithm.ToUpperInvariant();

        await AnsiConsole.Status()
            .StartAsync($"Generating {alg} key in {FormatKeyRef(keyRef)}...", async _ =>
            {
                switch (alg)
                {
                    case "RSA2048":
                        await session.GenerateKeyAsync(keyRef, RsaAttributes.Create(RsaSize.Rsa2048));
                        break;
                    case "RSA3072":
                        await session.GenerateKeyAsync(keyRef, RsaAttributes.Create(RsaSize.Rsa3072));
                        break;
                    case "RSA4096":
                        await session.GenerateKeyAsync(keyRef, RsaAttributes.Create(RsaSize.Rsa4096));
                        break;
                    case "ECCP256":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, KeyType.P256));
                        break;
                    case "ECCP384":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, KeyType.P384));
                        break;
                    case "ECCP521":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, KeyType.P521));
                        break;
                    case "ED25519":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, KeyType.Ed25519));
                        break;
                    case "X25519":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, KeyType.X25519));
                        break;
                    default:
                        throw new ArgumentException(
                            $"Unsupported algorithm: {settings.Algorithm}. " +
                            "Use RSA2048, RSA3072, RSA4096, ECCP256, ECCP384, ECCP521, Ed25519, or X25519.");
                }
            });

        OutputHelpers.WriteSuccess($"{alg} key generated in {FormatKeyRef(keyRef)} slot.");
        return ExitCode.Success;
    }
````

## `core-001:unit:4089aedab94d4e23`
`src/Cli.Commands/src/Otp/OtpCommands.cs:447–499`; implementation-state-and-internal-contracts; original rank 22.

````csharp


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpHotpSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);
        byte[] hmacKey;

        if (settings.Generate)
        {
            hmacKey = RandomNumberGenerator.GetBytes(20);
            OutputHelpers.WriteHex("Generated HMAC key", hmacKey);
        }
        else
        {
            hmacKey = OtpHelpers.RequireHex(settings.Key, "key");
        }

        byte[] accessCode = OtpHelpers.ParseHex(settings.AccessCode, "access-code") ?? [];

        try
        {
            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous(
                        $"program slot {OtpHelpers.FormatSlot(slot)} with HOTP"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync(
                deviceContext.SessionOptions);

            using var config = new HotpSlotConfiguration(hmacKey, settings.Imf ?? 0);

            if (settings.Digits == 8)
            {
                config.Use8Digits();
            }

            await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode);

            OutputHelpers.WriteSuccess($"Slot {OtpHelpers.FormatSlot(slot)} programmed for HOTP.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hmacKey);
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }
````

