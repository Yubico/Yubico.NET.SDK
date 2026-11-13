#!/usr/bin/env python3
"""Summarize preserved receipts without rerunning completed checks."""

import json
from pathlib import Path


root = Path(__file__).resolve().parents[1]
rows = []
for path in sorted((root / "validation").glob("*/receipt.json")):
    receipt = json.loads(path.read_text())
    result_path = path.parent / "toolchain/result.json"
    result = json.loads(result_path.read_text()) if result_path.exists() else {}
    rows.append({
        "label": path.parent.name,
        "command": receipt["command"],
        "candidate_tree": receipt["candidate_tree"],
        "exit_code": receipt["exit_code"],
        "toolchain_status": result.get("status"),
        "warning_count": result.get("warningCount"),
        "projects": result.get("projects", []),
        "tracked_changes_after_command": receipt["tracked_changes_after_command"],
        "receipt": str(path.relative_to(root)),
    })
output = {"scope": "Recorded bounded integration checks; platform evidence retains its original source", "runs": rows}
(root / "validation/results-summary.json").write_text(json.dumps(output, indent=2) + "\n")
print(json.dumps(output, indent=2))
