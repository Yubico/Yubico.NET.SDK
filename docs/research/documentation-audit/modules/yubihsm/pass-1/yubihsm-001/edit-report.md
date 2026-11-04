# YubiHSM Pass 1 — editorial handoff

Editor: GPT-6.1 Sol, 2026-11-04. Baseline `a74792facb657413e98223e059876d8b213ffff3`.
Status: **edited; independent review and coordinator validation pending**. This report identifies the
editor-owned product diff at the hashes below; it does not close implementation or hardware findings.

## Scope and safeguards

Read root/module/test instructions, campaign workflow/style, SDK house style, testing guidance, RUN plan,
findings, investigation, coverage and original-source excerpts. Loaded `docs-module` before module guide
edits. Read affected local implementations and assertion bodies. Other-module/Core source and docs stayed
outside scope. Shared-file reads/edits were restricted to assigned YubiHSM sections and enclosing context.

Edited 35 product paths: 26 C# comment-only paths, four module Markdown paths and five shared Markdown/YAML
paths. `docs/v2-highlights.md` remains unchanged: its YubiHSM package/presence references and Native AOT
qualification already agree with the scoped evidence. Root README and architecture/NativeAOT files remain
unchanged. Existing coordinator/user work and prior campaign records were preserved. The coordinator owns
status, findings, hardware execution, validation and publication; this editor owns this report and the
listed product changes only.

No builds, tests, formatter, hardware commands, agents, commits, import edits, runtime-string changes,
assertion edits, project/configuration edits or API-snapshot edits were performed. A lightweight Python
lexical comparison of all 26 edited C# paths against HEAD removed comments while preserving quoted text
and compared remaining non-whitespace tokens: all matched. This is a preliminary safeguard; the
coordinator's Roslyn token/directive/API checks remain authoritative. Compilation and analyzers have
not yet checked the new Markdown bodies.

## Finding-to-change map

| Finding | Applied changes |
| --- | --- |
| HSM-C001 | `HsmAuthCredential`, retry exception, README/CLAUDE and migration retry notes distinguish credential deletion from management blocking; source-qualified successful counter restoration and snapshot refresh replace unsupported success speculation. |
| HSM-C002 | Interface GET_CHALLENGE contract, retry exception, guide, retry-test summary and migration material scope typed retry translation to actual helper users. GET_CHALLENGE retains general APDU errors; application owns retry decisions. |
| HSM-C003 | README enrollment procedure and typed callback provision matching HSM objects before clearing generated keys. Three secret roles, label/object mapping, asymmetric PK-OCE enrollment and recoverable rotation storage are explicit. Matching XML and sample comments name external responsibilities. |
| HSM-C004 | Typed disposable HSM exchange sketch replaces undefined connector helper. Exact context ordering, static HSM identity versus credential PK-OCE/ephemeral EPK-OCE, actual HSM response, optional symmetric cryptogram and host handshake completion are explained. |
| HSM-C005 | Feature-field XML, SELECT/fallback/override/reset policy, backend comment, feature table and development-version qualifications align with source. Unsupported production-key complexity and alpha instruction assumptions were removed from test comments. |
| HSM-C006 | Borrowed input/owned array boundaries, SessionKeys.Parse copies, existing spans, disposed access and separate source/copy clearing are explicit. PBKDF2 copy wording is scoped to the helper. Factory and extension docs distinguish borrowed versus owned connection lifetimes. |
| HSM-C007 | Guide/shared presence paragraph describe conditional LIST lookup, cancellation propagation, advisory fallback, caller-owned prompt and prompt/non-reentrant callbacks. The guide links to the existing shared cancellation contract and tells callers to await the terminal operation result/drain and cleanup before recovery, without claiming native interruption or promising OperationCanceledException. |
| HSM-C008 | LIST/algorithm/credential XML and guide explain snapshots, preserved unknown identifiers, nullable touch, extension-property exceptions and label ordering versus all-property record equality. Parser behavior remains unchanged. |
| HSM-C009 | Sample entry point, guide and operation comments identify diagnostic key output, visible/argument string lifetime, pre-finally cleanup gaps, symmetric-only calculation dispatch and absent configured presence prompt. Runtime prompts/help/output/selection are preserved. |
| HSM-C010 | Test comments track actual assertions: synthetic-context length checks, nonempty challenge, multiple-invalid-input asymmetric rejection with exact exception type, touch metadata only, selected byte-level assertions, parameter recomputation and reset-before/resource-disposal/persistent-state distinction. |
| HSM-C011 | Module CLAUDE describes actual ownership/backend/gates and test coverage; short undefined-helper anti-pattern blocks were replaced by explanations preserving their failure rationale. Path validation rejected the investigator's proposed four-parent testing-guide link; the original three-parent target resolves correctly and is retained. Historical issue identifiers were removed from active test comments. |
| HSM-C012 | Assigned migration/interaction/comparison sections describe applet scope and external HSM tasks. Current clarification in the hybrid historical gap section distinguishes implemented SDK capability from device qualification and corrects reset version policy. Unrelated map parse debt is untouched. |
| HSM-IMP-008 (late evidence) | Symmetric interface/README/SessionKeys/CLAUDE/tool comments distinguish the strict 48-byte key parser from complete applet responses, the observed supplied-cryptogram 56-byte rejection and known vendor host-code handling since 2023. No suffix meaning, firmware introduction boundary or cryptogram-omission authentication workaround is asserted. |

## Sources and evidence limits

Re-fetched S1 (YubiKey Technical Manual, “YubiHSM Auth Specifics”) and S2 (v1 .NET user manual,
“YubiHSM Auth overview”) from their original URLs through Exa on 2026-11-04. Confirmed S1's identical-key
enrollment, separate HSM ID/label/password actors, host/HSM challenge flow and later host-response step;
confirmed S2's explicit successful retry restoration, credential deletion and management blocking passages.
S2's older immutable-credential statement is not applied to newer password-change support. These are
descriptive vendor guides, not versioned normative applet specifications. The existing pinned S4/S5
encoder/handshake evidence remains corroborating, with GAP-SPEC-1 unresolved. Context7 `/dotnet/docs`
confirmed current one-shot PBKDF2 guidance; storage clearing claims stay limited to supplied/owned buffers.

Initial hardware checkpoint: read coordinator raw log
`validation/hardware-v1-kat-20261104-125119-840-test/01-test-AuditHsm001.Hardware.IntegrationTests.log`.
It records serial 103, metadata qualifier `5.8.0.beta.0`, SmartCard session effective version `0.0.1`,
three independently matched keys with cryptogram omitted, one typed wrong-password `0x63C7`/seven retries,
then `SessionKeys.Parse` rejection of 56 bytes during supplied-cryptogram calculation. The failed body
did not reach retry-restoration assertions. Reset/reopen cleanup observed empty LIST and eight management
retries. Coordinator reports ykman applet info 5.8.0 separately. These distinguish metadata, effective SDK
policy and the actual observed result; they establish neither suffix meaning nor other firmware behavior.
Further original-vendor/measurement adjudication remains coordinator-owned and can reopen the draft.

### Follow-up incorporated after the initial handoff

Read `cryptogram-investigation.md` and `evidence/cryptogram-sources.md` in full. Vendor Python's July 2023
parser explicitly handled 56-byte input under a field named `card_crypto`; its later/current parser takes
three key slices and ignores the suffix. Vendor C deliberately sends tag78 only for cryptograms longer
than eight bytes and requires exactly 48 response bytes. Its later “Ignore host crypto for now” comment
neither bypasses that strict guard nor identifies this device's actual suffix. The product caveat now
describes a known response-format compatibility pattern, rather than suggesting a recent beta-only quirk.
Only the tested development device has hardware evidence here; suffix meaning and firmware range remain
unqualified. README links the original initial Python commit and pinned current Python/C implementations.
The standalone Parse constant/parameter describes its accepted key region rather than a universal complete
wire response. Secure real-HSM verification/handshake responsibilities remain explicit.

Coordinator follow-up message reports an omission-only independent KDF/retry sequence 8→7→8 passing and
user/admin password changes followed by all-three-key checks passing. These are separate restricted-scope
results, without promoting the failed supplied-cryptogram probe or authenticating an actual HSM. Coordinator
also reports cancellation requested at one second, terminal APDU 0x6982/Failed after roughly 14.3 seconds,
failed preplanned OperationCanceledException/0x6985 expectations, unreached recovery assertion and successful
finally reset. A measured-outcome/fresh-session recovery probe remains underway. Those updates were supplied
by the coordinator; this editor has not independently read their later raw logs. Current public wording
therefore advises awaiting terminal operation drain and cleanup and following the actual result for recovery,
without publishing test timings, guaranteed status words, recovery success or interruption claims. Effective
SDK version 0.0.1 and ykman's displayed 5.8.0 remain separate observations.

**Investigation correction:** `src/YubiHsm/tests/CLAUDE.md` is three directories below the repository
root. Its original `../../../docs/TESTING.md` exists and resolves correctly; the suggested
`../../../../docs/TESTING.md` leaves the repository and failed the editor's target-existence check.
The final guide retains the original target. The remaining C011 guide/test-infrastructure corrections
stand independently of that rejected link finding.

## Complete snippet and example inventory

### Current module Markdown

All six C# fences are in `src/YubiHsm/README.md`. The listed hashes cover fence bodies, including their
terminal newline. Compile full bodies together with actual SDK references and analyzers; use the stated
wrapper rather than extracting only calls. No execution is implied.

| Fence / initial line | Classification and prerequisites | Full-body SHA-256 |
| --- | --- | --- |
| README #2 / 51 | Complete listing example; .NET 10 SDK references and an enabled SmartCard applet. Keep discovery/session/list loop and disposal together. | `d9a185c971a74c028c30d3cc1a00690ceac728061d3df90639c365183c3427ca` |
| README #3 / 93 | Application-dependent enrollment method; directives from #2, selected IYubiKey, borrowed management/password buffers, typed HSM provisioning callback. Compile as a class member, retaining finally. Application callback provisions HSM object, mapping/recovery before return; caller clears borrowed inputs in its enclosing lifetime. | `263e9cc392690567965a610b78ff289a458b2c6636a55ca86aadce7b285ea966` |
| README #4 / 161 | Application-dependent handshake group; compile the entire ISymmetricHsmExchange declaration and UseSymmetricHsmAsync body as members of a wrapper class, with #2 directives. Adapter factory owns partial-construction cleanup; adapter owns HSM lifetime/copies; method owns returned challenge/cryptogram arrays, context and keys. Preserve both interface and full method. | `46dc3d75c653f2771c2e6d0178730837745a36cb2d4a4b4435483acb27bc0f4d` |
| README #5 / 228 | Application-dependent rotation method; class member with #2 directives, borrowed current key and durable typed recovery-storage callback. Storage succeeds before PUT; finally clears replacement. | `9506ec8f0799d0158cc43f44bd71346633c9d3137929142c351253c00e8c4ea5` |
| README #6 / 254 | Composable TouchPrompt declaration; IUserPresencePrompt and its context from #2 directives. Keep class body. | `ca8fe727d322c778aabf304fd3260fb21de2c9619067f783b11892536be2b6ae` |
| README #7 / 262 | Composable options/session fragment; typed selected IYubiKey device and TouchPrompt from #6. Preserve await-using scope in async wrapper; inspect alongside presence explanation. | `5f0eed8fc3440b3370f6a7f6b8404655799311da3fa836f044e7a34e7df35fe5` |

README #1 is the two-command package installation shell fragment; README #8 runs HsmAuthTool from the
repository root. PACKAGE_README #1 repeats the installation shell fragment. Module/test CLAUDE now contain
no fenced code; their former undefined-helper password/lifecycle/anti-pattern fragments were replaced by
present-tense explanations and the README's typed groups. Thus this edit introduces no remaining intentional
bad C# examples or excluded pseudo-helper bodies. Any historical bad-example fixture retained by validation
should be classified as historical/intentional rather than silently trimmed. No C# snippets were added to
the five edited shared sections; their unrelated snippets are outside this editor's ownership.

### Executable sample

All 22 existing sample C# files remain executable source with preserved bodies/imports/strings. Compile the
entire actual HsmAuthTool project, including its genuine references/analyzers, rather than wrapping isolated
operation calls. A successful compile alone establishes no safe output policy, total cleanup or HSM handshake.

- Entry: `examples/HsmAuthTool/Program.cs`.
- Commands: `Cli/Commands/AccessCommand.cs`, `CommandArgs.cs`, `CredentialsCommand.cs`, `InfoCommand.cs`,
  `ResetCommand.cs`.
- Menus: `Cli/Menus/CredentialMenu.cs`, `ManagementKeyMenu.cs`, `ResetMenu.cs`, `SessionKeyMenu.cs`.
- Infrastructure: `Cli/Output/OutputHelpers.cs`, `Cli/Prompts/DeviceSelector.cs`, `HsmAuthDeviceSelector.cs`.
- Operations: `HsmAuthExamples/AddDerivedCredential.cs`, `AddSymmetricCredential.cs`, `CalculateSessionKeys.cs`,
  `ChangeManagementKey.cs`, `DeleteCredential.cs`, `GenerateAsymmetricCredential.cs`, `GetManagementKeyRetries.cs`,
  `ListCredentials.cs`, `ResetHsmAuth.cs`.

Paths in this list are relative to `src/YubiHsm/`. The sample calculation is symmetric-only and prints keys;
enrollment covers the applet side, with external HSM provisioning required. Test/helper source remains test
source rather than an application exemplar; assertion edits await Pass 2.

## Clean-unit overlap reconciliation

Remove **U01** (LIST contract), **U04** (management-key rotation contract) and **U07** (public-key response
contract) from the pre-edit clean denominator because their prose changed. The other 17 nominations have
no editorial overlap: U02–03, U05–06, U08–20. In particular U08's automatic one-shot LIST member is unchanged
despite another member's comment edit in the same file. U15–16 helper units and U19–20 test units remain
unchanged. No sample nomination U09–14 was edited. Coordinator must independently verify these boundaries,
sample at least three remaining units across API/example/internal-test and expand a category upon a material
miss. This is eligibility reconciliation, not a sampling verdict.

## Reopening conditions

- HSM-IMP-001: GET_CHALLENGE retry/status policy changes → interface, retry exception, module guides,
  migration map/guide/gaps/comparison and retry-test summary.
- HSM-IMP-002: key/input/copy ownership, sample allocation or cleanup changes → SessionKeys XML, README
  sketch lifetimes, module guidance and affected sample comments.
- HSM-IMP-003: LIST/raw metadata/UTF-8 strictness or algorithm policy changes → LIST/algorithm/credential
  XML, sorting/equality/metadata guide and policy fallback explanations.
- HSM-IMP-004/005: symmetric dispatch, prompt configuration or safe input/output product policy changes
  → diagnostic overview, calculation/menu/command comments and sample guide.
- HSM-IMP-006: assertion, dummy input, default cleanup or test helper lifetime changes → full affected
  test commentary, test CLAUDE/module test inventory and qualification language.
- HSM-IMP-007: request/response cryptographic shape/point/cryptogram validation changes → exact XML
  exceptions/returns, typed HSM sketch prerequisites and asymmetric enrollment/handshake explanation.
- **HSM-IMP-008**: bounded vendor or raw measurement establishes 56-byte response layout, supported applet
  combinations, cryptogram semantics or a parser fix → symmetric interface result/exception remarks,
  SessionKeys class/Parse contract, README handshake caveat and full supplied-cryptogram sketch validation.
  Reconcile presence-success parsing and all claims of authenticated HSM use. Preserve failed probe and
  pending retry-restoration scope; never infer interoperability from the no-cryptogram KAT alone.
- HSM-DEP-001: Core effective/development/override semantics resolved → version/gate/reset descriptions
  and development-device qualifications. HSM-DEP-002: ownership/native cancellation evidence changes →
  factory, README/shared interaction and touch-test explanations. HSM-DEP-003: copied transport/TLV storage
  ownership resolved → qualified buffer-clearing assurances. These are recorded dependencies, without Core review.
- GAP-SPEC-1: versioned normative original or vendor clarification obtained → recheck the exact actor,
  clause strength/version and operation applicability before replacing descriptive source qualifications.

## Product path hashes at editorial handoff

The report itself is excluded from this table; coordinator records its acceptance hash after review.
Any subsequent content correction requires fresh affected hashes and snippet reconciliation.

```text
c95f5b2659b37bfaa5a4ff7e70ea0a87d983aac15675d5a41e8ad476097a3dd9 docs/migration/v1-to-v2-gaps.md
9d39813ce3424d633c6ae52c33a30e6b353fa8ec77c4d2bfbc74c09e390dc77c docs/migration/v1-to-v2-map.yml
a6947200550db0711a05c24b94f6859c2d083df75fbce06f25a1077ab41f2b78 docs/migration/v1-to-v2.md
11992cbcd4a23b2bc339d920f82c0ef659aefaffcfbcf24888da3d229fff22c0 docs/usage/user-interaction.md
a77eedd2b55ac4585194c24a25e20702d572e9dc07575ece055df770ee8693d3 docs/v1-to-v2-comparison.md
4208d45ec534c31d80024fa09e60a717bbee272c5d1bd71bea8661c0f4b1b68d src/YubiHsm/CLAUDE.md
541a09f386cf4040d2062a18634c50c8c9a4559f2f58536bac2f806b9b89cdb4 src/YubiHsm/PACKAGE_README.md
50d40f99913862a997f51394feddfc07236ad639df2a456f9103cd57ee3ff1b1 src/YubiHsm/README.md
c5730f84f59f2cb848babd7847c83560fedbea47c9e495d23bb9df0f128471fe src/YubiHsm/examples/HsmAuthTool/Cli/Commands/AccessCommand.cs
da561a1838312ec57f803941c78db8036686f165d96d394b0f5953c6b1565a16 src/YubiHsm/examples/HsmAuthTool/Cli/Commands/CommandArgs.cs
334cc747a83caa5a261f8d658ea79557a13f50f62538a6755d89213b7b85b63b src/YubiHsm/examples/HsmAuthTool/Cli/Commands/CredentialsCommand.cs
d8b0905ac49fb945cea844a260853f9aea8d55fada6c414ca2018c25ff8e6bb3 src/YubiHsm/examples/HsmAuthTool/Cli/Menus/SessionKeyMenu.cs
7749db37bffd28e2407c7f716a43e0cfaefb5ddb920d41658be184dd8bfb4413 src/YubiHsm/examples/HsmAuthTool/Cli/Prompts/DeviceSelector.cs
ce43fb6ed4d475c76e037b16c416e342a743957109bc415f8d9cef78e6eb1c32 src/YubiHsm/examples/HsmAuthTool/HsmAuthExamples/AddDerivedCredential.cs
3468e77a58f59a8b8e0c22cdc37a64c9ef05fda75dbc5b17e9f4e82512a941d8 src/YubiHsm/examples/HsmAuthTool/HsmAuthExamples/AddSymmetricCredential.cs
33d6b9d76eb7e1505b15959ab0f95dbd383fee5623e51664a0af9b9cecb568a9 src/YubiHsm/examples/HsmAuthTool/HsmAuthExamples/CalculateSessionKeys.cs
0e2a356ca3c26c903351d6b7f24961c31f6fba69a8c555e7bd5542473c1dee5e src/YubiHsm/examples/HsmAuthTool/HsmAuthExamples/ChangeManagementKey.cs
9113eb7309913fab613375215bbb8910f52feb2659d8b6898a45f8693f0e29aa src/YubiHsm/examples/HsmAuthTool/HsmAuthExamples/GenerateAsymmetricCredential.cs
c3f123c7d7f752bf2a608a967e8e191cc6b33f6ee9c1d63ee8c38ce634f4b382 src/YubiHsm/examples/HsmAuthTool/Program.cs
3d3d276a6c524e40565af5cd4ac8cd88fd8b60b9eafd14ac5b4b08d371057b6e src/YubiHsm/src/Backend/HsmAuthBackend.cs
a4069db87262908b57d3c61cdac00e2b8e9e041e0f056ef76d010f753a174ab6 src/YubiHsm/src/HsmAuthAlgorithm.cs
7e98e102e58f49c3635ae808cd86bcc8ffed3e2bf257e716c39357578c5a793e src/YubiHsm/src/HsmAuthCredential.cs
cf6a40f241abab64646bd590ea0d1f6b723b1d0c825000b53bc6be1e463c4372 src/YubiHsm/src/HsmAuthRetryException.cs
683e2b71f220bd783ab3bfea78ec57383e095f4275c298c84b541e08ca1b1bf4 src/YubiHsm/src/HsmAuthSession.cs
1d29d612f9737fdadcef209d727d7dc3ce7528e3d30a3af81c8d61a0b60ace7d src/YubiHsm/src/IHsmAuthSession.cs
1f776f28c68a665fd62a7b72ea1ea3febf795f35613f97f27004020d46130cae src/YubiHsm/src/IYubiKeyExtensions.cs
63f45c4a8640be5ac857993433744feca6f08b0962defc62dc6ff8d088e76bd6 src/YubiHsm/src/SessionKeys.cs
c65163bce092d689e9d88a08839a99a40fb3d9c08220df10bc61b55358f5b1c9 src/YubiHsm/tests/CLAUDE.md
e51edb16dde78133f385c9d873ea979700af349276b3854b759767fcb50aaf92 src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/HsmAuthAsymmetricTests.cs
b6ebdbfa680fb584362406f54233e765dc67f7c60111cc9dae3632c0e11dae72 src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/HsmAuthSessionTests.cs
30a1b138942b0c7d2424752b9f7efbd93f0fbae13d9fec54fd6f6c080754aa25 src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/TestExtensions/HsmAuthTestStateExtensions.cs
bc040900d1e337b72bfdaf3ebe16e8386393e86c8f833e329cc53dc22fd56e0b src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/CredentialPasswordTests.cs
e31ef994f355f0b2d6bd8c22510b00a3beb6df8f494d725948b93e6b17bb4e2a src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthRetryExceptionTests.cs
ff05e66130cf172ee174b3c55d41545b0432dd6297f145184eed386af1a31b0e src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthSessionByteLevelTests.cs
7da09206e1e3fa80b471c5fad97ab74c01420a0a325a1e2349a74b7bf871c66f src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/Pbkdf2DerivationTests.cs
```
