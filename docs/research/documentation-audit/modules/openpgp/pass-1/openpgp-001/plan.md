# OpenPGP Pass 1 execution plan

**Goal:** Enable consumers to complete OpenPGP card tasks using accurate contracts, complete examples,
useful explanations and verified references, while preserving precise implementation follow-ups.

**Approach:** Whole-module Sol 6.1 investigation, fresh editing, independent content/original-citation
review and fresh final evidence review. Astra coordinates, adjudicates, samples, validates and publishes.
Independent tooling preparation runs alongside investigation; one owner per editable surface.

**Stack:** .NET 10 / C# 14, OpenPGP SmartCard applet, OpenPgpTool, xUnit v3 unit/v2 integration.

## Authority and baseline

User approved execution after read-only planning and explicitly authorized all testing/configurations
on the connected dedicated YubiKey, including ad hoc scripts and ykman. Python YubiKey Manager source,
tests and commit history are approved references for behavior and firmware-gate origins. Physical
touch/insert-remove timing is coordinated separately. Consequential irreversible effects and actual
recovery limits are made explicit before such operations.

Expected baseline `fe180282a2d77b15eaaa5868d6c8b00e251d1f05`, branch `yubikit-docs-audit`;
tracked tree clean, four original untracked user files preserved. Planning verified 2,794 previous
archive identities. Execution rechecks baseline/preservation and hashes all module paths. Scout
`ses_e5948306fffeVQPOJz8pBsIYij` supplied 96 paths, 39 public types, 423 snapshot entries, 40 interface
operations, 19 tool C# sources and 49 integration methods; independent investigation reconciles them.
Root/module/test instructions and campaign workflow.md govern. Pass 1 across remaining modules precedes
Pass 2; PIV stays late and Core last, including dedicated per-applet boundary reviews.

## Editorial acceptance

Read records/style.md and docs/SDK-HOUSE-STYLE.md. Write direct, calm, precise and approachable prose
for capable engineers learning this SDK. **Prefer affirmative, task-oriented wording:** lead with actual
behavior, name the actor and make responsibilities actionable. Use negatives for consequential
limitations or likely misconceptions, paired with the supported action where possible. Avoid repeated
“does not”, “cannot”, emphatic “NOT” and rhetorical “X, not Y” phrasing. Preserve useful rationale,
complete resource scopes and nearby firmware/transport/ownership/failure conditions. Optimize reader
understanding rather than word count. Apply this criterion in every investigator/editor/reviewer packet.

## Phases and ownership

1. **Astra baseline:** local/GitHub commit, user files and prior frozen manifests; full tracked inventory
   (44 library C#, 19 example C#, 14 unit C#, seven integration C#, four Markdown/four projects/two APIs/
   two runners). Preserve working-byte versus Git normalization differences explicitly.
2. **Sol investigation:** complete semantic units across public/internal/example/test prose, assertions,
   implicit calls, unfinished work and assigned shared OpenPGP sections. Reconcile effective accessibility,
   generated/extension/partial representation, clean-unit nominees and historical exclusions. Focus on PIN
   identity versus VERIFY P2, KDF transitions/ownership, crypto formats, partial key/config mutation,
   certificates/attestation, reset exceptions, firmware provenance, UIF and real tool behavior.
3. **Sources/triage:** original OpenPGP Card specification plus applicable RFC/vendor claims; exact title,
   revision/status/section/actor/strength/conditions. Pin Python implementation and relevant history as
   corroboration; distinguish firmware gates from normative requirements. Initial research plus one focused
   follow-up, then precise evidence gap/decision. Stable PGP-IMP-* and PGP-DEP-* IDs bind evidence to future
   implementation and documentation-reopening triggers. Core questions remain record-only.
4. **Fresh Sol editing:** module XML/guides/internal/example/test commentary plus assigned shared sections,
   one owner. Preserve executable bodies, help strings, assertions, API snapshots and directives. Complete
   task guidance for discovery, PINs/recovery, generation/import, algorithm-specific crypto, metadata,
   certificates/attestation, configuration and reset. Classify complete/composable/application-dependent
   and historical examples. Runtime fixes remain deferred to Pass 2.
5. **Fresh Sol review:** full affected units and original consequential citations, actual assertions and
   house style. Required factual/usefulness/style corrections versus optional preferences; recheck exact
   corrected hashes. Reconcile late hardware implications across the claim-to-document map.
6. **Astra sampling:** at least 10% of final eligible clean semantic units, minimum three where available,
   spanning API/narrative-example/internal-test; remove edited overlaps, expand categories after a miss.
7. **Astra execution gates:** serialized toolchain builds/tests; complete documentation groups and entire
   19-source OpenPgpTool with real references/analyzers and actual Csc evidence. Unique project names;
   ordinary sample bin/obj; capture immediately after real compilation. Scope formatter to edited solution
   files and prove tokens/directives preserved with exact harmless import accounting. All API snapshots,
   clean solution/unit/docs-package/architecture/complexity gates; zero compiler/analyzer warnings.
8. **Hardware:** establish identity/app state before mutation. Inspect all 49 setup/body/cleanup effects;
   every method requests reset-before, helper has no post-test reset. Isolate KDF transition and inspect
   firmware early-return paths. Run applicable cases including two slow RSA cases; distinguish actual
   ceremonies from setup failure/skip/early return. Add bounded independent crypto/PIN/KDF/presence probes
   where they resolve consequential claims. Coordinate real touch; record cancellation terminal outcome,
   drain and fresh-session recovery separately. Final independent cleanup/readback checks actual reset
   scope, PIN/retry/KDF/key/certificate/UIF/counter/configuration state and attestation exceptions.
9. **Fresh final Sol evidence review:** exact coverage/acceptance/current status, raw failures and successes,
   compiler provenance, sampling, archive preservation, unresolved dimensions. Compact evidence review.
10. **Astra publication:** resolve required corrections, final record checks, immutable manifest, explicitly
    stage owned paths, commit/push and verify remote HEAD. External handoff records publication after freeze.

## Boundaries and evidence

Run: modules/openpgp/pass-1/openpgp-001. External handoff: documentation-audit/openpgp.md. Related logical
records may share files; save phase transitions, ownership, full coverage, findings/decisions, original
source evidence, edits/reviews/corrections, failed attempts, validation/hardware outcomes and closeout.
Preserve all previous finalized archives. Keep generated binaries/caches outside the frozen set; retain
actual compiler source/configuration/log identities. Verify required external identities explicitly.

Shared-document candidates: root README, highlights, comparison, migration guide/map/gaps, user interaction
and narrow architecture/NativeAOT claims. Read relevant sections only; other-module targets need existence
checks rather than source investigation. Existing DOC-002 map parsing issue remains separately tracked.
Live-device Linux USB evidence qualifies that scope; NFC, older firmware, other platforms and AOT runtime
need separate evidence. Documentation completion, implementation completion and hardware qualification
are separate statuses. A passing test name or output length alone establishes only its actual assertions.
