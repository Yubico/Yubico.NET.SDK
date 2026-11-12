"""Static join of manually authored boundary observations to unchanged inventory identities."""
import collections
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path.cwd()
RUN = ROOT / "docs/research/documentation-audit/modules/core/pass-1/core-001"
OUT = Path(__file__).resolve().parent
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def load(p):
    return json.loads(p.read_text())


def sha(b):
    return hashlib.sha256(b).hexdigest()


def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + "\n")


# Authored after complete contextual reads. Hashes establish identity, never reading.
NOTES = {
"CLAUDE.md": "Guide distinguishes session/fixture/discovery snapshots, selected transports, unused prompt and borrowed SCP/codes. Complete examples retain cleanup scopes. Reopen returned firmware/timeout ownership with X002; current row/pin qualifications remain useful.",
"README.md": "Independent examples acquire one owned session at a time; reboot scope ends before fresh Find and delay/order are qualified. Reset family, per-command lock, effective firmware, validation limits and unused prompt match source. X002 adds precise returned-firmware/storage provenance.",
"PACKAGE_README.md": "Package purpose, alpha qualification and transitive Core dependency match shipping project. Installation text read only.",
"src/ManagementSession.cs": "Direct factory binds before Prepare, transport probe and DeviceInfo; Configure/SCP precede backend/readiness. _version retains initial discovery. Later reads use that fallback. Write clears encoded bytes in finally. Reset has no family gate/success invalidation; inherited decisions and B6 remain.",
"src/IManagementSession.cs": "XML separates full workflows from individual exchanges, caller-code memory and reset family/gates. X002 supplies returned-firmware/timeout qualification. Interface declares no Management IsLocked property.",
"src/IYubiKeyExtensions.cs": "One-shot methods await owned scopes. Factory selects one transport, copies options with resolved preference, opens typed connection, transfers ownership after direct success. Failures propagate without sibling retry.",
"src/DeviceConfig.cs": "Null omission/full-field replacement; exact USB None guard; ushort/byte narrowing without supported-mask checks. Encoder clears owned TLVs/intermediate bytes; returned length-prefixed buffer belongs to session cleanup. Existing encoder findings retain applet ownership.",
"src/SetDeviceConfigOptions.cs": "Context-only: borrowed code lifetime includes one-shot creation; false reboot default; null/empty/sixteen-zero replacement differ. Accurate existing text; no primary IDs.",
"src/Backend/IManagementBackend.cs": "Context-only: backend owns no resource/protocol/connection; write borrows payload; internal SetMode is outside public surface. No primary IDs.",
"src/Backend/SmartCardBackend.cs": "SELECT fallback firmware; write INS1C, internal mode INS16, reset INS1F use borrowed effective protocol/default error handling. Reset zero CLA/P1/P2/no data; final wire/status/SCP review remains B6.",
"src/Backend/FidoHidBackend.cs": "C3 write and C0 internal mode use prepared IFidoHidProtocol with None notification. Backend neither initializes channel nor owns it; Core C2 reader and session HID-reset refusal are separate.",
"src/Backend/OtpBackend.cs": "ReadStatus initializes and reads status; fallback prefers protocol firmware. Writes delegate slot constants; Core owns framing/outbound CRC and DeviceInfoReader checks inbound info CRC. Backend owns nothing.",
"src/PublicAPI.Shipped.txt": "Context exclusion: nullable directive only, no active shipped declarations or assigned primary IDs.",
"src/PublicAPI.Unshipped.txt": "Snapshot matches inspected factories/operations, separate config/session options and Core-owned metadata. Compiler binding remains VALID-NOT-CHECKED.",
"src/Yubico.YubiKit.Management.csproj": "Shipping Management references Core only and opts into shared AOT policy; IVT enables backend fakes. Static configuration supplies no compile/publish result.",
"tests/CLAUDE.md": "Fake readiness cases in integration project, explicit read-only hardware filter, two-key monitor prerequisites, cached callback and device-filter/transport-pin distinctions are current. X001/X002 reopen linked explanations.",
"examples/ManagementTool/Program.cs": "Menu callbacks discard ct; first-token command dispatch ignores remaining args. README already qualifies cancellation/help limitations. Each menu owns fresh selection/session lifetime; runtime strings preserved.",
"examples/ManagementTool/README.md": "Guide accurately scopes SmartCard eligibility, commands versus menus, requested reboot flag, recovery, cancellation gaps and existing misleading FIPS/reset output. No new output or prompt-policy changes.",
"examples/ManagementTool/ManagementTool.csproj": "Actual Management/Core/Cli.Shared/Cli.Commands references plus Spectre PackageReference and explicit HintPath. Reference resolution/analyzer participation requires later coordinator compilation.",
"examples/ManagementTool/Cli/Prompts/DeviceSelector.cs": "Shared selector requires SmartCard and RequireChoice after All discovery, preserving composites/reset menu eligibility. FormFactor formatting is shared exact-value policy. Exposed SmartCard wins normal Management order.",
"examples/ManagementTool/Cli/Prompts/LockCodePrompt.cs": "Context-only: full helper read; immutable strings and confirmation throw before cleanup remain documented limitations. Returned arrays are cleared by menu callers. No primary IDs or new whole-prompt audit.",
"examples/ManagementTool/Cli/Menus/CapabilitiesMenu.cs": "Owned session/live read precede complete modeled supported-mask choice, USB None guard and borrowed code under finally. Reboot is requested and no later old-session read occurs. Unknown-bit representation remains A dependency.",
"examples/ManagementTool/Cli/Menus/DeviceFlagsMenu.cs": "Live flags drive two modeled choices/full replacement; code finally encloses awaited write. Existing TouchEject output omits CCID-only condition; adjacent comment/README already qualify it.",
"examples/ManagementTool/Cli/Menus/DeviceInfoMenu.cs": "Awaited status callback owns session/live read. Existing FIPS Mode/reset-availability output exceeds manufacturing/firmware evidence; nearby prose preserves limitations. A-C004/C005 retain interpretation ownership.",
"examples/ManagementTool/Cli/Menus/LockCodeMenu.cs": "Live IsLocked chooses operations; Set/Change/Remove await wrappers inside independently cleared caller-array scopes. Unsupported reset-recovery warning is already qualified by comment/README; retain old finding.",
"examples/ManagementTool/Cli/Menus/ResetMenu.cs": "Live read then firmware-only helper and double confirmation; SmartCard eligibility comes from selector. Broad erase/default-PIN output is already documented as unreliable. Success adds no cache invalidation; MGMT-IMP-010 remains.",
"examples/ManagementTool/Cli/Menus/TimeoutsMenu.cs": "Live auto-eject/first optional timeout byte drive defaults. Assignment ceilings are SDK policy. Code finally encloses awaited write; reboot ends usable old scope. A-C003 affects timeout storage separately from UI default15.",
"examples/ManagementTool/Cli/Output/OutputHelpers.cs": "Renderers enumerate known modeled capability/flag bits only and can print empty text for unknown-only values. Presentation is neither raw preservation nor support proof; reopen with A-C002/C006.",
"examples/ManagementTool/Commands/ConfigCommand.cs": "Selector then owned session/explicit DeviceInfoQuery. Failure returns1 without substituting selection snapshot; displayed configuration inherits Core interpretation.",
"examples/ManagementTool/Commands/InfoCommand.cs": "Selector then owned session/explicit live read; FIPS Mode/masks retain documented limits, flagged form factors can miss exact formatter. No stale fallback after live failure.",
"examples/ManagementTool/Features/DeviceConfiguration.cs": "Wrappers borrow session/codes and create per-command options; RebootRequired echoes request. Broad catch includes cancellation. Encoder/session own transient copies; caller owns source arrays. Full helper cleanup/examples read.",
"examples/ManagementTool/Features/DeviceInfoQuery.cs": "Context-only: live-read wrapper converts operation exceptions including cancellation to failure; no cached fallback. Typed example disposes session before connection. No primary IDs.",
"examples/ManagementTool/Features/DeviceReset.cs": "Numeric firmware helper inherits major-zero policy. Supplied metadata preflight can differ from overridden session firmware; family stays caller prerequisite. Broad catch converts operation/cancellation exceptions to result.",
"examples/ManagementTool/Features/Results/DeviceInfoResult.cs": "Record copies DeviceInfo value while retaining A-C003's underlying timeout alias. Success constructor adds no deep snapshot; X002 reopening dependency.",
"examples/ManagementTool/Features/Results/ConfigResult.cs": "Context-only: RebootRequired explicitly records requested flag, with no pending/completed reboot observation. No primary IDs.",
"examples/ManagementTool/Features/Results/ResetResult.cs": "Context-only: helper outcome carrier adds no device metadata or physical reset evidence. No primary IDs.",
"tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs": "Factory failure/pre-cancel preserve borrowed connection; constructor-only ProbeSession never attaches (X003). Reset fake checks one INS1F; HID fake throws on I/O. UninitializedObject backend fake checks successful encoded clearing only. Disposal helpers bypass initialization.",
"tests/Yubico.YubiKit.Management.UnitTests/IYubiKeyExtensionsTransportTests.cs": "SelectionProbe records requested generic type then throws; FallbackProbe records exact attempts. Synthetic held/SCP failures prove no HID attempt, without native/initialization proof. Inline owned default SCP remains undisposed: MGMT-IMP-004.",
"tests/Yubico.YubiKit.Management.UnitTests/ManagementBackendLifecycleTests.cs": "Reflection asserts IDisposable assignability only; prepared fake throws if Initialize called and records C3. Resource-free implementation follows source, beyond reflection's limited oracle.",
"tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionExchangeOverlapTests.cs": "Production factory over scripted two-page SmartCard fake holds page0, asserts refusal/no extra wire then pages0/1 and sequential reuse. Finally releases gate; per-exchange drain can precede whole-reader task (B5-C006/C008). Zero FIPS fixtures prove no mapping correctness.",
"tests/Yubico.YubiKit.Management.UnitTests/AuditV2/ManagementAuditReproTests.cs": "Record derivation, exact USB None/null omission, timeout guards and representative length/TLV bytes use explicit fixture. Python order is comparator, not mandatory protocol order; synthetic A5 copies are not production cleanup evidence.",
"tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs": "Complete comparison/null/parse/bytes/string/default/operator vectors read. Major-zero optimistic ordering is SDK policy; AlphaOrBeta names supply no hardware/qualifier proof. Hash inequality is one vector, not collision-free guarantee.",
"tests/Yubico.YubiKit.Management.UnitTests/Yubico.YubiKit.Management.UnitTests.csproj": "net10 executable xUnit3/MTP references Management with inherited compiler policy. No restore/evaluation/runner execution.",
"tests/Yubico.YubiKit.Management.IntegrationTests/IntegrationTestBase.cs": "Context-only no-op async lifetime has no monitor or reset setup, confirming one-shot applet model. No primary IDs.",
"tests/Yubico.YubiKit.Management.IntegrationTests/ManagementHidConcurrencyTests.cs": "Concrete rows pin/assert transport; five rounds accept success or specifically matched guard refusal, require success and subsequent read. Serial/firmware/form equality leaves other fields outside oracle. Actual refusal timing remains hardware-unexecuted.",
"tests/Yubico.YubiKit.Management.IntegrationTests/ManagementLockCodeTests.cs": "Cached starting metadata/default transport; missing-code assertion accepts any exception. Fallible finally restoration/readback precedes code zeroing; second restoration lacks readback. Preserve MGMT-IMP-004/old weak-oracle qualification.",
"tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionAdvancedTests.cs": "Complete rows read: repeated decode FIPS/form comparisons, independent AllTransports pins, inline undisposed default SCP (MGMT-IMP-004), cached-discovery wording X001. AdvancedPiv USB assertion is stronger than either-transport capability filter.",
"tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionCapabilityTests.cs": "OATH persisted USB mask toggle/finally readback differs from active-interface proof. NFC true write/readback and cached-value restoration supply no NFC-access/false-recovery evidence. Preserve original applet findings.",
"tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs": "Typed direct scopes choose SmartCard/FIDO; unpinned one-shot HID rows only select eligible devices. Serial NotEqual0 permits null. Success SCP parameters correctly scoped; wrong-key broad assertion cannot isolate auth. Simple restoration skips failed assertions/lacks readback (MGMT-IMP-004).",
"tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionTests.cs": "One placeholder per attribute/first matching fixture; exact form and FIPS interpretations repeat Core. MinFirmware5.0 remains ignored with existing truthful comment (MGMT-IMP-009). Default helper selection is separate from row metadata.",
"tests/Yubico.YubiKit.Management.IntegrationTests/OwnedMonitorReadOnlyTests.cs": "Two authorized serial-bearing fixtures required; immediate snapshot/replay, repeated reference retention, subscriber-local cancellation, factory token independence, completion/recreate and convenience no-match asserted. Every explicit live comparison occurs after monitor disposal; no attended hotplug proof.",
"tests/Yubico.YubiKit.Management.IntegrationTests/ReconfigurationDiscoveryInvariantTests.cs": "Fresh live original USB mask; disable OTP/reboot then repeated fresh scans assert distinct IDs/at most one serial-form holder. Finally fresh lookup/write/reboot/readback attempts restoration. Deadlines checked between awaits; stale single-key-never-reads-serial comment X001.",
"tests/Yubico.YubiKit.Management.IntegrationTests/ScopedDiscoveryReadOnlyTests.cs": "Seven scopes aggregate authorized connections by serial and compare eight named semantic fields through sequential typed connections/live reads. All scans precede live scopes; readiness validates metadata before allow-list filter. Snapshot list aliases timeout storage; X001 equality rationale stale, A-C003/X002 limit deep stability.",
"tests/Yubico.YubiKit.Management.IntegrationTests/ScopedDiscoveryReadinessTests.cs": "Hardware-free injected complete bystanders/serial-less views return authorized fixtures only; missing metadata on any view exhausts three attempts. ScanView.Connect throws; this tests readiness/authorization projection, not real discovery/protocols.",
"tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj": "xUnit2/VSTest references Management/Core/Tests.Shared; no blanket project-run safety inferred from fake readiness cases. Build/runner/native lanes remain separate.",
}

CANDIDATES = [
 {"id":"CORE-MGMT-X001","kind":"documentation","title":"Refresh boundary test provenance and equality rationale","paths":["src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ReconfigurationDiscoveryInvariantTests.cs","src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ScopedDiscoveryReadOnlyTests.cs","src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionAdvancedTests.cs"],"evidence":"TryFindBySerialAsync comment says single-key scans never read serials; PopulateMetadataAsync now does best-effort metadata. AssertMetadataMatches comment assumes reference-based record equality; DeviceInfo now overrides content equality. AdvancedTests calls fixture data cached discovery metadata, although shared initialization performs a separate live read.","action":"Describe current sources of evidence and intentionally selected comparison fields; retain assertions and fresh-lookup recovery. Alias MGMT-XREF-003/A-C013, without adding a fixture runtime bug.","trigger":"Metadata acquisition, equality, serial lookup or fixture binding changes."},
 {"id":"CORE-MGMT-X002","kind":"documentation/dependency","title":"Explain returned metadata firmware and buffer provenance","paths":["src/Management/src/IManagementSession.cs","src/Management/src/ManagementSession.cs","src/Management/README.md","src/Management/CLAUDE.md","src/Management/examples/ManagementTool/Features/Results/DeviceInfoResult.cs","src/Management/examples/ManagementTool/Cli/Menus/TimeoutsMenu.cs","src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ScopedDiscoveryReadOnlyTests.cs"],"evidence":"Public read passes initial _version into DeviceInfoReader; supplied default wins tag05, while non-final qualifier can win again. Initial metadata probe supplies null. DeviceInfo timeout memory aliases input TLV/caller storage (CORE-A-C003); copying into result/snapshot lists adds no deep ownership.","action":"Qualify the live-I/O claim locally: fresh configuration query with captured initial firmware fallback/qualifier rules. Coordinate storage/equality language with A-C003/C016; preserve implementation decision rather than normalizing defect as intended contract.","trigger":"DeviceInfo ownership/firmware precedence or Management initialization/read fallback changes."},
 {"id":"CORE-MGMT-X003","kind":"verification-gap","title":"Bound direct-factory guard-release oracle","paths":["src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs"],"evidence":"CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached constructs ProbeSession directly; ApplicationSession constructor never Attach-es. Thus final probe cannot detect a stale guard holder. Source currently rejects unsupported type before Construct attachment; reset fake asserts only Single command/INS1F.","action":"Retain source conclusion and narrow claimed oracle. A later authorized test owner should create a genuinely bound probe and assert exact cleanup/zero I/O; no tests or assertions changed here.","trigger":"Factory/guard binding, cleanup or reset framing changes."},
]

# Secondary source ranges actually inspected; original owners retain all dispositions.
DEPENDENCIES = {
"src/Core/src/Abstractions/IYubiKey.cs":[[1,225]],
"src/Core/src/Sessions/ApplicationSession.cs":[[1,569]],
"src/Core/src/Sessions/SessionCreationOptions.cs":[[1,80]],
"src/Core/src/Devices/YubiKeyConnectionExtensions.cs":[[130,305]],
"src/Core/src/Devices/YubiKeyDevice.cs":[[1,322]],
"src/Core/src/Devices/DeviceInfo.cs":[[1,361]],
"src/Core/src/Devices/DeviceInfoReader.cs":[[1,201]],
"src/Core/src/Devices/Feature.cs":[[1,42]],
"src/Core/src/Devices/FindYubiKeys.cs":[[65,110],[330,475]],
"src/Core/src/Devices/SnapshotDiff.cs":[[1,80]],
"src/Core/src/Utilities/ExchangeGuard.cs":[[1,140]],
"src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs":[[25,160]],
"src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs":[[25,160],[340,420],[520,562]],
"src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs":[[55,230],[450,520],[605,632]],
"src/Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs":[[15,60]],
"src/Core/src/Protocols/SmartCard/Scp/Scp11KeyParameters.cs":[[20,121]],
"src/Core/src/Protocols/SmartCard/Scp/StaticKeys.cs":[[15,110]],
"src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs":[[1,310]],
"src/Tests.Shared/YubiKeyTestStateExtensions.cs":[[1,140]],
"src/Tests.Shared/YubiKeyTestState.cs":[[315,428]],
"docs/migration/v1-to-v2.md":[[300,347]],
}


def main():
    packet_path = RUN / "inventory/packets/BOUNDARY-Management.json"
    packet = load(packet_path)
    all_units = load(RUN / "inventory/semantic-units.json")
    assigned = set(packet["unit_ids"])
    assert not packet["shared_read_unit_ids"]
    units = [u for u in all_units if u["id"] in assigned]
    assert len(units) == len(assigned)
    files = []
    for path in packet["disposition_paths"]:
        key = path.removeprefix("src/Management/")
        assert key in NOTES, key
        raw = (ROOT / path).read_bytes()
        text = raw.decode("utf-8-sig")
        pin = subprocess.check_output(["git", "show", f"{PIN}:{path}"], cwd=ROOT)
        norm = lambda b: b.decode("utf-8-sig").replace("\r\n", "\n")
        assert norm(raw) == norm(pin), path
        owned = [u for u in units if u["path"] == path]
        for u in owned:
            assert sha(raw) == u["source_sha256"], u["id"]
            assert sha(text[u["start_character"]:u["end_character_exclusive"]].encode()) == u["range_sha256"], u["id"]
        related = [c["id"] for c in CANDIDATES if path in c["paths"]]
        files.append({"path":path,"source_sha256":sha(raw),"pinned_git_blob":subprocess.check_output(["git","rev-parse",f"{PIN}:{path}"],cwd=ROOT,text=True).strip(),"pinned_bytes_sha256":sha(pin),"normalized_pin_equal":True,"read_scope":"complete file for assigned boundary units and enclosing helpers; no whole-module redisposition","unit_ids":[u["id"] for u in owned],"status":"READ-CANDIDATE-CONTEXT" if related else ("READ-CLEAN" if owned else "READ-CONTEXT-EXCLUDED-FROM-PRIMARY"),"candidates":related,"observation":NOTES[key],"unchanged_path_action":"Retain product bytes; fresh editor/reviewer dispositions required for candidate/dependency passages." if related else "Retain current text for this boundary; unrelated applet findings keep original scope."})
    by_path = {f["path"]:f for f in files}
    results = []
    for u in units:
        f = by_path[u["path"]]
        results.append({**{k:u[k] for k in ["id","path","source_sha256","range_sha256","start_character","end_character_exclusive","first_line","last_line","label","category"]},"status":"READ-CANDIDATE-CONTEXT" if f["candidates"] else "READ-CLEAN","read_context":f["observation"],"candidate_ids":f["candidates"],"review":"PENDING-FRESH-INDEPENDENT-REVIEW","validation":"VALID-NOT-CHECKED","sampling":"INELIGIBLE-CANDIDATE-CONTEXT" if f["candidates"] else "ELIGIBLE-SUBJECT-TO-COORDINATOR-STRATIFICATION"})
    for c in CANDIDATES:
        c["unit_ids"] = [u["id"] for u in results if c["id"] in u["candidate_ids"]]
        c["state"] = "LOCAL-CANDIDATE; zero canonical allocations"
        c["independent_review"] = "PENDING"
    registry = load(RUN / "inherited-findings/registry.json")
    inputs = load(RUN / "inventory/inherited-packet-inputs.json")["BOUNDARY-Management"]
    rows = registry["findings"] if "findings" in registry else registry["rows"]
    inherited = [{"key":r["key"],"inherited_state_preserved":r["current_state"],"original_definition":r["historical_definition"],"boundary_return":"See boundary-Management.md inherited-outcomes table; original authoritative state remains unchanged."} for r in rows if r["key"] in inputs["provenance_keys"]]
    assert len(inherited) == len(inputs["provenance_keys"])
    shared = load(RUN / "inventory/shared-sections.json")
    dependent_paths = ["src/Core/src/Abstractions/IYubiKey.cs","src/Core/README.md","src/Core/CLAUDE.md","src/Tests.Shared/CLAUDE.md","src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs","src/Tests.Shared/YubiKeyTestStateExtensions.cs","docs/migration/v1-to-v2.md"]
    save("shared-dependencies.json",{"assigned_shared_ids":[],"rule":"Secondary reads only; original primary readers and SHARED future editor retain ownership. Listed records route dependencies rather than claim whole shared-unit acceptance.","records":[x for x in shared if x["path"] in dependent_paths]})
    dependency_records = []
    for path, ranges in DEPENDENCIES.items():
        raw = (ROOT / path).read_bytes()
        pin = subprocess.check_output(["git", "show", f"{PIN}:{path}"], cwd=ROOT)
        assert norm(raw) == norm(pin), path
        dependency_records.append({"path":path,"read_line_ranges":ranges,"raw_sha256":sha(raw),"pinned_sha256":sha(pin),"normalized_pin_equal":True,"scope":"Secondary mechanism read only; preserves original owner/denominator.","intersecting_original_units":[{"id":u["id"],"source_sha256":u["source_sha256"],"range_sha256":u["range_sha256"],"owner":u["stage"]} for u in all_units if u["path"]==path and any(u["first_line"]<=end and u["last_line"]>=start for start,end in ranges)]})
    save("mechanism-identities.json",dependency_records)
    save("unit-dispositions.json",results)
    save("file-dispositions.json",files)
    save("candidates.json",CANDIDATES)
    save("inherited-dispositions.json",inherited)
    nominee_names = ["ReferenceScanAsync_MetadataCompleteSeriallessDevice_ReturnsOnlyAuthorizedFixture","WatchAsync_CancellationIsSubscriberLocal_DisposalCompletesStreams_AndRecreationIsIndependent","CreateManagementSessionAsync_DefaultOnlyHidOtp_FallsBackToHidOtp","CreateManagementSessionAsync_SmartCardHeld_PropagatesWithoutHidFallback"]
    nominees = [u for u in results if u["status"]=="READ-CLEAN" and any(name in u["label"] for name in nominee_names)]
    save("clean-nominees.json",{"state":"Nominations only; coordinator chooses stratified sample and removes later edited overlaps.","units":nominees})
    save("verification.json",{"source_pin":PIN,"observed_metadata_head":subprocess.check_output(["git","rev-parse","HEAD"],cwd=ROOT,text=True).strip(),"packet_sha256":sha(packet_path.read_bytes()),"primary_unit_count":len(results),"shared_primary_count":0,"candidate_disposition_paths":len(files),"primary_source_paths":len(packet["paths"]),"unit_status_counts":dict(collections.Counter(u["status"] for u in results)),"all_assigned_ids_exact":set(u["id"] for u in results)==assigned,"all_raw_range_and_normalized_git_hashes_match":True,"unread_units":0,"builds":0,"tests":0,"runtime_experiments":0,"native_operations":0,"device_operations":0,"formatter_runs":0,"commits":0,"subagents":0,"independent_content_review":"PENDING","B4":"PENDING-RETURN + future user-ready Windows delta","B6":"PENDING-RETURN","claim_scope":"Complete assigned Management source investigation with explicit deferred mechanisms; not whole-Core or nine-boundary closeout."})
    print(json.dumps(load(OUT / "verification.json"),indent=2))


if __name__ == "__main__":
    main()
