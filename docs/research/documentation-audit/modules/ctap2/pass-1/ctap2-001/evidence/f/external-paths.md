# External reference path manifest — F

Baseline 5b09a9b6, discovered/read 2026-11-03. Exact repository paths below, not parent-count guesses.
Tracked Markdown/YAML/JSON/props/project reference discovery searched names plus capabilities/PIN:
`Ctap2|CTAP|Fido2|FIDO2|FidoTool|MakeCredential|GetAssertion|ClientPin|PinUvAuth|credProtect|hmac-secret|
largeBlob|bioEnroll|authnrCfg|credMgmt|FidoSession|CreateFido|GetFido` (case-insensitive). Focused follow-up
searched old package/session/factory/credential-option/AID names and stale WithLargeBlob/FeatureCtap2/
PreviewSignAuthenticationInput. C# follow-up found the WebAuthn PreviewSignAuthenticationInput family,
which is valid in that layer. No active old CTAP package/session/factory name was found outside explicitly
historical migration material. Search discovery is not a whole-source audit.

## Active/reference paths: disposition of every match outside historical campaign files

`read` means relevant complete paragraphs/blocks were read; `full` means the entire document was read.
`index` means reference-only collateral discovered and retained for coordinator coverage, not claimed
fully audited. Independent G ownership is preserved for full CTAP/WebAuthn claim research.

| Exact path | Disposition |
|---|---|
| CLAUDE.md | full required authority; root examples not current CTAP task tutorials |
| README.md | read module/package links; clean FIDO2 authenticator operations over CTAP2 label |
| TOOLCHAIN.md | read CTAP-related runner snippet; negative direct-test example historical |
| docs/LOGGING.md | read CTAP logger-name and complete illustrative class block; no runtime change |
| docs/NATIVE-AOT.md | index shared AOT capability reference; no CTAP journey snippet |
| docs/SDK-HOUSE-STYLE.md | index architectural policy reference; no CTAP consumer snippet |
| docs/TESTING.md | full runner/authorization/traits/sample reference; F-06 shared policy conflict |
| docs/architecture/connection-ownership-and-contention.md | read exchange/cancellation/lifetime CTAP units and test-evidence table; F-02 dependency |
| docs/architecture/device-discovery-guarantees.md | index discovery/correlation reference, not CTAP operation docs |
| docs/architecture/physical-device-model.md | read full CTAP-relevant discovery/connection blocks; separate using scopes clean |
| docs/architecture/raw-access-tiers.md | read raw CTAPHID full block and cancellation/sensitive-buffer contracts |
| docs/architecture/sdk-architecture-diagrams.md | read all six CTAP-relevant complete Mermaid blocks; F-11 |
| docs/architecture/sdk-architecture-map.yml | index architecture source-path evidence map; re-review diagrams consistently |
| docs/architecture/webauthn-extension-processing.md | read current boundary/claim/flow sections and full text diagram; G substantive standards owner |
| docs/docs-inventory-report.md | index documentation inventory; no new current API authority |
| docs/linux-setup.md | read complete FIDO/OTP udev block and setup references; libfido2 naming correct |
| docs/migration/v1-to-v2-changelog.md | read matched complete migration entries; old commit subjects/alpha names historical |
| docs/migration/v1-to-v2-gaps.md | read baseline/status and relevant FIDO/WebAuthn units; point-in-time report, preserve |
| docs/migration/v1-to-v2-map.yml | read current CTAP rename/raw-return/result-contract/PIN/prompt entries, including original notes; F-11 helper boundary |
| docs/migration/v1-to-v2.md | read complete CTAP recipes/historical before-after blocks and current WebAuthn boundary text |
| docs/troubleshooting/testdiscovery-findings.md | index dated runner troubleshooting, not current task proof |
| docs/usage/device-discovery.md | read complete discovery block and one-shot/order caveats |
| docs/usage/user-interaction.md | full; implicit client call, method-scope and callback blocks included |
| docs/v1-to-v2-comparison.md | read CTAP/exception/forward-response/parity boundary units; historical v1 labels retained |
| docs/v2-highlights.md | read package/client-vs-RP and shared prompt scope units |
| scripts/alpha/RELEASE_NOTES.md | read current ten-package list and CTAP package spelling |
| src/Cli.Commands/src/Yubico.YubiKit.Cli.Commands.csproj | index CLI dependency reference; does not prove sample command compatibility |
| src/Core/CLAUDE.md | read required CTAP dependency/naming/raw transport units, not whole Core audit |
| src/Core/README.md | index related consumer dependency; raw/lifecycle technical audit remains A/Core |
| src/Core/src/Yubico.YubiKit.Core.csproj | index assembly/shared dependency |
| src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/core-boundaries.v1.json | generated evidence index, excluded from prose audit |
| src/Management/CLAUDE.md | read complete capability flags block; DeviceCapabilities.Fido2 correct |
| src/Management/README.md | index sibling feature reference; prior Management cycle authoritative |
| src/Management/examples/ManagementTool/README.md | read current selector/combined-reset scope qualifications; not every Bio key |
| src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/Yubico.YubiKit.PublicApi.UnitTests.csproj | index test-only CTAP project dependency |
| src/Tests.Shared/CLAUDE.md | index shared test harness dependency; no claim of test execution |
| src/Tests.Shared/appsettings.json | index shared authorization/config path, no device calls/config edits |
| src/WebAuthn/CLAUDE.md | full current boundary and every block; G owns claim adjudication |
| src/WebAuthn/PACKAGE_README.md | index package boundary qualification; G owns exact current package review |
| src/WebAuthn/README.md | full including implicit client blocks; reference model for F-01/F-02 |
| src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj | index one-way package dependency |
| src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/Yubico.YubiKit.WebAuthn.IntegrationTests.csproj | index test-project references, not device evidence |
| verification/MacOSHidRouteVerification/MacOSHidRouteVerification.csproj | index verification support project, no live route claims |
| verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj | index verification support project, no AOT verification executed |

## Extra source dependencies traced (not F-owned source edits)

- src/Ctap2/src/Ctap2Session.cs — complete file, request validation/GetInfo/UP/lifecycle/reset source.
- src/Ctap2/src/ICtap2Session.cs — complete interface, real operations and raw request signature.
- src/Ctap2/src/IYubiKeyExtensions.cs — complete factory/one-shot/XML blocks and transport resolution.
- src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs — complete encoding file, real tokenless uv handling.
- src/Ctap2/src/CredentialManagement/CredentialManagement.cs — complete file, borrowed token and result ownership.
- src/Ctap2/src/Pin/ClientPin.cs — full token-request/key-agreement/ValidatePin/PadPin units (not full file).
- src/Ctap2/src/AuthenticatorInfo.cs — Decode return/array helper units and PublicKeyCredentialParameters declaration.
- src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs — complete XML block and API snapshot signatures.
- src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs — XML/candidate response shape and typed parser units.
- src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs — complete layout XML block, not independent protocol audit.
- src/Cli.Shared/src/Cli/CommandHelper.cs — complete source, actual Ctrl+C registration.
- scripts/toolchain/Context.cs — complete selection/option construction unit (lines 1–90).
- scripts/toolchain/Discovery.cs — actual active-doc/project discovery references, no execution.
- scripts/toolchain/TestTargets.cs — runner selection description/filter search evidence only.
- scripts/toolchain/Conventions.cs — MTP opt-in and smoke trait definitions search evidence only.
- src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs — current type/uses discovered,
  not a removed CTAP type; G/E own semantic protocol input research.

## Agent/tooling collateral matches (index only, not consumer documentation)

- .claude/agents/dx-validator.md
- .claude/agents/ralph-loop.md
- .claude/agents/spec-writer.md
- .claude/skills/docs-write-agent-copilot/SKILL.md
- .claude/skills/domain-api-design-standards/SKILL.md
- .claude/skills/domain-build/SKILL.md
- .claude/skills/domain-platform-porting/SKILL.md
- .claude/skills/domain-security-guidelines/SKILL.md
- .claude/skills/domain-spec-writing-standards/SKILL.md
- .claude/skills/domain-test/SKILL.md
- .claude/skills/domain-ux-heuristics/SKILL.md
- .claude/skills/domain-yubikit-compare/SKILL.md
- .claude/skills/workflow-interface-refactor/SKILL.md
- .claude/skills/workflow-product-orchestrator/SKILL.md
- .github/agents/dx-validator.agent.md
- .github/agents/spec-writer.agent.md
- .github/agents/yubikit-porter.agent.md
- .github/prompts/architecture-docs-pr-preview.md
- .github/prompts/migration-docs-monthly-synthesis.md

## Historical plan/research/campaign matches — immutable exclusions

These exact paths were discovered, not rewritten or counted as current API/sample validation. Baseline
names and previously recorded implementation/spec conclusions must be preserved and re-baselined by
reference rather than edited to match new spelling.

- 2026-09-21-yubikit-async-boundaries-ISA.md
- docs/plans/2026-09-04-applet-public-api-consolidation.md
- docs/plans/2026-09-14-public-return-contracts.md
- docs/plans/raw-access-tiers.md
- docs/plans/single-connection-single-session.md
- docs/plans/yubikit-async-boundaries/00-status.md
- docs/plans/yubikit-async-boundaries/01-product.md
- docs/plans/yubikit-async-boundaries/03-program-design.md
- docs/plans/yubikit-async-boundaries/addenda/earlier-multi-key-architecture.md
- docs/plans/yubikit-async-boundaries/addenda/earlier-multi-key-program-design.md
- docs/research/audit-v2-close-out.md
- docs/research/documentation-audit/README.md
- docs/research/documentation-audit/maintenance/2026-11-03-record-preservation/validation/result.json
- docs/research/documentation-audit/modules/management/pass-1/management-001/correction-c-compilation.md
- docs/research/documentation-audit/modules/management/pass-1/management-001/coverage.json
- docs/research/documentation-audit/modules/management/pass-1/management-001/edit-c.md
- docs/research/documentation-audit/modules/management/pass-1/management-001/investigation-a.md
- docs/research/documentation-audit/modules/management/pass-1/management-001/investigation-c.md
- docs/research/documentation-audit/modules/management/pass-1/management-001/record-manifest.json
- docs/research/documentation-audit/modules/management/pass-1/management-001/review-a.md
- docs/research/documentation-audit/modules/management/pass-1/management-001/review-c-module.md
- docs/research/documentation-audit/modules/management/pass-1/management-001/review-c-shared.md
- docs/research/documentation-audit/modules/management/pass-1/management-001/validation/20261103-195646-771-build+test+docs-qa+docs-architecture+complexity/result.json
- docs/research/documentation-audit/modules/management/pass-1/management-001/validation/checker-source/xunit2-context/strict-raw-manifest.json
- docs/research/documentation-audit/modules/management/pass-1/management-001/validation/compiled-examples/manifest.json
- docs/research/documentation-audit/modules/management/pass-1/management-001/validation/preliminary-examples/manifest.json
- docs/research/documentation-audit/record-manifest.json
- docs/research/documentation-audit/record-preservation-review.md
- docs/research/documentation-audit/records/resumption.md
- docs/research/documentation-audit/records/runs/fixes-wave-001/cli-full-gates/result.json
- docs/research/documentation-audit/records/runs/fixes-wave-001/combined-gates/result.json
- docs/research/documentation-audit/records/runs/fixes-wave-001/protocol-full-gates/result.json
- docs/research/documentation-audit/records/runs/fixes-wave-002/combined-full-gates/result.json
- docs/research/documentation-audit/records/runs/fixes-wave-002/inputs-full-gates/result.json
- docs/research/documentation-audit/records/runs/fixes-wave-002/lifecycle-full-gates/result.json
- docs/research/documentation-audit/records/runs/oath-001/coverage.json
- docs/research/documentation-audit/records/runs/oath-001/toolchain-result.json
- docs/research/documentation-audit/records/runs/oath-002/toolchain-result.json
- docs/research/documentation-audit/records/runs/oath-003-004/pre-edit-content.json
- docs/research/documentation-audit/records/runs/oath-003-004/toolchain-result.json
- docs/research/documentation-audit/records/runs/upstream-001/toolchain-result.json
- docs/research/documentation-audit/records/runs/webauthn-001/closeout.md
- docs/research/documentation-audit/records/runs/webauthn-001/coverage-final.json
- docs/research/documentation-audit/records/runs/webauthn-001/edit-a.md
- docs/research/documentation-audit/records/runs/webauthn-001/edit-b.md
- docs/research/documentation-audit/records/runs/webauthn-001/edit-c.md
- docs/research/documentation-audit/records/runs/webauthn-001/investigation-a.md
- docs/research/documentation-audit/records/runs/webauthn-001/investigation-b.md
- docs/research/documentation-audit/records/runs/webauthn-001/investigation-c.md
- docs/research/documentation-audit/records/runs/webauthn-001/packet-c.md
- docs/research/documentation-audit/records/runs/webauthn-001/review-a-followup.md
- docs/research/documentation-audit/records/runs/webauthn-001/review-a.md
- docs/research/documentation-audit/records/runs/webauthn-001/review-b-blob-final.md
- docs/research/documentation-audit/records/runs/webauthn-001/review-b-blob-followup.md
- docs/research/documentation-audit/records/runs/webauthn-001/review-b.md
- docs/research/documentation-audit/records/runs/webauthn-001/review-c-followup.md
- docs/research/documentation-audit/records/runs/webauthn-001/review-c.md
- docs/research/documentation-audit/records/runs/webauthn-001/review-checkpoint.md
- docs/research/documentation-audit/records/runs/webauthn-001/reviewed-files.json
- docs/research/documentation-audit/records/runs/webauthn-001/sample-expansion.md
- docs/research/documentation-audit/records/runs/webauthn-001/sampling.md
- docs/research/documentation-audit/records/runs/webauthn-001/status.md
- docs/research/documentation-audit/records/runs/webauthn-001/triage.md
- docs/research/documentation-audit/records/runs/webauthn-001/validation.md
- docs/research/documentation-audit/records/runs/webauthn-001/validation/20261103-145050-713-test/result.json
- docs/research/documentation-audit/records/webauthn-plan.md
- docs/research/documentation-audit/webauthn.md
- docs/research/native-aot-readiness-data.md
- docs/research/native-aot-readiness.md
- docs/research/webauthn-documentation-audit-checkpoint.md

Untracked campaign work and user-preserved audit-v2 files were not scanned into a claim of immutable
baseline coverage or overwritten. Newly created ctap2-001 investigation records are not original source
references. Generated API analyzer projections, snapshot removals, runner-result JSON and Core boundary
inventory JSON are evidence/support artifacts, not prose examples to rewrite.

## Re-review owners / gaps

F edits only consumer Markdown/sample comments after triage. A owns factory XML/session/capability
contracts, B PIN/token prerequisites, C credential/response semantics, D admin sequences/cleanup, E
extension builder/output XML, G cross-layer claim map. Astra assigns shared-document editing ownership
after these candidates are reconciled. Index-only collateral is a precise remaining coverage limit;
it is not silently called clean. Consumer CTAP journeys/blocks are fully inventoried here; wholesale
Core/framework/project verification is outside this role and no compilation was attempted.
