"""Read-only verification against the coordinator's immutable before receipt."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

HERE = Path(__file__).resolve().parent
BEFORE = HERE.parent / "evidence/preservation-before.json"
BEFORE_SHA256 = "8f45a2c984ab0d741152378d5fcadb4f785a478fca9b47f4ba384139cf7558c9"


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def require(value, message):
    if not value:
        raise ValueError(message)


def safe(root, relative):
    path = root / relative
    require(not Path(relative).is_absolute() and ".." not in Path(relative).parts,
            f"Unsafe relative path: {relative}")
    require(path.resolve().is_relative_to(root.resolve()) and not path.is_symlink(),
            f"Escaping/symlinked input: {path}")
    return path


def verify(before=BEFORE):
    raw = before.read_bytes()
    require(sha(raw) == BEFORE_SHA256, "Coordinator before-receipt identity changed")
    data = json.loads(raw)
    require(data["artifact_total"] == 5366 and len(data["archives"]) == 8,
            "Reconcile changed before-receipt denominators")
    prior = data["prior_integration_files"]["files"]
    require(len(prior) == len({row["path"] for row in prior}) == 114,
            "Expected 114 unique PIV integration files")
    require(len(data["original_user_files"]) == 4, "Expected four primary user files")
    report = {"before_sha256": sha(raw), "checkouts": []}
    for role, location in (("integration", data["integration_checkout"]), ("primary", data["primary"])):
        root = Path(location)
        artifact_count = 0
        for archive in data["archives"]:
            manifest_path = safe(root, archive["path"])
            manifest_raw = manifest_path.read_bytes()
            require(sha(manifest_raw) == archive["sha256"], f"Manifest changed: {manifest_path}")
            manifest = json.loads(manifest_raw)
            rows = manifest["files"]
            require(len(rows) == archive["artifacts"], f"Manifest count: {manifest_path}")
            require(len({row["path"] for row in rows}) == len(rows), f"Duplicate artifact: {manifest_path}")
            # The campaign-wide manifest's paths are relative to records/; module
            # manifests are relative to their containing run. Preserve that original format.
            base = manifest_path.parent / "records" if archive["path"] == "docs/research/documentation-audit/record-manifest.json" else manifest_path.parent
            for row in rows:
                path = safe(base, row["path"])
                content = path.read_bytes()
                require(sha(content) == row["sha256"] and len(content) == row["bytes"], f"Frozen artifact changed: {path}")
            artifact_count += len(rows)
        require(artifact_count == data["artifact_total"], "Artifact total changed")
        for row in prior:
            path = safe(root, row["path"])
            require(sha(path.read_bytes()) == row["sha256"], f"PIV integration changed: {path}")
        users = 0
        if role == "primary":
            for row in data["original_user_files"]:
                path = safe(root, row["path"])
                content = path.read_bytes()
                require(sha(content) == row["sha256"] and len(content) == row["bytes"], f"User work changed: {path}")
                users += 1
        head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root).decode().strip()
        if role == "primary":
            require(head == data["campaign"], "Primary campaign moved; coordinator must authorize a new phase")
        report["checkouts"].append(dict(role=role, root=str(root), head=head,
            manifests=8, frozen_artifacts=artifact_count, piv_integration_files=len(prior), original_user_files=users))
    require(before.read_bytes() == raw, "Before receipt changed during verification")
    return report


def historical_binaries(manifest):
    """Explicit optional old-live prerequisite; every registered external byte is required."""
    rows = json.loads(manifest.read_bytes())["files"]
    require(rows and len({row["preserved_path"] for row in rows}) == len(rows), "Empty/duplicate binary registry")
    for row in rows:
        path = Path(row["preserved_path"])
        require(path.is_absolute() and not path.is_symlink(), f"Require registered external binary: {path}")
        raw = path.read_bytes()  # A missing original is an explicit failure, never a skipped check.
        require(len(raw) == row["original"]["bytes"] and sha(raw) == row["original"]["sha256"],
                f"Historical binary identity changed: {path}")
    return dict(manifest=str(manifest), binaries=len(rows), status="all-registered-external-bytes-verified")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--before", type=Path, default=BEFORE)
    parser.add_argument("--historical-binary-manifest", type=Path,
                        help="Required old-live external binary registry; missing bytes fail")
    args = parser.parse_args()
    report = verify(args.before)
    if args.historical_binary_manifest:
        report["historical_binaries"] = historical_binaries(args.historical_binary_manifest)
    print(json.dumps(report, indent=2))
