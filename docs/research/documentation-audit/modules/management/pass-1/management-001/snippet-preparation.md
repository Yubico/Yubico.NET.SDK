# Management snippet preparation

## Status

Prepared **63 independent compile-only source groups** and an inventory of **83 excluded fences**.
No helper rebuild, snippet compilation, SDK build, tests, hardware, commits, or delegation were run
in this follow-up. The coordinator reports that the earlier helper build, initial two-file token
comparison and all 20 API snapshots passed; those results predate the helper changes here and are
not new validation evidence for these snippets.

Current extraction uses C's edited files from `/tmp/opencode/management-001-edit-c`, and A/B XML from
PRIMARY `/home/rupicola/Code/y/Yubico.Yubikit.NET`. **Rerun extraction against final PRIMARY after C
integration and review corrections.** Hashes describe the extraction inputs, not an already-reviewed
final release. This report does not authorize executing any extracted operation or test.

## Ready paths and commands

All paths below are relative to the primary repository:

- `validation/compiled-examples/extract-examples.py` beneath this run: rerunnable extractor and
  explicit classification rules/locators.
- `validation/compiled-examples/manifest.json`: exact compilation inventory, explicit reference paths,
  excluded-block reasons and per-input provenance/hashes.
- `validation/compiled-examples/<group>.raw.txt`: unwrapped extracted C# (XML entities/doc prefixes
  decoded where necessary; trailing syntactic whitespace normalized). The manifest also retains exact
  pre-normalization extracted text and its hash losslessly in JSON.
- `validation/compiled-examples/<group>.cs`: complete typed compilation input.
- `validation/checker-source/src/Program.cs`: compiler now accepts repeatable `--reference <DLL>`.
- `validation/checker-source/compile-examples.py`: manifest runner forwards explicit references and
  rejects a generated source whose hash no longer matches the manifest.

Run these commands from PRIMARY. The first is extraction only and needs Python, not the SDK:

```bash
RUN=docs/research/documentation-audit/modules/management/pass-1/management-001

# Current split-root extraction, before C has been integrated:
python3 "$RUN/validation/compiled-examples/extract-examples.py" \
  --guide-root /tmp/opencode/management-001-edit-c

# Final extraction after integration/review; BOTH input roles now default to PRIMARY:
python3 "$RUN/validation/compiled-examples/extract-examples.py"

# Coordinator only: after final SDK/sample/test-project Release compilation, load domain-build.
# This rebuild is necessary because --reference support changed the helper since its first build.
dotnet toolchain.cs -- build --project Management.DocCheck

CHECKER=/tmp/opencode/management-001-doccheck/bin/Release/net10.0/Yubico.YubiKit.Management.DocCheck.dll
python3 "$RUN/validation/checker-source/compile-examples.py" \
  --repo "$PWD" --checker "$CHECKER" \
  --manifest "$RUN/validation/compiled-examples/manifest.json"
```

The extractor also accepts `--root <primary checkout>` for A/B XML and default guide inputs.
It overwrites its generated `<group>.cs`, `<group>.raw.txt`, and manifest; correct source documentation
and rerun, rather than hand-editing generated sources. It neither builds nor executes anything.
An ordinal/anchor/language mismatch fails extraction instead of silently selecting a different block.
Changed block counts also fail for every inventoried file, requiring explicit reclassification rather
than silently dropping a new example into an unrelated-example exclusion.
Inspect command exit status and retain compiler output; an unguarded `tee` must not mask failure.

For a single group, explicit references can be passed independently of SDK module references:

```bash
dotnet "$CHECKER" compile "$RUN/validation/compiled-examples/tool-reset.cs" "$PWD" \
  --reference "$PWD/src/Management/examples/ManagementTool/bin/Release/net10.0/ManagementTool.dll"
```

All SDK module references remain real `src/<Module>/src/bin/Release/net10.0` binaries. Nonstandard
references come only from explicit real Release output paths in the manifest, not guessed module
locations. The compiler hashes each supplied DLL at invocation, including additional references;
the runner prints exact command arrays and source hashes. Framework trusted-platform metadata remains
available; an explicitly supplied filename supersedes a matching runtime metadata filename. No
generated assembly is loaded, saved or executed. Helper `obj`/`bin` remain under `/tmp/opencode`.

## Coverage and exact block mapping

Fence ordinals count **all** Markdown fences; XML ordinals count `<code>` elements. These agree with
`edit-c.md`'s final classification and `edit-a.md` / `edit-b.md`'s separate XML ownership. The machine
manifest additionally records source-root role, document SHA-256, block line, stable anchor in the
provenance string, raw extracted SHA-256, generated source SHA-256, imports/scaffold assumptions and
actual reference paths. There is one compilation unit per selected block, including repeated test
names in independent namespaces/classes; alternatives are never concatenated into overlapping sessions.
`reference_inventory` records provisional DLL hashes at extraction (or null for absent outputs); final
compiler invocations hash actual DLL bytes again, after the coordinator's final Release builds.

| Source | Selected blocks | Groups / coverage |
| --- | --- | --- |
| `src/Management/README.md` | 2–7 | 6: bounded starter; query; full supported-mask guard/write/delay/rescan; lock creation/finally; authorized lock removal/finally; SmartCard Bio MPE reset |
| `src/Management/CLAUDE.md` | 1–25, 28–29 | 27: all public/contributor/test/helper/configuration/flags/version/logging/enumeration snippets; internal excerpts 26–27 excluded |
| `src/Management/tests/CLAUDE.md` | 1–2 | 2: distinct test-member classes for filtering and actual cached/fresh query helper |
| ManagementTool `LockCodePrompt.cs` XML | 1 | Prompt, cancellation branch, actual authorized timeout write and zeroing finally |
| ManagementTool `DeviceConfiguration.cs` XML | 1–2 | Actual capability/reboot helper and real lock-code write with both cleanup paths |
| ManagementTool `DeviceInfoQuery.cs` XML | 1 | Actual connection/factory/query/display and both owned disposals |
| ManagementTool `DeviceReset.cs` XML | 1 | Actual query/guard/reset and both result branches; no task omitted |
| `src/Management/src/IManagementSession.cs` XML | 1 | A's actual SmartCard connection preference, owned session and destructive reset |
| `src/Management/src/DeviceConfig.cs` XML | 1 | B's actual read/derive mask/configure-with-reboot task |
| `src/Tests.Shared/YubiKeyTestStateExtensions.cs` XML | 1–3 | Current theory/state/read comparison, synchronous cached callback, and actual borrowed-connection session/read |
| `src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs` XML | 1–2 | All three independent test bodies, criteria and real custom-filter declarations |
| `src/Tests.Shared/README.md` | 1, 4, 5, 6, 7, 8 | Complete quick-start class (both reads/SCP), basic test, actual SecurityDomain integration-helper reset/key-info task, custom filter+callback, pinned OTP-HID read/assert, and actual Oath extension task |
| `src/Tests.Shared/CLAUDE.md` | 6 | Actual Oath helper declaration/callback/disposal, independent static extension class |
| `docs/TESTING.md` | 11, 12, 13, 15 | All three eligible-device examples, default cached/fresh task, all pinned concrete rows/assert/read, modern HID-FIDO test |
| root `CLAUDE.md` | 14 | Actual **xUnit v3** TestContext token propagation, discovery, SCP, read/assert and owned session |
| `docs/architecture/physical-device-model.md` | 3–4 | Read-only typed metadata and both bounded default/OTP sessions with actual reads |
| `docs/migration/v1-to-v2.md` | 8 | Current V2 discovery, owned session, query and displays |
| PivTool `SDK_PAIN_POINTS.md` | 6 | Management query/disposal completes before actual PIV factory await; caller owns shared SmartCard connection |

This covers all **43 final module Markdown fences** by selection or explicit non-C#/internal-excerpt
exclusion; all five ManagementTool XML blocks; both A/B XML blocks; and the selected current shared
references enumerated in C's report. The expanded inventory across the directly related documents is
146 blocks total. Commands, JSON, directory maps and conceptual flow fences are recorded but not run.
Unrelated examples in the broader repository are not implicitly certified.

## Typed prerequisites; no surrogate SDK implementation

- **M:** real Core + Management metadata; System, collection/LINQ, threading/tasks, cryptography and
  actual Core abstraction/device/session/typed transport/APDU/SCP namespaces. Additional imports are
  compiler scaffolding, not implicit application state. Preserve the real host/runtime prerequisites
  described in `edit-c.md` (supported masks, lock authorization, dedicated devices, Bio MPE destruction,
  SCP setup, transport exposure and ownership).
- **T:** M plus actual `Yubico.YubiKit.Tests.Shared.dll` from its Release output and **xUnit v2**
  `xunit.core`, `xunit.assert`, `xunit.abstractions`, `Xunit.SkippableFact` from the Management integration
  project's Release directory. This uses the genuine WithYubiKey attribute, state, filters and helper
  extension methods. It does not imitate them, execute discovery, or attempt to mix v2/v3 attributes.
- **V3:** M plus real `xunit.v3.core`, `xunit.v3.assert`, `xunit.v3.common` from Management.UnitTests
  Release output; only the root SCP example uses this profile. No Tests.Shared/xUnit v2 reference.
- **SD:** T plus the actual SecurityDomain SDK Release DLL and
  `Yubico.YubiKit.SecurityDomain.IntegrationTests.dll` from that project's Release directory; imports
  its real public `TestExtensions.TestStateExtensions` extension namespace. The helper is not copied
  or reimplemented; the full filtered-test reset/list/assert task is retained, never executed.
- **E:** M plus real `ManagementTool.dll`, with its existing public prompt, feature and result types.
  No helper class implementation is copied. Its five XML tasks remain intact.
- **L:** M plus explicit real logging/console/DI-abstractions/options DLLs from ManagementTool Release
  output. Existing logging setup and owned session/factory disposal remain intact.
- Oath helper declarations additionally reference the real Oath Release DLL. The PIV borrowed-session
  example additionally references the real Piv Release DLL. There is no copied applet implementation.

Method wrappers expose only the exact required typed parameters: `IYubiKey device`/`yubiKey`,
`CancellationToken cancellationToken`/`ct`, `YubiKeyTestState state`, caller-owned
`IManagementSession session`/`ManagementSession mgmt`, idle caller-owned `ISmartCardConnection connection`,
or actual `ProtocolConfiguration customProtocolConfig`. Lock removal receives caller-owned
`byte[] currentLockCode`; application acquisition in the lock-change sketch receives typed
`Func<byte[]?> GetCurrentCode` / `Func<byte[]> GetNewCode` delegates. No delegate implementation,
fake API, no-op input provider, replacement library class or artificial hardware initializer is added.

Synchronous metadata/flags fragments get `void` wrappers rather than newly introduced no-await async
methods. Complete test members are wrapped as instance class members; Oath extension members get static
classes. The C#14 extension-container teaching block is retained verbatim in an isolated namespace so
it cannot shadow the actual Management extension for consumers. Only existing namespace `using`
directives are relocated outside method wrappers, with their original directive text. Trailing syntactic
whitespace is normalized in readable raw/generated source files to keep added artifacts whitespace-clean;
the manifest's `extracted_text` / `extracted_sha256` preserve original extracted text losslessly. A
multiline/verbatim string with such trailing whitespace fails extraction pending explicit review rather
than risking changed literal content. No statement,
callback, awaited call, branch, reset/write, assertion, finally or disposal is removed to get a pass.

## Explicit exclusions

Each excluded block has an individual manifest entry with root/path/ordinal/line/document/raw hashes
and reason. Important deliberate exclusions:

- Management guide 26: private-constructor/protected-initializer factory excerpt; actual SDK build
  validates that implementation, not a fabricated public replacement.
- Guide 27: internal session field and backend interface in deliberately different source scopes.
- Tests.Shared guide 1–5: unchanged partial harness/filter/allow-list/provider skeletons.
- Tests.Shared README 9: deliberate
  `SomeSession`/`expectedInstruction` recorder sketch, no authorized fake implementation.
- Historical V1 Device Info and retired DeviceId parsing, deliberate pre-enforcement ownership conflict,
  illustrative root model/validation-only skeletons, and unrelated PIV/other-applet teaching examples.
- All non-C# commands/configuration/flow/directory fences; no shell, test or device operation is run.
- Historical audit records and `v1-to-v2-gaps.md` are not extraction inputs and remain immutable.

## Source concerns and pending checks

1. **Likely source warning:** Tests.Shared README block 6 (`shared-readme-6.cs`) currently passes an
   `async` callback with only `Assert.True`, no `await`. This likely produces compiler warning CS1998,
   which this checker treats as failure. It is preserved exactly, not hidden by adding a fake awaited
   task, suppressing the warning or dropping the callback. C/reviewer should correct the actual example
   if confirmed, then rerun extraction. This is static inspection, not a compiled diagnostic claim.
2. DLL paths were selected from actual Release output layout. The coordinator must build the final
   real Tests.Shared/integration dependencies, ManagementTool and relevant SDK modules before compiling
   groups. Missing/stale DLLs are not fixed by copying their source into the harness. Additional real
   metadata dependencies, if compiler diagnostics require them, must be explicitly added to the
   profile/manifest rather than silently auto-loaded or stubbed.
3. This helper revision's `--reference` parsing and C# source are **not rebuilt yet**. Python AST and
   manifest/hash/extraction checks cannot prove Roslyn compilation or API binding. Compilation checks
   are limited to syntax/binding/compiler warnings, not SDK analyzer parity or execution semantics.
4. Review corrections can change content while retaining locators. Rerunning regenerates raw and source
   hashes from actual final PRIMARY inputs. If a locator moves, update the explicit classification rule
   after confirming the source/report, never silently skip the example. Final logs must identify the
   final integrated source/doc hashes and actual Release DLL hashes.

The coordinator's late steering about `review-c-module.md` is covered by whole-block extraction:
contributor block 10's new caller-owned `using var scpParameters` and its options assignment are retained
automatically, including the real factory and both awaited reads. README secret/reboot qualifiers,
top-level-help wording and test-command labels are rehashed with their containing documents. Earlier
hashes are snapshots, not immutable acceptance assertions; final PRIMARY extraction remains mandatory.
Subsequent shared-review steering promotes Tests.Shared README 5 from the earlier unchanged exclusion
to a real SecurityDomain integration-helper compilation group. Root SCP and Quick Start/Filtered Test
parameter scopes are captured from their complete blocks; no initial hash is treated as final. The root
fake-APDU-boundary teaching sketch remains outside executable Management tasks, not fabricated as a
standalone runnable example.

## Static preparation checks performed

- Python AST parsing passed for both extraction and manifest-runner scripts, without bytecode output.
- Latest split-root extraction completed: **63 groups, 83 exclusions**. Its `guide-10.raw.txt` contains
  `using var scpParameters = Scp03KeyParameters.Default;`, the variable's options assignment, both awaited
  reads, and the explicit session-before-parameters disposal comment from C's correction.
- All 63 generated/raw hashes matched the manifest. A line-order check confirmed every original
  non-import/nonblank extracted line survives in order in its generated input, apart from documented
  trailing syntactic whitespace normalization; namespace imports are relocated by the scaffold.
- All **20** distinct SDK/external Release reference paths currently exist and have provisional
  extraction hashes. Existing files are not proof that final source has been rebuilt.
- No `obj`, `bin`, or `__pycache__` directory was created under retained tooling or examples; this
  report's fences are balanced.

These are static preparation results only, not helper/snippet/compiler/analyzer/test/hardware passes.
