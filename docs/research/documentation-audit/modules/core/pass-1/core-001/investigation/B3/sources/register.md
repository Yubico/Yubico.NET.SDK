# B3 original-source register

`captures.json` binds original URLs, UTC access, HTTP result, bytes and SHA-256. Original source was
retrieved on November 11, 2026 UTC. The session's date advanced to November 12 while recording the
return. No native binary was loaded. Research used an initial targeted lookup and one focused
follow-up for exact release/header identities. Further runtime/build provenance is a recorded gap.

## Sources and authority

| Source / exact revision | Passage actually read | Actor, strength and applicability |
| --- | --- | --- |
| Apple IOKitUser `323ead896d04424f87184d8f6ff0cce811aab106`, `IOHIDDevice.h` | Open/Close 87–118; dispatch queue/cancel/activate 233–326; input registration 353–385; synchronous and callback report APIs 638–750 | Apple API contract for a client using these APIs. Queue and run-loop are alternatives; registration precedes activation; cancel requests stop new handlers and leaves active handlers running. Release follows cancel handler. Dispatch APIs are available from macOS 10.15. This is published-header availability, not this SDK's supported deployment floor or a measured host. |
| Same IOKitUser `IOHIDBase.h` | IOHIDCallback and IOHIDReportCallback definitions 70–105 | Native ABI: pointer context/sender, IOReturn and 32-bit enum, uint32 report ID, pointer buffer, CFIndex length. Synchronous report ID/length are CFIndex. Current managed callback static Cdecl signatures and synchronous 64-bit lengths match the reviewed shapes on Apple 64-bit platforms. |
| Same IOKitUser `IOHIDManager.h/c` | Header Open/Close/CopyDevices/callback/run-loop contracts; implementation Open 921–956, Close 961–991, CopyDevices 1136–1152, registration 1157 onward | API and version-specific implementation evidence. Open sets manager isOpen before applying per-device opens. Close unschedules and clears isOpen before per-device close statuses return. A nonzero per-device result therefore differs from an exception/unknown completion. CopyDevices can return NULL. None of this proves every deployed OS build has identical internals. |
| Same IOKitUser `IOHIDDevice.c` | Open/Close 502–534; dispatch queue/cancel implementation vicinity 839–961; report wrappers | Direct synchronous wrapper calls the device interface under a device lock. Source explains the SDK worker classification; it supplies no maximum duration or native cancellation for report output/feature calls. |
| Apple IOHIDFamily `777ccd9698845aadf711e32d843c8c9b777431d9`, `IOHIDLib/IOHIDDeviceClass.m` | initConnect/open/close 496–645; deallocation connection-release context | Version-specific OS implementation. Open retains `_opened` on Success and ExclusiveAccess. TCC requirement/denial returns NotPermitted; device metadata controls that requirement. Close clears `_opened` after its scalar call. These are source facts, not permissionless operation on a fresh machine or proof every arbitrary close error releases safely. |
| Apple XNU `xnu-12377.1.9`, `iokit/IOKit/IOReturn.h` | Constants and reserved gaps 140–198 | Original numeric ABI definitions. NotOpen=0xE00002CD; NotPermitted=0xE00002E2; gaps at 0x2CB/0x2DC; Invalid=common error1. Static comparison supports C001. No translated Windows/elevation policy is inferred. |
| Apple CF `CF-1153.18`, CFBase/CFNumber/CFSet/CFString/CFRunLoop headers | CFBase Boolean/CFIndex/type-ID/retain/release definitions; CFNumberType and conversion111–123; CFSet count/values contracts; CFString190–195; CFRunLoop50–89 | Historical published ABI/header contracts, conditional on Apple LP64. Boolean is unsigned char; CFIndex signed long; CFTypeID unsigned long. CFNumberType is CFIndex-sized, whereas requested SInt32 value storage is four bytes. CFStringCreateWithCString copies call-time input; CFSetGetValues requires count-sized pointer storage. No claim these are the exact headers on the historical runtime machine. |
| Same IOKitUser `IOKitLib.h` | IOObjectRelease, IORegistryEntryGetRegistryEntryID, IORegistryEntryIDMatching and IOServiceGetMatchingService declarations/ownership comments | Mach object handles are 32-bit, entry IDs are 64-bit, and the matching dictionary is a pointer. GetMatchingService consumes a dictionary reference, service result must be released. Managed CreateDevice finally releases service; IOHIDDevice creation reference is separate. |
| Yubico.NET.SDK release tag `1.18.1`, annotated tag `3eb2d3c35f9f9b1744ad942a25d6bb37a66eed1d`, commit `dedba95b94946907112a7953cd82df8a15c22655` | Entire five native files retained as `native-*`: owner.h/c, internal.h, backend_iohid.c, native_exports.c | Published vendor implementation and ABI contract, not external standard. Captured raw bytes match release-tree Git blobs. Immutable URL pattern: `https://raw.githubusercontent.com/Yubico/Yubico.NET.SDK/dedba95b94946907112a7953cd82df8a15c22655/Yubico.NativeShims/hidinput/<file>`. Tagger date is source metadata; build correspondence to a particular installed dylib is not established by that date. |
| Microsoft .NET interop and Memory usage guidance, captured current main bytes | Callback/function-pointer guidance, borrowed/pinned-memory and asynchronous native completion rules; Context7 `/dotnet/docs` also consulted | Framework guidance, not an OS guarantee. Persistent native buffers/context need lifetime beyond registration call; `UnmanagedCallersOnly` avoids delegate thunk ownership but leaves context rooting necessary. Async invocation shape does not establish cancellation/drain. Do not copy legacy DllImport syntax from an illustrative docs example into this SDK. |
| Context7 .NET nullable reference | `Nullable<T>.Value` throws InvalidOperationException when HasValue is false | Framework API contract supporting C003. Context7 supplied current documentation orientation; no SDK runtime test executed. |
| B1 retained `.NET v10.0.0` Thread source | Start/StartCore ExecutionContext capture | Reused original at `../../B1/sources/Thread-v10.0.0.cs`; B3 reads the same capture mechanism for C006. No duplicate download or general secret-retention inference. |

## Native lifetime proof read from the published implementation

The caller supplies an already-open, unscheduled device, retaining its creation reference. Native
Create validates lengths/capacity, allocates a ring, length array, serial delivery queue, backend
report buffer and serial IOKit queue, and retains its own device reference. Allocation failures unwind
the allocations already made. Start marks started under the mutex and installs queue, report/removal
callbacks and cancel handler before activation. It has no fallible operation after registration begins.

Input validation classifies native status/type/report ID, then copies accepted native report bytes
into a ring slot before returning to IOKit. `count` includes an in-delivery slot, preventing overwrite.
The delivery queue invokes the managed receiver outside the mutex and retires a slot only after it
returns. Overflow/malformed/removal sends one terminal after accepted reports and requests cancellation.
An explicit clean cancel promises no terminal callback. The IOKit cancel handler schedules acknowledgment
as a later item on its serial queue so handler return precedes acknowledgment.

WaitShutdown's complete predicate requires started, acknowledged, no scheduled/delivering/count, and
terminal delivered where required. `UINT32_MAX` means indefinite wait. Fault here still means quiescent;
it is separate from close failure. Destroy requires that predicate (or never-started), attempts checked
close on the caller's worker and frees only after its release rule succeeds. NoDevice needs a removed
terminal plus drain; BadArgument additionally needs a successful service-termination callback. Unknown
close sets a permanent close_failed flag, with no retry. Callback self-wait/destroy is rejected. External
callers serialize Destroy against other external owner calls. Managed code does so on its original worker.

The managed bridge copies borrowed native data, calls the owner synchronously, then clears its temporary
copy. The managed owner takes another bounded copy for read result/queue. Destroy OK permits freeing
the callback-state GCHandle; its own owner root persists until original registry token disposition.
Freed native ring/report storage has no explicit zeroing in these source files. This is an ownership
proof for the identified source, not a claim of end-to-end secret erasure or current Mac execution.

## Bounded gaps and comparators

- The installed package nuspec contains no source commit. Published release source is available; exact
  binary-to-producer-source reproducibility remains unproved here. Package contents were hashed/read,
  never loaded. Retained `.8` native commit `71a23cd0269c968c1d9420eddb2e2fec71e0cc29` is a historical
  implementation identity; its source-tree index was captured only to locate that lineage. It is not
  silently substituted for current stable 1.18.1 implementation.
- The old report-callback timeout discrepancy concerns an unselected alternative API. Current paths
  use synchronous report calls plus a worker. No async report deadline is inferred from old proposed API names.
- C's shared Python pin remains `4ca60f706af930459138d8dc0f0f953480e1c7a4`. Its context and source role
  were consumed without import or duplicate fetch. That YubiKey Manager pin does not independently
  authenticate the IIOKitDeviceLifetime comment's unnamed Rust or python-fido2 revision; retain that
  comparison as an unverified citation, or replace it with direct local policy and Apple contract.
- Mac permission, removal-close, device timing and all host behavior remain revision/profile-specific.
  Fresh Mac work requires coordinator/machine/operator assignment. Linux unit/native executions and
  AOT compilation confer no new Mac qualification.
