"""Retain one serialized hardware command and its exact result without overwriting prior evidence."""
from pathlib import Path
import datetime
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
name, *command = sys.argv[1:]
assert name.replace('-', '').isalnum() and command
log = HERE / (name + '.log')
with log.open('x') as output:
    output.write('$ ' + ' '.join(command) + '\n')
    output.write('UTC ' + datetime.datetime.now(datetime.timezone.utc).isoformat() + '\n')
    output.flush()
    result = subprocess.run(command, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT, timeout=120)
    output.write(f'\nEXIT={result.returncode}\n')
print(log.read_text())
raise SystemExit(result.returncode)
