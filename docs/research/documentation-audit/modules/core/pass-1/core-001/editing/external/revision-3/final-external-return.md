# Final external documentation successor

## Outcome

All three explicitly released toolchain D-comment facets are now edited. **No D-comment format-scope
hold remains.** The final uncommitted product diff contains **35 files**, derived from the actual Git
diff: the preserved 32-file revision-2 batch plus exactly three disjoint comment-only script changes.
This is the single full external successor for independent review, not three separate review handoffs.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-external`. Base and unchanged HEAD:
`da66f5d01ba594993974ddd26723e8c300c0a454`. Source pin:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.

Final product patch: `final-successor-source.patch`.
SHA-256: `76244276822b64598a78ab11b2db94d116da4bf021b555a73d4e02acaca836db`.

Every initial record, the original 19-file patch and its raw product hashes, every revision-2 record,
the full 32-file revision-2 patch and those 32 raw product hashes remain unchanged. The earlier reports'
three scope holds describe those historical checkpoints; this revision explicitly discharges them
without rewriting their bytes. Both frozen output sets contain 16 bound record identities each.

Only the three authorized script comments and new `editing/external/revision-3/*` records were written.
Other worktrees were read-only. No validation tooling, profiles, runtime/configuration values, build/test,
formatter, native/device/provider/crypto operation, Git index/ref update, commit or subagent occurred.

## Exact three-file comment delta

All base coordinates below refer to the unchanged product pin/base source, not a later primary edit.

| Path / original identities | Applied wording and retained qualification |
| --- | --- |
| `scripts/toolchain/TestTargets.cs`, CORE-A-C018 / FPT-018 | Replace duplicate/stale TranslateToMtpFilter comments at base158–168. The branches171–187 and190–256 reject mixed operators, restrict positive alternative flag kinds and append a separate native flag per term. Two AND-split method patterns are not combined into one wildcard. Emitted flags do not prove the pinned runner's same-kind boolean selection; CORE-A-C018/A-E05 remains unrun. Error/help literals and every filter expression/branch are unchanged. |
| `scripts/toolchain/Results.cs`, CORE-B4-C008 / FPT-055 | Scope EnsureTestsRan XML at base97–109 to the actual failed/all-NoTestsExecuted outcome predicates. Explain empty/mixed and unknown-count limits without claiming per-project nonzero execution or introducing a new admission policy. No gate, count parser, assertion or error text changed. |
| `scripts/toolchain/Process.cs`, CORE-B4-C007/C008 / FPT-054/055 | Scope Exec XML to processes dispatched through it and full step logging to when a step log exists. Dotnet.Capture:130–150 and RunLog.Git:451–467 launch directly. Exec.Mask:114–115 masks supplied secrets in shown commands, not captured output or RunLog.Start:55–64 stored complete argv. No process/masking/redaction/drain behavior or credential value changed. |

The statements describe current source limitations, not desired test policy or repaired behavior.
The original qualified family records remain authority: `investigation-A.md:320–331`, B4's completed
candidate/report and `investigation/B4/file-evidence.md:177–191`. No new library research, protocol
requirement, specification edition/section, native claim or accepted finding ID was introduced.

## Full successor and record map

Paths below are relative to this revision-3 directory.

| Record | Purpose |
| --- | --- |
| `final-successor-source.patch` | One complete 35-file product patch against the own-worktree base, conventional a/b prefixes |
| `comment-delta.patch`, `comment-delta.json` | Exact three-file delta, old/current full-line comments, source anchors, IDs and hashes |
| `allowlist.json` | Exact 124-path final active/comment allowlist: revision-2 ownership plus only the three released script paths |
| `path-decisions.json` | All 164 consolidated paths with final edit/retain/hold reasons and base/current identities; three previous holds become EDIT |
| `candidate-decisions.json` | Hashed immutable earlier candidate map plus final three-path D dispositions and retained independent facets |
| `citations-and-examples.json` | Exact source/report anchors, preserved citation qualifications and hashed full example inventory |
| `remaining-referrals.json` | Zero remaining comment-scope holds; protected data/runtime and independent qualification needs retain actual reasons |
| `history-preservation.json` | Both immutable output sets and unchanged earlier 32 edited product hashes |
| `static-preservation.json`, `final-checks.json` | Fresh comment/XML/hash/scope preservation and actual counts/patch identity |
| `output-identities.json` | Final owned record identities, excluding itself to avoid a self-hash cycle |
| `static-attempts.md`, `record.py` | Bounded receipt extraction correction and reproduction; neither older assembler was executed |

The 26 primary verification/benchmark decisions from revision 2 remain **12 edits, 13 retains and one
historical README hold**. Their ownership gap stays closed. The original 37 excluded paths now have
**three comment edits, 29 semantic retains and five protected literal/configuration holds**, rather
than a blanket format exclusion. No new path enters the 164-path decision population: these three
scripts were already present. Earlier Core/applet/shared-guide ownership limits are unchanged.

The full example inventory is unchanged: **243 initial original/current bodies plus four retained
historical compatibility command bodies**, with their complete text, hashes and classifications in
the bound revision-2 inventory. No new code example body was introduced and none was compiled or run.
The new XML uses summary/remarks only, not a consumer or library API example.

## Preservation proof

The receipt compares each of the three changed scripts with this worktree's base and requires exact
equality of every line except full-line `//`/XML documentation comments, including blank lines.
Existing inline comments, directives, imports, declarations, string literals, guards, expressions,
branches and assertions are preserved. Changed XML documentation is well formed. This is exact text
preservation appropriate to these hunks, not a Roslyn semantic binding or compiler result.

All other 161 rows retain their revision-2 raw product hashes, including all earlier 32 edited files.
The original/revision-2 patch and output identities are verified byte-exact. The final patch count is
computed from Git and checked against the earlier 32-path set union the three specifically authorized
paths; it is not assumed from the requested total. `git diff --check`, unchanged HEAD, current 164-path
hashes and the exact working-tree write scope are checked. No primary product file is compared with
raw base bytes; accepted edits in other worktrees are not treated as drift.

The first receipt's new-example scan matched the regex named capture `(?<code>...)` in executable
Process.cs source. It was corrected to scan actual XML documentation lines only. The extraction
failure is preserved in `static-attempts.md`; no product code, frozen receipt, profile or test was
changed to satisfy it. The successor checks introduce no runtime execution credit.

## Remaining handoff and stopping point

Protected YAML mapping/state, JSON/configuration values, executable help/output, actual filter/gate
behavior, complete-argv retention/redaction and harness changes remain their original M/P/E or data
handoffs. None remains held merely because these three C# scripts were outside the earlier format list.
The actual reasons and original identities are retained in `remaining-referrals.json` and the full
path/candidate maps.

Installed native producer/header/loaded-path, all-platform behavior, profile/physical acceptance and
stronger experiments remain qualified. The user's Windows concurrency handoff is UNREADY/UNREVIEWED;
PIV physical touch/hotplug and touch-dependent cancellation/recovery remain deferred. Historical
serial-9681620 uncertainty and serial-125 setup6A88-before-RESET evidence are unchanged.

These script files participate in validation input identities. This isolated edit does not alter
primary inputs or avoid that consequence: **the coordinator reconciles accepted final source/input
identity after full-batch independent review and the current pilot run**. No validation profile or
tool was changed here. Finish Core Pass 1 publication, provide the deferred-finding handoff, then stop.
