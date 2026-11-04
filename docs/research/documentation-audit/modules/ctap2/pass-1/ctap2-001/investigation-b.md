# CTAP2 investigator B — PIN/UV

Date: **2026-11-03**. Baseline verified with `git rev-parse --short HEAD`: **5b09a9b6**
(inventory full baseline `5b09a9b6942ded3913b5c9340c3e126617e61a82`). Investigation only:
**no builds, tests, device/PIN operations, runtime fixes, source edits, commits or delegation**.
Only this report and `evidence/b/original-clauses.md` were authored.

Read root `CLAUDE.md`, available skill descriptions, `src/Ctap2/README.md`, module/test
`CLAUDE.md`, `docs/TESTING.md`, canonical workflow/style and run plan. There is no root
`tests/CLAUDE.md`; the applicable one is `src/Ctap2/tests/CLAUDE.md`. Loaded secure-credential-prompt
and security-guidelines skills. Build/test/commit skills are not applicable to this prohibited-work packet.

## Outcome and evidence boundary

All **13 owned paths** were read completely, including every test body/assertion and helper in
the owned files. Eight source files include the public permissions enum in `ClientPinSubCommand.cs`;
there is no separate `PinUvAuthTokenPermissions.cs` in the inventory. Five owned test files were
inspected, never executed. Original CTAP 2.1 errata was used for existing citations; original CTAP
2.2 was used only to verify the explicit 2.2+ read-only permission claim. Focused quotations,
exact anchors, dates, publication status, actors and normative strengths are retained in
[original evidence](evidence/b/original-clauses.md).

Consequential findings: incorrect KDF/padding prose; undocumented protocol disposal and returned
secret ownership; protocol fallback misread as negotiation; byte-only PIN validation versus Unicode
platform obligations; ambiguous missing retry state and unsupported UV power-cycle model; missing
peer COSE labels validation; overbroad cleanup guarantees; unmodeled persistent read-only token
conditions; misleading blocked-state/test claims. Documentation candidates below must be triaged by
the coordinator. Deferred implementation candidates are **not** permission to fix code in Pass 1.

## Exact owned-path dispositions

Every row covers the whole file: XML, executable behavior as evidence, internal comments, and all
assertions where present. Paths are the exact inventory entries.

| Owned path | Disposition | Semantic units / candidates |
| --- | --- | --- |
| `src/Ctap2/src/Pin/ClientPin.cs` | Read; doc corrections and deferred behavior | construction/disposal B-02; retries B-06; set/change/hash B-04; all token methods B-03/B-05/B-09; parsers B-11; cleanup B-08; clean B-C01–04 |
| `src/Ctap2/src/Pin/ClientPinSubCommand.cs` | Read; doc corrections | command/parameter/response constants; permissions B-03/B-06/B-09; clean B-C05–06 |
| `src/Ctap2/src/Pin/IPinUvAuthProtocol.cs` | Read; doc corrections | algorithm overview B-01; every crypto API ownership/key contract B-02; clean B-C07–08 |
| `src/Ctap2/src/Pin/PinRetryStatus.cs` | Read; doc qualification depends on parser decision | B-06; no independent model-storage defect |
| `src/Ctap2/src/Pin/PinUvAuthHelpers.cs` | Read; deferred security-critical validation | B-07; clean B-C09–10 |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs` | Read; doc specificity / deferred cleanup | B-01/B-02/B-08; clean B-C11–13 |
| `src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs` | Read; contract completeness / cleanup limit | B-02/B-08; clean B-C14–17 |
| `src/Ctap2/src/Pin/UserVerificationRetryStatus.cs` | Read; unsupported state claim qualification | B-06; immutable scalar storage itself clean |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EnhancedPinTests.cs` | Read every assertion; doc mismatch / deferred harness-lifetime issues | B-10/B-12; clean B-C18 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PinManagementTests.cs` | Read every assertion; doc correction / deferred cleanup and diagnostic strings | B-10/B-12; clean B-C19 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/ClientPinTests.cs` | Read every assertion and fake helper | B-10/B-11; clean B-C20–22; fake is not crypto interoperability evidence |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV1Tests.cs` | Read every assertion | clean B-C23–25; omitted peer alg illustrates B-07; no memory-safety/device proof |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Pin/PinUvAuthProtocolV2Tests.cs` | Read every assertion | clean B-C26–28; omitted peer alg illustrates B-07; missing KDF vector noted B-11 |

## Candidates: documentation versus deferred implementation

Evidence abbreviations `S21-*`, `S22`, `NET10` resolve to the retained original evidence file.
Source line numbers are at the baseline. Severity here is investigation priority, not a certification
verdict. Each dependency is a precise future documentation re-review trigger.

### B-01 — protocol algorithm prose and padding are wrong or underspecified

**Candidate-doc; high factual priority.** `IPinUvAuthProtocol.cs:26–30` says V1 HKDF-SHA-256;
V1 `Kdf` actually SHA-256(z), `PinUvAuthProtocolV1.cs:74–87`, established by inspected
`Kdf_IsSha256` assertions. COSE alg=-25 does not alter that. `src/Ctap2/CLAUDE.md:461` says
“64 bytes, PKCS#7 style”; `ClientPin.cs:513–518` zero-fills to 64, and both AES implementations
set `PaddingMode.None`. **Desired docs:** V1 SHA-256, V2 two independent HKDF derivations;
PIN right-zero-padding separately from no AES padding. Replace generic “security limitations”
in V1 XML with concrete V1/V2 differences or retain only a supported, scoped security rationale;
original §6.5.7 explains certification motivation, not a blanket insecure-V1 verdict.

Evidence: S21-encoding/key. Dependencies: interface remarks; V1/V2 algorithm remarks;
module `CLAUDE.md` Common Pitfalls / PIN protocol sections; F-owned example explanations that
name KDF/padding; E hmac-secret explanation if it duplicates the crypto contract. No runtime defect
is alleged for actual zero-padding or V1 SHA-256.

### B-02 — protocol/helper/returned-buffer lifetimes need explicit ownership contracts

**Candidate-doc.** `ClientPin` stores the supplied session/protocol but `Dispose():689–695`
**disposes the protocol**, not the session. Constructor XML does not say so. Tests
`Dispose_DisposesProtocol` and `Dispose_CanBeCalledMultipleTimes` assert that behavior.
`Protocol` still returns its reference after disposal; V1/V2 Version/TagLength remain readable,
while crypto operations throw. The module's “disposed-instance misuse throws” should refer to
crypto operations, not promise every property is guarded.

All token methods return newly allocated decrypted byte arrays, yet their `<returns>` omit
caller-owned/zero-in-finally guidance. `Encapsulate.SharedSecret`, `Kdf`, `Decrypt`, `Authenticate`
outputs likewise escape ownership; none are retained for protocol disposal to clear. Interface
key parameters do not explain accepted lengths or token versus encapsulated-secret roles.
Encrypt/Decrypt do **not** invoke Kdf internally despite “with the protocol's key derivation” prose.
`Verify` is local MAC verification, not a device token-validity/permission/expiry check.

**Desired docs:** session borrowed; helper disposes supplied protocol; outputs caller-owned;
use borrowed inputs only through the completed operation and clear after completion including failure.
Describe crypto key lengths per byte matrix and separate independent local MAC checks from authenticator
authorization. Explain that `using protocol` clears no caller output.

Dependencies: `ClientPin` class/ctor/Protocol/Dispose and all token returns; complete interface
crypto members; module README Security notes and PIN task, CLAUDE Protocol Disposal; F examples
that keep a protocol after disposing ClientPin; G `src/WebAuthn/src/Client/WebAuthnBackend.cs:71–136`
uses undisposed short-lived wrappers intentionally to avoid disposing its shared protocol. Do not
blindly add using wrappers in that backend. Any future borrowing redesign reopens all these docs.

### B-03 — token-command fallback is not protocol negotiation or arbitrary authorization

**Candidate-doc; deferred capability-policy decision only if later requested.**
`ClientPin.cs:343–352` obtains **fresh GetInfo** from the real session, tests option
`pinUvAuthToken` true, and falls back to 0x05 otherwise. It retains the supplied protocol, ignores
requested permissions/RP during legacy fallback, and never retries a failed 0x09 exchange with 0x05.
Only None is locally rejected; unknown flag bits are encoded. Constructor performs no negotiation.
Permission-method XML currently lacks fallback and required caller protocol selection.

**Desired docs:** caller selects a mutually supported protocol using `PinUvAuthProtocols`; V2
preference is policy, not guaranteed support. Legacy fallback requests no permissions/RP; it does
not promise cm/be/lbw/acfg/pcmr authorization. Under S21 its ordinary default mc/ga and first-use
RP binding have a noMcGaPermissionsWithClientPin exception. Permission combinations may be requested
where valid; not every flag combination is valid (see B-09). `LargeBlobWrite` means write authorization,
not all reads. `ClientPinParam.PinUvAuthProtocol` XML says globally “required” though S21's general
table makes it optional and retry subcommands list only subCommand.

Evidence: S21-negotiation/scope; actual fallback source; unit token-permission test asserts only
non-null, not fallback/protocol fields. Dependencies: ClientPin ctor/class/token methods; constants /
enum XML; README PIN protocol prerequisites and matching-permission sentence; CLAUDE PIN fallback;
FidoTool PIN/token examples; G backend hard-coded V2 and WebAuthn CLAUDE UV Decision / README limitations.
G should preserve existing WebAuthn negotiation limitation, not count sibling agreement as conformance.

### B-04 — 4–63 bytes is actual validation, not complete Unicode PIN validity

**Candidate-doc plus deferred-IMP security/platform-validation gap.** `ValidatePin:498–511`
checks only byte count for every PIN argument; it does not validate UTF-8, NFC, Unicode characters /
code points, advertised MinPinLength, or final zero. Concrete static traces: one U+1F600 is four
UTF-8 bytes and passes local validation despite too few characters/code points; `1234 || 00`
passes, is indistinguishable from `1234` when setting the padded PIN, but later raw-byte hashing
includes 00 and can produce a wrong-PIN attempt. Invalid UTF-8 of length four also passes locally.
No experiment/device operation was performed.

**Desired Pass-1 docs:** retain truthful byte-only SDK check, explain caller's UTF-8/NFC/non-NUL
and user-visible/current-device minimum obligations. Do not imply byte count establishes compliance.
Authenticator may reject additional policy and forcePinChange; token use is not PIN setting. Desired
Pass-2 enforcement allocation (SDK versus credential input layer) needs maintainer decision; do not
silently normalize secret bytes or add immutable PIN strings to solve it.

Evidence: S21-encoding; byte-length tests check ASCII/ParamName only, no Unicode assertions.
Dependencies: all ClientPin PIN params/exceptions/private ValidatePin prose; README PIN task; F PIN
input/sample docs; A AuthenticatorInfo.MinPinLength / ForcePinChange; G prompt length code-points versus
bytes and direct PIN documentation. On future validation fixes, re-review public errors and prompts,
not just shorten the existing “4–63 bytes” phrase.

### B-05 — built-in UV capabilities, permissions and interaction are distinct from PIN

**Candidate-doc, with capability/retry policy deferred to coordinator/G.**
`ClientPin.cs:396–460` checks only nonzero permissions, performs getKeyAgreement and 0x06,
and does not check uv, pinUvAuthToken, uvBioEnroll/uvAcfg or do a UV/PIN retry loop.
XML names only options.uv as prerequisite. Built-in UV requires configuration and token capability;
be/acfg have extra permission-specific gates. PIN issuance does not assert UP; built-in UV may.
“cannot be automated” is a harness interaction restriction, not a universal protocol theorem.
Generic CBOR receives no operation-specific notification handle, so no touch prompt is guaranteed
by this helper; README “ClientPin commands are silent” must distinguish SDK notifications from
authenticator biometric/display interaction.

**Desired docs:** enumerate capability/current-state requirements, say errors come from the
authenticator if it cannot issue the token, explain caller/higher-level retry choice and interaction.
Evidence: S21-scope/state; `Ctap2Session.SendCborRequestAsync:395–403` passes notification None;
tests use a fake decrypt and assert non-null, not biometric execution. Dependencies: ClientPin UV token /
UV retries XML; README User interaction and PIN versus biometric phrasing; CLAUDE notification section;
A backends; D bio/config permission prerequisites; G `WebAuthnClient.PinUvAuth.cs` / UV Decision claims.

### B-06 — retry models lose unknown PIN state and imply nonexistent UV power-cycle evidence

**Candidate-doc plus deferred-IMP model/parser contract decision.**
`ParsePinRetriesResponse:534–562` defaults to retries=0/powerCycle=false; omitted optional
powerCycleState and explicit false are indistinguishable. S21 explicitly says omission gives **no
information**, not “no cycle required”. `ParseUvRetriesResponse:564–592` accepts field 4 though S21
says it is only valid for getRetries, and `UserVerificationRetryStatus.PowerCycleRequired` advertises
a UV meaning the standard does not supply. UV test with false omits field 4 entirely, so it asserts
only the parser default; no owned test sends a true UV power-cycle state.

**Desired Pass-1 docs:** model properties report parsed/defaulted values; false is not evidence a
power cycle is unnecessary; UV result has no CTAP 2.1 standardized power-cycle signal. Explain
PIN_INVALID versus PIN_AUTH_BLOCKED (temporary power-cycle) versus PIN_BLOCKED (exhausted; reset),
and UV_BLOCKED recovery by successful configured ClientPIN or reset under S21. Never recommend
blind retries or infer device action did not occur from a failed call. Deferred decision: preserve
absent/unknown semantics and reject unsupported/malformed status instead of manufacturing certainty.

Dependencies: both retry models; ClientPin retry XML/parser comments; ClientPinResponse.PowerCycleState;
README retry paragraph; A CtapStatus/CtapException and common fixtures; G WebAuthn backend drops
PowerCycleRequired entirely and prompt loop must not gain a guarantee from false/default0.

### B-07 — security-critical COSE input validation does not match strictness claims

**Deferred-IMP; high security-boundary priority; candidate-doc precision without weakening requirement.**
`PinUvAuthHelpers.cs:55–75` checks only X/Y byte-array type and 32-byte lengths. It never checks
kty, alg or crv despite constants for those labels; import forces nistP256 regardless of input labels.
Thus valid P-256 coordinates with absent/wrong kty/crv/alg reach ECDH as P-256. Both owned
Encapsulate_ValidPeerKey tests **omit alg** and accept it. Returned platform key labels are correct.
Point validity is delegated to .NET import/derivation; import at line 88 is outside the catch at
92–99, so XML promising ArgumentException for all malformed keys is too broad/unverified.

**Desired:** retain required S21 peer structure and strict malformed/security data policy; document
actual explicit checks and platform crypto error possibility without pretending relaxed checks are
intended forward compatibility. Later enforce required peer labels/structure and meaningful regression
cases. Additional optional fields handling requires the exact standards boundary, not blind echoing.
No exploit or cross-platform point behavior was executed. Evidence S21-key/NET10.

Dependencies: helper/Encapsulate XML; `src/Ctap2/CLAUDE.md:307` security-critical boundary;
interface malformed-key exception; V1/V2 tests; A response parsing / C COSE forward-compat docs;
E hmac-secret and G PRF key agreement, because both depend on this helper. A fix must reopen these
claims together; do not count as separate bugs per consumer.

### B-08 — cleanup claims must stop at actual buffers, including exceptional paths

**Candidate-doc plus deferred-IMP memory-hygiene group.**
Positive: ClientPin finally clears returned shared secret and its named PIN hashes/padded plaintext/
encrypted PIN/auth-param arrays. Encapsulate finally clears rawZ; both ECDH objects are using-scoped.
V2 Kdf clears partially derived secret on exception. Verify clears expected MAC in finally and uses
FixedTimeEquals. These are not complete end-to-end cleanup.

Observed gaps: `WithBytes` clones every PIN ciphertext/MAC into captured builder arrays that have
no clearing owner (`CtapRequestBuilder.cs:76–94`); ClientPin request arrays are never cleared, and
generic `SendCborRequestAsync` does not clear caller requests. `ParsePinTokenResponse` allocates
encrypted-token bytes with no cleanup, and successful transport response is transferred without a
ClientPin cleanup. Both Decrypt methods allocate returned plaintext inside try but do not clear it
if TransformBlock fails before return; V1 Authenticate fullHash stack buffer is not explicitly zeroed.
Partial-output failure residue is a **static exceptional-path risk**, not an executed fault injection.
Helper constructs public-key dictionary after raw z derivation, outside a raw-z cleanup guard if that
allocation fails. Underlying crypto provider internal copies are not characterized.

**Desired docs:** replace README “ClientPin zeroes what it derives internally” with bounded ownership
and cleanup, not a blanket guarantee; explain borrowed caller arrays and returned token separately.
Preserve legitimate internal finally guarantees. Pass-2 needs request/builder/response ownership and
error-path clearing review, not a claim that disposing a stateless protocol erases all secrets.

Dependencies: README Security notes; CLAUDE Memory Handling/Protocol Disposal; ClientPin and protocol
XML; A builder and raw response/session docs; E secret extension cleanup; G WebAuthnBackend token
ownership comments and `PinUvAuthTokenSession` disposal. Any fix changing buffer ownership reopens
all dependency claims. No named owned assertion checks zeroing, cancellation cleanup or provider copies.

### B-09 — 0x40 is persistent read-only, cannot be freely combined, and needs capability evidence

**Candidate-doc; capability enforcement/typed-token design deferred, not assumed necessary API work.**
`ClientPinSubCommand.cs:119–169` introduces enum as CTAP2.1, says multiple permissions can combine,
then names 0x40 generic CredentialManagementRO with 2.2+ prose. Original 2.2 §6.5.5.7 says **pcmr /
persistent** token; RP ID ignored; `perCredMgmtRO` true; any other permission in the request causes
UNAUTHORIZED_PERMISSION. SDK encodes enum as-is and returns raw bytes; it does not represent
persistent identity or enforce the combination/capability. That is not evidence 0x40 universally works.
encIdentifier uses persistent token, not any full-CM token by implication. Exact 2.2 original has no
encCredStoreState; that metadata claim requires E/A's original vendor/later-revision evidence.

**Desired docs:** ordinary permissions apply from 2.1; explain 0x40 separately under exact 2.2
reference and capability, exclusion of combinations, persistent versus ordinary lifetime, raw token
caller ownership. Do not rename public enum or invent persistence guarantees in Pass 1.
Dependencies: enum type and CredentialManagementRO remarks; both permission token methods; D CM
read-only behavior; E encrypted metadata decryptor; A AuthenticatorInfo; F encrypted metadata examples;
G any matching token-lifetime claims. Re-review when capability, persistence or metadata-key contract changes.

### B-10 — owned test prose/labels exceed assertions and blocked recovery is conflated

**Candidate-doc; diagnostic string changes deferred because executable tokens are frozen.**
`Ctap2PinManagementTests.cs:83–89` comment treats both PIN_BLOCKED/PIN_AUTH_BLOCKED as reset required;
temporary PIN_AUTH_BLOCKED needs power-cycle, not necessarily destructive reset. Skip message strings
repeat this at 88/130 and conflate original CTAP ten-second condition with product instructions;
leave those strings for authorized Pass 2, but correct surrounding explanatory docs now with A/F.
The change-PIN test checks a nonempty token from changed PIN, not invalidation of the old PIN/token;
restoration is best effort and may itself submit a wrong PIN if change failed/was cancelled.

UV-discouraged test XML says UserVerification false but executable options leave it **absent** and
provide pinUvAuthParam. Assertions only verify nonempty credential; no UV flag assertion. Comments
160–162 claim false is rejected under CTAP2.0 without exact original/version evidence; S21 §6.1 has
default false and a non-normative false-UV path. Broad catches at 180–189 classify several errors as
authenticator policy without determining wire/source cause. Do not turn catches into a conformance verdict.

Enhanced PIN class claims complexity; compliant test returns early below 5.8, otherwise calls
SetOrVerify and asserts only clientPin=true. No pinComplexityPolicy/negative complexity assertion.
Other tests conditionally assert scalar types/ranges; absence can do nothing. ClientPin unit
`GetPinRetriesAsync_SendsCorrectProtocolVersion` merely counts one send; adjacent canonical test
actually decodes and checks Version/SubCommand. Do not transfer the latter assertion to the former.

Dependencies: owned integration/XML/comments; Unit ClientPin test comment units; F tests CLAUDE PIN
management / safe lanes; A common fixtures and reset policy; G UV-discouraged boundary documentation.
Executable names/assertions/strings remain unchanged in Pass 1; explain precise coverage in class XML
and nearby comments instead. Test runtime gaps remain B-12, not independently counted per prose surface.

### B-11 — missing response/negative/wire evidence, not broad test security assurance

**Deferred-IMP verification/response-validation group.** All ClientPin parsers read a map without
checking complete consumption; retries default missing counts to zero and accept signed/negative ints.
Token parser requires field 2 but Decrypt accepts arbitrary nonempty block counts: raw plaintext token
length is not validated against S21 V1 16/32 or V2 32. A well-formed V2 IV + 48 ciphertext bytes would
return a 48-byte token, only rejected later if passed to Authenticate. This is not ciphertext-integrity
validation or proof of a usable token. No owned negative response-length/required-label tests establish
strictness or forward-compatible complete-envelope behavior.

Owned ClientPin tests check send counts, ParamName, simple retry results, non-null fake tokens,
dispose idempotence, and actual canonical retry fields. Fake Encrypt/Decrypt are identity copies,
Verify always true; they cannot establish CTAP cryptographic interop, RP binding, negotiated protocol,
permission fallback, zeroing, cancelled exchanges or a coherent state sequence. V2 tests check KDF
length/determinism but no independent exact HKDF two-info vector; roundtrip alone cannot prove CTAP
encoding. V1 does compare Kdf/HMAC to SHA256/HMAC expected computations.

**Desired:** Pass-2 choose strict response/count/length validation and actual fakeable byte/state/
ownership regression tests where meaningful. Avoid reporting broad security properties from test names.
Dependencies: parsers/token/retry XML and B-06; interface crypto result lengths and B-07; owned test
class explanations; A parser/raw escape-hatch docs; E/G reliance on returned token length. Unknown
fields should be tolerated without relaxing required security fields; decide response policy explicitly.

### B-12 — owned integration cleanup and default normalization obstruct safe evidence

**Deferred-IMP test/harness behavior, ownership primarily A for shared units.**
Owned integration token arrays/authParams are not zeroed. EnhancedPin_CompliantPin discards returned
ClientPin; ClientPin_RetrievesPinRetries leaves helper/protocol undisposed. Scalar GetInfo enhanced
tests use default `WithCtap2SessionAsync(normalizePin: true)` although three lack UP trait: helper
normalization can set/change PIN or request a token **before** read-only action. It is not safe evidence
of a read-only lane. Common normalization can skip/fail before the changed-PIN ceremony starts;
restoration only runs inside action, so no “all tests restore” claim follows.

Shared `Ctap2TestHelpers.SetOrVerifyPinAsync:59–61` discards decrypted token; default normalization
`TestExtensions/Ctap2TestStateExtensions.cs:97–138` has force-change temp buffer only cleared on
success, discards token and conflates blocked states. Those shared paths belong to **A**, read here
only to bound owned test meaning; coordinator should merge duplicates with A rather than add bug counts.
Do not run or repair harness behavior during this documentation packet.

Dependencies: owned tests class summaries/cleanup comments; A common fixtures/test helpers; F module
tests CLAUDE safe lane and normalization prose; G WebAuthn integration helpers if sharing assumptions.
After fixes, re-review whether each test truly executes the claimed operation and how skips/cleanup
are reported. No run status is inferred from inspected code.

### B-13 — cross-owner PIN ceremony guide conflicts with CTAP uv/token requirements

**Candidate-doc; deferred request-enforcement gap owned C/A/G (deduplicate there).**
`src/Ctap2/README.md:116–158` says UserVerification alone rejected, then sets both token parameter
and UserVerification(true). Source does not locally reject UV alone, and encoder can send both uv and
pinUvAuthParam (`Ctap2SessionRequestEncoding.cs:54–64,94–103,154–158,181–185`). S21 platform MUST NOT
send both; uv alone is built-in UV capability-dependent, not PIN collection. Valid token auth is
the PIN/UV authorization route; omit uv option even false alongside token.

**Desired docs:** correct PIN task to supply auth parameter/protocol **without** setting uv; distinguish
built-in UV's older uv option path and request versus verified result. Correct related options XML,
not invent SDK rejection. Pending behavior enforcement is separate. Dependencies: F README Verify user
and assertion fragments; C Make/Get options UserVerification/PinUvAuthParam docs; A session raw contract;
G `WebAuthnBackend.cs:157–160` already preserves uv omission in its adapter and WebAuthn guide claims.
Cross-layer equality is not required when WebAuthn legitimately does higher-level validation.

## State-sequence matrix contributions

These are baseline static observations with S21 requirements alongside, not executed state-machine tests.

| Row | Sequence / state | Protocol/capability conditions | SDK observed behavior / completion boundary | Test evidence / gap |
| --- | --- | --- | --- | --- |
| B-S01 | Choose protocol → getKeyAgreement → Encapsulate → PIN command | mutually supported protocol; each clientPIN transaction new secret | caller supplies object; helper fetches key on each set/change/token request; no negotiation | V2 repeated Encapsulate asserts different public coordinates, not shared-secret/device state |
| B-S02 | No PIN → SetPin → PIN configured | clientPin supported; NFC UTF-8 composition/minimum; PIN not already set | validates bytes only; zero pads; awaits set; caller can refresh GetInfo afterward | unit counts two sends; enhanced integration asserts clientPin true only if reached |
| B-S03 | Current PIN → ChangePin → new hash / token invalidation | device policy/forcePINChange; S21 resets all tokens | helper authenticates newPinEnc || pinHashEnc; no retained SDK auth state; does not guarantee rollback on failure | integration gets token using new PIN, best-effort restore; never checks old token/PIN rejection |
| B-S04 | Permission token request → capability fallback | pinUvAuthToken true → 0x09, otherwise 0x05; selected protocol unchanged | fresh real-session GetInfo; legacy permissions/RP dropped; failures not retried | fake permissions test non-null only; no fallback-wire assertion |
| B-S05 | RP-scoped token → mc/ga use → UP consumes most permissions | scoped RP required; cm optionally scoped; be/lbw/acfg ignore RP | raw token returned, no metadata/checks; device enforces actual lifetime/scope; local Authenticate only MAC | no RP mismatch, permission consumption or UP-state assertions |
| B-S06 | New ordinary token → prior tokens invalid | S21 ordinary issuance resets all protocol tokens | each request returns independent array; zeroing old host array is not device invalidation | no owned token invalidation sequence |
| B-S07 | Wrong PIN → retries decrease → third mismatch temporary lock / zero exhausted lock | PIN_AUTH_BLOCKED power-cycle versus PIN_BLOCKED reset | CTAP exception propagated; no retry loop; GetPinRetries is a separate snapshot | fake count/boolean tests only; no failed PIN device operations performed |
| B-S08 | UV request → UV_INVALID / UV_BLOCKED → configured PIN recovery | uv + pinUvAuthToken true; permission-specific uvBioEnroll/uvAcfg | single 0x06; caller chooses retry or PIN fallback; generic notification None | UV fake returns token; no biometric/counter/recovery assertions |
| B-S09 | Read retry snapshot missing powerCycleState | optional PIN field means unknown; not valid UV response field | models collapse absent to false, missing count to 0; no device enforcement shifted to host | false test responses omit field; PIN true test parses true; no UV true test |
| B-S10 | Token timers / power cycle / reset | device-defined shorter initial/max periods, optional rolling timer; reset invalidation | byte-array remains until caller clears; stateless protocol disposal does not manage device timer | no expiry/consumption/reset sequence executed/established |
| B-S11 | pcmr-only request → persistent token → read metadata | S22 perCredMgmtRO true; no combination; RP ignored | generic raw byte token request; no persistent token type/capability check | no owned pcmr test; E metadata evidence needed |
| B-S12 | Cancellation or error during getKeyAgreement / final exchange | transport admission/cancellation separate from operation result | tokens/hashes in established finally clear; no retry/rollback; cancellation may follow admitted device work | no owned cancellation assertion; transport timing belongs to A |

## Byte-contract matrix contributions

| Row / value | Representation / lengths | Ownership / copies | Disposal / failure limits |
| --- | --- | --- | --- |
| B-B01 PIN/currentPin/newPin | borrowed UTF-8 bytes; local 4–63 bytes; caller NFC/composition/minimum/final-NUL obligation | no direct private input clone; exact supplied bytes hashed/copied to pad | caller clear after completed await, including error; immutable string literal sample is only demonstration, not secure prompting |
| B-B02 padded newPin | exactly 64: input || zero bytes; never PKCS#7 | ClientPin new array | cleared in finally |
| B-B03 PIN hash | first16 SHA256(exact supplied bytes) | stack full32 cleared; returned16 ClientPin-owned | cleared in finally; no NFC normalization |
| B-B04 peer key / platform key | COSE EC2 P256 kty2/alg-25/crv1; x/y32 big-endian | coordinates public; helper borrows peer byte arrays; exports own public coordinates | required labels not explicitly validated; no secret-clearing need for public coords |
| B-B05 raw ECDH z | 32-byte big-endian x-coordinate contract | helper returns owned z to Encapsulate | Encapsulate finally clears z; crypto providers using-scoped; post-derive allocation failure not guarded in helper |
| B-B06 V1 sharedSecret | SHA256(z),32 | newly allocated caller-owned; Kdf input borrowed | ClientPin finally clears; direct caller must clear; protocol Dispose not owner |
| B-B07 V2 sharedSecret | HMAC32 || AES32,64; separate HKDF labels | newly allocated caller-owned; empty-salt output equivalence reasoned, not vector-tested | Kdf catches/clears partial; ClientPin finally clears; direct caller clear |
| B-B08 AES plaintext/ciphertext | nonempty multiple16 in SDK; V1 CBC zero IV ct lengthN; V2 randomIV16 || ct lengthN+16 | inputs borrowed; crypto temporary array copies; returned output owned | no AES padding; no authentication/decryption-integrity promise from Decrypt; failed plaintext output not guarded |
| B-B09 newPinEnc/pinHashEnc | V1 64/16 bytes; V2 80/32 bytes | ClientPin arrays plus WithBytes clones plus request encoding | named arrays clear; captured clones/request do not |
| B-B10 set/change auth message | set newPinEnc; change newPinEnc || pinHashEnc, V1 80 / V2 112 | ClientPin change message private array | message/authParam cleared in finally; builder copies escape it |
| B-B11 Authenticate / Verify keys and tags | V1 auth key16 or32,tag16; V2 auth key32(token) or64(secret), first32 used,tag32 | inputs borrowed; new caller-owned tag; expected Verify tag temporary | FixedTimeEquals/expected zeroing; V1 full32 stack MAC not explicitly cleared; no live-device authorization check |
| B-B12 encrypted / decrypted token | S21 token V1 16/32; V2 32 (wire encrypted V1 16/32; V2 48) | response borrowed; parser encrypted copy; Decrypt new plaintext transfers to caller | token plaintext length not enforced by ClientPin; caller clear token; encrypted copy/response not cleared by helper |
| B-B13 permissions / rpId | uint flags + optional CBOR text; null/empty string omit | strings public policy scope, not PIN; no returned token scope metadata | mc/ga required, cm optional, pcmr-only exception; not locally enforced; fallback drops both |
| B-B14 retry values | parsed int / bool scalar; device spec uses unsigned count; PIN powerCycle optional | immutable scalar model snapshot | absent becomes false/0; UV field4 has no S21 meaning; scalar disposal unnecessary |

## Clean semantic units for coordinator sampling

“Clean” means the bounded comment/contract below matches inspected code/assertions. It does not
wash out adjacent candidates, imply execution, or endorse incomplete API prose.

| ID | Exact unit / scope | Why clean |
| --- | --- | --- |
| B-C01 | ClientPin Set/Change hash/auth-message comments 210–223 | truncates SHA256 and orders newPinEnc before pinHashEnc as S21 |
| B-C02 | ClientPin ComputePinHash 520–532 | returned first16 escapes; stack hash zeroed finally |
| B-C03 | ClientPin borrowed PIN-buffer parameter ownership sentences | methods never clear supplied memory; correct caller-clear responsibility |
| B-C04 | ClientPin GetKeyAgreement summary/returns 462–481 | fetches and returns parsed public key dictionary |
| B-C05 | ClientPinSubCommand public numeric codes 27–65 | 01/02/03/04/05/06/07/09 match S21 subcommand table |
| B-C06 | Permissions mc/ga/cm/be/acfg names/value summaries | exact operation authorization roles; scope qualifications handled separately B-03 |
| B-C07 | Interface Authenticate tag-size returns 89–98 | V1 left16, V2 full32 matches source and S21 |
| B-C08 | Interface Encrypt returned wire-shape 76 | V1 ciphertext versus V2 IV-prefix matches implementations |
| B-C09 | Helper coordinate-length comment/checks 66–75 | P256 length32 checks actually performed |
| B-C10 | Helper raw-z caller-zero requirement / ephemeral key 44–47,77–79 | Encapsulate owns zeroing after return; using-scoped generated key |
| B-C11 | V1 Kdf local comment 83 | SHA256, not HKDF, actual source |
| B-C12 | V1 ciphertext/no-IV comment145–150 | nonempty block-aligned V1 data, no prefix |
| B-C13 | V1 Verify core 206–219 | exact-length early false; constant-time compare; expected cleared |
| B-C14 | V2 class crypto-list/shared-secret layout 24–37 | separate keys64/random IV/full HMAC32 matches source/S21 |
| B-C15 | V2 IV||ciphertext comments 152–169,192–203 | V2 representation and extraction actually implemented |
| B-C16 | V2 Authenticate token/secret comment241–252 | lengths32/64 and first32 HMAC role correct |
| B-C17 | V2 Kdf catch / Encapsulate finally | explicit clearing guards on these owned intermediate values |
| B-C18 | Enhanced ClientPin_RetrievesPinRetries assertion meaning111–114 | only 0≤count≤8, consistent with baseline range (not execution proof) |
| B-C19 | PIN management best-effort restoration comment103 | correctly admits PIN may remain changed |
| B-C20 | ClientPinTests class remarks26–29 / Fake protocol implementation | accurately says mock session / concrete fake, no crypto-conformance promise |
| B-C21 | ClientPinTests ParamName rationale110–115 / assertions128,193,203,213,253 | assertions really identify the public parameter name |
| B-C22 | ClientPinTests canonical PIN/UV retries351–394 | asserts command byte, map size, ordered keys/version/subcommand |
| B-C23 | V1Tests Kdf_IsSha256123–132 | compares independent SHA256 computation to actual Kdf result |
| B-C24 | V1Tests Authenticate_IsTruncatedHmac245–257 | compares first16 of HMACSHA256 to tag |
| B-C25 | V1Tests roundtrip / disposal scope195–206,299–310 | checks plaintext equality and five crypto methods after dispose, not all properties |
| B-C26 | V2Tests encrypt-return shape160–171 | asserts N32 gives IV+ct48 |
| B-C27 | V2Tests Verify_TamperedMessage355–366 | genuinely checks modified message fails local Verify |
| B-C28 | V2Tests Encapsulate_ReturnsUniqueKeyAgreement383–404 | compares public X/Y between calls; does not assert cleanup or remote key agreement |

## Narrow external dependency reads and coordination

Read as evidence only: `src/Ctap2/src/Ctap2Session.cs`, both backend files,
`src/Ctap2/src/Cbor/CtapRequestBuilder.cs`, `Ctap2SessionRequestEncoding.cs`,
both credential options files, shared `Ctap2TestHelpers.cs` and
`TestExtensions/Ctap2TestStateExtensions.cs`. Narrow grep read CtapException status mapping and
AuthenticatorInfo encrypted-metadata fields. Read WebAuthn CLAUDE relevant UV/ownership sections and
`src/WebAuthn/src/Client/WebAuthnBackend.cs`; searched WebAuthn PIN-related paths to identify G's
dependencies, but did not claim a complete WebAuthn audit or inspect every searched test result.
`docs/usage/user-interaction.md` PIN terminology was a narrow search only, not a clean whole-unit claim.

Coordination: A owns common fixture edits and generic response/builder lifetimes; C owns uv/token
request semantics and options; D owns administrative permission consequences; E owns crypto extension /
encrypted metadata consequences; F owns guides/sample/test Markdown; G owns cross-layer disposition.
Use B-07/B-08/B-13 as shared evidence rather than creating one bug per consumer.

## Research bound / remaining evidence gaps

- Initial original CTAP 2.1 read and focused anchor check settle main protocol/encoding/state claims.
  Original 2.2 read plus one focused section follow-up settle pcmr conditions. Its encCredStoreState
  provenance is not in that original; hand off exact missing vendor/later-publication evidence to E/A.
- Context7 framework search and focused official .NET sources did not establish cross-platform ECDH
  import exception behavior or provider-internal key clearing. Do not advertise a single exception
  or complete-provider erasure. No additional runtime research is authorized here.
- Empty HKDF salt versus 32-zero-byte salt is not classified as a defect: HMAC block-key zero padding
  makes them mathematically equivalent. Exact .NET/platform vectors remain unexecuted.
- CTAP §6.6 ten-second/no-display requirement does not settle YubiKey's five-second product window;
  A/F adjudicate original product evidence and preserve distinct protocol/product conditions.
- The owned test's CTAP2.0 false-UV rejection claim lacks exact original evidence; S21 does not justify
  it. Record as unsupported prose; do not make another-version conformance claim to rescue it.
- No observed hardware retry state, biometric modality, persistent token, Unicode PIN interoperability,
  cancellation sequencing or cleanup fault injection. This report establishes inspected source and
  assertions only. Documentation, implementation and hardware status remain separate.
