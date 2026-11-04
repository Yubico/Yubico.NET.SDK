# Final boundary assessment — original map and G-001–011

**2026-11-04; fresh independent reviewer GPT-6.1 Sol, not original G. PRIMARY checkout.**
**Baseline/HEAD:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.
**Final verdict, 2026-11-04 correction acceptance: PASS for the inspected documentation boundary.**
BF-R1/BF-R2/BF-R3 are corrected and independently re-reviewed; authorized BF-O1 PASS. No required
boundary documentation corrections remain. Deferred implementation/product/actor decisions remain.
See [boundary-review.md](boundary-review.md) for required/optional feedback and exact corrections;
[fresh original clauses](evidence/g/boundary-originals.md) for independent authority.

## Dated correction follow-up — 2026-11-04

Astra explicitly owns the two correction paths in PRIMARY per `correction-boundary.md`.
[boundary-review-followup.md](boundary-review-followup.md) independently accepts complete permissions/
invariants/key-agreement paragraphs and registration XML/call chain at the refreshed hashes below.
BF-R1 no longer converts requested permissions into issued authorization; BF-O1 separates independent
key agreement from token bytes; BF-R2 now names makeCredential outer0x06, not getAssertion outer0x08.

The coordinator's bounded additional question identified **BF-R3** at `src/WebAuthn/CLAUDE.md:544`:
"flags optional ... Some authenticators return only key3" misleadingly frames draft algorithm-only
outer output as a device variant. P4 puts policy flags inside generated-key attestation; outer4 is
parser tolerance, not beta/device proof. Astra's explicitly assigned narrow correction is now accepted
by [boundary-review-final.md](boundary-review-final.md): item3 accurately separates those layers and
disclaims normative optionality/device-variant proof. Final three corrected hashes are recorded there
and refreshed below, before later coordinator formatting. No source edit was made by this reviewer. Original G map and
initial boundary-review remain unchanged; this section and current row dispositions supersede only
their pending correction status, not evidence/implementation limits or original attribution.

Original `boundary-map.md` is immutable baseline evidence, not rewritten to simulate its investigator
having seen final edits. Its original 29 rows are assessed below in their exact order. PASS means the
documentation truthfully describes the named boundary, not implementation conformance or closure of
the deferred engineering ID. Full paths are repository-relative. In the tables **C** is
`src/Ctap2/src/`, **W** is `src/WebAuthn/src/`; filenames after these roots identify exact source paths.
CTAP README/CLAUDE are `src/Ctap2/README.md`/`src/Ctap2/CLAUDE.md`; WA README/CLAUDE are their
`src/WebAuthn/` counterparts. Original clause labels are defined in the evidence report.

## Row-by-row disposition of the original boundary map

| Original row / claim pair | Final actual files, claims and independently checked evidence | Final assessment / retained IDs / exact reopening trigger |
| --- | --- | --- |
| 1 — G-001 User UV/discoverable restriction | C`Credentials/GetAssertionResponse.cs:24–46,77–87,197–199`; C`Credentials/PublicKeyCredentialTypes.cs:268–407`; W`Client/Authentication/AuthenticationResponse.cs:56–64`, `MatchedCredential.cs:43–50`; WA README:141–147,170–187. S20 §5.2; S21/S21E/S23 §6.2.2 permit server-side user, require discoverable id and prohibit identifying fields without UV. Parser passes name/displayName and skips icon. | **PASS; WA-XREF-001 documentation dependency satisfied.** No display privacy filter/authorization is claimed. CTAP-IMP-026 and DEC-002 stay open. Reopen User/layout/entity constructor/parser, match/selected User and RP handoff on parsing, handle representation or serializer/privacy changes. |
| 2 — G-002 descriptor omission | C`Credentials/GetAssertionResponse.cs:54–59,168–175,222–239`; W`Client/CredentialFilter.cs:130–138`, `Authentication/CredentialMatcher.cs:59–74`, `WebAuthnClient.Authentication.cs:216–220`; WA README:183–187. Fresh S20 actual-request allowList exactly-one exception; S21 original/errata/S23 require descriptor. | **PASS docs; WA-IMP-010 unfixed**, CTAP-IMP-028 context checks deferred. Final ID remains empty despite known selected descriptor. Original singleton is unambiguous application-side, never first original multi-entry guess. Reopen matcher/builder/result/picker/per-credential extension docs on selected-ID recovery or validation. |
| 3 — raw next assertions vs eager matches | C`Ctap2Session.cs:310–387`; CTAP README:114–133; W`Client/Authentication/CredentialMatcher.cs:63–75`, `WebAuthnClient.Authentication.cs:122–173`; WA README:170–181. S23 §6/§6.3 plus S20 §5.3 sequence. | **PASS legitimate orchestration difference.** CTAP caller tracks N−1/adjacency; WA eagerly collects and builds outputs, no-list may enumerate; allow-list narrows instead. CTAP-IMP-003/019 and WA-IMP-010 dependencies remain. Reopen sequence/count/timer/selection/lazy-work guidance after orchestration changes. |
| 4 — G-003 uv/PIN/token | C`Cbor/Ctap2SessionRequestEncoding.cs:63–73,102–112,163–166,190–193`; CTAP README:166–230/CLAUDE:168–209; FidoTool MakeCredential:109–125/GetAssertion:87–102; W`Client/UserVerification/UvDecision.cs:93–116`, `WebAuthnBackend.cs:157–173,215–233`. S23 §6.1/§6.2 platform MUST NOT include uv key with authParam. | **PASS docs; CTAP-IMP-011 still executable in raw/sample paths.** Even uv:false is mutually exclusive. Built-in UV without token is a separate valid capability-dependent request. Reopen options/encoder/sample/guide token tasks on validation or executable correction; do not mandate WebAuthn policy for all raw callers. |
| 5 — G-004 selected protocol vs backend V2 | C`Pin/ClientPin.cs:319–459`; W`Client/WebAuthnBackend.cs:285–292`; both guides' protocol prerequisites. S21E/S23 §6.4 ordered supported protocols and §6.5.5.7 selected protocol. | **PASS explicit layer restriction**, WA-IMP-009 and CTAP-IMP-010 remain. Legacy command fallback is not V1 negotiation. Reopen token/PRF context, protocol requirements and factories on negotiated-protocol changes; no universal PINless incompatibility claim. |
| 6 — built-in UV capability vs token method | C`Pin/ClientPin.cs:462–535`; W`Client/UserVerification/UvDecision.cs:77–79,107–116`; WA CLAUDE:254–270/README:29–31. S23 §6.5.5.7.3 authenticator MUST implement only under uv+pinUvAuthToken true; configured UV and permission gates separate. | **PASS disclosed gap**, WA-IMP-009/CTAP-IMP-010. uv alone is not token assurance; raw helper has no full preflight. Reopen UV decision/retry/PIN fallback and both guides on capability enforcement; no PIN-first normative mandate. |
| 7 — raw tokens vs union/re-mint | C`Pin/ClientPin.cs:396–459`; W`Client/WebAuthnClient.Ceremony.cs:49–97,166–195`, authentication:110–119/registration:117–127,168–170; corrected architecture `docs/architecture/webauthn-extension-processing.md:185–196`. S23 §6.5.5.7 default versus explicit permissions. | **BF-R1 corrected / independently re-reviewed PASS, 2026-11-04.** Invariant now describes requested union and locally disclaims issued authorization after legacy fallback. Union/re-mint remains legitimate orchestration. WA-IMP-009/CTAP-IMP-010 remain; reopen all token/permission lifetime claims on behavior changes, not as a new introspection/negotiation feature mandate. |
| 8 — legacy command retains protocol/scope caveat | C`Pin/ClientPin.cs:375–393,410–417`; W`Client/WebAuthnBackend.cs:119–136`; CTAP README:174–177/CLAUDE:203–210 and WA README:29–31. Original S23 §6.5.5.7.1 forbids legacy permissions/rpId, same selected protocol. | **PASS source/guide relationship; BF-R1 architecture dependency satisfied in 2026-11-04 follow-up.** WA-IMP-009/CTAP-IMP-010 remain. Reopen same-protocol/default-permission/scope guidance on fallback changes; never infer admin permission from token acquisition. |
| 9 — direct caller PIN vs SDK prompt loop | C`Pin/ClientPin.cs:303–308,365–387`; W`Client/WebAuthnClient.PinUvAuth.cs:118–246`; WA CLAUDE:196–236/README:192–215,465–467. Actual fresh-secret, PinInvalid-only retry, blocked/fault/decline/cancel/late-owner handling; F1 ownership rules. | **PASS legitimate orchestration difference.** MaxPromptAttempts is not device retries; accepted PIN can be reused for re-mint, rejected PIN never resubmitted; null means decline. CTAP-IMP-006/008 and WA-IMP-002/009 dependencies remain. Reopen prompt/secret lease, cleanup/late result and retry metadata docs after changes. |
| 10 — G-009 silence/all assertions touch | CTAP README:256–267/CLAUDE:335–350; C`Pin/ClientPin.cs:473–484`, `Ctap2Session.cs:338–350`; W`Client/CredentialFilter.cs:108–116`. S23 built-in UV interaction and up:false assertion semantics. | **PASS corrected blanket narrative.** Generic ClientPin/bio notification silence is not absence of physical biometric interaction; silent probes differ from required-UP final commands. CTAP-IMP-011/012 and WA-IMP-009 remain. Reopen exact operation/notification scope on modeled touch additions or UV flow changes. |
| 11 — HID signal/SmartCard policy/WA forwarding | C`Backend/HidBackend.cs:65–116`, `SmartCardBackend.cs:86–147`; Core `Credentials/UserPresenceNotification.cs:142–203`, `Protocols/Fido/Hid/FidoHidProtocol.cs:327–390`; WA README:217–241/CLAUDE:272–282. | **PASS consistent architecture.** Backend resolves terminal CTAP status before typed decode, request callback differs from resolution callback; untransferred backend bytes cleared, no end-to-end output erasure. CTAP-IMP-004/023, WA-IMP-002 remain. Reopen callback faults, request/terminal ownership, response transfer and prompt lifetimes on any change. |
| 12 — cancellation drain vs deadline/success race | Core FidoHidProtocol:282–315,327–387; C backends and session decode-after-exchange; W`Client/CeremonyDeadline.cs:20–59`, authentication:73–87; both guides cancellation sections. | **PASS no unconditional cancellation/rollback claim.** Completed CTAP success can win; cooperative timeout maps only resulting timer cancellation, caller cancellation propagates. WA-IMP-003/002 and CTAP-IMP-023 remain; do not reopen already-corrected Core raced-success as new bug. Reopen deadline/status/side-effect claims on transport or return-policy changes. |
| 13 — CTAP faults vs selected WA mapping | C backends status/SELECT wrapping; W`Client/WebAuthnClient.Ceremony.cs:129–145`, `WebAuthnClient.PinUvAuth.cs:91–103,174–193`; both extension sessions output catches; authentication:122–135. | **PASS legitimate partial normalization**, WA-IMP-002, CTAP-IMP-023. GetInfo/token/output work are not universally caught; output failure cannot extend creation retry. Reopen exception provenance/InnerException and retry boundary on mapping changes; no transport retry or rollback guarantee. |
| 14 — G-005 hmac-secret variants vs PRF | C`Extensions/Shared/ExtensionBuilder.cs:166–239`; CTAP CLAUDE:273–286/README:321–323; W`Extensions/Inputs/PrfInput.cs:90–101`, `Processing/Processors/PrfProcessor.cs:129–190,236–256`, `Codec/PrfCodec.cs:60–89`, `Outputs/PrfOutput.cs:20–31`; WA README:308–327. Fresh S23 §12.7 and W3 §10.1.4 verified-variant MUST/override. | **PASS documentation; WA-IMP-011/013 unfixed**, CTAP-IMP-035. No PRF wire key, plaintext/result copies remain uncleared; Required workaround is not conformance/cleanup fix. Reopen both ceremonies, UV/processor/input/output docs and CTAP linkage after verified-policy or owned cleanup changes. |
| 15 — hmac-secret-mc plus enablement | C builder:241–272,393–397; W PrfProcessor:69–72,147–157,218–220; both guide capability qualifiers. S23 §12.8 requires hmac-secret:true, W3 creation evaluation permits future CTAP extension. | **PASS source relationship; earliest SKU/firmware remains DEC-003.** Separate advertisement required; threshold metadata is not qualification. WA-IMP-011/013 remain. Reopen registration evaluation/enablement/algorithms/protocol/capability docs on mc support or actual product-evidence changes. |
| 16 — G-006 enforcePolicy | C builder:60–90,370–374; W`Extensions/Inputs/CredProtectInput.cs:25–33`, `Processing/Processors/CredProtectProcessor.cs:24–45`, registration session:64; both guides enforcement paragraphs. Fresh S23 §12.1 platform SHOULD NOT, effective 2→3 permitted. | **PASS truthful inert enforcement; WA-IMP-012 open.** No wire boolean, no post-creation prevention, stronger policy is not failure. Reopen unsupported-device prevention/default-policy/output comparison and all sensitive-storage examples after enforcement change. |
| 17 — credBlob creation bytes/retrieval true | C builder:81–112,353–368; W`Extensions/Processing/Processors/CredBlobProcessor.cs:27–56`, both extension-session factories. S23 §12.2/§12.2.1 capacity, byte/true split, PII level3+enforcement MUST. CTAP CLAUDE:268–271 cautions nonsensitive demo. | **PASS bounded wire/security guidance**, CTAP-IMP-031/027/035, WA-IMP-012/013 remain. Not a whole credBlob capacity audit; no sensitive recipe blessed by inert flag. Reopen input-size/empty/capability policy, enforcement and returned ownership on relevant changes. |
| 18 — G-007 chosen largeBlob route | C builder:114–139; CTAP CLAUDE extension table/largeBlob paragraph and README:251–254; W LargeBlobProcessor:25–50/WA README:338–350. Fresh S23 §12.3/§12.4 explicitly defines direct creation support and unsigned compressed/originalSize outputs. | **PASS corrected SDK-route limitation**, CTAP-IMP-032/030 and WA-IMP-017 remain. WithLargeBlobKey is actual method; absent direct route is not protocol prohibition/device absence/automatic mandatory feature. Reopen builder/codec/unsigned/storage route docs if direct support is selected. |
| 19 — key envelope/RK/Required support | C make response:80–102,210–228/get response:107–117,206–232; W LargeBlobProcessor:58–75,117–140 and LargeBlobInput:23–30. S23 §12.3 advertisement+largeBlobs:true, rk:true, top0x05/0x07, platform-internal key. | **PASS docs; WA-IMP-014 still unfixed**, CTAP-IMP-028/027. Required can return unsupported credential, missing key not validated, present wrong-length key possible. Reopen effective RK/response key/Support enum/output/guide on enforcement changes; never expose key as RP extension output. |
| 20 — original WebAuthn write cardinality | W LargeBlobProcessor:78–104; authentication session:41–60; authentication:164–172; WA LargeBlobInput:48–54/README:343–350. Fresh W3 §10.1.5 exact-one original allowCredentials versus S23 direct write's merely nonempty allowList. | **PASS disclosure; WA-IMP-015 unfixed.** Selected singleton does not validate original count; not a raw CTAP exact-one policy. Reopen original-option validation, selected inputs/eager write/results on cardinality fix. |
| 21 — conditional CTAP write auth vs SDK token-only | C LargeBlobStorage:33–39,173–202,452–480; W LargeBlobProcessor:98–104/152–165 and UvDecision:168–190. Fresh S23 §6.10.2 conditional UV/alwaysUv/lbw rule. | **PASS explicit legitimate supported-policy restriction**, WA-IMP-017/009, CTAP-IMP-010/036. Do not force optional pinless feature implementation or claim every returned legacy token has lbw (BF-R1). Reopen supported route/permissions/UV guide on selected support-policy change. |
| 22 — nullable decrypted blob/errors | C LargeBlobStorage:135–158,204–224,495–519; W LargeBlobProcessor:138–167; authentication session:89–122; WA CLAUDE:416–423/README:343–350. Fresh W3 read rule and original F1 conversion mechanics. | **PASS precise limitation**, WA-IMP-016/002/013 and CTAP-IMP-035/036/037. Missing key omits result; null storage becomes present empty Blob, stored empty is not distinguishable by presence/length; read Ctap/Argument/InvalidData can escape, InvalidOperation/CBOR suppressed; write Ctap/InvalidOperation sets Written=false, other faults may escape. Reopen absent/empty/support/error/cleanup and post-assertion output boundaries on changes. |
| 23 — G-008 report vs config/false facade | C`Extensions/MinPinLength/MinPinLengthInput.cs:19–56`, builder:274–283/405–408; W registration session:66, MinPinLengthProcessor:23–43; both guides report/config distinction. Fresh S23 §12.5 authorized code-point disclosure versus §7.4 policy setting. | **PASS docs; WA-IMP-018 unfixed**, CTAP-IMP-016. Raw DTO encodes Requested, facade ignores false on nonnull presence; null omits. Reopen reused carrier/factory/codec/config/RP-list/units on input or policy changes. |
| 24 — raw preview params vs original-map selection | C builder:304–320,411–439; W PreviewSignProcessor:45–108, PreviewSignCodec:58–81; CTAP CLAUDE:230–243/WA README:356–372. Fresh V4 §10.2.1 nonempty allowlist/full map/selected params/UV-derived flags. | **PASS legitimate transformation**, WA-IMP-019 and CTAP-IMP-040 remain. Do not add signByCredential to raw CTAP or claim support from unsignaled output. Reopen original-map normalization/flags/selection/constructor/context when behavior changes. |
| 25 — preview TBS/args/generated key/trust | C PreviewSignCbor:179–217,230–400; W corrected PreviewSignCodec:83–165; C PreviewSignRegistrationOutput:79–102; seed key:33–53,157–184; V4 generic prehash/map/inner-attestation clauses; A10 §5.1/§5.2. Corrected WA CLAUDE Known Gotchas:540–546. | **BF-R2 and BF-R3 corrected / independently re-reviewed PASS, 2026-11-04.** Gotcha separates draft algorithm3/inner policy from tolerated outer4, without device proof. Generic bytes not semantic validation; seed -65700 agreement retained; kty/wrapping/dkalg differ; inner parsing not trust; distinct chosen/signing/seed/derived identifiers. WA-IMP-006/019, CTAP-IMP-029/038/039/040/041, DEC-003 remain. Reopen profile/algorithm/verifier/trust/flags guidance on implementation changes. |
| 26 — response copies vs scoped owners | C make/get response ownership XML/body; builder saltData:213–239; W PinUvAuthTokenSession:70–99, PinUvSharedSecret:30–54, PrfCodec:75–89, client response construction. F1 owner/view distinction. | **PASS limitations**, WA-IMP-006/013, CTAP-IMP-004/005/027/035. Token/shared-secret clearing does not clear decoded PRF/blob/request copies or borrowed source. Reopen input leases/owner adoption/failure transfer/returned views/disposal examples after ownership changes, not by advising zeroing one copy. |
| 27 — direct hash/context vs client/RP duties | CTAP README:83–142 and sample make/get complete flow; WA README:44–55,133–150,374–450; W authentication/registration construction and RP gate sequencing. Fresh W3 §7.1/§7.2/§13.4.3. | **PASS legitimate responsibility split.** Demo random hashes do not register/sign in; exact signed bytes/RP challenges/account binding/verification and application RP authorization separate. CTAP-IMP-044 optional demo, WA-IMP-005/006/010 retained. Reopen client-data/origin/RP callback/serializer/server handoff on changes; do not mandate SDK RP-server implementation. |
| 28 — native selected-user display actor | W MatchedCredential.User, AuthenticationResponse.User and authentication:219–232 preserve full modeled entity; WA README:141–147 sends handle and provides no response serializer. S21/S21E/S23 account-picker ONLY-id platform-to-WebAuthn passage freshly verified. | **DEC-002 remains needs-decision**, not forced field-stripping, vulnerability or conformance PASS. Correct raw CTAP docs do not decide native vs browser-equivalent selected DTO. Reopen exact result/picker/privacy/RP serializer and conformance claims when actor boundary is decided or serializer introduced. |
| 29 — G-010 requirements/support | CTAP README:10–29/CLAUDE:28–50; WA README:17–31; C session:147–168,462–478, factories:146–172; W backend:285–292. Fresh S23 GetInfo capability definitions; firmware constants/gates observed in source, not device qualification. | **PASS qualified inherited requirements**, DEC-003/CTAP-IMP-001/002/010 and WA-IMP-009 remain. Revision, optional advertisement, configured state, permissions, sensor/SKU, firmware and current FIDO-AID transport independent. NFC capability alone is not current Transport.Nfc; no fallback transport. Reopen both guides/factories/features on qualification/firmware/AID/current-transport changes. |

## G candidate closeout dimensions

| Original candidate | Final documentation dimension | Implementation / decision dimension |
| --- | --- | --- |
| G-001 | **PASS; WA-XREF-001 docs verified/satisfied** (response layout and User/local entity/match/selected/handle guidance checked). | CTAP-IMP-026 representation and DEC-002 stay open. Coordinator can record documentation-only closure, without editing immutable original WA/G history. |
| G-002 | **PASS version/actual-cardinality explanation.** | WA-IMP-010 remains final empty-ID defect; CTAP-IMP-028 context validation deferred. |
| G-003 | **PASS corrected XML/Markdown plus candid executable sample caveat.** | CTAP-IMP-011 remains raw/sample UV-key bug; WebAuthn correctly omits uv. |
| G-004 | **PASS selected-protocol and built-in-UV caveats; BF-R1 architecture correction independently accepted 2026-11-04.** | WA-IMP-009 and CTAP-IMP-010 remain, not protocol negotiation or full capability enforcement. |
| G-005 | **PASS verified-variant/transform/cleanup linkage.** | WA-IMP-011/013 and CTAP-IMP-035 remain. |
| G-006 | **PASS inert enforcement/platform strength/sensitive content/stronger policy.** | WA-IMP-012 remains; no protection-prevention fix. |
| G-007 | **PASS route/envelope/RK/original cardinality/token restriction/result errors.** | WA-IMP-013/014/015/016/017, CTAP-IMP-027/028/030/032/035/036/037 remain. |
| G-008 | **PASS report not configure, authorized RP/code points/nonnull false caveat.** | WA-IMP-018 and CTAP-IMP-016 remain. |
| G-009 | **PASS operation-specific notifications/terminal-before-decode/provenance/eager side effects/timeout.** | WA-IMP-002/003, CTAP-IMP-004/023 remain; existing raced-success fix not recounted. |
| G-010 | **PASS truthful support qualification including F's sole WA README requirements change.** | DEC-003 product questions, CTAP-IMP-001/002/010 and WA-IMP-009 remain. |
| G-011 | **Raw-field documentation PASS; selected-native actor not adjudicated.** | **DEC-002 needs-decision**, no forced privacy-filtering API, full browser privacy audit or new security-defect count. |

### Late COORD-001 and experimental precision

**COORD-001:** C`AuthenticatorInfo.cs:376,381,487–510,556,561` directly assigns nullable byte arrays to
nullable memory. Fresh original runtime source confirms null array → nonnullable default memory →
present nullable memory. A's corrected intro/encrypted properties/Decode and F's README:67–70/
CLAUDE:155–159 accurately send callers to complete `RawData` for encoded presence. Optional numeric,
Boolean/text locals stay null. This remains CTAP-IMP-001, not an extra engineering group or blanket
absence-preservation guarantee. Reopen GetInfo/metadata/security/support guides and presence fixtures
if assignment/representation/decoder strictness changes. No SDK conversion experiment was run.

**ARKG:** A10 explicitly agrees with seed -65700/BL=-1/KEM=-2/derived example -9. Do not resurrect
withdrawn seed-ID disagreement. Actual encoder kty2/bstr nested maps and required integer derived
algorithm differ from A10's kty-65537/direct maps/optional int-or-text; draft-10 expired 2026-08-31.
Fixed P-256/SHA256 raw-message VerifySignature and incomplete DER checks are not generic TBS/trust
verification. The actual accessor's seed-vs-derived propagation mismatch remains CTAP-IMP-039.
Production warnings and DEC-003/CTAP-IMP-029/038/040/041 survive prose acceptance.

## Inspected scope — boundary dependencies, not a whole-module re-audit

All narratives listed as full were read completely for boundary claims; this does not re-audit every
unrelated API/example in them. Source full means complete file read; partial explicitly limits claims.

- **Full narratives/instructions:** root CLAUDE; both module CLAUDE/README; SDK-HOUSE-STYLE;
  canonical workflow/records style; run plan/triage/addendum/original G/map/evidence; final A/B/C/D/E-core/F
  follow-ups and E-crypto; WA handoff/original triage; FidoTool README; WebAuthn extension architecture.
  These records supply scope/provenance, not independent code/spec proof.
- **CTAP full source:** Ctap2Session, IYubiKeyExtensions, Backend/HidBackend, Backend/SmartCardBackend,
  Cbor/Ctap2SessionRequestEncoding, Credentials/GetAssertionResponse, Extensions/Shared/ExtensionBuilder,
  Extensions/MinPinLength/MinPinLengthInput, Extensions/HmacSecret/HmacSecretOutput,
  LargeBlobs/LargeBlobStorage, Cose/CoseArkgP256SeedKey,
  Extensions/PreviewSign/PreviewSignRegistrationOutput, Extensions/PreviewSign/PreviewSignDerivedKey,
  Crypto/EncryptedMetadataDecryptor; complete FidoTool FidoExamples/MakeCredential and GetAssertion.
- **CTAP partial source:** AuthenticatorInfo:19–569 (properties, full decoder/initializer; remaining
  helper implementations not re-audited); Pin/ClientPin:290–564 (legacy/PIN/UV tokens/key agreement);
  Credentials/PublicKeyCredentialTypes:260–440 (complete user entity, not all earlier RP/descriptor units);
  Credentials/MakeCredentialResponse:19–305 (all response contracts/decoder; boilerplate excluded);
  Extensions/PreviewSign/PreviewSignCbor:160–419 (creation encoder tail, complete signing encoder,
  registration/inner parser, authentication decoder introduction; extraction tail not freshly reread).
- **WA full source:** Client/WebAuthnBackend, WebAuthnClient.Authentication, WebAuthnClient.Ceremony,
  WebAuthnClient.PinUvAuth, CredentialFilter, UserVerification/UvDecision, CeremonyDeadline,
  PinUvAuthTokenSession, PinUvSharedSecret; Client/Authentication/AuthenticationResponse,
  MatchedCredential, CredentialMatcher; Extensions/Processing/RegistrationExtensionSession,
  AuthenticationExtensionSession; Processors/PrfProcessor, LargeBlobProcessor, CredProtectProcessor,
  CredBlobProcessor, MinPinLengthProcessor, PreviewSignProcessor; Extensions/Codec/PrfCodec,
  PreviewSignCodec; Extensions/Inputs/PrfInput, CredProtectInput, LargeBlobInput; Outputs/PrfOutput.
  Processors refers to `src/WebAuthn/src/Extensions/Processing/Processors/` throughout this bullet.
- **WA partial:** Client/WebAuthnClient.Registration:85–280 (core/re-mint/result/transport policy).
  No whole WA source/test/security/privacy audit; unchanged WASource was read as a boundary dependency,
  not newly accepted as a whole module. No original WA engineering ID is closed.
- **Shared partial:** Core CLAUDE:1–260; Core FidoHidProtocol:240–400 (drain/callback/cancel-success
  mechanism); complete Core Credentials/UserPresenceNotification; architecture sdk-architecture-diagrams:
  92–283 (L2/L3b CTAP source/teaching; intervening unrelated L3 not independently audited). No whole Core
  transport/platform audit. No tests were semantically re-reviewed as new proof or executed here.

## Exact key final claim/source hashes

SHA256 hashes actual PRIMARY file bytes at review, not Git-normalized blobs. Full/partial inspection
scope is as listed above; hashing a whole file does not imply full semantic inspection of it. The two
BF-R1/BF-R2 hashes are refreshed after 2026-11-04 follow-up and BF-R3 after final acceptance; original
hashes remain in initial review/follow-up provenance. These are pre-format source snapshots, not a
formatter verification claim or the eventual committed-byte manifest.

| Repository-relative path | SHA256 |
| --- | --- |
| `src/Ctap2/README.md` | `2d41ea0fb0c4477355680cc6f271e7277a4f108b6012986a230aadf3d3df1225` |
| `src/Ctap2/CLAUDE.md` | `0bfe308f3dcaf2307bc2e3d463f174bd4a341a85b583c1ac2cb07d0ad65c2dcf` |
| `src/WebAuthn/README.md` | `cf64612066288c5845cd5d9892038abae2ac0cc17e746ab4fbc7e401069208f7` |
| `src/WebAuthn/CLAUDE.md` | `1d47c824d5c7eb16e7e0ec18f64300c621f542a7bd19d9d2d4bdeea8495918e9` |
| `docs/architecture/webauthn-extension-processing.md` | `56ce5bf608fa1452402100b3d6e51af6b8a3b8277d0f3ada1e8b4b342bffe09c` |
| `docs/architecture/sdk-architecture-diagrams.md` | `a38fe512d725b2f405796da6fbc7c2535fa4106fc816d333ad7bc306c69e9701` |
| `src/Ctap2/src/Credentials/GetAssertionResponse.cs` | `3440938cf36d8db127535daf253e24e4568e2c22480d58205654a7db8369ee99` |
| `src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs` | `ac31b8bb4dbdd1e51723d44512cd1b0a78ff02d42965dffcccf7e0e747aa869a` |
| `src/Ctap2/src/Credentials/MakeCredentialResponse.cs` | `d3da37b6fd4ba90c003950c984b7ec697e44f4f49fb630bba7fb9c31b6d23bed` |
| `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs` | `7a13e008169055255249c08fea67fb1dd035b2996220a428e299f719dd880b8b` |
| `src/Ctap2/src/Ctap2Session.cs` | `23fbfeec9b58730d49638c7557b160102dc01fa233f8e1fd1241778d41056eab` |
| `src/Ctap2/src/IYubiKeyExtensions.cs` | `141e10279591de25200a5d1a41d9a493326eb12b0f14b3f503ec3b8e5dcb3644` |
| `src/Ctap2/src/AuthenticatorInfo.cs` | `922322ac1814df72ab9b03d03eb1ad74a65584c7cbf3a55abb90a79f8437f88a` |
| `src/Ctap2/src/Pin/ClientPin.cs` | `5a937547f76796fd6c72b5ff7a01d60c428e10098474fda747928605ce0407c0` |
| `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs` | `b2867e349c5e6232461ac869052dc086947786f5f7baa6da87f5bff14255a345` |
| `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs` | `b94c0c18a83700b75d2d122af35a12cc2d2759905c90734613396ee3c8597bfc` |
| `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs` | `cda6964cd8372bd805274f57f4d3fde51e00b9dd143becc6d87829888bf820ac` |
| `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs` | `5aad0e30eab64df67832d6f61b36923b1a0175d73c889bdff0dbd5fb06687caf` |
| `src/WebAuthn/src/Client/WebAuthnBackend.cs` | `ad7e6403a5938844ccdfc05680230252c47f8cb53170b04d2541d0f09664365b` |
| `src/WebAuthn/src/Client/UserVerification/UvDecision.cs` | `f4326f93e6a56ebb853c2e21976d284f217716c32a3bf2be239eec7538794a9f` |
| `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs` | `6a56a3b90271e8aab24cf959fc7ff26c3263af64e92bf4c3d1c2f174d7b44c51` |
| `src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs` | `bc4efb9c5baabe280698100dcf93e640dc088c1f191887229f823319eb0d28b5` |
| `src/WebAuthn/src/Extensions/Processing/Processors/PrfProcessor.cs` | `4e547a860099b61ba5e6cf9feeed4ead4ea3f6ff39ffdf86cb487fb405fd2cb3` |
| `src/WebAuthn/src/Extensions/Codec/PrfCodec.cs` | `19c79865f60ff7e49f057bc94c5a2e92f1a884ce1f15f37e6666aa779c8fa872` |
| `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs` | `2c13aca84e970d61ba6941f782d1e6019bb330fbbe07e8a736d78957370a6c9f` |
| `src/WebAuthn/src/Extensions/Codec/PreviewSignCodec.cs` | `2ed3b03efcb3e82fd58980b91518abe0638b064207e7ccae025e920aa2a30fb7` |
| `src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs` | `fc50686f051f5fb6f3ee01aca09f3b9f2175f9fc60cc6c97bbc7f2ed308c62a2` |
| `src/Core/src/Credentials/UserPresenceNotification.cs` | `4940e70c5f1ef5c2892a444b6e8bae77dabdc50253e25bc10ee8556e8f3985f1` |

## Integration identity, closure limits and re-review handoff

Fresh read-only Python parsed final A/B/C/D/E-core/E-crypto/F hash manifests: **162 unique accepted
content paths**, 160 exact matches in PRIMARY. Two csproj current hashes match integration-line-endings.json;
independent comparison with accepted F checkout proves normalized LF contents identical:

- `src/Ctap2/examples/FidoTool/FidoTool.csproj`: current
  `3afca1e1da61588d000b80f452ace1649cb16b0168aee625f93bed1b43927af0`, accepted F
  `24482e538235542bd6d5609c9e6ec06ab1687cc1ada93efe32f4f45a27b25c76`.
- `src/Ctap2/src/Yubico.YubiKit.Ctap2.csproj`: current
  `9648863ec979a242a2f1c9ccb6be0d005309a53764a8e07b53e3605b7954c747`, accepted F
  `859c43a9e8e3254af21bd74b46c9174ec2c65a977a489624a0785d1d16ba7344`.

Original map SHA256 remains `097c23d16d9f72f16a0a05db730b5bee4a5e0fda6a6d872fe59e2e668c713c36`.
These are identity checks, not executable-token/API/compiler or complete162-file semantic coverage.
No implementation/test assertion changed by this reviewer. Report source hashes and row dispositions
must be rechecked if final files/dependencies change. BF-R1/BF-R2/BF-R3 focused re-review is now
accepted, with no required boundary correction remaining. Later formatter/import/EOF changes require
the coordinator's exact final manifest and separate code-body/directive checks; no format PASS is
claimed by this review.

Coordinator owns explicit new comment-path assignment, ledger status updates, mechanical preservation,
compiler/XML/snippet/build/test/format/architecture gates, generated L2/L3b SVG/PNG/manifest freshness,
tokens, sampling, integration/commit/remote checkpoint and any later authorized hardware. This reviewer
accepts **diagram source only**, not generated images. Original WA engineering IDs remain open; only
WA-XREF-001's documentation dependency is satisfied. No whole-WebAuthn re-audit, native privacy decision,
full standards/vendor conformance, production crypto, hardware qualification, rollback, end-to-end
erasure, or Pass2 commencement follows from this review.
