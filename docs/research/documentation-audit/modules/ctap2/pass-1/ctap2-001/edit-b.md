# B — PIN/UV documentation edit handoff

2026-11-03; fresh editor `github-copilot/gpt-6.1-sol`. Status: **RB-R1 and authorized RB-O1 corrected; review-B follow-up pending**.
Source edits exist only in `/tmp/opencode/ctap2-001-edit-b`, at baseline
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. That worktree was clean on entry. No worktree
edits were copied to the primary checkout. This report was the initial batch's only primary-checkout
write; the correction follow-up also writes [correction-b.md](correction-b.md).

Read root `CLAUDE.md`, available skill descriptions, module and test `CLAUDE.md`,
`docs/TESTING.md`, this packet's plan/inventory/triage/investigations B and G, retained
`evidence/b/original-clauses.md`, and campaign workflow/style. All 13 B-owned files were
read completely, including test assertions. Narrow dependency reads checked concrete
`Ctap2Session.GetInfoAsync` and `CtapRequestBuilder.WithBytes`; other owners' paths were not edited.
Loaded secure-credential-prompt, security-guidelines and verify skills. Build/test/commit skills
were inapplicable: **no builds, tests, hardware operations, commits or delegation were performed**.
Context7 `/dotnet/docs` confirmed the asynchronous buffer lease ends on task completion, fault or
cancellation; this supports borrowing prose, not cryptographic-provider or hardware guarantees.

## Exact changed files

These are exactly the 13 inventory paths owned by B. All changes are XML/line comments, plus
terminal-newline normalization. Executable nonblank lines, including strings, directives, imports,
API declarations and assertions, are unchanged.

| Changed path | Edited semantic units | Accepted groups / provenance |
| --- | --- | --- |
| `src/Ctap2/src/Pin/ClientPin.cs` | Class lifecycle/PIN/token contract; constructor/Protocol/Dispose; retries; all PIN parameters and token returns; fallback and UV remarks; validation/padding/parser comments | DOC-004/005/006/007; B-01–09/11/13 |
| `src/Ctap2/src/Pin/ClientPinSubCommand.cs` | Protocol/response-field constants; permission type, LargeBlobWrite and CredentialManagementRO XML | DOC-005/006; B-03/06/09 |
| `src/Ctap2/src/Pin/IPinUvAuthProtocol.cs` | Algorithm overview; every crypto member's input/output representation, lengths, borrowing, ownership and exceptions; local Verify versus device authorization | DOC-004/005; B-01/02/07/08/11 |
| `src/Ctap2/src/Pin/PinRetryStatus.cs` | Snapshot, count default and omitted power-cycle state | DOC-006; B-06 |
| `src/Ctap2/src/Pin/PinUvAuthHelpers.cs` | Required peer structure versus current checks; raw-Z ownership; import/derivation exception and allocation-cleanup limits | DOC-004; B-07/08 |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs` | Concrete V1/V2 distinctions, no AES padding, caller selection, disposal/property and scoped-cleanup remarks | DOC-004; B-01/02/08 |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs` | Independent HKDF labels/salt/layout, no AES padding, caller selection, disposal/property and scoped-cleanup remarks | DOC-004; B-01/02/08 |
| `src/Ctap2/src/Pin/UserVerificationRetryStatus.cs` | UV retry snapshot/defaults; unsupported standardized UV power-cycle inference; configured-PIN recovery | DOC-006; B-06 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EnhancedPinTests.cs` | Class assertion/setup limits and firmware early-return comment | DOC-012; B-10/12 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PinManagementTests.cs` | Class and method assertion scope; callback-limited restoration/cleanup; temporary/exhausted block distinction; uv omission; caught-error cause uncertainty | DOC-006/007/012; B-10/12/13 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/ClientPinTests.cs` | Mock/fake scope, omitted retry fields, send-count versus encoded-field assertions, fake Decrypt, fake class summary | DOC-012; B-10/11 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV1Tests.cs` | Class evidence scope and accepted peer fixture's missing alg | DOC-004/012; B-07/11 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV2Tests.cs` | Class evidence scope, missing alg fixture and public-coordinate-only assertion scope | DOC-004/012; B-07/11 |

Group abbreviations above mean `CTAP-DOC-004/005/006/007/012` from `triage.md`.
Security-critical key precision follows the accepted B-07 / `CTAP-IMP-007` dependency; it does not
reclassify missing validation as intended forward compatibility.

## Candidate-to-edit dispositions

| B candidate | Documentation disposition / remaining boundary |
| --- | --- |
| B-01 | V1 is SHA-256(z); V2 uses two independent labeled HKDF derivations. ClientPin right-zero-pads to 64; AES adds no padding. F's incorrect PKCS#7 guide text is routed, not edited. |
| B-02 | Supplied protocol is disposed by ClientPin, session borrowed. Protocol reference/version/tag properties remain readable after disposal; operations do not. Inputs borrowed through completed calls; returned secrets and tags caller-owned. |
| B-03 | Caller chooses supported protocol. PIN permission method calls supplied GetInfoAsync: **concrete Ctap2Session performs a fresh query**. Exact condition is `pinUvAuthToken` present and true; otherwise legacy 0x05 drops permissions/RP and retains protocol. No retry of failed 0x09. Legacy defaults retain noMcGaPermissionsWithClientPin exception and first-use RP binding. |
| B-04 | NFC UTF-8, Unicode-character baseline, device code-point minimum, no final zero and additional policy remain caller/device obligations. SDK checks only 4–63 bytes and hashes exact unpadded bytes. No normalization/enforcement fix. |
| B-05 | Configured built-in UV and token capability distinguished from PIN. Permission-specific uvBioEnroll/uvAcfg gates, authenticator interaction, possible UP, generic-CBOR notification limits and caller recovery choice documented. |
| B-06 | Retry counts/defaults and false-versus-unknown are explicit. Field 4 has no CTAP 2.1 UV retry meaning. Temporary PIN_AUTH_BLOCKED, exhausted PIN_BLOCKED and configured-PIN UV recovery separated. |
| B-07 | Retained required EC2/P-256/alg=-25/coordinates/no-other-optional-parameters structure. Disclosed current coordinate-only checks and provider import outside wrapped derivation catch. Fixtures' missing alg identified; no validation/assertion changes. |
| B-08 | Positive named-buffer finally guarantees retained, without end-to-end/all-exception-path/provider-erasure claims. Returned output disposal not attributed to stateless protocols. Builder/request/response copies remain deferred. |
| B-09 | 0x40 is persistent pcmr under original CTAP 2.2 conditions: perCredMgmtRO=true, alone, RP ignored. Raw array has no persistent identity/lifetime metadata. encIdentifier linkage retained; unverified encCredStoreState linkage removed from this enum's prose and routed to A/E. |
| B-10 | Comments now describe actual nonempty-token/credential/scalar/count assertions. UV key is absent, not false; returned UV is not asserted. Broad catches are not proof of policy. Existing executable diagnostic strings/names remain frozen. |
| B-11 | Protocol-length expectations separated from absent ClientPin plaintext-length validation. Fake send/non-null/roundtrip evidence bounded; V1 independent SHA/HMAC computations and actual canonical retry assertions retained. Missing meaningful verification stays deferred. |
| B-12 | Default PIN-normalizing setup, early return, callback-only restoration and lack of asserted clearing disclosed. No harness, secret-cleanup, skipped-test or placeholder-test changes. Shared findings retain A ownership, not new bug counts. |
| B-13 | Token-authenticated Make/Get requests must omit uv, even false; returned data carries verification result. Owned test prose corrected. F Markdown and executable samples and C option/encoder dependencies routed; WebAuthn's already-correct omission is not changed. |

Normative protocol basis remains original CTAP 2.1 errata, Proposed Standard 2022-06-21,
§6.5.1/§6.5.5–7 and §6.1/§6.2; pcmr uses original CTAP 2.2, Proposed Standard 2025-07-14,
§6.5.5.7 and §6.4. Exact excerpts/anchors/actors/strength are retained in B's original evidence.
No automatic citation upgrade, certification claim or device qualification was added.

## Snippet classifications and typed prerequisites

There are **no new or edited executable snippets** in B's scope. The only existing XML `<code>`
block is `PinUvAuthProtocolV2`'s two-line shared-secret layout. Classification: **representation
diagram**, not compilable C# or an application sketch. It is retained unchanged; no compilation
gate applies. Its conceptual input is a 64-byte `byte[]`/`ReadOnlySpan<byte>` derived shared secret,
with the first 32 bytes HMAC and final 32 AES. No hidden session/device prerequisite belongs to
that diagram. XML `<see>`/`<paramref>` references are API links, not additional snippets.

Test bodies are inspected verification source, not newly introduced examples. Their project setup,
assertions, fixtures, attributes and strings remain unchanged; no tests were compiled or run.
For F's dependent task snippets, the explicit prerequisite contract is an open `ICtap2Session`,
supported `IPinUvAuthProtocol`, `ReadOnlyMemory<byte>` unpadded PIN backed by caller-owned mutable
storage, `PinUvAuthTokenPermissions`, optional public `string` RP ID where permitted, and
`CancellationToken`. Results are caller-owned `byte[]` tokens; `Authenticate` returns another
caller-owned `byte[]` tag. Ceremony fragments additionally require a correctly constructed
32-byte clientDataHash, RP/user/credential inputs and appropriate Make/Get options; they must
omit uv alongside token auth and cannot replace application/RP verification responsibilities.
F owns classification/compilation of those actual snippets, not B.

## Cross-owner documentation dependencies — not edited

| Owner | Exact dependent paths / units | Re-review trigger and routed correction |
| --- | --- | --- |
| F | `src/Ctap2/CLAUDE.md` PIN/UV, Protocol Disposal, Common Pitfalls, strict-key and notification sections; `src/Ctap2/README.md` PIN/user-verification/retries/security/interaction tasks; `src/Ctap2/tests/CLAUDE.md` PIN setup and safe lanes | Apply B-01–06/08/10/12/13 together: zero padding, protocol ownership, exact concrete-session fallback, Unicode policy, unknown retry state, device interaction versus notifications and callback-limited restoration. |
| F | `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoPinHelper.cs`; `src/Ctap2/examples/FidoTool/FidoExamples/PinManagement.cs`, `MakeCredential.cs`, `GetAssertion.cs`, `CredentialManagement.cs`, `BioEnrollment.cs`, `ConfigManagement.cs` | Reopen protocol selection, permission/RP/pcmr prerequisites and buffer lifetime explanations. Frozen executable uv/token combinations require the authorized Pass-2 correction, not a misleading current-sample success claim. |
| A | `src/Ctap2/src/AuthenticatorInfo.cs`; `src/Ctap2/src/Ctap2Session.cs`; `src/Ctap2/src/ICtap2Session.cs`; `src/Ctap2/src/Cbor/CtapRequestBuilder.cs`, `CtapResponseParser.cs`; `src/Ctap2/src/Ctap/CtapStatus.cs`, `CtapException.cs`; backend files | Keep fresh/cached info, minPINLength/forcePinChange/perCredMgmtRO, strict peer boundary, input-copy/raw-response cleanup and blocked/error/cancellation claims aligned. Reopen on changes to these contracts; false/default0 and a failed await are not device-state proof. |
| A | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestHelpers.cs`; `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs` | Normalization can mutate/request/discard secrets before callbacks. Coordinate shared cleanup/restoration findings under CTAP-IMP-020/021/022, without per-consumer duplicate counts. |
| D | `src/Ctap2/src/CredentialManagement/CredentialManagement.cs`; `src/Ctap2/src/BioEnrollment/FingerprintBioEnrollment.cs`; `src/Ctap2/src/Config/AuthenticatorConfig.cs` | Align borrowed session/protocol/token ownership, operation-specific permission/RP/capability requirements, built-in UV be/acfg gates and persistent read-only scope. Reopen if authorization representation or enforcement changes. |
| E | `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs`; `src/Ctap2/src/Extensions/HmacSecret/HmacSecretInput.cs`, `HmacSecretOutput.cs`; `src/Ctap2/src/Crypto/EncryptedMetadataDecryptor.cs`; `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs` | Shared-secret/tag/result ownership, no-padding AES, strict peer validation and scoped clearing must agree. Verify exact encrypted metadata field/key provenance separately; pcmr's encIdentifier contract does not prove encCredStoreState. LargeBlobWrite concerns writes, not every read. |
| G | `src/WebAuthn/src/Client/WebAuthnBackend.cs`; `WebAuthnClient.PinUvAuth.cs`, `PinUvAuthTokenSession.cs`, `PinUvSharedSecret.cs` in that directory; `src/WebAuthn/src/Client/UserVerification/UvDecision.cs`; `src/WebAuthn/CLAUDE.md`, `README.md`; packet `boundary-map.md` | Retain WA-IMP-009 and shared cleanup/PRF provenance. Do not insert disposing ClientPin wrappers around the backend's longer-lived shared protocol. Re-review both layers after negotiation, UV capability, prompting/retry, token identity/lifetime or peer validation changes; legitimate facade policy is not direct-CTAP behavior. |
| C (additional boundary) | `src/Ctap2/src/Credentials/MakeCredentialOptions.cs`, `GetAssertionOptions.cs`; `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs` | DOC-007 / CTAP-IMP-011: uv key and pinUvAuthParam exclusion is a platform obligation not currently enforced by the encoder. Coordinate F/G without changing executable tokens in this packet. |

## Initial edit local diff check — preserved historical evidence

Performed in the editor worktree, against its clean HEAD baseline:

- `git diff --check`: exit 0, no whitespace diagnostics.
- `git diff --stat` / `--numstat` and complete PIN/test diff reads: exactly 13 owned files,
  **292 insertions / 148 deletions** after final interface wording.
- Static Python comparison loaded B paths from the primary `inventory.json`, required exact equality
  with `git diff --name-only`, compared original/current nonblank lines excluding whole-line `//`
  comments, and parsed every contiguous `///` block with `xml.etree.ElementTree` under a wrapper.
  Result: **all non-comment nonblank lines identical; 91 XML documentation blocks well-formed**.
  Original inline comments also remain identical. This is stronger than token equality for the
  preserved executable lines, but is not a compiler/API-reference-resolution check.
- `git diff --binary | sha256sum`: `b8fc9814c9d5754dd72436b9315127cc30818f7306418de5ee5cabf7b3ce13e0`.
  This identifies the uncommitted source patch with the current local diff formatting configuration.

One oversized patch request was rejected by the patch parser before application because of an empty
hunk marker; corrected requests were applied with the patch tool. No partial runtime edit occurred.
Terminal newlines were added to edited files that lacked them; no imports or directives were reordered.
At the initial handoff, no source edit followed that static comparison and final diff read/hash.
The ClientPin review corrections below supersede that patch hash; the evidence above is retained.

Build, unit tests, example compilation, generated-doc/cref validation and hardware are **not run**.
The initial independent review is [review-b.md](review-b.md); its required clarification and the
authorized optional addition are addressed below, with review-B follow-up still pending.

## Unresolved questions / Pass-2 handoff

- Maintainer allocation of Unicode/device-policy enforcement, retry presence/required-count handling,
  token length/complete-response validation and capability/permission/RP/pcmr preflight remains deferred
  under CTAP-IMP-006/008/009/010. No silent normalization or token API redesign is authorized.
- Required peer labels/optional structure, import exception characterization across providers and
  exceptional-path clearing remain CTAP-IMP-005/007; no cross-platform experiment or provider-erasure
  claim was made. Empty-salt HMAC equivalence is retained reasoning, not an executed independent vector.
- A/E/F must settle encCredStoreState's original field/key provenance and product reset windows. B does
  not infer either from CTAP 2.2 pcmr or a frozen test diagnostic.
- Executable integration diagnostic strings still conflate blocked states and claim ten-second reset
  instructions; names still say UV discouraged. Comments correct their meaning, not their runtime text.
  Cleanup, normalization, restoration and meaningful assertion work retain CTAP-IMP-020/021/022.
- F/C/G must reconcile all resulting uv/token task/sample/option claims, and G must independently
  compare direct caller-selected protocols with the current hard-coded WebAuthn V2 limitation.

No new implementation-defect counts were introduced. Existing ledger groups and WA provenance remain
the authority; documentation edited does not mean implementation fixed or hardware qualified.

## Review correction amendment

2026-11-03: only `src/Ctap2/src/Pin/ClientPin.cs` changed from the reviewed file manifest.
RB-R1 changes the UV-token opening paragraph to distinguish the authenticator's implementation
obligation from caller invocation, explicitly require caller-checked uv=true, pinUvAuthToken=true
and configured built-in UV, and state that the helper queries/preflights none of those conditions.
Existing permission-specific gates, interaction, single-request/no-fallback and persistence text remain.

Authorized RB-O1 adds one class paragraph: check forcePINChange and change the PIN before PIN-token
acquisition. Original CTAP 2.1 distinguishes forced-change errors after correct-PIN verification:
legacy 0x05 returns PIN_INVALID, permission 0x09 returns PIN_POLICY_VIOLATION. PIN_INVALID alone
does not establish either wrong PIN or forced change; no helper preflight or retry policy is implied.
This reopens B-05 / DOC-005 and related force-policy/error guidance, not a new defect count.

Independent original-source excerpts, the D-14 legacy-versus-permission correction dependency and
scope are preserved in [correction-b.md](correction-b.md). D-owned findings/source were not rewritten.
The inspected force-policy assertion calls GetPinTokenAsync (0x05); its expected PinInvalid is not
contradicted by the original 2.1 forced-change clause. Other D-14 setup/cleanup/coverage concerns remain.

Fresh static checks: exact 13 B-owned changed paths against baseline; only ClientPin's hash differs
from `review-b.md`; the other 12 hashes match. All non-comment nonblank lines remain identical to
baseline, 91 XML documentation blocks parse, and `git diff --check` exits 0. Current overall diff:
301 insertions / 148 deletions. Current ClientPin SHA256:
`5a937547f76796fd6c72b5ff7a01d60c428e10098474fda747928605ce0407c0`.
Current `git diff --binary` SHA256:
`6740b13faeae2eee6196fa307b2f64cd4c2aa73d911c31136e4542491d7a560f`.
No builds, tests, hardware, commits, delegation or source-copy integration occurred.
