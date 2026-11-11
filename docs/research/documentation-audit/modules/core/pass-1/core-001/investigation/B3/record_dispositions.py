"""Serialize completed human semantic reads; hashes are identity checks, not read evidence."""
import collections
import hashlib
import json
from pathlib import Path
import re
import subprocess
import zipfile

HERE = Path(__file__).parent
RUN = HERE.parent.parent
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"
PACKET = json.loads((RUN / "inventory/packets/B3-macos-lifetimes.json").read_text())
UNITS = json.loads((RUN / "inventory/semantic-units.json").read_text())
NOTES = [
    ("CoreFoundation.Interop.cs", "CF pointers, count-sized pointer-array output, 64-bit CFIndex/type IDs and U1 Boolean; synchronous call buffers remain borrowed. CFNumberType parameter width is C009; null-set caller is C002."),
    ("HidInput.Interop.cs", "Five macOS-only shim imports; size_t/nuint, pointer context, Cdecl static callbacks, int status, uint timeout. Result0/4 quiescence differs from Destroy0 release; owner.h source read independently (C008)."),
    ("IOKitFramework.Interop.cs", "Mach handles use32-bit bit patterns, registry ID64 and pointer dictionaries; matching-service consumes dictionary, service release separate. Sequential IOReturn enum loses reserved gaps (C001)."),
    ("IOKitHid.Interop.cs", "Manager/device references, create/copy ownership, report type32 and CFIndex report ID/length; synchronous report behavior supports worker separation. Old unused callback array API needs persistent storage if revived (C004)."),
    ("IOKitHidConstants.cs", "Metadata keys identify integer CF properties; report type0/1/2 selects input/output/feature. They do not encode physical device identity or permission."),
    ("Interop.Libraries.cs", "Absolute Apple framework paths for CF/IOKit/PCSC. Path constants supply neither successful loading nor platform execution."),
    ("MacOSUnmanagedDynamicLibrary.cs", "dlopen Lazy returns SafeLibraryHandle; invalid handle throws; dlsym null yields false and null delegate. Successful function delegates depend on library owner lifetime; no native load performed. Error text is a literal resource key, a diagnostics-quality follow-up for B4 rather than a release/lifetime proof."),
    ("CoreFoundationString.cs", "Builds independent null-terminated UTF-8 bytes, explicit0x08000100; CF creation copies input. Null result becomes PlatformApiException; each successful reference belongs to caller."),
    ("IIOKitDeviceLifetime.cs", "CreateDevice consumes matching dictionary via service lookup, finally releases service, transfers CF device reference. Non-seizing0 open; checked close bool. ExclusiveAccess still means open in original Apple implementation. Borrowed native property references are not released. C004 callback seam; unsupported cross-SDK citation retained as gap."),
    ("IOKitHelpers.cs", "Property key string released in finally, GetProperty result borrowed; CF type checked, SInt32 value copied, false conversion returns0. Absent nullable.Value exception differs from XML (C003); enum carrier width C009."),
    ("MacOSFidoHidConnection.cs", "Root precedes acquisition; one worker/admitted ordinary call, async open/send, persistent input callback owner. Copies reports twice across native/managed boundaries; zeroes staging. Read cancellation identity-detaches only reader; public-read markers drain registration-disposal wrappers before shutdown returns. Checked Destroy then creation-reference release then original claim/root. Unknown close quarantines without retry. Context capture C006; source/runtime versions C008."),
    ("MacOSHidDeviceListener.cs", "Rooted per-generation static callback context; stop sets flag, wakes retained loop, shares monotonic8s budget. Callback exit precedes same-thread close/unschedule/release. Timeout refuses restart until actual cleanup; exceptions retain. Manager nonzero return differs from throw by original source. Initial duplicate additions suppressed; unknown removals still hint. Partial startup C005, context C006, corrected instruction C004."),
    ("MacOSHidFeatureReportConnection.cs", "Sync expert facade over OTP owner: metadata native sizes, SET caller length, GET original whole8-byte array with short success padding. Caller blocks; async disposal shares owner; no grouped claim at direct open. C007 nearby input qualification."),
    ("MacOSHidIOReportConnection.cs", "Sync expert facade over FIDO owner, six-second read detaches waiter and maps timeout to PlatformApiException. Transfers whole queued owned array, arbitrary SET length; partial-open unrecovered exceptions preserved. Native metadata and actual fixed64 input differ (C007)."),
    ("MacOSHidInterface.cs", "Entry ID plus descriptor classification; enumerates match-all then vendor/type filter; copied-set finally and manager finally own separate references. GetList null outputs unchecked (C002). Direct factories are synchronous and unregistered; GetEntryId checks status and preserves64-bit bits."),
    ("MacOSOtpHidConnection.cs", "Separate rooted one-worker OTP owner; copies/clears SET, GET allocates8, clears error/short output and transfers successful array. Pre-dispatch token prevents native call; cancellation after dispatch does not detach. Exclusive failed open checked-closes; false/throw release retains claim/root. Open failure aggregates cleanup failure. C006 context and C008 source qualification."),
    ("CoreFoundationStringTests.cs", "Asserts exact UTF-8 plus terminator and encoding; null fake result throws. No CF runtime or native allocation oracle."),
    ("MacOSHidConnectionLifetimeTests.cs", "Feature constructor fake status-1 releases CF object without Close; successful Dispose records Close and Release. ThrowOnOpen is actually translated by fake OpenDeviceResult; assertions qualify fake semantics, not arbitrary thrown native open."),
    ("MacOSHidFeatureCompatibilityTests.cs", "Expert metadata17/23, SET5, GET8 and one worker identity; held SET blocks two disposals until gate; GET-99 hex preserved; false close no Release and one Close. Finally releases/drains sender before teardown."),
    ("MacOSHidFeatureFacadeTests.cs", "Expert metadata65/72 and SET72 versus typed8 enforcement; held GET/SET drain, one release/worker; mutated error buffers cleared. Short0/3/8 GET returns same padded8 array;9 fails/clears. Exclusive partial close and unrecovered failure asserted."),
    ("MacOSHidFidoReadCancellationTests.cs", "Order-specific cancel/report/old-reader identity tests keep input running; Cancel count0 before dispose. Held ack delays release. Sync and async disposal assert actual public read terminal before returning; fake supplies ack, not OS cancellation."),
    ("MacOSHidFidoRouteLifetimeTests.cs", "Slot claims before held create; ApplicationSession waits fake INIT; keepalive cancel once and same channel reuse. Refuses send/read overlap; overflow terminal, separate owner isolation; isolated CloseFault retains registry. Partial/exclusive/pre-cancel cases; held output copy/zero; publication race; callback exceptions contained. Dropped wrapper weak reference + held ack observes managed finalization only. Several held gates lack finally drain on assertion failure; future harness work must address that before stress execution."),
    ("MacOSHidIOCompatibilityTests.cs", "Real6s managed timeout then late fake report retry; pending disposal ack gate; error inner type and native status; descriptor sizes/arbitrary SET; reflection proves original queued array transfer. Typed64 remains. Unproven partial close preserved; no Mac library loaded by fakes."),
    ("MacOSHidIOFacadeTests.cs", "Asserts metadata65/72, SET72/empty, strip leading zero report ID from65 to64; fake failed open releases without Start; double disposal releases once. C007 is explicit current oracle, not proposed behavior."),
    ("MacOSHidListenerLifetimeTests.cs", "Held callback timeout once/no early cleanup/no restart, late drain order and restart; self-stop; failed unschedule/release quarantines; concurrent Stop/Dispose shares deadline; nonzero manager results continue versus thrown calls quarantine. Fakes use static native callback pointer. No allocation/registration/start fault vectors (C005)."),
    ("MacOSOtpRecoveryTests.cs", "Real OtpHidProtocol over fake feature seam: ten accepted frame reports, gated touch status, cancellation dummy reset, next exchange and CRC-correlated response. Exactly22 sends including two resets and frame command/sequence/CRC bytes. No physical touch or native abort claim."),
    ("MacOSOtpRouteTests.cs", "Discovery100ms abandoned GET retains claim until late cleanup/retry. One worker open/send/get/close order; held calls refuse overlap; pre-cancel submits nothing, mid-call may succeed; failed/short/throw GET clears array. Exclusive partial close; release errors aggregated and claims quarantined. Actual fake worker lifecycle, no hardware."),
    ("CurrentProfile.cs", "Fresh child2 warmup+10 normal+1idle samples with failure/censor counts. Measures invocation vs terminal, session+connection disposal, reopen; process allocation/CPU distinct idle/close scopes. Native-only/pending count/loaded path/firmware/build correspondence explicitly unavailable. Deployed Core/package hash checks and native version equality; not historical comparison semantics."),
    ("Directory.Build.props", "Distinct comparison variant intermediate/output roots imported before project; default local remains. Property routing and cached canonical package belong to retained integration profile; no restore or build here."),
    ("MacOSHidBoundaryComparison.csproj", "Historical default async.8 override remains; selected Core HintPath + after-publish copy; excludes obj; runtime profile may explicitly select stable package. Project metadata is not evidence that default legacy check accepts current stable1.18.1."),
    ("Program.cs", "Historical comparison requires async.8/package hash/exact before65964966 and after21498d24; two fresh-child warmups,10 lifecycles, no-input censor. Phase classification distinguishes prefix block/pending/fault and removes unsafe error text. Pair check validates shared native/runtime/OS/fixture and distinct Core, permits partial descriptive summaries. --profile separately dispatches current source profile."),
    ("RunnerSelfTest.cs", "Static source oracle tests exact historical commit admission, child classification and typed-error sanitization, optional old dataset pairs. Historical datasets absent prints not run; package async.8 requirement remains. No self-test was executed."),
    ("MacOSHidInputVerification.csproj", "Nonshipping synthetic experiment dylib supplied by explicit path; net10 unsafe/AOT metadata. It references no shipping Core package or device host."),
    ("Program.cs", "Synthetic C ABI host pins GCHandle context; callback copies borrowed bytes and contains exceptions, self-wait rejected. Probes inject source mutation/GC, blocked callback+cancel/ack, owner isolation, overflow drain/terminal, removal, malformed input. End frees root only after DestroyOK. Parent watchdog kills child but supplies no cooperative release proof. Standalone experiment entry points differ from shipping exports."),
    ("AcceptanceScenarios.cs", "Selected serial/type, Ctap Selection cancel/request/resolution/same-session GetInfo/reopen; timing stores first monotonic events and permits resolved-before-catch. OTP3 info cycles pinned transport. Removal requires terminal1 plus observed absence and new object; outer owner finally disposes. Prompt borrowed; cancellation task explicitly awaited. Whole CTAP/Management behavior stays later boundaries."),
    ("ExpertFeatureScenario.cs", "Requires exactly one key and one OTP keyboard interface to bind raw route; three GET8/live Management serial checks. Verification adapter copies/clears outbound report and borrows expert connection; session disposed before outer connection; synchronous calls wrapped in tasks remain synchronous."),
    ("ExpertIoScenario.cs", "Unique one-key/FIDO interface;6s no-command timeout, same connection INIT/GetInfo then reopen. Verification adapter copies/clears SET and borrows raw connection; success checks CTAP status/payload presence, not parsed full authenticator metadata."),
    ("ListenerDrainScenario.cs", "Real native wrapper forwards static callbacks and roots wrapper independently. Holds real matching/removal callback; snapshots order stop/callback exit/close/unschedule/release/return. Late timeout retains/refuses restart then drains/restarts. Selected removal binds registry entry to serial. Stop watchdog/cleanup warnings separate from positive proof; captures nonzero CloseResult without calling it per-device success."),
    ("MacOSHidRouteVerification.csproj", "Actual Core/Ctap2/Management project refs, AOT/unsafe/invariant globalization; compile-only Linux traversal is separate from all real Mac modes."),
    ("Program.cs", "Mac guard precedes all modes including timing self-test. Parent child/watchdog, explicit selected serial; three production FIDO cycles, actual public pending receive invocation barrier/dispose/terminal/reopen, borrowed two-session reuse, pre-cancel no command then GetInfo. OTP mode reports abandoned discovery recovery rather than early claim release. No current Mac execution. Intro no-device-mutation wording needs read-only persistent-state qualification because SELECT changes applet state (C008)."),
    ("SmartCardScenario.cs", "SmartCard-only fresh discovery then three async open/begin/Management read/end/dispose cycles; serial and stable firmware equality. Explicit async transaction cast assumes built-in route; end in finally and session before connection. SELECT read-only metadata does change applet selection; no physical PIN-state/isolation or native error lane proved."),
]

def sha(data):
    return hashlib.sha256(data).hexdigest()

def git(*args):
    return subprocess.check_output(["git", *args])

def write(name, obj):
    (HERE / name).write_text(json.dumps(obj, indent=2) + "\n")

def main():
    assert len(NOTES) == len(PACKET["paths"]) == 41
    evidence = {}
    for path, (expected, note) in zip(PACKET["paths"], NOTES, strict=True):
        assert Path(path).name == expected, path
        for old, new in [
            ("use32", "use 32"), ("ID64", "ID 64"), ("result0/4", "results 0/4"),
            ("Destroy0", "Destroy 0"), ("explicit0x", "explicit 0x"), ("returns0", "returns 0"),
            ("metadata17/23", "metadata 17/23"), ("metadata65/72", "metadata 65/72"),
            ("SET5", "SET 5"), ("SET72", "SET 72"), ("GET8", "GET 8"), ("GET64", "GET 64"),
            ("from65", "from 65"), ("Short0/3/8", "Short 0/3/8"), ("Fresh child2", "Fresh child: 2"),
            ("warmup+10", "warmup + 10"), ("+1idle", " + 1 idle"), ("Stable1.18", "Stable 1.18"),
            ("stable1.18", "stable 1.18"), ("enum32", "enum 32-bit"), ("type32", "type 32-bit"),
            ("Real6s", "Real 6s"), ("exactly22", "exactly 22"), ("Typed64", "Typed 64-byte"),
            ("typed8", "typed 8-byte"), ("GET0/3/8", "GET 0/3/8"), ("65 to64", "65 to 64"),
            ("Source659", "Source 659"), ("OTP3", "OTP 3"),
        ]:
            note = note.replace(old, new)
        evidence[path] = note
    primary = set(PACKET["unit_ids"])
    shared = set(PACKET["shared_read_unit_ids"])
    selected = [u for u in UNITS if u["id"] in primary | shared]
    assert len(selected) == len(primary) + len(shared)
    files = {}
    for path in sorted({u["path"] for u in selected}):
        raw = Path(path).read_bytes()
        pinned = git("show", f"{PIN}:{path}")
        text = raw.decode("utf-8-sig")
        assert raw.replace(b"\r\n", b"\n") == pinned.replace(b"\r\n", b"\n"), path
        files[path] = {"path": path, "source_sha256": sha(raw), "pinned_git_blob": git("rev-parse", f"{PIN}:{path}").decode().strip(),
                       "pinned_raw_equal": raw == pinned, "pinned_lf_equal": True,
                       "primary": path in PACKET["paths"], "complete_primary_file_read": path in PACKET["paths"],
                       "lines": len(text.splitlines()), "evidence": evidence.get(path, "Five assigned shared ranges read with enclosing guide/architecture context; see investigation-B3.md.")}
    results = []
    for u in selected:
        text = Path(u["path"]).read_bytes().decode("utf-8-sig")
        body = text[u["start_character"]:u["end_character_exclusive"]]
        assert files[u["path"]]["source_sha256"] == u["source_sha256"], u["id"]
        assert sha(body.encode()) == u["range_sha256"], u["id"]
        p, label = u["path"], u["label"]
        candidates = []
        if p.endswith("IOKitFramework.Interop.cs") and 90 <= u["first_line"] <= 127: candidates.append("CORE-B3-C001")
        if u["id"] == "core-001:unit:200ff2d981c3a51d": candidates.append("CORE-B3-C001")
        if p.endswith("MacOSHidInterface.cs") and "GetList(" in label: candidates.append("CORE-B3-C002")
        if p.endswith("IOKitHelpers.cs") and ("GetIntPropertyValue(" in label or "GetNullableIntPropertyValue(" in label): candidates.append("CORE-B3-C003")
        if p.endswith("IIOKitDeviceLifetime.cs") and ("RegisterInputReportCallback(" in label or "OpenDevice(" in label): candidates.append("CORE-B3-C004")
        if p.endswith("IOKitHid.Interop.cs") and "IOHIDDeviceRegisterInputReportCallback(" in label: candidates.append("CORE-B3-C004")
        if p.endswith("MacOSHidDeviceListener.cs") and ("StartGeneration(" in label or "public Generation(" in label): candidates.append("CORE-B3-C005")
        if (p.endswith("MacOSFidoHidConnection.cs") or p.endswith("MacOSOtpHidConnection.cs")) and "ScheduleLocked(" in label: candidates.append("CORE-B3-C006")
        if p.endswith("MacOSHidDeviceListener.cs") and "public Generation(" in label: candidates.append("CORE-B3-C006")
        if (p.endswith("MacOSHidIOReportConnection.cs") or p.endswith("MacOSHidFeatureReportConnection.cs")) and ("GetReport(" in label or u["first_line"] == 1): candidates.append("CORE-B3-C007")
        if p == "verification/MacOSHidRouteVerification/Program.cs" and u["first_line"] == 1: candidates.append("CORE-B3-C008")
        if p.endswith("CoreFoundation.Interop.cs") and ("CFNumberCreate(" in label or "CFNumberGetValue(" in label): candidates.append("CORE-B3-C009")
        results.append({**u, "ownership": "PRIMARY" if u["id"] in primary else "ASSIGNED-SHARED-READ",
                        "status": "READ-CANDIDATE" if candidates else "READ-CLEAN", "candidate_ids": candidates,
                        "read_scope": "Complete semantic unit plus enclosing owner/helper/assertion context",
                        "read_evidence": files[p]["evidence"], "current_source_sha256": files[p]["source_sha256"],
                        "current_range_sha256": sha(body.encode()), "review_status": "PENDING-INDEPENDENT-REVIEW",
                        "validation_status": "VALID-NOT-CHECKED", "platform_status": "NO-NEW-EXECUTION",
                        "qualification": "Source/assertion read; original OS/shim source and historical machine evidence retain their separate revisions. No universal native or hardware acceptance.",
                        "sample_status": "INELIGIBLE-CANDIDATE" if candidates else "ELIGIBLE-AFTER-COORDINATOR-RECONCILIATION"})
    write("unit-dispositions.json", results)
    write("file-identities.json", list(files.values()))
    counts = collections.Counter(u["status"] for u in results)
    write("coverage-summary.json", {"source_pin": PIN, "observed_head": git("rev-parse", "HEAD").decode().strip(),
          "packet_sha256": sha((RUN / "inventory/packets/B3-macos-lifetimes.json").read_bytes()), "primary_files": 41,
          "primary_lines": sum(files[p]["lines"] for p in PACKET["paths"]), "primary_units": len(primary),
          "shared_units": len(shared), "dispositions": dict(counts), "unread": 0,
          "primary_exclusions": 0, "secondary_handoffs_counted_as_primary": 0,
          "new_sdk_builds_tests_native_device_formatter_runs": 0, "independent_review": "PENDING"})
    lines = ["# Complete B3 primary-file evidence", "", "All 41 files were read in full, including helper bodies, assertions, error paths and project context. These are source observations, not test executions. Exact IDs/ranges/hashes remain in unit-dispositions.json. Candidate-associated unchanged paths require the separate decisions in candidates.md.", "", "| Path | Actual read evidence |", "| --- | --- |"]
    for path in PACKET["paths"]:
        lines.append(f"| `{path}` | {evidence[path]} |")
    (HERE / "file-evidence.md").write_text("\n".join(lines) + "\n")
    # Original-source blob check is independent of SHA-256 record hashes.
    tree = json.loads((HERE / "sources/native-release-tree.json").read_text())
    tree_map = {x["path"]: x for x in tree["tree"]}
    source_checks = []
    for f in ["owner.h", "owner.c", "internal.h", "backend_iohid.c", "native_exports.c"]:
        raw = (HERE / "sources" / ("native-" + f)).read_bytes()
        blob = hashlib.sha1(f"blob {len(raw)}\0".encode() + raw).hexdigest()
        expected = tree_map["Yubico.NativeShims/hidinput/" + f]["sha"]
        assert blob == expected
        source_checks.append({"path": f, "sha256": sha(raw), "git_blob": blob, "release_tree_blob_equal": True})
    enum_text = Path("src/Core/src/Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs").read_text(encoding="utf-8-sig")
    enum_part = enum_text[enum_text.index("IOReturnError ="):enum_text.index("/*\n * kern_return_t")]
    managed = {name: 0xE00002BC + i for i, name in enumerate(re.findall(r"\b(IOReturn\w+)\b", enum_part))}
    native_text = (HERE / "sources/apple-IOReturn.h").read_text()
    native = {"IOReturn" + name: 0xE0000000 | int(value, 16) for name, value in re.findall(r"^#define kIOReturn(\w+)\s+iokit_common_err\(0x([0-9a-f]+)\)", native_text, re.M)}
    enum_checks = [{"name": name, "managed_hex": f"{value:08X}", "apple_hex": f"{native[name]:08X}", "equal": value == native[name]} for name, value in managed.items() if name in native]
    write("source-checks.json", {"native_source": source_checks, "ior_return_static_comparison": enum_checks,
          "comment": "Pure text/integer analysis; no SDK import, native load or C# type initialization."})
    parents = []
    for parent_id, disposition, candidates in [
        ("core-001:unit:9fe6da15d252bcd2", "SECONDARY-READ-CLEAN-MACOS-CLAUSE-ONLY; Windows clauses remain B4", []),
        ("core-001:unit:7a72c68ab4742b48", "SECONDARY-READ-CANDIDATE-CALLBACK-PARAGRAPH", ["CORE-B3-C004"]),
    ]:
        original = next(u for u in UNITS if u["id"] == parent_id)
        parents.append({**original, "secondary_disposition": disposition, "candidate_ids": candidates,
                        "primary_count_increment": 0, "future_editor": "SHARED", "central_reconciliation": "PENDING"})
    write("secondary-handoffs.json", parents)
    path_groups = {
        "CORE-B3-C001": [PACKET["paths"][2], "docs/architecture/connection-ownership-and-contention.md", PACKET["paths"][8], PACKET["paths"][10], PACKET["paths"][15]],
        "CORE-B3-C002": [PACKET["paths"][14], PACKET["paths"][0], PACKET["paths"][3], "src/Core/src/Transports/Hid/FindHidInterfaces.cs", "docs/usage/device-discovery.md"],
        "CORE-B3-C003": [PACKET["paths"][9], PACKET["paths"][7], PACKET["paths"][0], PACKET["paths"][14], PACKET["paths"][8]],
        "CORE-B3-C004": ["src/Core/CLAUDE.md", PACKET["paths"][11], PACKET["paths"][8], PACKET["paths"][3], PACKET["paths"][24], PACKET["paths"][37], PACKET["paths"][10]],
        "CORE-B3-C005": [PACKET["paths"][11], PACKET["paths"][24], PACKET["paths"][37], "src/Core/src/Devices/HotplugSignals.cs", "src/Core/CLAUDE.md"],
        "CORE-B3-C006": [PACKET["paths"][10], PACKET["paths"][15], PACKET["paths"][11], PACKET["paths"][21], PACKET["paths"][26], PACKET["paths"][24], "docs/usage/user-interaction.md"],
        "CORE-B3-C007": [PACKET["paths"][i] for i in [10,12,13,15,18,19,22,23]] + ["src/Core/README.md", "docs/architecture/raw-access-tiers.md", "src/Core/src/Transports/Hid/IHidConnection.cs", "docs/migration/v1-to-v2.md"],
        "CORE-B3-C008": PACKET["paths"][27:] + ["src/Core/README.md", "src/Core/CLAUDE.md", "docs/architecture/raw-access-tiers.md", "docs/plans/yubikit-async-boundaries/02-architecture.md", "docs/NATIVE-AOT.md"],
        "CORE-B3-C009": [PACKET["paths"][0], PACKET["paths"][9], PACKET["paths"][11]],
    }
    write("candidate-associated-paths.json", [{"candidate_id": c, "path": p,
          "investigator_edit": "UNCHANGED", "future_editor_disposition": "PENDING-EXPLICIT-RETAIN-QUALIFY-OR-EDIT",
          "future_reviewer_disposition": "PENDING-FULL-UNIT-AND-ORIGINAL-CITATION-REVIEW",
          "primary_ownership": "B3" if p in PACKET["paths"] else "DEPENDENCY-OWNER-RETAINED"}
          for c, paths in path_groups.items() for p in paths])
    package_root = Path.home() / ".nuget/packages/yubico.nativeshims/1.18.1"
    package = package_root / "yubico.nativeshims.1.18.1.nupkg"
    with zipfile.ZipFile(package) as archive:
        assets = [{"path": name, "sha256": sha(archive.read(name))} for name in archive.namelist()
                  if name.endswith(".dylib")]
    write("sources/package-static-identity.json", {"package_path": str(package), "package_sha256": sha(package.read_bytes()),
          "nuspec_sha256": sha((package_root / "yubico.nativeshims.nuspec").read_bytes()),
          "mac_asset_hashes": assets, "execution": "READ-ONLY-ZIP-HASH; NO-NATIVE-LOAD",
          "source_build_correspondence": "UNESTABLISHED-BY-THIS-PACKET"})
    print(json.dumps({"files": len(PACKET["paths"]), "primary": len(primary), "shared": len(shared), "counts": dict(counts), "enum_mismatches": sum(not x["equal"] for x in enum_checks)}, indent=2))

if __name__ == "__main__":
    main()
