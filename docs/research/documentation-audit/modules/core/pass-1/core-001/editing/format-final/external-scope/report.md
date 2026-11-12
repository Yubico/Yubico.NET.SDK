# Folder whitespace follow-up — two introduced EOF bytes only

## Independent-review decision

Review only `eof-only.patch` in this directory: remove one terminal LF from each
of `verification/LinuxHidRouteVerification/Evidence.cs` and
`verification/LinuxHidRouteVerification/OperatorScenarios.cs`. Both lacked that LF
at source pin `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; the current accepted
copies have it. The patch is **unapplied** and preserves every preceding byte.
See `eof-identities.json` and the literal-UTF-8 `source-snapshots.json` for exact
before/after source bytes and hashes. No generated gzip is retained in RUN.

The final declaration in `Evidence.cs` is a semicolon-terminated partial class:
`internal sealed partial class EvidenceJson : JsonSerializerContext;`.
The final line in `OperatorScenarios.cs` is `}`. Neither is a comment or directive;
the complete final declaration/brace bytes are retained. The total proposed
deletion is exactly **two bytes**, both `0a`.

Patch SHA-256:
`f5c82111cb86e52a8c5a0de620f487efff3f934d28e529389243aa5c5105e818`.
Text snapshot bundle SHA-256:
`fb60b98283f685a1b30e9d0d1262942855b2ea667fc710adf14bed72e4a36cd9`.

## Actual failed gate retained

The retained `validation/commands-v2/format-whitespace-final-tree` invocation is:

```text
dotnet format whitespace . --folder --include <310 separate accepted C# paths> --verify-no-changes --report <external report> --verbosity diagnostic
```

It completed with **exit code 2**, with a report containing **73 WHITESPACE and
16 FINALNEWLINE diagnostics**. The stdout summary is **“Formatted 17 of 310
files.”** This is a verify-only run: these are proposed-change files, not written
files. The actual command/report/receipt/log identities and exact 310-path include
list are retained in `source-evidence.json`. All 310 requested paths are included
in the folder workspace population reported by this run; the earlier 16
solution-unmatched paths are no longer unmatched for this **folder whitespace**
gate. That does not establish solution membership, semantic/style/analyzer
coverage, successful compilation, or equivalent conditional-compilation context.

This proposal addresses only the **two introduced EOF findings**. It does not
make the failed folder gate clean. The remaining 14 baseline EOF findings and
all 73 baseline/context-sensitive whitespace findings remain explicitly open
under the preservation fence. No formatter was replayed here, including at the
pin; baseline classification is based on direct source evidence, not measured
pinned diagnostic output.

## Preserve the 14 baseline EOF findings

`baseline-findings.json` records all 16 current/pinned terminal-newline states,
exact current/pinned EOF context, import/directive sequences, and report sites.
The other 14 files already had a terminal LF at the source pin and are not in
this patch:

```text
benchmarks/Yubico.YubiKit.PerformanceBenchmarks/AsyncBoundaryBaseline.cs
benchmarks/Yubico.YubiKit.PerformanceBenchmarks/Program.cs
scripts/toolchain/Process.cs
scripts/toolchain/Results.cs
scripts/toolchain/TestTargets.cs
src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/SecureCredentialTests.cs
verification/MacOSHidBoundaryComparison/Program.cs
verification/MacOSHidInputVerification/Program.cs
verification/MacOSHidRouteVerification/ListenerDrainScenario.cs
verification/MacOSHidRouteVerification/Program.cs
verification/MacOSHidRouteVerification/SmartCardScenario.cs
verification/NativeAotVerification/Program.cs
verification/NativeCompatibilityVerification/Program.cs
verification/NativeCompatibilityVerification/RuntimeConsumer/Program.cs
```

In particular, keep the three `scripts/toolchain` EOFs unchanged. This proposal
does not change profile/toolchain inputs, configuration, or their identities.

Two baseline EOF report coordinates are virtual post-whitespace-reflow locations,
not current physical source line numbers: AsyncBoundaryBaseline reports 464 versus
current 431 (33 proposed inserted newlines), and Mac comparison reports 420 versus
current 384 (36 proposed inserted newlines, plus one whitespace-only deletion).
The recorded whitespace proposals account exactly for both offsets. The two
introduced EOF sites have no preceding whitespace proposals and match their
current physical source positions exactly. No source was reflowed to manufacture
agreement with the virtual coordinates.

## Preserve the 73 whitespace findings

| Source | Count | Source-pinned classification |
|---|---:|---|
| `benchmarks/Yubico.YubiKit.PerformanceBenchmarks/AsyncBoundaryBaseline.cs` | 33 | Existing compact executable-code layout. Reported source spans map exactly to unchanged pinned lines after documentation-only line shifts. |
| `verification/MacOSHidBoundaryComparison/Program.cs` | 37 | Existing compact executable-code layout. Reported spans map exactly to pinned lines, shifted by the added XML summary. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Scp/AesCmacTests.cs` | 3 | Conditional-compilation formatting context, including inherited four-space indentation in the `#else` branch. |

Every reported span has current/pinned source evidence in `baseline-findings.json`.
All involved current/pinned import sequences, preprocessor directive sequences,
and namespaces are checked separately and left unchanged.

### AesCmac conditional context — do not reflow or change assertions

The solution formatter previously loaded Debug/net10.0 project configuration.
This folder invocation does not load that project or pass its `DEBUG` defines.
The source is explicitly `#if DEBUG` / `#else` / `#endif`:

`conditional-context.json` retains the full current/pinned method block, the
unchanged assertion text, and relevant actual Debug configuration log entries.

- Current/pinned lines 53–54 contain the same statement-ending two LF bytes.
  The folder report nevertheless requests a two-character whitespace replacement
  at 53:47. The report alone does not justify inventing a byte-level edit there.
- Current line 59 is a documentation-pass replacement of the pin's two release
  comments (pin lines 59–60). Its **four-space leading indentation** was inherited
  from those comments; the new prose is not falsely classified as byte-identical
  to the old prose. The folder report requests four more spaces at 59:5.
- Current line 60 is exactly the pin's line 61:
  `    Assert.True(true);`. Its four-space indentation and every assertion token
  are unchanged. The folder report requests four more spaces at 60:5.

The `#else` indentation requests are consistent with that branch being active in
the folder-default parse but inactive under `DEBUG` in the solution context.
This is not a Release build or semantic test result. Preserve the existing
`Assert.True(true)` tokens and the accepted release-comment content; do not rename,
reflow, suppress diagnostics, or change preprocessor/style policy to force a pass.

## Verification and coordinator handoff

`verification-receipt.json` records exact two-byte proof, patch dry-run against
isolated before copies, current accepted-product hash checks, prior 239-file EOF
after-state checks, predecessor-record identities, and static content admission
using the preserved checker classifier only. Full Git-clean/index admission is
not executed. Source/policy/checker/predecessor records are not modified.

The coordinator must independently review this new two-file patch, preflight
both before hashes, apply only if authorized, verify both after hashes, and
update the accepted map through its existing mechanism. Keep the actual failed
formatter qualifications above. The prior 239-file EOF patch/review/text bundle
are separate, unchanged predecessors. No SDK, formatter, test, native/device, or
subagent execution occurred in this bounded preparation.
