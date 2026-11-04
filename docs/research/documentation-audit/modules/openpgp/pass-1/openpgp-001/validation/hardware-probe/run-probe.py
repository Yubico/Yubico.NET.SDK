"""Coordinator-only serialized one-case execution with independent vendor cleanup in finally."""
from pathlib import Path
import argparse
import datetime
import json
import re
import shlex
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent
VENDOR = RUN / 'hardware/vendor-state.py'
CASES = (
    'KdfDistinctSalts_ExtendedVerifyUsesUserIdentity',
    'ExtendedWrongPin_ReportsUserRetryCounter',
    'TouchSign_P256_HostVerifies',
    'TouchAuthenticate_P256_OrdinaryVerifyIsInsufficient',
    'TouchDecrypt_Rsa_KnownPlaintextAndModeBoundary',
    'TouchAttest_PreservedIssuerSignsGeneratedKeyCertificate',
    'CancelSign_NoTouch_ObserveTerminalThenFreshRecoveryBeforeReset',
)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('case', choices=CASES)
parser.add_argument('--name', required=True)
parser.add_argument('--baseline', required=True, type=Path)
args = parser.parse_args()
if not re.fullmatch(r'[a-z0-9-]+', args.name):
    parser.error('Choose a unique lowercase evidence name')
baseline = args.baseline.resolve()
initial = json.loads(baseline.read_text())
if initial.get('mode') != 'baseline' or initial.get('serial_guard') != 103 or not initial.get('success'):
    parser.error('A successful original serial-103 read-only baseline is required')
destination = HERE / 'evidence' / args.name
destination.mkdir(parents=True, exist_ok=False)
results = {'case': args.case, 'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
           'baseline': str(baseline), 'test_exit': None, 'cleanup_exit': None}


def command(label, arguments):
    print(f'{label}: {shlex.join(arguments)}', flush=True)
    with (destination / f'{label}.log').open('x') as output:
        output.write('$ ' + shlex.join(arguments) + '\n')
        output.flush()
        # Await child termination before starting another PC/SC connection. The
        # test runner has its own 120-second session watchdog; avoiding a second
        # parent-only timeout keeps this wrapper from abandoning native workers.
        completed = subprocess.run(arguments, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT)
        output.write(f'\nEXIT={completed.returncode}\n')
    return completed.returncode


def retain_toolchain_logs():
    text = (destination / 'dotnet.log').read_text()
    matches = re.findall(r'Run log: (artifacts/toolchain/runs/[^\s]+/run\.log)', text)
    if not matches:
        results['toolchain_log_retention'] = 'No RESULT run-log path found; inspect dotnet.log'
        return
    source = (ROOT / matches[-1]).parent.resolve()
    source.relative_to((ROOT / 'artifacts/toolchain/runs').resolve())
    target = destination / 'toolchain'
    target.mkdir(exist_ok=False)
    for path in sorted(source.iterdir()):
        if path.is_file() and path.suffix in ('.log', '.json'):
            data = path.read_bytes()
            data.decode('utf-8')
            with (target / path.name).open('xb') as output:
                output.write(data)
    results['toolchain_log_retention'] = str(source.relative_to(ROOT))


try:
    results['test_exit'] = command('dotnet', ['dotnet', 'toolchain.cs', '--', 'test', '--integration',
        '--project', 'AuditPgp001.Hardware.IntegrationTests', '--filter', 'FullyQualifiedName~' + args.case])
    retain_toolchain_logs()
finally:
    try:
        # Capture independent fresh public readback before reset, including a
        # cancellation failure. This complements the stronger .NET fresh-session
        # crypto recovery assertion; it never substitutes for that assertion.
        results['before_cleanup_read_exit'] = command('fresh-before-reset', [sys.executable, str(VENDOR),
            'baseline', '--output', str(destination / 'fresh-before-reset.json')])
    finally:
        try:
            results['cleanup_exit'] = command('vendor-cleanup', [sys.executable, str(VENDOR), 'cleanup',
                '--authorize-reset-serial-103', '--baseline', str(baseline),
                '--output', str(destination / 'vendor-cleanup.json')])
        finally:
            with (destination / 'result.json').open('x') as output:
                json.dump(results, output, indent=2)
                output.write('\n')
print(json.dumps(results))
raise SystemExit(0 if results['test_exit'] == results['cleanup_exit'] == results.get('before_cleanup_read_exit') == 0 else 1)
