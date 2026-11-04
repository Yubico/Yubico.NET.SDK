# OpenPGP whole-module investigation

**Checkpoint:** investigator report, 2026-11-04; baseline
`fe180282a2d77b15eaaa5868d6c8b00e251d1f05`. Findings below are **candidates for coordinator
adjudication**, with source-confirmed observations distinguished from hardware and intent gaps.
Documentation, implementation, and hardware dispositions remain independent.

**Historical coordinator checkpoint received during investigation:** installed Python direct-session
`session.version` reports `0.0.1`; ykman CLI reports application `5.8.0`, card `3.4`, and device metadata
identifies a `5.8` beta device. Vendor certificate helpers reject ATT/DEC/SIG reads at their `>=5.2`
gate, while AUT reads absent. Other baseline metadata reports ATT key Imported, ordinary keys None,
all UIF Off, retries 3/0/3 and KDF None. Hardware mutation has **not started**: the separate probe owner
is completing the ATT public-certificate baseline using framing justified by actual device metadata
and installed vendor source. These observations are coordinator-supplied, not investigator execution.
See C021 for the bounded version-provenance question; keep physical-device metadata, applet response,
CLI interpretation, and effective SDK version policy distinct.

**Current coordinator checkpoint superseding that pause:** `hardware/vendor-baseline-v2.json` records
successful explicit public certificate/UIF APDU reads. ATT has a 793-byte certificate, DER SHA-256
`94afb703099edb4aaaee443c6e197c379f7f909d23f717f0868ef73057e4a16a`; ordinary keys are None and
certificates absent. All four actual UIF reads report Off. The helper preserved direct version 0.0.1,
selected certificate framing from fresh serial-103 metadata `5.8.0.beta.0`, and applied no global
version override. Earlier Python `get_uif` Off values were synthesized by its low-version branch;
the corrected readback now establishes actual DO values. The coordinator has started 48 non-KDF
methods including both Slow cases after the baseline; no outcome is claimed here. The KDF case is
ready for isolated execution, with C002's comment/transition distinction preserved. Seven supplemental
compilation groups are reported warning-free with actual Csc evidence by their independent owner;
this investigator has neither run nor independently reviewed those compilation gates.

## Scope and method

Read root `CLAUDE.md`, module/test instructions, `docs/TESTING.md`, campaign workflow, approved style,
`docs/SDK-HOUSE-STYLE.md`, RUN plan/status/inventory, then **all 96 inventory paths in full**: 44 library
C#, 19 tool C#, 14 unit C#, seven integration C#, four Markdown, four project, two API snapshots,
and two runner files. The semantic read included executable bodies, XML inheritance, internal helpers,
commentary, assertions, implicit `session` calls, exception paths, cache updates, and resource scopes.
No product changes, builds, tests, formatter, hardware operations, commits, or delegation were performed.
The Python scripts under `evidence/` acquire references and generate research manifests only.

`coverage.json` records every file, public type and interface operation, clean-unit nominees, shared
sections, and exclusions. `hardware-test-plan.md` inventories all 49 integration methods with filters
and actual setup/body/cleanup effects. Tooling validation belongs to the separate `validation/` owner.

Editorial acceptance: lead with behavior and actor, then actionable prerequisites, ownership, failure
conditions and rationale. Retain consequential limitations beside the supported action. Avoid repeated
“does not”, “cannot”, emphatic “NOT”, and rhetorical “X, not Y”. Public prose should help the reader
complete a task; keep this investigation's history and candidate IDs in research records.

## Surface reconciliation

- **39 distinct effectively public shipping types**, including public `Crt` and `DataObject`.
  `PrivateKeyTemplate`, `RsaKeyTemplate`, `RsaCrtKeyTemplate`, `EcKeyTemplate`, `PrivateKeyImport`,
  backend types, `BcdHelper`, `DigestInfo`, and `Ins` are internal. Public members nested inside an
  internal type remain internal for this count. Module `CLAUDE.md` currently reverses two categories.
- **423 nonblank/non-directive API snapshot entries**; `PublicAPI.Shipped.txt` contains only the
  nullable directive. Snapshot entries include types, enum constants, constructors, accessors, static
  fields, abstract/override members and extension representations. These are not 423 independent
  prose tasks. The class's seven partial declarations form one type; its 40 operation implementations
  inherit interface prose. Property get/init entries share their property contract. Implicit constructors
  share the owning model's initialization contract. The C# extension container, extension declaration,
  extension member and lowered static method describe one public session factory.
- **40 declared `IOpenPgpSession` operations**, mirrored by the concrete session. Inherited Core session
  members are a cross-module boundary, rather than extra OpenPGP operations. Factory and extension
  creation contracts are additional semantic units. Internal existing-connection extension is not public.
- Models, format helpers, credential-option factories, firmware fields, collection contracts and
  protocol enums each received semantic review; parser and serializer contracts are included even when
  test coverage is sparse. Preserve snapshots in Pass 1.

## Original-source evidence and bounds

Full originals and focused Python history patches are cached in `evidence/`; URLs, access date and
hashes are in `evidence/source-manifest.json`. Read `evidence/source-analysis.md` for exact section,
actor, normative strength and firmware provenance. Original standards take precedence for their own
layer. Python/vendor code corroborates behavior; agreement between SDKs alone establishes no standard.

The filename `OpenPGP-smart-card-application-3.4.pdf` currently contains **Functional Specification of
the OpenPGP application on ISO Smart Card Operating Systems, Version 3.4.1, Achim Pietig, March 18,
2020**. It is an application functional specification, not an IETF RFC or a certification of this SDK.
Read §§4.3, 4.3.2, 4.4.3.4, 4.4.3.6–10, 4.4.3.12, 7.2.2–5, 7.2.10–11,
7.2.14–17 and 7.9 for the consequential claims below. Yubico's attestation extension defines ATT
separately. RFC 9580 §3.7.1.3 supports S2K byte-count semantics; its OpenPGP packet framing and
algorithm policies are not an automatic upgrade of the card's RFC 4880-era profile.

Pinned Python repository: `Yubico/yubikey-manager` at
`4ca60f706af930459138d8dc0f0f953480e1c7a4`. Read the relevant source flows in `yubikit/openpgp.py`,
the CLI attestation flow, complete `tests/device/test_openpgp.py` and CLI tests. Cached source is
reproducible at the pinned revision; it is not the installed ykman version or a claim that those tests
were executed here. Context7 supplied current API orientation; original source/history settled claims.

Initial acquisition plus one focused history/vendor follow-up was used. Exact commercial introduction
and every historical firmware's behavior remain bounded gaps where originals diverge or evidence is
absent. The coordinator's current 5.8 USB key can qualify that scope, not NEO, 4.x, 5.2.x, NFC, other
operating systems, FIPS devices, or Native AOT execution.

## Candidate ledger and documentation dependencies

Each `PGP-Cnnn` is stable. Proposed implementation IDs (`PGP-IMP-*`) and dependency IDs
(`PGP-DEP-*`) are recommendations for the coordinator's durable Pass-2 ledger; no item is implemented.
For every entry the **reopen trigger** is evidence of changed behavior or an agreed contract, followed
by independent review of the listed documentation surfaces. Source locations are baseline locations.

### PGP-C001 — Keep User PIN identity separate from VERIFY mode

**Source-confirmed; high; proposed PGP-IMP-001 / PGP-DEP-001.**
`OpenPgpSession.Pin.cs:31–38` uses `Pw.Reset` both as P2=82 and as the credential identity passed to
`VerifyPwAsync`. `Kdf.GetSalt` maps that to `SaltReset`; `ResolveRemainingRetriesAsync` selects
`AttemptsReset` for 6982. Both 81 and 82 verify PW1, sharing the User PIN and its retry counter
(Card §7.2.2). Python `_verify(PW.USER, pin, mode=1)` derives PW.USER and adds mode only to P2.
With distinct user/reset salts, extended verification sends the wrong derived bytes and consumes a
User PIN retry; on 6982 the exception can report Reset Code attempts instead. The unit test
`VerifyPinAsync_ExtendedWhenSecurityStatusNotSatisfied_FallsBackToResetAttemptsFromPinStatusQuery`
explicitly enshrines that current mismatch. `Pw.Reset` describes a Reset Code, not a second User PIN.

**Pass 1:** explain the actual separation and qualify the affected KDF/error path without presenting it
as intended policy. Align attestation examples/comments with their evidenced authorization route,
while retaining any separately demonstrated extended-mode success; the source difference alone
establishes no exclusive-mode requirement for attestation.
**Intended Pass 2:** derive/retry-report PW.User for both User PIN modes; retain P2=82 for extended
authorization. Regression with distinct salts and distinct counters, plus recorded hardware probe.
**Reopen:** `Pw.cs`, `IOpenPgpSession` PIN contracts, `.Pin.cs` helpers, KDF guidance, retry exception,
affected wire-test explanation, README PIN/crypto and migration exception notes.

### PGP-C002 — Explain KDF provisioning as a persistent credential transition

**Source-confirmed mismatch; high; PGP-IMP-002 / PGP-DEP-002.**
`SetKdfAsync` writes F9 and immediately caches the supplied object. `ChangePwAsync` derives **both**
current and new PIN using that cached KDF. `OpenPgpAdvancedTests:80–87` says the old PIN is raw but
calls precisely this deriving method. Python `set_kdf` docs/tests show YubiKey resetting User/Admin
credentials to initial values and invalidating the Reset Code, followed by direct verification.
Card §4.3.2 assigns alignment of KDF and credentials to terminal software and says card commands do
not evaluate F9; therefore distinguish the generic standard from YubiKey-specific transition behavior.
The existing test's comments are wrong regardless of whether its equal-old/new calls happen to pass
on a device already reset to derived initial values.

**Pass 1:** explain capability, initial hashes, changed credentials, invalidated reset code, and a tested
provisioning sequence only after isolated device evidence. **Pass 2:** decide a supported high-level
transition contract and meaningful same-/fresh-session enable/change/disable tests. Hardware execution
must isolate this method and restore/read back state even after failure. **Reopen:** Config/Pin/KDF XML,
README configuration/recovery, Advanced test comments and any migration recipe for KDF.

### PGP-C003 — Make KDF cache ownership and serialization lifetime explicit

**Source-confirmed; high; PGP-IMP-003 / PGP-DEP-003.**
`GetKdfAsync` returns the cached **same instance**; `SetKdfAsync` retains the caller's object;
`Dispose(bool)` disposes it. A caller disposing a returned/input `KdfIterSaltedS2k` clears its salts
while the session still uses it. Replacing KDF or `ResetAsync` drops the old reference without
disposing it. `SetKdfAsync(kdf.ToBytes())` creates a serialized array containing initial PIN hashes
without an explicit clearing scope. `PutDataAsync(F9,...)` and raw writes can leave cached KDF stale.
Refreshing ApplicationRelatedData clears UIF overrides but leaves KDF unchanged. The comment
“Invalidate cached KDF” actually installs `kdf`. Wire tests verify disposal of a cached salt and base
teardown after a throwing custom KDF; they establish no coherent borrow/transfer policy.

**Pass 1:** state current alias/lifetime behavior carefully; offer an actionable lifetime scope rather
than invent caller-owned clones. **Pass 2:** choose borrowing/transfer/cloning contract, dispose replaced
owned values, clear serialized credential-hash payloads, and define cache invalidation for raw writes.
**Reopen:** Kdf type/property/Process/ToBytes XML, session Get/SetKdf and Reset XML, module security notes,
configuration examples, dispose/cache comments and wire-test explanations. Core async-dispose dispatch
is record-only pending the final Core boundary review.

### PGP-C004 — Preserve the attestation exception in reset and fixed-UIF guidance

**Original-vendor confirmed; high; PGP-DEP-004 (documentation).** README:189 says reset erases every
key/certificate/setting; tool Reset prose/help strings repeat ALL. Vendor Attestation and CLI originals
state that the ATT key/certificate survive reset; vendor touch guidance says ATT fixed policy requires
replacing the ATT private key to change it. `Uif.IsFixed` promises factory-reset recovery for every
slot. For ordinary slots, replacing the associated key can also clear fixed UIF. The generic Card
§4.4.3.6 reset wording covers its own profile, while ATT is a Yubico extension.

**Pass 1:** describe reset scope and ATT persistence prominently, alongside irreversible ordinary-key
loss and the supported recovery paths. Preserve executable CLI strings in this pass; explicitly warn
readers that those strings overstate scope. **Reopen:** README constraints, interface Reset, `Uif.cs`,
module guidance, tool Reset/KeysSetTouch comments and narrative sample caveats. Hardware final readback
must preserve ATT certificate identity and separately record UIF; never replace the factory ATT key
merely to satisfy coverage. Factory attestation trust cannot be recreated from a saved public certificate.

### PGP-C005 — Check reset statuses and distinguish attempted reset from completed reset

**Source-confirmed; high; PGP-IMP-005 / PGP-DEP-005.** Reset ignores TERMINATE and ACTIVATE status
words, then reads initial state/application data and logs completion. `BlockPinAsync` assumes its
initial number of invalid attempts blocks each PIN; it only early-stops on 6983 and otherwise logs
“exhausted” even for unrelated failures. No postcondition verifies erased keys, default credentials,
or counters. A successful reselect/read alone can mask a refused reset. Python checks E6/44 responses.
**Pass 1:** describe command sequence and partial failure consequences; qualify completion claims.
**Pass 2:** status/postcondition policy with fake-response regressions; keep bounded recovery after
cancellation or partial PIN blocking. **Reopen:** `.Reset.cs`, Reset XML, README reset, helper guidance,
reset test commentary and sample reset limitations. Hardware full-state readback is mandatory.

### PGP-C006 — Describe the algorithm-specific signing contract

**Source-confirmed; medium; PGP-DEP-006 (documentation).** `.Crypto.cs:157–163` hashes RSA/ECDSA
input but passes EdDSA message bytes unchanged and ignores `hashAlgorithm`. README:126 and the interface's
unsupported-hash exception imply universal hashing. RSA returns raw modulus-sized signatures; ECDSA
returns DER `(r,s)`; Ed25519 returns raw 64-byte signatures. Card §7.2.10 and RFC 8017 §9.2 separate
host DigestInfo construction from device PKCS#1 v1.5 padding. RFC 8032 §5.1.6 explains Ed25519's own
signing algorithm; it is not Ed25519ph selected by passing SHA256. Python source and host-verifying
device tests corroborate this distinction.
**Pass 1:** state all three input/output contracts locally in Sign/Authenticate and README; include
correct authorization states and host verification example. **Reopen:** crypto XML/README if prehashed
overloads, supported algorithms, input semantics, or response encoding change.

### PGP-C007 — Keep decipher input at the raw key-operation layer

**Source-confirmed; high; PGP-IMP-007 / PGP-DEP-007.** README reads `message.gpg`, inviting a complete
OpenPGP packet/file into a raw private-key operation. RSA expects one RSAES-PKCS1-v1_5 ciphertext block;
the SDK prepends 00 and the card checks/removes padding. NIST ECDH takes `04 || X || Y`, wraps
`A6 {7F49 {86 point}}`, and returns raw shared-secret material for caller KDF/content decryption.
For X25519 the current formatter passes raw input **without the cipher DO wrapper**. Python wraps
all EC inputs; Card §7.2.11 defines A6 for ECDH, with X25519 encoding supplied by the vendor profile
and RFC 7748. Existing .NET tests check X25519 import/public key, not X25519 agreement.
**Pass 1:** remove file-decryption implication, use composable typed raw input and clear plaintext/shared
secret in `finally`; qualify current X25519 limitation pending hardware. **Pass 2:** exact X25519 APDU
and independent RFC/host agreement. **Reopen:** Decrypt XML, crypto helper, README example/security,
unit wire expectations, X25519 integration guidance and applicable migration references.

### PGP-C008 — Explain transferred signature storage accurately

**Source-confirmed; medium; PGP-DEP-008 (documentation).** README:196 promises raw signature responses
are cleared. `FormatResolveAndTransferSignatureAsync` deliberately transfers the raw backing response
for RSA/EdDSA on success; ECDSA gets new DER storage and clears raw response. All abandoned buffers are
cleared on formatting/resolution failure. Wire tests:605–676 assert both branches. Decryption resolves
completion before copying plaintext and always clears raw storage. **Pass 1:** explain result ownership
and returned-memory clearing without claiming every raw signature buffer is already cleared.
**Reopen:** response-transfer helper, crypto result XML/security narrative, wire-test commentary whenever
copy/transfer or prompt-resolution policy changes.

Because results use `ReadOnlyMemory<byte>`, a cleanup example must clear the returned backing storage
through a supported mutable-memory view where appropriate; clearing only a newly created `ToArray()`
copy leaves that original storage intact. Have the editor's compilation owner check the chosen recipe.

### PGP-C009 — Explain multi-command mutation and stale cached algorithms

**Source-confirmed; high; PGP-IMP-009 / PGP-DEP-009.** Generate/import set attributes before generating
or importing; delete writes RSA4096 then RSA2048. Attribute changes can remove/render unusable an
existing key (Card §4.4.3.9). A later failure or cancellation leaves persistent partial changes, and
refresh failure can occur after successful private-key mutation. `SetAlgorithmAttributesAsync` and raw
PutData leave `_appData` unchanged, whereas Sign/Decrypt/Authenticate format using cached attributes.
Generation/import/deletion refresh only after success. The SDK performs no transaction or rollback.
**Pass 1:** place destructive/partial-success warnings beside these operations, describe explicit refresh
or fresh-session inspection after mutation, and distinguish slot key data from certificate/metadata.
**Pass 2:** decide validation, cache updates, readback/error representation and rollback boundaries.
**Reopen:** key/config/raw APIs, README key tasks/constraints, mutation-refresh test explanations.

### PGP-C010 — Qualify firmware gates and device capabilities

**Source-confirmed; high; PGP-IMP-010 / PGP-DEP-010.** SDK feature constants are present for reset
1.0.6, UIF 4.2, EC/certificates/attestation/algorithm info 5.2, SELECT DATA fix 5.4.4 and unverify 5.6.
These are **SDK thresholds**, rather than a blanket guarantee of functionality. Cached/ATT UIF lacks
Python's 5.2.1 subgate; SetPinAttempts lacks Python's NEO1.0.7/other4.3.1 gate and NEO default-PIN
effect; GetChallenge lacks advertised capability/max-length checks. Raw and several config paths
defer rejection to the card. `GetSupportedAlgorithms` requires5.2 instead of Python's synthesized old
RSA list. Generic “all firmware-gated operations fail before any APDU” and “basic available1.0.0”
overstate evidence: 1.0.0 is a **version-query fallback**, not a verified minimum device qualification.
Vendor technical manual names commercial5.2.3 while historical Python gates name5.2.0/5.2.1.
**Pass 1:** label thresholds as current checks, keep card capability/family/profile conditions nearby,
record old-firmware qualification gaps. **Pass 2:** settle capability enforcement and family/version
policy. **Reopen:** constants/interface feature docs, README requirements, module table, challenge/retry/
UIF/config docs and algorithm discovery comments.

### PGP-C011 — Preserve the ROCA rationale and exact test execution boundary

**Source-confirmed; high; PGP-IMP-011 / PGP-DEP-011.** Four integration methods return normally for
4.2.0 through **4.3.5 inclusive** before setup: RSA generate, RSA sign, RSA import/sign, RSA decrypt.
Their bodies execute zero commands and assertions in that range despite runner success. Python excludes
`[4.2.0,4.3.5)` for on-device generation; vendor YSA-2017-01 says4.3.5 uses corrected generation and
externally generated/imported keys are a mitigation. The C# “generation unreliable” comment obscures
the security origin, and the same blanket gate on imported RSA has no support from that advisory.
Shipping GenerateKey currently lacks the Python ROCA guard.
**Pass 1:** explain actual early-return boundary and provenance without calling the omitted body
verified or silently editing executable conditions. **Pass 2:** agree shipping generation guard and
test applicability/skip reporting; preserve imported-key mitigation. **Reopen:** `.Keys.cs`, integration
comments, hardware matrix, firmware requirements and generation task guidance.

### PGP-C012 — Scope attestation evidence and prerequisites

**Original/source confirmed; high; PGP-DEP-012 (documentation).** Existing warning about target
certificate overwrite is correct and must remain. User PIN **signature mode** is used by Python CLI
and .NET test, while `.Pin.cs` comment lists attestation under extended mode. These routes differ;
the source references establish ordinary-mode use, not proof that extended mode is rejected. The
coordinator's supplemental attestation probe uses extended mode; retain its actual result separately.
ATT's UIF
controls the private-key operation. The vendor extension supports targets Sig/Dec/Aut generated on
device, writes their7F21 slot before retrieval, and defines no automatic certificate-chain validation
by this SDK. Reading may fail after irreversible overwrite. Returned X509 objects need disposal.
Host identity/trust verification requires public-key comparison and CA-chain validation; mutable
name/fingerprint/time extensions are not independent provenance proof. Read back existing certificate
before attestation if it must be restored, then use Admin authentication to restore it.
**Reopen:** Attest/interface/README/user-interaction/tool comments and attestation-test explanations
if authorization, supported targets, output retrieval, or verification behavior changes.

### PGP-C013 — Encode the ATT control reference template as a nested TLV

**Original/source confirmed; high; PGP-IMP-013 / PGP-DEP-013.** `Crt.BuildAttCrt:52–54` passes
`inner.Value.Span` into B6, producing **B6 01 81**, whereas Yubico's original attestation extension
specifies **B6 03 84 01 81**. ATT read/generate/import use this public CRT; ordinary slot wire tests
exclude ATT. This is separate from target-key attestation (FB), which does not use this CRT.
**Pass 1:** document ATT operation limits without promising successful provisioning. **Pass 2:** exact
wire regression and public read-only ATT query where possible; replacing the factory ATT key is
unnecessary to prove the encoder. **Reopen:** `Crt`, `KeyRef`, key APIs/module model guidance and tests.

### PGP-C014 — Reconcile NEO key format and delete semantics

**Source-confirmed compatibility candidate; high; PGP-IMP-014 / PGP-DEP-014.** PrivateKeyImport chooses
`RsaImportFormat.Crt` (02) on major1–3, while the constructed template includes modulus N (tag97).
Card §4.4.3.9 distinguishes02 from03 (CRT with modulus); Python chooses03 for NEO. Generation always
writes supplied attributes; README defaults to standard even for older devices. Delete always uses
attribute switching, whereas Python overwrites with an RSA2048 key on NEO. Source-confirmed mismatch
does not establish which old devices tolerate it. **Pass 1:** qualify untested old-family behavior and
correct internal claims that current delete is universally Python-equivalent. **Pass 2:** fake-wire
regressions plus dedicated old-family evidence before promising compatibility. **Reopen:** import/key
XML, internal import/delete comments, requirements and provisioning examples.

### PGP-C015 — Keep OpenPgpTool instructions honest about its current implementation

**Source-confirmed; high; PGP-IMP-015 / PGP-DEP-015.** `KeysAttestCommand` declares `--pin` but never
reads/prompts/verifies it; a fresh command session calls Attest directly. It also leaves returned cert
undisposed. Certificate import/export likewise omit certificate disposal. `InfoCommand` infers key
presence from fingerprint **length**, including zero-filled reset fingerprints; use SDK key-information
results for reliable status. Command-line PIN strings and PEM read-as-string remain immutable;
clearing the temporary encoded buffer does not erase their strings or process-argument exposure.
Noninteractive selection picks the first SmartCard device; no serial selection safeguard is present.
The alternate `OpenPgpDeviceSelector` is defined but the command base uses static `DeviceSelector`.
No presence prompt is installed by the tool. Existing `--force`/confirmation coverage varies by command.
**Pass 1:** explain current sample limitations and safer direct-SDK task path; preserve help strings and
runtime bodies for Pass2. **Pass 2:** honor PIN option, dispose certs, correct presence/status reporting,
decide secret input and deterministic device selection policies. **Reopen:** tool comments, module
Example section and any examples that present tool output as proof of device state.

### PGP-C016 — Match test explanations to actual assertions

**Source-confirmed; medium; PGP-IMP-016 / PGP-DEP-016.** SessionTests
`Sign_EcP256_ProducesValidSignature` only asserts nonempty signature/public-key buffers; its “Verify
the signature” comment performs no host verification. RSA/Ed25519 generation/sign tests mostly assert
length. Import RSA/P256/P384 and Aut tests actually verify signatures with host keys; P256 decipher
compares raw host agreement. Import Ed/X25519 tests compare RFC public keys only. Attest validity test
checks nonnull/nonempty cert; the overwrite test checks auth failure and certificate byte replacement,
not CA trust or key binding. Reset tests assert three retry counters only. A successful result supports
only the assertions actually reached. Unverify only probes signature state; signature-policy test
only reads policy bytes. Four-slot metadata tests are not gated for historical three-slot devices.
**Pass 1:** correct inflated explanations, preserve assertions, describe meaningful existing evidence.
**Pass 2:** stronger host crypto, authorization and reset-state tests where useful; no coverage-only
assertions. **Reopen:** all affected test commentary and any public qualification based on those tests.

### PGP-C017 — Complete the task flow and resource scopes

**Source-confirmed; medium; PGP-DEP-017 (documentation).** README has discovery, PIN, generation,
sign/decipher and short attestation sketches, but lacks usable change/recovery, private-key import,
raw-public-key decoding, metadata, certificate CRUD, configuration and reset workflows. Attestation
fragment uses undefined `userPin`, lacks X509 import and disposal; snippets after introduction assume
different local session lifetimes ambiguously. Decryption result remains uncleared. Retry count may
be null although README lists only positive/zero. Public raw APIs and most one-line interface
contracts omit auth conditions, bytes/units/output encoding, disposal, persistent effects and useful
failure context. Package description claims encryption although the public operation is decipher/
agreement; host applications perform content encryption.
**Pass 1:** develop coherent composable fragments with typed prerequisites and complete owned scopes;
distinguish factory defaults used for a dedicated example from production PIN acquisition. Explain
caller-owned `IPrivateKey` disposal without pretending it is a borrowed ROM parameter. Keep firmware,
destructive consequences, transport and cache conditions local. Compile under independent tooling.
**Reopen:** all task/API/package surfaces whenever the respective deferred contract changes.

### PGP-C018 — Align model and internal explanations with wire reality

**Source-confirmed; medium; PGP-DEP-018 (documentation), PGP-IMP-018 for parser policy decisions.**
DataObject.Language says two bytes; Card §4.4.3.4 permits1–4 language pairs (2–8 bytes). Composite-tag
comment calls the entire group unaddressable, but C0/C5/C6/CD/DE handling varies and direct C4 is
mandatory; identify container versus nested fields individually rather than infer prohibition from
enum placement. SignatureCounter says since reset, while Card §4.4.3.12 also resets it on signature
key import and §7.2.14 resets it on signature-key generation. OpenPgpAid invalid BCD fallback casts
uint32 to int then negates; high-bit cases can yield positive values, so “returned as a negative
number” is too broad. `GetOidBytes` returns the DER **value octets**, not a complete tag/length/OID.
Credential-option factories supply reader limits, rather than device-enforced universal character
limits; UTF-8 bytes and model/profile limits matter. `PwStatus` currently treats encoded length bytes
literally and unknown policy asOnce; Card byte-format bits and future identifiers need explicit
parser policy. Required algorithm parsing rejects unknown IDs/OIDs and optional fields are dropped;
GetData provides a raw escape hatch but initialization still requires understood algorithms.
The fingerprint setter accepts exactly 20 bytes and generation/import leave fingerprint calculation
to the host. A citation to RFC 9580's S2K primitive supplies no support for switching this card API to
newer packet/fingerprint formats. Ed25519 import/test prose should call the RFC 8032 input a seed,
distinguishing it from the expanded/pruned scalar used inside the signature algorithm.
**Pass 1:** document current parse/encoding scope, useful units and fallback behavior; remove historical
ticket tokens/T12 and “upstream on this branch” narration. **Pass2:** agree strictness and forward
compatibility without silently widening security-sensitive request encoding. **Reopen:** model XML,
parsers, CLAUDE architecture/model tables, internal wire comments and relevant tests.

### PGP-C019 — Bound PIN exception claims to the methods and statuses actually handled

**Source-confirmed; medium; PGP-IMP-019 / PGP-DEP-019.** Exception summary includes Reset Code
verification, but only VerifyPin/VerifyAdmin create it. Change/reset operations propagate general
ApduException. Every non-OK VERIFY response is converted, including unrelated command/SM failures;
6982 can mean wrong/unverified PW **or SM failure** in Card §7.9. Only6982 triggers status fallback;
other unexpected responses yield null without a failed query, contradicting “null only when fallback
also failed”. “Permanently blocked” hides supported User PIN recovery. **Pass1:** state current
mapping and uncertainty; retain SW for diagnosis, avoid automatic retry advice that spends attempts.
**Pass2:** choose classified versus broad mapping and retry-aware change/reset behavior. **Reopen:**
exception/PIN XML, helper comments, wire tests, README recovery and migration `openpgp-exception`.

### PGP-C020 — Keep shared OpenPGP claims current without expanding the audit

**Source-confirmed local mismatch; medium; PGP-DEP-020 (documentation).** Migration OpenPGP section
asks readers to review “management key behavior”, a PIV concept absent from this module's contract.
Use Admin PIN and optional secure-channel terminology with actual task link. README/highlights/
comparison OpenPGP package claims are otherwise narrow; link highlights directly to the module guide
if useful. Guide current-return/result tables match source; map's existing OpenPGP entry also labels
v1 OpenPGP APIs while highlights/comparison say first/new in v2. Historical v1 source was outside the
assigned investigation, so record the chronology discrepancy as a bounded verification question
rather than guess that either history claim is true. Gaps document explicitly mixes initial historical
snapshot claims with current remediation notes; preserve historical meaning and qualify current
OpenPGP exception statements where they still appear current. User interaction's OpenPGP policy
description matches cache logic but benefits from a module link explaining ATT scope, fallback and
terminal/drain recovery. Architecture transport/module dependency and NativeAOT link-only limitations
match this module; avoid converting future JIT hardware success into AOT execution evidence.
**Reopen:** assigned shared sections after any C001–C019 behavior correction; leave other-module source
and docs untouched. Existing DOC-002 map parser issue remains separately owned.

### PGP-C021 — Preserve version provenance on development devices

**Coordinator observation; compatibility question; proposed PGP-DEP-021, record-only Core boundary.**
The installed direct Python session reports `0.0.1` while its CLI and device metadata identify
application 5.8.0/card 3.4 and a 5.8 beta device. Its helper's certificate feature rejection occurs
before the certificate read and establishes no certificate absence. This observation is distinct
from Python source-history evidence explaining stable-firmware gates. The pinned source's
`c910e5d25ba675ac2bfc97d35f14a4cc0314864f` explicitly concerns overriding version checks for 0.0.1;
installed revision and CLI setup must be recorded before applying that precedent to this run.
The OpenPGP backend reads applet GET VERSION and passes the result or a supplied creation override
into shared protocol/session configuration. Whether Core's effective development-firmware policy
already handles this device remains a deferred boundary question, **not an established SDK defect**.
The separate probe owner completed the public-certificate correction in `hardware/vendor-baseline-v2.json`:
actual ATT certificate and all four UIF DO reads succeed, with direct 0.0.1 retained and framing chosen
from fresh 5.8 beta device metadata. This report authorizes no concurrent device query or product edit.
Reopen module firmware/factory guidance only
after comparing observed applet bytes, device metadata, installed vendor setup, and effective SDK
feature policy. Record the Core question for the final cross-applet review, without investigating
Core source now.

## Clean units and historical exclusions

Nominees in coverage are **investigator-clean, unsampled**, with no implication of hardware execution.
They span public collection/property contracts, package narrative, README metadata task, ordinary slot
CRT/KeyRef mappings, internal DigestInfo encoding and model/wire tests. Coordinator must remove any
edited overlaps from the final clean denominator, sample at least10% (minimum3) across API/narrative/
internal-test categories, and expand a category after a material miss. File read-complete is separate
from clean-unit verdict. Some files deliberately contain both finding and clean units.

No TODO/FIXME/HACK/XXX markers were found in current module C#/Markdown. This is a search observation,
not completion evidence: unsupported transitions, wire gaps and test deficiencies remain in this ledger.
License boilerplate, API snapshot syntax, generated compiler/accessor representations and build-runner
metadata are accounted for without artificial prose. Historical changelog/planning/research claims
outside assigned active sections retain their original meaning; link them as context rather than
rewrite past evidence. Other-module targets received link-existence checks only.

## Coordinator handoff

Adjudicate the candidates and dependency IDs, then give a fresh editor ownership of all accepted module
and shared OpenPGP documentation changes. Preserve runtime bodies, assertions, directives, snapshots,
help strings and unrelated work. Obtain original-citation re-review for consequential changes. Hardware
priority: C001 distinct KDF salts/counters, C002 isolated transition, C007 X25519 agreement, C004/C005
reset scope/readback, C012 attestation auth/overwrite/trust boundaries, then presence/cancellation and
fresh-session recovery. The supplied plan authorizes these operations on the connected dedicated key;
coordinate touch/timing, record irreversible effects and recovery limits, and keep all execution with
the coordinator. Reopen this report's current/intent distinction after late device evidence.
