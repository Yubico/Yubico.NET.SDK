"""Reuse the frozen latest HSM actual-Csc reconciliation, with an explicit workspace adaptation."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent
baseline = next(row for row in json.loads((RUN / 'prior-archive-baseline.json').read_text())
                if '/yubihsm/' in row['manifest'])
manifest = ROOT / baseline['manifest']
raw = manifest.read_bytes()
if hashlib.sha256(raw).hexdigest() != baseline['manifest_sha256']:
    raise SystemExit('Frozen HSM manifest identity changed')
source = manifest.parent / 'validation/hardware-probe/retain-inputs.py'
row = next(row for row in json.loads(raw)['files']
           if row['path'] == 'validation/hardware-probe/retain-inputs.py')
original = source.read_bytes()
if len(original) != row['bytes'] or hashlib.sha256(original).hexdigest() != row['sha256']:
    raise SystemExit('Frozen HSM actual-Csc reconciliation source changed')
text = original.decode('utf-8')
before = '/tmp/opencode/yubihsm-001-hardware'
if text.count(before) != 1:
    raise SystemExit('Unexpected HSM workspace adaptation count')
adapted = text.replace(before, '/tmp/opencode/openpgp-001-hardware')
# The source uses __file__ to locate its evidence and project. Its verified logic
# runs in this probe directory; sys.argv retains the two explicit evidence names.
exec(compile(adapted, str(source), 'exec'), {'__file__': str(HERE / 'retain-inputs.py'), '__name__': '__main__'})
