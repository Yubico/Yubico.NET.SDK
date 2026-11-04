"""Adapt Management's final SDK context collector. Prepare/check/collect only; never build or test."""

import argparse
import copy
import hashlib
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET


HERE = Path(__file__).resolve().parent
PRIMARY = next(parent for parent in HERE.parents if (parent / "toolchain.cs").is_file())
BASELINE = "5b09a9b6942ded3913b5c9340c3e126617e61a82"
PROPERTY_NAMES = {"TargetFramework", "OutputType", "UseMicrosoftTestingPlatformRunner", "AllowUnsafeBlocks"}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_xml(path, element):
    ET.indent(element, space="  ")
    path.write_text(ET.tostring(element, encoding="unicode") + "\n", encoding="utf-8")


def read_inputs(repo, manifest_path, context_id):
    configuration = json.loads(manifest_path.read_text())
    if configuration["baseline"] != BASELINE:
        raise ValueError("Context configuration has the wrong CTAP2 baseline")
    contexts = configuration["contexts"]
    ids = [item["id"] for item in contexts]
    if not ids or len(set(ids)) != len(ids) or any(not re.fullmatch(r"[a-z][a-z0-9-]*", name) for name in ids):
        raise ValueError("Require unique safe context IDs; no inferred groups or runner-shaped paths")
    if any(word in name.lower() for name in ids for word in ("ctap2", "unittests", "integrationtests")):
        raise ValueError("Context IDs must not introduce module/test project-filter collisions")
    context = next(item for item in contexts if item["id"] == context_id)
    if context["kind"] not in {"consumer", "sample", "xunit2", "xunit3"}:
        raise ValueError("Unknown context kind")
    if context["output_type"] not in {"Library", "Exe"} or context["kind"] == "xunit3" and context["output_type"] != "Exe":
        raise ValueError("Explicit compile host output type required; preserve xUnit3's genuine generated entry point")
    extraction_path = (manifest_path.parent / configuration["extraction_manifest"]).resolve(strict=True)
    extraction = json.loads(extraction_path.read_text())
    if extraction["primary_root"] != str(repo) or extraction["guide_root"] != str(repo):
        raise ValueError("Regenerate extraction against the final integrated checkout")
    groups = extraction["groups"]
    names = [group["name"] for group in groups]
    if not names or len(set(names)) != len(names):
        raise ValueError("Require nonempty uniquely named extraction groups")
    assigned = [name for item in contexts for name in item["groups"]]
    raw = [group["name"] for group in groups if group["context"] == "strict-raw"]
    if len(set(assigned)) != len(assigned) or set(assigned) & set(raw) or set(assigned) | set(raw) != set(names):
        raise ValueError("SDK/strict-raw membership must be explicit, disjoint and exhaustive")
    for item in contexts:
        if not item["groups"] or any(group["context"] != item["id"] for group in groups if group["name"] in item["groups"]):
            raise ValueError("Nonempty context membership must agree with extraction routing")
    for group in groups:
        source = (extraction_path.parent / group["source"]).resolve(strict=True)
        if not source.is_relative_to(HERE.parent) or source.suffix != ".cs" or sha(source) != group["source_sha256"]:
            raise ValueError(f"Stale or out-of-run generated source: {group['name']}")
        original = group["extraction"]
        if sha(repo / original["path"]) != original["document_sha256"]:
            raise ValueError(f"Source document changed: {original['path']}")
        raw_source = (extraction_path.parent / group["raw_source"]).resolve(strict=True)
        if sha(raw_source) != group["raw_sha256"] or hashlib.sha256(group["extracted_text"].encode()).hexdigest() != group["extracted_sha256"]:
            raise ValueError(f"Stale original extraction: {group['name']}")
        if not group["provenance"] or not isinstance(group["prerequisites"], list):
            raise ValueError("Require provenance and explicit typed wrapper prerequisites")
    selected = [group for group in groups if group["name"] in context["groups"]]
    return context, extraction_path, extraction, selected


def project_closure(repo, source_project):
    """Preserve genuine project package closure; root project's direct metadata wins."""
    pending = [source_project]
    visited, package_sources, package_items = [], {}, {}
    while pending:
        relative = pending.pop(0)
        if relative in visited:
            continue
        visited.append(relative)
        path = (repo / relative).resolve(strict=True)
        if not path.is_relative_to(repo):
            raise ValueError("Project closure escaped repository")
        project = ET.parse(path).getroot()
        if project.attrib.get("Sdk") != "Microsoft.NET.Sdk" or project.findall("Import"):
            raise ValueError(f"Source SDK/imports require explicit adaptation: {relative}")
        for item_group in project.findall("ItemGroup"):
            relevant = [item for item in item_group if item.tag in {"PackageReference", "ProjectReference", "Using"}]
            if relevant and (item_group.get("Condition") or any(item.get("Condition") for item in relevant)):
                raise ValueError(f"Conditional dependency/using items require review: {relative}")
        # Root props/targets are the only inherited build configuration this bounded adapter models.
        for parent in path.parents:
            if parent == repo:
                break
            if any((parent / name).exists() for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props")):
                raise ValueError(f"Intermediate build configuration requires explicit adaptation: {parent}")
        for item in project.findall(".//PackageReference"):
            name = item.attrib["Include"]
            package_sources.setdefault(name, []).append(relative)
            package_items.setdefault(name, copy.deepcopy(item))
        for item in project.findall(".//ProjectReference"):
            child = (path.parent / item.attrib["Include"].replace("\\", "/")).resolve(strict=True)
            pending.append(str(child.relative_to(repo)))
    return visited, package_items, package_sources


def prepare(repo, manifest_path, context_id):
    context, extraction_path, extraction, groups = read_inputs(repo, manifest_path, context_id)
    destination = HERE / "context" / context_id
    if destination.exists():
        raise ValueError("Context already exists; retain it and choose a fresh context ID for regenerated evidence")
    projects, packages, origins = project_closure(repo, context["source_project"])
    package_names = set(packages)
    v2 = "xunit" in package_names
    v3 = any(name.startswith("xunit.v3") for name in package_names)
    if v2 and v3 or (context["kind"] == "xunit2") != v2 or (context["kind"] == "xunit3") != v3:
        raise ValueError("Context kind must match genuine package closure; xUnit2 and xUnit3 stay separate")
    original = ET.parse(repo / context["source_project"]).getroot()
    properties = ET.Element("PropertyGroup")
    for group in original.findall("PropertyGroup"):
        for item in group:
            if item.tag in PROPERTY_NAMES:
                if group.get("Condition") or item.get("Condition"):
                    raise ValueError("Conditional source compiler properties require review")
                properties.append(copy.deepcopy(item))
    if properties.findtext("TargetFramework", "net10.0") != "net10.0":
        raise ValueError("Only the repository's net10.0 context is modeled")
    output_type = properties.find("OutputType")
    if output_type is None:
        output_type = ET.SubElement(properties, "OutputType")
    # A factored method-only wrapper can be a library; complete top-level programs need Exe.
    # This selects the compile host shape, never alters the retained example's statements.
    output_type.text = context["output_type"]
    ET.SubElement(properties, "IsPackable").text = "false"
    ET.SubElement(properties, "IsTestProject").text = "true" if v2 or v3 else "false"
    ET.SubElement(properties, "EnableDefaultCompileItems").text = "false"
    ET.SubElement(properties, "ProvideCommandLineArgs").text = "true"
    # No source-project NoWarn is copied. Canonical root/package/SDK policy is imported as-is.
    package_document = ET.Element("Project")
    package_group = ET.SubElement(package_document, "ItemGroup")
    for item in packages.values():
        package_group.append(item)
    using_group = ET.SubElement(package_document, "ItemGroup")
    for item in original.findall(".//Using"):
        using_group.append(copy.deepcopy(item))
    references = {
        "src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll",
        "src/Ctap2/src/bin/Release/net10.0/Yubico.YubiKit.Ctap2.dll",
        *context.get("references", []),
    }
    modules = set(context.get("modules", [])) | {module for group in groups for module in group.get("modules", [])}
    if any(not re.fullmatch(r"[A-Za-z0-9.]+", module) for module in modules):
        raise ValueError("Additional SDK modules must be names, not paths")
    references |= {f"src/{module}/src/bin/Release/net10.0/Yubico.YubiKit.{module}.dll" for module in modules}
    references |= {path for group in groups for path in group.get("references", [])}
    for reference in references:
        path = (repo / reference).resolve(strict=True)
        if path.suffix.lower() != ".dll" or not path.is_relative_to(repo):
            raise ValueError("Supply real repository-built/resolved DLL references explicitly")
        name = path.name.lower()
        if (v3 and (name.startswith("xunit.") and not name.startswith("xunit.v3.") or name in {"xunit.skippablefact.dll", "yubico.yubikit.tests.shared.dll"})) or (v2 and name.startswith("xunit.v3.")):
            raise ValueError("Explicit references mix xUnit generations")
    items = ET.Element("Project")
    compile_group = ET.SubElement(items, "ItemGroup")
    reference_group = ET.SubElement(items, "ItemGroup")
    for group in groups:
        path = (extraction_path.parent / group["source"]).resolve(strict=True)
        ET.SubElement(compile_group, "Compile", Include="$(RepositoryRoot)/" + str(path.relative_to(repo)))
    for reference in sorted(references):
        item = ET.SubElement(reference_group, "Reference", Include=Path(reference).stem)
        ET.SubElement(item, "HintPath").text = "$(RepositoryRoot)/" + reference
    (destination / "src").mkdir(parents=True)
    for name in ("Directory.Build.props", "Directory.Build.targets"):
        text = (HERE / "context" / (name + ".template")).read_text()
        (destination / name).write_text(text.replace("@CONTEXT@", context_id).replace("@IS_TEST@", "true" if v2 or v3 else "false"))
    write_xml(destination / "Context.Packages.props", package_document)
    write_xml(destination / "Context.Items.props", items)
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    project.append(properties)
    ET.SubElement(project, "Import", Project="../Context.Packages.props")
    ET.SubElement(project, "Import", Project="../Context.Items.props")
    project_path = destination / "src" / f"Yubico.YubiKit.AuditCt001.Examples.{context_id}.csproj"
    write_xml(project_path, project)
    # Capture all genuine project/configuration/package inputs as well as exact wrappers and DLLs.
    config_paths = [repo / name for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", ".editorconfig", "global.json")]
    config_paths += [repo / path for path in projects]
    config_paths += [manifest_path, extraction_path, Path(__file__).resolve(), *destination.glob("*.props"), destination / "Directory.Build.targets", project_path,
                     * (HERE / "context").glob("*.template")]
    inputs = config_paths + [repo / path for path in sorted(references)]
    for group in groups:
        inputs += [extraction_path.parent / group["source"], extraction_path.parent / group["raw_source"], repo / group["extraction"]["path"]]
    snapshot = dict(schema_version=1, baseline=BASELINE, repo=str(repo), context=context,
                    source_project_closure=projects, package_origins=origins,
                    groups=[dict(name=group["name"], source=str((extraction_path.parent / group["source"]).resolve())) for group in groups],
                    direct_references=sorted(references),
                    inputs=[dict(path=str(path.resolve()), sha256=sha(path)) for path in dict.fromkeys(inputs)],
                    limits="Prepared inputs only; no restore/build/analyzer/snippet execution. Source-local warning suppressions not copied.")
    (destination / "context-manifest.json").write_text(json.dumps(snapshot, indent=2) + "\n")
    print(f"Prepared {context_id}: {len(groups)} exact wrappers. Future command: dotnet toolchain.cs -- build --project AuditCt001.Examples.{context_id} --clean")


def check(repo, manifest_path, context_id):
    read_inputs(repo, manifest_path, context_id)
    snapshot = json.loads((HERE / "context" / context_id / "context-manifest.json").read_text())
    if snapshot["repo"] != str(repo) or snapshot["baseline"] != BASELINE:
        raise ValueError("Prepared context root/baseline changed")
    for record in snapshot["inputs"]:
        if sha(Path(record["path"])) != record["sha256"]:
            raise ValueError(f"Prepared input changed: {record['path']}; retain/reprepare with a fresh ID")
    print("PASS: prepared input hashes match; no compilation performed.")
    return snapshot


def collect(repo, manifest_path, context_id):
    snapshot = check(repo, manifest_path, context_id)
    workspace = Path(f"/tmp/opencode/ctap2-001-{context_id}-context").resolve(strict=True)
    if not workspace.is_relative_to(Path("/tmp/opencode")):
        raise ValueError("Evidence workspace escaped approved temporary root")
    files = ["compile-inputs.txt", "resolved-references.txt", "analyzers.txt", "analyzer-configs.txt", "csc-command-line.txt", "policy-final.txt"]
    policy = dict(line.split("=", 1) for line in (workspace / "policy-final.txt").read_text().splitlines() if "=" in line)
    for name in ("TreatWarningsAsErrors", "RunAnalyzersDuringBuild"):
        if policy.get(name, "").lower() != "true":
            raise ValueError(f"Unexpected compiler/analyzer policy {name}")
    expected_mode = "Recommended" if snapshot["context"]["kind"].startswith("xunit") else "AllEnabledByDefault"
    if policy.get("AnalysisMode") != expected_mode or policy.get("SkipCompilerExecution", "").lower() == "true":
        raise ValueError("Compiler execution or canonical analysis context changed")
    if policy.get("RunAnalyzers", "").lower() == "false" or policy.get("SkipAnalyzers", "").lower() == "true":
        raise ValueError("Actual analyzer execution disabled")
    if policy.get("TargetFramework") != "net10.0" or policy.get("LangVersion") not in {"14", "14.0"} or policy.get("WarningLevel") != "9999":
        raise ValueError("Framework/language/preview warning policy changed")
    records = {}
    for name in files[:4]:
        values = []
        for line in (workspace / name).read_text().splitlines():
            if line:
                path, _, identity = line.partition("|")
                actual = Path(path).resolve(strict=True)
                values.append(dict(path=str(actual), identity=identity, sha256=sha(actual)))
        records[name] = values
    expected = {group["source"] for group in snapshot["groups"]}
    compiled = {record["path"] for record in records["compile-inputs.txt"]}
    sdk_program = policy.get("GeneratedProgramFile", "")
    allowed = set()
    if sdk_program:
        path = Path(sdk_program).resolve(strict=True)
        if path.name != "Microsoft.NET.Test.Sdk.Program.cs" or "microsoft.net.test.sdk" not in str(path).lower():
            raise ValueError("Unexpected package-generated entry-point source")
        allowed.add(str(path))
    if not expected <= compiled or any(path not in expected | allowed and not Path(path).is_relative_to(workspace) for path in compiled):
        raise ValueError("Missing wrappers or unexpected non-generated compilation source")
    resolved = {item["path"] for item in records["resolved-references.txt"]}
    if not {str((repo / path).resolve()) for path in snapshot["direct_references"]} <= resolved:
        raise ValueError("Required real DLL reference not passed to Csc")
    resolved_names = {Path(path).name.lower() for path in resolved}
    kind = snapshot["context"]["kind"]
    if kind == "xunit3" and (any(name.startswith("xunit.") and not name.startswith("xunit.v3.") for name in resolved_names)
                            or resolved_names & {"xunit.skippablefact.dll", "yubico.yubikit.tests.shared.dll"}):
        raise ValueError("Actual compiler references mix xUnit3 with xUnit2/harness dependencies")
    if kind != "xunit3" and any(name.startswith("xunit.v3.") for name in resolved_names):
        raise ValueError("Actual compiler references introduce unapproved xUnit3 dependencies")
    analyzers = records["analyzers.txt"]
    if not any(item["path"].endswith("Microsoft.CodeAnalysis.NetAnalyzers.dll") for item in analyzers):
        raise ValueError("Actual NET analyzer input absent")
    if snapshot["context"]["kind"].startswith("xunit") and not any("xunit.analyzers" in item["path"].lower() for item in analyzers):
        raise ValueError("Actual xUnit analyzer input absent")
    # FINAL Management correction: NuGet disables duplicate SDK copies, not analyzer execution.
    versions = ET.parse(repo / "Directory.Packages.props").getroot().findall(".//PackageVersion")
    version = next(item.attrib["Version"] for item in versions if item.attrib.get("Include") == "Microsoft.CodeAnalysis.NetAnalyzers")
    if policy.get("EnableNETAnalyzers", "").lower() not in {"true", "false"}:
        raise ValueError("Missing SDK analyzer-selection policy")
    if policy["EnableNETAnalyzers"].lower() == "false" and not any(
        f"microsoft.codeanalysis.netanalyzers/{version}/analyzers/" in item["path"].lower()
        and item["path"].endswith("Microsoft.CodeAnalysis.NetAnalyzers.dll") for item in analyzers
    ):
        raise ValueError("SDK analyzer flag false without canonical NuGet analyzer replacement")
    if not (workspace / "csc-command-line.txt").read_text().strip() or not records["analyzer-configs.txt"]:
        raise ValueError("Missing actual compiler command line/analyzer configuration inputs")
    destination = HERE / "context" / context_id / "evidence"
    if destination.exists():
        raise ValueError("Evidence already retained; never overwrite it")
    result = dict(note="Input/policy evidence only; separately require fresh toolchain PASSED + Warnings: 0",
                  prepared_manifest_sha256=sha(destination.parent / "context-manifest.json"), policy=policy,
                  actual_compiler_inputs=records, project_assets_sha256=sha(workspace / "obj/project.assets.json"),
                  captured_files=[dict(path=str(workspace / name), sha256=sha(workspace / name)) for name in files])
    destination.mkdir()
    (destination / "resolved-inputs.json").write_text(json.dumps(result, indent=2) + "\n")
    for name in files:
        (destination / name).write_bytes((workspace / name).read_bytes())
    print(f"Collected actual compiler/analyzer inputs: {destination}; not itself a build-pass gate.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=PRIMARY)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--context", required=True)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--check", action="store_true")
    modes.add_argument("--collect", action="store_true")
    args = parser.parse_args()
    repo, manifest = args.repo.resolve(strict=True), args.manifest.resolve(strict=True)
    if manifest != HERE / "context/contexts-manifest.json":
        raise ValueError("Retain approved context configuration at checker-source/context/contexts-manifest.json")
    action = collect if args.collect else check if args.check else prepare
    action(repo, manifest, args.context)


if __name__ == "__main__":
    main()
