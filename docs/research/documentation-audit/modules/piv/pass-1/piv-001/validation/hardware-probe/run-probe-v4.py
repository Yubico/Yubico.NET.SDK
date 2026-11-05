"""Coordinator-only one-case runner with raw exits and independent serial-103 cleanup."""
from pathlib import Path
import argparse
import datetime
import hashlib
import json
import os
import re
import shlex
import shutil
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent
CASES = (
    'P01_X25519_Rfc7748AliceAndBobAgree',
    'P02_PinAlwaysConsumesVerification_PinOnceReusesIt',
    'P03_MoveAndDeletePreserveIndependentCertificates',
    'P04_AttestationBindsGeneratedKeyToPreservedIssuer',
    'P05_ActualPivToolRsaSignsOriginalMessage',
    'P06_ActualPivToolEd25519SignsOriginalMessage',
    'P07_CancelTouchWait_DrainThenFreshSameKeyRecovery',
)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('case', choices=CASES[:6])  # Physical P07 remains deferred.
parser.add_argument('--name', required=True)
args = parser.parse_args()
if not re.fullmatch(r'[a-z0-9-]+', args.name):
    parser.error('Use a new lowercase evidence name')
configuration_path = ROOT / 'src/Tests.Shared/appsettings.json'
configuration_text = configuration_path.read_text()
configuration = json.loads(re.sub(r'(?m)^[ \t]*//[^\r\n]*', '', configuration_text))
if configuration['YubiKeyTests']['AllowedSerialNumbers'] != [103]:
    parser.error('Coordinator must narrow the existing shared allowlist to exactly [103]')
destination = HERE / 'evidence' / args.name
destination.mkdir(parents=True, exist_ok=False)
fixtures = Path('/tmp/opencode/piv-001-hardware-fixtures') / args.name
fixtures.mkdir(parents=True, exist_ok=False)
environment = dict(os.environ, AUDITPIV001_EVIDENCE=str(destination), AUDITPIV001_FIXTURES=str(fixtures))
results = dict(case=args.case, utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
               build_exit=None, test_exit=None, cleanup_exit=None, expected_failure=args.case.startswith(('P05_', 'P06_')))
results['configuration_reader'] = dict(path=str(configuration_path),
    sha256=hashlib.sha256(configuration_path.read_bytes()).hexdigest(),
    transformation='Remove full-line // comments only; configuration file remains unchanged')
results['public_fixture_directory'] = str(fixtures)
baseline = RUN / 'hardware/baseline-103.json'
cleanup = RUN / 'hardware/vendor-cleanup-v2.py'


def command(label, arguments, timeout=None):
    print(label + ': ' + shlex.join(arguments), flush=True)
    with (destination / (label + '.log')).open('x') as output:
        output.write('$ ' + shlex.join(arguments) + '\n')
        output.flush()
        # All hardware-bearing children run under GNU timeout, which awaits TERM/KILL
        # termination. Cleanup starts only after the parent command has returned.
        complete = subprocess.run(arguments, cwd=ROOT, env=environment,
                                  stdout=output, stderr=subprocess.STDOUT, timeout=timeout)
        output.write(f'\nEXIT={complete.returncode}\n')
        return complete.returncode


def retain_toolchain(label):
    text = (destination / (label + '.log')).read_text()
    matches = re.findall(r'Run log: (artifacts/toolchain/runs/[^\s]+/run\.log)', text)
    if not matches:
        results[label + '_retention'] = 'No RESULT run path found; inspect retained raw log'
        return
    source = (ROOT / matches[-1]).parent.resolve()
    source.relative_to((ROOT / 'artifacts/toolchain/runs').resolve())
    target = destination / (label + '-toolchain')
    target.mkdir(exist_ok=False)
    for path in sorted(source.iterdir()):
        if path.is_file() and path.suffix in ('.log', '.json'):
            with (target / path.name).open('xb') as output:
                output.write(path.read_bytes())


def vendor(label):
    return command(label, ['timeout', '--signal=TERM', '--kill-after=5s', '240s',
        sys.executable, str(cleanup), '--serial', '103', '--authorize-reset-serial', '103',
        '--reader', 'Yubico YubiKey OTP+FIDO+CCID 00 00', '--baseline', str(baseline),
        '--receipt', str(destination / (label + '.json'))])


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def binary_identity():
    directory = Path('/tmp/opencode/piv-001-hardware/bin')
    paths = sorted(directory.rglob('*.dll'))
    if not any(p.name == 'AuditPiv001.Hardware.IntegrationTests.dll' for p in paths):
        raise RuntimeError('Compiled unique audit host DLL is absent')
    return {str(p): sha(p) for p in paths}


try:
    results['input_identity'] = {str(p.relative_to(ROOT)): sha(p) for p in
        [Path(__file__), HERE / 'capture-v3.py', HERE / 'vendor-x25519-reference.py',
         baseline, cleanup, RUN / 'hardware/vendor-state.py', ROOT / 'src/Tests.Shared/appsettings.json',
         ROOT / 'src/Piv/examples/PivTool/PivExamples/Signing.cs',
         ROOT / 'src/Piv/examples/PivTool/PivExamples/Results/SigningResult.cs']}
    results['build_exit'] = command('build', ['dotnet', 'toolchain.cs', '--', 'build',
        '--project', 'AuditPiv001.Hardware.IntegrationTests', '--clean'])
    retain_toolchain('build')
    if results['build_exit'] != 0:
        raise RuntimeError('Build failed; hardware execution remains unstarted')
    results['capture_exit'] = command('compiler-capture', [sys.executable, str(HERE / 'capture-v3.py'),
        str(destination / 'compiler')])
    if results['capture_exit'] != 0:
        raise RuntimeError('Compiler capture failed; retain original evidence before correction')
    results['compiled_binary_identities'] = binary_identity()
    openssl = shutil.which('openssl')
    if openssl:
        results['openssl_executable'] = dict(path=openssl, sha256=sha(Path(openssl)))
        results['openssl_version_exit'] = command('openssl-version', [openssl, 'version', '-a'], timeout=10)
    results['pre_cleanup_exit'] = vendor('vendor-before')
    if results['pre_cleanup_exit'] != 0:
        raise RuntimeError('Initial independent restoration failed; probe remains unstarted')
    results['test_exit'] = command('dotnet', ['timeout', '--signal=TERM', '--kill-after=10s', '300s',
        'dotnet', 'toolchain.cs', '--', 'test', '--integration',
        '--project', 'AuditPiv001.Hardware.IntegrationTests', '--filter', 'FullyQualifiedName~' + args.case])
    retain_toolchain('dotnet')
    counts = re.findall(r'tests:\s*(\d+) executed,\s*(\d+) failed,\s*(\d+) skipped',
                        (destination / 'dotnet.log').read_text())
    results['selection_verified'] = False
    if counts:
        executed, failed, skipped = map(int, counts[-1])
        results['test_counts'] = dict(executed=executed, failed=failed, skipped=skipped)
        results['selection_verified'] = executed == 1 and skipped == 0 and failed in (0, 1)
    event_path = destination / 'probe-events.jsonl'
    events = [json.loads(line) for line in event_path.read_text().splitlines()] if event_path.exists() else []
    results['host_environment_verified'] = any(e['stage'] == 'prepared' and e['detail'].get('serial') == 103
        for e in events) and {'AuditPiv001.Hardware.IntegrationTests', 'Yubico.YubiKit.Piv',
        'PivTool', 'Yubico.YubiKit.Tests.Shared'}.issubset({e['detail'].get('name') for e in events
        if e['stage'] == 'runtime-assembly'})
    if args.case.startswith('P01_') and results['test_exit'] not in (124, 137):
        results['reference_exit'] = command('vendor-x25519', ['timeout', '--signal=TERM', '--kill-after=5s', '30s',
            sys.executable, str(HERE / 'vendor-x25519-reference.py'), '--serial', '103',
            '--output', str(destination / 'vendor-x25519.json')])
    # Toolchain discovery/test may perform incremental builds. Bind execution back to
    # the retained actual compiler capture through unchanged compiled DLL identities.
    results['post_test_binary_identities'] = binary_identity()
    results['compiled_binaries_preserved'] = results['compiled_binary_identities'] == results['post_test_binary_identities']
except Exception as error:
    results['wrapper_error_type'] = type(error).__name__
finally:
    try:
        pending = (destination / 'recovery-pending.txt').exists()
        wedged = (destination / 'drain-blocked.txt').exists()
        exits = [results['test_exit'], results.get('reference_exit')]
        timed_out = any(code is not None and (code < 0 or code >= 124) for code in exits)
        if pending or wedged or timed_out:
            results['cleanup_disposition'] = 'held-for-coordinator-drain-and-same-key-recovery; preserve state before reset'
        elif results['test_exit'] is not None:
            results['cleanup_exit'] = vendor('vendor-after')
        else:
            results['cleanup_disposition'] = 'probe-unstarted; inspect prerequisite/build/pre-cleanup evidence'
    finally:
        with (destination / 'result.json').open('x') as output:
            json.dump(results, output, indent=2)
            output.write('\n')
print(json.dumps(results))
raise SystemExit(0 if results['test_exit'] == results['cleanup_exit'] == results.get('reference_exit', 0) == 0
                 and results.get('compiled_binaries_preserved') and results.get('selection_verified')
                 and results.get('host_environment_verified') and results.get('test_counts', {}).get('failed') == 0 else 1)
