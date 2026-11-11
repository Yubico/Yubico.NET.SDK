"""Read-only source/preservation inspection. This does not compile or run SDK code."""

from collections import Counter
import hashlib
import json
from pathlib import Path
import subprocess

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
CAMPAIGN = "f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa"
INCOMING = "5746e72ed9a848d08b5782ddde025aa4a4b522ef"
BASE = "b1362127203beda5e9c797e8648b28e2716c496c"


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def blob(revision, path):
    result = subprocess.run(["git", "show", f"{revision}:{path}"], cwd=ROOT, capture_output=True)
    return result.stdout if result.returncode == 0 else None


def clean_blob(path):
    if not (ROOT / path).exists():
        return None
    oid = git("hash-object", f"--path={path}", path).decode().strip()
    # hash-object applies the repository's clean filters but does not write an object.
    return oid


def main():
    before = json.loads((HERE / "evidence/preservation-before.json").read_text())
    failures = []
    archives = []
    for archive in before["archives"]:
        path = ROOT / archive["path"]
        if digest(path) != archive["sha256"]:
            failures.append(str(path))
        manifest = json.loads(path.read_text())
        scope = path.parent / "records" if path.parent.name == "documentation-audit" else path.parent
        for item in manifest["files"]:
            target = scope / item["path"]
            if not target.is_file() or digest(target) != item["sha256"] or target.stat().st_size != item["bytes"]:
                failures.append(str(target))
        archives.append({"path": archive["path"], "manifest_sha256": digest(path), "artifacts": len(manifest["files"])})
    for item in before["prior_integration_files"]["files"]:
        if digest(ROOT / item["path"]) != item["sha256"]:
            failures.append(item["path"])
    primary = Path(before["primary"])
    for item in before["original_user_files"]:
        path = primary / item["path"]
        if digest(path) != item["sha256"] or path.stat().st_size != item["bytes"]:
            failures.append(str(path))
    (HERE / "evidence/preservation-after-resolution.json").write_text(json.dumps({
        "archives": archives,
        "artifact_total": sum(row["artifacts"] for row in archives),
        "prior_integration_files": len(before["prior_integration_files"]["files"]),
        "original_user_files": len(before["original_user_files"]),
        "primary_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=primary).decode().strip(),
        "failures": failures,
        "scope": "Exact filesystem bytes; read-only inspection of primary user files and HEAD. No device/compiler execution.",
    }, indent=2) + "\n")
    api_path = "src/Core/src/PublicAPI.Unshipped.txt"

    def entries(data):
        return [line for line in data.decode().splitlines() if line.strip() and not line.startswith("#")]

    snapshots = {label: entries(blob(revision, api_path)) for label, revision in
                 (("base", BASE), ("campaign", CAMPAIGN), ("incoming", INCOMING))}
    snapshots["resolved"] = entries((ROOT / api_path).read_bytes())
    duplicates = [line for line, count in Counter(snapshots["resolved"]).items() if count > 1]
    api = {
        "counts": {key: len(value) for key, value in snapshots.items()},
        "duplicates": duplicates,
        "campaign_additions": sorted(set(snapshots["campaign"]) - set(snapshots["base"])),
        "incoming_removed": sorted(set(snapshots["base"]) - set(snapshots["incoming"])),
        "incoming_added": sorted(set(snapshots["incoming"]) - set(snapshots["base"])),
        "resolved_extra_over_incoming": sorted(set(snapshots["resolved"]) - set(snapshots["incoming"])),
        "incoming_missing_from_resolved": sorted(set(snapshots["incoming"]) - set(snapshots["resolved"])),
        "qualification": "Snapshot line identities, including generated record members. Source declarations and callers reviewed separately; compiler/analyzer agreement awaits assigned gates.",
    }
    (HERE / "evidence/api-reconciliation.json").write_text(json.dumps(api, indent=2) + "\n")
    inputs = json.loads((HERE / "evidence/resolver-inputs/overlaps.json").read_text())
    rows = []
    for row in inputs["paths"]:
        path = row["path"]
        oid = clean_blob(path)
        rows.append({"path": path, "resolved_blob": oid, "equals_parent": [
            label for label, value in row["parents"].items()
            if (value is None and oid is None) or (value is not None and value["blob"] == oid)
        ]})
    (HERE / "evidence/overlap-resolution-identities.json").write_text(json.dumps(rows, indent=2) + "\n")
    # Every incoming shipping Core C# file is byte-identical after clean filters; the only overlapping
    # campaign Core source edit is UserPresenceIntent, whose campaign blob remains intact.
    core = []
    for path in git("ls-tree", "-r", "--name-only", INCOMING, "--", "src/Core/src").decode().splitlines():
        if path.endswith(".cs"):
            actual = clean_blob(path)
            expected = git("rev-parse", f"{INCOMING}:{path}").decode().strip()
            if actual != expected:
                core.append({"path": path, "resolved": actual, "incoming": expected,
                             "equals_campaign": blob(CAMPAIGN, path) is not None and
                             actual == git("rev-parse", f"{CAMPAIGN}:{path}").decode().strip()})
    oath_changes = []
    for path in git("diff", "--name-only", BASE, CAMPAIGN, "--", "src/Oath").decode().splitlines():
        if path.endswith(".cs") and (ROOT / path).exists():
            if clean_blob(path) != git("rev-parse", f"{CAMPAIGN}:{path}").decode().strip():
                oath_changes.append(path)
    (HERE / "evidence/source-preservation.json").write_text(json.dumps({
        "incoming_shipping_core_csharp_exceptions": core,
        "campaign_oath_csharp_changed_by_integration": oath_changes,
        "campaign_base32_exact": clean_blob("src/Core/src/Utilities/Base32.cs") == git("rev-parse", f"{CAMPAIGN}:src/Core/src/Utilities/Base32.cs").decode().strip(),
        "campaign_base32_tests_exact": clean_blob("src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/Base32Tests.cs") == git("rev-parse", f"{CAMPAIGN}:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/Base32Tests.cs").decode().strip(),
        "unmerged_index_entries": git("ls-files", "-u").decode().splitlines(),
        "head": git("rev-parse", "HEAD").decode().strip(),
        "merge_head": git("rev-parse", "MERGE_HEAD").decode().strip(),
    }, indent=2) + "\n")
    # This deliberately narrow proof removes only whole // comment lines. It leaves imports,
    # directives, literals and every other source line intact; broader token verification is a gate.
    comment_only_paths = [
        "src/Piv/src/Authentication/AuthenticationProtocol.cs",
        "src/Piv/src/Cryptography/PrivateKeyOperations.cs",
        "src/Cli.Commands/src/Infrastructure/YkCommandBase.cs",
        "src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs",
        "src/Cli.Commands/src/Management/ManagementConfigCommand.cs",
        "src/Cli.Commands/src/Management/ManagementResetCommand.cs",
        "src/Ctap2/examples/FidoTool/Program.cs",
        "src/OpenPgp/examples/OpenPgpTool/Cli/Commands/OpenPgpCommand.cs",
        "src/YubiHsm/examples/HsmAuthTool/Program.cs",
        "src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ReconfigurationDiscoveryInvariantTests.cs",
        "src/YubiOtp/examples/OtpTool/DeviceHelper.cs",
        "src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs",
        "src/YubiHsm/examples/HsmAuthTool/Cli/Prompts/DeviceSelector.cs",
        "src/Management/examples/ManagementTool/Cli/Prompts/DeviceSelector.cs",
        "src/Management/examples/ManagementTool/Program.cs",
        "src/Cli.Shared/src/Device/DeviceSelection.cs",
    ]

    def without_comment_lines(data):
        return [line for line in data.decode().splitlines() if not line.lstrip().startswith("//")]

    comment_proof = [{"path": path, "same_noncomment_lines_as_incoming":
                      without_comment_lines(blob(INCOMING, path)) == without_comment_lines((ROOT / path).read_bytes())}
                     for path in comment_only_paths]
    (HERE / "evidence/runtime-overlap-line-proof.json").write_text(json.dumps({
        "policy": "Remove whole // comment lines only; normalize line endings and EOF terminator through splitlines. This is narrower than a C# lexer and changes no executable lines.",
        "paths": comment_proof,
    }, indent=2) + "\n")
    projects = sorted(path.relative_to(ROOT).as_posix() for path in ROOT.rglob("*.csproj")
                      if "/.claude/" not in str(path))
    buildable = [path for path in projects if "/src/" in "/" + path and "Yubico.YubiKit." in path]
    units = [path for path in projects if "/tests/" in "/" + path and ".UnitTests" in path and "Yubico.YubiKit." in path]
    build_selectors = ("Core", "Yubico.YubiKit.Core", "AuditMerge20261111.Examples",
                       "AuditMerge20261111.Consumers", "AuditMerge20261111.MacComparison")
    unit_selectors = ("Yubico.YubiKit.Core.UnitTests", "Yubico.YubiKit.Cli.Shared.UnitTests",
                      "Yubico.YubiKit.Cli.Commands.UnitTests", "Yubico.YubiKit.Oath.UnitTests", "Core")
    (HERE / "evidence/project-selector-inventory.json").write_text(json.dumps({
        "source": "scripts/toolchain/Context.cs and Discovery.cs; filesystem emulation, no toolchain invocation",
        "build_selectors": {selector: [path for path in buildable if selector.lower() in Path(path).stem.lower()]
                            for selector in build_selectors},
        "unit_selectors": {selector: [path for path in units if selector.lower() in Path(path).stem.lower()]
                           for selector in unit_selectors},
        "all_unit_projects": units,
    }, indent=2) + "\n")
    assert not failures, failures
    assert not duplicates, duplicates
    assert not api["incoming_missing_from_resolved"]
    assert all(row["same_noncomment_lines_as_incoming"] for row in comment_proof), comment_proof
    print(json.dumps({"preserved_artifacts": sum(row["artifacts"] for row in archives),
                      "prior_integration_files": len(before["prior_integration_files"]["files"]),
                      "api_counts": api["counts"], "source_exceptions": core,
                      "oath_migrations": oath_changes}, indent=2))


if __name__ == "__main__":
    main()
