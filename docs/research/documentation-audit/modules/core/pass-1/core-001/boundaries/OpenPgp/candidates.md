# OpenPGP boundary proposals

These are local triage groups at source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
They allocate no accepted Core IDs. D means documentation proposal, M implementation
follow-up, P policy decision and Q qualification. Each facet retains its own gate.
All named product paths remain unchanged in this investigator assignment.

## CORE-PGP-X001 — Own the complete factory and disposal scopes

**D:** Preserve direct-factory borrowing and device-extension hidden-connection ownership.
`IYubiKeyExtensions.cs:37–62` resolves only SmartCard, copies creation settings, invokes
the public direct factory and calls `OwnConnection` after success. Core binds before
SELECT, registers the initial protocol before setup, and publishes readiness only
after OpenPGP application-data parsing and logging finish (`OpenPgpSession.cs:101–155`).
Failure disposes/detaches the session; the device helper owns failed hidden opens.
The backend borrows the effective protocol and needs no competing disposal owner.

`OpenPgpSession.cs:399–414` clears the current KDF and calls base teardown in finally.
Core `ApplicationSession.cs:444–520` makes the ordering concrete: synchronous Dispose
enters that override before protocol drain; asynchronous Dispose awaits protocol and
owned-connection cleanup, then calls the same derived override in finally. Both clear
the current cache, including asynchronous teardown failure. This settles the historical
async-dispatch question for the inspected source; it supplies no whole-workflow lease.

**M/P:** Keep `openpgp-001:PGP-IMP-003` open. Get/SetKdf aliases, dropped replacement/reset
references and caller-disposed objects remain. Synchronous teardown can clear salts
while a pre-existing public workflow is between exchanges or computing a KDF. A custom
KDF Dispose can throw; base teardown still runs. Decide a coherent KDF borrow/transfer/
clone and operation-lifetime policy separately. This is actual OpenPGP applicability
of `CORE-B5-C006`, rather than a second generic Core finding.

**Q/oracle:** `OpenPgpSessionWireTests.cs:750–807` observes one retained salt after async
disposal and sync base detachment after a throwing KDF. It exercises no replacement,
reset-disposal, held derivation, asynchronous connection failure or concurrent workflow.

**Reopen:** `src/OpenPgp/src/OpenPgpSession.cs` factory/disposal XML and rationale;
`src/OpenPgp/src/IYubiKeyExtensions.cs`; `src/OpenPgp/src/IOpenPgpSession.cs` Get/SetKdf;
`src/OpenPgp/src/Kdf.cs`; README “Open and inspect” and KDF scopes; CLAUDE shared state;
wire disposal-test comments. Core session/connection guidance stays with B5/SHARED.

## CORE-PGP-X002 — Separate reader units, credential identity and authorization

**D:** `Credentials/OpenPgpCredentialOptions.cs:19–60` sets 6/8/8 minimum and 127 maximum
reader lengths. Core checks UTF-16 char counts before UTF-8 conversion. PW status
returns raw length/format bytes; the OpenPGP PIN methods accept bytes and add no
general UTF-8/profile-length validation. OpenPgpTool's supplied string branch bypasses
reader limits entirely (`OpenPgpCommand.cs:93–123`). Its helper comment saying the
application “also applies” card limits must distinguish caller responsibility from
actual sample enforcement. Say “UTF-16 code units” locally where preset lengths are
explained. Preserve `CORE-C-C001–C004`; CTAP NFC/code-point rules do not transfer here.

**M/P:** Preserve `PGP-IMP-001`, `002`, `018` and `019`. Extended VERIFY passes Pw.Reset
to derivation and fallback retry lookup while P2=82 still addresses PW1. Distinct salts
and counters expose the error. All unsuccessful VERIFY statuses become the typed PIN
exception; only 6982 triggers C4 fallback. KDF errors broadly cache KdfNone. PIN changes
derive both old/new inputs; reset-code and prior-admin routes have separate payloads.
SCP `IsAuthenticated` represents channel setup, never PW1/PW3 access authorization.

**Q/oracle:** Existing extended retry test explicitly expects Reset's count. The custom
CancelOnPwStatusLookupConnection returns its own canceled task even though Core passes
None after admission; that test proves custom-provider propagation, not native token
interruption. Historical distinct-salt and retry-identity failures remain confirmed
engineering findings on serial 103. C's generic reader fixes do not close them.

**Reopen:** credential preset XML, PIN/Pw/PwStatus/KDF and exception XML, `.Pin.cs`
comments, OpenPgpTool credential helper, README credential/KDF/crypto tasks, migration
`openpgp-exception` and actual retry/cancellation-test explanations after policy changes.

## CORE-PGP-X003 — Follow each secret copy through APDU and TLV producers

**D:** Name separate owners at `OpenPgpSession.Pin.cs:107–121,154–191,247–278,331–361`,
`PrivateKeyImport.cs:53–81,112–172`, `PrivateKeyTemplate.cs:46–115` and
`OpenPgpSession.Crypto.cs:28–145,195–245`. Applet finally blocks clear derived PINs,
combined credentials, import/template copies and crypto payloads. Caller keys/inputs
remain borrowed. RSA/EdDSA transfer successful response storage; ECDSA returns DER and
clears raw storage. Decrypt resolves Completed, copies output, then clears raw storage.
The current producer uses a writable view of owned response memory; Core Clear is the
preferred explicit owner operation for a later scoped edit, with no runtime rewrite here.

**M/P:** Extend `PGP-IMP-003` with the actual KDF copy graph: ToBytes disposes individual
TLVs but `EncodeList(...).ToArray()` abandons the first complete encoding; SetKdf's
returned encoding has no clearing scope. Parse's DecodeDictionary allocates field
copies, then init setters clone salts/hashes; those dictionary copies are abandoned.
The F9 response owner likewise escapes as Data and is not cleared after parse. Initial
PIN hashes are authentication material; public algorithm/AID/certificate metadata has
a different semantic classification. Reuse `CORE-C-C005/C006` and `CORE-B6-C002/C007`.

Core ApduTransmitter clones returned transport bytes into ApduResponse without clearing
the transport array. Chained responses leave preceding response owners, and failures
have narrower accumulator cleanup. SCP adds protected/plaintext owners. Clearing the
final OpenPGP response reaches neither those producers nor previous fragments. B6 owns
those shared mechanisms; this return adds actual PIN/F9/import/decipher caller links.

**Profile qualification:** VERIFY and CHANGE tables in Card 3.4.1 §§7.2.2–3 specify
absent Le; current Core formatters append Le, including zero on these callers. Link
`CORE-B6-C001` here. Card command-profile evidence is available even though exact ISO
7816-4 case/corrected-length clauses remain unavailable. Device tolerance is separate.

**Reopen:** Kdf serialization/parse/lifetime XML, Config SetKdf comment, private import/
template cleanup rationale, raw/crypto result XML, README resource scopes, TLV and raw
access guides, memory/security guidance and actual retained-array tests. Preserve
caller-owned source cleanup and successful-output transfer when changing an owner.

## CORE-PGP-X004 — Guard exchanges and await whole public workflows

**D:** Link `CORE-B5-C006`, B6 continuation/recovery and C's notification ownership at
the actual sites. Certificate SELECT DATA→GET/PUT, attributes→generate/import→refresh,
VERIFY→6982 status query, two-mode Unverify, KDF lookup→derivation→VERIFY and the whole
reset sequence each span more than one Core exchange. Core refuses overlapping
logical exchanges; it supplies no exclusion or disposal lease for these full methods.
Post-exchange parsing/cache changes and presence callbacks are outside the guard.
Await complete public tasks before another operation or disposal, and keep borrowed
keys, memory and prompt services alive through their actual last use.

Presence uses cached slot UIF, one direct read if absent, cancellation propagation and
PolicyMayRequire fallback on other errors (`OpenPgpSession.cs:322–371`). Sign, Decrypt,
Authenticate and Attest use Sig/Dec/Aut/Att scopes. Request runs before crypto APDU.
Resolution uses None; device status failure maps Failed, including historical 6600.
A pre-admission token can prevent an exchange; an admitted SmartCard exchange runs
with None through chaining/SCP and waits for native completion. An outer WaitAsync
ending gives no permission to clear inputs, release claims or retry a mutation.

**M/P/Q:** Preserve applet mutation/cache and callback policy questions. Attest constructs
a disposable public certificate before the fallback Completed resolution; callback
failure can abandon that object (`.Keys.cs:150–182`). This is a resource-lifetime
extension of the producer boundary, not a secret-output claim. Sign/Authenticate
explicitly resolve before transfer and Decrypt before copying. The existing helper
tests actually retain raw arrays for signature transfer/zeroing, while the Decrypt
resolution test checks exception identity/outcome only. No test establishes full
workflow drain, certificate cleanup on resolution failure or arbitrary reentrancy.

**Reopen:** interface preamble and certificate/key/PIN/crypto contracts, `.Crypto.cs`,
`.Keys.cs` Attest rationale, README presence/cancel/resource examples, user-interaction
guide and wire/helper comments when operation admission or producer policy changes.

## CORE-PGP-X005 — Reset and reopen with explicit channel provenance

**D:** `.Reset.cs:25–97` checks reset feature, reads counters, attempts PIN blocking,
ignores E6/44 status, calls the existing backend's ReadInitialState, refreshes app data,
clears UIF overrides and drops KDF. It leaves inherited readiness/authentication state
and the effective version unchanged. No complete erasure postcondition is checked.
Preserve `PGP-IMP-005`, ATT persistence and fixed-policy recovery qualifications.

At an SCP-created session, reset commands use the retained secure graph. Reselect uses
`PcscProtocolScp.SelectAsync:87–109`, which sends plain SELECT through that graph without
discarding/re-establishing keys; the following GET VERSION and application read again
use secure Send. OpenPGP retains no creation SCP parameters for a reset handshake.
This is the actual caller for `CORE-B6-C003` and B5 readiness qualifications. Normal
plain-reset history and an IsAuthenticated flag provide no SCP-reset acceptance.

**M/P/Q:** Decide reset terminality versus supported reinitialization, status handling,
SCP restoration and failure-state publication in separately authorized work. Preserve
the original configured version deliberately, but do not describe reselect as fresh
channel authentication. The integration helper resets in a separate *plain* borrowed
session, disposes it, then creates the body session with optional SCP; that shape tests
neither reset of an SCP session nor reset restoration of its options. Dispose/reopen
after uncertainty and inspect persistent state before retrying. No new reset ran.

**Reopen:** Reset XML/internal sequence, backend Select/version comments, README reset/
factory/SCP recovery, CLAUDE reset state, test helper and reset-test explanations;
Core raw/SCP guide and shared reset claims require their existing owners.

## CORE-PGP-X006 — Preserve detected, fallback, metadata and effective versions

**D:** `Backend/OpenPgpBackend.cs:67–91` accepts OK with at least three bytes and decodes
the first three as BCD. Every non-OK response, every short OK and caught ApduException
falls back to 1.0.0. The comment/instruction attributing fallback only to 6985 is too
narrow. Transport/other exceptions still propagate. This fallback can conceal unrelated
status/malformed-version causes; changing that policy is an M/P facet, not a prose fix.

`OpenPgpSession.cs:130–154` logs detected version, passes override-or-detected version
to configuration and never consults physical metadata. Feature accepts major zero;
Pcsc Configure's major-zero branch ignores ForceShortApdus and selects transport
extended capability/Yk43 ceiling. Stable overrides affect framing, feature checks,
NEO import form, SELECT DATA and algorithm-info workaround. Overrides do not skip
SELECT/version probes. Reset rereads but deliberately discards the version. An OpenPGP
AID version is the card specification version, a separate value again.

**M/P/Q:** Reuse `CORE-A-C015`, `CORE-B5-C001` and B6 framing. Preserve historical applet
0.0.1, metadata 5.8.0.beta.0 and CLI application 5.8.0 observations separately. Generic
SCP03 5.3.0/SCP11 5.7.2 gates do not establish an OpenPGP earliest-firmware/profile
contract or import OATH's 5.6.3 floor. Major-zero acceptance is optimistic SDK policy.
Commercial/source threshold discrepancies and actual old-family/NFC lanes remain open.

**Reopen:** backend version rationale, CLAUDE initialization, README requirements and
factory/version diagnosis, public factory/options/Feature guidance, workaround comments
and version/framing tests after fallback or effective-version policy changes.

## CORE-PGP-X007 — Keep protocol representation separate from Core capability

**D:** `IOpenPgpSession.cs:305–314` lists only NIST/25519 EC imports, while current
OpenPgpCurveOids and PrivateKeyImport admit secp256k1 and Brainpool P256/P384/P512.
README already lists them with host/card qualification. Reconcile this active XML
without implying device support. GetOpenPgpInfo excludes RSA1024 and symmetric types;
legacy OpenPGP 25519 OIDs remain module-owned value octets, separate from RFC8410
key AlgorithmIdentifiers. Parsing preserves received algorithm/import format and
rejects unknown required OIDs; factories select by construction/slot, not capability.

**M/P/Q:** Preserve `PGP-IMP-007/010/011/013/014/018`. Core provider validation/catalog
evolution and exact ordinary import vectors do not fix X25519 decipher's absent A6
wrapper, ATT's B60181 CRT, old-family CRT02-with-modulus, destructive delete strategy,
ROCA generation protection or signed timestamp representation. Read-only ATT failure
and correct-CRT comparison remain engineering evidence; target-key FB attestation is
a separate path. Core mathematical metadata supplies no extra slot/firmware claim.

**Reopen:** PutKey XML, current model/import comments, README catalog/provisioning,
CLAUDE model/compatibility, Crt/KeyRef, algorithm/key-wire and agreement/ATT explanations
after the respective implementation/profile changes. C owns generic provider/ASN.1
decisions; no duplicate Core mathematical finding is proposed.

## CORE-PGP-X008 — Keep test and sample evidence within its actual oracle

**D:** Preserve already corrected assertion-accurate comments. Tests read in this packet
were not run. Recorder queues responses, copies commands, has no PC/SC timing semantics
and counts both disposal forms. Init asserts command membership, not complete failure
cleanup/order; dedicated key generation asserts three-operation order. Signature helper
tests retain actual response arrays, but no complete producer-chain erasure follows.
KDF SHA512 asserts length; same-logic SHA256 tests and retained literal KATs have distinct
independence. Current libgcrypt-named tests execute only SDK derivation.

**M/P/Q:** Preserve `PGP-IMP-015/016`. Hardware helper leaves body state in place and has
no final restoration. RSA/P256/P384 import/sign and P256 decipher have host equality;
Ed/X import checks public points, never agreement/signature. Generated signature and
attestation shape tests leave trust/binding unverified. Four old-firmware methods return
through 4.3.5 inclusive before any setup/assertion. Unit tests/provider skips and a
runner success add no physical acceptance. Sample certificate import/export/attest omit
certificate disposal; attestation --pin is unused, supplied PIN/PEM strings persist,
info infers presence from length, and no presence prompt is installed. Executable help
and output remain frozen in this documentation assignment.

**Reopen:** affected complete integration/unit/helper comments, test instructions,
README examples/sample limits, OpenPgpTool command/helper comments and migration
qualification only after corresponding assertions/input/ownership change. Preserve
historical failed setup, intended-contract failures and successful scoped successors.
No new generic tooling, SDK execution or hardware task is requested.
