"""Preserve SDK failure, compare the same key with vendor Python, then clean up independently."""
from pathlib import Path
import json
import subprocess
import sys

HERE = Path(__file__).resolve().parent
RUN = HERE.parent.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
BASELINE = RUN / 'hardware/vendor-baseline-v2.json'
DEST = HERE / 'evidence/v3-x25519-same-key'
DEST.mkdir(parents=True, exist_ok=False)
results = {}


def command(name, arguments):
    # The shared runner retains the full toolchain run immediately, including failures.
    completed = subprocess.run([sys.executable, str(RUN / 'validation/run-command.py'), name,
                                *map(str, arguments)], cwd=ROOT)
    results[name] = completed.returncode


try:
    command('x25519-sdk', ['dotnet', 'toolchain.cs', '--', 'test', '--integration', '--project',
        'AuditPgp001.Hardware.IntegrationTests', '--filter',
        'FullyQualifiedName~X25519Decipher_Rfc7748AliceBob_MatchesSharedSecret'])
finally:
    try:
        command('x25519-vendor', [sys.executable, '-B', HERE / 'vendor-reference-v2.py', 'x25519',
            '--baseline', BASELINE, '--output', DEST / 'vendor-reference.json'])
    finally:
        try:
            command('x25519-cleanup', [sys.executable, '-B', RUN / 'hardware/vendor-state.py', 'cleanup',
                '--authorize-reset-serial-103', '--baseline', BASELINE, '--output', DEST / 'cleanup.json'])
        finally:
            with (DEST / 'result.json').open('x') as output:
                json.dump(results, output, indent=2)
                output.write('\n')
print(json.dumps(results))
raise SystemExit(0 if all(value == 0 for value in results.values()) else 1)
