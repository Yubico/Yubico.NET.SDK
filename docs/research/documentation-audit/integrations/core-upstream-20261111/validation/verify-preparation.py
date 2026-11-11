"""Static preparation inspection and replay of a historical RESULT parser, no gates."""
import ast
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, HERE / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    tools = []
    for path in sorted(HERE.glob("*.py")):
        raw = path.read_bytes()
        ast.parse(raw, filename=str(path))
        tools.append(dict(path=path.name, bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest()))
    preflight = json.loads((HERE / "preflight.json").read_bytes())
    for row in preflight["pinned_sources"] + preflight["frozen_piv_reuse_sources"]:
        raw = subprocess.check_output(["git", "show", f"{row['revision']}:{row['path']}"], cwd=ROOT)
        if len(raw) != row["bytes"] or hashlib.sha256(raw).hexdigest() != row["sha256"]:
            raise ValueError(f"Pinned provenance changed: {row['path']}")
    expected = json.loads((HERE / "expected-unit-projects.json").read_bytes())
    projects = preflight["discovered_unit_projects"]
    actual = ["test:" + Path(path).stem.removeprefix("Yubico.YubiKit.") for path in projects]
    if len(actual) != len(set(actual)) or sorted(expected) != sorted(actual):
        raise ValueError("Explicit expected unit projects differ from captured filename discovery")
    canonical = "src/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj"
    if preflight["selectors"]["Core"]["unit"] != [canonical]:
        raise ValueError("Resilience's hard-coded Core selector collides")
    logger = load("command_capture", "run-command.py")
    historical = ROOT / "docs/research/documentation-audit/modules/piv/pass-1/piv-001/validation/final-repository-v2-20261105-090952-590-build+test+docs-qa+docs-architecture+complexity/result.json"
    result = json.loads(historical.read_bytes())
    replay = logger.check_result(result, 0, ["build", "test", "docs-qa", "docs-architecture", "complexity"], expected)
    records = load("record_inspection", "check-records.py")
    rows = [records.record(path) for path in records.files(HERE / "final-record-manifest.json")]
    print(json.dumps(dict(status="static-preparation-verified", tools=tools,
        pinned_source_identities=len(preflight["pinned_sources"]), frozen_piv_source_identities=len(preflight["frozen_piv_reuse_sources"]),
        canonical_unit_projects=len(projects), historical_result_parser_replay=replay,
        historical_result_sha256=hashlib.sha256(historical.read_bytes()).hexdigest(),
        current_packet_text_records=len(rows), normalization_changes=[row["path"] for row in rows if not row["raw_preserved_in_git"]],
        limits="AST/provenance/filename/attribute inspection plus a frozen historical RESULT parse only. No command logger invocation, build, test, native, device or final staged-byte gate."), indent=2))


if __name__ == "__main__":
    main()
