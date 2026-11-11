# Integration validation preparation

## Handoff and present evidence

Sol prepared the validation tools in `validation/` while the integration resolver retained product
ownership. Astra owns checkout release, actual gate execution and publication. This preparation binds
to campaign `f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa` and incoming
`5746e72ed9a848d08b5782ddde025aa4a4b522ef`. The resolved integration identity is still pending.

The preparation inspected pinned Git toolchain/CI sources and frozen PIV tooling, plus current project
filenames and read-only host availability. Current consumer bodies remain with the resolver until
release. Builds, tests, native-host execution, formatting, device operations, staging and commits
remain coordinator work. Runtime findings from those gates go back to the resolver as scoped blockers.
Whole-Core semantic investigation starts after independent integration acceptance, required gates,
the pushed checkpoint and ancestry/preservation verification.

Read `coordination.md` and both files in `planning/` for the approved phase boundary and style contract.
Root instructions were read from both Git parents. The `domain-build`, `domain-test` and verification
skills were loaded during preparation; the executing coordinator should load the current build/test
skills again before gates and re-read the final integrated instructions.

## Small tools and provenance

| Tool | Purpose and bound |
| --- | --- |
| `validation/preflight.py` | Read-only incoming-source hashes, filename-selector matches, unit-project inventory, CPU/SDK/compiler/native-library availability. Uses `git show 5746…:path` for tooling policy. Frozen PIV source hashes use campaign `f0ba…`. |
| `validation/run-command.py` | One exclusive cooperative command lease; separate full stdout/stderr; immediate byte-exact retention of the newly created actual toolchain directory, including every step log and both RESULT files, before another run can rotate it. Fresh labels preserve failed attempts. |
| `validation/check-preserved.py` | Checks the exact coordinator before receipt, eight manifest hashes, all 5,366 manifest-listed artifacts and 114 PIV integration files in **each** checkout. Checks the four original user files and unchanged campaign HEAD in the primary checkout only. |
| `validation/check-records.py` | Final packet text/artifact inventory, raw/LF-normalized/Git-clean hashes, attributes, and exact staged object bytes. Final snapshot waits for all packet writers. Rejects binary/cache payloads and symlinked inputs. |
| `validation/verify-preparation.py` | AST/provenance/selector/attribute inspection and replay of the result parser against a frozen historical PIV RESULT. Executes no product gate or command logger. |

`preflight.json` records exact revision/path/size/SHA-256 identities for incoming TOOLCHAIN/CI/target
sources and the PIV sources used as algorithm references. The tools above are small independent
implementations; they execute no frozen PIV module or old generated framework. Reused principles are
byte/hash preservation, actual compiler membership, explicit project closure and staged-byte proof.
PIV's `review-closeout.md`, `corrections.md`, `snippet-validation.md`, final `capture-v4.py`,
`compiler_policy.py` and `project_graph_v4.py` supply the source-derived lessons below.

The pinned before receipt is SHA-256
`8f45a2c984ab0d741152378d5fcadb4f785a478fca9b47f4ba384139cf7558c9`.
Preservation verification binds that identity before reading artifact expectations. Old compilation
fixtures keep their original bytes even when their APIs are now stale.

### Observed host and selectors

`preflight.json` records Linux `x86_64`, 20 logical CPUs, .NET SDK **10.0.404** / MSBuild
18.9.15, runtime 10.0.15, clang 23.1.3, GNU ld/objcopy 2.47, zlib 1.3.2, PC/SC 2.5.3
and libudev 262. The `pacman -Q` diagnostic exits 1 because the **lld package is absent**; clang,
GNU ld and objcopy availability are recorded independently. Availability is a prerequisite observation;
successful AOT linking and runtime execution require their own gates. Preparation installs nothing and
leaves services unchanged. Current `global.json` policy at the incoming pin is 10.0.300 with
`latestFeature` roll-forward.

Discovery scans recursively and matches `--project` against the **filename stem**, case-insensitively.
Archived projects can enter discovery. At preparation time, `Core` selects the canonical Core library
and both Core test projects for a build; its unit-test match is the one canonical Core unit project.
`Yubico.YubiKit.Core.UnitTests` selects exactly that project. Re-run preflight after resolver release,
compare the entire actual project set, and prefer full canonical filenames for scoped commands.
The resilience implementation itself selects `Core`; require its unit candidate set to remain exactly
the canonical project before execution. A `--project` argument does not override that implementation.

## Coordinator release gate

1. Resolver completes product migrations and records exact file identities; another fresh Sol reviews
   both-parent intent and the resolved source. Re-read final root and applicable module instructions.
2. Verify an empty unmerged index and exclusive checkout ownership. Record the accepted working-source
   identities and ancestry state. During the pending merge, `MERGE_HEAD` must equal the incoming pin;
   after the merge commit, verify both pinned parents are ancestors of the integrated SHA.
3. Run preservation and preflight again, retaining their fresh outputs under new attempt names. Reconcile
   the 13 discovered canonical unit projects with `validation/expected-unit-projects.json`; a changed
   set receives explicit review before updating the expectation. Check active toolchain ownership.
4. Run the command plan serially. Inspect raw failures before a scoped correction and a new attempt.
   The command logger is a cooperative lease for this packet, not an OS prohibition on bypassing it.
   Astra keeps all other checkout build/capture/device jobs stopped while it holds this lease.

## Source-validated command plan — execution pending

Use the isolated checkout root. The following shell variable is a path abbreviation only:

```bash
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
```

### Main integration lane

```bash
python3 -B "$V/run-command.py" --label main-gates-first --toolchain \
  --expect-target build --expect-target test --expect-target docs-qa \
  --expect-target docs-architecture --expect-target complexity --expect-target resilience \
  --expect-projects-file "$V/expected-unit-projects.json" \
  --expect-project resilience:Core.UnitTests -- \
  dotnet toolchain.cs -- build test docs-qa docs-architecture complexity resilience \
  --clean --fast --complexity-args "--base f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa"
```

This names each target explicitly and lets Bullseye run prerequisites once. The clean build compiles
the solution in Release with `--no-incremental`; it includes integration test compilation. `test`
selects unit projects because `--integration` is absent. `test-infrastructure-qa` and
`native-aot-contract-qa` are prerequisite static gates. Docs QA checks the active document set and
package README policy; architecture runs both evidence-map and image-freshness scripts through Bash.
Complexity compares the integration delta to the campaign pin, including incoming runtime changes.
Inspect new/worsened findings and obtain the required dispositions or justifications; a zero exit
alone supplies no complexity acceptance. An optional second report against the incoming pin separates
campaign/resolution changes from upstream debt without replacing the complete integration report.

Resilience requires `--fast` and selects `Category=RuntimeResilience` in Core unit tests. Verify the
actual target/project counts, skipped cases and raw runner outputs; preparation sets no historical
execution-count threshold. The logger requires all named targets, exact target/project membership,
nonzero execution for every project, zero failures, zero warnings, matching RESULT text/JSON totals
and a successful actual process exit. The result has 13 expected unit project rows plus the separate
Core resilience row; their execution counts remain fresh observations.

Incoming `build.yml` also runs `python3 scripts/docs/test-docs-patches.py` and package creation.
If the coordinator requires full local CI parity, capture the patch-guard check as a separate generic
logger command and add `pack` to the **same main invocation**, with `--expect-target pack` and
`--package-version 2.0.0-alpha.2`. That is the normalized argument form of the pinned CI package step.
The build dependency then stays deduplicated. Package publication, feed mutation and attestation remain
CI/publication work. Docs snippet compilation is a separate follow-up below.

Pinned CI provisions `pcscd`, `libpcsclite-dev` and `libudev-dev`, starts PC/SC and opens its CI socket.
The local availability receipt reports libraries only. Astra checks required service readiness through
read-only diagnostics and records any environment blocker; copying CI's privileged setup is a separate
system action. Bash, Python 3, restore/feed access and the pinned .NET SDK policy are prerequisites.

### Separate Linux external-host lane

```bash
python3 -B "$V/run-command.py" --label linux-publish-first --toolchain \
  --expect-target linux-hid-verification-publish \
  --retain-text artifacts/linux-hid-verification/before-publish.json \
  --retain-text artifacts/linux-hid-verification/publish-manifest.json -- \
  dotnet toolchain.cs -- linux-hid-verification-publish

python3 -B "$V/run-command.py" --label linux-native-first \
  --retain-text artifacts/linux-hid-verification/evidence -- \
  python3 -B verification/LinuxHidRouteVerification/capture.py --native-only
```

After the second command, read its **actual** `Evidence directory:` line and the retained invocation.
Use that exact directory as `CAPTURE`; choose by emitted identity rather than an assumed newest result:

```bash
python3 -B "$V/run-command.py" --label linux-evidence-gates-first -- \
  python3 -B verification/LinuxHidRouteVerification/test_evidence_gates.py "$CAPTURE"
```

The host lives outside the solution and ordinary unit/build success leaves this lane pending. The
publish target restores separate `linux-x64` JIT/AOT trees, binds evaluated imports/assets/package
content, publishes self-contained without implicit restore, then seals unchanged inputs and artifacts.
Retain the complete text binding manifests. Runtime binaries stay external; the manifests record their
identities. Keep the publish/capture transaction intact and avoid a clean/rebuild between these steps.

`--native-only` runs both runtimes' native cases and the intended-failure blocking/quiescence controls.
The capture validates the expected negative exits, uniquely named checks, cleanup and current publish
binding; its own successful exit is 0. Check `summary.json`: `nativePassed=true`,
`allExpectedOutcomes=true`, six matched run rows, `physicalModesExecuted=0`, and no selected serial.
The evidence-gate script takes that capture directory as its sole positional argument and exercises
the retained real records plus fail-closed provenance cases. Capture still reads OS/sysfs availability
metadata; native-only avoids SDK key enumeration and key scenarios. Operator modes remain deferred.

The all-module Native AOT publish/startup matrix in `native-aot.yml` is a distinct CI qualification:
macOS arm64, Linux x64 and Windows x64. Its no-key discovery startup contract requires an appropriate
host with no attached key. Astra assigns that lane separately; Linux HID native-only results establish
neither other-platform qualification nor PIV hardware proof.

## Bounded current-example compilation follow-up — owner pending

Assign a fresh Sol preparation owner after resolver release and independent integration review.
Current extraction and consumer-host construction are deliberately pending because the resolver is
still changing signatures and callers. Use one representative complete current consumer and prove its
capture before scaling. Keep this as a small follow-up rather than recursively cloning the PIV framework.

1. Inventory final affected Core/shared/applet guides and XML bodies, including retained unchanged
   callers affected by renamed `Find…`, watch/monitor options or named parameters. Bind full current
   document hashes, complete body boundaries and helper/resource scopes. Include migration guides and
   actual shared CLI/applet sample projects. Give every candidate a complete/composable/application-
   dependent/historical disposition. Frozen fixtures remain historical evidence.
2. Prepare a uniquely named host such as `Yubico.YubiKit.AuditIntegration20261111.Consumers` only after
   checking its filename against full recursive discovery. Compile whole bodies with genuine SDK
   project references and real typed application prerequisites. Preserve wrappers separately from
   extracted bytes. Build actual affected sample projects as whole projects where applicable.
3. Reuse PIV's final capture **algorithms**, adapting its pinned paths and policies through one explicit
   provenance receipt. SDK 10.0.403 paths, PIV project names, three old contexts, old graph counts and
   original acceptance receipts are historical inputs, not portable constants for this integration.
   Record the actual 10.0.404-or-selected build SDK/compiler at execution time.
4. Bind actual executed Csc source/reference/analyzer/analyzer-config sets to the captured MSBuild
   items and arguments, with duplicate detection and exact membership. Retain generated sources,
   imports, effective policy, assets, assembly attributes and source text snapshots immediately after
   the genuine clean compilation. Keep strict warnings-as-errors and actual execution/no-skip proof.
5. Prove actual canonical analyzer participation from restored package identity/content, import order
   and DLL hashes. The explicit NetAnalyzers package can set `EnableNETAnalyzers=false` while supplying
   the canonical analyzers. Capture effective **per-project** AnalysisMode and warning policies;
   Recommended and AllEnabledByDefault are separate observations. Avoid forcing one policy label on
   all contexts or suppressing a real analyzer failure.
6. Bind the declared and restored **direct and transitive** project graph from current csproj/assets,
   actual reference assembly paths, resolved output paths and identities. Reject duplicate project
   paths/IDs, canonicalize only unordered graph-node collections, and compare complete node values,
   edges, closures and output bindings. Late ProjectReference items may contain the transitive closure.
   Host Version, library restore version, catalog/package version and .NET SDK Version are independent;
   derive relationships from the current project's actual assembly attributes rather than assuming
   `libraryVersion == SDKVersion` or host/catalog equality.
7. Preserve the first failure and its exact tools/inputs before correction. Run the current compiled
   and live-dependency checks against current accepted sources after all example corrections. Retain
   text capture in this packet and keep compiler/runtime binaries outside versioned records.

Historical PIV live checks have an additional prerequisite: their **original tested binaries** must
remain at their registered external identities or an explicitly verified mapping. The optional
`check-preserved.py --historical-binary-manifest <piv-001>/validation/tested-project-binary-preservation.json`
requires every registered external binary and fails immediately on missing or changed bytes. An old
record's presence, a regenerated DLL or a skipped missing-file check supplies no new live proof.
This preparation neither replays old hardware checks nor releases the retained PIV physical deferral.

## Final preservation and staging handoff

Run the preservation tool before and after each meaningful gate and before the primary campaign moves.
It intentionally requires the primary checkout to stay at `f0ba…`; after an authorized campaign
advancement, retain this successful checkpoint and assign a separately recorded ancestry/preservation
phase rather than weakening the baseline guard.

After resolver/reviewer/coordinator records are complete, choose a fresh final manifest name and run:

```bash
python3 -B "$V/check-preserved.py"
python3 -B "$V/check-records.py" snapshot
python3 -B "$V/check-records.py" check
# Coordinator stages the exact reviewed paths, including validation/.gitattributes and the manifest.
python3 -B "$V/check-records.py" staged
```

The checker observes Git clean conversion with `git hash-object` without writing objects. Each record
retains raw and CRLF-to-LF hashes separately; `.csproj`/props EOL differences stay explicit. Snapshot
source and command-stream attributes in `validation/.gitattributes` use `-text`, preserving captured
native/tool bytes. The checker requires captured `.snapshot` and resolver Git-blob source bytes to
survive staging exactly, rejects unexpected transforms, and verifies index/worktree attribute equality.
It rejects runtime/test DLLs, executables, native objects, package archives, caches and non-text payloads.
Only binary identities belong in these records. The cooperative lock file is operational state and is
excluded from the record set. New files or changed logs after snapshot require an explicit new final
snapshot/disposition; preserve earlier attempts. Final checker output can remain in the coordinator's
external log to avoid a self-changing archive manifest.

The static preparation receipts are `validation/static-verification.log`,
`validation/preservation-preparation.log` and `validation/preflight.json`. They establish their stated
read-only scope. Main gates, Linux execution, final staged-byte verification, current example compilation,
independent integration acceptance and publication remain pending coordinator gates.

Reproduce the read-only preparation inspection from the isolated checkout root:

```bash
python3 -B "$V/preflight.py"
python3 -B "$V/check-preserved.py"
python3 -B "$V/verify-preparation.py"
```

The final command checks the captured provenance and preparation sources. Replacing `preflight.json`
with a fresh inventory is an explicit coordinator record update; retain its earlier preparation
observation separately when recording the released integration's preflight.
