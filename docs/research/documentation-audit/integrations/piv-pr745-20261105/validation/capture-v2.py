"""Versioned adapter for the preserved v1 capture's host/catalog version assumption.

Reads the completed build workspace; never invokes MSBuild, Csc, tests or examples.
The original extractor, manifest and generated compiler inputs stay unchanged.
"""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ORIGINAL = HERE / 'examples.py'
ORIGINAL_SHA = '7a8a4327642048a1e121a37ce02891d25ddff25227ec8232aba2b3949e63374f'
MANIFEST_SHA = '6fa660fa570161a5d3035c2492b27651aea306bd9a882f858bdd2ace6f7a37d1'
POLICY_SHA = 'ef072cb30326126d990698ef12c8beb1ded11e8e55222abc8c098b5acdbc7404'
FAILURE = HERE / 'capture-v1-failure'
DEST = HERE / 'compiler-evidence-v2'
RECEIPT = HERE / 'capture-v2-receipt.json'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def frozen(path, expected):
    raw = path.read_bytes()
    assert sha(raw) == expected, f'Input identity changed: {path}'
    return raw


def run(mode):
    source = frozen(ORIGINAL, ORIGINAL_SHA)
    manifest = frozen(HERE / 'manifest.json', MANIFEST_SHA)
    old = "    assert policy['Version'] == policy['YubiKitVersion'] and policy['Version']\n"
    new = "    assert policy['Version'] and policy['YubiKitVersion']  # Separate observed host/catalog properties; adapter verifies project versions.\n"
    text = source.decode()
    assert text.count(old) == 1 and text.count("dest = HERE / 'compiler-evidence'") == 1
    adapted = text.replace(old, new).replace("dest = HERE / 'compiler-evidence'", "dest = HERE / 'compiler-evidence-v2'")
    ns = dict(__file__=str(ORIGINAL), __name__='piv745_preserved_capture_v2')
    exec(compile(adapted, str(ORIGINAL) + '[capture-v2-adapter]', 'exec'), ns)
    ns['prepare_or_check']('check')
    root, work, project = ns['ROOT'], ns['WORK'], ns['PROJECT']
    retain, record, json_bytes = ns['retain'], ns['record'], ns['json_bytes']
    policy_raw = frozen(work / 'policy-final.txt', POLICY_SHA)
    policy = dict(line.split('=', 1) for line in policy_raw.decode().splitlines() if line.strip())
    args = (work / 'csc-command-line.txt').read_text().splitlines()
    assert policy['LangVersion'] == '14' and policy['TargetFramework'] == 'net10.0'
    assert '/langversion:14' in args and '/target:library' in args and '/nullable:enable' in args
    catalog_version = ET.parse(root / 'Directory.Packages.props').findtext('.//YubiKitVersion')
    assert policy['YubiKitVersion'] == catalog_version
    assets = json.loads((work / 'obj/project.assets.json').read_text())
    declared = [(root / row.attrib['Include'].replace('$(RepositoryRoot)/', '')).resolve()
                for row in ET.parse(project).findall('.//ProjectReference')]
    restore = assets['project']['restore']['frameworks']['net10.0']['projectReferences']
    assert {str(p) for p in declared} == set(restore)
    ref_rows = dict(line.split('|', 1) for line in (work / 'resolved-references.txt').read_text().splitlines() if line.strip())

    def attributes(path):
        text = path.read_text()
        result = {}
        for key in ['AssemblyVersion', 'AssemblyFileVersion', 'AssemblyInformationalVersion']:
            matches = re.findall(r'\b' + key + r'Attribute\("([^\"]+)"\)', text)
            assert len(matches) == 1, (path, key)
            result[key] = matches[0]
        return result

    sdk = []
    for path in declared:
        name = path.stem
        matches = [(key, value) for key, value in assets['libraries'].items()
                   if key.split('/')[0] == name and value['type'] == 'project']
        assert len(matches) == 1
        identity, library = matches[0]
        assert (project.parent / library['msbuildProject']).resolve() == path
        assert assets['targets']['net10.0'][identity]['type'] == 'project'
        binary = path.parent / 'obj/Release/net10.0/ref' / (name + '.dll')
        assert str(binary) in ref_rows, f'Actual compiler reference is not the declared project output: {name}'
        assembly_source = path.parent / 'obj/Release/net10.0' / (name + '.AssemblyInfo.cs')
        versions = attributes(assembly_source)
        assert identity.split('/', 1)[1] == versions['AssemblyInformationalVersion'].split('+', 1)[0]
        assert f"Version={versions['AssemblyVersion']}," in ref_rows[str(binary)]
        sdk.append(dict(project=record(path), restore_identity=identity,
                        compiler_reference=record(binary), fusion_identity=ref_rows[str(binary)],
                        assembly_source=record(assembly_source), versions=versions))
    host_source = work / 'obj/Release/net10.0' / (project.stem + '.AssemblyInfo.cs')
    host_versions = attributes(host_source)
    assert host_versions['AssemblyInformationalVersion'].split('+', 1)[0] == policy['Version']
    workspace_before = [record(p) for p in sorted(work.rglob('*')) if p.is_file()]
    if mode == 'capture':
        assert not DEST.exists() and not RECEIPT.exists(), 'Preserve completed v2 capture; use check to verify it'
        # Reproduce only the original Python capture failure, with the held completed compiler workspace.
        failed = subprocess.run([sys.executable, str(ORIGINAL), 'capture'], text=True, capture_output=True)
        assert failed.returncode == 1 and "assert policy['Version'] == policy['YubiKitVersion']" in failed.stderr
        assert not (HERE / 'compiler-evidence').exists(), 'Original failure unexpectedly wrote capture output'
        retain(FAILURE / 'examples.py.snapshot', source)
        retain(FAILURE / 'manifest.json.snapshot', manifest)
        retain(FAILURE / 'policy-final.txt.snapshot', policy_raw)
        retain(FAILURE / 'invocation.stdout.txt', failed.stdout.encode())
        retain(FAILURE / 'invocation.stderr.txt', failed.stderr.encode())
        retain(FAILURE / 'invocation.json', json_bytes(dict(
            command=[sys.executable, str(ORIGINAL), 'capture'], exit_code=failed.returncode,
            provenance='Coordinator reported the same failure; this retained invocation reproduces it against the unchanged held build workspace. Python capture only; no build or compiler invocation.',
            original_sha256=ORIGINAL_SHA, manifest_sha256=MANIFEST_SHA, policy_sha256=POLICY_SHA)))
        ns['capture']()  # All original warning, no-skip and actual Csc membership assertions remain.
        for item in sdk:
            assembly = root / item['assembly_source']['path']
            retain(DEST / 'sdk-projects' / (Path(item['project']['path']).stem + '.AssemblyInfo.cs.snapshot'), assembly.read_bytes())
        retain(DEST / 'adapted-capture-source.py.snapshot', adapted.encode())
        assert workspace_before == [record(p) for p in sorted(work.rglob('*')) if p.is_file()], 'Capture modified compiler workspace'
        receipt = dict(status='captured-current-compiler-membership-v2',
            original_capture_sha256=ORIGINAL_SHA, original_manifest_sha256=MANIFEST_SHA,
            adapter=record(Path(__file__).resolve()), adapted_source_sha256=sha(adapted.encode()),
            adaptations=[dict(old=old.strip(), new=new.strip()),
                         dict(old="compiler-evidence", new="compiler-evidence-v2")],
            explanation='Host Version and catalog YubiKitVersion are separate evaluated properties. Real restored project versions, actual reference paths/FusionName versions and generated SDK assembly attributes are recorded independently; no equality with catalog version is inferred.',
            sdk_project_references=sdk, host_assembly_versions=host_versions,
            host_version=policy['Version'], catalog_version=policy['YubiKitVersion'],
            language_and_target=dict(LangVersion=policy['LangVersion'], TargetFramework=policy['TargetFramework']),
            compiler_workspace_files=workspace_before,
            preserved=[record(p) for folder in [FAILURE, DEST] for p in sorted(folder.rglob('*')) if p.is_file()],
            limits='No build/test/example/hardware execution. Compiler capture and source membership verified against the held completed build; the coordinator owns the separate passing build RESULT. SDK reference FusionName/assembly-source versions are recorded as observed, not changed to catalog/package intent.')
        retain(RECEIPT, json_bytes(receipt))
    else:
        receipt = json.loads(RECEIPT.read_text())
        assert receipt['adapter'] == record(Path(__file__).resolve())
        assert receipt['sdk_project_references'] == sdk
        assert receipt['compiler_workspace_files'] == workspace_before
        for row in receipt['preserved']:
            path = Path(row['path']) if Path(row['path']).is_absolute() else root / row['path']
            assert record(path) == row, f'Retained capture changed: {path}'
    frozen(ORIGINAL, ORIGINAL_SHA)
    frozen(HERE / 'manifest.json', MANIFEST_SHA)
    print(f'{mode}: v2 compiler capture verified; original tool/manifest preserved; three genuine SDK project references and observed versions bound; net10.0/C#14 and original strict compiler checks retained.')


if __name__ == '__main__':
    assert sys.argv[1:] in [['capture'], ['check']], 'Usage: capture-v2.py capture|check'
    run(sys.argv[1])
