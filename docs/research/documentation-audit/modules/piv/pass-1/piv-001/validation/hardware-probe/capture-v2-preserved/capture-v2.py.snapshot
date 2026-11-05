"""Reconcile exact actual-Csc sets and retain canonical analyzer/import/source evidence."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import re
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
WORK = Path('/tmp/opencode/piv-001-hardware')
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('destination', type=Path)
options = parser.parse_args()
DEST = options.destination.resolve()
DEST.mkdir(parents=True, exist_ok=False)
retained, records = [], []


def require(condition, message):
    if not condition:
        raise ValueError(message)


def identity(path):
    data = path.read_bytes()
    return dict(path=str(path), bytes=len(data), sha256=hashlib.sha256(data).hexdigest())


def retain(path, relative):
    data = path.read_bytes()
    data.decode('utf-8-sig')
    require(b'\0' not in data, 'Binary input stays outside the text evidence archive: ' + str(path))
    target = DEST / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open('xb') as output:
        output.write(data)
    row = dict(identity(path), retained=str(relative))
    retained.append(row)
    return row


def lines(path):
    return [line.strip() for line in path.read_text(encoding='utf-8-sig').splitlines() if line.strip()]


try:
    names = ('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt',
             'analyzer-configs.txt', 'csc-command-line.txt', 'policy-final.txt')
    for name in names:
        retain(WORK / name, Path(name))
    arguments = lines(DEST / 'csc-command-line.txt')
    policy = dict(line.split('=', 1) for line in lines(DEST / 'policy-final.txt'))
    projects = list((HERE / 'tests').rglob('*.csproj'))
    require(len(projects) == 1, 'Expected exactly one unique audit project')
    project = projects[0].resolve()

    def resolve(value):
        return str((project.parent / value.strip('"')).resolve())

    def csc_set(prefix):
        return {resolve(arg[len(prefix):]) for arg in arguments if arg.startswith(prefix)}

    sets = {
        'source': {str(Path(line).resolve()) for line in lines(DEST / 'compile-inputs.txt')},
        'reference': {str(Path(line.split('|', 1)[0]).resolve()) for line in lines(DEST / 'resolved-references.txt')},
        'analyzer': {str(Path(line).resolve()) for line in lines(DEST / 'analyzers.txt')},
        'analyzer-config': {str(Path(line).resolve()) for line in lines(DEST / 'analyzer-configs.txt')},
    }
    source_arguments = {resolve(arg) for arg in arguments if arg.endswith('.cs') and not re.match(r'^/[A-Za-z]+:', arg)}
    require(bool(sets['source']) and source_arguments == sets['source'], 'Exact Csc source/Compile set mismatch')
    for kind, prefix in [('reference', '/reference:'), ('analyzer', '/analyzer:'), ('analyzer-config', '/analyzerconfig:')]:
        require(bool(sets[kind]) and csc_set(prefix) == sets[kind], 'Exact Csc membership mismatch: ' + kind)
    require({str(p.resolve()) for p in project.parent.glob('*.cs')} <= source_arguments, 'Complete local probe source omitted')
    require(csc_set('/embed:') <= source_arguments, 'Embedded source outside actual compilation')
    require('/warnaserror+' in arguments and '/warn:9999' in arguments and '/langversion:14' in arguments
            and '/nullable:enable' in arguments, 'Actual Csc language/warning policy mismatch')
    require(not any('/skipanalyzers' in arg.lower() for arg in arguments), 'Actual Csc skips analyzers')
    require(not any(arg.lower().startswith(('/warnaserror-', '/warnaserror-:', '/langversion:13')) for arg in arguments),
            'Actual Csc warning/language policy weakened')
    require(policy['TargetFramework'] == 'net10.0' and policy['LangVersion'] in ('14', '14.0')
            and policy['TreatWarningsAsErrors'] == 'true' and policy['RunAnalyzersDuringBuild'] == 'true'
            and policy.get('SkipCompilerExecution', '').lower() != 'true', 'Effective framework/compiler policy mismatch')
    require(any('xunit.analyzers' in path.lower() for path in sets['analyzer']), 'Genuine xUnit analyzers omitted')

    # Fresh clean-build hooks must supply every effective field. The old failed
    # capture stays historical; current product sources cannot rebind that build.
    for key in ('MSBuildProjectFullPath', 'ProjectAssetsFile', 'WarningLevel', 'RunAnalyzers', 'SkipAnalyzers'):
        require(key in policy, 'Fresh clean-build capture must supply policy field: ' + key)
    require(Path(policy['MSBuildProjectFullPath']).resolve() == project, 'Policy belongs to another project')
    require(Path(policy['ProjectAssetsFile']).resolve() == WORK / 'obj/project.assets.json', 'Assets escaped unique workspace')
    require(policy['NETCoreSdkVersion'] == '10.0.403' and policy['AnalysisLevel'] == 'preview', 'Pinned SDK/analysis level mismatch')
    require(policy['AnalysisMode'] == 'AllEnabledByDefault', 'Require fresh AllEnabledByDefault build before hardware execution')

    proof_path = HERE / 'analyzer-policy-v2.py'
    spec = importlib.util.spec_from_file_location('piv_hardware_analyzer_policy_v2', proof_path)
    proof_module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(proof_module)
    analyzer_proof = proof_module.prove(policy, arguments, sets['analyzer'], project)

    # Use the known project-reference algorithm: declared real projects each bind
    # one exact actual-Csc ref DLL rooted under that project's own obj directory.
    expected_projects = {ROOT / path for path in (
        'src/Piv/src/Yubico.YubiKit.Piv.csproj', 'src/Core/src/Yubico.YubiKit.Core.csproj',
        'src/Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj', 'src/Piv/examples/PivTool/PivTool.csproj')}
    declared = {Path(node.attrib['Include'].replace('$(RepositoryRoot)', str(ROOT))).resolve()
                for node in ET.parse(project).iter('ProjectReference')}
    require(declared == expected_projects, 'Require the exact four real project references')
    project_proof = []
    for path in sorted(declared):
        names = [node.text for node in ET.parse(path).iter('AssemblyName')]
        assembly = names[-1] if names else path.stem
        matches = [Path(p) for p in sets['reference'] if Path(p).name == assembly + '.dll']
        require(len(matches) == 1 and matches[0].is_relative_to(path.parent / 'obj'),
                'Actual Csc ref must bind the real project output: ' + str(path))
        project_proof.append(dict(project=identity(path), assembly=assembly, actual_csc_reference=identity(matches[0])))
    for name in ('project-references.txt', 'resolved-project-references.txt', 'imported-projects.txt'):
        retain(WORK / name, Path(name))
    actual_projects = {str(Path(p).resolve()) for p in lines(DEST / 'project-references.txt')}
    require(actual_projects == {str(p) for p in declared}, 'Actual ProjectReference item set mismatch')

    for kind, paths in sets.items():
        for value in sorted(paths):
            path = Path(value)
            row = dict(identity(path), kind=kind)
            if kind in ('source', 'analyzer-config'):
                row['retained'] = retain(path, Path(kind) / (row['sha256'] + '-' + path.name + '.snapshot'))['retained']
            records.append(row)
    input_paths = {ROOT / name for name in ('Directory.Build.props', 'Directory.Build.targets',
        'Directory.Packages.props', 'global.json', '.editorconfig')}
    input_paths |= {project, project.parent / 'xunit.runner.json', project.parent / 'probe.runsettings',
                    ROOT / 'src/Tests.Shared/appsettings.json', HERE / 'Directory.Build.props', HERE / 'Directory.Build.targets'}
    input_paths |= declared
    input_paths |= {Path(row['path']) for row in analyzer_proof['shared_sources'] + analyzer_proof['project_sources']}
    input_paths |= {Path(resolve(p)) for p in lines(DEST / 'imported-projects.txt')}
    sample = ROOT / 'src/Piv/examples/PivTool'
    sample_paths = {p for p in sample.rglob('*') if p.is_file() and p.suffix in ('.cs', '.csproj')
                    and not {'bin', 'obj'} & set(p.relative_to(sample).parts)}
    input_paths |= sample_paths
    for path in sorted(input_paths):
        row = identity(path)
        retain(path, Path('configuration-and-sample') / (row['sha256'] + '-' + path.name + '.snapshot'))
    retain(Path(policy['ProjectAssetsFile']), Path('project.assets.json.snapshot'))
    binaries = [identity(path) for path in sorted((WORK / 'bin').rglob('*.dll'))]
    require(any(Path(row['path']).name == 'AuditPiv001.Hardware.IntegrationTests.dll' for row in binaries), 'Unique compiled host DLL absent')
    result = dict(status='qualified', execution_qualified=True,
        actual_policy=policy, analyzer_package=analyzer_proof,
        exact_csc_sets={kind: sorted(paths) for kind, paths in sets.items()}, counts={kind: len(paths) for kind, paths in sets.items()},
        direct_project_proof=project_proof, records=records, retained=retained, live_output_binaries=binaries,
        actual_sample_sources=[identity(p) for p in sorted(sample_paths)],
        tools=[identity(Path(__file__)), identity(proof_path)],
        limitation='Matching fresh clean-build log supplies compiler success/diagnostics. Coordinator holds source stable from compilation through capture. Binary identities only; source/config/assets retained as UTF-8. Old failed compilation contexts remain historical and are never rebound to current product sources.')
    with (DEST / 'capture.json').open('x') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(json.dumps(dict(status=result['status'], destination=str(DEST), counts=result['counts'])))
    sys.exit(0)
except Exception as error:
    with (DEST / 'capture-failure.json').open('x') as output:
        json.dump(dict(error_type=type(error).__name__, error=str(error), scope='Compiler capture/provenance; no build/test/device execution'), output, indent=2)
        output.write('\n')
    raise
