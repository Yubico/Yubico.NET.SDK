# Core documentation handoff

**Pass 1 is active** as `core-001`, based on **`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`** on
`yubikit-docs-audit`. The integration commit is pushed and verified on GitHub; the actual campaign
checkout contains both published campaign `f0ba9687…` and pinned upstream `5746e72e…` histories.

## Verified integration baseline

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

## Core cycle

See [execution status](modules/core/pass-1/core-001/status.md) and
[execution plan](modules/core/pass-1/core-001/plan.md). Fresh Sol 6.1 owners perform detailed
investigation, documentation editing and independent reviews. Astra coordinates ownership, phase
gates, consequential decisions, sampling, validation and publication.

The scope includes the whole Core module, relevant native/verification/shared documentation and
dedicated boundary reviews for OATH, Management, CTAP2, WebAuthn, Security Domain, YubiOTP, YubiHSM Auth,
OpenPGP and PIV. Re-baseline inherited findings against this integrated source before classifying
their current state. Implementation changes remain for the selected second sweep.

The user reports ongoing **Windows CCID/HID concurrency work** and will provide a handoff when ready.
Continue the current audit; perform a focused late delta and documentation-impact review when that
handoff is supplied. Retain existing physical-test deferrals and coordinate new attended work explicitly.
