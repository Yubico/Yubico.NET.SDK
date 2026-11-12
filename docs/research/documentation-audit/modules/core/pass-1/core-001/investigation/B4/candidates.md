# B4 candidates — completed pinned investigation

Source: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. These local IDs reserve this
packet's next candidate numbers. They allocate no accepted `CORE-001`, `CORE-DOC`
or `CORE-IMP` finding. Ten groups remain stable. Checkpoint 1 is preserved under
`history/checkpoint-1/`; the active ledger supersedes its partial coverage.

Every product path mentioned below is unchanged. The recommendation is explicit
future disposition by the primary editor and independent reviewer, including
paths whose existing wording is retained. Shared narrative changes belong to
SHARED. Runtime changes and changed test assertions belong to separately selected
Pass 2. Source/assertion reading adds zero executions.

## CORE-B4-C001 — Windows compatibility lifetime and early claim release

**Documentation:** describe Windows' actual synchronous compatibility route beside
the macOS/Linux owner routes. **Runtime/policy:** extend `CORE-B1-C005`
nonadditively to a successful Dispose while synchronous native I/O is still active.
**Qualification:** pending on every Windows architecture/device lane.

`HidConnectionSlot.OpenRawConnectionAsync:82–85` creates the synchronous report
connection before Task.FromResult. `FidoHidConnection.SendAsync/ReceiveAsync` and
`OtpHidConnection.SendAsync/ReceiveAsync` call SetReport/GetReport inline and ignore
their token. `WindowsHidReportAccess:214–224` opens FILE_FLAG.NORMAL; ReadFile and
WriteFile at 155/183 use a null OVERLAPPED pointer. There is no baseline
CancelIoEx/GetOverlappedResult import, native-pending request object, native I/O
admission gate, shared disposal task, or checked-close outcome in that owner.

A synchronous native call borrows SafeFileHandle through its generated marshaller.
.NET 10 SafeHandleMarshaller.ManagedToUnmanagedIn takes DangerousAddRef and releases
it on Free. Calling `_handle.Dispose()` at WindowsHidReportAccess:316 during a held
ReadFile can close managed admission while deferring the actual native close to
the last reference release. Windows report facade Dispose/DisposeAsync simply
forwards this call. RegisteredFidoHidConnection and RegisteredOtpHidConnection use
DisposalGate's default lease-on-failure policy; after the inner Dispose returns,
the registration can be released while the other thread is still in native I/O.
On a custom adapter's throwing Dispose, B1-C005's unknown-release case also applies.
The built-in path's missing release observation and the custom path's arbitrary
teardown contract remain distinct. Protocol guard drain can protect a correctly
owned session exchange, but direct raw calls and connection disposal on another
thread still need their own contract.

The SafeHandle protects the active native handle from premature physical close;
its protection does not extend the registry token automatically. This is a
source-backed ordering concern, not a captured Windows race. A caller's task
submission, method invocation return, actual native terminal status, reference
drain and release proof must be recorded separately in future tests.

**Current oracles:** WindowsHidConnectionOpenFailureTests asserts a fake report
owner was disposed when open throws, and was retained after successful feature
open; it exercises no blocked native call or native close. PublicApi adapter tests
remain B1's read oracle for forwarding/copy cleanup, rather than Windows native
acceptance. No current assertion read here establishes the early-release case.

**Reopen:** WindowsHidReportAccess; both Windows report connections; the two typed
compatibility adapters; HidConnectionSlot; RegisteredConnections and DisposalGate
(B1); IFidoHidConnection/IOtpHidConnection/IHidConnection/IHidInterface XML (B5/B4);
Core README/CLAUDE, raw-access-tiers, connection-ownership-and-contention and the
adapter/public-sync registries. A new Windows owner, admission rule, actual
cancellation implementation or checked native release requires a focused delta.
Retain accurate existing Windows/custom limitations until that evidence arrives.

## CORE-B4-C002 — Listener shutdown proof, partial startup and disposed restart

**Documentation:** qualify callback/finalizer assurance and timeout-test commentary.
**Runtime:** preserve native registration/context/delegate through actual successful
unregister; refuse Start after disposed. **Policy:** choose quarantine/retry on
unregister failure separately. **Qualification:** pending Windows native fault lane.

WindowsHidDeviceListener.Stop:139–145 and Dispose(false):297–305 ignore
CM_Unregister_Notification's CONFIGRET and free the GCHandle regardless. Microsoft
documents that unregister waits for pending callbacks, prohibits calling it from
the callback, and reports success versus CR_* failure. Success therefore supplies
the native drain relevant to releasing callback state; simply invoking it supplies
no successful-unregister observation. Restart after such an error discards the old
registration identity. Start:59–125 has no disposed check: Dispose sets `_disposed`
and suppresses finalization, but a subsequent successful Start can allocate a new
registration that a later Dispose's early-return path leaves live. This conflicts
with IDeviceChangeListener.Start's ObjectDisposedException contract.

The weak GCHandle allows the target to disappear before finalizer unregister.
The static callback drops a collected target, but GCHandle recovery/Target access
and its diagnostic log precede the try at line 168; the catch's log can also throw.
Containment of all managed exceptions crossing the unmanaged callback boundary
needs its own evidence. A failed Start's logging paths can likewise throw before
ClearRegistrationState. These are failure-path candidates, not a claim that normal
successful CM registration leaks or that all finalizers are unsafe.

**Current oracles:** CfgMgr32InteropTests checks 416 bytes on any host; its Windows
case asserts Started then Stopped using the real OS. WindowsHidDeviceListenerTests
checks bounded UTF-16 parsing only. HidDeviceListenerDisposalTests accepts Error
after Start, and its two asynchronous disposal cases only compare Task.WhenAny's
winner without awaiting the disposal task. The integration disposal case also
checks status, but never awaits that task's result. These can miss a fast fault;
their time budget bounds the test's wait, rather than proving native drain or
cleanup after a failing assertion. No finalizer/failed-unregister assertion is
present in these read files. The quiet test's zero events also succeeds if Start
failed, so it establishes no successful native registration by itself.

**Reopen:** Windows listener, native CM unregister declaration/comments,
HidDeviceListener and IDeviceChangeListener lifecycle XML, the four named listener
test files, Core listener guidance, ownership guide and A's monitor teardown
explanation. Preserve parsing tests' precise and useful bounded-buffer oracle.

## CORE-B4-C003 — Windows ABI declarations need architecture-specific repair

**Documentation:** explain exact native carriers. **Runtime:** selected Pass-2 ABI
corrections. **Policy:** preserve public numeric/API shape unless separately agreed.
**Qualification:** x86/x64/arm64 JIT and AOT remain separate pending cells.

Microsoft's pinned cfgmgr32.h declares LPGUID for both interface-list calls;
Cfgmgr32.Interop:284–299 passes Guid by value. Microsoft's x64 ABI passes a 16-byte
aggregate indirectly, which can make this declaration happen to interoperate on
x64; that coincidence supplies no x86 or arm64 guarantee. No universal Windows
enumeration crash is inferred from this declaration mismatch.

CM_Get_Device_ID_Size's PULONG and the two property APIs' PULONG sizes are declared
as out/ref IntPtr (Cfgmgr32.Interop:214–235,324–350). Windows ULONG stays 32 bits
across its architectures. The address itself is pointer-width; the pointed-to
integer is not. Current property calls start with IntPtr.Zero, so writing its low
32 bits can leave a zero high half on little-endian x64; record the mismatch
without inventing a demonstrated allocation failure. CmPropertyAccessHelper also
parses the type from the first probe while discarding the second returned type,
decodes the whole allocated string including its NUL, and uses only Debug.Assert
for several exact-size expectations. Property mutation between calls and CR_BUFFER_SMALL
need explicit later policy/tests. CmDevice.GetDevicePaths performs only one size/read
pair; the original API recommends retrying when hotplug grows the list.

Microsoft hidsdi.h returns BOOLEAN (one byte) for the HidD_* functions. All seven
declarations in HidD.Interop use UnmanagedType.Bool (Win32 BOOL, four bytes).
Kernel32's ReadFile/WriteFile correctly use BOOL and must keep that different
representation. HIDP_CAPS' native fields are unsigned USHORT; managed short has
the same width but changes value semantics above 32767, including report lengths.
No exercised return-register corruption or huge-report device is claimed.

**Oracles:** the 416-byte filter test never checks parameter indirection,
PULONG size or BOOLEAN returns. Pure symbolic-link tests use offset 24 and UTF-16
and supply no calling-convention proof. Existing successful macro-width Windows
traces, if retained elsewhere, need exact artifact/source/architecture matching.

**Reopen:** Cfgmgr32.Interop, CmPropertyAccessHelper, CmDevice, HidD.Interop,
IWindowsHidReportAccess and WindowsHidReportAccess; Windows enumeration/topology
explanation; native contract tests, public support metadata and A-C009's topology
qualification. Keep System32 import search restrictions; this concern is ABI,
rather than permission or grouped-identity proof.

## CORE-B4-C004 — Elevation and feature access claims need their actual conditions

**Documentation:** correct the unconditional elevation sentence and overbroad
feature-access rationale. **Runtime/policy:** no access-mode change proposed here.
**Qualification:** recover the exact historical Windows test artifact before using
its 10/10 observation to support this source baseline.

WindowsHidReportAccess distinguishes zero-access metadata/OTP feature handles from
GENERIC_READ|GENERIC_WRITE FIDO handles, with FILE_SHARE.ALL and OPEN_EXISTING.
Preserve that meaningful split. CreateFile's zero-access metadata allowance and
share compatibility rules are API contracts, while device ACLs, OS-reserved
collections and host token policy determine actual availability. The API does
not establish a universal requirement to elevate every FIDO process.

The secondary B1 platform-notes parent is exactly
`core-001:unit:9fe6da15d252bcd2`, ownership guide lines 212–220. Its line 218 says
Windows FIDO read/write requires elevation. Recommend environment-specific access
guidance instead, after ordinary availability/ACL/contention checks. The native
exception message itself already says the environment *may* require elevation;
it is retained executable output. The stronger “usually” in HidEnumerationTests
is also executable output and stays unchanged in Pass 1.

OpenFeatureConnection:98–105 claims zero access is a superset, always rescues a
read/write failure, and has no sharing conflicts. The original API evidence here
supports the selected desired-access distinction; it leaves those universal
driver/environment claims unestablished. Feature GET's documented report buffer
includes a zero report-ID byte for unnumbered reports and the caps-defined size.
Metadata GetPreparsedData/GetCaps success alone tests no feature transfer. Keep
the useful explicit distinction already present in lines 80–86, but bind the
historical run separately rather than promote its count to current acceptance.

**Oracles:** the open-failure fake selects which method throws; it never records
CreateFile desired access, sharing, token elevation or real HidD success. Current
enumeration uses CM properties and deliberately opens no report handle. Process
registry claims constrain known grouped IDs, while FILE_SHARE.ALL permits compatible
OS opens; neither controls physical applet authentication state or all other
processes/PCSC handles.

**Reopen:** the exact B1 secondary parent (without another primary count),
OpenIOConnection/OpenFeatureConnection XML, Windows troubleshooting/Core requirements,
IHidInterface direct-open XML and integration test commentary. Preserve cross-process
PC/SC policy and the PIV physical deferral. No Linux permission finding is imported.

## CORE-B4-C005 — Dormant dynamic loader declaration and lifetime explanation

**Documentation:** remove unsupported .NET Framework cleanup history and describe
actual function/library ownership. **Runtime:** exact Windows entry point and
captured-last-error handling need selected repair. **Policy:** library search and
delegate-after-owner-disposal contracts need a decision. **Qualification:** unrun.

Native/NativeMethods.LoadLibraryEx:47–49 has Utf16 but no EntryPoint="LoadLibraryExW"
and no SetLastError=true. LibraryImport resolves the exact method/EntryPoint spelling;
it supplies no DllImport-style A/W probing. WindowsUnmanagedDynamicLibrary.OpenLibrary
then calls GetHRForLastWin32Error. The source therefore has both a binding concern
and missing captured-last-error contract. Its LoadLibraryEx flags argument is zero;
the System32 restriction on importing kernel32 does not constrain the target
library's dependency search automatically.

Static repository search found no shipping caller of UnmanagedDynamicLibrary.Open,
only the loader implementations and API support declarations. Retain the concern
as latent support code, rather than attributing present NativeShims load failures
to it. Actual LibraryImport NativeShims resolution follows the runtime/package or
static AOT route. TryGetFunction returns a delegate after GetProcAddress's temporary
SafeHandle borrow ends; the delegate's managed lifetime alone roots no library
handle here. An owner must keep the library loaded until all function calls drain.
Generic declaration `where TDelegate : class` also leaves actual delegate validity
to Marshal.GetDelegateForFunctionPointer; no public consumer contract is inferred.

Libraries.cs still describes a removed Libraries.Net47.cs, multi-framework cleanup
and a Cleanup method absent from this tree. SdkPlatformInfo.DwordSize says Linux=8
on every Linux process; its public value is not a universal native DWORD type rule.
Its Encoding/CharSize are selected helpers, not declarations of every native API's
encoding; PC/SC shim and Windows direct imports already differ.

**Reopen:** Libraries, NativeMethods, all platform dynamic loaders/SafeLibraryHandle,
SdkPlatformInfo, root/Core interop/AOT guidance and loader support tests. Other
platform loader units retain B2/B3 ownership; this is a narrow dependency handoff.

## CORE-B4-C006 — Shared PC/SC shim strings and native PCI layout

**Documentation:** identify the wrapper ABI and source/producer gap precisely.
**Runtime:** reader-name corruption and platform PCI carrier discrepancy need
separate implementation tests. **Policy:** select one explicit string boundary.
**Qualification:** exact deployed native producer and native successful-transmit
cells remain outstanding.

SCard.Interop:90/111 encodes groups and decodes readers with ASCII; Connect marshals
UTF-8. SCARD_READER_STATE allocates names with StringToHGlobalAnsi, while
GetReaderName decodes UTF-8. ASCII replacement loses every non-ASCII byte sequence
before grouping or Connect. On Windows the ANSI allocation is code-page-dependent;
Unix's ANSI convention is UTF-8. Published 1.18.1 pcsc.c forwards char* reader names
straight to the platform SCard macro and performs no UTF-8/UTF-16 conversion.
The inspected CMake/header/Windows script defines no UNICODE conversion route;
installed producer flags and its selected Windows A/W export remain unbound.
This is the bounded follow-up gap, rather than proof of every installed shim's
code page. B1's #18 allocation/free source resolution remains intact.

Managed SCARD_IO_REQUEST contains two four-byte values and reports its own size.
Published Native_SCardTransmit accepts `const SCARD_IO_REQUEST*` directly and
forwards it to native SCardTransmit, whereas the shim explicitly converts other
uint32_t counts and reader-state fields. The retained pcsc-lite header documents
two `unsigned long` PCI fields: on Linux LP64 that structure is 16 bytes, unlike
the managed eight-byte carrier. This is a conditional source-level ABI discrepancy
for that producer/header target, not a Windows error or a demonstrated failure
with this host's installed binary. Windows uses its own DWORD layout; do not
generalize Linux width to Win32 or macOS.

Reader-state pack(1), two pointer-width fields, three uint32_t values and 36 ATR
bytes match the published wrapper's NATIVE_SCARD_READERSTATE, rather than the
platform SCARD_READERSTATE. GetAtr copies AtrLength bytes from fixed[36] without
a production upper-bound check; malformed native length needs a separate safe
negative vector before broadening robust-buffer claims. Published status-shim
copies platform ATR storage width, rather than arbitrarily trusting a C# buffer.

**Oracles:** RuntimeConsumer covers establish/cancel-invalid/connect-nonexistent/
release and crypto on macOS arm64, explicitly excludes transmit and successful
connect, and uses no non-ASCII reader. The export checker checks names plus BN
allocate/free, rather than PCI/string signatures. B1's fake transmit/reader-name
tests establish managed ownership but supply no native layout proof.

**Reopen:** SCard.Interop, SCARD_IO_REQUEST, SCARD_READER_STATE, state extensions,
SdkPlatformInfo, B1 PC/SC lifetime/reader-name docs, native support/AOT/packaging
claims and native compatibility host instructions. B2 receives the LP64 PCI
dependency without a second primary disposition of its Linux files. C owns crypto
semantics; a package containing OpenSSL is not provider/algorithm acceptance.

## CORE-B4-C007 — Windows report copies retain payloads beyond scoped forwarding

**Documentation:** describe caller/result ownership without claiming all owned
native staging is cleared. **Runtime:** conditional sensitive-buffer cleanup.
**Policy:** classify actual protocol payloads with C/B5; metadata is ordinary data.
**Qualification:** source only, no secret/device probe.

WindowsHidReportAccess.SetFeatureReport and SetOutputReport allocate report-ID-prefixed
sendBuf arrays and leave them for GC after success or failure. GetFeatureReport and
GetInputReport copy payload from another allocated native buffer and leave that
staging array for GC. The typed adapters clear their own first send copy in
finally, but that does not erase the nested Windows sendBuf. The returned array
properly escapes to the caller; deterministic cleanup of that independent result
is the caller's responsibility. The issue applies when those arrays contain PIN,
token, secret-derived or plaintext data; ordinary status/public metadata alone
does not justify a blanket secret classification.

ReadFile/WriteFile use `!success || count != expected`, then translate the captured
Win32 error. A short *successful* call supplies no failure code. With a zero/stale
last error, ThrowExceptionForHR may fail to throw or report an unrelated error.
GetInputReport can then expose zero-filled suffix; SetOutputReport can appear
successful. Keep this explicit short-count handling question separate from actual
Windows HID driver guarantees and B5's framing rules.

**Oracles/reopen:** current open-failure tests supply no staging/partial-count
assertion. Reopen WindowsHidReportAccess, typed adapter cleanup commentary, public
HID result ownership XML, memory/security guidance and actual B5/app boundary
payload traces after selected changes. Meaningful fake-native tests should capture
the real nested buffer, inject failure/short success and observe cleanup after
actual native return; clearing a second fake copy proves nothing about production.

## CORE-B4-C008 — Verification and benchmark scope remains evidence-specific

**Documentation:** correct source-oracle descriptions and qualify historical
platform assertions. **Runtime/test policy:** improved harness/oracles require a
separate assignment. **Qualification:** fresh execution is unrun.

The four benchmark files separate invocation-return and task-terminal timing,
label killed children censored, and explicitly leave native duration/report count
unobserved. Preserve these useful limits. BoundarySelfTest checks seven synthetic
classifications plus one dataset/provenance record; it exercises no physical key.
The ordinary BenchmarkDotNet fixture selects the first matching transport and
performs no firmware=5.8 or serial authorization assertion. Its error message's
5.8 device request is not an enforced selection rule. Real baseline discovery can
probe attached bystanders before serial selection. The lifecycle child checks
CTAP success status on GetInfo/reopen, rather than parsing complete GetInfo or
observing native cancellation/close status. Dataset hash comparison binds a
cached package asset to a file at a constructed deployed path; it does not observe
the process-loaded library or attest its native producer.

NativeAotVerification roots ten entry types and performs discovery; it supplies
reachable-link evidence, rather than runtime coverage of all public methods. Its
monitor uses three WatchAsync sinks, not every former event/observable surface.
Rapid activity asserts at least one Added and Removed, rather than three cycles.
Cross-sink snapshots are compared while the pumps still run, before final shutdown;
they are a sample, not a barrier-established equal prefix. No run occurred here.
The project comment's claim that its filename lacks Yubico.YubiKit.* is stale;
the external directory/nonpackable status are the useful exclusion conditions.

NativeCompatibilityVerification's hardcoded binaries are historical macOS arm64
1.18.0 / async.8, not stable 1.18.1 Windows. Its 36 exports and five input symbols
are explicitly names, and only BN allocation/free executes in that checker.
RuntimeConsumer uses hash-checked managed/native binaries, actual internal crypto
delegates, four SCard imports and a release-failure control. Its negative control
checks failure before valid-handle suppression; it observes no later ReleaseHandle.
The README already correctly distinguishes that limit, historical source/binary
identity, missing successful connect/transmit and platform qualification. Retain it.

**Reopen:** these 13 external primary files, docs/NATIVE-AOT's assigned nine
sections, research readiness claims, CI and toolchain support ranges (continuation),
root/Core package claims, migration descriptions and A-C020/B3-C008 historical
qualification maps. A-C020's unknown old Windows artifact remains nonadditive;
compiling Windows/macOS route code on Linux gives no Windows runtime acceptance.

## CORE-B4-C009 — Shared error carrier preserves Windows constants across backends

**Documentation:** describe raw-code/backend provenance. **Runtime:** correct
literal error value and select backend-aware symbolic mapping separately.
**Policy:** avoid changing public exception shape silently. **Qualification:** source.

SCardError.ERROR_BROKEN_PIPE is 0x00000109; Microsoft's pinned winerror.h says
109 decimal (0x6D). Its comment is a PC/SC remote-session explanation, while the
generic Win32 message is "The pipe has been ended." The existing literal cannot
match Win32's ERROR_BROKEN_PIPE. Preserve raw numbers and actor context before
choosing a repaired display.

The shared SCard error class assigns Windows SCARD_E_UNSUPPORTED_FEATURE=0x80100022
and SCARD_E_UNEXPECTED=0x8010001F. pcsc-lite's original API explicitly uses
0x8010001F for UNSUPPORTED_FEATURE and says UNEXPECTED is never returned there.
The published shim returns the native status unchanged, so GetErrorString labels
that pcsc-lite result UNEXPECTED. Its SCARD_PROTOCOL.Raw=0x00010000 also differs
from pcsc-lite's raw=0x0004. Current built-in Tx selects T0|T1; no raw-protocol
failure is inferred without a caller. The exception constructor accepts long but
sets a 32-bit HResult; symbolic lookup expects the stored positive uint values,
so sign-extended external inputs need a documented raw-carrier policy as well.

B3's 37 IOReturn mismatches and CFNumber LP64 issue remain Mac findings, not
Windows diagnoses. PlatformApiException currently formats an int as X8 and keeps
the supplied native source label only inside Message; a negative int can preserve
the raw 32-bit bit pattern without endorsing an incorrect symbolic enum. No
automatic cross-platform enum normalization follows from a generic exception type.

**Reopen:** SCardError, SCardException, SCARD_PROTOCOL, PlatformApiException and
public native-error XML; native error tests, compatibility README's raw observations,
platform troubleshooting and migration. Preserve historical numeric logs and B3
IDs unchanged; any normalized mapping needs platform-qualified vectors.

## CORE-B4-C010 — Direct HID/support reference needs practical ownership contracts

**Documentation:** add focused reference for direct factories, enumeration and
public support helpers. **Runtime/policy:** no API removal or new DI route.
**Qualification:** consumer compilation stays VALID-NOT-CHECKED.

IHidInterface's two factory summaries omit synchronous waiting, caller disposal
and the absence of a grouped-key registration. IHidConnection already accurately
states borrowed SetReport input and caller-owned built-in results with platform
length/blocking differences; preserve that detail. B2-C006 owns its Linux-specific
factory extension; this candidate supplies the common interface and Windows case.

FindHidInterfaces runs its enumerator through Task.Run; a pre-canceled token
submits none, while mid-enumeration cancellation leaves its task pending through
native completion. FindHidInterfacesBoundaryTests observes these distinctions,
call count, identical thrown exception and finally drain of actual workers. XML
for its public finder/factory and native support APIs is sparse. ServiceLocator
is public and test-only by comment, holds a process-global provider without disposal,
and forwards GetRequiredService; it supplies no automatic Core DI/discovery route.
BadResponseException's only explanatory prose is inside its empty body. Describe
these actual scopes without inventing blanket error wrapping: Windows report calls
can throw HRESULT exceptions, while CM APIs use PlatformApiException.

**Reopen:** IHidInterface, finder APIs, public support types, exception XML and
Core/root install/direct-usage guidance. PublicAPI snapshots now have explicit
support-surface reconciliation and related-context dispositions; no all-member
implementation acceptance follows. Preserve source shape and executable strings.

## Continuation facets and final reopening decisions

The paragraphs above preserve the checkpoint findings. The additions below are
part of the same ten local groups, not ten new findings. `candidate-index.json`
is the exact assigned-ID/unchanged-path reopening map. Paths mentioned as narrow
dependencies below retain their original primary owner. Every affected unchanged
path needs an explicit retain/correct decision during editing and independent
review; none is silently omitted because no product edit occurred here.

### C005: current-token ownership and dormant provider scope

`SdkPlatformInfo.IsElevated` constructs `WindowsIdentity.GetCurrent()` inline
without disposing it. Retained .NET runtime **v10.0.0 WindowsIdentity.cs**
`GetCurrentInternal:809–826` obtains an owned `SafeAccessTokenHandle` and returns
the owning identity; `Dispose:728–744` disposes that handle. SafeHandle finalization
is fallback cleanup, not deterministic disposal. This is a resource-scope facet,
not an observed unbounded leak or a reason to require elevation. Reopen that
getter, elevation guidance and ownership examples separately from loader binding.

The provider support follow-up read `CryptographyProviders`, CMAC native wrappers
and ARKG native SafeHandles. The current path uses BCL factories plus NativeShims
OpenSSL imports, not a Core BCRYPT import family. C's C011–C013/C019 own the actual
provider/secret cleanup and ARKG API findings. Their original OpenSSL 3.0 source
register is reused rather than claiming native execution or reassigning findings.

### C006: resolved conditional PCI applicability

The live built-in path is `PcscNativeResources.Transmit:87–98` →
`NativeSCardConnectionApi.Transmit:73–85` → `NativeMethods.SCardTransmit` →
published `Native_SCardTransmit`. The first step constructs the managed eight-byte
PCI; the provider forwards it, and published C forwards its pointer without field
conversion. Fake `ISCardConnectionApi` implementations do not establish native ABI.
The owner confinement/drain contract does not convert this carrier.

Published 1.18.1 Linux amd64 and arm64 scripts install target `libpcsclite-dev`;
`pcscd.cmake:74–93,126–127` obtains flags/headers through pkg-config. The captured
pcsc-lite header and this host's **2.5.3 headers, read only as text**, declare two
`unsigned long` members. For those Linux x64/arm64 LP64 header targets, the native
PCI has two eight-byte fields while the managed carrier has two four-byte fields.
This establishes the conditional source-build discrepancy on an actual selected
route, not merely an unused struct-size hypothesis. It does not establish which
headers/patches produced an installed package or its observed transmit behavior.

Windows' official `winsmcrd.h:270–274` uses two 32-bit DWORDs: this size concern
does not apply there. The macOS backend selects framework PCSC headers/linkage,
not the Linux pkg-config target; no Apple producer/header identity was recovered
for this comparison. The Linux header must not be used to pronounce an Apple
ABI mismatch. Exact installed producer/target/header correspondence remains an
external native-package-owner task. No successful transmit, device or ABI harness
was executed here.

### C007: complete invocation logging is a separate secret carrier

`RunLog.Start` stores `string.Join(' ', args)` as `s_command`; the run log and
serialized result retain it. `toolchain.cs` calls Start before target execution.
`Options` exposes `--nuget-api-key`, and `PackageTargets` uses it for push. The
process helper's `Exec.Mask` masks a different command-display path, not this
recorded invocation. A caller supplying a real key on that option can persist it
in the toolchain artifacts. No real key was supplied to demonstrate this.
Reopen `toolchain.cs`, RunLog/Options/PackageTargets/Process, TOOLCHAIN and logging
guidance. Documentation redaction policy, implementation masking, artifact
retention and historical artifact inspection are separate facets. C-C008 remains
the nonadditive logging/opaque-exception ownership alias.

### C008: actual support oracles and historical populations

- `Gates.EnsureTestsRan` rejects **all** projects reporting no tests, not every
  individual no-test result. `TestTargets.Classify` treats exit-zero with missing
  parsed counts as Passed. TOOLCHAIN's “every project” guarantee is stronger than
  that oracle. Reopen Results/TestTargets/RunLog and the associated documentation;
  do not change runner assertions or accepted filters in Pass 1.
- `CrossLayerAcceptanceTests` checks continuation state only when that task has
  already been assigned. A failed early wait/assertion can precede assignment;
  later continuation work then needs a separately reviewed release/drain path.
  Preserve the meaningful protocol/frame/cancel/reuse assertions. Do not equate
  the five-second watchdog with native quiescence.
- `ChildProcessTestRunner` requests Kill on cancellation but does not await child
  exit and redirected-output drain on that path. This bounds a host wait, not
  cooperative SDK cleanup or proven borrowed-memory release.
- `OperatorCue.Announce` performs console/echo outside the speech catch and ignores
  the bool result of the 15-second WaitForExit. “Never affects outcome” and
  “returns once speech finished” need those failure/timeout conditions. No speech
  process was launched here.
- `docs/v2-highlights.md` says the AOT publish directory includes a shared shim.
  Published 1.18.1 `Yubico.NativeShims.Aot.targets` selects one RID static archive,
  DirectPInvoke and system libraries, and removes the shared asset from publish.
  Reopen highlights, current AOT/package guidance, shared target commentary and
  historical readiness assertions; preserve historical output as historical.
- Boundary scanner/manifest, adapter registry and public-sync registry distinguish
  source sites, symbol reachability and linked managed profiles. The current
  baseline is **218 sites / 43 operations / 76 links / 23 public-sync entries**.
  Earlier 206/219/24 counts in dated product/architecture text are superseded
  checkpoints, not current gate totals. The adapter registry excludes baseline
  Windows HID verification. Neither resolving a method name nor reading its test
  is execution or proof of pre-task-return/native completion properties.
- Resilience scanning is a scoped lexical source oracle, with explicit synthetic
  false-positive/negative fixtures; it supplies no runtime cadence measurement.
  PublicReturnContract scanning recursively follows member/type shapes with
  inherited-interface handling, cycle guards, exclusions and independent fixtures;
  it does not prove disposal, native lifetime or every reflection surface.

The original Microsoft SourceLink advisory **CVE-2026-62900**, V1.0 dated
2026-08-11, supports Directory.Build.props' explanation for removing an explicit
8.0.0 task reference. It also lists affected10.0 SDK/task lines and fixed
10.0.111/10.0.303 versions. `global.json` requests10.0.300 with latestFeature;
this policy does not prove a fixed SDK was resolved on a particular host.
Reopen the comment and SDK qualification together; no current vulnerability or
fixed-host result is inferred without that identity. No SDK command ran here.

Keep the inherited Linux **38 JIT + 38 AOT native-only** checks separate from
**43 historical physical checks** on serial **103**. This audit contributes zero
to either population. Historical readiness's Windows output lacks exact current
source/native artifact binding and per-function execution traces. A-C020/B3-C008
and the frozen integration qualifications remain authoritative, nonadditive aliases.

### C009/C004: backend-specific status and contention explanations

`SCARD_SHARE.DIRECT`'s exclusive-reader explanation is Windows-specific;
pcsc-lite explicitly documents shared direct mode. `SCARD_SCOPE.USER` describes
Windows-style database scope; pcsc-lite documents USER/TERMINAL as unused and
SYSTEM as local service. `SCARD_STATUS` values 0–6 match official Windows headers,
not pcsc-lite's status flags. There is no baseline SCardStatus import using that
enum, so the status catalog is not a demonstrated active-call failure.
State-change flags remain distinct; UNAWARE returns immediately, and ATRMATCH's
SCardLocateCards wording is a Windows API condition, not a current Core call.

Highlights says another process produces ordinary `SCardException`; strict PC/SC
sharing maps to `ConnectionInUseException`, while Windows report failures can be
HRESULT-derived exceptions. Keep raw native errors, process registry refusal,
resource-manager sharing, host ACL/token checks and physical application state
separate. Reopen those enum comments, highlights/migration/troubleshooting and
error-carrier XML. No claim about physical PIN state follows.

### C010: keyboard, API and architecture support

All ten keyboard files were reviewed using full forward tables and independently
decoded reverse entries, including duplicate indexer assignments and aliases.
Seven locale maps contain aliases around usages `0x31`/`0x32`/`0x64`; later
character-indexer assignment chooses the forward result, so byte→char→byte is not
necessarily identity. `sv_SE` is US-shaped and lacks Swedish letters; `es_US` is
also US-shaped. This is table/applicability evidence, not a tested host-layout
failure. HUT identifies usages, not a universal OS character layout.

The character indexer's summary says HID code corresponds to HID code;
`SupportedCharacters` yields dictionary keys, not an array. The internal shift
marker means shifted keystroke, not universally uppercase. ModHex contains sixteen
lowercase plus sixteen uppercase non-whitespace characters, and CLI
`GenerateStaticPassword` selects all 32, while its XML invokes the familiar
16-letter alphabet. “Identical on every host” needs layout/case conditions. Reopen
translator/tables/KeyboardLayout, assigned OTP CLI helper/XML, public snapshot
and practical direct-HID/OTP guidance; keep B5's wire protocol separate.

Official pinned devpkey/hidclass headers match **27** managed property GUID/PID
values. Active HID/keyboard/USB interface values match official hidclass/ntddkbd/
usbiodef headers. Class catalog values match where present except `SmartCard`
`990a2bd7-e738-46c7-b26f-1cf8fb9f1391`, not found after the focused official-header
follow-up. That one entry is **READ-SOURCE-QUALIFIED**, not clean or called wrong;
Core Windows maintainers own its provenance. Related constant matches establish
catalog identity, not that any CM/HID API ran. PublicAPI.Shipped is the nullable
header only. Unshipped's support entries/type inventory reconcile with B4 source;
unrelated A/B1/B5/C member signatures remain related context, not reaccepted API
semantics or a claim that every implementation/signature was independently read.

The five assigned PNGs were visually inspected; all SVG meaningful labels and
source-block/image-manifest digests reconcile. Fresh image bytes do not make
teaching prose accurate. The appendix calls `ApduResponse` a record struct though
current source is a sealed class; L1's “every app module references Core” needs
the WebAuthn→Ctap2 exception already represented in the graph/map. Keep conceptual
pipeline boxes distinct from actual source types and protocol-owned wire details.

Core/test CLAUDE examples still use `state.YubiKey`, while current state exposes
`Device`; Core's platform snippet invents scanner types and a string SafeLibraryHandle
constructor absent from current source. Test guidance lists obsolete Core/PIV
directory shapes, and integration-only examples omit `--integration --project`.
Root CLAUDE's APDU decorator names and `PcscProtocol(logger, connection)` example
also predate the current flat transmitter/receiver construction; B5 owns the
protocol-specific reconciliation rather than this support audit accepting it.
Reopen exact examples and helper declarations; C-C017/C018 own related stale
examples/Span guidance, A/B1/B5 own session/discovery semantic claims. No snippets
were compiled here. Keep normal ownership examples and borrowed-input wording
where accurate instead of rewriting their contracts broadly.

`oath-pilot-and-fixes-wave-002:DOC-002` was reproduced by read-only PyYAML parsing:
ScannerError, line **274**, column **488**, unquoted `: ` in PIV scalar text.
The narrow parent row was inspected; preserve the inherited ID and cross-module
mapping dependency. The assigned later map entries received dispositions despite
that earlier syntax error. Neither docs-qa nor an OATH/PIV runtime check ran.
The inherited host-selector collision remains the recursive substring selector
with no exact-single-project requirement; coordinator execution must pin full
project paths and inspect selected projects rather than infer uniqueness.
