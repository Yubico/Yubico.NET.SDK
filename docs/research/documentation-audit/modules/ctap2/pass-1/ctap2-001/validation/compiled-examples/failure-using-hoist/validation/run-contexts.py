"""Compile the explicit documentation contexts serially; never execute their assemblies/tests."""
import json
from pathlib import Path
import re
import shutil
import subprocess

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[7]
TOOLS = HERE / "checker-source"
MANIFEST = TOOLS / "context/contexts-manifest.json"
LOGS = HERE / "context-runs"
LOGS.mkdir(exist_ok=True)
contexts = json.loads(MANIFEST.read_text())["contexts"]
results = []

for context in contexts:
    identifier = context["id"]
    log_path = LOGS / f"{identifier}.log"
    if log_path.exists():
        raise RuntimeError(f"Refusing to overwrite previous evidence: {log_path}")
    base = ["python3", str(TOOLS / "prepare-project-context.py"), "--repo", str(ROOT),
            "--manifest", str(MANIFEST), "--context", identifier]
    commands = [base, [*base, "--check"],
                ["dotnet", "toolchain.cs", "--", "build", "--project",
                 f"AuditCt001.Examples.{identifier}", "--clean"], [*base, "--collect"]]
    with log_path.open("w") as log:
        for command in commands:
            run = subprocess.run(command, cwd=ROOT, capture_output=True, text=True)
            output = run.stdout + run.stderr
            log.write("$ " + " ".join(command) + "\n" + output + f"\nEXIT={run.returncode}\n")
            log.flush()
            if command[0] == "dotnet":
                match = re.search(r"log: (artifacts/toolchain/runs/[^)]+/run\.log)", output)
                if match:
                    source = ROOT / Path(match.group(1)).parent
                    destination = LOGS / f"{identifier}-{source.name}"
                    shutil.copytree(source, destination)
            print(f"{identifier}: {command[0]} {command[-1]} -> {run.returncode}", flush=True)
            if run.returncode:
                print(output[-12000:], flush=True)
                results.append({"context": identifier, "status": "failed", "command": command,
                                "exit_code": run.returncode, "log": str(log_path.relative_to(HERE))})
                (LOGS / "result.json").write_text(json.dumps(results, indent=2) + "\n")
                raise SystemExit(run.returncode)
    results.append({"context": identifier, "status": "compiled-and-inputs-collected",
                    "groups": context["groups"], "log": str(log_path.relative_to(HERE))})
    (LOGS / "result.json").write_text(json.dumps(results, indent=2) + "\n")

print(f"PASS: {len(results)} explicit SDK contexts compiled; no snippet/test/device operation executed.")
