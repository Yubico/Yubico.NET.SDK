# Editor A — session, metadata, CBOR and shared-test documentation

Date: **2026-11-03**. Fresh editor: GPT-6.1 Sol, assigned subagent; no delegation.
Worktree: `/tmp/opencode/ctap2-001-edit-a`. Baseline/HEAD:
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. Source edits exist **only in this worktree**;
the coordinator must integrate after fresh independent review. Status: **edited / local mechanical
checks passed / independent review pending**, not module closeout or implementation completion.

## Inputs and scope

Read canonical root CLAUDE, CTAP module/test CLAUDE, applicable testing instructions, available skill
descriptions, campaign workflow/style, current run plan/triage/inventory and investigations A/B/C/G.
Later coordinator steering supplied `framework-adjudication.md` and `evidence/framework`; these
were read and reconciled below before the final checks. Their probe was not rerun by this editor.
The packet's `plan/triage/investigation-*.md` shorthand resolves to the actual run-root files;
there is no nested `plan/triage/` directory. All 30 A-owned C# files and complete affected test
assertions were inspected. Narrow Core source reads corroborated creation/configuration/ownership,
Feature sentinel treatment, exchange admission and transport cancellation. No Core files were edited.
Loaded `workflow-verify` for completion-evidence discipline. Build/test/commit skills and operations
are inapplicable to this explicitly prohibited-work packet: **no builds, tests, hardware, formatting,
SDK execution, commits or delegation occurred**.

Applied accepted **CTAP-DOC-001/002/003/011/012** and **C-013** entry-point handoff. UV/token
guidance also incorporates the already accepted B-13/G-003 distinction without changing encoding.
No API, executable token, directive, assertion, attribute or executable string was intentionally changed.
The patch tool added final newlines to edited files previously lacking them; this is whitespace only.
No import-order changes occurred. XML example text was corrected, which is within documentation scope.

## Exact finding-to-file mapping

Paths below are repository-relative and are the complete **26-file** source diff manifest.
DOC numbers abbreviate `CTAP-DOC-` only in this table.

| Exact path | Mapping and edited semantic unit |
| --- | --- |
| `src/Ctap2/src/AuthenticatorInfo.cs` | DOC001; A-004/005/006: snapshot and absence representation; capability/configured-state options and exact keys; protocol preference order, message-size default, estimate/code-point units, YubiKey firmware interpretation, optional reset fields; encrypted device/store identifiers and persistent-token distinction; Decode copy/validation bounds including unread trailing bytes despite single-root map completion; shared credential parameter type/algorithm interpretation. |
| `src/Ctap2/src/Backend/HidBackend.cs` | DOC002/003; A-002/007/009/010: borrowed protocol, HID wait notification, terminal CTAP status before typed decode, transfer/clearing bounds and synthesized Other. |
| `src/Ctap2/src/Backend/ICtap2Backend.cs` | DOC003; A-007: borrowed protocol lifecycle and exchange versus model/RP boundary. |
| `src/Ctap2/src/Backend/SmartCardBackend.cs` | DOC002/003; A-002/009: pre-transmit policy notification, status/decode/transfer boundary, broad SELECT ApduException wrapper retaining original cause. |
| `src/Ctap2/src/Cbor/AaguidConverter.cs` | DOC001/011; A-014/CTAP-IMP-025: hold lifted by coordinator framework evidence. Correct machine-dependent serialization rationale: default Guid byte convention is mixed-endian independent of native memory layout, while current SDK component reversal is conditional. Explain skipped big-endian conversion calmly; preserve exact 16-byte/copy/output ownership. No executable conversion change or big-endian qualification. |
| `src/Ctap2/src/Cbor/CtapRequestBuilder.cs` | DOC003/011; A-007/008: immediate private copies, replacement without clearing, delayed/repeated callbacks, complete map/array wrappers, caller-owned output and bounded intermediate clearing; numeric insertion versus automatic encoded-key sorting at map completion. No runtime-order bug claim. |
| `src/Ctap2/src/Cbor/CtapResponseParser.cs` | DOC003; A-016: handlers consume complete values, nullable memory is a same-array view, optional readers consume the current value/null/undefined rather than detecting absent keys. |
| `src/Ctap2/src/Ctap/CtapException.cs` | DOC002/011; A-009: wire/synthetic/caller status provenance, separate failure layers, no rollback inference. |
| `src/Ctap2/src/Ctap/CtapStatus.cs` | DOC002/011; A-009 and B-06 dependency: original-revision 0x38 and 0x36 names, minimum-length versus later complexity policy, extension range, original-request cancellation status, continuation/late-reset state, temporary versus exhausted PIN block. All constants/messages unchanged. |
| `src/Ctap2/src/Ctap2Session.cs` | DOC001/002/003; A-003/004/007/010 and C-013: actual optional-feature/firmware threshold scope, direct borrowed connection/options/prompt/SCP lifetime, exact detected gate versus configured override, initialization order and state snapshot, macOS active-disposal uncertainty; request-only finally clearing, policy notification, decode-after-success and caller-managed next-assertion count/sequence. |
| `src/Ctap2/src/ICtap2Session.cs` | DOC001/002/003; A-001/002/003/004/007/010 and C-013: local task responsibilities, pre-cancel/overlap/drain/raced success/no deadline/no rollback; raw prefix/schema/authorization/4-level/device-size obligations and successful view ownership; fresh info versus fixed session state; selection orchestration; destructive reset/product/revision clocks and Bio limitation; exact serialized-client-data hash, RP verification/account binding, borrowed inputs/request-only clearing, built-in UV versus token prerequisites, scoped exclude/discoverable search, N−1 continuation rules and exact basic argument checks. |
| `src/Ctap2/src/IYubiKeyExtensions.cs` | DOC001; A-004/015: two GetInfo exchanges in one-shot helper, selected transport versus failure retry, hidden connection ownership, caller-owned SCP/prompt, exact override/gate bounds; XML example uses nonoverlapping ordinary/SCP session scopes and approved provisioned-device prerequisites. Removed inaccurate quoted SCP exception text in XML only. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2GetInfoTests.cs` | DOC012; A-011/013: default PIN work before metadata, not read-only smoke; optional/conditional assertions, tautological extension disjunction, nonzero-AAGUID test assumption. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2SessionSimpleTests.cs` | DOC012; A-011/013: helper versus direct setup distinction, no safe-lane inference from touch filtering, exact conditional algorithm/option/extension checks, selection completion only and generic CtapException reset assertion not clock/no-deletion proof. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2SmartCardTests.cs` | DOC012; A-011/013/009: default normalization before GetInfo, exact first-test field assertions, skip does not prove precise SELECT failure cause. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestData.cs` | DOC012; A-013/C-013: product-configured PIN policy, known immutable test constant not production prompting; random32 is a stand-in hash, not client-data/challenge construction. Removed comment-only CLI PIN-setting recipe. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestHelpers.cs` | DOC012; A-012/B-02/03: known-PIN setup, token request even on match, helper/protocol/session/token ownership, failed-return cleanup limitation, version-string heuristic versus actual authorization, broad RP cleanup/no transaction and caller-owned Authenticate result. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TransportTests.cs` | DOC012; A-012/013/C-013: normalization/secret cleanup limits, returned fields and nonempty signature rather than attestation/RP verification, selected SmartCard applicability, broad RP cleanup, diagnostic PIN-block conflation qualified without editing its string. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/IntegrationTestBase.cs` | DOC012; A-013: empty class adds no utilities; discovery belongs to shared infrastructure. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs` | DOC012; A-011/012/013: after-creation/default-per-call normalization, explicit approved-PIN lane, matching-PIN token exchange and mismatch retry, force-change known→temporary→known partial restoration and success-only clearing, uncleared discarded token, diagnostics not coordinated reset procedure, info overload passes a fresh snapshot rather than returning info. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuthenticatorInfoTests.cs` | DOC012; A-005/006/013: synthetic incomplete parser fixtures, deliberately duplicate transport not conforming standard advertisement, arbitrary encrypted-field bytes prove retention not decryptability. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2SessionHidCrossLayerTests.cs` | DOC012; A-006/010/013: scripted fake bridge/drain/disposal scope, test wait bound not SDK deadline, versions-only probe omits required AAGUID. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2SessionTests.cs` | DOC012; A-006/013: present borrowed-connection rationale instead of upstream history, false-UP assertion is after initialization I/O, incomplete fake GetInfo scope. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CtapExceptionTests.cs` | DOC011/012; A-009/013: constants-only assertion does not prove all revisions assign legacy0x38. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CtapRequestBuilderTests.cs` | DOC012; A-008/013: prefix/length assertions do not establish schema/order; key/value decoding does not measure allocation/copy absence. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/IYubiKeyExtensionsTransportTests.cs` | DOC012; A-013: fake connect-probe scope, SmartCard selection on absent HID not failure fallback, resolve-only remap rationale instead of phase history. |

**2026-11-04 follow-up mapping:** `src/Ctap2/src/AuthenticatorInfo.cs` additionally addresses
**A-REV-001** and coordinator **COORD-001** under A-006/CTAP-IMP-001. Intro null semantics are
restricted to optional numeric/Boolean/text fields; missing required AAGUID is explicitly empty;
both encrypted metadata properties and Decode explain absent-field conversion to present empty
memory and RawData wire-presence inspection. Source assignments remain unchanged. Details and
new E/F/G dependencies: [correction-a.md](correction-a.md). Initial reviewer snapshot is preserved
in review-a.md; correction re-review is pending, not declared passed by the editor.

Four owned paths remain unchanged after inspection:
`src/Ctap2/src/Ctap/CtapCommand.cs` (assigned command/prototype labels),
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/UserPresenceProbeTests.cs`
(coordinated notification/reuse assertions, not execution evidence),
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cbor/CtapResponseParserTests.cs`
(selected current-value fixtures), and
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2BackendLifecycleTests.cs`
(borrowed-protocol/status-view and untransferred-buffer assertions).

## Snippet classifications and typed prerequisites

The only `<code>` blocks in A-owned files are in `src/Ctap2/src/IYubiKeyExtensions.cs`.
No complete standalone application, hardware run or RP-verification example is claimed.

| Snippet | Classification | Typed and operational prerequisites / validation status |
| --- | --- | --- |
| `GetCtap2InfoAsync` example | Composable async fragment, unchanged executable example text | `IYubiKey yubiKey`; `System` (`Console`, `Convert`), LINQ (`Contains`), and CTAP2 extension namespace/package. Available supported connection; version output is advertisement only. Static XML inspection only; not compiled/run. |
| `CreateCtap2SessionAsync` ordinary scope | Composable async fragment, corrected lexical scope | `IYubiKey yubiKey`, CTAP2 namespace/package, async context with async disposal. A supported exposed connection; scope ends before another physical-key connection opens. Static inspection only. |
| `CreateCtap2SessionAsync` SCP03 scope | Alternative composable async fragment, not a universal default-security recipe | Same `IYubiKey`, plus Core `Scp03KeyParameters`, `SessionCreationOptions`, `ConnectionType` namespaces/types. Explicitly authorized SmartCard device provisioned for supplied default keys, FIDO AID/current transport gate, caller-owned keys enclosing session disposal. No PIN/UV permission follows from SCP. Static inspection only; meaningful offline compilation remains coordinator work. |

Tests and known-PIN helpers are test-specific fixtures/utilities, **not production examples**.
Commented diagnostics remain executable strings and were not corrected. No invented application
upload/verification API, serialization token or new snippet execution was added.

## Original evidence rechecked and held questions

Fresh reads of original CTAP2.1 June21 2022 errata §§6.3/6.4/6.6/8.2,
CTAP2.3 February26 2026 Proposed Standard §§6.3/6.4/6.6/8.2, CTAP2.0 January30 2019
legacy token text and Yubico's FIDO2 reset product guide corroborate next-count/timeouts, stateful
GetInfo, required AAGUID, option state, reset effects/clock separation, status provenance and encrypted
device/store identifiers. Exact URLs remain in source XML and original investigations; no revision
was upgraded to imply broader conformance. Vendor reset API examples are legacy and were not imported.
This editor's Context7 .NET API query for `CborConformanceMode.Ctap2Canonical` returned no matching
documentation and was not treated as evidence. The subsequently supplied framework adjudication
resolves sorting and reader/Guid questions using the actual System.Formats.Cbor10.0.2 binary probe
and pinned official source. Read `probe.log`, writer map sorting/completion source, ReadEndMap source,
and runtime v10.0.14 default Guid constructor/ToByteArray/TryWriteBytes source. The editor did not
rerun the probe and makes no new provider/writer-memory erasure guarantee.

- **A-014 / CTAP-DEC-001 resolved as CTAP-IMP-025:** pinned official default Guid byte APIs
  compensate for native machine order and preserve mixed-endian serialization. Current SDK branches
  skip required reversal on a hypothetical big-endian runtime. Portability rationale and current
  limitation are now documented; conversion code/tests remain unchanged. Coordinator's little-endian
  fixture passed; **no big-endian execution or affected supported deployment was established**.
- **C-008 / D-08 canonical ordering resolved:** actual Ctap2Canonical writer automatically sorts
  key encodings at map completion. Numeric insertion is not wire order; D-08/CTAP-IMP-017 runtime
  diagnosis is rejected, not an outstanding serialization limitation. Builder comments name the
  real sorting mechanism. No encoding writes/conformance modes/fixtures were changed.
- **A-006 trailing envelope subcase:** framework fixture A0 01 leaves BytesRemaining=1 after
  ReadEndMap even when PeekState is Finished and multiple roots are disabled. AuthenticatorInfo
  does not perform explicit remaining-byte validation; its public Decode qualification now makes
  this precise. No SDK decoder was executed here; this does not establish that every malformed
  trailer is accepted or that nested reader-based parsers should require whole-buffer exhaustion.
- **Reset product gap / CTAP-DEC-003:** vendor guide explicitly says >=5.5.4 ten seconds and <5.5.2
  five seconds; 5.5.2/5.5.3 remains unclassified. Bio Multi-protocol individual-reset blocked state
  can require alternate device-wide reset. No timer, reset preflight, product API or live qualification
  was added. F owns the complete product procedure and executable diagnostic dependencies.
- **E/B encrypted metadata:** public metadata meaning/token relationship is corrected; exact SKU/
  firmware availability and decryptor crypto/exception conditions remain E/B-owned prerequisites.
  Removed blanket 5.7/5.8 availability promises instead of treating thresholds as qualified support.

## Cross-owner dependencies / integration prerequisites

- **C:** no `GetAssertionResponse.cs` or other C-owned file edited. C owns User/descriptor semantics,
  standalone response/version/context validation, response secret ownership, option XML and RP-helper
  limits. Reconcile C-001/002/003/004/005/006/010/013 with interface text; preserve WA-XREF-001 and
  WA-IMP-010. A's parameter class is physically in `AuthenticatorInfo.cs` and stays A-owned.
- **F:** no guides, module/test CLAUDE, shared docs or FidoTool paths edited. Reconcile exact transport/
  SCP alternatives, detected/configured firmware, reset procedure, raw byte/cancellation knowledge,
  clientDataHash/RP task flow and non-read-only default normalization throughout those surfaces.
  Existing executable reset/PIN instructions and command strings remain deferred under token freeze.
- **B/D/E:** reopened state/ownership dependencies remain explicit. PIN changes/reset can invalidate
  token state; GetInfo must not interleave administrative continuations. Shared helpers' discarded
  tokens/partial restoration and broad-RP deletion remain implementation findings, not repaired code.
- **G:** fresh final CTAP/WebAuthn boundary review must preserve legitimate higher-level orchestration
  and existing WA-IMP-002/003/009/010/013 provenance; do not infer parity, rollback, RP verification,
  browser serialization or full feature support from this source-only edit batch.

## Local checks and limits

Read-only mechanical check command (no SDK/tests executed):
`python3 /tmp/opencode/ctap2-001-edit-a-check.py`.
Latest output: **exit0**, all **30/30 baseline inventory hashes** matched, only A-owned paths
changed, executable/string lexical token sequences and directives matched in **26/26 edited C# files**,
and **218 XML comment blocks** parsed. `git diff --check`: **exit0**.
The checker is a bounded Python lexical comparison, not Roslyn/compiler/API-snapshot verification;
the coordinator still owns the repository's final preservation/build/docs/example gates.
Full affected diffs were self-inspected; this is **not independent review**.

Current worktree diff after the 2026-11-04 correction: **26 files, 490 insertions, 207 deletions**
including harmless EOF newlines. The initial review snapshot was 481/207 and is superseded only
by the AuthenticatorInfo documentation correction recorded above.
SHA-256 of `git diff --binary`:
`b692d2fe56fc7101131f454098a084c9804491a0d9702ca45e75271867aefd42`.
No builds/tests/hardware/commits or platform/ceremony/support qualification. Reviewer and coordinator
must use this exact source diff, resolve held questions, integrate selected changes and rerun applicable
authorized gates after all owners' edits.
