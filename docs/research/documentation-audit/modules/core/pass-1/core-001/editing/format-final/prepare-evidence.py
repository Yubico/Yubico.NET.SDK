"""One-off Core Pass 1 EOF reconciliation; writes only this record dir and isolated copies.

No formatter, build, test, source write, Git mutation, or subprocess other than git show.
"""

import collections
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import tarfile


ROOT = Path(__file__).resolve().parents[9]
OUT = Path(__file__).resolve().parent
RUN = OUT.parent.parent
RUNTIME = ROOT.parent / ".core-docs-audit-runtime/core-001/format-final-reconciliation"
REPORT = ROOT.parent / ".core-docs-audit-runtime/core-001/format-final-source-check.json"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def emit(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + "\n")


accepted_bytes = (RUN / "editing/integration/current-products.json").read_bytes()
accepted = json.loads(accepted_bytes)
products = {row["path"]: row for row in accepted["product_files"]}
assert len(products) == 363
before_all = {p: (ROOT / p).read_bytes() for p in products}
assert all(sha(data) == products[p]["sha256"] for p, data in before_all.items())
requested = [p for p in products if p.endswith(".cs")]
report_bytes = REPORT.read_bytes()
report = json.loads(report_bytes)
counts = collections.Counter(c["DiagnosticId"] for row in report for c in row["FileChanges"])
assert len(report) == 249
assert counts == {"FINALNEWLINE": 239, "IMPORTS": 1, "IDE1006": 3, "IDE0005": 6}
config = (ROOT / ".editorconfig").read_bytes()
assert b"insert_final_newline = false" in config
assert not list((ROOT / "src").rglob(".editorconfig"))
invocation = json.loads((RUN / "validation/commands-v2/format-final-source-check/invocation.json").read_bytes())
command = invocation["command"]
assert set(command[command.index("--include") + 1:command.index("--verify-no-changes")]) == set(requested)
eof = []
baseline = []
patch = []
archive_files = {}
pin = accepted["source_baseline"]
baseline_cache = {}
for row in report:
    path = Path(row["FilePath"]).relative_to(ROOT).as_posix()
    assert path in products and path in requested, f"STOP outside accepted/requested map: {path}"
    assert len(row["FileChanges"]) == 1
    change = row["FileChanges"][0]
    data = before_all[path]
    lines = data.decode("utf-8-sig").splitlines(keepends=True)
    if change["DiagnosticId"] == "FINALNEWLINE":
        assert change["FormatDescription"] == "Fix final newline. Delete 1 characters."
        assert data.endswith(b"\n") and not data.endswith(b"\r\n")
        assert not data.endswith(b"\n\n")
        assert change["LineNumber"] == len(lines)
        # All final lines here are ASCII; char/byte offsets consequently agree.
        assert lines[-1].isascii()
        assert change["CharNumber"] == len(lines[-1][:-1]) + 1
        after = data[:-1]
        assert after + b"\n" == data and not after.endswith((b"\n", b"\r"))
        tail = lines[-1].rstrip("\n")
        assert not tail.lstrip().startswith(("///", "//", "#")), path
        eof.append({"path": path, "before_sha256": sha(data), "after_sha256": sha(after),
                    "before_bytes": len(data), "after_bytes": len(after),
                    "removed_hex": "0a", "report": change, "last_line": tail,
                    "accepted_receipt": products[path]["accepted_receipt"],
                    "all_nontrailing_bytes_identical": True})
        for state, content in (("before", data), ("after", after)):
            target = RUNTIME / state / path
            target.parent.mkdir(parents=True, exist_ok=True)
            assert not target.exists(), f"Refuse to overwrite {target}"
            target.write_bytes(content)
            archive_files[f"{state}/{path}"] = content
        n = len(lines)
        start = max(1, n - 3)
        count = n - start + 1
        patch.extend([f"diff --git a/{path} b/{path}\n", f"--- a/{path}\n", f"+++ b/{path}\n",
                      f"@@ -{start},{count} +{start},{count} @@\n"])
        patch.extend(" " + line for line in lines[start - 1:-1])
        patch.extend(["-" + lines[-1], "+" + lines[-1], "\\ No newline at end of file\n"])
    else:
        if path not in baseline_cache:
            baseline_cache[path] = subprocess.run(
                ["git", "show", f"{pin}:{path}"], cwd=ROOT, check=True,
                capture_output=True).stdout
        pinned = baseline_cache[path]
        old_lines = pinned.decode("utf-8-sig").splitlines()
        current_lines = data.decode("utf-8-sig").splitlines()
        n = change["LineNumber"]
        site = current_lines[n - 1]
        assert site == old_lines[n - 1], f"Reported site differs at pin: {path}:{n}"
        imports = lambda ls: [l for l in ls if re.match(r"^\s*(global )?using\s", l)]
        assert imports(old_lines) == imports(current_lines)
        baseline.append({"path": path, "report": change, "site": site,
                         "baseline_line": n, "exact_site_unchanged_at_pin": True,
                         "current_sha256": sha(data), "baseline_sha256": sha(pinned),
                         "current_import_sequence": imports(current_lines),
                         "baseline_import_sequence": imports(old_lines),
                         "classification": "preservation-conflicting baseline finding; unchanged"})

assert len(eof) == len({x["path"] for x in eof}) == 239
(OUT / "eof-only.patch").write_bytes("".join(patch).encode())
with tarfile.open(OUT / "source-snapshots.tar.gz", "w:gz") as archive:
    for path, data in sorted(archive_files.items()):
        entry = tarfile.TarInfo(path)
        entry.size = len(data)
        entry.mode = 0o644
        entry.mtime = 0
        archive.addfile(entry, io.BytesIO(data))
emit("eof-identities.json", {"files": eof, "count": len(eof), "total_removed_bytes": 239,
                             "runtime_copies": str(RUNTIME), "applied_to_primary": False})
emit("baseline-findings.json", {"pin": pin, "findings": baseline,
                                "qualification": "Source-site/import-sequence evidence only; no pinned formatter replay."})

solution = (ROOT / "Yubico.YubiKit.sln").read_text()
projects = [Path(p.replace("\\", "/")) for p in re.findall(
    r'Project\([^\n]+?= "[^\"]+", "([^\"\n]+\.csproj)"', solution)]
linked = {}
for project in projects:
    for include in re.findall(r'<Compile\s+Include="([^\"]+)"', (ROOT / project).read_text()):
        if "$" not in include and "*" not in include:
            target = (ROOT / project.parent / include.replace("\\", "/")).resolve()
            linked.setdefault(target, []).append(project.as_posix())
membership = []
log = (RUN / "validation/commands-v2/format-final-source-check/stdout.log").read_text()
logged = {Path(p).relative_to(ROOT).as_posix() for p in re.findall(r"Formatted code file '([^']+)'", log)}
for path in sorted(requested):
    p = Path(path)
    candidates = [pr.as_posix() for pr in projects if p.is_relative_to(pr.parent)]
    candidates.extend(linked.get((ROOT / p).resolve(), []))
    membership.append({"path": path, "static_solution_project_candidates": sorted(set(candidates)),
                       "formatted_entry_in_verify_only_log": path in logged,
                       "qualification": "static candidate, not runtime processing proof" if candidates
                       else "absent from solution project roots and literal linked Compile includes"})
assert len(logged) == 241
absent = [x["path"] for x in membership if not x["static_solution_project_candidates"]]
assert len(absent) == 16
emit("format-membership.json", {"requested_cs": len(requested), "accepted_products": len(products),
                               "solution_projects": len(projects), "logged_changed_files": 241,
                               "logged_workspace_files": 1426, "absent_cs_paths": absent,
                               "files": membership,
                               "qualification": "241 is changed-file count in a verify-only run, not all included/processed files; no runtime membership evaluation performed."})
inputs = ["CLAUDE.md", ".editorconfig", "Directory.Build.props", "Directory.Build.targets", "Yubico.YubiKit.sln",
          str((RUN / "editing/integration/current-products.json").relative_to(ROOT))]
inputs += [str((RUN / "validation/commands-v2/format-final-source-check" / name).relative_to(ROOT))
           for name in ("invocation.json", "receipt.json", "stdout.log", "stderr.log")]
assert all((ROOT / p).read_bytes() == data for p, data in before_all.items()), "Primary product changed during preparation"
emit("preparation-receipt.json", {"status": "unapplied EOF-only proposal; independent review required",
                                  "all_363_primary_bytes_unchanged": True,
                                  "accepted_map_sha256": sha(accepted_bytes),
                                  "inputs": [{"path": p, "sha256": sha((ROOT / p).read_bytes())} for p in inputs]
                                  + [{"path": str(REPORT), "sha256": sha(report_bytes)}],
                                  "diagnostics": dict(counts), "primary_source_writes": 0,
                                  "formatter_build_test_native_device_crypto_runs": 0,
                                  "patch_sha256": sha((OUT / "eof-only.patch").read_bytes()),
                                  "snapshot_sha256": sha((OUT / "source-snapshots.tar.gz").read_bytes())})
print(json.dumps({"eof_paths": len(eof), "baseline_findings": len(baseline), "absent_cs": absent,
                  "patch_sha256": sha((OUT / "eof-only.patch").read_bytes()),
                  "last_line_kinds": dict(collections.Counter(x["last_line"] for x in eof))}, indent=2))
