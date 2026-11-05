"""Prove the exact canonical NuGet replacement of duplicate SDK built-in analyzers."""
from pathlib import Path
import hashlib
import json
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
EXAMPLES = HERE.parent
ROOT = next(path for path in HERE.parents if (path / 'toolchain.cs').exists())
PACKAGE = Path('/home/rupicola/.nuget/packages/microsoft.codeanalysis.netanalyzers/9.0.0')
VERSION = '9.0.0'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def nodes(path, tag):
    return [node for node in ET.parse(path).getroot().iter() if node.tag.rsplit('}', 1)[-1] == tag]


def prove(policy, args, analyzers, project):
    require(policy['EnableNETAnalyzers'] == 'false', 'Re-adjudicate a different SDK built-in analyzer selection')
    require(policy['RunAnalyzersDuringBuild'] == 'true' and policy['RunAnalyzers'].lower() != 'false' and
            policy['SkipAnalyzers'].lower() != 'true' and policy['SkipCompilerExecution'].lower() != 'true',
            'Analyzer/compiler execution disabled')
    require(policy['TreatWarningsAsErrors'] == 'true' and policy['WarningLevel'] == '9999' and
            '/warnaserror+' in args and '/warn:9999' in args and
            not any('/skipanalyzers' in arg.lower() for arg in args), 'Actual Csc warning/skip policy changed')
    actual_analyzers = {str((project.parent / arg[len('/analyzer:'):].strip('"')).resolve())
                        for arg in args if arg.startswith('/analyzer:')}
    require(actual_analyzers == analyzers, 'Actual Csc analyzer arguments differ from Analyzer items')
    expected = {str(PACKAGE / 'analyzers/dotnet/cs' / name) for name in
                ('Microsoft.CodeAnalysis.NetAnalyzers.dll', 'Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll')}
    require(expected <= analyzers, 'Both exact canonical NetAnalyzers 9.0.0 assemblies must participate in actual Csc')
    require({path for path in analyzers if Path(path).name.endswith('NetAnalyzers.dll')} == expected,
            'Unexpected/duplicate NetAnalyzers assembly origin')
    root_props, root_targets, versions = (ROOT / name for name in
                                         ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props'))
    require(any(node.text == 'true' for node in nodes(root_props, 'EnableNETAnalyzers')), 'Canonical initial built-in analyzer policy changed')
    refs = [node for node in nodes(root_targets, 'PackageReference') if node.attrib.get('Include') == 'Microsoft.CodeAnalysis.NetAnalyzers']
    require(len(refs) == 1 and refs[0].find('PrivateAssets').text == 'all' and
            'analyzers' in [value.strip() for value in refs[0].find('IncludeAssets').text.split(';')], 'Canonical explicit analyzer PackageReference changed')
    pins = [node for node in nodes(versions, 'PackageVersion') if node.attrib.get('Include') == 'Microsoft.CodeAnalysis.NetAnalyzers']
    require(len(pins) == 1 and pins[0].attrib['Version'] == VERSION, 'Canonical analyzer package pin changed')
    assets = Path(policy['ProjectAssetsFile'])
    data = json.loads(assets.read_text())
    library = data['libraries']['Microsoft.CodeAnalysis.NetAnalyzers/' + VERSION]
    require(library['type'] == 'package' and library['path'] == 'microsoft.codeanalysis.netanalyzers/' + VERSION and
            str(PACKAGE.parent.parent) + '/' in data['packageFolders'], 'Restore selected another analyzer package/root')
    require(any('Microsoft.CodeAnalysis.NetAnalyzers/' + VERSION in target for target in data['targets'].values()),
            'Canonical analyzer package absent from resolved target')
    props = PACKAGE / 'buildTransitive/Microsoft.CodeAnalysis.NetAnalyzers.props'
    disable = PACKAGE / 'buildTransitive/DisableNETAnalyzersForNuGetPackage.props'
    package_targets = PACKAGE / 'buildTransitive/Microsoft.CodeAnalysis.NetAnalyzers.targets'
    require(all(str(path.relative_to(PACKAGE)) in library['files'] for path in (props, disable, package_targets)),
            'Resolved package lacks exact import files')
    generated_props = assets.parent / (project.name + '.nuget.g.props')
    generated_targets = assets.parent / (project.name + '.nuget.g.targets')
    for generated, suffix in ((generated_props, 'props'), (generated_targets, 'targets')):
        needle = '$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers/9.0.0/buildTransitive/Microsoft.CodeAnalysis.NetAnalyzers.' + suffix
        imports = [node for node in nodes(generated, 'Import') if node.attrib.get('Project') == needle]
        require(len(imports) == 1 and imports[0].attrib['Condition'] == "Exists('" + needle + "')",
                'Actual generated NuGet import differs from pinned analyzer package')
    imports = [node for node in nodes(props, 'Import') if node.attrib.get('Project') == disable.name]
    require(len(imports) == 1 and imports[0].attrib['Condition'] == "Exists('DisableNETAnalyzersForNuGetPackage.props')",
            'Package built-in-disable import changed')
    require([node.text for node in nodes(disable, 'EnableNETAnalyzers')] == ['false'] and
            [node.text for node in nodes(disable, '_NETAnalyzersNuGetAssemblyVersion')] == [VERSION],
            'Package no longer supplies the precise false/version assignment')
    common = Path('/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.403/Current/Microsoft.Common.props')
    common_text = common.read_text()
    early = '<Import Project="$(DirectoryBuildPropsPath)"'
    later = '<Import Project="$(MSBuildProjectExtensionsPath)$(MSBuildProjectFile).*.props"'
    require(early in common_text and later in common_text and common_text.index(early) < common_text.index(later),
            'Pinned SDK directory-props/generated-NuGet-props import order changed')
    shared = [root_props, root_targets, versions, props, disable, package_targets, common]
    return dict(package='Microsoft.CodeAnalysis.NetAnalyzers', version=VERSION,
                builtin_selection='false: duplicate SDK analyzers replaced by the explicit canonical NuGet package',
                assemblies=[identity(Path(path)) for path in sorted(expected)],
                shared_sources=[identity(path) for path in shared],
                project_sources=[identity(path) for path in (generated_props, generated_targets)],
                assets=identity(assets), package_sha512=library['sha512'],
                analyzer_count=len(analyzers), csc_analyzer_membership_exact=True,
                warning_arguments=[arg for arg in args if arg.startswith(('/warn', '/nowarn:'))])


def verify_capture(data, evidence, live):
    require(data['tooling_provenance_sha256'] == identity(HERE / 'tooling-provenance.json')['sha256'],
            'Capture binds another analyzer correction receipt')
    proof = data['analyzer_package']
    require(proof['package'] == 'Microsoft.CodeAnalysis.NetAnalyzers' and proof['version'] == VERSION, 'Canonical analyzer package identity changed')
    retained = {row['path']: row for row in data['retained']}
    for row in proof['shared_sources'] + proof['project_sources']:
        name = 'analyzer-package/' + row['sha256'] + '-' + Path(row['path']).name + '.snapshot'
        require(retained[name]['sha256'] == row['sha256'] and retained[name]['bytes'] == row['bytes'],
                'Analyzer import proof snapshot missing/changed')
    captured_analyzers = {row['path']: row for row in data['records'] if row['kind'] == 'analyzer'}
    for row in proof['assemblies']:
        require(captured_analyzers[row['path']]['sha256'] == row['sha256'], 'Canonical analyzer identity differs from Csc record')
    if live:
        args = (evidence / 'csc-command-line.txt').read_text().splitlines()
        current = prove(data['policy'], args, set(captured_analyzers), Path(data['policy']['MSBuildProjectFullPath']))
        require(current == proof, 'Live canonical analyzer/import/assets proof differs from capture')
