# WebAuthn/Core local concerns and documentation reopening

`CORE-WA-X001` onward are packet-local concern groups, not accepted Core IDs or an additive defect
count. All product paths remain unchanged. Documentation, runtime, decision and qualification facets
remain separate. Original source-run keys are preserved in `inherited-dispositions.json`.

Here `W/` is `src/WebAuthn/`, `C/` is `src/Ctap2/`, and `K/` is `src/Core/`. Future editors retain
single-file ownership; shared-document editing belongs to SHARED, not this investigator.

## X001 — Bound connection exclusion to observed membership

**Documentation:** correct W/README.md:455's “all of its interfaces” to SDK-managed opens across
overlapping known member interfaces. Preserve the one-session-per-connection rule and no-failover
factory behavior. A's grouping/serial-to-slot caveats qualify consumer selection as well as native
admission. **Runtime:** reuse A/B1/B4 mechanisms, with no new registry defect. **Decision:** retain
current conservative membership policy. **Qualification:** external processes, direct factories and
disjoint scoped views need their own coordination. Reopen W/README Constraints, W/CLAUDE Client Usage,
W/src/IYubiKeyExtensions.cs, K device/session XML, physical-device/discovery/raw-tier guides and
docs/migration/v1-to-v2.md when grouping, known-member acquisition, ownership or transport order changes.

## X002 — Explain the facade's whole-ceremony lifetime

**Documentation:** extend W/src/Client/WebAuthnClient.cs:24–45 and README Constraints with fully awaited,
serialized ceremony ownership before disposal and prompt resources through actual late work. Existing
session-transfer and returned-buffer qualifications remain accurate. **Runtime:** client/backend bool
flags change after awaited disposal; backend disposes shared PIN protocol before awaiting session.
Core drains one exchange, not token acquisition/probing/GetNext/blob/output continuation. There is no
client operation join or concurrent-disposal gate. **Decision:** future workflow admission, shared
disposal and reentrancy design belongs to separately selected implementation work; do not invent a lease
API. **Qualification:** current substitute disposal-count tests establish one call in one sequential
scenario. Reopen Client/Backend/ceremony/extension lifetime XML, both module guides, user-interaction/raw
tiers, migration ownership guidance, Construction/CeremonyFrame/Backend/intent tests when this changes.
This carries B5-C006 / CTAP-X002 rather than counting another generic Core bug.

## X003 — Keep prompt units distinct from PIN profile enforcement

**Documentation:** retain exact-length prompt owner transfer, code-point minimum and byte maximum in
W/CLAUDE:196–236 and prompt-context tests. Qualify README:194–197's suggestion of a Core reader adapter
with the UTF-16/options boundary if expanding it. **Runtime:** WebAuthn supplies context but does not
validate strict UTF-8/NFC/code-point minimum/final NUL; ClientPin validates byte length only. Core reader
uses char counts/configurable encoding, including redirected string ownership. **Decision:** select
application/reader/CTAP enforcement responsibility without silently normalizing an existing credential.
**Qualification:** ASCII scripted prompts and context values prove no Unicode enforcement. Reopen
W PIN parameters, WebAuthnClient.PinUvAuth, WebAuthnClientOptions, prompt examples; K reader/options/
CredentialPromptContext and C Credentials/Ctap2CredentialOptions/ClientPin/PIN helper guidance after
unit/profile changes. Preserve `ctap2-001:CTAP-XREF-003` and CTAP-IMP-006; no universal Core PIN rule.

## X004 — Preserve the two prompt owners and outcome boundaries

**Documentation:** retain sessionOptions.UserPresencePrompt versus clientOptions.CredentialPrompt.
Clarify W UnitTests/Client/UserPresenceIntentTests.cs:30–32: its real CTAP/Core session and SmartCard
protocol stop at a fake connection, not “every layer” through native transport. **Runtime:** Core
captures ambient intent in caller flow at notification creation; lower protocol requests; CTAP backend
resolves from status before typed decode. Generic ClientPin and GetNext are silent; a successful CTAP
notification is not whole-ceremony/RP success. Request failure disables resolution; terminal resolution
uses None and can delay return after timeout. CTAP-X020's throwing-logger path can bypass backend
cleanup after callback failure. **Decision:** retain callback/diagnostic-failure resilience as distinct
selected work; no new WebAuthn progress/prompt policy. **Qualification:** intent tests inspect request
context only, not resolved outcomes/native waits/physical gestures. Reopen K notification/intent/XML,
C backend and session/presence XML, W interaction/CeremonyDeadline and intent-test commentary, both
guides and docs/usage/user-interaction.md after outcome/capture/callback-drain changes.

## X005 — Carry cooperative timeout and recovery bounds to ceremonies

Alias `webauthn-001:WA-IMP-003`; Core B5-C002/C003, B4-C001, B6-C003 retain their own paths.
**Documentation:** existing options/Deadline/README cancellation prose correctly discloses cooperative
signals and raced success; retain it. Add no universal “cancellation leaves session reusable” claim.
**Runtime:** timer is after validation/RP callback; CTS delay can throw; WaitAsync cancels prompt wait
only; admitted HID cancellation waits for keepalive then original response, SmartCard drains APDU.
Malformed/partial/native failures retain recovery uncertainty. **Decision:** timeout range/error and
success-race contract remain WebAuthn policy, native cancel/drain/release remain Core/platform.
**Qualification:** hardware timeout test's five seconds plus two-second slack is its own assumption,
and checks one same-session GetInfo after expected cancellation. No run here. Reopen both Timeout
properties, public ceremony exceptions, Deadline, validation mapper, README cancellation, timeout/PIN
tests and C/K cross-layer/native guidance after mechanism, delay or outcome changes.

## X006 — Track response and extension storage across transfers

Aliases WA-IMP-006/013, CTAP-IMP-004/027 and CTAP-X004/X013/X014/X016/X019; B6-C002/C006 remain distinct.
**Documentation:** retain borrowed attestation input, separately materialized authData, read-only
sensitive-result and client-disposal limitations. **Runtime:** Core output/response copies, CTAP typed
arrays/slices, WebAuthn additional authData, PRF salt concat/range temporaries/results, blob plaintext
and builder-held arrays have different owners. Clearing one view does not clear unrelated clones.
**Decision:** choose explicit owner/view/transfer and exceptional cleanup without sacrificing valid
unknown response data. **Qualification:** token sentinel tests observe their actual arrays; shared-secret
test only observes an old copy/access guard. Native/provider internal erasure remains unproven.
Reopen W Client/AuthenticationResponse/RegistrationResponse/WebAuthnAuthenticatorData/Attestation XML,
PRF/blob/credBlob output and processor/codec/aggregate XML, both guides and architecture; C response/
builder/storage XML; K ApduResponse/FIDO/SCP buffer rationale; actual retained-buffer observers after
each selected repair. Public identifier metadata is not classified as secret merely because received.

## X007 — Keep descriptor recovery separate from corrected descriptor prose

Alias `webauthn-001:WA-IMP-010`. **Documentation:** retain corrected CTAP2.0 versus 2.1/2.3 attribution
and safe original-singleton workaround. **Runtime:** the selected/probed descriptor enters final
singleton request, but Matcher and BuildAuthenticationResponse both reconstruct ID from final response
and lose it on omission. **Decision:** define association and malformed/version mismatch policy; do
not guess a first original multi-list entry or add an API policy here. **Qualification:** normal fake
responses supply descriptors; current selected tests miss final omission. Reopen CredentialFilter,
CredentialMatcher, Authentication construction, MatchedCredential.Id, AuthenticationResponse.CredentialId,
per-credential PRF/preview inputs, selection guide and actual omission fixtures after repair. Existing
WA-XREF-001/CTAP-XREF-001 documentation resolution remains intact.

## X008 — Resolve the native output actor before disclosure policy

Alias `ctap2-001:CTAP-DEC-002`; distinct from WA-IMP-010 and both resolved XREF keys.
**Documentation:** User XML correctly describes actual passthrough/handle versus display fields.
README:133–147 supplies application RP handoff and explicitly says no SDK response serializer/upload
client. **Runtime:** selected result keeps modeled User unchanged; selection is cached response access,
not a privacy transformation. **Decision:** maintainer/coordinator must name whether native DTO is
picker/raw application data or intended platform-to-WebAuthn/browser-equivalent disclosure boundary,
and define corresponding RP serializer contract. Source cannot decide this intent. **Qualification:**
authenticator's MUST NOT return identifying fields without UV and platform's ONLY-id-after-account-picker
statement have separate actors/conditions. No field stripping authorized. Reopen C GetAssertionResponse/
PublicKeyCredentialUserEntity, W AuthenticationResponse/MatchedCredential/selection construction, any
future serializer, both guides and migration on that decision; keep existing account-binding advice.

## X009 — State version, cache and reset provenance without substitution

**Documentation:** factory XML/README correctly use actual NFC transport, detected USB gate, Core
forwarded options and no WebAuthn firmware gates. Retain them. If expanding initialization/lifetime
guidance, explain backend-local lazy GetInfo cache versus CTAP factory GetInfo and effective firmware.
**Runtime:** no cache invalidation/reset/reinitialization facade; ClientPin can use fresh GetInfo while
ceremony decision uses cached state. Core INIT/Management/discovery snapshots differ. A-C003 metadata
alias, A-C017 PID cache, A-C021 serial-less cache are relevant to selection only, not WebAuthn-owned
cache defects. **Decision:** future reset/cache/major-zero/ForceShortApdus policies retain CTAP/Core
owners. **Qualification:** 0.0.1 does not prove a CTAP edition or 5.8 support; cached transports are
advertised hints. Reopen W factory/Backend/ExtensionContext/transport response and guide prerequisites,
C init/GetInfo/firmware and K Feature/ProtocolConfiguration/session guidance on each selected change.

## X010 — Keep crypto metadata, strict profiles and experimental helpers separate

Aliases CTAP-IMP-025/029/039 and WA-IMP-007/019. **Documentation:** retain draft status, algorithm roles,
unverified generated attestation, borrowed signing params and no production ARKG advice. **Runtime:**
Core numeric algorithm carrier conversion is no provider/profile validator; PIN crypto uses direct BCL.
CTAP accessor algorithm propagation, AAGUID endian conversion and limited signature DER are distinct
from WebAuthn default Aaguid and empty-handle constructor. **Decision:** select applicable draft/vendor
wire profile and algorithm dispatch before repairs; C's generic catalog/provider/math work does not
silently fix CTAP peers. **Qualification:** Core math draft-09 and CTAP representation draft-10 remain
separate; deployed NativeShims/ISO/SCP/provider gaps are unchanged. Reopen W previewSign/Aaguid/
registration public-key XML and reflection bridge/test rationale, C COSE/ARKG/PIN/DER and K catalog/
provider/native/math documentation only for the corresponding change. Preserve -65700 erratum.

## X011 — Preserve applet extension and conformance decisions

Aliases WA-IMP-001/002/005/009/011/012/014/015/016/017/018/019; related CTAP-IMP-030 and old #4.
**Documentation:** current limitations already accurately disclose Enterprise/Indirect passthrough,
CCDToString gap, protocol-2-only token/key agreement, incomplete error normalization, PRF UV override,
credProtect enforcement, blob Required/cardinality/result/route, minPin presence, empty handles.
Retain them as candidate-associated unchanged paths. **Runtime:** Core transport/copy changes supply
none of those missing policies. **Decision:** applet maintainers select coherent implementation groups;
this boundary closes none automatically. **Qualification:** positive round trips and fake parsers do
not prove browser/RP conformance. Reopen each original inherited key's exact XML/guide/test locations
after its own implementation decision; a general Core release is not a blanket reopening trigger.

## X012 — Narrow test explanations to actual oracles and provenance

Aliases WA-IMP-004/008/020/021. **Documentation:** propose two localized corrections beyond X004:
CeremonyFrameTests.cs:281–284's “5.8.0” re-mint rationale has no pinned measurement here; describe the
asserted permission sequence and retain firmware rationale as qualified historical provenance.
IYubiKeyExtensionsTransportTests.cs:67's one selection case supports that route, not a proof of all
absence of WebAuthn transport logic. Existing UV guard, shared-secret, inner-fixture and membership
limitations remain accurate. **Runtime:** stronger guards/actual retained-view/minimal-inner/member-ID
assertions retain their old applet test IDs. **Decision:** no test assertion or executable diagnostic
change in Pass 1. **Qualification:** transport fake deliberately throws at Connect, GetInfo fake can
omit required AAGUID, protocol fake Version=2/tagLength=16 is noncryptographic, Core's single-exchange
fake journeys do not test a whole ceremony/native release. Reopen actual test/helper summaries plus
W/CLAUDE test lanes and docs/TESTING.md/shared fixtures only when their setup/assertions/run evidence
changes. No helper existence or prior compilation is new physical proof.

## Active reopening map versus applet-owned retention

**Current Core boundary facts require active documentation work now** for W/README:455 membership
qualification (X001) and a whole-ceremony-before-disposal condition beside W client lifetime/README
Constraints (X002). Clarify request-only fake-connection intent coverage in the internal test summary
(X004). Coordinate parallel Ctap2 membership/lifetime guide corrections with its owner; neither worker
edits the other's records. X012's two local oracle/provenance corrections are proposed separately.

**Current Core facts require retained qualifications, not a new applet rewrite:** factory prepare/ready
ordering, no fallback, borrowed connection versus transferred session, prompt ownership/intent, timer
versus drain, firmware provenance, sensitive response copies and native cross-OS limits (X003–X010).
Existing accurate prose receives explicit unchanged-path decisions and future triggers in the ledgers.

**Applet-owned until separately selected:** all 21 WA engineering IDs retain their definitions and
status dimensions; X011 and the inherited table locate each one. The two XREF prose resolutions remain
resolved; DEC-002 remains a decision. A new Core response owner, credential reader fix, native Windows
owner, SCP repair or catalog definition alone cannot close those WebAuthn requirements.

| Change arriving later | Exact active surfaces to recheck together |
| --- | --- |
| Ready Windows CCID/HID owner delta | W factory/README requirements/cancellation/Constraints; client lifetime XML, Timeout properties, Deadline comments, intent/timeout/SmartCard factory tests; C backends/session/cross-layer tests; K slots/registered wrappers/native owner/raw tiers. Pin commit and actual invocation/terminal/drain/release/reopen evidence first. |
| Core session/workflow/disposal/notification | W Client/Backend/CeremonyAuth/extension session XML; README Constraints/User interaction; docs/usage/user-interaction.md and docs/architecture/webauthn-extension-processing.md; Construction/Backend/CeremonyFrame/intent tests; migration. |
| Core APDU/SCP/configuration/response-copy or release | W factory/sessionOptions XML, README SmartCard/SCP prerequisites, timeout/lifetime/security and examples; C SmartCardBackend and owning CTAP tests; K APDU/SCP/source/provider notes. Distinguish derived session keys from borrowed parameters. |
| Reader/prompt/copy/profile repair | W explicit/prompt PIN parameters/examples/options/context/retry tests; C ClientPin/PIN/profile; K reader/options/pooled owner and interaction guide. Remove no limitation beyond observed fix. |
| Crypto/provider/catalog/metadata changes | W numeric algorithm conversion, result/key/transport meanings and experimental test bridge only where actual dependency changes; C strict PIN/COSE/ARKG; K A/C metadata/catalog/provider XML. General catalogs/last-known metadata remain no applet capability or native qualification. |
