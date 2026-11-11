"""Short-scope FR-1 acceptance and in-memory fail-closed boundaries; no snapshot."""
import ast
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
from unittest.mock import patch

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
VALIDATION = HERE.parent


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


class View:
    """A read-only path/content shadow: every mutation exists in this process only."""
    def __init__(self, path, raw, *, symlink=False, resolved=None, missing=False, reads=None):
        self.path, self.raw = path, raw
        self.symlink, self.resolved, self.missing = symlink, resolved, missing
        self.reads = list(reads) if reads is not None else None

    def is_symlink(self):
        return self.symlink

    def resolve(self):
        return self.resolved or self.path.resolve()

    def relative_to(self, base):
        return self.path.relative_to(base)

    def read_bytes(self):
        if self.missing:
            raise FileNotFoundError(str(self.path))
        if self.reads is not None:
            return self.reads.pop(0)
        return self.raw

    @property
    def suffixes(self):
        return self.path.suffixes

    @property
    def name(self):
        return self.path.name

    def __str__(self):
        return str(self.path)


def main():
    adapter = VALIDATION / 'check-records-v2.py'
    ast.parse(adapter.read_bytes(), filename=str(adapter))
    spec = importlib.util.spec_from_file_location('fr1_checker', adapter)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    scope = module.load()
    admin = Path(subprocess.check_output(['git', 'rev-parse', '--git-dir'], cwd=module.ROOT,
                 env=dict(os.environ, GIT_OPTIONAL_LOCKS='0')).decode().strip())
    index_before = sha((admin / 'index').read_bytes())
    rows = scope['classification_proof']['exceptions']
    accepted = []
    for row in rows:
        path = module.PACKET / row['path']
        result = scope['record'](path)
        assert result['bytes'] == row['bytes'] and result['raw_sha256'] == row['sha256']
        assert result['raw_preserved_in_git'] is True
        accepted.append(dict(**row, staged_oid=result['staged_oid'], attributes=result['attributes']))
    scope['approved_population']()
    rejected = []

    def reject(label, operation):
        try:
            operation()
        except (ValueError, FileNotFoundError) as error:
            rejected.append(dict(case=label, error_type=type(error).__name__, message=str(error)))
        else:
            raise AssertionError(f'Boundary unexpectedly accepted: {label}')

    for row in rows:
        path = module.PACKET / row['path']
        raw = path.read_bytes()
        changed = (b'X' if raw[:1] != b'X' else b'Y') + raw[1:]
        reject('changed-hash:' + row['path'], lambda p=path, b=changed: scope['record'](View(p, b)))
        reject('changed-size:' + row['path'], lambda p=path, b=raw: scope['record'](View(p, b + b' ')))
        reject('missing:' + row['path'], lambda p=path: scope['record'](View(p, b'', missing=True)))
        reject('unlisted-same-kind-and-bytes:' + row['path'],
               lambda p=path, b=raw: scope['record'](View(p.with_name('unlisted-' + p.name), b)))
        reject('symlink:' + row['path'], lambda p=path, b=raw: scope['record'](View(p, b, symlink=True)))
        reject('escaping-root:' + row['path'],
               lambda p=path, b=raw: scope['record'](View(p, b, resolved=Path('/outside-packet'))))
        reject('change-between-classification-and-record:' + row['path'],
               lambda p=path, a=raw, b=changed: scope['record'](View(p, a, reads=[a, b])))
    normal = module.PACKET / 'validation/ordinary-unlisted-text.txt'
    for label, raw in [('NUL', b'text\0payload'), ('MZ', b'MZpayload'), ('ELF', b'\x7fELFpayload'), ('invalid-UTF8', b'\xff')]:
        reject('unlisted-content-guard:' + label, lambda b=raw: scope['record'](View(normal, b)))
    for name in ('new.nupkg', 'new.zip', 'new.dll', 'new.nupkg.metadata.snapshot',
                 'obj/unlisted.json', 'bin/unlisted.json', '__pycache__/unlisted.json', 'node_modules/unlisted.json'):
        reject('unlisted-coarse-rule:' + name,
               lambda n=name: scope['record'](View(module.PACKET / 'validation' / n, b'{}')))
    saved_read = Path.read_bytes
    missing = module.PACKET / rows[0]['path']

    def missing_approved(path):
        if path == missing:
            raise FileNotFoundError(str(path))
        return saved_read(path)

    with patch.object(Path, 'read_bytes', missing_approved):
        reject('mandatory-population-missing-even-before-enumeration', scope['approved_population'])
    for label, target in [('receipt-pin', module.RECEIPT), ('original-source-pin', module.ORIGINAL),
                          ('review-pin', module.PACKET / 'final-review.md')]:
        def altered_pin(path, expected=target):
            raw = saved_read(path)
            return raw + b' ' if path == expected else raw
        with patch.object(Path, 'read_bytes', altered_pin):
            reject(label, module.load)
    assert sha((admin / 'index').read_bytes()) == index_before
    assert sha(module.ORIGINAL.read_bytes()) == module.ORIGINAL_SHA
    for row in rows:
        raw = (module.PACKET / row['path']).read_bytes()
        assert len(raw) == row['bytes'] and sha(raw) == row['sha256']
    print(json.dumps(dict(status='FR-1-short-scope-verified', adapter_sha256=sha(adapter.read_bytes()),
        verifier_sha256=sha(Path(__file__).read_bytes()), proof=scope['classification_proof'], accepted=accepted,
        rejected=rejected, index_sha256_before_and_after=index_before,
        original_checker_and_seven_inputs_unchanged=True,
        scope='Seven actual read-only record() checks and process-local path/content shadows only. No full packet snapshot, index mutation, SDK build/test/capture/native/device gate.'), indent=2))


if __name__ == '__main__':
    main()
