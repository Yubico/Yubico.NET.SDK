# B3 macOS HID, callbacks and native lifetime investigation

**Outcome:** completed semantic reading of all **41 primary files / 7,636 lines**, **1,139 primary
units** and the packet's actual **five shared-read units**. Nine local candidate groups are ready
for coordinator triage. The ledger contains 1,087 read-clean and 57 read-candidate outcomes, with zero
unread primary/shared IDs and zero primary exclusions. These counts establish source-review scope,
rather than native acceptance or nine independent runtime defects.

Source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed HEAD was the records-only
`01a114d4e251e0f52b92f73393fbe1dca5d5c24e`. All selected files/ranges match their inventory hashes;
every selected path matches the source pin after explicit Git newline normalization. B3 owns only
`investigation/B3/*` and this report. Product files, assertions, central inventories, protected history,
index and refs remain untouched. No subagent, SDK build/test, formatter, native library, device, CLI,
package import, C# type initializer, commit or push was run. Python performed source capture/static
text/hash checks only. All newly written bytes are inside B3's allowed record paths; no scratch was
written to the exhausted temporary filesystem.

## Record map

| Record | Purpose |
| --- | --- |
| [Unit dispositions](investigation/B3/unit-dispositions.json) | Every original assigned ID/range/hash, actual read context, local candidate, review and validation status |
| [File identities](investigation/B3/file-identities.json) | All primary/shared files, raw hashes, pinned blobs and LF identity |
| [Complete file evidence](investigation/B3/file-evidence.md) | Distinct owner, native, assertion and verification-host reasoning for every primary file |
| [Candidates](investigation/B3/candidates.md) | C001–C009, separate documentation/runtime/decision/qualification facets and unchanged-path reopening maps |
| [Source register](investigation/B3/sources/register.md) | Original Apple/header/shim/.NET passages, exact revisions, actors, strength and applicability |
| [Original capture manifest](investigation/B3/sources/captures.json) | Original URL, access time, status, bytes and SHA-256 |
| [Static source checks](investigation/B3/source-checks.json) | Published shim source Git-blob correspondence; managed versus Apple status values |
| [Coverage](investigation/B3/coverage-summary.json) | Exact populations and pending independent review/validation |
| [Secondary handoffs](investigation/B3/secondary-handoffs.json) | B1 platform-note partial disposition and B4 callback-instruction correction, without denominator inflation |
| [Verification](investigation/B3/verification.md) | Reconciliation results and bounded future experiments; all new SDK executions remain unrun |

B3 read root/Core/nested test instructions, Core README, campaign workflow/style, approved plan,
inventory, dispatch/source ownership and SDK house style. Verification has no nested CLAUDE/AGENTS
files; each of its three actual project files and all host sources were read. B1's complete return,
ownership vocabulary, candidates and file evidence were consumed, along with A's complete return,
metadata alias/PID-cache questions and C's shared pinned Python context. Those remain their owners'
findings. Native source outside the packet was read narrowly from the published release, rather than
from an unfinished working branch or a new checkout of the Apple source forest.

## Consumer task and ownership model

Prefer an applet session or a typed connection/raw session. `IYubiKey.ConnectAsync<T>` acquires all
known member IDs; the Mac slot transfers that exact opaque registration before native acquisition.
`SharedDeviceInfoRead` likewise transfers its interface discovery lease, including its PID admission,
before calling the native owner. The Windows/custom registered HID disposal wrapper is bypassed.
An outer canceled wait, disposal exception or discovery-scope retirement never manufactures release.
Direct `IHidInterface.ConnectTo*Reports` remains a synchronous expert open without a grouped claim;
its shared native owner still retains uncertain native state even when there is no registry token.

| Resource / state | Owner and observed transition | Bound |
| --- | --- | --- |
| Device creation reference | Managed FIDO/OTP owner acquires IOHIDDeviceRef on its worker; matching dictionary consumed and service released by CreateDevice | CFRelease and Close are separate responsibilities |
| FIDO input backend | Native Create retains a second device reference and owns native input buffer, ring and serial queues | Published 1.18.1 source located/read; installed binary's reproducible producer binding remains open |
| Callback context | Bridge GCHandle owns CallbackState; static Cdecl callback recovers it | Free only after Destroy returns OK; callback exceptions are contained |
| Managed lifetime root | GCHandle roots Owner independently of the facade | Native uncertainty retains root and original registration; facade collection alone proves no release |
| Native input buffer | Backend allocates persistent native report buffer; incoming copy enters native bounded ring | No temporary managed array pin is used for active persistent input |
| Managed report staging/result | Bridge copies native bytes, clears temporary in finally; Owner copies normalized 64 bytes to reader or bounded queue | Successful result ownership escapes; terminal clears queued arrays, not already returned results |
| FIDO pending reader | One identity-tagged TCS and cancellation registration | Cancellation detaches only that reader, frees overlap slot and keeps native input running |
| Actual public read task | AwaitReadAsync disposes cancellation registration; public marker retires after that wrapper is terminal | Shutdown waits for markers as well as native close, preserving the fixed public-task completion barrier |
| Ordinary output | One admitted call copies before publication; original worker calls synchronous SetReport | Copy cleared before send task terminal; token observed before dispatch, no native output interrupt/deadline |
| OTP GET/SET | Separate one-worker owner, no FIDO reactor/input queue | Active cancellation can still return success after native return; checked close follows admitted call |
| Shutdown request | Admission closes, managed FIDO terminal wake faults reader and clears queue | Native Cancel occurs later on the worker, after any held output returns; terminal wake itself is neither native interruption nor close |
| FIDO quiescence/release | Cancel → WaitShutdown OK/FAULT → checked Destroy OK → managed creation CFRelease → original registration release/root free | Wait OK/FAULT means quiescent, while Destroy determines release; no replacement worker or retry on uncertainty |
| OTP release | Checked IOHIDDeviceClose success → CFRelease → original registration release/root free | Every nonzero close is conservatively unproven here, unlike native FIDO's narrow removed/drained status allowances |
| Listener generation | Generation root owns manager/context/thread, retained loop and mode | Stop requests wake and joins one shared budget; actual run-loop callback exit precedes same-thread cleanup |
| Listener timeout | Retains generation; repeated Stop consumes no fresh budget and Start refuses restart | Late cleanup enables restart; a permanently unstarted/ambiguous registration has no late worker recovery (C005) |

The built-in open passes option 0. Apple's API assigns exclusive links to SeizeDevice; this source
chooses process-local registry admission instead. Both FIDO and OTP explicitly recognize
0xE00002C5 as a failed open that still needs checked close. There is no non-seizing retry or transport
fallback after an ambiguous failure. This completes the macOS scope of B1-C005: native owners,
rather than compatibility wrappers, control the original token through successful, failed and canceled
opens. Safe transfer is supported by managed source and fake oracles; deployed driver proof remains
profile-specific. Windows access/elevation policy belongs to B4.

## Candidate priorities

1. **C001:** correct NotPermitted/NotOpen interpretation and preserve old raw error history; separately
   correct the latent numeric enum gaps. The static comparison found 37 mismatched named constants.
   Current exclusive-open literal remains correct, so this is not evidence that that branch misroutes.
2. **C002:** handle nullable manager/copied-device-set outputs before CFSet APIs. Original Apple
   implementation explicitly returns NULL when its device set is absent; the count API requires a valid set.
3. **C003/C004:** correct Nullable exception/property XML and the stale delegate-field instruction;
   retain explicit persistent-buffer requirements if the dormant array-registration seam is reused.
4. **C005:** distinguish ordinary late callback drain from partial listener startup with no worker;
   pre-generation allocation/start-catch diagnostic failures need an explicit later implementation owner.
5. **C006:** extend B1-C010's ambient-context policy decision to Mac owner/listener Threads, nonadditively.
6. **C007:** explain that direct IO descriptor metadata coexists with normalized fixed-64-byte input;
   feature expert GET separately transfers eight-byte storage and pads short success.
7. **C008/C009:** preserve current/historical native evidence distinctions and exact source/binary gap;
   independently review the CFNumberType parameter's 32-versus-64-bit declaration discrepancy.

Each group's detailed record names exact current code, actual assertions, missing proof, future editor
and re-review trigger. Candidate-associated unchanged paths are part of that review even when the editor
retains their current wording. Source-confirmed behavior is distinguished from desired policy; this
packet authorizes no runtime repair, changed assertion, API redesign or public documentation edit.

## Exact shared and secondary dispositions

| Original shared ID | Outcome |
| --- | --- |
| `core-001:unit:200ff2d981c3a51d` | Read-candidate C001: historical numeric failure remains; symbolic error name is wrong and permission-specific advice needs qualification |
| `core-001:unit:50f46182140d5a26` | Read-clean with historical-source qualification: architecture table carries exact `.8`/21498d24 machine evidence and direction-specific bounds; current stable package has its own source/runtime status |
| `core-001:unit:f4ee297b180bacbb` | Read-clean historical architecture: report completion, native ack/drain and native-source provenance are separate; current chosen backend is persistent dispatch, no fallback inferred |
| `core-001:unit:351c8a6fdb1c251e` | Read-clean historical proposal: old pumping line references and possible callback-report APIs describe the proposal at that stage, rather than current selected synchronous report workers |
| `core-001:unit:51fddcc95d7f7156` | Read-clean current Core lifetime summary, qualified by C008's exact producer/runtime evidence; channel initialization remains session/B5, not a native-open side effect |

Secondary parent `core-001:unit:9fe6da15d252bcd2` remains B1's platform-notes unit. Its macOS
non-seizing statement is supported by actual open argument, Apple API contract and owner route;
Windows clauses remain B4's responsibility. The whole parent is not newly counted as a B3 primary unit
or declared universally clean. Secondary parent `core-001:unit:7a72c68ab4742b48` remains B4's
LibraryImport instruction unit; C004 supplies the exact stale listener paragraph correction. Both
handoffs preserve parent identity/range/hash for central reconciliation.

## Actual test and external-host oracles

All assertions were read; none executed. Fake-native tests establish managed ordering and buffers:
held open/output/feature GET/SET/ack, exact registry token retention, pre-cancel no native submission,
late successful cancellation, same-worker order, independent result copy, staging zeroing and unknown
close retention. FIDO cancellation tests distinguish cancel-before-report, report-before-cancel and
old cancellation from a new reader. Disposal assertions check the returned public task, not only its
inner TCS. Overflow tests feed 161 reports to the 160-entry managed queue; they test that queue, not
the native ring. Dropped-wrapper tests observe a weak facade and fake acknowledgment, not universal
finalization or a dropped session's protocol cleanup. Several older held gates lack a full failure-path
finally drain; future stress execution should first receive an explicitly scoped harness review.

OTP recovery uses the real protocol over fake feature calls: two ten-report read-only frames, one abort
reset, one read-completion reset, sequence/command/CRC assertions and exactly 22 outgoing reports.
Its source proves no physical touch, firmware or HID driver behavior. Whole applet creation/handshake,
cache/firmware and presence semantics remain the later B5/B6 and nine boundary packets.

The standalone input host tests a synthetic nonshipping experiment library with different exports;
its blocked callback, buffer mutation, GC, late-owner isolation, overflow/removal/terminal order and
self-wait checks are useful oracles for that ABI. The route host targets actual production connections,
selected serial, read-only info, pending public read disposal/reopen, borrowed two-session reuse,
active cancel/touch/removal and real manager callback drain in separate modes. Its watchdog explicitly
classifies process kill as OS cleanup. A compiled AOT trait or host supplies no execution credit.

Historical source `21498d24`/NativeShims `.8` results on macOS 15.7.7 arm64, .NET 10.0.12, selected
serial 31683481/firmware 5.7.4 remain the retained upstream evidence. Earlier selected removal and
late-drain evidence remains separately bounded, including nonzero per-device close and the original
inconclusive no-open probe. Final `.8` normal/cancel/direct/listener modes do not turn fresh-host
permission, all fixtures or later stable 1.18.1 into qualified outcomes.

The integrated Linux capture compiled actual route/comparison projects with canonical stable 1.18.1
under the retained three-property launcher profile, cached sealed outputs and accepted
current-source/exact-retained-output scope. The historical comparison project's default remains
`.8`, with exact before/after commits. `--profile` is the current per-source mode, with honest missing
native-only/process-loaded-path/producer-build/firmware fields. These are separate modes and evidence
populations. The integration's 4,442 units and 197 overlapping reruns are inherited execution counts,
not fresh B3 tests or Mac support evidence.

## Complete typed ownership example for later compilation

This is a composable method with explicit typed prerequisites. It was checked by source inspection;
compiler shape remains **VALID-NOT-CHECKED**. It sends a caller-supplied CTAPHID command/payload and
leaves response interpretation to the caller; it is not a complete CTAP application ceremony.

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;

static async Task<ReadOnlyMemory<byte>> ExchangeAsync(
    IYubiKey device,
    byte ctapHidCommand,
    ReadOnlyMemory<byte> callerOwnedPayload,
    CancellationToken cancellationToken)
{
    await using IFidoHidConnection connection =
        await device.ConnectAsync<IFidoHidConnection>(cancellationToken);
    await using RawCtapHidSession session =
        await RawCtapHidSession.CreateAsync(connection, cancellationToken);

    // The session borrows connection; the method owns both enclosing lifetimes.
    return await session.SendAndReceiveAsync(
        ctapHidCommand, callerOwnedPayload, cancellationToken);
}
```

Keep payload memory valid/unchanged until this actual task is terminal and clear caller-owned sensitive
storage afterward, including failure. An outer `WaitAsync` cancellation ends a different wait. The
returned response escapes the method and retains its separate caller-owned handling requirements.
Session cleanup precedes connection cleanup. A caller-owned prompt is likewise retained/borrowed by
its session, rather than a disposable native lifetime root. C/B5 own the prompt operation contract.

## Inherited and cross-packet questions

No B3-specific primary inherited group is allocated in `inherited-packet-inputs.json`. Preserve
`fixes-wave-001:CORE-001` and `audit-v2-20261022-20261031:multi-key-exclusions` as dependencies:
the old serial 9681620 Linux HID failure is neither reproduced nor explained by Mac source/fake tests.
A's metadata TLV alias, metadata-cache PID qualification and successful serial-less identity caching
remain A's findings; native owner transfer neither fixes those caches nor changes their source IDs.
B1-C005 now has a completed Mac route disposition, while B1-C010 receives the Mac ambient-context
extension. No finding aliases are additive. Windows late work remains uninspected and awaits the
user's ready handoff. PIV physical/touch/hotplug work remains deferred.

Fresh independent review should prioritize C001/C002/C005/C009 and consequential Apple/source
passages before editing. Coordinator sampling should choose actual eligible clean IDs across
interop, callback/worker, assertion and external-host strata from the ledger, excluding any later
edited overlaps. See verification.md for concrete nominees and unexecuted bounded checks.
