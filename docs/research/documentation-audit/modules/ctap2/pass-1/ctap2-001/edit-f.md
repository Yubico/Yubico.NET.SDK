# Editor F — guides, FidoTool comments and narrow shared references

## Revision, authority and status

Fresh editor worktree: `/tmp/opencode/ctap2-001-edit-f`; unchanged HEAD/baseline
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. Editing began 2026-11-03; handoff 2026-11-04.
This report is written directly to the PRIMARY packet. Source edits remain only in the isolated editor
worktree: **26 F-owned module paths and four narrow active external paths changed**. Seven F support
paths remain unchanged. No integration, build, test, hardware operation, commit or delegation occurred.
Status: **edited, awaiting fresh module/shared/boundary reviews and coordinator validation**, not closeout.

**2026-11-04 steering follow-up:** [correction-f.md](correction-f.md) records verified COORD-001/A-REV-001
GetInfo encrypted-field absence and RC-R1 ARKG-10 partial agreement. Only README Getting started and
CLAUDE GetInfo/profile prose changed in this follow-up. All 39 fences/five XML dependencies and their
stable ordinal classifications remain unchanged; no snippet extraction input changed within a fence.
The seed placeholder -65700 agrees with the draft, rather than being a profile-difference reason.
Final corrected guide SHA256 values and independent original/source evidence are in that follow-up.

**Fresh-review follow-up:** [correction-f-review.md](correction-f-review.md) records RF-R1–R4 and accepted
RF-O1–O3. Seven of the existing 30 source/document paths changed narrowly; no new source path. Module
fence bodies/counts remain unchanged. Discoverable-alternative and CLI protocol-2/verify-scope prerequisites
are now explicit. The additional L2 CTAP diagram fence changes the external diagram classification and
requires coordinator regeneration of **both L2 and L3b** images, as recorded below. Ready for F follow-up
review/final snippet extraction, not an independent PASS or compilation result.

Read root CLAUDE, CTAP module/test guidance, WebAuthn module guidance, canonical workflow/approved style,
SDK house style, testing guidance, plan, inventory, triage, investigations A–G, F's snippet/external
manifests, boundary map and framework adjudication. Loaded docs-module for guides, secure-credential-prompt
for PIN-buffer/string distinctions, and verify for record-only preservation checks. Generic skill examples
are not protocol/product authority and were not copied as current CTAP APIs. Context7 resolved .NET and
queried static SHA256.HashData documentation; its returned examples support the static API but did not
establish the requested span-overload details. No compiler or device evidence is inferred from that query.

The accepted mapping is CTAP-DOC-014/018 (F-01–12), with dependent DOC-001–013/015–017 as below. The
original investigations/spec excerpts remain evidence, not edited history. Existing WA IDs remain
authoritative; neither CTAP nor WA engineering is fixed by this batch. Coordinator-owned campaign
handoffs were deliberately not edited despite the broader earlier triage wording.

## Exact path/change mapping (33 F inventory paths)

Each changed C# path changes **XML/comments only**, not executable strings, directives, assertions,
traits, API or runtime tokens. Complete affected FidoTool files and guide blocks were read, not only
keyword hits. Public API support disposition uses the complete investigator F reconciliation plus
mechanical preservation; this editor does not claim a second complete 948-entry semantic API audit.

| Exact repository-relative path | Status and stable edit locator | Finding mapping / important contract |
| --- | --- | --- |
| `src/Ctap2/CLAUDE.md` | Edited: Module Context, Architecture Overview, all Key Patterns, CBOR/escape hatches, test patterns, Feature Flags/Pitfalls | F-02–06/12; DOC-001/003–007/009–017. Current types/tree, complete mock action, useful hardware method, PIN normalization, accurate canonical writer, extension route/status and bounded ownership. |
| `src/Ctap2/PACKAGE_README.md` | Edited: opening support paragraphs, before FIDO identity | F-12; DOC-001/010/016–018. Protocol versus RP verification, advertised/state/transport prerequisites and bio/experimental caveats; alpha/formal-audit warning retained. |
| `src/Ctap2/README.md` | Edited: Requirements, Getting started, Common operations, User interaction, Constraints/Security notes | F-01–06/12; DOC-001–010/013–018. Persistent demo tasks, fresh distinct hashes, exact-byte RP integration, meaningful allow-list/discoverable/next flow, exclusive uv/token fields, scoped prompt alternatives, reset/support/error/ownership boundaries. |
| `src/Ctap2/examples/FidoTool/Cli/Menus/AccessMenu.cs` | Edited: class summary/remarks | F-07/08/10; DOC-004–007/018. Subset not ykman compatibility; synchronous prompts, byte lifetime, retry/default/UV signal and verify-RP gap. |
| `src/Ctap2/examples/FidoTool/Cli/Menus/ConfigMenu.cs` | Edited: class remarks and support-probe comment | F-08/12; D-05/07; DOC-009/018. False can mask failures; current every-operation alwaysUV string is not true scope; code-point/forced-change/toggle caution. |
| `src/Ctap2/examples/FidoTool/Cli/Menus/CredentialMenu.cs` | Edited: class and DisplayAssertionResult remarks | F-01/03/08; G-001–003; DOC-007/008/016/018. Demo versus RP login, first-only assertion, deferred uv/token code, opaque user IDs, optional display fields and exact descriptor-omission revision. |
| `src/Ctap2/examples/FidoTool/Cli/Menus/CredentialsMenu.cs` | Edited: class remarks and returned-RP-hash comment | F-08; D-09–11; DOC-009/018. Hash routing, owned results, estimated capacity, existing handle and no deletion undo/blob cleanup. |
| `src/Ctap2/examples/FidoTool/Cli/Menus/FingerprintsMenu.cs` | Edited: class remarks | F-08/12; D-01–04/15; DOC-010/018. Candid wire/schema/default/status limitations, false provisioned-state misclassification, swallowed failures, synchronous prompts and non-Touch-as-Swipe UI gap. |
| `src/Ctap2/examples/FidoTool/Cli/Menus/InfoMenu.cs` | Edited: DisplayAuthenticatorInfo remarks | F-C06/F-12; A-004; DOC-001/018. Advertised metadata, not executed features/certification; code-point lengths and estimated capacity. |
| `src/Ctap2/examples/FidoTool/Cli/Menus/ResetMenu.cs` | Edited: class remarks and reselection/preflight comments | F-09; DOC-002/018. Second selection not bound to confirmed key, old transport reused, defaults/timing strings not authority, no rollback. |
| `src/Ctap2/examples/FidoTool/Cli/Output/OutputHelpers.cs` | Unchanged: forwarding XML/inheritdoc and hex facade | F-C07. Existing delegation/ReadOnlyMemory facade needs no invented protocol claim. |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs` | Edited: class remarks and SelectDeviceAsync summary | F-08/09; DOC-018. Exposed-interface filter, single/noninteractive auto-selection, synchronous prompts, swallowed info cancellation/Debug, duplicate labels and target continuity. |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoDeviceSelector.cs` | Edited: class remarks | F-08; DOC-018. Inactive alternative, not the selector used by Program/menus. |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoPinHelper.cs` | Edited: class summary/remarks and argument-encoding comments | F-10; C-012/B-04; DOC-006/018. Encoded-byte disposal cannot erase CLI strings/args/history; actual UTF-16 preset and synchronous prompt limits. |
| `src/Ctap2/examples/FidoTool/FidoExamples/BioEnrollment.cs` | Edited: class, EnrollFingerprintAsync and IsSupported XML; continuation comment | F-08/12; D-01–04/09/15/16; DOC-005/009/010/018. Standard wire/schema/status limits, actual retained begin ID, completion-default and thrown-cancellation gap, false support/current-state semantics. |
| `src/Ctap2/examples/FidoTool/FidoExamples/ConfigManagement.cs` | Edited: class, toggle/min-policy/support XML | F-08/12; D-05/07/09/16; DOC-005/006/009/018. Borrowed inputs, fixed V2, state reconciliation, omitted empty lists/false and policy versus disclosure. |
| `src/Ctap2/examples/FidoTool/FidoExamples/CredentialManagement.cs` | Edited: class and metadata/RP/credential/delete/update/support XML | F-08/11; D-09–12/16 and corrected D-08; DOC-005/009/011/018. Correct credMgmt identity, standard-only route, RP-only method, estimates, result ownership, existing handle, writer auto-sorting. No broken-order update warning. |
| `src/Ctap2/examples/FidoTool/FidoExamples/GetAssertion.cs` | Edited: class XML, demo/auth/first-only comments | F-01/03/08; G-003; DOC-007/016/018. Missing exact client-data context/hash handoff, no RP verification, deferred mutually exclusive executable options, first-only, provenance/ownership. |
| `src/Ctap2/examples/FidoTool/FidoExamples/GetAuthenticatorInfo.cs` | Edited: class remarks | F-08/12; A-004; DOC-001/018. Initialization plus fresh query, no PIN setup, metadata not qualification, flattened cancellation/errors. |
| `src/Ctap2/examples/FidoTool/FidoExamples/MakeCredential.cs` | Edited: class remarks and hash/handle/auth comments | F-01/03/08; G-003; DOC-007/016/018. Persistent demo, stable production account handle/exact-byte RP flow, deferred uv/token call, fixed V2 and bounded cleanup. |
| `src/Ctap2/examples/FidoTool/FidoExamples/PinManagement.cs` | Edited: class, VerifyPinAsync and GetUvRetriesAsync remarks | F-08/10; B-03–06; DOC-004–007/018. Borrowed bytes/V2/defaulted retries; GetAssertion permission lacks RP ID and can fail for scope, not wrong PIN. |
| `src/Ctap2/examples/FidoTool/FidoExamples/ResetAuthenticator.cs` | Edited: class/preflight/touch/reset XML | F-09; A-001/010; DOC-002/018. Receipt clock versus gesture, conditional long touch, metadata defaults/failure, qualified product window gap, target/no-rollback boundaries. Executable messages remain. |
| `src/Ctap2/examples/FidoTool/FidoTool.csproj` | Edited: existing CA1852 comment only | F-07/F-C17. Specific existing sealing-analyzer exception; no policy, reference, assembly name or analyzer change. |
| `src/Ctap2/examples/FidoTool/Program.cs` | Edited: verb-routing and reset preflight/reselection comments | F-07–09; DOC-018. Subset/parser/help defects and wrong-target reset risk; routing/help strings/control flow untouched. |
| `src/Ctap2/examples/FidoTool/README.md` | Edited: every affected command/task/setup/support/selection/security section | F-01/07–10/12; DOC-001/002/005–010/014/018. Exact executable/project context, supported subset/set-pin, interactive PIN examples, full hex IDs, reset procedure/risk, actual selector/cancellation/provenance and secret-string limits. |
| `src/Ctap2/src/PublicAPI.Shipped.txt` | Unchanged: nullable directive only | F-C15; no public prose or example. |
| `src/Ctap2/src/PublicAPI.Unshipped.txt` | Unchanged: API analyzer inventory | F-C16: generated projections/removals excluded from invented documentation; effective type/member reconciliation remains investigation F. |
| `src/Ctap2/src/Yubico.YubiKit.Ctap2.csproj` | Edited: Description only | F-12; DOC-001/016/018. Protocol operations/selected APIs, not complete verified passwordless login. All references/API/analyzer metadata unchanged. |
| `src/Ctap2/tests/CLAUDE.md` | Edited: transport/setup, meaningful creation/encoding examples, commands and warnings | F-05/06/12; A/B/D/E test dependencies; DOC-001/007/010/012/017/018. Actual V2/known-PIN setup, no invented timeout/empty test, unit versus integration selection, default normalization and secret-export/cleanup caveats. Existing coordination policy retained. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Yubico.YubiKit.Ctap2.IntegrationTests.csproj` | Unchanged | F-C18: xUnit v2/traditional runner and shared harness references, not execution. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/xunit.runner.json` | Unchanged | F-C19: no assembly/collection parallelism, one thread; configuration not hardware proof. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Yubico.YubiKit.Ctap2.UnitTests.csproj` | Unchanged | F-C20: xUnit v3/MTP, NSubstitute/CBOR and SDK/Core references; actual snippet validation context. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/xunit.runner.json` | Unchanged | F-C21: schema only, no parallelism-disable guarantee. |

## Narrow ACTIVE external edits

| Exact path / stable locator | Changed explanatory block and classification | Mapping / remaining dependency |
| --- | --- | --- |
| `src/WebAuthn/README.md`, Requirements firmware/transport bullets | Prose only; no fence changed. NFC requires current Transport.Nfc and exposed FIDO AID; thresholds are not qualification, advertisement/state/permission checks distinct. | Explicit G-010 authorization; F-12/DOC-001/015/018. Fixed-V2/built-in-UV requirements already truthful and preserved. No other WA file found to require an additional edit in this bounded pass; any new WA edit needs coordinator assignment first. |
| `docs/TESTING.md`, FIDO2/WebAuthn Hardware Coordination, paragraph immediately before integration command fence | Prose qualification only. Existing complete command fences beginning `dotnet toolchain.cs -- test --integration --project Ctap2 --smoke` and human-coordinated UP fence unchanged. | F-06/A-011/DOC-012/018: filtering is not read-only setup. Existing hardware authorization and CTAP human policy neither relaxed nor expanded; no new GetInfo gate. |
| `docs/architecture/sdk-architecture-diagrams.md`, L2 CTAP path box/teaching paragraph and L3b CTAP2 sequence/Teaching notes | Two Mermaid fences changed: L2 CTAP box names `Ctap2SessionRequestEncoding` and typed make/get `Decode`; L3b names `BuildMakeCredentialRequest` and `MakeCredentialResponse.Decode`/result. Both are architectural sketches, not compilable API or complete branch contracts. Teaching notes distinguish the general CtapResponseParser utility from these actual traced credential paths. | F-11/DOC-014/018/RF-R4. Typed internal participants are current CTAP helpers/backend/response, with supported HID/SmartCard paths. **Both L2/L3b images stale until coordinator regeneration**. All unrelated PIV/Core diagram areas and other diagram fences untouched. |
| `docs/migration/v1-to-v2-map.yml`, entry `id: webauthn-credential-prompt`, its `notes` scalar | Narrow active claim correction only: PIN bytes go to ClientPin; credential options carry auth parameter/protocol with uv omitted; notification source/scope qualified. No fences. Historical before/after meaning/evidence/review-commit metadata retained. | F-11/G-003/009/DOC-007/018. Known pre-existing YAML parse issue DOC002 is **not repaired or claimed fixed**. No unrelated scalar/schema/record churn. |

No current campaign handoff, historical migration gaps, original research/run records, source evidence
map or generated image was edited. The extensive external manifest remains a dependency/index inventory,
not a new whole-Core/whole-WebAuthn audit. Unchanged collateral is not falsely called independently clean.

## Every final module Markdown fence (39)

Stable locator = exact path + fence ordinal in that file + heading/first code line below. This avoids
line-number drift while retaining every full block. `complete` means complete within the stated real
project or class context, **not compiled/executed or RP/hardware qualified**. `fragment` requires the
listed typed inputs/context; `sketch` is explanatory, not an operation/test to count as executed.

Typed contexts:

- **Console**: .NET 10 async top-level console project referencing Ctap2 (transitive Core), implicit
  System collections/threading/tasks; README imports Core.Abstractions/Credentials/Devices/Sessions,
  Ctap2/Credentials/Extensions/Pin and System.Text/Cryptography. Device chosen by application; one owning
  session live. No sample CLI references needed for this package-only demo.
- **UT**: real `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Yubico.YubiKit.Ctap2.UnitTests.csproj`,
  xUnit v3/MTP, NSubstitute and System.Formats.Cbor references, SDK/Core references and existing
  warning/analyzer policy. Methods go in a test class. Import Ctap2, Ctap2.Cbor/Pin/Extensions as used,
  System.Formats.Cbor, NSubstitute and Xunit (project global Using already supplies Xunit). Use the
  actual runner `TestContext.Current.CancellationToken` where the snippet calls cancellable operations.
- **IT**: real `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Yubico.YubiKit.Ctap2.IntegrationTests.csproj`,
  xUnit v2/Xunit.SkippableFact, Tests.Shared reference/global Xunit Using, Core.Devices/Sessions,
  `Yubico.YubiKit.Tests.Shared` and `.Infrastructure`, Ctap2/Pin/Credentials and
  `.IntegrationTests.TestExtensions` where used, System.Text/Cryptography. No xUnit v3 TestContext.
  `[WithYubiKey]` device-filter context is retained, and actual session preference is pinned to HID.
- **P**: live ICtap2Session/Ctap2Session, supported IPinUvAuthProtocol, caller-owned unchanged PIN/token/
  auth-param bytes and finally cleanup; RP permission scope and current device PIN/UV policy. V2 snippets
  require advertised protocol 2; legacy token command fallback is not protocol negotiation.
- **E**: caller `ReadOnlyMemory<byte> blobData`, supported protocol, owned/clearable sharedSecret/salt1/
  salt2 arrays (each salt32), public `IReadOnlyDictionary<int, object?> keyAgreement`; keep borrowed
  inputs stable through encoding/await. No promise to clear builder/output private copies.
- **CM**: caller `CredentialManagement credentialManagement/credMgmt`, authorized token/protocol/session,
  `ReadOnlyMemory<byte> rpIdHash` copied from returned RP hash, descriptor `credentialId`; dispose owned
  results after view use. Caller coordinates whole sequences; no assumed atomic lease.
- **CLI**: repository-root explicit run path or built FidoTool executable on PATH, actual sample csproj
  references to CTAP/Core/Management/Cli.Shared/Cli.Commands plus Spectre.Console; interactive prompts and
  known dedicated-key state. Human reset/UP/UV policy applies. Compilation is separate from library build.

| Exact path | Fence # / stable locator | Final classification and typed prerequisites |
| --- | --- | --- |
| `src/Ctap2/CLAUDE.md` | 1 Architecture Overview, `Yubico.YubiKit.Ctap2/` | sketch: directory tree, current COSE/ARKG, no PRF directory; not C#. |
| `src/Ctap2/CLAUDE.md` | 2 Session Creation, `await using var session` | fragment: IYubiKey yubiKey/async Console; direct IConnection alternative described separately with caller disposal. No simultaneous sessions. |
| `src/Ctap2/CLAUDE.md` | 3 GetInfo, `var info = await session.GetInfoAsync()` | fragment: session, LINQ Contains; advertised/state read, not feature operation or authorization. |
| `src/Ctap2/CLAUDE.md` | 4 PIN/UV, `// Fragment: session` | fragment with real awaited makeCredential: P, byte[32] clientDataHash, RP/user entities, MakeCredentialOptions, CancellationToken; uv null, auth/token clear after await. Exact-client-data/RP duties explicit. |
| `src/Ctap2/CLAUDE.md` | 5 Example Usage, `// Builder fragment` | fragment: E/MakeCredentialOptions; registration and assertion builder inputs only, no synthetic helper. Sensitive credBlob protection/enforcement caveat adjacent. |
| `src/Ctap2/CLAUDE.md` | 6 Credential Management, `using var credMgmt` | fragment: CM/P, standard command support and permission; actual metadata/RP/credential/delete tasks, owned enumeration disposal. |
| `src/Ctap2/CLAUDE.md` | 7 CBOR Encoding, `var request = CtapRequestBuilder.Create` | fragment: session/protocol and Ctap2.Cbor/Pin imports, raw command-prefix/status handling; caller schema/size/depth/sequence responsibilities. |
| `src/Ctap2/CLAUDE.md` | 8 Memory Handling, `Span<byte> pinHash = stackalloc byte[32]` | fragment: synchronous method, caller byte[] pinBytes, System.Security.Cryptography. Real SHA256 hash computation and finally clearing PIN/hash; not a complete ClientPin protocol or fake no-op helper/test. No span across an await. |
| `src/Ctap2/CLAUDE.md` | 9 Protocol Disposal, `using var protocol` | fragment: PinUvAuthProtocolV2 import/using scope; marks disposed, does not clear caller outputs; crypto-method scope of rejection explained. |
| `src/Ctap2/CLAUDE.md` | 10 Unit Tests, `AuthenticatorInfo_Decode_ParsesVersions` | complete method in UT class: partial versions-only parser fixture, explicitly not conforming full GetInfo (AAGUID absent); real Assert.Contains, no hardware claim. |
| `src/Ctap2/CLAUDE.md` | 11 Mock Patterns, `PinRetries_EncodesProtocolAndSubcommand` | complete method in UT class: typed mock {pinRetries:8}, actual awaited ClientPin request with runner token, captures bytes then asserts prefix/protocol/subcommand and decoded retry count. Not crypto/UV interoperability. |
| `src/Ctap2/CLAUDE.md` | 12 Hardware Integration, `GetInfoAsync_Returns_Aaguid` | complete method in IT class: WithYubiKey HID filter plus actual HID preference; direct session, no normalization, AAGUID length assertion; not new approval gate or all-device validity test. |
| `src/Ctap2/CLAUDE.md` | 13 hardware command fence, `dotnet toolchain.cs -- test --project Ctap2` | complete repo-root commands; first units only, second includes both then filters integration smoke; setup/approval caveat applies, not executed. |
| `src/Ctap2/CLAUDE.md` | 14 Feature Flags, `public static readonly Feature FeatureFido2` | sketch: actual schematic static fields in Feature/Core.Devices class context, not declarations to add to SDK; metadata not qualified support. |
| `src/Ctap2/CLAUDE.md` | 15 threshold fragment, `if (!Ctap2Session.FeatureBioEnrollment` | fragment: FirmwareVersion firmwareVersion/Core.Devices; threshold can rule out known older firmware, sentinel passes, not sensor/capability proof. |
| `src/Ctap2/PACKAGE_README.md` | 1 Installation | complete commands in an existing .NET10 consumer project, alpha feed/network; installation only. |
| `src/Ctap2/README.md` | 1 Installation | complete same consumer/feed context; not build evidence. |
| `src/Ctap2/README.md` | 2 Getting started, imports/device/session/GetInfo | complete Console top-level example: actual discovery/session/fresh read, no PIN/touch helper setup. Later prompt factory replaces this one. |
| `src/Ctap2/README.md` | 3 Create a credential, registrationHash | fragment after Console setup: demo hash/RP/user created locally, real persistent nonresident ES256 command, guarded attested result, ID/public key retained. Not verifiable RP registration. |
| `src/Ctap2/README.md` | 4 Get an assertion, authenticationHash | fragment: same session/imports and prior credentialId; separate demo hash, explicit single-descriptor allow list, signature output not verification. |
| `src/Ctap2/README.md` | 5 discoverable alternate, first/assertions/for | fragment **alternative to fence4**: same typed authenticationHash/session, existing discoverable credential for example.com and applicable policy required; the preceding nonresident credential is ineligible. Count default1, actual N−1 consecutive next calls with no intervening command; no account/RP selection or local timer/lease invented. |
| `src/Ctap2/README.md` | 6 administrative enumeration, using credentialResult | fragment: CM is explicitly caller-supplied; Ctap2.CredentialManagement import/fully qualified helper type required for harness, not fabricated by earlier credential example. Iterates and disposes owned result. |
| `src/Ctap2/README.md` | 7 PIN creation/assertion, pin/try/finally | fragment: Console/P, previously defined rp/user, known configured demo PIN and advertised V2. Fresh distinct pinRegistrationHash/pinAuthenticationHash; real creation/guarded ID then assertion targeting it; no uv with token. Clears owned auth/token/PIN after awaits. No assertion insertion after dead try scope. |
| `src/Ctap2/README.md` | 8 TouchPrompt class | complete class with IUserPresencePrompt/Core.Credentials and System threading/tasks imports; place outside top-level statements. Informational callback, no touch wait or verification. |
| `src/Ctap2/README.md` | 9 prompting factory | fragment: Console device/TouchPrompt; **replace** original factory, same session name, not a second live connection. |
| `src/Ctap2/README.md` | 10 Example run command | complete CLI repository-root explicit project command; ceremony-capable sample, never executed here. |
| `src/Ctap2/examples/FidoTool/README.md` | 1 Quick Start | complete CLI root-context build/run commands with explicit csproj. Module build not sample compilation; PIN-changing/token flows need advertised protocol2, not plain info/reset or all PINless operations. Actual key/prompt state required. |
| `src/Ctap2/examples/FidoTool/README.md` | 2 info | complete built-executable command; actual selector can prompt/admit unsupported SmartCard candidate; info itself no PIN. |
| `src/Ctap2/examples/FidoTool/README.md` | 3 reset | complete command syntax, human-timed dedicated target/known state; `-f` skips confirmation/reinsert instructions, not hardware restrictions or target verification. |
| `src/Ctap2/examples/FidoTool/README.md` | 4 access | complete set/change/verify command syntax with interactive entry; supported protocol2 and existing-known or no-PIN state as appropriate. Adjacent missing-RP verify caveat: permission-capable rejection is not proof of wrong PIN; no blind retry. |
| `src/Ctap2/examples/FidoTool/README.md` | 5 config | complete command syntax with configured PIN/advertised subcommand; toggle/result reconciliation, not universal FW support. |
| `src/Ctap2/examples/FidoTool/README.md` | 6 credentials | fragment: CLI plus shell CREDENTIAL_ID_HEX holding actual full ID for deletion; literal angle-bracket shell syntax removed. List is complete; deletion/force requires actual target/state. |
| `src/Ctap2/examples/FidoTool/README.md` | 7 fingerprints | fragment: CLI plus FINGERPRINT_ID_HEX actual template ID, sensor/PIN/human sampling. List/add syntax complete but known wire/schema/cancel limits candid; no workflow qualification. |
| `src/Ctap2/examples/FidoTool/README.md` | 8 interactive launch | complete built-executable syntax/interactive terminal; launch only, not full API coverage. |
| `src/Ctap2/tests/CLAUDE.md` | 1 marked MakeCredential method | complete method in IT class: HID filter/pinned helper, default known-PIN normalization, supported V2, real RP/user/hash/options/token/await/assertion and finally auth/token cleanup. Persistent demo credential not deleted, prior PIN state not restored; no invented timeout or RP registration proof. |
| `src/Ctap2/tests/CLAUDE.md` | 2 PIN Management | fragment: IT/P existing ICtap2Session, no PIN already set, supported V2, KnownTestPinString/Encoding imports, actual SetPin/finally; not normalizing an existing PIN. |
| `src/Ctap2/tests/CLAUDE.md` | 3 Running Tests | complete root-context commands; exact unit/integration/FQN/UP selection labels; smoke does not classify implicit setup as safe. Existing policy retained. |
| `src/Ctap2/tests/CLAUDE.md` | 4 Mock CBOR Responses | fragment: System.Formats.Cbor/UT class method context; partial versions-only parser fixture with missing required AAGUID stated, not complete valid GetInfo. |
| `src/Ctap2/tests/CLAUDE.md` | 5 RegistrationExtensions_EncodePolicyAndHmacEnablement | complete method in UT class: real builder with nullable Build guard, CBOR map/keys/policy/enablement assertions. Not no-op stub, device enforcement, output cleanup or WebAuthn PRF evaluation. |

## Every module XML code block (five, A/B/E-owned, untouched here)

There are **no `<code>`/`<example>` blocks in FidoTool source**; its changed XML is contract prose, mapped
per member/path above. A mechanical scan of the isolated module finds exactly the following five code
blocks. Their baseline classification is preserved below; A/B/E own the corrected final XML. This
worktree has no sibling integration, so final integrated XML classification/compilation must use their
actual accepted patches, not falsely describe baseline snippets as already corrected by F.

| Exact path / stable XML locator | Classification / typed prerequisites and dependency |
| --- | --- |
| `src/Ctap2/src/IYubiKeyExtensions.cs`, GetCtap2InfoAsync code block (baseline line52) | fragment: IYubiKey yubiKey/async Console/Ctap2; one-shot owns temporary session, use only without another live owning session. A-owned final XML review. |
| `src/Ctap2/src/IYubiKeyExtensions.cs`, CreateCtap2SessionAsync example (baseline line109) | fragment: IYubiKey, Core.Sessions/Devices/Protocols.SmartCard.Scp, caller-owned Scp03KeyParameters.Default before secure-session scope, authorized appropriately configured SmartCard key. Baseline opens incompatible simultaneous sessions; A owns scoped-alternative correction. Do not compile a flattened pair as the accepted result. |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs`, shared-secret layout code (baseline line34) | sketch: binary layout HMAC first32/AES next32, not C# assignments/executable task; B owns final layout contract. |
| `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs`, registration builder example (baseline line31) | fragment: ExtensionBuilder/CredProtectPolicy, ReadOnlyMemory<byte> myBlobData/E; building bytes not enforcePolicy/device acceptance. E owns sensitive-blob/capacity/enforcement qualifications. |
| `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs`, response.Extensions example (baseline line29) | baseline sketch: undefined response shape. E owns final replacement with explicit AuthenticatorData/guard or correct response property; typed raw extension memory/current decoder limitation, no result-owner guarantee. Not marked compilation-ready until actual E edit reviewed. |

Together these tables account for **39 final F Markdown fences + five XML dependencies = 44 module
blocks**. Each full block is included even when it has no CTAP keyword or only an implicit session/helper.
API snapshots/runner/project XML have no code/example blocks to add. Compiler-generated record members,
snapshot removed entries, generated bin/obj/XML/AST and immutable historical blocks remain excluded from
current runnable-example counts. Complete executable FidoTool methods remain one real sample-project
context; comments do not make their deferred runtime defects compile-tested or hardware-qualified.

## Preservation and validation performed (record/static only)

- `git status --short` at start: clean. `git rev-parse HEAD` in the final preservation script: baseline
  unchanged. The final changed-path list contains the 26 F module paths and four external paths above.
- Python compared changed C# files to baseline after discarding **only whole-line // comments and blank
  lines**: all other lines identical. This directly covers imports/directives/runtime strings/calls/traits
  and assertions in the edited sample sources, without a simplistic regex that strips strings.
- Python compared changed csproj XML after removing comments, the allowed Description and whitespace:
  all other elements identical. No project reference, warning policy, assembly name or API token changed.
- Python compared all **150 A–E module paths byte-for-byte against Git baseline**: all unchanged. No
  overlapping library/test edit. Public API snapshots and untouched test project/runner files remain
  unchanged; the initial four inventory hash discrepancies were Astra-verified Git LF versus checkout
  CRLF only, not unexplained mutations. Patch normalized the two edited csproj checkout files to LF;
  Git emits its usual LF→CRLF checkout warning, with only the allowed Description/comment source diff.
- Mechanical final fence scan: all fences closed; 39 Markdown openings and five source XML code
  openings, matching the classifications above. No F source XML code block silently omitted.
- Mechanical local Markdown link-path check on all changed Markdown: **zero missing file paths**.
  This is file existence, not remote-link or heading-anchor certification.
- `git diff --check`: exit0, no whitespace errors; only the two csproj checkout-line-ending warnings.

No build/test/compiler/formatter/complexity/docs gate, live device/UP/UV/PIN/retry/reset/storage/signing
operation, CLI invocation or image renderer was run. Typed snippets are inspected candidates for later
meaningful compilation in the **real UT/IT/sample contexts** above. Keep actual diagnostics if a harness
fails; do not remove attributes, calls, cleanup, analyzer policy or useful tasks to manufacture success.

## Coordinator dependencies and fresh-review checklist

1. **Module review:** review all five guides/package Description and 20 FidoTool sources/comment units
   together against actual source/primary excerpts, not just revised phrases. Check every fence/table
   above in complete context. Seven unchanged F support paths retain bounded investigator dispositions.
2. **Shared review:** check the four exact active external edits, L2 CTAP box/teaching paragraph, entire L3b sequence/notes and adjoining
   test/migration context. Preserve existing CTAP hardware policy; default PIN setup, not GetInfo itself,
   is the gating distinction. No unrelated YAML repair or historical migration-gap rewrite.
3. **Boundary review:** re-read resulting CTAP and WA UV/token/RP/transport/support statements plus E/C
   output/ownership docs. Retain WA-XREF-001 and WA-IMP-002/003/009/010/011/012/013/014–019. No new blanket
   conformance, privacy-filter, reset rollback or device-qualification claim. G-011 actor/serializer
   boundary remains needs-decision, not DTO stripping authority.
4. **Diagram regeneration required:** from coordinator's integrated checkout, use
   `scripts/architecture/render-architecture.sh --check`, then `scripts/architecture/render-architecture.sh`
   (actual pinned Mermaid CLI from package.json/architecture-images.env; root `npm ci` if installation
   is needed). It renders all blocks to SVG/PNG and updates the freshness/hash manifest, not a hand-edit
   image workflow. The changed L2 CTAP box and L3b CTAP sequence make both images stale now. Run the coordinator's
   normal `dotnet toolchain.cs -- docs-architecture` gate afterward and retain outputs; none was run here.
   Exact affected outputs are `docs/architecture/images/L2-layered-stack.svg`,
   `docs/architecture/images/png/L2-layered-stack.png`, `docs/architecture/images/L3b-fido2-ctap-sequence.svg`,
   `docs/architecture/images/png/L3b-fido2-ctap-sequence.png` and the updated
   `docs/architecture/images/image-manifest.txt`; the renderer deliberately processes all seven blocks.
5. **A/B/C dependencies:** integrated A/B/E XML blocks need final snippet classification/compilation.
   COORD-001 is part of the GetInfo decoder family: decoded absent EncIdentifier/EncCredStoreState become
   present empty nullable memory, so HasValue does not prove wire presence/capability; RawData preserves
   encoded presence. A's correction and the F guide qualification must remain consistent with E metadata
   call/test explanations. RC-R1 establishes seed -65700/BL-KEM labels/example ESP256 agreement, with
   outer kty, nested wrapping and derived-algorithm modeling differences still deferred.
   Session ownership/initialization/error/next/timer, caller-selected protocol/legacy fallback, PIN Unicode/
   retry/peer-key/cleanup and credential/user/descriptor/attestation/raw bounds remain underlying IMPs.
   Code-inferred Guid big-endian portability is framework-adjudicated inference, **not platform execution**;
   F adds no platform qualification claim. D-08 runtime wire-order defect is withdrawn, not pending fix.
6. **D dependencies:** bio standard wire/schema/status/missing-field completion and cancellation fixes
   reopen bio sample/menu/helper/package/support guidance. Preserve the sample's real independent begin
   ID retention. Administrative helper disposed use, eager totals/sequence/returned hash, config omission/
   toggle/current state and exception/cleanup gaps reopen README/CLAUDE/admin examples and tests guidance.
   Update-user is not described as broken from insertion order; writer auto-sorts.
7. **E dependencies:** hmac-secret variants/UP/salt versus token roles, PRF verified-variant/cleanup,
   inert credProtect enforcement/sensitive credBlob, real largeBlobKey/storage versus direct route,
   disclosure-only minPinLength and current previewSign/ARKG profile/verifier limits reopen all related
   guide/table/extension snippets after implementation. No blanket all-YubiKey direct-route absence claim.
8. **F executable follow-up:** MakeCredential/GetAssertion still set uv with token auth; random hashes/
   handles and missing assertion context/first-only collection still prevent RP integration. CLI fixed
   V2, help/set-pin omission, parser value/exit quirks, synchronous prompt cancellation, caught-failure
   provenance/support false, duplicate selector labels, inactive alternative and reset second-selection
   wrong-target risk remain. Reset `-f`/timing and alwaysUV strings are untouched executable defects.
   PinManagement verify lacks RP ID; FidoPinHelper CLI strings cannot be zeroed. Reopen matching README,
   menu/helper XML/comments and all executable help after authorized fixes, not from this prose correction.
9. **Test/hardware dependencies:** normalizePin defaults, partial PIN/config restoration, response/fixture
   cleanup and optional blob-key export remain deferred (A/B/C/D/E detailed ledgers). Smoke filter labels
   are corrected without claiming safe execution. Current bio fixtures/assertions do not qualify hardware,
   nonempty signature does not verify RP proof, and firmware constants do not certify model capabilities.
10. **Records/integration:** Astra owns current handoff updates, final source/example gates, sampling,
    hashes, integration and remote checkpoint. This isolated edit/report is not documentation closeout,
    no review is self-certified, and Pass 2 is not started.
