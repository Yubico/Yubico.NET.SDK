"""One-shot, static consumer derivation. Never invokes an SDK, compiler or historical script."""
import hashlib
import json
import os
from pathlib import Path
import re
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
PACKET = HERE.parent.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').is_file())
RUNTIME = Path('/home/rupicola/Code/y/.core-docs-audit-runtime/consumer-proof')
PREFIX = 'Yubico.YubiKit.AuditIntegration20261111'
FENCES = re.compile(r'^```([^\n]*)\n(.*?)^```\s*$', re.M | re.S)
IMPORTS = '''using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
'''
# Parameters are independently enumerated free variables, not inferred from a textual name heuristic.
PARAMETERS = {
    4: ['CancellationToken cancellationToken', 'int serial'],
    8: ['string[] args'], 10: ['CancellationToken cancellationToken'],
    13: ['CancellationToken cancellationToken'], 14: ['IYubiKey device'],
    15: ['int expectedSerialNumber', 'CancellationToken cancellationToken'],
    20: ['CancellationToken cancellationToken', 'Action<DeviceChange> Apply'],
    21: ['IYubiKey device'], 22: ['IYubiKey device'],
    23: ['CancellationToken cancellationToken'], 24: ['CancellationToken cancellationToken'],
    25: ['CancellationToken cancellationToken'], 26: ['IYubiKey device'],
    27: ['int serial', 'CancellationToken ct', 'Action<DeviceChange> Apply'],
    29: ['IPcscDevice reader', 'CancellationToken cancellationToken'],
}


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path.relative_to(ROOT)), bytes=len(raw), sha256=sha(raw))


def write(path, raw):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('xb') as output:
        output.write(raw.encode() if isinstance(raw, str) else raw)


def markdown(path, number):
    text = (ROOT / path).read_text()
    match = list(FENCES.finditer(text))[number - 1]
    require(match[1] == 'csharp', 'Selected fence is not C#: ' + path)
    return match[2], dict(source=path, fence=number,
                         first_line=text[:match.start(2)].count('\n') + 1,
                         last_line=text[:match.end(2)].count('\n'))


def xml_body(path, number):
    # Strip exactly the XML-doc prefix, never alter code names, expressions or scopes.
    text = (ROOT / path).read_text()
    comments = '\n'.join(re.sub(r'^\s*/// ?', '', line) for line in text.splitlines()
                         if re.match(r'^\s*///', line)) + '\n'
    match = list(re.finditer(r'<code>\n(.*?)</code>', comments, re.S))[number - 1]
    return match[1], dict(source=path, xml_code=number,
                         extraction='Strip XML-documentation prefix; retain full code element.')


def wrapper(number, raw):
    imports, body = [], []
    for line in raw.splitlines(keepends=True):
        if re.fullmatch(r'using [\w.]+;\n?', line):
            imports.append(line.strip())
        else:
            body.append(line)
    body = ''.join(body)
    shared = [line for line in IMPORTS.splitlines() if number != 1 or line != 'using Yubico.YubiKit.Management;']
    header = '\n'.join(dict.fromkeys(shared + imports)) + '\n'
    header += f'namespace AuditIntegration20261111.Example{number:02};\n'
    if number == 30:
        header += ('using Xunit;\nusing Yubico.YubiKit.Core.Sessions;\n'
                   'using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;\n')
        return header + 'public sealed class ScpExample\n{\n' + body + '}\n'
    if number == 11:
        return header + body
    if number == 9:
        return header + 'public static class Example\n{\n' + body + '}\n'
    trailing = ''
    if number == 8:
        body, trailing = body.split('\nsealed class MonitorLifetime', 1)
        trailing = '\nsealed class MonitorLifetime' + trailing
    is_async = 'await ' in body
    signature = ('async Task RunAsync' if is_async else 'void Run')
    return (header + 'public static class Example\n{\npublic static ' + signature + '(' +
            ', '.join(PARAMETERS.get(number, [])) + ')\n{\n' + body +
            '\n}\n}\n' + trailing)


def project(folder, name, references, packages=(), extra=''):
    path = HERE / folder / 'src' / (PREFIX + '.' + name + '.csproj')
    refs = '\n'.join('    <ProjectReference Include="' + os.path.relpath(ROOT / p, path.parent) + '" />'
                     for p in references)
    packs = '\n'.join(f'    <PackageReference Include="{p}" />' for p in packages)
    write(path, '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n'
          '    <IsPackable>false</IsPackable>\n    <OutputType>Library</OutputType>\n'
          '  </PropertyGroup>\n  <ItemGroup>\n' + refs + '\n' + packs + '\n  </ItemGroup>\n' + extra + '</Project>\n')
    return path


def direct(path):
    result = []
    for node in ET.parse(path).iter('ProjectReference'):
        value = node.attrib['Include'].replace('$(RepositoryRoot)', str(ROOT)).replace('\\', '/')
        require(not any(c in value for c in ('$(', '*', '?', ';')), 'Unresolved XML reference: ' + value)
        target = (path.parent / value).resolve()
        require(target.is_relative_to(ROOT) and target.is_file(), 'Reference escapes active checkout')
        result.append(str(target.relative_to(ROOT)))
    require(len(result) == len(set(result)), 'Duplicate direct project reference')
    return sorted(result)


def main():
    require(ROOT == Path('/home/rupicola/Code/y/yubikit-core-docs-integration'), 'Use the active home checkout')
    require(not (HERE / 'inputs.json').exists(), 'Preparation is immutable; preserve and name a derivative')
    original = json.loads((HERE.parent / 'examples-manifest.json').read_text())
    require(len(original['groups']) == original['body_count'] == 25, 'Original candidate membership changed')
    groups, source_paths, preserved = [], set(), []
    for number, old in enumerate(original['groups'], 1):
        raw, location = markdown(old['source'], old['fence'])
        for field in ('raw', 'generated'):
            path = PACKET / old[field]
            require(sha(path.read_bytes()) == old[field + '_sha256'], 'Original candidate changed')
            preserved.append(identity(path))
        require(raw.encode() == (PACKET / old['raw']).read_bytes(), 'Current complete body differs from original')
        groups.append((number, raw, dict(location, original_id=old['id'])))
    raw, location = markdown('src/Management/README.md', 4)
    # This alternative explicitly inherits the getting-started directives in its own guide.
    groups.append((26, raw, location))
    for number, path, code in ((27, 'src/Core/src/Devices/YubiKeyManager.cs', 1),
                               (28, 'src/Core/src/Transports/SmartCard/SmartCardConnectionFactory.cs', 1),
                               (29, 'src/Core/src/Transports/SmartCard/SmartCardConnectionFactory.cs', 2)):
        raw, location = xml_body(path, code)
        groups.append((number, raw, location))
    raw, location = markdown('CLAUDE.md', 14)
    require('[Fact]' in raw and 'TestContext.Current.CancellationToken' in raw and
            'Assert.Equal(device.SerialNumber, deviceInfo.SerialNumber)' in raw, 'Root SCP context changed')
    groups.append((30, raw, location))
    records = []
    for number, raw, location in groups:
        name = f'body-{number:02}'
        raw_path = HERE / 'raw' / (name + '.cs.txt')
        folder = 'annotations' if number == 30 else 'examples'
        generated = HERE / folder / 'src' / (name + '.cs')
        write(raw_path, raw)
        write(generated, wrapper(number, raw))
        source_paths.add(location['source'])
        records.append(dict(id=name, **location, raw=identity(raw_path), generated=identity(generated),
                            parameters=PARAMETERS.get(number, []),
                            disposition='complete method in genuine xUnit v3 context; compile only' if number == 30
                            else 'complete class' if number == 11 else 'complete helper' if number == 9
                            else 'composable full body with explicit typed caller prerequisites',
                            transform='Relocate using directives, add namespace/class/method only. No SDK call or body rewrite. '
                                      'Body 08 retains MonitorLifetime outside the wrapper method. No injected await.'))
    write(HERE / 'probe/src/body-01.cs', wrapper(1, groups[0][1]))
    core = 'src/Core/src/Yubico.YubiKit.Core.csproj'
    management = 'src/Management/src/Yubico.YubiKit.Management.csproj'
    piv = 'src/Piv/src/Yubico.YubiKit.Piv.csproj'
    hosts = [project('probe', 'Probe', [core]),
             project('examples', 'Examples', [core, management, piv], ['Microsoft.Extensions.Hosting'])]
    # Copy the real v3 project's dependency/runner policy, replacing ONLY its source context and paths.
    context = ROOT / 'src/Management/tests/Yubico.YubiKit.Management.UnitTests/Yubico.YubiKit.Management.UnitTests.csproj'
    tree = ET.parse(context)
    for group in tree.getroot().findall('ItemGroup'):
        for node in list(group):
            if node.tag == 'Content':
                group.remove(node)  # No test execution; runner configuration is not a compiler input.
            if node.tag == 'ProjectReference':
                node.set('Include', os.path.relpath(ROOT / management, HERE / 'annotations/src'))
    annotation = HERE / 'annotations/src' / (PREFIX + '.Annotations.csproj')
    write(annotation, ET.tostring(tree.getroot(), encoding='unicode') + '\n')
    hosts.append(annotation)
    # Defeat the preserved parent validation props' /tmp default by importing canonical root directly.
    root_props = os.path.relpath(ROOT / 'Directory.Build.props', HERE)
    write(HERE / 'Directory.Build.props', '<Project>\n  <PropertyGroup>\n'
          f'    <IsTestProject Condition="\'$(MSBuildProjectName)\' == \'{PREFIX}.Annotations\'">true</IsTestProject>\n'
          f'  </PropertyGroup>\n  <Import Project="{root_props}" />\n  <PropertyGroup>\n'
          f'    <BaseIntermediateOutputPath>{RUNTIME}/hosts/$(MSBuildProjectName)/obj/</BaseIntermediateOutputPath>\n'
          '    <MSBuildProjectExtensionsPath>$(BaseIntermediateOutputPath)</MSBuildProjectExtensionsPath>\n'
          f'    <BaseOutputPath>{RUNTIME}/hosts/$(MSBuildProjectName)/bin/</BaseOutputPath>\n'
          '  </PropertyGroup>\n</Project>\n')
    samples = sorted(str(p.relative_to(ROOT)) for p in (ROOT / 'src').glob('*/examples/**/*.csproj'))
    require(len(samples) == 7, 'Review a changed real sample-project inventory')
    verification = ['verification/MacOSHidRouteVerification/MacOSHidRouteVerification.csproj',
                    'verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj']
    consumers = project('consumers', 'Consumers', [])
    # This is a new derivative, not a rewrite of the provisional resolver host.
    data = consumers.read_text().replace('  <ItemGroup>\n', '  <ItemGroup>\n' + '\n'.join(
        f'    <ProjectReference Include="{os.path.relpath(ROOT / p, consumers.parent)}" ReferenceOutputAssembly="false" />'
        for p in samples + verification) + '\n', 1)
    # New file has not been published in inputs.json yet; replace its initial generated construction.
    consumers.write_text(data)
    hosts.append(consumers)
    comparison = project('mac-comparison', 'MacComparison', [])
    target = ROOT / 'verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj'
    data = comparison.read_text().replace('  <ItemGroup>\n',
        '  <ItemGroup>\n    <ProjectReference Include="' + os.path.relpath(target, comparison.parent) +
        '" ReferenceOutputAssembly="false" AdditionalProperties="BoundaryCorePath=' +
        str(ROOT / 'src/Core/src/bin/Release/net10.0/Yubico.YubiKit.Core.dll') +
        ';BoundaryNativeVersion=1.18.1;BoundaryComparisonVariant=audit-integration-20261111" />\n', 1)
    comparison.write_text(data)
    hosts.append(comparison)
    contexts = dict(probe=[str(hosts[0].relative_to(ROOT))], examples=[str(hosts[1].relative_to(ROOT))],
                    annotations=[str(annotation.relative_to(ROOT))], consumers=samples + verification,
                    fixture=['src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj'],
                    **{'mac-comparison': [str(target.relative_to(ROOT))]})
    graph, pending = {}, [p for paths in contexts.values() for p in paths]
    while pending:
        path = pending.pop()
        if path in graph:
            continue
        graph[path] = direct(ROOT / path)
        pending.extend(graph[path])
    for path in sorted(source_paths):
        write(HERE / 'source-documents' / (path + '.snapshot'), (ROOT / path).read_bytes())
    refs = ['prepare-examples.py', 'validation/examples-manifest.json', 'validation/Directory.Build.props',
            'validation/src/Yubico.YubiKit.AuditMerge20261111.Examples.csproj',
            'validation/consumers/src/Yubico.YubiKit.AuditMerge20261111.Consumers.csproj',
            'validation/mac-comparison/src/Yubico.YubiKit.AuditMerge20261111.MacComparison.csproj',
            'review.md', 'evidence/format-preservation.json']
    preserved.extend(identity(PACKET / path) for path in refs)
    piv_tools = ROOT / 'docs/research/documentation-audit/modules/piv/pass-1/piv-001/validation/examples/v4-tools'
    provenance = dict(algorithm_sources=[identity(piv_tools / p) for p in
        ('capture-v4.py', 'compiler_policy.py', 'project_graph_v4.py')],
        adaptation='Independent bounded implementation: exact Csc sets, package-supplied analyzers, '
                   'restored direct/transitive graph, text snapshots and external binary identities. '
                   'No historical module is imported/executed; no SDK-version or graph-count constants copied.',
        annotation_source_context=identity(context), preserved_inputs=preserved)
    write(HERE / 'tooling-provenance.json', json.dumps(provenance, indent=2) + '\n')
    manifest = dict(status='static preparation; compilation and capture pending coordinator', root=str(ROOT),
        runtime=str(RUNTIME), groups=records, hosts=[identity(p) for p in hosts], contexts=contexts,
        declared_graph=graph, graph_projects=[identity(ROOT / p) for p in sorted(graph)],
        source_documents=[identity(ROOT / p) for p in sorted(source_paths)],
        actual_samples=samples, verification_hosts=verification,
        notes='All example/helper bodies are uncalled. Annotations executable is compiled only. '
              'No original archive host or source is built. No platform/runtime qualification.')
    write(HERE / 'inputs.json', json.dumps(manifest, indent=2) + '\n')
    print(f'Prepared {len(records)} current complete bodies, {len(hosts)} derivative hosts, {len(graph)} graph projects; SDK not invoked.')


if __name__ == '__main__':
    main()
