"""Extract classified Management documentation verbatim; add only typed compilation scaffolding."""

import argparse
import hashlib
import html
import json
from pathlib import Path
import re
import textwrap


PRIMARY = next(parent for parent in Path(__file__).resolve().parents if (parent / "toolchain.cs").is_file())
COMMON = [
    "System", "System.Collections.Generic", "System.Linq", "System.Threading", "System.Threading.Tasks",
    "System.Security.Cryptography", "Yubico.YubiKit.Core", "Yubico.YubiKit.Core.Abstractions",
    "Yubico.YubiKit.Core.Devices", "Yubico.YubiKit.Core.Sessions",
    "Yubico.YubiKit.Core.Transports.SmartCard", "Yubico.YubiKit.Core.Transports.Hid",
    "Yubico.YubiKit.Core.Protocols.SmartCard.Apdu", "Yubico.YubiKit.Core.Protocols.SmartCard.Scp",
    "Yubico.YubiKit.Management",
]
TEST_IMPORTS = ["Xunit", "Yubico.YubiKit.Tests.Shared", "Yubico.YubiKit.Tests.Shared.Infrastructure"]
EXAMPLE_IMPORTS = [
    "Yubico.YubiKit.Management.Examples.ManagementTool.Features",
    "Yubico.YubiKit.Management.Examples.ManagementTool.Features.Results",
    "Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts",
]
V2_DIR = "src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/bin/Release/net10.0/"
V3_DIR = "src/Management/tests/Yubico.YubiKit.Management.UnitTests/bin/Release/net10.0/"
EXAMPLE_DIR = "src/Management/examples/ManagementTool/bin/Release/net10.0/"
SECURITY_TEST_DLL = "src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/bin/Release/net10.0/Yubico.YubiKit.SecurityDomain.IntegrationTests.dll"
V2_REFS = ["src/Tests.Shared/bin/Release/net10.0/Yubico.YubiKit.Tests.Shared.dll"] + [
    V2_DIR + name for name in ["xunit.core.dll", "xunit.assert.dll", "xunit.abstractions.dll", "Xunit.SkippableFact.dll"]
]
V3_REFS = [V3_DIR + name for name in ["xunit.v3.core.dll", "xunit.v3.assert.dll", "xunit.v3.common.dll"]]
LOG_REFS = [EXAMPLE_DIR + name for name in [
    "Microsoft.Extensions.Logging.Abstractions.dll", "Microsoft.Extensions.Logging.dll",
    "Microsoft.Extensions.Logging.Console.dll", "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
    "Microsoft.Extensions.Options.dll",
]]
A_B_XML = {"src/Management/src/IManagementSession.cs", "src/Management/src/DeviceConfig.cs"}
EXPECTED_COUNTS = {
    "CLAUDE.md": 16, "docs/TESTING.md": 22,
    "docs/architecture/connection-ownership-and-contention.md": 2,
    "docs/architecture/physical-device-model.md": 5, "docs/migration/v1-to-v2.md": 24,
    "src/Management/CLAUDE.md": 29, "src/Management/PACKAGE_README.md": 1,
    "src/Management/README.md": 8, "src/Management/tests/CLAUDE.md": 3,
    "src/Management/examples/ManagementTool/README.md": 2,
    "src/Management/examples/ManagementTool/Cli/Prompts/LockCodePrompt.cs": 1,
    "src/Management/examples/ManagementTool/Features/DeviceConfiguration.cs": 2,
    "src/Management/examples/ManagementTool/Features/DeviceInfoQuery.cs": 1,
    "src/Management/examples/ManagementTool/Features/DeviceReset.cs": 1,
    "src/Management/src/IManagementSession.cs": 1, "src/Management/src/DeviceConfig.cs": 1,
    "src/Piv/examples/PivTool/SDK_PAIN_POINTS.md": 6, "src/Tests.Shared/CLAUDE.md": 6,
    "src/Tests.Shared/README.md": 10, "src/Tests.Shared/YubiKeyTestStateExtensions.cs": 3,
    "src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs": 2,
}
SPECS = []


def spec(name, path, ordinal, anchor, mode="method", parameters="", profile="M", modules=(), synchronous=False):
    SPECS.append(dict(name=name, path=path, ordinal=ordinal, anchor=anchor, mode=mode,
                      parameters=parameters, profile=profile, modules=list(modules), synchronous=synchronous))


README = "src/Management/README.md"
GUIDE = "src/Management/CLAUDE.md"
for ordinal, anchor, parameters in [
    (2, "using System.Security.Cryptography", ""),
    (3, "bool pivOverUsb", "IYubiKey device"),
    (4, "var requested", "IYubiKey device"),
    (5, "byte[] lockCode", "IYubiKey device"),
    (6, "CurrentLockCode = currentLockCode", "IYubiKey device, byte[] currentLockCode"),
    (7, "await session.ResetDeviceAsync", "IYubiKey device"),
]:
    spec(f"readme-{ordinal:02}", README, ordinal, anchor, mode="program" if ordinal == 2 else "method", parameters=parameters)

GUIDE_ANCHORS = {
    1: "ReadMetadata_ForMatchingKey", 2: "FirstMatchingKey_ReturnsSameSerial", 3: "MultiFormFactor_Test",
    4: "int? serial", 5: "public static class IYubiKeyExtensions", 6: "var deviceInfo",
    7: "await using var connection", 8: "var config", 9: "var config", 10: "customProtocolConfig",
    11: "var remaining", 12: "await state.WithManagementAsync", 13: "public async Task MyTest",
    14: "GetDeviceInfo_ReturnsValidData", 15: "DeviceInfo_Firmware57Plus_HasVersionDisplay",
    16: "BioFormFactor_MetadataMatchesCache", 17: "PivCapability_EnabledDevices_Accessible",
    18: "AdvancedFiltering_ModernUsbPiv_Works", 19: "AutoEject_PersistsAndRestorationIsAttempted",
    20: "await state.WithManagementAsync", 21: "var info3", 22: "Assert.Equal(info1.SerialNumber",
    23: "ChallengeResponseTimeout = 15", 24: "bool hasPivAndOath", 25: "VersionQualifier qualifier",
    28: "builder.AddConsole", 29: "devices.SingleOrDefault",
}
GUIDE_MEMBERS = {1, 2, 3, 13, 14, 15, 16, 17, 18, 19}
GUIDE_TEST_FRAGMENTS = {4, 12, 20, 21, 22}
for ordinal, anchor in GUIDE_ANCHORS.items():
    profile = "T" if ordinal in GUIDE_MEMBERS | GUIDE_TEST_FRAGMENTS else "L" if ordinal == 28 else "M"
    parameters = ""
    if ordinal in {4, 12, 20, 21}:
        parameters = "YubiKeyTestState state"
    elif ordinal in {6, 7, 8, 9, 10, 11}:
        parameters = "IYubiKey yubiKey, CancellationToken cancellationToken"
        if ordinal == 10:
            parameters += ", ProtocolConfiguration customProtocolConfig"
    elif ordinal in {22, 25}:
        parameters = "ManagementSession mgmt"
    elif ordinal == 23:
        parameters = "IManagementSession session, CancellationToken cancellationToken"
    elif ordinal == 28:
        parameters = "ISmartCardConnection connection"
    elif ordinal == 29:
        parameters = "CancellationToken cancellationToken"
    mode = "members" if ordinal in GUIDE_MEMBERS else "declaration" if ordinal == 5 else "method"
    spec(f"guide-{ordinal:02}", GUIDE, ordinal, anchor, mode=mode, parameters=parameters,
         profile=profile, synchronous=ordinal in {4, 24})

for ordinal in (1, 2):
    spec(f"module-tests-{ordinal}", "src/Management/tests/CLAUDE.md", ordinal, "MyTest", mode="members", profile="T")

EXAMPLE = "src/Management/examples/ManagementTool/"
for name, path, ordinal, anchor, parameters in [
    ("tool-lock-prompt", "Cli/Prompts/LockCodePrompt.cs", 1, "LockCodePrompt.PromptForLockCode", "IManagementSession session, CancellationToken ct"),
    ("tool-capabilities", "Features/DeviceConfiguration.cs", 1, "DeviceConfiguration.SetCapabilitiesAsync", "IManagementSession session, CancellationToken ct"),
    ("tool-lock-change", "Features/DeviceConfiguration.cs", 2, "GetCurrentCode()", "IManagementSession session, CancellationToken ct, Func<byte[]?> GetCurrentCode, Func<byte[]> GetNewCode"),
    ("tool-query", "Features/DeviceInfoQuery.cs", 1, "DeviceInfoQuery.GetDeviceInfoAsync", "IYubiKey device, CancellationToken ct"),
    ("tool-reset", "Features/DeviceReset.cs", 1, "DeviceReset.ResetDeviceAsync", "IManagementSession session, CancellationToken ct"),
]:
    spec(name, EXAMPLE + path, ordinal, anchor, parameters=parameters, profile="E")

spec("session-reset-xml", "src/Management/src/IManagementSession.cs", 1, "await session.ResetDeviceAsync", parameters="IYubiKey device, CancellationToken cancellationToken")
spec("config-xml", "src/Management/src/DeviceConfig.cs", 1, "info.UsbEnabled & ~DeviceCapabilities.Otp", parameters="IManagementSession session")

for ordinal, anchor in [(1, "GetDeviceInfo_ReturnsValidData"), (2, "CheckDeviceProperties"), (3, "ReadMetadata_WithBorrowedConnection")]:
    spec(f"shared-helper-xml-{ordinal}", "src/Tests.Shared/YubiKeyTestStateExtensions.cs", ordinal, anchor, mode="members", profile="T")
for ordinal, anchor in [(1, "ReadIndependentlyBoundRows"), (2, "ReadSelectedRange")]:
    spec(f"shared-attribute-xml-{ordinal}", "src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs", ordinal, anchor, mode="members", profile="T")

for ordinal, anchor, mode in [
    (1, "public class MyIntegrationTests", "declaration"), (4, "GetDeviceInfo_AllDevices", "members"),
    (5, "await state.WithSecurityDomainSessionAsync", "members"),
    (6, "RunsOnlyOnProductionKeyRange", "members"), (7, "ReadMetadata_OverOtpHid", "members"),
    (8, "public static async Task WithOathAsync", "extension-member"),
]:
    modules = ("Oath",) if ordinal == 8 else ("SecurityDomain",) if ordinal == 5 else ()
    spec(f"shared-readme-{ordinal}", "src/Tests.Shared/README.md", ordinal, anchor, mode=mode,
         profile="SD" if ordinal == 5 else "T", modules=modules)
spec("shared-guide-oath", "src/Tests.Shared/CLAUDE.md", 6, "public static async Task WithOathAsync", mode="extension-member", profile="T", modules=("Oath",))
for ordinal, anchor in [(11, "EachManagementConnection"), (12, "GetDeviceInfo_DefaultManagementSelection"), (13, "GetDeviceInfo_UsesRequestedManagementConnection"), (15, "ManagementOverFido_Works")]:
    spec(f"testing-{ordinal}", "docs/TESTING.md", ordinal, anchor, mode="members", profile="T")
spec("root-scp-v3", "CLAUDE.md", 14, "CreateSession_WithSCP03_AuthenticatesAndCommunicates", mode="members", profile="V3")
spec("physical-metadata", "docs/architecture/physical-device-model.md", 3, "DeviceInfo info", parameters="IYubiKey device")
spec("physical-transports", "docs/architecture/physical-device-model.md", 4, "await using (var otpMgmt", parameters="IYubiKey device")
spec("migration-device-info", "docs/migration/v1-to-v2.md", 8, "var info = await session.GetDeviceInfoAsync", parameters="CancellationToken cancellationToken")
spec("piv-borrowed", "src/Piv/examples/PivTool/SDK_PAIN_POINTS.md", 6, "await using (var management", parameters="ISmartCardConnection connection", modules=("Piv",))

EXCLUSION_REASONS = {
    ("CLAUDE.md", 15): "Documentation-only ScpInitializer teaching skeleton now describes the actual fakeable APDU boundary and existing test; no consumer task or actual fake fixture implementation to compile here.",
    (GUIDE, 26): "Internal ManagementSession factory excerpt requires its private constructor/initialization and inherited protected context; inspect actual compiled SDK, no replacement implementation.",
    (GUIDE, 27): "Internal field/interface excerpt deliberately combines separate source scopes; no consumer task, no fabricated backend.",
    ("src/Tests.Shared/README.md", 9): "Unchanged recorder sketch has deliberate SomeSession/expectedInstruction placeholders; no fake session implementation permitted.",
    ("docs/TESTING.md", 14): "Explicit historical DeviceId-parsing pattern, superseded by current pinned Management example.",
    ("docs/migration/v1-to-v2.md", 7): "Historical Device Info V1 API/package, intentionally contrasted with current V2 block 8.",
    ("docs/architecture/connection-ownership-and-contention.md", 1): "Deliberate historical/pre-enforcement contention example, not a valid current parallel ownership recipe.",
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def blocks(path):
    document = path.read_text(encoding="utf-8")
    if path.suffix == ".cs":
        matches = list(re.finditer(r"<code>(.*?)</code>", document, re.S))
        return [dict(language="csharp", text=html.unescape(re.sub(r"^[ \t]*/// ?", "", match[1], flags=re.M)),
                     line=document[:match.start()].count("\n") + 1, ordinal=index)
                for index, match in enumerate(matches, 1)]
    return [dict(language=match[1].strip(), text=match[2], line=document[:match.start()].count("\n") + 1, ordinal=index)
            for index, match in enumerate(re.finditer(r"^```([^\n]*)\n(.*?)^```[ \t]*$", document, re.M | re.S), 1)]


def scaffold(item, raw):
    imports = list(COMMON)
    references = []
    if item["profile"] in {"T", "SD"}:
        imports += TEST_IMPORTS
        references += V2_REFS
        if item["profile"] == "SD":
            imports += ["Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions"]
            references += [SECURITY_TEST_DLL]
    elif item["profile"] == "V3":
        imports += ["Xunit"]
        references += V3_REFS
    elif item["profile"] == "E":
        imports += EXAMPLE_IMPORTS
        references += [EXAMPLE_DIR + "ManagementTool.dll"]
    elif item["profile"] == "L":
        imports += ["Microsoft.Extensions.Logging"]
        references += LOG_REFS
    imports += [f"Yubico.YubiKit.{module}" for module in item["modules"]]
    # Move only namespace imports out of the method wrapper, preserving their exact source text.
    directive = re.compile(r"^[ \t]*using [A-Za-z_][A-Za-z0-9_.]*;[ \t]*$", re.M)
    original_imports = [match[0].strip() for match in directive.finditer(raw)]
    existing_names = {line.removeprefix("using ").removesuffix(";") for line in original_imports}
    header = "\n".join(f"using {namespace};" for namespace in dict.fromkeys(imports) if namespace not in existing_names)
    header += "\n" + "\n".join(original_imports) + "\n\n"
    body = textwrap.dedent(directive.sub("", canonical_whitespace(raw))).strip("\n") + "\n"
    mode = item["mode"]
    if mode == "program":
        wrapped = body
    else:
        wrapped = "namespace ManagementSnippet_" + item["name"].replace("-", "_") + ";\n\n"
        if mode == "declaration":
            wrapped += body
        elif mode in {"members", "extension-member"}:
            modifier = "static " if mode == "extension-member" else ""
            wrapped += f"public {modifier}class DocumentationExample\n{{\n" + textwrap.indent(body, "    ") + "}\n"
        else:
            returns = "void" if item["synchronous"] else "async Task"
            wrapped += "public static class DocumentationExample\n{\n"
            wrapped += f"    public static {returns} Example({item['parameters']})\n    {{\n"
            wrapped += textwrap.indent(body, "        ") + "    }\n}\n"
    prerequisites = [f"Profile {item['profile']}: actual SDK/test/example assemblies; see snippet-preparation.md",
                     f"Scaffold mode: {mode}; typed parameters: {item['parameters'] or '(none)'}",
                     "Only namespace imports moved and trailing syntactic whitespace normalized; all original statements, callbacks, awaits, branches and disposals retained."]
    return header + wrapped, references, prerequisites


def canonical_whitespace(raw):
    canonical = "\n".join(line.rstrip(" \t") for line in raw.split("\n"))
    if canonical != raw and ('\"\"\"' in raw or '@"' in raw):
        raise ValueError("Multiline/verbatim literal with trailing whitespace requires explicit preservation review")
    return canonical


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=PRIMARY, help="Primary SDK/A+B XML and Release-reference root")
    parser.add_argument("--guide-root", type=Path, help="C-edited Markdown/XML root; defaults to --root after integration")
    args = parser.parse_args()
    root = args.root.resolve(strict=True)
    guide_root = (args.guide_root or root).resolve(strict=True)
    output = Path(__file__).resolve().parent
    paths = {item["path"] for item in SPECS} | {
        "src/Management/PACKAGE_README.md", "src/Management/examples/ManagementTool/README.md",
        "docs/architecture/connection-ownership-and-contention.md",
    }
    parsed = {}
    inventory = []
    for path in sorted(paths):
        source_root = root if path in A_B_XML else guide_root
        actual = source_root / path
        parsed[path] = blocks(actual)
        if len(parsed[path]) != EXPECTED_COUNTS[path]:
            raise ValueError(f"Block inventory changed: {path}: expected {EXPECTED_COUNTS[path]}, found {len(parsed[path])}; reclassify explicitly")
        inventory.extend(dict(path=path, source_root=str(source_root), document_sha256=sha(actual.read_bytes()), **block)
                         for block in parsed[path])
    groups = []
    selected = set()
    for item in SPECS:
        block = parsed[item["path"]][item["ordinal"] - 1]
        if block["language"] != "csharp" or item["anchor"] not in block["text"]:
            raise ValueError(f"Classification locator changed: {item['path']} block {item['ordinal']}: {item['anchor']}")
        selected.add((item["path"], item["ordinal"]))
        raw = block["text"]
        generated, references, prerequisites = scaffold(item, raw)
        raw_name = item["name"] + ".raw.txt"
        source_name = item["name"] + ".cs"
        canonical_raw = canonical_whitespace(raw)
        (output / raw_name).write_text(canonical_raw, encoding="utf-8")
        (output / source_name).write_text(generated, encoding="utf-8")
        provenance = next(record for record in inventory if record["path"] == item["path"] and record["ordinal"] == item["ordinal"])
        groups.append(dict(name=item["name"], source=source_name, source_sha256=sha(generated.encode()),
                           provenance=[f"{item['path']} {'XML code' if item['path'].endswith('.cs') else 'fence'} #{item['ordinal']} line {block['line']}; anchor={item['anchor']}"],
                           prerequisites=prerequisites, modules=item["modules"], references=references,
                           extraction={key: value for key, value in provenance.items() if key != "text"},
                           raw_source=raw_name, raw_sha256=sha(canonical_raw.encode()),
                           extracted_text=raw, extracted_sha256=sha(raw.encode()),
                           transformations=["namespace-import relocation", "trailing syntactic whitespace normalization"]))
    exclusions = []
    for record in inventory:
        key = (record["path"], record["ordinal"])
        if key in selected:
            continue
        if record["language"] != "csharp":
            reason = "Non-C# command/configuration/directory/flow fence; no command or test executed."
        elif record["path"] == "src/Tests.Shared/CLAUDE.md":
            reason = "Unchanged internal harness/allow-list/filter/provider skeleton, not complete source; actual helpers referenced as compiled types."
        else:
            reason = "Outside narrow Management task: unrelated teaching/PIV/other-app example or historical migration alternative; not certified by this audit."
        exclusions.append({key: value for key, value in record.items() if key != "text"} |
                          dict(raw_sha256=sha(record["text"].encode()), reason=EXCLUSION_REASONS.get(key, reason)))
    reference_paths = {path for group in groups for path in group["references"]}
    for module in {"Core", "Management"} | {module for group in groups for module in group["modules"]}:
        reference_paths.add(f"src/{module}/src/bin/Release/net10.0/Yubico.YubiKit.{module}.dll")
    reference_inventory = [dict(path=path, sha256_at_extraction=sha((root / path).read_bytes()) if (root / path).is_file() else None)
                           for path in sorted(reference_paths)]
    manifest = dict(schema_version=2, primary_root=str(root), guide_root=str(guide_root),
                    classification="management-001/edit-{a,b,c}.md", groups=groups, exclusions=exclusions,
                    reference_inventory=reference_inventory)
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Prepared {len(groups)} compile-only groups; {len(exclusions)} explicitly excluded blocks. No compilation or execution.")


if __name__ == "__main__":
    main()
