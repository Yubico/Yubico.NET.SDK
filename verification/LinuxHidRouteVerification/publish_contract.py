#!/usr/bin/env python3
"""Local publish/input/artifact binding. No signing claim and no hardware operations."""

import datetime
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
PUBLISH = ROOT / "artifacts/linux-hid-verification"
BASELINE = "3de42bb54c5e8c986364f0f04398d4e85a66a596"
PROJECT = "verification/LinuxHidRouteVerification/LinuxHidRouteVerification.csproj"
STAMP = "publish-manifest.json"
COMMANDS = [f"publish {PROJECT} -c Release -r linux-x64 --self-contained -p:PublishAot={aot} -o artifacts/linux-hid-verification/{kind}"
            for kind, aot in (("jit", "false"), ("aot", "true"))]
# Explicit build knobs only; never dump environment, NuGet credentials or configuration contents.
BUILD_ENV = ("DOTNET_ROLL_FORWARD", "DOTNET_MULTILEVEL_LOOKUP",
             "MSBuildSDKsPath", "MSBuildExtensionsPath", "NUGET_PACKAGES", "DOTNET_CLI_HOME",
             "CC", "CXX", "AR", "CFLAGS", "CXXFLAGS", "LDFLAGS", "SOURCE_DATE_EPOCH")


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def save(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n")


def source_manifest():
    names = subprocess.check_output(["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"], cwd=ROOT).decode().split("\0")
    # Conservative superset: include source assets/resources and uncommon imported config
    # extensions too, rather than assuming every effective build input has a C# suffix.
    files = {name: sha(ROOT / name) for name in sorted(set(names)) if name and (ROOT / name).is_file()
             and not name.startswith(("docs/", ".claude/", ".github/agents/"))
             and Path(name).suffix.lower() != ".md"}
    encoded = "".join(f"{value}  {name}\n" for name, value in files.items()).encode()
    return {"baseline": BASELINE, "head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
            "definition": "SHA256 of sorted UTF-8 'file-sha256  relative-path\\n'; git-visible non-Markdown source/build/config/assets, docs/agent instructions excluded",
            "sourceHash": hashlib.sha256(encoded).hexdigest(), "files": files}


def package_inputs():
    # Restore occurs before begin. Hash resolved closure, package contents (including build targets,
    # native archives and managed compiler dependencies), not only the NativeShims version label.
    packages = {}
    projects = [ROOT / PROJECT] + [ROOT / f"src/{name}/src/Yubico.YubiKit.{name}.csproj" for name in ("Core", "Ctap2", "Management")]
    closure = {}
    for project in projects:
        assets = json.loads((project.parent / "obj/project.assets.json").read_text())
        closure[str(project.relative_to(ROOT))] = sorted(assets["libraries"])
        for identity, library in assets["libraries"].items():
            if library["type"] != "package":
                continue
            relative = library["path"]
            folder = next((Path(base) / relative for base in assets["packageFolders"] if (Path(base) / relative).is_dir()), None)
            if folder is None:
                raise ValueError(f"resolved package unavailable: {identity}")
            if identity not in packages:
                packages[identity] = {str(path.relative_to(folder)): sha(path) for path in sorted(folder.rglob("*")) if path.is_file()}
    pins = ET.parse(ROOT / "Directory.Packages.props").getroot()
    return {"sdkPackageVersion": pins.findtext(".//YubiKitVersion"), "resolvedClosure": closure, "packageFiles": packages}


def current_inputs():
    source = source_manifest()
    dotnet = subprocess.check_output(["dotnet", "--version"], cwd=ROOT, text=True).strip()
    return {"source": source, "commands": COMMANDS, "sdk": dotnet,
            "environment": {name: os.environ.get(name) for name in BUILD_ENV},
            "effectiveBuild": effective_build_inputs(), "dependencies": package_inputs()}


def effective_build_inputs():
    properties = ("TargetFramework,LangVersion,Configuration,RuntimeIdentifier,SelfContained,PublishAot,PublishTrimmed,"
                  "TrimMode,InvariantGlobalization,DefineConstants,Optimize,DebugType,ContinuousIntegrationBuild,"
                  "Deterministic,TreatWarningsAsErrors,IlcOptimizationPreference,StripSymbols,UseSystemResourceKeys,"
                  "IlcGenerateStackTraceData,IlcDisableReflection,IlcInstructionSet,MSBuildSDKsPath,NETCoreSdkVersion,MSBuildAllProjects")
    result = {}
    for kind, aot in (("jit", "false"), ("aot", "true")):
        value = json.loads(subprocess.check_output(["dotnet", "msbuild", PROJECT, "-nologo", "-p:Configuration=Release",
                          "-p:RuntimeIdentifier=linux-x64", "-p:SelfContained=true", f"-p:PublishAot={aot}",
                          f"-getProperty:{properties}"], cwd=ROOT, text=True))
        imported = value["Properties"].pop("MSBuildAllProjects").split(";")
        value["importedFileHashes"] = {name: sha(Path(name)) for name in sorted(set(imported)) if name and Path(name).is_file()}
        result[kind] = value
    return result


def artifact_manifest(publish):
    result = {}
    for kind in ("jit", "aot"):
        directory = publish / kind
        binary = directory / "LinuxHidRouteVerification"
        if not binary.is_file() or not os.access(binary, os.X_OK):
            raise ValueError(f"missing published executable: {kind}")
        if kind == "jit" and not binary.with_suffix(".dll").is_file():
            raise ValueError("missing JIT host assembly")
        for path in sorted(directory.rglob("*")):
            if path.is_symlink():
                raise ValueError("published symlink is outside this binding contract")
            if path.is_file():
                result[str(path.relative_to(publish))] = sha(path)
    return result


def seal(publish, before, after):
    (publish / STAMP).unlink(missing_ok=True)
    if before != after:
        raise ValueError("publish inputs changed during publish; no valid manifest issued")
    stamp = {"schema": 1, "publishedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
             "binding": "local stable-input publish record, not a signature or environment-label attestation",
             "sourceHash": before["source"]["sourceHash"], "inputHash": digest(before), "inputs": before,
             "artifacts": artifact_manifest(publish)}
    save(publish / STAMP, stamp)
    return stamp


def verify_publish(publish=PUBLISH, inputs=None):
    path = publish / STAMP
    if not path.is_file():
        raise ValueError("missing publish manifest; run linux-hid-verification-publish")
    stamp = json.loads(path.read_text())
    actual = current_inputs() if inputs is None else inputs
    if stamp.get("schema") != 1 or stamp.get("inputHash") != digest(stamp.get("inputs")):
        raise ValueError("invalid publish manifest input digest")
    if stamp.get("sourceHash") != actual["source"]["sourceHash"] or stamp["inputs"] != actual:
        raise ValueError("stale publish source/effective inputs; republish required")
    if stamp.get("artifacts") != artifact_manifest(publish):
        raise ValueError("stale published binary/assembly/artifact; republish required")
    return stamp


def main():
    if sys.argv[1:] == ["begin"]:
        PUBLISH.mkdir(parents=True, exist_ok=True)
        (PUBLISH / STAMP).unlink(missing_ok=True)
        save(PUBLISH / "before-publish.json", current_inputs())
        print("Publish input snapshot recorded; previous stamp invalidated")
    elif sys.argv[1:] == ["seal"]:
        stamp = seal(PUBLISH, json.loads((PUBLISH / "before-publish.json").read_text()), current_inputs())
        print(f"BOUND SOURCEHASH={stamp['sourceHash']} INPUTHASH={stamp['inputHash']} artifacts={len(stamp['artifacts'])}")
    elif sys.argv[1:] == ["verify"]:
        print(f"VERIFIED SOURCEHASH={verify_publish()['sourceHash']}")
    else:
        raise ValueError("Usage: publish_contract.py begin|seal|verify")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, KeyError) as error:
        print(f"FAIL publish binding: {error}", file=sys.stderr)
        sys.exit(1)
