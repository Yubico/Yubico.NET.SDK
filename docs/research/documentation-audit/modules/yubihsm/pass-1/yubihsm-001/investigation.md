# YubiHSM whole-module investigation

Investigator: GPT-6.1 Sol, report-only; 2026-11-04. Baseline
`a74792facb657413e98223e059876d8b213ffff3`, primary checkout. Read root/module/tests instructions,
`docs/TESTING.md`, campaign workflow/style, RUN plan/status/inventory. The plan already contains the
required affirmative/task-oriented editorial supplement at lines27–34. No plan edit was needed or owned.
No product edits, builds, tests, formatter, hardware, commits or delegation were performed.

## Coverage and accessibility

Read all 56 tracked module paths fully: 10 library C#, 22 example C#, 12 test/helper C#, four Markdown,
four project files, two API snapshots and two runner files. `coverage.json` records per-file disposition,
semantic families and candidate clean units. Directory glob included existing generated outputs; these
were excluded from investigation rather than counted as tracked source. Source inspection includes
all bodies, implicit session calls, comment regions and unfinished-work/development residue.

Eight effective public named types: `HsmAuthAlgorithm`, `HsmAuthAlgorithmExtensions`,
`HsmAuthCredential`, `HsmAuthRetryException`, `HsmAuthSession`, `IHsmAuthSession`, `IYubiKeyExtensions`,
`SessionKeys`. The two backend types and AssemblyInfo are internal. Public members inside internal
types add no public shipping surface. Example/test public types belong to nonshipping executables/tests.

`PublicAPI.Shipped.txt` contains only the nullable directive. Unshipped has 83 non-directive entries:
eight named-type entries, two extension-block entries, 15 interface operation signatures duplicated by
15 class implementations, generated record members/accessors/operators and duplicate lowered extension
representations. Review 15 operation contracts once at the interface plus actual implementations; review
four extension semantic operations once (two algorithm properties/two IYubiKey methods). Record synthesis
needs no invented boilerplate prose, but equality/sorting semantics warrant explicit explanation. Six
public feature fields include one obsolete alias; class factory and SessionKeys parser/lifecycle remain
separate authored contracts. API count is a snapshot count, not 83 independent documentation tasks.

## Findings — observed behavior versus supported documentation

All HSM-C IDs are documentation observations, not assertions that underlying behavior is intended.
`accepted-doc-change` below means an investigator recommendation pending coordinator/editor review.
Original claims and authority limits are captured in `evidence/sources.md` (S1–S6, GAP-SPEC-1).

### HSM-C001 — Retry recovery and deletion semantics (high, accepted-doc-change)

Observed: `HsmAuthCredential.cs:23–28` describes decrement and deletion, then speculates that success
leaves the counter unchanged. S2 explicitly says a correct credential password resets retries to eight
before deletion; a correct management key resets management retries before lockout. These are different
failure consequences: credential reaches zero → deletion; management reaches zero → management
operations blocked until destructive applet reset. `HsmAuthRetryException.cs:47–48` conflates these as
“credential or management key is locked out”. README omits management recovery and successful resets.

Propose: state applet counter behavior calmly, explain that LIST returns a snapshot, correct the
credential-deletion/management-block distinction, explain single-attempt SDK operations and application
retry decisions. Preserve prior one-failure hardware observation in historical records, rather than API
XML. Success-reset hardware proof is a proposed coordinator probe, not completed evidence. Reopen current
map rename notes when new hardware evidence arrives; preserve historical claims with explicit scope.

### HSM-C002 — Typed retry coverage excludes GET_CHALLENGE (high, accepted-doc-change + HSM-IMP-001)

Observed: session `GetChallengeAsync:601–606` uses ordinary backend Send with default error throwing;
it bypasses `TransmitWithRetryCheckAsync`. Other named management/password operations parse 63Cx into
`HsmAuthRetryException`. README:158–159, exception summary:25 (“every retry-counted ... operation”),
retry test class summary:23 and current migration-map uniformity claims overgeneralize. On password-capable
firmware GET_CHALLENGE may authenticate; S4 says the password is needed for asymmetric credentials.
No fake/hardware test here establishes typed GET_CHALLENGE errors. Desired consistency is an implementation
decision, not something Pass1 prose may pretend is already enforced.

Propose: scope typed claims to listed operations; describe GET_CHALLENGE protocol failures as
`ApduException`, inspect SW retry pattern if needed, and avoid encouraging blind retries. Add a precise
exception note/remarks at GetChallenge. Show the appropriate supplied password for asymmetric challenge
retrieval on supported firmware. Keep the 5.6.0–5.7.0 omission behavior explicit.

### HSM-C003 — Enrollment and HSM integration task is incomplete (high, accepted-doc-change)

Observed: README storage sample generates 32 random bytes, stores only on YubiKey, then clears them,
with just “escrow it” as the enrollment explanation. No matching HSM authentication object is created.
Rotation sample generates a management key, installs it, then clears its only shown copy. Readers need
secure persistence before clearing a replacement management key. S1/S3 require identical symmetric
long-lived keys in YubiKey and HSM. A YubiKey credential label and HSM authentication-key ID are distinct.
The credential password protects local applet access; the derivation password derives HSM authentication
keys; neither is the management key.

Propose: explain enrollment before key use, the three secret roles, host-owned secure storage, and
external HSM SDK responsibilities. Show matching-key provisioning as an application-dependent prerequisite
without inventing connector members. Clear managementKey in every sample's finally as well as key/password
arrays. For asymmetric generation, export PK-OCE with GetPublicKey and provision the corresponding HSM
authentication object; the credential private key stays on YubiKey. Actual provisioning is unverified.

### HSM-C004 — Handshake actors and key identity (high, accepted-doc-change)

Observed: interface symmetric/asymmetric context contracts correctly require exact 16/130 bytes, and
implementation validates context before LIST/device I/O. README uses undefined `ExchangeWithConnectorAsync`
without a typed declaration/complete application-dependent contract; calls it “connector session” and
asymmetric “device public key”, inviting confusion between YubiKey credential PK-OCE, ephemeral EPK-OCE,
HSM static public key and HSM ephemeral EPK-SD. CALCULATE returns key material, not an authenticated
connector/HSM session. S1 handshake still requires host authentication/completion. S4/S5 corroborate the
asymmetric inputs and independent static HSM identity.

Propose: classify connector block as an application-dependent sketch with a typed prerequisite (delegate
or local function supplied by application), preserve full disposal scopes, explain exact challenge order
and actual returned HSM half, identify HSM public-key provenance/trust separately from GetPublicKeyAsync,
and explain cryptogram verification/handshake completion ownership. A locally generated HSM challenge is
appropriate for isolated applet tests only. Optional symmetric cryptogram omission yields keys without
establishing live HSM authentication; do not promote a length check to authenticated interoperability.

### HSM-C005 — Applet version, metadata and development firmware (medium, accepted-doc-change + HSM-DEP-001)

Observed: backend:27–54 reads SELECT tag79, requires length3 and falls back to 5.4.3 when absent/wrong
length. Initialize uses caller override else applet version. Reset reselects and discards returned version,
preserving resolved policy. The 5.8 integration device-selection filter uses test metadata while method
EnsureSupports uses effective session version; exact development-firmware normalization is delegated to
Core. Comments assume alpha/beta supports instruction0B and claim any nonzero 16-byte production management
key works without supporting evidence. README's floor table is useful but lacks enabled-app prerequisite.
FeatureGetChallengeNoPassword is an obsolete alias of **WithPassword**, not a distinct old-firmware gate.

Propose: label floors as SDK feature gates, describe applet SELECT/override/reset policy, link applet
enablement guidance from S1, scope hardware outcomes to exact device/applet versions. Add XML to all
feature fields and obsolete alias. Correct current/hybrid gap entry claiming version refresh on reset;
preserve dated history. Drop unsupported production-key-complexity promise and alpha capability certainty.

### HSM-C006 — Ownership and clearing boundaries (medium, accepted-doc-change + HSM-IMP-002)

Observed: caller-owned ReadOnlyMemory inputs are borrowed; padded/password/encoded APDU data and derived
32-byte key are cleared in finally. DeriveKeys passes password.Span to .NET PBKDF2; “makes no copy” is
defensible only for this helper's explicit copy, not runtime internals. SessionKeys.Parse copies three
slices to owned arrays. Dispose clears those owned arrays, prevents later getters, and leaves original
Parse input and caller-made copies untouched (S6). Existing spans alias owned arrays. Factory extension
opens/owns connection; direct CreateAsync borrows it; Core implements failure cleanup and protocol ownership.

Propose: document keyEnc/keyMac ownership, SessionKeys source/copy lifetime, disposal and disposed access;
clarify the borrowed direct factory and owned extension factory with await using. Qualify “all key material”
to owned arrays. Explain explicit managed copies rather than framework-wide zero-copy assurance.

### HSM-C007 — Presence, callback failures and cancellation (medium, accepted-doc-change + HSM-DEP-002)

Observed: with prompt enabled CALCULATE performs LIST; known true → PolicyRequires, unknown touch or LIST
failure → PolicyMayRequire, false/missing label → silence. LIST cancellation propagates rather than
falling back; no prompt avoids this extra LIST. Notifications precede CALCULATE; completed resolution
occurs after Parse, failure cleanup disposes keys if successful resolution throws. Tests show pre-send
cancel, request exception, failure preservation and resolution CancellationToken.None; they establish
no in-flight physical cancellation. README “Pass a CancellationToken to abandon a wait” overstates native
smartcard interruption; shared guide correctly separates UI cancellation from device stop.

Propose: retain mapping and supported cancellation but explain notification is policy evidence, callback
returns promptly and avoids reentrancy, caller owns prompt, cancellation does not prove device operation
stopped. Use existing shared guide rather than duplicating its entire lifecycle. Physical touch/cancel
probe is separate from the two metadata-only touch-tagged tests.

### HSM-C008 — Open metadata and comparison semantics (medium, accepted-doc-change + HSM-IMP-003)

Observed: LIST casts unknown algorithm bytes into enum and unknown touch bytes to null; short recognized
records (<3 bytes) are skipped; unrecognized TLVs skipped; UTF8 decoding uses replacement behavior.
Algorithm extension properties throw ArgumentOutOfRangeException for unknown identifiers. Read response
data permissively without implying every algorithm is usable for requests. Record equality uses all
four fields (case-sensitive label); CompareTo only label OrdinalIgnoreCase, so CompareTo==0 need not imply
equality. Public record constructor/init impose no label/algorithm/retry validation, unlike request helpers.

Propose: document unknown algorithm/touch behavior, extension property exceptions, snapshot DTO rather
than validated enrollment object, and comparison vs equality. Do not copy Python label-only equality.
Short malformed known records and invalid UTF8 are an implementation robustness decision for Pass2.

### HSM-C009 — Example tool is diagnostic, with symmetric-only calculation (high, accepted-doc-change + HSM-IMP-004/005)

Observed: all22 sources read, including command/menu calls. `CalculateSessionKeys.RunAsync` lists every
credential then always calls symmetric calculation; asymmetric selection is possible. Program/menu do
not configure a presence prompt. AddSymmetric and CLI generation print long-lived keys; calculation
prints session keys. CLI args and visible prompts retain immutable secret strings/history. Management
key/privkey allocations can escape clearing before finally (AccessCommand invalid input/selection return;
AddAsymmetric invalid32byte input; ResolveManagementKey invalid length; AddSymmetric parse second key;
interactive AddSymmetric prompts/parsing before try; ChangeManagementKey second parse before try).

Propose: clearly introduce a diagnostic evaluation sample, warn before recommending it that terminal/
process history may expose secrets and output includes keys, state calculation is symmetric-only even
though selection includes asymmetric, and distinguish imported/generated/derived enrollment. Comments
should describe current local constraints; runtime strings/menu/filter/cleanup changes wait for Pass2.
Avoid claiming the entire example securely clears every sensitive allocation merely because happy-path
finally blocks exist. Device selection is SmartCard-capable discovery, not applet-capability validation.

### HSM-C010 — Test commentary exceeds assertion evidence (high, accepted-doc-change + HSM-IMP-006)

Observed: all12 test/helper sources read. `CalculateSessionKeysSymmetric_Returns48Bytes` uses arbitrary
16-byte context, omits card cryptogram and checks three lengths. Derived integration checks algorithm
only. Asymmetric GET_CHALLENGE checks nonempty only. Dummy test passes credential PK-OCE as HSM static
public key plus synthetic EPK-SD and8byte random cryptogram; exact `Assert.ThrowsAsync<ApduException>`
and nonzero SW can accept errors from missing instruction or wrong context/key, establishing neither
cryptogram verification nor handshake correctness. An HsmAuthRetryException subclass may fail the exact
exception assertion despite comments accepting63Cx. Byte-level password-change assertions pin instruction/
selectors and selected tags; they cannot prove every firmware0x6D00 outcome uniquely identifies an absent
instruction. PBKDF2 “known vector” recomputes through the same .NET primitive with independently stated
constants, rather than comparing to an external fixed KAT. SessionKeys disposal test really observes all
three arrays, but broader tests do not establish end-to-end secret absence. TestPassword/default accessors
allocate unmanaged-by-test arrays, timers are undisposed; helper closes resources, not persistent state.

Propose: scope test explanations to actual assertions, remove ISC/upstream development narration while
preserving useful rationale, clarify byte-level vs hardware diagnosis, classify dummy flow as rejection
exercise with multiple invalid inputs. Keep assertions unchanged in Pass1. See hardware matrix for all13.

### HSM-C011 — Internal/agent documentation accuracy and unfinished work (medium, accepted-doc-change)

Observed: no TODO/FIXME/HACK/XXX markers in tracked module source. Unfinished work remains real: absent
connector integration, challenge error consistency, permissive parsing, sample selection/secret cleanup,
weak hardware oracles and Core boundary questions. CLAUDE says “most security-sensitive”, “all sensitive
buffers zeroed” and every gated operation ensures at method entry, while base operations lack a base
EnsureSupports and support depends on successful selection/Core. Management error sections name only
ApduException despite typed subtype. Unit test list omits byte-level/typed suites. Test CLAUDE's relative
`../../../docs/TESTING.md` resolves to `src/docs`, while intended root path is `../../../../docs/TESTING.md`.
Wire instruction0x07 exists in table but SDK reads cached SELECT version instead of exposing live command.

Propose: explain actual session/backend responsibilities, correct test inventory/link, scope memory
assurances, describe gated feature groups accurately and use local protocol rationale instead of
“canonical Python”/issue-number history. Do not create/remove TODOs to manufacture completion.

### HSM-C012 — Narrow shared-document reconciliation (medium, accepted-doc-change)

Read relevant sections of root README, user-interaction, current migration guide/map/gaps, comparison,
highlights, two architecture guides and NativeAOT; ranges in coverage. Root README applet identity,
shared presence mapping and SmartCard-only architecture agree with local source. NativeAOT explicitly
limits YubiHsm to link evidence; ordinary coordinator JIT hardware tests must not erase that limit.
Migration guide:769/map applet notes mention connector/object workflows too broadly; identify applet scope
and manual external integration. Current map's “every retry-counted operation” needs C002 qualification.
Historical gaps file declares a dated snapshot but contains newer current-status additions: avoid global
rewrites, add scoped present clarification only where a current reader would infer reset refresh or
unqualified password-change device qualification. Comparison's new capabilities are implemented SDK
surfaces, not proof every alpha applet supports them. Source/version conditions should travel with claims.

## Deferred implementation and dependency map

All items below: implementation `deferred-pass-2`, documentation recommendation `accepted-doc-change`,
owner future module maintainer unless coordinator assigns otherwise. No bug is fixed by this report.

| ID | Observed question / desired decision | Implementation locus | Documentation reopening trigger |
|---|---|---|---|
| HSM-IMP-001 | GetChallenge generic retry path; decide typed consistency and add focused fake/device proof | HsmAuthSession.GetChallengeAsync | Changed error path → interface, exception class, README, migration retry map, retry test summary |
| HSM-IMP-002 | Examples/API-owned copy boundaries and pre-finally secret leaks; make cleanup total | example command/prompt allocation scopes; SessionKeys.Parse if lifecycle changes | Cleanup/ownership changes → README, SessionKeys XML, security/agent guidance and affected comments |
| HSM-IMP-003 | LIST skips malformed known records; UTF8 replacement/unknown metadata policy deserves deliberate tests | ListCredentialsAsync; algorithm extension properties | Strictness/raw-metadata decision → LIST/DTO/algorithm XML, touch fallback guide, parsing tests |
| HSM-IMP-004 | Calculation UI selects asymmetric but always symmetric; prompt absent | CalculateSessionKeys, SessionKeyMenu | Filtering/dispatch/prompt integration → sample scope, README, example comments and ceremony tests |
| HSM-IMP-005 | Visible/argument secret input and printed key material; decide safe tooling defaults | AccessCommand, CredentialsCommand, HsmAuthExamples | Input/output policy changes → diagnostic warning, example security guidance, CLI comments |
| HSM-IMP-006 | Hardware tests weak/dummy exact-exception oracles, partial cleanup/CTS lifetime | both integration classes/helper; PBKDF2 tests | Improved assertions/cleanup → test guidance, qualification statements, hardware plan; retain earlier failures |
| HSM-IMP-007 | Add cryptographic input/response validation only after contract decision: publicKey length/point, cryptogram length, GetChallenge result shape presently delegated/unchecked | asymmetric CALCULATE/GetChallenge/GetPublicKey | Validation changes → exact XML exceptions/format guarantees, README handshake and byte-level tests |
| HSM-DEP-001 | Core effective/development version normalization vs metadata filter and applet SELECT; verify Feature gate behavior including major0 and override, not here | src/Core/src/Sessions/ApplicationSession.cs; src/Core/src/Devices/Feature.cs and FirmwareVersion.cs | Core final boundary review resolves semantics → feature XML/table, alpha integration comments, reset/gaps notes |
| HSM-DEP-002 | Core smartcard in-flight cancellation, callback ownership and drain behavior; local tests prove pre-send only | src/Core/src/Sessions/ApplicationSession.cs; src/Core/src/Credentials/UserPresenceNotification.cs; downstream SmartCard protocol/transport | Core cancellation/ownership fix or native evidence → README cancellation/ownership, shared guide and touch tests |
| HSM-DEP-003 | TLV copies/owned raw response zeroability across actual formatter/protocol; local finally clears reachable memory and success response only | src/Core/src/Utilities/Tlv.cs and TlvHelper.cs; src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs; downstream transport/processor buffers | Core memory audit or ownership change → qualified security claims, ZeroApduResponse, SessionKeys/input lifetime guidance |

HSM-DEP entries are precise symbol/path questions from imports/calls, not findings against unread Core
source. Dependency file existence was checked by filename-only glob; no Core API snapshot/source/docs was opened. Vendor GAP-SPEC-1 remains evidence debt rather than
an inferred runtime defect. No dedicated cross-module boundary review was started.

## Clean-unit sampling handoff

`coverage.json.clean_units` defines pre-edit eligible semantic units. It is intentionally distinct from
file coverage: a file may have one clean member and a finding elsewhere. Coordinator must remove any
unit overlapping actual editorial changes, then sample ceil(10% of remaining units), minimum3 if available,
spanning API, narrative/example and internal/test. Suggested independent sample: U01 LIST contract,
U08 automatic one-shot lifecycle, U14 reset confirmation/body, U18 retry-mask partition. If any is edited,
choose another from that category. These are nominations, not completed independent sampling or accepted
final clean hashes. Material miss expands the category.

## Hardware handoff and limitations

`hardware-test-plan.md` is a source-inspected plan only. Coordinator owns read-only identity/state baseline,
all execution/touch coordination and explicit final cleanup. User already authorizes mutations on the
dedicated connected key; no extra approval gate is added. No HSM2/connector work is authorized or needed
for selected YubiKey-only tests/probes. All13 bodies and every persistent side effect are classified there.
No claim here upgrades prior evidence, API snapshots, fake tests or copied vendor code to live results.
