# Independent source integration review

**No Critical or Important source-integration issue found. The reviewed source candidate is ready for software validation, within the limits below.** This is not build, test, native-equivalence, device, publication, or platform acceptance. Documentation review is separate.

## Reviewer, authority and candidate

- Reviewer: fresh independent GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), separate from the source resolver.
- Worktree: `/home/rupicola/Code/y/yubikit-windows-audit-integration-20261113`.
- Audited `HEAD`: `d5d01ef99f1478e4d49911194c17667b2450d2fd`.
- Incoming `MERGE_HEAD`: `e322712d609aaf03b9bd1cf2964a99326728866a`.
- Independently checked merge base: `5746e72ed9a848d08b5782ddde025aa4a4b522ef`.
- Governing inputs: `merge-review/decision-map.md`, `coordinator-decisions.md`, and `docs/plans/2026-11-13-windows-refactor-integration.md`.
- Instructions consulted: root/Core contributor instructions, PIV instructions for the read-only rollback inspection, house style, and `docs/research/documentation-audit/records/style.md`.

I inspected actual working-tree C# and parent blobs/diffs rather than accepting the resolver's conclusions. Product source, assertions and documentation were not edited. Only this new review record was created. No build, test, native/device/cryptographic exercise, formatter, inventory generator, subagent, index mutation, ref mutation or commit operation was performed. Reading source and computing requested file fingerprints are static review operations.

## Identity checks and newline correction

The coordinator's `execution/terminal-newline-correction.json` is the current pin for the 16 surviving resolver-edited C# files. The resolver's original 18-path manifest remains its predecessor, including the two deletions. `.editorconfig:36` specifies `insert_final_newline = false`.

At the review checkpoint after receiving that correction, and again after source inspection:

1. All 16 surviving files matched their successor SHA-256 values in the correction JSON.
2. Appending exactly one LF to each current file reproduced its predecessor hash in both the correction JSON and resolver report. This independently verifies the stated LF-only transition; no other byte difference was accepted as part of that correction.
3. Both retired Windows report-access paths remained absent.
4. The aggregate identity of all 1,363 on-disk C# files under `src/`, `verification/`, and `scripts/`, excluding `bin`/`obj` path components, remained unchanged:

   `c05c4010789edd3424ec62dc143c68bb3fa3cdc36cacc2eef4069c27d6e82b02`

The aggregate is SHA-256 over lexically sorted repository-relative paths, each encoded as UTF-8, followed by NUL and the raw SHA-256 digest of that file's bytes. This pins source bytes, not a compiled artifact or resolved index.

The consumed record identities at the final checkpoint were:

| Record | SHA-256 |
| --- | --- |
| `execution/source-resolution.md` | `eb66131e19288058d4e5102ccfbe4c550f714fc931bf055aecab8f8b7910f3e6` |
| `execution/terminal-newline-correction.json` | `791f42da01ec24211b120a42fc62757ec266569e8ff9d4066a4ce2f35b22a515` |

Both parent pins were rechecked at the end and were unchanged. Actively edited narrative Markdown was not assessed for acceptance by this source review.

At handoff, the coordinator supplied product-tree identity `05d4409729dd81c7717c99a15a2697d49eb10906` in `execution/candidate-before-validation.json`. All 114 C# file/deletion identities in that receipt independently match the reviewed working tree, and the 1,363-file aggregate and 18-path resolution pin were rechecked unchanged after the coordinator's staging update. I did not construct that tree or certify its index/narrative contents. The coordinator also supplied `execution/piv-rollback-reconciliation.json`; its production/test identity conclusions agree with the independent comparisons below. Reported coordinator build results and the still-running unit suite belong to the separate validation lane, not to this review's evidence.

## Both-parent preservation checks

A fresh base-to-parent comparison, with rename detection disabled to include both old and new path spellings, identified 111 incoming C# path spellings. Nineteen of those differ in current bytes from incoming. Inspection of their diffs shows retained audit commentary plus the audited redundant `using Yubico.YubiKit.Ctap2;` removal in `Ctap2SessionHidCrossLayerTests.cs`.

For every surviving incoming C# path, removing blank lines and whole-line `//` comments leaves exactly the incoming lines, except that single redundant using. After accounting for it, those lines match too. Deleted incoming paths remain absent. This comparison retains signatures, directives, statement ordering, native arguments, exception strings and assertions; it is a lexical preservation check, not a compiler or semantic-equivalence proof.

I also independently checked all 1,495 audited C# path spellings outside the incoming path set, including historical paths. Only `RegisteredConnections.cs` and `SharedDeviceInfoRead.cs` differ from audited HEAD, and both differences are the approved Windows owner-transfer comments. Restricting that comparison to `src/`, `verification/`, and `scripts/` gives 729 audited non-overlap paths: 727 byte-identical to HEAD and those same two comment-only recipients. This preserves the audited runtime corrections as well as their qualifications, including Base32 admission/cleanup and the Core/CTAP2/applet protocol, initialization and ownership paths; it does not requalify their known deferred limitations.

## Six source conflict dispositions

| Resolved path | Independent assessment |
| --- | --- |
| `src/Core/src/Devices/HidConnectionSlot.cs` | Incoming executable code retained. Windows is in `OwnsNativeLifetime`; typed FIDO/OTP go to their own native owners. Fresh unopened API factories remain per acquisition. A factory-construction failure has the explicit no-native claim-release path. Wrong-transport rejection and raw pre-cancellation remain; the transferred-claim route does not introduce an early-cancellation escape. The compatibility warning now applies only to custom interfaces. |
| `src/Core/src/Native/Windows/HidD/IWindowsHidReportAccess.cs` | Correctly deleted. Report-ID framing, payload sizes and caller-owned receive storage are explained at the live report/feature seams and facades rather than by retaining an inactive type. |
| `src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs` | Correctly deleted. The live APIs preserve operation-specific access, ACL/token/collection/driver/sharing qualifications and the incoming access-denied guidance strings. Removed normal-handle/null-OVERLAPPED, constructor fallback, unchecked-close and GC-only staging descriptions are not misrepresented as current behavior. |
| `src/Core/src/Transports/Hid/Linux/LinuxUdevHidEventSource.cs` | Contains the implementation only. Extracted interface, outcome and event declarations are not duplicated. Actual poll leases, shutdown-before-monitor event ordering, null receive and four-attempt EINTR/EAGAIN signal handling remain incoming. The audited scoped-poll versus registration-wide reactor distinction is retained. |
| `src/Core/src/Transports/Hid/MacOS/IMacOSHidReportApi.cs` | Retains incoming signatures/default `OpenDeviceResult`, checked-close distinction, CF ownership and callback APIs. Audited managed composition/status handling, seam-not-release-proof and persistent pin/context-root qualifications survive. The implementation is in `MacOSHidReportApi.cs`, not duplicated here. |
| `src/Core/src/Transports/SmartCard/PcscNativeResources.cs` | Retains `connectionApi`, `_connectionApi` forwarding and live owner cref. Strict exclusive defaults and optional fallback are unchanged. Checked disconnect requesting UNPOWER precedes checked context release. The retained comment correctly distinguishes requested native disposition from observed power-off or PIN-state clearing. |

## Windows ownership, cancellation, ABI and storage

I read the actual slot/device/discovery transfer paths, both Windows native owners, both mechanical APIs, overlapped request carrier, SafeHandle, typed facades and direct report facades.

- `YubiKeyDevice` and `SharedDeviceInfoRead` transfer the original claim before owner acquisition. Discovery marks transfer before invoking open and releases in its own `finally` only when ownership was not transferred. The selected transport still has to match. Adding Windows through the slot's existing predicate does not replace these audited rules with a wrapper release-on-failure path.
- FIDO roots before acquisition and admits one ordinary request. The worker cannot retire an accepted request until submission returns and completion has been recorded. Immediate success still requires the callback. Targeted `CancelIoEx`, including ERROR_NOT_FOUND, is not terminal evidence. Native success is not overwritten by late cancellation. Callback resource-last-use, request retirement, staging clearing, actual public completion and checked native release remain separate steps.
- The known overlapped-preparation failure path still faults the actual operation and shutdown, clears payload storage after proven no-dispatch, and retains uncertain BCL resources/root/claim without pretending that a null pointer proves release. No conflict choice silently converts this into a clean rejection or retry.
- OTP remains a separate blocking-worker lifetime with zero desired access and no read/write fallback. A cancellation observed before dispatch prevents the call; a dispatched feature call retains storage/admission through native return and can succeed after cancellation. HidD success supplies no invented transfer count.
- Successful receive payloads escape independently of native staging. Both owners clear their owned outgoing/native staging after last use; caller payload and protocol/application output retain separate cleanup owners. Unknown checked release retains/quarantines ownership rather than releasing at a public fault, cancellation or discovery wait expiry.
- New HidD imports return byte and use unsigned native lengths; Kernel32 BOOL results remain four-byte integer carriers. The added warning accurately leaves older Bool-marshalled declarations and signed `HIDP_CAPS` storage qualified. New APIs reinterpret descriptor fields as `ushort`; this is not a claim that every consumer or residual ABI issue was fixed.
- Direct facades drain these owners synchronously without importing the macOS six-second reader timeout. Descriptor payload sizes exclude report ID; SET retains exact descriptor-size requirements. Typed FIDO64 and OTP8 remain separate contracts. Direct opens do not acquire an implicit grouped claim.

These are source-ordering and preservation observations, not proof of BCL callback-tail behavior or HID-driver races on Windows.

## Clean overlaps, extractions and references

The source clean overlaps were individually inspected, not accepted merely because Git merged them:

- `HidD.Interop.cs`: incoming declarations plus the precise residual audit warning; no integration ABI implementation change.
- macOS `MacOSHidIOReportConnection.cs`: renamed bridge, descriptor metadata versus fixed input64/zero-prefixed65, terminal rejection of other input lengths, supported SET sizes and reader-only six-second timeout retained.
- macOS `MacOSHidFeatureReportConnection.cs`: renamed API, expert GET8 with zero-padded successful short results versus typed exact8, supported SET lengths and worker/checked-close drain retained.
- `SmartCardConnectionFactory.cs`: live `PcscConnection` construction and `connectionApi:` arguments coexist with audited exclusive/no-shared-retry, explicit coexistence, policy snapshot and scoped ownership examples. No public factory parameter was renamed by reconciliation.
- `PcscConnectionLifetimeTests.cs` and `SmartCardSharingTests.cs`: incoming construction/named-argument changes and approved `NativeOwner_` test rename retained with all incoming assertions. Audit summaries still distinguish held transmit from held native EndTransaction, error reporting from release proof, and requested UNPOWER from physical isolation.
- Additional two-sided C# overlap `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/Ctap2SessionHidCrossLayerTests.cs`: incoming `IMacOSHidInputBridge` substitution retained along with audited fixture limits, missing-AAGUID comment and redundant-using removal. No assertion or scripted cancellation/drain/reuse behavior was changed.

Linux `ILinuxHidNativeApi`/`LinuxIoResult`, read-registration, readiness-native, poll-outcome and event splits retain their declarations/contracts in incoming files. The extracted registration still distinguishes Activation, actual Quiescence and owner-worker reference release; removal/wake requests are not drain. The listener interface now retains the audited no-event/error/null-rescan and attempted-wake qualifications. The listener implementation and native signal policy were not changed to make those comments sound stronger.

For macOS, both old combined FIDO/OTP files are byte-identical between base and audited HEAD. I compared their nested owners to the actual extracted owners: differences are the approved owner/seam/field/parameter names, moved constants, equivalent OTP8 constant use and formatting/layout. Worker/admission, callback/root, public-read drain, output-copy clearing, close and claim-release flow remain the received organization. The extracted mechanical API retains the audit's composition/status-handling and non-seizing qualification. This does not certify native equivalence.

For PC/SC, the old facade/state-to-current connection/owner comparison shows the approved internal rename and extracted APIs, not a replacement lifetime design. Worker confinement, one ordinary slot, admission retirement before public completion, transaction dispositions, checked disconnect/context release and quarantine remain. `ObjectDisposedException("UsbSmartCardConnection")` is intentionally preserved at `PcscConnectionNativeOwner.cs:252`. Public `PscsConnectionKind` and `DesktopSmartCardDeviceListener` remain unchanged.

The two owner-transfer comment recipients and `verification/LinuxHidRouteVerification/BlockingReportConnection.cs` correctly describe built-in Windows versus custom fallback without changing execution. A scan of the 1,363 active-root C# files found no conflict markers and no references to retired Windows report-access or macOS seam/bridge/state names. The only old `UsbSmartCardConnection` occurrence is the intentional runtime diagnostic above. Each of the 19 extracted/live declarations named in the resolver's bounded duplicate check independently has exactly one declaration under `src/Core/src`.

## PIV rollback and assertion boundary

The actual `PivPinOnlyModeSessionTests.cs` is byte-identical to incoming. Its capture keeps non-pooled response storage only. Recovery candidate counts, witness submissions, cancellation/unexpected-status handling, restoration outcomes and response clearing assertions remain; no pooled command view is retained for post-return observation and no injected pool oracle was restored.

Current `AuthenticationProtocol.cs` is byte-identical to audited HEAD blob `eed0152966b89f8627c4749daa359d2c65b92f59`. Incoming/base restored blob `526c5da214546c1655ab8f36e2f7e95831cfdbf0` differs only in the audited explanatory comments; the authentication method and executable lines are unchanged. I inspected the actual shared-pool rent/finally-zero/return path without exercising it. This preserves the committed incoming rollback and audit explanations, not a late-memory runtime assertion of pre-return clearing. The coordinator's public-delivery adjudication is sufficient; no private rollback-instruction prerequisite is imposed.

## Findings and handoff limits

- **Critical:** none found in this bounded source integration review.
- **Important:** none found in this bounded source integration review.
- Fresh `git diff --check -- src verification scripts` exited 0 with no output. This is a whitespace check, not formatter or compilation validation.
- Start/end source identities match. No product correction is requested by this review.

Proceed to the coordinator's authorized software-validation gates and separate completed-Markdown review. Compilation/analyzers, executed test selection/results, API/inventory reconciliation, example contexts, packaging and final index/commit scope remain unverified here. Historical Windows first-open/mixed ceremony results, unsealed JIT versus sealed/AOT/HID-driver limits, unavailable raw macOS evidence, native extraction qualification and PIV physical deferrals retain their existing scope. This review neither closes those items nor requests unrelated deferred fixes.
