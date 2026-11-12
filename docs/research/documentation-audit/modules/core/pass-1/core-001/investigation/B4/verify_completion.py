"""Fresh read-only record/source verification; writes B4 result/nominees only.

No product tool, SDK, native library, device, formatter or Git mutation is run.
Structural success does not establish semantic quality or independent review.
"""
import collections
import ast
import datetime
import hashlib
import json
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(p for p in HERE.parents if (p / ".git").exists())
PIN = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"
failures = []
def sha(data): return hashlib.sha256(data).hexdigest()
def load(p): return json.loads(p.read_text())
def check(ok, message):
    if not ok: failures.append(message)
def git(*args): return subprocess.check_output(["git", *args], cwd=ROOT)

packet = load(RUN / "inventory/packets/B4-windows-support.json")
units = load(HERE / "unit-dispositions.json")
summary = load(HERE / "coverage-summary.json")
inventory = {u["id"]: u for u in load(RUN / "inventory/semantic-units.json")}
expected = set(packet["unit_ids"] + packet["shared_read_unit_ids"])
check(len(units) == len({r["id"] for r in units}) == len(expected) == 2350, "2350 unique IDs")
check({r["id"] for r in units} == expected, "exact assigned ID set")
check(len(packet["unit_ids"]) == 1456 and len(packet["shared_read_unit_ids"]) == 894, "primary/shared counts")
check(len(packet["paths"]) == 106 and {r["path"] for r in units if r["ownership"] == "primary"} == set(packet["paths"]), "106 primary file outcomes")
check(not any(r["status"] in {"UNREAD", "READ-PENDING-CONTEXT", "AWAIT-CONTEXT"} or r["pending_context"] for r in units), "zero unread/await-context")
check(all(r["assigned_scope_disposition_complete"] and r["reason"] and r["scope"] and r["disposition_owner"] for r in units), "explicit outcomes/scope/owners")
check(not load(HERE / "continuation.json")["remaining_unit_ids"], "empty active continuation")
check(summary["complete_primary_file_dispositions"] == 106 and summary["complete_primary_file_reads"] == 105 and summary["scoped_related_context_snapshot_files"] == 1, "honest file read versus qualified disposition counts")
check(dict(collections.Counter(r["status"] for r in units)) == summary["combined_status_counts"], "combined status reconciliation")
for ownership, key in [("primary", "primary_status_counts"), ("assigned-shared", "shared_status_counts")]:
    check(dict(collections.Counter(r["status"] for r in units if r["ownership"] == ownership)) == summary[key], key)
check(sum(f["files"] for f in summary["families"]) == 106 and sum(f["lines"] for f in summary["families"]) == 14842, "106/14842 family totals")
check(sha((RUN / "inventory/packets/B4-windows-support.json").read_bytes()) == summary["packet_sha256"] == load(HERE / "history/checkpoint-1/coverage-summary.json")["packet_sha256"], "unchanged authoritative packet")
for path in HERE.glob("*.py"):
    ast.parse(path.read_text(), filename=str(path))
transition = load(HERE / "completion-transition.json")
historical_units = load(HERE / "history/checkpoint-1/unit-dispositions.json")
outstanding = {r["id"] for r in historical_units if r["status"] in {"UNREAD", "READ-PENDING-CONTEXT"}}
check(len(transition) == len(outstanding) == 1462 and {r["id"] for r in transition} == outstanding, "1462 exact historical transitions")
cache = {}
for r in units:
    u = inventory[r["id"]]
    for k in ["path", "label", "first_line", "last_line", "start_character", "end_character_exclusive", "source_sha256", "range_sha256"]:
        check(r[k] == u[k], f"inventory field {r['id']}:{k}")
    data = cache.setdefault(r["path"], (ROOT / r["path"]).read_bytes())
    check(sha(data) == r["source_sha256"], f"source hash {r['id']}")
    text = data.decode("utf-8-sig", errors="replace")
    check(sha(text[r["start_character"]:r["end_character_exclusive"]].encode()) == r["range_sha256"] == r["actual_range_sha256"], f"range hash {r['id']}")
    check(r["sample_eligible"] == (r["status"] == "READ-CLEAN"), f"sample eligibility {r['id']}")
    check(not r["sample_eligible"] or (r["complete_context_read"] and not r["candidate_ids"]), f"clean eligibility context {r['id']}")
    check(r["platform_execution"] == "UNRUN" and r["validation_status"] == "VALID-NOT-CHECKED", f"execution boundary {r['id']}")

history_checks = []
for row in load(HERE / "history/checkpoint-1/manifest.json"):
    data = (HERE / "history/checkpoint-1" / row["history"]).read_bytes()
    ok = sha(data) == row["sha256"] and len(data) == row["bytes"]
    check(ok, f"checkpoint history {row['history']}")
    history_checks.append({"path": row["history"], "sha256": sha(data), "match": ok})

identities = []
for previous in load(HERE / "history/checkpoint-1/file-identities.json"):
    path = previous["path"]
    data = cache[path]
    check(sha(data) == previous["current_raw_sha256"], f"checkpoint source identity {path}")
    pin = subprocess.run(["git", "show", f"{PIN}:{path}"], cwd=ROOT, capture_output=True)
    equal = pin.returncode == 0 and pin.stdout.replace(b"\r\n", b"\n") == data.replace(b"\r\n", b"\n")
    if previous["pinned_git_blob"] is not None:
        check(equal, f"pinned normalized identity {path}")
    identities.append({**previous, "final_current_raw_sha256": sha(data), "final_lf_equal_to_pin": equal,
                       "current_pin_path_exists": pin.returncode == 0})
(HERE / "file-identities.json").write_text(json.dumps(identities, indent=2) + "\n")
pinned_newline_rows = [{"path": r["path"], "raw_sha256": r["current_raw_sha256"], "pinned_raw_sha256": r["pinned_content_sha256"], "lf_equal": r["final_lf_equal_to_pin"]}
                       for r in identities if r["pinned_content_sha256"] is not None and r["current_raw_sha256"] != r["pinned_content_sha256"]]
check(len(pinned_newline_rows) == 13 and all(r["lf_equal"] for r in pinned_newline_rows), "13 assigned baseline-only newline representations")

captures = load(HERE / "sources/captures.json")
for row in captures:
    check(row.get("status") == 200 and sha((HERE / "sources" / row["name"]).read_bytes()) == row["sha256"], f"original capture {row['name']}")
tree = load(RUN / "investigation/B3/sources/native-release-tree.json")
blobs = {f["path"]: f["sha"] for f in tree["tree"] if f["type"] == "blob"}
blob_checks = []
for local, original in {
    "shim-pcsc.c": "pcsc.c", "shim-native_abi.h": "native_abi.h", "shim-CMakeLists.txt": "CMakeLists.txt",
    "shim-exports.msvc": "exports.msvc", "shim-nuspec.xml": "Yubico.NativeShims.nuspec",
    "shim-header.in": "Yubico.NativeShims.h.in", "shim-build-windows.ps1": "build-windows.ps1",
    "shim-pcscd.cmake": "cmake/pcscd.cmake", "shim-build-linux-amd64.sh": "build-linux-amd64.sh",
    "shim-build-linux-arm64.sh": "build-linux-arm64.sh", "shim-Aot.targets": "msbuild/Yubico.NativeShims.Aot.targets",
}.items():
    data = (HERE / "sources" / local).read_bytes()
    actual = hashlib.sha1(f"blob {len(data)}\0".encode() + data).hexdigest()
    path = f"Yubico.NativeShims/{original}"
    check(actual == blobs[path], f"published source blob {local}")
    blob_checks.append({"local": local, "release_path": path, "git_blob": actual, "expected": blobs[path], "match": actual == blobs[path]})

candidates = load(HERE / "candidate-index.json")
check([c["id"] for c in candidates] == [f"CORE-B4-C{i:03}" for i in range(1, 11)], "ten stable local IDs")
for c in candidates:
    linked = [r for r in units if c["id"] in r["candidate_ids"]]
    check(set(c["unit_ids"]) == {r["id"] for r in linked}, f"candidate exact IDs {c['id']}")
    check(set(c["unchanged_primary_or_shared_paths"]) == {r["path"] for r in linked}, f"candidate path map {c['id']}")

nominee_ids = ["982be8e7f147990c", "6059b9db1740aa4e", "a791b5ae1d430f97", "31321beb19407856", "9b95a812279deba5", "618325296e7023d9", "ceddba86243abd3f"]
by_id = {r["id"]: r for r in units}
nominees = []
for suffix in nominee_ids:
    uid = "core-001:unit:" + suffix
    r = by_id[uid]
    check(r["sample_eligible"], f"nominee clean {uid}")
    nominees.append({k: r[k] for k in ["id", "path", "first_line", "last_line", "label", "reason", "source_sha256", "range_sha256"]})
(HERE / "clean-nominees.json").write_text(json.dumps({"status": "PROVISIONAL-NOT-INDEPENDENTLY-SAMPLED", "nominees": nominees}, indent=2) + "\n")

# Static diagram identity and meaningful text reconciliation record; no renderer.
arch = ROOT / "docs/architecture"
source = (arch / "sdk-architecture-diagrams.md").read_text()
blocks = re.findall(r"^```mermaid\s*\n(.*?)^```", source, re.M | re.S)
manifest = {l.split("\t")[0]: l.split("\t")[1:] for l in (arch / "images/image-manifest.txt").read_text().splitlines() if "\t" in l and not l.startswith("#")}
image_checks = []
for i, name in enumerate(["L0-context", "L1-assembly-deps", "L2-layered-stack", "L3-apdu-sequence", "L3b-fido2-ctap-sequence"]):
    svg, png = arch / f"images/{name}.svg", arch / f"images/png/{name}.png"
    actual = [sha(blocks[i].encode()), sha(svg.read_bytes()), sha(png.read_bytes())]
    check(actual == manifest[name], f"diagram source/SVG/PNG digest {name}")
    labels = ["".join(e.itertext()).strip() for e in ET.fromstring(svg.read_bytes()).iter() if e.tag.split("}")[-1] in {"text", "p"} and "".join(e.itertext()).strip()]
    image_checks.append({"name": name, "source_svg_png_digests": actual, "match": actual == manifest[name], "meaningful_labels": labels,
                         "review": "PNG visually inspected and SVG labels compared with Mermaid; C010 teaching wording separately qualified."})
(HERE / "diagram-reconciliation.json").write_text(json.dumps(image_checks, indent=2, ensure_ascii=False) + "\n")

# Protected record identities are checked fresh without writing central records.
protected = load(HERE / "history/checkpoint-1/preservation-verification.json")["protected_input_hashes"]
for path, digest in protected.items(): check(sha((ROOT / path).read_bytes()) == digest, f"protected input {path}")
baseline = load(RUN / "baseline.json")
frozen_count = 0
for row in baseline["frozen_manifests"]:
    p = ROOT / row["path"]
    check(sha(p.read_bytes()) == row["sha256"], f"frozen manifest {row['path']}")
    records = load(p)["files"]
    frozen_count += len(records)
    artifact_root = p.parent / "records" if p == ROOT / "docs/research/documentation-audit/record-manifest.json" else p.parent
    for record in records:
        check(sha((artifact_root / record["path"]).read_bytes()) == record["sha256"], f"frozen artifact {artifact_root / record['path']}")
check(frozen_count == 5366, "5366 frozen artifact population")
integration = ROOT / baseline["integration_manifest"]["path"]
check(sha(integration.read_bytes()) == baseline["integration_manifest"]["sha256"], "integration manifest identity")
integration_rows = load(integration)["files"]
for row in integration_rows:
    check(sha((ROOT / row["path"]).read_bytes()) == row["raw_sha256"], f"frozen integration raw {row['path']}")
check(len(integration_rows) == 8325, "8325 integration record population")
prior_piv = baseline["prior_piv_integration"]["files"]
for row in prior_piv: check(sha((ROOT / row["path"]).read_bytes()) == row["sha256"], f"prior PIV {row['path']}")
users = load(RUN / "user-work-baseline.json")
for row in users: check(sha((ROOT / row["path"]).read_bytes()) == row["sha256"], f"user file {row['path']}")

# Product differences can exist as baseline LF/CRLF representations. Never reset
# them. Record the exact raw/normalized distinction, not a clean-tree claim.
diff = git("diff", "--name-only", "--", "src", "benchmarks", "verification").decode().splitlines()
newline_rows = []
for path in diff:
    head = git("show", f"HEAD:{path}")
    current = (ROOT / path).read_bytes()
    equal = current.replace(b"\r\n", b"\n") == head.replace(b"\r\n", b"\n")
    check(equal, f"non-newline product working difference {path}")
    newline_rows.append({"path": path, "raw_equal_to_head": current == head, "lf_equal_to_head": equal})
unmerged = git("ls-files", "--unmerged").decode()
check(not unmerged, "unmerged index entries")

# Link verification reads our new active report; the result's self-link is
# deferred until the result is written, avoiding the historical self-link error.
links_checked = 0
self_link = HERE / "completion-verification.json"
for path in [RUN / "investigation-B4.md", HERE / "candidates.md", HERE / "continuation-and-verification.md", HERE / "sources/register.md", HERE / "file-evidence.md"]:
    for link in re.findall(r"\]\(([^)]+)\)", path.read_text()):
        if "://" not in link and not link.startswith("#"):
            target = path.parent / link.split("#")[0]
            if target != self_link:
                check(target.exists(), f"local link {path.name}: {link}")
            links_checked += 1

gaps = load(HERE / "qualification-gaps.json")
check(len(gaps["gaps"]) == 5 and all(g["owner"] and g["scope"] and g["limit"] and g["reopen_with"] for g in gaps["gaps"]), "bounded external scope/owners")
result = {"status": "PASSED-STATIC-RECORD-VERIFICATION" if not failures else "FAILED", "utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
          "packet_completion": summary["status"], "source_pin": PIN, "observed_head": git("rev-parse", "HEAD").decode().strip(),
          "assigned_units": len(units), "primary_files_dispositioned": 106, "full_primary_reads": 105, "qualified_snapshot_files": 1,
          "primary_units": 1456, "shared_units": 894, "outstanding_units": 0, "resolved_checkpoint_outstanding": len(transition),
          "status_counts": summary["combined_status_counts"], "source_paths_checked": len(cache), "source_capture_count": len(captures),
          "history_checks": history_checks, "published_source_blob_checks": blob_checks, "local_links_checked": links_checked,
          "clean_nominees": len(nominees), "external_qualification_gaps": len(gaps["gaps"]),
          "frozen_populations_checked": {"prior_artifacts": frozen_count, "integration_records": len(integration_rows), "prior_piv_files": len(prior_piv), "user_files": len(users)},
          "product_worktree_differences": newline_rows,
          "assigned_pinned_raw_newline_differences": pinned_newline_rows,
          "execution": "Python/Git static reads and B4-only record writes. Zero SDK/build/test/native/device/formatter executions. No index/ref/commit/push writes or subagents.",
          "qualification": "Structural/source/history/capture checks do not establish semantic completeness, independent review, current SDK/provider/native producer identity or runtime/device acceptance.", "failures": failures}
output = HERE / "completion-verification.json"
attempt = HERE / f"completion-verification-attempt-{datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')}.json"
if failures:
    attempt.write_text(json.dumps(result, indent=2) + "\n")
output.write_text(json.dumps(result, indent=2) + "\n")
check(output.exists(), "verification self-link")
print(json.dumps({k: v for k, v in result.items() if k not in {"history_checks", "published_source_blob_checks", "product_worktree_differences", "assigned_pinned_raw_newline_differences"}}, indent=2))
if failures: raise SystemExit(1)
