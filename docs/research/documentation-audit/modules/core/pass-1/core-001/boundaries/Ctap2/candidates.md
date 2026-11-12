# CTAP2/Core local concerns and reopening decisions

All identifiers below are **packet-local candidates**, `CORE-CTAP-X001` onward. They allocate no
accepted Core IDs and supply no additive defect count. An inherited issue retains its source-run key.
Documentation is proposed or retained pending independent review; implementation, policy and qualification
each have their own disposition. Every listed product path remains unchanged in this investigation.

Paths beginning `S/` mean `src/Ctap2/src/`; `T/` means `src/Ctap2/tests/`; `E/` means
`src/Ctap2/examples/FidoTool/`. Exact assigned ranges and hashes are in `unit-dispositions.json`.

## X001 — Bound connection exclusion to observed membership

**Documentation:** qualify `src/Ctap2/README.md:306–310` and `src/Ctap2/CLAUDE.md:47–50` with
SDK-managed opens and the member interfaces Core actually grouped. The README's “all of its interfaces”
can imply coordination between independent disjoint discovery scopes. Core's process-local registry
coordinates overlapping known member IDs. A's accepted-evidence limitations also retain possible
partial-evidence cross-key grouping. An applet choosing HID first supplies no stronger physical proof.
**Implementation:** reuse A/B1 mechanisms; no new registry defect allocated. **Policy:** retain existing
grouping/selection policy. **Qualification:** external processes, direct factories and disjoint scopes
need their own coordination. Reopen these two guides, `S/IYubiKeyExtensions.cs`, discovery/physical-model
and raw-tier guidance after changes to member identity, grouping, direct-open admission or no-fallback.

## X002 — Keep complete workflows alive through their actual final continuation

**Documentation:** retain the existing command-adjacency qualifications in `S/ICtap2Session.cs`,
`S/CredentialManagement/CredentialManagement.cs` and `S/LargeBlobs/LargeBlobStorage.cs`. Add the
public-workflow disposal scope alongside general session lifetime advice if X001 is edited.
`ApplicationSession.cs:43–44,470–568` and `ExchangeGuard.cs:49–62` protect an admitted protocol exchange,
then detach/release; they supply no whole-workflow lease. CTAP decode/logging and terminal notification
can continue after that exchange exits. Credential enumeration, ClientPin GetInfo/key-agreement/token
requests and blob read-modify-write each span several admissions. **Implementation:** source-visible
boundary, shared with B5-C006; a workflow lease/reentrancy design needs separate authorization.
**Policy:** caller serializes and fully awaits the workflow, then disposes; callbacks should return
promptly and leave disposal to the enclosing owner. **Qualification:** the held Linux/Mac tests exercise
one exchange, not every post-exchange continuation or interleaving schedule. Reopen the named helpers,
session XML, both module guides, interaction/raw-tier guidance and cross-layer tests if workflow admission,
post-exchange ownership or disposal behavior changes. Do not prescribe an invented SDK lease helper.

## X003 — Preserve CTAP-XREF-003 and the PIN input allocation decision

Aliases: `ctap2-001:CTAP-XREF-003`, `ctap2-001:CTAP-IMP-006`,
`openpgp-001:PGP-CORE-001`; shared C001–C004 concerns remain C-owned.
**Documentation:** `S/Credentials/Ctap2CredentialOptions.cs:24–40` already states UTF-16 counts and
the UTF-8/code-point/NFC/final-NUL distinction. Retain it. Core options XML needs its C-owned units
explained; this boundary contributes the actual CTAP call site. `FidoPinHelper` uses Core's reader,
whereas WebAuthn's separate prompt contract carries explicit units. **Implementation:** ClientPin
`ValidatePin:571–584` checks only 4–63 bytes; Core first counts `char` values and then encodes.
**Policy:** decide where strict UTF-8, normalization checks, code-point/device minima and final-NUL
validation belong. Avoid silently normalizing an existing credential. **Qualification:** ASCII fixtures
leave multibyte and surrogate behavior open. Reopen the preset, Core reader/options, ClientPin parameters,
FidoPinHelper and both layer guides after enforcement or units change. OpenPGP retains its own profile;
its shared alias confers no CTAP rules on PW1/PW3.

## X004 — Track each response owner rather than clearing one convenient view

Aliases: `ctap2-001:CTAP-IMP-027`, related `webauthn-001:WA-IMP-006/013`.
**Documentation:** current response/extension XML and README correctly disclose the separate copies.
**Implementation:** HidBackend transfers a status-stripped view of Core's accumulated array;
SmartCardBackend transfers a view of `ApduResponse`'s copied data. Typed make/get decoders create
authData, largeBlobKey and other independent arrays; make retains attestation/unsigned slices of its
input. AuthenticatorData creates another raw array and an ED suffix. Session disposal clears none of
these successful returned copies. Decode failure after backend transfer also leaves those lifetimes
outside backend cleanup. `StoredCredentialInfo.Decode` instead creates a private response clone,
clears that clone on failure/disposal and exposes largeBlobKey as a slice; the borrowed original survives.
**Policy:** design one coherent explicit owner/view/transfer contract, preserving valid unknown response
data. **Qualification:** `StoredCredentialInfo_Dispose_ZeroesOwnedViewsAndKeepsPublicFieldsUsable` checks
actual retained clone views and unchanged source; it supplies no transport/failure-clone observer.
Reopen all response/AuthenticatorData/attestation/extension owner XML, stored/enumeration models,
S/ICtap2Session.cs, both guides and WA result/PRF/blob consumers after ownership changes. B6 must separately
account for transport/APDU/SCP intermediate owners and APDU-error responses.

## X005 — PIN cryptographic exceptional cleanup remains local

Alias: `ctap2-001:CTAP-IMP-005`. **Documentation:** retain V1/V2/helper cleanup bounds.
**Implementation:** V1 Authenticate's stack full MAC lacks finally clearing; V1/V2 Decrypt can abandon
their allocated output after transform failure; PerformEcdhKeyAgreement derives Z before allocating its
public dictionary. Encapsulate's finally starts only after that helper returns. These are separate
failure sites, rather than one generic Core crypto defect. **Policy:** clear actual SDK-owned arrays on
every exit; provider/internal erasure requires separate evidence. **Qualification:** no provider/crypto
execution occurred. Reopen `S/Pin/PinUvAuthHelpers.cs`, both protocols, security notes and hmac-secret/PRF
callers after each selected site's repair. C's generic private-key/native cleanup concerns stay distinct.

## X006 — PIN peer labels are a CTAP profile boundary

Alias: `ctap2-001:CTAP-IMP-007`. **Documentation:** retain the explicit gap in helper and COSE guides;
“strict checks remain” in a high-level catalog passage means the required target, not proof they all run.
**Implementation:** helper validates X/Y byte-array type/32-byte width, forces BCL P-256 import and
delegates point validation, while omitting peer kty=2/crv=1/alg=-25 and optional-label restrictions.
No call to `Algorithms.FindForKey`, Core COSE metadata or generic CoseKey decode supplies those checks.
**Policy:** keep unknown response carriers open while validating security-critical PIN peers strictly.
**Qualification:** import errors can occur outside the helper's derivation catch. Reopen PIN/Encapsulate,
COSE/forward-compatibility XML, both guides and actual peer fixtures after profile validation changes.

## X007 — ARKG wire profile and Core mathematics have separate authorities

Aliases: `ctap2-001:CTAP-IMP-029`, `ctap2-001:CTAP-DEC-003`; Core C018/C019 remain their own concerns.
**Documentation:** preserve draft-10's -65700 erratum and explicit experimental status. **Implementation:**
CTAP dispatches on alg before kty, reconstructs padded EC2 coordinates without checking nested curve,
and emits kty=2 plus byte-string-wrapped maps. Core Derive validates actual P-256 points and ctx <= 64,
but that mathematical boundary cannot recover the original labels or validate the chosen seed profile.
**Policy:** maintainer selects the pinned vendor/draft profile before changing encoders. **Qualification:**
Core cites expired draft-09 mathematics; CTAP cites expired draft-10 representation. Full equivalence,
Rust generator pin and actual NativeShims producer/provider evidence remain explicit gaps. Reopen
S/Cose, S/Arkg, previewSign generated/derived key types, Core ARKG rationale and both layer draft claims
after a selected profile or mathematical revision; preserve all frozen originals.

## X008 — Derived algorithm propagation is its own defect

Alias: `ctap2-001:CTAP-IMP-039`. **Documentation:** current accessors disclose disagreement.
**Implementation:** `PreviewSignRegistrationOutput.TryGetGeneratedKey` passes seed Algorithm(-65700),
while PreviewSignCbor extraction passes DerivedKeyAlgorithm (typically -9); downstream derivation and
verification remain fixed P-256. **Policy:** settle algorithm dispatch separately from seed-profile
selection. **Qualification:** current generated-key KAT asserts point/handle/context, not either
accessor's algorithm. Reopen both extraction routes, DerivedKeyAlgorithm/DerivePublicKey/VerifySignature,
both-layer result mappings and both-accessor assertions after propagation changes.

## X009 — Experimental signature DER is not Core key DER

Alias: `ctap2-001:CTAP-IMP-041`. **Documentation:** preserve fixed raw-message SHA-256 and limited DER
conversion qualifications. **Implementation:** CTAP's converter reads two INTEGERs and pads them;
it leaves sequence/root exhaustion and component admission incomplete. Core's ASN.1 key representation
validation is another parser and supplies no implicit signature checks. **Policy:** select strict
signature/profile behavior and exception contract. **Qualification:** downstream provider rejection,
malformed vectors and hardware remain unexecuted. Reopen DerivedKey converter/verifier, generated-key
cross-references and preview integration raw-message/prehash comments after this change.

## X010 — Unsigned output support retains the old dropped-output identity

Aliases: `audit-v2-20261022-20261031:#4`, `ctap2-001:CTAP-IMP-030`,
`webauthn-001:WA-IMP-017`. **Documentation:** current get/make field tables accurately distinguish key 8
versus key 6 and incomplete outer-envelope preservation. **Implementation:** get skips key 8; make retains
modeled unsigned values as slices. Core CTAPHID/APDU preserves transport payload, not a CTAP model API.
**Policy:** select supported extension/raw escape hatch with X004 ownership; optional direct-largeBlob
support is a separate choice. **Qualification:** old tested-device absence and API removal give no
key 8 handling evidence. Reopen response raw/unsigned XML, extension codecs, forward-compatibility guides
and WA largeBlob processing only after support/ownership changes.

## X011 — Structural and conditional response checks stay with CTAP

Alias: `ctap2-001:CTAP-IMP-028`. **Documentation:** preserve declared ED/AT/count/key-length/full-envelope
limitations. **Implementation:** AuthenticatorData copies the ED remainder without one-map exhaustion;
make lacks AT enforcement; get casts uint32 count to int. Generic framing guards validate their own
length/CID/sequence domain; they supply no model/request/version validation. **Policy:** distinguish
standalone full-envelope checks from nested-reader consumption and request-conditioned checks.
**Qualification:** no parser change or new execution. Reopen the actual parsers, flags/count/extension
XML, model fixtures and WA recovery consumers after each selected validation change, keeping defects
separate even where they share the same model.

## X012 — Blob numeric ranges and cap are independent policy questions

Alias: `ctap2-001:CTAP-IMP-037`. **Documentation:** retain signed-Int64/read-cap/reconstruction bounds.
**Implementation:** blob origSize/key parsing uses signed ranges; decrypt skips above 1 MiB while encrypt
has no matching cap. Unknown encoded slices have a separate preservation lifetime. Core APDU/HID length
limits neither choose this cap nor promise a transaction across fragments. **Policy:** select numeric/raw
preservation and symmetrical/asymmetrical cap behavior. **Qualification:** no arbitrary-foreign-client
or whole-array atomicity claim. Reopen LargeBlobData/Storage, SpecFormat tests and both layer storage
guidance when the range/cap/preservation target changes.

## X013 — Blob plaintext probes and compression copies need their own cleanup

Alias facet: `ctap2-001:CTAP-IMP-035`. **Documentation:** retain plaintext-probe/request-copy qualifications.
**Implementation:** Set/Delete discard successful TryDecrypt plaintext; fragment auth/tag/payload/request
copies have no comprehensive finally; compression MemoryStream backing storage is another owner.
**Policy:** select scoped clearing without treating public metadata as secret by direction alone.
**Qualification:** current positive storage round trips give no failure-copy/native-erasure proof.
Reopen actual probe/compression/write helpers, storage lifetime XML and WA blob consumers after each repair.

## X014 — hmac-secret salt input has an independent failure scope

Alias facet: `ctap2-001:CTAP-IMP-035`, related `webauthn-001:WA-IMP-013`.
**Documentation:** retain ExtensionBuilder's intermediate-copy qualification. **Implementation:**
BuildHmacSecretInput allocates saltData then calls Encrypt/Authenticate without a cleanup finally;
an authentication failure can abandon encrypted output too. **Policy:** assign DTO/builder/result owners
without conflating salt authentication with token authorization. **Qualification:** PRF decoder clearing
its aggregate plaintext leaves its range/result copies separate. Reopen builder/HmacSecretInput,
PrfCodec/PrfOutput, both guides and real array-observer assertions after salt/result cleanup changes.

## X015 — Encrypted metadata needs an explicit protocol-wrapper choice

Aliases: `ctap2-001:CTAP-IMP-043`, cleanup facet `CTAP-IMP-035`.
**Documentation:** retain IV-only/arbitrary-block acceptance, opaque state and unauthenticated wrong-token
qualifications. **Implementation:** metadata uses BCL HKDF/AES directly, not Core catalog enforcement;
DeriveKey and failed plaintext allocation/transform have separate cleanup scopes. **Policy:** choose
strict GetInfo wrapper versus generic helper; token persistence/permissions remain ClientPin/authenticator
responsibilities. **Qualification:** a byte-equality test with opaque state neither authenticates the
token nor proves persistent-token issuance. Reopen decrypt/DeriveKey/GetInfo fields, two-session fixtures
and WA callers when length/token/cleanup behavior changes.

## X016 — Builder clones outlive transmission-buffer cleanup

Alias: `ctap2-001:CTAP-IMP-004`. **Documentation:** preserve WithBytes/WithEncodedValue private-copy and
WithValue deferred-read descriptions. **Implementation:** Build clears Encode's intermediate array;
captured copies and CborWriter internal storage remain separate. Make/Get clear the final encoded
request in finally, while ClientPin/admin helpers have additional request copies. Raw SendCborRequest
borrows its supplied request; Core report copying never erases it. **Policy:** choose explicit builder
ownership/reset semantics, keeping raw API callers responsible for their borrowed input. **Qualification:**
no end-to-end erasure inference from one cleared array. Reopen builder/raw API, PIN/admin/extension callers,
security examples and WA-IMP-013 dependencies after copy-lifetime changes.

## X017 — Distinguish all firmware observations and configuration policy

**Documentation:** Ctap2Session factory XML accurately places the gate before the override. Retain that.
Core A-C015/B5-C001 own optimistic major-zero wording. CTAP CLAUDE's “treated as modern” should use the
same policy vocabulary when edited. **Implementation:** HID INIT caches vendor bytes separately;
Ctap2Session takes GetInfo's packed low-24-bit interpretation, or 0.0.0, then applies the override only for
Configure/SCP/session FirmwareVersion. Future GetInfo queries do not update it. **Policy:** preserve
capability/configuration/token/device checks beyond nominal thresholds. **Qualification:** Management
5.8.0, CTAP 0.0.1 and effective override are distinct evidence. Reopen factory/feature/GetInfo XML,
transport tests, Core FirmwareVersion and both guides after gate/provenance/config changes. B6 must settle
major-zero ForceShortApdus handling before any claim that a caller's short-APDU option always applies.

## X018 — Carry B5 framing/cancellation recovery into actual CTAP calls

**Documentation:** retain CTAPHID command versus CTAP operation/status distinction and valid-terminal
drain conditions. **Implementation:** backend sends unmasked CBOR 0x10; Core sends CANCEL once after an
observed keepalive, then drains the original response. Raced CTAP success is SDK policy; encoded 0x90's
late-success test differs from normalized-command matching. Malformed control lengths/reserved assigned
CIDs/partial failure without recovery latch remain distinct B5-C002/C003 mechanisms. **Policy:** dispose
and reopen after uncertainty instead of promising universal reuse. **Qualification:** no keepalive/no
terminal response can leave the task pending; physical cancellation and OS abort are separate observations.
Reopen backend/interface/session cancellation XML, cross-layer tests and raw-tier guide after any B5
framing/cancel/reset decision. These mechanisms are reused, not newly counted defects.

## X019 — Received reports remain independent of the accumulated response

**Documentation:** retain scoped cleanup wording, especially secret-derived extension traffic.
**Implementation:** B5-C005 applies at CTAP's real Make/Get boundary: Core clears outgoing packets and
failed response accumulation but leaves returned init/continuation report arrays uncleared. Linux/Mac
native owners clear their staging/queues, while already-returned report storage has escaped that owner.
**Policy:** define transferred report ownership and actual-array clearing across custom connections.
**Qualification:** no general native-memory erasure guarantee. Reopen FidoHidProtocol/connection result
XML, backend ownership, secret-extension notes and report-observer tests after that contract changes.

## X020 — Keep response cleanup reachable when diagnostic callbacks fail

**Documentation:** qualify the universal failure-cleanup sentence in CTAP CLAUDE:344–345 if this candidate
is accepted. Ordinary terminal-callback failure already has actual response-array assertions.
**Implementation:** both backends await final ResolveAsync before zeroing untransferred response.
On a CTAP failure, a throwing terminal callback is caught by UserPresenceNotification and logged; a
throwing logging provider there can escape before backend zeroing. This conditional diagnostic-failure
path is separate from X004 successful-response ownership and X005 crypto cleanup. **Policy:** decide
whether cleanup/primary-exception preservation must survive logger failure; route shared notification/log
behavior to C, local finalization structure to CTAP. **Qualification:** no new sentinel test ran and the
existing backend tests use ordinary logging. Reopen notification/Backend failure XML, module presence
claims and actual-buffer observers after this exceptional-path choice. Do not claim a reproduced leak.

## X021 — Correct the selector's reset-target statement

**Documentation:** `E/Cli/Prompts/DeviceSelector.cs:17–18` says reset callers retain confirmed identity.
`E/Cli/Menus/ResetMenu.cs:98–116` selects independently after reinsertion and reuses the old transport;
Program.cs:130–176 independently selects too. Their current comments and README already disclose it.
Refer the selector to that limitation instead of describing the desired behavior as implemented.
**Implementation:** existing sample target-continuity work stays deferred in the CTAP ledger; no new
selection gate or reset change. **Policy:** final physical-target verification remains human-controlled.
**Qualification:** no reset executed. Reopen selector, both reset callers and README Device Selection/Reset
after identity correlation or current-transport revalidation changes.

## X022 — Test existence, returned bytes and physical qualification remain separate

**Documentation:** retain actual-assertion qualifications in integration fixtures. The CTAP algorithm
test checks nonempty key bytes, not returned COSE curve/alg; FIPS fixtures inspect optional metadata,
not certified execution. Broad NotSupported skips on SmartCard inherit SELECT's broad wrapper.
**Implementation:** harness/stronger oracles require separate scope; existing known deficiencies keep
their original CTAP IDs. **Policy:** run selected meaningful mechanisms under approved setup, never
infer read-only safety from smoke filtering. **Qualification:** no tests ran here; compiled Mac code,
integrated Linux native tests and fake cross-layer journeys supply their recorded scopes only. Reopen
each named fixture's commentary when its actual assertions/setup or exact OS/device run changes.

## X023 — AAGUID conversion and default value remain distinct

Aliases: `ctap2-001:CTAP-IMP-025`, `webauthn-001:WA-IMP-007`.
**Documentation:** retain mixed-endian serialization and default-value qualifications.
**Implementation:** CTAP conversion conditionally reverses the first 4/2/2 byte groups on little-endian hosts; WA default
storage/equality is a separate value-type issue. Neither belongs to Core mathematical/key metadata.
**Policy:** choose fixes independently. **Qualification:** earlier little-endian framework evidence is
retained; no big-endian execution or new Guid probe. Reopen converter/attested AAGUID XML, WA Aaguid
and platform claims only when the corresponding implementation/evidence changes.

## Closure handoffs with explicit owners

These are mandatory follow-ups, rather than guessed B4/B6 conclusions:

| Owner | Required answer before final boundary closure | Reopening paths |
| --- | --- | --- |
| B4 Windows/support, then coordinator's user-ready delta reviewer | Pin actual compatibility/Windows HID and CCID call admission, caller-thread blocking, accepted-I/O drain, cancellation/terminal wake, dispose/reopen, exclusive access/elevation interpretation and binary/runtime identity. Compare the user-supplied ready commit only when announced. | Core FidoHidConnection/Windows report/PCSC XML; CTAP factory/ICtap2Session, cross-layer tests, TestStateExtensions Windows diagnostic, both guides and raw tiers |
| B6 APDU/SCP | Resolve CTAP CLA=0x80 / INS=0x10 / P1=P2=0 / Le=0 through actual short/extended/chaining and SW/error owners; major-zero ForceShortApdus; SCP initial selection/GetInfo/configure/init/rebind, post-handshake applet state, SCP failure recovery and parameters/certificates/intermediate buffers. Keep ISO and final-GP unavailable passages explicit. | SmartCardBackend, factory/SCP example, Core APDU/SCP XML, CTAP transport/SmartCard tests, both guides and raw tiers |
| BOUNDARY-WebAuthn + maintainer/Astra | Keep `ctap2-001:CTAP-DEC-002` selected-native-user actor/privacy/serializer choice distinct from documentation-resolved `WA-XREF-001`/`CTAP-XREF-001`. Decide actual output actor before considering display-field filtering. | GetAssertionResponse.User, AuthenticationResponse.User, selected-response construction/serializer, both layer guides |
| BOUNDARY-WebAuthn | Rebaseline `webauthn-001:WA-IMP-010` descriptor recovery using actual singleton/probe selection and response construction. Keep X011 structural parsing and native privacy separate. | CredentialFilter, WebAuthnClient.Authentication, AuthenticationResponse.CredentialId, CTAP descriptor XML and actual recovery assertions |
| BOUNDARY-WebAuthn | Complete WA-IMP-006/007/013/017/019 caller/result/extension dispositions; the narrow source reads here establish their CTAP dependency, not full WA closure. | WA attestation/Aaguid/PRF/blob/preview params, owning ceremony tests and both layer ownership guides |
| Fresh independent reviewer and coordinator | Recheck complete affected units/consequential originals; choose clean sample IDs; integrate candidate-associated unchanged paths explicitly. | This packet's records plus active product documentation; frozen archives remain read-only |

Investigation completion leaves these mechanism/actor answers, independent review and platform/device
qualification open. The user's PIV physical deferrals and old serial-9681620 discovery uncertainty remain.
