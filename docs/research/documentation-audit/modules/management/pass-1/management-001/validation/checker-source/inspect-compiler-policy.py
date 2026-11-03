"""Read evaluated compiler policy and existing dependency assets; no target or compilation is run."""

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--log", required=True, type=Path)
    args = parser.parse_args()
    repo = args.repo.resolve(strict=True)
    project = "src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj"
    with args.log.open("x", encoding="utf-8") as log:
        version_command = ["dotnet", "--version"]
        version = subprocess.run(version_command, cwd=repo, check=True, text=True, capture_output=True).stdout.strip()
        log.write(json.dumps({"command": version_command, "sdk": version}) + "\n")
        command = ["dotnet", "msbuild", project, "-p:Configuration=Release",
                   "-getProperty:NoWarn,TreatWarningsAsErrors,TargetFramework,NETCoreSdkVersion,MSBuildProjectFullPath,WarningLevel,LangVersion,AnalysisLevel,AnalysisMode"]
        log.write(json.dumps({"command": command, "note": "Evaluation only; no target requested"}) + "\n")
        log.flush()
        subprocess.run(command, cwd=repo, stdout=log, stderr=subprocess.STDOUT, check=True)
        dotnet = Path(shutil.which("dotnet") or "dotnet").resolve(strict=True)
        sdk = dotnet.parent / "sdk" / version
        version_command = ["dotnet", str(sdk / "Roslyn/bincore/csc.dll"), "-version"]
        log.write(json.dumps({"command": version_command, "note": "Version only; no source compiled"}) + "\n")
        log.flush()
        subprocess.run(version_command, cwd=repo, stdout=log, stderr=subprocess.STDOUT, check=True)
        for path in [sdk / "Roslyn/Microsoft.CSharp.Core.targets",
                     sdk / "Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.CSharp.props",
                     repo / "Directory.Build.props"]:
            data = path.read_bytes()
            log.write(json.dumps({"file": str(path), "sha256": hashlib.sha256(data).hexdigest()}) + "\n")
            for number, line in enumerate(data.decode().splitlines(), 1):
                if "1701" in line or "NoWarn" in line or "TreatWarningsAsErrors" in line:
                    log.write(f"{number}: {line.strip()}\n")
        assets = repo / Path(project).parent / "obj/project.assets.json"
        data = assets.read_bytes()
        log.write(json.dumps({"file": str(assets), "sha256": hashlib.sha256(data).hexdigest()}) + "\n")
        for framework, target in json.loads(data)["targets"].items():
            for name, metadata in target.items():
                if name.lower().startswith("xunit"):
                    log.write(json.dumps({"framework": framework, "package": name,
                                          "dependencies": metadata.get("dependencies", {}),
                                          "compile": list(metadata.get("compile", {}))}) + "\n")
    print(args.log.read_text())


if __name__ == "__main__":
    main()
