# Candidate clean-sampling population and coordinator reading plan

**Prepared for Astra's eligibility adjudication and actual reading. No sample has a content verdict.**
Original source: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. All 14,044 final assigned IDs are
joined exactly once from the eight family and nine boundary ledgers. Original inventory, coverage,
progress, ownership, investigations and editor records remain unchanged. This is a bounded indexing
preparation for Core Pass 1 publication, followed by stopping; it authorizes no implementation pass.

## Proposed denominator policy

Begin with every final **`READ-CLEAN`** disposition: **9,763 units**. Keep the other **4,281** final
candidate, qualified, context, historical and excluded dispositions outside the clean population, with
their exact original statuses and authoritative ledger references preserved in `population.json`.
The B4 whole Unshipped API snapshot is related context and receives zero clean credit. CTAP/WebAuthn
API support exclusions likewise remain excluded.

The proposed broad policy keeps complete contracts and meaningful explanations, including implicit
declaration, state, buffer, lifetime and assertion contracts. It retains private/internal members,
test helpers, native declarations, field/property/type/enum contracts, large units and code-only units.
**6,218 code-only units remain in the candidate population.** Their original categories and full
declaration contexts explain their inclusion; an absence of XML does not make an SDK or test contract
irrelevant. These are investigator read-clean outcomes, rather than new correctness judgments by this
preparer. Astra may narrow a specific unit only with its actual claim scope and a recorded reason.

Additional proposed exclusions are deliberately narrow:

| Reason | Units | Policy and retained evidence |
| --- | ---: | --- |
| Administrative C# preamble | 160 | Only namespace/import/license/structural context, with no remaining declaration, XML or substantive explanation under a lexical check. Exact IDs remain available for coordinator restoration. |
| Derived rendering | 14 | SVG/PNG artifacts retain raw identities; source diagram/map/narrative units retain their own eligibility. Renderings are not clean prose samples. |
| Analyzer API support | 3 | Read-clean support snapshots are declaration inventory, not consumer prose or independent compiler/member binding. |
| Historical/dormant material | 11 | Explicit inventory historical/dormant classification or historical design-plan location. Current contract units remain separate. |
| Accepted edited overlap | 111 | Exact original changed spans or attached insertion boundary in accepted A/C, protocols or FIDO patches. Diff context lines do not exclude units. |
| **Provisional** pending-edit overlap | 112 | Exact overlap with completed editor patches awaiting review/acceptance/integration. Restore/reselect if a final patch drops or moves the change. |

This gives **9,464 candidate units after accepted edits only**, or **9,352 after known pending edit
overlaps**. The latter is the proposed working population, **not the final campaign denominator**.
Its global 10% minimum is **936**. The proposed deterministic selection is **983 units**, because
ceilings are applied independently across owner/claim-role strata. Restoring all 160 administrative
preambles would give 9,512 units and a global minimum of 952; `summary.json` retains that adjudication
option. Wholesale removal of private/internal/test or code-only units is not proposed.

## Exact editing overlap inputs

`edit-overlaps.json` binds each input patch and receipt by path/SHA-256 and records only actual changed
blocks in original character coordinates. Full unified hunk old text is checked against pinned source.
Replacement/deletion spans use half-open original ranges. Pure insertions conservatively attach to both
neighboring unit boundaries. This makes ambiguous attachment exclusions visible rather than silently
counting edited commentary as clean. Unit IDs and the population itself are never split or renamed.

| Completed batch | Product paths | Read-clean units overlapping | Current sampling treatment |
| --- | ---: | ---: | --- |
| Accepted A/C successors | 91 | 74 | Accepted edited overlap removed |
| Accepted protocol successor | 57 | 30 | Accepted edited overlap removed |
| Accepted FIDO successor | 15 | 7 | Accepted edited overlap removed |
| Transport revision 2 | 62 | 21 | Provisional exclusion; focused review/integration pending |
| SHARED guides | 26 | 49 | Provisional exclusion; review/integration and cross-owner reconciliation pending |
| Five applet batch | 37 | 8 | Provisional exclusion; final review/integration pending |
| OpenPGP | 17 | 11 | Provisional exclusion; final review/integration pending |
| PIV | 13 | 11 | Provisional exclusion; final review/integration pending |
| External commentary | 19 | 12 | Completed delivered patch indexed provisionally; acceptance/integration and final retained-path dispositions pending |

The nine fixed patch snapshots cover 337 distinct paths at preparation. No path conflicts occur among
these snapshots. This is a structural input observation, not acceptance of their content or any later
revision. Completed pending editor records were read only at their explicit released worktree paths;
no unfinished Windows worktree or partial investigation was inspected. New handoffs or revisions need
their own hash/delta reconciliation. Many edited units already have candidate statuses; they were
excluded before this overlap subtraction and are never subtracted twice.

## Deterministic owner and claim-role selection

For each nonempty **owner × structural claim-role** stratum, rank the complete eligible set by
`SHA256("core-001-clean-sampling-preparation-v1|owner|role|unit-ID")` and take `ceil(N/10)`.
Add deterministic owner-minimum top-ups if needed to reach three where at least three eligible units
exist. This preparation needs no such preference-driven cherry picking. The precise quota sets and
ranks are reproducible from `strata.json`, the script and hash-bound inputs.

| Owner | Candidate eligible | Owner 10% minimum | Proposed selected |
| --- | ---: | ---: | ---: |
| A | 1,722 | 173 | 176 |
| B1 | 1,076 | 108 | 111 |
| B2 | 1,168 | 117 | 120 |
| B3 | 1,056 | 106 | 110 |
| B4 | 1,174 | 118 | 121 |
| B5 | 690 | 69 | 73 |
| B6 | 264 | 27 | 29 |
| C | 595 | 60 | 62 |
| Management | 203 | 21 | 23 |
| CTAP2 | 75 | 8 | 9 |
| Security Domain | 106 | 11 | 12 |
| OATH | 156 | 16 | 18 |
| WebAuthn | 162 | 17 | 19 |
| YubiOTP | 256 | 26 | 28 |
| YubiHSM Auth | 79 | 8 | 10 |
| OpenPGP | 198 | 20 | 22 |
| PIV | 372 | 38 | 40 |

All 17 owners have at least three proposed samples. Source category counts by owner are explicit in
`summary.json`; category names are preserved rather than equated with reader-facing prose. Claim-role
strata use an explicit priority order: enum values; native interop; tests/helpers; narrative/examples/
maps; XML contracts; lifetime/protocol internals; other types; implementation/state/internal contracts.
Thus a native declaration with XML stays in native scope, and a narrative test-instruction unit stays
in its test context. Roles are structural reading categories, not semantic conclusions.

| Claim-role stratum | Candidate eligible | Proposed selected |
| --- | ---: | ---: |
| Native interop contracts | 570 | 58 |
| Tests and helpers | 4,629 | 472 |
| Lifetime/protocol internal contracts | 1,203 | 124 |
| XML contracts | 1,148 | 123 |
| Narrative/examples/maps | 356 | 43 |
| Implementation/state/internal contracts | 900 | 99 |
| Type declarations | 125 | 19 |
| Enum values | 421 | 45 |

Enum samples receive their own quota and displace no lifetime/native/test/narrative/XML sample.
The 341 mechanically matched investigator nominee IDs are annotations, never selection inputs.
Of these, 338 are eligible and 36 happen to be selected by the fixed hash ranking. The other 302 are
listed in `nominee-supplement.json` for optional additional targeted reading. Narrative-only nomination
suggestions remain in their original reports; this numeric join is not an exhaustive interpretation of
those suggestions. Extra nominee reads can supplement strata, while the full population still governs
the required sample percentage.

## How Astra can read the sample

- `selection.json`: all 983 exact IDs, original paths/whole-unit character and line ranges, source/range
  hashes, structural strata, pending verdict fields and current-primary line mappings.
- `read-plan.md`: 17 owner groups, then 456 source-path groups, with exact original/current ranges and
  every required selected ID. Read one group at a time; large units retain their complete scope.
- `read-unit.py <selected-ID>`: emits one exact original whole unit from Git, verifying the original
  source and range identities. Optional `--current-lines` shows current intersecting lines only when
  their current file hash still matches preparation. It supplies text, not a sampling verdict.
- Read enclosing owner/caller/helper/assertion/cleanup context from the named source path as needed.
  Recheck consequential original citations independently. A passing hash join supplies no content
  approval, native evidence, or reason to skip a large or difficult unit.

All 983 proposed selections have contiguous unchanged-line mappings to the current primary source
at preparation, including offsets caused by accepted documentation edits elsewhere in a file. Original
investigator ranges remain pinned; current comments are a separate representation. The selected units
total about 551,307 original characters. No full source corpus is copied into these records: Git supplies
the originals, the reading plan supplies exact ranges and the helper emits only a requested whole unit.

The first final hash check observed eight primary guide/source files changing after initial mapping
capture while all bound final ledgers and editor patch inputs stayed exact. The first 983-ID selection,
summary and reading plan are preserved under
`history/selection-v1-before-current-guide-transfer/`, with original and observed current hashes.
A read-only mapping refresh retained **every selected ID and population decision**, and all 983 units
still map to contiguous unchanged primary lines. This observation supplies no automatic acceptance of
a guide transfer or final combined diff. The affected editor batches retain their recorded provisional
exclusion status until coordinator freeze; the original failed current-hash check remains explicit.

A subsequent verification observed the five-applet transfers continuing in primary: 14 selected
source paths then differed from the refreshed mapping hashes. `current-mapping-state.json` preserves
those exact observations and affected IDs. Preparation stops chasing active integration. **Current line
locations are captured mappings, not live guarantees**; use pinned original whole units and refresh
current context after the final integration freeze. The reader helper rejects a changed current hash.
All 983 proposed IDs, population decisions and bound patch inputs remain unchanged. This second
current-hash failure is retained as a finalization dependency, rather than relabeled as a live pass.

## Finalization gates

1. Astra adjudicates this broad contract-inclusive policy and the 160 administrative exclusions.
2. Freeze the final accepted combined product diff, including pending transport, guide, applet,
   PGP/PIV and external returns. Recompute actual original-unit edit overlaps and current mappings.
3. Freeze every candidate-associated unchanged-path edit/retain/hold decision and required independent
   correction/re-review. Reopen neighboring clean claims when a dependency changes; a clean ledger row
   alone does not close an editor's candidate-path responsibility.
4. Preserve this preparation's selection/population/input hashes before producing a reconciled successor.
   `prepare.py` refuses to replace a different selected-ID set silently. The final denominator is
   deliberately `null` today; current thresholds are candidate policy counts for adjudication.
5. Astra actually reads the selected complete units and contexts, records clean/required-correction/
   source-gap verdicts, removes subsequently edited overlaps and expands affected strata after material
   misses. Verify the final >=10% and minimum-three/stratification requirements against the frozen set.

This preparer read/indexed source bytes only to classify structural eligibility, validate diff coordinates
and map exact units. It performed no source correctness review, coordinator sample reading, external
research, SDK test/build, formatter, native/device/crypto operation, Git mutation or subagent work.
