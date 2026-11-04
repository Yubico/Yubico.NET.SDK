# F fresh-review corrections — RF-R1–R4 and RF-O1–O3

**2026-11-04.** Editor F; worktree `/tmp/opencode/ctap2-001-edit-f`, unchanged baseline/HEAD
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. Read PRIMARY `review-f.md` completely and checked each
requested correction against actual source before editing. Prior root/module/test/shared guidance,
approved style and received-review/record-verification skills remain applicable. Status: **corrected,
stable for F follow-up review and final snippet extraction**, not an independent PASS or compilation.

Seven paths from the existing reviewed **30-path F source/document diff** changed. No new source path,
unrelated style rewrite or PIV/Core diagram-area edit. This report and the current `edit-f.md` update
are written directly to PRIMARY; source corrections remain only in the F worktree. No builds, tests,
hardware/PIN/UP/UV/reset operations, commits, delegation, integration or rendering.

## Verified required corrections

| Review ID | Exact path / stable corrected unit | Independent source check and correction |
| --- | --- | --- |
| RF-R1 | `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs`, class remarks, SmartCard transport sentences | `src/Ctap2/src/Ctap2Session.cs:436–452` returns immediately for `Transport.Nfc`; only the other path reaches the USB SmartCard firmware comparison. Local XML now separately states current NFC Transport.Nfc plus exposed FIDO AID, versus USB SmartCard SDK5.8 gate plus AID. Existing interface-filter/auto-selection/cancellation/duplicate-label/reset-target qualifications remain. No selector policy/string/code change or older-NFC-device qualification. |
| RF-R2 | `src/Ctap2/examples/FidoTool/README.md`, Quick Start prerequisite paragraph and access paragraph immediately after the command fence | Re-read `FidoExamples/PinManagement.cs` set/change/verify paths and existing administrative/credential token paths: fixed `PinUvAuthProtocolV2`, no negotiation. Re-read `ClientPin.cs:332–352`: default rpId=null and legacy fallback through the same supplied protocol. Guide now requires advertised2 in `AuthenticatorInfo.PinUvAuthProtocols` for PIN-changing/token-authorized flows, explicitly not plain info/reset or all PINless operations. `VerifyPinAsync:167–170` asks GetAssertion permission without rpId; ClientPin's scope contract at325–326 requires it. Adjacent guide warns correct-PIN requests can be rejected for missing scope, legacy fallback drops scope/permission, and failure is not wrong-PIN evidence or permission to blindly retry. No request or help string fix. |
| RF-R3 | `src/Ctap2/CLAUDE.md`, Protocol Disposal final sentence | Re-read exact declarations: `IPinUvAuthProtocol.AuthenticationTagLength` at46, V1 at53 and V2 at60. Replaced `TagLength` shorthand with the actual `AuthenticationTagLength` member. Stateless/disposal/shared-secret contract and code fences unchanged. |
| RF-R4 | `docs/architecture/sdk-architecture-diagrams.md`, L2 CTAP path box and CTAP2 teaching paragraph | Actual credential session calls request helper and typed make/get response Decode (`Ctap2Session.cs`251–277/311–337), as already explained correctly by L3b. L2 now names `Ctap2SessionRequestEncoding` for request bytes and `MakeCredentialResponse / GetAssertionResponse.Decode` for response bytes. Paragraph explains request composition through CtapRequestBuilder, backend exchange/status resolution, then typed Decode, and distinguishes CtapResponseParser as a general utility not the traced credential decoder. PIV/Core boxes, unrelated teaching notes and backend/APDU convergence remain unchanged. |

These are source-backed clarification/member/path corrections; no new framework or protocol experiment
was needed. RF-R2's scope requirement is independently consistent with the actual helper/defaulted
argument and ClientPin contract; original protocol evidence remains the bounded references freshly checked
by review-f and the existing PIN investigation, not a new device rejection witnessed by this editor.

## Accepted useful optional corrections

| Review ID | Exact path / stable unit | Change and retained meaning |
| --- | --- | --- |
| RF-O1 | `src/Ctap2/examples/FidoTool/Cli/Menus/InfoMenu.cs`, DisplayAuthenticatorInfo XML summary | Indented two summary lines to match surrounding XML. No wording, member, display string or guard change. |
| RF-O2 | `src/Ctap2/examples/FidoTool/FidoExamples/CredentialManagement.cs`, UpdateUserInfoAsync remarks | Replaced private audit-runtime-defect narration with the lasting fact: the canonical writer automatically sorts encoded user-map keys when the map completes. Existing user-ID/name/RP-account boundary retained. No inference that update is broken from insertion order, no operation/hardware qualification added, no runtime write change. |
| RF-O3 | `src/Ctap2/README.md`, Get an assertion, discoverable alternative introduction | Explicitly requires an existing discoverable credential for example.com; preceding nonresident creation is ineligible for that population. The alternative remains conditional, not a claimed fresh-key success path. Actual first-response/N−1 consecutive next-call fence is unchanged. |

## Snippet/diagram classifications and remaining regeneration

All **39 module Markdown fence bodies and their ordinals remain unchanged** in this follow-up.
Five A/B/E-owned XML code dependencies likewise remain untouched; no FidoTool `<code>` example added.
`edit-f.md` now updates the necessary typed prerequisites without dropping tasks or inventing stubs:

- README fence5: composable **discoverable alternative**, requiring existing discoverable example.com
  credential/state; prior nonresident credential does not satisfy it. Real consecutive getNext code retained.
- FidoTool README fence1/fence4: complete command syntax in real sample project/executable context;
  PIN-changing/token flow advertised-V2 prerequisite and verify missing-RP failure caveat are adjacent.
  Plain info/reset and all-PINless claims are not accidentally gated by V2.
- CLAUDE Protocol Disposal fence9: unchanged V2 using fragment; surrounding member is now exact
  AuthenticationTagLength, not an invented property.
- External architecture **L2 Mermaid fence (index2)** is an architectural sketch, not executable C#.
  Its current CTAP internal encoding/typed response participants agree with the already corrected
  **L3b Mermaid fence (index4)** main-flow sketch. Backend/APDU and transport context retained.

Coordinator must regenerate **both** affected image pairs after integrated source acceptance:

- `docs/architecture/images/L2-layered-stack.svg`
- `docs/architecture/images/png/L2-layered-stack.png`
- `docs/architecture/images/L3b-fido2-ctap-sequence.svg`
- `docs/architecture/images/png/L3b-fido2-ctap-sequence.png`
- `docs/architecture/images/image-manifest.txt`

Use the existing pinned workflow in `scripts/architecture/render-architecture.sh` (preflight `--check`,
then render; renderer deliberately processes all seven diagrams), followed by coordinator's architecture
gate. None was invoked here. Existing generated images are not hand-edited or asserted fresh.

## Exact final hashes for the seven corrected paths

SHA256 is over the actual F checkout bytes after all RF corrections. These replace only the seven
reviewed snapshot hashes; initial review and earlier correction hashes remain immutable provenance.

| Exact repository-relative path | New SHA256 |
| --- | --- |
| `docs/architecture/sdk-architecture-diagrams.md` | `a38fe512d725b2f405796da6fbc7c2535fa4106fc816d333ad7bc306c69e9701` |
| `src/Ctap2/CLAUDE.md` | `0bfe308f3dcaf2307bc2e3d463f174bd4a341a85b583c1ac2cb07d0ad65c2dcf` |
| `src/Ctap2/README.md` | `2d41ea0fb0c4477355680cc6f271e7277a4f108b6012986a230aadf3d3df1225` |
| `src/Ctap2/examples/FidoTool/Cli/Menus/InfoMenu.cs` | `e413b6d9c272c8b679b682b7bd4f097e7c5dcb03679e3afbe0f7bae3b7ae68f1` |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs` | `783d6bc26251ab9ea7a609374601131cc161b37023f3810d28375b90c9eb8558` |
| `src/Ctap2/examples/FidoTool/FidoExamples/CredentialManagement.cs` | `236bf3766f0f0630eb7c7096aaddbd25072d5ae04569edbda821e1b21581d706` |
| `src/Ctap2/examples/FidoTool/README.md` | `897c0f2a2775d10d41e6540932f44bed409919520418b93b71cdfe23ced42cea` |

## Fresh bounded verification

Read-only Python in the F checkout:

1. Parsed the 30-path reviewed hash table in `review-f.md`, compared every actual hash: **exactly the
   seven listed paths changed; other23 reviewed hashes unchanged**. `git diff --name-only` remains
   exactly the original30 paths. No added source/document edit outside that scope.
2. Compared changed C# to Git baseline after excluding only whole-line `//` comments/blank lines:
   **all non-comment lines unchanged**, including executable strings/imports/traits/assertions/calls.
3. Parsed csproj XML and normalized text/removed only allowed Description: **all other elements
   unchanged against baseline**. Sample remains comment-only; shipping remains Description-only.
   No project reference, analyzer/NoWarn/policy or runtime/API change.
4. Extracted all seven Mermaid fences and compared to baseline: **only indices2=L2 and4=L3b differ**;
   five unrelated diagram fences byte-identical. L3b itself was not changed by this follow-up.
5. Counted/closed module fences: **39 retained**. No current-module snippet body was edited by these
   RF patches; only surrounding prerequisites and the two external diagram classification contexts differ.
6. `git diff --check`: **exit0**, no whitespace errors, only the two previously recorded csproj
   LF→CRLF checkout warnings. No build, Roslyn/API gate, formatter, SDK unit/integration test,
   hardware operation, renderer or compilation success inferred.

F is ready for the requested **fresh follow-up review** of these seven complete units and final
snippet extraction in the real consumer/UT/IT/sample contexts retained in edit-f. COORD-001, ARKG-10
partial agreement/experimental limits, unmodified hardware approval policy, deferred executable defects,
known migration YAML DOC002, sibling-owned final XML and the final integrated CTAP↔WebAuthn boundary
review remain unchanged dependencies. Neither these corrections nor later F prose acceptance starts Pass2.
