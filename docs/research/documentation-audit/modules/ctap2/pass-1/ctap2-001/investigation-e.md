# Investigator E — extensions, storage and cryptographic conveniences

**Status:** investigation complete; candidates awaiting coordinator triage/fresh editing and independent review. **Date:** 2026-11-03. **Baseline:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`. Investigator: GPT-6.1 Sol. All **57 owned paths** were read completely, including implementation, XML, internal comments, fixtures and assertions; all 57 current SHA-256 hashes match `inventory.json`. This is static inspection, not a passed test suite or hardware qualification.

Read root/module/tests CLAUDE, module README, canonical audit workflow/style and SDK house style; loaded security-guidelines for ownership classification and verify for final record checks. Available skills were reviewed. Build/test/commit skills were not applicable: **no builds, tests, hardware operations, commits, delegation or source edits occurred**. Only this report and `evidence/e/primary-excerpts.md` were written, using patch. No executable sample or test assertion was changed. An HTTP extraction attempt requiring unavailable bs4 failed; subsequent read-only original HTML extraction succeeded. This is tooling/research history, not an SDK experiment.

Evidence shorthand **S21/S22/S23/P4/V58/A10/L** refers to [dated primary excerpts](evidence/e/primary-excerpts.md), containing exact revision/status/URLs, actors and strengths. Findings are local **E-*** candidates, not final CTAP-DOC/IMP IDs. DOC and IMP dispositions are separate even where one finding groups the same underlying contract. Severity is prioritization, not a claim of exploitability.

## Findings and exact documentation-impact map

### E-01 — credProtect enforcement flag is inert (high; DOC + deferred IMP)

**Observed:** `ExtensionBuilder.cs:41–65` saves `_credProtectEnforcePolicy`, but `Encode:330–334` emits only policy and no code subsequently reads the flag. XML `:57` promises failure if policy cannot be honored. Builder has neither session capability nor response-enforcement context; shared request encoder takes only encoded bytes. S21 §12.1.1 makes enforcement a **platform SHOULD NOT** creation rule for levels 2/3, not another CTAP field. Actual returned policy can be stronger than requested.

**Desired:** Pass 1 state that builder requests the policy and the SDK currently does not implement `enforcePolicy`; caller must assess support and returned policy, with preflight needed if failure must prevent creation. Pass 2 decide a real enforcement boundary rather than inventing a wire bit or claiming post-response rejection undoes creation. Add regression evidence for unsupported/absent/downgraded policy. Do not silently remove the unfinished behavior.

**Impact/re-review:** builder WithCredProtect XML/example; `CredProtectPolicy`; F's README PIN and extensions examples/CLAUDE table; C's MakeCredential request/options/response and A's session; G's WebAuthn credProtect enforcement boundary. Reopen when policy enforcement is implemented or contract chosen. Report to **A/C/F/G**; do not duplicate their domain findings.

### E-02 — credBlob fixed limits and sensitive-content prerequisites (medium DOC + deferred IMP)

**Observed:** `CredBlobInput.cs:25,47–58` says “up to 32 bytes minimum”/“Must be between 1 and 32 bytes per CTAP2.1” and enforces that fixed range. Builder `:78–82,319–322` allows any length, including zero; owned builder tests assert empty and 128-byte emission, while `ExtensionBuilderTests:172` calls 64 the “Max spec limit.” S21/S23 require **at least 32 capacity**, expose maxCredBlobLength and describe empty retrieval. Neither path documents the RP MUST use level-3 credProtect + enforcement for PII/sensitive blob.

**Desired:** Explain authenticator-specific capacity, DTO's current SDK-only 1–32 restriction and builder's lack of size/capability validation; do not call that restriction a protocol limit. Store byte string vs retrieve boolean remains correct. PII/account-identifier examples need adjacent protection qualification, especially with E-01. Pass 2 reconcile inconsistent input surfaces and appropriate platform handling of oversize, with actual boundary/empty tests.

**Impact:** `CredBlobInput` full remarks/Blob/Encode; builder WithCredBlob/WithGetCredBlob; outputs' absence/empty explanation; builder test comment; F's extension example; C/G's request/validation boundary. Trigger: new size/capability policy. Empty blob is not the same as absent extension.

### E-03 — largeBlobKey, direct largeBlob and WebAuthn output are conflated (high DOC + deferred IMP)

**Observed:** `ExtensionIdentifiers:61–78` calls `largeBlob` support-only with separate storage; `LargeBlobOutput:19–47` invents key-bearing direct output and says CTAP only returns a key. `ExtensionOutput:158–205` reads these shapes from a signed map, maps registration boolean to **Written**, and seeks a key there. Builder's `WithLargeBlobKey:108–112` correctly distinguishes alternatives, but incorrectly says CTAP “carries no largeBlob:{support}” without qualifying **this implemented route**. S23 §12.4 explicitly defines that input on the direct route and uses unsigned output with compressed blob/originalSize. §12.3 keys instead arrive at response envelope 5/7; no signed extension output. The owned fixture `LargeBlobOutputTests` checks a synthetic `{largeBlobKey,written}` map, not spec output.

**Desired:** Document what each existing decoder actually consumes, and identify incomplete/nonstandard convenience shape rather than asserting it is CTAP output. Guide callers to `MakeCredentialResponse.LargeBlobKey`/`GetAssertionResponse.LargeBlobKey` and LargeBlobStorage for the implemented route. Direct §12.4 is not implemented by builder/storage. No universal “no YubiKey supports it” rationale: retrieved V58 evidence cannot establish that across all current devices. Pass 2 decide whether to remove/rework legacy DTO/decoder or implement correct unsigned direct path and originalSize handling.

**Impact:** identifiers, LargeBlobOutput, ExtensionOutput TryGetLargeBlob/Key, WithLargeBlobKey XML, storage intro, LargeBlob test intro/trait explanation; **C** envelope docs, **F** CLAUDE largeBlob table, **G** WebAuthn largeBlob transformations. Reopen all on direct-path implementation/output relocation. Existing API shape cannot be redesigned in Pass 1.

### E-04 — minPinLength is disclosure, not setting or complete complexity evidence (medium DOC)

**Observed:** `MinPinLengthInput:24–30` implies adequate complexity; output `:32` presents four/default and 63/max without distinguishing current minimum, provisioned defaults and UTF-8 limit. Builder WithMinPinLength gives no ceremony/RP authorization prerequisites. S21 §12.4 and S22/S23 §12.5 report current minimum **code points**, only at creation; unauthorized RP gets **no output**, not zero or a protocol failure. Source decoders accept integer and do not enforce the prose range.

**Desired:** Name authorized RP list (`minPinLengthRPIDs`, not invented wire `minPinLengthRPIDList`), disclosure and absent output; link D-owned config for changing policy. Reporting minimum is not modifying it, verifying current PIN length or attesting all complexity rules. F-owned table “Require minimum PIN length” must change. No protocol-derived mandatory 4–63 decoder range inferred from tests.

**Impact:** both DTO remarks/properties, WithMinPinLength, identifiers; F table; D set-min/config; G adapter claims. Re-review on disclosure/config policy change.

### E-05 — hmac-secret origin, UV mode and protocol-dependent lengths (medium DOC)

**Observed:** `HmacSecretInput:30` calls secret derived from biometrics/PIN. SaltEnc `:55–61` gives 32/64 then separately mentions IV. Output `:37–42` gives vague “32+”/“64+”. S23 §12.7: random credential secrets, **different WithUV/WithoutUV branches**; salts one/two ×32; protocol 1 ciphertext 32/64, protocol 2 IV-prefix ciphertext 48/80 (verify protocol behavior with B, not repeat its audit). SaltAuth 16/32 is already accurate. HmacSecret DTO Encode does not validate advertised capability, raw lengths or key-agreement coherence.

**Desired:** Explain credential-scoped random secret, salt and actual UV-mode selection; repeatability requires same credential/salt/**UV mode**, not same PIN or biometric. Use exact conditional lengths and caller borrowing/cleanup. Extension cryptographic salt authentication is distinct from ceremony PinUvAuthParam over clientDataHash. hmac-secret requires UP (`up:false` not supported); builder is not enforcement/negotiation. MC evaluation also requires ordinary hmac-secret true, already emitted correctly.

**Impact:** HmacSecretInput/Output, builder helper overload XML, identifiers; B protocol helper contracts; F example; C authorization/options; G PRF hashing/decryption/UV branch. Pass 2 validation boundaries go to B/C/G; do not duplicate COSE/key-agreement domain audit.

### E-06 — hmac output accessor only supports assertion byte-string shape (medium DOC + deferred IMP)

**Observed:** `ExtensionOutput.TryGetHmacSecret:136–150` is unqualified but always invokes a byte-string decoder. Registration `hmac-secret:true/false` causes a CBOR exception, not a supported-state result. MC output is separately keyed hmac-secret-mc; no typed accessor exists, though raw retrieval + HmacSecretOutput.Decode can consume it. Unit `HmacSecretMcOutput_DecodesCorrectly` feeds a byte string directly and establishes no MC key routing.

**Desired:** State assertion/encrypted-output-only accessor and manual raw route for registration boolean/MC. Pass 2 add ceremony-appropriate output API/validation if intended; test actual keyed registration/MC envelopes and malformed lengths. Do not claim “true if present” means malformed or wrong-shape input never throws.

**Impact:** TryGetHmacSecret/DTO Decode XML, output example, unit comments; G registration PRF support handling. Trigger: new typed output accessors.

### E-07 — “raw preservation” is re-encoding with unknown-value loss (high DOC + deferred IMP)

**Observed:** `ExtensionOutput.DecodeWithRawData:284–295` decodes/re-encodes each value; `CopyCborValue:302–347` skips unsupported states (tags/floats/simple values) without writing them, and routes unsigned integers through signed ReadInt64. It cannot preserve all valid CBOR or byte-exact original encoding. Duplicate keys overwrite in dictionary under Lax. Reader overload `:253–269` intentionally discards values and stores **empty**, so typed accessors are not usable. This is a real unfinished decoding boundary, not a reason to delete raw/unknown guarantees elsewhere. `TryGetCredProtect` can return false for present out-of-range enum; Try methods can throw for malformed/wrong shape.

**Desired:** Qualify current reconstruction/limited value types and reader-overload identifiers-only behavior. Pass 2 preserve actual encoded slices (L ReadEncodedValue), decide strict malformed/duplicate/security behavior while retaining valid unknown outputs; test tags, floats, large uint, nonminimal encoding, malformed and duplicate maps. No experiment was run.

**Impact:** ExtensionOutput entire Decode/Raw/Try contract and misleading response.Extensions example; F module raw escape-hatch paragraph; C authData raw map; G forward compatibility. **Do not weaken C/G raw guarantees to conceal this defect**; map distinct raw-envelope versus convenience-decoder capabilities.

### E-08 — extension copies have no sensitive owner/disposal boundary (high DOC + deferred IMP)

**Observed:** ExtensionOutput owns newly encoded arrays and byte-string getters create further arrays as ROM; no IDisposable/zeroing. LargeBlobOutput can carry a secret key and plaintext; credBlob may be sensitive; hmac output is secret-derived encrypted material. Reader-based decoders do not borrow views for ReadByteString results. Raw convenience “read-only” is not immutable storage or a cleanup guarantee. `ArkgP256SignArgs:30–35` says encoder “never copies,” but `PreviewSignCbor:137–143` writes copied encoded output. Borrowing the **input** does not imply no copies in encoding.

**Desired:** Explicitly distinguish borrowed inputs, encoded copies, decoder-owned copies and parent raw response views; callers clear arrays they own, but cannot rely on response/session disposal to clear independent convenience copies. Do not promise actionable zeroing of privately owned ROM without API ownership. Pass 2 design disposable owner/view lifecycle coherent with C/G. ARKG public keys/nonce/ordinary info need not be called secrets; content/context/TBS can be application-sensitive.

**Impact:** all byte-bearing DTO properties/constructors/encoders, ExtensionOutput raw/getters, builder Build/Encode, PreviewSign inputs/outputs, F ownership guidance; **C/G** response ownership. Trigger: any owner/view/disposal redesign.

### E-09 — storage borrowing, plaintext responsibility and internal cleanup gaps (high DOC + deferred IMP)

**Observed:** `LargeBlobStorage:58–89` retains borrowed session/protocol/token with no disposal; caller must keep them usable through operations. `GetBlobAsync:188–189` returns fresh plaintext. Set/Delete `:225,268` discard successful TryDecrypt plaintext used as membership probe without clearing. `LargeBlobEntry.Deflate:274–280` clears the returned compressed array later, but MemoryStream backing buffer is not explicitly cleared by that code. `BuildHmacSecretInput:183–208` has no finally for saltData (nor helper-owned intermediates on failure). WriteFragment `:397–448` leaves auth param/encoded payload/request copies without finally clearing. These are separate buffers, not all raw key exposure; classify auth/secret-derived material semantically.

**Desired:** Pass 1 document borrowed token/session/protocol lifetime, caller-owned returned plaintext clearing (including errors), and no end-to-end cleanup guarantee. Remove broad “all temporaries” assertions. Pass 2 clear SDK-owned sensitive probe/plain/compressed/encoded buffers on success/failure; include failing compression/encrypt/auth/transmit paths. No production crypto fix authorized.

**Impact:** storage constructors/Get/Set/Delete, ILargeBlobStorage, entry Encrypt/TryDecrypt/FindAndDecrypt; builder helper; F storage/PIN snippets; G per-credential blob result lifecycle. Trigger: buffer-owner or probe algorithm change.

### E-10 — storage state sequence, SDK authorization policy and failure boundary (high DOC + deferred IMP assessment)

**Observed:** storage first loads GetInfo limits; reads full fragments until short/empty; writes sequential set chunks with total length only at offset 0 and per-fragment auth. Intermediate success does not commit full array. Set/Delete read-modify-write complete shared array with no sequence-wide lock or conflict detection; two helpers/clients can lose updates/interleave. All cancellation tokens forward to session calls; no explicit rollback/resume, state reset or completion marker. Cached limits are instance-local, not refreshed automatically. Read-only constructor's Delete(no match) can return false; Set can read/encrypt before Write rejects absent protocol.

**Desired:** Explain capability detection, limits, caller coordination across shared array/session, read-modify-write and failure/cancellation needing re-read rather than assuming rollback. Storage always requires configured protocol for actual writes, **stricter SDK policy** than S23's conditional authenticator UV rule. Permission is declared, not locally inspected in opaque token. Do not state ordinary reads obtain plaintext without credential key/authorization. Pass 2 decide sequencing/limits robustness: maxMsgSize ≤64 or oversized/nonterminating fragments not guarded; missing required config/empty response currently becomes initial array (`ParseReadResponse:453–475`) rather than strict malformed error. Desired stricter response handling needs separate regression evidence.

**Impact:** storage/ILargeBlobStorage class and methods, private fragment auth comment `:390` (wrong “32-byte hash” description; actual message is 70 bytes and offset LE), F task narrative, A raw cancellation sequencing, G blob failure semantics. Trigger: locking, recovery, limit refresh, malformed-response or authorization-policy change. S23 allows torn writes; do not invent transaction guarantees.

### E-11 — foreign-entry preservation and exception claims exceed modeled scope (medium DOC + deferred IMP assessment)

**Observed:** LargeBlobEntry `:38–40` says unchanged foreign entry interoperates universally; only unknown key/value bytes are retained while known fields/maps are reconstructed. LargeBlobArray drops nonconforming entries, carries caller-supplied Entries list without copying and constructor retains ciphertext/nonce ROM. TryDecode uses signed long for protocol uint and ReadInt64 for unknown integer keys; a valid >Int64 uint can fail. Strict Deserialize documentation promises ArgumentException for malformed CBOR but CborContentException originates from reader. `TryDecrypt` rejects origSize >1MiB as null before decryption; Encrypt accepts larger plaintext, so SDK can write a blob its read helper will not return.

**Desired:** Preserve useful format/decompression/AAD rationale and explicitly label 1MiB SDK read cap (SHOULD-derived choice), returned plaintext ownership, constructor borrowing and actual exception families. Limit preservation claim to modeled conforming entries/retained unknowns, not universal byte-exact envelope guarantee. Pass 2 resolve >cap write/read asymmetry, full unsigned range and canonical unknown rewriting. S23 unknown fields MAY is not an assurance arbitrary unknown CBOR is handled. No claim of all-client interoperability from local fixture.

**Impact:** LargeBlobEntry/Array remarks/constructor/Deserialize, storage writer, SpecFormatTests foreign-entry comment; C/G forward compatibility. Trigger: encoded-slice/full-uint or cap policy change.

### E-12 — specification labels, status and firmware claims require precise bounds (high DOC)

**Observed:** `ExtensionIdentifiers` cites S21 for all identifiers including hmac-secret-mc/previewSign and calls previewSign “CTAP v4.” PreviewSignRegistrationInput cites absent `Plans/cnh-authenticator-rs-previewsign-parity.md`; CoseSignArgs/PreviewSignCbor/test comments repeat CTAP v4. Builder/test comments cite “CTAP 2.1–2.3 §12.7” although S21 hmac-secret is §12.5. ArkgP256 says “Conforms to ... -10” while dependency Core intro says -09; public generated/derived helpers carry experimental warnings but generic previewSign input/encoder lacks clear draft status. F table says MC 5.4+, builder says advertised 5.8+, V58 confirms bounded 5.8 not earliest all-family support.

**Desired:** Use exact original CTAP revision/status/anchors, version-dependent numbering, Yubico Labs **previewSign v4 Editor’s Draft** and separate ARKG Internet-Draft/helper experimental status. Preserve explicit production prohibition; do not turn experimental availability into guidance. Remove unsupported blanket conformance, beta/device-error claims unless provenance retained. Earliest support/version equivalence remains **unknown**, not assumed disproved. Feature advertisement on actual authenticator is needed independently of firmware floor.

**Impact:** all E citation regions, particularly identifiers/ArkgP256/PreviewSignCbor/RegistrationInput/CoseSignArgs, builder hmac comments and owned tests; C COSE alg/seed claims; F README/CLAUDE firmware tables; G current boundary standards claims. Trigger: publication/revision/feature support change. Report **C/F/G** inconsistent provenance; do not change their sources.

### E-13 — previewSign signed algorithm/inner flags and attestation trust (medium DOC + deferred IMP)

**Observed:** PreviewSignCbor DecodeRegistrationOutput `:221–229` treats missing **outer** flags as beta exception; P4 correctly puts flags **inside** signing-key attestation only. Inner-attestation shape/byte-string wrapper is implemented, but caller still must verify attestation before trust: extraction helper parses seed key and discards attestation statement/outer alg/inner policy checks. `TryExtractGeneratedKey` catches only CBOR/InvalidOperation exceptions; malformed authData can throw other exceptions, contrary “false if malformed” blanket. PreviewSignRegistrationOutput claims backward-compatible accessor rather than current contract.

**Desired:** Distinguish signed chosen algorithm, unsigned wrapped attestation object, inner policy flags, syntactic extraction and attestation/trust verification. Optional outer flags tolerated by parser is not device qualification. Returned ARKG helper is not verification of device identity, key binding or agreed policy. Pass 2 test actual malformed envelopes and define exception/validation contract.

**Impact:** PreviewSignCbor output/extraction XML/internal claims, RegistrationOutput, GeneratedKey, preview integration comments; C authData/attestation and G higher-level transforms/trust boundary. Trigger: extraction/attestation policy change.

### E-14 — two generated-key accessors disagree on derived algorithm (medium deferred IMP + DOC qualification)

**Observed:** `PreviewSignRegistrationOutput.TryGetGeneratedKey:91–95` passes **arkgKey.Algorithm** (-65700 seed marker) to property named DerivedKeyAlgorithm; `PreviewSignCbor.TryExtractGeneratedKey:498–502` passes **DerivedKeyAlgorithm** (e.g. -9). `PreviewSignGeneratedKey.DerivePublicKey` does not consult property; VerifySignature unconditionally uses P-256/SHA256. Unit generated-key fixture sets property to -65539 but checks only derived point/handles/context, not algorithm propagation.

**Desired:** Pass 1 identify ARKG-specific convenience and distinction between negotiated signing-op (-65539), seed-key marker (-65700), derived verification algorithm (-9); do not claim interchangeable generated-key accessors. Pass 2 correct propagation and unsupported algorithm handling with assertions on both accessors. This is E's consumer wiring finding, not a duplicate C COSE audit.

**Impact:** RegistrationOutput TryGetGeneratedKey, GeneratedKey DerivedKeyAlgorithm/Derive, DerivedKey Verify, C seed/algorithm docs, G generated output mapping; reopen on propagation/algorithm dispatch changes.

### E-15 — previewSign raw generic contract and SDK-only guards (medium DOC + deferred IMP decision)

**Observed:** SigningParams constructor rejects empty keyHandle/Tbs, public init can bypass guards; P4 explicitly permits empty key handle and CDDL accepts empty bstr TBS. Generic additionalArgs emits any bytes, including empty, despite P4 bstr.cbor map requirement. Registration list and flags borrowed/init-overridable, only nonempty original list checked. Builder if both registration/auth inputs are set silently chooses registration. No ceremony/allow-list enforcement exists here. ArkgP256SignArgs validates lengths, not SEC1 prefix/point, yet comment predicts one exact firmware error and attributes ≤64 to byte-prefix capacity.

**Desired:** Name SDK-only constructor restrictions and byte passthrough, protocol expectations and caller responsibility; retain null versus empty additionalArgs encoding distinction without claiming empty is valid for every algorithm. State one ceremony input per builder and nonempty selected-credential allow list. Pass 2 choose consistent construction/operation validation; do not blindly expand scope to crypto fixes. 64 limit comes from A10 explicit algorithm bound, not 8-bit capacity.

**Impact:** SigningParams/RegistrationInput, builder WithPreviewSign, ArkgP256SignArgs/CoseSignArgs/PreviewSignCbor, C allowList request and G selection. Trigger: guard/encoding/algorithm contract change.

### E-16 — experimental DER verification lacks full structural checks (medium deferred IMP)

**Observed:** DerivedKey ConvertDerToIeee `:151–163` reads two INTEGERs but never checks sequence or root exhausted, signed integer positivity, or canonical positive r/s bounds itself; broad catch reports false for all exceptions. VerifySignature hashes **raw message** with SHA256. The generic TBS can already be a digest; passing the digest to this helper hashes it again. No unit file covers verifier malformed/trailing ASN.1 shapes; integration positive fixture sends SHA256(raw) and verifies raw.

**Desired:** Preserve “experimental, not production” and exact raw-message/DER contract. Pass 2 review strict DER/full-consumption and algorithm-specific verification, with real positive/negative tests; no experiment/exploit/hardware result claimed. Public key import performs its own validation; no claim malformed key bypass from this inspection.

**Impact:** DerivedKey VerifySignature/converter, GeneratedKey cross-reference, preview integration comments; G verification adapter boundary. Trigger: verifier implementation/algorithm change.

### E-17 — assertions do not establish the narrated feature/security coverage (medium DOC + deferred IMP test work)

**Observed/desired comment correction and future assertion work:**

- `Ctap2PrfTests:26,42–46,130,192–195`: calls “CTAP2 PRF,” requires 2.1, promises two assertions/same salt, but makes **one ordinary assertion without encrypted salts**, no PRF transform/output/determinism assertion. First registration checks any extensions conditionally, not hmac true. S23 hmac sample advertises FIDO_2_0; no universal 2.1 PRF floor. Explain actual underlying hmac registration/ordinary assertion only; G owns real PRF transformation audit. Test behavior upgrades deferred.
- `Ctap2CredProtectTests`: level2 name implies discovery/allow-list requirement, but both requests have PIN auth and no no-UV negative case. Policy echo conditional; level3 positive UV bit does not prove rejection without UV. `:128–130` falsely says normal token **must regenerate for every invocation**; report to **B** for exact lifetime clause, not an E token-domain finding.
- `Ctap2CredBlobTests` max-length stored assertion nested behind optional output; not unconditional stored proof. First round-trip retrieval assertion **is unconditional and useful**. Historical macOS NRE skip comments need rebaseline with A; preserve real platform issue evidence, not declare current defect fixed from silence.
- `Ctap2HmacSecretTests` registration checks output presence conditionally; positive assertion does verify 32-byte nonzero output and same-salt test compares two UV-authorized results, but does not establish different UV branches or all protocol versions.
- `Ctap2MinPinLengthTests` no configured RP authorization or unconditional output; GetInfo min field assertion also conditional. Do not describe these as proving authorized disclosure.
- `LargeBlobSpecFormatTests:217–229` says **Encrypt** leaves no copies; actual seam observes TryDecrypt compressed/inflate arrays on success only. `AuditV2` unit comment “Every SDK-owned temporary” needs same bound. Foreign-entry fixture preserves one unknown bstr; local handcrafted spec-shaped encoder is not executed python-fido2 interoperability.
- `Ctap2PreviewSignTests:41–44,55–57,297,336–356` retains Phase 9.5/D “root-cause fix” history, calls builder encoding manual, and asserts inner ARKG args provide “own auth” and second ClientPin changes ECDH state/0x7F. Generic P4 does not justify this authorization claim; firmware reproduction/provenance **unknown**. Full ceremony uses flags0 + upfalse yet remarks label signing “touch #2.” Replace with actual request policy, not touch count assertion.
- Preview negative tests `:530,622,779` TODOs say constants absent, though A-owned CtapStatus defines MissingParameter `:92`, UnsupportedAlgorithm `:142` and InvalidOption `:162` (narrow grep confirmed); catch CtapException wraps setup and can pass before target invocation. Preserve desired exact-error work as deferred finding, not deletion metric. No current error-code/phase assertion proof.
- `ExtensionOutputTests` last test's name says unsupported extension, but supplies empty input only. Defer renaming executable identifier/assertions; correct commentary where necessary. `HmacSecretInputTests` checks only map count, not all key-agreement/length bytes. Mock Version2/tag16/XOR is a fake, not protocol2 crypto evidence.

**Impact:** exact owned test regions above and all dependent “tested/proven” public claims; F testing examples; A/B/C/G cross-domain comments. Pass 2 trigger: meaningful new assertions/fixtures or hardware run with recorded device/firmware/transport. Existing useful tests remain; no indiscriminate removal of TODOs or placeholder metric.

### E-18 — integration fixtures leave secret material and one exports keys (high deferred IMP + DOC)

**Observed:** many owned integration tests acquire tokens without finally zeroing; HmacSecret clears shared/plain only after successful assertions and uses non-fixed-time secret comparison plus extra plaintext ToArray copies. LargeBlob write tokens cleared only success; retrieved plaintext/key views not systematically cleared. EncryptedMetadata tests clear token but retain plaintext and compare with SequenceEqual. Preview cleanup swallows errors, success-only token clearing and broad error catch. Unit random keys/PPUAT/plain copies also lack comprehensive cleanup (synthetic fixtures, not actual live secrets). `IntegrationTests/AuditV2/Ctap2LargeBlobAuditReproTests:102–106` **optionally writes largeBlobKey as immutable hex text to disk**; sibling `:122–124` says never persist key while reading secret from environment. Export is current executable behavior, not authorized or executed here.

**Desired:** Document explicit secret-export risk and human-controlled interop prerequisite, do not label test safe/no-persistence. Pass 2 remove/redesign key-export channel, review test cleanup/fixed-time secret comparisons/exception safety and avoid secrets in strings/logs. No source/test strings changed. Copying nonsecret fixture signatures/public points is not a zeroing violation by itself.

**Impact:** AuditV2 integration interop comments; HmacSecret/LargeBlob/EncryptedMetadata/Preview fixture cleanup and any guide suggesting copying fixtures; F security/testing notes. Trigger: test evidence-channel/cleanup redesign. Separate test deficiency from shipping cleanup E-09 and raw-response owner E-08.

## Shared claim-matrix rows (coordinator contributions)

| Feature | Revision/status and capability | Request / configured state | Actual SDK enforcement/result | Inspected evidence and limits |
|---|---|---|---|---|
| credProtect | S21 §12.1.1; advertise credProtect; authenticator UV-capable | request 1/2/3; platform enforcement distinct; effective result may be stronger | builder emits integer; enforcePolicy inert; caller can parse result | enum/encoding fixtures; hardware positive UV tests only, not run; E-01/E-17 |
| credBlob | S21 §12.2/§12.2.1; maxCredBlobLength ≥32, credProtect dependency | store bstr vs retrieve true; sensitive RP MUST level3+enforce | DTO fixed 1–32, builder no length gate; bool stored, retrieval empty possible, absence separate | encoding + unconditional retrieval fixture; max stored conditional, not run; E-02 |
| hmac-secret/MC | S21 §12.5; S22/S23 §§12.7/12.8; MC separate advertisement, V58 bounded | boolean registration; encrypted 1/2 salts and saltAuth; UV-mode/UP dependency; MC companion true | no capability/protocol negotiation; output accessor only byte-string hmac; raw MC possible | positive/count/32-byte fixtures only; no all-protocol/all-UV proof; E-05/E-06 |
| minPinLength | S21 §12.4, S22/S23 §12.5; configured RP list distinct from support | creation true; authorized RP only | reports integer, no config change; absent unauthorized | conditional tests no disclosure-policy proof; E-04/E-17 |
| largeBlobKey/storage | S21/S23 §12.3 + §6.10; both advertisement and largeBlobs true | discoverable credential key; write SDK token required; underlying auth UV-conditional | envelope key5/7; trial AEAD/decompress; cap 1MiB; read corruption → initial | local format/hash fixtures; one-fragment hardware interop fixture not run; E-03/E-09/E-11 |
| blob chunks | S23 §6.10.2, platform repeated read SHOULD, integrity/max write MUST | get + offset; set + offset, first length; per-chunk 70-byte auth message | cached limits; sequential chunks, no whole-flow conflict lock/rollback/resume | first-fragment/count/default960/size gate tests, no cancel/interleaving/malformed-config proof; E-10 |
| direct largeBlob | S22/S23 §12.4 standard alternative; no blanket hardware absence evidence | support/read/write compressed + originalSize; mutually exclusive key path | not builder/storage implemented; old DTO conflates outputs | synthetic map fixture only; E-03 |
| previewSign | P4 Editor’s Draft 2025-08-26; V58 preview, not CTAP4 | one selected credential; signing flags fixed at generation; args CBOR-bstr by algorithm | generic bytes unchanged; SDK guards imperfect, no attestation trust validation | byte fixtures, positive/negative hardware assertions inspected not executed; negative setup catches; E-12/E-13/E-15/E-17 |
| ARKG helpers | A10 Internet-Draft work in progress, expired stated date; experimental; no full conformance claim | borrowed public seed, entropy/context ≤64; offline RP derivation | Core provider delegated; algorithm propagation inconsistent; SHA256 raw/DER verifier | one expected point + different ikm/ctx comparisons, not unlinkability/full conformance; E-14/E-16 |
| encrypted metadata | S23 §6.4 optional GetInfo; vendor V58 bounded | matching persistent token; IV16+cipher16 for standard 128-bit values | AES128CBC no padding; wrong token can return bytes; no authenticity verification; generic lengths accepted | CBC same/wrong token local tests; live tests read only lengths/equality, not run; see qualification below |
| raw/sensitive binary | root security/forward-compat doctrine + L | borrow inputs until encoding/exchange completes; output copies independent | ExtensionOutput reconstructs/loses some CBOR; no disposable owner; plaintext probes uncleared | no end-to-end lifecycle proof; E-07/E-08/E-09/E-18 |

### Encrypted-metadata contract qualification (E-19; high DOC + deferred IMP length decision)

`EncryptedMetadataDecryptor:19–27,52–54,69–70` incorrectly calls fields per-credential metadata/credential-management outputs and says privacy “across sessions.” Originals S23 §6.4 identify stable **device identifier** and opaque **credential-store state**, with randomized ciphertext each GetInfo. Class/functions are offline helpers; no firmware/capability gate. They borrow token/encrypted spans and derive HKDF-SHA256 salt32zero/info-specific AES128 key; internal stack key cleared in finally. `DeriveKey` returns caller-owned **secret key**, decrypt returns caller-owned plaintext; zero both when finished. CBC/no-padding is **not authenticated encryption**; null means selected invalid/crypto failures, not reliable wrong-token detection. Owned wrong-PPUAT unit explicitly expects non-null wrong bytes. No integer/status/credential ID interpretation should be invented for 16-byte opaque values.

Standard representation is IV16 + ciphertext16 for a 128-bit value; source accepts IV-only and arbitrary block-length ciphertext, PPUAT any nonempty length. Unit tests intentionally accept 32-byte plaintext/64-byte token; those prove generic local behavior, not spec GetInfo formats. Pass 2 decide strict protocol wrapper versus generic helper, add exact malformed-length/null/empty tests and failure-output cleanup. Earliest 5.7 support is not settled by V58, so do not assert impossibility or universal minimum from firmware names. **Impact:** all decryptor XML, unit Java-format comments, integration two-session/different-PPUAT claims (tokens not asserted distinct), A AuthenticatorInfo field docs, B persistent token contract, D credential management narrative, F table, G cached discovery. Reopen on token/field validation or freshness semantics change.

## Named clean semantic units for independent sampling

“Clean” below means the named narrow unit matches inspected source/spec/assertions; it does **not** clear adjacent findings, certify crypto or imply execution. Boilerplate licenses/imports/namespaces have no substantive finding and were read, not counted as new prose units.

| Unit | Exact semantic unit | Why clean / bounded evidence |
|---|---|---|
| E-CLEAN-01 | CredBlobMakeCredentialOutput Stored + Decode (`:19–48`) | boolean storage result, including failure; no unsupported enforcement promise |
| E-CLEAN-02 | CredBlobAssertionOutput Blob + Decode (`:19–48`) | bstr retrieval, empty no stored blob; source-copy ownership separately E-08 |
| E-CLEAN-03 | Builder WithCredBlob/WithGetCredBlob operation distinction + conflict (`:68–96,313–327`) | S21 store bstr / assertion true, rejects mixing |
| E-CLEAN-04 | Builder MC companion generation (`:222–228,347–357`) | S22 §12.8 requires hmac-secret true, emitted |
| E-CLEAN-05 | HmacSecretInput SaltAuth (`:65–73`) | protocol1 truncated16 / protocol2 full32, crosscheck B |
| E-CLEAN-06 | PreviewSignSigningParams null/empty additionalArgs narrative (`:26–33,48–53`) | source distinguishes omission vs bstr-empty; validity qualification E-15 |
| E-CLEAN-07 | PreviewSignCbor EncodeAuthenticationInput (`:175–214`) | flat chosen credential keys2/6/7, bstr wrapping |
| E-CLEAN-08 | CoseSignArgs closed hierarchy (`:33–58`) | private-protected base, known subtype encoder; experimental context retained |
| E-CLEAN-09 | LargeBlobEntry AAD/decrypt length rationale (`:28–35,126–147,262–268,283–323`) | S23 raw DEFLATE/GCM/AAD; success seam checks two allocated arrays only |
| E-CLEAN-10 | LargeBlobStorage invalid hash fallback (`:118–131`) | short/hash-corrupt initial array; malformed valid-hash CBOR not normalized |
| E-CLEAN-11 | LargeBlobStorage size gate (`:153–168`) | refuses reported overcapacity before blob write; mocked DidNotReceive assertion |
| E-CLEAN-12 | AuditV2 extension GetCredBlob fixture (`:10–30`) | reads complete request key4/credBlob true, not hardware proof |
| E-CLEAN-13 | PreviewSignCborTests deterministic 126-byte map (`:126–155`) | exact expected bytes, dummy point not crypto-valid; historical PRD narration can be simplified |
| E-CLEAN-14 | Ctap2CredBlobTests retrieval assertions (`:147–152`) | output, key and byte equality unconditional; creation assertion remains E-17 |
| E-CLEAN-15 | EncryptedMetadataDecryptor key derivation/stack cleanup (`:88–104,127–157`) | observed HKDF16/context separation and stack-key finally; returned arrays E-19 |

## All 57 owned path dispositions

Each row means **complete file read at matching inventory hash**. “Clean” refers only to named unit or narrow assertions, not blanket audit approval. No path is excluded or left uninvestigated. Documentation review and implementation/hardware verification remain pending independently.

| # | Exact path | Disposition / semantic coverage |
|---|---|---|
| 1 | `src/Ctap2/src/Arkg/ArkgP256.cs` | E-12; delegated derivation XML, experimental/provenance; narrow Core boundary |
| 2 | `src/Ctap2/src/Crypto/EncryptedMetadataDecryptor.cs` | E-19, E-CLEAN-15; all HKDF/decrypt/ownership/error units |
| 3 | `src/Ctap2/src/Extensions/CredBlob/CredBlobAssertionOutput.cs` | E-CLEAN-02 + E-08 ownership |
| 4 | `src/Ctap2/src/Extensions/CredBlob/CredBlobInput.cs` | E-02/E-08; full purpose/range/encoding |
| 5 | `src/Ctap2/src/Extensions/CredBlob/CredBlobMakeCredentialOutput.cs` | E-CLEAN-01 |
| 6 | `src/Ctap2/src/Extensions/CredProtect/CredProtectPolicy.cs` | E-01/E-12; defaults unsupported “most”; discovery/UV policy values |
| 7 | `src/Ctap2/src/Extensions/HmacSecret/HmacSecretInput.cs` | E-05/E-08; E-CLEAN-05 tag lengths |
| 8 | `src/Ctap2/src/Extensions/HmacSecret/HmacSecretOutput.cs` | E-05/E-06/E-08; encrypted output/decode |
| 9 | `src/Ctap2/src/Extensions/LargeBlob/LargeBlobOutput.cs` | E-03/E-08; every field/decoder |
| 10 | `src/Ctap2/src/Extensions/MinPinLength/MinPinLengthInput.cs` | E-04; disclosure/request false encoding distinct from comment true |
| 11 | `src/Ctap2/src/Extensions/MinPinLength/MinPinLengthOutput.cs` | E-04; code-point/current minimum/decode |
| 12 | `src/Ctap2/src/Extensions/PreviewSign/ArkgP256SignArgs.cs` | E-08/E-12/E-15; borrow/shape/ctx bound/error claim |
| 13 | `src/Ctap2/src/Extensions/PreviewSign/CoseSignArgs.cs` | E-12/E-15; E-CLEAN-08 hierarchy |
| 14 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignAuthenticationOutput.cs` | E-08/E-12; passive signature DTO/guard, no crypto validation |
| 15 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignCbor.cs` | E-08/E-12/E-13/E-14/E-15; E-CLEAN-07; all encoders/decoders/extraction |
| 16 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignDerivedKey.cs` | E-08/E-12/E-16; raw/SHA256/DER/public views |
| 17 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignGeneratedKey.cs` | E-08/E-12/E-14; offline derivation/context uniqueness bounds |
| 18 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationInput.cs` | E-08/E-12/E-15; algorithm preference, flags, ctor/init |
| 19 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs` | E-08/E-13/E-14; extraction/alg propagation/raw fields |
| 20 | `src/Ctap2/src/Extensions/PreviewSign/PreviewSignSigningParams.cs` | E-08/E-15; E-CLEAN-06 null/empty |
| 21 | `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs` | E-01–03/E-04–05/E-08–09/E-12/E-15; E-CLEAN-03/04; complete state/encoding/helpers |
| 22 | `src/Ctap2/src/Extensions/Shared/ExtensionIdentifiers.cs` | E-03–05/E-12; all identifier claims |
| 23 | `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs` | E-03/E-06–08; all overloads/getters/raw copying |
| 24 | `src/Ctap2/src/LargeBlobs/ILargeBlobStorage.cs` | E-09–10/E-11; all borrowed/plain/result/state contracts |
| 25 | `src/Ctap2/src/LargeBlobs/LargeBlobData.cs` | E-09/E-11; E-CLEAN-09; entry/array full format/unknown/cap/collections |
| 26 | `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs` | E-03/E-09–11; E-CLEAN-10/11; all operation/chunk/failure units |
| 27 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/AuditV2/Ctap2LargeBlobAuditReproTests.cs` | E-17/E-18; one-fragment independent format, cross-client manual prerequisite/key export; not run |
| 28 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredBlobTests.cs` | E-17/E-18; E-CLEAN-14; store/retrieve/max/skip/cleanup; not run |
| 29 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredProtectTests.cs` | E-17/E-18; positive UV/discovery, conditional echo, false token-lifetime comment to B; not run |
| 30 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2EncryptedMetadataTests.cs` | E-17/E-18/E-19; two sessions/field lengths/wrong token/helpers, not run |
| 31 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2HmacSecretTests.cs` | E-05/E-17/E-18; registration, 32-byte derivation/same salt positive UV, not run |
| 32 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2LargeBlobTests.cs` | E-03/E-09/E-17/E-18; set/get/delete positive fixtures, not run |
| 33 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2MinPinLengthTests.cs` | E-04/E-17/E-18; conditional disclosure/info assertions, not run |
| 34 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PreviewSignTests.cs` | E-12–18; full 800-line registration/ceremony/negative/error/cleanup/comments, not run |
| 35 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2PrfTests.cs` | E-17/E-18; actual hmac registration/one ordinary assertion, not PRF evaluation; not run |
| 36 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuditV2/Ctap2ExtensionAuditReproTests.cs` | E-12 numbering; E-CLEAN-12; hmac true + single-credential preview keys; not run |
| 37 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/AuditV2/Ctap2LargeBlobAuditReproTests.cs` | E-17/E-18; default960/local spec-shaped crypto/hash/two-temp seam, not run |
| 38 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Crypto/EncryptedMetadataDecryptorTests.cs` | E-18/E-19; entire HKDF/CBC/same/wrong/context/multiblock/64token/short input fixture, not run |
| 39 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/CredBlob/CredBlobAssertionOutputTests.cs` | narrow clean decoded byte equality, ownership not asserted; not run |
| 40 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/CredBlob/CredBlobInputTests.cs` | narrow clean 8-byte bstr encode; no capacity boundary evidence; not run |
| 41 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/CredBlob/CredBlobMakeCredentialOutputTests.cs` | narrow clean boolean true decode; not run |
| 42 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/CredProtect/CredProtectPolicyTests.cs` | narrow clean enum1/2/3 values, no policy enforcement; not run |
| 43 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/HmacSecret/HmacSecretInputTests.cs` | E-17 count-only evidence; not run |
| 44 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/HmacSecret/HmacSecretOutputTests.cs` | narrow clean 48-byte decode/equality; MC routing not asserted E-06; not run |
| 45 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/LargeBlob/LargeBlobOutputTests.cs` | E-03 synthetic key/written map, not standard direct output; not run |
| 46 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/MinPinLength/MinPinLengthInputTests.cs` | narrow clean true encode; not authorized disclosure evidence; not run |
| 47 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/MinPinLength/MinPinLengthOutputTests.cs` | narrow clean integer8 decode/equality; not run |
| 48 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/ArkgP256SignArgsTests.cs` | E-15; lengths/alg/ctx empty64; accepts dummy no-prefix key; not run |
| 49 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/CoseSignArgsTests.cs` | narrow clean factory type/field equality; fixture explicitly dummy; not run |
| 50 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/PreviewSignCborTests.cs` | E-12/E-17 historical/unbounded claims; E-CLEAN-13 exact126 and flat wrappers; not run |
| 51 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/PreviewSign/PreviewSignGeneratedKeyTests.cs` | E-12/E-14; expected point/different inputs/contexts, not algorithm field/full conformance; not run |
| 52 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/Shared/ExtensionBuilderTests.cs` | E-02/E-12/E-17; full null/encode/conflicts/oversize/empty/mock fixture, not run |
| 53 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/Shared/ExtensionIdentifiersTests.cs` | narrow clean seven string constants, no standard-status inference; not run |
| 54 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Extensions/Shared/ExtensionOutputTests.cs` | E-03/E-06/E-17; modeled positive outputs/missing/empty, no unknown/raw preservation proof; not run |
| 55 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobDataTests.cs` | E-18; complete entry roundtrip/wrongkey/empty/4096/array/hash/list/probe fixtures, not run |
| 56 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobSpecFormatTests.cs` | E-11/E-17/E-18; format/AAD/unknown99/nonconforming/cap/defaultsize/sizegate/seam; not run |
| 57 | `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/LargeBlobs/LargeBlobStorageTests.cs` | E-10/E-17/E-18; all first-fragment/protocol/authparam/no-match fixtures, no multichunk/cancel/state proof; not run |

## Cross-owner handoff and research bounds

- **A:** capability caches/firmware flags, malformed raw response and cancellation/sequence boundary, stale macOS skip claims, constants available for negative preview assertions; E only documents storage/extension manifestations.
- **B:** exact protocol1/2 lengths/auth distinction; regular-token reuse comment; persistent-token requirements for encrypted metadata. E does not duplicate PIN/key agreement audit.
- **C:** response envelope keys/ownership, raw authData vs reconstructed ExtensionOutput, allowList/nonempty preview requirement, seed/derived algorithm disambiguation and inconsistent accessor wiring. C owns COSE/request code; its adjacent seed Encode map/bstr choices were noted during dependency reading but require C's own assessment, not another E full-domain finding.
- **D:** min PIN setting vs disclosure, encrypted fields belong to GetInfo rather than per-credential management; borrowed token/storage lifetimes aligned with its helper docs.
- **F:** stale extension table APIs/status/firmware, privacy/enforcement/ownership examples and broad unsupported-device assertions. Exact modules sources read as dependencies: root/module/test instructions + README only, not all F source/examples.
- **G:** hmac↔PRF transforms, key-storage vs direct largeBlob↔WebAuthn result, preview credential selection/attestation trust, owner/view lifetimes. Do not infer client/RP transforms from CTAP fixtures.

No all-device support statement, production crypto recommendation, full standard conformance claim or test/hardware execution claim is made. Unknown after bounded research: earliest MC/PPUAT/encrypted-field support by device family, beta 0x7F/ECDH interaction provenance, -09/-10 complete algorithm parity and every device's direct-largeBlob support. Preserve these as evidence gaps/second-sweep dependencies; no source behavior was fixed and no TODO metric was used.
