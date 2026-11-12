"""Join explicit B6 readings to assigned IDs; never infer semantic reads from hashes."""
import collections
import hashlib
import json
import pathlib
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
ROOT = next(parent for parent in HERE.parents if (parent / ".git").exists())
RUN = HERE.parent.parent
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"

def load(path):
    return json.loads(path.read_text())

def sha(data):
    return hashlib.sha256(data).hexdigest()

def save(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2) + "\n")

packet = load(RUN / "inventory/packets/B6-wire-scp.json")
inventory = {u["id"]: u for u in load(RUN / "inventory/semantic-units.json")}
notes = load(HERE / "semantic-notes.json")
assert len(notes) == 50
unread = {"ansx963_2001.rsp", "ansx963_2001.txt"}
primary = set(packet["unit_ids"])
shared = set(packet["shared_read_unit_ids"])
assert not primary & shared

# Concerns are file-context associations. Narrow edits require later unit-by-unit triage.
associations = {
    "AnswerToReset.cs": [16], "ApduCommand.cs": [1, 2, 15], "ApduException.cs": [1],
    "ApduFormatterExtended.cs": [1, 2, 11], "ApduFormatterShort.cs": [1, 2, 11],
    "ApduResponse.cs": [2], "ApduTransmitter.cs": [2], "ChainedApduTransmitter.cs": [1, 2, 3],
    "ChainedResponseReceiver.cs": [1, 2, 3, 12], "IApduProcessor.cs": [13],
    "ISmartCardProtocol.cs": [1, 3], "PcscProtocol.Scp.cs": [4, 6], "PcscProtocol.cs": [1, 3, 14],
    "ProtocolConfiguration.cs": [1, 3], "SWConstants.cs": [1], "SmartCardMaxApduSizes.cs": [1],
    "AesCmac.cs": [4, 13], "DataEncryptor.cs": [7], "KeyReference.cs": [8],
    "PcscProtocolScp.cs": [4, 7, 15], "Scp03KeyParameters.cs": [7], "Scp11KeyParameters.cs": [7, 9],
    "Scp11X963Kdf.cs": [2, 9, 10], "ScpInitializer.cs": [6, 10, 12], "ScpKeyParameters.cs": [7],
    "ScpKid.cs": [10], "ScpProcessor.cs": [2, 4, 5, 10, 11, 12],
    "ScpState.Scp03.cs": [6, 10], "ScpState.Scp11.cs": [7, 9, 10], "ScpState.cs": [2, 4, 7, 10, 11],
    "SessionKeys.cs": [6, 7], "StaticKeys.cs": [6, 7],
    "CoreScpAuditReproTests.cs": [5, 6, 13], "ApduFormatterTests.cs": [1, 13],
    "FakeApduProcessor.cs": [2, 13], "FakeSmartCardConnection.cs": [13],
    "PcscProtocolConcurrencyTests.cs": [3, 13], "PcscProtocolTests.cs": [1, 2, 3, 13],
    "AesCmacTests.cs": [13], "PcscProtocolScpTests.cs": [4, 5, 11, 12, 13],
    "Scp11TestData.cs": [9, 13], "Scp11Tests.cs": [7, 9, 13],
    "Scp11X963KdfTests.cs": [6, 9, 13], "ScpEmptyResponseTests.cs": [5, 10, 13],
    "ScpExtensionsTests.cs": [7, 13], "X963KdfTests.cs": [13],
}
shared_notes = {
    "d753e6c270ca4e74": ([2, 15], "Root illustrative APDU class examples privately clone data but omit cleanup; current canonical command is caller-memory record and response is sealed Clear owner. Reconcile examples without promoting a new runtime type design."),
    "adcf2d48efd5b510": ([], "Plain ApduCommand/visible wire constants/shared TLV and fake-protocol tests match flat style. This design instruction does not claim every APDU profile or helper is already correct."),
    "e86e64e529f433a8": ([1, 3], "SmartCard raw task, ownership/reuse and continuation refusal read with complete examples. Blanket failure refusal needs first-fragment uncertainty qualification; borrowed connection recovery remains dispose/reopen."),
    "911aecf7d17711ec": ([7, 15], "RawSCP fragment has enclosing parameter lifetime and config-beforeSCP but SELECT afterSCP sends plain while retaining old state. LoadScp helper is application-dependent; exact later B5/public shape review pending."),
    "3be8f3ae5cdbd456": ([], "Dated July construction entry describes historical public wrapper/WithScp; August entry explicitly supersedes it. Preserve as dated history, not present API."),
    "b3bcf9df88d4a939": ([], "Dated August internalization entry is historical analysis, with current raw-session migration direction. Its old assertion that all factories only have convenience path needs scope if republished as current API; preserve history."),
    "a196d3416b0b4e9d": ([7, 9, 10, 15], "Parity/gap section claims no genuine protocol regressions after spot checks and names retained Piv SCP fields; reopen with actual current parameter/selection/profile findings and B5/boundary trace. Treat historical comparison scope separately from proof."),
    "feb637f3002838f3": ([7, 15], "Current migration raw-session path and config-beforeSCP accurate by source; secure-storage loader remains typed application-dependent fragment and contained ownership rules need concrete cross-reference."),
    "1de200300b0e4882": ([1, 12], "Pipeline graphic linearizes command/response decorators; actual secure response graph differs and sizes/configuration need exact source qualifications. Private protocol Configure examples are contributor fragments."),
    "4a8677b31b689a8c": ([7, 9, 15], "Three SCP constructor fragments have implicit inputs and no enclosing disposable parameter scopes or trust setup. Required public-key/host-key/certificate roles and owner lifetime need task context."),
    "7f8ea35c6e10ae37": ([2, 15], "Sensitive PIN fragment clears only after await success, lacks finally and response cleanup; caller-memory command retention explanation itself is accurate."),
    "2336d8929cbba2f8": ([], "Complete APDU response ownership section matches sealed copied owner and Clear over status/data aliases, preserving source. Scope is only response owner, not upstream transport or intermediate buffers."),
    "2637633b642ae69c": ([1, 2, 3, 15], "Complete209-line rawAPDU task/transaction/direct-platform examples read. APDU-specific clauses checked against protocol; native paragraphs retain B1/B2/B3/B4 ownership and their qualifications. No native acceptance added from reading shared prose."),
    "4ea14f95e100e574": ([7, 9, 15], "Secure-channel task creates keys/params/raw session then stops; supplied source arrays, trusted provenance, useful operation and cleanup-on-failure need complete explicit task. Existing nested disposal order is correct."),
    "8cec358279fb0d77": ([], "RecordingSmartCardConnection guidance accurately scopes queue/record behavior and warns against protocol emulator or hardware substitution. Actual helper source read for context."),
    "ad3312a036a8ce65": ([], "Recording helper explicitly returns false extended support, so production uses short framing/chaining. This method supplies fake routing only, not actual reader capability."),
}

# Explicit substantive clean nominations after source/assertion/original passage reading.
# Other context in these candidate-associated files remains pending narrow triage.
clean_nominee_ids = {
    "core-001:unit:6be1d61c780616e4": "Raw response constructor copies input; alias/source assertions cover this overload.",
    "core-001:unit:c2ce872fba34a5bc": "Data-plus-status constructor independently owns appended storage; both ownership-test modes read.",
    "core-001:unit:186860eadeaa30ae": "Clear reaches actual owned raw/status/data aliases and is repeatable; scope excludes upstream copies.",
    "core-001:unit:5cb383d7e8d3b26c": "Actual original source snapshot and all owned aliases compared after repeated Clear for both constructors.",
    "core-001:unit:9df2a1654c179c92": "RFC4493 section4 Example1 exact empty-message tag and key compared with retained original text.",
    "core-001:unit:87ef60aeb0f61a18": "RFC4493 section4 Example2 exact16-byte message and full tag compared with original.",
    "core-001:unit:beddbb946521ed32": "RFC4493 section4 Example3 exact40-byte message and full tag compared with original.",
    "core-001:unit:2efcc7f8ae90292e": "RFC4493 section4 Example4 exact64-byte message and full tag compared with original.",
}

files = {}
units = []
for uid in packet["unit_ids"] + packet["shared_read_unit_ids"]:
    u = inventory[uid]
    path = u["path"]
    if path not in files:
        raw = (ROOT / path).read_bytes()
        pinned = subprocess.check_output(["git", "show", f"{PIN}:{path}"], cwd=ROOT)
        files[path] = {"path": path, "source_sha256": sha(raw), "pinned_git_blob": subprocess.check_output(["git", "rev-parse", f"{PIN}:{path}"], cwd=ROOT, text=True).strip(), "pinned_source_sha256": sha(pinned), "pin_equality": "exact" if raw == pinned else "LF-normalized-equal" if raw.replace(b"\r\n", b"\n") == pinned else "DIFFERENT", "lines": len(raw.decode("utf-8-sig").splitlines()), "assigned_unit_ids": [], "primary": uid in primary}
    file = files[path]
    raw = (ROOT / path).read_bytes()
    text = raw.decode("utf-8-sig")
    current_range = sha(text[u["start_character"]:u["end_character_exclusive"]].encode())
    assert sha(raw) == u["source_sha256"], path
    assert current_range == u["range_sha256"], uid
    assert file["pin_equality"] != "DIFFERENT", path
    file["assigned_unit_ids"].append(uid)
    name = pathlib.Path(path).name
    if uid in primary:
        observation = notes[name]
        concerns = associations.get(name, [])
        state = "UNREAD" if name in unread else "READ-RELATED-CANDIDATE-CONTEXT" if concerns else "READ-CLEAN"
    else:
        concerns, observation = shared_notes[uid.rsplit(":", 1)[1]]
        state = "READ-RELATED-CANDIDATE-CONTEXT" if concerns else "READ-HISTORICAL-CONTEXT" if "changelog" in path else "READ-CLEAN"
    if uid in clean_nominee_ids:
        state = "READ-CLEAN"
        observation = clean_nominee_ids[uid] + " No execution or all-path cleanup/conformance claim."
    record = {k: u[k] for k in ["id", "path", "source_sha256", "range_sha256", "start_character", "end_character_exclusive", "first_line", "last_line", "category", "label"]}
    record.update(assignment="primary" if uid in primary else "shared", current_source_sha256=sha(raw), current_range_sha256=current_range, identity_verified=True, disposition=state, observation=observation, candidate_context_ids=[f"CORE-B6-C{n:03}" for n in concerns], final_narrow_triage="SOURCE-SCOPE-COMPLETE" if state == "READ-CLEAN" else "PENDING" if concerns or state == "UNREAD" else "SOURCE-SCOPE-COMPLETE", review_status="UNASSIGNED", validation="VALID-NOT-CHECKED", sample_status="INELIGIBLE-PENDING-FINAL-TRIAGE" if state != "READ-CLEAN" else "ELIGIBLE-ONLY-FOR-SCOPED-SOURCE-REVIEW")
    units.append(record)

for path, file in files.items():
    selected = [u for u in units if u["path"] == path]
    file["read_scope"] = "complete-file" if file["primary"] and pathlib.Path(path).name not in unread else "unread-complete-assignment" if file["primary"] else "assigned-ranges-with-context"
    file["dispositions"] = dict(collections.Counter(u["disposition"] for u in selected))
    file["observations"] = sorted(set(u["observation"] for u in selected))
    file["product_disposition"] = "UNCHANGED; pending fixture reading" if file["read_scope"].startswith("unread") else "UNCHANGED; source observations retained for fresh-editor triage, no product edit authorization"

summary = {"source_pin": PIN, "observed_metadata_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(), "state": "CHECKPOINT-INCOMPLETE", "primary_files_assigned": len(packet["paths"]), "primary_lines_assigned": packet["lines"], "primary_files_read_completely": len(packet["paths"]) - len(unread), "primary_lines_read": sum(f["lines"] for f in files.values() if f["primary"] and not f["read_scope"].startswith("unread")), "primary_unit_count": len(primary), "shared_unit_count": len(shared), "unit_dispositions": dict(collections.Counter(u["disposition"] for u in units)), "primary_dispositions": dict(collections.Counter(u["disposition"] for u in units if u["assignment"] == "primary")), "shared_dispositions": dict(collections.Counter(u["disposition"] for u in units if u["assignment"] == "shared")), "identity_checks": "All assigned raw/range hashes and all selected pinned-file normalized identities matched; hashes do not supply semantic coverage", "unread_paths": [p for p in packet["paths"] if pathlib.Path(p).name in unread], "new_sdk_builds": 0, "new_sdk_test_executions": 0, "new_native_device_executions": 0, "accepted_core_ids_allocated": 0, "next": "Read two complete fixture files; finish narrow dependency/history/original ISO/oracle provenance and per-unit candidate triage; retain bounded GP-final gaps; coordinator owns execution and acceptance."}
save("unit-dispositions.json", units)
save("clean-nominees.json", [{**u, "nomination_reason": clean_nominee_ids[u["id"]], "exclude_if_later_edited": True, "sampled": False} for u in units if u["id"] in clean_nominee_ids])
save("file-dispositions.json", list(files.values()))
save("checkpoint-summary.json", summary)
save("unchanged-paths.json", [{"path": f["path"], "source_sha256": f["source_sha256"], "disposition": f["product_disposition"], "candidate_context_ids": sorted({c for u in units if u["path"] == f["path"] for c in u["candidate_context_ids"]}), "future_owner": "B6 fresh editor" if f["primary"] else "SHARED fresh editor", "reopening_map": "candidates.md; exact local edit ranges await coordinator triage"} for f in files.values()])
print(json.dumps(summary, indent=2))
