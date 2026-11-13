# Integration source resolution

The assigned source conflicts are resolved in the working tree. The incoming Windows lifetime routes and extracted adapter organization are retained, and applicable audit qualifications now describe their live owners. The coordinator still owns index resolution, independent review and validation; this report is not a build, test or platform-acceptance result.

## Authority and inputs

- Resolver: GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`).
- Worktree: `/home/rupicola/Code/y/yubikit-windows-audit-integration-20261113`.
- Audited parent: `d5d01ef99f1478e4d49911194c17667b2450d2fd`.
- Incoming parent: `e322712d609aaf03b9bd1cf2964a99326728866a`.
- Natural base: `5746e72ed9a848d08b5782ddde025aa4a4b522ef`.
- Governing decisions: [decision map](../merge-review/decision-map.md), [coordinator decisions](../coordinator-decisions.md), and the [integration plan](../../../../../plans/2026-11-13-windows-refactor-integration.md).

The pinned `HEAD` and `MERGE_HEAD` were checked against the actual applied merge. Preparation records that describe an unapplied object-only preview remain capture-time history, not the current state. Root instructions were also read from `git show HEAD:CLAUDE.md` because the working-tree guide was conflicted. Core instructions, `docs/SDK-HOUSE-STYLE.md` and `docs/research/documentation-audit/records/style.md` were read before editing.

Edits were restricted to C# source/comments under `src/Core/src`, the assigned `verification/LinuxHidRouteVerification/BlockingReportConnection.cs`, and this new report. No other Markdown, tests, assertions, index entries, commits, merge state or refs were edited. No build, test, formatter, codemapper, native call, hardware operation or subagent was run. All product edits and deletions used the patch tool.

## Conflict decisions

| Assigned path | Resolution and preserved boundary |
| --- | --- |
| `src/Core/src/Devices/HidConnectionSlot.cs` | Keep incoming executable code and constructor seams. Windows remains in `OwnsNativeLifetime`, with fresh unopened FIDO/feature API factories and the explicit factory-failure/no-native claim-release path. Keep transport rejection, raw pre-cancellation and post-transfer owner responsibility. Resolve the fallback warning to custom interfaces only, retaining the audit's explanation that a Task return type adds neither native cancellation nor release proof. |
| `src/Core/src/Native/Windows/HidD/IWindowsHidReportAccess.cs` | Delete as incoming requires. Its still-applicable framing, payload ownership and borrowing qualifications are mapped below; the retired synchronous seam is not restored. |
| `src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs` | Delete as incoming requires. Access-policy qualifications move to the actual report APIs. Descriptions of normal-handle/null-OVERLAPPED I/O, inline typed Windows calls, unchecked close, constructor metadata fallback and staging left to GC are retired because they describe the removed implementation. |
| `src/Core/src/Transports/Hid/Linux/LinuxUdevHidEventSource.cs` | Retain only incoming's implementation declaration. Remove the old embedded interface, outcome enum and event record, whose extracted files already exist. Preserve the audited `WaitForEvent` comment distinguishing scoped listener poll leases from report-reactor registration-wide references. No signal, poll, event-order or null-rescan behavior changes. |
| `src/Core/src/Transports/Hid/MacOS/IMacOSHidReportApi.cs` | Keep the incoming interface name and signatures, including default `OpenDeviceResult` behavior and checked-close distinction. Retain the audited managed-composition/status-handling explanation, owner-controlled drain and release qualifications, and registration-wide pin/context-root requirements. Omit the analogy to the deleted Windows seam. Remove the embedded old `IOKitDeviceLifetime` implementation rather than duplicating the extracted implementation. |
| `src/Core/src/Transports/SmartCard/PcscNativeResources.cs` | Keep incoming `_connectionApi`, constructor parameter `connectionApi`, forwarding and `PcscConnectionNativeOwner` cref. Combine the audited requested-power-off qualification with `_connectionApi.Disconnect(cardHandle, SCARD_DISPOSITION.UNPOWER_CARD)`. Checked disconnect and checked context release remain separate required steps; this is not evidence of physical power cycling or PIN-state clearing. |

## Retired-to-live qualification mappings

### Windows reports

- `IWindowsHidReportAccess` native-length/framing contract → `Transports/Hid/Windows/IWindowsHidReportApi.cs` and `IWindowsHidFeatureReportApi.cs`. Both now explicitly distinguish native descriptor lengths, which include the zero report-ID prefix, from payload sizes, which exclude it. Typed FIDO64 and OTP8 remain separate connection contracts.
- Old `OpenIOConnection` access qualification → `WindowsHidReportApi.cs`. Its actual FIDO route requests read/write access and overlapped I/O; zero-access feature opening is not a fallback. The collection, ACL, driver, token and existing sharing policy still govern success. Elevation is environment-specific, not a universal prerequisite.
- Old `OpenFeatureConnection` access qualification → `WindowsHidFeatureReportApi.cs`. Its actual route requests zero desired access for HidD feature calls without read/write fallback. Metadata acquisition does not establish feature permission or transfer success. Collection/ACL/driver/token/sharing qualifications remain explicit.
- Old `FILE_SHARE.ALL` qualification → both concrete report APIs. Compatible requested sharing does not override another handle's restrictions or acquire a grouped SDK claim.
- Old independent-payload/staging distinction → `WindowsHidIOReportConnection.cs` and `WindowsHidFeatureReportConnection.cs`. Successful payload arrays belong to the caller. The native owner clears its separate staging after last use; this does not clear returned payloads or protocol/application output. Direct opens acquire no implicit grouped claim. Existing descriptor-sized SET rules and indefinite disposal drain are retained.
- Old synchronous borrowed-memory/unchecked-disposal limitation → the actual `WindowsFidoHidNativeOwner` and `WindowsOtpHidNativeOwner` contracts, not a copied warning about the removed owner. Existing incoming code/comments already specify FIDO submit-return plus callback-last-use retirement, OTP synchronous borrowing through native return, independent successful receive storage, staging clearing, and checked release before claim release. These owner files and typed facades were inspected but did not need edits. FIDO cancellation or ERROR_NOT_FOUND is not completion; OTP cancellation cannot detach dispatched calls. Unproven cleanup retains the rooted owner and original claim.

Both incoming concrete APIs retain their exact access-denied runtime guidance strings. This resolution qualifies explanatory comments rather than changing diagnostic text.

### Linux and macOS extraction

- Old combined `LinuxHidEventSource.cs` interface comments → `ILinuxHidEventSource.cs`. Fake-driven loop-policy/caller-stop tests do not establish native poll completion or a shutdown deadline. Null receive can mean no event or failed reception, and the listener requests a rescan. `SignalShutdown` attempts a wake; normal return is not acknowledgment, drain or release. The existing four-attempt EINTR policy, EAGAIN handling and scoped handle leases are unchanged.
- Old combined Linux outcome/event declarations → `LinuxHidPollOutcome.cs` and `LinuxHidUdevEvent.cs`, retained unchanged. Event identity fields remain rescan hints rather than physical identity attestation.
- Old `IIOKitDeviceLifetime` interface → `IMacOSHidReportApi.cs`; old `IOKitDeviceLifetime` implementation → `MacOSHidReportApi.cs`. The extracted implementation now carries the audited forwarding-plus-managed-compositions summary and non-seizing comment. Process-local admission and OS permission are separate policies; unqualified Rust/Python parity prose is not restored.
- `IMacOSHidInputBridge`, `MacOSHidInputBridge`, `MacOSFidoHidNativeOwner` and `MacOSOtpHidNativeOwner` remain the incoming composition. The bridge owns callback mechanism/state, not another scheduler or physical claim. Device close, CF release, callback quiescence and physical-claim release remain distinct. No macOS native-equivalence or hardware acceptance is claimed.

## Clean overlaps and active followthrough

| Path or group | Disposition |
| --- | --- |
| `src/Core/src/Native/Windows/HidD/HidD.Interop.cs` | Keep all incoming declarations unchanged. Refine the audit warning: older Bool-return imports remain uncorrected; new `GetHid*`/`SetHid*`/`FreeHid*` declarations already return byte. `HIDP_CAPS` still stores unsigned native fields in signed shorts; new report APIs reinterpret them as ushort, while other consumers need separate review. No deferred ABI fix is implemented. |
| `src/Core/src/Transports/Hid/MacOS/MacOSHidIOReportConnection.cs` | Accept the clean merge unchanged. Keep renamed bridge, descriptor-metadata versus normalized input64/zero-prefixed65, terminal other lengths, supported SET sizes, and six-second reader-detach qualification. No timeout is imported into Linux or Windows. |
| `src/Core/src/Transports/Hid/MacOS/MacOSHidFeatureReportConnection.cs` | Accept unchanged. Keep the renamed API, expert GET8/padded-short versus typed exact8 distinction, supported SET lengths, worker wait and checked-close drain. |
| `src/Core/src/Transports/SmartCard/SmartCardConnectionFactory.cs` | Accept unchanged. Keep `PcscConnection` and `connectionApi:` calls with audited exclusive/no-shared-retry, explicit coexistence, factory-policy snapshot, requested-UNPOWER and scoped ownership examples. |
| Core `PcscConnectionLifetimeTests.cs` and `SmartCardSharingTests.cs` | Read-only clean-overlap review: the working-tree merge retains audited XML summaries and incoming construction/named-argument changes. Scope-end holds transmit, not native EndTransaction; failed end reporting is separate from release proof. Keep incoming `NativeOwner_` rename and all assertions. Sharing tests observe policy and requested release arguments, not physical PIN/power/process isolation. No test file was edited. |
| `src/Core/src/Devices/RegisteredConnections.cs` | Add Windows to the built-in native-owner transfer comment. Preserve compatibility wrappers' existing release-on-failure distinction and all executable behavior. |
| `src/Core/src/Devices/SharedDeviceInfoRead.cs` | Add Windows to the pre-open discovery-owner transfer comment. Preserve exact registration transfer before invocation, selected-slot resolution, failed/late-open responsibility and no double-release behavior. |
| `verification/LinuxHidRouteVerification/BlockingReportConnection.cs` | Narrow the blocking-adapter explanation to custom providers/test contrast. Built-in Windows bypasses that adapter. Keep diagnostic behavior untouched. |
| Other clean-overlap Markdown | Assigned to the separate documentation resolver. This source report does not edit or certify that agent's files. |

The incoming Linux native API/result, read-registration and readiness-native splits remain unchanged. Activation is not quiescence, a removal request is not drain, and retained-reference release follows actual quiescence on the original owner worker before checked close. PC/SC retains `PcscConnection`, `PcscConnectionNativeOwner`, `NativeSCardApi` and `NativeSCardConnectionApi`; no removed implementation is restored. The intentional literal `ObjectDisposedException("UsbSmartCardConnection")`, public `PscsConnectionKind`, public `DesktopSmartCardDeviceListener` and public/named-parameter contracts are untouched.

## Runtime differences and bounded static checks

There is **no integration-authored executable difference from incoming in the 16 surviving edited C# files**. A read-only comparison removed whole-line comments and blank lines from each file and its incoming blob, then required exact equality of every remaining line. This preserves signatures, named parameters, diagnostic strings, native arguments and call ordering in those files. Deleting the two retired files and removing duplicate extracted declarations follows incoming's organization; it does not introduce replacement behavior.

Relative to the audited parent, the candidate receives the approved incoming Windows owner/routing implementation and mechanical renames/extractions. This resolution does not revert those changes to preserve obsolete comments. Conversely, audit-side runtime changes outside incoming path spellings are retained: a read-only comparison of base→ours and base→incoming path sets found 161 audited paths under `src/Core/src` outside incoming path spellings. Of those, 159 are byte-identical to ours, and only the two approved `RegisteredConnections.cs`/`SharedDeviceInfoRead.cs` comment followthrough files differ; their non-comment lines match ours. This set includes non-C# files under that directory, which were read but not edited. Both old macOS FIDO/OTP combined files are byte-identical between base and ours, confirming there is no audit-side delta there to port through extraction.

Fresh static checks completed:

1. Before editing, capture SHA-256 for all 336 then-present owned C# files. After editing, exactly the 18 source paths listed below differ: 16 survive and two are deleted. No additional C# file was created.
2. Scan all 334 surviving owned C# files: no conflict markers and no references to `IWindowsHidReportAccess`, `WindowsHidReportAccess`, `IIOKitDeviceLifetime` or `IOKitDeviceLifetime`.
3. Check one declaration each for 19 extracted/live types: `ILinuxHidNativeApi`, `LinuxIoResult`, `ILinuxHidReadRegistration`, `ILinuxHidReadinessNative`, `LinuxHidPollOutcome`, `LinuxHidUdevEvent`, `ILinuxHidEventSource`, `LinuxUdevHidEventSource`, `IMacOSHidReportApi`, `MacOSHidReportApi`, `IMacOSHidInputBridge`, `MacOSHidInputBridge`, `MacOSFidoHidNativeOwner`, `MacOSOtpHidNativeOwner`, `PcscConnection`, `PcscConnectionNativeOwner`, `PcscNativeResources`, `NativeSCardApi`, and `NativeSCardConnectionApi`. Each has exactly one declaration under owned source.
4. Run `git diff --check -- src/Core/src verification/LinuxHidRouteVerification/BlockingReportConnection.cs`: exit 0, no output. This is a whitespace check, not compilation or formatting validation.

The source scan does not assert that the index has been resolved: unmerged entries remain the coordinator's responsibility. No new runtime or whole-platform result follows from these static checks. Independent review, compilation, tests, inventory reconciliation and platform-specific qualification remain pending in their authorized lanes. Historical Windows results retain their producer/source/device/OS scope; macOS extraction and Windows native/HID-driver/JIT/AOT acceptance are not newly established here. No PIV assertion or rollback behavior was changed, and no physical PIN isolation, power cycle or release result is claimed.

## Documentation style applied

Comments follow the local documentation voice: lead with behavior, name the responsible owner or caller, keep transport/permission/lifetime conditions beside the claim, and retain qualifications that prevent unsafe conclusions. The native-adapter role vocabulary follows `SDK-HOUSE-STYLE`: report APIs are mechanical seams subordinate to native owners; facades expose the appropriate payload contract rather than acquiring scheduling or claim authority. No new abstraction, symmetry-driven helper, speculative platform promise or unrelated wording cleanup was introduced. The shared style guide itself belongs to the documentation resolver and was not edited here.

## Exact source change manifest

These are SHA-256 hashes of working-tree file bytes immediately before this resolver's edits and after source resolution. A conflicted file's “before” hash includes its actual merge markers; it is not the audited-parent blob. `DELETED` means the path is absent, not an empty file. Patching also adds a final newline to edited files that previously lacked one.

| Path | Before SHA-256 | After SHA-256 |
| --- | --- | --- |
| `src/Core/src/Devices/RegisteredConnections.cs` | `f8d8b39cb29a4225cd933ca868fd39d20d588c03fdde2ee485db3c153d5cb8e0` | `a9456f2c65b5fde1d2d7baccadb9dcbaf4ee3f354fab8e13341670324c0e885a` |
| `src/Core/src/Devices/SharedDeviceInfoRead.cs` | `ba072b3d0bf55003ff4b4bae305473a634b20684bfc1041c5f5f1462e9c14392` | `6fc663283e11019485345f4b702e50a0d59d7014cca306cdb95254b6ce6209e2` |
| `src/Core/src/Devices/HidConnectionSlot.cs` | `fc2b4a443f1b12392f2d1936b01772268720b644f0b00f2c9add0bb16e4d7231` | `f3f5bfdb61b41ba9f85b0252672aa13028c0a32607a476ab049d615b0e02dc81` |
| `src/Core/src/Transports/SmartCard/PcscNativeResources.cs` | `a9e94502bde6b0cc62b9ae6122e460c01b0c7e3be40abc0e3f8ec0f2de27bdd0` | `fd190b2438ef98e31ec42040ca4800c75670cddcd87c7b75a493b12426ab1e68` |
| `src/Core/src/Transports/Hid/Linux/ILinuxHidEventSource.cs` | `e87bd101c993303e125fb669c0956d31d4c80421beb562acdd066b6c49eb3a84` | `0bf5f8f1f3f0105e790851b1d86a6774ac004bae798e8555017fe60d86f91707` |
| `src/Core/src/Transports/Hid/Linux/LinuxUdevHidEventSource.cs` | `0355db9306746ca3eeeb16e0a113676b425479ff09d889596fc3a6d05907d62a` | `01303fb68bb73f9c5ca704a17ffba9a31d3d465b4251904e1792c07eaa08410b` |
| `src/Core/src/Transports/Hid/MacOS/IMacOSHidReportApi.cs` | `e52961118747a527d90bd82ca2bf7a7b862df046e11694b7a12ed7277859256d` | `5a245f409bdca58848b56eee44777cb37c10844b62a44ebf1c3d745473465d53` |
| `src/Core/src/Transports/Hid/MacOS/MacOSHidReportApi.cs` | `e26e2f6305e6a9201b10c478216fb07dfc6a624e722f9a2581e72cd333ec38a9` | `deb7517608fa98a9ea937d327e70677a299a5f4ec9d81aa0ba49a903f9603406` |
| `src/Core/src/Transports/Hid/Windows/IWindowsHidFeatureReportApi.cs` | `c8e4037aea101f73940623eb5e95ab58f3ee9f5e4850916e725a526a3702e803` | `41acad03e3e696cb0eafac73e43818883bd8fa19bad8ec07ddb2dc2bab7be064` |
| `src/Core/src/Transports/Hid/Windows/IWindowsHidReportApi.cs` | `c1cbef3fa9bf8a54d97d45c5caf5b991ce0807ef5ac1960ff4685bd3e3218ff3` | `d51ca95706f35ac756a69cba1f2345d3b92f91b37df397996fa3e20a849f87b7` |
| `src/Core/src/Transports/Hid/Windows/WindowsHidFeatureReportApi.cs` | `d362dbcb1f3b8f0bfec4658b7c2a866ca26c2a454d43ac366dad968c0db448ff` | `aa8938cb94b3f05800d1bf8e75c57fcf37b462ede974152c9fa983eb63b4f2c8` |
| `src/Core/src/Transports/Hid/Windows/WindowsHidFeatureReportConnection.cs` | `e47ae9eb53144ede91f45312b84a1038f62716575eca48a514bdda7a11c0ecf8` | `71dbb8cf99e9b7fdf0bccbe47a0a20cb9ccde6787cda366857cd777987f589ae` |
| `src/Core/src/Transports/Hid/Windows/WindowsHidIOReportConnection.cs` | `0240c331cdea8596550bcb56b215186e279f00937e9b4807439a1192dc4f164b` | `409d2dc4631e036a3dfcec4877c9af44c134537671c8740f5fa5331666e3d123` |
| `src/Core/src/Transports/Hid/Windows/WindowsHidReportApi.cs` | `a924d138f807f76458e125fe9145ed42854e9c7cbf6692d4d2dbfb450cff46ac` | `ebba1ce849828d962e422eebd02a73ac2420f25d5aea3a4b2a73375cf40359b8` |
| `src/Core/src/Native/Windows/HidD/IWindowsHidReportAccess.cs` | `8c7dfabb8b3d011317912b1c71dc6374b2a15318cb49918d40acb125da5bbd21` | DELETED |
| `src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs` | `183a5bcd93e003b9a4f6fb17178fe2e72c1e221652fc0c4287cbecb330a18d86` | DELETED |
| `src/Core/src/Native/Windows/HidD/HidD.Interop.cs` | `cf7f3541e1b6d26b416bdf28ec8ad8fbfc3658db95db1dbd110677b164e4efe4` | `8bdc36dc9d6c3d91dee40e53da899805f82783c044c9288118697d105b17e333` |
| `verification/LinuxHidRouteVerification/BlockingReportConnection.cs` | `175517f1fac130a7c052d95d437594879a314a097e245936f6d1107d32bb65de` | `8f1bea02ecefd860c20e27bebb5e2d5f336099c142a38321d1e9044ca74ac43c` |

The only additional file created by this resolver is `docs/research/documentation-audit/integrations/windows-upstream-20261113/execution/source-resolution.md` (this report; absent before). Its own hash is excluded from the embedded manifest to avoid self-reference. No other source or documentation path is part of this resolver's edit set.
