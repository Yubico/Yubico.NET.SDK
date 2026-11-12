"""Read-only hook/item-source regression check; never replays the failed build as success."""
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
BINDING = HERE.parent
ROOT = BINDING.parents[8]
SDK = Path('/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.404')


def inspect(hook):
    tree = ET.fromstring(hook)
    compiler = ET.parse(SDK / 'Roslyn/Microsoft.CSharp.Core.targets').getroot()
    ns = {'m': 'http://schemas.microsoft.com/developer/msbuild/2003'}
    core = compiler.find("m:Target[@Name='CoreCompile']", ns)
    if core is None:
        core = compiler.find("Target[@Name='CoreCompile']")
    assert core is not None, 'Selected SDK CoreCompile missing'
    csc = next(node for node in core if node.tag.rsplit('}', 1)[-1] == 'Csc')
    output = next(node for node in csc if node.attrib.get('TaskParameter') == 'CommandLineArgs')
    item = output.attrib['ItemName']
    assert csc.attrib['ProvideCommandLineArgs'] == '$(ProvideCommandLineArgs)'
    target = tree.find("Target[@Name='CoreDocCheckActualCsc']")
    assert target is not None
    assert target.attrib.get('AfterTargets') == 'CoreCompile', 'Read Csc task outputs after CoreCompile commits its lookup'
    assert not tree.findall('.//TargetsTriggeredByCompilation'), 'Capture must avoid the legacy in-target CallTarget scope'
    writer = next(node for node in target if node.attrib.get('File', '').endswith('/csc-arguments.txt'))
    assert writer.attrib['Lines'] == '@(' + item + ')', 'Hook reads a different SDK compiler output item'
    assert tree.find("PropertyGroup/ProvideCommandLineArgs").text == 'true'
    assert tree.find("PropertyGroup/SkipCompilerExecution").text == 'false'
    return dict(sdk_item=item, parameter=csc.attrib['ProvideCommandLineArgs'], phase=target.attrib['AfterTargets'])


def main():
    old = (HERE / 'hook.targets.snapshot').read_bytes()
    try:
        inspect(old)
    except AssertionError as error:
        original_rejection = str(error)
    else:
        raise AssertionError('The original in-target scope was accepted')
    current = inspect((BINDING / 'hook.targets').read_bytes())
    failed = BINDING / 'attempts/checker-bound-build-first'
    assert (failed / 'csc-arguments.txt').read_bytes() == b''
    assert not any((failed / name).exists() for name in ('compile.json', 'finish.json', 'sealed.json'))
    sys.path.insert(0, str(BINDING.parent))
    spec = importlib.util.spec_from_file_location('late_capture_binder', BINDING / 'bind.py')
    binder = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(binder)
    try:
        binder.actual_arguments(failed, json.loads((failed / 'before.json').read_bytes())['items'])
    except ValueError:
        empty_arguments_still_rejected = True
    else:
        raise AssertionError('The retained empty argument capture was admitted')
    preserved = json.loads((HERE / 'failure-preservation.json').read_bytes())
    for row in preserved['retained_failed_records']:
        raw = Path(row['path']).read_bytes()
        assert len(raw) == row['bytes'] and hashlib.sha256(raw).hexdigest() == row['sha256']
    for pair in preserved['external_job_files']:
        raw = Path(pair['preserved']['path']).read_bytes()
        assert len(raw) == pair['original']['bytes'] and hashlib.sha256(raw).hexdigest() == pair['original']['sha256']
    integration = ROOT / 'docs/research/documentation-audit/integrations/core-upstream-20261111/validation/consumer-proof/Capture.targets'
    proven = ET.parse(integration).getroot()
    late = proven.find("Target[@Name='AuditIntegration20261111AfterCompiler']")
    assert late.attrib['AfterTargets'] == current['phase']
    assert any(node.attrib.get('Lines') == '@(' + current['sdk_item'] + ')' for node in late)
    return dict(scope='Static hook/item-source and failed-byte preservation checks only; no build or capture replay.',
                original_hook_rejection=original_rejection, corrected_hook=current,
                retained_failed_records=len(preserved['retained_failed_records']),
                preserved_external_job_files=len(preserved['external_job_files']),
                empty_arguments_still_rejected=empty_arguments_still_rejected,
                failed_build_stays_failed=True, actual_second_build='pending coordinator assignment')


if __name__ == '__main__':
    print(json.dumps(main(), indent=2))
