# D fresh documentation edit — administrative operations

**Date:** 2026-11-03. **Editor:** GPT-6.1 Sol, requested independent editing subagent.
**Worktree:** `/tmp/opencode/ctap2-001-edit-d`.
**Baseline/HEAD:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
**Status:** edited; fresh independent review and coordinator integration pending.
Implementation and hardware status remain deferred/not run. No build, test, compiler,
formatting tool, device operation, commit, integration or delegation was performed.

Read root CLAUDE, available skills, module/test CLAUDE, relevant TESTING guidance,
SDK house style, canonical workflow/style, plan, inventory, triage, D (including leading
erratum), B, A and G investigations, and framework-adjudication. Applied workflow-execute
and workflow-verify for documentation execution and bounded local checks only; their
build/test/branch-finalization suggestions do not override this assignment's prohibition.
Loaded review-receive when fresh B review supplied the legacy force-change status correction;
verified the original clauses and actual ClientPin/test call before applying it.
Read all 20 owned files completely, including executable methods, fixture factories,
fake protocols, assertions and comments. Only those 20 source/test paths were edited;
this report is written in the PRIMARY packet, not integrated into the editor tree.

## Evidence and adjudication

Primary standard: [CTAP 2.1 Proposed Standard, 2022-06-21 errata](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html),
accessed directly again on 2026-11-03. Re-read original Sections 6.7/6.8/6.11,
7.1/7.2/7.4, with Section 6 stateful-command conditions and Section 8 canonical
encoding in the packet. Exact retained clauses/actor/strength remain in
[D primary quotes](evidence/d/primary-quotes.md), [B original clauses](evidence/b/original-clauses.md)
and A's primary-source record. No automatic upgrade to a newer CTAP publication.
Section 6.11's opening capability Note and Section 6.8.5's blob-cleanup Note are not
promoted into unconditional normative MUST claims. Standards are not weakened to fit bugs.

**D-08 emitted-wire defect is rejected.** Actual System.Formats.Cbor **10.0.2** sorts
closed Ctap2Canonical maps. The source inserts displayName first but emitted user keys
are id/name/displayName. Changed explanatory comments only; no broken-update warning,
invalid-fixture conclusion from insertion order, encoding fix or assertion change.
[Framework adjudication](framework-adjudication.md) is the authority for the pinned
source/binary evidence and its limits; it does not prove full update interoperability.
Context7 was queried for `.NET 10 CborWriter Ctap2Canonical automatic map sorting at
WriteEndMap` against `/websites/learn_microsoft_en-us_dotnet_api`; it returned **no match**.
That failed lookup is not used as evidence. No duplicate framework probe was run.

**D-14 legacy force-change wrong-status inference is also rejected.** Fresh B review
steering led to direct re-reading of the same original CTAP 2.1 errata:

- Section **6.5.5.7.1**, legacy **getPinToken (0x05)**: “If the value of the forcePINChange
  member of the authenticatorGetInfo response is true, authenticator returns
  CTAP2_ERR_PIN_INVALID error.” Its following Note explains backward compatibility.
- Section **6.5.5.7.2**, permission-based **getPinUvAuthTokenUsingPinWithPermissions (0x09)**:
  “If the value of the forcePINChange member of the authenticatorGetInfo response is true,
  authenticator returns CTAP2_ERR_PIN_POLICY_VIOLATION.” The platform SHOULD direct PIN change.
- Baseline `src/Ctap2/src/Pin/ClientPin.cs:263-304` shows GetPinTokenAsync sends
  ClientPinSubCommand.GetPinToken (0x05), not a permission-method call or negotiation.
  The D integration test's baseline lines 242-244 explicitly call GetPinTokenAsync and
  assert PinInvalid. That assertion agrees with the original legacy algorithm.

Config XML and test comments now state the subcommand/revision-specific statuses; neither
publishes an unqualified policy-violation promise for legacy acquisition. This does not
establish an executed test result or all-device behavior. Other D-14 normalization,
state/partial-restoration/cleanup and proof-scope findings remain. Coordinator owns the
investigation/triage erratum; this editor does not rewrite those historical packets.

The biometric request/response discrepancies remain genuine deferred work: standard
subcommands, sensor subcommand, modality authentication prefix, timeout parameter-map
placement, template-array decoding and no-enrollment status. Missing field defaults and
status/vendor interpretations remain candidly exposed, not called a vendor profile.
Retain the begin ID independently: later sample responses need not repeat it. Tests
that repeat the ID do not establish the omitted-ID case or real sensor persistence.

## Exact edited files and candidate mapping

All paths below are repository-relative and individually owned by D in inventory.json.
Every row is an edited complete-file disposition, not a hardware/conformance verdict.

| Exact path | Candidates / accepted groups | Changed semantic units |
| --- | --- | --- |
| `src/Ctap2/src/BioEnrollment/BioEnrollmentModels.cs` | D-02/03/04/16; DOC-010/011/009 | Status 0x0B-0x0E discrepancy; sensor defaults/unmodeled name maximum/local key 4; sample missing-field completion and begin-ID contract; flat-template/schema limits; no invented vendor provenance. |
| `src/Ctap2/src/BioEnrollment/BioEnrollmentSubCommand.cs` | D-01/04; DOC-010/011 | Exact standard-versus-local values, unused 8/provenance, no constant edits. |
| `src/Ctap2/src/BioEnrollment/FingerprintBioEnrollment.cs` | D-01/02/03/09/12/15/16; DOC-009/010/011 | Whole helper ownership/prerequisites/known wire limits; each method's actual boundary; null timeout and wrong placement; inventory/status conflation; explicit cancel distinct from .NET cancellation; begin-ID loop, notifications and bounded cleanup. |
| `src/Ctap2/src/Config/AuthenticatorConfig.cs` | D-05/06/07/09/12/16; DOC-006/009/011 | Borrowed protocol/token, helper always-auth versus standard exceptions, capability/state distinctions, fresh GetInfo/toggle outcome, enterprise operation versus ceremony, Unicode minimum/force/list semantics, exact Section 6.11 transcript and copy limits. |
| `src/Ctap2/src/Config/ConfigSubCommand.cs` | D-05/06/07; DOC-009/011 | Correct Section 6.11; active 1-3 retained; unused 4/5 not standard identifiers; precise alwaysUv/minimum meaning. |
| `src/Ctap2/src/CredentialManagement/CredManagementSubCommand.cs` | D-12; DOC-011 | Standard Section 6.8 provenance clarified; all seven identifiers/values preserved. |
| `src/Ctap2/src/CredentialManagement/CredentialEnumerationResult.cs` | D-09/10/16; DOC-009 | Copied/unvalidated first total, incomplete fallback interpretation, entry ownership/disposal and surviving independent public data. |
| `src/Ctap2/src/CredentialManagement/CredentialManagement.cs` | D-08/09/10/11/12/16; DOC-009/011 | Borrowed dependencies unlike ClientPin; Dispose marker not enforced; eager begin/next adjacency/total/failure contract; unbound/scoped permissions; raw UTF-8-RP hash routing; permanent delete/no blob cleanup; existing-handle name replacement; automatic canonical writer; final-request-only clearing. |
| `src/Ctap2/src/CredentialManagement/CredentialManagementModels.cs` | D-03/10/11; DOC-009 | Remaining count estimate, missing-count defaults, display truncation versus original hash, first-response total and no hash-length enforcement. |
| `src/Ctap2/src/CredentialManagement/RelyingPartyEnumerationResult.cs` | D-10; DOC-009 | Total copied rather than recomputed/validated, null not proof of completeness. |
| `src/Ctap2/src/CredentialManagement/StoredCredentialInfo.cs` | D-11/16; DOC-009 | Encoded public key not private key; copied total; caller-clearing of retained secret copies; exact owned-view versus independent-field/input/transport disposal boundary. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2AuthenticatorConfigTests.cs` | D-07/14/16; DOC-006/012 | Default normalization before option checks, nondecreasing target not guaranteed increase, assertion-only minimum, legacy PinInvalid correct versus unexercised permission-token policy status, non-finally toggle restoration and partial PIN recovery/cleanup. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2BioEnrollmentTests.cs` | D-12/15/16; DOC-010/012 | Sensor-only positive hint/kind checks; normalization side effects, presence versus provisioned state; no enrollment or qualification. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredentialManagementExtendedTests.cs` | D-13/14/16; DOC-012 | Display-name-only update and two-ID presence assertions; narrow version gate, broad shared-RP cleanup, default normalization and success-only token clearing. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredentialManagementTests.cs` | D-13/14/16; DOC-012 | Unsupported callbacks return normally, enumeration/metadata assertion scope, deletion success branch does not fail, accepted NoCredentials fallback, partial cleanup/normalization. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/BioEnrollmentModelsTests.cs` | D-02/03/04/15; DOC-010/012 | Synthetic/flat schema, local unproven field interpretations, supplied zero/count/status versus sensor/completion proof. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/FingerprintBioEnrollmentTests.cs` | D-01/02/03/15; DOC-010/012 | Actual modality-only/timeout fixtures; InvalidCommand conflation; begin-only Authenticate observation; send-count-only rename/remove/cancel; repeating-ID simulation; fake crypto limits. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Config/AuthenticatorConfigTests.cs` | D-07/15; DOC-009/012 | Fake version-2/16-byte tags, prefix versus crypto/device proof, map-size-only RP/force tests and local numeric acceptance. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CredentialManagement/CredentialManagementModelsTests.cs` | D-08/16/17; DOC-011/012 | Automatic canonical sorting, opaque incomplete COSE fixtures (not insertion-order defects), narrow clone/input proof, integer 2 is optional UV with credential-ID list rather than required UV. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CredentialManagement/CredentialManagementWireTests.cs` | D-10/15/16; DOC-009/012 | Selected metadata/delete fields, totals/order/wrappers, continuation errors/input preservation versus inaccessible-entry cleanup or global sequencing; no update transcript proof; fake-tag limitation. |

`DOC-*` abbreviates `CTAP-DOC-*`. Candidates map to accepted DOC-006/009/010/011/012
only. No APIs, assertions, codes, fixtures, executable strings, imports or directives
changed. Patch application added terminal newlines to the 20 baseline files that lacked
them; this is whitespace-only, not an executable token or import-order change.

## Snippet classifications and typed prerequisites

- **0 XML `<code>`/`<example>` blocks and 0 consumer-code fences** exist in these owned
  C# paths; none were introduced. No current consumer snippet requires a compiler check.
- Inline protocol transcripts and key sequences in prose/comments are **wire-format
  descriptions**, not executable C# examples. Standard/local discrepancy labels are retained.
- The five unit-test files' executable bodies/fixture factories are **existing synthetic
  test evidence**, not public composable snippets or enrollment instructions. Types are
  supplied by their original test projects: ICtap2Session mock, IPinUvAuthProtocol fake,
  ReadOnlyMemory<byte>/byte[] CBOR fixtures and xUnit/NSubstitute context. All unchanged.
- The four integration classes' bodies are **existing application/test-dependent hardware
  workflows**, not ready-to-run reader samples. Their typed entry is YubiKeyTestState;
  shared WithCtap2SessionAsync supplies an initialized Ctap2Session, with default known-PIN
  normalization before the callback. Physical authorized key, actual options, known PIN,
  human approval/presence and permanent-state awareness remain prerequisites. Not run.
- D's operational prose assumes an initialized Ctap2Session (CM public constructor) or
  ICtap2Session (bio/config), an authenticator-supported IPinUvAuthProtocol, and caller-owned
  writable byte[] token storage passed as ReadOnlyMemory<byte>. Keep objects alive/token
  unchanged through awaits and clear bytes after their final use, including failure.
  Metadata/RP enumeration need unbound cm; credential administration permits unbound or
  matching-RP cm. Built-in-UV be/acfg acquisition has additional capability conditions.
- Credential enumeration routes the returned raw 32-byte RelyingPartyInfo.RpIdHash, not
  possibly truncated display text; update keeps PublicKeyCredentialUserEntity.Id and
  replaces names. Bio guidance requires retaining begin's opaque TemplateId separately,
  but is explicitly limited by the current wire/schema defects, not an interoperability recipe.

## Genuine deferred work retained

The authoritative ledger remains [triage.md](triage.md); no deferred item was closed here.

| IDs | Work still needed / exact re-review trigger |
| --- | --- |
| CTAP-IMP-012/013/014/015 | Bio request/subcommand/transcript/timeout, array/status parsing, operation-aware required fields/begin-ID handling and status/vendor definitions. Reopen all three BioEnrollment sources, both unit files, sensor integration and F bio guides/menus/examples after real wire/parser evidence. |
| CTAP-IMP-016 | Config unused constants, omission/expressiveness and capability-policy decisions. Reopen both Config sources, config tests, E minPinLength and F config tasks. |
| CTAP-IMP-017 | **Rejected ordering-runtime inference**, not genuine fix work. Retain erratum/provenance; only canonical explanation changed. Re-review C/F canonical claims consistently; no update/fixture defect from insertion order. |
| CTAP-IMP-018/019 | CM disposed-helper enforcement, malformed totals/count/hash validity and whole-sequence policy. Reopen CM helper/results/models, wire tests, A exchange contracts and F enumeration/lifetimes. |
| CTAP-IMP-020/021/022 | Default PIN normalization/lane policy, secret teardown/partial restoration, deletion/fake protocol assertion gaps. **D14's legacy PinInvalid expectation is not a defect**; permission-token behavior is untested by that assertion, not proved wrong. Reopen four D integration classes and all five unit files plus A/B shared helpers/F test guide when executable behavior changes. |
| CTAP-IMP-024; dependencies IMP-004/006/010 | Admin owned payload/auth-tag/request cleanup, builder/storage ownership, Unicode and token-capability enforcement decisions. Reopen helper XML/internal cleanup and caller-token guidance, B PIN and A builder/raw sources, F security/examples. |
| CTAP-DEC-003 | Exact SKU/standard-versus-preview firmware and original vendor definitions remain missing evidence, not substituted coarse firmware guarantees. Reopen helper support/constant/model prose and F/A capability surfaces when evidence resolves. |

Removed historical narration (T12 parenthesis, previous minimum-increment/python pattern)
without removing its useful rationale: borrowed-token ownership, nonaccumulating test target,
and partial restoration remain explained. Missing meaningful assertions and exception-safe
cleanup are genuine work above, not erased to achieve a zero-TODO metric. No hardware
enrollment, deletion, config, PIN change or reset was attempted as documentation validation.

## Cross-owner dependencies — no edits outside D

- **A:** `src/Ctap2/src/ICtap2Session.cs`, `Ctap2Session.cs`, `AuthenticatorInfo.cs`,
  `Cbor/CtapRequestBuilder.cs`, both backends; integration `Ctap2TestHelpers.cs` and
  `TestExtensions/Ctap2TestStateExtensions.cs`. Keep single-exchange guard versus whole
  state sequence, fresh info/snapshot, bio/ep/alwaysUv options and default normalization/
  failure-cleanup claims aligned. No new rollback or session-discard policy.
- **B:** `src/Ctap2/src/Pin/ClientPin.cs`, `IPinUvAuthProtocol.cs`,
  `ClientPinSubCommand.cs` and retry/status models. Preserve ClientPin-owned protocol
  versus D-borrowed protocol; command fallback is not negotiation or scoped authorization;
  force-change status depends on actual subcommand: legacy 0x05 PinInvalid versus
  permission-based 0x09 PinPolicyViolation in the original CTAP 2.1 errata. Permission-method
  fallback can select legacy 0x05, so API name alone cannot determine which status applies.
- **C:** `src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs`,
  `Cbor/Ctap2SessionRequestEncoding.cs`; enterprise/FIPS integration explanatory units.
  Canonical maps are sorted by the writer, not numeric/alphabetical insertion. Preserve
  user handle and standard-versus-test evidence distinctions; no D-08 runtime inference.
- **E:** `src/Ctap2/src/Extensions/CredProtect/CredProtectPolicy.cs`,
  `Extensions/MinPinLength/MinPinLengthInput.cs`, `MinPinLengthOutput.cs`,
  `LargeBlobs/LargeBlobStorage.cs`/`ILargeBlobStorage.cs`, minPIN integration tests.
  Policy 2/3, reporting not setting minimum, mutable-list replacement and separate platform
  blob deletion/GC remain aligned. No automatic blob cleanup or invented normative MUST.
- **F:** `src/Ctap2/README.md`, `CLAUDE.md`, `PACKAGE_README.md`, `tests/CLAUDE.md`,
  `examples/FidoTool/FidoExamples/{BioEnrollment,ConfigManagement,CredentialManagement}.cs`,
  `Cli/Menus/{FingerprintsMenu,ConfigMenu,CredentialsMenu}.cs` (and the adjacent
  `CredentialMenu.cs` where its actual task applies). Complete tasks need D limits,
  retained begin ID, known-PIN setup/partial restoration, borrowed tokens, total/hash
  routing and fresh state; executable sample fixes remain deferred.
- **G:** boundary-map administrative capability/lifetime/config/PIN-policy/biometric
  rows and final CTAP/WebAuthn review. Higher-level token/prompt orchestration can differ;
  no device qualification or browser/RP conformance follows from helper/fake agreement.

Exact candidate-level triggers and immutable investigation provenance remain in D's
documentation-impact map. That map's original D-08 encoding-defect statements are
superseded by its leading erratum and framework adjudication, not silently rewritten here.

## Local diff check and validation boundary

Executed in the editor worktree:

1. `git status --short` and `git rev-parse HEAD`: initially clean; exact baseline above.
2. `git diff --check`: exit **0**, no whitespace-error output.
3. Read-only Python (`python3 - <<'PY' ... PY`) selected owner D from PRIMARY inventory,
   asserted changed-path equality (exact 20/20), read each `git show HEAD:<path>`, lexed
   ordinary/verbatim string and char literals before stripping line/block comments,
   compared remaining non-whitespace token sequences, compared all preprocessor lines,
   and structurally parsed every contiguous `///` block wrapped in a `<doc>` element.
   Exit **0**: 20/20 executable/directive sequences identical; **129 XML blocks parsed**.
   This is a local text/XML check, not Roslyn API/XML-cref validation or compilation.
4. `git diff --stat` and `git diff --unified=2`: inspected the complete 20-file diff,
   including both portions of the retained output. Follow-up wording clarifies that a
   standard next sample *need not* repeat the ID, not that repetition is forbidden.
   Fresh B steering additionally corrected the false D14 status inference after original
   Section 6.5.5.7.1/2 and baseline GetPinTokenAsync/test-source verification.
5. Final preservation/XML/diff checks are rerun after that correction; editor handoff
   reports their actual result. No import-order or runtime/directive exception is claimed.

**Final rerun result:** exit **0**; HEAD still baseline, exact **20/20 D paths** changed,
all comment-stripped executable token/string/assertion/import sequences and directives
identical, **129 XML blocks** structurally parsed, `git diff --check` exit **0**.
Final source diff (`git diff --no-ext-diff --no-color`, UTF-8 text) SHA-256:
`795ecb234cb9a18b7992f467c6cef7aa37968d9b8d506af36dbe7b398883912d`.
Final stat: **20 files, 501 insertions, 206 deletions**; source edits are XML/comments
plus terminal-newline whitespace. Final config/test diff was read after status correction.
No subsequent source edit was made by this editor.

No build/unit/docs/architecture/complexity/scoped-format/example-compilation/hardware
gate was run here. The coordinator's final gates and fresh independent review remain
required; the framework report's previous results are not this editor's passing gates.

## 2026-11-04 fresh-review correction checkpoint

[review-d.md](review-d.md) accepted the remaining D scope, including D08's withdrawn
runtime-ordering inference and D14's correct legacy force-change status, with one
required local comment correction (RD-R1) and one optional clarity change (RD-O1).
Both were verified against actual source and applied without executable edits; see
[correction-d.md](correction-d.md). Prior diff/hash results above identify the earlier
review target and remain historical, not the current corrected checksum.

| Review ID / candidate mapping | Exact path / unit | Current SHA-256 |
| --- | --- | --- |
| RD-R1; D-15; CTAP-DOC-010/012; existing CTAP-IMP-022 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/FingerprintBioEnrollmentTests.cs:213`: supplied count, only TooFast status asserted; integer 3/assertions unchanged | `942b123fa87c844f45e41f7719b7b55ae873fefad67f963c91de6e0819dafd8a` |
| RD-O1 optional accepted; D-13; CTAP-DOC-012 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredentialManagementExtendedTests.cs:29`: credential enumeration for one RP, not RP enumeration | `77908964a69218454942a89bdf6ac216998e71ce46f4e79e89c9c4abfe3fe390` |

Other **18 reviewed hashes remain byte-identical**. Fresh local checks: exactly two
comment substitutions since review, all 20 baseline executable/directive sequences
preserved, 129 XML blocks structurally parsed, `git diff --check` exit 0.
Current source-diff SHA-256: `17cd7c64500e8052c9fcee1efdba31d89d340e36e90828442ef0c53398c5b250`;
stat **20 files, 502 insertions, 207 deletions**. Follow-up review of these two changed
units remains pending; no build/test/hardware/commit/delegation/integration occurred.
