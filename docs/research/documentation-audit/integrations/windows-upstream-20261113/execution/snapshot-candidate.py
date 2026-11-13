#!/usr/bin/env python3
"""Pin a resolved, staged product tree before recording validation evidence."""

import hashlib
import json
from pathlib import Path
import subprocess
import sys


def git(*arguments):
    return subprocess.check_output(["git", *arguments])


root = Path(__file__).resolve().parents[1]
assert not git("ls-files", "--unmerged"), "Resolve and stage all conflicts first"
assert not git("diff", "--name-only"), "Stage the complete resolved candidate first"
tree = git("write-tree").decode().strip()
parents = [git("rev-parse", reference).decode().strip() for reference in ("HEAD", "MERGE_HEAD")]
rows = []
for path in git("diff", "--name-only", "--no-renames", parents[0], tree).decode().splitlines():
    file = Path(path)
    rows.append({"path": path, "sha256": hashlib.sha256(file.read_bytes()).hexdigest()
                 if file.is_file() else None})
receipt = {"tree": tree, "parents": parents, "changed_paths": rows,
           "scope": "Resolved product tree before integration records are staged"}
with (root / "execution" / sys.argv[1]).open("x") as output:
    json.dump(receipt, output, indent=2)
    output.write("\n")
print(json.dumps({"tree": tree, "parents": parents, "changed_paths": len(rows)}, indent=2))
