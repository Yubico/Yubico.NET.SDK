"""Compile explicitly classified strict-raw groups; never extract or execute examples."""

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--checker", required=True, type=Path)
    parser.add_argument("--manifest", required=True, type=Path)
    args = parser.parse_args()
    manifest_path = args.manifest.resolve(strict=True)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    groups = manifest["groups"]
    if not groups:
        parser.error("manifest groups must not be empty")
    repo = args.repo.resolve(strict=True)
    checker = args.checker.resolve(strict=True)
    failures = 0
    names = set()
    for group in groups:
        name = group["name"]
        if not isinstance(name, str) or not name or name in names:
            parser.error("each group needs a nonempty, unique name")
        names.add(name)
        if group.get("context") != "strict-raw":
            parser.error(f"{name}: route SDK/test/sample groups to their approved real context, not this runner")
        source = (manifest_path.parent / group["source"]).resolve(strict=True)
        if source.suffix != ".cs":
            parser.error(f"not a C# source: {source}")
        provenance = group.get("provenance")
        if not isinstance(provenance, list) or not provenance or not all(isinstance(item, str) and item for item in provenance):
            parser.error(f"{name}: retain source-document/block identifiers in provenance")
        prerequisites = group.get("prerequisites")
        if not isinstance(prerequisites, list) or not all(isinstance(item, str) for item in prerequisites):
            parser.error(f"{name}: prerequisites must explicitly list wrapper assumptions (or [])")
        modules = group.get("modules", [])
        if not isinstance(modules, list) or not all(isinstance(item, str) for item in modules):
            parser.error(f"{name}: modules must be a list of SDK module names")
        command = ["dotnet", str(checker), "compile", str(source), str(repo), *modules]
        references = group.get("references", [])
        if not isinstance(references, list) or not all(isinstance(item, str) for item in references):
            parser.error(f"{name}: references must be a list of explicit DLL paths")
        for reference in references:
            command.extend(["--reference", str((repo / reference).resolve(strict=True))])
        source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
        if group.get("source_sha256") != source_hash:
            parser.error(f"{name}: generated source hash differs from manifest; rerun extraction")
        extraction = group["extraction"]
        if hashlib.sha256((repo / extraction["path"]).read_bytes()).hexdigest() != extraction["document_sha256"]:
            parser.error(f"{name}: source document changed; rerun extraction")
        print(json.dumps(dict(group=name, source_sha256=source_hash, provenance=provenance,
                              prerequisites=prerequisites, command=command)), flush=True)
        result = subprocess.run(command, check=False)
        failures += result.returncode != 0
    print(f"Compile-only groups: {len(groups)}; failed: {failures}. No snippet executed.")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
