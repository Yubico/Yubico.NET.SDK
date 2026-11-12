# Management boundary: completed B6 source follow-up

**Outcome:** Management's mandatory B6 source reconciliation is complete. The earlier
`PENDING-RETURN` questions now have caller-specific source dispositions and links to B6's existing
concern groups. Wire/profile decisions, fixes, independent review and hardware qualification remain
open in their respective records. **B4 Windows remains an OPEN mandatory follow-up.**

This addendum supplements [the original boundary report](../../boundary-Management.md), particularly
its sections 1–3, 5 and 8–9. The original report, 331-unit ledger, dispositions and verification
checkpoints remain byte-exact. [followup-B6.json](followup-B6.json) records their hashes, B6 input
identities and old-to-new statuses. It creates no new unit, accepted finding or coverage allocation.
The source pin remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD remains
`bb94f0884f63f260156227957d4cc112d984e300`.

The completed [B6 return](../../investigation-B6.md), final ledgers/index, ownership handoff,
inherited recommendations and final source register/manifest were consumed for this bounded review.
B6's 50 files / 684 IDs / 16 local concern groups stay its population. Relevant current Management
factory, SmartCard backend, reader, initialization, response and disposal paths were retraced below.
No product edits, SDK/provider/native/device execution, tests, formatter, central inventory changes,
commits, index/ref changes or subagents occurred. Static source/hash/record checks are the only new
verification. No original capture was fetched again.

## Follow-up status transitions

| Original B6-dependent question | Additive current disposition | Existing concern / remaining gate |
| --- | --- | --- |
| Partial handshake and failure teardown: pending B6 | **SOURCE-RECONCILED, cleanup gaps retained.** Factory cleanup owns its registered protocol, while SCP03 pre-state-return allocation failures have a narrower owner boundary. | CORE-B6-C006/C004; implementation and fault-injection verification open |
| Parameter aliases and disposal: pending B6 | **SOURCE-RECONCILED.** Management borrows creation parameters; contained owners and record aliases are explicit. Existing undisposed Default test inputs remain. | CORE-B6-C007/C009; `management-001:MGMT-IMP-004`, `security-domain-001:SD-DEP-002` preserved |
| Authentication roles/public flag: pending B6 | **SOURCE-RECONCILED WITH PROFILE QUALIFICATION.** Successful setup sets the flag; Management creation has no subsequent protected command. SCP11 a/c first-command phase and b's card-only authentication remain explicit. | CORE-B6-C009/C010/C015 and CORE-B5-C001/C006; final SCP11/vendor profile open |
| APDU grammar/chaining/reset: pending B6 | **SOURCE-RECONCILED, independent wire proof open.** Current Le, empty-data and protected continuation paths are known for each operation. | CORE-B6-C001/C003/C011/C012; ISO/profile/physical verification open |
| Response/status/secret-copy lifetime: pending B6 | **SOURCE-RECONCILED, scoped cleanup limits retained.** Response status shares clearable owner; exception fields are copied mutable values; private transport/formatter/decryption copies have separate owners. | CORE-B6-C002/C005/C013; no universal erasure or ongoing-auth guarantee |
| Windows native completion/claim release: pending B4 | **OPEN — unchanged.** This task supplies no Windows route qualification. | B4 return, then explicitly user-ready Windows delta |

“Source-reconciled” completes the requested dependency read. It does not accept a wire defect as intended
contract or change the original ledgers' pending review/verification states.

## Factory, authentication and failure ownership

`src/Management/src/IYubiKeyExtensions.cs:138–170` still chooses one transport, copies the preferred-type
setting and transfers hidden ownership only after `ManagementSession.CreateAsync` succeeds. SCP without
an explicit preference chooses SmartCard. `ApplicationSession.cs:323–348` registers the base protocol
and rejects SCP over HID before protocol initialization I/O. The B6 follow-up therefore changes only
SmartCard/SCP interpretations; FIDO/OTP Management writes and reads keep their original boundary result.

`ManagementSession.cs:122–146,212–229` performs Management SELECT and its initial best-effort DeviceInfo
read **before** `ConfigureProtocolAsync` establishes SCP. The discovered/overridden effective firmware
then reaches `ScpInitializer.cs:58–96`: SCP03 uses Feature 5.3.0 and SCP11 uses 5.7.2, including Core's
major-zero permissive policy. An override affects this setup policy without authenticating the earlier
metadata or establishing actual device support. CORE-MGMT-X002's captured firmware fallback remains
unchanged; successful SCP adds no discovery-cache update or metadata-ownership fix.

`PcscProtocol.Scp.cs:27–42` holds the base ExchangeGuard across the handshake. On success,
`ApplicationSession.cs:377–394` publishes the wrapped protocol, effective firmware and IsAuthenticated;
Management creates its backend and logs, then the common factory publishes IsInitialized. The bare
`CreateManagementSessionAsync`/direct factory performs **no protected Management APDU after SCP setup**.
The first later `GetDeviceInfoAsync`, configuration write or reset supplies that command. A one-shot
`device.GetDeviceInfoAsync` creates the session and then performs its protected read before disposing it.

For SCP03, `ScpState.Scp03.cs:50–92` sends INITIALIZE UPDATE, derives keys and verifies the card
cryptogram; `ScpInitializer.cs:121–168` subsequently sends EXTERNAL AUTHENTICATE with security level
0x33. Its post-state-return catch disposes state on external-authenticate/processor/encryptor failure,
and finally clears the returned host-cryptogram array. CORE-B6-C006 identifies provider failures after
SessionKeys allocation but before state return that lie outside this catch. Management's outer factory
cannot dispose an unreturned/unregistered state merely by disposing its base protocol. Also, the
initializer requests the DEK encryptor even though Management never uses it: a StaticKeys instance with
omitted DEK can fail creation after authentication (`ScpState.cs:61–64`, initializer:151). This is an
existing Core setup limitation, not a new Management requirement to perform key-management operations.

For SCP11, `ScpState.Scp11.cs:54–157` sends a/c certificates and the authentication command, derives
session keys and verifies the receipt; initializer:183–207 owns returned state on later setup failure.
The caller supplies the trusted SD public key and appropriate provisioned OCE credentials. Constructor
admission is wider than the fixed P-256/AES-128 processing profile; certificate association, trust,
validity and OCE-reference requirements retain CORE-B6-C009's qualifications.

The retained **SCP11 v1.3.0.13 Public Review**, §4.3, says a/c OCE authentication to the SD includes the
first correct-MAC APDU after establishment, and b authenticates the SD to the OCE only. The relevant
original-derived passage is `investigation/B6/sources/scp11-public-review-1.3.0.13.txt:1085–1119`.
Treat this as a draft-specific actor/phase distinction while final 1.4 and the exact YubiKey profile
remain unverified. Factory IsAuthenticated=true records successful Core setup; it supplies neither an
ongoing channel-health check nor proof that Management configuration-lock authorization has occurred.
The stored lock remains `DeviceInfo.IsLocked`, authorized by each write's CurrentLockCode.

`ConstructInitializedAsync` disposes a failed session through
`DisposeAfterInitializationFailure` (`ApplicationSession.cs:247–270,399–420`). Direct creation preserves
the borrowed connection. The convenience helper additionally awaits hidden connection disposal
(`YubiKeyConnectionExtensions.cs:261–287`). Original creation failure remains primary even when cleanup
fails; checked native release/claim handling stays with the connection owner. B6's local recovery latch
and B4/platform native quarantine are separate states.

## Operation-specific APDU and status consequences

| Management operation | Actual B6 mechanism and qualification |
| --- | --- |
| Initial SELECT and metadata probe | SmartCardBackend:34–41 selects the fixed public Management AID on the plain base protocol. Reader:143–158 issues INS 1D. A failed initial metadata read can fall back to SELECT firmware; optional SCP has not yet started. A plain continuation recovery latch can still cause later Configure to refuse, rather than make fallback a recovery guarantee. |
| Public live metadata read | ManagementSession:150–154 uses the effective wrapped protocol and captured `_version`. Each page is INS 1D with P1=page and empty command data. Under SCP, each page runs through ScpProcessor and may also have APDU 61xx continuations inside that page. Reader pagination is a different loop from APDU continuation assembly. |
| Configuration write / reboot request | SmartCardBackend:44–56 sends INS 1C with the length-prefixed TLVs. Even an empty DeviceConfig produces a nonempty length-prefixed payload. Codes are copied into that payload, protected if SCP is selected, and session:180–196 clears its encoding after the actual backend task. A command/status failure supplies no rollback of an accepted write/reboot. |
| Device reset | Session:201–209 selects SmartCardBackend:74–85, INS 1F, zero CLA/P1/P2 and no data. With SCP it uses the same protected empty-command path as metadata reads. No post-reset handshake, readiness invalidation, metadata refresh or transport reacquisition occurs. `management-001:MGMT-IMP-010` remains open for family/lifecycle/configuration-lock effects. |

**Empty commands and Le.** The current short and extended formatters always append Le
(`ApduFormatterShort.cs:23–51`, `ApduFormatterExtended.cs:27–66`). Plain reset therefore includes a
trailing zero Le: five short bytes or seven extended bytes, rather than the manufacturer's four-byte
absent-Le example. These are source-derived formats, not newly transmitted bytes. With SCP,
`ScpProcessor.cs:79–87` invokes Encrypt for empty data and `ScpState.cs:78–102` creates a padded block.
Thus public metadata reads and reset directly exercise CORE-B6-C011. The retained **SCP03 1.1.2 Public
Release**, §6.2.6, requires counter advancement without encryption for an absent command field
(`scp03-1.1.2.txt:1149–1171`). Keep that conflict and the unresolved supported-profile decision visible;
Python agreement and the current synthetic-card expectations do not establish conformity. B6-C001
also retains the external-authenticate absent-Le discrepancy and the unavailable exact ISO clauses.

**Response chaining and errors.** The secure graph is outer ChainedResponseReceiver → ScpProcessor →
command transmitter/chainer (`ScpInitializer.cs:98–108,153–155`). Each remaining command is protected;
the receiver uses configured instruction, default C0, and fixed zero CLA/P1/P2/Le
(`ChainedResponseReceiver.cs:27–28,42–70`). Management supplies no alternate graph. CORE-B6-C012's
independent split-ciphertext/R-MAC and bare-status oracle questions therefore apply to a live metadata
page whenever APDU continuation occurs. The original overlap test's two metadata pages are plain
scripted responses; they establish neither protected APDU continuation nor SCP interoperability.

Plain and wrapped protocol public methods throw on every assembled non-9000 status, including 62xx/63xx
warnings (`PcscProtocol.cs:124–138`, `PcscProtocolScp.cs:70–84`). ScpProcessor separately requires an
R-MAC for empty success/warning responses except INS 82 and accepts empty error responses before the
wrapper constructs ApduException (:133–174). Management operations use INS 1C/1D/1F, so the later-INS82
exception in CORE-B6-C005 is **not exposed by these public Management operation bodies**; genuine
SCP03 setup still uses INS 82. Preserve `audit-v2-20261022-20261031:N2`'s exact ordinary-command fix
without implying phase-gating, all-warning or response-chaining qualification.

The initializer translates only ClaNotSupported ApduException into NotSupportedException
(`ScpInitializer.cs:89–92`). Other setup statuses such as 6A88 remain ApduException; protected response
length/MAC/padding failures can surface as BadResponseException, and provider failures have their own
translation. Management adds no generic authentication-error remapping. The initial plain metadata
fallback catch is the specific exception-handling exception to normal propagation.

**Recovery versus public flags.** A reported secure failure marks the base protocol recovery-required
and future reads/writes/reset fail before another exchange (`PcscProtocol.cs:88–95`, wrapped:70–75).
CORE-B6-C004 retains the counter-mutation-before-provider-failure hole; the failure flag is narrower
than an assurance that every uncertain failure terminalizes the protocol. Bare application-error status
handling is also distinct from a cryptographic failure. Neither path updates ApplicationSession's
historical IsAuthenticated/IsInitialized values. Dispose/reopen after uncertainty; the unchanged flags
and a newly detached borrowed-session slot supply no proof of card/channel recovery.

Management selects its applet before SCP and has no subsequent public reselect operation. Its reads,
writes and reset therefore **avoid CORE-B6-C015's plaintext SelectAsync-after-SCP path**. Do not copy
the raw-session reselect example into a Management recovery procedure. No Management operation submits
a caller-selected APDU or uses the exposed DEK encryptor.

## Parameters, response storage and actual test scope

CORE-B6-C007 confirms the original boundary's owner table. StaticKeys copies original ENC/MAC/DEK
arrays; Scp03KeyParameters retains/disposes the same StaticKeys. SCP11 parameters copy the certificate
collection while retaining key/certificate objects; disposal reaches the private-key wrapper and those
certificates, leaving the public-key object. Record clones share these owners. Management retains no
creation parameter field and adds no owner clone: its derived SessionKeys are independent Core state.
Keep parameter owners valid through actual factory completion, and preserve the conservative example
scope that disposes session before parameters. Clear caller source secrets independently.

`management-001:MGMT-IMP-004` remains exact at the current callers:

- SimpleTests:198–214 owns its fresh Default parameters outside connection/session scopes.
- AdvancedTests:68–77 passes fresh Default inline, leaving its owned arrays undisposed.
- IYubiKeyExtensionsTransportTests:176–186 also creates Default inline; its connection refusal means
  no SCP handshake occurs, while the caller still owns that fresh parameter object.
- SimpleTests:222–241 owns StaticKeys for its wrong-key case, but its broad exception assertion can
  succeed before cryptogram verification. Source-array cleanup and contained-owner cleanup are distinct.

These are preserved inherited findings, not new B6 or Management bug allocations.
`security-domain-001:SD-DEP-002` / CORE-C-C020 now have their Management-specific containment/creation
disposition; Security Domain's separate retained reset parameters remain outside this task.
CORE-B6-C008's cached KeyReference byte representation is not consumed by Management's setup path:
SCP03 reads Kvn, SCP11 reads Kid/Kvn and OCE reference scalar properties. This bounds that dependency
without accepting the general cached-encoding/equality issue. CORE-B6-C016 ATR equality/hashing has no
new Management operation-specific use requiring a separate finding here.

`ApduResponse.cs:30–55,61–99` stores a copied array and computes SW/SW1/SW2 from its trailing bytes.
Clear zeroes both status and data aliases. `ApduException.FromResponse:109–124` snapshots the status and
command-header scalar values into mutable exception properties; later response clearing does not
retroactively zero those copied fields or its message. No hypothetical separate `_statusword` cache is
used to explain current behavior.

DeviceInfoReader:156–158 copies response data into another array. SmartCardBackend's write/reset awaits
discard their response. Public device metadata/status is nonsecret by default; their lack of explicit
Clear is not automatically a secret-erasure defect. Configuration lock codes and derived key material
are sensitive: session encoding cleanup reaches that one encoding, while CORE-B6-C002 retains the
formatter stack copies, separate transport result, fragments and decrypted-response temporary scopes.
Management's successful encoded-buffer observer proves its specific buffer cleanup only. It cannot
establish erasure of all downstream copies or provider storage.

CORE-B6-C013's synthetic/length/equivalence/fixture limits remain attached to its actual oracles.
The Management default-key success test reads serial metadata after creation; the wrong-key test's
broad failure is weaker. Neither ran here. B6's complete Python ECHO comparator covers nonempty
messages under its own graph, rather than Management's protected empty reads/reset or independent
failure/cleanup qualification. Ordinary Core APDU logging records headers/length; the fixed Management
AID is public. Fallback logging passes exception objects, so provider/transport-defined text still
retains CORE-B6-C014 / C's logging qualifications.

## Documentation reopening additions

All paths remain unchanged. The later Management editor and SHARED editor should reconcile the
following specific passages with the accepted B6 triage; existing X001–X003 IDs remain unchanged.

| Surface | Additional precise reopening trigger |
| --- | --- |
| `src/Management/src/ManagementSession.cs` CreateAsync XML; `IYubiKeyExtensions.cs` creation/one-shot XML; `src/Management/README.md` initialization/security; `src/Management/CLAUDE.md` initialization/SCP examples | Distinguish setup flag, first protected Management operation, SCP11 actor/profile and failure/liveness. Keep initial metadata explicitly pre-SCP and the optional-DEK creation limitation tied to C006 rather than inventing Management key-use policy. |
| `src/Management/src/IManagementSession.cs` read/write/reset failure remarks and reset example; `src/Management/src/Backend/SmartCardBackend.cs` reset/format explanations | Reopen on C001/C011/C012 formatting, protected empty command or response-continuation resolution. API-level no-data reset must not be presented as an exact absent-Le wire vector without the formatter qualification. |
| `src/Management/src/SetDeviceConfigOptions.cs` borrowed-code remarks; `DeviceConfig.cs` encoding cleanup remarks; `IManagementSession.cs` encoded-buffer remarks | Preserve correct local-buffer guarantee; reject extrapolation to all formatter/transport/response/provider copies under C002. A runtime copy-cleanup fix requires fresh full-path re-review. |
| `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs` SCP cases; `ManagementSessionAdvancedTests.cs` ModernFeatures; unit `IYubiKeyExtensionsTransportTests.cs` SCP-held case | Reopen parameter ownership, exact failure phase/status and execution claims with MGMT-IMP-004/C007/C013; transport refusal is not authentication rejection. Assertions/cleanup edits require separate authorization. |
| `src/Management/examples/ManagementTool/Features/DeviceReset.cs`; `src/Management/examples/ManagementTool/README.md` reset/recovery; `docs/migration/v1-to-v2.md` Management/SCP rows | Preserve MGMT-IMP-010 family/lifetime uncertainty; reset success or historical plaintext success cannot qualify SCP reset, lock recovery or automatic reauthentication. |
| Core session/SCP/response XML, Core README/CLAUDE, `docs/architecture/raw-access-tiers.md`, shared helper parameter guidance | SHARED/B6/B5 owners integrate cross-cutting corrections once; Management contributes the actual caller mapping above without a second wire-level finding. |

MGMT-XREF-001/002/003 remain `management-001:MGMT-XREF-001/002/003` with the original metadata and
fixture dispositions. This follow-up adds no FIPS, form-factor, discovery or cached-firmware closure.

## Preserved gaps and the remaining mandatory Windows handoff

B6's exact retained originals remain authoritative for their inspected scopes: SCP03 **1.1.2 Public
Release**, March 2019, SHA-256 `973a10834f7fe8befd98da9af65393622234b80745092bd85248804c57b648dd`;
SCP11 **1.3.0.13 Public Review**, August 2023, SHA-256
`5db47b3df3c4e565a5ad91257395604f3482ed5db70c64853544c3b0e289fde0`.
Code-cited SCP03 1.1.2.6 is a different review edition; its exact clauses remain unavailable. Final
SCP11 1.4 publication is known but its final clauses and complete YubiKey profile remain unverified.
ISO 7816-4:2020 catalogue/abstract evidence supplies no inspected target normative APDU clauses.
This task inherits those completed bounded gaps; it neither substitutes a draft for final authority
nor starts another research sweep. N2 and `audit-v2-20261022-20261031:#23` retain their exact prior
resolved-source scopes, separately from the later phase/copy/profile concerns.

Historical serial 125's two 6A88 INITIALIZE UPDATE failures occurred **before EXTERNAL AUTHENTICATE
and RESET**. They establish neither SCP establishment nor SCP reset execution. Any separately recorded
plain reset success keeps its plain scope. No new secure-channel hardware interoperability, key/device
provisioning, actual loaded provider/native behavior or physical reset success is established here.
Implementation, independent content/citation review, compilation and selected current execution remain
future gates. PIV physical deferrals stay intact.

**B4 remains OPEN** for Windows compatibility adapter disposal, native operation task completion versus
drain/release, uncertain claim retention, cancellation, CCID/HID access/grouping/error/ABI and how those
outcomes affect Management's borrowed/owned factory failure, read/write/reset and rediscovery paths.
The exact affected Management surfaces remain those in the original B4 row and this addendum's
failure/lifetime explanations. The later Windows concurrency delta additionally requires the user's
explicit ready handoff and a new pinned source/evidence comparison. B6 source reconciliation supplies
no implicit Windows closure or physical-card-state guarantee.
