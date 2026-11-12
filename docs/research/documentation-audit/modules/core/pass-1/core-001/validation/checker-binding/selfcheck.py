"""Pure metadata oracles for the checker-only evidence-admission boundary."""
import copy
import importlib.util
from pathlib import Path

HERE = Path(__file__).resolve().parent


def main():
    spec = importlib.util.spec_from_file_location('checker_binding', HERE / 'bind.py')
    binding = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(binding)
    try:
        binding.interrupted(15, None)
    except KeyboardInterrupt:
        pass
    else:
        raise AssertionError('SIGTERM adapter bypassed the supervisor cleanup exception path')
    binding.verify_rows([dict(path='/known/abc', bytes=3,
        sha256='ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad')], lambda path: b'abc')
    stamp = dict(path='/owned/checker.dll', bytes=3, sha256='123')
    before = dict(inputs=[dict(path='/reviewed/Program.cs', bytes=4, sha256='456')])
    compile_record = dict(inputs=copy.deepcopy(before['inputs']), actual_csc=True,
                          intermediate=copy.deepcopy(stamp), arguments=['/out:/owned/checker.dll'])
    finish = dict(inputs=copy.deepcopy(before['inputs']), outputs=[copy.deepcopy(stamp)],
                  compiled_dll=copy.deepcopy(stamp), runtime_dll='/owned/checker.dll')
    receipt = dict(status='passed', exit_code=0, stream_complete=True, retention_complete=True,
                   quiescent=True, result_counts=dict(failed=0, warnings=0))
    binding.admit(before, compile_record, finish, receipt)
    rejected = []
    for label in ('source-changed', 'source-missing', 'no-csc', 'dll-replaced', 'dll-missing',
                  'wrong-dll-path', 'failed-build', 'incomplete-stream', 'incomplete-retention',
                  'live-child', 'warnings', 'nonzero-exit'):
        a, b, c, d = copy.deepcopy((before, compile_record, finish, receipt))
        if label == 'source-changed':
            b['inputs'][0]['sha256'] = 'changed'
        elif label == 'source-missing':
            c['inputs'] = []
        elif label == 'no-csc':
            b['actual_csc'] = False
        elif label == 'dll-replaced':
            c['compiled_dll']['sha256'] = 'replacement'
        elif label == 'dll-missing':
            c['outputs'] = []
        elif label == 'wrong-dll-path':
            c['runtime_dll'] = '/unrelated/checker.dll'
        elif label == 'failed-build':
            d['status'] = 'failed'
        elif label == 'incomplete-stream':
            d['stream_complete'] = False
        elif label == 'incomplete-retention':
            d['retention_complete'] = False
        elif label == 'live-child':
            d['quiescent'] = False
        elif label == 'warnings':
            d['result_counts']['warnings'] = 1
        elif label == 'nonzero-exit':
            d['exit_code'] = 7
        try:
            binding.admit(a, b, c, d)
        except ValueError:
            rejected.append(label)
        else:
            raise AssertionError('Accepted invalid proof: ' + label)
    try:
        binding.verify_rows([stamp], lambda path: b'old')
    except ValueError:
        rejected.append('stale-external-output')
    else:
        raise AssertionError('Accepted a stale external binary')
    def missing(path):
        raise FileNotFoundError(path)
    try:
        binding.verify_rows([stamp], missing)
    except FileNotFoundError:
        rejected.append('missing-external-output')
    else:
        raise AssertionError('Accepted a missing external binary')
    items = dict(sources=['/source/Program.cs'], references=['/references/framework.dll'],
                 analyzers=['/analyzers/canonical.dll'], configs=['/config/build.editorconfig'], additional=[])
    lines = ['/warnaserror+', '/out:/owned/checker.dll', '/refout:/owned/ref.dll', '/source/Program.cs',
             '/reference:/references/framework.dll', '/analyzer:/analyzers/canonical.dll',
             '/analyzerconfig:/config/build.editorconfig']
    class MemoryArguments:
        def __init__(self, values):
            self.values = values
        def __truediv__(self, name):
            assert name == 'csc-arguments.txt'
            return self
        def read_text(self):
            return '\n'.join(self.values)
    assert binding.actual_arguments(MemoryArguments(lines), items) == lines
    for label, changed in [('missing-source', lines[:3] + lines[4:]),
                           ('duplicate-reference', lines + [lines[4]]),
                           ('missing-analyzer', [line for line in lines if not line.startswith('/analyzer:')]),
                           ('different-config', [line.replace('build.editorconfig', 'other.editorconfig') for line in lines]),
                           ('unbound-source-link', lines + ['/sourcelink:/unbound/source-link.json']),
                           ('response-file', lines + ['@hidden.rsp']), ('skipped-analyzers', lines + ['/skipanalyzers+']),
                           ('weakened-warnings', lines[1:])]:
        try:
            binding.actual_arguments(MemoryArguments(changed), items)
        except ValueError:
            rejected.append(label)
        else:
            raise AssertionError('Accepted Csc/input mutation: ' + label)
    import xml.etree.ElementTree as ET
    hook = ET.parse(HERE / 'hook.targets').getroot()
    relative = hook.find('Import').attrib['Project'].replace('$(MSBuildThisFileDirectory)', '')
    assert (HERE / relative).resolve() == binding.ROOT / 'Directory.Build.targets'
    return dict(scope='Pure metadata admission and byte-identity checks; no SDK/process/native/device execution.',
                accepted_complete_fixture=True, accepted_known_sha256_vector=True, exact_csc_membership_fixture=True,
                sigterm_exception_path=True,
                root_policy_import=str((HERE / relative).resolve()), rejected=rejected)


if __name__ == '__main__':
    import json
    print(json.dumps(main(), indent=2))
