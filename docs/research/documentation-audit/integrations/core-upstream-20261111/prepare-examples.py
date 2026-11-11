"""Prepare current discovery examples in a new compile-only integration context.

No compiler or SDK operation is executed. Original bodies, hashes, typed prerequisites and the exact
wrapper transformation are retained. Historical examples remain in their frozen source contexts.
"""

import hashlib
import json
import os
from pathlib import Path
import re

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
OUT = HERE / "validation"
PROJECT = "Yubico.YubiKit.AuditMerge20261111.Examples"
# Fence numbers include non-C# fences. This is a bounded integration migration selection, not Core coverage.
SELECTED = {
    "README.md": [2],
    "docs/usage/device-discovery.md": list(range(1, 10)) + [12],
    "src/Core/README.md": [2, 3, 4, 5, 6, 9, 10],
    "docs/architecture/physical-device-model.md": [1, 2, 4, 5],
    "docs/migration/v1-to-v2.md": [6, 8],
    "src/Management/CLAUDE.md": [29],
}
IMPORTS = """using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
"""


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    source = OUT / "src"
    source.mkdir(parents=True, exist_ok=True)
    groups = []
    pattern = re.compile(r"^```([^\n]*)\n(.*?)^```\s*$", re.M | re.S)
    for path, numbers in SELECTED.items():
        text = (ROOT / path).read_text()
        matches = list(pattern.finditer(text))
        for number in numbers:
            match = matches[number - 1]
            assert match[1] == "csharp", (path, number)
            raw = match[2]
            identity = f"migration-{len(groups) + 1:02}"
            original = OUT / "raw" / f"{identity}.cs.txt"
            original.parent.mkdir(parents=True, exist_ok=True)
            original.write_text(raw)
            imports = []
            body_lines = []
            for line in raw.splitlines():
                if re.match(r"^using [\w.]+;$", line):
                    imports.append(line)
                else:
                    body_lines.append(line)
            body = "\n".join(body_lines) + "\n"
            # Keep complete nested type declarations at namespace scope, never inside a method.
            trailing = ""
            if "\nsealed class MonitorLifetime" in body:
                body, trailing = body.split("\nsealed class MonitorLifetime", 1)
                trailing = "\nsealed class MonitorLifetime" + trailing
            header = "\n".join(dict.fromkeys(IMPORTS.splitlines() + imports)) + "\n"
            namespace = f"namespace AuditMerge20261111.Examples.Group{len(groups) + 1:02};\n"
            if body.lstrip().startswith("public class MyService"):
                generated = header + namespace + body
                prerequisites = "Complete current application class; no wrapper or device execution."
            elif body.lstrip().startswith("static async Task WatchOnUiAsync"):
                generated = header + namespace + "public static class Example\n{\n" + body + "}\n"
                prerequisites = "Complete helper method; caller supplies its real Action<DeviceChange> and token."
            else:
                parameters = ["CancellationToken cancellationToken", "int serial", "int expectedSerialNumber", "string[] args", "Action<DeviceChange> Apply"]
                if not re.search(r"\b(?:var|IYubiKey) device\s*=", body):
                    parameters.append("IYubiKey device")
                generated = (header + namespace + "public static class Example\n{\n"
                             + f"public static async Task RunAsync({', '.join(parameters)})\n{{\n"
                             + body + "\nawait Task.CompletedTask;\n}\n}\n" + trailing)
                prerequisites = parameters
            destination = source / f"{identity}.cs"
            destination.write_text(generated)
            groups.append({"id": identity, "source": path, "fence": number,
                           "line": text[:match.start()].count("\n") + 1,
                           "raw_sha256": sha(original.read_bytes()), "generated_sha256": sha(destination.read_bytes()),
                           "raw": str(original.relative_to(HERE)), "generated": str(destination.relative_to(HERE)),
                           "prerequisites": prerequisites,
                           "transformation": "Relocate using directives, retain the complete body in a unique namespace; wrap statements in an uncalled async method, retaining trailing types at namespace scope. Add typed caller prerequisites and an awaited completed task for synchronous fragments."})
    # This project inherits the repository's normal props/targets/analyzers. It is not in the solution.
    references = []
    for module in ("Core", "Management", "Piv"):
        target = ROOT / "src" / module / "src" / f"Yubico.YubiKit.{module}.csproj"
        references.append(f'    <ProjectReference Include="{os.path.relpath(target, source)}" />')
    (source / f"{PROJECT}.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n'
        f'    <AssemblyName>{PROJECT}</AssemblyName>\n'
        f'    <RootNamespace>{PROJECT}</RootNamespace>\n'
        '    <IsPackable>false</IsPackable>\n    <OutputType>Library</OutputType>\n'
        '  </PropertyGroup>\n  <ItemGroup>\n'
        + "\n".join(references)
        + '\n    <PackageReference Include="Microsoft.Extensions.Hosting" />\n  </ItemGroup>\n</Project>\n')
    (OUT / "Directory.Build.props").write_text(
        '<Project>\n'
        f'  <Import Project="{os.path.relpath(ROOT / "Directory.Build.props", OUT)}" />\n'
        '  <PropertyGroup>\n'
        '    <IntegrationValidationWorkspace Condition="\'$(IntegrationValidationWorkspace)\' == \'\'">/tmp/opencode/core-upstream-20261111-validation</IntegrationValidationWorkspace>\n'
        '    <BaseIntermediateOutputPath>$(IntegrationValidationWorkspace)/$(MSBuildProjectName)/obj/</BaseIntermediateOutputPath>\n'
        '    <MSBuildProjectExtensionsPath>$(BaseIntermediateOutputPath)</MSBuildProjectExtensionsPath>\n'
        '    <BaseOutputPath>$(IntegrationValidationWorkspace)/$(MSBuildProjectName)/bin/</BaseOutputPath>\n'
        '  </PropertyGroup>\n</Project>\n')
    consumers = OUT / "consumers" / "src"
    consumers.mkdir(parents=True, exist_ok=True)
    sample_references = []
    for path in sorted((ROOT / "src").glob("*/examples/**/*.csproj")):
        sample_references.append(f'    <ProjectReference Include="{os.path.relpath(path, consumers)}" ReferenceOutputAssembly="false" />')
    for relative in ("verification/MacOSHidRouteVerification/MacOSHidRouteVerification.csproj",
                     "verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj"):
        sample_references.append(f'    <ProjectReference Include="{os.path.relpath(ROOT / relative, consumers)}" ReferenceOutputAssembly="false" />')
    (consumers / "Yubico.YubiKit.AuditMerge20261111.Consumers.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n'
        '    <IsPackable>false</IsPackable>\n    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>\n'
        '  </PropertyGroup>\n  <ItemGroup>\n' + "\n".join(sample_references)
        + '\n  </ItemGroup>\n</Project>\n')
    comparison = OUT / "mac-comparison" / "src"
    comparison.mkdir(parents=True, exist_ok=True)
    comparison_project = os.path.relpath(ROOT / "verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj", comparison)
    core_output = os.path.relpath(ROOT / "src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll", comparison)
    (comparison / "Yubico.YubiKit.AuditMerge20261111.MacComparison.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n'
        '    <IsPackable>false</IsPackable>\n    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>\n'
        '  </PropertyGroup>\n  <ItemGroup>\n'
        f'    <ProjectReference Include="{comparison_project}" ReferenceOutputAssembly="false"\n'
        f'      AdditionalProperties="BoundaryCorePath=$(MSBuildThisFileDirectory){core_output};BoundaryNativeVersion=1.18.1;BoundaryComparisonVariant=core-upstream-20261111" />\n'
        '  </ItemGroup>\n</Project>\n')
    (OUT / "examples-manifest.json").write_text(json.dumps({
        "project": PROJECT, "groups": groups, "body_count": len(groups),
        "status": "Prepared only; compiler/policy/Csc/input-output binding capture awaits coordinator gates.",
        "scope": "Complete current discovery/monitor/cache examples directly affected by integration; source archive untouched.",
        "excluded_historical": [{"source": "docs/usage/device-discovery.md", "fence": 11,
                                  "reason": "Historical IYubiKeyManager before-example; current complete counterpart is fence 12."}],
    }, indent=2) + "\n")
    print(f"Prepared {len(groups)} complete bodies in {PROJECT}; compilation not run.")


if __name__ == "__main__":
    main()
