# B6 provisional concerns and reopening map

Source: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. These are local concern groups,
`CORE-B6-C001`–`CORE-B6-C016`, awaiting coordinator triage. They allocate no accepted
Core identifiers and are not an additive count of independent defects. Product bytes
and assertions remain unchanged. Reading a test below supplies its oracle, not a new result.

## C001 — Explain the actual APDU grammar, limits and status behavior

**Documentation candidate.** `ApduCommand.Le` says only “Gets the Le value.” Both
formatters always append Le: short output has one byte, extended output has two bytes
and the extended marker. There is no absent-Le representation for Case 1/3 in these
formatters. API zero writes wire zero; extended 65536 also writes zero; short 256 is
rejected although zero is accepted. `ApduFormatterTests` asserts the exact trailing
zeros for empty and data-bearing commands and the 300-byte extended length `01 2C`.
Its “NoLe” names describe API input zero, not absence of a wire field.

`PcscProtocol` selects extended formatting from connection support at construction,
then firmware/configuration. Constants are complete-command ceilings 1390, 2038 and
3062, while short fragments carry up to 255 data bytes. Core's contributor guide
instead says NEO 254 and YubiKey 4+ 2048. Short chaining has no whole-command ceiling;
SCP MAC formatting has a separate 3062 ceiling, including overhead. Document those
distinct quantities after profile review.

`ChainedResponseReceiver` follows `61xx` with CLA/P1/P2 zero, configured instruction
(default C0), and API Le zero; it ignores SW2 and the originating CLA. There is no
`6Cxx` corrected-length resend. `throwOnError` applies after assembly and treats every
status except 9000, including warnings, as an `ApduException`. Unknown raw statuses
survive in `ApduResponse`; `SWConstants`' signed fallback calls any negative unknown
value an “Unknown success code.” That description overstates success.

**Decision / qualification.** Preserve current behavior while deciding absent-Le,
logical-channel, corrected-length and total-response limits against the exact supported
ISO/YubiKey profile. Exact ISO edition text remains unverified in this checkpoint.
The SCP03 final passage in C011 independently specifies absent Le for its external
authenticate message; the universal formatter appends it anyway. This is a profile
discrepancy to qualify, not permission to change bytes in Pass 1.

**Reopen:** XML in `ApduCommand.cs`, `ProtocolConfiguration.cs`,
`SmartCardMaxApduSizes.cs`, `ApduException.cs`, `SWConstants.cs`; formatter and chaining
comments; `src/Core/README.md` APDU/raw task, `src/Core/CLAUDE.md` pipeline/size sections;
`docs/architecture/raw-access-tiers.md` SmartCard section. Recheck actual formatter
assertions and later SecurityDomain SD-DEP-004 call sites. Compiler/example validation pending.

## C002 — Keep every sensitive copy's cleanup owner explicit

**Implementation candidate with documentation consequences.** `ApduTransmitter`
clears the allocated formatted payload in finally after awaiting the actual transport
task. Each formatter first copies data into stack storage and returns a heap copy;
neither formatter clears that stack copy. The transmitter constructs a copied
`ApduResponse` but leaves `responseBytes` intact. B1 establishes independent successful
transport-result ownership; clearing the response owner reaches neither that transport
array nor caller input.

`ChainedResponseReceiver` copies each response into an `ArrayBufferWriter`, drops prior
response owners without Clear, and clears the writer only on successful assembly.
Continuation failure leaves the writer outside cleanup. Its successful result is yet
another copied owner. There is no fragment-count/byte budget. BCL internal growth-copy
erasure remains unverified; the observable SDK copy graph alone establishes the scoped
cleanup gap without relying on that extra claim.

`ScpProcessor` calls `new ApduResponse(State.Decrypt(unmacdData), response.SW)`.
`Decrypt` returns an independent plaintext array; the response clones it and that
temporary is abandoned uncleared. The finally clears the un-MACed ciphertext copy,
not the returned plaintext temporary. The original protected response owner is also
separate. `ScpState.Unmac` clears pooled message storage and computed MAC but leaves
its small stack message uncleared (that message normally contains ciphertext/status).

**Desired boundary:** caller clears its source and returned sensitive response owner;
SDK owns its private intermediate cleanup, including exceptional exits. Do not advise
casts from read-only views. Keep metadata/public ciphertext and decrypted secrets
semantically distinct. `ApduResponseOwnershipTests` directly observes source unchanged,
all owned aliases zeroed, and repeated Clear; it does not observe these other copies.

**Reopen:** formatter/transmitter/receiver/ScpProcessor internals; public response and
command XML; Core README ownership/security/raw examples; Core contributor APDU examples;
root APDU type examples and `docs/MEMORY-MANAGEMENT.md`. Later PIV/HSM boundaries trace
their results separately (`piv-001:PIVCORE-003`, `yubihsm-001:HSM-DEP-003`). C's TLV
copies remain C-owned. Every listed product path remains unchanged pending triage.

## C003 — State the exact continuation-recovery and reconfiguration boundaries

`ChainedApduTransmitter` reports an ordinary continuation exception only after
`offset > 0`; the offset increments after a successful response. If the first chained
fragment reaches the card and the transport then throws, offset remains zero and the
plain protocol stays locally usable. The current fake tests cover second-fragment
failure and first-fragment protected failure, not this plain first-fragment uncertainty.
An intermediate plain error is returned without the recovery callback; the protected
transmitter throws and its SCP owner marks recovery. The tests explicitly preserve
that difference. The guide's blanket “if a chained command ... fails” claim needs this
qualification without presenting unsafe reuse as intended policy.

`Configure` on firmware below 4.0 retains previous format/maximum unless changing the
remaining instruction rebuilds the graph. The development-version branch ignores
ForceShortApdus and selects the 3062 ceiling. The “ignores configuration” test only
asserts protocol nonnull. Record explicit policy/behavior questions for later change.

**Reopen:** `ChainedApduTransmitter.cs`, `PcscProtocol.cs`,
`PcscProtocolConcurrencyTests.cs`, `PcscProtocolTests.cs`, raw session XML (B5 owner),
raw architecture recovery prose and Core README. Exchange guard state is instance-local;
replacing a borrowed session releases bookkeeping but gives no card recovery proof.

## C004 — Separate counter mutation, failure notification and cleanup

`ScpState.Encrypt` increments `_encCounter` in the argument to `DeriveIv` before
ECB/CBC calls can fail. `ScpProcessor.stateAdvanced` becomes true only after Encrypt
returns. A provider failure after the increment bypasses that catch filter when no
earlier mutation was recorded. The comment promising notification after any provider
failure is broader than source. Existing invalid-Le-after-encryption tests cover a
later formatter failure; they do not inject this earlier AES failure.

`Mac` deliberately computes into a fresh chain, swaps only after success, then clears
the previous chain. That atomic chain update is distinct from Encrypt's counter and
from session-key disposal. The signed 32-bit counter has no explicit exhaustion gate;
wide-counter/profile behavior needs a bounded maintainer decision, not a billion-call
probe. `ComputeCmac` wraps every exception from creation/init/update/final/dispose as
NotSupportedException, despite XML saying only provider failures. No native crypto probe ran.

**Reopen:** `ScpProcessor.cs`, `ScpState.cs`, recovery architecture and all later applet
failure-state explanations. B5 owns public readiness transitions. Quarantine here is
local protocol admission refusal; native claim quarantine remains B1/platform ownership.

## C005 — Restrict the external-authenticate response exception to handshake phase

`ScpProcessor` exempts an empty 9000/62xx/63xx from R-MAC rejection solely when the
input INS equals 82. The processor has no handshake-phase flag. The wrapper's later
public raw exchange can submit INS 82 and reach the same exemption with encryption
enabled. This source path is broader than the rationale “the one unauthenticated
success ... precedes the R-MAC session.” `ScpEmptyResponseTests` checks the intentional
initialization call with encrypt false; it does not distinguish a later same-INS call.

**Disposition:** retain N2's resolved ordinary-command scope and record this separate
phase-gating candidate. A fake later INS82 probe is proposed, not executed; no new
hardware exploit or management authority is claimed. Final SCP03 §6.2.5 supplies the
external-authenticate exception; it does not establish a safe general raw bypass.

**Reopen:** ScpProcessor exemption/rationale, ScpEmptyResponseTests explanation, raw
SCP/security task guidance and later applet authentication/failure qualifications.

## C006 — Complete SCP03 startup cleanup and describe actual derivation

`Scp03InitAsync` slices 10 diversification bytes and three key-info bytes but uses
neither. The supplied StaticKeys are already the per-card inputs; no BMK diversification
is implemented here. It accepts a minimum 29 bytes rather than validating selected
protocol/key-info/profile or conditional sequence-counter framing. Public Release
1.1.2 §7.1.1.6 permits an additional three-byte sequence counter under its stated
condition, so a blanket exact-29 requirement would also be inaccurate.

StaticKeys derives AES-128 S-ENC/S-MAC/S-RMAC with constants 04/06/07 and
host||card challenge context; DEK is cloned unchanged into SessionKeys. Cryptograms
use S-MAC, constants 00/01 and output length 0040. Card comparison is fixed-time.
The generated/card/host cryptogram stack copies are not explicitly cleared.
After SessionKeys exists, provider failure during either cryptogram derivation has
no encompassing finally to dispose it; explicit mismatch disposes it correctly.
The initializer later correctly disposes returned state on external-authenticate or
missing-DEK failure and clears its hostCryptogram array in finally. These are separate
pre-return/post-return lifetimes. Optional DEK is admitted by StaticKeys but the
initializer always requests GetDataEncryptor after successful authentication.

**Reopen:** `StaticKeys.cs`, `ScpState.Scp03.cs`, `ScpInitializer.cs`, SessionKeys XML,
Core SCP task and SecurityDomain parameter examples. Old T10/T11 comments need present
lifetime rationale; no finalizer exists on SessionKeys/ScpState, so “GC finalizer” is
inaccurate. #23/N2 short-response guards remain resolved for their exact old scopes.

## C007 — Document contained-reference ownership and record cloning

StaticKeys clones source ENC/MAC/optional DEK arrays and zeroes those arrays on Dispose.
Scp03KeyParameters retains the exact StaticKeys object and disposes it, rather than
cloning it. Scp11KeyParameters retains the exact ECPublicKey, ECPrivateKey and certificate
objects; ToList copies only the collection. Dispose reaches the private key and each
certificate, and leaves the public-key object alone. Keep source buffers and contained
object lifetime distinct. SessionKeys clones its inputs and zeroes/nulls its own arrays.

Both parameter types are records. A with-clone shares the contained disposable owners;
disposing one can invalidate the other's future use. Read-only collection/memory views
do not establish deep immutability. Do not recommend mutation by casting such views.
StaticKeys reads still expose the zeroed storage after disposal, while Derive checks
disposed. SessionKeys reads become empty. The encryptor delegate captures SessionKeys;
after state disposal it no longer has a valid live DEK. Avoid use after its session ends.

**Reopen:** parameter/StaticKeys/DataEncryptor XML; Core README security and secure-channel
task, Core CLAUDE SCP examples; raw architecture and migration examples. All later
applet factory/reset examples must scope the parameter owner outside session disposal.
SecurityDomain retains parameters for reset; creation-only applets have a different
retention need. This is the Core mechanism handoff for C020, SD-DEP-002 and MGMT-IMP-004;
their exact applet call sites remain with boundary owners.

## C008 — KeyReference properties and cached encoding can diverge

The record struct initializes `_bytes` once from constructor Kid/Kvn but exposes init
properties. `reference with { Kvn = ... }` or an object initializer can change displayed
properties without updating AsMemory/AsSpan. Default(KeyReference) exposes empty cached
memory, rather than two zero bytes. Generated record equality includes cached memory
identity as well as value properties, so separately constructed same-valued references
need a dedicated equality/hash decision. The type's “uniquely identified by KID/KVN”
and “two-byte array representation” need honest qualifications until implementation
is changed. These observations follow the declared generated-record shape; compilation
and a bounded consumer-shape probe remain pending.

**Reopen:** `KeyReference.cs` XML, callers serializing AsMemory/AsSpan in the
SecurityDomain boundary, SCP diagnostics/examples and any equality-dependent test/helper.
Do not silently expand this into the unrelated catalog/public-status caches in A/C.

## C009 — State the supported SCP11 parameter/profile and trust boundary

The constructor checks KID, nonnull SD public key and, for a/c, nonnull OCE private key
plus nonempty certificates. It does not enforce P-256, OCE-reference presence, certificate
association/order/signature/trust/validity, or a/b/c-specific prohibition of extra inputs.
OceKeyRef is described as required but accepts null and PSO emits 00/00. The long
constructor accepts a static OCE key for b; initialization then uses it for the second
ECDH instead of reusing the ephemeral OCE key. That differs from the b construction in
the retained GP draft §3.1.1 and Python's normal b inputs. The generic parameter API
allows a wider shape than the supported YubiKey profile.

Ephemeral host generation uses the supplied SD key's curve, while response parsing
hardcodes nistP256 and 32-byte X before the remaining Y bytes. It does not explicitly
check raw point prefix/exact65 before the BCL import. Receipt comparison is fixed-time
and length-sensitive, and ordered first tags 5F49/86 are checked; extra TLVs are
decoded then unused. This is not a claim that every malformed/unknown field is validated,
nor a recommendation to reject all future extension TLVs. Security-critical framing
and admitted extensibility need a specific policy.

The caller supplies a trusted SD public key. Receiving/parsing certificates is distinct
from building or validating a certificate chain and configuring trusted roots. Core
sends the caller's OCE chain; the SD performs PSO validation under the device's
provisioned CA-KLOC policy. Final device-specific trust/profile behavior remains qualified.

**Reopen:** `Scp11KeyParameters.cs`, `ScpState.Scp11.cs`, `Scp11X963Kdf.cs`,
Core/SecurityDomain SCP role/setup/security guides, public factory XML and actual
test-fixture explanations. Reuse C's EC ownership/provider qualification rather than
claiming generic provider or native-memory guarantees here.

## C010 — Identify exact editions and distinguish SCP11c's key roles

ScpState cites SCP03 **1.1.2.6**, whose located original is explicitly **Public Review,
December 2019, target 1.2**, not the retained March 2019 **Public Release 1.1.2**.
ScpState.Scp11 cites final **v1.4** and several section numbers; the accessible retained
original is **Public Review v1.3.0.13, August 2023**. The official catalogue confirms
v1.4 publication in March 2024 but supplies no inspected final passage here.

In the retained SCP11 draft §3.1.1 and §7.6.2.3/Table 7-18, c uses the SD static key
twice and returns that static key in tag5F49; a/b return an SD ephemeral key. The
implementation's uniform variable/comment name “ephemeral SD” is wrong for c, but
the same two-agreement helper can work when the c response carries the static key.
This checkpoint does **not** classify the helper as inherently wrong for c just from
that name. It derives ka1||ka2, X9.63 SHA-256 80 bytes, receipt key then SENC/SMAC/
SRMAC/SDEK, and verifies a MAC over full host-authenticate bytes||returned5F49 TLV.

The fixed request emits A6{90=11+01/00/03,95=3C,80=88,81=10} plus host5F49;
HostID/CardID inclusion is not selected. The draft's HostID/CardGroup requirements
are conditional on the requested option bit; they cannot be promoted to universal
missing-field violations. SCP11c replay/authorization restrictions likewise need
the applicable actor/profile, distinct from generic channel-establishment success.

The complete draft §4.3 passage also identifies an important phase: for a/c, the SD
authenticates the OCE when the certificate is supplied and the first APDU after
establishment has a correct MAC. The host verifies the card's receipt during setup;
that does not move the device's first-command check earlier. ApplicationSession sets
its historical IsAuthenticated flag after setup returns. Feed this exact phase into
B5-C001/C006 and the SecurityDomain boundary rather than describing that flag as
already-proven host management authority. Draft §4.4 applies SCP03 messaging to all
following APDUs and prohibits BEGIN/END R-MAC SESSION; §4.5/4.6 distinguish a/b forward
secrecy from c's replayable offline profile and c command restrictions.

**Reopen:** all exact GP citations in ScpState/partials/ScpProcessor/test XML;
SCP11 key-role comments and Core/SecurityDomain setup tables; migration gap parity
claim. Preserve final/vendor gaps after the completed bounded search rather than
silently substituting a draft or promising conformance.

## C011 — Empty command encryption conflicts with the inspected SCP03 rule

Public Release SCP03 v1.1.2 §6.2.6 instructs the off-card entity to advance the counter
for each protected command and explicitly says no encryption shall be applied when
the command has no data. Current ScpState.Encrypt(empty) always creates/encrypts a
16-byte 80/zero padding block, and ScpProcessor invokes it even for empty data.
The nearby comment confuses counter advancement with mandatory padding/encryption.
`ProtectedChainingCard` asserts a 24-byte data field (padding block+MAC) for empty
commands; that is an oracle of present code, not independent normative validation.
Pinned Python also pads empty input; sibling agreement does not resolve the conflict.

**Disposition:** separate runtime candidate, target-profile decision and hardware
qualification. Retain the current bytes in Pass 1, describe the actual supported
implementation narrowly, and avoid universal GlobalPlatform conformance assurances.
The vendor overview's external-authenticate narrative also describes encryption/R-MAC
more broadly than GP §7.1.2/§6.2.5; its overview alone is not a wire oracle.

**Reopen:** ScpState.Encrypt remarks, ScpProcessor empty-data rationale,
PcscProtocolScpTests fake/expected framing, Core/SecurityDomain SCP descriptions and
selected applet raw examples. A later fix must keep empty-command counter sequencing
and empty-response R-MAC verification as separate requirements.

## C012 — Verify protected response chaining with an independent wire oracle

Current secure graph is outer ChainedResponseReceiver → ScpProcessor → command-only
transmitter/chainer. Each remaining command is protected and advances SCP state.
Pinned Python instead places response assembly below ScpProcessor and performs
unmac/decrypt after assembly, with raw remaining commands. These are concrete differing
graphs, not a conformance verdict by sibling preference.

The C# fake `ProtectedChainingCard` computes R-MAC for 61xx and 6A82, including no
data. Final SCP03 §6.2.5 defines all statuses other than9000/62xx/63xx as errors and
requires status-only responses without protection. The fake's
AuthenticatedApplicationError test therefore proves reuse after its synthetic signed
6A82 response, not the conforming status-only case. Other tests begin from bare61xx
and then produce a signed9000 using current host state. Neither establishes receipt
of split encrypted bytes from an independent physical card/profile.

**Reopen:** `ScpInitializer.CreateSecureProcessor`, receiver/ScpProcessor response
comments, PcscProtocolScpTests chaining/error fake and claims; raw architecture and
Core/SecurityDomain docs that imply transparent SCP chaining. Plan exact split-block,
split-RMAC, bare-error, short-command and custom remaining-instruction vectors with
independent expected bytes. No assertion changes or hardware runs were made.

## C013 — Align test explanations with their actual assertions and fixtures

- `PcscProtocolTests.SelectAsync_ConstructsCorrectApdu` checks only one recorded
  command and says bytes cannot be inspected, although the fake copies and exposes
  them. `Dispose_DisposesConnection` asserts the disposed protocol throws, not that
  the borrowed connection was disposed. Two names say InvalidOperationException
  while their assertions expect ApduException. Revise explanations without changing assertions.
- CoreScpAuditReproTests describes old short-response crashes/provider errors in
  present tense even though guards now reject them. It actually checks child exit
  and success marker for1..5, typed exceptions for6/7, original-failure identity and
  one command on reuse refusal, short init lengths0/28, ordered tags, block lengths
  and padding. Preserve that meaningful exact regression scope.
- AesCmacTests includes RFC4493 exact full-value vectors, but several “correct MAC”
  tests assert only16-byte length or equivalence between the same implementation's
  chunkings. Dispose_ZeroesKeyMaterial asserts disposed-method refusal, not retained
  internal-byte erasure. The release subkey test uses Assert.True(true); it is not a
  release subkey check. “Completes buffer ... triggers ProcessBlock” at line244
  describes the wrong point: a complete final block is retained until more input.
- Scp11Tests checks parameter shape and parses two certificate Subjects/public-key
  objects; it does not perform certificate trust/key association or a handshake.
  The fixture is intermediate then root, not a leaf-last authentication chain. The
  PKCS12 test only checks nonempty bytes and password text; it never loads the container.
  Two skipped bodies await Task.CompletedTask; treat them as proposed plans, not acceptance.
- Scp11X963KdfTests' deterministic receipt case uses explicit fixed key/point/receipt
  literals and checks a nonnull result after verification. Its claimed real capture
  has no retained capture identity here. Other helper tests check output lengths;
  its fixed static scalar literal has 64 hex characters (32 bytes). Source-text
  conversion confirms its P-256 width; no early scalar-width failure is claimed.
  Fixtures and generated objects/private exports have separate undisposed/uncleared scopes.
- X963KdfTests embeds ten empty-shared-info16-byte and ten shared-info128-byte
  SHA256 vectors with exact equality; the other cases check lengths, determinism
  or changes. The two complete vector files remain unread at this checkpoint, so
  original NIST/CAVS provenance and quoted line ranges remain pending.

**Reopen:** exact test classes/fixture XML and comments above; nested test guidance,
Core CLAUDE fake snippets (QueueResponse/SentCommands mismatch), any guide/evidence
claim relying on them. Test names are executable identifiers and require a separately
authorized change, even when their comments can be corrected in Pass 1.

The focused dependency read confirms that `ECPrivateKey.CreateFromValue` calls
`AsnUtilities.ValidateEcPrivateScalarArgument` before provider work, requiring the
subgroup-order width. An initial visual miscount of this test literal was corrected
by extracting the actual source string and converting hex text with read-only Python.
The literal is 32 bytes; no fixture rejection or test failure follows from its width.

## C014 — Keep logging claims local to the inspected templates

PcscProtocol's command trace logs header bytes/Le/data length, not payload. SELECT
logs the caller-supplied application identifier as hex; the API accepts arbitrary
bytes, so classify caller input rather than infer universal secret safety. ScpState's
optional logger records byte counts. Failure objects retained as InnerException can
contain provider/transport-defined text. The guard does not centralize redaction.
Protocol logger invocation before exchange admission can itself fail before any
wire operation. Preserve C's logging/provider ownership findings and B1's distinct
post-open logger failure, without duplicating them as a new mechanism.

**Reopen:** Core security/logging guidance and exact protocol templates/comments only
where an accepted claim needs correction; later applet SD-DEP-005 checks its own calls.

## C015 — Complete secure-channel tasks and failure-safe cleanup examples

Core's “Use a secure channel” task stops after construction. It needs explicit typed
key input provenance, trusted provisioning context, the factory-default security
qualification, parameter/session scopes and a useful command/result task. Source
buffers need finally cleanup independently of session creation/transmission success.
Existing command XML and Core CLAUDE PIN fragments clear only after successful await.
`LoadScpParametersFromSecureStorage` examples are application-dependent fragments,
not complete compiled programs. Label them and compile meaningful enclosing examples later.

The raw-SCP architecture example selects an application after channel establishment.
PcscProtocolScp.SelectAsync deliberately sends it plain and retains the old processor
and encryptor; no re-handshake or invalidation happens locally. Python SELECT discards
its SCP processor. The vendor page says plaintext ends the channel/authorizations.
Treat select-after-SCP as a concrete wire/state qualification and trace actual callers
before recommending it. B5 owns IsAuthenticated publication/reset and full workflow
state: successful initial establishment is not an ongoing authorization/liveness probe.

**Reopen:** Core README secure-channel task, Core CLAUDE SCP/sending examples,
root APDU examples, raw-SCP architecture, migration SCP construction/gaps and actual
RawSmartCardSession/extension XML through B5. Nine applet boundaries own their complete
factory/backend/reset/action examples. Strong wrong-parameter errors and framing bugs
remain Pass2 concerns rather than intended-Core-contract explanations.

## C016 — ATR value equality and hashing disagree

AnswerToReset compares content with FixedTimeEquals but hashes the private array
object rather than its contents. Equal independent ATR values can therefore have
different hashes. Its equality operator also returns false for two null references.
The copied byte source remains owned privately and public ToString exposes ATR bytes.
No dedicated primary test checks these contracts. This is a source-backed runtime
candidate with a bounded collection/equality probe proposed for the coordinator.

**Reopen:** AnswerToReset public type/equality/hash XML if added, callers using equality
or hash collections in B1/B4, and related examples. ATR metadata is not classified as
secret merely because it is device-returned.
