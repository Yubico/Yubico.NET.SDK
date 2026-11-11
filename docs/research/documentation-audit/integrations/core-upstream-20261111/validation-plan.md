# Resolver validation-needs supplement

## Canonical assignment and present limit

Read `validation-preparation.md` for the independently prepared, source-validated commands and the
canonical `validation/run-command.py` logger, lease, result checks and preservation tools. Astra assigns
all actual gates after fresh independent integration review. The resolver has executed Git, file
preparation, hashing and static source inspections only. Builds, tests, formatting, native verification
and devices remain unrun. The primary checkout remains at the campaign pin.

The preparer's observed host is SDK 10.0.404 / Linux x64; that is prerequisite availability, not a
compilation result. Its main invocation explicitly combines build, test, docs QA, architecture,
complexity and resilience with `--clean --fast`, exact expected unit membership and a campaign-pin
complexity baseline. Preserve that combined invocation so Bullseye prerequisites run once. The normal
unit and required resilience rows overlap deliberately; report them as two gates, not independent
additional coverage. The separate Linux lane owns publish, native-only capture and evidence-gate tests.
No other worker should run overlapping checkout jobs.

Incoming CI also runs package creation and `scripts/docs/test-docs-patches.py`. Include these actual
checks when assigning CI-parity gates; `validation-preparation.md` gives the package arguments and
how to add `pack` to the same main invocation. A scoped diagnostic rerun follows a recorded failure;
it is not a simultaneous duplicate suite or permission to compile old archive hosts.

## Source-specific consumer follow-up

The resolver prepared **25 candidate complete current bodies** in a fresh integration context. They are
inputs for the independently assigned consumer/capture owner after resolution acceptance, not accepted
compiler evidence or a substitute for that follow-up. `validation/examples-manifest.json` binds paths,
fence/line identities, raw/generated hashes and typed prerequisites. `prepare-examples.py` records the
exact extraction/wrapper transformation; no generated body is invoked.

| Source | Migration bodies prepared |
| --- | --- |
| `README.md` | Complete current one-shot entry example |
| `docs/usage/device-discovery.md` | Fresh/scoped Find, cached metadata, private watch, projection, shared hosted lifetime, UI helper, error handling, current service class |
| `src/Core/README.md` | Entry discovery, scoped Find, cached snapshot, contention retry selection, sharing setup, first-result selection, revisioned watch |
| `docs/architecture/physical-device-model.md` | All versus scoped discovery, owned monitor subscription with caller-supplied `Action<DeviceChange> Apply`, last-known snapshot, live Management read |
| `docs/migration/v1-to-v2.md` | Complete current discovery and live Management migration bodies |
| `src/Management/CLAUDE.md` | Corrected named-token enumeration/live-read example |

The historical `IYubiKeyManager` before-example stays historical, with its complete current counterpart
prepared separately. The generic-host body retains the complete `MonitorLifetime` type and outer
`await using` scope. Context7 `/dotnet/docs` confirmed caller disposal for externally created
`AddSingleton(instance)` registrations at
`https://github.com/dotnet/docs/blob/main/docs/core/extensions/dependency-injection/guidelines.md`.
That current documentation consultation supplies the DI ownership explanation; it is not a versioned
runtime execution result. The host's real Hosting package reference uses the central 10.0.2 pin.

The root SCP test example retains the campaign's named discovery token, caller-owned SCP parameters
and awaited session lifetime. Its proof needs the actual test-project context, separately from these
25 bodies. Relevant source XML includes `IYubiKey.CachedDeviceInfo`, the five current manager methods,
the monitor's watch/disposal, revisioned record members and shared CLI metadata/options contracts.
Shipping compilation and PublicApi tests cover API-analyzer agreement; explanatory XML/code-body
capture still needs explicit candidate disposition by the assigned follow-up owner.

## Prepared project routing, subject to follow-up acceptance

The candidate hosts inherit canonical root props/targets/analyzers and send their generated build
output to `/tmp/opencode/core-upstream-20261111-validation/<project>/`. They are outside the solution:

- `AuditMerge20261111.Examples`: library containing the 25 uncalled bodies.
- `AuditMerge20261111.Consumers`: aggregation of the seven real applet example projects plus current
  Mac HID route and all-module AOT verification source hosts, using `ReferenceOutputAssembly=false`.
- `AuditMerge20261111.MacComparison`: compilation of the actual boundary-comparison project, with an
  explicit current Core DLL binding, current central `BoundaryNativeVersion=1.18.1`, and a distinct
  comparison variant. Its historical default async-package profile stays untouched. This lane follows
  the clean solution build so the explicitly referenced DLL exists.

If the follow-up owner accepts these hosts, exact scoped commands through the canonical logger are:

```bash
V=docs/research/documentation-audit/integrations/core-upstream-20261111/validation
python3 -B "$V/run-command.py" --label consumers-first --toolchain --expect-target build -- dotnet toolchain.cs -- build --project AuditMerge20261111.Consumers --clean
python3 -B "$V/run-command.py" --label examples-first --toolchain --expect-target build -- dotnet toolchain.cs -- build --project AuditMerge20261111.Examples --clean
python3 -B "$V/run-command.py" --label mac-comparison-compile-first --toolchain --expect-target build -- dotnet toolchain.cs -- build --project AuditMerge20261111.MacComparison --clean
```

The follow-up owner must add exact expected build-project rows and evaluated compiler/capture checks
before execution. Each selector has one filename match; re-run canonical preflight after all packet
writers finish. The earlier resolver static selector receipt is `evidence/project-selector-inventory.json`.
At this tree a short build selector `Core` selects the canonical shipping/unit/integration projects,
and the resilience implementation's unit match is exactly canonical Core.UnitTests. Full test selectors
`Yubico.YubiKit.Core.UnitTests`, `Yubico.YubiKit.Cli.Shared.UnitTests`,
`Yubico.YubiKit.Cli.Commands.UnitTests` and `Yubico.YubiKit.Oath.UnitTests` each have one match.

Preserve complete bodies and resource/helper scopes. Capture actual executed Csc membership,
canonical analyzer files, per-project effective policy, restored assets/imports and complete direct/
transitive graph/output identity. A green build alone proves none of those capture properties. Retain
original binary outputs externally before a later clean/rebuild replaces them; perform final current
source and live-binding checks against those exact bytes. Copy/adapt frozen PIV algorithms with a
provenance receipt; preserve every historical artifact and initial failed tool/input version.

## Platform, formatting and preservation bounds

The Linux no-hardware/native transaction is required and separate from solution/unit/consumer gates.
Use the preparer's exact native-only commands and the actual emitted capture directory. It runs JIT/AOT
libc/reactor cases and expected-failure sensitivity controls, with zero physical modes. Retain the full
text capture and sealed publish binding. SDK device discovery and physical scenarios remain unrun.
All-module AOT publish/startup on Linux, macOS and Windows remains a separately assigned workflow lane;
an attached-key workstation is not its no-key startup environment. Compilation does not qualify linking
or native execution on any platform.

After content review, scope solution formatting to `evidence/format-scope.txt`, one argument per file.
The candidate external hosts need their own project scope if formatting is assigned. Verify actual
processed membership rather than accepting an empty formatter match. Recheck source identities after
formatting. The noncomment-line proof is intentionally narrow; a future Roslyn token/directive check
provides a separate compiler-aware dimension.

Use the independently prepared preservation tools before/after meaningful gates. The resolver's
`inspect-resolution.py` supplies additional overlap/API/source checks. `validation/check-records.py`
must snapshot and verify final records only after all writers finish, then check exact staged bytes.
Commit/push remains Astra's release action after acceptance and required gates; before that, `HEAD`
and `MERGE_HEAD` are the pinned parents, not a published integrated audit baseline. PIV's physical
deferral and the prohibition on a full Core audit remain in force.
