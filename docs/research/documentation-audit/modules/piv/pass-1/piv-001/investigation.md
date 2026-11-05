# PIV whole-module investigation

**Outcome:** the post-PR745 module supports a coherent documentation pass, with11 documentation
candidate groups and10 separate implementation/qualification follow-ups. Earlier retry, management
state, recovery-witness and mutable-output findings have been re-baselined against the merged code.
This report supplies an editing/review packet; coordinator adjudication and independent review remain
pending. No product file, assertion, help string, API snapshot or runtime behavior was changed here.

Investigator: fresh GPT-6.1 Sol. Baseline`727fe43afc430c5d8364d868a6436de50626e476` in the primary
checkout. Root/module instructions, run plan/status/inventory, campaign workflow/style and SDK house
style were read. `src/Piv/tests/CLAUDE.md` is absent; root test guidance applies. Source research has
fresh capture timestamps spanning November4–5 UTC; the campaign date is November5,2026.

## Coverage and effective API reconciliation

All123 tracked inventory paths received a full semantic read:33 library C#,36 PivTool C#,
24 integration C#,18 unit C#,4 Markdown,4 projects,2 public-API snapshots and2 runner JSON files.
License boilerplate was treated as repeated administrative material; complete meaningful code,
XML, local comments, examples, assertions, setup/helpers and cleanup were read. Public/internal
classification follows containing-type accessibility, not the presence of a `public` token alone.

`coverage.json` records every file's original/current SHA256 and nonoverlapping semantic regions.
`evidence/api-reconciliation.json` reconciles370 API entries,23 effective public types,
42 declared `IPivSession` asynchronous operations (including both signing overloads), and2 declared
properties. The shipped snapshot has only its nullable header. The42 operations have concrete
facade counterparts; creation is additionally exposed through the static factory and one `IYubiKey`
extension. `SlotMetadataExtensions` adds2 host key extractors. Public `Features` has10 feature
properties and the RSA-generation predicate; it is a standalone catalog, not a session nested type.

The23 shipping public types comprise4 typed objects,5 metadata records,6 enums,2 session types,
3 static API containers (`DataObject`, `Features`, `SlotMetadataExtensions`), the creation-extension
container, `KeyCreationOptions`, and `InvalidPinException`. `PinUtilities` methods remain internal
through their containing type. Protocol helpers/backends/initialization/key references/mappings
remain internal; the facade constructor is internal and inherited `EnsureSupports` is protected.
The C#14 extension receiver declaration, extension member and static compatibility member account
for3 snapshot entries representing one creation operation. Generated record constructors,
accessors/equality/operators/Deconstruct/ToString/GetHashCode remain representation entries, with
meaningful positional-property semantics documented at the record; they are not extra task APIs.

The coverage generator nominates clean source regions conservatively: candidate-associated files
are excluded until the editor refines changed spans. The current clean-sampling pool has 396 eligible
regions: 83 API, 93 examples, 210 internal/test and 10 narrative, including seven clean scoped shared
regions. The coordinator selects at least 40
distinct regions spanning these categories. Selection and independent review are pending. Remove
edited overlaps, and expand a category after material misses. Counts are semantic-region inventory,
not an automated claim of correctness. Example XML blocks and surrounding resource scopes are
included even where the body uses only an implicit `session`, `client`, `device` or helper name.

## Source authority

`evidence/source-analysis.md` gives original publication/revision/status/part/section, actor,
normative strength and profile conditions. It includes the combined SP800-73-4 original (May2015,
updated February8,2016), SP800-78-4 (May2015), FIPS201-2 (August2013), FIPS186-5 §6.4.1,
RFC8017/8032/7748/3279 and focused Yubico references. Original archive notices establish
supersession; this pass preserves the referenced revision rather than adopting newer profiles.
PIN ASCII-digit rules in SP800-73-4 and raw-byte/UTF-8 YubiKey practice are explicitly separated.

Python YubiKey Manager code/tests/history are pinned to`4ca60f706af930459138d8dc0f0f953480e1c7a4`;
Android remains pinned to`76b89d35142d9a4937b6b72e5c840e057a280620`. Fresh immutable downloads,
bounded history patches and source hashes are retained. The source batch plus one focused follow-up
resolved reset/F9 and firmware-origin questions; ISO edition-specific conformance and unexecuted
device/product-profile qualification remain precise gaps. Sibling implementations supply evidence
of behavior/origin, while standards supply their own actor- and profile-conditioned requirements.

## Documentation candidates

Each group is a **candidate for accepted documentation change**; the coordinator owns acceptance.
Severity reflects consumer impact, not an assertion that the underlying runtime is defective.
Proposed edits use affirmative, task-oriented, actor-first wording, with useful rationale and nearby
conditions. Consequential limitations should point to a supported action. Preserve complete resource
scopes; avoid repeated negative formulations and rhetorical contrast.

### PIV-C001 — Explain destructive reset precisely, including retained attestation material

**Priority: high.** `IPivSession.ResetAsync:61–78`, moduleREADME constraints147 and
PivTool `Reset.cs:19–30` say all data/keys/certificates are destroyed. The Yubico reset contract
retains F9 private key and certificate. The SDK blocks PIN and PUK beforeFB; callers need no
management authentication. Cancellation/failure can leave those counters partly consumed before
the reset is sent. RESET success clears management authentication and refreshes the type; metadata
refresh can throw after the physical reset succeeded. Bio MPE uses a different reset route; current
PIV code has a preflight TODO, not a completed Bio guard.

Describe normal-slot/key/object/credential reset and the F9 exception together. Separate “reset
command applied” from “post-reset refresh completed.” Direct Bio MPE readers to the Management
device-reset task without implying that this module or this run qualified that path. The current
PivTool warning strings are preserved runtime text; README/comment qualifications can explain their
scope while PIVIMP-001/006 retain behavioral/UI follow-up.

**Dependencies:** moduleREADME/CLAUDE, IPivSession reset contract, PivTool README/Reset comments,
test `ResetAsync_ClearsAllSlots` commentary, migration PIV notes. **Reopen when:** reset preflight,
reset scope or post-reset failure policy changes.

### PIV-C002 — Credential bytes, retry counts and recovery instructions need actual operation limits

**Priority: high.** SDK VerifyPin/ChangePin validate6–8 **encoded bytes**, with no content validation.
ChangePuk/UnblockPin route through `EncodePinPairBytes`, which validates only the8-byte maximum;
there is no host minimum on those inputs. Device/product policy can enforce more. PivTool interactive
prompts use ASCII-digit filters; noninteractive credential flags are UTF-8 encoded. PivTool XML
`PinManagement.cs:40,91,139,172` incorrectly labels characters; `PinUtilities` attributes unrestricted
UTF-8 to the PIV specification. SP800-73-4 Part2 §2.4.3 instead specifies a6–8-byte decimal PIN
and an8-byte binary PUK. Describe SDK mechanics and vendor/profile requirements separately.

`GetPinAttemptsAsync` prefers metadata and falls back only on unsupported metadata instruction;
old firmware uses absent-data VERIFY. A9000 response supplies no count and raises
InvalidOperationException. Status63CX carries at most15; metadata can report larger counters.
Three is a reset/default count, not the configured general maximum. `SetPinAttemptsAsync` validates
1–255, requires management auth plus card-verifiedPIN and resets both credentials every time.
Teach bounded application-controlled retry using `InvalidPinException.RetriesRemaining`, PUK-based
unblock while PUK remains usable, and destructive reset as a distinct recovery decision.

**Dependencies:** IPivSession, README, exception summary (also used for Bio retry errors),
AuthenticationProtocol/PinUtilities local rationale, PivTool PIN/PUK comments and examples.
**Reopen when:** credential/profile validation, counter encoding or exception/fallback behavior changes.

### PIV-C003 — Give usable crypto formats and complete host verification/cleanup examples

**Priority: high.** Core raw PIV behavior is now correct for Ed25519 messages, ECDSA digest integer
conversion and RSA oversize rejection. Public output format remains underspecified: ECDSA returns
DER SEQUENCE(r,s), Ed25519 raw64-byte signature, RSA a modulus-sized raw result. RSA signing needs
host EMSA encoding (including hash/DigestInfo/PSS preparation); raw RSA decrypt returns the padded
representative, while `DecryptAsync` removes PKCS1/OAEP on the host and requires metadata5.3.
Existing attributes and comments that suggest high-level decrypt works on all4.x keys overreach.
`CalculateSecretAsync` accepts P256/P384/X25519 peers, returns a raw secret with no KDF, and relies
on the caller to choose a matching slot algorithm and authenticate the peer/protocol.

ModuleREADME's sign snippet stops at an unused signature. Add independent host verification with
the actual generated/extracted public key, explicitDER format and cleanup in finally. Add one
coherent ECDH or RSA decrypt example with mutable ownedMemory cleanup; preserve caller-owned inputs.
Key extractor examples need nullable-metadata guards and explanation that the returnedRSA/ECDsa
contains public material: external signing through an X509SignatureGenerator is needed for device
private-key CSR/certificate issuance. XML bullet dashes inside a paragraph should become actual
list elements where the documentation renderer requires them.

**Dependencies:** IPivSession crypto methods, README tasks, SlotMetadataExtensions XML/examples,
PivTool crypto explanations and source citations. **Reopen when:** formats, padding API, algorithm
selection, output validation/ownership or host-provider support changes.

### PIV-C004 — Explain data-object model scope, metadata representation and certificate independence

**Priority: high.** Certificates and keys are separate resources. Certificate retrieval null means
no usable certificate data, not proof the private-key slot is empty; generating/importing/moving/
deleting a key and deleting a certificate affect separate storage. StoreCertificate accepts a
certificate without proving its public key matches the private key. Consumers should compareSPKI
or perform a key-bound proof before enrollment. Automatic GZip usesDER length>1856; this threshold
is SDK policy, not a hard standard/device limit. Device storage limits still apply.

CHUID/CCC models implement a **fixed SDK/YubiKey provisioning shape**, rather than universal
device-fixed fields: supplied GUID/card-id memory is borrowed, decoding returns copies, generated
values use random bytes, Encode returns an owned nonsecret encoding. CHUID fixesFASC-N/date and
empty signature/LRC in the SDK; “identical for every YubiKey” overstates that convenience model.
Typed readers reject different otherwise-valid shapes; rawGetObject retains a route to inspection.
KeyHistory counts are retired keys with on-/off-card certificates; callers maintain the object,
and the constructor'sURI/byte/count validation is narrower than normative profile validation.
AdminData flags are stored software metadata, not fresh hardware proofs ofPUK/key state. Metadata
PublicKey is rawPIVTLV (notSPKI), RawAlgorithmId/RawData retain unknown fields, and KeyType=null
means unmodeled, not empty. Host extractor results require disposal.

**Dependencies:** data-model/metadata XML, IPivSession key/cert/object/typed accessors, README task
flow, tool result XML, migration typed-data map. **Reopen when:** generalized parsers, certificate
binding, object validation, memory borrowing or data-model encoding changes.

### PIV-C005 — Make PIN-only setup, recovery and partial transitions actionable

**Priority: high.** API explanations cover order but the entry guide omits the task. Protected-mode
enable authenticates the supplied **already active** management key, verifiesPIN, storesPRINTED,
blocksPUK and writesADMIN. It preserves any existing derived salt. Separate APDUs provide no rollback;
a failure can leave completed writes/blockedPUK. Disable restores the active-algorithm default first,
then deletesPRINTED andADMIN; it leavesPUK blocked. Detection readsADMIN only and authenticates
nothing. Recovery readsPRINTED, conditionally verifiesPIN once after6982, tries the stored key,
then a legacy derived candidate. Only6982 rejects a candidate; unrelated errors propagate. After
stale derived rejection it restores earlier protected authentication before returning that flag.

Explain intentional supplied-key retention/escrow and how to inspect/re-authenticate after partial
failure before retrying. RawChangePin/SetManagementKey/SetPinAttempts leave related recovery objects
unchanged. Advise coordinated recovery/migration for existing derived configurations. `PinOnlyMode.None`
currently says both modes are available to be set, although derived setup is excluded and absence
ofADMIN says nothing aboutPRINTED occupancy. Unavailable flags reflect ADMIN decoding, not a real
capability probe of both stores. `RecoverPinOnlyModeAsync` returningNone after no candidates does
not promise to clear a preexisting authentication; only an actual failed auth attempt clears it.

**Dependencies:** README complete setup/reopen/recover/disable task, IPivSession, PinOnlyMode,
PinOnlyProtocol rationale, migration recipe/map. **Reopen when:** atomicity/coherence/recovery or
mode detection changes; seePIVIMP-002/009.

### PIV-C006 — Distinguish managed authentication observations, card PIN policy and touch notification

**Priority: high.** `IsManagementKeyAuthenticated` reports localPIVmanagement auth, distinct from
inheritedSCP `IsAuthenticated`; it is not a cache ofPINverification. Authenticate clears prior state
even on canceled/wrong-length attempts after readiness admission. SuccessfulSET updates type and
retains authentication without retaining key bytes; SET6982 clears the flag, other command failures
preserve it. Disposal hides prior auth immediately; physical cardPIN release remains a separateCore
question. Local flag access does not prove a card command will succeed after external changes.

`PinPolicy.Once/Always` describe card verification frequency; default policy is slot-dependent;
MATCH policies use fingerprint verification rather than a requirement for bothPINandfingerprint.
Device metadata capability and enrollment are different. TouchPrompt notifications cover the three
private-key operations, not management auth/biometric ceremonies. Explicit-algorithm sign/agreement
can look up touch metadata if a prompt is configured; metadata-driven sign/decrypt already require
metadata. Default/Never/empty metadata is silent, unknown/unavailable policy conservative. Prompt
request failure prevents transmission; completion follows successful parse before output transfer/
RSAunpadding, so local padding failure can followCompleted. Cancellation is cooperative: await
terminal result/drain and inspect state before retrying a mutation.

**Dependencies:** session/interface properties/auth XML, policy enums, README interaction/security,
CLAUDE, shareduser-interaction PIVparagraph. **Reopen when:** auth/cache, callback sites/resolution
or native cancellation contracts change.

### PIV-C007 — Publish gates as scoped compatibility information, without a certification promise

**Priority: medium/high.** Requirements say4/5series while code and pinned siblings retain a limited
NEO-compatible subset; document tested/current support and old-firmware restrictions proportionately.
Public `Features.SupportsRsaGeneration` returns a firmware predicate and calls it “supported and safe”;
replace the blanket assurance with the exact modeledROCA exclusion and separate device support.
README already records Android/Pythonserial andROCA differences; retain that provenance and avoid
turning sibling agreement into normative requirements. VendorAESreleased-device origin5.4.2 differs
from5.4.0SDK/Pythongate. ModernFIPS profile restrictions are device-enforced beyond this SDK's
firmware4.4.x check. FirmwareVersionOverride changes compatibility evaluation, not physical support,
authorization or resetdefaults; majorzero handling is the SDK's development-device convention.
Catalog representation ofsecp256k1/Brainpool/P521/symmetric types remains distinct fromPIVsubsets.

**Dependencies:** README requirements/features/compatibility, FeaturesXML, CLAUDE, package description
where useful, migration capability notes. **Reopen when:** firmware/product/profile matrix changes.

### PIV-C008 — Repair stale explanatory mechanisms while preserving genuine unfinished work

**Priority: medium.** Constructor XML describes the historical public constructor and names the stale
ApplicationSession.Construct helper instead of ConstructInitializedAsync. DataObjectProtocolTests
names ArrayBufferWriter and says unwrapped responses borrow the original buffer; current code uses
an owned byte array, copies both response branches and always calls Clear. PrivateKeyOperationsTests
references removed rawBytes. BioProtocolTests labels a VERIFY response as GET DATA. Authentication
comments assume three failures and a 0–3 count, and attribute factory key bytes to the PIV standard.
The SET management-key comment lists a cached parameter absent from the boolean API.
PinPrompt.CreateFromSpan claims source zeroing while passing a read-only span to shared buffer
creation; arrays allocated by random/KDF helpers escape explicit clearing. Program's comment claims
TTY-dependent behavior while its branch only checks arguments. JsonOutput emits failure error fields
to stdout despite its broad stderr claim.

Retain the reset Bio TODO as unresolved implementation work; replace phase-number history with the
missing-preflight rationale if edited. Preserve outstanding X25519 verification in the Pass2 ledger
and align its comment with generation-only assertions. Replace speculative mock-test commentary
with what the assertions establish; changing the tests themselves remains deferred.

**Dependencies:** named internal/test/tool comments and module CLAUDE. **Reopen when:** ownership,
helper mechanisms or unfinished feature work changes. PIVIMP-001/006/008 retain real work.

### PIV-C009 — Rebuild current examples and PivTool guidance around tasks and real contracts

**Priority: high.** PivTool README's directory tree points to Features instead of Cli/Menus, discovery
names are obsolete, and only interactive usage is described despite 22 operational commands plus
help. The parser accepts four standard slot selectors; key selectors use `--key-type` with
`eccp256/eccp384`. CLI decrypt uses PKCS1 by default and offers no padding flag. Reset confirmation
varies by `--force`/`--json`; cert-delete help lists `--force`, which the handler ignores. Broad
claims about clearing every secret, displaying default warnings and demonstrating every major
feature exceed current bodies. Explain evaluation scope and process-list/history exposure while
preserving runtime help strings during Pass1.

XML snippets repeatedly use CreatePivSessionAsync(ct), whose first argument is options. Decryption
passes ct as optional padding; management-key change passes ct as requireTouch; the certificate
import example passes an enum to the example helper's boolean parameter. Examples need named
cancellation arguments, real prerequisites, full resource scopes, metadata/result guards and disposal
of returned certificates/host keys. PivTool result objects provide no disposal mechanism; recipients
own certificate disposal. DecryptionResult publishes ReadOnlyMemory even though the SDK transfers
writable Memory. Safe plaintext clearing needs an implementation fix there; favor a direct SDK
Memory example and preserve the rule against casting read-only memory writable.

The generic signing helper's RSA/Ed25519 framing bug (PIVIMP-005), GUI certificate export's wrong
field, CSR's missing PIN verification, cancellation messages and lifetime gaps (PIVIMP-006) need
truthful local qualifications while body fixes remain deferred. The actual self-sign/CSR generators
delegate signing to the device; a public-key CertificateRequest alone lacks access to that private
key. Defaults are evaluation credentials. Persisting random management keys needs secure escrow;
console display alone supplies no secure recovery mechanism.

**Dependencies:** tool README, example XML including implicit helpers/results, SlotMetadataExtensions,
PivSessionContention's XML sample (nonexistent SignAsync), and the shared raw-access example's
positional ct argument to the PIV factory. **Reopen when:** helper behavior, command parsing/help,
result ownership, callback signing or factory signatures change.

### PIV-C010 — Match hardware/test explanations to assertions and real eligibility

**Priority: medium/high.** The full 89-method packet is in hardware-test-plan.md and evidence JSON.
Current overclaims include X25519 agreement without an agreement call, Ed25519 import “verify”
without verification, Always policy with one sign, the ECDH workflow comparing no peer/device
secrets, “all slots” reset checking 9A only, a move proof using a certificate that never existed,
attestation checks without chain/key binding, and cross-key tests using nonempty signatures.
Existing strong oracles verify ECDSA/RSA and raw P256 ECDH; use those strengths in the test guide.

Decrypt tests claim rejection before any APDU while the public method reads metadata first, and
some attributes admit pre-5.3 firmware. The PSS header's randomized/untestable rationale is incorrect:
host VerifyData with Pss verifies randomized PSS signatures. Discovery comments describe already-fixed
“today” bugs. Cooperative hotplug tokens alone supply no native bound. Keep scopes, early skips and
actual filters visible. Runtime assertion/fixture fixes remain Pass2; this source review executed no
hardware commands.

**Dependencies:** affected test comments and CLAUDE/README maintainer verification. **Reopen when:**
test oracles, fixture gates, cleanup, cross-key binding or platform evidence changes.

### PIV-C011 — Complete public reference inheritance and scoped shared migration reconciliation

**Priority: medium.** Many concrete PivSession methods carry no XML/<inheritdoc/> while interface
contracts exist. Public reference pages should expose contracts at both entry points. Add appropriate
inheritance while preserving signatures. Metadata positional fields need roles, units and raw encoding
semantics at record declarations; generated methods require no repetitive boilerplate.

The shared migration guide renames PIV parameters to `currentPukUtf8/pinUtf8`, while current names
are `currentPuk/pin`. The comparison says PIV still uses the Utf8 suffix. The map links removed
Piv-prefixed paths/types and GetChuid instead of GetCardholderUniqueId; PIN-only notes exclude None
despite disable support. Update current PIV portions while preserving historical v1 names/examples.
The gap inventory's current PIV compression note still describes a boolean parameter, and “verified
improvement” wording overstates Bio/device qualification. Root README, v2 highlights and Native AOT's
bounded macOS PIV session evidence need no new feature claim. Any clarification of the PIV facade's
feature-local helpers in the architecture diagram stays local; Core pipeline machinery remains deferred.

**Dependencies:** concrete facade XML, Metadata, migration guide/map/gaps/comparison, PIV shared
usage and raw-access snippets. **Reopen when:** public surface, named parameters or qualification
evidence changes.

## Separate implementation and qualification ledger

These are **deferred Pass2** (or explicit decisions) rather than product edits authorized by this
report. Each item's documentation trigger is part of the future acceptance criteria.

| ID | Observed behavior / proposed investigation | Priority; documentation reopening |
|---|---|---|
| PIVIMP-001 | Reset starts PIN blocking with a Bio preflight TODO. Vendor Bio MPE supports a device-reset route; Python checks configured Bio first. Decide safe preflight/product routing and inspect mutation-before-error behavior on appropriate hardware. | High; C001/C006/C008 and reset examples. |
| PIVIMP-002 | PIN-only enable/disable are partial multi-APDU transitions. Raw PIN/key/retry changes leave ADMIN/PRINTED coherence to callers. Decide workflow/rotation/recovery contracts while preserving legacy derived users. The 6982-only restoration behavior is already fixed. | High; C002/C005/C006, migration recipe. |
| PIVIMP-003 | Firmware gates model 4.4.x FIPS; newer FIPS/product/NFC restrictions remain device-enforced. Serial 5.0.0 versus Python 5.0.3 and AES 5.4.0 versus released 5.4.2 need bounded qualification. The .NET ROCA 4.2.6 boundary matches the vendor PIV advisory; Python 4.2.0 is conservative. Preserve boundaries pending evidence. | Decision/qualification; C007. |
| PIVIMP-004 | Typed CHUID/CCC/KeyHistory readers reject broader valid encodings. KeyHistory always emits/requires F3 and accepts unconstrained counts/URIs; the full PIV profile requires conditional F3, retired-key counts/layout and a URL format. Decide general representation versus fixed provisioning scope. | Medium/high; C004 and migration map. |
| PIVIMP-005 | PivTool Signing.SignDataAsync hashes every algorithm, then performs a raw private operation: RSA receives a hash without EMSA padding; Ed25519 signs the hash instead of the original message. The generic selector also offers X25519. Add algorithm-specific framing/eligibility and independent host oracles. | High; C003/C009, tool README and Sign examples. |
| PIVIMP-006 | GUI export reads CsrPem while its helper returns Certificate. The CSR menu omits PIN verification. The read-only plaintext result prevents ordinary clearing. Generated/KDF source arrays lack explicit clearing; exceptions before credential finally scopes can leave copies. Callback generators block and omit cancellation; broad catches mislabel cancellation as timeout. Review certificate ownership/leaks and old-firmware HasKey=false reporting. | Medium/high; C006/C008/C009. Shared credential-reader implementation was outside this review. |
| PIVIMP-007 | Crypto response parsing checks tags but accepts empty/wrong-sized results; one-byte P256 fixture results demonstrate acceptance. RSA host unpadding uses BigInteger secret material without explicit internal clearing. Its “astronomically unlikely” modulus comment needs separate reasoning for valid zero-prefixed versus malformed blocks. Decide validation/error/memory requirements and bounded decompression/CertInfo interoperability. | Medium/high; C003/C004/C006/C008. |
| PIVIMP-008 | Hardware oracles, attributes and cleanup need focused improvements: high-level decrypt's metadata 5.3 mismatch; weak Always/X25519/import/attestation/cross-key assertions; WaitAsync/read tasks that can outlive failed assertions; persistent leftovers and cleanup that can mask primary failure. Add meaningful oracles and finite independent cleanup. | Medium/high; C010/hardware packet. |
| PIVIMP-009 | AdminData uses trimmed little-endian Unix seconds; pinned Python PivmanData uses fixed four-byte big-endian. Self-roundtrip tests miss this interoperability difference. Determine v1 compatibility and the desired format before migration. | Medium; C004/C005/AdminData comments and migration. |
| PIVIMP-010 | Raw object identifiers accept any int. EncodeObjectId truncates negative/over-24-bit input to wire bytes. Decide a modeled range and fail-fast request validation, separately from raw response forward compatibility. | Medium; C004/GetObject/PutObject XML and raw examples. |

## Deferred Core questions (PIV-local boundary notes only)

* **PIVCORE-001:** physical PIN state after PC/SC handle/session disposal and cross-process sharing.
  AuditV2.YESDK1630 is an explicit release/isolation-policy regression, separately from federal PIV
  requirements. Preserve its issue identity/provenance; reopen C006 with OS/share-mode/device evidence.
* **PIVCORE-002:** cooperative cancellation/drain, native-call interruption and multi-APDU workflow
  serialization. Authentication, recovery and reset span exchanges. Core's future boundary review
  should establish overlap/disposal guarantees beyond one APDU. This run reads no Core implementation
  and asserts no new transport defect. Reopen C005/C006/C010 accordingly.
* **PIVCORE-003:** owned ApduResponse.Clear/Tlv cleanup and borrowed-connection lifetime underpin
  PIV's clear/transfer behavior. Target existence and PIV call-site checks suffice here; Core's final
  cycle reviews shared implementation. Reopen C003/C004/C008 if those contracts change.

## Rebaseline of previous findings and unfinished work

Merged PR745 and prior fixes already address unknown absent-data VERIFY counts, narrow metadata
fallback, authentication invalidation on failed attempts/disposal, SET 6982 invalidation versus other
failure preservation, detected-firmware defaults independent of overrides, supplied-key authentication
before PIN-only writes, default-before-delete ordering, stale-derived candidate restoration, unknown
algorithm retention/extraction, explicit wire mappings/subsets, writable owned outputs, raw response
Clear and private-component working-copy cleanup. Commit 8a7d152d adds real witness validation in
mixed-recovery fakes and SPKI comparison in the metadata hardware test. Earlier 8a7860fa fixes raw
Ed25519 messages, short ECDSA left-padding and oversize RSA rejection. Earlier findings for these
behaviors are superseded/resolved code observations. Revised wording still needs normal review.

Genuine unfinished work remains: the reset Bio TODO, X25519 integration verification and the fixture,
profile and cleanup gaps above. Comment cleanup preserves those obligations.

## Scoped shared sections and link policy

PIV-relevant sections were read in root README, the package page, v2 highlights/comparison, migration
guide/map/gaps, user interaction, raw-access/app-public-API/PIV architecture and Native AOT evidence.
Current root/package overview and bounded AOT claims are clean; qualification upgrades remain outside
this pass. Shared example/migration corrections are C009/C011. Validation hosts need properly typed
prerequisites and complete scopes for credentials, prompts, certificates, peers and cancellation.
Empty placeholders supply little task validation. Cross-module targets receive existence checks only.

## Handoff

The coordinator can accept candidates, assign a fresh editor, execute the bounded hardware packet and
prepare snippet/tool compilation. Follow the approved direct, calm, precise, approachable voice:
affirmative behavior/task first; actor and conditions nearby; supported actions with consequential
limitations; rationale and resource scopes retained. Product code and Pass1/Pass2 sequencing remain
unchanged. Independent review, sampling, builds, tests, formatting, hardware qualification, publication
and commits belong to the coordinator and later phases.
