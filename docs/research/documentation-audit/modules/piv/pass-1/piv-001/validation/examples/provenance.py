"""Bind actual project references and SDK compiler DLLs while recording host versions separately."""
from pathlib import Path
import json
import xml.etree.ElementTree as ET
from inventory import ROOT, sha, require


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=sha(raw))


def project_reference_proof(project, policy, references, evidence):
    def resolve(value):
        value = value.replace('$(RepositoryRoot)', str(ROOT)).replace('\\', '/')
        require('$(' not in value, 'Resolve additional project-reference expressions explicitly')
        return (project.parent / value).resolve()

    declared = {resolve(node.attrib['Include']) for node in ET.parse(project).iter('ProjectReference')}
    actual = {Path(value).resolve() for value in (evidence / 'project-references.txt').read_text().splitlines() if value.strip()}
    require(declared == actual and bool(actual), 'Actual project references differ from the compiled project metadata')
    projects, snapshots = [], []
    for path in sorted(actual):
        require(path.is_relative_to(ROOT / 'src') and not path.is_symlink(), 'Require real SDK/shared-project dependencies')
        tree = ET.parse(path)
        names = [node.text for node in tree.iter('AssemblyName')]
        assembly = names[-1] if names else path.stem
        require('$(' not in assembly, 'Resolve computed assembly identity explicitly')
        matches = [Path(value) for value in references if Path(value).name == assembly + '.dll']
        require(len(matches) == 1, 'Actual Csc requires one resolved DLL for direct project: ' + str(path))
        row = identity(path)
        relative = 'project-reference/' + row['sha256'] + '-' + path.name + '.snapshot'
        target = evidence / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        with target.open('xb') as output:
            output.write(path.read_bytes())
        snapshots.append(dict(path=relative, bytes=row['bytes'], sha256=row['sha256']))
        projects.append(dict(project=row, assembly=assembly, csc_reference=identity(matches[0])))
    sdk = [identity(Path(value)) for value in sorted(references) if Path(value).name.startswith('Yubico.YubiKit.')]
    require({'Yubico.YubiKit.Piv.dll', 'Yubico.YubiKit.Core.dll'} <= {Path(row['path']).name for row in sdk}, 'Actual Csc requires current PIV and Core references')
    versions = ET.parse(ROOT / 'Directory.Packages.props')
    canonical_version = [node.text for node in versions.iter('YubiKitVersion')]
    require(canonical_version == [policy['YubiKitVersion']], 'Recorded SDK package version differs from canonical metadata')
    require(policy['TargetFramework'] == 'net10.0' and policy['Configuration'] == 'Release' and policy['LangVersion'] in ('14', '14.0'), 'Actual SDK compilation configuration changed')
    return dict(direct_projects=projects, sdk_csc_references=sdk, retained=snapshots,
                resolved_project_items=(evidence / 'resolved-project-references.txt').read_text(),
                host_version=policy['Version'], host_assembly_version=policy['AssemblyVersion'],
                sdk_package_version=policy['YubiKitVersion'],
                rationale='Root import order supplies host and SDK version properties independently; actual Csc DLL identities establish SDK binding.')


def verify_project_proof(proof, evidence, live):
    for row in proof['retained']:
        raw = (evidence / row['path']).read_bytes()
        require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], 'Retained project metadata changed')
    require((evidence / 'resolved-project-references.txt').read_text() == proof['resolved_project_items'], 'Resolved project items changed')
    if live:
        for row in proof['direct_projects']:
            require(identity(Path(row['project']['path'])) == row['project'], 'Live direct-project metadata changed')
            require(identity(Path(row['csc_reference']['path'])) == row['csc_reference'], 'Live actual project-reference DLL changed')
        for row in proof['sdk_csc_references']:
            require(identity(Path(row['path'])) == row, 'Live SDK Csc reference changed')
