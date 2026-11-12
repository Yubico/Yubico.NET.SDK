# External documentation successor: primary verification and benchmark ownership

## Outcome

Revision 2 closes the missing primary-path ownership assignment. The actual union of the primary
`B2-linux-lifetimes.json`, `B3-macos-lifetimes.json` and `B4-windows-support.json` packet paths, filtered
to `verification/` or `benchmarks/` and `.cs`/`.md`, is **26 paths**: B2 contributes 9, B3 10 and B4 7,
with no overlap. All 26 have an explicit decision: **12 edited, 13 retained and one historical README
held byte-identical**. No path is still deferred merely because it was absent from shared-sections.

One supporting project file also receives a required XML-comment correction. The **13 revision-2
edits** join the original 19 to make **one 32-file uncommitted successor product patch**. All original
19 raw product hashes, the original patch, initial report and every initial record remain unchanged.
The successor is for full-batch independent review, not blanket acceptance of prior editor referrals.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-external`. Base and unchanged HEAD:
`da66f5d01ba594993974ddd26723e8c300c0a454`. Source pin:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Only this worktree's explicitly allowed documentation/comments
and new `editing/external/revision-2/*` records were written. Other worktrees were read-only. No Git
index/ref update, commit, build/test, formatter, native/device/provider/crypto operation or subagent occurred.

## Transfer and preservation records

The files below are relative to this revision-2 directory. The initial return at `../../../edit-external.md`
and initial `../source.patch` remain historical inputs, not overwritten successors.

| Record | Purpose |
| --- | --- |
| `successor-source.patch` | One complete 32-file product diff against the named own-worktree base, including all initial edits |
| `expanded-allowlist.json` | Exact packet selection/counts, all 26 primary paths, permitted supporting formats and full successor write allowlist |
| `history-preservation.json` | Frozen initial output hashes, original patch/report identity and unchanged initial 19 raw product hashes |
| `path-decisions.json` | Every added primary/support path and all original 37 exclusions: edit/retain/hold, substantive reason, source anchors, candidate links and base/current hashes |
| `full-successor-path-decisions.json`, `full-successor-candidate-decisions.json` | Consolidated initial/successor decisions and current hashes; original records remain unchanged |
| `semantic-decisions.json` | Authored per-path source/assertion reasoning used to assemble that receipt |
| `candidate-decisions.json` | Original local/proposal IDs, definitions/facets and exact existing evidence units/range hashes; no new accepted IDs |
| `remaining-referrals.json` | Precise remaining D-comment scope holds, protected literal/data issues and independent qualification limits |
| `example-bodies.json` | All 243 original/current initial example bodies plus four retained historical compatibility command bodies; no new consumer example |
| `input-identities.json` | Completed B2/B3/B4 reports/candidate/file evidence and transport/protocol referrals consumed read-only |
| `static-preservation.json` | Exact comment-only/XML-structure checks, scope/counts, unchanged HEAD and full patch hash |
| `final-checks.json`, `output-identities.json` | Final hash/scope/fence/history checks and owned successor-record identities |
| `record.py` | Bounded static receipt assembler; never imports or executes the frozen initial assembler |

The successor compares **only this worktree** with its base. Accepted A/C/protocol/FIDO product changes
in the primary checkout are neither treated as unexpected drift nor compared with raw base bytes.
Primary completed investigation/referral records are read-only source evidence, with their own identities.

## Applied D facets

| Paths | Source-supported correction and retained limit |
| --- | --- |
| `benchmarks/Yubico.YubiKit.PerformanceBenchmarks/AsyncBoundaryBaseline.cs`, `Program.cs` | Separate invocation return/task terminal and watchdog censoring from native drain. GetInfo checks status, not complete map parsing. Asset/deployed-path hashes do not observe the process-loaded producer. The ordinary fixture picks first matching transport without enforcing serial/FW 5.8. Runtime help/error text and measurements are unchanged. B4 C008 / FPT-055. |
| `verification/LinuxHidRouteVerification/Evidence.cs`, `OperatorScenarios.cs` | Baseline is the original comparison checkpoint; SourceHash alone is a supplied label. Measurements are wrapper/process-wide, failures add no row. Operator modes have prerequisites, not execution credit from compilation; setup can change applet selection without persistent provisioning. B2 C002 plus scoped B4 C008 evidence limits. |
| `verification/MacOSHidBoundaryComparison/Program.cs` | Distinguish historical async.8 before/after mode from per-source CurrentProfile. Keep fixed pins, partial/censored counts and honest missing loaded-path/producer/native-only/firmware fields. Compiling either mode is not macOS execution. B3 C008 / FPT-046. |
| `verification/MacOSHidInputVerification/Program.cs` | Identify the nonshipping experiment ABI rather than shipping NativeShims input exports. Synthetic callback/buffer/overflow/self-wait oracles supply no shipping driver acceptance. B3 C008. |
| `verification/MacOSHidRouteVerification/ListenerDrainScenario.cs` | State actual callback/stop/cleanup-order assertions: CloseResult is recorded but not required zero. Late-drain starts with a real callback; it does not cover ambiguous no-worker partial startup. Preserve root/callback and native-result conditions separately. B3 C004/C005/C008. |
| `verification/MacOSHidRouteVerification/Program.cs`, `SmartCardScenario.cs` | Correct no-device-mutation/read-only prose: queries do not provision persistent data, but discovery/SELECT can affect applet/security state. No physical PIN isolation/clearing inference. Existing finite pending-read, borrowed-reuse and predispatch-cancel assertions/outputs stay intact. B3 C008. |
| `verification/NativeAotVerification/Program.cs` | Ten entry-type references are selected compile/link reachability, not every public method. Monitor mode uses three WatchAsync sinks; rapid activity asserts at least one Added/Removed, not three cycles. Cross-sink snapshots occur while pumps run, not after an equal-prefix barrier. Preserve every instruction/output/assertion. B4 C008. |
| `verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj` | Correct the false filename-exclusion comment: the file is named Yubico.YubiKit.*; external-src/nonpackable status are the relevant conditions. Ordinary build is not native publish/execution. Every XML element, attribute/reference/property value is preserved. B4 C008. |
| `verification/NativeCompatibilityVerification/Program.cs`, `RuntimeConsumer/Program.cs` | Bound fixed assets to historical macOS arm64 1.18.0/async.8, export names plus BN allocation/free, and the four SCard import paths. Explicit release failure leaves fallback eligible but does not observe ReleaseHandle or guarantee disposal before unhandled process termination. No successful connect/transmit, current Windows/stable producer or all-signature qualification. B4 C008. |

Retained files received substantive decisions rather than manufactured edits. Examples include the
accurate Linux socket/reactor and held synchronous-control scope, CurrentProfile's existing missing-field
qualifications, the macOS expert adapters' explicit borrowed ownership, and synthetic self-tests' finite
classification oracles. The compatibility README's dated hashes, working-tree snapshots, failure controls
and command recipes are preserved. Exact anchors/reasons are in `path-decisions.json`.

## Reconciliation of the original 37 exclusions

The prior blanket format dispositions are superseded by **per-path semantic decisions**, without
changing any initial record. The original 37 paths are all reconsidered against their actual comment,
configuration or literal-data surfaces:

- Workflow AOT comments already distinguish link/startup/discovery from hardware/monitor/applet
  acceptance. Fork-credential and PC/SC setup comments describe actual conditions/steps. They need no
  factual D rewrite; changing commands or strengthening test gates remains M/P.
- Root props/targets describe opt-in/analyzer/import timing; package pins are source dependencies,
  not resolved SDK security or native-producer evidence. SourceLink rationale retains the exact B4
  advisory qualification. No compiler/analyzer, package, configuration or credential value changes.
- Architecture scripts describe their bounded structural/source/artifact checks. They supply no new
  SDK/native acceptance. No candidate-associated D rewrite was selected; directives, heredocs and
  configuration values are preserved rather than starting a broader tooling audit.
- CLI/test project comments match their current utilities/extensibility/test-seam roles. No factual D
  correction requires changing project configuration. Initial source/comment corrections remain intact.
- Migration state/map and JSON surfaces have no permitted comment facet carrying the outstanding
  correction. YAML value/parse repair (including original DOC-002), SDK selection, rendering values,
  device allow-list data and executable help/output remain explicitly held.
- Toolchain files now have individual reasons distinguishing no false local comment, useful disclosure
  already supplied in TOOLCHAIN, protected literal output, and actual D-comment mismatches.

**Three D-comment facets stop at a precise format-scope ambiguity.** The added writable list names
`.py/.sh/.yml/.yaml/.csproj/.props/.targets`, not `scripts/toolchain/*.cs`. Those scripts were explicitly
read-only in the initial assignment. This revision therefore does not infer additional C# script writes:

| Held D facet | Required narrow correction; separate non-D work |
| --- | --- |
| `scripts/toolchain/TestTargets.cs:158–168`, A C018 / FPT-018 | Describe separate emitted same-kind arguments instead of claimed wildcard combination/AND preservation. Pinned repeated-switch runner semantics remain unrun; no filter/guard/assertion/help change. |
| `scripts/toolchain/Results.cs:97–109`, B4 C008 / FPT-055 | Scope EnsureTestsRan XML to failed/all-NoTestsExecuted outcomes. The current gate does not require each result to contain nonzero counts. Tightening classification/admission remains M/P. |
| `scripts/toolchain/Process.cs:11–14`, B4 C007/C008 | Narrow “every process” to processes dispatched through Exec; RunLog.Git launches Process directly. Command-display masking is not complete-argv persistence redaction or child-drain proof. Runtime logging/redaction work remains M/P. |

These are not blanket file-type holds or claimed fixed code. They are source-supported D actions with
exact anchors, stopped pending explicit permission for that omitted script format. RunLog's current
summary does not promise redaction; full-argv exposure is already disclosed in the unchanged initial
TOOLCHAIN edit and remains separate runtime/retention work.

## Static proof and remaining qualifications

The fresh receipt verifies all 26 primary identities/decisions, all 37 successor dispositions, initial
history and original 19 hashes. For each new C# edit, every line other than a full-line `//`/XML comment
is equal, **including blank lines**. Existing inline comments, imports, directives, declarations,
attributes, literal output, guards and assertions are preserved. Changed XML documentation parses.
For the project-file edit, exact text outside XML comments and the parsed element/attribute/text
structure are identical. Raw/LF/BOM/final-newline hashes/flags are recorded separately. The patch tool
normalizes that project's CRLF checkout to LF; the receipt makes that representation change explicit.
Unedited support files have normalized-byte equality, not invented parser or execution credit.

`git diff --check`, exact successor write scope and unchanged HEAD are checked. No SDK tests, Python
test modules, build/publish hooks or package/native loads were executed. There is no new framework,
runtime experiment, example compilation, Roslyn semantic binding, independent source review, physical
acceptance or all-schedule concurrency proof. No new consumer example body was introduced.

Retain the original B2/B3/B4/B5/B6 authority limits: published NativeShims 1.18.1 source is available,
but it does not identify a process-loaded producer/header build. Mac `.8` observations, stable-source
compilation and current runtime qualification are different evidence. Native-only Linux source records
are not physical runs. Original SCP public-release/public-review/final-profile distinctions, cleanup
allocation scopes and unrun experiments remain unchanged.

The user's Windows concurrency handoff remains UNREADY/UNREVIEWED, PIV physical deferrals stand,
serial 9681620's historical cause remains unexplained, and serial 125's 6A88 setup failures supply no
protected reset result. Return the full successor batch for independent review and existing publication
gates. Finish Core Pass 1 and its deferred handoff, then stop; no new investigation or Pass-2 project.
