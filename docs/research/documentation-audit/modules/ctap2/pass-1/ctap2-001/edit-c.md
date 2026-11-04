# C — credential documentation edit handoff

**Date:** 2026-11-03; final local checks/hand-off 2026-11-04 after date rollover.
**Editor:** GPT-6.1 Sol. **Status:** edited, awaiting fresh independent review;
not integrated, compiled, tested or hardware-qualified. Editor checkout:
`/tmp/opencode/ctap2-001-edit-c`, baseline
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. Checkout was clean on entry. Only the 30 inventoried
C paths were eligible for source editing; **27 changed, three intentionally unchanged**. This report
is written directly to the primary packet, not copied with source into the integration checkout.

**2026-11-04 review correction:** [review-c.md](review-c.md) requires RC-R1/R2. The original
ARKG investigation's claim that draft-10 does not support seed placeholder -65700 was false.
Fresh §§5.1/5.2/8.3 evidence establishes that value; BL/KEM labels and the derived -9 example also
agree. The actual profile differences are kty2 versus -65537, wrapped versus direct maps, and required
integer versus optional int/tstr dkalg. Three C source/test files received comments/XML-only corrections,
including accurate AA/AB/BB/BC fixture patterns and accepted RC-O1/O2 clarity suggestions. See
[correction-c.md](correction-c.md) for evidence, hashes and local checks. Original investigation/evidence
and review are preserved; correction applied is not yet a fresh follow-up PASS verdict.

Read root/module/test CLAUDE, available skills, canonical workflow/style, SDK house style and testing
guidance; loaded execute-plan, security-guidelines and verify. Read plan, inventory, triage,
investigations C/B/G/E and framework-adjudication, including the framework source supporting borrowed
`ReadEncodedValue` views. Read all 30 original owned files completely, including method bodies,
private helpers and assertions. Narrow dependency reads covered Core reader/options/buffer ownership
and session make/get/next behavior; no dependency source was edited.

**No builds, tests, hardware/PIN operations, commits, delegation, source-copy integration, API changes,
assertion changes, executable strings, directives or executable-token edits occurred.** Local Python
checks below are diff/XML inspection, not SDK tests or compiler gates. Coordinator Roslyn/API/build
gates and the fresh review remain outstanding.

## Accepted change mapping and exact changed paths

All paths below are repository-relative and are the exact editor diff paths. DOC abbreviations expand
to `CTAP-DOC-`; C candidate provenance retains the investigation's IDs rather than minting engineering
IDs. Supplemental implementation IDs are still coordinator-owned/reconciled. A owns session/interface
method edits; F owns Markdown/guide/example journeys. No A/F/WebAuthn source was changed here.

| Exact changed path | Accepted group / candidate mapping | Resulting documentation contract |
|---|---|---|
| `src/Ctap2/src/Cbor/CoseKeyWriter.cs` | DOC016; C-008; framework adjudication | Numeric insertion versus automatic canonical writer sorting; no wire-order defect alleged. |
| `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs` | DOC007/016; C-003/004/013 | Pure encoding, borrowed inputs/new request copy, explicit false versus omission, independent uv/auth and non-negotiating version-2 default. Duplicate/preference-order rationale preserved. |
| `src/Ctap2/src/Cose/CoseAlgorithm.cs` | DOC016; C-007/009 | ESP256 P-256 versus ES256K secp256k1; WebAuthn versus generic ES/EdDSA profiles; SDK recognition versus registration/support; distinct experimental signing/seed markers without future-stability claim. |
| `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs` | DOC016; C-004/008/009; RC-R1/O1 | Actual wrapped-map emission/direct-map acceptance; draft seed -65700, BL/KEM and derived -9 agreement versus kty/wrapping/dkalg differences; encoded-map byte-array wording; borrowed inputs/layout versus validity. Strong non-production warning retained. |
| `src/Ctap2/src/Cose/CoseKey.cs` | DOC016; C-004/005/006/008/009 | Carrier/required modeled fields versus math/trust validation, Other copies versus direct construction borrowing, unknown-field scope, no remaining-byte check, writer sorting. Separate strict PIN/UV protocol obligations retained. |
| `src/Ctap2/src/Credentials/AttestationFormat.cs` | DOC016; C-007 | None is no attestation information, not self; identifier recognition is not typed decoding/verification. |
| `src/Ctap2/src/Credentials/AttestationStatement.cs` | DOC016; C-004/005/006/007 | Typed/opaque support and field/constructor ownership XML; signatures/certificates versus verification, raw statement borrowing, none empty-map requirement versus absent enforcement. |
| `src/Ctap2/src/Credentials/AttestedCredentialData.cs` | DOC016; C-004/006 | Copied ID/key, one-value extraction rather than COSE verification, bytesRead scope, bounded ArgumentException/CBOR error descriptions. Binary offsets retained. |
| `src/Ctap2/src/Credentials/AuthenticatorData.cs` | DOC016; C-004/005/006 | Independent copies, ED suffix extraction/trailing behavior, received bits versus trusted events, credential BE/BS, counter risk policy and limited hash comparison. |
| `src/Ctap2/src/Credentials/Ctap2CredentialOptions.cs` | DOC006/016; C-012, B-04 | Actual console UTF-16 char-count preset and UTF-8 conversion, not code-point/byte/current-device policy validation; returned reader-owner disposal. |
| `src/Ctap2/src/Credentials/GetAssertionOptions.cs` | DOC007/016; C-003/004/013 | Built-in UV versus token authorization, omit uv key even false with auth, device token state, caller-owned buffers, allow-list/extension absence, silent-request limits. |
| `src/Ctap2/src/Credentials/GetAssertionResponse.cs` | DOC008/016; C-001/002/004/005/006/010/013; RC-O2 | User handle versus UV-gated display fields, modeled name/displayName passthrough versus skipped icon; exact CTAP2.0 singleton allowList exception and later required descriptor; nullable/fallback/next-count semantics; copies, no cleanup API, ignored unsigned key8, parsing versus RP verification. |
| `src/Ctap2/src/Credentials/MakeCredentialOptions.cs` | DOC007/016; C-003/004/013 | Token/built-in UV exclusivity and actual missing enforcement; input lifetimes, discoverable/exclude/enterprise prerequisites, device authorization not logged-in DTO state, protocol default/absence. Existing up=false rejection preserved. |
| `src/Ctap2/src/Credentials/MakeCredentialResponse.cs` | DOC016; C-004/005/006/010 | Command-specific unsigned key6, raw encoded values/borrowed slices, empty/omitted equivalence, copied secrets and no disposal, unknown outer loss, minimum parser checks versus RP verification. |
| `src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs` | DOC008/016; C-001/004/013 | Borrowed constructors/materialized parsing, user handle/account responsibilities, optional response display fields and SDK-only constructor restrictions, RP origin authorization outside constructor, transport hints not reachability. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/AuditV2/Ctap2RequestAuditReproTests.cs` | DOC012/016; C-014 | Rejection before this makeCredential command, not absence of initialization/GetInfo/PIN-normalization traffic; exact exception assertion scope. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2AlgorithmSupportTests.cs` | DOC012; C-011/015 | Requested algorithm versus unasserted returned alg/curve, early normal return, synthetic hashes, approved device/PIN/UP prerequisites, best-effort deletion and incomplete token cleanup. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EnterpriseAttestationTests.cs` | DOC012; C-011/015 | ep=false is disabled support, unchanged misleading skip diagnostic qualified, conditional epAtt versus certificate evidence, persistent config and broad RP deletion/no restoration. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2FipsComplianceTests.cs` | DOC012; C-011 | Metadata/optional/early-return assertions, no FIPS qualification gate, clientPin key existence versus true, setup normalization qualification. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2GetAssertionTests.cs` | DOC012; C-011/015 | Nonempty signature versus crypto verification, meaningful exact-handle/status assertions retained, synthetic hash/prerequisites/cleanup bounds. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2MakeCredentialTests.cs` | DOC012; C-011/014/015 | Field/ID versus attestation trust/discovery/AAGUID presence, configured-PIN discoverable request scope, four accepted statuses not firmware qualification, blocked-state/string caveats and cleanup bounds. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cbor/Ctap2SessionRequestEncodingTests.cs` | DOC007/012; C-003/011 | Full all-options fixtures are mechanical non-ceremony evidence; intentionally short fake tags, uv+token and synthetic extension inputs remain unchanged. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs` | DOC012/016; C-008/009/014; RC-R1/R2 | Writer canonicalization, explicit draft agreement/profile differences, exact synthetic BL X=AA/Y=AB and KEM X=BB/Y=BC, useful role routing retained; no draft-wide conformance, firmware-error or FullCeremony evidence claimed. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/AuthenticatorDataTests.cs` | DOC012; C-011 | Parsing/bit/hash comparison, AT and ED enum-only coverage distinguished from parser tests. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/CredentialResponseTests.cs` | DOC008/012/016; C-002/011/014 | Synthetic signatures/zero-coordinate keys, missing descriptor permissiveness, actual 37-byte prefix plus AT payload, correct WebAuthn attestation references, map-header-only check. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/PublicKeyCredentialTypesTests.cs` | DOC007/012; C-003 | Fluent setter-storage fixture is not a valid uv+token ceremony; no assertions changed. |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/MakeCredentialResponseTests.cs` | DOC012/016; C-010/011 | Key6 map-value capture and ignored make key8, distinct getAssertion key8; no general CTAP4/firmware/previewSign proof. |

### Exact unchanged C paths

Complete files/assertions read; no necessary comment edit within their existing explanatory units:

- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuditV2/Ctap2RequestAuditReproTests.cs` — C-CL-01/08:
  rejected duplicate parameters, preserved preference order and actual false-up wire observation.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cose/CoseKeyForwardCompatibilityTests.cs` — C-CL-04:
  modeled/opaque identifier and required-field fixtures, not point validity or exhaustive strictness.
- `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Credentials/AttestationStatementTests.cs` — C-CL-10:
  explicit dummy signature/empty-map and raw-byte equality assertions; no trust claim.

These three plus the 27 exact changed paths exhaust the inventory's 30 C entries. License boilerplate
and generated record projections were read but are not invented substantive prose/snippet units.

## Original evidence and bounded decisions

Primary passages were rechecked from original URLs on 2026-11-03 using read-only HTTP retrieval,
not inherited solely from an investigator verdict. Initial Exa previews established publication headers
but did not include all target clauses; focused original HTML extraction supplied the actual clauses.

| Original source / revision and status | Rechecked meaning and actor |
|---|---|
| [CTAP2.0, Proposed Standard 30 January 2019](https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html), §5.2 | Authenticator descriptor “May be omitted if the allowList has exactly one Credential.” No one-matching/non-discoverable general exception. |
| [CTAP2.1 errata, Proposed Standard 21 June 2022](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html), §§6.1/6.2.2 | Platform uv/auth exclusion and optional privacy-preserving display inputs; authenticator required descriptor, identifying-field UV prohibition, discoverable ID versus optional server-side entity. |
| [CTAP2.3, Proposed Standard 26 February 2026](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html), §§6.1/6.2.2 | Same relevant user/descriptor/UV boundaries; make unsigned key6 versus assertion unsigned key8; clients MUST treat empty map as omitted. No invented first-introduction history. |
| [WebAuthn L2, Recommendation 8 April 2021](https://www.w3.org/TR/2021/REC-webauthn-2-20210408/), §§5.4.3/6.1.1/8.7 | RP decisions use ID, not display fields; mismatch is a risk signal, not certain cloning; none is empty-map/no information, not signed self attestation. RP ceremony verification remains separate (§§7.1/7.2). |
| [WebAuthn L3, Recommendation 25 August 2026](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#sctn-credential-backup), §6.1.3 | Freshly resolved BE/BS evidence: credential eligibility/current state, permanent BE versus changeable BS, BE=0/BS=1 prohibited. SDK properties only inspect bits. L2 link retained for base layout, not presented as BE/BS authority. |
| [IANA COSE registry](https://www.iana.org/assignments/cose/cose.xhtml), retrieved last-updated 2026-08-25 | ESP256=-9 P-256/SHA256 versus ES256K=-47 secp256k1/SHA256; generic algorithm recognition is not a WebAuthn curve profile or device/trust approval. |
| [ARKG-10 Internet-Draft](https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html), §§5.1/5.2/8.3/status; corrected 2026-11-04 | Work in progress, stated expiry 31 August 2026. Seed alg=-65700 and BL=-1/KEM=-2 agree; §5.1 example uses derived ESP256=-9. Draft kty=-65537/direct maps and optional int/tstr dkalg differ from SDK kty2/bstr maps/required integer dkalg. The earlier unsupported-placeholder inference is withdrawn, not replaced by blanket conformance. |
| [Framework adjudication](framework-adjudication.md) and retained `evidence/framework/CborReader-v10.0.2.cs:46–81` | Actual writer auto-sorting, and reader ReadEncodedValue returns `_data.Slice(...)`; end-of-map is not buffer exhaustion. No new probe was run by this editor. |

Context7 resolved `/dotnet/docs` and was queried specifically about CborReader encoded-memory ownership.
It returned relevant general Memory owner/consumer guidance but no specific CBOR slice implementation;
the retained release-pinned reader source supplies that exact evidence. No broad provider erasure,
cross-platform key-validation exception or complete decoder-rejection claim follows from those results.

**Framework correction applied:** numeric insertion is not emitted ordering. Canonical writer sorts
COSE keys to 1/3/-1/-2/-3 (or the respective four-key subset). D-08's inferred serialization defect is
withdrawn under CTAP-IMP-017; comments corrected, wire writes untouched. Likewise, parser claims are
scoped to observed checks, not a claim that ReadEndMap/Finished rejects trailing root bytes or that
every SDK decoder was exercised by the coordinator's pure framework probe.

## Full snippet classifications and typed prerequisites

The complete 30-file source/XML/comment inventory contains **zero `<example>` blocks, zero `<code>`
blocks and zero fenced snippets**. No new executable documentation example was introduced. Thus there
are no complete consumer programs, composable XML fragments, application-dependent XML sketches or
historical before/after blocks to extract/compile in this owner set. Inline XML code/identifier spans,
wire-layout descriptions and comments are explanatory text, not compilable examples. The local scan
covered all owned files, not only changed lines. F owns guide/fenced journeys and their compilation.

For clarity, existing test bodies are **complete project-context fixtures, not public documentation
snippets**. Every body/helper/assertion in the following exhaustive file set was inspected and preserved;
none was executed or stripped into a misleading consumer wrapper. Typed prerequisites/classifications:

| Complete fixture file (under exact root below) | Classification / typed prerequisites and limits |
|---|---|
| Integration `AuditV2/Ctap2RequestAuditReproTests.cs` | xUnit2 hardware/setup rejection fixture; `YubiKeyTestState state`, `Ctap2Session session` from `WithCtap2SessionAsync`, `Ctap2TestData`, Core/Ctap2/Tests.Shared and integration helpers. Initialization/normalization precedes exception check. |
| Integration `Ctap2AlgorithmSupportTests.cs` | xUnit2 application-dependent creation fixtures; same state/session/helpers plus Core `CoseAlgorithmIdentifier`, `ClientPin`, returned `byte[]` token/auth and best-effort credential-management cleanup. Optional normal return is not an exercised algorithm. |
| Integration `Ctap2EnterpriseAttestationTests.cs` | xUnit2 state-mutating config/creation fixture; same state/session/helpers plus `AuthenticatorConfig`, matching `IPinUvAuthProtocol` and token permission. Approved enterprise RP/device/current state required; deletes all RP credentials and does not restore config. |
| Integration `Ctap2FipsComplianceTests.cs` | xUnit2 conditional metadata fixtures; `YubiKeyTestState`, initialized `Ctap2Session`, `AuthenticatorInfo` via actual helpers. No certified-device qualification; normalization may perform PIN work. |
| Integration `Ctap2GetAssertionTests.cs` | xUnit2 creation/assertion/status fixtures; actual state/session/PIN/token helpers and saved credential/account IDs. Human UP/UV/known-device state; synthetic hashes and missing comprehensive cleanup exclude secure RP integration use. |
| Integration `Ctap2MakeCredentialTests.cs` | xUnit2 creation/exclusion/no-auth fixtures; actual state/session/PIN/token helpers, saved credential ID and cleanup. Human UP/UV/known-device state; inspected status/field assertions do not complete RP registration. |
| Unit `AuditV2/Ctap2RequestAuditReproTests.cs` | xUnit3 complete pure request fixtures; Core/Ctap2 friend-access build context, `PublicKeyCredentialParameters`, internally accessible `Ctap2SessionRequestEncoding`, `CborReader`. No hardware/state fixture. |
| Unit `Cbor/Ctap2SessionRequestEncodingTests.cs` | xUnit3 complete mechanical encoding fixtures; same friend context, typed options/entities/hash helpers and `CborWriter/Reader`. Non-ceremony all-options combinations deliberately retained. |
| Unit `Cose/CoseArkgP256SeedKeyTests.cs` | xUnit3 complete experimental codec fixtures; Ctap2 COSE types, `CborWriter`, synthetic coordinate arrays and SEC1-format helpers. No pinned vendor profile or validated public points. |
| Unit `Cose/CoseKeyForwardCompatibilityTests.cs` | xUnit3 complete codec/required-field fixtures; Ctap2 COSE, `CborWriter`, synthetic points and unknown identifiers. Not exhaustive structural/math/trust evidence. |
| Unit `Credentials/AttestationStatementTests.cs` | xUnit3 complete synthetic attestation fixtures; internal Decode friend context, `CborWriter`, dummy signature/certificate bytes, no real certificate trust setup. |
| Unit `Credentials/AuthenticatorDataTests.cs` | xUnit3 complete bit/layout/hash fixtures; `AuthenticatorData`, SHA256 and synthetic byte arrays. AT/ED enum-only bodies do not call Parse. |
| Unit `Credentials/CredentialResponseTests.cs` | xUnit3 complete response/reader/fallback fixtures; Ctap2 credential types, `CborWriter/Reader`, synthetic signatures/keys and SHA256 helper. Required-descriptor/context enforcement and crypto validity are not established. |
| Unit `Credentials/PublicKeyCredentialTypesTests.cs` | xUnit3 complete model/roundtrip/setter fixtures; typed entities/options, `CborWriter/Reader`, ID/auth arrays. Local storage/roundtrip is not immutable ownership, valid token tags or an authorized ceremony. |
| Unit `MakeCredentialResponseTests.cs` | xUnit3 complete unsigned-output envelope fixtures; `MakeCredentialResponse`, `CborWriter`, dummy authData and encoded previewSign value. No live previewSign proof. |

Exact roots: Integration = `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/`;
Unit = `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/`. xUnit2 hardware fixtures require their real
integration project and Tests.Shared helpers, never a mixed xUnit3 host. Unit internal calls require
the real unit/friend context, not consumer assembly-name impersonation. Ordinary compilation belongs
to coordinator gates; no compile success is asserted here. All hardware fixture operations remain
not-run and subject to the existing human coordination policy, not an added device authorization gate.

## Deferred work and cross-owner re-review triggers

- **C-001 / WA-XREF-001:** user/constructor privacy and empty-ID representation decisions remain
  deferred. Reopen entity constructor/Parse/Encode, response User/GetUserHandle and WebAuthn
  AuthenticationResponse/MatchedCredential/account-picker docs if representation/filtering changes.
- **C-002 / WA-IMP-010:** no final ID recovery implemented. Reopen Credential/GetCredentialId,
  session get/next, WebAuthn matcher/final result and credential-specific extension association when
  request/version-aware validation or recovery changes.
- **C-003 / CTAP-IMP-011:** exclusive uv/auth and capability policy, protocol selection and fake-tag
  fixture upgrades remain deferred. Reopen both options, encoder, A session/interface, B token and
  F PIN/FidoTool examples plus G boundary. Existing WebAuthn adapter already omits uv for token use.
- **C-004 / WA-IMP-006/013 and response-owner supplement pending:** no secret-response owner/API
  invented. Reopen response Decode/raw/key/output, AuthenticatorData, nested attestation/extension
  copies, E outputs/storage and WebAuthn response lifetimes after explicit ownership/zeroing changes.
- **C-005/006:** no RP verifier, complete malformed/conditional/envelope checks, counter range fix
  or mathematical key validation added. Reopen parser/error/flag/hash/raw/COSE/fallback docs and
  structural fixtures when those contracts change. PIN/UV required-label work remains CTAP-IMP-007;
  generic COSE permissiveness is not a justification to relax that boundary.
- **C-007/008:** no identifiers/writes changed. Reopen registry/profile/typed-attestation/none prose
  after identifier/decoder/policy changes. Sorting alone warrants no serialization fix; canonical-mode
  changes would require exact-byte evidence and renewed comment review.
- **C-009:** actual SDK compatibility profile versus draft target remains a maintainer decision.
  Draft authority for seed placeholder -65700 is now established; vendor compatibility evidence for
  the SDK's kty2/wrapping profile, nested curve/point/length/constructor checks and E
  generated-key algorithm propagation remain deferred. Reopen seed/signing/mapping/previewSign/ARKG
  experimental contracts together; do not infer full conformance from routing or roundtrip fixtures.
- **C-010:** no assertion unsigned key8 support or full envelope owner added. Reopen both response
  output tables/raw access, E codecs and WebAuthn transformations after support/owner changes.
- **C-011/014/015 / CTAP-IMP-021/022:** meaningful assertions, applicability outcomes, token finally
  cleanup, broad enterprise deletion/config restoration and executable reset diagnostics remain
  deferred. Reopen all owned integration summaries, associated unit fixture descriptions and F test
  guidance after actual fixture changes/execution. Normal return/conditional assertions/setup failure
  do not establish completed device ceremonies, FIPS validation or per-firmware outcomes.
- **C-012 / CTAP-IMP-006 with Core input-layer supplement pending:** code-point/encoding/current
  policy enforcement not changed. Reopen preset, Core reader length/conversion, ClientPin prompts
  and F/G input examples after the shared policy fix.
- **C-013:** A/F/G retain ownership of clientDataHash construction/origin/RP duties, complete next
  assertion sequencing, callback/cancellation/side-effect explanations. A connection exchange guard
  is not a whole-sequence reservation. No rollback/no-side-effects promise added to C DTOs.

**Additional evidence handoff to F/G/A:** pinned L3 Recommendation §6.1.3 now settles the earlier
BE/BS citation dependency. This adds no BE/BS validation or device-support claim and does not edit
other owners' prose. C's local XML names the exact later revision solely for those meanings.

## Local diff checks and limits

Executed from `/tmp/opencode/ctap2-001-edit-c`:

- `git status --short` on entry: empty.
- `git diff --check`: exit 0, no output.
- `git diff --name-only 5b09a9b6942ded3913b5c9340c3e126617e61a82`: only the 27 paths above.
- Python heredoc read primary `inventory.json`, selected owner C, read each baseline through
  `git show <full-baseline>:<path>`, and compared SHA256 to its inventory hash: **30/30 matched**.
- The same local inspection compared source-specific lexical streams after discarding only comments
  and whitespace, preserving exact regular/verbatim/interpolated string and char literal text and all
  other non-whitespace characters; asserted no raw-string syntax needing an expanded lexer. **30/30
  identical**, including the three unchanged paths. Preprocessor lines compared separately: **30/30
  identical**. Diff scope had no deleted/new source or non-C paths.
- Every contiguous XML `///` block was parsed inside an ElementTree `<doc>` wrapper: **211 blocks
  well-formed**. All 30 files scanned for `<code`, `<example` and fence markers: **zero**.

Observed check summary:

```text
OWNED=30 CHANGED=27 BASELINE_HASH_MATCHES=30
LEXICAL_PRESERVED=30 DIRECTIVES_PRESERVED=30 XML_BLOCKS=211 DOC_SNIPPET_MARKERS=0
```

This is a source-specific lexical/diff/XML check, **not Roslyn token classification, cref/param binding,
public-API analyzer validation, compilation, scoped formatting, tests or protocol execution**. Those
coordinator gates must run after independent review/integration. No positive build/test claim is
borrowed from the coordinator's earlier suite/probe. Fresh reviewer should inspect original source
and clauses, then the complete diff, with particular attention to borrowing/copying, exception and
conditional-field limits, experimental wire-profile attribution, and actual fixture assertions.

**Review-correction check, 2026-11-04:** only the three correction paths in correction-c.md differ
from the reviewed checkpoint; all24 other reviewed changed files match review hashes, and all three
clean files still match baseline. Repeated local lexical/directive/XML/scope checks retain the summary
above (30 owned, 27 baseline-changed, 211 XML blocks, zero documentation snippet markers), with
`git diff --check` exit0. The exact three new SHA256 values and proposed historical leading erratum
are retained in correction-c.md. No reviewer acceptance or compiler/test gate is claimed by this update.
