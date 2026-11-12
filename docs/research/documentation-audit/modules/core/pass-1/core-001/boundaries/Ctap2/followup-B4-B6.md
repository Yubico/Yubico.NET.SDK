# CTAP2 boundary: completed B4/B6 follow-ups

**Outcome:** CTAP2's named **pinned B4 and B6 mechanism follow-ups are source-reconciled with
qualifications** at `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. This additive return changes neither
the original 590 unit identities/dispositions nor the 26 inherited source-run keys. The original boundary
report, candidates and verification remain preserved. [The companion JSON](followup-B4-B6.json) records
input/source hashes, exact read ranges, preservation digests and the static verification result.

Source reconciliation answers which paths run and which owners retain resources; it supplies no runtime
qualification, independent content acceptance or implementation repair. **BOUNDARY-WebAuthn remains OPEN**
because its return has not arrived. The user's new Windows concurrency delta also remains **UNREADY /
UNREVIEWED**. No unfinished checkout, branch or delta was inspected.

## Status and existing-group links

| Named follow-up | Additive status | Existing concerns and reopening scope |
| --- | --- | --- |
| B4 Windows HID open/admission/blocking/cancel/disposal/access | **SOURCE-RECONCILED; native/runtime/access qualification OPEN** | `CORE-CTAP-X001/X002/X018/X019/X022`; `CORE-B4-C001/C003/C004/C007/C008`, nonadditive `CORE-B1-C005` and B5 framing/lifetime groups |
| B4 built-in CCID route, native release and PCI producer condition | **SOURCE-RECONCILED; deployed producer/ABI/device qualification OPEN** | `CORE-CTAP-X004/X017/X022`; `CORE-B4-C006`, `B4-Q001/Q002`; portable B1 ownership retained |
| B6 CTAP APDU format/chaining/status and development configuration | **SOURCE-RECONCILED; profile/ISO and recovery repairs OPEN** | `CORE-CTAP-X004/X017/X018/X022`; `CORE-B6-C001/C002/C003/C012` |
| B6 SCP setup/rebinding/authentication phase and failure ownership | **SOURCE-RECONCILED; final SCP11/vendor/crypto/device qualification OPEN** | `CORE-CTAP-X002/X004/X017/X020`; `CORE-B6-C002/C004/C005/C006/C007/C009/C010/C011/C012/C015` |
| WebAuthn actor/recovery/result dependencies | **OPEN — separate mandatory return pending** | `ctap2-001:CTAP-DEC-002`; `webauthn-001:WA-IMP-010`; routed WA-IMP-006/007/013/017/019. Old documentation-resolved WA-XREF-001/CTAP-XREF-001 stays resolved only for its original prose scope. |

These are associations with existing local groups, not new accepted IDs or combined defect counts.
B4 completed 105 full primary-file reads plus qualified whole-snapshot support/type reconciliation,
covering 2,350 assigned IDs; it did not independently accept every non-B4 API signature. B6 completed
50 files / 684 IDs / 16 local concerns. Their provisional findings and external qualifications retain
their original owners; neither audit is repeated here.

## B4: actual CTAP2 Windows and CCID outcomes

The device factory still chooses one transport, implicitly SmartCard for SCP, and never retries a failed
HID open as SmartCard (`IYubiKeyExtensions.cs:140–207`). Core binds/registers the protocol, rejects HID
SCP before INIT/GetInfo, then prepares HID. The baseline Windows slot calls `ConnectToIOReports()`
before `Task.FromResult`; its FidoHidConnection adapter calls report Get/Set inline and ignores its token.
WindowsHidReportAccess opens FIDO with GENERIC_READ|GENERIC_WRITE, FILE_SHARE.ALL, FILE_FLAG.NORMAL and
uses synchronous ReadFile/WriteFile with null OVERLAPPED. Thus factory/INIT/GetInfo and later CTAP calls
can block their invoking thread before returning a task. An async method name grants no stronger bound.

Core still requests presence on a user-presence keepalive, observes caller cancellation between report
reads, sends at most one CANCEL and drains the original response. On Windows this cooperates with the
same synchronous report route; it supplies no native read interruption or completion deadline. The
registered wrapper implements terminal-wake forwarding, but the inner compatibility adapter has no
terminal-wake implementation. Disposing an owned session first waits for its admitted protocol exchange;
that wait can remain pending while the report call is held. A direct session still borrows its connection.

B4-C001's concurrent raw-disposal concern has a separate scope: another thread can dispose the raw
Windows connection while a generated native call retains a SafeHandle reference. Managed Dispose can
return before deferred native close, and the registered wrapper can then release its registry token.
The SafeHandle protects the active native borrow, not the registry token. This is a source-visible
ordering gap for that path, not an observed race or a claim that correctly sequenced session disposal
always releases early. Await complete workflows and their owning disposal; avoid raw/disposal interleaving.
Per-exchange drain still leaves multi-exchange workflows and post-exchange callbacks/parsing under X002.

The adapter clears its outgoing packet copy; Windows adds another report-ID-prefixed send buffer and
copies returned input from separate native staging. Those nested arrays remain independent cleanup
scopes under B4-C007 and X019. Successful short native transfers also retain B4's exact-count/error
candidate; the CTAPHID fixed-length check alone does not establish a successful full native transfer.

Access depends on the collection, ACL, token, sharing and driver. CTAP's test helper catches any Windows
UnauthorizedAccessException during session creation and offers elevation/close-software advice
(`Ctap2TestStateExtensions.cs:148–150,180–198`). That diagnostic identifies neither exclusive ownership
nor a universal elevation prerequisite; B4-C004 applies without changing its executable string.
Metadata/ABI/header observations and historical Mac/Linux evidence provide no Windows acceptance.

**CCID uses a different owner.** The actual path is UsbSmartCardConnection → PcscConnectionNativeState's
single worker → PcscNativeResources.Transmit → NativeSCardConnectionApi → SCard.Interop → NativeShims.
Accepted raw input remains borrowed until native return; the worker rejects ordinary overlap, drains
accepted work before shutdown, checks disconnect/context release and releases or retains the original
claim according to native-release proof. Protocol cancellation after exchange admission passes None.
This source ordering is distinct from Windows HID's compatibility wrapper; it provides no OS/device
latency bound or proof that requested unpower erased physical applet state.

B4-C006 now establishes a **conditional selected-route ABI discrepancy**: the managed PCI carries two
four-byte fields, and published NativeShims 1.18.1 forwards its pointer without field conversion. The
inspected Linux LP64 pcsc-lite producer/header route uses two unsigned-long fields, sixteen bytes in
that target. Windows uses two 32-bit DWORDs; macOS framework selection remains separate. Published build
recipes, local header text and managed source do not bind the installed shared/static binary to its
producer/headers or demonstrate a transmit failure. B4-Q001 remains open, including reader-name encoding.

## B6: actual CTAP APDU and SCP applicability

SmartCardBackend selects FIDO and sends a plain GetInfo before the detected USB firmware gate,
configuration and optional SCP. Its operational APDU is CLA=0x80 / INS=0x10 / P1=P2=0 / Le=0, with
at least the CTAP command byte in Data. Both formatters append a wire Le; zero is encoded, rather than
meaning absence. Short data fragments carry up to 255 bytes; extended formatting uses Core's complete
command ceiling. Initial SELECT/GetInfo precede options-based reconfiguration, so ForceShortApdus cannot
govern those earlier probes. Later major-zero configuration explicitly chooses connection support and
returns before ordinary ForceShortApdus handling (`PcscProtocol.cs:162–195`): X017's exact question is
answered, with the policy/implementation decision retained as B6-C003.

Plain response chaining follows 61xx using CLA/P1/P2/Le zero and configured instruction (default C0),
ignoring SW2; there is no 6C resend. Core assembles responses and checks APDU status before the CTAP
backend reads its first CTAP status byte. APDU errors and warnings therefore remain APDU failures,
not CTAP KeepAliveCancel/timeout outcomes. SELECT's broad NotSupported wrapper retains the inner error.
Plain first-fragment uncertainty can miss the offset-based recovery callback; native release, session
detachment and a later new protocol supply no proof of card recovery (B6-C003/X018).

### Secure graph and narrowly applicable defects

The configured secure path is **ChainedResponseReceiver → ScpProcessor → command-only
transmitter/chainer**. It shares the base protocol's exchange guard. Each remaining command is protected
and each response is un-MACed/decrypted before outer assembly. This is the actual CTAP call path, not
an independently qualified transparent-SCP-chaining claim.

- **Empty commands (B6-C011):** ordinary CTAP INS 0x10 requests are nonempty, including GetInfo/reset.
  The empty-data path is reached by secure remaining commands such as default GET RESPONSE C0 after a
  61xx result. Current Encrypt pads/encrypts that empty field. Retained SCP03 **1.1.2 Public Release,
  March 2019, §6.2.6** directs the off-card entity to increment the counter while applying no encryption
  when the command data field is absent. Counter sequencing, empty-command encryption and empty-response
  authentication remain distinct requirements. This source discrepancy adds no new CTAP ID or YubiKey
  interoperability verdict; final SCP11/vendor profile gaps stay attached.
- **Counter/failure notification (B6-C004):** Encrypt increments before IV/AES work completes, while
  ScpProcessor sets `stateAdvanced` after Encrypt returns. Failure after that internal increment can
  miss recovery notification. Other post-mutation failures normally mark the base protocol recovery-required;
  this local latch is separate from native retained-claim quarantine. ApplicationSession's historical
  IsAuthenticated/readiness flags are not recomputed by that callback. The per-exchange guard adds no
  workflow lease or rollback, and a provider failure is not inferred from source alone.
- **INS 0x82 exemption (B6-C005):** setup intentionally uses it for SCP03 EXTERNAL AUTHENTICATE with
  encryption disabled. Later ordinary CTAP commands use INS 0x10 and default remaining INS C0, so they
  do not directly select that exemption. The generic processor's instruction-only check still lacks
  a handshake-phase restriction. Caller-configured `InsSendRemaining` also remains unvalidated here;
  choosing 0x82 could reach that instruction path during continuation, without making it a supported
  FIDO/profile operation. Preserve the existing generic phase candidate; no bypass exploit is asserted.
- **Protected chaining/errors (B6-C012):** retained SCP03 §6.2.5 makes statuses other than 9000/62xx/63xx
  status-only and unprotected. The inspected fake signs 61xx and 6A82 and expects padded empty commands.
  Its cancellation/no-replay assertions establish that synthetic transcript only. Split ciphertext,
  split R-MAC, conforming bare errors and actual CTAP/SCP device behavior remain qualification work.

### Setup, applet state and cleanup

Configure/SCP success returns an effective wrapper; CTAP2 rebinds its backend without selecting FIDO
again. SCP03 sends INITIALIZE UPDATE and EXTERNAL AUTHENTICATE; SCP11 sends its applicable certificate
and authenticate commands. These inspected helpers issue no additional applet SELECT. The factory
also sends no post-handshake protected CTAP command before returning. Thus the source order is settled,
while **actual applet-selection survival and FIDO-over-SCP product acceptance remain vendor/device
qualifications**. Advising a plain SELECT after SCP would enter B6-C015's old-graph/plain-selection
problem and is not a supported recovery substitute.

For SCP11 a/c, retained **1.3.0.13 Public Review, August 2023, §4.3** distinguishes host receipt verification
from SD-side OCE authentication, which includes the first post-establishment correct-MAC APDU and the
applicable certificate/allowlist policy. CTAP creation can publish its historical authentication flag
before that first protected command. Final SCP11 1.4 and exact YubiKey profile clauses remain unverified;
neither that flag nor SCP success grants CTAP PIN/UV permissions or host management authority.

The factory borrows SCP parameters for creation. Scp03 parameters retain/dispose the exact StaticKeys;
Scp11 parameters retain the key/certificate objects and dispose the private key and certificates.
Record clones share contained owners. Keep caller parameter/source scopes separate from the session's
derived keys, and retain the current outer-parameter/inner-session example scope (B6-C007/X004).

On setup failure, Core disposes the registered protocol; direct creation leaves the caller's connection
owned by that caller, while convenience creation awaits opened-connection cleanup. After a successful
state return, initializer guards dispose state on later setup/encryptor failure. B6-C006's earlier
SCP03 cryptogram-derivation failure window can precede that guard. The original CTAP cleanup claim is
therefore reconciled only for reachable owners, never as complete secret erasure.

Operational copies remain separate: formatted payload clears after the actual transport task, but
formatter stack copies, raw transport result, per-fragment owners, failure-path assembly, protected
response owner and decrypted temporary cloned into ApduResponse retain B6-C002's scopes. If Core throws
an APDU error before returning, SmartCardBackend has not assigned `responseData`; its finally cannot
clear that inaccessible response. A returned CTAP error normally clears the backend Data view, while
success transfers it before typed decode. X004 and `ctap2-001:CTAP-IMP-004/027/035` retain their identities;
X020's independent diagnostic-callback failure path remains unchanged.

## Reopening and remaining gates

| Evidence or change | Exact reopening paths / owner |
| --- | --- |
| Windows HID owner/admission/terminal wake/checked release, or separately announced ready delta | Core HidConnectionSlot, RegisteredConnections, FidoHidConnection, WindowsHidIOReportConnection, WindowsHidReportAccess and raw-tier lifetime guidance; CTAP IYubiKeyExtensions, Ctap2Session/ICtap2Session, both module guides, cross-layer test commentary and TestStateExtensions Windows diagnostic. B4/platform owner plus fresh ready-delta reviewer. |
| Actual CCID producer/header, reader encoding or release behavior | Core SCard.Interop/SCardIORequest, NativeSCardConnectionApi, PcscNativeResources/NativeState and compatibility/AOT guidance; CTAP SmartCard/transport test commentary and factory prerequisites. NativeShims producer plus B1/B4/platform owners. |
| APDU/configuration/recovery/secure-chain repair or chosen profile | Core PcscProtocol, formatters/transmitter/receiver, ProtocolConfiguration, ScpInitializer/Processor/State/PcscProtocolScp; CTAP SmartCardBackend, factory XML, README/CLAUDE, SmartCard/transport tests and raw-tier guidance. B6/Core and CTAP editor. |
| Setup/auth phase, parameter alias or response-copy repair | ApplicationSession/SCP parameter XML and examples; CTAP factory/ICtap2Session, backend ownership, security/presence sections and actual lifecycle assertions. Preserve successful/failed/private-copy/native scopes and whole-workflow caveat. |
| WebAuthn return | **OPEN:** BOUNDARY-WebAuthn owns current ceremony/result/prompt trace, WA-IMP-010 recovery and CTAP-DEC-002 native actor/privacy/serializer decision, plus the routed WA ownership/extension aliases. No field stripping or parser repair authorized; earlier resolved prose is not this return. |

All five B4 external gaps remain: **Q001** deployed producer/header; **Q002** exact historical Windows
runtime artifacts; **Q003** SmartCard class-GUID provenance; **Q004** unpinned comparative HIDAPI revision;
**Q005** historical serial-9681620 failure cause. Q003/Q004 are retained family source qualifications,
not newly diagnosed CTAP exchange failures. B6 retains code-cited SCP03 review-edition, final SCP11/vendor,
exact ISO clauses, hidden-buffer/oracle and runtime/native/device qualifications. Inspected final SCP03
clauses never stand in for unavailable final SCP11/ISO passages. Published NativeShims source still
does not identify the installed producer. Linux native runs and compiled Mac source qualify neither
Windows runtime nor CTAP/SCP physical interoperability.

These named B4/B6 **source-reading dependencies are completed**, with implementation/policy, independent
review and qualification gates still open. The new Windows delta and WebAuthn return are separate
mandatory follow-ups. PIV physical deferrals remain in force. Only this file and its companion JSON were
written; no product changes, builds/tests, crypto/native/device execution, formatter, central inventory,
index/ref changes, commits or subagents occurred.
