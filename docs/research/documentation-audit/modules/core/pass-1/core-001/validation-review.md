# Independent Sol review: Core validation foundation

## Decision

**Required correction R1 before accepting checker execution as source/API evidence.**
The run-owned logger adapter is accepted for the separately assigned disposable Python qualification
only. Static preparation and preservation checks passed within the limits below. This review grants
no SDK, native, device, formatter, test, index/ref or publication permission.

Reviewer: fresh independent GPT-6.1 Sol. Source baseline:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD:
`d9cf31a17c7580ab580917ce201c4855c3cf89b4`. Later metadata commits need not equal this observation;
the final source/API comparisons must retain the fixed source baseline.

Paths below are relative to this run unless explicitly rooted elsewhere.

## R1 — Bind the executed checker, rather than only its logger

**Affected:** `validation/run-command.py:23–28`, `validation/foundation.py:74–81`, and
`validation-preparation.md:108–125`.

The new receipt enrichment correctly hashes the frozen v1/v2 logger sources, `foundation.py` and the
Core logger adapter. It does not identify the external checker DLL loaded by `dotnet "$C"`, its copied
Roslyn assemblies, or the checker source/project/props used to produce that DLL. The static
`foundation-verification.json` separately hashes the reviewed source/project/props, but has no produced
DLL identity or link to a future actual build receipt. Merely printing the DLL path and preserving a
successful toolchain log does not establish that later `selfcheck`, `tokens` and `api` invocations loaded
that reviewed build. Replacing the DLL at the same home-backed path leaves the current logger
participant list unchanged. A source change after compilation likewise need not alter the invoked DLL.

**Required bounded correction:** add an explicit checker input/output binding to the first-command
workflow. A small additive Core-owned receipt/sidecar is sufficient; do not broaden or modify the
frozen supervisor. Bind the reviewed `Program.cs`, checker project and local props; the relevant root
policy/configuration; the actual clean build receipt; and hashes/paths of the produced DLL, runtime
configuration/dependency metadata and loaded Roslyn assemblies. Verify the reviewed inputs across the
build and verify those output identities when accepting each checker invocation. Keep binaries external.
Source/input snapshots and identity metadata can remain textual Core records. If a participant changes,
retain the earlier attempt and make a fresh binding/proof. Add fields or a sidecar, leaving original
logger receipt fields and historical records unchanged.

The correction can be a narrow coordinator checkpoint rather than a generic new compiler framework.
The checker’s actual SDK/compiler version and warning-free build still require the assigned execution.
Re-review that binding before accepting the subsequent checker results. This is an evidence-admission
gap, not a demonstrated Core implementation defect or a request for SDK tests.

### Hash/signature coverage answer

- **Static source coverage: yes.** Every `checked_owned_records` raw hash in the supplied foundation
  report matched its current file, including the new checker and Python guards.
- **Logger algorithm coverage: yes.** Pins and the additive `reuse` object cover the actual frozen
  logger stack and Core context adapter. Existing receipt fields are retained by the adapter.
- **All executed semantic guards: not yet.** The checker executable-to-reviewed-source binding above
  is absent. A logger receipt authenticates capture inputs, not arbitrary binaries named in its command.
  The static source manifest is not a compiled-output signature. No new checker execution is claimed.

## Accepted static findings

1. **Context and preservation.** Core's eight-level root calculation is correct. The adapter explicitly
   rebinds both the v2 supervisor and v1 RESULT/active-run helper; no original capture function is used.
   Qualification rebinds its own `HERE`, `ROOT`, runtime, attempt/workspace and `__file__`, so its child
   invocations select the Core adapter rather than the frozen integration path.
2. **Process contracts.** The pinned v2 session/subreaper, proven-group-only TERM/KILL, adoption/reaping,
   bounded EOF drain, actual quiescence, immediate toolchain retention and atomic primary/fallback
   receipt algorithms remain unchanged. Reading those algorithms is not current-process qualification.
   The wrapper's SIGTERM handler applies to itself; there is no new global process signalling policy.
3. **Output and environment ownership.** Default command records route to `validation/commands-v2/`;
   journals, jobs, temporary state and qualification route beneath the Core-specific home runtime.
   `TMPDIR`, `TMP`, `TEMP` and no-bytecode settings propagate to assigned children. Receipt environment
   capture is limited to these named settings, not the inherited environment or secret values. Old
   integration, PIV and inactive temporary copies remain inputs, never output destinations.
4. **Record classifier.** Content-checked compound metadata names are accepted without importing the
   seven historical exception paths. Direct binary suffixes, binary magic, NUL, invalid UTF-8, invalid
   JSON, generated directory components and symlinks remain rejected. The original enumeration,
   raw/LF/actual-clean/attribute and exact staged-set/object checks remain in force. A safe textual
   `library.dll.sha256.snapshot` is not a blanket `.dll` payload exemption.
5. **Source and API guards.** C# uses exact token kind/text, rather than value text or whitespace/escape
   rewriting of literals. Significant directives, disabled branches and skipped tokens are retained at
   token boundaries; imports, attributes, defaults, signatures and modifiers are not normalized away.
   All executable C# membership outside the stated frozen/agent areas is checked, including managed
   native interop under Core. API snapshot membership is derived across `src/`, nonempty and exact;
   the observed 20-file count is not an acceptance constant. C# selfcheck remains prepared, not run.
6. **Native lane.** Current C/C++ source outside excluded archives is empty. The result is explicitly
   not applicable, not a fabricated native pass. New native membership requires assignment; edited
   ambiguous translation-phase cases fail closed. This does not exempt native-facing C# from review.
7. **Scope and staging.** The full-source checkpoint includes HTML and separates raw identities from
   actual Git-clean identity. Record snapshot/staged commands operate on the complete Core run and must
   wait for all relevant writers. Inventory counts/plots do not establish semantic review coverage.

## Fresh read-only verification

Executed from the primary checkout, with `python3 -B`; all four commands returned 0:

```bash
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/selfcheck.py
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/preflight.py
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/check-preserved.py
python3 -B docs/research/documentation-audit/modules/core/pass-1/core-001/validation/native-tokens.py
```

- Selfcheck reproduced **23** meaningful static rejection boundaries: forbidden receipt destination;
  eight payload/classifier failures; two unsafe paths; seven native mutation/malformed-source cases;
  and five historical RESULT mutations. These are tooling boundaries, not new SDK validation-only tests.
- Historical RESULT replay: 14 project rows, **4,639 executions / five skips**, zero failures/warnings;
  **4,442 unit + 197 overlapping resilience** executions remain historical, not new Core outcomes.
- Preflight: **13 canonical unit projects**, one canonical Core resilience project and one uniquely
  selectable non-packable `Audit20261111.DocCheck` host. Root import and home output workspace resolve
  correctly. SDK 10.0.404 is installed; no SDK selection or compilation was exercised by this reviewer.
- Preservation: **5,366 earlier artifacts**, including **nine non-text objects**; **8,325 integration
  records / 226,365,754 bytes**; **114 prior PIV integration files**; **four original user files**.
  Actual protected Git-clean/index objects and pinned ancestry passed. No other worktree HEAD freeze
  was imposed.
- A separate read-only call to `source-state.capture()` reproduced the complete existing checkpoint:
  **1,608 paths**, **zero Git-clean changes**, **57 newline-only raw differences**, scope SHA-256
  `521f13d5d696744c597b94586e17d2740b48c4e8ce7a954f53285e07d962a1cf`.
- All supplied foundation `checked_owned_records` raw hashes matched. No writing preparation entry
  point was replayed. No child was started through the logger; no qualification or compiler was run.

## Exact coordinator release sequence

The following is a release plan, not execution performed by this review. Keep exclusive checkout
ownership. First release **only** the disposable logger qualification; it may create/control its own
local Python child group, but grants no SDK, service, native or device work:

```bash
V=docs/research/documentation-audit/modules/core/pass-1/core-001/validation
R=/home/rupicola/Code/y/.core-docs-audit-runtime/core-001
python3 -B "$V/qualify-logger.py" --attempt core-foundation-first
```

Use a fresh attempt name if that name has become occupied. Retain the qualifier streams and textual
external qualification/case/atomic-write receipts as new Core records. Accept all four actual cases,
EOF completeness, proven-group cleanup/reaping, durable fallback and failed-fsync partial evidence.
Do not treat the deliberate wrapper failures as failed qualification or as successful SDK commands.

After qualification acceptance, R1 correction/re-review, explicit SDK release, and loading the current
build skill, use the canonical commands below. The added R1 checkpoint must surround the build and
bind each following checker acceptance; its final receipt path/command belongs in the corrected plan.

```bash
C="$R/jobs/Yubico.YubiKit.Audit20261111.DocCheck/bin/Release/net10.0/Yubico.YubiKit.Audit20261111.DocCheck.dll"
python3 -B "$V/run-command.py" --label sdk-info-first -- dotnet --info
python3 -B "$V/run-command.py" --label checker-build-first --toolchain \
  --expect-target build -- dotnet toolchain.cs -- build --project Audit20261111.DocCheck --clean
python3 -B "$V/run-command.py" --label checker-selfcheck-first -- dotnet "$C" selfcheck
python3 -B "$V/run-command.py" --label source-tokens-first -- dotnet "$C" tokens "$PWD"
python3 -B "$V/run-command.py" --label api-first -- dotnet "$C" api "$PWD"
```

Verify actual output paths, compiler participation, zero warnings/errors and every receipt's successful
exit, stream completeness, retention and quiescence. Preserve first failures and use fresh attempts.
No repository tests, full source gates, native execution or example-host scaling are released here.
Final source/docs/API reconciliation waits for inventory ownership, accepted content and compiler proof.
Complete-body/snippet extraction and consumer hosts remain future work; no compiled example claim is made.
The 100 inherited rows / 70 nonadditive groups remain tentative review inputs, not accepted bug counts.
Continue the existing source pin until the user's Windows handoff is ready. PIV physical deferral stands.

## Reviewed identities and write boundary

| Run-relative file | SHA-256 |
| --- | --- |
| `validation/foundation-verification.json` | `3508896b9945bb5041d35df8cf7eb03ee65ddb0740d0a5b7367dad7539997477` |
| `validation-preparation.md` | `105c6c428f26ccac5363f1408143f5861fc65fbdace0f11d0d07a2db07bf048b` |
| `validation/reuse-provenance.json` | `caeacf20664cf857cd0e0358142616e05f46b3cbbea3b656817247f714eac4eb` |
| `validation/foundation.py` | `c62bc72cecc56a8441b36a24425879fd00bc3db55ea3b39a28dc0198aac5caaa` |
| `validation/run-command.py` | `648234e0eaa442249047a3c9c1337b9ec97d8e4431338d0048b9319b71f9b9bf` |
| `validation/qualify-logger.py` | `779c4bb3864d5352b6189489177b61a61c7b245ea09e4260c1b612ecc3aa8fa6` |
| `validation/check-records.py` | `e83a736ac1ab8bdb05af92cc3acfee03e518b15414baa1212b3e6450d8209503` |
| `validation/checker/src/Program.cs` | `7c2d0df344f925ef1fccfe1333f9e40cc6bddc0a86dfa22cfa7e9f413128898b` |

The foundation report supplies the other exact guard/configuration hashes checked above. This review
writes only `validation-review.md`; parallel inventory, registry and coordinator changes are not mine.
The observed index SHA-256 before this review-file write was
`227feab5a1bab849ecae433dbd6b1fa5fa264fb685a1098ef245e39867d980ba`.
