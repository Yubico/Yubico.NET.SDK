# Management coverage reconciliation

## Outcome and authority

The baseline inventory is reconciled: **55 tracked module files, 42 C# files, A21/B2/C32 ownership,
five public families and 47 non-header API snapshot entries** at
`da61d2e6d754b4c155cbb2579202e910986f4b3a`. Every baseline module path has an explicit owner,
reported full-read disposition and evidence in [coverage.json](coverage.json).

This is a records-only reconciliation of [plan](plan.md), [triage](triage.md), investigations
[A](investigation-a.md), [B](investigation-b.md), [C](investigation-c.md), and
[sampling](sampling.md). It does not repeat the source semantic audit or upgrade investigator reads
into independent review. No build, test, hardware operation, commit or delegation was performed.
Only these two coverage records were authored by this task; ongoing source edits and other records
are preserved.

## Inventory and public surface

| Assignment | Files | C# | Other scope |
| --- | ---: | ---: | --- |
| A | 21 | 21 | Seven session/interface/extension/backend files, six unit-test files, eight integration-test files |
| B | 2 | 2 | Configuration record and write-options contracts/internal explanations |
| C | 32 | 19 | Complete executable examples, five Markdown files and eight project/API/runner support files |
| Total | **55** | **42** | **13 non-C# files** |

The JSON module list is exhaustive, not a representative sample. `mixed` includes required corrections,
unresolved claims and affected dependencies even when neighboring units are clean. `support` marks an
inventoried/read artifact, not consumer prose that needs invented documentation. Library descriptive
metadata remains a clean unit within a support file. SmartCard backend prose is clean within its
stated scope; dormant SetMode behavior remains separately deferred as MGMT-IMP-012.

| Public family | Semantic owner | Snapshot entries assigned by declaring family |
| --- | --- | ---: |
| DeviceConfig | B | 22 |
| IManagementSession | A | 4 |
| IYubiKeyExtensions | A | 8 |
| ManagementSession | A | 5 |
| SetDeviceConfigOptions | B | 8 |
| Total | | **47** |

These are snapshot entries, not 47 independent prose tasks. Count by the first declaring family, not
all occurrences of a type in parameters/returns. DeviceConfig properties have getter/init projections;
record constructors/clone/equality/hash/ToString/operators are generated boilerplate. The extension
receiver and three static projections do not add another three operations. Options' implicit constructor
is covered by the type contract. Inherited lifecycle, ConnectionType and FirmwareVersion belong to
Core; internal encoder/backends are not additional public families. Public classes in ManagementTool
belong to a non-packable example executable. API preservation on final content is still pending.

All **41 candidate groups** (A14/B8/C19) have a coordinator disposition in triage: twelve documentation
groups, seventeen deferred implementation/test groups and three cross-module dependencies. These
overlap; none of those totals is an independent-bug count. B-08 is clean/optional, not an eighth
configuration defect. The JSON preserves report-local IDs and uses triage as the authoritative mapping.

## Semantic units and sampling

A's complete per-file table and **20** named clean units are preserved in the JSON with expanded paths
and baseline ranges. C's complete module/support dispositions and **12** clean units are also preserved;
C-CLEAN-09 remains one investigator unit across two result files, not two invented samples.

B's table has **24 rows**. All have stable `B-U01` through `B-U24` dispositions in JSON. The eight
affirmative prose-bearing clean rows have these stable sampling IDs:

| Stable ID | B table row | Bounded unit |
| --- | --- | --- |
| B-C01 | B-U01 / report line 343 | DeviceConfig optional/null type summary |
| B-C02 | B-U11 / line 353 | Internal tag comment/values, retaining external-reference caveat |
| B-C03 | B-U12 / line 354 | MaxConfigLength / one-byte body-length prefix explanation |
| B-C04 | B-U13 / line 355 | Timeout constants as retained SDK policy, not firmware maxima |
| B-C05 | B-U14 / line 356 | GetBytes local-order summary, optional ownership expansion |
| B-C06 | B-U16 / line 358 | Serialization/prefix/size for ordinary modeled values |
| B-C07 | B-U17 / line 359 | Intermediate/final cleanup explanation at this layer |
| B-C08 | B-U20 / line 362 | Existing borrowed-memory core contract; practical expansion still reviewable |

The conservative clean prose population is **20 + 8 + 12 = 40 units**. Do not include mixed timeout
property/Validate rows merely because part of their implementation is clean. Exclude the no-prose
UInt16BigEndian helper, negative marker sweep and the broad-but-not-false options summary, which B
does not explicitly nominate as clean. Optional improvements do not invalidate a specifically clean
existing contract; neither do they exempt new wording from review. This denominator is not all source
methods, all read files, or all snippets.

The workflow requires **at least 10%, minimum three**, spanning public XML, narrative/examples and
internal/test explanations: **four** clean units for this population. The initial sampling checkpoint
contained six:

- S-01–04 → A-C02, A-C07, A-C08, A-C19.
- S-05 → **B-C01**; S-06 → **B-C03**.

Those **6/40 = 15%** met the numerical target but did **not** meet category breadth: narrative/example
sampling was pending, and no promised future sample was counted. During this reconciliation, the
coordinator added actual checks to sampling.md: **S-07 → C-CLEAN-01** package narrative,
**S-08 → C-CLEAN-02** capability-query example and **S-09 → C-CLEAN-09** result XML. The current record
therefore has **9/40 = 22.5%**, meeting **both numerical and category targets**, with no material miss
reported. These are attributed coordinator checks, not new sampling performed by this records task.
Any later material miss requires expansion of its category. Consequential reset/lock checks are not
retroactively counted as clean samples. Final cross-document reconciliation remains separate.

## Examples and external references

C's exhaustive baseline block table at investigation-c.md:613–686 remains the block-level authority:
**49 module Markdown fenced blocks and five C-owned XML blocks**, classified as complete, fragment,
sketch or historical. Multiple alternatives within one fence do not add fences. B owns the DeviceConfig
derivation fragment; A owns the reset-interface example. Those are additional XML responsibilities,
not silently included in C's five. Investigation classification/inspection does not establish compilation
or execution; final meaningful-snippet checks and changed-block reconciliation remain pending.

The JSON explicitly expands every grouped current documentation/example path in C's external manifest,
including the ellipsis PublicApi and PIV test paths. It distinguishes:

- **Full reported reads:** complete file read, with conclusions still limited to Management units.
- **Narrow reads:** complete named semantic blocks or registry/reference hits only; not full-file review.
- **Support inventory:** link roots/convention registries/project context, not consumer sample execution.
- **Historical/generated/pattern exclusions:** status/provenance classification, not invented inspection.

Core dependency files read completely by A are recorded separately from narrower B/C traces. Shared
test-helper full reads do not imply a complete Tests.Shared audit. Source examples, benchmarks,
verification scenarios and endpoints retain their host/lifetime prerequisites and not-run status.
The expanded current manifest has 28 documentation/instruction paths, 34 source/example/support paths,
18 additional dependency-read paths and two additional source-unit citations with no full-read claim.
Fifteen separate records classify historical/generated/pattern exclusions.
External primary URLs, access dates, focused quotes and authority limitations remain in A/B/C; this
local-path manifest does not replace citation evidence or claim a new network verification.

### Corrections and precision gaps

1. **Historical migration snapshot:** B:402–404 calls `docs/migration/v1-to-v2-gaps.md` active/stale and
   proposes a rewrite. C:753–756 and triage:105–107 explicitly correct this: preserve its point-in-time
   Builder/legacy/gating statements. Current guidance belongs in active migration/module documentation.
   This manifest applies that adjudication without rewriting either investigation.
2. **Abbreviated paths:** C:745–746 uses `tests/...` for PublicApi and PIV references; JSON expands actual
   UnitTests/IntegrationTests paths. C's `ManagementTool/README.md` and bare clean-unit filenames are
   module/example shorthand, not additional files. No module ownership/count mismatch was found.
3. **Obsolete skill target:** the API-design skill file exists, but its obsolete Management target is
   a reported instruction-maintenance issue, not a covered extant source file or authorized skill edit.
4. **Earlier async addendum:** C does not name which earlier addendum it read. JSON lists the existing
   two `earlier-multi-key-*` historical paths as exclusions, explicitly **not** full reads. Exact
   per-addendum inspection cannot be reconstructed from the report and is not invented.
5. **Nonexistent requested instruction:** A/C report root `tests/CLAUDE.md` absent. It is not entered
   as a covered path; the existing module-local test guide is inventoried.

Immutable campaign records, original plans, historical AOT/inventory/troubleshooting reports and
generated diagrams/build outputs are not current recipes to repair. Pattern exclusions point at existing
roots without pretending every matching historical/generated file was read. Generic management-key and
credential-management hits do not become device Management coverage. Core-wide and sibling whole-module
audits remain deferred.

## Final integration gates — original preparation checkpoint

Edits/reviews are ongoing in detached worktrees. Baseline inventory and investigator evidence do not
identify the final integrated bytes. The JSON `post_integration` section intentionally has no reviewed
file hashes, final changed paths or integrated revision yet. Existing edit/review reports elsewhere in
the run do not by themselves establish final primary-checkout review coverage.

The coordinator must:

1. Enumerate the final complete diff against the canonical baseline, including external documentation,
   campaign/run records and any added/deleted paths.
2. Map each changed path and full affected section to independent review, required corrections and
   correction re-review. Check dependent claims after integration, not only individual worktree diffs.
3. Record final reviewed content SHA-256, reviewer, review record, verdict and revision/content identity
   in `post_integration.reviewed_files`. Later edits invalidate affected hashes/reviews.
4. Preserve all nine actual samples, including C narrative/examples. Recalculate only if explicitly
   changing clean-unit definitions, preserving original evidence and sample mappings; expand categories
   after a material miss.
5. Attach final API and executable-token/directive preservation, meaningful snippet compilation and
   separately authorized validation evidence. Build/unit/device/platform status remains independent
   from documentation coverage. Commit/remote checkpoint remain coordinator closeout work.

This task's verification checks inventory arithmetic, JSON structure, path existence and evidence
reconciliation only. It is not runtime verification, hardware qualification, certification or a claim
that final documentation review/closeout is complete.

## Final integrated update (coordinator)

The original pending checkpoint above is superseded by `post_integration` in coverage.json and the
final reviewed-files.json manifest. All 50 edited content paths have independent review; required review
corrections were re-reviewed, including four late integrated cref/link corrections. For the other 46
files, differences from the exact reviewed worktree bytes are whitespace-only; executable-token and
directive checks independently preserve all 37 edited C# files. All 20 public API snapshots are unchanged.

Final repository gates passed with 3,998 unit tests (96 Management), five existing skips, zero failures
and zero compiler/analyzer warnings. All 63 selected example groups compiled in disjoint recorded
contexts (37 strict raw, 26 real SDK/MSBuild with NET/xUnit analyzers and warnings-as-errors). Raw xUnit2
identity diagnostics remain preserved and are not claimed fixed. See validation.md for exact evidence.
No live hardware, integration test or cross-platform execution occurred.

The coverage JSON now enumerates all current campaign/content/run paths, and its reviewed_files entries
identify the 50 source/documentation content files. Final campaign-record review passed in review-closeout.md.
The path list is refreshed before integration; the Management handoff records the resulting source commit
and verified remote backup separately.
