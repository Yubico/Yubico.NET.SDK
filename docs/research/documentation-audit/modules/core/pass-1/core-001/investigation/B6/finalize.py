"""Serialize completed B6 semantic decisions and independent static data-review evidence.

Authored anchors localize concerns from complete-file readings. Hashes prove identity only.
The immutable 48-file checkpoint remains under history/ and is never executed or rewritten.
"""
import collections
import hashlib
import json
import pathlib
import re
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(parent for parent in HERE.parents if (parent / ".git").exists())
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"

def load(path):
    return json.loads(path.read_text())

def sha(data):
    return hashlib.sha256(data).hexdigest()

def save(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2) + "\n")

packet = load(RUN / "inventory/packets/B6-wire-scp.json")
inventory = {u["id"]: u for u in load(RUN / "inventory/semantic-units.json")}
previous = {u["id"]: u for u in load(HERE / "history/checkpoint-48-files/unit-dispositions.json")}
old_nominees = {u["id"]: u for u in load(HERE / "history/checkpoint-48-files/clean-nominees.json")}
notes = load(HERE / "semantic-notes.json")
anchors = load(HERE / "final-annotations.json")
assert set(anchors) == set(notes) == {pathlib.Path(p).name for p in packet["paths"]}
assert len(load(HERE / "vector-review.json")["all_records"]) == 100
primary = set(packet["unit_ids"])
shared = set(packet["shared_read_unit_ids"])
assert len(primary) == 668 and len(shared) == 16 and not primary & shared

files, units, anchor_usage = {}, [], collections.Counter()
for uid in packet["unit_ids"] + packet["shared_read_unit_ids"]:
    original = inventory[uid]
    path = original["path"]
    raw = (ROOT / path).read_bytes()
    text = raw.decode("utf-8-sig")
    start, end = original["start_character"], original["end_character_exclusive"]
    body = text[start:end]
    assert sha(raw) == original["source_sha256"], path
    assert sha(body.encode()) == original["range_sha256"], uid
    if path not in files:
        pinned = subprocess.check_output(["git", "show", f"{PIN}:{path}"], cwd=ROOT)
        assert raw == pinned or raw.replace(b"\r\n", b"\n") == pinned, path
        files[path] = {"path": path, "source_sha256": sha(raw), "pinned_git_blob": subprocess.check_output(["git", "rev-parse", f"{PIN}:{path}"], cwd=ROOT, text=True).strip(), "pinned_source_sha256": sha(pinned), "pin_equality": "exact" if raw == pinned else "LF-normalized-equal", "primary": uid in primary, "lines": len(text.splitlines()), "assigned_unit_ids": [], "read_scope": "complete-file" if uid in primary else "complete-assigned-ranges-with-relevant-context"}
    files[path]["assigned_unit_ids"].append(uid)
    name = pathlib.Path(path).name
    local = []
    if uid in primary:
        line_offsets, offset = {}, 0
        for number, line in enumerate(text.splitlines(keepends=True), 1):
            line_offsets[number] = offset + len(line) - len(line.lstrip())
            offset += len(line)
        for line, candidates, reason in anchors[name]:
            position = line_offsets[line]
            if start <= position < end:
                local.append({"line": line, "candidate_ids": [f"CORE-B6-C{c:03}" for c in candidates], "reason": reason})
                anchor_usage[(name, line)] += 1
        if local:
            disposition = "READ-CANDIDATE"
            reason = " ".join(dict.fromkeys(a["reason"] for a in local))
        elif name.startswith("ansx963_"):
            disposition = "READ-CLEAN" if name.endswith(".rsp") else "READ-CONTEXT"
            reason = notes[name] + " Completed structured data review is in vector-review.json; original publication-byte gap is a qualification, not an unread state."
        elif original["category"] in ["source-context-and-explanations", "type-contract"]:
            disposition = "READ-CLEAN" if "///" in body else "READ-CONTEXT"
            reason = ("Retain accurate local type/ownership explanation: " if disposition == "READ-CLEAN" else "Retain structural/import/type context without treating it as a separate implemented feature or clean sampling substitute: ") + original["label"] + ". " + notes[name]
        else:
            declaration = original["label"].split(" :: ")[-1]
            meaningful = "(" in declaration and len(body.splitlines()) >= 7 and "private readonly" not in declaration and "const " not in declaration
            disposition = "READ-CLEAN" if meaningful or "///" in body else "READ-CONTEXT"
            reason = ("Complete unit retained after localized concern review: " if disposition == "READ-CLEAN" else "Final unchanged declaration/helper context, with no independent correction proposed: ") + declaration + ". " + notes[name]
    else:
        old = previous[uid]
        disposition = "READ-CANDIDATE" if old["candidate_context_ids"] else "READ-HISTORICAL-CONTEXT" if "changelog" in path else "READ-CLEAN"
        reason = old["observation"]
        if uid.endswith("a196d3416b0b4e9d"):
            reason += " Final disposition: reopen parity/current-field claims through C007/C009/C010/C015; B5 owns current field retention, and the historical comparison supplies no conformance oracle."
        if uid.endswith("2637633b642ae69c"):
            reason += " Final actor boundary: B6 accepts its APDU/recovery scope only; native paragraphs retain completed B1/B2/B3 and pinned B4 ownership. No platform paragraph is upgraded by this source read."
    direct_ids = sorted({c for a in local for c in a["candidate_ids"]}) if uid in primary else previous[uid]["candidate_context_ids"]
    if uid in old_nominees:
        assert not direct_ids, (uid, direct_ids)
        disposition = "READ-CLEAN"
        reason = old_nominees[uid]["nomination_reason"] + " Retained exact source/assertion scope; no new execution or upstream-copy erasure claim."
    record = {k: original[k] for k in ["id", "path", "source_sha256", "range_sha256", "start_character", "end_character_exclusive", "first_line", "last_line", "category", "label"]}
    declarations = [line.strip() for line in body.splitlines() if re.search(r"\bAssert\.|return |=>|\bconst\b|\breadonly\b", line)]
    record.update(assignment="primary" if uid in primary else "shared", disposition=disposition, semantic_review="COMPLETE-FOR-ASSIGNED-SCOPE", reason=reason, candidate_ids=direct_ids, localized_evidence=local, actual_source_oracle_lines=declarations[:12], unchanged_decision="Preserve product bytes. Route only listed candidate anchors to fresh-editor triage; retain clean/context units unless later behavioral changes reopen them.", review_status="INDEPENDENT-REVIEW-NOT-YET-ASSIGNED", validation="NO-NEW-SDK-EXECUTION", sample_status="NOMINATED-UNSAMPLED" if uid in old_nominees else "NOT-NOMINATED", current_source_sha256=sha(raw), current_range_sha256=sha(body.encode()), identity_verified=True)
    units.append(record)

for name, entries in anchors.items():
    for line, _, _ in entries:
        assert anchor_usage[(name, line)] == 1, (name, line, anchor_usage[(name, line)])
assert len(units) == 684
allowed = {"READ-CANDIDATE", "READ-CLEAN", "READ-CONTEXT", "READ-HISTORICAL-CONTEXT"}
assert all(u["disposition"] in allowed and u["semantic_review"] == "COMPLETE-FOR-ASSIGNED-SCOPE" for u in units)

for path, file in files.items():
    selected = [u for u in units if u["path"] == path]
    file.update(dispositions=dict(collections.Counter(u["disposition"] for u in selected)), observation=notes[pathlib.Path(path).name] if file["primary"] else " ".join(dict.fromkeys(u["reason"] for u in selected)), product_disposition="UNCHANGED; finalized source-scope decisions. Candidate anchors require coordinator triage and fresh editor/reviewer; related clean/context paths are explicitly retained.")

nominees = [u for u in units if u["id"] in old_nominees]
# Additional meaningful exact-byte and no-replay oracles, deliberately nominated by named method.
extra = {
    "ShortFormatter_WithExplicitLe_UsesCallerLe": "Exact short command bytes and explicit Le are independently spelled out in the test.",
    "ExtendedFormatter_WithoutDataAndExplicitLe_UsesExtendedCallerLe": "Exact seven-byte Case2E output and0100 Le oracle; no absent-Le inference.",
    "ExtendedFormatter_WithLargePayloadAndNoLe_AppendsExtendedZeroLe": "300-byte payload, explicit012C length, header and two trailing zero bytes checked in full.",
    "YESDK1631_ShortResponse_RefusesNextExchangeWithoutReplay": "BadResponse failure identity is retained as next refusal inner exception and one actual fake transmit is asserted.",
    "GetSharedSecret_WhenSecondPrivateKeyExportFails_ZeroesFirstPrivateValue": "Observer retains exact first exportedD and asserts every bytezero after injected second export failure.",
    "InvalidProtectedResponseMac_RefusesNextExchangeWithoutReplay": "Malformed response triggers WrongMAC, identical cause in base/wrapper refusal and one fake transmit; no device claim.",
    "X963Kdf_Sha256_EmptySharedInfo_128BitOutput_MatchesNistVectors": "All ten actual embedded tuples equal the complete fixture records and full derived-byte equality is asserted; archive provenance qualified.",
    "X963Kdf_Sha256_WithSharedInfo_1024BitOutput_MatchesNistVectors": "Ten nonemptySharedInfo128-byte outputs compare full bytes; fixture transcripts and four-block concatenation were inspected independently of execution."
}
for u in units:
    for method, reason in extra.items():
        if f"{method}(" in u["label"]:
            assert u["disposition"] == "READ-CLEAN", (method, u["disposition"])
            u["sample_status"] = "NOMINATED-UNSAMPLED"
            nominees.append({**u, "nomination_reason": reason})
assert len(nominees) == 16
for n in nominees:
    n["exclude_if_later_edited"] = True
    n["sampled"] = False

candidate_index = []
for number in range(1, 17):
    cid = f"CORE-B6-C{number:03}"
    affected = [u for u in units if cid in u["candidate_ids"]]
    assert affected, cid
    candidate_index.append({"id": cid, "status": "LOCAL-CANDIDATE-NOT-ACCEPTED", "definition": "candidates.md", "direct_unit_ids": [u["id"] for u in affected], "direct_paths": sorted({u["path"] for u in affected}), "documentation": "CANDIDATE; exact correction/qualification needs coordinator triage and fresh edit/review", "implementation": "SOURCE-ONLY; unchanged; no fix authorized", "decision": "Target-profile/API/lifetime choices remain explicit in candidate definition, not implied intended policy", "qualification": "Original edition/source and platform/device bounds retained; no new execution", "counting": "One stable local concern group; unit/path occurrences are nonadditive"})

unchanged = []
for file in files.values():
    selected = [u for u in units if u["path"] == file["path"]]
    unchanged.append({"path": file["path"], "source_sha256": file["source_sha256"], "disposition": "RETAIN-UNCHANGED", "reason": "Direct candidate units route to fresh editor; complete neighboring clean/helper/context units remain unchanged after scoped review. Original fixture bytes stay intact.", "candidate_ids": sorted({c for u in selected for c in u["candidate_ids"]}), "retained_unit_ids": [u["id"] for u in selected if not u["candidate_ids"]], "future_owner": "B6 fresh editor" if file["primary"] else "SHARED fresh editor", "reopening_trigger": "Accepted listed concern, implementation/profile/ownership change or supplied original source that changes the scoped qualification; use candidates.md and B5-C001/C004/C005/C006/C008/C009 handoff rather than re-auditing B5."})

dependencies = {
    "docs/MEMORY-MANAGEMENT.md": ("C / SHARED", [2,7], "Reopen copy/source/returned-owner cleanup rules only; complete document disposition remains C-owned."),
    "docs/LOGGING.md": ("C / SHARED", [14], "C owns logging architecture; B6 supplies actual APDU/header/AID/exception provenance."),
    "src/Core/src/Sessions/ApplicationSession.cs": ("B5", [4,5,7,15], "Narrow readiness/public-auth and setup/disposal ranges read; B5-C001/C006 own whole lifecycle/workflow."),
    "src/Core/src/Sessions/RawSmartCardSession.cs": ("B5", [1,3,7,15], "Complete dependency class read; B5 owns primary disposition, B6 supplies actual SCP select/recovery/wire behavior."),
    "src/Core/src/Sessions/SessionCreationOptions.cs": ("B5", [7,15], "Retained versus creation-only parameter boundary uses B5-C001; no fresh whole-carrier audit claimed."),
    "src/Core/src/Devices/YubiKeyConnectionExtensions.cs": ("B1 / B5", [7,15], "Public raw factory grammar and hidden connection ownership remain B1/B5; compile prepared tasks later."),
    "src/Core/src/Cryptography/ECPrivateKey.cs": ("C", [7,9,13], "Only clone/clear/CreateFromValue/ToECDiffieHellman dependencies read; other crypto findings remain C."),
    "src/Core/src/Cryptography/ECPublicKey.cs": ("C", [7,9], "Only public wrapper copy/Parameters dependencies read; read-only properties are not deep immutability."),
    "src/Core/src/Cryptography/AsnUtilities.cs": ("C", [9,13], "Narrow fixed scalar/coordinate admission read; corrected32-byte test literal supplies no failure finding."),
    "src/Core/src/Cryptography/X963Kdf.cs": ("C", [6,9,13], "Complete77-line dependency helper read for actual algorithm/order/truncation; secret digest cleanup remains C-owned."),
    "src/SecurityDomain/README.md": ("BOUNDARY-SecurityDomain / SHARED", [6,7,9,10,15], "Trust role, retained parameters, authority, reset and exact final/draft applicability remain applet-boundary scope."),
    "src/SecurityDomain/CLAUDE.md": ("BOUNDARY-SecurityDomain / SHARED", [6,7,9,10,15], "Contributor role/setup/ownership explanations reopen with accepted Core mechanism; preserve reset physical gap."),
    "src/SecurityDomain/src/SecurityDomainSession.cs": ("BOUNDARY-SecurityDomain", [7,8,9,15], "Only constructor/factory/parameter retention and GetCertificates partial cleanup read narrowly; no whole applet disposition."),
    "src/SecurityDomain/src/ISecurityDomainSession.cs": ("BOUNDARY-SecurityDomain", [9,10,15], "Public authority/provisioning/result XML reopening referral; no B6 coverage added."),
    "src/SecurityDomain/src/IYubiKeyExtensions.cs": ("BOUNDARY-SecurityDomain", [7,15], "Actual session factory method read for prepared complete tasks; remaining extensions retain boundary ownership."),
    "src/Core/tests/CLAUDE.md": ("SHARED / B5", [13], "Actual fake names are EnqueueResponse/TransmittedCommands; fix instructional fragments through single editor."),
    "src/Piv/README.md": ("BOUNDARY-Piv", [2,7,15], "PIVCORE-003 complete result/credential scopes are boundary-owned; retain physical deferral."),
    "src/Piv/CLAUDE.md": ("BOUNDARY-Piv", [2,7,15], "Dependent ownership/setup guidance referral only, not a whole-module read."),
    "src/YubiHsm/README.md": ("BOUNDARY-YubiHsm", [2,7], "HSM-DEP-003 response/TLV ownership is distinct from applet profile HSM-IMP-008."),
    "src/YubiHsm/CLAUDE.md": ("BOUNDARY-YubiHsm", [2,7], "Dependent copied-result/input cleanup explanation referral; no duplicate Core parser finding.")
}
for path, (owner, concerns, reason) in dependencies.items():
    raw = (ROOT / path).read_bytes()
    pinned = subprocess.check_output(["git", "show", f"{PIN}:{path}"], cwd=ROOT)
    assert raw == pinned or raw.replace(b"\r\n", b"\n") == pinned, path
    unchanged.append({"path": path, "source_sha256": sha(raw), "disposition": "RETAIN-DEPENDENCY-UNCHANGED", "assigned_B6_units": [], "coverage": "Dependency referral only; zero added B6 units", "reason": reason, "candidate_ids": [f"CORE-B6-C{n:03}" for n in concerns], "future_owner": owner, "reopening_trigger": "Accepted related Core concern or changed caller/ownership/profile contract; primary owner performs its own complete call-site review."})

counts = dict(collections.Counter(u["disposition"] for u in units))
summary = {"source_pin": PIN, "observed_metadata_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(), "state": "SOURCE-INVESTIGATION-COMPLETE", "primary_files": 50, "primary_lines": 9441, "primary_unit_count": 668, "shared_unit_count": 16, "total_assigned_ids": 684, "unit_dispositions": counts, "primary_dispositions": dict(collections.Counter(u["disposition"] for u in units if u["assignment"] == "primary")), "shared_dispositions": dict(collections.Counter(u["disposition"] for u in units if u["assignment"] == "shared")), "unread": 0, "awaiting_context_or_final_triage": 0, "local_concern_groups": 16, "clean_nominees": len(nominees), "accepted_core_ids_allocated": 0, "inherited_keys": 8, "historical_checkpoint": "history/checkpoint-48-files/manifest.json", "qualifications": "Bounded unavailable originals and later independent review/compilation/runtime/platform/device gates are separate from completed source reading.", "new_sdk_execution_count": 0, "writes": "B6 records only"}
save("unit-dispositions.json", units)
save("file-dispositions.json", list(files.values()))
save("candidate-index.json", candidate_index)
save("clean-nominees.json", nominees)
save("unchanged-paths.json", unchanged)
save("final-summary.json", summary)

sources = []
for manifest_name in ["initial-download-manifest.json", "focused-followup-manifest.json", "completion-initial-manifest.json", "completion-followup-manifest.json"]:
    path = HERE / "sources" / manifest_name
    for entry in load(path):
        record = {**entry, "capture_manifest": str(path.relative_to(HERE))}
        if "sha256" in entry:
            target = HERE / "sources" / entry["name"]
            assert sha(target.read_bytes()) == entry["sha256"]
            record["retained_path"] = str(target.relative_to(ROOT))
            record["byte_exact_original"] = True
        else:
            record["retained_path"] = None
            record["qualification"] = "Unavailable raw original after bounded research; extracted text/metadata is a separate evidence form."
        sources.append(record)
for name in ["scp03-1.1.2.txt", "scp11-public-review-1.3.0.13.txt"]:
    target = HERE / "sources" / name
    sources.append({"retained_path": str(target.relative_to(ROOT)), "sha256": sha(target.read_bytes()), "role": "pdftotext -layout reading derivative", "byte_exact_original": False})
for relative in ["sources/python/manifest.json", "sources/python/snapshot/yubikit/core/smartcard/__init__.py", "sources/python/snapshot/yubikit/core/smartcard/scp.py", "sources/python/snapshot/tests/device/test_scp.py", "investigation/C/sources/final-source-register.md", "investigation/C/sources/rfc4493.txt", "investigation-B5.md", "investigation/B5/candidates.md"]:
    target = RUN / relative
    sources.append({"retained_path": str(target.relative_to(ROOT)), "sha256": sha(target.read_bytes()), "role": "reused pinned source/evidence; read scope is explicit in completion-evidence.md or ownership-and-handoffs.md"})
patch_manifest = load(RUN / "sources/python/history/patch-manifest.json")
selected = {"652e5825a2ecea8f9b4e3c55da6bf7b802594263", "e55c9c2f37c48b0c6d7eaa5eff7794d58adf9b8c", "8706337cc0682ccffd6baae6563e02d4bf993036", "7fd50da5db6f844f49947ce888c8ffab9b8e4eff", "a10d3b3b249c6dcdba3691824e24758d40994647"}
for entry in patch_manifest:
    if entry["revision"] in selected:
        target = RUN / "sources/python/history" / (entry["revision"] + ".patch")
        assert sha(target.read_bytes()) == entry["sha256"]
        sources.append({**entry, "retained_path": str(target.relative_to(ROOT)), "read_scope": "Exact selected full file diffs listed in completion-evidence.md; other applet/CLI hunks excluded from claims"})
save("sources/final-manifest.json", {"state": "FINAL-BOUNDED-SOURCE-REGISTER", "sources": sources, "raw_originals_unchanged": True, "normative_status": "See final-source-register.md; draft/final/vendor/API/oracle roles are distinct"})
print(json.dumps(summary, indent=2))
