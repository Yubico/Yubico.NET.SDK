# Core Pass 1 execution plan

**Goal:** Complete whole-Core documentation and the nine deferred applet/Core boundary reviews,
then close the campaign's first documentation sweep with precise implementation return records.

**Architecture:** A separately reviewed, validated and published upstream reconciliation supplies
the baseline. Fresh Sol 6.1 workers investigate, edit and independently review; Astra coordinates,
adjudicates, samples, validates and publishes. Documentation, implementation and device/platform
qualification retain separate status fields.

**Tech stack:** Current .NET/C# SDK, managed/native discovery and transport lifetimes, APDU/HID/SCP,
cryptographic and encoding utilities, original standards/OS sources and pinned Python implementation
and history evidence.

## Authority and baseline

The user approved the ten-step Core Pass-1 cycle and released execution on November 11, 2026.
They explicitly assigned detailed investigation, fixes and independent review to GPT-6.1 Sol,
with Astra coordinating rather than duplicating that investigation.

**Audit baseline:** `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`, `yubikit-docs-audit`, verified local
and GitHub. Both campaign `f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa` and incoming
`5746e72ed9a848d08b5782ddde025aa4a4b522ef` are ancestors. Integration gates and qualification
details are in the external [Core handoff](../../../../core.md) and the frozen
[integration closeout](../../../../integrations/core-upstream-20261111/closeout.md).

The full approved [planning document](planning/2026-11-11-core-docs-pass-1.md) and
[independent planning review](planning/2026-11-11-core-docs-plan-review.md) are copied here unchanged.
Their planning-era mode/status is historical; this execution plan and current status supersede it.

## Sequence

1. **Complete baseline/coverage inventory and inherited registry.** Verify source, API and protected
   artifact identities. Assign every Core and relevant external semantic unit a clear owner. Reconcile
   historical finding IDs by source run; record changed/resolved/still-open/decision/gap outcomes with
   evidence. A new name or passing suite alone does not close an old finding.
2. **Whole-Core investigation in bounded families.** Start with at most three non-overlapping Sol
   packets after inventory: discovery/metadata/monitoring; connection/session/native/protocol lifetimes;
   and cryptography/credentials/utilities. Refine boundaries using actual file sizes and dependency
   relationships; keep one evidence/source register and a single shared-document owner.
3. **Nine dedicated boundary packets.** Check each applet's actual factory, transport, ownership,
   authentication, cancellation/recovery, version and shared-helper call sites. Reopen active applet
   documentation when the current Core boundary requires it; preserve frozen applet archives.
4. **Triage before fresh editing.** Accept source-backed documentation changes. Preserve implementation
   defects and consequential policy questions with stable IDs, desired/observed distinctions and
   exact documentation reopening triggers. No runtime fixes, changed test assertions or API redesign
   are bundled into this documentation run.
5. **Fresh Sol edits and independent reviews.** Improve real consumer task flows and precise XML,
   internal/native rationale, complete examples and assertion-accurate test commentary. Review full
   affected units and consequential original citations independently; re-review required corrections.
6. **Close coverage and sample.** Explicitly disposition candidate-associated unchanged paths before
   final compilation. Astra samples at least 10% of eligible clean units, minimum three, stratified
   across API/narrative/example/internal-test/native families and boundary work. Expand after material
   misses and remove edited overlaps from the clean denominator.
7. **Validate final source and scoped behavior.** Compile complete meaningful examples and actual
   relevant projects; preserve exact source/API/native-token identity and resource scopes. Run current
   repository gates with zero compiler/analyzer warnings. Use meaningful bounded experiments and an
   explicit device/OS/transport matrix. Preserve setup failures, negative outcomes and deferred lanes.
8. **Fresh final evidence review, freeze and publication.** Reconcile claims, coverage, accepted hashes,
   compiler/current-source/retained-output evidence, qualification limits and restoration. Commit/push
   owned documentation/records and verify remote state. Update the external handoff after archive freeze.
9. **Close the first sweep and stop.** Account for every actual module and all nine boundary reviews,
   publish the Core Pass-1 closeout and provide the deferred-finding handoff. Per the user's subsequent
   clarification, a separate Pass-2 planning or implementation project requires a later request.

## Mandatory packet contract

Read current root/module/nested instructions, campaign workflow, approved style and SDK house style.
Each assignment names its baseline, exact writable/read-only paths, inputs, required records, allowed
execution and independent-review boundary. Original standards identify exact revision/status/section,
actor, normative strength and applicability. Pinned Python code/tests/history clarify implementation
and firmware origins without being promoted to normative authority. Bound unresolved source research
to an initial pass and one focused follow-up, then record the precise gap or decision.

Use direct, calm, precise, approachable prose: affirmative task/actor/behavior first. Preserve useful
rationale, nearby conditions and complete resource scopes. Reserve negatives for consequential limits
or likely misconceptions and pair them with a supported action. Avoid repeated “does not,” “cannot,”
emphatic “NOT” and rhetorical “X, not Y.” Use normal word spacing in records as well as public prose.

## Execution environment and retained evidence

Primary audit checkout: `/home/rupicola/Code/y/Yubico.Yubikit.NET`. The separate reviewed integration
checkout remains available at `/home/rupicola/Code/y/yubikit-core-docs-integration`.

`/tmp` has exhausted this user's quota. Route all new audit scratch, compiler output, temporary
directories and logs that belong outside the run to a dedicated path under
`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/`. Verify `TMPDIR`, `TMP` and `TEMP` propagation
and any explicit temp paths. Leave historical tmpfs copies and other workers' files intact.

Reuse proven integration/PIV algorithms selectively in **new run-owned tooling** with source hashes
and explicit adaptations. Executing a historical logger in its frozen directory would mutate the
archive and is forbidden. Check actual XML namespaces, AdditionalFiles multiplicities, selected-target
versus global restore catalogs, unused potential assets, real HintPath copy owners, restore/build
property propagation and Python CLI import isolation before scaling any new host.

Prefer small context-specific proof over a growing generic harness. Capture actual Csc inputs and
outputs immediately, preserve original failures, and qualify historical retained outputs separately
from simultaneous current-path equality. An added versioned helper is distinct from a changed tool;
missing historical bytes or unknown binary replacements must fail explicitly. No bookkeeping rebuild
substitutes for an honest current-source/retained-output distinction.

New compiler/toolchain/device runs are coordinator-assigned and serialized within each checkout.
Use current build/test skills and toolchain commands, full unambiguous selectors and real nonzero
execution counts. Verify actual projects, compiler/analyzer participation, formatting membership and
final API/source preservation. Do not infer hardware/native execution from compilation.

## Windows handoff and physical work

The user reports ongoing Windows CCID/HID concurrency work and will notify the campaign when ready.
Continue this pinned audit. On a supplied handoff, assign Sol a focused delta, integration and
documentation-impact review with its own source/evidence baseline. Record the late checkpoint at
closeout rather than silently assuming readiness or inspecting another worker's unfinished checkout.

Existing dedicated-key authorization covers routine configuration, reset and bounded probes. Capture
current identity/state before mutation and independently restore observable state afterward. Preserve
existing attestation/private material. Physical touch, hotplug and timing require coordination; the
user's PIV physical-test deferral remains in force. Native admission, cancellation request, terminal
completion, drain, release and recovery are distinct observations. Qualify every device/firmware/OS
and secure-channel lane separately.
