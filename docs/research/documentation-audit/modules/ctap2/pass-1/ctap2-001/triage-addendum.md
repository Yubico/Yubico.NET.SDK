# CTAP2 supplemental candidate reconciliation

2026-11-04 reconciliation of 2026-11-03 evidence. **Accepted by Astra as the supplemental ledger**, not replacement of
[triage.md](triage.md). All 103 candidate groups are accounted for: A16/B13/C15/D17/E19/F12/G11.
They overlap; they are not 103 independent defects. Main-ledger IMP-001–025 remain reserved,
including rejected IMP-017. Supplemental **CTAP-IMP-026–048** are durable IDs (23 groups),
not approval of a design, source change, executable sample change or test. C/E/F documentation is
accepted for editing subject to independent original-source review. Source acceptance, final edits,
review/re-review, final hashes and validation remain pending. No build/test/hardware/commit occurred
in this records task. Source observations below are attributed to the immutable investigations,
not newly executed SDK reproductions.

## Reading the reconciliation

`DOC` = documentation-only correction; `bug` = specific source-backed incorrect behavior;
`decision` = desired contract/enforcement unresolved; `test-gap` = inspected assertions insufficient;
`demo` = optional sample enhancement, not a mandatory SDK feature. A group can contain more than one
kind, identified below. All implementation statuses are **deferred / desired-not-authorized**.
All DOC statuses are **accepted-to-edit / independent-review-pending**, not closed. IDs below omit
the repeated `CTAP-` prefix for compactness; **WA IDs never omit their prefix**. Exact observations,
baseline ranges, primary quotations and detailed impact maps remain in the linked investigation's
candidate section. Those sections are incorporated by reference, not overwritten.

### A — 16 candidates

| Candidate | Stable dispositions / overlap |
| --- | --- |
| A-001 | DOC-002; DEC-003 product/reset-window evidence; no mandatory local timer feature |
| A-002 | DOC-002; notification truth, no new implementation policy |
| A-003 | DOC-001; IMP-002 decision |
| A-004 | DOC-001; IMP-002 overlapping gate/default decision |
| A-005 | DOC-001/017; IMP-043 metadata-wrapper dependency |
| A-006 | IMP-001; DOC-003/012 decoder/fixture qualification |
| A-007 | DOC-003; IMP-003 typed/raw enforcement decision |
| A-008 | DOC-003; IMP-004; IMP-022 test-gap overlap |
| A-009 | DOC-002/011; IMP-023 error provenance decision; WA-IMP-002 dependency |
| A-010 | DOC-002; WA-IMP-003 dependency; historical raced-success defect not reopened |
| A-011 | DOC-012; IMP-020 shared PIN-normalizing setup |
| A-012 | DOC-012; IMP-021 cleanup/recovery |
| A-013 | DOC-012; IMP-022 test-gap; IMP-021 cleanup dependency |
| A-014 | IMP-025; DEC-001 resolved by framework adjudication; DOC-003 conversion prose; no big-endian execution |
| A-015 | DOC-001/014/018; alternative session scopes, not runtime factory defect |
| A-016 | DOC-003; required-field decisions remain with each consuming decoder |

### B — 13 candidates

| Candidate | Stable dispositions / overlap |
| --- | --- |
| B-01 | DOC-004; actual KDF/zero padding is not a bug |
| B-02 | DOC-004; supplied protocol disposal and caller outputs, not automatic ownership redesign |
| B-03 | DOC-005; IMP-010 decision; WA-IMP-009 dependency |
| B-04 | DOC-006; IMP-006 decision; XREF-003 reader dependency |
| B-05 | DOC-005; IMP-010 decision; WA-IMP-009 dependency |
| B-06 | DOC-006; IMP-008 parser/model decision |
| B-07 | IMP-007 security-critical peer-key validation; DOC-004/016/017 dependencies |
| B-08 | DOC-004; IMP-004/005 and IMP-024 distinct named-buffer scopes; IMP-027/035 consumers, not duplicates |
| B-09 | DOC-005; IMP-010 persistent pcmr-only decision; IMP-043 dependency |
| B-10 | DOC-006/012; IMP-022 assertion-gap; executable diagnostics remain deferred with IMP-021 |
| B-11 | IMP-008/009 response shapes; IMP-022 vector/test-gap; DOC-012 |
| B-12 | DOC-012; IMP-020/021 shared setup/cleanup; no duplicate per test |
| B-13 | DOC-007; IMP-011; C-003/F-03/G-003 same UV-key exclusion |

### C — every candidate, including documentation-only cases

| Candidate | Stable dispositions / overlap |
| --- | --- |
| C-001 | DOC-008/016; XREF-001 = WA-XREF-001; IMP-026 representation decision; DEC-002 native selected-user privacy remains separate |
| C-002 | DOC-008/016; WA-IMP-010 final-ID recovery (no duplicate CTAP recovery count); IMP-028 contextual decoder decision |
| C-003 | DOC-007/016; IMP-011 UV-key bug and IMP-010 capability/protocol-policy overlap; IMP-022 invalid fixture qualification |
| C-004 | DOC-016; IMP-027; IMP-004 request/transport ownership dependencies; WA-IMP-006/013 retained |
| C-005 | DOC-016; parse/hash/flag inspection is not verification; no mandatory new RP verifier |
| C-006 | DOC-016; IMP-028; IMP-001 standalone-envelope dependency; IMP-007 remains distinct PIN peer security boundary |
| C-007 | DOC-016; algorithm/none-attestation terminology only, executable identifiers unchanged |
| C-008 | DOC-011/016; framework writer sorting resolved; IMP-017 rejected mechanism retained, no COSE wire-order bug |
| C-009 | DOC-016/017; IMP-029 profile/validation decision; DEC-003 original vendor proof dependency |
| C-010 | DOC-016; IMP-030 response/raw support decision; IMP-027 ownership dependency |
| C-011 | DOC-012/016; IMP-022 test-gap, not certification work mandated by names |
| C-012 | DOC-016/018; XREF-003; IMP-006 input-layer decision; no whole-Core audit |
| C-013 | DOC-001/002/003/014/016/018; IMP-003/010/019 existing boundaries; entry-point/RP/sequence explanation only, no mandated orchestration feature |
| C-014 | DOC-011/012/016; historical/spec attribution and exact before-command I/O correction only; IMP-021 deferred diagnostic dependencies |
| C-015 | DOC-012/016; IMP-020/021 shared setup and owned-test cleanup; no duplicate cleanup count |

### D — 17 candidates

| Candidate | Stable dispositions / overlap |
| --- | --- |
| D-01 | DOC-010; IMP-012 standard biometric wire mismatch |
| D-02 | DOC-010; IMP-013 biometric inventory schema/error |
| D-03 | DOC-010; IMP-014 required sample/result identity; IMP-019 admin counts overlap |
| D-04 | DOC-010; IMP-015 enum/vendor evidence; DEC-003 |
| D-05 | DOC-009; IMP-016 capability decision |
| D-06 | DOC-011; IMP-016 unused config constant provenance |
| D-07 | DOC-006/009; IMP-016 expressiveness decision |
| D-08 | DOC-011; IMP-017 **rejected** runtime ordering inference; actual writer auto-sorts |
| D-09 | DOC-009; IMP-018 disposed-helper decision |
| D-10 | DOC-009; IMP-019 decoder/sequence decisions |
| D-11 | DOC-009; IMP-019 hash/totals boundary; blob cleanup is caller guidance, not mandated automatic GC |
| D-12 | DOC-009; DEC-003 product/standard-preview qualification |
| D-13 | DOC-012; IMP-022 deletion/assertion-gap |
| D-14 | DOC-006/012; IMP-020/021/022 setup/recovery/test-gap; alleged wrong forced-change status is disputed/rejected for original CTAP2.1 legacy0x05 (see clarification below), no new ID |
| D-15 | DOC-010/012; IMP-022 sensor-only/fake-wire evidence |
| D-16 | DOC-009/012; IMP-021 test cleanup; IMP-024 shipping admin buffers |
| D-17 | DOC-011; integer2 policy-label correction only |

### E — every candidate, retaining WA engineering provenance

| Candidate | Stable dispositions / overlap |
| --- | --- |
| E-01 | DOC-013/017; **WA-IMP-012** authoritative enforcement finding, CTAP builder flag evidence added by reference; enforcement boundary decision, not new wire flag or duplicate WA count |
| E-02 | DOC-017; IMP-031 input-surface/capacity decision; WA-IMP-012 sensitive-credBlob enforcement dependency |
| E-03 | DOC-013/017; IMP-032 current decoder-shape bug; optional direct feature decision; IMP-030 unsigned output; WA-IMP-014–017 remain separate facade gaps |
| E-04 | DOC-013/017; WA-IMP-018 false handling retained; IMP-016 config dependency, no mandatory decoder range |
| E-05 | DOC-017; IMP-007/010/011 validation dependencies; WA-IMP-011 verified PRF retained; not another PIN/KDF bug |
| E-06 | DOC-017; IMP-033 accessor/ceremony decision and wrong-shape behavior; no mandatory MC accessor inferred |
| E-07 | DOC-017; IMP-034 valid-output loss; IMP-030/028 distinct envelope/structural boundaries |
| E-08 | DOC-017; IMP-027 extension-owned result copies; WA-IMP-013; borrowed inputs do not imply encoding makes no copies |
| E-09 | DOC-017; IMP-035 shipping storage/salt-helper cleanup; IMP-004/005/024 buffer dependencies, not repeated IDs per allocation |
| E-10 | DOC-017; IMP-036 sequence/limit/response decisions; WA-IMP-017 token-only support restriction is not mandatory new pinless feature |
| E-11 | DOC-017; IMP-037 uint/cap/preservation decisions |
| E-12 | DOC-017; DEC-003 publication/product evidence; IMP-029 experimental profile dependency; citations alone do not mandate implementation |
| E-13 | DOC-017; IMP-038 extraction/error/validation decision; WA-IMP-006 trust/borrowing dependency |
| E-14 | DOC-017; IMP-039 definite derived-algorithm propagation bug |
| E-15 | DOC-017; IMP-040 constructor/init/ceremony decision; WA-IMP-019 existing facade argument policy retained |
| E-16 | DOC-017; IMP-041 DER structural verification/test-gap, experimental only |
| E-17 | DOC-012/017; IMP-022 assertion/fixture gap; real PRF remains WA-IMP-011, no duplicate feature bug |
| E-18 | DOC-012/017; IMP-021 test cleanup/comparison overlap; IMP-042 distinct optional immutable key-export channel |
| E-19 | DOC-001/017; IMP-043 strict wrapper versus generic crypto helper decision; IMP-035 failure-output cleanup dependency; IMP-010 persistent token dependency |

### F — every candidate, including rejected subcandidate

| Candidate | Stable dispositions / overlap |
| --- | --- |
| F-01 | DOC-014/018; IMP-044 **demo enhancement**, not mandatory full RP application/API |
| F-02 | DOC-001/014/018; A-015 same non-overlapping lifetime correction; no new runtime finding |
| F-03 | DOC-002/005/007/018; IMP-011 UV-key/sample; IMP-010 capability policy; no automatic touch redesign |
| F-04 | DOC-004/013/017/018; stale APIs/padding/status/trees only, no API rename |
| F-05 | DOC-012/014/017/018; IMP-001/022 existing parser/test limits; Markdown/XML sketches can be corrected without executable test changes |
| F-06 | DOC-012/018; IMP-020 lane/default-normalization; retain module human-coordination policy, no new GetInfo approval gate |
| F-07 | DOC-018; IMP-045 sample help/parser/control-flow bug; adding every absent CLI feature is not required |
| F-08 | DOC-018; IMP-046 selection/alias bug and IMP-047 error/cancellation bug; IMP-010 sample permission/RP prerequisite dependency; prompt redesign is a decision, not authorized Spectre API adoption |
| F-09 | DOC-002/018; IMP-048 destructive reset target-binding bug; DEC-003 product timing and IMP-023 error provenance dependencies |
| F-10 | DOC-018; XREF-003 input exposure dependency; **rejected cloned-CM-token-leak subcandidate** (constructor borrows); IMP-018 remains separate disposed-helper issue |
| F-11 | DOC-014/015/018; exact active architecture/migration helper wording; preserve historical FIDO names/records |
| F-12 | DOC-001/014/017/018; DEC-003 qualified support; no full-feature/model guarantee |

### G — 11 bounded cross-layer candidates, not whole-WebAuthn coverage

| Candidate | Stable dispositions / overlap |
| --- | --- |
| G-001 | DOC-008/015; XREF-001 = WA-XREF-001 |
| G-002 | DOC-008/015; WA-IMP-010, no duplicate recovery count |
| G-003 | DOC-007/015; IMP-011 same CTAP UV-key/sample bug |
| G-004 | DOC-005/015; WA-IMP-009 and IMP-010 distinct layer policies |
| G-005 | DOC-013/015/017; WA-IMP-011/013; IMP-035 salt helper |
| G-006 | DOC-013/015/017; WA-IMP-012 shared enforcement boundary |
| G-007 | DOC-013/015/017; WA-IMP-013/014/015/016/017; IMP-032/036 route dependencies |
| G-008 | DOC-013/015/017; WA-IMP-018; report versus configure |
| G-009 | DOC-002/015; WA-IMP-002/003; no transport raced-success regression inferred |
| G-010 | DOC-001/015/018; DEC-003 qualified inherited requirements |
| G-011 | DOC-015; **DEC-002** native selected-response privacy/serializer actor decision, not new vulnerability or automatic field stripping |

## Supplemental deferred groups and precise reopening conditions

Paths are repository-relative. `S` means `src/Ctap2/src/`, `I` means
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/`, `U` means
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/`, `X` means
`src/Ctap2/examples/FidoTool/`. These roots plus each filename below are exact paths. The named
members/ranges narrow each finding; no row is a whole-file defect declaration. Every proposed action
is **desired-not-authorized**, all independent DOC re-reviews remain pending.

| ID / kind / provenance | Exact source-observed condition | Desired decision/action; dependencies; documentation re-review trigger |
| --- | --- | --- |
| CTAP-IMP-026 — decision; C-001 | S`Credentials/PublicKeyCredentialTypes.cs:300–337`: public user ctor requires display names/nonempty id, parser allows absent display names but rejects empty id; valid CTAP privacy/empty-account allowances differ from WebAuthn handle profile. | Decide request/response representation and empty-id policy; do not strip native fields automatically. Dependencies B/F/G, DEC-002, WA-XREF-001. Reopen ctor/Parse/Encode XML, GetAssertionResponse.User/GetUserHandle, both credential options and CTAP README account tasks; WA AuthenticationResponse.User/MatchedCredential/picker/RP-handoff if representation changes. |
| CTAP-IMP-027 — ownership design gap; C-004/E-08 | S`Credentials/MakeCredentialResponse.cs:170–223` borrows statement/unsigned slices; get/make/authenticator/attested parsers and S`Extensions/Shared/ExtensionOutput.cs` materialize independent arrays, including largeBlobKey/secret-derived outputs, without disposable result owner. | Choose coherent explicit owner/view/failure lifetime, not a non-disposable RawData clone or advice to zero only one view. Dependencies IMP-004/030/034, B/E/F/G and WA-IMP-006/013. Reopen both response Decode/Raw/outputs/key XML, AuthenticatorData/AttestedCredentialData, attestation slices, all byte-bearing extension DTO/getter ownership and both-layer result-cleanup examples after ownership changes. |
| CTAP-IMP-028 — bugs plus decoder decisions; C-002/006 | S`Credentials/AuthenticatorData.cs:149–173` accepts ED remainder without one-map check, ignores trailing bytes without ED; `AttestedCredentialData.cs:122–127` skips any CBOR value; Make response lacks AT check; Get count uint→int can overflow and key length/conditional fields lack checks. Standalone decoders lack request/version context. | Set strict structural/full-envelope boundaries versus context-aware conditional validation and open COSE identifiers; nested readers must not be indiscriminately forced to exhaust parent input. Dependencies IMP-001/007/030, framework trailing-map evidence, A/E/F/G, WA-IMP-010 remains separate recovery. Reopen all named parser XML/errors, response field/default tables, flag/raw/extensions/COSE docs and parser fixtures after selected validation/range changes. No SDK decoder executed here. |
| CTAP-IMP-029 — experimental compatibility/security decision; C-009/E-12 | S`Cose/CoseArkgP256SeedKey.cs:95–167` emits wrapped maps with kty2/alg-65700, accepts outer99 and padded coordinates without nested curve=1 check; expired draft-10 specifies different outer/direct-map profile. | Choose pinned vendor profile vs draft target and security-critical key checks; do not treat sibling agreement as authoritative or authorize production use. Dependencies DEC-003, Core delegated ARKG, E/G. Reopen CoseAlgorithm seed/signing identifiers, seed Encode/Decode/reconstruction XML, CoseKey dispatch, ARKG fixtures, PreviewSignCbor/GeneratedKey/DerivedKey and experimental support claims after profile/validation changes. |
| CTAP-IMP-030 — support/raw-preservation decision; C-010 | S`Credentials/MakeCredentialResponse.cs` models unsigned key6 but drops unknown outer fields; S`Credentials/GetAssertionResponse.cs` drops S23 key8 and has no complete raw envelope. | Decide unsigned-output/raw escape hatch with IMP-027 ownership, not mandatory support for every optional extension. Dependencies E/G, IMP-032/034. Reopen both response field tables/Decode/unsigned/raw guarantees, module forward-compatibility doctrine, extension raw codecs and WebAuthn boundary after support changes. Introduction-history claim remains unverified. |
| CTAP-IMP-031 — input-policy decision; E-02 | S`Extensions/CredBlob/CredBlobInput.cs:47–58` enforces1–32 while `Extensions/Shared/ExtensionBuilder.cs:78–82,319–322` accepts empty/oversize; maxCredBlobLength is authenticator-specific (>=32 capacity). | Reconcile DTO/raw-builder responsibilities, empty semantics and capability/oversize handling. Dependencies WA-IMP-012 enforcement for sensitive blobs, C/F/G. Reopen CredBlobInput ctor/Blob/Encode, WithCredBlob/GetCredBlob, output absence/empty, builder fixture limits and sensitive-content guide examples after policy changes. No universal32-byte protocol maximum. |
| CTAP-IMP-032 — actual convenience-shape bug; optional feature decision; E-03 | S`Extensions/LargeBlob/LargeBlobOutput.cs:19–47` and `Extensions/Shared/ExtensionOutput.cs:158–205` conflate signed synthetic key/written output, top-level key5/7 route and unsigned direct largeBlob output; registration boolean becomes Written. | Decide remove/rework legacy convenience shape or separately support direct compressed/originalSize output. Direct feature is not mandatory. Dependencies IMP-030, C/F/G, WA-IMP-014–017 distinct facade findings. Reopen ExtensionIdentifiers, WithLargeBlobKey, TryGetLargeBlob/Key, LargeBlobOutput and storage/response route explanations and synthetic fixture labels after selected decoder/output relocation. |
| CTAP-IMP-033 — accessor contract decision / wrong-shape behavior; E-06 | S`Extensions/Shared/ExtensionOutput.cs:136–150` byte-string accessor throws on registration hmac boolean; no MC-key accessor; direct MC bstr fixture is not keyed routing proof. | Decide ceremony-specific accessor/validation API; no mandatory MC feature addition inferred. Dependencies B crypto lengths, G PRF. Reopen TryGetHmacSecret/HmacSecretOutput.Decode, registration/MC manual raw examples and keyed fixture commentary after contract/accessor changes. |
| CTAP-IMP-034 — actual valid-output loss; E-07 | S`Extensions/Shared/ExtensionOutput.cs:253–347` reader overload retains names with empty values; DecodeWithRawData re-encodes, skips tags/floats/simple values and reads uint via signed Int64; not byte-exact preservation. | Preserve encoded values appropriately and decide duplicates/malformed handling without rejecting valid unknowns. Dependencies IMP-027/030, C/G raw maps. Reopen entire ExtensionOutput Decode/Raw/Try contract, response.Extensions example, module raw escape-hatch and WebAuthn forward-compatibility claims after slice/decoder changes; tests should distinguish preserved encoding from reconstructed meaning. |
| CTAP-IMP-035 — shipping cleanup gaps; E-09/19/G-005 | S`LargeBlobs/LargeBlobStorage.cs:225,268,397–448` discards probe plaintext and retains auth/payload/request copies; `LargeBlobData.cs:274–280` does not explicitly clear MemoryStream backing storage; `Extensions/Shared/ExtensionBuilder.cs:183–208` lacks saltData/failure finally; metadata failure outputs need ownership review. | Clear SDK-owned sensitive buffers on success/failure within chosen ownership. Dependencies IMP-004/005/024/027, WA-IMP-013, B/F/G. Reopen storage/entry Encrypt/TryDecrypt/FindAndDecrypt/Get/Set/Delete lifetime/temporaries, helper BuildHmacSecretInput cleanup, metadata decrypt ownership/errors and both-layer blob/PRF cleanup examples after changes. No provider-wide erasure proof. |
| CTAP-IMP-036 — state/limit/response decisions; E-10 | S`LargeBlobs/LargeBlobStorage.cs` caches limits, sequentially reads/writes shared array without whole-flow lock/conflict detection; malformed/missing response bytes become initial array (`453–475`), maxMsgSize<=64/oversized or nonterminating fragments unguarded. | Separate required response validity/termination robustness from caller coordination and optional lock/recovery/pinless policy. Dependencies A raw guard, IMP-003/019, WA-IMP-017. Reopen ILargeBlobStorage and storage class/Get/Write/Set/Delete, per-fragment70-byte/LE-offset auth comment, F shared-array/failure/re-read guidance and G output-error/authorization boundaries after selected changes. No rollback/resume/atomicity promise. |
| CTAP-IMP-037 — uint handling bug / cap and preservation decisions; E-11 | S`LargeBlobs/LargeBlobData.cs` uses signed long for protocol uint/unknown integer keys, reconstructs modeled entries; TryDecrypt origSize>1MiB returns null while Encrypt accepts larger plaintext. | Decide full uint/preserved-slice behavior and explicit read/write-cap policy; no all-client interoperability claim. Dependencies IMP-035/036, C/G. Reopen LargeBlobEntry/Array ctor/Deserialize/TryDecode/TryDecrypt/Encrypt borrowing, exceptions, unknown preservation and cap guidance plus SpecFormatTests foreign-entry/seam comments after format/cap changes. |
| CTAP-IMP-038 — extraction/exception decision; E-13 | S`Extensions/PreviewSign/PreviewSignCbor.cs:221–229,TryExtractGeneratedKey` treats missing outer flags as beta exception although flags belong inside inner attestation; extraction discards trust/policy checks and catches only selected parse exceptions. | Decide extraction false-vs-throw/structural contract; adding trust verifier is not authorized or mandated. Dependencies C attestation/authData, WA-IMP-006, G. Reopen RegistrationOutput, DecodeRegistrationOutput/TryExtractGeneratedKey, GeneratedKey and preview test inner-flags/attestation explanations after extraction/validation changes; retain experimental warning. |
| CTAP-IMP-039 — actual algorithm propagation bug; E-14 | S`Extensions/PreviewSign/PreviewSignRegistrationOutput.cs:91–95` passes seed Algorithm(-65700) as DerivedKeyAlgorithm; PreviewSignCbor extraction passes actual derived alg; verifier always P256/SHA256. | Correct propagation and settle supported dispatch with meaningful both-accessor assertions. Dependencies IMP-029/041, C/G. Reopen both generated-key accessor XML, GeneratedKey.DerivedKeyAlgorithm/DerivePublicKey, DerivedKey.VerifySignature, seed/derived/signing identifiers and G generated-output mapping after change. |
| CTAP-IMP-040 — construction/operation policy decision; E-15 | S`Extensions/PreviewSign/PreviewSignSigningParams.cs` rejects empty handle/TBS despite draft allowances, init can bypass guards; additionalArgs passthrough accepts non-map bytes; ExtensionBuilder chooses registration when both modes set; ARKG args checks length not point. | Decide consistent ctor/init/operation/schema rules and ceremony boundary; do not impose generic curve validation on all opaque arguments. Dependencies IMP-029, C allowList, WA-IMP-019. Reopen SigningParams/RegistrationInput/WithPreviewSign, CoseSignArgs/ArkgP256SignArgs/PreviewSignCbor byte/null/empty/selected-credential guidance after chosen guards/encoding changes. |
| CTAP-IMP-041 — experimental verifier structural/test gap; E-16 | S`Extensions/PreviewSign/PreviewSignDerivedKey.cs:151–163` reads two ASN.1 INTEGERs without explicit root/sequence exhaustion or positivity/bounds checks; VerifySignature hashes raw message, not prehashed TBS. No malformed/trailing DER unit evidence. | Review strict DER/algorithm contract and downstream provider rejection before claiming actual acceptance/security impact; meaningful positive/negative evidence required. Dependencies IMP-039, G. Reopen VerifySignature/converter, GeneratedKey cross-reference and preview integration raw-vs-digest comments after verifier/algorithm changes. No exploit, hardware or production qualification. |
| CTAP-IMP-042 — test evidence-channel risk; E-18 | I`AuditV2/Ctap2LargeBlobAuditReproTests.cs:102–106` optionally exports largeBlobKey as immutable hex file while nearby guidance says never persist keys. No export executed. | Remove/redesign secret export only under later authorization; human-controlled interop evidence must not use an unqualified safe/no-persistence label. Test cleanup/comparisons stay IMP-021, not new counts per fixture. Reopen interop-test prerequisites/export/read-from-environment comments and F security/test guidance after evidence-channel change. |
| CTAP-IMP-043 — strict-wrapper versus generic-helper decision; E-19 | S`Crypto/EncryptedMetadataDecryptor.cs` accepts IV-only/arbitrary block ciphertext and any nonempty PPUAT; standard GetInfo fields IV16+ct16. CBC can return wrong-token bytes, not authenticate them. Generic tests use longer plaintext/token. | Decide strict protocol wrapper vs documented generic crypto helper; preserve wrong-token/unauthenticated distinction, do not invent credential identity interpretation. Dependencies B IMP-010, A EncIdentifier/EncCredStoreState, IMP-035, F/G. Reopen all decrypt/DeriveKey XML, metadata fixtures' Java/protocol labels, two-session/freshness claims, persistent-token/capability and GetInfo field docs after length/token/failure changes. |
| CTAP-IMP-044 — optional demo enhancement; F-01 | X`FidoExamples/MakeCredential.cs:73–125` random hash/user and `GetAssertion.cs:69–103` random hash/first-only result do not supply verifiable RP context or all assertions. | If selected, accept real ceremony/account inputs and expose result context/cardinality; not mandatory SDK RP/server feature. Dependencies C/A/G. Reopen X CredentialMenu/MakeCredential/GetAssertion XML, CTAP README create/get/getNext, package login claims and RP handoff after sample result/input changes. |
| CTAP-IMP-045 — sample help/parser bug; F-07 | X`Program.cs:70–80,211–219,682–692` help omits set-pin/new-pin; unknown subcommands can exit0; help selects device first; option value can be next flag. | Correct chosen help/parser/exit behavior separately from adding unsupported CLI features. Dependencies shared CommandHelper/F-08. Reopen X README supported commands/Quick Start/working directory and Program/help explanation after executable changes; compilation is not device execution. |
| CTAP-IMP-046 — sample selection/identity bug and policy decision; F-08 | X`Cli/Prompts/DeviceSelector.cs:63–105,156–177` filters interface not FIDO AID, auto-selects noninteractive candidate, still prompts retry, uses duplicate-label IndexOf; inactive FidoDeviceSelector is not active routing. | Fix identity aliasing and decide qualification/noninteractive selection policy; do not imply every SmartCard candidate is CTAP. Dependencies A/F-09, shared selector. Reopen selector XML and X README Device Selection plus menu/capability guidance after correlation/filter/selection changes. |
| CTAP-IMP-047 — sample error/cancellation bug and prompt-policy decision; F-08 | X DeviceSelector error catch/debug, FidoExamples broad Exception→failed results and admin IsSupported catches erase cancellation/read failure as unsupported; sync prompts cannot promise Ctrl+C termination; shared `src/Cli.Shared/src/Cli/CommandHelper.cs:29–38` registration persists. | Preserve error/cancellation provenance and decide prompt cancellation/event lifetime; no unverified library API migration authorized. Dependencies A/G error boundary, IMP-045/046. Reopen all FidoExamples helper result/cancel XML, menu prompt comments, X README Global Options/error/support/security and shared CommandHelper guidance after changes. |
| CTAP-IMP-048 — sample destructive target-binding bug; F-09 | X`Cli/Menus/ResetMenu.cs:35–47,91–109` and `Program.cs:135–180` select independently after reinsert, retain old transport, no key correlation; preflight catch substitutes defaults; force bypasses confirmation/reinsert instructions. | Bind destructive action to confirmed physical key and reconcile failed preflight/force behavior. Dependencies IMP-046/047, A reset/transport, DEC-003 product windows. Reopen ResetMenu/ResetAuthenticator/Program comments and X README reset/force/CI/security guidance after identity/preflight flow changes; human timing/presence authorization unchanged. |

## Narrow cross-reference and retained decisions

- **F per-path observations retained with numbered groups:** `FidoExamples/PinManagement.cs:155–158`
  requests GetAssertion permission without rpId; this is existing **IMP-010** authorization/RP policy
  evidence, not a new PIN-domain bug. F's `FidoExamples/BioEnrollment.cs` cancellation path may skip
  enrollment-cancel on thrown cancellation: review under **IMP-047** error/cancellation outcome and
  D's explicit-command distinction, without inventing automatic rollback. `Cli/Menus/FingerprintsMenu.cs`
  displays unknown sensor kind as Swipe: retain as a sample **UI labeling decision** with IMP-045's
  help/display correction scope, not an unsupported-sensor feature mandate. Reopen these exact helper/
  menu explanations and F README support/cancellation labels after any selected executable change.
- **D-14 forced-change status subclaim:** fresh [review-b.md](review-b.md) RB-O1 records
  original CTAP2.1 §6.5.5.7.1 legacy getPinToken **0x05 → PIN_INVALID** for forcePINChange,
  versus permission-bearing §6.5.5.7.2 **0x09 → PIN_POLICY_VIOLATION**. D's test calls legacy
  GetPinTokenAsync and expects PinInvalid, so its assertion is **not a confirmed wrong-status bug**.
  Preserve the original D report/main-ledger claim as disputed/rejected provenance pending D editor's
  verification/correction; do not mint another ID. D-14's real setup/state-cleanup/assertion limits
  remain IMP-020/021/022. B **RB-R1** actor/prerequisite precision is DOC-005/B-05/IMP-010,
  not new engineering work; RB-O1 force context is optional. Corrections/re-review are ongoing,
  not final B or D acceptance.
- **CTAP-XREF-003 (proposed, no separate Core bug count):** C-012/B-04/F-10;
  `src/Ctap2/src/Credentials/Ctap2CredentialOptions.cs:25–33` configures length4–63, while
  `src/Core/src/Credentials/ConsoleCredentialReader.cs:184–205,210–279` counts UTF-16 characters
  before UTF-8 conversion (confirm exact Core path through coverage record). The reader dependency
  is partial, not a full Core audit. Map enforcement allocation to existing **IMP-006** and later
  Core owner. Reopen preset, Core credential length-property XML, PIN prompts/ClientPin byte checks,
  WebAuthn prompt policy and FidoPinHelper CLI-string/byte-lifetime guidance if shared input changes.
- **CTAP-XREF-001 / WA-XREF-001** and **CTAP-XREF-002** remain unchanged. All existing WA-IMP
  counts are preserved; references here do not close, renumber or duplicate them. In particular
  WA-IMP-006/009/010/011/012/013/014–019 stay at their original layer/ownership boundaries.
- **DEC-002** remains a native-result/browser-equivalent/RP-serializer actor decision, not an
  automatic new privacy vulnerability. **DEC-001** is resolved into IMP-025 by pinned Guid source
  and little-endian probe, **not big-endian execution**. **IMP-017** is rejected for runtime order;
  comments alone remain wrong. Framework ReadEndMap evidence supports narrow standalone-envelope
  checks, not universal execution/validity of every decoder.
- **DEC-003** retains vendor bio fields/standard-vs-preview minima, reset5.5.2/5.5.3 product gap,
  MC/PPUAT/encrypted-field earliest support, ARKG vendor profile and preview beta0x7F/ECDH provenance.
  BE/BS later original citation and unsigned-output introduction history remain documentation evidence
  gaps, not automatically implementation bugs. F's four project SHA differences were verified as
  baseline Git-blob LF vs checkout CRLF with empty source diff; no implementation mutation inferred.

## Pending closeout schema

Coordinator review of this addendum: **accepted, subject to the later evidence corrections below**. Each supplemental ID requires selected owner,
implementation disposition, independent documentation review and precise post-change re-review.
`coverage.json` defines null final hash/edit/reviewer/validation fields and unsampled clean units.
Historical investigations/evidence remain immutable; current editor worktrees do not establish
integration or source acceptance. Completion cannot be inferred from this records reconciliation.

## Later coordinator evidence corrections and acceptance

- B/D follow-up reviews confirm that D-14's wrong legacy force-change status inference is rejected,
  not still disputed: 0x05 returns PIN_INVALID and 0x09 permission issuance returns PIN_POLICY_VIOLATION
  under the cited CTAP2.1 algorithms. Preserve other lifecycle/test gaps and original reports.
- C follow-up confirms draft-10 explicitly supplies seed placeholder -65700. IMP-029 still concerns
  actual kty/wrapping/dkalg/curve/profile differences; algorithm-marker disagreement is withdrawn.
  The pinned WebAuthn L3 Recommendation (2026-08-25) now supplies verified BE/BS evidence, resolving
  that citation gap. Unsigned-output first-introduction history remains unclaimed.
- E-core correction scopes IMP-037 to signed-range origSize/integer keys; unknown encoded values can
  contain larger unsigned values. Those retained slices borrow input memory. Do not copy ExtensionOutput's
  distinct reconstructive Int64 limitation into this parser. Its cap fixture also changes AAD, so that
  one assertion does not isolate a cap branch; preserve the test-gap under IMP-022/037.
- Late COORD-001 expands IMP-001: parsed absent EncIdentifier/EncCredStoreState become present empty
  nullable memory, unlike optional numeric/Boolean/text fields. A/F documentation is corrected; code
  remains unchanged. This is a new source observation, not an additional implementation-group count.
- IMP-046 also includes FidoTool bio capability probing that treats bioEnroll=false as unsupported,
  although it can mean supported with no enrollments. Reopen BioEnrollment.IsSupported/FingerprintsMenu
  and task/support guidance when the sample's qualification logic is fixed; no extra duplicate ID.
- Source review/progression and sampling updates are recorded in status.md/sampling.md/coverage.json.
  Investigation-era pending statements above are historical checkpoints, not current review verdicts.
