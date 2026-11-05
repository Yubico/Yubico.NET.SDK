"""Coordinator-approved serial execution of P02–P06, preserving expected assertion failures."""
from pathlib import Path
import json
import subprocess
import sys

HERE = Path(__file__).resolve().parent
CASES = (
    ('P02_PinAlwaysConsumesVerification_PinOnceReusesIt', 'p02-pin-policy-v4', False),
    ('P03_MoveAndDeletePreserveIndependentCertificates', 'p03-key-cert-v4', False),
    ('P04_AttestationBindsGeneratedKeyToPreservedIssuer', 'p04-attestation-v4', False),
    ('P05_ActualPivToolRsaSignsOriginalMessage', 'p05-tool-rsa-v4', True),
    ('P06_ActualPivToolEd25519SignsOriginalMessage', 'p06-tool-ed25519-v4', True),
)
output = HERE / 'batch-wrapper-output'
output.mkdir(exist_ok=False)
for _, name, _ in CASES:
    assert not (HERE / 'evidence' / name).exists(), name
results = []
for case, name, expected_failure in CASES:
    with (output / (name + '.log')).open('x') as log:
        completed = subprocess.run([sys.executable, '-B', str(HERE / 'run-probe-v4.py'),
            case, '--name', name], stdout=log, stderr=subprocess.STDOUT)
    receipt = json.loads((HERE / 'evidence' / name / 'result.json').read_text())
    row = {key: receipt.get(key) for key in ('case', 'build_exit', 'capture_exit', 'test_exit',
        'cleanup_exit', 'test_counts', 'compiled_binaries_preserved', 'host_environment_verified')}
    row['wrapper_exit'] = completed.returncode
    results.append(row)
    print(json.dumps(row), flush=True)
    # A failed intended signing assertion remains failed. Continue only after verified
    # complete execution, retained compiler/runtime identities and independent cleanup.
    assert receipt.get('build_exit') == receipt.get('capture_exit') == receipt.get('pre_cleanup_exit') == receipt.get('cleanup_exit') == 0
    assert receipt.get('compiled_binaries_preserved') and receipt.get('host_environment_verified')
    assert receipt['test_counts']['executed'] == 1 and receipt['test_counts']['skipped'] == 0
    assert receipt['test_exit'] in (0, 1) if expected_failure else receipt['test_exit'] == 0
with (output / 'summary.json').open('x') as log:
    json.dump(results, log, indent=2)
    log.write('\n')
raise SystemExit(1 if any(row['test_exit'] for row in results) else 0)
