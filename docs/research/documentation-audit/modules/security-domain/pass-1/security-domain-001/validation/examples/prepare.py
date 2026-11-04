"""Extract whole SD documentation tasks into two real SDK contexts; never execute hosts."""
from pathlib import Path
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
revision = sys.argv[1]
assert re.fullmatch(r'v[1-9][0-9]*', revision)
DEST = HERE / revision
if DEST.exists():
    raise SystemExit(f'Refusing to overwrite prior extraction: {DEST}')
DEST.mkdir()

def digest(data):
    return hashlib.sha256(data).hexdigest()

documents = ['src/SecurityDomain/README.md', 'src/SecurityDomain/CLAUDE.md',
             'src/SecurityDomain/PACKAGE_README.md', 'src/SecurityDomain/tests/README.md',
             'src/SecurityDomain/tests/CLAUDE.md']
blocks = []
for document in documents:
    text = (ROOT / document).read_text()
    for index, match in enumerate(re.finditer(r'^```csharp\s*\n(.*?)^```\s*$', text, re.M | re.S), 1):
        blocks.append(dict(path=document, index=index, text=match.group(1),
                           line=text[:match.start()].count('\n') + 1,
                           document_sha256=digest((ROOT / document).read_bytes()),
                           block_sha256=digest(match.group(1).encode())))

def block(path, index):
    matches = [b for b in blocks if b['path'] == path and b['index'] == index]
    assert len(matches) == 1, (path, index)
    return matches[0]

readme, guide, _, testreadme, testguide = documents
assert len(blocks) == 10, 'Reconcile new/deleted C# blocks before preparing hosts'
entry = block(readme, 1)['text']
lines = entry.splitlines(keepends=True)
imports = ''.join(line for line in lines if re.fullmatch(r'using [A-Za-z0-9_.]+;\n', line))
entry_body = ''.join(line for line in lines if not re.fullmatch(r'using [A-Za-z0-9_.]+;\n', line))
assert imports and 'await using var session' in entry_body
auth = block(readme, 2)['text']
groups = []

def group(name, context, original, body, parameters='', member=False, prerequisites=None):
    raw = ''.join(b['text'] for b in original)
    raw_path = DEST / 'raw' / f'{name}.cs.txt'
    raw_path.parent.mkdir(exist_ok=True)
    raw_path.write_text(raw)
    wrapped = body if member else f'public static async Task {name}Async({parameters})\n{{\n{body}\n}}\n'
    groups.append(dict(name=name, context=context, blocks=[{'path':b['path'], 'index':b['index']} for b in original],
                       raw=str(raw_path.relative_to(DEST)), raw_sha256=digest(raw.encode()),
                       wrapped=wrapped, prerequisites=prerequisites or []))

group('ReadOnly', 'consumer', [block(readme,1)], entry_body)
group('Authenticate', 'consumer', [block(readme,2)], auth, 'IYubiKey device', prerequisites=['Caller-selected IYubiKey device; no existing live session'])
group('Import', 'consumer', [block(readme,2),block(readme,3)], auth + '\n' + block(readme,3)['text'], 'IYubiKey device', prerequisites=['Factory-SCP03 administrative task; escrow remains caller responsibility'])
group('Generate', 'consumer', [block(readme,2),block(readme,4)], auth + '\n' + block(readme,4)['text'], 'IYubiKey device', prerequisites=['Factory-SCP03 administrative task; capable firmware if executed'])
group('Reset', 'consumer', [block(readme,5)], block(readme,5)['text'], 'IYubiKey device', prerequisites=['Separate unauthenticated task; destructive if executed'])
group('Inspect', 'consumer', [block(guide,2)], block(guide,2)['text'], 'ISecurityDomainSession session, ILogger logger, CancellationToken cancellationToken', prerequisites=['Caller-owned session/logger/token; only metadata is logged'])
inline = 'new Scp11KeyParameters(keyReference, devicePublicKey)'
assert f'`{inline}`' in (ROOT / readme).read_text()
inline_block = dict(path=readme,index='inline-constructor',text=inline,
                    document_sha256=digest((ROOT / readme).read_bytes()),block_sha256=digest(inline.encode()))
group('OpenScp11', 'consumer', [inline_block],
      f'using var parameters = {inline};\nawait using var session = await device.CreateSecurityDomainSessionAsync(\n    new SessionCreationOptions {{ ScpKeyParameters = parameters }});',
      'IYubiKey device, KeyReference keyReference, ECPublicKey devicePublicKey',
      prerequisites=['Trusted provisioned key/reference supplied by caller; previous administrative session disposed'])
for name,path,index in [('Basic',testreadme,1),('Factory',testguide,1),('Successive',testguide,2)]:
    group(name,'hardware',[block(path,index)],block(path,index)['text'],member=True,
          prerequisites=['Existing xUnit-v2 project context; public real test helpers/attributes; compile only'])

def xml_write(path, element):
    ET.indent(element, space='  ')
    path.write_text(ET.tostring(element,encoding='unicode')+'\n')

for context in ['consumer','hardware']:
    folder=DEST/context
    (folder/'src').mkdir(parents=True)
    is_test = context=='hardware'
    workspace=f'/tmp/opencode/security-domain-001-examples-{revision}-{context}'
    props=f'''<Project>
  <PropertyGroup>
    <RepositoryRoot>$([MSBuild]::GetDirectoryNameOfFileAbove('$(MSBuildThisFileDirectory)', 'toolchain.cs'))</RepositoryRoot>
    <IsTestProject>{str(is_test).lower()}</IsTestProject>
  </PropertyGroup>
  <Import Project="$(RepositoryRoot)/Directory.Build.props" />
  <PropertyGroup>
    <DocumentationWorkspace>{workspace}</DocumentationWorkspace>
    <BaseIntermediateOutputPath>$(DocumentationWorkspace)/obj/</BaseIntermediateOutputPath>
    <MSBuildProjectExtensionsPath>$(DocumentationWorkspace)/obj/</MSBuildProjectExtensionsPath>
    <BaseOutputPath>$(DocumentationWorkspace)/bin/</BaseOutputPath>
  </PropertyGroup>
</Project>
'''
    (folder/'Directory.Build.props').write_text(props)
    template=ROOT/'docs/research/documentation-audit/modules/ctap2/pass-1/ctap2-001/validation/checker-source/context/Directory.Build.targets.template'
    targets=ET.fromstring(template.read_text())
    for target in list(targets.findall('Target')):
        if target.get('Name')=='ValidateCtap2DocumentationInputs':
            targets.remove(target)
        else:
            target.set('Name',target.get('Name').replace('Ctap2','SecurityDomain'))
    xml_write(folder/'Directory.Build.targets',targets)
    project=ET.Element('Project',Sdk='Microsoft.NET.Sdk')
    properties=ET.SubElement(project,'PropertyGroup')
    for name,value in {'TargetFramework':'net10.0','OutputType':'Library','IsPackable':'false',
                       'ProvideCommandLineArgs':'true','EnableDefaultCompileItems':'false'}.items():
        ET.SubElement(properties,name).text=value
    items=ET.SubElement(project,'ItemGroup')
    original_project='src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Yubico.YubiKit.SecurityDomain.IntegrationTests.csproj' if is_test else 'src/SecurityDomain/src/Yubico.YubiKit.SecurityDomain.csproj'
    ET.SubElement(items,'ProjectReference',Include='$(RepositoryRoot)/'+original_project)
    if is_test:
        source=ET.parse(ROOT/original_project).getroot()
        for item in source.findall('.//PackageReference'):
            items.append(ET.fromstring(ET.tostring(item)))
    ET.SubElement(items,'Compile',Include='Examples.cs')
    project_path=folder/'src'/f'Yubico.YubiKit.AuditSd001.Examples.{revision}.{context}.csproj'
    xml_write(project_path,project)
    if is_test:
        usings='using Xunit;\nusing Yubico.YubiKit.Tests.Shared;\nusing Yubico.YubiKit.Tests.Shared.Infrastructure;\nusing Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;\nusing Yubico.YubiKit.Core.Protocols.SmartCard.Scp;\nusing Yubico.YubiKit.Core.Devices;\n'
        declaration='public sealed class HardwareExamples'
    else:
        usings=imports+'using Microsoft.Extensions.Logging;\n'
        declaration='public static class ConsumerExamples'
    code=usings+'\nnamespace AuditSd001.Examples;\n\n'+declaration+'\n{\n'+''.join(g['wrapped'] for g in groups if g['context']==context)+'}\n'
    (folder/'src/Examples.cs').write_text(code)
    print(f'Prepared {context}: {sum(g["context"]==context for g in groups)} groups; project {project_path.name}; no build or execution')

manifest=dict(revision=revision,baseline='b73efe3813b429b4b8eb2a7e819a6ad7be699d47',blocks=blocks,
              inline=inline_block,groups=groups,
              exclusions=[dict(path=guide,index=1,reason='Instruction/tag constant navigation sketch, not an executable task')],
              transforms='Only namespace imports separated from entry-point statements; all resource using/await/finally/attribute/assertion bodies retained verbatim. Typed wrappers do not execute.')
manifest['inputs']=[dict(path=str(p.relative_to(ROOT)),sha256=digest(p.read_bytes())) for p in sorted(DEST.rglob('*')) if p.is_file()]
(DEST/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
