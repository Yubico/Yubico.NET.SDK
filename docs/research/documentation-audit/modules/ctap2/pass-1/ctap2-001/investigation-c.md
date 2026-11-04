# CTAP2 investigator C — credentials, COSE and request encoding

> **Later original-source correction (2026-11-04):** C-009 incorrectly treated seed marker -65700
> as unsupported by ARKG draft-10. The original explicitly defines that placeholder; it agrees with
> the SDK. Outer kty, wrapped/direct maps and derived-algorithm model differences remain. See
> review-c.md RC-R1, correction-c.md and review-c-followup.md. The original investigation is retained.

## Scope, evidence and limits

Read-only investigation on **2026-11-03**, baseline
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. `git rev-parse HEAD` matched the plan.
All **30 C-owned files** were read as complete files, including implementations, public/internal
explanations and every test assertion. A read-only SHA-256 comparison subsequently matched all 30
inventory hashes. No source/test/sample executable tokens were changed. The working tree already
contained campaign README edits and untracked CTAP records alongside the preserved four user files;
none was reverted or overwritten.

Read root `CLAUDE.md`, CTAP2 README/CLAUDE/tests CLAUDE, campaign workflow/style/plan,
`docs/SDK-HOUSE-STYLE.md`, testing guidance, and the WebAuthn handoff, original investigator A
report **including its leading descriptor erratum**, and current triage ledger. Read Core CLAUDE
before narrow Core credential-reader dependencies. Loaded `domain-security-guidelines`; its generic
PRD/device tables are not protocol evidence. Build/test/commit skills were not applicable: **no
build, test, hardware operation, commit or delegation was performed**. No new skills/agents were
created. Only this report and `evidence/c/spec-quotes.md` are authored outputs.

Primary quotations, exact original URLs, revision/status, actor, section and access date are in
[focused evidence](evidence/c/spec-quotes.md). Context7 was used for the .NET CBOR question and its
irrelevant result explicitly rejected. Initial research plus focused follow-up was bounded; no
unbounded vendor-profile search or live ceremony was attempted.

**Status:** all IDs below are local **candidates**, not accepted edits or independently adjudicated
implementation bugs. A documentation change, a desired behavior decision, test coverage and hardware
qualification are separate dimensions. Agreement with sibling SDKs or a round-trip fixture is not
proof of standards conformance.

## Candidate findings

### C-001 — Re-baseline WA-XREF-001: entity, handle and display fields are different contracts

**Documentation correction; historical ID remains authoritative.**
`src/Ctap2/src/Credentials/GetAssertionResponse.cs:24–31,60–65` restricts `User` to discoverable
credentials and UV. Neither condition is a decoder filter: lines 163–165 parse any supplied user.
S21/S23 §6.2.2 require at least the ID for discoverable credentials, allow the user entity for
server-side credentials, and prohibit identifying name/displayName/icon without UV. The handle
is not itself conditional on UV. The present public user constructor also serves response data;
`PublicKeyCredentialTypes.cs:259–269` describes only creation.

The public constructor (`300–319`) additionally requires nonempty name/displayName, although
S21/S23 §6.1 permit omission of both for privacy. The parser/private constructor permits absent
display fields but rejects an empty ID (`324–337`), despite the CTAP empty-account-ID allowance.
W2's nonempty WebAuthn handle rule is narrower than that CTAP allowance. These are **current SDK
API restrictions**, not universal CTAP requirements. Explain the restrictions now; defer the choice
of privacy-preserving request construction and response empty-ID policy rather than silently
redefining valid CTAP input as malformed. Reopen constructor/Parse/Encode and account-selection
documentation if that representation/policy changes.

**Pass 1:** describe the authenticator-supplied optional entity; distinguish opaque account handle
from optional identifying display fields; explain that display fields may be absent and are not
authorization identifiers. The SDK passes supplied fields through, not a privacy-verification
filter. Preserve W2 §5.4.3's RP responsibility to use ID rather than display fields, without claiming
the CTAP layer completes RP authentication. The `GetUserHandle()` empty fallback is a convenience,
not proof of no account or failed verification.

**G coordination:** this is **WA-XREF-001 / historical WA-A-002**, not a new duplicate bug. Recheck
current WebAuthn `AuthenticationResponse.User`, `MatchedCredential.User` and account-selection
guidance against the corrected CTAP text. Do not resurrect the original investigator's superseded
descriptor attribution. **Deferred:** any future privacy filtering/response validation must reopen
these fields and the WebAuthn passthrough boundary, not silently become a Pass-1 change.

### C-002 — Descriptor omission conditions are wrong and the decoder cannot infer the version

**Documentation correction plus cross-layer deferred dependency.**
`GetAssertionResponse.cs:39–42` says omission is allowed when only one credential matches before
2.1 **or** for non-discoverable credentials. S20 §5.2 permits omission specifically when the
**request allowList has exactly one descriptor**. “One matches” is not equivalent to “one listed.”
S21/S23 §6.2.2 mark credential required, with no blanket non-discoverable exception. The decoder
has neither request context nor negotiated version and accepts absent credential for any response;
`GetCredentialId()` then returns empty. Unit `CredentialResponseTests:361–374` asserts that empty
fallback; it is not a conformance fixture for every CTAP revision.

**Pass 1:** identify the CTAP2.0 single-entry allow-list exception exactly; say this decoder exposes
absence as null and does not recover the known requested descriptor. Avoid claiming that all
non-discoverable credentials omit descriptors or that a missing 2.1/2.3 field is valid. Explain
caller-side association where the single selected descriptor is already known.

**Deferred:** preserve **WA-IMP-010** for WebAuthn final-ID recovery; G owns its re-baseline. C does
not invent a second recovery bug. A separate version/request-aware CTAP validation decision may be
needed: a standalone decoder cannot enforce all response conditions without that context. Trigger
re-review of `Credential`, `GetCredentialId`, interface get/next methods, WebAuthn matcher/result
IDs and credential-specific extension association after recovery/validation changes.

### C-003 — UV documentation and fixtures encourage invalid option combinations

**Consequential documentation correction; deferred request validation/selection decision.**
`MakeCredentialOptions.cs:63–82` says PIN/UV auth is required for verification and computed using
ClientPin. The CTAP uv option requests **built-in UV**, while a token-authenticated request uses
`PinUvAuthParam` and omits the uv key. S21/S23 §6.1/§6.2 prohibit **any** uv key alongside that
parameter, not merely `uv:true`. Built-in-UV absence also prohibits sending uv. Current session
methods and request encoding do not reject UV-alone and do not check these capability/combination
rules. `Ctap2SessionRequestEncoding.cs:54–64,93–103,154–158,181–185` independently sends both.
Thus the README's “Setting UserVerification alone is rejected” is not SDK validation evidence.

Unit `Ctap2SessionRequestEncodingTests:53–97,119–153` deliberately asserts both uv and a four-byte
auth parameter; the public fluent-options test `PublicKeyCredentialTypesTests:245–257` only checks
storage. Neither is an authorized/conformant PIN ceremony. The encoder defaults an omitted protocol
to **2** when nonempty auth bytes exist; it does not negotiate protocol or verify tag size.

**Pass 1:** distinguish built-in UV from PIN/UV token authorization; instruct callers to omit
`UserVerification` when supplying token auth; describe absent/null options versus explicit false;
state protocol2 default as encoding behavior, not negotiation. Treat all-options fixtures as
**mechanical encoding fixtures with intentionally non-ceremony inputs**, not valid invocation
examples. Do not assert SDK rejection that does not exist. F must correct README's PIN snippets
that set both fields; A owns interface/session explanation; B supplies protocol/tag/permission
requirements and G reviews the different WebAuthn policy (which already omits uv for token use).

**Deferred desired behavior:** decide capability-aware UV validation and exclusive-parameter
rejection versus a deliberately low-level encoder surface; add request-boundary regression coverage
before removing any limitation. Do not introduce protocol negotiation, biometric support policy or
new assertions during this pass. Reopen both options classes, both builders, interface/XML, PIN
journeys, invalid fixtures and WebAuthn UV/permission descriptions after any change.

### C-004 — Borrowed inputs and secret-bearing response copies lack actionable ownership contracts

**Documentation correction plus high-priority deferred ownership/security work.**
The constructor stores descriptor ID and transport collection references
(`PublicKeyCredentialTypes.cs:59–73`) and user ID references (`300–319`). Parsed IDs are newly
materialized bytes. Options retain supplied lists, auth bytes and extension-map memory. Callers
must keep borrowed inputs stable through awaited transmission; “read-only” memory/collection
interfaces are not immutable storage or snapshots.

`MakeCredentialResponse.Decode(data)` retains attestation-statement and unsigned-output **slices
of caller data** (`170–181,195–201,221–223`). `Decode(reader)` documents possible reader-buffer
references, but the memory overload and internal “owned response bytes” summary (`132–134`) hide
this borrowing. Authentication bytes are separately materialized, then `AuthenticatorData.Parse`
copies the complete authData, RP hash and extensions again. `GetAssertionResponse` independently
materializes authData/signature/largeBlobKey/IDs; it does not snapshot the complete outer map.
`AttestedCredentialData` copies ID/public-key bytes. Unknown COSE types decoded via `CoseKey.Decode`
clone raw bytes; directly constructed COSE records retain supplied memory.

Both credential response classes expose plaintext **LargeBlobKey** without disposable ownership,
and extension/authenticator raw copies can contain secret-derived data. No response/AuthenticatorData
dispose method clears these buffers; disposing the session is not response cleanup. Clearing a
caller's original makeCredential buffer cannot clear the separately materialized large-blob key
or extension copies. Captured read-only views are not a documented zeroing API. On success the
backend transfers its response; the session does not clear that transferred envelope after decoding.

**Pass 1:** clearly document borrowed versus copied storage per entry point, keep-borrowed-input
unchanged lifetime, and that response parsing/session disposal do **not** provide sensitive-result
cleanup. Do not tell callers they can solve every internal copy by zeroing one public view. Add
local non-logging guidance for large-blob/secret-derived results. A owns backend/session qualifiers;
E owns extension-output semantics; G reconciles **WA-IMP-006/WA-IMP-013** without duplicate counts.

**Deferred desired behavior:** choose one explicit owner for complete secret-bearing response storage,
view lifetimes, disposal guards and exception-safe cleanup, or another maintainable public cleanup
contract. Re-review response Decode/raw/optional-output XML, AuthenticatorData/attested-data docs,
attestation raw/unsigned slices, extension consumers, stored-credential ownership comparisons,
WebAuthn attestation/result boundaries and all examples after ownership changes. Validate retained
views and failure paths separately from mere disposed-property exceptions.

### C-005 — Decode, flag and RP-hash helpers are not complete verification

**Documentation correction; no cryptographic verifier implementation authorized.**
AuthenticatorData's `UserPresent`, `UserVerified`, `BackedUp`, `BackupEligible` return flag tests,
not independent proof of a trusted authenticator event. `BackupEligible` (`83–86`) concerns the
**credential's** backup eligibility, not generic device “support.” Its linked L2 authenticator-data
definition reserves bits 3–5, so it cannot substantiate modeled BE/BS meanings. Keep a verified
later-revision reference for those meanings or explicitly retain this citation only for the base
layout; G/F must supply an original later passage rather than upgrade mechanically.

`VerifyRpIdHash` compares SHA-256(UTF-8 RP ID) to parsed bytes, nothing more. Credential response
decoders do not validate origin, challenge freshness, account binding, signature, certificate chain,
attestation policy, requested UV/UP or counter policy. W2 §7.1/§7.2 assign complete verification to
the RP; W2 §6.1.1 counter mismatch is a risk signal, not certain cloning. COSE Decode selects
carriers, not mathematical validity or trusted algorithm acceptance.

**Pass 1:** state parsing/inspection and limited hash comparison; flag properties report the bit
received. Say counter zero/support/global-vs-per-credential policy belongs to the authenticator/RP,
not SDK monotonic enforcement. Name typed attestation decoding support (packed, fido-u2f, apple,
none; others opaque) separately from verification. Reopen these claims if a verifier or policy layer
is added. BE/BS exact later citation remains a focused evidence dependency for G, not a conformance
claim based on current sibling agreement.

### C-006 — Broad malformed/required-field claims exceed decoder enforcement

**Deferred implementation/contract decision with truthful public qualifications now.**
`AuthenticatorData.Parse:149–173` takes all remaining bytes as extensions when ED is set without
checking that they are one complete CBOR map. It ignores trailing bytes when ED is unset.
`AttestedCredentialData.GetCborLength:122–127` skips **any CBOR value**; it does not require a
COSE map, alg or modeled required parameters. `MakeCredentialResponse` requires fmt/authData/attStmt
but not AT/new credential data (its helper getters can return empty/Guid.Empty). `GetAssertionResponse`
requires field presence, not a nonempty signature or response conditional validity; largeBlobKey
length is unchecked. `NumberOfCredentials` casts uint32 to int without a range guard, so values above
Int32.MaxValue can become negative. These are source counterexamples, not executed repro tests.

Lax response decoders' definite-length loops and absence of root-consumption checks also do not
establish strict validation of duplicates/trailing values/all structurally invalid input. Unknown
attestation and `none` paths accept raw CBOR without map/empty-map enforcement; packed checks alg/sig
presence, fido-u2f/apple check nonempty x5c, not chain validity or every format cardinality rule.
Unknown COSE handling is permissive for identifiers; required kty/alg and known EC2/OKP/RSA parameter
types are checked, but algorithm/curve/coordinate semantic compatibility is not generally checked.
PIN/UV key agreement has a **separate** strict security boundary owned by B; these credential
carriers must not be advertised as a bypass or its substitute.

**Pass 1:** retain the useful minimum-length/required-field checks, but do not imply all malformed
or all security-critical cases are rejected. Attested public-key bytes are extracted, not fully
decoded by that parser. Public `ArgumentException` XML for “malformed” inputs is incomplete because
CBOR parsing can raise `CborContentException`; missing required fields generally produce
`InvalidOperationException`. Distinguish generic raw escape hatches from valid-model guarantees.

**Deferred desired behavior:** agree strict structural boundaries, optional/conditional-field checks,
counter range representation, and algorithm-specific validation versus forward-compatible carriers.
Do not automatically reject unknown identifiers/fields or blindly re-emit response unknowns into
requests. Re-review parser XML, flags/raw/extension/COSE properties, helper fallbacks, both response
tables, module forward-compatibility doctrine, error contracts and parsing fixtures after changes.

### C-007 — Algorithm and none-attestation terminology are factually wrong

**Documentation correction; wire values unchanged.**
`CoseAlgorithm.Esp256` (`CoseAlgorithm.cs:38–41`) says secp256k1. IANA COSE Algorithms assigns
**-9 = ESP256, P-256/SHA-256**; **-47 = ES256K, secp256k1/SHA-256**. `EdDsa` (`33–36`) says
Ed25519; generic COSE -8 is polymorphic (and currently deprecated in the registry), whereas W2's
credential-key profile specifies Ed25519. Qualify the layer rather than misdescribe the registry.
`IsKnown` means recognized by this SDK's finite set, including experimental private placeholders,
not IANA registration, implementation support, authenticator availability or production approval.

`AttestationFormat.None:40–43` and `NoneAttestationStatement:266–269` call none self-attestation.
W2 §8.7 and its attestation-type definitions distinguish absence of attestation from self attestation.
None's empty map does not provide a self-attestation signature. Public typed attestation constructors
and properties in `AttestationStatement.cs` also have almost no field/ownership XML; add concise
format/purpose/storage information instead of generic “statement” summaries. Unknown includes known
formats without modeled decoders (tpm/android-*), not only new identifiers.

**Dependencies:** Core's distinct `CoseAlgorithmIdentifier` is the type used by creation parameters,
not this module's `CoseAlgorithm`; do not redesign/conflate them in Pass 1. G/E reconcile ESP256/ARKG
and WebAuthn none/self prose. Re-review identifier/format docs if algorithms, registry recognition,
typed decoding or attestation-conveyance policy changes.

### C-008 — Numerically sorted COSE labels are not canonical encoded-key order

**Internal/test documentation correction, not a serialization edit.**
`CoseKey.cs:247,287,324`, `CoseArkgP256SeedKey.cs:159`, and its unit tests (`50,75`) call
`-3,-2,-1,1,3` canonical order. For these one-byte integer encodings the S21 §8.1 rule yields
**1,3,-1,-2,-3** (`01,03,20,21,22`). Numeric ordering is the **insertion order** in these methods;
it is not CTAP canonical encoded order. `CoseKeyWriter.cs:25–32` accurately names numeric sorting,
but should not imply that it alone establishes CTAP canonical output. Microsoft Ctap2Canonical
documentation requires keys sorted by encoding; no execution was performed to independently
characterize writer reordering here.

**Pass 1:** explain that canonical output is the writer's conformance concern; remove the false
ordering example or explicitly label numeric insertion order. Preserve executable writes and
synthetic fixtures. Re-review comments and byte-level test evidence if serialization/conformance
mode changes. Do not use a successful Encode→Decode round trip to claim draft interoperability.

### C-009 — ARKG wire profile differs from cited draft and current code's prose

**Documentation correction plus deferred compatibility/security decisions; coordinate E.**
`CoseArkgP256SeedKey.cs:28–41,146–148` claims direct nested COSE maps, outer kty=2/alg=-65700
“per draft … -10” and sibling/legacy sources. Actual `Encode:163–167` writes **byte strings
containing encoded maps**. `CoseKey.ReadModeledParameterValue` accepts either direct nested maps
or byte strings as byte[]; the ARKG tests build the wrapped form too. ARKG-10 §5.1 uses kty TBD
(placeholder **-65537**) and direct nested maps. Its BL/KEM labels **-1=BL, -2=KEM** agree, but that
does not validate the entire SDK profile. Draft §5.2 supports signing placeholder -65539; it does
not substantiate seed-key marker -65700. The fixed -10 revision is work in progress with stated
expiry 31 August 2026, before this investigation.

`CoseAlgorithm.ArkgP256` remarks (`80–84`) call an alias “stable” and narrate anticipated future
renaming; preserve experimental availability and the two distinct identifiers without implying
standardized/stable cryptographic guidance. `CoseArkgP256SeedKey.Decode:95–125` says nested keys
“must be … P-256,” but checks EC2 type and coordinate upper lengths, **not curve=1** or point
validity. Short/empty coordinates are left-padded. Outer -65700 dispatch happens before kty
switching; test `Decode_WithArkgAlgorithmAndNonEc2KeyType_UsesArkgVariant` explicitly pins acceptance
of outer kty=99. Public constructors do not enforce the documented “must be -65700.”

**Pass 1:** describe the actual experimental SDK wire profile, wrapped-map emission and accepted
input carriers; separate the verified draft label mapping from unsupported full-profile attribution.
Keep the strong experimental/non-production warning. Say reconstructed SEC1 layout is
`04 || X[32] || Y[32]`, not proof the points are valid P-256. Remove unpinned sibling line-number
and nonexistent plan references as current authority while preserving important mapping rationale.

**Deferred desired behavior:** maintainer must choose vendor compatibility profile vs draft target;
require authoritative pinned vendor/prototype evidence for -65700/wrapping if retained, and decide
outer kty, nested curve/point, coordinate-length and constructor checks. No algorithm/key bytes are
changed now. E owns previewSign/derivation implications; G owns WebAuthn generated-signing-key
boundary. Reopen COSE seed/signing identifiers, seed-key XML/Encode/Decode comments, mapping/roundtrip
fixtures, PreviewSignCbor/derived-key docs and experimental support claims after profile changes.

### C-010 — Unsigned-output shorthand blurs CTAP command, WebAuthn and unknown-data boundaries

**Documentation correction plus deferred getAssertion support decision.**
`MakeCredentialResponse.cs:34,79–85,188`, and unit `MakeCredentialResponseTests:25–27,42–45`
describe “CTAP2.2 / WebAuthn L3 key6,” cite sibling alignment, and say “early CTAP v4 draft.” There
is no general CTAP version4; any historical previewSign draft needs its exact revision/title or
should be described simply as an older decoder expectation. S23 distinguishes **makeCredential
key6** from **getAssertion key8**. WebAuthn's client layer is not an integer-key CTAP envelope.

MakeCredential's map values are **raw encoded CBOR values**, not necessarily CBOR byte strings;
they borrow the input envelope. Null/empty unsigned map is semantically equivalent under S23 even
though this decoder returns null versus a present empty dictionary. The decoder drops unknown
outer fields and has **no complete RawData envelope property**. Attestation/raw authenticator
substructures do not recover dropped outer fields. `GetAssertionResponse` ignores S23 key8 as
unknown and exposes no unsigned-output property or complete raw envelope.

**Pass 1:** use command-specific CTAP field descriptions, distinguish signed authData outputs from
unsigned outputs, disclose borrowing and limited unknown-field preservation, and tell consumers
not to treat empty unsigned maps as an extension-support distinction. The first introducing 2.2
revision was not verified; cite the fixed S23 passage for confirmed semantics, not an invented
introduction history. E/G check any extension that expects key8.

**Deferred:** decide getAssertion unsigned-output modeling/raw escape hatch and whole-envelope
preservation consistently with C-004's secret owner, rather than adding a non-disposable RawData
clone. Reopen both response tables/Decode/raw/output XML, module forward-compatibility guidance,
extension-output codecs and WebAuthn boundary after support/ownership changes.

### C-011 — Several test explanations imply more evidence than their assertions establish

**Test-comment corrections; stronger assertions/portable prerequisites deferred.**

- `I/Ctap2AlgorithmSupportTests.cs:37–50,111–115`: method names promise the returned algorithm,
  but assertions only check nonempty ID/key and attested-data presence, not decoded alg/curve.
  Unsupported optional algorithm exits by `return`, not an explicit skip. No certification proof.
- `I/Ctap2GetAssertionTests.cs:33–35,115–118`: “valid signature” means nonempty bytes in these
  assertions; no cryptographic signature/RP-hash/returned-ID association is checked. The resident
  test does compare exact user ID (`211–214`), and NoCredentials does compare CTAP status (`271`).
- `I/Ctap2MakeCredentialTests.cs:43,88–93`: “valid attestation” is not verified. Guid.ToByteArray
  length16 cannot establish that an AAGUID/AT field was present because helper fallback Guid.Empty
  also has 16-byte encoding. The resident creation test asserts an ID, not discovery by RP lookup.
- `I/Ctap2EnterpriseAttestationTests.cs:42–46,108–114`: ep false means supported but currently
  disabled under the GetInfo option contract (A/D must verify its exact passage), not necessarily
  unsupported. The comment promises packed/certificate but assertions require neither; epAtt is
  tested only **if present**. Configuration mutation and broad-RP cleanup are real, not isolated
  request observation. Do not claim actual enterprise certificate qualification.
- `I/Ctap2FipsComplianceTests.cs:22–24`: class/traits do not establish FIPS validation. There is
  no FIPS device/certificate qualification gate; tests inspect GetInfo fields and can return
  without checking intended criteria. `TryGetValue("clientPin", …)` checks existence, not that its
  bool is true. Describe advertised metadata checks rather than compliance verification.
- `U/AuthenticatorDataTests.cs:119–139`: the AT/ED tests only test enum flags and never invoke
  Parse. Existing note acknowledges the AT limitation; retain it accurately. Stronger complete
  AT/ED parsing evidence is deferred, not manufactured through naming/prose.
- `U/CredentialResponseTests.cs:282–285`: “valid CBOR” assertion only reads a nonempty map header.
  Synthetic two-byte signatures/zero-coordinate keys test parsing, not crypto validity. Default
  absent-descriptor fixtures need the C-002 version/context qualification.
- `U/MakeCredentialResponseTests.cs:23–36`: unsigned-output test establishes map key presence
  and a nonempty encoded value, not a valid previewSign attestation or live firmware behavior.

Here `I/` and `U/` expand to the exact integration/unit roots in the coverage table below. Executable
method names/assertion strings are **not** changed in Pass 1. Add precise commentary where the names
cannot be changed. Future test owners should check the actual returned algorithm/signature/certificate
and applicability/skip outcomes where meaningful. Re-review these summaries and published verification
counts after assertions/fixtures/prerequisites change; never count a setup/early-return path as a
completed ceremony or a passing certification test.

### C-012 — PIN reader preset measures characters, not its documented UTF-8 byte range

**Documentation correction and narrow Core/B deferred dependency.**
`Ctap2CredentialOptions.cs:25–33` calls its MinLength4/MaxLength63 preset “4–63 bytes UTF-8.” Narrow
Core source shows `ConsoleCredentialReader.cs:184–205,210–279` applies lengths to UTF-16 char
counts before encoding, not UTF-8 bytes or Unicode code points. Thus non-ASCII input can exceed
63 UTF-8 bytes; the preset also does not implement current authenticator minPinLength/state policy.

**Pass 1:** describe actual console-reader character-count preset and default UTF-8 conversion,
not complete CTAP PIN validation. B supplies exact PIN code-point/byte/policy requirements; F must
align contributor/examples use. **Deferred:** choose proper encoding/code-point/capability-aware
prompt validation with Core's credential-reader owner; reopen preset, Core length properties,
PIN prompt/ClientPin validation, test setup and WebAuthn prompt policy after the shared fix. This
is not a new standalone Core audit or authorization to change its input reader.

### C-013 — Entry-point responsibilities, sequencing and failure boundaries need local explanation

**A/F/G coordination only for files outside C edit ownership.**
`ICtap2Session.MakeCredentialAsync/GetAssertionAsync` must make it clear that caller supplies a
32-byte **SHA-256 hash of serialized client data**, not a raw RP challenge. Session verifies length
and sends it; it does not build/validate client data, validate RP ID against an origin, choose account
policy or verify returned proofs. Caller/RP must track credential IDs/user mapping and perform RP
verification. A CTAP protocol demonstration using arbitrary 32 bytes is not a complete WebAuthn
application. `PublicKeyCredentialRpEntity` constructor checks only nonempty text, not RP authorization;
transport hints are metadata, not proof a transport is reachable. Exclude-list existence is scoped
to the authenticator's applicable credential/RP/authorization processing, not a global duplicate-user
guarantee; ResidentKey requests discovery, not generic storage of every key on/off device.

`GetNextAssertionAsync` sends a parameterless command with no local count/timer/sequence tracking.
S21/S23 §6.3 instruct the client to collect **N−1** additional assertions promptly; do not perform
GetInfo/PIN/admin/raw or other operations in between. §5 permits authenticator-global invalidation
even across transport channels. No new touch/PIN request is sent by this SDK method. Exhaustion,
missing state and the >30s timer can yield NotAllowed; NFC timeout step is optional. A live physical
connection/exchange guard is not a reservation across the whole sequence. Do not promise session
serialization/retry/recovery beyond source. `NumberOfCredentials` absence semantically defaults to
one; `UserSelected` absence defaults false at the protocol level while the SDK preserves nullable
properties, and selection can make count absent even when multiple credentials exist.

`Ctap2Session.MakeCredentialAsync/GetAssertionAsync` zero encoded request in finally, **not caller
inputs or decoded responses**. The backend resolves/strips CTAP status before model Decode; decode
or callback failure after command success cannot prove no credential/side effect occurred. SDK
argument errors, disposed/uninitialized errors, CTAP status, CBOR/model errors, transport/APDU,
cancellation and notification callbacks have different origins. No rollback is implemented here.

**A handoff:** own interface get/make/next XML and session comments at `206–361`, including absolute
touch claims, raw/hash responsibilities, zeroing, parameter validation and no sequence reservation.
**F handoff:** README creation/PIN/getNext journeys and test/example assumptions. **G handoff:**
WebAuthn owns client-data construction/delegated RP validation/selection policy; CTAP has no automatic
parity guarantee. Trigger full cross-layer re-review if sequencing, clientDataHash validation,
error mapping, cancellation completion, ownership or credential-selection behavior changes.

### C-014 — Historical test narration and misplaced references should not become active authority

**Documentation/comment correction; genuine gaps remain in this report.**
`U/CredentialResponseTests.cs:48,58` cites CTAP2.1 §§8.2/8.7 for packed/none formats; S21 §8 is
message encoding, while **W2 §8.2/§8.7** are attestation formats. Fix actor/spec rather than move
section numbers blindly. Line39 calls authData “37 bytes with AT” although helper includes complete
attested fields; describe the 37-byte fixed prefix plus attested payload. The parser-only key/signature
fixtures remain deliberately synthetic.

`I/AuditV2/Ctap2RequestAuditReproTests.cs:31–32` says “Previously … now” and unqualified “request
never leaves host.” The makeCredential validation occurs before that command, but the test helper
creates a **real session** (GetInfo/initialization I/O still occurs). Describe rejection of the
makeCredential request, not absence of all device traffic. Source, not this assertion alone, supports
the before-encoding location; the assertion only observes ArgumentException/ParamName.

`I/Ctap2MakeCredentialTests.cs:269–285,319–323` narrates “closes architectural gap” and says PIN
configuration makes all operations need PIN. Scope to this **discoverable creation** request;
status assertions permit a four-code set and do not prove each firmware produces a named status.
Do not claim WebAuthn hardware “proved” a path without archived execution evidence. The commented-out
PinUv fields are intentional absent-auth setup, not unfinished implementation. A skip message has
an executable “reset within 10s” instruction (`292`); it is **not editable in Pass 1** under runtime
string preservation. Flag it for A/F's reset timing adjudication and a later authorized string change.

`U/CoseArkgP256SeedKeyTests.cs:145–156` carries unpinned sibling/legacy line references and
`Plans/snoopy-strolling-star.md`; none is a current primary specification or present task prerequisite.
Retain BL/KEM routing rationale and synthetic-coordinate/byte-string-fixture conditions; preserve
historical firmware-error provenance in durable evidence if needed, not as proof this test executes
FullCeremony or draft conformance. `U/MakeCredentialResponseTests.cs`'s “CTAP v4” is handled by C-010.

Mechanical scan of all 30 files found **no TODO/FIXME/HACK/XXX/NotImplementedException or phase
placeholder**. Experimental identifiers are real current APIs, not TODOs to delete. The missing
validation/support/ownership and test-strength work remains explicitly deferred above; zero markers
is not implementation completion.

## Claim-matrix contributions

These rows distinguish standard requirements, request/current state, actual enforcement and inspected
test evidence. None was executed during this investigation.

| Claim/condition | Original normative actor/evidence | Source-observed enforcement | Inspected evidence and outcome limit | Disposition |
| --- | --- | --- | --- | --- |
| makeCredential UP | Platform S21/S23 §6.1: up true if present; CTAP2.0 compatibility differs | Rejects up=false; null omitted, true sent | U audit rejects false and checks ParamName; no device/touch execution | C-003/C-013; retain correct compatibility caveat |
| getAssertion UP | S21/S23 §6.2 up default true; preflight §5 up=false | Sends explicit false, suppresses policy notification; no proof of successful unverified use | U audit inspects false wire key | Describe requested/silent semantics subject to authenticator policy, not universal success |
| UV/token | Platform S21/S23 §6.1/§6.2 prohibit uv+auth and uv without built-in capability | Independent option writes; no capability check or combination rejection | All-options fixture pins invalid combination; four-byte fake tags | C-003; docs now, validation/policy later |
| Discoverable/allow list | Platform omit empty allowList; device searches applicable RP credentials | Null/empty AllowList both omitted; no source guarantee of credential policy/UV success | Resident assertion compares handle; creation test only ID presence | C-013; no discovery/capacity guarantee from creation alone |
| Duplicate parameter list | Platform §6.1 preference order, no duplicate entries | Rejects duplicate (type,algorithm), preserves order; session rejects zero count | U audit rejects duplicate, checks exact RS256 then ES256 order | Clean unit C-CL-01 |
| Credential descriptor | S20 §5.2 one-entry allowList exception; S21/S23 §6.2.2 required | Decoder accepts absent without version/request context | U helper asserts empty ID fallback | C-002; WA-IMP-010 separate owned follow-up |
| User fields | Authenticator S21/S23 §6.2.2: discoverable ID, server-side optional, display UV prohibition | Any supplied user parsed; no filtering | Unit with user checks names; resident I test exact ID | C-001 / WA-XREF-001 |
| Enterprise | CTAP request/current enabled support/RP authorization are distinct | Encodes any supplied enterprise int; no capability/policy/range validation | I conditional epAtt true, not certificate or mandatory flag | C-011; D/A exact ep-enabled citation dependency |
| COSE identifiers | IANA recognition != device support or trust | Int carrier; requires int kty/alg; dispatch by kty except -65700 first | Unknown IDs preserved, malformed/missing-field tests; no curve math | C-005/C-006/C-007/C-009 |
| Unknown response data | Root forward-compatibility doctrine + strict structural boundary | Make statement/unsigned slices; Get drops unknown outer fields; Other COSE clone | U checks unknown ignored/type retained, not whole-envelope recovery | C-004/C-006/C-010 |
| Signed/unsigned extension output | S23 §6.1 key6 versus §6.2.2 key8; empty equivalent absent | Make key6 modeled, Get key8 discarded; ED bytes not map validated | U key6 nonempty value, wrong make key8 ignored | C-006/C-010; E/G extension applicability |
| “Valid” signature/attestation/FIPS | RP verification W2 §7.1/§7.2; certification separate | No verification in result decoder | I nonempty/metadata/conditional checks only | C-005/C-011; no certification or live qualification claim |

## Stateful sequence and error-outcome rows

| Sequence stage | Caller/device contract | SDK behavior at baseline | Failure/recovery meaning and re-review dependency |
| --- | --- | --- | --- |
| Prepare make/get | Host constructs serialized client data and hashes it; RP/account state remains caller's | Validates hash length32, basic RP/list guards; no origin/client-data verification | Local rejection can precede command; no auth/selection policy inferred. Reopen interface/journeys if validation changes |
| Authorize | Matching protocol/token permission/RP scope and auth of exact hash; omit uv for token | Options store borrowed auth, encoder defaults protocol2 if absent | Wrong token/tag/capability can reach device; C-003 and B/G dependencies |
| First assertion | Receive applicable credential/count/user fields | Parses first map; exposes nullable count/selection | Absent count implies one semantically, no list reservation; C-001/C-002/C-006 |
| Next N−1 assertions | Same initializing sequence, no unrelated operations; collect promptly, >30s rule with NFC exception | Parameterless getNext; no local remaining count/timer/lock spanning sequence | NotAllowed may mean expired/exhausted/missing/interrupted state; do not reset or blindly retry an unrelated ceremony. Reopen A/F/G sequencing docs if state policy changes |
| CTAP success then parse | Returned bytes are not RP verification | Backend status handling/notification resolution precedes Decode | Parse/callback failure does not imply device rollback; request finally-zeroing != result cleanup. Reopen errors/ownership/examples after change |
| Caller cancellation/transport failure | Device may already have admitted operation | Credential methods pass cancellation to backend, no local rollback | A owns exact transport drain/completion contract; no universal no-side-effects promise |

## Binary-data contracts

| Input/result | Representation; absent versus empty | Storage/lifetime observed | Security/limit and Pass-2 trigger |
| --- | --- | --- | --- |
| clientDataHash | CBOR bstr32, SHA-256 of serialized client data, not challenge itself | Borrowed input; encoded request copy zeroed in finally | Caller client/RP duties; no hash-content verification; C-013 |
| Descriptor ID / user ID | Opaque bytes; constructors reject empty; user max64; user parser also rejects empty despite CTAP allowance | Constructors borrow ID; parsers materialize ID; transport list borrowed | Account identifiers not PIN/key secrets, but privacy/no logging policy still applies. Constructor/parse asymmetry/empty policy change reopens C-001/C-002 and public entity docs |
| RP ID/name / display fields | CBOR text, not URI authorization proof; RP name optional; user public ctor requires nonempty name/displayName though CTAP permits omission | Immutable strings; parse can return optional user display strings | Layer/API constraint, not universal CTAP requirement; C-001/C-013 |
| Allow/exclude/extension inputs | Null or empty list omitted; null/zero-byte extension omitted, encoded A0 map present | Lists/memory borrowed until builder/transmission finishes | Empty bytes != empty CBOR map; no extension validation/negotiation; C-003/C-013 and E |
| PinUvAuthParam/protocol | Null/zero-byte param both omitted; protocol alone omitted; nonempty param defaults protocol2 | Borrowed param, encoded request cleared | No direct zero-length auth probe through these builders; no tag-size/protocol/capability validation. C-003/B must decide low-level special-case support |
| Authenticator data | Fixed37 = hash32 + flags1 + BE uint32 counter4; AT/ED optional suffix | Raw, RP hash, extension and attested fields copied; raw response authData another copy | ED/raw may contain secret-derived outputs; flags/bytes not verification; C-004/C-005/C-006 |
| Attested data | AAGUID16 big-endian UUID, BE uint16 ID length, ID then one CBOR value | Copies ID/key, reports bytesRead | Extracts value without enforcing COSE map/alg; C-006; A owns converter details |
| Assertion signature | CBOR byte string, format depends on credential algorithm; field required by decoder | New array; empty accepted | Signature is public proof material, not standalone trust or private key; C-005/C-006/C-011 |
| LargeBlobKey | Nullable CBOR bstr; absent preserved null, present empty remains present | Separate new array, no disposal owner | Plain symmetric storage key; not public metadata. No zeroing API/lifecycle guarantee; C-004/C-006 |
| Attestation RawCbor | Encoded nested CBOR statement, not whole CTAP envelope | Make result borrows original buffer slice; constructors retain supplied memory | Generally public cert/proof data, but keep storage stable; no chain/signature verification; C-004/C-005/C-007 |
| Make unsigned outputs | Text ID→encoded CBOR value; null and empty map semantically equivalent | Borrowed envelope slices in dictionary; no whole response preservation | Unsigned/untrusted; may carry unmodeled data; never assume all nonsecret. C-004/C-010 |
| COSE records | Integer kty/alg, known modeled parameters; unknown IDs open | Decode copies modeled bstrs/raw Other; direct constructors borrow; Encode new array | Public-key carriers, no trust/math checks; modeled Encode drops extras, Other replays raw as explicit operation, not automatic request propagation. C-006/C-009 |
| ARKG seed keys | SDK kty2/alg-65700, -1 BL and -2 KEM wrapped CBOR maps, -3 derived alg; reconstructed SEC1 65 | Decoded/reconstructed arrays; direct constructors borrow | Experimental public material; profile not verified against vendor originals/draft full structure; C-009/E/G |

## Exact owned-file coverage and meaningful clean units

All paths are relative to **`src/Ctap2/`**. `I/` expands to
`tests/Yubico.YubiKit.Ctap2.IntegrationTests/`; `U/` expands to
`tests/Yubico.YubiKit.Ctap2.UnitTests/`. These explicit roots plus each table entry identify every
inventory path. “Read” is complete file/assertion coverage; “clean unit” is a scoped explanation
with no required correction, not a clean entire protocol or executed test. Copyright boilerplate
and compiler-generated record members are not invented prose units.

| # | Owned path | Complete semantic disposition |
| --- | --- | --- |
| 1 | src/Cbor/CoseKeyWriter.cs | Read; C-008 numeric-vs-canonical context; C-CL-02 supported-value/error unit |
| 2 | src/Cbor/Ctap2SessionRequestEncoding.cs | Read; C-003/C-013; C-CL-01 duplicate/order, C-CL-03 collection encoder; actual absent/empty predicates inspected |
| 3 | src/Cose/CoseAlgorithm.cs | Read; C-007/C-009; open integer-carrier rationale remains accurate, IsKnown narrower than registration/support |
| 4 | src/Cose/CoseArkgP256SeedKey.cs | Read; C-004/C-008/C-009; complete Decode/reconstruct/Encode methods inspected, no production claim |
| 5 | src/Cose/CoseKey.cs | Read; C-004/C-005/C-006/C-008/C-009; C-CL-04 required-int and unknown-label handling |
| 6 | src/Credentials/AttestationFormat.cs | Read; C-007 none/self; known identifier does not imply typed verification |
| 7 | src/Credentials/AttestationStatement.cs | Read; C-004/C-005/C-006/C-007; typed/opaque dispatch and required-field checks inspected |
| 8 | src/Credentials/AttestedCredentialData.cs | Read; C-004/C-006; binary prefix/bytesRead rationale accurate, COSE extraction weaker than validation |
| 9 | src/Credentials/AuthenticatorData.cs | Read; C-004/C-005/C-006; C-CL-05 prefix/endian parse; extension/backup/trust limitations |
| 10 | src/Credentials/Ctap2CredentialOptions.cs | Read; C-012 byte/count mismatch, narrow B/Core dependency |
| 11 | src/Credentials/GetAssertionOptions.cs | Read; C-003/C-004/C-013; null/empty allow list omission explanation accurate; silent requested UP != success guarantee |
| 12 | src/Credentials/GetAssertionResponse.cs | Read; C-001/C-002/C-004/C-005/C-006/C-010/C-013; C-CL-06 absent nullable-array conversion rationale |
| 13 | src/Credentials/MakeCredentialOptions.cs | Read; C-003/C-004/C-013; correct up=false rejection retained, PIN/token and policy language corrected |
| 14 | src/Credentials/MakeCredentialResponse.cs | Read; C-004/C-005/C-006/C-010; C-CL-06 absence conversion; getters' empty/Guid.Empty fallbacks inspected |
| 15 | src/Credentials/PublicKeyCredentialTypes.cs | Read; C-001/C-004/C-013; required parse fields, copied-versus-borrowed IDs and creation/response optional-display asymmetry inspected |
| 16 | I/AuditV2/Ctap2RequestAuditReproTests.cs | Read; C-014 before-command rejection vs whole-host-I/O wording; actual ParamName assertion only |
| 17 | I/Ctap2AlgorithmSupportTests.cs | Read; C-011/C-015; all three algorithm routes, early return, token and cleanup paths inspected |
| 18 | I/Ctap2EnterpriseAttestationTests.cs | Read; C-011/C-015; request/config-state scope, conditional epAtt, no certificate assertion |
| 19 | I/Ctap2FipsComplianceTests.cs | Read; C-011; all four metadata tests and conditional/early-return limits |
| 20 | I/Ctap2GetAssertionTests.cs | Read; C-011/C-015; C-CL-07 exact handle/NoCredentials assertions; signature claim weaker than name |
| 21 | I/Ctap2MakeCredentialTests.cs | Read; C-011/C-014/C-015; credential/exclusion/no-auth outcomes inspected; no runtime-string edits |
| 22 | U/AuditV2/Ctap2RequestAuditReproTests.cs | Read; explanations clean C-CL-01/C-CL-08, no unit wire/guard execution inferred |
| 23 | U/Cbor/Ctap2SessionRequestEncodingTests.cs | Read; C-003 invalid all-options combination/fake tag fixtures; C-CL-09 required command/key assertions |
| 24 | U/Cose/CoseArkgP256SeedKeyTests.cs | Read; C-008/C-009/C-014; label-routing/roundtrip/outer99 assertions faithfully characterized |
| 25 | U/Cose/CoseKeyForwardCompatibilityTests.cs | Read; clean explanation scope C-CL-04; unknown type/alg/curve/label and required-field error assertions, not math/trust validation |
| 26 | U/Credentials/AttestationStatementTests.cs | Read; clean synthetic/raw-equality unit C-CL-10; tests do not claim verified certs/signatures |
| 27 | U/Credentials/AuthenticatorDataTests.cs | Read; C-011 AT/ED enum-only coverage; C-CL-05 endian/hash/flag fixture explanations |
| 28 | U/Credentials/CredentialResponseTests.cs | Read; C-002/C-011/C-014; C-CL-11 reader consumes one value and raw-statement assertion |
| 29 | U/Credentials/PublicKeyCredentialTypesTests.cs | Read; C-003 fixture-vs-ceremony qualification; local roundtrip/storage statements clean C-CL-12, validation-only tests not added/removed |
| 30 | U/MakeCredentialResponseTests.cs | Read; C-010/C-011; key6/nonempty map value and ignored make key8 assertions not extension qualification |

### Named clean units suitable for independent sampling

| Unit | Exact source/semantic bounds | Why no required correction within this unit |
| --- | --- | --- |
| C-CL-01 | `src/Cbor/Ctap2SessionRequestEncoding.cs:108–126`; U audit tests `70–133` | Platform duplicate prohibition/order is accurately described; reject not deduplicate, order assertion explicit |
| C-CL-02 | `src/Cbor/CoseKeyWriter.cs:27–29,40–54` | Supported int/bstr/null value encoding and unsupported-type InvalidOperationException align; canonical claim excluded |
| C-CL-03 | `src/Cbor/Ctap2SessionRequestEncoding.cs:190–202` | Plain counted array encoder has no hidden operation/execution claim or unfinished marker |
| C-CL-04 | `src/Cose/CoseKey.cs:75–100,123–185`; U forward-compatibility tests `22–188` | Known label selection/required int checks and unknown-identifier fixtures are locally accurate; no mathematical-validity promise |
| C-CL-05 | `AuthenticatorData.cs:137–147`; U `AuthenticatorDataTests:25–40,93–105` | Fixed field offsets and big-endian uint32 rationale match exact asserted 0x01020304 value |
| C-CL-06 | Both response construction comments about byte[]?→nullable memory (`Make:236–237`, `Get:196–197`) | Preserves absent null instead of accidental present empty; accurate source rationale, no secret cleanup claim |
| C-CL-07 | I `Ctap2GetAssertionTests:129–130,211–214,225–227,259–271` | Handle test checks exact expected ID; no-credential test checks exact status; these summaries do not overstate crypto qualification |
| C-CL-08 | U request audit false-getAssertion `42–68` | Wire option assertion really checks false in map key5; no hardware touch claim |
| C-CL-09 | U encoding required-field tests `25–50,99–116` | Command byte + exact required outer parameters/hash/RP field checks; nested maps skipped are not described as verified contents |
| C-CL-10 | U `AttestationStatementTests:23–55,90–109` | Explicit dummy-signature/empty-map fixture; raw statement equality truly asserted |
| C-CL-11 | U `CredentialResponseTests:229–247` | Reader overload consumes exactly one encoded value and raw packed-statement bytes are asserted; no borrowed-input immutability claim |
| C-CL-12 | U `PublicKeyCredentialTypesTests:72–88,130–143,183–198` | Local entity encode/parse roundtrip preserves compared modeled fields; not unknown-field/snapshot/security conformance |

## C-015 — Inspected hardware tests do not safely own/clear every token or guarantee cleanup

**Deferred test security/lifecycle work; documentation must not claim otherwise.**
All owned ceremony integration files obtain byte[] tokens/auth parameters without encompassing
finally-zeroing. `Ctap2AlgorithmSupportTests:79–115`, enterprise `61–67,73–114`, getAssertion
`56–118,153–214,242–271`, and makeCredential `57–94,119–153,187–253` retain unzeroed tokens.
Algorithm/get/make cleanup helpers clear their token only after successful deletion/disposal;
an exception is swallowed before that statement, leaving the token uncleared. Enterprise setup
enables persistent config and its finally deletes **all credentials for the RP** rather than only
this run's ID. This does not restore prior enterprise configuration. These are not no-side-effects
or full cleanup tests, despite normal `using` on ClientPin: its returned tokens are caller-owned.

**Pass 1:** describe best-effort cleanup and dedicated approved-device prerequisites locally;
preserve no-hardware/no-PIN execution restrictions and do not change statements or broad deletion
behavior. **Deferred desired behavior:** test owners adopt exception-safe token/auth cleanup,
reassess broad RP deletion/config restoration and portable protocol/capability/skip selection.
Reopen those test cleanup comments, tests CLAUDE, algorithm/enterprise success descriptions, shared
PIN/setup helpers and FidoTool cleanup guidance after changes. B/A own shared helpers; this report
does not authorize editing them or infer the returned token is cleared by disposing its provider.

## Cross-owner handoff and precise Pass-2 re-review dependencies

| Receiving owner | C evidence to reconcile now | Implementation change that reopens documentation | Exact affected documentation families |
| --- | --- | --- | --- |
| A | C-003/C-004/C-013: session passes independent uv/auth, clears request only; getNext no reservation; statuses then parse | Validation, sequence admission/locking, error mapping, callback/cancel or response ownership change | `src/Ctap2/src/ICtap2Session.cs` make/get/next/raw XML; `Ctap2Session.cs:206–361`; backend response-transfer/notification comments; request/options/result docs |
| B | C-003/C-012/C-015: correct UV/token, tag/protocol default, character count, token-owner tests | Protocol negotiation/auth-param/special-empty probe/PIN-prompt validation or cleanup changes | Both credential options' auth/UV docs; encoder comments; Ctap2CredentialOptions; README PIN snippets; shared test helpers and owned ceremony summaries |
| D | C-011/C-015: ep currently enabled vs supported, conditional certificate evidence, config mutation/broad deletion | Enterprise policy/config/state or cleanup isolation changes | EnterpriseAttestation option/XML, enterprise integration summary/config comments, README/config journey and authorization docs |
| E | C-004/C-009/C-010: secret outputs/response copies, ARKG profile, Get unsigned key8 omitted | Output owner, profile/key-validation, unsigned-output/extension support changes | COSE seed/signing XML and tests; response raw/unsigned/key docs; PreviewSignCbor/derived/generated-key/extension-output XML and storage examples |
| F | C-003/C-011/C-012/C-013/C-014/C-015: invalid PIN snippet combination, hash/client/RP responsibility, next loop, test overclaims/cleanup, runtime reset message | Any behavior or fixture/prerequisite change below the narrative | `src/Ctap2/README.md` create/get/PIN/security/interaction; module/tests CLAUDE; FidoTool make/get/enterprise/cleanup comments; package support/claim text |
| G | C-001=WA-XREF-001; C-002=WA-IMP-010 dependency; C-004 overlaps WA-IMP-006/013; C-009/E; BE/BS later passage needed | User filtering/ID recovery, attestation snapshot/secret owner, protocol/policy, ARKG/output support or BE/BS enforcement changes | WebAuthn `AuthenticationResponse`/`MatchedCredential` User/ID, matcher/build result, attestation RawCbor/Decode/Encode, UV/protocol/PIN and extension result XML; CTAP↔WebAuthn boundary matrix and resulting guides |

### Bounded missing proof and decisions, not hidden conclusions

1. No authoritative pinned vendor/prototype original was retrieved for the SDK's **kty2/alg-65700,
   byte-string-wrapped ARKG seed profile**. Draft-10 is demonstrably different; sibling agreement
   cannot resolve the target. E/coordinator must adjudicate and retain the experimental warning.
2. The exact first CTAP revision introducing unsigned outputs and the “early CTAP v4” historical
   attribution were not established. Current command-specific S23 clauses are established.
3. BE/BS meanings are not substantiated by the linked W2 base-layout passage; later original
   revision/section citation is a G/F dependency. No automatic citation upgrade was applied.
4. Enterprise ep enabled/support original clause needs A/D's assigned GetInfo/config investigation;
   the tests' actual weak/conditional assertions are independently established here.
5. Parser strictness, decoder snapshots/secret owners, empty user/API privacy policy, invalid option
   rejection, ARKG key validation and test cleanup remain **desired-contract decisions/Pass-2
   candidates**, not implementation changes authorized by this documentation cycle.
6. No ceremonies, protocol matrices, software certification/FIPS qualification, runtime repros,
   compiler gates or hardware success claims were produced. This report establishes complete C-owned
   investigation coverage and source/primary-passage evidence only.

### Final record-integrity check

A fresh read-only Python check after report creation found **30/30 owned source/test hashes still
equal to inventory**, **30 exact owned-file coverage rows**, **15 C-* candidate headings**, **12
named clean units**, and **zero local-link/coverage/hash errors** (exit 0). This is record integrity,
not SDK test execution, build validation or independent factual review. Cross-owner coordination is
provided through the explicit A–G handoff table for the coordinator to route; no direct session
messaging tool was available and no source edit ownership was assumed.
