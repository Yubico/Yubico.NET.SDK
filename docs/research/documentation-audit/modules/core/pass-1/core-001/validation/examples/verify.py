"""Read-only A/C extraction, wrapper, reference and selector readiness checks."""
import ast
import importlib.util
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    prepare = load('accepted_example_extractor', HERE / 'prepare.py')
    run = load('example_run_readiness', HERE / 'run.py')
    capture = run.capture
    manifest = json.loads((HERE / 'manifest.json').read_bytes())
    rows = manifest['examples']
    assert len(rows) == len({row['id'] for row in rows}) == 41
    for row in manifest['acceptance_inputs']:
        assert prepare.identity(prepare.ROOT / row['path']) == row
    for row in rows:
        fresh, raw, body = prepare.extract(row['path'], row['ordinal'])
        assert raw == (prepare.ROOT / row['raw_file']).read_bytes()
        assert body.encode() == (prepare.ROOT / row['decoded_file']).read_bytes()
        assert fresh['raw_start_byte'] == row['raw_start_byte'] and fresh['raw_end_byte'] == row['raw_end_byte']
        assert prepare.sha(raw) == row['raw_sha256'] and prepare.sha(body.encode()) == row['decoded_sha256']
        if row['compilation_status'] != 'prepared':
            assert 'generated_host_source' not in row
            continue
        prefix = (prepare.ROOT / row['wrapper_prefix_file']).read_bytes()
        suffix = (prepare.ROOT / row['wrapper_suffix_file']).read_bytes()
        expected = body.encode()
        if row['id'] == 'A-03':
            imports = row['preserved_import_prefix'].encode()
            assert expected.startswith(imports)
            expected = expected[len(imports):]
        generated = (prepare.ROOT / row['generated_host_source']['path']).read_bytes()
        assert generated == prefix + expected + suffix
        assert generated[row['compiled_body_start_byte']:row['compiled_body_end_byte']] == expected
        assert prepare.sha(expected) == row['wrapped_body_sha256']
        assert prepare.identity(prepare.ROOT / row['generated_host_source']['path']) == row['generated_host_source']
    selected = [row for row in rows if row['compilation_status'] == 'prepared']
    assert len(selected) == 17
    sources = sorted((HERE / 'host/src').glob('*.cs'))
    assert {str(path) for path in sources} == {str(prepare.ROOT / row['generated_host_source']['path']) for row in selected}
    project = ET.parse(capture.PROJECT).getroot()
    assert project.findtext('PropertyGroup/OutputType') == 'Library' and project.findtext('PropertyGroup/IsPackable') == 'false'
    assert not project.findall('.//NoWarn') and not project.findall('.//WarningsNotAsErrors')
    props = ET.parse(HERE / 'Directory.Build.props').getroot()
    imported = (HERE / props.find('Import').attrib['Project'].replace('$(MSBuildThisFileDirectory)', '')).resolve()
    assert imported == prepare.ROOT / 'Directory.Build.props'
    hook = ET.parse(HERE / 'capture.targets').getroot()
    imported_target = (HERE / hook.find('Import').attrib['Project'].replace('$(MSBuildThisFileDirectory)', '')).resolve()
    assert imported_target == prepare.ROOT / 'Directory.Build.targets'
    assert hook.find("Target[@Name='DocExamplesCompiler']").attrib['AfterTargets'] == 'CoreCompile'
    assert 'GenerateSourceLinkFile' in hook.find("Target[@Name='DocExamplesBefore']").attrib['DependsOnTargets']
    projects = sorted(run.closure(capture.PROJECT))
    assert {path.name for path in projects} == {capture.NAME + '.csproj', 'Yubico.YubiKit.Core.csproj',
                'Yubico.YubiKit.Piv.csproj', 'Yubico.YubiKit.WebAuthn.csproj', 'Yubico.YubiKit.Ctap2.csproj'}
    assert 'core' not in capture.PROJECT.stem.lower() and '.UnitTests' not in capture.PROJECT.name
    for path in HERE.glob('*.py'):
        ast.parse(path.read_bytes(), filename=str(path))
    sdk = prepare.ROOT / 'docs/research/documentation-audit/modules/core/pass-1/core-001/validation/commands-v2/sdk-info-first'
    receipt = json.loads((sdk / 'receipt.json').read_bytes())
    assert receipt['status'] == 'passed' and receipt['exit_code'] == 0
    assert '10.0.404' in (sdk / 'stdout.log').read_text() and '10.0.15' in (sdk / 'stdout.log').read_text()
    return dict(scope='Static preparation only; C# has not been compiled or executed by this worker.',
                counts=manifest['counts'], complete_bodies=4, contextual_fragments=13,
                extraction_hashes_and_exact_wrapper_reconstruction=True, root_imports=True,
                declared_project_closure=[str(path) for path in projects],
                actual_csc_membership='Prepared late hook; actual clean compilation required.',
                sdk_info_reused=prepare.identity(sdk / 'stdout.log'),
                no_example_execution=True)


if __name__ == '__main__':
    print(json.dumps(main(), indent=2))
