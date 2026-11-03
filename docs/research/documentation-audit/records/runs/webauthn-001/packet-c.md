# WA-C — user documentation and cross-document investigation

Baseline `e353b49afa15d1d104fce70bbf1e1abc59ddb132`, branch `yubikit-docs-audit`, repository `/home/rupicola/Code/y/Yubico.Yubikit.NET`.

Read root/module instructions, `../../style.md`, and `status.md`. Read-only investigation, no recursive agents, device work, build/test/generation, or repository edits. Only allowed report write: `investigation-c.md` beside this packet, using patch.

Primary scope: complete `src/WebAuthn/README.md`, `PACKAGE_README.md`, `CLAUDE.md`. Reconcile both public API snapshots against source accessibility and classify seven project/API/runner support files. There are 542 non-comment API snapshot entries (including generated members); do not add artificial boilerplate XML for generated record methods. Complete all module public surface coverage through WA-A/WA-B evidence, with any omissions explicitly recorded.

External scope: WebAuthn-specific passages in the following active-discovery results (not wholesale review of their other modules). Read surrounding semantic sections, not only hit lines:

- `2026-09-21-yubikit-async-boundaries-ISA.md` (classify historical/specification role)
- `CLAUDE.md` (module list/toolchain example)
- `docs/architecture/applet-public-api.md`
- `docs/architecture/physical-device-model.md`
- `docs/architecture/raw-access-tiers.md`
- `docs/architecture/sdk-architecture-diagrams.md`
- `docs/architecture/webauthn-extension-processing.md` (complete page)
- `docs/docs-inventory-report.md` (generated historical inventory, source inputs only)
- `docs/NATIVE-AOT.md`
- `docs/SDK-HOUSE-STYLE.md`
- `docs/TESTING.md`
- `docs/usage/user-interaction.md`
- `docs/v1-to-v2-comparison.md`
- `docs/v2-highlights.md`
- `README.md`
- `src/Core/README.md`
- `src/Ctap2/CLAUDE.md`
- `src/Ctap2/examples/FidoTool/README.md`
- `src/Ctap2/README.md`
- `src/Ctap2/tests/CLAUDE.md`
- `src/Management/examples/ManagementTool/README.md`
- `src/Management/README.md`
- `src/PublicApi/CLAUDE.md`
- `src/Tests.Shared/CLAUDE.md`
- `TOOLCHAIN.md`

Supplemental migration files: `docs/migration/v1-to-v2.md`, `v1-to-v2-map.yml`, `v1-to-v2-changelog.md`, `v1-to-v2-gaps.md`. Existing YAML parse failure (DOC-002, line 101 column 621) is historical evidence to recheck only if relevant; do not repair unrelated entries.

Read required module instructions before working within external modules. Preserve historical records and user-owned untracked research. Other module implementations are narrow evidence dependencies, not additional audit targets.

Investigation criteria: help consumers choose the API, provide prerequisites and a complete usable path, explain application/RP responsibilities, resources/results, selection/error handling and extensions. Verify specification citations against primary version/status/section/actor/layer. Consult Context7 for framework/CLI API claims as needed. Preserve useful local context and qualifiers. Meaningful behavior changes are deferred with precise user-doc limitations and re-review triggers.

Coordinator's unverified candidates to inspect: package README still requires a PSL-backed checker; module README indexes matches[0] before count handling, generates challenges/account IDs locally without explaining production RP ownership, opens multiple clients on one device in sequential snippets, and transport-hint snippet repeats registration with null PIN despite Required UV. Contributor guide describes removed adapters/preflight files, missing permission aggregation and single-credential previewSign. Architecture page appears stale on CTAP2 names, exclude preflight touch ordering and empty-match error. Highlights uses unsupported promotional/user-research assertions. These are candidates, not accepted findings.

Report IDs WA-C-001 etc; exact headings/symbols/source evidence; minimal edits, dependent docs; citation index; every listed file disposition (clean/read, finding, historical/generated excluded with reason). Explicitly label any unverifiable or unread scope. Do not infer OS/hardware behavior. Finish full scope, then return summary and report path.
