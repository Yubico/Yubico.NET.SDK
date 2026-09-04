# Native AOT Readiness Assessment — Yubico.NET.SDK

**Status:** Historical assessment with follow-up verification. The resulting support contract lives
in [`docs/NATIVE-AOT.md`](../NATIVE-AOT.md). This document preserves the original analysis and
tracks how later implementation and cross-platform evidence closed its principal gaps.

**Assessment date:** 2026-08-26.

**Scope:** SDK libraries only — `Core`, `Management`, `Piv`, `Fido2`, `WebAuthn`, `Oath`,
`OpenPgp`, `SecurityDomain`, `YubiOtp`, `YubiHsm`. CLI tools, example apps, and test
infrastructure are out of scope as deliverables (though `Tests.TestProject` is used below as a
pre-existing AOT probe reference point).

**Methodology:** two passes were run against the actual repository code (no permanent changes were
needed to produce these results):
1. **Analyzer-verified static audit** — temporarily enabling `IsAotCompatible`/
   `VerifyReferenceAotCompatibility` per module and capturing real compiler warnings.
2. **Empirical Native AOT publish + runtime smoke tests** — first through a throwaway console app on
   macOS, then through the committed verification host on Windows and Linux. Every platform ran
   against physical YubiKey hardware.

Full commands and raw output are in `docs/research/native-aot-readiness-data.md`. All temporary
project-file edits used for pass 1 were reverted (`git status --short` clean) before this report
was finalized.

---

## Executive summary

Every in-scope SDK library builds with the Native AOT, trimming, and single-file analyzers enabled.
The audit initially found `IL3058` on the `System.Reactive` dependency; the dependency was removed
before the compatibility properties were enabled permanently. A Native AOT host referencing all
ten libraries published successfully on macOS Apple Silicon, Windows x64, and Linux x64. Core
device discovery ran against physical YubiKeys on all three platforms. The macOS run additionally
covered Management device information, a PIV session, and device monitoring.

| Module | Analyzer-checked | Link-verified | Runtime evidence under Native AOT |
|---|---|---|---|
| Core | yes | yes | Device discovery on macOS, Windows, and Linux; monitoring on macOS |
| Management | yes | yes | Device-information read on macOS |
| Piv | yes | yes | PIV session and APDU exchange on macOS |
| Fido2 | yes | yes | No representative protocol operation recorded |
| WebAuthn | yes | yes | No representative protocol operation recorded |
| Oath | yes | yes | No representative protocol operation recorded |
| OpenPgp | yes | yes | No representative protocol operation recorded |
| SecurityDomain | yes | yes | No representative protocol operation recorded |
| YubiOtp | yes | yes | No representative protocol operation recorded |
| YubiHsm | yes | yes | No representative protocol operation recorded |

**Bottom line recommendation for this phase:** the SDK's own code is in excellent shape for Native
AOT — no reflection-based activation, no dynamic proxies, no `Type.GetType`/`Activator.CreateInstance`,
no JSON reflection serialization, and the one AOT-sensitive native-interop pattern
(`Marshal.GetDelegateForFunctionPointer<TDelegate>` in the platform dynamic-library loaders) turned
out to be statically resolvable and analyzer-clean, contrary to the pre-assessment hypothesis. The
remaining runtime-coverage gaps are narrow and well-defined rather than architectural. The
cross-platform support claim and its exact boundaries are now documented in `docs/NATIVE-AOT.md`.

---

## Current-state findings

### Build & project metadata

- All SDK library projects target `net10.0` by default (`Directory.Build.props:35`).
- All 10 SDK library projects now opt in through `IsYubiKitSdkLibrary=true`; the centralized build
  target sets `IsAotCompatible=true` and `VerifyReferenceAotCompatibility=true`.
- `verification/NativeAotVerification` is the dedicated publish host. `Tests.TestProject` also sets
  `PublishAot` for its test-host configuration but is outside the SDK support surface.
- **Turning on `IsAotCompatible=true` for all 10 modules, with zero code changes, produced zero
  warnings from SDK code** (see `native-aot-readiness-data.md` Experiment 1). This is a
  significant, directly actionable, low-risk finding: the property can be added today without
  triggering the repo's `TreatWarningsAsErrors=true` build gate.

### Dependency surface

| Package | Used by | AOT finding |
|---|---|---|
| `System.Reactive` 6.0.1 | No current SDK library | Historical `IL3058` finding from the audit. The dependency was removed before Native AOT compatibility metadata was enabled permanently. |
| `Yubico.NativeShims` 1.16.1 | Core | **Runtime-verified for its live code paths.** Ships only native binaries (`.dll`/`.dylib`/`.so`) + MSBuild targets, no managed assembly, so nothing for the managed dependency analyzer to check against the package itself. Core reaches it via 23 *live* P/Invoke declarations, all blittable (`int`, `IntPtr`, `byte[]` with explicit length; no strings/structs/delegates) — the AOT-safest interop shape, and a different/lower-risk pattern than the `dlopen`/`dlsym` + `Marshal.GetDelegateForFunctionPointer` dynamic resolution Core uses for OS-level APIs (Cfgmgr32, HidD, IOKit, udev): (1) `SCard.Interop.cs`, 11 `[LibraryImport]` PC/SC functions — runtime-verified via the Experiment 3 hardware probe; (2) `ArkgPrimitivesOpenSsl.cs`, 12 `[DllImport]` OpenSSL EC functions backing ARKG-P256 (`CryptographyProviders.ArkgPrimitivesCreator` default) — **runtime-verified in a follow-up Native AOT publish+run** that called `IsPointOnCurve`, `Derive`, and `ComputeEcdhSharedSecret` with real P-256 test vectors (see Experiment 4 in the data log). A 5-function `CmacPrimitivesOpenSsl.cs` DllImport set also exists but is **dead code** — `CryptographyProviders.CmacPrimitivesCreator` defaults to the built-in `System.Security.Cryptography.AesCmac`, and the OpenSSL CMAC class is referenced only in a comment (`Scp11X963Kdf.cs:222`, "This works in legacy code"). It is unreachable from any production code path and out of scope for AOT risk. |
| `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Logging.Abstractions` | Core, Fido2 | Not flagged by `VerifyReferenceAotCompatibility` in these builds |
| `System.Formats.Cbor` | Fido2 | Not flagged |
| `Spectre.Console(.Cli)` | Cli.Shared/Cli.Commands only | Out of scope (CLI, not a published library) |

Managed reference verification now runs without a suppression. Native-only dependencies require
separate publish and runtime evidence because they do not contain managed compatibility metadata.

### Code-pattern findings (confirmed, not estimated)

- **No `Activator.CreateInstance`, `Type.GetType(string)`, `Assembly.Load`,
  `MakeGenericType`/`MakeGenericMethod`, `Expression<T>` compilation, `DynamicMethod`, or
  `System.Text.Json`/`JsonSerializer` usage anywhere in `src/*/src/**/*.cs`** for any in-scope
  module. This absence of the classic AOT/trim pain points is a strong positive and is corroborated
  by the zero-warning analyzer result.
- **`Marshal.GetDelegateForFunctionPointer<TDelegate>`** appears throughout Core's native loader
  (`UnmanagedDynamicLibrary` and its Windows/macOS/Linux implementations,
  `src/Core/src/Native/{Windows,MacOS,Linux}/*UnmanagedDynamicLibrary.cs`) and is the API most
  likely to trigger `IL3050` under AOT in general. **The analyzer did not flag it here** — every
  `GetFunction<TDelegate>` call site across `Cfgmgr32`, `HidD`, `Kernel32`, `WinSCard`, `SCard`,
  `Udev`, and `Libc` bindings instantiates a concrete, non-generic delegate type, which the AOT
  compiler can resolve statically. This was empirically confirmed, not just inferred, by both the
  analyzer pass and the real ILC-compiled/linked probe binary.
- Core's newer P/Invoke declarations already use `[LibraryImport]` with source-generated
  marshalling (e.g. `Cfgmgr32.Interop.cs`, `HidD.Interop.cs`) rather than `[DllImport]`, which is
  the AOT-recommended pattern and a genuine architectural strength.
- Every applet module's `DependencyInjection.cs` (Management, Fido2, WebAuthn, Oath, OpenPgp,
  SecurityDomain, YubiOtp, YubiHsm) uses the `extension(IServiceCollection)` +
  `TryAddSingleton<TFactory>(SomeSession.CreateAsync)` delegate-registration pattern — no runtime
  type scanning, confirmed AOT-safe by both static analysis and (for Management) the runtime probe.
- `ServiceLocator` (`src/Core/src/ServiceLocator.cs`) is explicitly commented
  `"ONLY FOR TESTING, DONT USE IN PRODUCTION CODE"` and is out of the supported production path
  regardless of AOT status; not counted against any module's rating.

### Probe status

Real, reproducible evidence exists for:
- **Core + Management (Verified):** full device discovery via the native dynamic-library loader
  (`dlopen`/`dlsym` + `Marshal.GetDelegateForFunctionPointer`) and PC/SC enumeration, plus a
  complete `ManagementSession` device-info read (TLV decode), all under a real self-contained
  Native AOT `osx-arm64` binary, against 3 physical YubiKeys.
- **Piv (Verified):** a full ISO 7816-4 APDU session (`PivSession.CreateAsync` +
  `GetSlotMetadataAsync`) run to completion under the same AOT binary.
- **Fido2, WebAuthn, Oath, OpenPgp, SecurityDomain, YubiOtp, YubiHsm (Ready — Static):**
  referenced and successfully compiled/linked into the same AOT binary with zero ILC warnings
  (proving no missing-method/reflection-stub failures at AOT link time), but **not
  runtime-exercised** — no HID connection, SCP handshake, OpenPGP/SecurityDomain/YubiHsm session was
  actually created during the probe run.
- **Core discovery is verified on all three primary desktop RIDs.** Follow-up Native AOT publishes
  on Windows x64 and Linux x64 each ran against 2 physical YubiKeys, closing the platform-specific
  native-loader gap. See Experiments 5 and 6 in the raw evidence log.
- **`Yubico.NativeShims`'s OpenSSL ARKG path is now runtime-verified too (follow-up experiment).**
  A second, standalone AOT publish+run (no hardware needed — this is pure cryptographic math) called
  `ArkgPrimitives.Create()` and exercised `IsPointOnCurve`, `Derive` (the full ARKG-P256 key-blinding
  algorithm: HMAC-KEM encapsulation, `HashToScalar`, `EC_POINT_mul`), and `ComputeEcdhSharedSecret`
  with real P-256 test vectors generated via `ECDiffieHellman`. All 12 `ArkgPrimitivesOpenSsl.cs`
  `[DllImport]` functions executed correctly (see Experiment 4 in the data log). Combined with the
  PC/SC path (verified in Experiment 3), **both live NativeShims interop surfaces in Core are now
  runtime-verified**, not just analyzer-clean. A third P/Invoke surface, `CmacPrimitivesOpenSsl.cs`
  (5 functions), was found during this follow-up to be **dead code** — never instantiated by any
  default provider (`CryptographyProviders.CmacPrimitivesCreator` uses the built-in
  `System.Security.Cryptography.AesCmac` instead) — so it carries no AOT risk and was excluded from
  further verification.
- **Deployment nuance found during this experiment (not an AOT-compatibility issue, but worth
  documenting):** the self-contained AOT binary must be *run from its own publish directory* (or
  otherwise have its native-library search path configured) for `libYubico.NativeShims.dylib` to be
  found via macOS's relative `dlopen` resolution — running it via an absolute/different-cwd path
  without `cd`-ing into the publish folder first produced a `DllNotFoundException` even though the
  `.dylib` was correctly bundled next to the executable. This is standard native-AOT/self-contained
  deployment behavior (same as any bundled native dependency), not specific to NativeShims, but is
  worth calling out explicitly in any packaging guidance for AOT consumers of this SDK.

The evidence establishes analyzer compatibility, cross-platform linking, and the specific runtime
paths listed above. It does not establish runtime behavior for every transport or applet session.

---

## Remaining limits on broader runtime claims

Cross-platform Native AOT publishing and Core hardware discovery are verified. Broader claims that
every SDK protocol path is runtime-verified still depend on:

1. **HID transports are runtime-exercised** — Fido2/WebAuthn (HID FIDO) and YubiOtp (HID OTP) are
   currently only link-verified; a real CTAP/OTP HID exchange under AOT has not been run.
2. **OpenPgp, SecurityDomain, and YubiHsm sessions are runtime-exercised**, not just linked — an
   actual OpenPGP command, an SCP03/SCP11 handshake, and a YubiHSM auth session should each run
   once under AOT to convert their "Ready — Static" rating into "Verified."
3. **CI expands its Native AOT publish + smoke-run matrix** from macOS to all three desktop RIDs so
   the manually verified Windows and Linux results become recurring evidence.

None of the above surfaced an architectural blocker in this assessment — they are gaps in
*breadth of verification*, not known incompatibilities.

---

## Remediation backlog

### P0 — Close the one confirmed compatibility gap

**AOT-B1 — Enable compatibility metadata and analyzers for supported libraries**
- Status: **Closed.** Each supported project opts into the central Native AOT property block.

**AOT-B5 — Resolve the `System.Reactive` IL3058 finding**
- Module: Core
- Status: **Closed — resolved by removal.** The dependency was dropped rather than suppressed.
  `System.Reactive`'s `Subject<DeviceEvent>` is replaced by two internal types:
  `DeviceEventBroadcaster` (multicast) and `DeviceEventStream` (per-consumer buffering). The public
  signature is unchanged — `DeviceChanges` is still `IObservable<DeviceEvent>`, a BCL type — and a
  new `IAsyncEnumerable` surface, `YubiKeyManager.WatchAsync`, gives consumers an ergonomic path
  with no third-party dependency.
- Why removal rather than an upstream fix: Rx cannot carry the metadata yet. The `IsAotCompatible`
  assembly attribute was introduced in .NET 10, and Rx 7.0.0 — the current release — targets at most
  `net8.0`. Verified directly against the packages: `IsAotCompatible` appears zero times in every
  `lib/` and `ref/` assembly of both 6.0.1 and 7.0.0, while the SDK's other dependencies
  (`Microsoft.Extensions.*` 10.0.2, `System.Formats.Cbor` 10.0.2) all carry it.
- Consequence: the proposed blanket `<NoWarn>IL3058</NoWarn>` was rejected, so
  `VerifyReferenceAotCompatibility` is introduced with genuine enforcement across all ten SDK
  libraries rather than being silently disabled for every future dependency.
- Validation: `dotnet toolchain.cs build` is clean with zero `IL3058` and no suppression; the
  current verification run passed 905 Core tests with 3 skipped and all 12 unit-test projects; the
  `osx-arm64` AOT publish emits zero ILC warnings.

### P1 — Broaden empirical verification

**AOT-B3 — Add a permanent Native AOT verification host**
- Status: **Closed.** `verification/NativeAotVerification` references reachable entry types from
  all ten supported libraries and runs Core discovery in its default mode.

**AOT-B4 — Run and record Windows + Linux Native AOT publishes**
- Status: **Closed.** Both RIDs published and ran successfully against 2 physical YubiKeys each;
  see Experiments 5 and 6 in the raw evidence log.
- Command: `dotnet publish <probe> -c Release -r win-x64 --self-contained -p:PublishAot=true` and
  the same for `linux-x64`, matching the platform branches in `SdkPlatformInfo.OperatingSystem`.
- Fix: none — evidence-gathering only. Record warnings/errors per RID in
  `docs/research/native-aot-readiness-data.md`.
- Validation: publish succeeds and, ideally, runs against real hardware or a fake/mocked transport
  on each OS.

**AOT-B9 — Runtime-exercise HID transports and the remaining application sessions under AOT**
- Modules: Fido2, WebAuthn, YubiOtp (HID); OpenPgp, SecurityDomain, YubiHsm (SmartCard, currently
  link-only)
- Status: **Partially closed.** A follow-up standalone AOT probe already runtime-verified Core's
  OpenSSL/ARKG-P256 P/Invoke surface (`ArkgPrimitivesOpenSsl.cs`, the code FIDO2's `ArkgP256`
  depends on) — see Experiment 4 in the data log. Remaining scope: HID transports (Fido2, WebAuthn,
  YubiOtp) and the OpenPgp/SecurityDomain/YubiHsm application sessions are still link-only.
- Fix: extend the permanent probe host (AOT-B3) with one representative call per remaining module —
  a CTAP `GetInfo`, an OTP slot read, an OpenPGP `GetApplicationRelatedData`, an SCP03/SCP11
  handshake, and a YubiHSM session creation — to convert each from "Ready — Static" to
  "Ready — Verified."
- Validation: each call completes without an unhandled exception under the AOT binary.

### P2 — Documentation and process

**AOT-B6 — Correct stale AOT/target-framework documentation**
- Status: **Closed.** TestProject documentation reflects the current target framework and remains
  outside the SDK Native AOT support surface.

**AOT-B10 — Add a recurring Native AOT publish gate**
- Status: **Closed for macOS Apple Silicon.** The recurring workflow publishes and runs the
  verification host without hardware. Windows and Linux remain recorded manual evidence.

**AOT-B11 — `Yubico.NativeShims` cannot be statically linked into an AOT binary (GitHub
[#60](https://github.com/Yubico/Yubico.NET.SDK/issues/60))**
- Modules: `Yubico.NativeShims` (packaging), Core (consumer-side opt-in props)
- Status: **Confirmed present in v2.** Reported against v1 in 2023-10, still open and labelled
  "awaiting yubico action". Reproduced against this branch in Experiment 7 below: a `PublishAot`
  build emits a 2.9 MB native executable **plus a separate 3.7 MB
  `libYubico.NativeShims.dylib`** — the shim is larger than the binary it accompanies.
- Root cause: `Yubico.NativeShims` 1.16.1 ships **only shared libraries** (7 RIDs) and **zero
  static libraries** — no `.a`, no `.lib`. `Yubico.NativeShims/CMakeLists.txt:118` hardcodes
  `add_library(Yubico.NativeShims SHARED)`. Native AOT can only fold native code into the
  executable from a *static* library referenced via `<NativeLibrary>` + `<DirectPInvoke>`; a shared
  library must always be deployed alongside. This is a linking/packaging gap, **not** an AOT
  *compatibility* defect — the shim compiles, links, and runs correctly under AOT on all three
  supported platforms (Experiments 3, 5, 6).
- Why this is more tractable than the original report assumed: the reporter suspected the shim was
  hitting "one of the many limitations of AOT compatibility." It is not. The hard part is already
  solved — **OpenSSL is already statically linked into the shim** (`otool -L` shows no external
  OpenSSL; 23 OpenSSL symbols are exported from the shim itself). The only remaining dependencies
  are OS-provided system libraries (`PCSC.framework` on macOS, `libpcsclite` on Linux,
  `winscard.lib` on Windows), which are exactly what a final static link would resolve against
  anyway.
- Fix:
  1. `Yubico.NativeShims`: add a `STATIC` CMake target alongside the existing `SHARED` one and pack
     the resulting `.a`/`.lib` per RID (e.g. under `runtimes/<rid>/native/static/`).
  2. Core: ship opt-in MSBuild props providing `<DirectPInvoke Include="Yubico.NativeShims" />`,
     `<NativeLibrary Include="…" />`, and the per-platform system link flags.
  3. The 23 `[LibraryImport(Libraries.NativeShims, …)]` declarations need **no source change** —
     `DirectPInvoke` binds them at link time.
- Note on the second half of #60: the reporter also flagged the absence of trimming warnings from
  NativeShims as suspicious. That silence is correct — the package contains no managed assembly, so
  there is nothing for the managed analyzer to inspect (see Experiment 2). Separately, the
  analyzer enablement the reporter asked for is now in place on all 10 SDK libraries
  (`IsAotCompatible` + `VerifyReferenceAotCompatibility`), so that half of #60 is already satisfied.
- **The fix is proven to work (Experiment 9).** Building the shim as a static library requires a
  one-line CMake change (`SHARED` -> `STATIC`) and no source changes. With
  `<DirectPInvoke Include="Yubico.NativeShims" />`, a `<NativeLibrary>` reference to the resulting
  `.a`, and per-RID system link flags, a Native AOT binary calls `Native_SCardEstablishContext`
  successfully with **no dylib present anywhere on disk**, and still enumerates real hardware. A
  matched control without that configuration throws `DllNotFoundException`. So the obstacle is
  packaging, not an AOT limitation — which is what the original 2023 report was unsure about.
- Recommended shape: NativeShims packs a static library per RID alongside the shared one, and its
  existing `msbuild/Yubico.NativeShims.targets` adds the `DirectPInvoke`/`NativeLibrary`/`LinkerArg`
  items under `Condition="'$(PublishAot)' == 'true'"`, so consumers get single-file output with no
  project changes. The one real decision is how OpenSSL is supplied: the shared build already links
  it statically, but a static archive does not carry its dependencies.
- Caution for anyone re-testing: `FindAllAsync` is **not** a valid probe. It degrades to HID-only
  when the PC/SC native library is missing, so it succeeds without the shim. Probe an export with
  no fallback path. See the methodological warning in Experiment 9.
- Blocked on: a `Yubico.NativeShims` release. Out of scope for any change confined to the v2 SDK
  repository layout.
- Validation: `dotnet publish … -p:PublishAot=true` produces a publish directory containing the
  executable with no `libYubico.NativeShims.*` / `Yubico.NativeShims.dll` beside it, and the binary
  still enumerates hardware.

---

## Confidence levels

| Finding | Confidence |
|---|---|
| "Zero AOT/trim analyzer warnings across all 10 in-scope modules with `IsAotCompatible=true`" | **High — directly reproduced**, clean rebuild, analyzer activation independently confirmed via `-getProperty` |
| "`System.Reactive` is the only dependency-level AOT finding" | **High — directly reproduced** via `VerifyReferenceAotCompatibility=true` |
| "`Yubico.NativeShims`'s P/Invoke surface is AOT-safe" | **High** — both live interop surfaces (PC/SC via `SCard.Interop.cs`, and OpenSSL ARKG via `ArkgPrimitivesOpenSsl.cs`) are now runtime-verified under real Native AOT publishes. The third surface (`CmacPrimitivesOpenSsl.cs`) is confirmed dead code (unreachable from any default provider), so it carries no risk. |
| "`Marshal.GetDelegateForFunctionPointer` call sites are AOT-safe in practice" | **High across macOS, Windows, and Linux** — confirmed by analyzer evidence and real running AOT binaries exercising each platform's Core discovery/native-loader path |
| "No reflection-activation/dynamic-proxy/JSON-reflection usage in SDK libraries" | **High** — exhaustive grep, corroborated by zero analyzer warnings |
| Fido2/WebAuthn/Oath/OpenPgp/SecurityDomain/YubiOtp/YubiHsm runtime behavior under AOT | **Medium** — link-verified (compiles, links, zero ILC warnings) but not runtime-exercised; static/analyzer evidence is strong, execution evidence is currently absent |
| Windows/Linux platform-specific native-loader behavior under AOT | **High — directly reproduced** through successful Native AOT publishes and physical-device discovery on both platforms |
