"""Emit completed static Packet C dispositions from explicit investigator annotations.

This verifies source identities and record joins. It executes no SDK/test/crypto code.
Semantic conclusions come from the complete reads and authored semantic/final notes,
not from this program loading or hashing input bytes.
"""

from collections import Counter
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[9]
RUN = ROOT / "docs/research/documentation-audit/modules/core/pass-1/core-001"
OWN = RUN / "investigation/C"
BASELINE = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def load(path):
    return json.loads(path.read_text())


def sha(data):
    return hashlib.sha256(data).hexdigest()


def save(name, value):
    (OWN / name).write_text(json.dumps(value, indent=2) + "\n")


def main():
    packet = load(RUN / "inventory/packets/C.json")
    units = {unit["id"]:unit for unit in load(RUN / "inventory/semantic-units.json")}
    files = {item["path"]:item for item in load(RUN / "inventory/files.json")}
    notes = load(OWN / "semantic-notes.json")
    review = load(OWN / "final-review.json")
    provenance = load(OWN / "checkpoint-provenance.json")
    findings = load(OWN / "findings.json")
    primary = set(packet["unit_ids"])
    shared = set(packet["shared_read_unit_ids"])
    assert not primary & shared
    assert len(primary) == 1386 and len(shared) == 176
    ids = [*packet["unit_ids"], *packet["shared_read_unit_ids"]]
    paths = {units[unit_id]["path"] for unit_id in ids}
    assert len(packet["paths"]) == 104
    assert set(review["candidate_addenda"]) == {item["id"] for item in findings["candidates"]}

    previous_report = RUN / "investigation-C.md"
    prior_copy = OWN / "prior-investigation-C.md"
    if not prior_copy.exists():
        raw = previous_report.read_bytes()
        assert sha(raw) == provenance["prior_identities"]["investigation-C.md"]
        prior_copy.write_bytes(raw)

    # Explicit continuation completion: all nine previously named tests and each
    # of the 110 formerly unread shared IDs were actually read with helpers.
    source = {}
    dossiers = []
    for path in sorted(paths):
        raw = (ROOT / path).read_bytes()
        text = raw.decode("utf-8-sig")
        source[path] = (raw, text)
        note_key = path if path in notes else Path(path).name
        assert note_key in notes, path
        assigned = [units[unit_id] for unit_id in ids if units[unit_id]["path"] == path]
        if path in packet["paths"]:
            pinned = subprocess.check_output(["git", "show", f"{BASELINE}:{path}"], cwd=ROOT)
            assert text.replace("\r\n", "\n") == pinned.decode("utf-8-sig").replace("\r\n", "\n"), path
        dossiers.append({
            "path":path, "source_sha256":sha(raw), "pinned_git_blob":files[path].get("git_blob"),
            "assignment":"primary" if path in packet["paths"] else "shared-read",
            "complete_primary_file_read":path in packet["paths"],
            "assigned_semantic_ranges_read":[{"id":unit["id"],"first_line":unit["first_line"],
                                            "last_line":unit["last_line"],"range_sha256":unit["range_sha256"]}
                                           for unit in assigned],
            "semantic_observation":notes[note_key],
            "execution":"NOT-RUN", "independent_review":"PENDING-FRESH-REVIEW",
        })

    for item in findings["candidates"]:
        additions = review["additional_locations"].get(item["id"], [])
        existing = {(loc["path"],loc["first"],loc["last"]) for loc in item["locations"]}
        item["locations"].extend(loc for loc in additions if (loc["path"],loc["first"],loc["last"]) not in existing)
        for loc in item["locations"]:
            assert 1 <= loc["first"] <= loc["last"] <= len((ROOT / loc["path"]).read_text(encoding="utf-8-sig").splitlines()), loc
        item["final_observation"] = review["candidate_addenda"][item["id"]]
        if item["id"] in {"CORE-C-C005","CORE-C-C006","CORE-C-C013","CORE-C-C017"}:
            item["ownership_qualification"] = review["ownership_qualification"]
        if item["id"] == "CORE-C-C014":
            item["observer_scope_qualification"] = review["test_hook_qualification"]
        if item["id"] == "CORE-C-C013":
            item["allocation_failure_qualification"] = review["allocation_failure_qualification"]
        item["documentation_state"] = "PROPOSED-CORRECTION-OR-QUALIFICATION; fresh editor/reviewer assignment required"
        item["implementation_state"] = "UNCHANGED; source concerns remain candidates, not fixed/intended-contract declarations"
        item["decision_state"] = "MAINTAINER-DECISION-REQUIRED" if item["id"] in {
            "CORE-C-C003","CORE-C-C007","CORE-C-C010","CORE-C-C016","CORE-C-C020"} else "No new policy authorized"
        item["qualification_state"] = "STATIC-SOURCE-ONLY; SDK/host-crypto/native/device execution NOT-RUN"
        item["independent_review"] = "PENDING-FRESH-REVIEW"
        item["counting"] = "Local related concern group; inherited associations are not additive bugs"
    findings["status"] = "Completed static investigator return; 20 provisional local groups, zero accepted allocations"
    findings["completion_date_session"] = "2026-11-12"
    findings["qualification_register"] = "final-review.json"
    save("findings.json", findings)

    historical = {"RsaFormat.cs","CoseKeyOperations.cs","DisposableTlvDictionary.cs"}
    historical_shared = {"2026-09-21-yubikit-async-boundaries-ISA.md","docs/migration/v1-to-v2-changelog.md"}
    boundary_shared = {"docs/architecture/webauthn-extension-processing.md","docs/migration/v1-to-v2-map.yml",
                       "docs/migration/v1-to-v2.md"}
    all_candidate_paths = {loc["path"] for finding in findings["candidates"] for loc in finding["locations"]} | {
        path for finding in findings["candidates"] for path in finding["reopen"]}
    rows = []
    for unit_id in ids:
        unit = units[unit_id]
        path = unit["path"]
        raw, text = source[path]
        assert sha(raw) == unit["source_sha256"], (unit_id,"whole-file drift")
        body = text[unit["start_character"]:unit["end_character_exclusive"]]
        assert sha(body.encode()) == unit["range_sha256"], (unit_id,"range drift")
        note_key = path if path in notes else Path(path).name
        direct = [finding["id"] for finding in findings["candidates"] if any(
            loc["path"] == path and loc["first"] <= unit["last_line"] and loc["last"] >= unit["first_line"]
            for loc in finding["locations"])]
        related = [finding["id"] for finding in findings["candidates"] if
                   path in finding["reopen"] or any(loc["path"] == path for loc in finding["locations"])]
        if Path(path).name in historical:
            status = "READ-HISTORICAL-EXCLUSION"
            decision = "Preserve commented historical source; no active API/runtime claim."
        elif direct:
            status = "READ-CANDIDATE"
            decision = "Full unit inspected; location intersects the named local concern(s). Product remains unchanged."
        elif path in historical_shared:
            status = "READ-HISTORICAL-CONTEXT"
            decision = "Preserve dated/aspirational provenance; not a current guarantee or fresh execution result."
        elif path in all_candidate_paths:
            status = "READ-CANDIDATE-CONTEXT"
            decision = "Full unit inspected and retained unchanged as related context; fresh editor decides exact local changes."
        elif path in boundary_shared:
            status = "READ-BOUNDARY-QUALIFIED"
            decision = "Assigned complete unit and narrow callers inspected; applet/profile qualification referred to its primary boundary owner."
        else:
            status = "READ-CLEAN"
            decision = "No required correction found in this exact unit for the inspected source scope; independent review remains separate."
        substantive = unit["last_line"] - unit["first_line"] + 1 >= 8
        method_or_narrative = (unit["category"] == "narrative-section-with-complete-examples" or
                               unit["category"] == "member-contract-and-implementation" and "(" in unit["label"])
        trivial_guard = any(marker in unit["label"] for marker in [
            "_ZeroSize_", "_NegativeSize_", "_ThrowsForEmpty", "_RejectsBlankIntent", "Memory_ThrowsAfterDispose",
            "_MissingX_", "_MissingY_", "_MissingModulus_", "_MissingExponent_", "_DIsNull_", "_UnsupportedKeyType_"])
        rows.append({
            "id":unit_id, "path":path, "first_line":unit["first_line"], "last_line":unit["last_line"],
            "start_character":unit["start_character"], "end_character_exclusive":unit["end_character_exclusive"],
            "source_sha256":unit["source_sha256"], "range_sha256":unit["range_sha256"],
            "label":unit["label"], "category":unit["category"], "features":unit["features"],
            "assignment":"primary" if unit_id in primary else "shared-read", "status":status,
            "read_complete":True, "candidate_ids":direct, "associated_candidate_ids":related,
            "reason":decision + " " + notes[note_key],
            "evidence_scope":"Complete assigned unit, implementation/assertion and helper context; no runtime execution",
            "source_qualifications":"sources/final-source-register.md; final-review.json",
            "execution":"NOT-RUN", "independent_review":"PENDING-FRESH-REVIEW",
            "sample_eligible":status == "READ-CLEAN" and substantive and method_or_narrative and not trivial_guard,
            "sampling_limit":"Namespace/import/enum/accessor/trivial-member populations do not substitute for substantive clean methods/tests."
        })
    assert len(rows) == len({row["id"] for row in rows}) == 1562
    assert all(row["read_complete"] and "PENDING-FINAL-TRIAGE" not in row["status"] and row["status"] != "UNREAD" for row in rows)
    save("unit-dispositions.json", rows)
    save("file-dispositions.json", dossiers)
    save("clean-nominees.json", [row for row in rows if row["sample_eligible"]])

    dependencies = []
    for finding in findings["candidates"]:
        for path in sorted({loc["path"] for loc in finding["locations"]} | set(finding["reopen"])):
            target = ROOT / path
            assert target.is_file(), path
            owner = files.get(path,{}).get("assigned_family", "boundary/shared owner to reconcile")
            direct = any(loc["path"] == path for loc in finding["locations"])
            dependencies.append({
                "candidate_id":finding["id"], "path":path, "source_sha256":sha(target.read_bytes()), "exists":True,
                "editor_owner":files.get(path,{}).get("future_editor_owner",owner),
                "disposition":"UNCHANGED-REFER-FRESH-EDITOR" if direct else "UNCHANGED-REOPEN-DEPENDENCY",
                "investigator_decision":"Preserve current product bytes; source-backed correction/qualification supplied with exact locations."
                    if direct else "Retain as an explicit dependent claim/caller task for its assigned boundary/shared editor; no full-file clean claim.",
                "read_scope":"all primary units and file context" if path in packet["paths"] else
                    "exact assigned shared units plus recorded helper context" if path in paths else
                    "dependency-only; primary owner must perform its full affected-unit review",
                "later_editor_question":finding["title"],
                "reopening_trigger":"Candidate acceptance, later behavior/profile/ownership change or new provider/source evidence.",
                "review_requirement":"Fresh affected-unit/editor and independent review; source concerns are not intended contracts."
            })
    save("unchanged-paths.json", dependencies)

    # Keep every original registry identity/state; add C's scoped recommendation
    # locally rather than modifying the inherited registry owner's records.
    group_outcomes = {
        "base32-failure-cleanup":("SOURCE-CONFIRMED-FOR-EXACT-SCOPE", "Complete Core codec/tests and narrow CredentialData.ParseUri/CLI delegation inspected. Disjoint failed output clears; overlap admission preserves source/destination. URI temporary cleanup is finally/delegation evidence, not directly observed abandoned storage."),
        "base32-shape":("SOURCE-CONFIRMED-FOR-EXACT-SCOPE", "Exact terminal-count/padding rules and independent RFC literals checked. Core permits empty material and noncanonical pad bits; OATH boundaries reject empty. Existing accepted local permissiveness is preserved."),
        "credential-confirm-cleanup":("SOURCE-CONFIRMED-FOR-EXACT-SCOPE", "Both entries stay under finally and only the matched first owner transfers. Old cleanup defect is distinct from stale before-second-prompt XML and the pool test's reuse assumption."),
        "credential-length-units":("DECISION-AND-BOUNDARY-QUALIFIED", "Core counts UTF-16 char units, prompt context carries code-point/byte units and current CTAP preset already documents its limitation. Unicode/device-minimum/final-NUL/profile enforcement belongs to a maintainer decision and each applet boundary; no silent normalization or generic PIN policy adopted."),
        "credential-redirect-policy":("SOURCE-CONFIRMED-WITH-SEPARATE-RESIDUALS", "Redirected text now validates policy and confirmation uses lines; hex, immutable string, cancellation and Unicode questions are separate current concerns. N3 CLI.Commands scan-code assertions/helper read; later standalone OtpTool behavior remains YubiOTP-owned and physical keyboard-output history is not rerun."),
        "ctap-crypto-cleanup":("BOUNDARY-REFERRAL-WITH-CORE-EVIDENCE", "C inspected Core AES/ARKG/encoding secret-copy/failure scopes. CTAP PIN, blob and extension salt/profile implementations retain their original boundary owner and unexecuted qualification; Core array cleanup cannot close all applet-owned outputs."),
        "logging-payload-policy":("SOURCE-CONFIRMED-QUALIFICATION-AND-B6-REFERRAL", "Global factory is not a redactor; static logger retention and application exception provenance were inspected. Actual formatter/SD templates remain B6/SD boundary primary work. Explicit CLI provisioning output is distinct from SDK logging."),
        "private-key-consistency":("SOURCE-GUARDS-CONFIRMED-WITH-RESIDUAL-ADMISSION", "RSA selected relations and provider-backed EC mismatch checks are present on parameter/typed-factory paths. Literal RFC vectors have stable byte widths; DP-based metadata sizing and minimal fresh perturbed components can reject before the desired check. No passing rerun or full mathematical/native validation claimed."),
        "runtime-hkdf-substitution":("SOURCE-CONFIRMED-FOR-EXACT-SCOPE", "Removed custom byte-counter wrap mechanism is absent; current C ARKG uses runtime HKDF with 32-byte outputs. X9.63 is a separate counter KDF, and runtime secrecy/profile correctness still require their own qualification."),
        "runtime-rng-substitution":("SOURCE-CONFIRMED-FOR-EXACT-SCOPE", "Exact removal commit 78c44513 and parent helper/repro read. Wide range could skip RNG consumption; parent GetInt32/GetByte search finds helper self-calls and its repro, not a shipping wide-range caller. Runtime replacement is not a statistical/all-provider randomness proof."),
        "runtime-scalar-copy":("REFERENCE-PATH-QUALIFIED; ACTUAL-RUNTIME-GAP", "SDK temporary D cleanup is explicit. .NET v10.0.0 absent-Q macOS extraction introduces a replacement managed D; supplied-Q import skips that extraction in inspected source. Narrow the warning and retain actual installed-runtime/upstream disposition gap without claiming native erasure or all-platform equivalence."),
        "ber-length-safety":("SOURCE-CONFIRMED-FOR-EXACT-SCOPE", "Truncated/reserved/overflowing lengths reject before slicing, and valid redundant long-form length octets survive as permitted by original X690. Current single-item/tag admission and negative BerLength questions are separate from the fixed length defect; preserve #20 and 25e as one nonadditive mechanism."),
        "tlv-encoder-sizing":("SOURCE-CONFIRMED-FOR-EXACT-SCOPE", "EncodeDictionary builds TLVs, sums actual TotalLength and clears temporary TLVs in finally. Literal two-byte-tag and long-length tests inspect exact complete bytes. Aggregate overflow, source/result ownership and decode-copy cleanup are separate concerns.")
    }
    registry_path = RUN / "inherited-findings/registry.json"
    registry = load(registry_path)
    expected_keys = set(load(RUN / "inventory/inherited-packet-inputs.json")["C"]["provenance_keys"])
    inherited = []
    for row in registry["rows"]:
        if row["key"] not in expected_keys:
            continue
        outcome, observation = group_outcomes[row["dedup_group"]]
        inherited.append({"key":row["key"],"group":row["dedup_group"],
                          "registry_state_preserved":row["current_state"],"C_recommendation":outcome,
                          "C_observation":observation,"execution":"NOT-RUN",
                          "counting":"Original provenance identity; association is not a new independent bug",
                          "registry_owner_action":"Reconcile C's bounded evidence; authoritative registry remains unchanged by C."})
    assert {row["key"] for row in inherited} == expected_keys
    save("inherited-dispositions.json", {"registry_sha256":sha(registry_path.read_bytes()),
        "provenance_rows":len(inherited),"nonadditive_groups":len({row["group"] for row in inherited}),"rows":inherited})

    summary = {
        "baseline":BASELINE, "observed_head":subprocess.check_output(["git","rev-parse","HEAD"],cwd=ROOT,text=True).strip(),
        "completion_date_session":"2026-11-12", "completion":"COMPLETE-STATIC-INVESTIGATION; editor/reviewer/acceptance/execution gates remain separate",
        "packet_sha256":sha((RUN / "inventory/packets/C.json").read_bytes()),
        "inventory_sha256":sha((RUN / "inventory/semantic-units.json").read_bytes()),
        "primary_files_read_completely":104, "primary_file_count":104, "primary_lines":sum(files[path]["lines"] for path in packet["paths"]),
        "primary_units_read":len(primary), "shared_units_read":len(shared), "selected_source_and_range_checks":len(rows),
        "shared_prose_count":174, "shared_assignment_count":176,
        "count_discrepancy":"All 176 actual JSON IDs retained and read. Inventory owner reconciles prose; C changed no central denominator.",
        "unit_status_counts":dict(Counter(row["status"] for row in rows)),
        "unit_status_counts_by_assignment":{assignment:dict(Counter(row["status"] for row in rows if row["assignment"]==assignment)) for assignment in ["primary","shared-read"]},
        "clean_sample_nominees":sum(row["sample_eligible"] for row in rows),
        "inherited_provenance_rows_dispositioned":len(inherited),
        "inherited_nonadditive_groups":len({row["group"] for row in inherited}),
        "local_candidate_groups":len(findings["candidates"]), "accepted_ids_allocated":0,
        "bounded_external_qualifications":list(review["qualified_gaps"]),
        "independent_review":"PENDING; not performed by this investigator", "docs_editing":"NOT-AUTHORIZED-IN-THIS-PACKET",
        "builds_tests_formatters_crypto_experiments_native_device_runs":0, "commits_index_ref_writes_pushes_subagents":0,
        "central_records_changed_by_C":False, "checkpoint_provenance":"checkpoint-provenance.json; prior-investigation-C.md"
    }
    save("final-summary.json", summary)
    print(json.dumps(summary,indent=2))


if __name__ == "__main__":
    main()
