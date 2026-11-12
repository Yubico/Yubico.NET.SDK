# Management boundary: completed pinned B4 source follow-up

**Outcome:** Management's original mandatory B4 source questions now have explicit caller-specific
dispositions. Together with [the B6 follow-up](followup-B6.md), this completes the **pinned Management/Core
boundary source investigation**. Candidate acceptance, independent content review, implementation,
compilation and native/device qualification remain pending. The user's **new Windows CCID/HID concurrency
work remains a separate future readiness handoff**, with no source or evidence inspection in this task.

This addendum supplements [the original boundary report](../../boundary-Management.md), especially its
B4 row in section 8 and pending-platform statements in sections 1–2, 5 and 9. Original report/unit-ledger
bytes and both B6 follow-up files remain unchanged. [followup-B4.json](followup-B4.json) records exact
input/source identities, existing unit anchors and additive status transitions. No new units, duplicate
defects, canonical IDs or central coverage changes are allocated.

Source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed metadata HEAD remains
`bb94f0884f63f260156227957d4cc112d984e300`. The completed [B4 report](../../investigation-B4.md), ledger,
candidate index, source register/captures and qualification/late-delta map were consumed for this
bounded caller reconciliation. B4 has **106 primary dispositions / 2,350 assigned IDs / zero unread**:
1,456 primary plus 894 shared IDs. Its precise reading scope is **105 complete primary-file reads plus
one whole Unshipped API snapshot's support-entry/type-inventory reconciliation**, with non-B4 signatures
explicitly related context. This follow-up adds no independent all-member API review or wholesale
Management re-audit. B4's ten groups remain provisional.

## Original follow-up status transitions

| Original mandatory question | Current source disposition | Existing group / remaining gate |
| --- | --- | --- |
| Windows HID open/task completion | **SOURCE-RECONCILED.** Slot, typed adapters and native reports use synchronous compatibility paths; async names supply no pre-return responsiveness guarantee. | CORE-B4-C001/C010; actual architecture/driver/runtime qualification open |
| Native cancellation / protocol cancellation | **SOURCE-RECONCILED.** Adapter tokens are unused; protocol entry/keepalive/touch-wait signaling is separate from native interruption. | CORE-B4-C001 and CORE-B5-C002/C007; no baseline CancelIoEx completion mechanism |
| Disposal, actual close and registry release | **SOURCE-RECONCILED WITH DEFECT QUALIFICATION.** Normal session disposal drains its admitted exchange first; direct concurrent Windows HID connection disposal can release a registration before a held native borrow ends. | CORE-B4-C001 extends CORE-B1-C005 nonadditively; no captured race or fix claimed |
| Windows SmartCard versus HID lifetime | **SOURCE-RECONCILED.** PC/SC retains its B1 worker/checked release route, rather than inheriting the HID wrapper's disposal model. | CORE-B4-C006/C009 support limits; B1 lifetime source scope preserved |
| Discovery/monitor/rediscovery consequences | **SOURCE-RECONCILED WITH QUALIFICATIONS.** Discovery's read worker retains the lease through its own read/cleanup; listener unregister and topology/native ABI evidence retain specific gaps. | CORE-B4-C002/C003/C008/C010 and A-C009; no freshness/native-quiescence guarantee |
| Mandatory B6 dependency | **SOURCE-RECONCILED — preserved.** The earlier additive B6 dispositions remain current for the unchanged pin. | B6 copy/phase/profile/wire gaps and MGMT inherited IDs remain open in their dimensions |
| New Windows concurrency implementation | **FUTURE USER-READY DELTA — unreviewed.** Separate from completing the pinned B4 dependency. | New source/package/operation identities and focused documentation-impact review required |

These are source-investigation transitions. B4-Q001–Q005 and B6's final/profile/ISO gaps retain their
explicit external qualification states; they are neither unread work nor implicitly resolved defects.

## Actual Management route and Windows HID boundaries

The original factory trace remains unchanged: `Management/IYubiKeyExtensions.cs:138–170` selects one
SmartCard → HidFido → HidOtp transport unless explicitly overridden; SCP defaults to SmartCard.
`YubiKeyDevice.cs:148–181,198–211` acquires known-member registration before opening. Windows HID's
`HidConnectionSlot.OwnsNativeLifetime` is false (:79–89), so it opens the synchronous report connection,
constructs FidoHidConnection/OtpHidConnection and returns a registered wrapper. Linux/macOS typed owners
instead accept transferred claims before native acquisition. This is a route distinction, not a
universal operating-system property inferred from IConnection.

`WindowsHidInterface.cs:102–106` creates WindowsHidIOReportConnection/WindowsHidFeatureReportConnection.
Their constructors (:35–50 in each) dispose the already-created report owner if report open fails.
`WindowsHidReportAccess.cs:214–224,274–278` uses CreateFile with NORMAL and swaps the metadata handle for
the selected report handle. The slot evaluates these calls **before Task.FromResult**. The typed
adapters call SetReport/GetReport inline before returning completed Tasks
(`FidoHidConnection.cs:32–60`, `OtpHidConnection.cs:32–62`). A Management factory/read/write can therefore
block its invoking thread along the synchronous portion of an async call. Whether an earlier await
already yielded is a separate scheduling observation.

FIDO uses GENERIC_READ|GENERIC_WRITE and ReadFile/WriteFile with a null OVERLAPPED pointer
(`WindowsHidReportAccess.cs:65–67,150–187`). OTP uses a zero-desired-access feature handle and
HidD_GetFeature/HidD_SetFeature (:108–147); both use FILE_SHARE.ALL. Preserve that meaningful access
split while retaining CORE-B4-C004: host collection/ACL/driver/token conditions determine availability.
Metadata caps success proves neither actual feature transfer nor universal zero-access rescue or
an elevation requirement. An exact comparative HIDAPI revision remains B4-Q004. Management factory
selection still propagates its one selected open/init failure without retrying another interface.
The standalone ManagementTool's SmartCard-only selector stays narrower than SDK HID support.

### Cancellation, disposal and recovery by operation

| Management path | Current Windows-specific consequence |
| --- | --- |
| Session creation | FIDO channel initialization or OTP status/version work can enter synchronous native reports. Pre-cancel guards can prevent dispatch; cancellation after entry into a report call supplies no native abort. Factory cleanup begins after the operation returns/throws, not at an unrelated outer timeout. |
| GetDeviceInfoAsync | Core's C2 FIDO or OTP capabilities-slot exchange uses these adapters for every report. Caller cancellation can be observed at protocol boundaries, including FIDO keepalive or OTP touch-wait logic; a blocked native report call must return before those loops can advance. No general completion deadline follows from their local poll windows. |
| SetDeviceConfigAsync | C3 FIDO or OTP set-device-info transmits the encoded TLVs. Cancellation cannot roll back accepted configuration/reboot. The session's finally clears its encoding after the actual backend task; keep caller code buffers valid until that task completes. |
| ResetDeviceAsync | Session dispatch still rejects both HID backends. On Windows this public reset uses the PC/SC route below, including optional SCP and the B6 qualifications. HID report cancellation observations are not reset-execution evidence. |
| Normal session disposal | ApplicationSession drains/disposes the protocol before disposing an owned connection; borrowed connections stay caller-owned. A Windows FIDO adapter supplies no terminal-wake implementation, so the registered forwarding wake is ineffective for a held baseline native read. Async disposal can remain pending for exchange drain. OTP also waits for its guard's admitted exchange. |
| Direct connection disposal concurrent with session/raw I/O | Registered HID wrappers call inner disposal, then release their lease via DisposalGate. SafeHandle native borrowing can defer physical close beyond that managed return. This is CORE-B4-C001's early-registration-release concern, rather than proof that ordinary correctly sequenced session disposal releases early. Avoid overlapping connection disposal/raw I/O with the session. |

Protocol details remain `FidoHidProtocol.cs:327–379,533–560` and
`OtpHidProtocol.cs:258–312,621–630`. FIDO CANCEL after a received keepalive is a device-protocol action;
it is not a request that interrupts the Windows ReadFile currently blocking a caller. The baseline
owner has no CancelIoEx/GetOverlappedResult request/completion lifecycle. Future CancelIoEx success
would only request cancellation; actual terminal result, borrowed-buffer/reference drain and close
must be observed separately.

For disposal, `RegisteredConnections.cs:60–106` uses the default release-on-failure DisposalGate;
`DisposalGate.cs:55–80` releases the registration after inner teardown invocation completes. Windows
report Dispose only calls SafeFileHandle.Dispose (:309–317); both report facades/adapters forward it.
B4's retained .NET v10.0.0 `SafeHandleMarshaller.ManagedToUnmanagedIn` takes DangerousAddRef at
29–33 and releases it at 44–50. The active native call retains the handle until return, while the
wrapper's registry token has no equivalent reference-drain observation. SafeHandle protection thus
protects the handle, without automatically protecting the grouped registration. The custom throwing-
Dispose case and the built-in successful-but-deferred-close case remain distinct facets of B1-C005.

Normal Management use should await complete operations, then dispose the session and its owned or
caller-owned connection in the established scope order. Reopen after uncertainty only once actual
work/cleanup has completed; a successful managed Dispose or a newly permitted registry open alone
does not prove the old Windows native call ended. This recommendation neither repairs the defect nor
promises native recovery. Per-exchange drain also leaves the original full multi-page workflow limit
intact, as already qualified by B5 and CORE-MGMT-X002/X003 context.

Windows report-ID staging is another distinct ownership scope (CORE-B4-C007). Typed send adapters
clear their first report copy, while WindowsHidReportAccess allocates an additional sendBuf for the
report ID and leaves it for GC. A configuration frame can contain sensitive lock-code bytes; clearing
the session encoding/adapter copy does not erase this nested array. Returned public metadata is ordinary
data and is not classified as secret merely because native code returned it. Successful short
ReadFile/WriteFile counts also use the captured last-error path; zero/stale last error can obscure a
short transfer. No actual short Windows transfer or end-to-end secret-erasure result is claimed.

## PC/SC, discovery and monitor recovery

Windows SmartCard opens retain the portable built-in PC/SC native owner, not the synchronous HID
compatibility wrapper. `PcscConnectionNativeState.cs:84–97,564–591` checks cancellation before native
invocation and publishes task completion after the native call returns. Shutdown closes admission,
drains accepted work on its owner worker, then checks native release (:141–155,368–375,416–453).
`PcscNativeResources.cs:110–161` requests UNPOWER disconnect and releases context; unsuccessful proof
retains the native claim/root through the existing unrecovered path. Errors in transaction/observer
reporting can coexist with release proof; a failed task alone does not classify native release.

Management's INS 1D reads, INS 1C writes and INS 1F reset reach this route through PcscProtocol or its
SCP wrapper. Strict sharing contention maps to ConnectionInUseException with a native cause
(`PcscNativeResources.cs:60–82`); other PC/SC failures remain raw-status-bearing SCardException.
HID failures can instead be HRESULT-derived or PlatformApiException. CORE-B4-C009 preserves backend
raw-code/symbolic differences rather than treating every transport failure as authentication rejection.
Requested UNPOWER, process-local admission and physical card/PIN state remain separate observations.

The default transmit path creates managed SCARD_IO_REQUEST and forwards it via NativeSCardConnectionApi
(`PcscNativeResources.cs:89–100`, `ISCardConnectionApi.cs:73–85`). CORE-B4-C006's PCI concern is
**conditional on the inspected published NativeShims producer/header route**: Linux x64/arm64 LP64
pcsc-lite headers use two unsigned longs, while managed PCI has two four-byte fields and the C shim
forwards its pointer. Windows' official PCI uses two 32-bit DWORDs and does not share this size mismatch.
macOS selects framework headers/linkage; Linux widths establish no Apple mismatch. B4-Q001 still requires
exact deployed binary/archive, producer source/build flags/patches and target headers. Managed worker
confinement does not convert an ABI carrier or prove a successful native transmit. Non-ASCII reader
name encoding and backend status labels retain their separate producer/environment qualifications.

Scoped Find and owned-monitor use remain the original A/Management model. In particular,
`SharedDeviceInfoRead.cs:219–245,261–292` runs the entire open/read/awaited cleanup on its dedicated
discovery worker and retains the untransferred Windows HID discovery lease until that work exits.
The caller's bounded wait does not dispose a live discovery connection concurrently to force release.
This ordinary discovery sequencing is different from the public direct-dispose race above. Timeouts
and scope retirement still end waits/admission, rather than prove native report completion.

After reboot/configuration, dispose old scopes, perform fresh discovery, identify the intended key and
read live state in new sequential scopes. Monitoring's listener hints are not device truth. B4-C002
adds a specific native teardown qualification: `WindowsHidDeviceListener.cs:139–145,269–305` ignores
CM_Unregister_Notification's return status before releasing callback state. Successful unregister is
the documented callback-drain observation; invocation alone supplies none. A new monitor object does
not itself establish that an old failed unregister released its native state. Management's read-only
monitor tests dispose monitors before live comparisons, but those assertions do not prove this native
failure path or successful unregister/finalization on the current Windows source/rig.

Windows ContainerId lookup remains optional evidence from a separate scoped PC/SC context
(`WindowsTopologyNativeOps.cs:35–107`, resolver:60–100), with synchronous work outside an SDK latency
budget. B4-C003's LPGUID/PULONG/BOOLEAN declaration concerns retain architecture-specific scope;
Windows x64 aggregate passing can mask one mismatch and gives no x86/arm64 acceptance. Matching
ContainerId values, GUID catalog provenance and fresh hardware identity are different evidence.
B4-Q003's particular CmClassGuid.SmartCard provenance gap neither invalidates every matched active
interface/property GUID nor proves that the unmatched value is wrong. CachedDeviceInfo, serial-latch
and partial-grouping limits in A and `management-001:MGMT-XREF-003` remain unchanged.

## Reopening map, oracles and preserved qualification triggers

No product or test changes were made. Link these contexts to the existing groups during coordinator
triage and fresh independent review; shared files retain SHARED editor ownership.

| Affected surface | Concrete B4 reopening trigger |
| --- | --- |
| `src/Management/src/ManagementSession.cs` factory XML; `IYubiKeyExtensions.cs` one-shot/factory XML; `IManagementSession.cs` sequencing/cancellation remarks; Management README/CLAUDE lifetime/constraints | C001/C010: show Windows synchronous invocation and actual native completion limits alongside Linux/macOS owners. Preserve truthful scope-order/drain guidance without claiming Dispose proves Windows native release in concurrent direct use. |
| `IManagementSession.cs` encoded-buffer remarks; `SetDeviceConfigOptions.cs`; Management security/lock-code examples and test commentary | C007 plus B6-C002: distinguish caller source, session encoding, adapter copy and nested report-ID storage. Keep the current local clearing guarantee; broader erasure needs implementation and real-buffer oracles. |
| Management scoped/owned-monitor/reconfiguration test descriptions; README read-only discovery pointers; shared discovery/physical-model/ownership guides | C002/C003/C008: preserve sequential scope assertions and unexecuted platform labels; reopen after listener/native ABI/identity evidence or new Windows teardown source changes. |
| `src/Management/examples/ManagementTool/README.md` permissions/recovery, module requirements, shared troubleshooting/error guidance | C004/C006/C009: retain actual SmartCard-only selector, raw error/backend/reader-encoding conditions and host-specific HID access. No universal elevation or cross-platform PCI-width assertion. |
| Existing transport/overlap/SCP test evidence statements | C001/C008/B6-C013: requested-type/attempt-list, fake overlap and broad exceptions supply their named managed oracle, not Windows close/cancel/native interoperability. |

B4's fake open-failure tests observe report-owner Dispose on a throwing open and owner retention on
success; they hold no native report call. Adapter forwarding tests and Management HID concurrency tests
do not establish native reference drain, actual cancellation or the early-release race. B4-C008's
Windows-excluded adapter registry and absent exact Windows artifact retain that limit. Historical
Linux 38 JIT + 38 AOT native-only checks and separately retained serial 103 physical results are not
new Windows or Management hardware qualification. No count or old method name substitutes for a
current per-operation terminal/drain/release trace.

| Existing qualification | Still-open trigger after source reconciliation |
| --- | --- |
| **B4-Q001** native producer/header | Supply exact RID/architecture, binary/archive hash, package/source/build/patch/header identities; later authorized successful transmit/non-ASCII reader evidence. No installed producer inferred from a published tag or local headers. |
| **B4-Q002** historical Windows artifact | Recover exact historical source/managed/native/OS/architecture/driver/token/fixture/output binding and operation oracles. x64 success, Linux compilation and historical prose do not qualify other architectures or this pin. |
| **B4-Q003** GUID provenance | Original Microsoft definition/version or source provenance for `990a2bd7-e738-46c7-b26f-1cf8fb9f1391`, exact unit `core-001:unit:d6efcc57eba79156`; preserve source-qualified status. |
| **B4-Q004** HIDAPI comparison | Supply exact external revision/platform passages or qualify/remove the comparison during authorized editing. Current Windows access code remains the actual behavior. |
| **B4-Q005**, `fixes-wave-001:CORE-001` | Original failing serial 9681620 Linux hidraw source/package/host/permission/device/output trace or separately authorized matching reproduction. Windows findings and serial 103 success do not establish its cause. |
| **B6 final/profile/ISO and hardware** | Preserve followup-B6.md's inspected final SCP03 versus review SCP11 authorities, final/vendor/ISO gaps, and serial 125's two pre-EXTERNAL-AUTHENTICATE/pre-RESET 6A88 results. No secure reset/interoperability credit is added. |

The original `management-001:MGMT-XREF-001/002/003`, `MGMT-IMP-004/008/009/010` and
`security-domain-001:SD-DEP-002` states retain their exact prior scoped outcomes. C001 extends B1-C005;
none of these aliases is counted as another defect. The original boundary's clean/candidate counts
and independent-review status stay as recorded, rather than being rewritten by this addendum.

## Separate future Windows ready-handoff delta

The user's new concurrency work is still unready and uninspected. On explicit handoff, pin its source
and retained evidence, then review only the changed/reached Windows report/request/completion/listener
and PC/SC paths; HidConnectionSlot/registry/registered wrappers/discovery transfers; B5 protocol/session
guards/terminal-wake/recovery; affected contract registries/tests/native hosts; and the Management/XML/
guide/test surfaces above. Reopen B4-C001/C002/C003/C004/C006/C007/C008/C009/C010 only where the delta
actually changes their mechanism or evidence. Dormant loader/token-helper C005 has no new Management
operation trace in this follow-up and remains its existing separate support concern.

Require distinct observations for admission, request submission, cancel request/result, actual native
terminal status/bytes, storage/callback/reference drain, close, release/quarantine and fresh reuse.
Any new async API/owner, overlapped I/O, cancellation or release-proof contract must be checked through
Management initialization, all supported read/write transports and SmartCard reset/SCP, then through
configuration/reboot rediscovery. Source investigation completion today supplies no acceptance of that
future implementation. Independent review, selected implementation work, compiler checks, physical
Management/SCP and Windows JIT/AOT/architecture qualification remain pending; PIV physical deferrals
stay intact. No build/test/native/device/formatter, commit/ref/index action or subagent ran here.
