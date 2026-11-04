# Security Domain Pass 1 — edit report

2026-11-04; fresh editor `github-copilot/gpt-6.1-sol`; baseline
`b73efe3813b429b4b8eb2a7e819a6ad7be699d47`. Documentation edits are ready for independent review and
coordinator validation, not a completed campaign gate or implementation closeout.

## Scope and evidence

Read root instructions; module README then CLAUDE; test instructions; campaign workflow/style; run plan,
findings, investigation, coverage and source evidence. Loaded `docs-module` before module guide edits and
`workflow-verify` before reporting checks. Read complete affected source/test semantic units, including
setup, assertions, cleanup and helper bodies. Read the actual integration project to establish the
xUnit-v2 snippet context. Context7 independently confirmed `Convert.FromHexString(string)` throws
`FormatException` for odd length/nonhex characters; source is the .NET 10 API page already identified in E3.

No Core implementation or documentation was opened. Module references to inherited types are not a Core
investigation. No other module, executable source tokens, test assertions, API snapshots, project/runner
settings, coordinator records or finalized archives were edited. No build, test, formatter, example host,
hardware, commit or delegation was run. Existing unrelated user/coordinator changes were preserved.

## Candidate-to-change mapping

| Candidate | Authored change |
| --- | --- |
| SD-C001 | Refreshed module/test guidance, actual helper/factory signatures, parameter scopes, xUnit-v2 typed examples and unit/integration command labels. Replaced obsolete initialization/error/logging implementation snippets with current flow explanations; retained useful command constants and typed metadata logging. Corrected the test-helper location link. |
| SD-C002 | README/package synopsis, interface reset contract (inherited by concrete API), contributor/test reset flow, cleanup comments and the exact SD example in AI-DOCS-GUIDE now distinguish destructive bounded attempts from verified factory restoration. Explain unauthenticated entry, partial mutation, cancellation, retry/exhaustion/empty-list limits and retained-parameter reauthentication. Vendor restoration remains SCP03-qualified, not a mixed-key guarantee. |
| SD-C003 | Delete contract and rotation guidance explain omitted-zero filters, multiple matches, SCP03 KVN-only normalization, nonzero version responsibility and last-key consequences. Warn against SCP03/KVN=0 rather than endorse the empty-filter edge. Lifecycle comments no longer claim last-set deletion leaves no SCP03 keys. |
| SD-C004 | Rotation flow distinguishes first import replacing factory defaults, adding custom versions and explicit replacement. Retains escrow/zeroing and requires fresh authentication before deleting retained old sets. Public SCP03 contract describes AES-128/vendor versions without inventing validation. |
| SD-C005 | README roles separate device key, trusted public key, CA-KLOC public reference, CA identifier, OCE private key/chain and device certificate store. Qualify SCP11b/IsAuthenticated, device authority and certificate loading versus trust. Corresponding API and test comments agree. Migration subsection mentions SCP11 and host authority. |
| SD-C006 | Added record/component/feature/interface purpose and result shapes; factories explain connection/parameter responsibility and creation-time firmware fallback. All 16 concrete operations use inheritdoc from the explicit local interface contracts. Exception class/factories/migration/map describe the selected configuration catch, earlier selection/preparation, cancellation and immediate-only status extraction. |
| SD-C007 | Allowlist XML/README describe FormatException, caller byte sequence/order, empty collection versus empty entry, CA-direct certificate scope, clear lifting restrictions and deletion to stop trusting a CA. Identify the August 2023 v1.3.0.13 Public Review draft explicitly. State missing SCP11c counter/profile limitation without a guessed recipe or final-GP conformance claim. Narrowed old final-version test citation to the actual wire assertion. |
| SD-C008 | GET/STORE/recognition contracts describe selected-SD scope, caller payloads, emitted header and nested tag-73 value. Removed unverified blanket final-GP/ISO conformance citations and false local authorization exceptions. |
| SD-C009 | Test comments now follow assertions: generated-key listing is not authentication; convenience listing does not observe disposal; broad exceptions do not prove failure cause; SCP03 fixture/CMAC vectors are not protocol conformance. Corrected firmware/public test-key descriptions, reset cleanup rationale and certificate name-ordering versus signature verification. Removed source development-history/audit chatter while preserving genuine behavioral explanations. |
| SD-C010 | Key/CA result contracts qualify partial parsing and lack of complete raw response; no parser change. |
| SD-C011 | Reset/factory/README/contributor guidance documents omitted original firmware/configuration and fresh-session workaround as a current limitation, not intended permanent policy. Effective inherited Core state remains unresolved. |
| SD-C012 | GET identifier and replacement-version contracts distinguish caller ranges from current truncation; generation does not claim local KID validation; SCP03 response checking is limited to the KCVs. No runtime enforcement added. |
| SD-C013 | Retain caller-owned scopes and supported certificate ownership; private-import comment no longer prescribes an unverified Clear API. Contributor helper explanation is limited to its cleared plaintext copy, not universal temporary/fixture cleanup. No new deep ownership/cleanup guarantees. |

### Evidence correction and review implications

The investigation's proposed testing-link correction was wrong: from `src/SecurityDomain/tests/`,
`../../../docs/TESTING.md` resolves to the repository's docs directory; four parent traversals do not.
The first read-only path check failed on the proposed four-level link. Restored the original three-level
link and reran successfully. Do not copy the incorrect proposed path from investigation.md into future work.

The package synopsis was synchronized with reset qualifications even though its original inventory
disposition was clean. Clean units SD-CL02 (StatusWord), SD-CL03 (certificate contract surroundings) and
SD-CL05 (package synopsis) overlap edited passages; coordinator should reconcile their sampling status.
The important original certificate ownership/failure cleanup statements remain. Other clean units were
not silently claimed reviewed or sampled by this editor.

## Current examples and typed compilation prerequisites

There are **10 current C# fenced blocks** across the five module Markdown files. No API XML code blocks
were added. Compile full task/resource scopes, not individual lines with invented dynamic placeholders.
No example has been compiled or executed by this editor.

| Path / block | Classification and complete context to validate |
| --- | --- |
| `src/SecurityDomain/README.md` — Getting started | Complete read-only top-level task. `net10.0`, C# 14, standard implicit usings, SecurityDomain project/package reference (Core transitive), all displayed imports. Device discovery yields `IYubiKey device`; await-using encloses enumeration. Requires a SmartCard YubiKey only if executed; do not execute during compilation validation. |
| Same — Authenticate with default SCP03 | Composable task fragment. Reuse displayed imports and a typed `IYubiKey device`, but do not retain Getting started's live session or duplicate its session declaration in one scope. Owned `Scp03KeyParameters.Default` scope encloses the session. Runtime prerequisite is an authorized device with its factory SCP03 set still installed. |
| Same — Rotate SCP03 | Composable administrative fragment. Include the preceding authentication task's complete parameter/session scopes and imports; `SecurityDomainSession session` is authenticated with accepted administrative keys. Retain the entire random 48-byte allocation, StaticKeys using scope and finally-zeroing. Escrow is an application responsibility, not an implemented persistence helper. This fragment imports only; fresh-authentication/retained-key deletion is subsequent prose, not an executable unbound delete example. |
| Same — Set up SCP11b | Composable administrative fragment. Same complete SCP03 task scope, firmware-capable device if executed. Produces typed `KeyReference keyReference` and `ECPublicKey devicePublicKey`; no secret export. Do not combine with rotation in one running program by default. |
| Same — inline `new Scp11KeyParameters(keyReference, devicePublicKey)` | Meaningful constructor-expression fragment, outside fences. Inputs are the typed outputs of generation retained through a trusted provisioning path. Validate in a fresh session task with an owned parameter scope enclosing session disposal; the original administrative session must already be disposed. This does not authorize management or establish certificate trust. |
| Same — Factory reset | Separate composable task: displayed imports and typed `IYubiKey device`, no live earlier session; await-using unauthenticated session encloses ResetAsync. Destructive if executed. Do not splice into the default-auth block with the same session variable. |
| `src/SecurityDomain/CLAUDE.md` — command constants | Reference sketch / source-navigation fragment, not a consumer task. Byte/int constant declarations reflect local instruction/tag values, not a complete command encoder or conformance test. Can compile as declarations if useful, but unused-constant warnings are not consumer-task defects. |
| Same — Key Information Inspection | Composable async statement fragment with `ISecurityDomainSession session`, `Microsoft.Extensions.Logging.ILogger logger`, `CancellationToken cancellationToken`; import SecurityDomain and Microsoft.Extensions.Logging. Session supplied by enclosing scope. Logging reference metadata uses no key bytes. Needs logging abstractions/reference context, not a guessed loggerFactory session parameter. |
| `src/SecurityDomain/tests/README.md` — Basic Pattern | Class-member test-context fragment in the existing xUnit-v2 integration project/reference/analyzer context. Displayed prerequisite namespaces include Xunit, Tests.Shared, Tests.Shared.Infrastructure, SD IntegrationTests.TestExtensions, Core.Protocols.SmartCard.Scp, Core.Devices; standard System/threading/tasks. Typed `YubiKeyTestState state` is the attribute-supplied argument. Own timeout and default parameters through awaited helper. Do not wrap in an xUnit-v3 host or execute discovery/hardware. |
| `src/SecurityDomain/tests/CLAUDE.md` — Standard Integration Test | Same xUnit-v2 class-member context and imports. SmartCard/5.4.3 attribute, scoped CancellationTokenSource/Scp03KeyParameters, callback contains its own returned-list assertion. No external unbound token. |
| Same — Multi-Session Test (Key Generation) | Same xUnit-v2 class-member context/imports, SmartCard/5.7.2 attribute. Complete method includes typed KeyReference, default parameters, first authenticated generation, second unauthenticated listing, finally and independent finite cleanup token. Preserve all those scopes in compilation. Assertions check registration, not SCP11 authentication. |

Other fences: module/package README installation shell commands; test README toolchain shell commands and
illustrative appsettings JSON; module CLAUDE text flow; AI-DOCS-GUIDE's Markdown teaching excerpt. These
are commands/configuration/reference examples, not current C# task hosts, and were not executed. The
shared migration subsection and YAML scalar contain no C# example blocks. Replaced obsolete implementation
and untyped SCP parameter examples are no longer validation targets; no historical before/after C# block
was added. Existing executable tests remain tests, not newly authored documentation hosts.

## Read-only checks and limits

Ran Python 3 through `functions.shell` to enumerate `git diff --name-only` under the exact owned product
scope, compare each changed C# file against `git show HEAD:<path>` using a comment/whitespace-excluding
lexer that retains string/character literals, check LF preservation, inventory C# fences, and resolve
relative Markdown link paths with `Path.exists()`. It does not open linked Core/other-module contents.

- Final output: **26 product paths; 18 non-comment C# comparisons equal; 10 module C# fences; 19 relative
  link paths; zero missing paths**. Exit 0 after correcting the investigation's mistaken testing link.
- All changed product files retain LF. Patch adds terminal newlines to edited C# files that lacked one
  at baseline; this is whitespace only. No import-order changes were made.
- `git diff --name-only` for module PublicAPI snapshots, csproj and runner JSON returned **none**.
- This lexical preservation check is not a Roslyn token/directive gate, XML documentation build, formatter,
  anchor/remote-link validation, test run, snippet compilation, or proof of intended behavior. Those remain
  coordinator gates. Independent review and meaningful example compilation have not happened yet.
- SD-IMP-001–008 and SD-DEP-001–005 remain deferred exactly as triaged. No Core boundary review, final GP
  document acquisition, SCP11c recipe, live hardware result, broad security audit or all-platform claim.
- The pre-existing unrelated YAML parsing defect DOC-002 remains untouched. Only the `securitydomain-exception`
  notes scalar changed in that shared map; do not treat this edit as whole-file YAML validation.

## Exact changed authored paths and SHA-256

The 26 product paths below are the full content edit list. The only record created by this editor is
`docs/research/documentation-audit/modules/security-domain/pass-1/security-domain-001/edit-report.md`.
This report does not include a self-referential hash; coordinator can hash it in the final run manifest.
Hashes describe the editor handoff before any coordinator formatting or review corrections.

| Path | SHA-256 |
| --- | --- |
| `docs/AI-DOCS-GUIDE.md` | `54ff0bf0a5c57143e73094f0b7f2bd67d4175e358c4ae2e554bdaa05616b9a93` |
| `docs/migration/v1-to-v2-map.yml` | `eea28b28c26fbcff358b04214e7fb660cc32226c5e5e7fa2d97bd5fbc9729c41` |
| `docs/migration/v1-to-v2.md` | `606f8822d3dc0f6360a709c7adf33acb164dffe1d559acaf8ce2c95971dd2588` |
| `src/SecurityDomain/CLAUDE.md` | `6d5e5c55f89c2c0dfa53200afe4f2d35732c09994cfe94b1ea1db4e924a14a12` |
| `src/SecurityDomain/PACKAGE_README.md` | `854d204ed4b648c436209aba6b87f055a0133b92a458a0b5f597bb8375386b9b` |
| `src/SecurityDomain/README.md` | `0a290632f4eb8db0eb6e051e69ad555e567009f8df4b888d93cefd922f9508f5` |
| `src/SecurityDomain/src/CaIdentifier.cs` | `6b3ce2f017aa678dffa796366b2850ecf56c53fd974848ce818651e3cec2ecf8` |
| `src/SecurityDomain/src/ISecurityDomainSession.cs` | `7862421d402c5bbeaefcaa523b5f65aa62968f69545cb5b716c4498a06009861` |
| `src/SecurityDomain/src/IYubiKeyExtensions.cs` | `40aeb7d45b6717b92a83caa7306ad6c204674b190ee12605e9413826de58183c` |
| `src/SecurityDomain/src/KeyInfo.cs` | `10d445ef6df239e4518a700f968208c8ea16c5af64a3a5eab31e744e4ed7a1e2` |
| `src/SecurityDomain/src/SecureChannelException.cs` | `7227829b83651cf905969fd52fdbd14d01f7301c27281ff249769407616b2f82` |
| `src/SecurityDomain/src/SecurityDomainSession.cs` | `5a67ccf399c1bd60c53485fba0976eb5dc492bb110fad2415f44463d4ab3f2e0` |
| `src/SecurityDomain/tests/CLAUDE.md` | `fa5db89cf8e780481a88245a5d4f52d716e13cb6f63b9fd2766de1bb788530c5` |
| `src/SecurityDomain/tests/README.md` | `658b60cae8657c4e0fe2d44fe5cbc7c174b606fde246b97bb4ad0c2ecd796394` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Helpers/ScpCertificates.cs` | `60fed66b3ad1d08e1a6bef9a70ae217c5d29960a2ccb755f9e6657e1d62a025f` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_NegativeTests.cs` | `fc3459f453a3be25d129a222d31e29ba879f8c62dd214df6115b5abacc9a88aa` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03KeyLifecycleTests.cs` | `62b28bb2b347f795da553fbc8ca3234c30b7df2558191f18aa7ea503d129da3a` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03Tests.cs` | `8295999e0895306b014de9e3f6d28513d7e61a53fe9d46b23d765b8dc357dd7e` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp11Tests.cs` | `f4b5cf41174b82a25d6b1c8e3a23a00cd02cd3062fb4a196506d6ebbb3a1d285` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/SecurityDomainStateRestoringTests.cs` | `1ca525a8e82b2b690d41952b15d69cb67d2fc98b4e88e407fa433d97766f4bcc` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs` | `0af1b49de21ffee8cb937379b98869bd20404a0a99798e880d135a0a7d360778` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/AuditV2/SecurityDomainAuditReproTests.cs` | `779c5caa812bb132918302fa016fd5fb8264483e312da3e8048ef24c9e188207` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs` | `28facb769793997f44eefa40418436e4b58dc085ad803cd09cd0137dd06539c2` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecureChannelExceptionTests.cs` | `f8edd5cb5a3f1240aef6a65b5257f1aa285d12db2f3d4212b3b4c5700bc45f04` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainCertificateBundleTests.cs` | `eb150df5f8103ddf02a1ca8cc6305a329a8a10f3bb8e576941bbb1d911c70f09` |
| `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs` | `493adcd9ce5bcd8f30ec01512d61141d6542401d65402beec660959eded03969` |
