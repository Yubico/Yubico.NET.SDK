"""Records-only checks for the isolated A documentation batch; no SDK execution."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[9]
RUN = ROOT / "docs/research/documentation-audit/modules/core/pass-1/core-001"
OUT = RUN / "editing/A"
BASE = "da66f5d01ba594993974ddd26723e8c300c0a454"
SOURCE = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


def load(path):
    return json.loads((RUN / path).read_text())


def sha(data):
    return hashlib.sha256(data).hexdigest()


def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + "\n")


def noncomments(data):
    # Existing audit's full-line-comment comparison: stronger than token equality for these edits.
    # Retain every non-comment line, including blank lines, inline comments, directives, strings and imports.
    return [line for line in data.splitlines() if not line.lstrip().startswith(b"//")]


assert git("rev-parse", "HEAD").decode().strip() == BASE
run_relative = RUN.relative_to(ROOT).as_posix()
untracked = git("ls-files", "--others", "--exclude-standard").decode().splitlines()
assert all(p.startswith(run_relative + "/editing/A/") or p == run_relative + "/edit-A.md" for p in untracked), untracked
packet = load("inventory/packets/A.json")
allowed = {p for p in packet["paths"] if p.endswith(".cs") and p.startswith("src/Core/")}
changed = git("diff", "--name-only", BASE).decode().splitlines()
assert set(changed) <= allowed, changed
prior = load("investigation/A/file-dispositions.json")
findings = load("investigation/A/findings.json")
units = load("investigation/A/unit-results.json")["units"]
extra = {
    "src/Core/src/Devices/DeviceFlags.cs": ["CORE-A-C002"],
    "src/Core/src/Devices/DiscoveryDeviceInfoReader.cs": ["CORE-A-C008"],
    "src/Core/src/Devices/CompositeDeviceMerger.cs": ["CORE-A-C009", "CORE-A-C010", "CORE-A-C011", "CORE-A-C020"],
    "src/Core/src/Devices/CompositeMetadataReader.cs": ["CORE-A-C008", "CORE-A-C020"],
    "src/Core/src/Devices/WindowsDeviceTopologyResolver.cs": ["CORE-A-C009"],
    "src/Core/src/Devices/WindowsTopologyNativeOps.cs": ["CORE-A-C009"],
    "src/Core/src/Devices/YubiKeyManager.cs": ["CORE-A-C007", "CORE-A-C011", "CORE-A-C012"],
    "src/Core/src/Devices/YubiKeyMonitor.cs": ["CORE-A-C007", "CORE-A-C008", "CORE-A-C012"],
    "src/Core/src/Devices/DeviceChange.cs": ["CORE-A-C007", "CORE-A-C012"],
}
paths = []
xml_checks = []
examples = []
for path in sorted(allowed):
    base = git("show", f"{BASE}:{path}")
    source = git("show", f"{SOURCE}:{path}")
    current = (ROOT / path).read_bytes()
    assert base == source, path
    assert noncomments(base) == noncomments(current), path
    ids = sorted(set(prior.get(path, {}).get("candidate_ids", []) + extra.get(path, [])))
    reason = "Accurate existing wording/assertions retained; no preference-only rewrite."
    if path in changed:
        reason = "Source-supported documentation edit; executable lines and assertions preserved."
    elif ids:
        reason = "Retain accurate existing commentary; stronger runtime/oracle or policy work is held. See edit-A.md."
    paths.append({
        "path": path, "candidate_ids": ids,
        "disposition": "EDIT" if path in changed else "RETAIN",
        "reason": reason,
        "source_pin_sha256": sha(source), "base_sha256": sha(base), "current_sha256": sha(current),
        "noncomment_lines_identical": True,
        "base_final_newline": base.endswith(b"\n"), "current_final_newline": current.endswith(b"\n"),
    })
    text = current.decode()
    if path in changed:
        blocks = list(re.finditer(r"(?:^[ \t]*///[^\n]*(?:\n|$))+", text, re.M))
        for block in blocks:
            xml = "\n".join(re.sub(r"^[ \t]*/// ?", "", line) for line in block.group().splitlines())
            ET.fromstring("<documentation>" + xml + "</documentation>")
        xml_checks.append({"path": path, "xml_blocks_parsed": len(blocks), "well_formed": True})
    for match in re.finditer(r"<code\b[^>]*>(.*?)</code>", text, re.S):
        body = re.sub(r"^[ \t]*/// ?", "", match.group(1), flags=re.M).strip("\n")
        examples.append({
            "path": path,
            "opening_tag_line": text[:match.start()].count("\n") + 1,
            "closing_tag_line": text[:match.end()].count("\n") + 1,
            "body_first_line": text[:match.start(1)].count("\n") + 2,
            "body_last_line": text[:match.end(1)].count("\n"),
            "body_sha256": sha(body.encode()), "body": body,
            "classification": "COMPLETE-METHOD" if path.endswith("YubiKeyManager.cs") else "ILLUSTRATIVE-INTERNAL-EXPRESSIONS",
            "compiler_status": "VALID-NOT-CHECKED",
            "execution_status": "NOT-RUN",
            "ownership": "await using owns monitor; token is a typed method input" if path.endswith("YubiKeyManager.cs") else "value-only internal extension expressions; no resource acquired",
        })
save("path-dispositions.json", paths)
save("changed-path-sha256.json", [row for row in paths if row["path"] in changed])
candidate_rows = []
triage = load("triage/full-family/map.json")
for finding in findings:
    cid = finding["id"]
    group = next(g for g in triage["groups"] if cid in g["local_ids"])
    associated = {row["path"] for row in paths if cid in row["candidate_ids"]}
    associated.update(p for p in finding["reopening_paths"] if p in allowed)
    associated.update(p for p in finding["test_oracle_paths"] if p in allowed)
    associated.update(c["path"] for c in finding["citations"] if c["path"] in allowed)
    evidence = {uid: record for uid, record in units.items() if cid in record.get("candidate_ids", [])}
    candidate_rows.append({
        "local_id": cid, "proposal_id": group["proposal_id"], "facets": group["facets"],
        "inherited_aliases": finding["inherited_aliases"],
        "paths": [{"path": p, "disposition": "EDIT" if p in changed else "RETAIN"} for p in sorted(associated)],
        "out_of_scope_reopening_paths": [p for p in finding["reopening_paths"] if p not in allowed],
        "held_facet_trigger": group["implementation_reopen_trigger_or_needed_probe"],
        "exact_investigation_units": evidence,
        "review": "Independent full-unit/content/citation review pending; no acceptance or execution credit.",
    })
save("candidate-dispositions.json", candidate_rows)
save("example-bodies.json", examples)
save("xml-checks.json", xml_checks)
inputs = ["triage/editing-release.md", "triage/full-family/map.json", "triage/full-family/relationships.json",
          "triage/full-family/inherited-links.json", "triage/full-family/probe-priorities.md", "investigation-A.md",
          "investigation/A/findings.json", "investigation/A/file-dispositions.json", "investigation/A/unit-results.json",
          "investigation/A/sources/captures.json", "investigation/A/sources/notes.md", "inventory/packets/A.json"]
inputs.extend("investigation/A/sources/" + capture["retained_path"] for capture in load("investigation/A/sources/captures.json"))
save("input-identities.json", [{"path": p, "sha256": sha((RUN / p).read_bytes())} for p in inputs])
diff = git("diff", "--binary", "--no-ext-diff", "--src-prefix=a/", "--dst-prefix=b/", BASE, "--", *sorted(changed))
(OUT / "product.patch").write_bytes(diff)
check = subprocess.run(["git", "diff", "--check", BASE], cwd=ROOT, capture_output=True, text=True)
assert check.returncode == 0, check.stdout + check.stderr
save("verification.json", {
    "base": BASE, "source_pin": SOURCE, "branch": git("branch", "--show-current").decode().strip(),
    "changed_cs_paths": len(changed), "packet_cs_paths": len(paths), "candidate_rows": len(candidate_rows),
    "example_bodies": len(examples), "exact_noncomment_lines_preserved": True,
    "base_product_equals_source_pin": True, "git_diff_check_exit_code": check.returncode,
    "patch_sha256": sha(diff), "xml_blocks_parsed": sum(row["xml_blocks_parsed"] for row in xml_checks),
    "scope": "Only packet-A C# full-line comments/XML plus incidental final-newline normalization.",
    "compiler_and_runtime": "NOT-RUN; no compiler/token-parser/API-bind/native/device claim",
    "tests_formatter_build_commit_index_push_subagents": "NOT-RUN",
})
artifacts = sorted(p for p in OUT.iterdir() if p.is_file() and p.name != "output-identities.json")
artifacts.append(RUN / "edit-A.md")
save("output-identities.json", {
    "self_excluded": True,
    "product_changes": [{"path": row["path"], "base_sha256": row["base_sha256"], "current_sha256": row["current_sha256"]}
                        for row in paths if row["path"] in changed],
    "new_editor_records": [{"path": p.relative_to(ROOT).as_posix(), "base_sha256": None,
                            "current_sha256": sha(p.read_bytes())} for p in artifacts],
})
print(f"Static checks: {len(changed)} edited / {len(paths)} packet C# paths; {len(candidate_rows)} candidates; {len(examples)} examples.")
print("Exact non-comment lines preserved; source-pin/base product bytes match; changed XML is well-formed; git diff --check passed.")
print("No SDK build, test, formatter, compiler, native/device, crypto, index, commit, push or subagent execution.")
