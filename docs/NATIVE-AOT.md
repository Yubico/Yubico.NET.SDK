# Native AOT Support

> **Scope:** this document defines the official Native AOT support contract for the **v2 SDK
> libraries only** — `Core`, `Management`, `Piv`, `Fido2`, `WebAuthn`, `Oath`, `OpenPgp`,
> `SecurityDomain`, `YubiOtp`, and `YubiHsm`. CLI tools (`Cli.Shared`, `Cli.Commands`, and the
> per-module example tools under `src/<Module>/examples/`) and all test projects are explicitly
> **out of scope** and are not built, published, or verified as Native AOT compatible.

## Support statement

**Cross-platform Native AOT support.**

All 10 in-scope SDK libraries are published with Native AOT compatibility metadata
(`IsAotCompatible=true`), are analyzer-clean for AOT/trim/single-file warnings, and are
runtime-verified under real Native AOT publishes on **macOS (`osx-arm64`)**, **Windows
(`win-x64`)**, and **Linux (`linux-x64`)** against physical YubiKey hardware. All three platforms
cover Core device discovery through their platform-specific native-loading paths. macOS additionally
covers `Management` (`DeviceInfo`) and a full `Piv` session over the APDU pipeline. See
[`docs/research/native-aot-readiness.md`](research/native-aot-readiness.md) and
[`docs/research/native-aot-readiness-data.md`](research/native-aot-readiness-data.md) for the
underlying evidence.

The SDK libraries are analyzer-clean for AOT on every target platform, and the
`Yubico.NativeShims` P/Invoke surface (the only native interop dependency) uses static P/Invoke
declarations. The cross-platform verification closes the platform-specific publish and basic
hardware-discovery gap; broader protocol runtime coverage remains intentionally tracked separately
(see [Platform matrix](#platform-matrix) below).

## What "Native AOT compatible" means for this SDK

Each in-scope SDK library project sets, via a centralized MSBuild property model (see
[Project configuration](#project-configuration)):

| Property | Value | Effect |
|---|---|---|
| `IsAotCompatible` | `true` | Declares the assembly AOT-compatible in its NuGet metadata; also cascades `IsTrimmable`, `EnableAotAnalyzer`, `EnableTrimAnalyzer`, and `EnableSingleFileAnalyzer` to `true`. |
| `VerifyReferenceAotCompatibility` | `true` | Fails the build if any referenced assembly (direct or transitive) is not itself marked AOT-compatible, unless explicitly suppressed. |

This means:

- Every public and internal code path in the 10 SDK libraries is analyzer-checked for
  `RequiresDynamicCode`/`RequiresUnreferencedCode` violations (reflection-based APIs, runtime
  code generation, etc.) on every build, not just when a consumer happens to publish with
  `PublishAot=true`.
- A regression that introduces a real AOT/trim violation fails the build immediately as an error
  (analyzer warnings are treated as errors project-wide), not silently at a consumer's publish
  time. This was confirmed by injecting synthetic `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]`
  members into `Core` and observing the analyzer correctly flag them as build errors.
- CLI and test projects are unaffected — they do not opt in to this property block and their
  `IsAotCompatible`/`VerifyReferenceAotCompatibility` properties remain unset.

## Platform matrix

| Platform | RID | Analyzer-clean | CI publish gate | Runtime-verified (hardware) | Phase |
|---|---|---|---|---|---|
| macOS (Apple Silicon) | `osx-arm64` | ✅ | ✅ (`.github/workflows/native-aot.yml`) | ✅ (discovery, Management, PIV, **device monitoring**) | 1 |
| Windows x64 | `win-x64` | ✅ | ⬜ manual verification | ✅ (2 physical YubiKeys; discovery) | 2 complete |
| Linux x64 | `linux-x64` | ✅ | ⬜ manual verification | ✅ (2 physical YubiKeys; discovery) | 2 complete |

"Analyzer-clean" means the build succeeds with `IsAotCompatible=true` and
`VerifyReferenceAotCompatibility=true` on that platform's target runtime with zero warnings and
**no suppressions**. This is platform-independent — the same
IL is analyzed regardless of host OS — but is listed per-platform for completeness since actual
Native AOT compilation (`PublishAot=true`) is inherently platform-specific (ILC cross-compilation
is not supported; you must publish on the target OS).

The verification host forces ILC to process entry types from all 10 SDK libraries on every RID.
Only Core discovery is runtime-exercised on all three platforms. Management and PIV have deeper
runtime coverage on macOS; Fido2, WebAuthn, Oath, OpenPgp, SecurityDomain, YubiOtp, and YubiHsm
remain link-verified rather than protocol-runtime-verified under Native AOT.

**Device monitoring is runtime-verified on macOS.** The hot-plug path — multicast fan-out to both
the `IObservable` and `IAsyncEnumerable` surfaces, composite-key merging, event coalescing,
Added/Removed correlation, and shutdown — is exercised against physical hardware by
`verification/NativeAotVerification --monitor`, an operator-driven protocol. This matters because
the recurring CI gate runs with zero keys attached and therefore cannot observe any of it. See
Experiment 8 in [`native-aot-readiness-data.md`](research/native-aot-readiness-data.md).

Windows and Linux have verified discovery but have not run the `--monitor` protocol. That is a
general coverage gap rather than a platform risk for the event pipeline specifically: the
broadcaster and stream are pure managed code — a copy-on-write array, a `Lock`, and a bounded
`Channel`, with no platform conditionals, no P/Invoke and no platform detection — so their behaviour
does not vary by OS. What *is* platform-specific (PC/SC and HID enumeration, native listener
cadence) sits below this layer in `FindYubiKeys` and `YubiKeyDeviceMonitorService`, which are
unmodified and already verified on all three RIDs. Running `--monitor` on Windows and Linux is
therefore worthwhile for broad SDK confidence, but it is not a precondition for trusting the event
pipeline on those platforms.

## Known constraints and governed exceptions

### No dependency-level exceptions remain

Reference verification is enforced for real: every dependency of every in-scope SDK library
carries the `IsAotCompatible` assembly metadata, and there is **no `IL3058` suppression anywhere in
the build**. A new `PackageReference` that lacks the metadata will fail the build under
`TreatWarningsAsErrors`, which is the intended behaviour.

> **History — do not reintroduce.** `Core` previously depended on `System.Reactive` for
> `YubiKeyManager.DeviceChanges`, and it was the SDK's only dependency-level AOT finding. Rx does
> not carry `IsAotCompatible` — and structurally cannot yet: the attribute was introduced in
> .NET 10, while Rx 7.0.0 (the current release) targets at most `net8.0`. Because
> `VerifyReferenceAotCompatibility` walks each project's full transitive closure, so suppressing
> `IL3058` would have required a blanket central `NoWarn` across all ten modules. That would also
> hide the same warning for every future non-AOT-safe dependency. The suppression was rejected and
> the dependency was removed first: `DeviceEventBroadcaster` (multicast) and `DeviceEventStream`
> (buffering) replace Rx's `Subject<T>`.
> If you are tempted to add a `NoWarn` to accommodate a new package, you would be trading the
> guarantee for the whole SDK to accommodate one dependency.

Consumers are unaffected in signature terms — `DeviceChanges` is still `IObservable<DeviceEvent>`,
a BCL type. Consumers who want Rx operators (`Where`, `Take`, `ObserveOn`, or the
`Subscribe(Action<T>)` overload) simply reference `System.Reactive` themselves; those are extension
methods on `IObservable<T>` and compose with the SDK unchanged. This is guarded by `RxInteropTests`.

### `Yubico.NativeShims` — safe by construction

`Yubico.NativeShims` (the native interop package `Core` depends on for PC/SC and OpenSSL/ARKG
primitives) is consumed exclusively via **static P/Invoke** (`[DllImport]`/`[LibraryImport]` on a
statically-linked/loaded native library), which is the most AOT-friendly native interop pattern —
no `dlopen`/`Marshal.GetDelegateForFunctionPointer<T>` generic-delegate resolution is involved for
this dependency. This is distinct from `Core`'s *other*, separate dynamic-loading path
(`UnmanagedDynamicLibrary` → `dlopen`/`dlsym` → `Marshal.GetDelegateForFunctionPointer`) used for
platform PC/SC discovery, which was itself runtime-verified under Native AOT on all three supported
platforms (see the readiness data doc, Experiments 3, 5, and 6).

The OpenSSL ARKG P/Invoke surface (`ArkgPrimitivesOpenSsl.cs`, 12 declarations) is
runtime-verified end-to-end under Native AOT. `CmacPrimitivesOpenSsl.cs` (5 declarations) is dead
code — never instantiated by any default provider — and needs no AOT verification.

### CLI and test projects are intentionally excluded

`Cli.Shared`, `Cli.Commands`, and the example tools do not set `IsAotCompatible` and are not part
of the Native AOT publish gate. Unit and integration test projects are similarly excluded — they
are not shipped and are not part of the SDK's Native AOT support surface. (`Tests.TestProject` is
unrelated: it targets `net10.0` with `PublishAot=true` for its own xUnit v3/Microsoft Testing
Platform test-host reasons, not as part of this SDK support contract.)

## Deployment guidance for consumers

When you publish your own application with `PublishAot=true` and reference one or more of the 10
in-scope SDK libraries:

- **Run from the publish output directory, not the build output directory.** Native AOT
  publishing produces a self-contained native executable alongside any native shared libraries
  (e.g., the `Yubico.NativeShims` native binary) that must sit next to it at the path the runtime
  resolves at startup. Copying only the managed DLL (pre-AOT build output) or omitting the native
  shim from your deployment layout will surface as a `DllNotFoundException` at first use, not at
  publish time.
- **Publish per-RID.** Native AOT compilation is platform- and architecture-specific — publish
  separately for each target RID (`osx-arm64`, `win-x64`, `linux-x64`, etc.) rather than expecting
  a single cross-platform AOT binary.
- **`InvariantGlobalization`** is safe to enable for most consumers of this SDK, since none of the
  in-scope modules depend on culture-specific formatting for protocol data; enable it if you want
  a smaller Native AOT binary and don't otherwise need full ICU globalization data.

## Project configuration

The AOT property model lives in two files, in this specific split (required by MSBuild import
order — see the inline comments in both files for the full rationale):

- **`Directory.Build.props`** — declares the `IsYubiKitSdkLibrary` opt-in property, defaulting to
  `false`. This file is imported *before* each project's own `<PropertyGroup>`, so it can only
  declare a default here, not react to a per-project override.
- **`Directory.Build.targets`** — contains the actual conditional block
  (`Condition="'$(IsYubiKitSdkLibrary)' == 'true'"`) that sets `IsAotCompatible`,
  `VerifyReferenceAotCompatibility`. This file is
  imported *after* each project's own body, so it correctly sees the `IsYubiKitSdkLibrary=true`
  that each of the 10 SDK library `.csproj` files sets.

Each in-scope SDK library `.csproj` opts in with a single line:

```xml
<IsYubiKitSdkLibrary>true</IsYubiKitSdkLibrary>
```

## CI verification

[`.github/workflows/native-aot.yml`](../.github/workflows/native-aot.yml) runs a publish-only gate
(no test framework involved) that:

1. Runs `dotnet publish -p:PublishAot=true` against
   [`verification/NativeAotVerification`](../verification/NativeAotVerification) — an internal,
   non-packable console host (not a shipped CLI tool) that references all 10 in-scope SDK
   libraries and forces the ILC (IL Compiler) to process each module's public entry-point type.
2. Executes the resulting native binary, which performs real device discovery via
   `YubiKeyManager.FindAllAsync` (this succeeds safely with zero YubiKeys attached, returning an
   empty list, which is the expected condition on CI runners).

Currently this recurring gate runs on `macos-latest`/`osx-arm64` only. Windows and Linux have been
verified manually against physical hardware; adding `windows-latest`/`win-x64` and
`ubuntu-latest`/`linux-x64` would convert those one-time results into recurring CI coverage.

## Related documentation

- [`docs/research/native-aot-readiness.md`](research/native-aot-readiness.md) — the original
  readiness assessment (analyzer sweep + hardware-backed publish/run experiments) that this
  support contract is built on.
- [`docs/research/native-aot-readiness-data.md`](research/native-aot-readiness-data.md) — the raw
  experiment log and evidence.
