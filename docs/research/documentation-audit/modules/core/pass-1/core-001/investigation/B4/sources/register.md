# B4 original-source register — final pinned investigation

Access date: 2026-11-12 UTC. `captures.json` binds each retained original's URL,
access time, bytes and SHA-256. These records concern the pinned managed source,
rather than a claim that a workstation binary was produced from a published tag.
Only the named symbols/passages were semantically read in large headers; the
retained whole header is context, not a whole-Windows-SDK review.

## Microsoft Windows API contracts

**Title/version/status:** Microsoft Win32 Configuration Manager and HID API
reference, live reference captured on the date above. Header source has an
immutable official counterpart in `microsoft/win32metadata` commit
`15fc870a5e96df7c560877ff30136c0fb5a21e9b`, paths under
`generation/WinSDK/RecompiledIdlHeaders/`. An initial SDK 10.0.16299.0 mirror
was checked, then a focused follow-up obtained the official pinned headers.
The mirror's winnt.h typedef `typedef BYTE BOOLEAN` is corroborating original SDK
text hosted by a third party; no claim of mirror release attestation is made.

**Actor/strength:** API obligations on native callers and native declarations;
these are contractual reference/header evidence, not RFC-style normative device
profile requirements or observations of deployed drivers.

| Source / exact passage | Consequential contract / applicability |
| --- | --- |
| official-cfgmgr32.h `CM_Get_Device_Interface_ListW` / `...List_SizeW` (2784 / 2852) | InterfaceClassGuid is LPGUID; sizes are ULONG/PULONG; output is UTF-16 multisz measured in characters. |
| official-cfgmgr32.h `CM_Get_Device_ID_Size` (2279), `CM_Get_DevNode_PropertyW` (2312), `CM_Get_Device_Interface_PropertyW` (2910) | Output size points to a 32-bit ULONG. The pointer itself has process pointer width. Property keys are const pointers; output types/counts are returned independently. |
| cm-interface-list.html, **Remarks** | Hotplug can grow the list between size/read; callers should retry CR_BUFFER_SMALL. This is robustness guidance, not an elapsed-time or retry-count guarantee. |
| cm-interface-property.html, **PropertyBufferSize**, **ulFlags** | Null probe initializes size to zero; size is in bytes; reserved flags are zero. |
| official-cfgmgr32.h `_CM_NOTIFY_FILTER` (1287), `_CM_NOTIFY_EVENT_DATA` (1339) | Fixed filter includes the largest union, including WCHAR[MAX_DEVICE_ID_LEN]; device-interface event begins with enum/DWORD/GUID before WCHAR symbolic link. Managed 416 bytes and offset 24 concern this selected branch/layout. |
| cm-unregister.html, **Return value**, **Remarks** | Unregister returns CONFIGRET; it waits for pending callbacks. Calling it from a callback can deadlock. Caller keeps context until the asynchronous work actually unregisters. Windows 8+ contract; failure is a separate outcome. |
| official-hidsdi.h `HidD_GetPreparsedData` (82), `HidD_FreePreparsedData` (108), `HidD_GetFeature` (191), `HidD_SetFeature` (218), neighboring seven imports | HidD functions return BOOLEAN, while ordinary Kernel32 file APIs return BOOL. Feature buffer carries a report ID and exact caps-defined native size. |
| hidd-getfeature.html, **Parameters**, **Remarks** | Caller initializes report ID, including zero for an unnumbered collection. Result returns in caller buffer; API reference gives no general elevation promise. Last-updated label retained: 2022-06-29. |
| createfile.html, **dwDesiredAccess**, **dwShareMode**, **dwFlagsAndAttributes** | Zero desired access permits certain metadata queries. Desired access and existing sharing must be compatible. FILE_FLAG_OVERLAPPED is a distinct selection; baseline uses NORMAL. No universal “FIDO requires Administrator” rule follows. |
| readfile.html, **lpOverlapped**, **Remarks** | Synchronous/null-overlapped and asynchronous request storage have different lifetimes; caller retains buffer until operation completes. Baseline's call is synchronous. |
| cancelioex.html, **Return value**, **Remarks** | Success requests cancellation; it neither waits for completion nor authorizes OVERLAPPED/buffer reuse. ERROR_NOT_FOUND is a failure outcome; a targeted request may complete normally, aborted, or with another error. Applies to a future Windows implementation, since baseline imports none of it. |
| getoverlappedresult.html, **bWait**, **Return value** | Observes completion/byte counts for a particular overlapped operation; awaiting a managed submission task is not automatically this observation. Future-delta checklist only. |
| x64-calling-convention.html, **Parameter passing** | A 16-byte aggregate is passed indirectly on Windows x64. This can hide a by-value Guid versus LPGUID declaration mismatch on that architecture; it establishes no x86/arm64 result. |
| loadlibraryex.html, **Syntax**, **dwFlags**, **Remarks** | Wide entry point is LoadLibraryExW; loading target/dependency search is controlled by its input/path/flags. Restricting the imported kernel32 module alone does not establish restricted target dependency resolution. |
| official-winerror.h `ERROR_BROKEN_PIPE` | Decimal 109, generic message “The pipe has been ended.” Managed 0x109 differs. The shared PC/SC remote-session explanation is a narrower actor/use case. |

**Initial checkpoint gaps (resolved or narrowed below):** the checkpoint did not recover an exact Windows CCID/HID
hardware artifact matching the source baseline, producer flags for installed
NativeShims, all original GUID/DEVPKEY definitions, or an exact HIDAPI revision
for the comparative retry paragraph. Those claims stay pending; another search
is not permission to promote a current page to historical binary proof.

## .NET source generation and handle lifetimes

**Title/version/status:** .NET Native interoperability reference, current
orientation through Context7 `/dotnet/docs`; exact runtime source
`dotnet/runtime` tag `v10.0.0`,
`src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshalling/SafeHandleMarshaller.cs`.

**Actor/strength/applicability:** managed interop caller/generated marshaller;
implementation behavior for this version, separate from a real Windows driver.
ManagedToUnmanagedIn.FromManaged (29–33) calls DangerousAddRef; ToUnmanaged
returns the handle value; Free (44–50) calls DangerousRelease. Native borrow ends
at actual call completion, not at another thread's Dispose return. Native output
array pins are scoped to a synchronous call; a future overlapped callback needs
explicit storage/handle ownership until actual completion.

`libraryimport.html`, **Differences from DllImport / ExactSpelling**, says:
“The method name or EntryPoint should be the exact spelling of the entry point
name.” StringMarshalling.Utf16 chooses a carrier, not automatic W suffix probing.
This supports the dormant LoadLibraryEx candidate.

Context7 first oriented boolean carriers using the .NET best-practices and
custom-structure-marshalling references: UnmanagedType.Bool is four-byte Win32
BOOL; U1/I1 selects one byte. Exact native hidsdi.h, rather than a blanket Boolean
style rule, determines the affected declarations here.

Focused Context7 Native AOT orientation returned
`docs/core/deploying/native-aot/index.md` and `warnings/il3058.md`:
IsAotCompatible defaults IsTrimmable, EnableTrimAnalyzer, EnableSingleFileAnalyzer
and EnableAotAnalyzer; VerifyReferenceAotCompatibility surfaces IL3058 for missing
managed assembly metadata. This explains the shared targets' intent. Configuration
reading establishes neither actual compiler/analyzer participation nor native
package/driver execution. Those remain coordinator validation.

## Published NativeShims and PC/SC boundary

**Title/version/status:** Yubico.NET.SDK release tag 1.18.1, annotated tag object
`3eb2d3c35f9f9b1744ad942a25d6bb37a66eed1d`, commit
`dedba95b94946907112a7953cd82df8a15c22655`. B3's retained tag/ref/tree is the
shared discovery source; B4 fetched only the eight needed package/build/ABI files.
The tag's verification field is unsigned, as already retained by B3.

**Actor/strength:** published native wrapper implementation and packaging intent,
rather than a PC/SC normative standard or deployed-binary attestation.

`native_abi.h` declares char* u8str_t and __stdcall on Windows; Unix branches use
cdecl. `pcsc.c` packs NATIVE_SCARD_READERSTATE to one byte and uses pointer-width
reader/user pointers, uint32_t state/count fields and fixed 36-byte ATR storage.
Native_SCardGetStatusChange (134–179) allocates/frees a platform-state array and
copies its fields. Native_SCardTransmit (181–205) converts receive count but
forwards SCARD_IO_REQUEST* directly. Native_SCardListReaders (207–225) converts
the DWORD count but forwards native char storage without an encoding transform.
Connect/Reconnect return raw native status; no error-code normalization appears.

`CMakeLists.txt` selects winscard/pcsc/macscard, links OpenSSL::Crypto, adds HID
input only for Apple, and merges OpenSSL into the separately produced static
archive. `exports.msvc` names 36 Windows exports and no Mac input-owner functions.
`Yubico.NativeShims.nuspec` declares shared/static assets for win-x86/x64/arm64,
osx-x64/arm64 and linux-x64/arm64. Asset declarations establish intended package
layout, not installed content, selected linker command, loaded path or producer.
The Windows script selects three matching triplets; inspected CMake/header/script
files supply no explicit char* UTF-8 conversion into WinSCardW. The exact deployed
ANSI/UTF-8 producer contract remains the bounded gap.

**PC/SC original source:** `pcsclite.apdu.fr/api/wintypes_8h_source.html` and
`pcsclite_8h_source.html`, live Doxygen source captured by hash/date. Their
non-Apple branch uses unsigned long DWORD; PCI explicitly has two unsigned long
fields (79–84). Thus the Linux LP64 native PCI is 16 bytes, versus managed eight.
This observation is conditional on the native producer using that ABI; deployed
producer/header matching remains open. Its Raw protocol is 4, while managed uses
the Windows value 0x10000.

B1's already-retained `pcsc-lite-api.html` **Differences with Windows**, lines
125–129, explicitly says UNSUPPORTED_FEATURE is 0x8010001F in pcsc-lite versus
0x80100022 on Windows; UNEXPECTED is never returned by pcsc-lite. B4 reused that
original rather than refetching it. Portable Part 5 and WinSCard operation
contracts remain B1-owned originals. B4 adds wrapper/ABI dispositions, rather than
reallocating their findings or certifying physical PIN-state isolation.

**Source identity:** checkpoint-verification.json compares all eight selected
shim source Git blobs with B3's retained release tree. This binds published
source files, while the installed binary's producer correspondence remains open.

## Continuation originals and final applicability

The successor capture manifest contains **42 originals**, all HTTP 200, with URL,
access time, bytes and SHA-256. No capture is an installed-binary attestation.
The eight original shim blobs remain bound to B3's retained release tree; the
three additional build/static-target files use the same immutable release commit.

| Original title / version / status | Actor and strength | Passage and conditions / final disposition |
| --- | --- | --- |
| Microsoft official devpkey.h, devguid.h, hidclass.h, ntddkbd.h, usbiodef.h, winsmcrd.h; win32metadata commit above | Windows native caller; exact header definitions, not execution | DEFINE_DEVPROPKEY/DEFINE_GUID: all 27 property GUID/PID values and active HID/keyboard/USB interface values match. Class SmartCard990a2bd7 not found after focused follow-up; Q003, not a clean/wrong declaration. winsmcrd PCI uses two DWORDs, status0..6 is Windows-shaped. |
| .NET runtime WindowsIdentity.cs, v10.0.0, immutable tagged implementation | Managed token owner, implementation evidence | GetCurrentInternal809–826 returns identity owning acquired SafeAccessTokenHandle; Dispose728–744 disposes it. Inline IsElevated identity lacks deterministic disposal; SafeHandle finalization remains fallback, not proof of no cleanup. |
| USB Device Class Definition for HID, version1.11, release specification dated2001-06-27 | Descriptor producer/parser; normative class item encoding | §§6.2.2.1–3 define short sizes0/1/2/4, reserved type3, long0xFE and no defined long tags. Parser's skip/stop behavior is source evidence, not whole-descriptor validity or driver qualification. |
| USB HID Usage Tables, version1.5, 2024-01-26, published usage specification | HID report/collection producer and consuming host | §38 FIDO0xF1D0 usage1 authenticator, input0x20/output0x21; keyboard/keypad §10 and Generic Desktop Keyboard usage distinguish collections/usages/modifiers from host characters. No universal keyboard-locale/case or direct report-size guarantee. |
| NativeShims1.18.1 Linux amd64/arm64 build scripts and pcscd.cmake; immutable release source | Package producer/build integrator; implementation recipe | Scripts install target libpcsclite-dev; pcscd.cmake selects pcsc backend/pkg-config flags. For inspected Linux LP64 two-unsigned-long PCI headers, direct pointer forwarding disagrees with managed two32-bit fields on the actual default provider path. No conversion exists in that path. Q001 preserves exact deployed header/producer gap. |
| NativeShims1.18.1 Yubico.NativeShims.Aot.targets; immutable release source | Build/link/publish integrator; target intent | PublishAot selects one RID static archive, DirectPInvoke and platform system dependencies; removes shared shim from publish. This contradicts highlights' blanket shared-file sentence, not every historical output directory. Actual evaluated/link/producer result unrun. |
| pcsc-lite API and headers; live original Doxygen retained by B1/B4 | Resource-manager caller; API contract versus platform typedef implementation | Differences with Windows explicitly shares DIRECT, marks USER/TERMINAL unused, gives distinct unsupported-feature/status/raw carriers. State-change UNAWARE is immediate; Windows LocateCards-only ATRMATCH wording is not portable Core behavior. Part5v2.01.01 retained by B1 describes portable resource-manager actors, not physical PIN-state isolation. |
| Microsoft Security Advisory CVE-2026-62900 / GHSA-23fw-v26w-5fgq; published2026-08-11 V1.0, captured2026-11-12 | SDK/package consumer; original vendor security advisory, not a scan of this host | Affected packages table lists Microsoft.Build.Tasks.Git8.0.0 with no patched8.0 NuGet version; fixed SDK/package lines10.0.111/10.0.303. SourceLink comment's reason is supported. Removing explicit8.0 reference alone does not establish a fixed installed SDK: global10.0.300/latestFeature is selection policy, not resolved version evidence. No dotnet --info/restore/build was run. |

### PCI platform/provider boundary

The source trace is `PcscNativeResources.Transmit` → default
`NativeSCardConnectionApi.Transmit` → managed `NativeMethods.SCardTransmit` →
published `Native_SCardTransmit`. Managed caller constructs the eight-byte PCI
and reports that size; the C shim forwards its address as the native PCI pointer.
`ISCardConnectionApi` fake tests never cross that boundary. Actual native owner
confinement protects lifetime but does not change layout.

Host headers `/usr/include/PCSC/{pcsclite,wintypes,winscard}.h` were read as text;
PCSCLITE_VERSION_NUMBER is **2.5.3**, and PCI explicitly uses two unsigned longs.
That corroborates the retained original pcsc-lite source; it is not evidence that
the installed NativeShims was built with those files. Linux x64/arm64 LP64 build
recipes select distro target pcsc-lite headers, so the discrepancy has a concrete
conditional producer/platform route. An ILP32 build or patched producer needs its
own layout. Windows official two-DWORD PCI is eight bytes and is outside this
specific mismatch. macOS selects framework PCSC headers/linkage; Linux header
width supplies no Apple producer/layout conclusion. Q001 records the remaining
artifact/producer conditions precisely, separate from a future successful
transmit and from physical-device qualification.

### Related provider originals and bounded research stop

C's `sources/final-source-register.md`, retained OpenSSL3.0.0 EVP_MAC/EC_POINT/BN
manual sources and .NETv10 provider sources remain the original provider authority.
B4 inspected current BCL factory selection, CMAC wrappers and ARKG SafeHandle
cleanup/import declarations as support dependencies. The current Core code has
no BCRYPT import family to certify. C-C011–C013/C019 retain cleanup/native API
semantics; B4 adds no provider or algorithm execution.

Initial original-source discovery plus focused follow-up recovered the official
Windows catalog and HID/PDF/build sources. It did not recover exact installed
NativeShims producer/header flags, the historical Windows run artifacts, SmartCard
GUID provenance, or a revision for the comparative HIDAPI retry assertion. These
are Q001–Q004, with owners and evidence needed in qualification-gaps.json. No
additional search loop or native probe is needed to finish this pinned packet.
