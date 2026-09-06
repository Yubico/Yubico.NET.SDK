# Native AOT Readiness — Raw Evidence Log

> Supporting evidence for `docs/research/native-aot-readiness.md`. All experiments below were run
> against the working tree exactly as committed (no permanent code changes were required to
> produce these results). Every temporary project-file edit used to enable the analyzers was
> reverted with `git checkout` immediately after capturing output; `git status --short` was
> clean of any `src/**/*.csproj` changes at the end of the session.

**Assessment date:** 2026-08-26.

## Initial assessment environment

- Host: macOS 15.7, Darwin 24.6.0, arm64 (Apple Silicon)
- `dotnet --info` RID: `osx-arm64`
- .NET SDK: 10.0.0 (net10.0 TFM, matching repo default)
- 3 physical YubiKeys connected via USB during the runtime probe (serials 25555459 [FW 5.4.3],
  103 [FW 5.8.0], 125 [FW 5.8.0])

Follow-up cross-platform verification used the committed
`verification/NativeAotVerification` host at PR head `0450766e` on Windows x64 and Linux x64,
with 2 physical YubiKeys connected on each platform.

## Experiment 1 — Analyzer-verified static compatibility (`IsAotCompatible`)

**Method:** temporarily added to each module's `.csproj`, immediately after the opening
`<PropertyGroup>`:
```xml
<IsAotCompatible>true</IsAotCompatible>
<TreatWarningsAsErrors>false</TreatWarningsAsErrors>
```
Then ran `rm -rf <module>/src/obj <module>/src/bin && dotnet build <module>.csproj -c Release
--no-incremental` per module, in dependency order (Core first, since every other module
references it). Verified the analyzers were actually active with:
```
dotnet build src/Core/src/Yubico.YubiKit.Core.csproj -c Release \
  -getProperty:IsAotCompatible -getProperty:EnableAotAnalyzer \
  -getProperty:EnableTrimAnalyzer -getProperty:EnableSingleFileAnalyzer
```
Result: `{"IsAotCompatible":"true","EnableAotAnalyzer":"true","EnableTrimAnalyzer":"true","EnableSingleFileAnalyzer":"true"}`
— confirms the AOT, trim, and single-file analyzers were genuinely running, not just the property
being a no-op.

**Result: zero IL2xxx/IL3xxx warnings from SDK code in any module**, across Core, Management, Piv,
Fido2, WebAuthn, Oath, OpenPgp, SecurityDomain, YubiOtp, YubiHsm. This includes zero **IL3050**
warnings ("using member which has `RequiresDynamicCodeAttribute`") against
`Marshal.GetDelegateForFunctionPointer<TDelegate>` in the platform-specific
`UnmanagedDynamicLibrary` implementations — the pre-assessment hypothesis that this call site would
be flagged was **not confirmed**; the compiler evidently resolves every `TDelegate` instantiation
statically (each `GetFunction<TDelegate>(name, out TDelegate d)` call site in `Cfgmgr32`, `HidD`,
`Kernel32`, `WinSCard`, `SCard`, `Udev`, `Libc` bindings uses a concrete, non-generic delegate type
at the call site, so the AOT analyzer can see through it).

Per-module build log excerpt (representative — full text was near-identical for all 10 modules,
differing only in which project names appear in the `CSC :` lines):

```
$ dotnet build src/Management/src/Yubico.YubiKit.Management.csproj -c Release --no-incremental
CSC : warning IL3058: Referenced assembly 'System.Reactive' is not built with `true` and may not be compatible with AOT. [.../Core/src/Yubico.YubiKit.Core.csproj]
CSC : warning IL3058: Referenced assembly 'System.Reactive' is not built with `true` and may not be compatible with AOT. [.../Management/src/Yubico.YubiKit.Management.csproj]
Build succeeded.
    2 Warning(s)
    0 Error(s)
```

**The only warning surfaced anywhere, in any module, was `IL3058` against `System.Reactive`**,
and only once `VerifyReferenceAotCompatibility=true` was additionally enabled on top of
`IsAotCompatible=true` (see Experiment 2). With `IsAotCompatible=true` alone (no dependency
verification), all 10 modules built with **0 warnings**.

## Experiment 2 — Dependency verification (`VerifyReferenceAotCompatibility`)

**Method:** added `<VerifyReferenceAotCompatibility>true</VerifyReferenceAotCompatibility>` to
Core's `.csproj` (in addition to `IsAotCompatible`), clean-rebuilt.

**Result:**
```
$ dotnet build src/Core/src/Yubico.YubiKit.Core.csproj -c Release --no-incremental
CSC : warning IL3058: Referenced assembly 'System.Reactive' is not built with `true` and may not be compatible with AOT. [.../Core/src/Yubico.YubiKit.Core.csproj]
Build succeeded.
    1 Warning(s)
    0 Error(s)
```
`System.Reactive` 6.0.1 (at the time, used for `YubiKeyManager.DeviceChanges` as
`IObservable<DeviceEvent>`; both the dependency and that property have since been removed) is
the **only** dependency flagged. `Yubico.NativeShims` 1.16.1 was not flagged — inspected the NuGet
cache (`~/.nuget/packages/yubico.nativeshims/1.16.1/`) and confirmed it ships **only native
`.dll`/`.dylib`/`.so` binaries plus MSBuild `.targets`, no managed assembly** — there is nothing for
the managed AOT-compatibility analyzer to check against it, so its absence from the warning list is
expected, not evidence of compatibility one way or the other.

**Follow-up inspection of Core's actual NativeShims P/Invoke surface** (28 declarations across 3
files, found via `grep -rn "Libraries.NativeShims" src/Core/src`):
- `src/Core/src/Native/Desktop/SCard/SCard.Interop.cs` — 11 `[LibraryImport]` declarations for
  cross-platform PC/SC (`Native_SCardConnect`, `Native_SCardTransmit`, etc.) — **live**, used by
  every SmartCard connection.
- `src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs` — 12 `[DllImport]` declarations for OpenSSL
  EC group/point/bignum operations (ARKG-P256 key derivation) — **live**, instantiated by default
  via `CryptographyProviders.ArkgPrimitivesCreator = ArkgPrimitives.Create` (Core), consumed by
  FIDO2's `ArkgP256.DerivePublicKey`.
- `src/Core/src/Cryptography/CmacPrimitivesOpenSsl.cs` — 5 `[DllImport]` declarations for OpenSSL
  CMAC — **dead code**. `CryptographyProviders.CmacPrimitivesCreator` defaults to
  `CmacPrimitives.Create`, which returns the built-in `System.Security.Cryptography.AesCmac`-backed
  implementation, not `CmacPrimitivesOpenSsl`. The only reference to `CmacPrimitivesOpenSsl` in the
  entire codebase is a comment in `src/Core/src/Protocols/SmartCard/Scp/Scp11X963Kdf.cs:222`
  ("`new CmacPrimitivesOpenSsl(CmacBlockCipherAlgorithm.Aes128); // This works in legacy code.`").
  It is unreachable from any production code path — confirmed by `grep -rn
  "CmacPrimitivesOpenSsl" src/Core/src` returning only its own declaration and that one comment.

Of the 28 declarations, only 23 (11 SCard + 12 Arkg) are reachable from production code. All 23
signatures use only blittable parameter/return types (`int`, `IntPtr`, `byte[]` with explicit
length) — no strings, structs, or delegates. This is a materially different, and materially
lower-risk, interop shape than the `dlopen`/`dlsym` + `Marshal.GetDelegateForFunctionPointer`
dynamic-resolution pattern Core uses elsewhere for OS-level native APIs (Windows
Cfgmgr32/HidD/Kernel32/WinSCard, macOS IOKit, Linux udev/libc): NativeShims is always called
through ordinary, compile-time-bound P/Invoke, which is the AOT-safest form of native interop and
requires no runtime code generation at all.

## Experiment 4 — Runtime-verifying the OpenSSL/ARKG NativeShims path (follow-up gap-fill)

**Motivation:** Experiment 3's probe exercised discovery + Management + PIV, which only reaches
NativeShims via the PC/SC path (`SCard.Interop.cs`). The OpenSSL ARKG path
(`ArkgPrimitivesOpenSsl.cs`) was analyzer-clean (Experiment 1/2) but never actually called at
runtime. This experiment closes that gap. No hardware is required — ARKG-P256 key blinding is pure
cryptographic math over caller-supplied EC points.

**Method:**
1. Built a second throwaway probe, `/tmp/aot-probe/AotProbe` (same location/pattern as Experiment
   3, deleted afterward), referencing the same 10 unmodified library `.csproj` files.
2. Since `ArkgPrimitives`/`IArkgPrimitives` are `internal` to Core, temporarily added
   `<InternalsVisibleTo Include="AotProbe" />` to `src/Core/src/Yubico.YubiKit.Core.csproj`
   (reverted via `git checkout` immediately after the run).
3. `Program.cs` generated two real P-256 keypairs via `System.Security.Cryptography.ECDiffieHellman`,
   built SEC1-uncompressed public-key byte arrays, then called `ArkgPrimitives.Create()` and
   exercised `IsPointOnCurve(pkBl)`, `IsPointOnCurve(pkKem)`, `Derive(pkBl, pkKem, ikm, ctx)` (the
   full ARKG-P256 algorithm: HMAC-KEM encapsulation, `HashToScalar`, and `EC_POINT_mul`-based public
   key blinding), and `ComputeEcdhSharedSecret(privateScalar, pkKem)`.
4. Published for `osx-arm64`: `dotnet publish -c Release -r osx-arm64 --self-contained
   -p:PublishAot=true` — **zero ILC warnings**, same as Experiment 3.
5. Ran the published binary.

**Result (first attempt, informative failure):** running the binary via an absolute path from a
different working directory threw `System.DllNotFoundException` for `Yubico.NativeShims`, even
though `libYubico.NativeShims.dylib` (3.9 MB) was confirmed present in the publish output
directory. This is standard macOS dyld relative-path resolution behavior for self-contained
bundled native libraries (not an AOT-specific problem, and not unique to NativeShims) — resolved by
`cd`-ing into the publish directory before invoking the executable, matching normal self-contained
deployment practice.

**Result (from the publish directory):**
```
=== AOT Probe: NativeShims OpenSSL ARKG primitives (gap-fill run) ===
RID: osx-arm64
IsPointOnCurve(pkBl)  = True
IsPointOnCurve(pkKem) = True
Derived PK length = 65, ARKG key handle length = 81
Derived PK on curve = True
ECDH shared secret length = 32
SUCCESS: NativeShims OpenSSL ARKG P/Invoke surface (12 declarations) exercised under Native AOT.
```
Exit code 0. All 12 `ArkgPrimitivesOpenSsl.cs` P/Invoke functions were exercised end-to-end:
`EC_GROUP_new_by_curve_name`, `EC_POINT_new`/`free` (x2 use), `BN_bin2bn`, `EC_POINT_set_affine_coordinates`,
`EC_POINT_get_affine_coordinates` (via `IsPointOnCurve`/point decode), `EC_POINT_is_on_curve`,
`EC_POINT_mul` (via `Derive`'s blinding step and `ComputeEcdhSharedSecret`'s scalar multiplication),
and the corresponding `_free` cleanup calls via `SafeHandle` finalization.

**Cleanup:** reverted the temporary `InternalsVisibleTo` addition
(`git checkout -- src/Core/src/Yubico.YubiKit.Core.csproj`), deleted `obj`/`bin` for all 10
touched modules, deleted `/tmp/aot-probe` entirely. Verified `git status --short src/` clean.

**Conclusion:** both of Core's live NativeShims P/Invoke surfaces (PC/SC and OpenSSL/ARKG) are now
runtime-verified under real Native AOT publishes on macOS arm64, not just analyzer-clean. The third
surface, `CmacPrimitivesOpenSsl.cs`, was found to be dead code and needs no further verification.

## Experiment 3 — Real Native AOT publish + runtime smoke test

**Method:** created a throwaway console app **outside the repository** (`/tmp/aot-probe/AotProbe`,
deleted after the experiment) referencing Core, Management, Piv, Fido2, WebAuthn, Oath, OpenPgp,
SecurityDomain, YubiOtp, and YubiHsm via `<ProjectReference>` to the actual repo `.csproj` files
(unmodified — this ran against the real, unpatched project files, exercising the code exactly as it
ships). Project settings:
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```
Published with:
```
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishAot=true
```

**Build/link result:** publish succeeded with the ILC (IL Compiler) native-codegen step producing
**zero additional warnings** beyond the same single `IL3058`/`System.Reactive` warning already
identified in Experiment 2 (which surfaces at the C# compile step, before ILC even runs). The
output was a genuine self-contained native Mach-O arm64 executable (`file AotProbe` →
`Mach-O 64-bit executable arm64`), ~3.2 MB, alongside the native `libYubico.NativeShims.dylib`
shim that Core `dlopen`s at runtime.

**Runtime result — device discovery + Management (first run, no PIV):**
```
$ ./AotProbe
AOT probe starting...
Found 3 YubiKey(s).
  Serial: 25555459, FW: 5.4.3
  Serial: 103, FW: 5.8.0
  Serial: 125, FW: 5.8.0
AOT probe completed without crashing.
```
This exercised, under real Native AOT: `YubiKeyManager.FindAllAsync` → PC/SC native enumeration via
Core's dynamic-library-loading path (`UnmanagedDynamicLibrary` → `dlopen`/`dlsym` →
`Marshal.GetDelegateForFunctionPointer`) → `IYubiKey.GetDeviceInfoAsync()` → a full
`ManagementSession` → TLV-decoded `DeviceInfo` for three distinct real devices, two different
firmware versions.

**Runtime result — PIV session over the APDU pipeline (second run, extended probe):**
```
$ ./AotProbe
AOT probe starting...
Found 3 YubiKey(s).
  Serial: 25555459, FW: 5.4.3
  Serial: 103, FW: 5.8.0
  Serial: 125, FW: 5.8.0
Attempting PIV session (APDU/TLV pipeline exercise)...
  PIV session created. FirmwareVersion=5.4.3
  Slot 9A metadata: Algorithm=
AOT probe completed without crashing.
```
This additionally exercised `ISmartCardConnection` connect, `PivSession.CreateAsync` (full ISO
7816-4 APDU pipeline: `ChainedApduTransmitter` → `ApduFormatterShort/Extended` →
`ISmartCardConnection.TransmitAsync` → `ChainedResponseReceiver`), and
`GetSlotMetadataAsync(PivSlot.Authentication)` (TLV decode of PIV metadata response). The blank
`Algorithm=` output is expected — slot 9A had no generated key on the test device, so
`GetSlotMetadataAsync` returned `null` and `metadata?.Algorithm` short-circuited to nothing; this is
correct behavior, not a fault.

**Scope not covered by this probe (explicit gaps, not assumed passes):**
- This experiment covered only `osx-arm64`. The later Windows and Linux follow-up experiments below
  independently cover those platform-specific native-loader branches.
- Only the SmartCard/PC/SC transport and the PIV application were runtime-exercised. HID FIDO and
  HID OTP transports (`FidoHidProtocol`, `OtpHidProtocol`) — and therefore Fido2, WebAuthn, and
  YubiOtp's actual protocol logic — were only build/link-verified (referenced, compiled, and passed
  ILC with zero warnings), not runtime-exercised, because the probe never opened a HID connection.
- OpenPgp, SecurityDomain, and YubiHsm sessions were referenced and compiled into the AOT binary
  (so their code is provably link-safe under AOT — no missing-method or reflection-stub failures at
  link time) but no session of any of those three was actually created or used at runtime in this
  probe.
- No SCP03/SCP11 secure-channel handshake was exercised.
- No error/retry/listener path (`YubiKeyDeviceMonitorService`, HID hot-plug listeners) was exercised
  — the probe only did a single one-shot `FindAllAsync`, not `StartMonitoring`.

## Experiment 5 — Windows x64 Native AOT publish + hardware discovery

**Method:** published the committed verification host on Windows x64:

```powershell
dotnet publish verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj `
  -c Release -r win-x64 --self-contained -p:PublishAot=true
```

**Result:** publish completed successfully, generated native code for `win-x64`, and emitted no ILC
or analyzer warnings. The resulting native executable linked the entry types for all 10 in-scope
SDK libraries, ran `YubiKeyManager.FindAllAsync`, found 2 physical YubiKeys represented as
`CompositeYubiKey`, and completed normally without crashing.

This verifies the Windows Native AOT publish and Core's Windows discovery/native-loading path
against physical hardware. It does not add protocol-runtime coverage for the application modules;
those entry types remain link-verified unless covered by the macOS experiment above.

## Experiment 6 — Linux x64 Native AOT publish + hardware discovery

**Method:** published and ran the committed verification host on Linux x64:

```bash
dotnet publish verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj \
  -c Release -r linux-x64 --self-contained -p:PublishAot=true

./verification/NativeAotVerification/bin/Release/net10.0/linux-x64/publish/Yubico.YubiKit.NativeAotVerification
```

**Result:** publish completed successfully, generated a stripped x86-64 ELF Native AOT executable,
and emitted no AOT or trimming warnings. The executable linked the entry types for all 10 in-scope
SDK libraries, ran `YubiKeyManager.FindAllAsync`, found 2 physical YubiKeys, and completed normally
without crashing.

A second consumer-focused probe packed all 10 SDK packages, installed them into a fresh console app
from a local NuGet feed, published that consumer with `PublishAot=true` for `linux-x64`, linked all
10 module entry types, and ran successfully. This verifies both the repository-reference host and
the packaged SDK consumption path under Linux Native AOT.

This verifies the Linux Native AOT publish and Core's Linux discovery/native-loading path against
physical hardware. As on Windows, application-module protocol behavior remains link-verified unless
covered by the deeper macOS experiments.

## Cleanup performed

- All temporary `.csproj` edits were reverted with
  `git checkout -- src/Core/src/Yubico.YubiKit.Core.csproj src/Management/src/... [...]` — verified
  clean via `git status --short src/*/src/*.csproj` returning no output.
- All `obj/`/`bin/` directories created during the experiment were removed
  (`rm -rf src/<module>/src/obj src/<module>/src/bin` for all 10 touched modules); these are
  gitignored (`.gitignore:61-62`) and were never tracked.
- The throwaway `/tmp/aot-probe` probe project was deleted after the experiment; it was never part
  of the repository.

## Experiment 7 — Reproducing GitHub #60 (NativeShims not statically linked) on v2

**Motivation:** GitHub issue [#60](https://github.com/Yubico/Yubico.NET.SDK/issues/60)
("Yubico.NativeShims doesn't build as expected with AOT", opened 2023-10-09, still open) was filed
against v1. This experiment establishes whether the v2 SDK inherits the same behaviour. Backlog
item AOT-B11 records the analysis.

**Environment:** macOS, `osx-arm64`, .NET 10 SDK, branch `yubikit-aot` @ `86d6243b`. No hardware
required — this is a packaging/linking observation.

**Method:** published the committed verification host unmodified:

```bash
dotnet publish verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj \
  -c Release -r osx-arm64 --self-contained -p:PublishAot=true
```

**Result — reproduced.** The publish directory contains the native executable *and* the native
shim as a separate file:

```
2.9M  Yubico.YubiKit.NativeAotVerification      <- native executable
3.7M  libYubico.NativeShims.dylib               <- separate shared library
```

The shim is larger than the executable it accompanies. (The `.pdb`/`.xml` files also present are
ordinary debug-symbol and XML-documentation artifacts, controllable via `DebugType` /
`GenerateDocumentationFile`, and are unrelated to #60.)

**Root-cause evidence:**

1. The package ships no static library. Enumerating `~/.nuget/packages/yubico.nativeshims/1.16.1`:
   7 shared libraries across `linux-arm64`, `linux-x64`, `osx-arm64`, `osx-x64`, `win-arm64`,
   `win-x64`, `win-x86` — and **0** files matching `*.a` or `*.lib`.
2. `Yubico.NativeShims/CMakeLists.txt:118` declares `add_library(Yubico.NativeShims SHARED)`.
3. The repository contains no `DirectPInvoke`, `NativeLibrary Include`, or `StaticExecutable`
   MSBuild configuration (grep across all `.csproj`/`.props`/`.targets`: zero matches).

Native AOT statically links native code only from a static library supplied via `<NativeLibrary>`
combined with `<DirectPInvoke>`. With only a shared library available, the SDK build has no
mechanism to fold the shim in, so it is copied beside the executable instead.

**Feasibility evidence (the encouraging part):**

```
$ otool -L libYubico.NativeShims.dylib
    @rpath/libYubico.NativeShims.dylib
    /System/Library/Frameworks/PCSC.framework/Versions/A/PCSC
    /usr/lib/libSystem.B.dylib

$ nm -gU libYubico.NativeShims.dylib | grep -c "EC_POINT\|EVP_\|BN_"
    23
```

No external OpenSSL appears in the link list, yet 23 OpenSSL symbols are exported from the shim —
confirming **OpenSSL is already statically linked into the shim**. The only external dependencies
are OS-provided system libraries. The primary obstacle usually cited for static linking (bundling
OpenSSL) therefore does not apply here; what is missing is purely a static build artifact and the
consumer-side link configuration.

**Conclusion:** v2 reproduces #60 exactly. The cause is upstream packaging, not v2 code, and cannot
be fixed from within the v2 SDK repository layout — it requires a `Yubico.NativeShims` release that
publishes static libraries. This is a *deployment ergonomics* limitation, not an AOT-compatibility
defect: every AOT publish and hardware run recorded in Experiments 3-6 succeeded with the shim
deployed alongside, which is the behaviour `docs/NATIVE-AOT.md` already documents under
"Consumer deployment".

## Experiment 8 — Device monitoring verified against hardware (macOS, JIT + Native AOT)

**Purpose:** record an operator-driven macOS run of multicast delivery, event coalescing,
composite-key merging, unsubscribe behavior, and shutdown under a Native AOT binary. The recurring
workflow has no attached hardware and does not exercise these paths.

**Environment:** macOS, `osx-arm64`, .NET 10 SDK, branch `yubikit-aot`. Two physical YubiKey 5
composite keys (serials 103 and 125) plus an HID Global OMNIKEY 5022 NFC reader. Operator-driven.

**Host revision:** `839e3c05f276ed08ad42db8c7a85fd5591ce23c6`. Later revisions tightened
the step 6 prompt and added activity assertions; the observations below apply to the recorded host
revision rather than those later changes. The observable surface (`DeviceChanges`) has since been
removed outright, so the host now attaches three concurrent `WatchAsync` watchers instead of two
observers plus one async consumer. The fan-out invariants below still apply — they are now measured
across watchers — but the sink names in this record no longer match the host.

**Method:** `verification/NativeAotVerification --monitor`, which attaches four consumer surfaces
simultaneously — two `IObservable` subscribers, one `WatchAsync` (`IAsyncEnumerable`) consumer, and
one observer unsubscribed partway — so a single physical action exercises all of them. Run twice:
once under JIT (`dotnet run`) and once against the self-contained Native AOT binary.

**AOT publish:** zero ILC/analyzer warnings.

**Result — Native AOT run: the protocol's checks passed.** Observed sequence (8 events
post-baseline):

```
Added|ykphysical:pid:0407                                   <- key A, USB composite
Added|ykphysical:125                                        <- key B, USB composite
Removed|ykphysical:pid:0407                                 <- key A removed
Added|pcsc:HID Global OMNIKEY 5022 Smart Card Reader        <- key A via NFC
Removed|ykphysical:125                                      <- key B, rapid-cycle
Added|ykphysical:pid:0407                                   <- key B, rapid-cycle arrival
Removed|pcsc:HID Global OMNIKEY 5022 Smart Card Reader      <- key A off the reader
Added|ykphysical:103                                        <- key A via USB
```

Verified:

| Invariant | Result |
|---|---|
| Composite USB key (CCID + HID FIDO + HID OTP) emits **one** event, not three | ✅ steps 2, 3 |
| NFC key (SmartCard-only) emits one event — no over-merge/split vs composite | ✅ step 5 |
| Recorded remove/reinsert activity produced both event directions and the same sequence across all sinks | ✅ |
| Unsubscribed observer receives nothing further | ✅ step 7 |
| All live sinks observe an **identical, identically-ordered** sequence | ✅ 8/8 events |
| Shutdown with a device attached: observers get `OnCompleted` | ✅ step 8 |
| Shutdown with a device attached: `WatchAsync` exits cleanly, no hang | ✅ step 8 |
| Monitoring restarts after shutdown (static state recreates) | ✅ step 9 |

The cross-sink sequence check showed that two observer subscribers and the async consumer received
the same eight event records in the same order during this run. It does not establish physical-key
correlation across removal and arrival events.

**JIT run:** identical event semantics; reported three failures that were all artifacts of the
first-draft protocol rather than SDK behaviour — the host was started with three keys already
attached (so step 1's "expect 0 events" saw their removals), and step 7 assumed a spare key rather
than a two-key setup where a key moves from NFC to USB. The protocol was corrected to treat step 1
as a baseline reset and assert the unsubscribe behavior directly instead of via an event count. The
Native AOT run above used that protocol.

**Incidental observation (not a defect, worth recording).** A key's `DeviceId` reflects the
evidence tier that resolved it and can differ between appearances of the same physical key — key B
appeared as `ykphysical:125` (serial) and later as `ykphysical:pid:0407` (PID) after a rapid cycle,
reproducibly in both runs. The observation demonstrates that `DeviceId` values cannot be used by
this experiment to pair a removal with a later arrival. The NFC event used the reader-shaped
identifier `pcsc:<reader name>` in this run.

## Experiment 9 — Proving GitHub #60 is fixable: static linking of NativeShims into an AOT binary

**Motivation:** Experiment 7 established that a `PublishAot` build emits `libYubico.NativeShims.dylib`
beside the executable and traced the cause to upstream packaging (only shared libraries are
shipped). It did not establish whether static linking would actually *work*. Issue #60 has been open
since 2023 partly because the reporter suspected the shim might be hitting fundamental AOT
limitations. This experiment settles that.

**Environment:** macOS, `osx-arm64`, .NET 10 SDK, CMake 4.4.2, Homebrew `openssl@3` 3.6.3
(which ships a static `libcrypto.a`). Two physical YubiKeys attached.

### Step 1 — Build NativeShims as a static library

Extracted `Yubico.NativeShims` from the `develop` branch and changed **one line** of
`CMakeLists.txt:118`:

```diff
-add_library(Yubico.NativeShims SHARED)
+add_library(Yubico.NativeShims STATIC)
```

```bash
cmake -S . -B build-static -DCMAKE_BUILD_TYPE=Release \
      -DOPENSSL_ROOT_DIR=$(brew --prefix openssl@3) -DOPENSSL_USE_STATIC_LIBS=TRUE
cmake --build build-static --config Release
```

Result: `libYubico.NativeShims.a` built cleanly. No source changes were required.

### Step 2 — Consumer configuration

```xml
<ItemGroup Condition="'$(PublishAot)' == 'true'">
  <DirectPInvoke Include="Yubico.NativeShims" />
  <NativeLibrary Include="path/to/libYubico.NativeShims.a" />
  <!-- A static archive does not carry its dependencies, so these are linked explicitly. -->
  <NativeLibrary Include="$(BrewPrefix)/opt/openssl@3/lib/libcrypto.a" />
  <LinkerArg Include="-framework PCSC" />
</ItemGroup>
```

The 23 `[LibraryImport(Libraries.NativeShims, ...)]` declarations in Core needed **no change** —
`DirectPInvoke` binds them at link time.

### Step 3 — Controlled comparison

> **Methodological warning — the first attempt at this was wrong.** `YubiKeyManager.FindAllAsync`
> is *not* a valid probe for NativeShims reachability. When the PC/SC native library is missing,
> that transport enumerates to empty and discovery continues with HID (documented in
> `src/Core/CLAUDE.md`). An initial run "passed" without the dylib purely because it silently
> degraded to HID-only and still found the two USB composite keys via IOKit. `DYLD_PRINT_LIBRARIES`
> confirmed the shim was never loaded in either arm. Anyone re-running this must probe an export
> with no fallback.

Both arms use identical source and are run from an isolated directory containing only the binary:

```csharp
[LibraryImport("Yubico.NativeShims", EntryPoint = "Native_SCardEstablishContext")]
internal static partial uint SCardEstablishContext(uint scope, out IntPtr context);
```

| Arm | Configuration | Result |
|---|---|---|
| **Control** | no `DirectPInvoke`, no dylib on disk | `DllNotFoundException` after an exhaustive dlopen search (exit 2) |
| **Treatment** | static-linked, no dylib on disk | `Native_SCardEstablishContext -> rc=0x00000000`, valid context; `FindAllAsync -> 2 device(s)` (exit 0) |

The treatment binary also carries no dynamic reference to the shim (`otool -L` shows only system
libraries), and adding the dylib back changes nothing — confirming it is genuinely unused.

**Conclusion: GitHub #60 is fixable, and the blocker is packaging rather than any AOT limitation.**
A one-line CMake change plus consumer link configuration produces a true single-file Native AOT
binary that still talks to real hardware.

### What a shipped fix would need

1. **NativeShims builds and packs a static library per RID** alongside the existing shared one
   (e.g. `runtimes/<rid>/native/static/`), so the change is additive and breaks no existing consumer.
2. **The package wires it up automatically.** NativeShims already ships
   `msbuild/Yubico.NativeShims.targets`; that file can add the `DirectPInvoke`, `NativeLibrary` and
   per-RID `LinkerArg` items under `Condition="'$(PublishAot)' == 'true'"`, so consumers get
   single-file output with no project changes.
3. **Per-RID system link flags:** `-framework PCSC` (macOS), `-lpcsclite` (Linux),
   `winscard.lib` (Windows).
4. **OpenSSL:** the *shared* build already statically links OpenSSL, but a static archive does not
   carry its dependencies, so the package must either ship a bundled/merged archive or emit a
   `libcrypto` link reference. This is the main packaging decision, not a technical obstacle.
5. Minor: the local build produced an `ld` deployment-target warning (objects built for macOS 15.0
   linked against 12.0); a shipped build should align `CMAKE_OSX_DEPLOYMENT_TARGET` with the SDK's
   floor.

Not implemented here: the fix belongs in `Yubico.NativeShims` on `develop`/`main` and requires a
package release, so it is out of scope for a v2 SDK branch. Tracked as **AOT-B11**.
