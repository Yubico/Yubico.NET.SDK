# Bounded merged-tree validation preflight

**Preparation only. No build, test, formatter, example/test DLL, native/device/provider operation, Git mutation or subagent was run by this owner.** Only this file and fresh RUN-owned validation preparation files were added. Source/document resolutions belong to the other owners. Commands below are for the coordinator **after** resolutions are approved and the checkout is exclusively released for validation.

## Inputs and release conditions

- Audited input: `d5d01ef99f1478e4d49911194c17667b2450d2fd`.
- Incoming input: `e322712d609aaf03b9bd1cf2964a99326728866a`.
- Natural base: `5746e72ed9a848d08b5782ddde025aa4a4b522ef`.
- Resolution authority: `../merge-review/decision-map.md`, especially lines 15–103 (nine conflicts, ten clean overlap records, live extraction recipients and frozen archives).
- Read root rules using `git show HEAD:CLAUDE.md`; build/test skill material was read, but its abbreviated dependency/discovery summaries are not substituted for current source. Actual target definitions are cited below.

Run from the repository root, not an archived host directory:

```bash
RUN=docs/research/documentation-audit/integrations/windows-upstream-20261113
git rev-parse HEAD
git rev-parse MERGE_HEAD
git ls-files -u
git diff --check HEAD
python3 -B "$RUN/validation/select-examples.py"
```

During the merge, the first two identities must name the audited/incoming parents above. After a merge commit, inspect its parents instead; do not require HEAD to remain the audited parent. `git ls-files -u` must be empty before a build. Marker absence alone is not index resolution. A `diff --check` finding is reviewed in the approved delta, not permission for broad cleanup. Recheck the preservation fingerprints and approved path/behavior allowlist through the coordinator's existing integration checks; do not regenerate archived inventories or run their old wrappers. The input fingerprint algorithm and expected identities are in `../merge-review/input-identities.md:34–54`.

Serialize all build/test/package commands; do not run them alongside resolution agents. Preserve each complete toolchain run directory **immediately** in a fresh coordinator-owned RUN receipt before more than five runs rotate it away. Save the current commit/merge inputs and working-tree identity with receipts. `artifacts/toolchain/latest/` is a moving pointer, not durable evidence. Require actual exit 0, `RESULT: PASSED`, `Warnings: 0`, and the expected per-project counts; target-level green alone does not establish execution (`TOOLCHAIN.md:154–183,200–220`). Never use `default`, `publish` or `setup-feed`: they add feed/publication work not requested here.

## Exact execution sequence

No command in this section has been executed in preparation.

### 1. Ordinary clean solution build

```bash
dotnet toolchain.cs -- build --clean
```

This cleans the solution in Release and rebuilds with `--no-incremental`, including ordinary unit/integration test compilation. It does **not** delete all `artifacts/`, run tests, publish AOT, or build every recursively discovered project. Do not prepend the separate `clean` target: it deletes other artifact evidence. Dependencies are `test-infrastructure-qa` and `native-aot-contract-qa`; restore is the normal implicit build restore, not a separate registered build dependency (`scripts/toolchain/BuildTargets.cs:144–175,177–225`; `Conventions.cs:6–11`). SDK requirement is 10.0.300+ with `latestFeature` roll-forward (`global.json`); record the actual selected SDK rather than inheriting an old host's SDK claim.

If fresh documentation snippets are injected into a real test assembly later, move that compile-only activity **before this step**, then do this uninjected clean build before executing tests. Never run a test assembly containing compile-only documentation declarations.

### 2. Ordinary hardware-free unit suite

```bash
dotnet toolchain.cs test
```

No `--integration`, no broad class-name filter, no coverage/benchmark/resilience replay and no smoke thinning. Current source-only discovery found exactly 13 ordinary projects under `src/`: Cli.Commands, Cli.Shared, Core, Ctap2, Management, Oath, OpenPgp, Piv, PublicApi, SecurityDomain, WebAuthn, YubiHsm and YubiOtp, each `.UnitTests`. There were no archive projects in that test selection. Reconcile this list against the actual receipt at execution time. The ordinary suite already includes managed Windows owner/routing/cross-layer tests, macOS extraction/Linux split/PCSC regressions, API/inventory and PIV response/recovery tests; do not rerun all these classes separately just to duplicate evidence.

The target selects unit projects unless `--integration` is present, and invokes the project's real runner with a build (`scripts/toolchain/Context.cs:44–50,75–78`; `TestTargets.cs:11–26,124–155`). Do not supply imagined `--no-build`/`--no-restore` toolchain switches. Ordinary unit crypto/fake-seam behavior is not device or native acceptance. Existing platform skips retain their qualifications; inspect each project's executed/skipped counts and raw log.

### 3. Portable Windows evidence-gate self-tests

```bash
dotnet toolchain.cs windows-hid-verification-evidence-test
```

This depends on `windows-hid-verification-build`, which compiles the **real** standalone verifier in Release with `PublishAot=false`, then runs its DLL with `--evidence-self-test`. It is portable and does not invoke native APIs (`scripts/toolchain/BuildTargets.cs:117–125`; `verification/WindowsHidRouteVerification/Program.cs`). Keep the gate's individual assertions/output in addition to the toolchain result: this is a console self-test host, not another discovered xUnit test project. Do not separately repeat its build target. Do **not** run `windows-hid-verification-publish` or `windows-hid-verification-nohardware`: those are Windows-native/JIT/AOT publication/execution work, explicitly outside this bounded regression.

### 4. Exactly three hardware-free lifecycle cases in the integration assembly

```bash
dotnet toolchain.cs -- test --integration --project Yubico.YubiKit.Core.IntegrationTests --filter "FullyQualifiedName~Yubico.YubiKit.Core.IntegrationTests.Devices.DeviceEventLifecycleTests"
```

Require **3 executed, 0 failed, 0 skipped** and these exact methods:

1. `Lifecycle_TransientFragmentAddedThenRemoved_DoesNotResurrectObject`
2. `Lifecycle_RemovedObjectAddedAgain_IsRejected`
3. `Lifecycle_EqualValuedSuccessorWithDifferentReference_IsAllowed`

They construct local `TestKey` records; their `ConnectAsync` throws and is not called. They exercise the hotplug assertion's reference identity, not attended hotplug/device enumeration (`src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/DeviceEventLifecycleTests.cs:20–74`). Selecting the qualified `.IntegrationTests` stem avoids also selecting Core units even though `--integration` **adds** integration projects. A single class filter avoids the toolchain's same-kind compound-filter ambiguity. Do not select `DeviceEventHotPlugTests`, all Core integration tests, or any device trait as a replacement.

### 5. Documentation and architecture checks

```bash
dotnet toolchain.cs docs-qa
dotnet toolchain.cs docs-architecture
```

`docs-qa` checks bounded active fences/local links/stale patterns/agent-document references and package readmes; its MSBuild metadata evaluation does not compile C# snippets. `docs-architecture` runs exactly `scripts/architecture/check-architecture-map.sh` and `check-architecture-images.sh` (Bash required). It checks map resolution and existing image freshness, not fresh renders or native lifetime correctness. Research/RUN archives are excluded from active docs discovery; review this preparation directly. Do not invoke `docs-inventory`, which writes a report and adds no required acceptance evidence (`scripts/toolchain/DocsTargets.cs:10–42,63–150`; `Discovery.cs:51–90`; `TOOLCHAIN.md:232–253`).

### 6. Complexity: incoming delta and reconciliation distinction

```bash
dotnet toolchain.cs -- complexity --complexity-args "--base d5d01ef99f1478e4d49911194c17667b2450d2fd --json artifacts/windows-merge-complexity-from-audit.json"
dotnet toolchain.cs -- complexity --complexity-args "--base e322712d609aaf03b9bd1cf2964a99326728866a --json artifacts/windows-merge-complexity-from-incoming.json"
```

First comparison covers the incoming plus reconciliation shipping-source delta against the audited input, even after HEAD becomes a merge commit. Second comparison identifies audited-side/reconciliation changes relative to incoming; it is **not** a whole-SDK scan. Do not replace these with `--all`, coverage/CRAP, or a changing `origin/yubikit` comparison. Only shipping `src/<Module>/src` methods overlapping changed lines are measured; tests, scripts, examples and verification hosts are excluded. Report exits 0 even with findings unless `--fail-on-findings` is supplied, so read every new/worse row and preserve any new resolution-specific justification. Unchanged/improved findings are inherited debt (`TOOLCHAIN.md:390–435,452–463`; `scripts/toolchain/CoverageTargets.cs`, `complexity` registration).

Incoming commit messages already contain the following justifications, obtained read-only with `git log --format='%H%n%B' 5746e72ed9a848d08b5782ddde025aa4a4b522ef..e322712d609aaf03b9bd1cf2964a99326728866a --grep='Complexity-Justification'`:

> `53221b33d77ae04e5cf017b27ca728ae5305926a`
>
> Complexity-Justification: HidConnectionSlot.OpenBuiltInConnectionAsync: Keep the six platform/transport routes and their claim transfer visible in one flat switch; cyclomatic 12, cognitive 1, with no additional lifecycle state machine.

> `4b0575d8f032c8c72377b80278e5be7dddfd20bb`
>
> Complexity-Justification: WindowsFidoHidNativeOwner.CompleteRequest: Keep cancellation/error reconciliation, exact retirement, owned-buffer clearing, and actual public task publication together so their ordering remains auditable.
>
> Complexity-Justification: WindowsFidoHidNativeOwner.Advance: Keep the submit-return/completion join and once-only worker cancellation in one state transition method; these branches encode the frozen independent native observations.

Retain these as incoming design rationale, not authorization for new complexity. If the measured final method has changed again, reassess that difference instead of mechanically copying its old justification. No unrelated complexity cleanup is part of reconciliation.

### 7. Docs-inclusive packaging, no publication

```bash
dotnet toolchain.cs -- pack --include-docs
```

This runs its normal `build` dependency and packs discovered packable projects in Release, normally `--no-build`; `--include-docs` avoids disabling `GenerateDocumentationFile`. Root props already enable XML documentation. It does not embed all developer Markdown in packages: module `PACKAGE_README.md` is shipped as `README.md`. Do not add a package-version override, symbol mode, signing, feed registration or push (`scripts/toolchain/PackageTargets.cs:9–34`; `Directory.Build.props:68–69,78–99`). Check the receipt's exact pack project/package list and package XML/readme contents, rather than counting stale files already in `artifacts/packages`. The fresh preparation aggregate below is explicitly non-packable.

## Standalone verifier compilation and legitimate project discovery

Normal solution build is driven by `Yubico.YubiKit.sln`, which contains **none** of the standalone `verification/` projects. `native-aot-contract-qa` checks host references/anchor text, not compilation of that host. `--project` is not a path argument: build discovery recursively requires a `src` directory segment **and** `Yubico.YubiKit.` in the full path; matching then uses the case-insensitive project **filename stem** (`scripts/toolchain/Context.cs:44–46`; `Discovery.cs:10–18,98–109`; `BuildTargets.cs:199–225`). Thus neither `--project WindowsHidRouteVerification` nor `--project verification/LinuxHidRouteVerification/...csproj` is legitimate direct selection.

For bounded compile-only checking of the two existing route consumers, a fresh empty aggregate is prepared at:

`validation/verifier-compile/src/Yubico.YubiKit.WindowsMerge20261113.VerifierCompile.csproj`

Its explicit `ProjectReference` items build only actual MacOSHidRouteVerification and LinuxHidRouteVerification (and their actual SDK dependencies). Both route hosts reference Core/Ctap2/Management, and Linux links the actual Mac route `SmartCardScenario.cs` (`verification/{MacOS,Linux}HidRouteVerification/*.csproj`). The real hosts already have Core friend access (`src/Core/src/Yubico.YubiKit.Core.csproj:27–37`). The aggregate uses `ReferenceOutputAssembly=false`, has no executable or example sources, and does not impersonate a friend name or grant access. `PublishAot=false` selects ordinary managed host compilation; it neither publishes nor executes native scenarios. No analyzer/warnings-as-errors override is added. Normal referenced outputs are their ordinary ignored `bin/obj`, not frozen archive outputs.

After release, the exact separate command is:

```bash
dotnet toolchain.cs -- build --project Yubico.YubiKit.WindowsMerge20261113.VerifierCompile --clean
```

This aggregate is discovered because its real path includes `/src/` and its own unique stem has the required prefix. Its name avoids `Core`, `.UnitTests` and `Test`, so it is not a broad Core/test selection accident. It does not join the solution and does not affect the ordinary unit-suite membership. Static preparation checked its XML and both reference paths; **compilation is unverified**. Run it before the ordinary clean solution build if the coordinator wants one final normal-output boundary before testing.

| Existing verifier | Bounded treatment |
| --- | --- |
| WindowsHidRouteVerification | Separate compilation already supplied by the portable evidence target; no duplicate command. |
| MacOSHidRouteVerification / LinuxHidRouteVerification | Compile only through the fresh aggregate above; do not run their entry points/capture/publish scripts. This proves compiler binding only, not extraction/native equivalence. |
| NativeAotVerification | Outside solution; static contract check is already a build dependency. No new all-module AOT publish/link acceptance requested; do not add one. |
| MacOSHidInputVerification | Separate experimental native shim host with no SDK ProjectReference; not affected by these managed seam/PCSC renames. No fresh compile/native experiment needed. |
| MacOSHidBoundaryComparison | Old assembly-path/package-version comparison host, not a renamed-seam consumer. Do not replay old comparison context or pin an old Core DLL to claim merged-source acceptance. |
| NativeCompatibilityVerification (+ RuntimeConsumer) | Independent compatibility probe, not part of this source-resolution regression; no new run/build gate. |

## Example selection: no required snippet replay found

The source-backed resolution plan changes vocabulary, ownership qualifications, source locations and one PIV shell test recipe; it does not request a C# body rewrite. Static comparison against the audited input found:

| Region | C# fences / XML code bodies | Result at preparation |
| --- | ---: | --- |
| Root `CLAUDE.md` | 14 fences | Exact body sequence retained. |
| `docs/architecture/raw-access-tiers.md` | 8 fences | Exact body sequence retained. |
| `docs/SDK-HOUSE-STYLE.md` | 2 fences | Exact body sequence retained; existing illustrative qualifications stay qualified. |
| `src/Core/CLAUDE.md` | 11 fences | Exact body sequence retained. |
| `src/Core/README.md` | 15 fences | Exact body sequence retained; logger-category rename is prose, not a changed logger sample. |
| `src/Piv/README.md` / `src/Piv/CLAUDE.md` | 8 / 4 fences | Exact body sequence retained. New focused test command is shell, not a C# compilation target. |
| `SmartCardConnectionFactory.cs` | 2 XML code bodies | Exact bodies retained: FULL-414 (policy before discovery), FULL-415 (caller-supplied `IPcscDevice`, real factory/raw session scopes). Implementation's `connectionApi:` rename is not in either sample. |
| Other overlap records, deleted predecessors and checked live recipients | 0 code bodies | No new snippet compilation target. Ordinary build covers actual executable C# and crefs. |
| Additional active `connection-ownership-and-contention.md` | 2 fences | Exact body sequence retained; Windows qualification correction is prose. |

The selector also read the actual **302 selected Full body-ID wrappers** from the final closeout, including aliases, and found **no references** to `UsbSmartCardConnection`, `PcscConnectionNativeState`, `IIOKitDeviceLifetime`/`IOKitDeviceLifetime`, `IHidInputBridge`/`HidInputBridge` or the two retired Windows report-access types. It did not import/run their generators, inject tests or alter their archived names. Original pilot bodies are manager/credential/provider examples, not dependencies on those internal renamed adapters (`core-001/example-validation-preparation.md:8–47`). Historical ISA/design names are not live snippet bindings and must not be rewritten merely to eliminate search hits.

Therefore the minimal required changed/rename-dependent snippet compilation selection is **empty**, conditional on the final resolution retaining these bodies. Do not replay all **317** accepted preparations. Existing closeout is the right evidence reuse point, not the older preparation status text: `docs/research/documentation-audit/modules/core/pass-1/core-001/validation/examples/full/closeout/acceptance.md:3–41,54–76,98–118` records 300 Full unique preparations + 17 pilot, exact original compiler/copy associations and no execution. That evidence remains about its original source/producer, not current-binary equivalence.

After final source release, rerun only the fresh read-only selector:

```bash
python3 -B "$RUN/validation/select-examples.py" > "$RUN/validation/example-selection-final.json"
```

Require empty `changed_example_paths` and `accepted_wrapper_rename_candidates`; review any nonempty result against actual source before deciding to compile. It prints marker observations but is not a replacement for full merge/index checks, an XML compiler, or semantic dependency analysis. It compares exact C# fence/XML body text in the bounded resolution set; whole-document hash changes and line shifts alone are **not** a reason to rerun unchanged bodies.

If a final editor genuinely changes a body, the smallest **new** context is a non-packable library under fresh RUN `validation/.../src`, named with `Yubico.YubiKit.` and a unique non-test/non-Core stem, referencing only the actual modules that body uses. Public samples need genuine `IYubiKey`/`IPcscDevice`, buffer/token and helper parameters and their full original cleanup scopes, not a fake implementation. Core has no general audit-host IVT: a standalone host cannot compile `PcscProtocol`, `PcscConnection` or internal transport seams just by adding a Core reference. An internal example must instead use a bounded compile-only injection into the **actual** existing Core.UnitTests assembly (or its actual module context), with its existing fake/import/package access, followed by uninjected clean rebuild before tests. Do not add an IVT or forge `AssemblyName`. Prior real-context methods are documented at `core-001/full-example-preparation.md:47–85` and implemented in `validation/examples/full/test-contexts.py:31–69,81–136`. Read those as methods only; create no current output or repin in their archive. No such snippet host/injection is prepared here because the selection is empty.

## Formatter membership assessment, not broad formatting

No formatter was run and no formatting cleanliness is claimed. Before an optional coordinator-authorized scoped verification, divide **existing** changed C# paths into:

1. Real solution project documents under Core/Piv/Ctap2/YubiOtp, including tests: solution-scoped `dotnet format Yubico.YubiKit.sln --verify-no-changes --no-restore --include <separate actual paths>` can visit these. This is not permission to format all incoming edits or fix baseline diagnostics.
2. `scripts/toolchain/BuildTargets.cs`: a `#:include` file-based-app source, **not** a solution document. A solution-format exit 0 with this path means skipped. New `WindowsHidVerification/{PublicationSeal,RequiredNativeChecks}.cs` are also outside the solution, but are explicitly linked compiler sources of the Windows verifier; if selected for formatting, use that real verifier project rather than pretending solution membership.
3. `verification/WindowsHidRouteVerification/*.cs` and `verification/LinuxHidRouteVerification/BlockingReportConnection.cs`: outside the solution, real members of their respective verifier projects. A separate project-scoped check is possible only if requested, after their restore/build. Do not include Mac route or archive files merely because they are nearby.
4. Deleted/stale predecessor paths, Markdown, `.props`, `.csproj`, shell and JSON: not eligible solution C# documents. The new RUN aggregate has no C# sources to format. New untracked C# sources, if any, must be explicitly classified rather than omitted by `git diff`.

Static solution-path inspection during active reconciliation found 96 existing changed C# paths under solution projects, 11 outside, and 3 deleted; these are a transient membership observation, not an authoritative final file list or analyzer result. The outside set was the toolchain file + 2 linked seal/check sources + Linux blocking-adapter qualification + 7 Windows verifier sources. Recompute the final allowlisted paths after resolution. `--include` takes separate space-separated path arguments, never one semicolon/comma-joined quoted string. Exit 0 for zero matched documents is **skipped**, not verified clean (`HEAD:CLAUDE.md`, Pre-Commit Checklist item 4; `TOOLCHAIN.md:227–230`). Broad formatter/analyzer cleanup is outside this integration.

## Evidence boundary / stop

Reuse the parent's PR evidence receipt with its original producer/source/runtime/OS/device/firmware/transport limitations. The portable gate, ordinary suite, three local-record cases, compiler binding, docs and packages above do not extend historical Windows device/JIT/RDP evidence to this merged source. They establish no Windows native pipe/HID-driver race/AOT seal acceptance, macOS native extraction acceptance, real hotplug, PIV physical PIN isolation/power clearing, or new crypto/provider interoperability. No ABI/logger/listener/protocol fix is smuggled into validation. If bounded regression fails, preserve the literal diagnostic and return it to its owning resolver; do not broaden acceptance work or repair frozen examples under this file's authority.
