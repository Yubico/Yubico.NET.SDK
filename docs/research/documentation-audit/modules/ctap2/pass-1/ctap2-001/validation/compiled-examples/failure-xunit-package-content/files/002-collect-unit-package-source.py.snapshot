"""Focused unit-v3 input collector: genuine pinned xUnit package content, no execution.

Generic collector remains unchanged. All its input/policy checks are retained here,
with one exact additional package-owned Compile item proven from assets/targets/nupkg.
"""
import hashlib
import json
from pathlib import Path
import runpy
import shutil
import xml.etree.ElementTree as ET
import zipfile

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p/'toolchain.cs').is_file())
TOOLS = HERE.parent/'checker-source'
CONFIG = TOOLS/'context/contexts-manifest.json'
GENERIC = TOOLS/'prepare-project-context.py'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    generic = runpy.run_path(str(GENERIC))
    snapshot = generic['check'](ROOT,CONFIG,'unit-v3')
    assert snapshot['context']['kind'] == 'xunit3'
    context = TOOLS/'context/unit-v3'
    workspace = Path('/tmp/opencode/ctap2-001-unit-v3-context').resolve(strict=True)
    destination = context/'evidence'
    archive = HERE/'failure-xunit-package-content'
    assert not archive.exists() and not destination.exists(), 'One-shot evidence; never overwrite'
    assets_path = workspace/'obj/project.assets.json'
    assets = json.loads(assets_path.read_text())
    versions = ET.parse(ROOT/'Directory.Packages.props').getroot().findall('.//PackageVersion')
    version = next(i.get('Version') for i in versions if i.get('Include') == 'xunit.v3')
    library_key = f'xunit.v3.core/{version}'
    library = assets['libraries'][library_key]
    assert library['type'] == 'package' and library['path'] == library_key
    matches = [Path(folder)/library['path'] for folder in assets['packageFolders'] if (Path(folder)/library['path']).is_dir()]
    assert len(matches) == 1
    package = matches[0]
    content = package/'_content/DefaultRunnerReporters.cs'
    targets = package/'buildTransitive/xunit.v3.core.targets'
    nupkg = package/f'xunit.v3.core.{version}.nupkg'
    assert '_content/DefaultRunnerReporters.cs' in library['files']
    ns = {'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
    target = ET.parse(targets).getroot().find("m:Target[@Name='_XunitAttachSourceFiles']",ns)
    assert target is not None and target.get('BeforeTargets') == 'BeforeCompile;CoreCompile;XamlPreCompile'
    item = target.find('m:ItemGroup/m:Compile',ns)
    assert item is not None and item.get('Include') == '$(MSBuildThisFileDirectory)..\\_content\\DefaultRunnerReporters$(DefaultLanguageSourceExtension)'
    with zipfile.ZipFile(nupkg) as zipped:
        assert zipped.read('_content/DefaultRunnerReporters.cs') == content.read_bytes()
        assert zipped.read('buildTransitive/xunit.v3.core.targets') == targets.read_bytes()
    # Preserve the failed collector inputs/host/package/full intermediate outputs before
    # supplementing evidence. .csproj.snapshot avoids project-selector duplicates.
    archived = []
    sources = [GENERIC,CONFIG,Path(__file__),content,targets,nupkg]
    sources += [p for p in context.rglob('*') if p.is_file()]
    sources += [p for p in workspace.rglob('*') if p.is_file()]
    for index,p in enumerate(sources):
        saved = archive/'files'/f'{index:03d}-{p.name}.snapshot'
        saved.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(p,saved)
        assert sha(p) == sha(saved)
        archived.append(dict(source=str(p),archived=str(saved),sha256=sha(p)))
    (archive/'archive-manifest.json').write_text(json.dumps(dict(reason='Unit-v3 build PASSED/Warnings0; original generic collection refused genuine xUnit package Compile source',files=archived),indent=2)+'\n')
    files = ['compile-inputs.txt','resolved-references.txt','analyzers.txt','analyzer-configs.txt','csc-command-line.txt','policy-final.txt']
    policy = dict(line.split('=',1) for line in (workspace/'policy-final.txt').read_text().splitlines() if '=' in line)
    for name in ('TreatWarningsAsErrors','RunAnalyzersDuringBuild'):
        assert policy.get(name,'').lower() == 'true',name
    assert policy.get('AnalysisMode') == 'Recommended'
    assert policy.get('SkipCompilerExecution','').lower() != 'true'
    assert policy.get('RunAnalyzers','').lower() != 'false' and policy.get('SkipAnalyzers','').lower() != 'true'
    assert policy.get('TargetFramework') == 'net10.0' and policy.get('LangVersion') in {'14','14.0'} and policy.get('WarningLevel') == '9999'
    records = {}
    for name in files[:4]:
        records[name] = []
        for line in (workspace/name).read_text().splitlines():
            if line:
                path,_,identity = line.partition('|')
                actual = Path(path).resolve(strict=True)
                records[name].append(dict(path=str(actual),identity=identity,sha256=sha(actual)))
    expected = {g['source'] for g in snapshot['groups']}
    compiled = {r['path'] for r in records['compile-inputs.txt']}
    allowed = {str(content.resolve())}
    assert str(content.resolve()) in compiled
    assert not policy.get('GeneratedProgramFile'), 'Unit-v3 must not introduce v2 Test.Sdk program'
    assert expected <= compiled
    assert all(p in expected|allowed or Path(p).is_relative_to(workspace) for p in compiled)
    resolved = {r['path'] for r in records['resolved-references.txt']}
    assert {str((ROOT/p).resolve()) for p in snapshot['direct_references']} <= resolved
    names = {Path(p).name.lower() for p in resolved}
    assert not any(n.startswith('xunit.') and not n.startswith('xunit.v3.') for n in names)
    assert not names & {'xunit.skippablefact.dll','yubico.yubikit.tests.shared.dll'}
    analyzers = records['analyzers.txt']
    assert any(r['path'].endswith('Microsoft.CodeAnalysis.NetAnalyzers.dll') for r in analyzers)
    assert any('xunit.analyzers' in r['path'].lower() for r in analyzers)
    analyzer_version = next(i.get('Version') for i in versions if i.get('Include') == 'Microsoft.CodeAnalysis.NetAnalyzers')
    assert policy.get('EnableNETAnalyzers','').lower() in {'true','false'}
    if policy['EnableNETAnalyzers'].lower() == 'false':
        assert any(f'microsoft.codeanalysis.netanalyzers/{analyzer_version}/analyzers/' in r['path'].lower() and r['path'].endswith('Microsoft.CodeAnalysis.NetAnalyzers.dll') for r in analyzers)
    assert (workspace/'csc-command-line.txt').read_text().strip() and records['analyzer-configs.txt']
    result = dict(note='Actual input/policy evidence only; separate retained clean host build PASSED/Warnings0; no execution',
                  prepared_manifest_sha256=sha(context/'context-manifest.json'),policy=policy,actual_compiler_inputs=records,
                  project_assets_sha256=sha(assets_path),captured_files=[dict(path=str(workspace/name),sha256=sha(workspace/name)) for name in files],
                  collector_source_sha256=sha(Path(__file__)),generic_collector_sha256=sha(GENERIC),
                  package_content_proof=dict(package=library_key,content=dict(path=str(content),sha256=sha(content)),targets=dict(path=str(targets),sha256=sha(targets)),
                                             nupkg=dict(path=str(nupkg),sha256=sha(nupkg)),exact_package_bytes_match=True,allowed_extra_compile_items=sorted(allowed)))
    destination.mkdir()
    (destination/'resolved-inputs.json').write_text(json.dumps(result,indent=2)+'\n')
    for name in files:
        shutil.copy2(workspace/name,destination/name)
    print(f'Collected unit-v3: four exact wrappers plus pinned package/generated inputs; all policy/reference/analyzer checks preserved: {destination}')


if __name__ == '__main__':
    main()
