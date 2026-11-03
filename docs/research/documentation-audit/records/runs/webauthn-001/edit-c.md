# Editor C — WebAuthn documentation reconciliation

2026-11-03; fresh GPT-6.1 Sol editor C; branch `yubikit-docs-audit`; baseline
`e353b49afa15d1d104fce70bbf1e1abc59ddb132`.

## Outcome and boundaries

Applied the accepted WA-C-001–013 documentation dispositions, dependent A/B limitations, both external
consumer-snippet corrections, A review R1/R2, and the subsequently assigned B-R1 summary correction.
This is an editorial application report, not independent review or runtime acceptance. Runtime defects
remain defects/limitations; no API, executable code, test assertion, fixture value, or device state was
changed by this editor.

Read root/WebAuthn contributor guidance, Core guidance for the one XML example, Management guidance
and its required README, approved `style.md`, complete investigation A/B/C reports, sample expansion,
triage including the review amendments, and reviews A/B. Loaded `docs-module` before module Markdown
edits; also loaded receive-review and verification guidance. Context7 `/dotnet/docs` confirmed the
named-third-argument syntax when omitting an optional second argument. Local SDK declarations, result
types, extension sessions, test helpers and the affected test assertions supplied narrow source checks;
this did not repeat the investigators' complete implementation audit or independently re-fetch their
specification corpus.

All repository writes used `patch` and were confined to the **15 paths** below. No build, test,
formatter, compilation, package installation, hardware operation, commit, staging, push or delegation
was performed. Historical migration sections and files were preserved. The coordinator's research
checkpoint was not edited.

## Exact path/finding map

| Repository path | Applied finding / edited semantic sections |
| --- | --- |
| `src/WebAuthn/README.md` | C-002/C-003: one explicitly local hardware demo with one client, one retained registration result, separate request options/challenge, borrowed demo PIN cleanup, production RP-owned inputs and account handle, byte-level response handoff and independent RP completion. C-004/A-001/A-002/A-Q1: count before indexing, allow-list narrowing versus discoverable enumeration, eager outputs/cached selection, user display-field limits, original-singleton-only ID guidance with corrected CTAP version attribution. C-005/A-008/A-021: preference versus method availability, extension permissions, one escalation, protocol-2 restriction scoped to relevant operations. C-007: phase/task extension overview and complete credProps input/result in the demo; B-001–003/005–010/012–015/C-D1/D2: contextual PRF UV/cleanup, credProtect enforcement, blob Required/write/read/support/lifetime, minPinLength presence, signing capability/args/attestation/empty-handle limits. C-012: presence versus a literal gesture and transport-specific notifications. C-013: privacy-sensitive identifiers, no blanket logging safety. A-003–007/012–014/019: actual default/other attestation behavior, error mapping boundaries, cooperative timeout/success race, challenge responsibility, client/session ownership, serialization/default-AAGUID limits, and SCP SmartCard default. Exact RP allowlist/application-service examples, server checklist, alpha status, transport snapshots, prompt ownership/retries and historical migration material retained. |
| `src/WebAuthn/PACKAGE_README.md` | C-001: required supported origin and application `RpIdValidator`, not removed PSL checker; RP syntax/binding policy and server completion explicit. C-002/C-010/C-011: client-only scope and direct administration in CTAP2. Alpha disclaimer, feed/package commands, transitive dependencies and task links retained. |
| `src/WebAuthn/CLAUDE.md` | C-004–006/C-008/C-009/C-012/C-013 and A-020: actual Codec/Processing/CredentialFilter/Ceremony tree, processor preparation phase, no stale adapters/pipeline/phase claims; accurate no-local-logger observation without a downstream silence guarantee; count-safe example and alternative (not overlapping) client construction; eager cached selection; correct L2 §5.8.6 citation and SDK UV policy; current permission union; protocol-2 and built-in-UV capability limits. previewSign vendor draft v4, complete allow-list mapping/multi-credential selection, public dictionary, args/inner-attestation/algorithm-role/empty-handle and ARKG limits. Narrow A/B policy/lifetime/error/attestation/default-value limitations reconciled locally. `NormalizePinAsync(Ctap2Session)` actual fixed-PIN signature and example corrected. Hardware approval/physical-presence lanes, prompt contract, exact RP policy, transport metadata and useful test commands preserved. |
| `src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj` | C-011: **Description only** now advertises registration/authentication, typed extension processing and experimental vendor draft, not credential administration. No build properties/references/tags changed. |
| `docs/architecture/webauthn-extension-processing.md` | C-004/C-008/C-009 and dependent A/B limits: Ctap2 naming/layers; version-specific hmac-secret sections; actual PrepareAsync/input/output phases and creation/effective-RK/allow-list factory context; response largeBlobKey timing; current permissions union. Exclusion retains final required-UP command, with tokenless/signed-zero-hash branches and registration remint; allow probing can take multiple round trips; main NoCredentials/InvalidCredential gives no matches; no fake no-disclosure guarantee. Unsupported previewSign omission, credProps/blob exceptions and two-category output-isolation policy are distinguished from WebAuthn optionality. Eager outputs, per-attempt secrets and separate ceremony token lifetime retained. Protocol-2, PRF/cleanup, credProtect, blob/read/write, minPinLength and signing limitations explicit. Unpinned detailed sibling comparison table replaced with current SDK flow/boundaries rather than extending unverified parity claims. Direct-blob and selection-UI non-goals retained without universal hardware claims. |
| `docs/v2-highlights.md` | C-010: WebAuthn section now explains client orchestration and RP/application responsibilities, links the guide, and removes unsupported user-research superlative, universal Android/Python/Rust transfer and roadmap promises. Adjacent module prose untouched. |
| `docs/v1-to-v2-comparison.md` | C-010: only the New-in-v2 WebAuthn bullet is qualified as client-side orchestration with RP options/verification, not the full FIDO ceremony. Historical comparisons untouched. |
| `src/Management/examples/ManagementTool/README.md` | C-012: only the FIDO HID transport bullet changed from “FIDO/WebAuthn HID interface” to FIDO/CTAPHID with WebAuthn-over-CTAP2 explanation. |
| `docs/usage/user-interaction.md` | WA-S-001 / WA-SAMPLE-EXPAND-001: relevant intent snippet uses `requestOptions` and `cancellationToken: cancellationToken`; immediate prerequisite distinguishes WebAuthn request options from session-creation options and locates the retained presence prompt. It remains an intent fragment, not a second authentication tutorial. |
| `src/Core/src/Credentials/UserPresenceIntent.cs` | WA-SAMPLE-EXPAND-002: **one XML code invocation only**, matching `requestOptions` and the named cancellation argument. No Core implementation or other prose changed. |
| `src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs` | Review A R1: `CredentialId` XML explicitly identifies singleton omission permission as CTAP2.0 §5.2, versus required descriptor in CTAP2.1/2.3 §6.2.2. Empty-ID defect, original-singleton-only recovery context and mandatory RP verification retained. |
| `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs` | Review A R1: same local version distinction on GetAssertionAsync; corresponding known-limitation comment at final ID construction identifies the CTAP2.0 permission. A's other edits and all executable text retained. |
| `src/WebAuthn/src/Client/CredentialFilter.cs` | Review A R1: only singleton-response comment/citation corrected from generic §6.2.2 to 2.0 §5.2 versus 2.1/2.3 required field. Filter behavior untouched. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs` | Review A R2: only `GetAssertion_AllowList_NothingMatches_SendsDummyDescriptorAndReturnsEmpty` summary now describes the dummy final request's required-UP policy and empty result. It explicitly does not claim gesture observation or a no-disclosure proof. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs` | Review B B-R1 (later explicit scope extension): only `TwoCredentialAllowList_SignsWithTheSelectedCredentialsEntry` summary changed. It now states the actual first-ID match/otherwise-second verification-key branch and absent returned-ID membership assertion. Experimental ARKG/capability qualification retained; assertions, strings and runtime text untouched. Deferred stronger coverage remains WA-IMP-021. |

All thirteen C findings are accounted for: C-001 package page; C-002–005 README/contributor/architecture
journey; C-006 contributor architecture/signing; C-007 extension usage; C-008 architecture;
C-009 draft identity; C-010 discovery prose; C-011 Description; C-012 presence/transport; C-013 privacy.
No finding is declared an implemented runtime fix by this mapping.

## Verification actually performed

- Read the complete resulting module README, PACKAGE_README and CLAUDE, architecture page, and every
  other complete edited section in context. Re-read the final R1/R2/B-R1 summaries with the relevant
  ID construction/probe/fake/verification-key branches. Subsequent small corrections were re-read.
- `git diff --check` and an explicitly scoped `git diff --check -- <15 owned paths>` returned exit 0,
  without whitespace-error output. Git does emit its LF-to-CRLF working-copy warning for the csproj;
  this is not a build warning or a claimed compiler pass.
- A read-only Python comparison of the **six C# correction paths** against HEAD removed comments and
  nonliteral whitespace while preserving string/character literal contents. All six matched. This
  covers the four A-correction files, the Core snippet, and B's integration summary, including existing
  A/B changes in those files. It is a lexical sanity check, not a C# compiler/tokenizer certification.
- Parsed all XML documentation groups in those six resulting C# files with an XML parser: no syntax
  parse failure. This does not resolve crefs or compile XML examples.
- Parsed csproj XML and compared baseline/current after replacing only the Description element: all
  other normalized text matches. Git's project diff is one removed/one added Description line.
- Checked local Markdown link file targets and fence balance in the three module Markdown files and
  architecture page: no missing local target or unbalanced fence. HTTP targets and fragment anchors
  were not revalidated as a network/link-check gate.
- Checked status before editing; no pre-existing modification was discarded. This editor did not write
  any original untracked file: the four `docs/research/` files (including
  `webauthn-documentation-audit-checkpoint.md`) and `mise.toml` remain coordinator/user work.

No build/test/format/compile/device success is asserted. Final campaign checks, independent semantic
review, public-API/analyzer checks and actual snippet compilation remain coordinator gates.

## Code-block compilation handoff

Numbers below count `csharp` fences within each file, not all fences. Locations are also identified by
section so line movement does not invalidate this handoff.

### `src/WebAuthn/README.md`

| Block | Status / later compilation recipe |
| --- | --- |
| 1 — Getting started | **Complete composable local demo**, intended as one top-level .NET 10 console program with the WebAuthn project/package reference and ordinary console-project implicit usings enabled. Contains discovery, narrow validator, one owned client, registration with credProps, original transport/result access, a separate single-entry assertion request, count guard and PIN-byte cleanup. No application stubs required for compilation. Compile only unless a human separately authorizes dedicated-key interaction; execution needs the existing demo PIN, protocol-2 token support and discoverable capacity, and creates persistent state. Local RNG/options are deliberately not production RP semantics. |
| 2 — PinPrompt class | Complete class definition with block 1's relevant usings; place type definitions after top-level statements. The injected delegate is application-provided when instantiated. The class illustrates copying exact UTF-8 bytes into the returned owner and clearing the acquired source. |
| 3 — PIN prompt option replacement | **Application-stub fragment**. Needs `ReadPinAsync(CredentialPromptContext, CancellationToken) -> Task<byte[]>`; insert before block 1's one factory call. Use with block 2 and replace ceremony PIN arguments with null. Do not append a second client. |
| 4 — TouchPrompt class | Complete class with block 1's relevant usings; place after top-level statements. No new application stub. USB-oriented display wording is an example, not transport-independent gesture evidence. |
| 5 — notifying factory replacement | Composable with blocks 1 and 4 **by replacing**, not appending, block 1's factory call. Reuses `device`, `origin`, `clientOptions`; no second physical connection. Can combine with the PIN option replacement if its input delegate is supplied. |
| 6 — Explicit origin/RP allowlist | Complete option-configuration fragment with the relevant standard/WebAuthn usings. Supplies its own trusted pairs; use as an alternative validator before construction, not a standalone ceremony. No service stub. |
| 7 — Application-service validator | **Application-stub fragment**: `rpValidationService.IsRpValidForOrigin(WebAuthnOrigin, string) -> bool`, owning syntax and binding with preloaded trusted policy. No SDK service implementation is invented. |
| 8 — Migration Before/After | **Historical sketch**, deliberately not a compilable combined sample. “Before” uses removed members. “After” requires an existing `ctap2Session` and `origin`; do not mechanically compile the combined fence or rewrite history. |

The RP handoff is prose, not invented upload/serialization code. A production integration additionally
needs its own trusted option retrieval, response encoding/network protocol, verification and persistence.
Those are not missing SDK stubs in the local demo.

### Other edited documents

- `src/WebAuthn/CLAUDE.md`: logging is an internal field/method fragment (ILogger/YubiKitLogging,
  PreviewSignProcessor and selectedAlgorithm context); direct-client/alternative-factory blocks need
  the named application/session/options/prompt/RP-validation/SCP inputs. They are alternatives, not
  concatenated opens. `ICredentialPrompt` is a contract sketch, not a second type to compile beside the
  SDK definition. The previewSign block needs generatedKey, credentialId, algorithm-appropriate
  toBeSigned/additionalArgs and the public signing/extension usings; the now-ordinary dictionary needs
  no inaccessible comparer. The hardware-test block is a test-project skeleton using actual internal
  WebAuthnTestHelpers and YubiKeyTestState; it intentionally leaves test logic to the author and is not
  a standalone console demo. Setup session closes before the client factory opens.
- `docs/usage/user-interaction.md`, Adding the application's intent: corrected fence needs an existing
  WebAuthnClient, PublicKeyCredentialRequestOptions `requestOptions`, and CancellationToken. It owns
  only the intent scope. The companion callback is a prompt method fragment. Ignoring the returned
  list here is intentional for scope teaching, not a complete authentication flow.
- `src/Core/src/Credentials/UserPresenceIntent.cs`: corrected XML fence has the same existing
  client/request-options/token prerequisites. Neither intent example is a standalone demo.
- Architecture type trees and inline method calls are internal schematics, not literal full signatures
  or a proposed public extension API. Package installation fences were retained, not executed.

## Remaining questions / review requests

No new blocking documentation question was discovered beyond the accepted dispositions. The existing
deferred implementation ledger (WA-IMP-001–021 and WA-XREF-001) remains open, including the unverified
largeBlob nullable conversion; no result-presence outcome was inferred from that hypothesis.

Reviewer A should narrowly recheck the four assigned R1/R2 files; reviewer B should recheck the single
B-R1 summary. Fresh C review should check the reader journey and qualifications together, especially
PRF Required not being presented as full conformance/cleanup, protection checks not substituting for
prevention, main-versus-output error boundaries, and source/citation layer/version/actor distinctions.
The coordinator retains responsibility for later compilation and campaign acceptance gates.
