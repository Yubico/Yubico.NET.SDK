"""Serialize manual review decisions. Hash equality never creates a read verdict."""
import collections
import hashlib
import json
from pathlib import Path
import subprocess

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(parent for parent in HERE.parents if (parent / ".git").exists())
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def dump(name, data):
    (HERE / name).write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n")


# Explicit complete-file reads. Values record actual implementation/assertion
# context; entries are never generated from file names or structural identity.
NOTES = {
    0: "Watchdog censors killed calls; separate invocation/task/lifecycle timing; GetInfo checks status only; dataset hashes configured files, not process-loaded shim or producer.",
    1: "Seven synthetic outcomes plus one dataset assertion; pending task retains null terminal time; temporary artifact deleted in finally; no hardware self-test.",
    2: "BDN setup chooses first matching transport without serial/firmware assertion; monitor replay counts through SnapshotComplete; measured connection/session scopes dispose asynchronously.",
    3: "net10 executable, nonpackable; BoundaryBaselineRoot selects project references independently of runtime source-label environment variable; compiler binding unrun.",
    4: "BadResponse type has body-only comment; PlatformInteropException message/inner exception overloads supply no universal native wrapper; actual Windows failures use other exception types.",
    5: "Eleven wrapper imports retain SafeHandles; reader groups/results use ASCII unlike Connect UTF8; transmit pins spans through native return; published shim forwards PCI layout directly.",
    6: "Pointer-width owned card SafeHandle; fallback disposition LEAVE_CARD; ReleaseHandle compares native status; B1 live owner performs checked UNPOWER/disconnect before invalidation.",
    7: "Pointer-width owned context; successful Dispose flag alone supplies no actual deferred ReleaseHandle proof; obsolete CER annotation remains commented history.",
    8: "Disposition 0..3 requests leave/reset/unpower/eject; enum names describe native requests, not observed physical application/PIN reset; B1-C001 qualification retained.",
    9: "Complete numeric/comment table read; BROKEN_PIPE differs from Win32 decimal109; Windows unsupported-feature code differs from pcsc-lite mapping; preserve raw logs.",
    10: "Complete error-string switch read; constructor stores low32 HResult from long, symbolic matching uses positive constants; unknown input numeric fallback; backend/sign extension need policy.",
    11: "Managed PCI has two four-byte fields and self-computed size; published shim forwards pointer without LP64 conversion; Windows/non-Windows widths stay separate.",
    12: "T0/T1/Tx are ordinary built-in paths; Raw=0x10000 differs from inspected pcsc-lite4; unused raw constant is no demonstrated transmit failure.",
    13: "Mask low16 states/high16 sequence independently; UTF8 reader decode contrasts with Windows ANSI allocator; ATR copies native count from fixed36 without explicit cap.",
    14: "Pack1 wrapper state has two pointers/three uint32/fixed36; materialize storage before names; partial failure frees previous pointers; one owning array clears pointers exactly once.",
    15: "User/terminal/system describes resource-manager database scope, distinct from physical card access or application authentication; B1 operation authority retained.",
    16: "Exclusive/shared/direct requests concern resource-manager access; direct-reader conditions remain native/backend-specific rather than HID/PCSC isolation.",
    17: "All state comments read: UNAWARE immediate return is consequential to B1 cadence; acknowledgement removes CHANGED; state bits supply no physical security-reset proof.",
    18: "Status0..6 describes card position/power/protocol negotiation, distinct from SCARD_STATE flags; no fresh native status or applet state assertion.",
    19: "Extensionless runtime import; Net47 counterpart/Cleanup description predates tree; intended native layout is separate from selected installed asset.",
    20: "All platform imports read; LoadLibraryEx lacks W EntryPoint/captured-last-error; System32 applies to imported kernel32, not target flags; dl flags need platform context.",
    21: "Public PlatformApiException supports ordinary/source-int-message construction; X8 preserves raw32 pattern in text; no typed code/source properties or universal enum normalization.",
    22: "Owned library SafeHandles call unload and compare status; deferred native references and returned function delegate lifetime are separate from managed Dispose.",
    23: "Unknown/Windows/MacOS/Linux is host selection only; enum is no ABI-width/device qualification table.",
    24: "Encoding/CharSize choose UTF16 Windows/UTF8 Unix; DwordSize unconditionally Linux8; elevation queries current Windows token role; exact identity-lifetime review remains support continuation.",
    25: "Factory dispatches loaders; GetFunction/TryGetFunction yield delegates; library must remain loaded through invocation; obsolete fixed cleanup instruction.",
    26: "All DEVPKEY values/type annotations read with usages; exact original GUID/PID comparison pending beyond focused cfgmgr32 ABI sources.",
    27: "Complete constants/layout/imports read; 416 filter/event prefix differ from parameter ABI; LPGUID by-value/PULONG IntPtr require architecture-specific disposition.",
    28: "Complete public class-GUID catalog read; exact original per-GUID definitions pending; constants enter no clean sample denominator.",
    29: "Constructors resolve extra class/PDO properties before ContainerId return; paths list UTF16 two-call without growth retry; property NUL/stale netstandard rationale; topology fallback A-owned.",
    30: "Public property selector includes device/hardware/location/PDO/parent/children/container; CmDevice's LocationInfo->LocationPaths mapping read; focused public helper docs pending.",
    31: "Four active interface GUIDs plus commented reference catalog read; original constant reconciliation pending; inactive catalog supplies no executed API coverage.",
    32: "Probe demands BUFFER_SMALL; allocation uses IntPtr; second type ignored; string includes terminator; fixed-size checks only Debug.Assert; growth policy unselected.",
    33: "All seven HidD imports use BOOL despite native BOOLEAN; caps signed16 versus USHORT; caller frees preparsed pointer in finally; Kernel32 BOOL distinction retained.",
    34: "Internal report-handle seam exposes synchronous open/feature/IO and native lengths; no cancellation, async drain or release-proof result.",
    35: "Unauthorized-only metadata fallback; split feature/IO access and report ID zero; nested buffers unzeroed; synchronous native handle borrow yet no claim/release observation in Dispose.",
    36: "Explicit Cfgmgr32/Hid/Kernel32/WinSCard DLL names, with direct System32 import restrictions distinct from extensionless package binding.",
    37: "Complete access/share/creation/file flags and ReadFile/WriteFile signatures read; owner chooses NORMAL/null OVERLAPPED; BOOL and pointer-width handle match file API family.",
    38: "Direct WinSCardW reader-instance query uses context handle, UTF16 name/char output and32-bit count; caller establishes/scopes separate topology context; no connection-context sharing.",
    39: "Zero LoadLibraryEx flags; GetProcAddress then W suffix fallback for function lookup; LoadLibraryEx binding/captured error is a separate earlier stage concern.",
    42: "Public test-only ServiceLocator retains global provider without disposal, forwards GetRequiredService; no source caller found; no production Core DI composition implied.",
    43: "Task.Run separates invocation/native enumeration; token prevents queued dispatch but leaves started call pending; Windows CM metadata filtering versus unavailable-udev empty Linux result.",
    44: "Vendor0x1050 participates in common/Windows filters, distinct from usage/type classification or physical serial identity.",
    45: "Descriptor record has unsigned usages/signed16 VID/PID bit patterns; path is interface identity, not physical proof; ToString is diagnostic metadata.",
    46: "Added/Removed/Unknown classify internal native hints; no physical arrival/removal event is promised by enum alone.",
    47: "Hint-only callback/volatile diagnostic status; platform factory/explicit Dispose; baseline/restart/drain prose needs Windows failure and B3 timeout conditions.",
    48: "Immutable hint optional ID/path and shared Unknown; repository-rescan wording predates owned finder/feed split; A dependency rather than a new runtime finding.",
    49: "Classify uses usage pair only, not VID; callers perform vendor filter; FIDO->IO and keyboard->feature mapping; unknown report kind throws; exact USB/FIDO authority remains continuation.",
    50: "Public interface kind documents FIDO64/OTP8 typed protocols and usage pairs; direct IHidConnection lengths remain platform-specific; original usage-source check pending.",
    51: "Short item3 maps to four bytes; little-endian uint accumulation; complete long item skipped, truncated header/payload stops; zero-size advances; internal cursor supplied by platform parser.",
    52: "Report-kind enum says typical64/8, not universal direct-buffer length; original HID specification check retained as continuation.",
    53: "Detailed borrowed SetReport lifetime and caller handling of escaped GetReport result are accurate; provider-specific lengths/sharing/timeouts avoid universal assumptions.",
    54: "Direct factory methods omit caller disposal/synchronous wait/ungrouped claim; underlying Windows constructors wait inline and return owned report facade.",
    55: "Typed8 raw contract leaves wire sequencing/CRC/recovery to caller; Mac-specific native-dispatch remarks do not grant Windows cancellation/drain semantics; B5 wire owner retained.",
    66: "Weak GCHandle/static callback/instance delegate field; success unregister waits callbacks; ignored failure frees state; Start after disposed can reacquire; callback exception boundary incomplete.",
    67: "Constructor transfers report owner, catches open failure and disposes; feature native size minus report ID; synchronous GET/SET and DisposeAsync forwarding; no admission/drain policy.",
    68: "IO counterpart owns report seam/catches open failure; separate input/output size minusID; GET/SET inline and DisposeAsync synchronous; handle protection is not registry release proof.",
    69: "CM metadata enumeration reads usage/VID/PID, filters supported Yubico pairs, skips per-interface CM/unsupported errors; list failure propagates; opens create report facade only on demand.",
    70: "Changed is payloadless rescan hint; callback-stop prohibition justified by native drain; Start declares ObjectDisposedException absent from Windows implementation.",
    71: "One-shot integration base has completed no-op async disposal; tracking tests separately own monitor; no hidden cleanup/native test execution.",
    72: "Type selection and Started-or-Error; quiet zero-event can succeed without registration; timed Dispose uses WhenAny without awaiting result; actual native APIs despite integration metadata.",
    73: "Enumerates all attached interfaces/keys and requires nonempty HID; no serial authorization/each-device assertion; Windows output mixes enumeration with elevation advice; logger/finder cleanup scope limited.",
    90: "416 filter layout oracle runs any host; Windows-only actual register asserts Started/Stopped with finally Stop; no signature width, failure/finalizer or callback-drain oracle.",
    92: "Precancel zero calls, held invocation returns pending, midcancel remains pending, one failure instance/no replay; finally releases and actually drains worker before gates dispose.",
    93: "Platform lifecycle tests accept Error, use real native APIs; timed cases compare WhenAny without awaiting disposal result, so fast faults can pass; no finalizer/unregister-failure vector.",
    94: "Exact usage pair classification, support truth table and report type; helper's VID fixed so tests do not establish vendor rejection or OS descriptor/native behavior.",
    95: "Open failure disposes fake owner; success retains owner and feature payload size; no SafeHandle/native release assertion; successful fake connection not scoped for disposal.",
    96: "Six parser vectors, offset24/UTF16, missing/early NUL, empty/short/null; sentinel after declared payload detects overread; unmanaged buffer always freed in finally.",
    97: "Ten rooted module entry types/default discovery; monitor three same-stream sinks, sampled live sequence equality, at-least-one rapid add/remove; cancellation/drain scopes; historical execution remains separate.",
    98: "Ten project references and AOT/globalization opt-in; external nonpackable host; filename-exclusion comment stale; actual publish/link participation unrun.",
    99: "Parent compatibility host explicitly compiles only Program to exclude nested generated consumer code; nonpackable/nonlibrary; no product reference in export checker.",
    100: "Macarm64 hash-pinned old/async8 export census; all36 names and five input names with absence old; only BNnew/free actually invoked; finally NativeLibrary.Free.",
    101: "Full historical compatibility matrix read; source/binary distinction, four SCard calls/exclusions, negative control never observes fallback ReleaseHandle; limits explicitly exclude Windows/Linux.",
    102: "Fresh-process resolver supplies selected native handle; actual reflected crypto span delegates/known answers; four SCard context/error calls, valid-handle suppression only after success; no HID/transmit.",
    103: "Binary HintPath CoreAssemblyPath rather than product ProjectReference; separate nonpackable executable; retained assembly/output/source identity need exact matching.",
    104: "36-name canonical old native export set, includes more than historical managed imports; names supply no signature/native-lifetime acceptance.",
    105: "Same36 negative fixture replaces Transmit with MissingFixture; cardinality stays36 so missing-symbol assertion rather than invalid fixture shape must fail.",
}

# Original-authority or additional lifetime context still needed after the
# complete file read; these entries deliberately receive no clean verdict.
PENDING = {
    15: "Recheck platform scope semantics against B1's original API passages.",
    16: "Bind direct mode explanation to each resource manager; retain Windows-only scope where applicable.",
    17: "Finish exact state-comment original-API comparison, including obsolete reader tracking flags.",
    18: "Bind numeric status semantics to each platform header before accepting portable carrier claims.",
    24: "Resolve WindowsIdentity ownership and remaining public helper contracts in support continuation.",
    26: "Original DEVPKEY GUID/PID/type reconciliation pending.",
    28: "Original class-GUID constant reconciliation pending.",
    30: "Public selector and LocationInfo alias need complete source/API disposition.",
    31: "Original interface-GUID constant reconciliation pending.",
    45: "Original HID Usage Tables/profile claims still require exact source passage.",
    49: "Original USB/FIDO usage mapping authority pending.",
    50: "Original usage/profile conditions pending.",
    51: "Original HID 1.11 short/long-item passage and descriptor test dependency pending.",
    52: "Original HID report/profile wording pending.",
}


def family(index):
    for label, indices in {
        "benchmarks": range(0, 4),
        "desktop-pcsc-native": range(5, 19),
        "native-loading-and-platform-support": range(19, 26),
        "windows-native": range(26, 40),
        "public-api-and-support": [4, 40, 41, 42],
        "portable-hid": [*range(43, 56), 70],
        "keyboard-translators": range(56, 66),
        "windows-hid-facades": range(66, 70),
        "integration-oracles": range(71, 74),
        "audit-child-hosts": range(74, 76),
        "boundary-inventory-and-contract-oracles": range(76, 90),
        "windows-native-registration-test": [90],
        "resilience-static-oracle": [91],
        "hid-unit-oracles": range(92, 97),
        "external-verification-hosts": range(97, 106),
    }.items():
        if index in indices:
            return label
    raise ValueError(index)


# Reviewed exact candidate spans, preserving overlapping facets on one unit.
SPANS = {
    "CORE-B4-C001": [(35, 150, 188), (35, 274, 318), (34, 17, 32), (67, 30, 83), (68, 30, 83)],
    "CORE-B4-C002": [(66, 59, 178), (66, 269, 315), (47, 24, 84), (70, 31, 38), (72, 107, 166), (93, 231, 280)],
    "CORE-B4-C003": [(27, 203, 363), (32, 24, 100), (29, 197, 231), (33, 32, 124)],
    "CORE-B4-C004": [(35, 50, 109), (35, 214, 240), (73, 51, 57)],
    "CORE-B4-C005": [(19, 15, 49), (20, 40, 59), (25, 34, 64), (24, 37, 41), (39, 19, 58)],
    "CORE-B4-C006": [(5, 73, 114), (5, 130, 166), (11, 19, 47), (13, 31, 46), (14, 38, 68)],
    "CORE-B4-C007": [(35, 111, 188)],
    "CORE-B4-C008": [(i, 1, 10000) for i in [*range(0, 4), *range(97, 106)]],
    "CORE-B4-C009": [(9, 22, 27), (9, 235, 258), (10, 31, 35), (10, 42, 189), (12, 40, 47)],
    "CORE-B4-C010": [(4, 17, 29), (21, 19, 39), (42, 19, 31), (43, 25, 49), (43, 98, 100), (54, 34, 43)],
}


if __name__ == "__main__":
    packet_path = RUN / "inventory/packets/B4-windows-support.json"
    packet = json.loads(packet_path.read_text())
    units = json.loads((RUN / "inventory/semantic-units.json").read_text())
    selected = set(packet["unit_ids"] + packet["shared_read_unit_ids"])
    by_id = {u["id"]: u for u in units}
    assert len(selected) == 2350 and len(by_id.keys() & selected) == 2350
    assert len(packet["paths"]) == 106
    paths = packet["paths"]
    indices = {path: i for i, path in enumerate(paths)}
    read_paths = {paths[i]: note for i, note in NOTES.items()}
    pending_paths = {paths[i]: reason for i, reason in PENDING.items()}
    shared_notes = {
        ".github/workflows/build.yml": "Ubuntu unit/resilience/build/pack workflow; CI source config is no executed result. Test/toolchain/package implementation ranges remain continuation.",
        ".github/workflows/native-aot.yml": "Three RID publish/run matrix requires ten type labels and empty discovery; Linux adds native-only capture. Actual hosted run/source binding and full toolchain gate review pending.",
        "Directory.Build.props": "Warnings/analyzers/language/net10 defaults and readme/source-link metadata read; CVE claim and exact SDK native/AOT support originals remain pending.",
        "Directory.Build.targets": "Late SDK opt-in sets AOT/reference metadata, common analyzer packages, two API AdditionalFiles and friend assemblies; exact compiler participation and contract-QA toolchain context pending.",
        "Directory.Packages.props": "Explicit central versions: NativeShims1.18.1, xUnit3.0.0, BDN0.15.8, .NET10 dependencies; declarations read without restore or package execution.",
        "src/Core/src/Yubico.YubiKit.Core.csproj": "Explicit library opt-in, unsafe and NativeShims package, ownership-related friend assemblies; TargetFrameworks comment predates singular shared TargetFramework; real compiler inputs pending.",
        "docs/NATIVE-AOT.md": "All nine assigned sections read with host/package context; historical native/platform claims remain source/artifact-qualified; Linux owned section remains B2 secondary context.",
    }
    callback_id = "core-001:unit:7a72c68ab4742b48"
    shared_pending = {".github/workflows/build.yml", ".github/workflows/native-aot.yml", "Directory.Build.props", "Directory.Build.targets"}
    identity_rows = []
    data_by_path = {}
    failures = []
    for path in sorted({by_id[uid]["path"] for uid in selected}):
        data = (ROOT / path).read_bytes()
        data_by_path[path] = data
        git = subprocess.run(["git", "show", f"{PIN}:{path}"], cwd=ROOT, capture_output=True)
        pinned = git.stdout if git.returncode == 0 else None
        normalized_equal = pinned is not None and data.replace(b"\r\n", b"\n") == pinned.replace(b"\r\n", b"\n")
        expected = {by_id[uid]["source_sha256"] for uid in selected if by_id[uid]["path"] == path}
        raw_match = sha(data) in expected and len(expected) == 1
        identity_rows.append({"path": path, "current_raw_sha256": sha(data), "inventory_source_sha256": sorted(expected), "inventory_match": raw_match,
                              "pinned_git_blob": subprocess.check_output(["git", "hash-object", "--stdin"], cwd=ROOT, input=pinned).decode().strip() if pinned is not None else None,
                              "pinned_content_sha256": sha(pinned) if pinned is not None else None,
                              "lf_equal_to_pin": normalized_equal, "semantic_evidence": "Recorded separately; identity is not a read disposition."})
        if not raw_match:
            failures.append({"path": path, "failure": "inventory-source-hash"})
        if pinned is not None and not normalized_equal:
            failures.append({"path": path, "failure": "pinned-source-drift"})
    results = []
    for uid in packet["unit_ids"] + packet["shared_read_unit_ids"]:
        u = by_id[uid]
        path = u["path"]
        primary = uid in packet["unit_ids"]
        text = data_by_path[path].decode("utf-8-sig", errors="replace")
        actual_range = sha(text[u["start_character"]:u["end_character_exclusive"]].encode())
        if actual_range != u["range_sha256"]:
            failures.append({"id": uid, "failure": "inventory-range-hash"})
        candidates = []
        if primary:
            i = indices[path]
            read = path in read_paths
            reason = read_paths.get(path, "Continuation: complete source/assertion/context read remains pending; no verdict inferred from identity checks.")
            if read:
                for cid, spans in SPANS.items():
                    if any(i == index and u["first_line"] <= end and u["last_line"] >= start for index, start, end in spans):
                        candidates.append(cid)
            pending = pending_paths.get(path)
            group = family(i)
        else:
            read = path in shared_notes or uid == callback_id
            reason = shared_notes.get(path, "Continuation: assigned shared range still needs complete implementation/caller/original-source context.")
            pending = "Finish exact implementation/original-claim dependencies." if path in shared_pending else None
            group = "assigned-shared"
            if uid == callback_id:
                reason = "Complete interop instruction read; exact stale Mac listener paragraph is B3-C004 secondary handoff; BOOL/SafeHandle blanket rules need native-specific conditions."
                candidates = ["CORE-B4-C003"]
            if path in {"docs/NATIVE-AOT.md", "src/Core/src/Yubico.YubiKit.Core.csproj"}:
                candidates = ["CORE-B4-C008"]
        status = "UNREAD" if not read else "READ-PENDING-CONTEXT" if pending else "READ-CANDIDATE" if candidates else "READ-CLEAN"
        results.append({"id": uid, "ownership": "primary" if primary else "assigned-shared", "path": path,
                        "first_line": u["first_line"], "last_line": u["last_line"], "label": u["label"],
                        "start_character": u["start_character"], "end_character_exclusive": u["end_character_exclusive"],
                        "source_sha256": u["source_sha256"], "range_sha256": u["range_sha256"], "actual_range_sha256": actual_range,
                        "family": group, "complete_context_read": read, "status": status, "reason": reason,
                        "pending_context": pending, "candidate_ids": candidates,
                        "documentation_status": "CANDIDATE-TRIAGE-PENDING" if candidates else "SOURCE-READ" if status == "READ-CLEAN" else "CONTINUATION-REQUIRED",
                        "implementation_status": "NOT-ACCEPTED-BY-THIS-AUDIT", "review_status": "INDEPENDENT-REVIEW-PENDING",
                        "validation_status": "VALID-NOT-CHECKED", "platform_execution": "UNRUN",
                        "sample_eligible": status == "READ-CLEAN"})
    dump("unit-dispositions.json", results)
    dump("file-identities.json", identity_rows)
    counts = collections.Counter(row["status"] for row in results)
    family_rows = []
    for name in dict.fromkeys(family(i) for i in range(106)):
        iset = [i for i in range(106) if family(i) == name]
        family_rows.append({"family": name, "files": len(iset), "lines": sum(len(data_by_path[paths[i]].splitlines()) for i in iset),
                            "complete_file_reads": sum(i in NOTES for i in iset), "unread_paths": [paths[i] for i in iset if i not in NOTES]})
    summary = {"status": "CONTINUATION-REQUIRED", "source_pin": PIN,
               "observed_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
               "packet_sha256": sha(packet_path.read_bytes()), "primary_files": 106, "primary_lines": 14842,
               "complete_primary_file_reads": len(NOTES), "unread_primary_files": 106 - len(NOTES),
               "primary_unit_count": len(packet["unit_ids"]), "shared_unit_count": len(packet["shared_read_unit_ids"]),
               "primary_status_counts": dict(collections.Counter(r["status"] for r in results if r["ownership"] == "primary")),
               "shared_status_counts": dict(collections.Counter(r["status"] for r in results if r["ownership"] == "assigned-shared")),
               "combined_status_counts": dict(counts), "families": family_rows,
               "sdk_builds_tests_native_device_formatter_executions": 0, "identity_failures": failures,
               "note": "Complete reads with pending context remain outside the clean denominator. Instruction/orientation reading alone does not disposition shared units."}
    dump("coverage-summary.json", summary)
    remaining = [r for r in results if r["status"] in {"UNREAD", "READ-PENDING-CONTEXT"}]
    dump("continuation.json", {"status": "REQUIRED", "source_pin": PIN,
                              "next_candidate_number": "CORE-B4-C011", "remaining_unit_ids": [r["id"] for r in remaining],
                              "remaining": [{k: r[k] for k in ["id", "path", "ownership", "first_line", "last_line", "label", "status", "pending_context", "source_sha256", "range_sha256"]} for r in remaining],
                              "late_windows_delta": "Separate ready handoff only; continue the pinned packet first; never read unfinished branches/worktrees."})
    index = []
    for cid in SPANS:
        affected = [r for r in results if cid in r["candidate_ids"]]
        index.append({"id": cid, "status": "LOCAL-CANDIDATE", "unit_ids": [r["id"] for r in affected],
                      "unchanged_primary_or_shared_paths": sorted({r["path"] for r in affected}),
                      "additional_dependencies": "See candidates.md; narrow dependencies retain their packet's primary disposition.",
                      "final_disposition_recommendation": "Future editor/reviewer explicitly retain or correct each candidate-associated unchanged path; selected implementation is Pass2."})
    dump("candidate-index.json", index)
    evidence = ["# B4 file evidence — checkpoint 1", "", "Each row below follows a complete source/assertion read. Pending context remains explicit; the other 29 primary files retain UNREAD units.", "", "| Path | Read context | Remaining original/context check |", "| --- | --- | --- |"]
    for i, note in sorted(NOTES.items()):
        evidence.append(f"| `{paths[i]}` | {note} | {PENDING.get(i, 'Candidate and qualification limits are in candidates.md; independent review/compilation/execution remain pending.')} |")
    evidence += ["", "## Shared ranges", "", "Only the exact shared unit IDs marked read in unit-dispositions.json are dispositioned. Full instruction reads are orientation unless the semantic ledger explicitly says otherwise.", ""]
    for path, note in shared_notes.items():
        evidence.append(f"- `{path}`: {note}")
    evidence.append(f"- `{callback_id}`: LibraryImport instruction and B3-C004's exact stale paragraph handoff; preserve sole B4 primary range ownership.")
    (HERE / "file-evidence.md").write_text("\n".join(evidence) + "\n")
    protected = [RUN / "user-work-baseline.json", RUN / "baseline.json", RUN / "inventory/semantic-units.json", RUN / "inventory/shared-sections.json"]
    dump("preservation-verification.json", {"source_pin": PIN, "selected_ids_exact": selected == {r["id"] for r in results},
                                           "duplicate_disposition_ids": len(results) != len({r["id"] for r in results}),
                                           "identity_failures": failures, "protected_input_hashes": {str(p.relative_to(ROOT)): sha(p.read_bytes()) for p in protected},
                                           "executed": "Python/Git reads, HTTP source capture and B4-only record writes; zero SDK/native/device executions.",
                                           "claims": "Structural checks verify record/source identities, never semantic completeness."})
    print(json.dumps({k: summary[k] for k in ["status", "complete_primary_file_reads", "unread_primary_files", "primary_status_counts", "shared_status_counts", "identity_failures"]}, indent=2))
    if failures:
        raise SystemExit(1)
