#!/usr/bin/env python3
"""Verify the frozen records and original user files against preparation pins."""

import hashlib
import json
from pathlib import Path
import subprocess
import sys


def sha(data):
    return hashlib.sha256(data).hexdigest()


root = Path(__file__).resolve().parents[1]
expected = {
    "docs/research/documentation-audit/modules/core/pass-1":
        "6939f308bca6f97573f8a9e79145c2d27b464c62ef2924c161832b94f629ece0",
    "docs/research/documentation-audit/integrations/core-upstream-20261111":
        "5afe057c26475840f0c2c9775ab4c8dfd0e02f92f1d517533c06b896b236d0fb",
    "docs/research/documentation-audit/integrations/piv-pr745-20261105":
        "3afc466bbd3a937107a27af1ff7fa3d0baf866450f3a47d509cb9d860071cbe7",
}
records = []
for directory, fingerprint in expected.items():
    digest = hashlib.sha256()
    files = sorted(path for path in Path(directory).rglob("*") if path.is_file())
    for path in files:
        digest.update((path.as_posix() + "\0" + sha(path.read_bytes()) + "\n").encode())
    actual = digest.hexdigest()
    assert actual == fingerprint, (directory, actual, fingerprint)
    records.append({"path": directory, "files": len(files), "sha256": actual})
intake = json.loads((root / "intake/summary.json").read_text())
for record in intake["protected_user_files"]:
    data = Path(record["path"]).read_bytes()
    assert len(data) == record["bytes"] and sha(data) == record["sha256"], record["path"]
primary = intake["primary_worktree"]
assert subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=primary, text=True).strip() == intake["audited_parent"]
assert not subprocess.check_output(["git", "diff", "--name-only"], cwd=primary)
assert not subprocess.check_output(["git", "diff", "--cached", "--name-only"], cwd=primary)
receipt = {"status": "passed", "frozen_roots": records, "protected_user_files": 4,
           "original_checkout": "unchanged at published audit parent"}
destination = root / "execution" / sys.argv[1]
with destination.open("x") as output:
    json.dump(receipt, output, indent=2)
    output.write("\n")
print(json.dumps(receipt, indent=2))
