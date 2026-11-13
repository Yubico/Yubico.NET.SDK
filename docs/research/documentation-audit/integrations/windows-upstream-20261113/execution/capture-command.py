#!/usr/bin/env python3
"""Run one bounded validation command and preserve its complete toolchain logs."""

import datetime
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys


def main():
    label, *command = sys.argv[1:]
    root = Path(__file__).resolve().parents[1]
    destination = root / "validation" / label
    destination.mkdir(parents=True, exist_ok=False)
    candidate_tree = subprocess.check_output(["git", "write-tree"], text=True).strip()
    assert not subprocess.check_output(["git", "diff", "--name-only"]), "Stage the product candidate before validation"
    started = datetime.datetime.now(datetime.timezone.utc).isoformat()
    with (destination / "console.txt").open("wb") as output:
        result = subprocess.run(command, stdout=output, stderr=subprocess.STDOUT)
    latest = Path("artifacts/toolchain/latest")
    if command[:2] == ["dotnet", "toolchain.cs"] and latest.exists():
        shutil.copytree(latest, destination / "toolchain", symlinks=False)
    files = [
        {"path": str(path.relative_to(destination)),
         "bytes": path.stat().st_size,
         "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
        for path in sorted(destination.rglob("*")) if path.is_file()
    ]
    receipt = {
        "actor": "GPT-6 Astra",
        "platform": "Linux integration host",
        "command": command,
        "started_utc": started,
        "finished_utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "exit_code": result.returncode,
        "head": subprocess.check_output(["git", "rev-parse", "HEAD"], text=True).strip(),
        "candidate_tree": candidate_tree,
        "tracked_changes_after_command": subprocess.check_output(
            ["git", "diff", "--name-only"], text=True).splitlines(),
        "files": files,
    }
    (destination / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print((destination / "console.txt").read_text(errors="replace"))
    print(f"Preserved command exit {result.returncode}: {destination}")
    return result.returncode


if __name__ == "__main__":
    sys.exit(main())
