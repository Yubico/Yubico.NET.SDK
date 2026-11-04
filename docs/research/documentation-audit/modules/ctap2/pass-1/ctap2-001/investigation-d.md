# Investigation D — administrative operations

> **Coordinator erratum, 2026-11-03:** D-08's emitted-wire defect inference is rejected. The actual
> System.Formats.Cbor10.0.2 Ctap2Canonical writer automatically sorts the inspected insertion order.
> The alphabetical-canonical explanation needs correction, but neither an encoding fix nor a broken
> update-user-operation warning follows. See [framework-adjudication.md](framework-adjudication.md) and
> CTAP-IMP-017 in [triage.md](triage.md). The original investigation below is preserved as provenance.
>
> **Later status erratum:** D-14's wrong-status inference is also rejected for the inspected call.
> The test uses legacy getPinToken0x05, whose original CTAP2.1 forced-change response is PIN_INVALID;
> 0x09 permission issuance instead returns PIN_POLICY_VIOLATION. See review-b-followup.md and
> review-d-followup.md. Other D-14 setup/cleanup/evidence limitations remain unchanged.

**Date:** 2026-11-03. **Investigator:** GPT-6.1 Sol, requested subagent; no delegation.
**Baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82` (HEAD independently read).
**Status:** investigation complete for 20 owned paths; candidates await coordinator triage and
fresh independent review. Documentation completion, implementation completion, and hardware
qualification are separate. All 20 current SHA-256 values match inventory.json.

Read root CLAUDE.md; available-skill catalog; module README/CLAUDE/test CLAUDE; docs/TESTING.md;
campaign workflow, approved style and this run's plan. Loaded domain-security-guidelines for
sensitive-data review, using root semantic classification and original specifications rather
than treating that skill's general firmware table as evidence. Build/test/commit skills not
applicable: none of those actions authorized or performed. No source edits, assertions, policy
changes, executable samples, device calls, credential deletion, enrollment, PIN or config calls.
Loaded workflow-verify before handoff for record/scope verification only; its example direct build/test
commands are not applicable and were not run. Only this report and `evidence/d/primary-quotes.md` are authored. Read-only Python hash comparison
is an inventory check, not a test run. No passing-suite claims are made.

## Primary evidence and research boundary

[Focused official quotes](evidence/d/primary-quotes.md) provide exact revision/status, section,
actor and normative strength. S1–S8 refer to CTAP2.1 Proposed Standard, 2022-06-21 errata;
V1 is current Yubico product/manual evidence, not a standard; L1 is Microsoft framework evidence.
Initial official research plus one focused follow-up completed. Missing original preview wire
definitions, exact earliest interoperable standard-command firmware, and vendor-specific bio
fields are explicitly unresolved. No automatic citation upgrade to CTAP2.3; no copied firmware
claim from sibling SDKs. Standard clauses inside labeled Notes are distinguished from normative
algorithms and RFC2119 requirements. Product metadata, fake tests and the presence of APIs prove
neither biometric enrollment nor FIPS/CMVP/device qualification.

## Numbered candidates — documentation versus deferred implementation

### D-01 — biometric subcommands, sensor-info request, auth transcript and timeout

**Deferred IMP; high; source-observed protocol mismatch.**
`src/Ctap2/src/BioEnrollment/BioEnrollmentSubCommand.cs:27–60` shifts every implemented
biometric subcommand by one relative to S4 (begin=2 rather than 1, next=3 rather than 2,
cancel=4 rather than 3, enumerate=5 rather than 4, rename=6 rather than 5, remove=7 rather
than 6; sensor constant=1 rather than 7). FingerprintBioEnrollment.cs:286–299 sends only
modality for sensor info; S4 requires sensor subcommand 7. All authenticated builders omit
the modality byte from the authenticated message (301–400, 422–545). Timeout is incorrectly
top-level key 6 integer (301–400), which S4 assigns to Boolean getModality; timeout belongs
in parameter-map key 3 and in the signed parameter-map bytes.

Desired later behavior: reconcile standard 0x09 against original §6.7, separately investigate
prototype 0x40; correct actual bytes only in Pass 2 with meaningful wire assertions. Do not
change constants or tests now. Unit tests inspect modality-only sensor map and top-level timeout,
use all-zero fake auth, and mostly count sends; no real transcript verification. Public XML
currently implies a working standardized enrollment journey. Pass 1 must candidly qualify
known standard-command limitations rather than present constants as correct CTAP values.
Docs: bio class/method XML and subcommand explanations; F-owned guide/example surfaces listed below.

### D-02 — biometric enumeration response and error conflation

**Deferred IMP; high.** FingerprintBioEnrollment.cs:179–233 reads flat key4 templateId/key7
friendlyName; EnrollmentEnumerationResult.Decode (BioEnrollmentModels.cs:386–395) likewise
wraps one flat TemplateInfo. Standard success is outer key7 **array**, nested key1 templateId/
key2 name (S4 §6.7.6). Standard array at key7 is fed to ReadTextString, not decoded as templates.
`InvalidCommand` is converted to empty list (190–194), whereas standard no-enrollment status
is `InvalidOption`; unsupported command is therefore concealed as empty inventory.
Desired: decode actual array and preserve unsupported errors, with scope-specific handling for
no templates. Existing fixture and “WhenNoTemplates” test intentionally encode the wrong
shape/status; they are not evidence the device implements that shape. Pass-1 doc qualification,
not assertion fixes. Exact impact: EnumerateEnrollmentsAsync, TemplateInfo.Decode,
EnrollmentEnumerationResult.Decode XML/internal comments and corresponding test explanatory units.

### D-03 — completion can be fabricated and continuation template ID can disappear

**Deferred IMP; high for false completion, consumer-sequence DOC.**
BioEnrollmentModels.cs:244–287 initializes remaining=0, status=Good, templateId=empty and
never checks required operation-specific fields. An empty map decodes to IsComplete=true.
This is not proof of enrollment success. Standard next replies need not repeat templateId
(S4); the helper returns an empty TemplateId for such a reply. XML tells callers to use the
begin ID (127), which is correct, but the fake workflow (FingerprintBioEnrollmentTests.cs:382–417)
feeds **result.TemplateId** each iteration and all fake replies repeat it.
Desired: operation-aware malformed-response checks and a deliberate result-ID contract later.
Docs now: retain the begin identifier independently; RemainingSamples is device feedback, not
an SDK-enforced sample budget, and IsComplete is only the equality projection of decoded count.
Do not invent an enrollment transaction/session policy. Sensor info also defaults missing kind
to Touch and maxSamples to zero (143–187); metadata defaults missing counts to zero
(CredentialManagementModels.cs:52–79). Include required-field/default-value validation in this
same deferred parser family, not as a forward-compatibility justification.

### D-04 — sample status labels and unproven vendor extensions

**DOC with deferred IMP; medium.** BioEnrollmentModels.cs:90–103 says StorageFull=0x0B,
NoUserActivity=0x0C, NoUserPresence=0x0D. S4 specifies FP_EXISTS=0x0B, unused=0x0C,
NO_USER_ACTIVITY=0x0D, NO_USER_PRESENCE_TRANSITION=0x0E. Enum names/values are executable/API
and remain untouched; fix/qualify standards claims, preserve future behavioral correction.
Unit tests merely assert same hard-coded values (202–237), not original-spec agreement.
`MaxTemplateCount` key4, `SampleCount` key8 and `GetTemplateInfo=8` are called vendor/YubiKey
extensions (BioEnrollmentModels.cs:133–136,168–169,306–312,350–351;
BioEnrollmentSubCommand.cs:62–68). No original vendor definition obtained. Key8 in the
standard response envelope is maxTemplateFriendlyName, not sample count. Public TemplateInfo
decoder expects a flat response rather than standardized nested TemplateInfo. Label the
actual modeled fields/limitations without asserting YubiKey-specific provenance. Do not delete
members or recast invented wire values as optional standard features.

### D-05 — configuration capability/current state/authorization are distinct

**DOC; medium, capability enforcement choice deferred IMP.**
AuthenticatorConfig.cs:24–36 and 86–101 overstate unconditional token requirements and
“every operation” verification. The helper always computes auth with a supplied protocol/token;
the standard has unprotected-device exceptions (S6), and alwaysUV concerns registration and
authentication, not GetInfo. Its three methods do not query authnrCfg, setMinPINLength, ep,
alwaysUv or firmware. Presence/false of ep still means supported-but-disabled; bioEnroll=false
means supported with no templates, unlike authnrCfg/credMgmt=false. Toggle is a state-changing
command, not setting desired state; disabling may be refused (OPERATION_DENIED), notably for
YubiKey 5 FIPS per V1. Public guides should instruct fresh GetInfo before deciding whether
to toggle and again after success. Do not guarantee retry safety: if a sent toggle's outcome
is unknown, repeating it can invert the intended state. No automatic verification or rollback
is performed. Desired enforcement of capability preconditions is a future policy decision,
not authorization to add checks now. Opening config precondition is a labeled Note in S6;
do not cite it as a normative MUST without this distinction.

### D-06 — config references and unused constants do not match cited revision

**DOC plus deferred IMP; medium.** ConfigSubCommand.cs:22 names §6.10; actual config is
§6.11 (S6). AuthenticatorConfig.cs:178–179,246–247 incorrectly cites §6.8 for its otherwise
correct config authentication prefix; actual §6.8 is credential management. ConfigSubCommand
VendorPrototype=4 is not standard vendorPrototype=0xFF. SetMinPinLengthRpIds=5 has no definition
in the cited revision; RP lists are fields of subcommand3, as the active builder correctly
uses. Neither extra constant is used by this helper. Correct citation/comment claims without
changing constants. Original vendor justification remains missing proof; do not retain “some
implementations” as unsupported authority. Config's supported first three codes and transcript
prefix are clean independently of these two unused constant claims.

### D-07 — minimum PIN policy, allow-list update and forced change need precise task guidance

**DOC; medium, API expressiveness deferred IMP/decision.** AuthenticatorConfig.cs:109–136
says “characters” and PIN “invalidated”; S6 measures **Unicode code points** and keeps the
existing PIN for changePIN while policy blocks token acquisition. Increasing minimum above
current PIN length also sets forcePINChange even if force=false. SDK range 4–63 is local
validation, not all-device acceptance/current-minimum or a complete UTF-8 PIN-byte guarantee.
RP IDs require both setMinPINLength capability and minPinLength extension, subject to device
list/storage limits. Nonempty list replaces mutable entries plus any immutable preconfigured
list; not an append-only permission grant. Empty and null lists are both omitted by this SDK
(214–234); force=false omitted (236–241). It cannot express “false field” or a policy-only
update without passing a numeric length; decide whether that matters in Pass2. No guarantee
empty list clears recipients. Coordinate minPinLength extension claims with E and PIN policy
with B. A config helper does not change the RP's account or independently enforce its policy.

### D-08 — update-user CBOR text-key order contradicts canonical encoding

**Deferred IMP; high, statically established encoding-order mismatch; exception not executed.**
CredentialManagement.cs:459–483 writes displayName, id, name and calls that alphabetical
order canonical. S8 requires id, name, displayName (text-key lengths 2,4,11). Both signed
parameter construction and request construction use this writer. Several owned credential
model fixtures also put displayName first; their writer is Ctap2Canonical, so green test
names cannot be assumed executed/pass proof. Context7 was attempted; official Microsoft L1
supports mode intent, with no local experiment. Desired: canonical encoder and meaningful
update wire coverage later. In Pass1 correct the explanatory rule, not emitted order or
test fixtures. Dependency to C: shared user-entity encoder/parser and sibling canonical
comments must be independently inspected, not concurrently edited by D.

### D-09 — administrative helper ownership and disposed-helper behavior

**DOC; medium; deferred IMP for disposed misuse.** All three helpers retain references to
session/protocol and **borrow** ReadOnlyMemory token (CM:40–67; bio:41–70; config:40–60).
Caller must keep session/protocol alive and token bytes unchanged for operations, then zero
caller-owned token in finally. No helper obtains/refreshes a token or validates its permissions,
RP binding or expiration locally. Bio/config have no Dispose. CredentialManagement.Dispose
only marks `_disposed` (223–231); **no method reads it**, so later methods can still operate
with a live session/protocol. Do not claim Dispose guards use, ends a device authorization,
zeros the token or owns the session. T12 ownership-violation historical parenthesis at229
is removable development residue; retain its useful caller-zeroing rationale. Decide actual
disposed enforcement later, separately from truthful documentation.

### D-10 — credential enumeration completion, state and failure boundaries

**DOC; medium; deferred IMP/decision for atomic sequence and malformed totals.**
CredentialManagement.cs:90–188 eagerly issues begin then firstTotal-1 next calls; nullable
absent total -> exactly one result, zero/negative total -> first result with unvalidated total;
no probing or streaming. ReportedTotal is copied from first response, not recomputed or
cross-checked. Begin NO_CREDENTIALS -> empty/null, next failure -> throw rather than partial
result. Credential failures dispose decoded entries; RP failures have no secret entry ownership.
S2 permits globally exclusive sequences, <=30-second assumptions and state invalidation on
token expiry/power-cycle. Helper has no operation-wide guard; A must inspect actual session/Core
admission before any claim of atomic serialization. Keep guidance against interleaving related
device operations; do not invent a new session invalidation/recovery policy. Next requests are
authenticated by this implementation even though S5 lists only subcommand; record actual bytes
without asserting additional fields necessarily violate interoperable behavior. No test checks
next request bytes, global interleaving or per-device state survival.

### D-11 — credential administration semantics and binary inputs

**DOC; medium.** Metadata remaining count is an **estimate**, not guaranteed storage capacity
(S5); XML at CredentialManagementModels.cs:35–38 lacks that context. RP entity ID can be
truncated; `RpIdHash` is SHA-256 of original RP ID and should be passed unchanged into
EnumerateCredentialsAsync, not recalculated from returned display text. Helper accepts any
rpIdHash length (CM:280–317); no local 32-byte validation, deferred consideration, not a
claim of acceptance by authenticator. Update-user must keep existing user.Id; only name/
displayName change and absent/empty names remove fields (S5), not create a user/rebind the
credential or update RP-side account. Delete sends descriptor and returns no result; there
is no undo or associated-large-blob cleanup. NO_CREDENTIALS is propagated for delete/update.
Coordinate E for explicit large-blob deletion/GC guidance without upgrading S5's Notes to MUSTs.

### D-12 — firmware thresholds are source metadata, not qualified support

**DOC; medium; unresolved exact minima.** Class XML CM5.2/bio5.2/config5.4 and A-owned feature
constants mirror broad F-owned README table. None of these helpers uses the constants to gate
operations. V1 proves credential-management introduction5.2.1, distinguishes early features
from complete CTAP2.1, and gives conflicting/coarse config/bio availability chronology. CM
sends **only standard0x0A** (233–250), no prototype0x41 fallback, so early preview management
cannot establish current helper interoperability. Bio requires actual sensor hardware, not a
5.2 firmware comparison. Config support is per subcommand/model/current capability, not all
firmware>=5.4. Keep source threshold values intact, describe them as SDK feature metadata,
and defer exact standard/preview firmware qualification. No certificates/hardware were checked.

### D-13 — deletion test can pass without proving deletion; support returns count as success

**DOC plus deferred test IMP; high evidence-quality defect.**
Ctap2CredentialManagementTests.cs:232–247 catches expected NO_CREDENTIALS but has **no assertion
or failure when GetAssertion succeeds**. Name/comment “RemovesFromDevice” / “credential was
deleted” overstate assertions. Capability-unsupported cases return normally (40–43,121–124,
159–162,257–260), not explicitly skipped. NoCredentials test also permits a thrown status,
although helper converts begin NO_CREDENTIALS to empty. Counts test only >=0, not a before/after
delta, capacity, or authentication. Extended tests do assert created IDs present and updated
displayName, but not updated Name/userID/public-key preservation or all unspecified fields.
Do not fix assertions in Pass1; make comments truthful and record missing proof for later.

### D-14 — config test claims and force-change status require re-baselining

**DOC plus deferred test IMP; medium/high.**
Ctap2AuthenticatorConfigTests.cs:182–260 describes token-block/change/recovery lifecycle; it
expects **PinInvalid** from GetPinTokenAsync (242–244), while S6/pin-token algorithm specifies
**PIN_POLICY_VIOLATION** for forcePINChange. Refer to B before diagnosing actual ClientPin behavior;
no test run confirms that assertion. Toggle test asserts changed GetInfo state but restore is
after assertion rather than finally and is not re-read; restore failure can leave toggled state.
Min-length test targets6; equality is allowed, so name “Increases” is not always established;
it asserts >=6, not extension output/allow-list values. Full-cycle safety net is best-effort
and catches errors, not guaranteed state restoration; a failed second PIN change can leave
temporary PIN. All owned integration tests call A-owned WithCtap2SessionAsync with default
normalizePin=true; PIN set/token verification/forced-change recovery occurs **before** callback
capability checks. Preserve authorization policy unchanged; explain that these are human-approved
PIN/state-changing tests, not silent read-only smoke because a callback later calls GetInfo.

### D-15 — biometric and configuration tests are not qualification

**DOC; medium; deferred test coverage.** Ctap2BioEnrollmentTests.cs:25–28 describes enrollment
tests requiring Bio; its sole test only reads sensor info (75–82) after PIN setup. It accepts
presence of bioEnroll/preview keys correctly independently of their boolean provisioned state,
but never enrolls/captures/cancels/renames/removes. Unit fake workflow has no sensor, modality
auth verification, absent-next-template case, real timeout, cancellation recovery or actual
template persistence. “AllOperations_UseCorrectPinUvAuthProtocol” only calls begin and checks
AuthenticateWasCalled (361–379). Rename/remove “SendsCorrectPayload” tests only Received(1).
Config fake protocol Version2 returns16-byte tags (44–72), an artificial fixture, not Protocol2
tag conformance. RP/force unit tests inspect map size, not actual RP strings/force Boolean.
Any passing result would be narrowly interpreted; no existing test result was consulted here.
Do not treat sensor metadata, FIPS-labeled flags elsewhere, or fake fixture agreement as biometric,
FIPS or cross-firmware qualification. Coordinate C for its FIPS test documentation only.

### D-16 — secret cleanup and failure guarantees need bounded statements

**DOC plus deferred IMP; medium.** CM clears **final request** in finally (233–251), but retains
intermediate encoded payload/authParam arrays without explicit zeroing (254–403). Bio/config
do not clear their request/authParam copies. Authentication tags are secret-derived output under
root classification; assess complete lifetime in Pass2, without claiming a plaintext token is
sent (it is not). Several owned integration paths omit make/assert token cleanup entirely or
clear tokens/PIN arrays only on success: credential tests49–104,164–248; extended68–117,
160–213; config restore86–101/reversedPIN247–255/tempPIN273–276. The outer config token finally
is correct, but does not cover all newly allocated material. A-owned SetOrVerifyPinAsync
discards a token (60), NormalizePinAsync discards one (125), and DeleteAllCredentialsForRpAsync
never zeroes its returned token (127–146); cross-owner dependency, not competing edit.
StoredCredentialInfo's disposal is independently correct for its one private clone, including
unknown fields; it does not clear transport/borrowed input or user/credential/public-key copies.
Copying RawData/LargeBlobKey creates a new caller cleanup responsibility; XML currently says
“copy it” without that caveat. Template IDs/status/names are metadata, not the actual fingerprint
template bytes; do not impose blanket secret-key treatment on all device output.

### D-17 — credProtect fixture comment mislabels policy 2

**DOC; low.** CredentialManagementModelsTests.cs:284 labels integer2 as
`userVerificationRequired`. Policy2 is UV optional with a credential-ID list; required UV
is policy3. E owns the authoritative CredProtectPolicy type. The test only asserts decoded
integer2, so correct the explanatory comment without changing its encoded value/assertion.

## State-sequence matrix contributions

| Row | Initiation/authorization | Continuation/completion | Failure/cancellation/outcome and SDK boundary |
| --- | --- | --- | --- |
| D-S1 metadata | Live session/protocol, borrowed cm token; no RP binding for global metadata (S5) | One standard0x0A response; counts copied/default0; remaining is estimate | CTAP/transport/decode errors propagate; not a capacity reservation; no helper checks capability |
| D-S2 RP enumeration | Authenticated begin2; global unbound cm | firstTotal-1 next3; order retained; absent total yields one; empty begin -> null total | No partial result; state can be invalidated by intervening ops/expiry/powercycle; SDK whole-sequence lock unproven |
| D-S3 credential enumeration | Hash is raw32-byte SHA256, not text; unbound or matching-RP cm | begin4 then next5; first total copied; owned disposable entries | Begin NO_CREDENTIALS -> empty/null; next error disposes local entries and throws; backend source untouched; no rollback/state-reset claim |
| D-S4 delete | descriptor uses binary ID/type; cm RP scope must match credential | Sends delete6, awaits success, no return map; no blob cleanup | Unknown -> NO_CREDENTIALS, propagated. Lost terminal response/cancellation does not prove nothing deleted; do not blindly retry/remediate |
| D-S5 update | existing user handle must match; updated names supplied | Sends update7; absent/empty names request removal; re-enumerate for device state | Actual canonical-name encoder defect D-08; no local handle equality validation; result does not update RP database |
| D-S6 bio begin/next | Choose standard/preview based on GetInfo; be PIN token, built-in UV acquisition additionally needs uvBioEnroll | Retain begin template ID; inspect status/count; continue until zero/unrecoverable error/cancel; no fixed SDK capture limit | D-01/D-03 prevent qualified standard journey; fake repeating TemplateId hides omitted-next-field case; callbacks are generic/silent |
| D-S7 enrollment cancel | Separate CTAP cancelCurrentEnrollment command, no auth specified by S4 | Cancels ongoing enrollment if any, device success response | .NET token cancellation/CTAPHID_CANCEL is not this command; helper never auto-invokes it; drained exchange/state reconciliation depend on A/Core |
| D-S8 template inventory/name/remove | be token for inventory/name/remove; no sensor placement required for those commands | Standard inventory is all-template array; IDs opaque; rename byte limits; remove deletes template | D-02 conflates unsupported with empty and misreads array; no proof no templates. No enrollment/cancel/removal hardware coverage |
| D-S9 enable EA | authnrCfg + ep capability, token when required; helper always auth | Idempotent enable; fresh GetInfo can show ep=true; later MakeCredential must request eligible EA | Does not ensure RP/platform acceptance, serial certificate or device qualification; failed call can have ambiguous device outcome |
| D-S10 toggle | Read fresh alwaysUv; false differs from absence; disabling can be prohibited | Toggles, not sets; fresh GetInfo after success; effects on makeCredUvNotRqd | Unknown outcome -> read/reconcile before another toggle. No SDK retry/rollback/state cache policy invented |
| D-S11 minPIN/force | authnrCfg,setMinPINLength,minPinLength extension if RP list; numeric4–63 local check | Nondecreasing code-point policy; new length may force change; changePIN clears flag; list replace semantics | Permanent policy changes not undone on Dispose; force=true without PIN may fail; expected status requires B review; current PIN remains change credential |
| D-S12 borrowed authorization | Caller owns token; helper borrows session/protocol/token; permissions/expiry device-enforced | Keep dependencies alive, token unchanged through awaited calls; clear token after last use | CM.Dispose doesn't prevent reuse or clear token; fresh token acquisition can invalidate earlier device tokens; lifetime not equal to object lifetime |

Error provenance: these helpers wrap no transport/APDU errors; underlying session/backend establishes
CTAP status and transport failures (A-owned). Known catches are CM begin NO_CREDENTIALS and bio
inventory InvalidCommand. Local config numeric and bio friendly-name validation is distinct from
device policy. Decode errors can occur after a successful state-changing device operation; .NET
cancellation is not rollback. Do not assert session must be discarded, auto-cancel, retry, or reset.

## Feature/claim matrix contributions

| Row | Standard/revision | Advertised capability versus current state | SDK enforcement/source | Evidence status |
| --- | --- | --- | --- | --- |
| D-F1 standard credMgmt | S5 §6.8, S7 preview distinct | credMgmt=true; preview option is not standard support | Only0x0A; no firmware/capability/token-scope checks | Static full source/fakes inspected; early V1 firmware5.2.1 != qualified0x0A support |
| D-F2 bio | S4 §6.7, S7 §6.12 | bioEnroll present means support; Boolean describes provisioned templates; actual sensor required | Explicit bool chooses0x09/0x40, no negotiation; wire/schema defects | Fake unit loop + sensor-only integration assertions; no biometrics/device qualification |
| D-F3 config | S6 §6.11 | authnrCfg true plus individual feature support; ep/alwaysUv false means disabled but capable | Always computes auth; no GetInfo gating; three implemented methods | Standard prefix inspected and unit-asserted; vendor minimum5.4 unproven/coarse conflicts |
| D-F4 minPIN | S6 §6.11.4; extension E-owned | setMinPINLength, extension list support, current min/force state distinct |4–63 only; omits null/empty RP list and false-force | Unit map-size evidence not RP/result proof; integration expected force status disputed |
| D-F5 EA | S6 §6.11.1, vendor custom configuration | ep presence=capable, value=enabled; RP/platform requirements | Enable only; no disabled method; source factory-reset remark | V1 supports FIDO-reset disable; no cert/EA registration/CMVP validation |
| D-F6 alwaysUV | S6 §6.11.2 and V1 | current state and ability to disable differ; FIPS model may forbid disable | Toggle only; not every protocol operation; no verification read | Integration asserts changed state only if executed; restore not guaranteed |
| D-F7 raw/secret fields | Source ownership, S5 largeBlobKey | metadata and secret-bearing envelopes differ | One disposable credential clone; raw/key views clear on dispose; independent fields persist | Strong static/owned-view fake assertions, not end-to-end transport cleanup |

## Complete owned-path disposition and clean semantic units

Every path below was read **in full**, including all methods, XML, helpers, assertions, fixtures
and comments. Prefixes are exact: `S=src/Ctap2/src/`,
`U=src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/`,
`I=src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/`.
“Clean” means the specified semantic unit agrees with inspected implementation/evidence, not
the whole file or hardware conformance. Copyright/license boilerplate is read/no editorial
change, not counted as a meaningful clean unit. No generated-source exclusion inside these files.

| # | Full path (expand exact prefix) / complete lines | Disposition and meaningful clean-unit IDs |
| --- | --- | --- |
|1|S BioEnrollment/BioEnrollmentModels.cs:1–404|D-02/03/04/16. **D-C01** modality1 and kind Touch1/Swipe2 XML/constants; **D-C02** sample-status0–0x0A meanings; **D-C03** independent RawData clone for sensor/sample preserves unknown bytes; not a completion-validation claim |
|2|S BioEnrollment/BioEnrollmentSubCommand.cs:1–69|D-01/04. §6.7 citation correct, but code-value claims not clean; no whole-file clean declaration |
|3|S BioEnrollment/FingerprintBioEnrollment.cs:1–571|D-01/02/03/09/12/15/16. **D-C04** EnrollCaptureNext param says begin TemplateId; **D-C05** template ID/name parameter-map keys1/2 in rename encoding, independently of wrong subcommand/auth prefix; **D-C06** all async sends forward provided cancellation token; not auto-enrollment cancellation |
|4|S Config/AuthenticatorConfig.cs:1–282|D-05/06/07/09/12/16. **D-C07** config auth34-byte prefix ff×32/0d/subcommand, parameters appended for minPIN; **D-C08** local4–63 range XML matches guards; **D-C09** numeric parameter1/RP-array2/forceBoolean3 and omission conditions; **D-C10** monotonic minimum/device enforcement explanation agrees S6 |
|5|S Config/ConfigSubCommand.cs:1–71|D-05/06/07. **D-C11** implemented subcommands1/2/3 match S6, no claim for unused4/5 |
|6|S CredentialManagement/CredManagementSubCommand.cs:1–59|**D-C12** all seven credential-management identifiers/values and original exact-revision URL are clean |
|7|S CredentialManagement/CredentialEnumerationResult.cs:1–58|**D-C13** result owns read-only wrapper/order and first-reply total; Dispose idempotently disposes entries; property access itself not revoked |
|8|S CredentialManagement/CredentialManagement.cs:1–486|D-08/09/10/11/12/16. **D-C14** begin-only NO_CREDENTIALS normalization and credential failure entry cleanup; **D-C15** descriptor id/type serialization canonical for those keys; **D-C16** CM subcommand||parameter-map signing structure (excluding misordered user map) |
|9|S CredentialManagement/CredentialManagementModels.cs:1–190|D-03/10/11. **D-C17** metadata raw response independent clone and decoded supplied counts; **D-C18** RP required outer entity/hash checks, nullable total and unknown preservation; not RP-ID-required/hash-length validation |
|10|S CredentialManagement/RelyingPartyEnumerationResult.cs:1–37|**D-C19** read-only order-preserving collection and unrecomputed nullable first total; no Dispose requirement for this metadata-only result |
|11|S CredentialManagement/StoredCredentialInfo.cs:1–205|D-11/16; **D-C20** single private response clone, RawData/LargeBlobKey disposal guards and zeroing including unknowns; **D-C21** decoder borrows input, zeroes own clone on failure; **D-C22** nested PublicKey is encoded COSE CBOR copy, not a private key; optional fields nullable; user/descriptor/key survive disposal |
|12|I Ctap2AuthenticatorConfigTests.cs:1–285|D-14/16. **D-C23** toggle assertion compares initial/fresh GetInfo bool; **D-C24** explicit PermanentDeviceState marker and target6 skip-if-above reflect nondecreasing test policy; not guaranteed cleanup/increase |
|13|I Ctap2BioEnrollmentTests.cs:1–89|D-12/15. **D-C25** presence-based bio detection distinguishes unsupported from supported-not-provisioned, token finally clears; assertions only sensor-kind and positive sample hint |
|14|I Ctap2CredentialManagementExtendedTests.cs:1–220|D-08/13/14/16. **D-C26** update assertion requires returned credential and updated display name; **D-C27** two-created-ID enumeration asserts both IDs and >=2 count; not full roundtrip/qualified execution |
|15|I Ctap2CredentialManagementTests.cs:1–303|D-13/16. **D-C28** resident enumeration checks created ID present; **D-C29** metadata assertions are nonnegative bounds only; rest limitations preserved, especially deletion success path |
|16|U BioEnrollment/BioEnrollmentModelsTests.cs:1–364|D-02/04/15. **D-C30** unknown-field raw-copy independence assertions for sensor/sample; **D-C31** completion fixture asserts zero equality, poor-quality fixture asserts decoded status/count; fake samples not sensor behavior |
|17|U BioEnrollment/FingerprintBioEnrollmentTests.cs:1–512|D-01/02/03/15. **D-C32** queue-driven loop is explicitly simulated and reaches decoded0; null/empty-name tests only validation, read but no new tests/removal; “correct payload/all operations” names overstate assertions |
|18|U Config/AuthenticatorConfigTests.cs:1–383|D-07/15. **D-C33** enterprise/toggle captured signing prefix asserts all32 ff and0d/subcommand; **D-C34** command/protocol/tag-presence and numeric min8 assertions are fake wire evidence, tag16 is explicitly test-protocol output |
|19|U CredentialManagement/CredentialManagementModelsTests.cs:1–537|D-04/08/16. **D-C35** disposition unit470–504 asserts captured-view zeroing, guards, surviving independent fields, borrowed-source preservation; decode failure test proves unchanged source, not inaccessible clone zeroing; comment284 labels policy2 as userVerificationRequired incorrectly (DOC correction only; E dependency) |
|20|U CredentialManagement/CredentialManagementWireTests.cs:1–355|D-10/15. **D-C36** ordered results/first total/read-only wrapper/disposed views assertions; **D-C37** continuation cancellation/malformed decode assertions propagate and leave supplied responses unchanged, not observed local-owner cleanup; **D-C38** canonical metadata/delete request field assertions and begin absence/NoCredentials fallback; no next-byte/update transcript checks |

All validation-only assertions, fake protocol implementations, helper encoders and fixture factories
were read; they are not silently excluded from coverage and will not be changed to manufacture a green
suite. D-17 is a comment correction, not a test-assertion fix. Source-observed integer2 remains unchanged.

## Exact documentation-impact and re-review map

Paths below are authoritative scope requests, **not** edit authorization outside D ownership.
`S/U/I` expand as above; `F` owns module/examples Markdown and sample sources, A shared session/helpers,
B PIN, C credential codecs/FIPS tests, E extension/storage. G owns boundary report.

| Candidate | D-owned exact XML/comment/test unit to reopen | Cross-owner exact surface and re-review trigger |
| --- | --- | --- |
|D-01|S BioEnrollment/BioEnrollmentSubCommand.cs all code claims; S BioEnrollment/FingerprintBioEnrollment.cs class, sensor/begin/next/cancel/rename/remove XML and every builder auth/timeout comment; U BioEnrollment/FingerprintBioEnrollmentTests.cs sensor/timeout/cancel/rename/remove/fake-auth explanations|F: src/Ctap2/README.md Requirements/helper sentence; src/Ctap2/CLAUDE.md Bio tree, Feature Flags, generic notification caveat; src/Ctap2/examples/FidoTool/FidoExamples/BioEnrollment.cs and Cli/Menus/FingerprintsMenu.cs full enrollment tasks. Reopen after actual wire/prototype fix and independent bytes/hardware evidence |
|D-02/03/04|S BioEnrollment/BioEnrollmentModels.cs sample-status/sensor/template/enumeration/sample result XML and decoder comments; S BioEnrollment/FingerprintBioEnrollment.cs inventory and loop docs; U BioEnrollment/BioEnrollmentModelsTests.cs all affected fixture comments; U BioEnrollment/FingerprintBioEnrollmentTests.cs inventory/no-template/workflow comments|Same F bio paths; A: src/Ctap2/src/AuthenticatorInfo.cs bioEnroll/current-state XML; G boundary-map biometric capability/lifetime rows. Trigger response schema, operation validation, begin-ID/result-ID contract or original vendor proof |
|D-05/06|S Config/AuthenticatorConfig.cs class, EA/toggle XML, BuildCommandPayload/BuildSetMinPinLengthPayload spec comments; S Config/ConfigSubCommand.cs whole explanatory unit|F: src/Ctap2/README.md helper/config/Requirements; src/Ctap2/CLAUDE.md Config tree/Feature Flags; src/Ctap2/examples/FidoTool/FidoExamples/ConfigManagement.cs and Cli/Menus/ConfigMenu.cs. A: AuthenticatorInfo.cs ep/alwaysUv/authnrCfg claims; C: Ctap2EnterpriseAttestationTests.cs/Ctap2FipsComplianceTests.cs claims; G capability/UV boundary. Trigger capability gating, retry/state policy, actual subcommand support or certificate evidence |
|D-07|S Config/AuthenticatorConfig.cs SetMinPinLengthAsync XML and omission comments; S Config/ConfigSubCommand.cs minPIN remarks; U Config/AuthenticatorConfigTests.cs RP/force comments; I Ctap2AuthenticatorConfigTests.cs min/force explanations|F config example/menu above; E: src/Ctap2/src/Extensions/MinPinLength/MinPinLengthInput.cs and MinPinLengthOutput.cs, src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2MinPinLengthTests.cs; B: src/Ctap2/src/Pin/ClientPin.cs set/change/token policy XML. Trigger parameter-expressiveness/allow-list/PIN policy fixes |
|D-08|S CredentialManagement/CredentialManagement.cs WritePublicKeyCredentialUserEntity canonical-order comment and update XML qualification; U CredentialManagement/CredentialManagementModelsTests.cs affected user-map fixture explanations|C: src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs and src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs user-map/canonical comments; F: examples/FidoTool/FidoExamples/CredentialManagement.cs update task and README credential docs. Trigger canonical encoder/fixture correction with real wire evidence; no assertion edit now |
|D-09/10|S CredentialManagement/CredentialManagement.cs class/constructor/Dispose/metadata/enumeration XML/comments; S CredentialManagement/CredentialEnumerationResult.cs, RelyingPartyEnumerationResult.cs total/lifetime XML; S BioEnrollment/FingerprintBioEnrollment.cs and S Config/AuthenticatorConfig.cs constructor ownership; U CredentialManagement/CredentialManagementWireTests.cs continuation explanations|A: src/Ctap2/src/ICtap2Session.cs and Ctap2Session.cs raw-send/sequencing/lifetime; F: README.md enumeration/helper/security notes; CLAUDE.md Credential Management/Protocol Disposal; F credential/bio/config examples and menus. B: src/Ctap2/src/Pin/ClientPin.cs and IPinUvAuthProtocol.cs borrowed/owned token docs. Trigger multi-command guard, disposed-helper enforcement, token lifecycle, malformed-total validation |
|D-11|S CredentialManagement/CredentialManagementModels.cs count/hash/total XML; S CredentialManagement/StoredCredentialInfo.cs binary/public-key views; S CredentialManagement/CredentialManagement.cs enumerate hash/delete/update XML|E: src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs and ILargeBlobStorage.cs delete/GC guidance; F: examples/FidoTool/FidoExamples/CredentialManagement.cs, Cli/Menus/CredentialsMenu.cs, README.md credential tasks. Trigger blob cleanup, truncated RP routing, handle/name replacement or input validation change |
|D-12|Three S administrative class firmware XML|A: src/Ctap2/src/Ctap2Session.cs FeatureBioEnrollment/FeatureCredentialManagement/FeatureAuthenticatorConfig XML (53–70); F: README.md Requirements table; CLAUDE.md feature flags, Config/Bio tree, tests/CLAUDE.md capability claims; src/Ctap2/PACKAGE_README.md support prose. Trigger original exact vendor/preview evidence and recorded device/firmware/transport matrix; no runtime thresholds changed |
|D-13/14/15|I all four owned integration class summaries/affected test comments; U BioEnrollment/FingerprintBioEnrollmentTests.cs simulated workflow/all-operations/payload claims; U Config/AuthenticatorConfigTests.cs fake protocol/params explanations; U CredentialManagement/CredentialManagementWireTests.cs assertion-boundary comments|A: I Ctap2TestHelpers.cs and TestExtensions/Ctap2TestStateExtensions.cs setup/cleanup/permission assumptions; B: ClientPin.cs force-status handling; C: Ctap2FipsComplianceTests.cs metadata versus qualification docs; F: src/Ctap2/tests/CLAUDE.md and example README test/lane claims. Trigger assertion/setup changes or actual executed ceremony evidence; preserve no-run status until authorized |
|D-16|S StoredCredentialInfo.cs RawData/LargeBlobKey copy/Dispose XML; S CM/bio/config internal zeroing/lifetime explanations; I all token/PIN cleanup comments|A-owned shared helper paths above; B protocol/ClientPin cleanup claims; F README.md Security notes and all three admin examples. Trigger end-to-end ownership/secret-array cleanup and exception-safe teardown fixes; never infer transport cleanup from private-clone tests |
|D-17|U CredentialManagement/CredentialManagementModelsTests.cs:284 inline policy2 label|E: src/Ctap2/src/Extensions/CredProtect/CredProtectPolicy.cs authoritative modeled policies. Trigger enum/policy semantics change; comment-only correction keeps integer/assertion2 |

## Handoff priorities and verification limits

1. Coordinator triage first: D-01/02/03/08 are consequential standard-wire/result defects;
   do not silently hide them by weakening “CTAP2.1” citations or normalizing wrong fixtures.
2. Fresh documentation editor can improve ownership, sequence, estimated counts, exact actor/state,
   code-point and update/delete guidance immediately; underlying implementations remain deferred.
3. A/B/C/E/F/G receive the cross-owner dependencies above. A verifies global/transaction-level
   admission separately; B resolves actual force-policy and token lifecycle. D changes no shared file.
4. Pass2 meaningful tests must inspect original-spec wire bytes, bio array/no-template status,
   absent-next-template and missing-count cases, update canonical names, deletion success-as-failure,
   forced-PIN policy status and exception cleanup. That is a proposed verification scope, not newly
   authorized test/assertion changes or device remediation.
5. Hardware needs explicit authorized known-state device/model/firmware/transport matrix and human
   presence/PIN/bio coordination. No credentialdelete/enrollment/PIN/config/reset operation was
   performed; no build/test/compiler/hardware/commit result exists from this investigation.

### Final record-only verification

Read-only Python checked report/evidence existence, candidate headings D-01–D-17, all20 coverage
rows, 38 distinct clean-unit IDs, 12 state rows and7 feature rows, and all20 owned file hashes
against inventory: every owned source/test file unchanged. `git diff --name-only` listed only
`docs/research/documentation-audit/README.md`, an outside-scope change not made by D and left
untouched; this report/evidence are new files, not tracked modifications. Narrow dependency read
of E-owned CredProtectPolicy.cs confirmed modeled values2 optional-with-ID-list,3 required for
D-17. These checks verify report structure and preservation, not independent factual review or
execution of SDK behavior. Coordinator must still adjudicate candidates and review evidence.
