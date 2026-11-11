# B3 local candidates and precise reopening map

These are investigation-local groups, ready for coordinator triage. They allocate no canonical bug IDs.
Documentation, implementation decisions, source evidence and platform qualification remain separate.
All associated product paths are unchanged. A fresh editor and independent reviewer must explicitly
retain, qualify or edit each associated path; a dependency read confers no primary ownership.
Paths abbreviated below start at `src/Core/src/Transports/Hid/MacOS/` unless otherwise specified.

## CORE-B3-C001 — macOS status names and numeric values

**Documentation:** supported correction. **Runtime:** enum constants need separately scoped correction.
**Decision:** retain numeric native diagnostics and permission-specific advice. **Qualification:** no native run.

`docs/architecture/connection-ownership-and-contention.md:248` calls `0xE00002E2`
`kIOReturnNotOpen`. Apple's `xnu-12377.1.9` `IOReturn.h:160,183` assigns NotOpen to
`0xE00002CD`, and NotPermitted to `0xE00002E2`. Keep the old observed numeric error and history;
correct its interpretation. `IOHIDDeviceClass.m:496–552` at `777ccd9698845aadf711e32d843c8c9b777431d9`
returns NotPermitted when TCC authorization is required and denied. That is one supported cause,
rather than proof of the cause on the historical host. Replug advice alone is insufficient for a
permission refusal: inspect the exact host/application Input Monitoring authorization and preserve
the complete diagnostic. Fresh-host permission behavior remains user-deferred.

`src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs:90–127` increments across
reserved 0x2CB and 0x2DC, and gives Invalid the next sequential value rather than 0xE0000001.
Consequently CannotLock onward has wrong values (one then two positions low). Current packet code
uses only `KERN_SUCCESS`; FIDO/OTP exclusive-open handling explicitly uses correct 0xE00002C5.
This establishes a latent definition defect and misleading diagnostics, rather than a demonstrated
wrong live branch. `source-checks.json` records a static comparison, with no C# type initialization.

**Oracles:** existing fake tests inject raw status integers and check hex text/cleanup; none compare
the whole enum to Apple's constants. **Unchanged review set:** enum, troubleshooting unit, `IIOKitDeviceLifetime.cs`,
FIDO/OTP owners, feature/IO facade and route tests, Core platform troubleshooting/setup guidance.
**Reopen:** numeric definitions, error translation, permission findings or recovery advice changes.
The SHARED editor owns the guide; a later implementation owner fixes numeric definitions and adds
an independent constant vector without invoking native libraries.

## CORE-B3-C002 — enumeration dereferences a possibly null copied set

**Documentation:** qualification candidate. **Runtime:** source/API-supported unsafe native argument path.
**Decision:** select empty-versus-error handling separately. **Qualification:** no empty-host native reproduction.

`MacOSHidInterface.GetList:65–72` passes manager and `IOHIDManagerCopyDevices` results onward without
null checks. Apple's pinned `IOHIDManager.c:1136–1152` initializes the copied set to NULL and returns
it when `manager->devices` is absent. `CFSet.h:309–317,390–404` requires a valid CFSet; invalid arguments
have undefined behavior. Thus an empty/unavailable device set can reach `CFSetGetCount(0)`, outside
its contract. The listener's `GetInitialEntryIds:408–427` already handles a null set separately.
Do not describe a guaranteed managed exception or guaranteed crash; the OS behavior remains unexecuted.

**Oracles:** packet lifetime tests inject owners, not enumeration. `FindHidInterfacesBoundaryTests`
(B4) gates a supplied enumeration delegate and supplies no null-CFSet oracle. **Unchanged review set:**
`MacOSHidInterface.cs`, CFSet/manager declarations, `FindHidInterfaces.cs` (B4), discovery setup and
no-device troubleshooting (A/SHARED), scan boundary tests. **Reopen:** acquisition null guards,
empty-result policy or enumeration failure handling changes. A future narrow fakeable scan seam or
supervised empty-Mac host can verify this; a Linux build is irrelevant to the native condition.

## CORE-B3-C003 — integer-property XML names the wrong exception

**Documentation:** supported correction. **Runtime:** preserve current behavior pending any separate policy.
**Decision:** document absent versus lossy-conversion outcomes. **Qualification:** source only.

`IOKitHelpers.GetIntPropertyValue:35–46` declares NullReferenceException but deliberately accesses
`Nullable<int>.Value`. Absence produces InvalidOperationException. Its nullable sibling returns null
for a missing property, throws PlatformApiException on a different CF type and returns zero if
`CFNumberGetValue` reports a lossy/out-of-range conversion. Apple's `CFNumber.h:111–123` says a best
conversion is written even when false is returned; the wrapper discards it. XML should expose these
wrapper outcomes and avoid implying every query requires an already-open device: enumeration queries
properties before device open. No new conversion policy or validation is authorized.

**Oracles:** CoreFoundationString tests establish UTF-8 bytes/encoding/null creation, not numeric-property
exception behavior. **Unchanged review set:** helper, `CoreFoundationString.cs`, CFNumber imports,
interface enumeration and lifetime GetIntProperty callers. **Reopen:** exception, absent-property,
numeric-conversion or pre-open-query policy changes. A static Nullable contract citation and a later
pure injected property vector can settle the public commentary without device work.

## CORE-B3-C004 — callback rationale predates the rooted listener generation

**Documentation:** supported correction, with a dormant-boundary warning. **Runtime:** current active
listener route is rooted; old array-registration surface remains risky if reused. **Decision:** retain
or remove that unused seam in a separately authorized scope. **Qualification:** generated stubs uninspected here.

Core `CLAUDE.md:488–495` describes `_arrivedCallbackDelegate`, `_removedCallbackDelegate`,
`_abandonedCallbackDelegates` and a zero context. None exists in the pinned listener. `StartGeneration:79–83`
passes static Cdecl UnmanagedCallersOnly addresses and `generation.Context`; Generation roots itself
with GCHandle before registration. Rewrite the instruction around the actual context and native drain.
This shared passage is a secondary handoff to B4/SHARED, outside B3's five assigned shared IDs.

`IIOKitDeviceLifetime.RegisterInputReportCallback` and its native import still accept `byte[]` storage
for an asynchronous registration. The current active persistent FIDO route instead allocates the report
buffer in the native backend and roots only managed callback context. An ordinary call-scoped array pin
would be insufficient if the old registration method were revived without an enclosing persistent pin.
Repository usage search found no production call to that lifetime method outside its forwarding definition.
Record this dormant hazard without alleging a current active buffer-use-after-free.

**Oracles:** listener fakes invoke the static unmanaged address through a Cdecl delegate; native route
host wraps real callbacks and observes callback exit/close/unschedule/release order. Fake tests supply
no generated-marshaller or deployed OS proof. **Unchanged review set:** Core CLAUDE, listener, IOKit
callback declarations, lifetime seam, listener tests and `ListenerDrainScenario.cs`, FIDO bridge.
**Reopen:** callback ABI, GC root, retained input pointer, scheduling mode or registration usage changes.

## CORE-B3-C005 — partial listener startup has permanently retained paths

**Documentation:** retain the conservative quarantine rationale, qualify completeness. **Runtime:** partial
allocation cleanup candidates. **Decision:** distinguish intentional post-registration retention from
unnecessary pre-registration loss. **Qualification:** source only; allocation/start faults unexecuted.

`StartGeneration:73–92` obtains a manager before constructing Generation. If construction throws before
assignment, the catch has no local cleanup for that manager. Generation allocates its GCHandle before
constructing Thread (`335–342`), so failure during Thread construction can also leave its new root.
After `RegistrationAttempted=true`, any registration or `Thread.Start` exception intentionally retains
the rooted generation. With an unstarted thread, Stop returns at 107 and restart is refused at 54;
there is no late worker to perform cleanup. That is safety-biased retained uncertainty, not successful
teardown or the ordinary callback-timeout/late-drain recovery tested elsewhere.

`StartGeneration` also uses an unguarded Error logger in its catch. A throwing logger can bypass its
remaining pre-registration Cleanup/Status assignments; `HotplugSignals` may still attempt disposal,
which cannot clean an unstarted generation. Keep diagnostics failure distinct from native failure.

**Oracles:** listener tests cover held callbacks, shared timeout budget, unschedule/release failure,
normal restart and thrown/nonzero manager Open/Close. None injects Generation/Thread construction,
register/start failure or start-catch logger failure. **Unchanged review set:** listener/Generation,
listener tests, Core listener guidance, monitor HotplugSignals (A), real drain host. **Reopen:** startup
ownership, explicit registration proof, no-thread cleanup or logger-failure handling changes. Preserve
rooting after ambiguous registration until a separately proved quiescence path exists.

## CORE-B3-C006 — macOS worker context flow extends B1's decision

**Documentation:** record-only pending policy. **Runtime:** source-visible ExecutionContext capture.
**Decision:** extend `CORE-B1-C010`, nonadditively. **Qualification:** no AsyncLocal experiment.

FIDO `ScheduleLocked:288–291`, OTP `ScheduleLocked:105–108` and listener `Generation:341`
start ordinary Threads without suppressing flow. The pinned .NET 10 Thread source retained by B1
captures ExecutionContext at Start. These rooted long-lived workers can therefore retain caller
ambient state. Linux's separate suppression policy is a comparison point, not automatically a Mac
requirement. This concerns ambient state, not ownership of a passed connection, prompt, or report copy.

**Oracles:** worker identity assertions establish one worker; they contain no ambient-state assertion.
**Unchanged review set:** two owners, listener, their lifetime tests, logging/presence context guidance
(C/B5), B1-C010 and B2 policy comparison. **Reopen:** selected context-flow policy or explicit nonsecret
AsyncLocal probe. Coordinator must decide whether to consolidate all three routes under B1's question.

## CORE-B3-C007 — direct IO reads preserve fixed FIDO framing, not arbitrary descriptor-sized input

**Documentation:** qualify expert-facade input semantics. **Runtime:** current behavior is deliberately
asserted; no change selected. **Decision:** any future generalized raw descriptor input is separate.
**Qualification:** fake-native source oracles and historical selected-key evidence remain distinct.

`MacOSHidIOReportConnection` reports native InputReportSize/OutputReportSize, forwards arbitrary SET
lengths, but Receive shares FIDO `Owner.Report:419–442`: exactly 64 bytes are accepted; 65 bytes with a zero leading
report ID are stripped to 64 bytes; any other frame terminalizes. `MacOSHidIOFacadeTests.ReportsUseNativeDescriptorSizesAndFixedFidoPackets`
asserts metadata sizes 65/72, SET lengths 72/empty and GET length 64 from a 65-byte input. Explain this near direct-report guidance so a
descriptor-size property is not mistaken for the actual returned buffer length. The six-second timeout
detaches only the waiting reader; native owner/input continues. Feature expert GET separately returns
the original eight-byte array and zero-pads successful short GETs, whereas typed OTP rejects short GET.

**Unchanged review set:** both expert facades, both owners, IO/feature facade and compatibility tests,
Core README direct access, raw-tier synchronous table, IHidConnection XML (B4), migration examples.
**Reopen:** normalization, accepted frame shape, direct GET length or typed/expert split changes.
Fresh editor should retain the accurate timeout/SET/caller-owner material and add the missing nearby
input qualification rather than replacing it with a universal raw-HID contract.

## CORE-B3-C008 — native source and runtime evidence need explicit versions

**Documentation:** qualification candidate for future publication. **Runtime:** native protocol source
located and read; no new native defect or runtime acceptance allocated. **Decision:** source publication
and package build provenance remain a release question. **Qualification:** exact current binary build
correspondence and fresh Mac execution remain open.

The published 1.18.1 source at `dedba95b94946907112a7953cd82df8a15c22655` provides `owner.h/c`,
`internal.h`, `backend_iohid.c` and `native_exports.c`. It explicitly distinguishes acknowledgment/drain
from checked destroy. Destroy accepts success, or narrow removed/drained NoDevice and confirmed-service-
termination BadArgument cases, and permanently retains unknown close failures. Managed WaitShutdown
accepts OK/FAULT as quiescence; only successful Destroy frees the GCHandle context. The caller separately
releases its IOHIDDevice creation reference. Native report slots are freed, not explicitly zeroed: managed
copy clearing supports no end-to-end secret-erasure claim.

The cached package nuspec gives version 1.18.1 and repository URL, without a source commit. Source blobs
match the published release tree; that establishes source identity, not a reproducible build mapping to
the cached dylib. Preserve a precise binary/source-provenance qualification until producer evidence binds it.
Historical `.8` macOS 15.7.7 arm64/.NET 10.0.12 serial 31683481 firmware 5.7.4 results remain original-revision
results. The current integration compiled route/comparison hosts on Linux using canonical 1.18.1 under
its explicit three-property profile. It neither ran them on Mac nor changed comparison defaults.

**Oracles:** `MacOSHidInputVerification` exercises a nonshipping synthetic experiment ABI, not the
shipping package; fake bridge CloseFault=6 probes managed policy. Route host checks actual production
type/selected-serial responses and borrowed reuse, and explicitly treats watchdog kill as OS cleanup.
`CurrentProfile` separates process allocations/CPU, native-only unavailability, fixture firmware absence,
loaded-path absence, partial samples and source/build correspondence. Preserve those honest bounds.

**Unchanged review set:** all three verification hosts/projects, Core macOS lifetime guidance, historical
architecture/AOT evidence, package/loader declarations (B4), raw tier/setup/migration summaries. **Reopen:**
native package/export/source version, native close proof, OS floor, runtime profile or actual Mac results
changes. Keep old cold-start before/after mode (`.8` and two exact commits) distinct from current `--profile`.
Future complete source publication should include producer commit, artifact/package hashes, native deployment
floor and a typed borrowed-connection example; source access alone is not device qualification.

## CORE-B3-C009 — CFNumberType's native width is implicit/mismatched

**Documentation:** ABI rationale candidate. **Runtime:** declaration-width discrepancy for separate review.
**Decision:** exact native type carriers should follow headers. **Qualification:** no call corruption alleged.

`CoreFoundation.Interop.cs:67,81` declares `theType` as int for CFNumberCreate/GetValue. Apple's
`CFNumber.h:49` defines CFNumberType as `CF_ENUM(CFIndex, ...)`, and `CFBase.h:380–390` makes CFIndex
a signed long on Apple LP64. The declarations therefore use 32 bits for a native 64-bit type. Current
callers pass only positive 3, so this source-level discrepancy does not demonstrate a failing live call.
Keep the four-byte **value storage** for kCFNumberSInt32Type separate from the enum's parameter width.
The other reviewed synchronous CFIndex/report length carriers are long; Boolean has explicit U1.

**Oracles:** string byte tests and injected integer-property returns bypass these imports. **Unchanged
review set:** CF imports, IOKitHelpers, listener vendor dictionary, original CF headers and later generated
stub/native ABI validation (B4). **Reopen:** native carrier types, architecture targets or generated call
evidence changes. A later reviewer should inspect actual stubs/ABI on supported Mac architectures before
describing a physical effect; avoid an unqualified statement that every imported signature already matches.
