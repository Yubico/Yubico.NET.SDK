"""Redirect five explicit incoming test-temporary directories to the home runtime.

Source-pinned in-memory adaptation only; every original test body/assertion remains.
--check-source inspects syntax/provenance without running the evidence-gate tests.
"""
import ast
import hashlib
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]
SOURCE = ROOT / 'verification/LinuxHidRouteVerification/test_evidence_gates.py'
SOURCE_SHA = '1ef6aaa3c5e8fd60e9f939139146e43ca585c3e2713ab6ce6efe498ed34d003d'
HOME_TMP = '/home/rupicola/Code/y/.core-docs-audit-runtime/tmp'
raw = SOURCE.read_bytes()
if hashlib.sha256(raw).hexdigest() != SOURCE_SHA:
    raise SystemExit('Re-adjudicate changed incoming evidence-gate source')
old = 'dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None'
text = raw.decode()
if text.count(old) != 5 or not Path(HOME_TMP).is_dir():
    raise SystemExit('Require five exact temporary-directory sites and the dedicated home directory')
adapted = text.replace(old, 'dir=HOME_TMP')
ast.parse(adapted, filename=str(SOURCE))
proof = dict(source=str(SOURCE), source_sha256=SOURCE_SHA, changes=5,
             old_expression=old, new_expression='dir=HOME_TMP', home_tmp=HOME_TMP,
             adapted_sha256=hashlib.sha256(adapted.encode()).hexdigest(),
             scope='Explicit temporary-directory routing only; original source and all test bodies/assertions preserved')
print(json.dumps(proof), flush=True)
if sys.argv[1:] == ['--check-source']:
    raise SystemExit(0)
if len(sys.argv) != 2:
    raise SystemExit('Usage: linux-evidence-gates-home.py --check-source | CAPTURE_DIRECTORY')
sys.path.insert(0, str(SOURCE.parent))
sys.argv = [str(SOURCE), sys.argv[1]]
# unittest.main discovers this actual __main__ namespace, including the original classes.
globals()['__file__'] = str(SOURCE)
exec(compile(adapted, str(SOURCE) + '[home-temporary-directory-adapter]', 'exec'), globals())
