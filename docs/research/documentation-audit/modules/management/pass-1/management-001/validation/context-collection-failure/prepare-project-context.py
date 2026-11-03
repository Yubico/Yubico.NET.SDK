"""Prepare/check/collect a real SDK xUnit2 compilation context. Never invokes a build or test."""

import argparse
import copy
import hashlib
import json
import os
from pathlib import Path
import xml.etree.ElementTree as ET


HERE = Path(__file__).resolve().parent
PRIMARY = next(parent for parent in HERE.parents if (parent / "toolchain.cs").is_file())
CONTEXT = HERE / "xunit2-context"
SOURCE_PROJECT = "src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj"
# Explicit, approved dependency-context membership; never infer routing from a failed compiler exit.
CONTEXT_GROUPS = [
    "guide-01", "guide-02", "guide-03", "guide-13", "guide-14", "guide-15", "guide-16",
    "guide-17", "guide-18", "guide-19", "module-tests-1", "module-tests-2",
    "shared-helper-xml-1", "shared-helper-xml-2", "shared-helper-xml-3",
    "shared-attribute-xml-1", "shared-attribute-xml-2", "shared-readme-1", "shared-readme-4",
    "shared-readme-5", "shared-readme-6", "shared-readme-7", "testing-11", "testing-12",
    "testing-13", "testing-15",
]
DIRECT_REFERENCES = [
    "src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll",
    "src/Management/src/bin/Release/net10.0/Yubico.YubiKit.Management.dll",
    "src/Tests.Shared/bin/Release/net10.0/Yubico.YubiKit.Tests.Shared.dll",
    "src/SecurityDomain/src/bin/Release/net10.0/Yubico.YubiKit.SecurityDomain.dll",
    "src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/bin/Release/net10.0/Yubico.YubiKit.SecurityDomain.IntegrationTests.dll",
    "src/Oath/src/bin/Release/net10.0/Yubico.YubiKit.Oath.dll",
]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_xml(path, element):
    ET.indent(element, space="  ")
    path.write_text(ET.tostring(element, encoding="unicode") + "\n", encoding="utf-8")


def read_groups(repo):
    manifest_path = HERE.parent / "compiled-examples/manifest.json"
    manifest = json.loads(manifest_path.read_text())
    if manifest["primary_root"] != str(repo) or manifest["guide_root"] != str(repo):
        raise ValueError("Regenerate extracted examples against final PRIMARY without --guide-root first")
    groups = manifest["groups"]
    names = [group["name"] for group in groups]
    if len(names) != 63 or len(set(names)) != 63 or not set(CONTEXT_GROUPS) <= set(names):
        raise ValueError("The approved 63-group inventory changed; reclassify explicitly")
    for group in groups:
        path = manifest_path.parent / group["source"]
        if sha(path) != group["source_sha256"]:
            raise ValueError(f"Stale generated source: {group['name']}")
        extraction = group["extraction"]
        if sha(repo / extraction["path"]) != extraction["document_sha256"]:
            raise ValueError(f"Source document changed: {extraction['path']}; rerun extraction")
    return manifest_path, manifest, groups


def project_closure(repo):
    """Retain the root project's package items and its genuine ProjectReference package closure."""
    pending = [SOURCE_PROJECT]
    visited = []
    package_sources = {}
    package_items = {}
    while pending:
        relative = pending.pop(0)
        if relative in visited:
            continue
        visited.append(relative)
        path = repo / relative
        project = ET.parse(path).getroot()
        if project.attrib.get("Sdk") != "Microsoft.NET.Sdk":
            raise ValueError(f"Unexpected source SDK: {relative}")
        for item_group in project.findall("ItemGroup"):
            relevant = [item for item in item_group if item.tag in {"PackageReference", "ProjectReference"}]
            if relevant and (item_group.get("Condition") or any(item.get("Condition") for item in relevant)):
                raise ValueError(f"Conditional package/project items need explicit context review: {relative}")
        for item in project.findall(".//PackageReference"):
            name = item.attrib["Include"]
            package_sources.setdefault(name, []).append(relative)
            # The real integration project's direct metadata wins over inherited references.
            package_items.setdefault(name, copy.deepcopy(item))
        for item in project.findall(".//ProjectReference"):
            child = (path.parent / item.attrib["Include"].replace("\\", "/")).resolve(strict=True)
            pending.append(str(child.relative_to(repo)))
    return visited, package_items, package_sources


def prepare(repo):
    manifest_path, manifest, groups = read_groups(repo)
    projects, packages, package_sources = project_closure(repo)
    original = ET.parse(repo / SOURCE_PROJECT).getroot()
    properties = original.find("PropertyGroup")
    if properties is None or properties.findtext("TargetFramework") != "net10.0" or properties.findtext("IsTestProject") != "true":
        raise ValueError("Original framework/test-project configuration changed; review context alignment")
    package_document = ET.Element("Project")
    package_group = ET.SubElement(package_document, "ItemGroup")
    for item in packages.values():
        package_group.append(item)
    using_group = ET.SubElement(package_document, "ItemGroup")
    for item in original.findall(".//Using"):
        using_group.append(copy.deepcopy(item))
    write_xml(CONTEXT / "Context.Packages.props", package_document)

    items = ET.Element("Project")
    compile_group = ET.SubElement(items, "ItemGroup")
    reference_group = ET.SubElement(items, "ItemGroup")
    for group in groups:
        if group["name"] in CONTEXT_GROUPS:
            path = (manifest_path.parent / group["source"]).resolve(strict=True)
            item = ET.SubElement(compile_group, "Compile", Include="$(RepositoryRoot)/" + str(path.relative_to(repo)))
            ET.SubElement(item, "Link").text = "Snippets/" + path.name
    for reference in DIRECT_REFERENCES:
        item = ET.SubElement(reference_group, "Reference", Include=Path(reference).stem)
        ET.SubElement(item, "HintPath").text = "$(RepositoryRoot)/" + reference
    write_xml(CONTEXT / "Context.Items.props", items)

    routed = []
    raw_groups = []
    for group in groups:
        source = (manifest_path.parent / group["source"]).resolve(strict=True)
        route = "sdk-xunit2" if group["name"] in CONTEXT_GROUPS else "strict-raw"
        routed.append(dict(name=group["name"], route=route, source=str(source.relative_to(repo)),
                           source_sha256=group["source_sha256"], extraction=group["extraction"],
                           extracted_sha256=group["extracted_sha256"]))
        if route == "strict-raw":
            raw = copy.deepcopy(group)
            raw["source"] = os.path.relpath(source, CONTEXT)
            raw["raw_source"] = os.path.relpath(manifest_path.parent / group["raw_source"], CONTEXT)
            raw_groups.append(raw)
    if len(raw_groups) != 37 or len(CONTEXT_GROUPS) != 26:
        raise ValueError("Context/raw partition is not exactly 26 + 37")
    (CONTEXT / "strict-raw-manifest.json").write_text(json.dumps(dict(
        origin_manifest=str(manifest_path.relative_to(repo)), origin_sha256=sha(manifest_path),
        groups=raw_groups), indent=2) + "\n")

    config_paths = ["Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", ".editorconfig", "global.json", *projects]
    config_paths += [str(path.relative_to(repo)) for path in [
        HERE / "prepare-project-context.py", CONTEXT / "Directory.Build.props", CONTEXT / "Directory.Build.targets",
        CONTEXT / "src/Yubico.YubiKit.Management.DocExamples.Xunit2.csproj",
        CONTEXT / "Context.Packages.props", CONTEXT / "Context.Items.props", CONTEXT / "strict-raw-manifest.json",
    ]]
    references = sorted(set(DIRECT_REFERENCES) | {item["path"] for item in manifest["reference_inventory"]})
    context_manifest = dict(
        schema_version=1, repo=str(repo), source_project=SOURCE_PROJECT,
        source_project_closure=projects, package_origins=package_sources,
        origin_manifest=str(manifest_path.relative_to(repo)), origin_sha256=sha(manifest_path),
        groups=routed, context_groups=CONTEXT_GROUPS, strict_raw_groups=[group["name"] for group in raw_groups],
        configurations=[dict(path=path, sha256=sha(repo / path)) for path in dict.fromkeys(config_paths)],
        references=[dict(path=path, sha256=sha(repo / path)) for path in references],
        limits="Prepared snapshot only; no project build, restore, analyzers or snippets executed")
    (CONTEXT / "context-manifest.json").write_text(json.dumps(context_manifest, indent=2) + "\n")
    print("Prepared SDK xUnit2 context: 26 exact wrapper sources; 37 strict-raw groups; no build/restore/execution.")


def check(repo):
    manifest = json.loads((CONTEXT / "context-manifest.json").read_text())
    if manifest["repo"] != str(repo) or sha(repo / manifest["origin_manifest"]) != manifest["origin_sha256"]:
        raise ValueError("Extraction manifest changed; rerun prepare-project-context.py")
    read_groups(repo)
    for record in manifest["configurations"] + manifest["references"]:
        if sha(repo / record["path"]) != record["sha256"]:
            raise ValueError(f"Prepared input changed: {record['path']}; rerun preparation after final builds")
    print("PASS: prepared source/document/configuration/reference hashes still match; no compilation performed.")
    return manifest


def collect(repo, workspace):
    manifest = check(repo)
    workspace = workspace.resolve(strict=True)
    if not workspace.is_relative_to(Path("/tmp/opencode")):
        raise ValueError("Evidence workspace must be under /tmp/opencode")
    files = ["compile-inputs.txt", "resolved-references.txt", "analyzers.txt", "csc-command-line.txt", "policy-final.txt"]
    policy = dict(line.split("=", 1) for line in (workspace / "policy-final.txt").read_text().splitlines() if "=" in line)
    for name in ["TreatWarningsAsErrors", "EnableNETAnalyzers", "RunAnalyzersDuringBuild"]:
        if policy.get(name, "").lower() != "true":
            raise ValueError(f"Unexpected compiler/analyzer policy {name}: {policy.get(name)}")
    if policy.get("SkipCompilerExecution", "").lower() == "true" or policy.get("AnalysisMode") != "Recommended":
        raise ValueError("Compiler execution or inherited test analysis context changed")
    if policy.get("RunAnalyzers", "").lower() == "false" or policy.get("SkipAnalyzers", "").lower() == "true":
        raise ValueError("Actual analyzer execution was disabled")
    records = {}
    for name in ["compile-inputs.txt", "resolved-references.txt", "analyzers.txt"]:
        values = []
        for line in (workspace / name).read_text().splitlines():
            if line:
                path, _, identity = line.partition("|")
                actual = Path(path).resolve(strict=True)
                values.append(dict(path=str(actual), identity=identity, sha256=sha(actual)))
        records[name] = values
    expected_sources = {str(repo / group["source"]) for group in manifest["groups"] if group["route"] == "sdk-xunit2"}
    compiled = {record["path"] for record in records["compile-inputs.txt"]}
    # Microsoft.NET.Test.Sdk adds its genuine package-supplied entry-point source, just as in the
    # original integration project. It is compiled but never executed; do not replace or suppress it.
    sdk_program = policy.get("GeneratedProgramFile", "")
    allowed_sdk_sources = set()
    if sdk_program:
        sdk_path = Path(sdk_program).resolve(strict=True)
        if sdk_path.name != "Microsoft.NET.Test.Sdk.Program.cs" or "microsoft.net.test.sdk" not in str(sdk_path).lower():
            raise ValueError("Unexpected package-generated program source")
        allowed_sdk_sources.add(str(sdk_path))
    if not expected_sources <= compiled or any(path not in expected_sources | allowed_sdk_sources and not Path(path).is_relative_to(workspace) for path in compiled):
        raise ValueError("Actual Compile inputs are missing wrappers or contain unexpected source outside generated workspace")
    resolved = {record["path"] for record in records["resolved-references.txt"]}
    if not {str(repo / path) for path in DIRECT_REFERENCES} <= resolved:
        raise ValueError("A required real Release metadata reference was not passed to Csc")
    analyzers = records["analyzers.txt"]
    if not any("xunit.analyzers" in item["path"].lower() for item in analyzers) or not any("Microsoft.CodeAnalysis.NetAnalyzers.dll" in item["path"] for item in analyzers):
        raise ValueError("Expected real xUnit/NET analyzer metadata is absent")
    result = dict(note="Input/policy evidence only; require separate toolchain PASSED + Warnings: 0 before claiming a successful gate",
                  prepared_manifest_sha256=sha(CONTEXT / "context-manifest.json"), policy=policy,
                  captured_files=[dict(path=str(workspace / name), sha256=sha(workspace / name)) for name in files],
                  actual_compiler_inputs=records, project_assets_sha256=sha(workspace / "obj/project.assets.json"))
    destination = CONTEXT / "evidence"
    destination.mkdir(exist_ok=True)
    target = destination / "resolved-inputs.json"
    if target.exists():
        raise ValueError("Evidence already exists; retain it separately before collecting another run")
    target.write_text(json.dumps(result, indent=2) + "\n")
    for name in files:
        (destination / name).write_bytes((workspace / name).read_bytes())
    print(f"Collected actual source/reference/analyzer hashes and Csc policy: {destination}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=PRIMARY)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--check", action="store_true")
    modes.add_argument("--collect", action="store_true")
    parser.add_argument("--workspace", type=Path, default=Path("/tmp/opencode/management-001-xunit2-context"))
    args = parser.parse_args()
    repo = args.repo.resolve(strict=True)
    if args.collect:
        collect(repo, args.workspace)
    elif args.check:
        check(repo)
    else:
        prepare(repo)


if __name__ == "__main__":
    main()
