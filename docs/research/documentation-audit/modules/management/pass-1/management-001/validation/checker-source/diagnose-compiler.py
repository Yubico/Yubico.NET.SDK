"""Run focused compile-only identity experiments, retaining exact commands and unfiltered diagnostics."""

import argparse
import json
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--checker", required=True, type=Path)
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--log", required=True, type=Path)
    parser.add_argument("--identity-comparer", choices=["default", "desktop"])
    parser.add_argument("--group", action="append", required=True)
    args = parser.parse_args()
    repo = args.repo.resolve(strict=True)
    manifest_path = args.manifest.resolve(strict=True)
    manifest = json.loads(manifest_path.read_text())
    groups = {group["name"]: group for group in manifest["groups"]}
    # Refuse to overwrite a failed experiment or its command evidence.
    with args.log.open("x", encoding="utf-8") as log:
        for name in args.group:
            group = groups[name]
            source = (manifest_path.parent / group["source"]).resolve(strict=True)
            command = ["dotnet", str(args.checker.resolve(strict=True)), "compile", str(source), str(repo), *group.get("modules", [])]
            for reference in group.get("references", []):
                command.extend(["--reference", str((repo / reference).resolve(strict=True))])
            if args.identity_comparer:
                command.extend(["--identity-comparer", args.identity_comparer])
            log.write(json.dumps({"group": name, "command": command}) + "\n")
            log.flush()
            result = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, check=False)
            log.write(f"EXIT: {result.returncode}\n")
            log.flush()
            print(f"{name}: compiler exit {result.returncode}; evidence: {args.log}")
    # This driver preserves intentional failed experiments; per-command exit statuses are in the log.


if __name__ == "__main__":
    main()
