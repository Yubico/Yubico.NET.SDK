# Investigator A — sessions, capabilities, transport and shared test explanations

> **Later absence-semantics correction (2026-11-04):** COORD-001 found that parsed absent
> EncIdentifier/EncCredStoreState byte arrays implicitly become present empty nullable memory.
> Null-preservation statements apply to optional numeric/Boolean/text fields, not every nullable
> property. See correction-a.md, review-a-followup.md and CTAP-IMP-001; original investigation follows.

Date/access date: **2026-11-03**. Investigator: GPT-6.1 Sol, assigned subagent; no further delegation.
Baseline: `5b09a9b6942ded3913b5c9340c3e126617e61a82`. All **30 A-owned C# paths** were read in full,
including helper implementations, fake responses and assertions. A read-only SHA-256 comparison found
all 30 equal to `inventory.json`; HEAD equals the baseline. This establishes inspection coverage, not
build, test, device or standards-conformance success. None of those validations was run.

Root/module/test CLAUDE, module README, `docs/TESTING.md`, run plan, campaign workflow and approved
style were read. The requested root `tests/CLAUDE.md` does not exist; the applicable file is
`src/Ctap2/tests/CLAUDE.md`. Available skill descriptions were reviewed; `domain-security-guidelines`
was loaded for sensitive-data distinctions. Its PRD checklist is not protocol authority. No build,
test, commit, source edit or agent creation occurred; those skills/operations were not applicable.
Only this report is written. Candidates below need coordinator triage and independent review.

## Important results

* Reset has **two different clocks**, and firmware/product restrictions matter. The five-second
  universal touch claim is wrong. CTAP2.1/2.3 prescribe command receipt within ten seconds for a
  displayless authenticator; Yubico documents five seconds on older firmware and ten on later firmware,
  then a separate USB touch wait. Reset also invalidates non-discoverable/U2F credentials.
* GetInfo is fresh **per call**, but session firmware/configuration is a construction-time snapshot.
  Option values can represent configured state rather than capability; they are not authorization.
* Several nominal GetInfo/session smoke tests **first set, verify or change a PIN** because the shared
  helper defaults to normalization. Existing read-only/agent-safe explanations are not true for those
  call paths. This is an operationally consequential deferred test/setup concern, not permission to run them.
* A prompt notification is not proof of touch, UV, RP verification or application success. Backend
  `Completed` precedes model decode; a callback or decoder may fail after the device has mutated state.
* `0x38` is a **CTAP2.0 legacy** PIN-token-expired status, not a CTAP2.2 addition. Later inspected
  publications reserve it. This is a citation/provenance correction, not an instruction to remove the API.
* GetInfo decoder accepts missing required fields; several tests call versions-only data “minimal”.
  Request-builder tests mostly assert prefix/length, not the encoding their names/comments imply.

## Primary citation cache (focused excerpts, not automatically upgraded references)

All URLs below were fetched from originals on **2026-11-03**. Quoted text is whitespace-normalized;
ellipsis means an omitted portion, not a reconstructed normative sentence. SDK source is separate evidence.

### S21 — CTAP2.1

**Client to Authenticator Protocol (CTAP), CTAP2.1 Proposed Standard, June 21, 2022 (errata)**:
<https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html>.
Header explicitly says “Proposed Standard, June 21, 2022”; preserve this revision when correcting its claims.

* **§5 Terminology**, actor authenticator/platform: “Platform-mediated user interactions such as
  clientPin may provide user verification but are not considered to assert user presence.” NFC placement
  can establish presence for authenticators without another in-boundary gesture mechanism; the passage
  specifies the NFC userPresent flag and 120-second maximum, not a universal physical-touch ceremony.
* **§6 Authenticator API**, `#authenticator-api`, actor authenticator: “The authenticator MAY maintain
  state based on the assumption that each stateful command is exclusively preceded by either another
  instance of the same command, or by the corresponding state initializing command, and no more than
  30 seconds will elapse between such commands.” “Here, ‘exclusively preceded’ means that no other
  authenticator operation occurs in between.” State MUST be discarded if the initializing token expires.
* **§6.3**, `#authenticatorGetNextAssertion`: client calls when numberOfCredentials exceeds one;
  authenticator returns NOT_ALLOWED without remembered parameters, after exhausting the count, or
  after a timer greater than 30 seconds. Timer steps are OPTIONAL over NFC. NumberOfCredentials is
  omitted on next responses. These are device rules, not SDK sequence tracking.
* **§6.4**, `#authenticatorGetInfo`: “The values of various authenticatorGetInfo response structure
  members and option IDs may change over time depending upon the commands the platform sends to the
  authenticator.” Versions and AAGUID are **Required**; AAGUID is “16 bytes in length”. Transports:
  “The list MUST NOT include duplicate values nor be empty if present. Platforms MUST tolerate unknown
  values.” PIN protocol list is in decreasing authenticator preference; algorithms in preferred order.
* **§6.4 option table**: clientPin true = capable and PIN set; false = capable and PIN not set; absent =
  not capable. uv true = built-in UV capable/configured; false = capable/not configured; absent = no
  built-in UV. bioEnroll false still means enrollment supported, with no enrollments provisioned.
  credMgmt/authnrCfg support identifiers are **`credMgmt` / `authnrCfg`**, not `credentialMgmt`.
* **§6.4 fields**: minPINLength is current enforced length in **Unicode code points**; remainingDiscoverableCredentials
  is an **estimate**, with worst-case-field-size assumptions, not exact free slots; firmwareVersion is
  an unsigned monotonically increased model version, not a standardized major/minor/patch layout.
* **§6.6**, `#authenticatorReset`: “Invalidates all generated credentials, including those created over
  CTAP1/U2F.” “Erases all discoverable credentials.” Resets serialized large-blob storage and named
  configuration features. “In case of authenticators with no display, request MUST have come to the
  authenticator within 10 seconds of powering up of the authenticator.” Late receipt yields NOT_ALLOWED;
  disabled transport or explicit presence denial yields OPERATION_DENIED; user-action timeout is distinct.
  Transport support MAY vary; at least one transport and supported USB HID are **MUST** in this revision.
* **§6.9**, `#authenticatorSelection`: “This command allows the platform to let a user select a certain
  authenticator by asking for user presence.” No inputs. Platform SHOULD NOT repeat explicit denial;
  MAY repeat timeout; SHOULD cancel other authenticators after selection. SDK does not orchestrate that
  multi-authenticator policy.
* **§8 Message Encoding**, `#ctap2-canonical-cbor-encoding-form`: “Indefinite-length items MUST be made
  into definite-length items.” Map keys sort by major type, encoded length, then byte-wise order;
  “Clients, platforms, and servers MUST NOT use more than 4 levels of CBOR nesting.” They MUST NOT send
  messages larger than 1024 bytes unless maxMsgSize indicates support. Unknown map keys MUST be ignored.
  Using a canonical writer is not proof of depth, command schema or device-size compliance.
* **§8.1 Command Codes**: vendor range 0x40–0xBF; Bio Enrollment Prototype 0x40 and Credential Management
  Prototype 0x41. **§8.2**, `#error-responses`: 0x38 **Reserved for Future Use**; 0xE0/0xEF extension-specific
  range boundaries, not “CTAP1/U2F error”. CTAPHID transport error is a separate frame/provenance.
* **§11.2.9.1.5**, `#usb-hid-cancel`: “If there is an outstanding request that can be cancelled, the
  authenticator MUST cancel it and that cancelled request will reply with the error
  CTAP2_ERR_KEEPALIVE_CANCEL.” Authenticator MUST NOT reply to CANCEL itself; cancellation error is
  response to the original CBOR request, not a HID error. **§11.2.9.1.7** STATUS_UPNEEDED = 2 means
  “The authenticator is waiting for user presence.” **§11.3.3** client SHALL SELECT before other ISO7816
  commands. None of these clauses promises host-side instantaneous cancellation or rollback.

### S23 — CTAP2.3

**Client to Authenticator Protocol (CTAP), CTAP2.3 Proposed Standard, February 26, 2026**:
<https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html>.
Header explicitly identifies publication status/date. This is evidence for explicitly 2.3-labelled fields,
not standing permission to replace all 2.1 citations.

* **§6.4**, `#authenticatorGetInfo`: encIdentifier 0x19 is IV || ciphertext of a **128-bit device
  identifier**, derived with persistentPinUvAuthToken and HKDF info `encIdentifier`; IV must be regenerated
  for each GetInfo output. encCredStoreState 0x1E similarly encrypts a 128-bit credential-store state.
  These are standardized fields in this revision, even where YubiKey-specific availability is discussed.
* **§6.4** longTouchForReset true means supported/enabled and “A touch of >= 5 sec is required for
  reset”; false = supported/disabled; absent = unsupported. transportsForReset lists reset-supported
  transports; MUST not duplicate or be empty when present; platforms MUST tolerate unknown values.
* **§6.4** pinComplexityPolicy true = supported/enabled, false = supported/disabled, absent = unsupported.
  pinComplexityPolicyURL is optional **Byte String**, a URL platform can use to inform the user. maxPINLength
  is in Unicode code points; an authenticator specifying it still MUST restrict PIN representation to
  63 or fewer bytes; absence with ClientPIN support gives the effective 63-code-point default. The
  same paragraph also says “less than 63 octets”; that wording tension is **not adjudicated here**.
* **§6.6**, `#authenticatorReset`: ten-second command-receipt rule for no-display authenticators remains;
  long touch is conditional on present/enabled feature. Reset regenerates device/store-state identifiers
  and resets persistent PUAT and long-touch state as well as previously named effects. Transport support
  requirement is **SHOULD**, not 2.1's MUST; vendor MUST provide an alternate factory-reset method if the
  command is unsupported. Keep actor/version distinctions.
* **§8.2**, `#error-responses`: 0x38 remains **Reserved for Future Use**; PIN_POLICY_VIOLATION says
  “Minimum PIN length or PIN complexity may trigger this error.” Do not repeat the older minimum-only
  description as universal current device policy.

### S22RD / S20 — status provenance

**CTAP2.2 Review Draft, October 03, 2024**, §8.2:
<https://fidoalliance.org/specs/fido-v2.2-rd-20241003/fido-client-to-authenticator-protocol-v2.2-rd-20241003.html#error-responses>.
Fetched initial and one focused follow-up: **“0x38 Reserved for Future Use”**. Header says Review Draft.

**CTAP2.0 Proposed Standard, January 30, 2019**, §6.3:
<https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html#error-responses>.
Exact table: **“0x38 CTAP2_ERR_PIN_TOKEN_EXPIRED pinToken expired on authenticator.”** Also 0x36 is
PIN_REQUIRED in 2.0 rather than later PUAT_REQUIRED. Retained legacy support is legitimate; citing it
as a newly added CTAP2.2 status is not.

### YR — vendor reset scope

Yubico **FIDO2 Reset**, living legacy .NET SDK product guide (not the v2 API or a normative CTAP publication):
<https://docs.yubico.com/yesdk/users-manual/application-fido2/fido2-reset.html>.
“A reset will remove any credentials present and set the application to the ‘no PIN’ state.”
Step 2: **“10 seconds for YubiKeys with firmware version 5.5.4 and later or 5 seconds for firmware
versions prior to 5.5.2”** to send ResetCommand. Step 3: USB user must touch within **30 seconds**;
NFC must remain in contact with reader. Bio Multi-protocol individual FIDO reset applies only if FIDO
is not blocked; otherwise device-wide reset is needed. Do not import this page's legacy command API.
**Gap:** this vendor passage does not classify 5.5.2/5.5.3; no guessed boundary or tested reset claim.

### Framework documentation and bounded retrieval gap

Context7 resolved .NET to `/websites/learn_microsoft_en-us_dotnet_api` and `/dotnet/docs`.
Initial exact queries for Guid span/byte ordering and Cbor canonical mode/BytesRemaining had no matching
API docs. One focused follow-up on `/dotnet/docs` returned GUID *sorting* material (not byte serialization)
and no CBOR match. Those results **do not substantiate** architecture-independent Guid byte ordering,
CborReader trailing-item enforcement or internal writer-buffer zeroing. No compiler experiments or
extra unbounded framework searches were run. A-014 records the precise Guid portability gap. GetInfo's
explicit lack of post-map checks is source evidence; no unsupported framework exception claim is made.

## Rebaseline inputs and narrow dependencies

Read current WebAuthn `triage.md` (webauthn-001), especially WA-IMP-002/003/009/010/013 and WA-XREF-001;
read `audit-v2-closeout-review.md` original findings #9/YESDK-1617 and #10/YESDK-1618. These are historical
evidence/decisions, not new green gates. Current `Ctap2Session.cs:220–227` plus the fake assertions in
`Ctap2SessionTests.cs:187–214` show the formerly accepted makeCredential up=false case is now refused
before sending/prompting. It was not a presence bypass. GetAssertion false remains supported in the
source and fake notification tests. Current Core `FidoHidProtocol.cs:302–310` retains CTAP success racing
CANCEL; the old success-discard claim must not be repeated as current. No hardware confirmation here.
WA-XREF-001 and descriptor omission remain **C/G-owned**; A does not reopen their full data-decoder audit.

Narrow Core reads: `ApplicationSession.cs` complete (construction, configuration and ownership units),
`Feature.cs` complete; `FirmwareVersion.cs:22–81` for default/sentinel semantics;
`FidoHidProtocol.cs` complete for the single exchange/cancellation/disposal seam;
`UserPresenceNotification.cs` complete; `ExchangeGuard.cs` complete;
`PcscProtocol.cs:100–189` for transmit/SELECT/status/configuration units. No discovery, platform,
SCP crypto or general Core inventory audit. `ClientPin.cs:683–697` was read only to confirm that
disposing ClientPin disposes its protocol; the rest of PIN investigation belongs to B.

## Candidate findings and documentation dependencies

### A-001 — reset clock, applicability and destructive effects (DOC; consequential)

**Code/docs:** `ICtap2Session.cs:73–90` says touch within five seconds of insertion and “all credentials
and settings”; `Ctap2Session.cs:188–204` simply sends command 0x07, with no timer, transport/reset
capability preflight, reset state refresh or rollback. `AuthenticatorInfo.cs:199–226` exposes long-touch
and reset transports without linking their significance to Reset. README:200–201 repeats five seconds.
S21/S23 §6.6 and YR separate power-up→command receipt from user interaction; five seconds is not universal.

**Proposed claim:** reset invalidates generated credentials (including non-discoverable/U2F), erases
discoverable credentials and clears the PIN; named settings/storage effects depend on applicable
protocol/product behavior. Send promptly after power-up; older YubiKeys have the documented shorter
window. Meet the device's separate presence/long-touch requirements. The authenticator enforces its
window/applicability and reports CTAP errors; SDK does not arrange reinsert/touch or enforce a local timer.
Avoid an unconditional “every setting deleted” guarantee. A failed call is not rollback evidence.

**Disposition:** accept documentation correction; no timer/device-wide reset implementation authorized.
YR's 5.5.2/5.5.3 gap and Bio blocked-state mechanism need F/product corroboration, not invented v2 API.
Dependencies: A interface/info XML; **F** README constraints, reset menu/example/full procedure and test
CLAUDE; **B/D/E** reset invalidation of token/config/bio/storage state; **G** cross-layer reset claims.
Re-review trigger: reset timing/capability/state-refresh or channel-reset behavior changes.

### A-002 — physical touch, notification and ceremony are not interchangeable (DOC)

`ICtap2Session.cs:125–129` universally requires touch for assertions, but session:315–324 passes no
notification for `UserPresence=false`; fake test session-tests:411–433 asserts no notification.
makeCredential always rejects false and creates PolicyRequires; selection/reset create HID-only
contexts. Generic request and next assertion use None. NFC presence can derive from insertion (S21 §5);
S23 broadens ISO7816 presence discussion. HID backend waits for keep-alive; SmartCard requests before
APDU and cannot observe in-flight waiting. `Completed` in both backends is based on successful CTAP status
**before** `MakeCredentialResponse.Decode` / `GetAssertionResponse.Decode` in session:277/337.

**Proposed claim:** presence is requested by ordinary makeCredential and default getAssertion; a silent
getAssertion request can set false. The authenticator supplies/validates evidence according to transport
and policy. Notification only signals SDK policy/device wait and resolution of that exchange; it does
not verify UP/UV response flags, RP signature/challenge/origin or successful application authentication.
No notification does not mean no device interaction. Generic helper commands remain silent as implemented.
Dependencies: A interface/backend XML; **C** option and result semantics; **F** README:70,165–185 and
test CLAUDE; **D** bio helper notification gap; **G** WebAuthn ceremony/UP/UV matrix. Do not count D's
generic-helper notification concern twice. No new notification semantics are proposed in Pass 1.

### A-003 — broad implementation/support claim and firmware flags (DOC + needs-decision)

`Ctap2Session.cs:38–43`, `ICtap2Session.cs:26–27` claim implementation of CTAP2.1/2.3 while citing only
S21. Session feature constants:47–80 are firmware thresholds, not complete feature negotiation.
Only USB SmartCard gate is directly invoked here (436–452); FeatureFido2 is not checked in initialization.
There is no requirement that parsed Versions contain FIDO_2_1/2_3 or that each advertised optional
feature is implemented. Feature.IsSupportedByFirmware:36–40 treats Major==0 as supported.

**Proposed claim:** package exposes CTAP2 operations and selected features; actual availability depends
on GetInfo advertisement/current state, product/firmware and transport, and successful authorization.
Nominal thresholds are not certification, complete protocol implementation or per-key support guarantees.
SCP `IsAuthenticated` comes from Core secure-channel establishment, not PIN/UV authentication or
permission to use a CTAP credential. **Needs-decision:** initialization accepts a versions-only/U2F-only
decoded response and unknown/default firmware; do not invent rejection policy in documentation.
Dependencies: **F** package/README/module CLAUDE firmware/support tables (including stale `FeatureCtap2`
name versus actual `FeatureFido2`); **B/D/E** exact feature detection; **G** WebAuthn claims. Owner-specific
minimum-firmware product checks stay with those investigators; A does not independently certify them.

### A-004 — GetInfo capability, configured state, defaults and snapshot semantics (DOC)

`AuthenticatorInfo.cs:74–202,229–282` is too compressed for options and optional fields. `Options`
examples include incorrect `credentialMgmt`; bioEnroll false is not unsupported. `RemainingDiscoverableCredentials`
says slots rather than estimate. MinPinLength omits code-point units; PIN/UV and algorithms omit preference
order; MaxMsgSize omission has an effective protocol default (1024) but the DTO deliberately remains null.
Lists/maps default empty, losing absent-versus-empty at typed surface; RawData retains the encoding.
LongTouchForReset/PinComplexityPolicy null/false/true have different meanings (S23 §6.4).

`GetInfoAsync` makes a new exchange each call (session:163–167,405–413); it does not cache an InfoData
property. Initialization does a separate GetInfo and stores only session firmware/configuration in Core.
One-shot `GetCtap2InfoAsync`:62–64 therefore does **two GetInfo calls**, creation probe then requested read.
`FirmwareVersionOverride` configures base protocol/session firmware but does **not** override the detected
firmware used by the USB SmartCard gate (session:132–145). Later GetInfo does not update session firmware.
Missing firmware becomes 0.0.0, and therefore passes optimistic Feature support, not proof of real 5.8 firmware.

**Proposed claim:** reported fields describe a device response at query time, not SDK enforcement or
permission. Explain absent/false/true per option, protocol defaults versus raw nullable properties and
current-state refresh after configuration/PIN changes. Firmware packed-byte conversion:590–597 is
**YubiKey-specific**, not defined by S21's generic unsigned firmwareVersion. No Management fallback here.
Dependencies: A info/interface/factory XML; **F** info sample/capability tables/initialization guide;
**B/D/E** helpers' cached GetInfo/token-feature decisions; **G** WebAuthn cached-info boundary.
Re-review if session starts caching/updating info or firmware discovery/gating policy changes.

### A-005 — encrypted identifier names and field provenance (DOC; routed E dependency)

`AuthenticatorInfo.cs:205–217` calls EncIdentifier a unique identifier for discoverable credentials.
S23 §6.4 instead specifies **device identifier**. Its encrypted data is not a credential ID and cannot
select a credential. EncCredStoreState:255–268 is store-state metadata. Both descriptions need protocol
revision, IV/ciphertext representation, optionality and the persistent token relationship. Decoder just
reads byte strings; it does not validate lengths or persistent-token availability. Tests info:302–325,
445–469 assert arbitrary 16-byte strings deserialize, **not decryptability or valid IV||ciphertext**.

**Disposition:** correct A semantic label; route firmware 5.7/5.8 and “when PPUAT active” product/extraction
checks and decryption details to **E/B**; do not independently duplicate their crypto audit. Persistently
encrypted field availability is not evidence ordinary permission-scoped PIN tokens can decrypt it.
Dependencies: A info XML/test fixtures commentary; **E** decryptor/encrypted-metadata tests;
**F** InfoMenu/capability guide; **G** boundary if exposed. No end-to-end decryption claim here.

### A-006 — GetInfo required-field and response-envelope validation gaps (IMP + DOC qualification)

`AuthenticatorInfo.Decode`:289–294 clones borrowed input, constructs canonical reader and returns Parse.
Parse:474–514 returns defaults when Versions/AAGUID are missing and has no explicit required-presence,
nonempty-version, AAGUID-length or post-map complete-input check. Algorithm Parse:666–697 defaults
missing type/algorithm; Type:609–611 says “always public-key” despite public init and parser accepting others.
S21/S23 §6.4 require Versions and 16-byte AAGUID; generic ReadInt32 also does not impose unsigned/range
constraints on all those advertised integer fields. Unknown top-level keys are correctly skipped.

**Concrete source case:** CBOR `a0` yields empty versions/AAGUID; versions-only fixture is accepted.
No test was executed to claim a repro, and the exact framework behavior on appended bytes is a bounded
unverified subcase: source does not request a post-map check. Tests info:39–59 call incomplete data
“Minimal GetInfo response”; fake session/cross-layer helpers likewise omit AAGUID. These fixtures are
decoder probes, not minimal **conforming** GetInfo envelopes.

**Disposition:** deferred validation/decoder contract work; documentary limitation must not redefine
missing required fields as intended standard validity. Correct fixture descriptions without changing
assertions. Preserve raw unknown fields and strict PIN-key-agreement boundary (B); do not promise all
malformed data is rejected merely because canonical mode is chosen.
Dependencies: A Decode/RawData/Algorithms/parameter Type XML and fixtures; **C** shared algorithm parameter
serialization/validation (same physical file owned A); **F/G** full-conformance/raw-forward-compatibility
claims. Re-review after required-field, integer-range or envelope-consumption validation changes.

### A-007 — raw exchange and general encoding responsibilities (DOC + bounded IMP decision)

`ICtap2Session.SendCborRequestAsync`:32–50 describes prefix/output but not schema, buffer lifetime,
sequencing, notification/error or cleanup responsibilities. Session:395–403 forwards caller memory
unchanged with None; backends only reject missing command byte and validate outer response status.
Result strips status and can be empty for successful commands; it is not necessarily a CBOR map.
No capability/PIN-UV/schema interpretation or GetInfo max-size check happens at this API boundary.
Core HID only checks framing MaxPayloadSize (FidoHidProtocol:219), a different limit from device maxMsgSize.

**Proposed claim:** caller constructs applicable command-prefixed canonical CBOR, chooses correct
capabilities/authorization/sequence and ensures allowed size/depth, keeps request memory valid and
unchanged through completion, and clears caller-owned secrets. API adds session transport/framing/status
handling but does not infer touch or bypass device authorization. Returned read-only view is no disposal/
zeroing guarantee; clearing `.ToArray()` clears a copy, not backend storage. Raw result lifetime does not
silently acquire a disposable owner. Typed operations similarly rely on this response contract.

**IMP/decision:** session discarded initialization MaxMsgSize and does not preflight typed request size;
canonical builder does not explicitly enforce CTAP depth. S21 §8 constrains client messages. Route
typed-operation size/depth assessment to **C** and shared builder policy to coordinator; decide whether
raw API intentionally leaves those responsibilities to callers before requiring new runtime validation.
Dependencies: A interface/backend/builder; **F** raw-access/user-interaction guide/module escape-hatch text;
**B/D/E** secret-bearing generic requests; **G** low-level versus WebAuthn responsibilities.

### A-008 — builder copies, delayed callbacks and assertion overclaim (DOC + IMP)

`CtapRequestBuilder.cs:76–94,160–165` immediately clones byte/encoded inputs into retained closures;
no Dispose/clear exists. Replaced parameters are abandoned; repeated Build can reuse the retained clones.
WithValue/WithMap/WithArray:120–152 store callbacks and invoke them at **Build**, not insertion.
WithMap/Array do not create wrappers; callback must write the complete map/array value, not just content.
Repeated keys replace prior values. Build:192–205 clears only the `Encode()` intermediate copy, not
retained inputs, returned command-prefixed array, callbacks or any unverified CborWriter internal storage.
BuildAsMemory returns read-only access to another newly built array; not borrowed input or disposable storage.

**Proposed claim:** copying versus deferred callback contract and output ownership/lifetime explicit;
for sensitive input prefer direct WithValue encoding to avoid this retained input clone, without promising
whole writer memory cleanup. Returned output remains caller-owned to clear; retain callback inputs until
Build and do not imply ReadOnlyMemory means secrets are cleared automatically.

**IMP:** missing cleanup lifetime for retained secret-bearing builder clones; defer design, no new disposal
API here. Test request-builder:77–91 proves key/value decode, not absence of intermediate allocation.
Test:94–107 comments promise sorted keys but assertions only check length/prefix; other With* tests
often have the same limitation. **F** CLAUDE direct-encoding advice is good but insufficient for public XML.
Dependencies: A builder/tests; **B/D/E** uses for secret auth data (owners inspect their own uses);
**C** canonical request composition; **F** encoding/memory claims. Re-review after builder lifetime or
meaningful exact-wire/allocation assertion changes.

### A-009 — error provenance and version-sensitive status descriptions (DOC)

`CtapException.Status`:22–25 says status “from the authenticator”, but both backends synthesize
`CtapStatus.Other` for empty response (HID:70–74, SmartCard:100–104); public constructors also take any
caller status. Nonzero wire CTAP status becomes CtapException without normalized rollback semantics.
APDU failures occur before payload parsing; selection catches **any ApduException** and wraps
NotSupportedException as missing FIDO AID, preserving inner exception (SmartCard:46–58). That translation
does not prove all such SELECT failures mean absent AID. Transport errors/CTAPHID_ERROR are not
CtapException (Core Fido:392–409). SDK argument/disposal/overlap faults, callbacks and decode errors differ.

`CtapStatus.cs:22–30` attributes additions to S22RD; 0x38:220–223 is legacy S20, reserved S21/S22RD/S23.
Retain the member but accurately explain provenance. Minimum-only PinPolicyViolation:215–218 is exact
S21 table text, **not** a universal later-device rule (S22RD/S23 broaden it). E0–EF descriptions:270–278
are extension-specific, not U2F errors. Known status numeric tests assert constants, not every cited revision.

**Disposition:** DOC correction. **Potential IMP/decision**: review overbroad SELECT translation and whether
empty responses deserve another error type; source observed, not automatic approval to redesign exceptions.
Dependencies: A errors/backend/interface; **B** retry/PIN-policy interpretations; **F** error troubleshooting;
**G** WA-IMP-002 normalization boundaries. Re-review after translation/error mapping changes.

### A-010 — cancellation, concurrency and success/failure boundaries (DOC; historical rebaseline)

Core ExchangeGuard:49–62 refuses pre-cancellation/overlap and supplies None inside admitted exchange.
HID polls caller cancellation only on keep-alive (Fido:334–379), sends at most one CANCEL and drains
terminal frames; success racing cancellation is preserved (302–310). No keep-alive means no cancellation
signal at that loop. Prompt failure is rethrown **after** draining valid terminal data and can supersede
device success. SmartCard exchange completes despite cancellation after admission (Pcsc:124–138).
Neither Ctap2Session nor Reset establishes a host deadline. Callback terminal token is None.

Two-stage distinction matters: guard covers **wire exchange**, not arbitrary operation admission or
multi-command sequence lease. Session teardown can race decoding; healthy idle borrowed connection can
be reused, but active built-in macOS FIDO disposal may terminally wake/poison it. Cross-layer tests:70–91
and94–149 explicitly assert fake pending-disposal failure versus drained cancellation recovery.

**Proposed claim:** await each call; no overlapping exchanges or automatic queued retry. Preserve
stateful-command adjacency even across fresh info calls. Cancellation is cooperative, may not return
promptly, cannot undo admitted work, and can race a successful return. Callback/decode failure is not
proof of device failure. Do not make universal “session remains usable after every error” promises.
Dependencies: A all public cancellation params/interface; **F** README:165–170, lifetime guide;
**G** WebAuthn deadlines/mapping; **C/D/E** stateful sequence guidance. No new timeout/recovery policy.

### A-011 — hidden PIN mutation in nominal read-only integration tests (IMP + DOC; consequential)

`Ctap2TestStateExtensions.cs:165–202,222–259` defaults normalizePin=true. Normalize:74–95 calls GetInfo
and may SetPin; :102–120 can change known→reversed temporary→known PIN; :123–138 requests PIN token,
consumes a retry on mismatch and skips on failure. GetInfo tests and most simple tests call the helper
without false, e.g. GetInfoTests:39–50/57–66; SessionSimple:66–83. SmartCard info helpers likewise normalize.
Thus README/test CLAUDE “read-only smoke” categorization and SessionSimple:27–31's no-PIN framing exceed
actual preconditions. Skip is **after possible mutation/retry**, not a no-operation assurance.

**Proposed claim now:** helper's default performs PIN setup/verification/recovery before callback;
these paths require known device state and explicit approved PIN work even when final assertion is GetInfo.
“No-op if PIN matches” is false: a token round trip still occurs. “Before each session” is imprecise:
normalization is after initialized session creation and optional per call. Mention forcePinChange recovery.
**IMP:** separate read-only setup from normalization and enforce correct hardware lanes later; do not
change default/traits/assertions in this pass. Coordinator should block unattended integration smoke
claims until the selected paths are reviewed. Do not run to confirm this source-observed concern.
Dependencies: A shared helper/GetInfo/simple/SmartCard comments; **F** test/module guides and runnable
commands; **B/D/E/C** integration tests using normalization. Re-review after helper/default/lane changes.

### A-012 — helper cleanup and recovery are not transaction guarantees (IMP + DOC)

`Ctap2TestHelpers.SetOrVerifyPinAsync`:47–63 creates ClientPin/protocol but has no failure disposal,
and discards a returned PIN token without zeroing at :60. GetCredManToken:92–107 selects by version strings
(excluding FIDO_2_3) rather than actual token option; legacy branch cannot promise scoped permissions.
DeleteAllCredentialsForRp:127–146 does not clear returned token; it **does** dispose result/ClientPin.
ClientPin.Dispose owns protocol disposal (narrow B dependency:689–695), so successful caller disposal
does not retroactively clear a discarded returned token.

Normalize:106–111 clears temp PIN only after both changes succeed, not finally. If first change succeeds
and second fails, device can retain temporary PIN and heap temporary remains uncleared; comments promise
known PIN without explaining this partial recovery. Token at :125 is discarded uncleared. Ctap2TransportTests
stores make/assert PIN tokens without cleanup. Constants are intentionally known test secrets, not
production-safe PIN collection examples.

**Disposition:** deferred cleanup/setup fault-path concern; documentary truth now, no executable fixes.
**B** owns token-return/permissions protocol contract; route overlaps there, do not duplicate ClientPin
algorithm audit. **D** owns enumeration/deletion semantics; helper's broad RP cleanup is destructive
and not proof of deleting only this test's created credential. **F** owns command strings: TransportTests:135
and StateExtensions:118/137 embed reset advice; token-preservation restriction means these executable
strings remain deferred, including the misleading ten-second universal advice and invalid legacy paths.
Re-review helper/test docs after cleanup, recovery and token negotiation changes.

### A-013 — test explanations exceed assertions and retain development history (DOC + test IMP)

* `Ctap2GetInfoTests.cs:136–153` and SessionSimple:99–110: guarded nonempty list makes
  `hasHmacSecret || Count>0` tautological. SessionSimple:129–140 similarly does not establish rk.
  GetInfo firmware/max checks are conditional; absent fields can pass. AAGUID all-zero rejection is an
  additional test assumption, not the cited required 16-byte rule. Comments should describe the
  conditional assertions, not general authenticator validity or feature execution.
* `Ctap2TransportTests.cs:32–36` says creates **and verifies** a credential end-to-end; body:85–100
  only checks creation fields. NonDiscoverable:209–212 checks nonempty signature, not cryptographic/RP
  verification. No actual execution recorded. **C/G** own server/RP responsibility explanation.
* SessionSimple reset:199–202 asserts **some CtapException**, not NOT_ALLOWED, window or absence of
  deletion. Selection:187 checks only completion. It is not a safe reset-window regression proof.
* RequestBuilder test mismatches are A-008 (same cause, not extra independent count).
* Session-tests:52–54, extensions-transport-tests:25–27/147–150 contain upstream/Phase38/38.5 history.
  Keep useful current rationale (borrowed connection and narrow remap) without phase narration.
  `IntegrationTestBase.cs:22–27` is empty: shared-utilities intent is not implemented functionality.
* `Ctap2TestData.cs:71–73` random 32-byte value is stand-in **clientDataHash**, not an actual challenge
  plus WebAuthn client-data hash calculation. PIN complexity claims:48–51 and StateExtensions:49–50
  must be product/model qualified; S23 supports configured policy, not universal firmware5.7 policy.
  CLI PIN in immutable command text is a dedicated-test convenience, not safe production guidance.

**Disposition:** accurate DOC/test comment changes now; strengthen tests later without rewriting assertions
in this cycle. Dependencies: A test files; **F** package/test/example journey explanations; **B** PIN policy;
**C/G** algorithm/credential/RP verification. Re-review after meaningful asserted properties or fixtures change.

### A-014 — AAGUID platform claim has an unresolved framework evidence gap (needs-decision)

`AaguidConverter.cs:18–22,38–55,67–83` says .NET's mixed-endian byte layout applies on little-endian
systems, then conditions component reversal on BitConverter.IsLittleEndian. On this Linux architecture
the source path is coherent with the intended familiar conversion, but no runtime test or architecture
qualification was run and no A-owned converter test exists. Context7 initial/follow-up did not return
serialization docs; GUID sorting material is not evidence. **Bounded unknown:** is default Guid span
serialization platform-independent mixed-endian such that conditional reversal is incorrect on a
big-endian runtime? Reviewer should obtain original API/runtime implementation evidence before accepting
an IMP finding or universal portability claim. Do not make a runtime fix or say converter verified.
Dependencies: A converter XML/comments; **C/G** AAGUID usage; **F** platform claims.

### A-015 — factory example opens incompatible simultaneous sessions (DOC)

`IYubiKeyExtensions.cs:108–122` opens ordinary session then secureSession on the same yubiKey in one
scope. First remains live; second conflicts with one-connection-per-key rule, demonstrated by assigned
transport fake tests and session-disposal tests. Example must present **alternatives or non-overlapping
scopes**. SCP03 default keys require an explicitly authorized provisioned device, not a universal secure
default recipe. `using scpKeys` lifetime already correctly encloses secure-session lifetime.
Direct CreateAsync XML omits borrowed connection ownership and PreferredConnectionType being a type
assertion, not transport selector (Core ApplicationSession:280–293). Factory failure cleans only owned
session resources; hidden connection cleanup is convenience entrypoint responsibility. Do not promise
automatic fallback after connection failure (selection source:128–154,162–184).
Dependencies: A factory XML; **F** all direct-versus-convenience/scp examples; **G** analogous WebAuthn
alternatives. Re-review after factory ownership/connection-admission/options semantics change.

### A-016 — optional parser values are not absent map members (DOC)

`CtapResponseParser.cs:128–186` says “returning null if not present” but it peeks/reads the **current
value**; null/undefined are consumed as null. It cannot detect an absent key, end-of-container or
missing bytes. Map/array helper handler must consume exactly one complete value, including SkipValue
for unknown keys. ToNullableMemory wraps the same array, not a clone/owner; null versus empty preserved.
**Disposition:** precise internal XML correction, not new accepted null values in protocol models.
Tests parser:193–262 exercise selected values/null, not absent fields or every undefined branch.
Dependencies: A parser; **C/D/E/B** consuming models decide required/optional strictness.

## Proposed claim/sequence matrix contributions

| Row | Standard / advertised | Configured / authorized | Actual SDK enforcement and sequence | Inspected evidence / limit |
| --- | --- | --- | --- | --- |
| A-M01 CTAP versions | S21/S23 define version strings; optional feature advertisement separate | Version does not grant PIN/UV permissions | No version negotiation or minimum FIDO2 check here; flags are firmware comparisons | Source session initialization; fake versions-only GetInfo; no conformance gate |
| A-M02 transport | GetInfo transports are advertised names, unknown tolerated | OS exposed connection + selected FIDO AID matter | HID first, otherwise SmartCard; SCP defaults SmartCard; no retry/fallback after opening failure | Extensions transport probe tests inspect requested connection, not device operations |
| A-M03 USB SmartCard gate | Product gate 5.8 nominal; NFC skips gate | Current connection Transport, not NFC-capability metadata | SELECT→GetInfo→detected gate→configured firmware override→SCP→backend rebind→initialized | Unit gate/sentinel probes; no real firmware/AID/SCP qualification |
| A-M04 info snapshots | S21 §6.4 values can change | clientPin/uv/bioEnroll have configured-state values | Fresh GetInfo every call; construction firmware remains fixed; one-shot performs creation probe plus fresh read | Source direct call count; assigned fakes show exchanges; no mutation refresh execution |
| A-M05 SCP authentication | Not CTAP PIN/UV authorization | `IsAuthenticated` reflects secure protocol establishment | HID SCP rejected before channel I/O; SmartCard SELECT/GetInfo precede SCP | Cross-layer fake asserts zero I/O on HID rejection; no secure-device operation |
| A-M06 makeCredential UP | S21 requires up true if present | Authorization independent; NFC/ISO presence gesture differs | false rejected before request/prompt; PolicyRequires context; HID wait signal/SmartCard pre-APDU | Fake rejection + timeout notifications; no actual credential ceremony |
| A-M07 assertion UP | up=false valid silent request | Device policy/UV result still must be interpreted | false suppresses prompt; ordinary calls notify; next assertions silent | Fake no-credentials status and no prompt; C owns wire/results/selection |
| A-M08 selection | S21 §6.9 asks presence; client multi-key SHOULD behavior | No PIN token specified | One command only; HID may notify; SmartCard no predicted prompt; no multi-key cancellation coordinator | Fake waits + unexecuted UserPresenceProbe intended hardware tests |
| A-M09 reset | S21/S23 §6.6 ten-second request rule; YR older shorter window; reset transport policy differs by revision | Long-touch/transport/block state can constrain availability | No local clock/power-cycle/capability orchestration; sends 0x07; no state refresh/rollback | Generic exception test does not establish window or successful reset |
| A-M10 next assertions | S21 §6/6.3 count, adjacency and >30-sec rule; NFC timer exception | Initial authorized operation; token expiration invalidates device sequence | Caller sends count−1 next calls, no interposed GetInfo/other operation; SDK does not own sequence lease/counter | Source command-only next request; C owns full response test audit |
| A-M11 cancellation | S21 §11.2.9.1.5 CANCEL signal/terminal response | Token cancellation is not undo/authorization revocation | Pre-cancel refuses; admitted HID polls keep-alive, drains; raced success retained; SmartCard completes; no host deadline | Fake Mac bridge asserts CANCEL/drain/recovery; no OS/hardware generalization |
| A-M12 notification completion | Keep-alive 2 signals wait, not touch completion | No verified RP success implied | backend CTAP success→resolve→return raw view→typed decode; callback/decode can fail after device work | Fake callbacks assert propagation, outcome and untransferred-buffer zeroing |
| A-M13 ownership | Protocol does not define managed cleanup API | Caller request/token lifetime remains caller responsibility | Direct session borrows; convenience owns connection; backends borrow protocols; decoded GetInfo clones input | Disposal fakes inspect counts/failure sharing; raw response success not zeroed |
| A-M14 raw/CBOR | S21 canonical order/depth/maxMsgSize/schema requirements | Caller builds authorization and applicable sequence | Prefix required; no inferred UP/schema/device-max enforcement; status stripped; error provenance retained | Builder tests mostly prefix/length, not full encoding; no universal validation proof |
| A-M15 optional fields | Protocol defaults/required fields differ | Absence ≠ disabled for every option | Lists/maps empty when absent; scalar null preserved; raw envelope cloned; missing required GetInfo accepted | Unknown/raw clone and nonboolean-option unit assertions; malformed required cases deferred |
| A-M16 test lane | GetInfo itself is no-PIN/no-touch query | Helper may set/change PIN/request token | normalizePin default true before action, skips only after attempted PIN work | Source complete helper trace; **not** read-only executed validation |

## Every owned-file disposition and named clean semantic units

Statuses: **read/findings** = full file read with candidates; **read/support** = full supporting evidence
read, no independent new doc defect in named clean units. Clean IDs are bounded semantic units, not
whole-file conformance verdicts. No clean unit below is claimed executed; no duplicate findings counted.
All paths below are exact paths relative to repository root.

| Owned path | Status / candidates | Named clean unit (sampling ID and range) |
| --- | --- | --- |
| `src/Ctap2/src/AuthenticatorInfo.cs` | read/findings A-004/005/006 | A-C01 RawData clone/unknown-field preservation explanation, 65–72,289–294,468–478 (does not imply required-field validity) |
| `src/Ctap2/src/Backend/HidBackend.cs` | read/findings A-002/009/010 | A-C02 command-prefixed CTAPHID_CBOR and status-stripped success view, 47–68,76–94 |
| `src/Ctap2/src/Backend/ICtap2Backend.cs` | read/support A-007 | A-C03 backend boundary accepts same non-null notification and returns validated-status payload, 19–39 |
| `src/Ctap2/src/Backend/SmartCardBackend.cs` | read/findings A-002/009 | A-C04 APDU wrapper fields/data/Le explanation, 85–95 (not real-device interoperability) |
| `src/Ctap2/src/Cbor/AaguidConverter.cs` | read/findings A-014 | A-C05 explicit 16-byte input requirement and fresh returned array, 26–36,60–65,85 |
| `src/Ctap2/src/Cbor/CtapRequestBuilder.cs` | read/findings A-007/008 | A-C06 no-parameter command-only build and command-prefix output, 167–177,192–205 |
| `src/Ctap2/src/Cbor/CtapResponseParser.cs` | read/findings A-016 | A-C07 handler consumes one value; nullable memory wraps borrowed array, 32–49,57–82 |
| `src/Ctap2/src/Ctap/CtapCommand.cs` | read/support A-003 | A-C08 assigned command values/standard-versus-prototype labels, 27–90 (S21 commands/§8.1; existence ≠ feature implemented) |
| `src/Ctap2/src/Ctap/CtapException.cs` | read/findings A-009 | A-C09 ThrowIfError preserves nonzero byte as status, 60–79; constructors preserve supplied status/inner, 28–58 |
| `src/Ctap2/src/Ctap/CtapStatus.cs` | read/findings A-009 | A-C10 SUCCESS/NO_CREDENTIALS/USER_ACTION_TIMEOUT values and descriptive meanings, 34–37,169–177 (revision-sensitive exceptions separately flagged) |
| `src/Ctap2/src/Ctap2Session.cs` | read/findings A-001/002/003/004/007/010 | A-C11 direct makeCredential false rejection rationale, 220–227; A-C12 owned typed-request arrays zeroed in finally, 247–275,307–335 (not response cleanup) |
| `src/Ctap2/src/ICtap2Session.cs` | read/findings A-001/002/003/004/007/010 | A-C13 fresh GetInfo contract, 52–61; A-C14 next-assertion immediate adjacency instruction, 149–161 (add exact count/timer conditions) |
| `src/Ctap2/src/IYubiKeyExtensions.cs` | read/findings A-004/015 | A-C15 narrow resolve-only exception remap and no failure fallback rationale, 157–191; A-C16 hidden connection ownership comment, 148–151 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2GetInfoTests.cs` | read/findings A-011/013 | A-C17 PIN protocol test's exact nonempty/1-or-2 assertions, 99–115 (after normalization; not protocol execution) |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2SessionSimpleTests.cs` | read/findings A-011/013 | A-C18 second explicitly HID session refusal assertion, 156–166; A-C19 direct borrowed SmartCard lifetime, 40–62 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2SmartCardTests.cs` | read/findings A-011 | A-C20 explicit SmartCard preference in all three helpers and limited path applicability remarks, 23–28,56,92,127 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestData.cs` | read/findings A-013 | A-C21 random 16-byte user ID and RP/user generator contracts, 65–68,75–91 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TestHelpers.cs` | read/findings A-012 | A-C22 auth-param helpers delegate to protocol.Authenticate on clientDataHash, 154–175; A-C23 disposes enumeration result and ignores only NoCredentials, 132–151 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2TransportTests.cs` | read/findings A-012/013 | A-C24 non-discoverable request explicitly sets ResidentKey=false, then uses returned ID in allow list, 165–207 (nonempty signature ≠ verification) |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/IntegrationTestBase.cs` | read/findings A-013 | A-C25 discovery attributed to WithYubiKey shared infrastructure, 17–24; class itself is empty |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs` | read/findings A-011/012/013 | A-C26 clientPin absent/false/true comment accurately distinguishes capability/state, 76–79; A-C27 explicit SmartCard preference rationale, 158–163,215–220 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/UserPresenceProbeTests.cs` | read/support A-002/010 | A-C28 planned HID request/resolution/reuse assertions and UP/Slow traits, 26–92 (not a proof human touch occurred or cancellation always bounded) |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuthenticatorInfoTests.cs` | read/findings A-005/006/013 | A-C29 exact unknown/case/order/duplicate transport preservation test/comment, 9–36; A-C30 cloned RawData survives input mutation, 118–151 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Cbor/CtapResponseParserTests.cs` | read/support A-016 | A-C31 map/array parse and selected current-value/null assertions, 26–81,104–141,158–188,193–262; no absent-member guarantee |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2BackendLifecycleTests.cs` | read/support A-002/007/010 | A-C32 not IDisposable type assertions, borrowed-protocol calls and zeroing of retained untransferred response views, 12–84,123–166 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2SessionHidCrossLayerTests.cs` | read/support A-010 | A-C33 fake HID SCP rejection before I/O and idle borrow reuse, 17–67; A-C34 fake cancellation blocks overlap and drains before recovery, 94–149; fixture validity gap A-006 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2SessionTests.cs` | read/findings A-013 | A-C35 false-UP no-send/no-prompt summary/assertions, 187–214; A-C36 callback primary fault/response transfer tests, 330–388; A-C37 idempotent async disposal/failure sharing, 90–161 |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CtapExceptionTests.cs` | read/support A-009 | A-C38 inner-exception identity and ThrowIfError assertions, 30–67; numeric table test is constants, not cross-revision conformance |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/CtapRequestBuilderTests.cs` | read/findings A-008/013 | A-C39 command-only Build/BuildAsMemory exact prefix length, 10–19,109–118; WithValue decoded bytes,77–91 (no allocation proof) |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/IYubiKeyExtensionsTransportTests.cs` | read/findings A-013 | A-C40 selection-probe assertions, default/override/SCP routes and held-HID no SmartCard retry, 32–145,164–173 (probe aborts before initialization) |

## Bounded unknowns and required follow-through

1. A-014 framework portability; no substitute sorting citation or unexecuted experiment result.
2. YR does not classify 5.5.2/5.5.3; reset Bio blocked-state applicability needs product-specific v2
   consumer guidance. Keep five-versus-ten source scope explicit; no hardware/firmware claim generalized.
3. Decoder appended-data behavior requires precise framework API evidence or future authorized offline
   regression; source shows missing explicit envelope check, not an executed appended-data test.
4. Feature firmware thresholds/PPUAT availability are **B/D/E product evidence dependencies**. No
   universal 5.7 complexity, hmac-secret-mc firmware or encrypted-field guarantee accepted by A alone.
5. Reset over SCP may invalidate channel/session/token state; A inspected send/no-refresh boundary,
   not device reset/SCP interoperability. Do not add automatic reopen/rollback/session-usability policy.
6. Success response ownership/secret-derived result cleanup remains partly WA-IMP-013 / E/C territory.
   Backend fault cleanup is narrower than end-to-end cleanup, despite successful fake retained-view assertions.
7. Every test here is **inspected/not-run**. Smoke classification is particularly unsafe until A-011
   is triaged; future authorized hardware evidence must distinguish setup, mutation, assertion and skipped paths.

Protocol research for the main S21/S23 session/info/reset topics used an initial retrieval and one focused
follow-up; S22RD status and framework topics were likewise bounded. Separate exact legacy-status and
CBOR/CTAPHID topics used focused original passages, not repeated open-ended searches. Further uncertainty
is routed above rather than researched indefinitely. Coordinator should reconcile candidate overlaps,
assign durable CTAP-DOC/IMP/XREF IDs, and give fresh editors/reviewers these source/spec dependencies.
