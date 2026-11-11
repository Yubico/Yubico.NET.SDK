"""Bounded routing adapter for the existing successful fixture build / failed capture. No SDK execution."""
import hashlib
from pathlib import Path
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
source_path = HERE.parent / 'correction-v4/preserve.py'
raw = source_path.read_bytes()
if hashlib.sha256(raw).hexdigest() != '2cd258fcedb91a00f81389dd5a7232af29b0ac282ed2cc563f86c02eabb1aaf7':
    raise ValueError('Pinned capture-failure preservation source changed')
source = raw.decode()
for before, after in (
    ("HERE / 'consumer-preservation'", "HERE / 'fixture-preservation'"),
    ("'preserved-consumers-first-v4'", "'preserved-fixture-first-v5'"),
    ('consumer-consumers-build-first', 'consumer-fixture-build-first'),
    ('consumer-consumers-capture-first', 'consumer-fixture-capture-first'),
    ('evidence/consumers-first-v3', 'evidence/fixture-first-v4'),
    ('attempts/consumers-first/raw', 'attempts/fixture-first/raw'),
    ("compiler_attempt='consumers-first'", "compiler_attempt='fixture-first'"),
):
    if source.count(before) != 1:
        raise ValueError('Exact fixture preservation routing site changed: ' + before)
    source = source.replace(before, after)
exec(compile(source, str(source_path) + '#fixture-routing-only', 'exec'),
     dict(__name__='__main__', __file__=str(Path(__file__))))
