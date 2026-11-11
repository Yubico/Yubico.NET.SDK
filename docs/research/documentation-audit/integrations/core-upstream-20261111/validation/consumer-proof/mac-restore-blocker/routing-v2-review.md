# Independent narrow re-review — routing receipt fix

## Decision

**PASS. The requested caller-provenance fix is satisfied; no further correction is required within
this narrow scope.** The additive launcher now binds the three approved routing values into the
actual build receipt and gates capture on the actual restored profile and unchanged admission Core DLL.

This is source-only approval of the caller revision, not a claim that restore/build/capture will pass.
Reviewer: independent Sol narrow re-review, 2026-11-11. Only this review file was written. Neither
descriptor creator/checker, lane, SDK build/test, capture, native/device operation nor authentication
action was executed. No product/configuration, index/ref, original receipt/review or preserved record
was changed; no subagent was used.

## Exact reviewed identities

Paths are relative to this integration's `validation/consumer-proof/` directory.

| File | SHA-256 |
| --- | --- |
| `mac-restore-blocker/proposed-lane-v2.sh` | `d5fa76a207ae2c2d49df0f0b7a9a2132fb87d9459292e2e243d80bd239c9f96e` |
| `mac-restore-blocker/routing-v2.py` | `3a64e73ff0675a65458589aa0478275a6b0a4e1e299b9a1d10ebceb249a60488` |
| `mac-restore-blocker/routing-v2-preparation.json` | `26d4d855c5eda0e875879e899eb6ec13211afd311d5b557043fbcfb6bd2ac408` |
| `mac-restore-blocker/routing-v2-handoff.md` | `33d7bee08dc2eae03c19d2a2fbec4b3fb777d2fc7042e3c5b538a9f790a8e0bd` |

Independently checked every new/preparation-listed and protected-original identity, the old diagnosis
receipt's listed identities, and all **30 original/preserved failed-record pairs**. All hashes and
sizes agree. Prior routing review remains
`c7b2e1e9ab925d2a13994d7379585a1c249590861f6baa829e65e56ba5fe20cf`;
the accepted v5 receipt remains
`174bef5e532a95386a0e573bef9692b68e1e90824186b930779868f72eaabc39`.

## Receipt linkage — prior R1 resolved

- The shell defines exactly the three readonly Boundary values. Python requires exact equality with
  the approved profile, active checkout, and pinned original proposal/lane/review identities.
- The attempt-specific descriptor includes those three environment properties, active root, actual
  compiler attempt/build label, current regular Core DLL size/hash, and original/derivative/creator/
  review source identities. There is no full environment dump or credential/provider access.
- Creation uses an exclusive same-directory partial, file fsync, atomic no-clobber link publication
  and directory fsync. Existing descriptors/symlinks and unsafe routing directories fail admission.
  Failures stop the shell before logger/SDK execution; partial/published evidence is not quietly
  overwritten or rolled back.
- Only after successful creator completion does the shell explicitly export the same readonly
  Boundary values. The actual build logger receives `--retain-text "$descriptor"` for the exact
  root-relative file created by that attempt.
- Checked the existing logger and base proof source: extra text is copied/hash-bound into the build
  receipt's retained records, and capture retains those build-text records. This closes the missing
  actual-build provenance link without modifying the logger or sealed v5 verifier.

The exact three values match the sealed prepared wrapper:

```text
BoundaryCorePath=/home/rupicola/Code/y/yubikit-core-docs-integration/src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll
BoundaryNativeVersion=1.18.1
BoundaryComparisonVariant=audit-integration-20261111
```

No fourth Boundary/property override, package substitution, source/authentication setting or hidden
temporary Core path is introduced. Existing TMP/TEMP, attempt and capture-hook routing remain the
approved lane mechanics. Process-local exports do not alter parent-shell or machine configuration.

## Pre-capture acceptance gate and unchanged commands

The separate logged `restore-profile` stage retains the new child dgspec/assets and must succeed
before capture. The checker requires:

1. Descriptor values, root, attempt and build label matching this exact invocation.
2. Current Core DLL identity equal to its admission identity.
3. Both actual dgspec and assets using `obj/audit-integration-20261111/` and native VersionOverride
   **`[1.18.1, )`**.
4. Exactly the canonical resolved NativeShims library **`Yubico.NativeShims/1.18.1`**.

Strict actual Csc/source/reference/analyzer/copy/live verification remains v5's responsibility after
that gate. Static source comparison confirms the canonical toolchain selector/command, observation
hook, v5 static call and capture/live command fragments are unchanged. The new stage is not a compiler
proof and makes no authentication-success claim.

Suffix validation and pre-admission checks cover the actual attempt, v5 evidence destination and all
five logger labels. The routing filename is attempt-specific. The old failed `first` records, original
proposal, sealed v5 lane and prior review remain untouched. No global trace destination or competing
capture hook is added.

## Independent verification and coordinator command

Performed hash/size checks, Python AST parsing, fixed-property/command/source-chain checks and
fresh-destination checks read-only, with an audit hook forbidding filesystem writes/mutation and
subprocess execution within that verification. No routing functions were called. At review time
there is **no routing directory or descriptor**, and the proposed suffix's actual attempt, evidence
and five logger destinations are unused. This verifies preparation, not runtime durability behavior.

After coordinator checkout reservation, the reviewed command is:

```bash
cd /home/rupicola/Code/y/yubikit-core-docs-integration
P=docs/research/documentation-audit/integrations/core-upstream-20261111/validation/consumer-proof
bash "$P/mac-restore-blocker/proposed-lane-v2.sh" restore-bound-first
```

It names actual attempt `mac-comparison-restore-bound-first`, build label
`consumer-mac-comparison-build-restore-bound-first`, descriptor
`mac-restore-blocker/routing/mac-comparison-restore-bound-first.json`, and evidence
`mac-comparison-restore-bound-first-v5`. Use another fresh suffix if these become reserved.

Approval discharges only the prior receipt-linkage request. A future 401 or any restore/profile/build/
capture failure must remain retained and be handled within its own scope; no credential lookup,
source suppression or config change is authorized. Earlier fixture/consumers/examples acceptance and
whole-Core/native/platform qualification were not reopened or reasserted.
