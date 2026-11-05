# PIV hardware execution packet

**State: source review complete; investigator executed zero hardware commands, builds or tests.**
The coordinator owns discovery, fixture configuration, execution, restoration and raw logs. The
integration checkpoint's4,124 unit cases (289PIV), zero warnings and five skips are preceding
evidence; they establish no new live PIV run in this assignment.

## Current identity and preservation boundary

The coordinator supplied fresh `hardware/baseline-103.json` observations during this investigation:

* Serial103, USB-A, metadata-reported5.8 beta/enhanced-PIN profile. PivTool reports5.8 while direct
  GET VERSION returns000001. Preserve both origins; no private firmware override was used.
* PIN and PUK default=true, total/remaining3/3. Management AES192, raw algorithm0A (decimal10),
  default=true, TouchNever. CHUID/CCC/KeyHistory/ADMIN DATA absent.
* All25 public slots inspected:24 ordinary slots empty (certificate6A82, metadata6A88). F9 already
  holds RSA2048/PinOnce/TouchNever, origin imported-or-other, and a793-byte certificate whose SPKI
  matches metadata. Preserve this material and its exact bytes/SPKI/hash.
* Public Bio metadata reports unsupported. That supplies a negative observation on this key;
  positive Bio operation evidence remains outstanding.
* Two keys are attached (103 and 125). Primary mutations target 103. The coordinator subsequently
  supplied `hardware/baseline-125.json`: reader01 00, metadata 5.8.0.alpha.2 Enhanced PIN USB-A,
  raw PIV000001; 24 normal key/certificate slots empty; default PIN/PUK3/3 and AES192 management
  key with TouchNever; CHUID/CCC/KeyHistory/ADMIN absent. Its existing F9 RSA2048/Once/Never
  certificate is793 bytes, DER SHA256 `84d4b3407b5ca5743ae7f0ea46a58ccb493db357f877483e5c288790fe34fd1a`,
  SPKI SHA256 `5fbb057994305ab5e98f70b6c17a582fee5ccff580aeb8c9d821cf52ff3d4c27`, matching metadata.
  Preserve this distinct baseline. A coordinator multi-key scope decision still precedes mutation
  of125; both devices are development5.8 profiles and supply no older-commercial-firmware evidence.

These are coordinator observations, not investigator device evidence. Refresh identity after any
replug. A previous agent's serial103/full-reset claim supplies no new baseline. The coordinator
also established that this checkout's shared allowlist configuration is **tracked**, despite the
general harness documentation describing a gitignored list. Preserve its original bytes; narrow
temporarily to103 before canonical tests and restore exactly afterward. Do not copy secret or
unrelated configuration into the public record.

Standing user authorization covers routine dedicated-key reset, configuration, PIN/PUK and
management authentication. Physical touch/hotplug timing still requires coordination. Every
selected operation stays inside those boundaries. Existing F9 private-key replacement/deletion,
factory-attestation replacement, irreversible policy changes and unrelated applet mutations are
outside this packet. **Zero existing PIV integration methods explicitly replace/delete F9 private
material or install an irreversible policy.** They reset PIV frequently; vendor reset retains F9.

## Exact source matrix and counts

Read `evidence/hardware-method-matrix.md` for all89 named methods and
`evidence/hardware-methods.json` for each full body, source lines, attributes, actual assertion
oracles, early-return/skip paths, expanded helper effects and cleanup disposition. This is the
authoritative method-level effect matrix, not a count inferred from test names.

There are24 integration C# files,89 test methods:86 SkippableTheory methods and3 multi-key
SkippableFact methods. All methods use ordinary PIV state; the Bio method is negative-only.
Counts describe source methods, not discovered/executed case totals. WithYubiKey produces
device rows; fixture/firmware filtering can skip or prevent discovery.

| Source filter/lane | Trait-selected methods | Effect on103-only run |
|---|---:|---|
| Whole PIV integration project |89| Includes touch/hotplug and multi-key methods; coordinate lanes first. |
| `Category!=RequiresUserPresence` |86| Three multi-key methods still included and should skip under103-only allowlist;83 other methods selected before firmware/profile filtering. |
| Smoke (excludes Slow and RequiresUserPresence) |80| Includes two non-Slow multi-key facts;78 single-key methods before fixture filtering. |
| `FullyQualifiedName~PivMultiKeyContentionTests` |3| Requires two freshly scoped allowlisted keys; one method isSlow. |
| `FullyQualifiedName~UserPresenceProbeTests` |2| One read-only printer and one touch ceremony; both haveRequiresUserPresence, one alsoSlow. |
| `FullyQualifiedName~PivHotplugContentionTests` |1| RequiresUserPresence+Slow; operator removal and separate fresh-session reinsertion proof. |
| `Category=Slow&Category!=RequiresUserPresence` |6| RSA3072/4096 sign, RSA4096 decrypt, RSA3072 import metadata, busy discovery, cross-key liveness. Last needs two keys. |

Eight methods haveSlow; three haveRequiresUserPresence; two overlap. Actual command selection
and discovered/skipped counts must be retained from the runner. A green run with skips is qualified
to the executed subset. No canonical test is selected solely because its name sounds read-only.

## Setup, mutation and cleanup review

Every RESET first consumes PIN/PUK retries until blocked, sendsFB and refreshes management type.
On this beta key the applet version is000001; default-type fallback uses detected firmware's
major-zero compatibility rule while initialization metadata suppliesAES192. Record refreshed
metadata and firmware origin instead of hiding them with an override.

| Test family | Actual setup/body effects and local cleanup |
|---|---|
| Authentication (6) | All reset initially. Exercise default/wrong management authentication, correct/wrong PIN and retry reads. ChangePin rejects the old PIN, accepts the new one and resets in finally. Other methods can leave a decremented retry counter until restoration. |
| Certificates (4) and compression (1) | All reset. Roundtrip generates 9A P256 and stores a software-key certificate; fingerprint equality checks storage, separately from key binding. Delete submits two deletes. Empty-object reads CHUID. Compression explicitly selects GZip. No final reset. |
| Key operations (8) | Seven reset; serial read is read-only. Generate P256/Ed25519, attest 9A, move 9A to Retired1 and verify with the original key, import P256 and verify, delete 9A, and write/read PRINTED. PRINTED cleanup writes empty and swallows failure; others leave keys. |
| Crypto (10) | All reset/authenticate and generate keys; private operations verify PIN. Host verification covers P256/P384 and RSA1024/2048/3072/4096 signatures. P256 ECDH compares raw secrets; raw RSA decrypt checks PKCS1 structure/plaintext. Ed25519 uses differential/determinism checks. The X25519 method only generates a key. No final reset. |
| Decrypt (5) | Two helper-driven roundtrips reset/authenticate, generate RSA2048 in 9D, verify PIN, encrypt on host and compare plaintext. ECC/length tests also generate keys; metadata APDU precedes rejection. RSA4096 compares plaintext. Some attributes admit 4.0/4.3.5 although high-level decrypt requires metadata 5.3. No final reset. |
| Signing algorithms (6) | Reset/authenticate; host-verified RSA2048 SHA1/384/512 PKCS1 signatures, OAEP-SHA1 plaintext, P256 SHA256 and P384 SHA384 signatures. The P256 comment says SHA384 incorrectly. PSS remains unexecuted; randomized PSS can be verified. No final reset. |
| Import (4) | Reset/authenticate; RSA2048 has host signature verification, RSA3072 only metadata. Ed25519 signs SHA256(message), checking nonempty result and imported origin. X25519 checks metadata only. Seed arrays are cleared in finally; the RSA helper clears PKCS8. No final reset. |
| Management key (4) | Reset/authenticate and install a new key. Three tests close then authenticate in a fresh session; each has fresh-session cleanup reset. The same-session case installs AES128, proves privilege with key generation, resets, checks state/type and authenticates defaults. Most cleanup lacks an independent finite token. |
| Metadata (5) | Four reset. Key metadata overwrites 9A with P256 then RSA2048 and compares generated/extracted SPKI. The Bio method expects an error without excluding Bio devices. No final reset. |
| PIN-only (1) | Reset; install a random 24-byte active key; enable protected mode; assert PUK blocked; close/reopen, recover and disable; assert PRINTED/ADMIN empty and default-key authentication. Independent 30-second cleanup session resets, then clears owned keys/PIN. No physical touch. |
| PIN retries (4) and PUK (4) | Reset first; consume wrong-PIN retries, set custom retry limits (resetting credentials), change credentials and unblock PIN. Mutation workflows use same-session finally reset; the PUK metadata test only resets at entry. Bio profiles are unpartitioned. |
| Policy (2) | Reset/authenticate; provision PinNever in 9A or PinAlways in 9C, then host-verify one signature. Always's second-use rejection remains untested. No final reset. |
| Reset (3) | Reset, check default management type/authentication or PIN metadata 3/3. The third provisions 9A, resets and checks 9A empty only. Coordinator readback covers the remaining slots and retained F9. |
| Slot overwrite (2) | Reset/authenticate; overwrite 9A RSA2048 with P256 or the reverse. Check metadata and verify a signature against the new key. Certificate independence and final reset remain outside these bodies. |
| AuditV2 (2) | PIN isolation: reset/provision 9A PinOnce, establish rejected-sign baseline, verify PIN on a closed handle, then try a fresh unverified handle. Shared usable PIN state intentionally fails this policy regression. Ed25519: provision 9A, sign 10/64-byte messages and independently verify with OpenSSL; child process lacks a timeout. No final reset. |
| Connection concurrency (1) | Reset/authenticate, provision 9A P256 and verify PIN; write 2,500 random bytes to Retired1's certificate object. Ten overlapping read/sign attempts assert sign refusal/read equality. A failed assertion can leave a read task undrained. Scope cleanup only. |
| Discovery contention (2) | Reset/authenticate. Busy RSA4096 in Retired1 drains key generation in finally and checks scan time under four seconds. The other provisions 9A P256/PIN and checks nonempty signatures before/after a cold scan. “Today bug” comments describe earlier behavior. No final reset. |
| Session contention (5) | Three expand ArmPinGatedSigning: reset/authenticate, generate 9A P256, verify PIN, refuse a second device/session/connection, then check signature liveness. Two read-only methods check successive borrowed sessions (PIV→Management→PIV) and a second-session guard. |
| Multi-key (3) | Helpers select the first two authorized SmartCard states (5.7+ for RSA4096), skipping when insufficient. First resets key A and scans B; second resets both and runs ten parallel signs each. Third provisions both, exercises busy RSA4096 in both directions and independently attempts both resets. First two leave keys. Oracles check nonempty results/timing rather than cryptographic cross-key binding. |
| Presence (2) | Printer is read-only. Ceremony skips before mutation unless management default/type/no-touch and empty Retired20 prerequisites hold. Generate P256 PinNever/TouchAlways; sign with a cooperative 30-second token, verify on host, check callbacks and same-session serial/metadata. Finally deletes the key when keyCreated is true; cleanup uses the existing session without an independent finite token. |
| Hotplug (1) | Borrow connection, open PIV, establish serial liveness, poll until a failure, dispose connection in finally and check the second open avoids a stranded lease. No reset/key write. The operator must actually remove the key; a generic failure alone fails to prove removal. Fresh reinsertion/serial read is a separate control. |

Per-method JSON retains original source and precise lines for each setup, assertion and cleanup path.

## Recommended execution order and commands

Use the repository test skill/toolchain through the coordinator; these are planned commands only.
Hold one toolchain/device runner at a time. Scope the shared allowlist to103 before any broad run.

1. Retain the coordinator's fresh identity and all25-slot/certificate/object baseline. Confirm the
   independent cleanup probe can restore PIN/PUK/default management authentication and preserve F9.
   Preserve failed probe output before adjusting assumptions.
2. Run routine smoke: `dotnet toolchain.cs -- test --integration --project Piv --smoke`.
3. Run the full unattended subset:
   `dotnet toolchain.cs -- test --integration --project Piv --filter "Category!=RequiresUserPresence"`.
   Include the five single-key Slow methods; three multi-key skips under103-only configuration are
   expected. Record every other skip/failure and whether setup, operation or cleanup was reached.
4. Run only the bounded material probes below. Keep same-key/host controls before cleanup; do not
   use RESET to hide the failure under investigation.
5. Coordinate the touch ceremony and hotplug lane separately when the user is available. If absent,
   retain `not-run-human-coordination` rather than expanding fake-protocol results into hardware proof.
6. If multi-key work is approved after serial125 baseline, allowlist exactly103+125 temporarily;
   run `FullyQualifiedName~PivMultiKeyContentionTests`, then restore both baselines independently.
7. Final independent session: verify defaultPIN, PUK usability/metadata, managementalgorithm/default/
   authentication/touch, all24normalkey/certslots, CHUID/CCC/KeyHistory/ADMIN/PRINTED andF9byte/SPKI
   preservation. Restore original allowlist bytes even after failures. Retain differences, not merely
   “reset succeeded.” A successful operation result and a successful cleanup are separate outcomes.

## Meaningful bounded probes

| Probe | Missing material fact and oracle | Bound/recovery |
|---|---|---|
| PureEd25519 + X25519 host binding | Existing AuditV2 verifies 10/64-byte Ed25519 messages. Add a scoped 103 probe for generated/imported X25519, with a host peer and independent raw-secret equality. Use both host directions or an RFC7748 vector where practical. | Ordinary 9D/retired slot; record seed/public encoding and byte order, clear owned secrets, verify before normal-state cleanup. Generation alone supplies no agreement evidence. |
| Policy enforcement | Provision reversible P256 PinAlways; verify PIN, sign and host-verify; immediately try a second sign expecting rejection; reverify and sign successfully. For PinOnce, distinguish two signs after one verification from fresh-session/card-state observations. | Use 9A/9C and known factory PIN. Treat disposal and physical PIN-state release as separate facts. |
| Key/certificate independence | Existing move tests bind the signature to the original public key; the certificate roundtrip stores an unrelated software-key certificate. If the task guide needs live proof, compare certificate/key SPKI, then move/delete the key and read certificates separately. | Two ordinary empty slots; preserve F9. Prefer one coherent probe over redundant CRUD checks. The application owns certificate binding. |
| Actual PivTool certificate task | Generate an ordinary P256/RSA key, obtain a self-signed certificate/CSR through the actual sample helper, verify its DER signature with the slot SPKI and compare SPKIs. | Compile in the coordinator's actual tool context; retain certificate ownership scopes. Record GUI export/CSR limitations while runtime fixes remain Pass2. |
| PIN-only recovery | Execute the existing dedicated-key test: randomized active key, close/reopen, recover, disable and default authentication. | Mixed/stale candidates and failure cleanup already have fake APDU/witness coverage. Real partial-write fault injection stays deferred unless a material unresolved claim depends on it. |
| Cancellation/drain, with coordinated touch | Request cancellation during a TouchAlways wait; observe the terminal task/callback, await drain/disposal, then perform fresh-session serial and same-key crypto controls before cleanup. | Cooperative cancellation differs from native abort. Keep the connection reserved until termination. An independent finite watchdog records a wedge instead of issuing concurrent RESET. |
| Optional cached touch | Distinguish the first touch, an immediate cached operation and a post-window touch; host-verify each signature and record PolicyMayRequire. | Separate human-coordinated dimension. Timings qualify serial103 USB only; fake policy tests already cover callback mapping. |

Bio metadata/mode payloads, enrolled MATCH policies, temporary PIN reuse and Bio MPE reset remain
separate capability/profile dimensions requiring appropriate hardware and coordinated fingerprint
interaction. NFC, older releases/NEO, FIPS products, other operating systems and Native AOT runtime
remain separate qualifications. This PIV packet records dependencies without opening a Core-wide
transport or CTAP framework investigation.
