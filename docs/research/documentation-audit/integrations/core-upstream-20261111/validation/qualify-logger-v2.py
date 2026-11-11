"""Disposable home-only Python process/I/O qualification; no SDK/native/device gates."""
import copy
import argparse
import errno
import hashlib
import importlib.util
import json
import os
import re
from pathlib import Path
import subprocess
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]
RUNTIME = Path('/home/rupicola/Code/y/.core-docs-audit-runtime')
WORK = RUNTIME / 'qualification/logger-v2-first'
ATTEMPT = 'first'


def load():
    spec = importlib.util.spec_from_file_location('qualified_logger_v2', HERE / 'run-command-v2.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def case(name):
    runner = load()
    basic = "import sys;print('owned stdout',flush=True);print('owned stderr',file=sys.stderr,flush=True);sys.exit(7)"
    orphan = ("import subprocess,sys,time;print('parent stdout',flush=True);"
              "print('parent stderr',file=sys.stderr,flush=True);"
              "p=subprocess.Popen([sys.executable,'-c',"
              "\"import signal,time,os;signal.signal(signal.SIGTERM,signal.SIG_IGN);print('OWNED_DESCENDANT='+str(os.getpid()),flush=True);time.sleep(120)\"]);"
              "time.sleep(0.25);sys.exit(9)")
    code = orphan if name == 'orphan' else basic if name == 'nonzero' else basic.replace('sys.exit(7)', 'sys.exit(0)')
    args = runner.parser().parse_args(['--label', name, '--output-root', str(WORK / 'records'),
                                     '--', sys.executable, '-B', '-c', code])
    return runner.capture(args, faults=[name] if name in ('retention', 'receipt') else ())


def main():
    assert not WORK.exists(), 'Preserve previous qualification; choose a fresh explicitly reviewed identity'
    WORK.mkdir()
    environment = dict(os.environ, TMPDIR=str(RUNTIME / 'tmp'), TMP=str(RUNTIME / 'tmp'),
                       TEMP=str(RUNTIME / 'tmp'), PYTHONDONTWRITEBYTECODE='1')
    results = []
    for name in ('nonzero', 'orphan', 'retention', 'receipt'):
        command = [sys.executable, '-B', str(Path(__file__).resolve()), '--case', name, '--attempt', ATTEMPT]
        completed = subprocess.run(command, cwd=ROOT, env=environment, capture_output=True, timeout=20)
        (WORK / (name + '.stdout.log')).write_bytes(completed.stdout)
        (WORK / (name + '.stderr.log')).write_bytes(completed.stderr)
        assert completed.returncode == 1, (name, completed.returncode, completed.stderr)
        folder = WORK / 'records' / name
        external = WORK / 'records/journals' / name / 'final.json'
        receipt = json.loads(external.read_bytes())
        assert receipt['status'] == 'failed' and receipt['quiescent'] is True
        assert receipt['stream_complete'] is True
        assert receipt['ownership']['after'] == {'members': [], 'escaped': []}
        assert receipt['streams'] and (folder / 'stdout.log').read_bytes() and (folder / 'stderr.log').read_bytes()
        if name == 'nonzero':
            assert receipt['exit_code'] == 7
            assert (folder / 'stdout.log').read_bytes() == b'owned stdout\n'
            assert (folder / 'stderr.log').read_bytes() == b'owned stderr\n'
        if name == 'orphan':
            assert receipt['exit_code'] == 9
            signals = receipt['ownership']['signals']
            assert [row['signal'] for row in signals] == ['SIGTERM', 'SIGKILL']
            assert all(member['pgid'] == receipt['ownership']['pgid'] and member['sid'] == receipt['ownership']['sid']
                       for row in signals for member in row['proven_members'])
            pid = int(next(line.split('=', 1)[1] for line in (folder / 'stdout.log').read_text().splitlines()
                           if line.startswith('OWNED_DESCENDANT=')))
            assert not Path(f'/proc/{pid}').exists(), 'Owned orphan was not reaped'
        if name == 'retention':
            assert receipt['retention_complete'] is False
            assert any('Injected disposable retention failure' in error for error in receipt['errors'])
        if name == 'receipt':
            assert not (folder / 'receipt.json').exists(), 'Primary failure published a misleading receipt'
            assert any('Injected disposable primary receipt failure' in error for error in receipt['errors'])
        results.append(dict(case=name, wrapper_exit=completed.returncode, child_exit=receipt['exit_code'],
                            quiescent=receipt['quiescent'], durable_external_receipt=str(external),
                            signals=[row['signal'] for row in receipt['ownership']['signals']]))
    runner = load()
    existing = WORK / 'immutable-receipt.json'
    runner.atomic_json(existing, dict(identity='preserve me'))
    baseline = existing.read_bytes()
    try:
        runner.atomic_json(existing, dict(identity='replacement forbidden'))
        raise AssertionError('Receipt overwrite was accepted')
    except ValueError:
        assert existing.read_bytes() == baseline
    partial_target = WORK / 'injected-fsync-receipt.json'
    saved_fsync = runner.os.fsync
    def fail_fsync(fd):
        raise OSError(errno.EDQUOT, 'Injected disposable fsync failure')
    runner.os.fsync = fail_fsync
    try:
        try:
            runner.atomic_json(partial_target, dict(identity='partial stays diagnosable'))
            raise AssertionError('Injected write failure was accepted')
        except OSError as error:
            assert error.errno == errno.EDQUOT
            assert not partial_target.exists()
            assert list(WORK.glob('injected-fsync-receipt.json.partial-*'))
    finally:
        runner.os.fsync = saved_fsync
    historical = ROOT / 'docs/research/documentation-audit/modules/piv/pass-1/piv-001/validation/final-repository-v2-20261105-090952-590-build+test+docs-qa+docs-architecture+complexity/result.json'
    result = json.loads(historical.read_bytes())
    expected = json.loads((HERE / 'expected-unit-projects.json').read_bytes())
    targets = ['build', 'test', 'docs-qa', 'docs-architecture', 'complexity']
    v1 = runner.original()
    counts = v1.check_result(result, 0, targets, expected)
    rejections = []
    for label in ('exit', 'target', 'warnings', 'membership', 'zero-execution', 'failed-test'):
        changed = copy.deepcopy(result)
        code = 1 if label == 'exit' else 0
        if label == 'target':
            changed['targets'] = [row for row in changed['targets'] if row['name'] != 'build']
        if label == 'warnings':
            changed['warningCount'] = 1
        if label == 'membership':
            changed['projects'].pop()
        if label == 'zero-execution':
            changed['projects'][0]['executed'] = 0
        if label == 'failed-test':
            changed['projects'][0]['failedTests'] = [dict(name='deliberate parser mutation')]
        try:
            v1.check_result(changed, code, targets, expected)
            raise AssertionError(f'Guard silently accepted {label}')
        except ValueError:
            rejections.append(label)
    report = dict(status='qualified-disposable-python-cases', attempt=ATTEMPT, cases=results,
        atomic_overwrite_rejected=True, failed_fsync_preserved_partial_without_publishing=True,
        historical_parser_counts=counts, fail_closed_parser_rejections=rejections,
        logger_sha256=hashlib.sha256((HERE / 'run-command-v2.py').read_bytes()).hexdigest(),
        qualifier_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        limits='Python process/I/O qualification and historical parser replay only; no SDK build/test/native/device execution.')
    (WORK / 'qualification.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--case', choices=('nonzero', 'orphan', 'retention', 'receipt'))
    parser.add_argument('--attempt', default='first')
    args = parser.parse_args()
    assert re.fullmatch(r'[a-z0-9][a-z0-9-]*', args.attempt)
    ATTEMPT = args.attempt
    WORK = RUNTIME / 'qualification' / ('logger-v2-' + ATTEMPT)
    if args.case:
        sys.exit(case(args.case))
    main()
