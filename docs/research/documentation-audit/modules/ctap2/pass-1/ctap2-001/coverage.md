# CTAP2 investigation and final content coverage

2026-11-04 reconciliation of 2026-11-03 evidence; **accepted by the coordinator with later corrections below**. Machine-readable ledger:
[coverage.json](coverage.json). Candidate dispositions: [triage-addendum.md](triage-addendum.md).
This records editor read plan/inventory/triage, all A–G reports, boundary map, framework adjudication,
F's external/snippet/reference evidence and fresh review B. Source reads below are **attributed to
the original investigators**. Neither inclusion in this ledger nor an ongoing editor worktree means
final edit acceptance, independent re-review, sampling, executable preservation or validation passed.

## Complete module-path denominator

Every one of the **183** baseline paths is explicitly listed under `coverage.json.module_paths`.
Exact path is `src/Ctap2/` plus the listed relative path, not an inferred directory glob. Join each
to `inventory.json.files[path]` for baseline Git-blob SHA/bytes and to the owner-report coverage table
for the individual candidate/support disposition. All reports claim complete file reads including
test bodies, helper encoders and assertions. This is investigation coverage, not full-file clean prose.

| Owner | Exact inventory paths / reported full reads | Evidence table | Nominated semantic units | Eligible denominator |
| --- | ---: | --- | ---: | ---: |
| A | 30 | investigation-a.md:515–546 | 40 | 39 |
| B | 13 | investigation-b.md:35–49,384–411 | 28 | 26 |
| C | 30 | investigation-c.md:490–538 | 12 | 11 |
| D | 20 | investigation-d.md:329–350 | 38 | 34 |
| E | 57 | investigation-e.md:189–205,211–269 | 15 | 12 |
| F | 33 | investigation-f.md:228–277 | 21 | 2 |
| G | 0 overlapping module edit paths | boundary dependency units, not WebAuthn whole-file audit | 8 | 8 |
| **Total** | **183** | A30+B13+C30+D20+E57+F33 | **162** | **132** |

**170 C#** =73 library+77 tests+20 FidoTool; other13=5 Markdown+8 API/project/runner support.
No module path is silently excluded from investigation merely because it contains no useful prose.
B's coverage table typo `UserVerificationRetryStatus.cs` resolves to the actual inventory path
`src/Ctap2/src/Pin/UserVerificationRetryStatus.cs`; there is no extra similarly named file or missing
PIN-permission file. `CredentialManagementModels.cs` is also explicitly retained despite compact
plan labels. G has no duplicate module file owner/count.

### Hash provenance and framework reconciliation

At investigation checkpoints A30/B13/C30/D20/E57 matched baseline hashes. F29/33 matched checkout
bytes; all four differing project files were subsequently verified by the coordinator as **LF Git
blob versus CRLF checkout**, with empty source diff. Earlier F/framework reports retain the initially
unresolved observation. Do not rewrite them, call the source mutated, or report historical179/4 as
current final hashes. Investigation-era null fields have now been replaced by final per-path records below.

D-08's wire-order inference is rejected: actual Ctap2Canonical writer auto-sorts. C-008's numerical
canonical-order explanation is wrong, not its emitted wire order. A-014 is IMP-025, code-inferred
big-endian portability from pinned Guid implementation; **no big-endian execution**. Framework
ReadEndMap/trailing-byte probe is bounded evidence, not execution of every SDK decoder.
Fresh B review distinguishes forced-change **CTAP2.1 legacy0x05 PIN_INVALID** from **permission0x09
PIN_POLICY_VIOLATION**. D-14's legacy assertion is not a confirmed wrong-status defect; preserve
disputed/rejected subclaim provenance and ongoing D correction, without removing its cleanup/test gaps.
RB-R1 actor precision and optional force context are ongoing documentation corrections, not final review.

## API snapshot and support-data exclusions

F read all950 snapshot lines: **948 nonblank/non-directive entries**, **85 effective public families**
=84 top-level public types+public nested `PreviewSignCbor.InnerAttestationObject`. This reconciles
the initial apparent85 type entries, not 948 independent prose units or 86 types.

- The C#14 extension receiver marker is an analyzer/compiler projection, not another public family.
- Two logical device extension operations appear as extension members and lowered static projections;
  audit the source documentation once per logical operation, not four unrelated APIs.
- Generated record Clone$/equality/operators/hash/ToString/EqualityContract/PrintMembers/copy ctor/
  Deconstruct are excluded from **invented prose**, not deleted from API preservation checks. Explicit
  domain constructors/properties/methods remain A–E-owned documentation units.
- Three `~override` removals plus nested ToString removal (931–934) are historical removals, not four
  active undocumented APIs. FeatureFido2 is a real current public field.
- Shipped nullable marker, API snapshots, project/reference declarations, JSON runner/config/results,
  generated Core boundary inventory and bin/obj/XML/AST output do not manufacture clean semantic prose.
  Their existence/inventory/preservation responsibilities remain distinct.

## Clean semantic-unit denominator and completed sampling

The clean denominator is **named bounded explanatory units**, not whole files, every assertion, or
generated members. `coverage.json.clean_semantic_units.units` identifies all162 nominations by ID
and exact original report row; that row supplies source/member/range, rationale and limitations.
When a row holds multiple IDs, each ID selects only its own named clause. Scope overrides/exclusions
are explicit. Referencing these immutable rows retains provenance without inventing new full reads.

Eligibility means investigator-nominated **prose** with no reported required correction in the named
unit. It is not independent review, conformance or executed verification. Conservative exclusions
remove mixed/unverified scope and no-prose units; an investigator's accurate behavior observation
alone does not create a documentation unit. The30 exclusions are not source defects or missing-path
coverage. They may be split/reinstated only after precise prose bounds are identified and coordinator
review approves a revised denominator.

| Excluded IDs | Why excluded rather than silently counted clean |
| --- | --- |
| A-C14 | Named next-assertion instruction explicitly needs count/timer additions; mixed unit |
| B-C17/18 | Clearing guard/conditional assertion meaning without exact bounded explanatory prose |
| C-CL-03 | Plain counted encoder, no substantive prose identified |
| D-C06/23/29 | Forwarding/toggle/range behavior, not identified clean explanatory prose |
| D-C34 | Mixed fake-wire/tag16 fixture still needs exact prose qualification |
| E-CLEAN-06/09/13 | SDK null/empty guards, broad AAD/cleanup seam range and historical PRD/byte fixture need narrow clean-prose splits |
| F-C02–06/08–14 | Using/progress/confirmation/display/finally/demo behavior nominations lack exact prose bounds; some include known XML/UI/error/RP-binding corrections |
| F-C15–21 | API marker/snapshot/project/runner/support data, not substantive prose |

F-C01 counts only package alpha/install/transitive-Core/naming paragraphs, not the shell fence.
F-C07 is forwarding XML/inheritdoc, not whole OutputHelpers behavior. D-C16 counts signing-prefix/
parameter rationale only, excluding the rejected insertion-order judgment. Original nominations
remain immutable. E15 and F21 are explicit counted nominations, not table/data size guesses.

**Sampling complete: 19 eligible units / 132 (14.4%)**, exceeding the minimum14. The20 source/assertion
checks in [sampling.md](sampling.md) span API XML, narrative/examples, internal/test explanations and G
boundary units. F-C10 was excluded from the conservative population, so its useful behavior check is
supplemental and not counted. No material clean-unit miss was found. Inspected assertions are not executed
tests; later material misses still require category expansion. Current JSON unit rows account for leading
errata while retaining original baseline source ranges.

## External reference read depth and history

F discovered **133 exact external paths**:44 active/reference rows+19 agent/tooling collateral+70
historical/campaign discovery rows. The **133-path manifest** is
[evidence/f/external-paths.md](evidence/f/external-paths.md), incorporated path-by-path with original
discovery history intact. `full` means whole document read by that investigator as a dependency;
`partial` means complete named paragraphs/blocks/ranges only; `index` means discovered collateral,
not a semantic read. Active consumer material, authority, current campaign records, immutable historical
material and generated evidence are distinct classifications. No index path becomes a clean unit.

### All44 F active/reference rows, with exact read depth

| Exact repository path | F depth / history classification and bounds |
| --- | --- |
| CLAUDE.md | full; current agent authority, not CTAP tutorial |
| README.md | partial; current module/package links and operation label |
| TOOLCHAIN.md | partial; current CTAP runner blocks; negative direct-test example historical |
| docs/LOGGING.md | partial; current logger name plus complete illustrative class block |
| docs/NATIVE-AOT.md | index; current platform collateral, not execution |
| docs/SDK-HOUSE-STYLE.md | index by F; full required policy by A/C/E/G, not consumer audit |
| docs/TESTING.md | full; current runner/authorization/traits; historical negative blocks classified separately |
| docs/architecture/connection-ownership-and-contention.md | partial; exchange/cancel/lifetime and test-evidence table |
| docs/architecture/device-discovery-guarantees.md | index; current correlation/discovery collateral |
| docs/architecture/physical-device-model.md | partial; complete CTAP discovery/connection blocks, separate scopes |
| docs/architecture/raw-access-tiers.md | partial; full raw CTAPHID block and buffer/cancellation contracts |
| docs/architecture/sdk-architecture-diagrams.md | partial; six full CTAP Mermaid blocks, F-11 correction |
| docs/architecture/sdk-architecture-map.yml | index; current source-path data map |
| docs/architecture/webauthn-extension-processing.md | partial F claim/flow sections/full text diagram; full G boundary dependency |
| docs/docs-inventory-report.md | index; documentation inventory data, not API authority |
| docs/linux-setup.md | partial; complete FIDO/OTP udev block/setup references |
| docs/migration/v1-to-v2-changelog.md | partial; matched full migration entries, historical commit subjects retained |
| docs/migration/v1-to-v2-gaps.md | partial; point-in-time baseline/status/FIDO/WA units, **immutable historical conclusions** |
| docs/migration/v1-to-v2-map.yml | partial; current rename/result/raw/PIN/prompt mappings with historical notes |
| docs/migration/v1-to-v2.md | partial; complete CTAP recipes and historical before/after blocks, current WA boundary |
| docs/troubleshooting/testdiscovery-findings.md | index; dated runner troubleshooting history |
| docs/usage/device-discovery.md | partial; full discovery block/one-shot/order caveats |
| docs/usage/user-interaction.md | full F/G; boundary callback/example, other applet semantics not audited |
| docs/v1-to-v2-comparison.md | partial; CTAP/errors/raw/parity boundary; historical v1 labels retained |
| docs/v2-highlights.md | partial F package/client/RP/prompt; G1–65 WebAuthn section |
| scripts/alpha/RELEASE_NOTES.md | partial; current ten-package list and CTAP spelling |
| src/Cli.Commands/src/Yubico.YubiKit.Cli.Commands.csproj | index; current dependency data, not CLI compatibility |
| src/Core/CLAUDE.md | partial F CTAP/naming/raw units; required authority read by C/A, no whole-Core audit |
| src/Core/README.md | index; current consumer dependency, no inferred full read |
| src/Core/src/Yubico.YubiKit.Core.csproj | index; dependency data |
| src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/core-boundaries.v1.json | generated evidence index; excluded from prose |
| src/Management/CLAUDE.md | partial; complete capability flags block, legitimate Fido2 name |
| src/Management/README.md | index; sibling reference, prior cycle authority |
| src/Management/examples/ManagementTool/README.md | partial; selector/combined-reset qualifications, not whole sibling sample |
| src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/Yubico.YubiKit.PublicApi.UnitTests.csproj | index; test-only project dependency |
| src/Tests.Shared/CLAUDE.md | index; harness authority collateral, no full-read inference |
| src/Tests.Shared/appsettings.json | index; config data, no operation or edit |
| src/WebAuthn/CLAUDE.md | full F/G; every block, boundary dependency, not complete WA module audit |
| src/WebAuthn/PACKAGE_README.md | index; no invented full package review by G |
| src/WebAuthn/README.md | full F/G including implicit-client examples; G-010 inherited requirement corrected and independently re-reviewed |
| src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj | index; one-way dependency data |
| src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/Yubico.YubiKit.WebAuthn.IntegrationTests.csproj | index; references, not test execution |
| verification/MacOSHidRouteVerification/MacOSHidRouteVerification.csproj | index; support project, not live route evidence |
| verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj | index; support project, not AOT qualification |

###19 tooling collateral paths — discovery/index only

These exact paths have no claimed consumer/protocol full-read coverage merely from F discovery:

- `.claude/agents/dx-validator.md`
- `.claude/agents/ralph-loop.md`
- `.claude/agents/spec-writer.md`
- `.claude/skills/docs-write-agent-copilot/SKILL.md`
- `.claude/skills/domain-api-design-standards/SKILL.md`
- `.claude/skills/domain-build/SKILL.md`
- `.claude/skills/domain-platform-porting/SKILL.md`
- `.claude/skills/domain-security-guidelines/SKILL.md`
- `.claude/skills/domain-spec-writing-standards/SKILL.md`
- `.claude/skills/domain-test/SKILL.md`
- `.claude/skills/domain-ux-heuristics/SKILL.md`
- `.claude/skills/domain-yubikit-compare/SKILL.md`
- `.claude/skills/workflow-interface-refactor/SKILL.md`
- `.claude/skills/workflow-product-orchestrator/SKILL.md`
- `.github/agents/dx-validator.agent.md`
- `.github/agents/spec-writer.agent.md`
- `.github/agents/yubikit-porter.agent.md`
- `.github/prompts/architecture-docs-pr-preview.md`
- `.github/prompts/migration-docs-monthly-synthesis.md`

###70 historical/campaign discovery rows — history, not current API approval

Exact paths below retain F's discovery provenance. `R/` expands to
`docs/research/documentation-audit/`. Unless deeper attributed reads are stated below, these are
**index-only**. Historical plans/reports/old edit/review/closeout records remain immutable; result/
manifest/coverage JSON are generated evidence/data, never invented prose. Current campaign README,
resumption/module handoff and preservation records are **current coordination records referencing
history**, not stale SDK documentation to automatically rewrite.

- `2026-09-21-yubikit-async-boundaries-ISA.md`
- `docs/plans/2026-09-04-applet-public-api-consolidation.md`
- `docs/plans/2026-09-14-public-return-contracts.md`
- `docs/plans/raw-access-tiers.md`
- `docs/plans/single-connection-single-session.md`
- `docs/plans/yubikit-async-boundaries/00-status.md`
- `docs/plans/yubikit-async-boundaries/01-product.md`
- `docs/plans/yubikit-async-boundaries/03-program-design.md`
- `docs/plans/yubikit-async-boundaries/addenda/earlier-multi-key-architecture.md`
- `docs/plans/yubikit-async-boundaries/addenda/earlier-multi-key-program-design.md`
- `docs/research/audit-v2-close-out.md`
- `R/README.md` — current coordination record
- `R/maintenance/2026-11-03-record-preservation/validation/result.json` — generated historical validation evidence
- `R/modules/management/pass-1/management-001/correction-c-compilation.md`
- `R/modules/management/pass-1/management-001/coverage.json` — historical coverage data
- `R/modules/management/pass-1/management-001/edit-c.md`
- `R/modules/management/pass-1/management-001/investigation-a.md`
- `R/modules/management/pass-1/management-001/investigation-c.md`
- `R/modules/management/pass-1/management-001/record-manifest.json` — historical hash data
- `R/modules/management/pass-1/management-001/review-a.md`
- `R/modules/management/pass-1/management-001/review-c-module.md`
- `R/modules/management/pass-1/management-001/review-c-shared.md`
- `R/modules/management/pass-1/management-001/validation/20261103-195646-771-build+test+docs-qa+docs-architecture+complexity/result.json` — generated historical validation evidence
- `R/modules/management/pass-1/management-001/validation/checker-source/xunit2-context/strict-raw-manifest.json` — historical compilation evidence data
- `R/modules/management/pass-1/management-001/validation/compiled-examples/manifest.json` — historical compilation evidence data
- `R/modules/management/pass-1/management-001/validation/preliminary-examples/manifest.json` — historical compilation evidence data
- `R/record-manifest.json` — current campaign preservation data, not source prose
- `R/record-preservation-review.md` — current preservation record
- `R/records/resumption.md` — current coordination record
- `R/records/runs/fixes-wave-001/cli-full-gates/result.json` — generated historical evidence
- `R/records/runs/fixes-wave-001/combined-gates/result.json` — generated historical evidence
- `R/records/runs/fixes-wave-001/protocol-full-gates/result.json` — generated historical evidence
- `R/records/runs/fixes-wave-002/combined-full-gates/result.json` — generated historical evidence
- `R/records/runs/fixes-wave-002/inputs-full-gates/result.json` — generated historical evidence
- `R/records/runs/fixes-wave-002/lifecycle-full-gates/result.json` — generated historical evidence
- `R/records/runs/oath-001/coverage.json` — historical coverage data
- `R/records/runs/oath-001/toolchain-result.json` — generated historical evidence
- `R/records/runs/oath-002/toolchain-result.json` — generated historical evidence
- `R/records/runs/oath-003-004/pre-edit-content.json` — historical source snapshot data
- `R/records/runs/oath-003-004/toolchain-result.json` — generated historical evidence
- `R/records/runs/upstream-001/toolchain-result.json` — generated historical evidence
- `R/records/runs/webauthn-001/closeout.md`
- `R/records/runs/webauthn-001/coverage-final.json` — historical coverage data
- `R/records/runs/webauthn-001/edit-a.md`
- `R/records/runs/webauthn-001/edit-b.md`
- `R/records/runs/webauthn-001/edit-c.md`
- `R/records/runs/webauthn-001/investigation-a.md` — G partial1–120 including leading erratum, A dependency findings; no whole-report G read
- `R/records/runs/webauthn-001/investigation-b.md` — G1–292 citation/dependency passages
- `R/records/runs/webauthn-001/investigation-c.md` — G focused returned search excerpts WA-C-002/004/005/008/010/012, not full
- `R/records/runs/webauthn-001/packet-c.md`
- `R/records/runs/webauthn-001/review-a-followup.md`
- `R/records/runs/webauthn-001/review-a.md`
- `R/records/runs/webauthn-001/review-b-blob-final.md`
- `R/records/runs/webauthn-001/review-b-blob-followup.md`
- `R/records/runs/webauthn-001/review-b.md`
- `R/records/runs/webauthn-001/review-c-followup.md`
- `R/records/runs/webauthn-001/review-c.md`
- `R/records/runs/webauthn-001/review-checkpoint.md`
- `R/records/runs/webauthn-001/reviewed-files.json` — historical review-target hash data
- `R/records/runs/webauthn-001/sample-expansion.md`
- `R/records/runs/webauthn-001/sampling.md`
- `R/records/runs/webauthn-001/status.md`
- `R/records/runs/webauthn-001/triage.md` — full G/A/C handoff authority read, historical engineering IDs retained
- `R/records/runs/webauthn-001/validation.md`
- `R/records/runs/webauthn-001/validation/20261103-145050-713-test/result.json` — generated historical evidence
- `R/records/webauthn-plan.md`
- `R/webauthn.md` — current handoff referencing completed historical checkpoint, full G and C handoff read
- `docs/research/native-aot-readiness-data.md` — dated readiness data
- `docs/research/native-aot-readiness.md` — dated readiness conclusions
- `docs/research/webauthn-documentation-audit-checkpoint.md` — historical checkpoint

Untracked user-preserved audit-v2 closeout/execution/Jira files and `mise.toml` were not added to the
133 tracked discovery denominator or overwritten. A's narrow historical closeout findings#9/10 are
attributed separately in investigation-a.md:156–165. Earlier WA baseline `e353b49…` and documentation
checkpoint `9399b94…` are history, not this cycle's current-source/runtime qualification.

## Additional exact source-boundary dependencies

These augment F's133 discovery paths, not increase183 module paths or turn G into an entire WA audit.
Different investigators may read different depths of the same file. No aggregate union is reported as
one invented whole-file read. The strongest explicitly claimed full read can be attributed to its
owner; otherwise only the stated range applies.

### Core/shared — attributed narrow reads

| Exact path | Attributed read depth and semantic boundary |
| --- | --- |
| src/Core/src/Sessions/ApplicationSession.cs | full A; construction/configuration/ownership units |
| src/Core/src/Devices/Feature.cs | full A; firmware support sentinel semantics |
| src/Core/src/Devices/FirmwareVersion.cs | A partial22–81 |
| src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs | full A; single exchange/cancellation/disposal seam, not native platform qualification |
| src/Core/src/Credentials/UserPresenceNotification.cs | full A; callback notification boundary |
| src/Core/src/Utilities/ExchangeGuard.cs | full A; admission/overlap/cancellation boundary |
| src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs | A partial100–189 transmit/SELECT/status/configuration |
| src/Core/src/Credentials/ConsoleCredentialReader.cs | C partial184–205,210–279 char-count/UTF-8 boundary, XREF-003/IMP-006 only |
| src/Cli.Shared/src/Cli/CommandHelper.cs | full F; Ctrl+C event registration; shared CLI not whole-module audit |
| scripts/toolchain/Context.cs | F partial1–90 selection/options unit, not CLI execution |
| scripts/toolchain/Discovery.cs | F relevant discovery references, depth not claimed full |
| scripts/toolchain/TestTargets.cs | F search-only runner/filter references |
| scripts/toolchain/Conventions.cs | F search-only MTP/smoke constants |
| src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs | F discovered declaration/uses, not full semantic read |

B's WebAuthnBackend dependency read and relevant WebAuthn CLAUDE sections do not certify every
searched PIN test. B's shared user-interaction PIN terminology was search-only; F/G's separate full
guide read does not retroactively turn B's search into a full read. E's cross-owner source/domain
reads remain those expressly named in its report, not a whole C/F source audit. CTAP cross-owner
full/partial dependencies are explicitly in A/B/C/F/G manifests; they overlap the183 owned paths,
not new files. C/E Core ARKG delegation/profile evidence is not a whole primitive audit.

### G WebAuthn source manifest — boundary units, not whole-module denominator

`WC/`=`src/WebAuthn/src/Client/`; `WE/`=`src/WebAuthn/src/Extensions/`.
All26 following files were reported **full boundary-dependency reads by G**, with only named boundary
claims/clean units assessed. They are not26 completely re-audited WA source/document files:

- `WC/WebAuthnClient.cs`
- `WC/WebAuthnBackend.cs`
- `WC/WebAuthnClient.Authentication.cs`
- `WC/WebAuthnClient.Ceremony.cs`
- `WC/WebAuthnClient.PinUvAuth.cs`
- `WC/UserVerification/UvDecision.cs`
- `WC/CredentialFilter.cs`
- `WC/PinUvAuthTokenSession.cs`
- `WC/PinUvSharedSecret.cs`
- `WC/Authentication/AuthenticationResponse.cs`
- `WC/Authentication/MatchedCredential.cs`
- `WC/Authentication/CredentialMatcher.cs`
- `WE/Processing/RegistrationExtensionSession.cs`
- `WE/Processing/AuthenticationExtensionSession.cs`
- `WE/Processing/Processors/PrfProcessor.cs`
- `WE/Processing/Processors/LargeBlobProcessor.cs`
- `WE/Processing/Processors/CredProtectProcessor.cs`
- `WE/Processing/Processors/MinPinLengthProcessor.cs`
- `WE/Processing/Processors/PreviewSignProcessor.cs`
- `WE/Codec/PrfCodec.cs`
- `WE/Codec/PreviewSignCodec.cs`
- `WE/Inputs/PrfInput.cs`
- `WE/Inputs/CredProtectInput.cs`
- `WE/Inputs/LargeBlobInput.cs`
- `WE/Outputs/PrfOutput.cs`
- `WE/PreviewSign/PreviewSignSigningParams.cs`

Partial G only: `WC/WebAuthnClient.Registration.cs:94–278`,
`WC/WebAuthnClient.Validation.cs:110–191`. G's eight clean IDs select bounded API/guide/ownership/
extension orchestration units from these source reads and two-layer guides. Full browser/native
serializer privacy, every WA test, all Core transport/crypto, hardware and product support were not
audited. DEC-002 native selected-user privacy remains a decision, not forced DTO stripping.

## Final snippets, per-path records and executed gates

F's complete inventory includes **39 Markdown fences+5 XML code=44 module blocks**, and external
blocks including implicit WebAuthn `client`/callback/setup scopes. Each block's path/opening-line,
complete/fragment/sketch/historical classification and typed prerequisites are in
[evidence/f/snippets.md](evidence/f/snippets.md). That initial classification is not itself compilation
proof; final [snippet-validation.md](snippet-validation.md) supplies the completed33-group/six-context
build and actual compiler-input evidence, with95 blocks inventoried,39 selected and56 excluded.
FidoTool is20 C# source files in one project, not20 standalone RP journeys. Historic negative commands,
layout notation, sketches and generated artifacts do not inflate runnable example counts. Final
meaningful compilations must retain real project references/analyzers/attributes/calls/cleanup and
record exact diagnostics; no ceremony execution is implied by compilation.

`coverage.json.final_per_path_records` now contains all183 module paths:158 edited with independent
acceptance/hash/transformation pointers, and25 unchanged with completed investigation dispositions and
baseline/EOL reconciliation. Unchanged paths are not falsely called individually independently reviewed.
Seven additional authored external paths and five generated outputs are recorded separately. The
authoritative [reviewed-files.json](reviewed-files.json) reconciles165 authored files and five outputs.
The original null templates remain explicitly labeled as schema defaults, not current statuses.

Actual `final_gates` point to independent review/re-review,19/132 clean samples, final boundary acceptance,
clean solution and3,998 unit tests (492 CTAP), docs/architecture/complexity/scoped-format passes, all33
compiled groups,152 import-accounted C# preservation checks and20 unchanged API snapshots. Thirty-one
unused namespace imports are an explicit formatter exception, not identical source syntax or a behavior
fix. Stable content/evidence and retention passed final review; CR-R1 progression reconciliation awaits
its focused follow-up, and commit/remote publication remains pending. Earlier investigator/editor no-run
statements remain true for those actors and are not the current coordinator execution result.

## Precise remaining gaps

1. CR-R1 current-record follow-up, final run-artifact manifest/staging checks and verified publication.
2. Addendum026–048/XREF-003 are accepted; original WA engineering counts remain unchanged. Current source
   reviews, boundary and validation are complete; WA-XREF-001 is resolved as documentation only.
3. Sampling19/132 exceeds minimum14;30 conservative exclusions remain excluded. Future denominator
   revisions must preserve reasons and material-miss expansion history; no promised sample is counted.
4. F index-only external collateral remains index-only. No invented full reads or global Core/WA audit.
5. Product reset5.5.2/5.5.3, bio vendor fields/standard-preview minima, MC/PPUAT/encrypted-field earliest
   SKU support, ARKG vendor-wrapped profile, preview beta0x7F/ECDH and exact unsigned introduction
   remain bounded evidence gaps. BE/BS is now supported by the pinned L3 Recommendation verified by
   editor and independent reviewer; that citation gap is resolved without a code or conformance claim.
6. All shipping/sample/test implementation changes are deferred. Mandatory decoder safety, optional
   platform policy and demo enhancement are distinguished, not filed as one missing-feature checklist.
7. Hardware/PIN/UP/UV/reset/storage/signing/retry/OS/firmware support and big-endian qualification not run.

Static record validation is separate: JSON/inventory/math/path/ID/reference consistency only. Its
result is recorded in `coverage.json.record_only_static_validation`, not a final SDK validation gate.

### Original static record validation checkpoint (superseded by current gates above)

Read-only Python heredoc parsed JSON and compared the expanded module paths/owner map to inventory,
checked on-disk path existence, **162 actual unit IDs** against exact immutable report rows,
excluded/eligible math,103 unique candidate rows, ordered026–048 supplemental IDs, all133 F external
manifest paths and59 added boundary path occurrences, and948 API entries from `git show BASE:path`.
The result was **exit0 / errors[]**. The explicit132-unit minimum is14; all final gates and final
per-path acceptance/hashes remain pending. This is not formal JSON Schema tooling, protocol re-review,
source acceptance, SDK build/test/example compilation or hardware evidence. A fresh recheck of the
final edited records, including baseline inventory hashes and pending-field schema, precedes handoff.
