# Core documentation handoff

**Whole-Core Pass 1 is complete and published** as `core-001` on `yubikit-docs-audit`.
Frozen audit commit: **[`bff374eb533f9ec03deecc04561aa286c050efa6`](https://github.com/Yubico/Yubico.NET.SDK/commit/bff374eb533f9ec03deecc04561aa286c050efa6)**,
pushed and verified by remote readback on 2026-11-12. Product source baseline remains
**`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`**; both campaign and upstream histories are preserved.

## Published Core Pass-1 result

- **14,044 source IDs** accounted for: 9,020 Core and 5,024 external, covering all eight Core families
  and all nine dedicated applet/Core boundary reviews.
- **363 product paths** updated with independently reviewed documentation, comments and scoped
  formatting corrections. All 1,320 C# executable structures/directives/import orders and 20 API
  snapshots match the source baseline.
- **317 unique example preparations compiled** with zero warnings: 17 pilot and 300 additional
  preparations across 11 real contexts. Two aliases account for the additional set's 302 body IDs.
  No example or injected test assembly was executed.
- Clean 42-project solution build; ordinary test outputs restored before **4,442 unit executions**
  across 13 projects, with five existing skips, zero failures and zero warnings.
- Documentation/architecture checks, six documentation-patch regressions and ten exact docs-inclusive
  `2.0.0-alpha.2` packages passed. Complexity had no new or worsened violation.
- **Source sampling accepted:** 1,134 distinct actual reads, 1,071 counted supported/unchanged units
  meeting all 94 owner-role targets across 17 owners, using a conservative 9,352-unit basis. The 63
  withheld units and compiler-discovered logging-example retraction remain explicit.
- Fresh independent final evidence review passed. **5,225 frozen records plus manifest** passed
  snapshot, check and staged identity verification. Earlier failures, reviews, source evidence,
  original user files and protected archives remain preserved.

### Handoff and evidence

- **[Deferred findings](modules/core/pass-1/core-001/deferred-findings.md)** — original 185 candidates,
  inherited resolutions/retractions, implementation and qualification facets, and exact reopening scopes.
- [Verification closeout](modules/core/pass-1/core-001/closeout.md) and
  [independent final review](modules/core/pass-1/core-001/validation/closeout-review/final-review.md).
- [Example acceptance](modules/core/pass-1/core-001/validation/examples/full/closeout/acceptance.md),
  [sampling acceptance](modules/core/pass-1/core-001/sampling/final/coordinator-acceptance.md), and
  [frozen manifest](modules/core/pass-1/core-001/validation/record-manifest.json).
- [Publication verification](publications/core-001-20261112/publication.md).

Frozen readiness documents retain their pre-commit checkpoint wording; this external handoff records
the subsequent verified publication. Formatting is explicitly qualified: 241 introduced terminal LFs
were corrected, while source-pinned import/declaration and baseline/context-sensitive whitespace
findings remain untouched. Neither failed formatter run is relabeled clean.

This closes the requested first sweep, not the deferred implementation or universal device/platform
qualification. PIV physical touch/hotplug and touch-dependent cancellation/recovery remain deferred.
The user's separate Windows CCID/HID concurrency work remains unready/unreviewed pending a later handoff.
**Stopping point reached: publish Core Pass 1, deliver the deferred findings, then stop.** No separate
Pass-2 planning or implementation project is authorized.

## Verified integration baseline — preserved prior milestone

[Integration records](integrations/core-upstream-20261111/closeout.md) preserve 17 resolved conflicts,
45 overlapping-path decisions, independent reviews, current-source qualifications and all original
failures. The separate integration includes upstream runtime changes and necessary consumer migrations;
the following Core audit is documentation-only.

- Clean build and **4,442 unit executions** passed with five existing skips and zero warnings.
- The separate resilience target passed **197 executions**; these repeat a subset of the unit suite.
- Documentation/architecture checks, six patch-guard tests and all ten local packages passed.
- Linux JIT/AOT publishing, 38 native checks per runtime, four expected-failure sensitivity runs and
  31 evidence-gate tests passed. Physical device modes executed: zero.
- All **30 complete example bodies**, actual consumer tools and designated comparison/fixture projects
  compiled. Six contexts passed **current-source/exact-retained-output verification**. Four older
  strict-live output comparisons remain failed because seven shared DLL/PDB paths were replaced by
  later successful build states; original outputs are retained exactly. No binary/runtime equivalence
  or unqualified six-context strict-live success is claimed.

Final review and its archive-classification follow-up passed. The frozen packet contains **8,325
records plus its manifest**. All **8,617 committed change paths** were explicitly authorized and
checked, including 291 product paths. Two OATH newline-only index entries were restored to their
reviewed working/HEAD contents before commit. Fifty-one original conflict-marker lines remain only
in hashed historical conflict snapshots; the active-source diff check and unmerged-index checks passed.

The campaign fast-forward preserved every integration-record byte, all 5,366 preceding frozen
artifact identities, 114 earlier PIV integration files and four original untracked user files.
The inactive tmpfs integration copy remains preserved. New scratch/build work uses the home-backed
runtime location recorded in the execution plan; frozen integration tools are historical inputs,
not destinations for new audit logs.

## Core cycle provenance

See [execution status](modules/core/pass-1/core-001/status.md) and
[execution plan](modules/core/pass-1/core-001/plan.md). Fresh Sol 6.1 owners performed detailed
investigation, documentation editing and independent reviews. Astra coordinated ownership, phase
gates, consequential decisions, sampling, validation and publication.

The scope includes the whole Core module, relevant native/verification/shared documentation and
dedicated boundary reviews for OATH, Management, CTAP2, WebAuthn, Security Domain, YubiOTP, YubiHSM Auth,
OpenPGP and PIV. Inherited findings were reconciled against the integrated source. Their remaining
engineering and qualification scopes are delivered in the deferred handoff; this publication starts
no subsequent implementation work.
