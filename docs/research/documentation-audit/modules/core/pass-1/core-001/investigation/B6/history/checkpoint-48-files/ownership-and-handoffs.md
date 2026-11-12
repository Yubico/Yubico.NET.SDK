# B6 ownership and boundary checkpoint

| Resource / state | Actual owner and transition | Independent cleanup / qualification |
| --- | --- | --- |
| Caller APDU data | ApduCommand retains caller memory; copied record values share it | Keep unchanged until the actual exchange task is terminal; clear sensitive source in finally. An external timeout task is not that terminal task. |
| Formatted APDU | Formatter returns independent writable owner; transmitter clears it after actual transport await | Formatter's earlier stack copy is independent. A fake processor may retain an ApduCommand alias rather than a byte snapshot. |
| Raw transport response | Built-in transport returns an independent result (B1) | ApduTransmitter clones it into ApduResponse and leaves transport result intact; response Clear reaches only its copied owner. |
| APDU response | Sealed copied owner; Data/RawData and ordinary reference aliases share it | Check status before Clear because Clear also zeroes status. No write-cast is needed or proposed. |
| Chained response | Each fragment owner plus writer staging plus final copied owner | Fragment response arrays are dropped without Clear; writer Clear appears only on success. A failure callback establishes admission refusal, not erasure or card drain. |
| SCP03 source buffers | Caller owns original ENC/MAC/DEK bytes | StaticKeys clones them; disposal never clears caller input. |
| StaticKeys | Owns cloned arrays; Scp03KeyParameters retains/disposes the exact object | Do not dispose or mutate a contained owner while any parameter/session/reset flow still uses it. Fresh Default owns fresh default arrays. |
| SCP11 private/public keys | Key wrappers clone original parameter arrays (narrow C-owned dependency read); Scp11KeyParameters retains the wrapper objects | Parameter disposal clears the exact ECPrivateKey wrapper, not original private arrays or a separately created BCL key. ECPublicKey is retained and left intact. Its Parameters arrays are mutable aliases despite read-only property syntax. |
| SCP11 certificates | Parameter constructor copies collection but retains each X509Certificate2 object | Parameter disposal disposes those same certificate objects. Trust/chain validation and object disposal are separate responsibilities. |
| Parameter record clone | Record with-clone shares contained references | It is not a deep owner clone. Define lifetime before sharing; do not propose writable casts as consumer action. |
| Derived SessionKeys | Constructor clones all S-key inputs and optional DEK; ScpState owns it | Dispose zeroes/nulls its private arrays. No finalizer exists. Static keys and derivation staging remain separate owners. |
| SCP03 temporary derivation | StaticKeys output spans clear in finally; returned SessionKeys escapes | Card/host cryptogram failure between key creation and state return has narrower cleanup than initializer's later state guard. |
| SCP11 ephemeral private material | Provider returns ECParameters.D; initializer clears it after BCL key import; using disposes the generated BCL key | GetSharedSecret separately exports/clears D arrays and clears both agreement outputs. Reuse C's provider/native internal-copy qualification. |
| SCP11 receipt/key material | KDF helper clears shared secret, 80-byte derived material and computed receipt in finally | SessionKeys clones its four slices. Receipt becomes an independent MAC-chain copy. Public transcript copies/returned raw response are distinct. |
| MAC chain | ScpState replaces chain only after successful computation, clearing predecessor | Current response authentication links to current command chain; direct helper reuse/replay expectations are not a physical anti-replay proof. |
| Encryption counter | ScpState increments inside Encrypt before deriving IV | ScpProcessor's post-return boolean misses a failure after that internal increment. Signed-counter exhaustion has no terminal check. |
| DataEncryptor | Delegate captures SessionKeys DEK | Valid only within its session's live key lifetime; output is caller-owned ciphertext. Dispose/reopen and new keys are distinct from keeping the old delegate. |
| Base/SCP protocol graph | SCP wrapper owns secure processor/state then base; both share exact ExchangeGuard | They borrow connection. Guard closes/drains an admitted logical exchange before state disposal, without creating an OS transaction. |
| Recovery latch | PcscProtocol retains first failure and refuses subsequent local exchanges/configuration | This is protocol-instance state, not B1's native retained-claim quarantine. Session detachment or another protocol cannot certify card recovery. |
| Public auth/readiness | ApplicationSession publishes auth after successful SCP configuration; IsInitialized follows whole applet callback | Narrow source read confirms it is not recomputed on ScpProcessor failure/Select. Disposal admission masks both false. B5 owns final lifecycle/workflow interpretation. |

## Explicit handoffs

- **B5:** `ApplicationSession.cs` lines 76–86, 307–397 and 461–537 were read narrowly.
  ConfigureProtocolAsync assigns Protocol/FirmwareVersion/IsAuthenticated only after
  successful configuration/SCP; ScpProcessor's recovery callback never calls that
  setter. A channel can be locally recovery-required while the historical public
  authentication flag remains true until disposal/reset. Each individual APDU and its
  chaining is guarded; multi-exchange applet operations remain B5/boundary work.
- **C:** `ECPrivateKey` copy/clear/CreateFromValue/ToECDiffieHellman and ECPublicKey
  constructor/Parameters were read narrowly. Preserve C's private-array aliases,
  AsnWriter/runtime/native provider findings. Read-only conversion confirms the test
  helper's static scalar is 32 bytes; an initial visual miscount was corrected before
  return and supplies no consuming-fixture failure finding.
- **B1/platform:** no transaction is begun by PcscProtocol; built-in transaction
  scope native drain/release and raw input borrowing retain B1's actual owner.
  No synchronous/custom implementation inherits another platform's cancellation,
  task-terminal or native-release behavior by type/name alone.
- **SecurityDomain boundary:** constructor/factory retains `_scpKeyParams`, and
  certificate retrieval transfer/partial cleanup was read narrowly. CA-KLOC input
  trust provisioning differs from host-chain validation and returned bundle disposal.
  Retain SD-DEP-001..005 and the separate certificate-bundle resolved scope; do not
  reopen reset implementation or declare it verified from a plain successful reset.
- **OATH/OpenPGP/PIV/HSM boundaries:** retain applet auth/reset/profile decisions.
  Original serial 125's two 6A88 results precede EXTERNAL AUTHENTICATE/RESET, leaving
  SCP reset physical verification open. The OATH 5.6.3 policy origin and unknown
  rationale remain unchanged. HSM-IMP-008's 56-versus-48 response parser remains
  applet-owned; it is not another Core short-RMAC finding.
- **A:** device/public-status clone/alias findings remain with A. B6's KeyReference
  cached encoding is a separate actual local record implementation. No device metadata
  or discovery freshness is inferred from SCP parameters.
- **Windows:** use the pinned source and B4 return; user-ready concurrency delta
  is still a future checkpoint. PIV physical touch/hotplug/cancellation remains deferred.

These are narrow dependency statements with ownership referrals, not complete
reading dispositions for another packet's primary files.

## B5 completed handoff incorporated

After the coordinator announced B5 completion, its complete `investigation-B5.md`
and the relevant `B5/candidates.md` sections were read. B5's 43 files / 8,830 lines,
893 primary + 39 shared IDs, 716 clean / 212 candidate / four exclusions / zero unread
remain B5's counts, not added B6 coverage. Its nine local IDs are provisional.

Use **CORE-B5-C001** for configured/unknown/assumed firmware, raw readiness and
parameter-retention exceptions; **C004** for borrowed input through the actual task
and platform-scoped memory/admission; **C005** for uncleared incoming HID reports;
**C006** for per-exchange versus public-workflow lifetime and state publication;
**C008** for weak or held-test explanations; **C009** for current creation grammar.
B6-C002 is the separate APDU/secure-response copy graph. B6-C004/C005/C015 supply
actual SCP mutation/recovery/select mechanisms to those lifecycle explanations.
CTAPHID command/cancel grammar, CBOR normalization and OTP NEO/poll classification
remain B5-C002/C003/C007; B6 does not re-audit them or infer APDU behavior from them.

The newly read SCP11 Public Review §4.3 further separates receipt verification from
SD-side host authentication: a/c require the first protected post-establishment APDU
with a correct MAC. B5's historical IsAuthenticated setup flag must retain that
device-phase and applet-authorization qualification. The inspected final/vendor
profile gap remains, so this is not a new unconditional YubiKey behavior assertion.

Reopen the common Core README/CLAUDE, raw architecture and factory/auth XML through
those precise B5 groups when B6 wire findings are triaged. B4 remains active, and
the future user-ready Windows delta and PIV physical deferral are unchanged.
