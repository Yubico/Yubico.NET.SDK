"""Run one bounded coordinator hardware command and retain its exact result exclusively."""
from pathlib import Path
import datetime
import shlex
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
name, *command = sys.argv[1:]
assert name.replace('-', '').isalnum() and command
with (HERE / (name + '.log')).open('x') as out:
    out.write('$ ' + shlex.join(command) + '\nUTC ' + datetime.datetime.now(datetime.timezone.utc).isoformat() + '\n')
    out.flush()
    result = subprocess.run(command, cwd=ROOT, stdout=out, stderr=subprocess.STDOUT, timeout=180)
    out.write(f'\nEXIT={result.returncode}\n')
print((HERE / (name + '.log')).read_text())
raise SystemExit(result.returncode)
