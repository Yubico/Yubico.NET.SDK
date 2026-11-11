"""FR-1: admit seven exact retained text identities through coarse path rules only."""
import argparse
import base64
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
PACKET = HERE.parent
ROOT = HERE.parents[5]
ORIGINAL = HERE / 'check-records.py'
RECEIPT = HERE / 'record-classification-correction/approved-text.json'
ORIGINAL_SHA = 'e287c3e35d6fec35d125c870125201816dc4d9822ae78ba94d7c0da167eeb3e4'
RECEIPT_SHA = 'eeb2b9a302f57d57569e77d9fd7a844735cc7867edbcc07d97d908cdc9c21d36'
REVIEW_SHA = '7e53e938c3d396bba6ea8173e89696398408e3696d57a991c602fd23bb83b71f'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def require(value, message):
    if not value:
        raise ValueError(message)


def load():
    require(ROOT == Path('/home/rupicola/Code/y/yubikit-core-docs-integration'), 'Use the active home checkout')
    require(not ORIGINAL.is_symlink() and not RECEIPT.is_symlink(), 'Symlinked checker/receipt')
    source = ORIGINAL.read_bytes()
    receipt_raw = RECEIPT.read_bytes()
    require(sha(source) == ORIGINAL_SHA and sha(receipt_raw) == RECEIPT_SHA, 'Checker/exception receipt identity changed')
    receipt = json.loads(receipt_raw)
    review = PACKET / receipt['review']['path']
    require(not review.is_symlink() and sha(review.read_bytes()) == receipt['review']['sha256'] == REVIEW_SHA,
            'Pinned FR-1 review identity changed')
    require(receipt['original_checker'] == dict(path='validation/check-records.py', sha256=ORIGINAL_SHA),
            'Receipt refers to another checker')
    rows = receipt['exceptions']
    approved = {row['path']: row for row in rows}
    require(len(rows) == len(approved) == 7, 'Require seven unique reviewed text paths')

    def reviewed_text(path):
        require(not path.is_symlink() and path.resolve().is_relative_to(PACKET), 'Escaping/symlinked exception input')
        relative = path.relative_to(PACKET).as_posix()
        row = approved.get(relative)
        if row is None:
            return False
        raw = path.read_bytes()  # A missing approved input is a failure, never an omitted exception.
        require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], f'Reviewed text identity changed: {relative}')
        text = raw.decode('utf-8-sig')
        require(b'\0' not in raw and not raw.startswith((b'MZ', b'\x7fELF')), f'Binary exception content: {relative}')
        if row['text_kind'] == 'utf8-json-object':
            require(isinstance(json.loads(text), dict), f'Expected retained JSON object: {relative}')
        elif row['text_kind'] == 'utf8-base64-sha512':
            require(len(base64.b64decode(text, validate=True)) == 64, f'Expected SHA-512 text: {relative}')
        else:
            raise ValueError(f'Unreviewed text kind: {relative}')
        return True

    old_path = '    require(not forbidden.intersection(path.relative_to(PACKET).parts), f"Generated directory: {path}")\n'
    new_path = '    require(not forbidden.intersection(path.relative_to(PACKET).parts) or reviewed_text(path), f"Generated directory: {path}")\n'
    old_suffix = '''    require(not any(suffix.lower() in {".dll", ".exe", ".pdb", ".so", ".a", ".o", ".dylib", ".pyc", ".nupkg", ".zip"}
                    for suffix in path.suffixes), f"Binary/cache artifact: {path}")
'''
    new_suffix = old_suffix.replace('for suffix in path.suffixes),', 'for suffix in path.suffixes) or reviewed_text(path),')
    text = source.decode()
    require(text.count(old_path) == text.count(old_suffix) == 1, 'Reconcile the two exact classification sites')
    adapted = text.replace(old_path, new_path).replace(old_suffix, new_suffix)
    scope = dict(__file__=str(ORIGINAL), __name__='record_checker_fr1_bound', reviewed_text=reviewed_text)
    exec(compile(adapted, str(ORIGINAL) + '[FR-1-seven-text-identities]', 'exec'), scope)
    require(scope['ROOT'] == ROOT and scope['PACKET'] == PACKET and scope['HERE'] == HERE,
            'Original checker context is not the active integration root')
    original_record = scope['record']

    def record(path):
        result = original_record(path)
        row = approved.get(path.relative_to(PACKET).as_posix())
        if row is not None:
            # Bind the actual original record() read too, closing a change-between-reads gap.
            require(result['bytes'] == row['bytes'] and result['raw_sha256'] == row['sha256'],
                    'Reviewed text changed between classification and record capture')
        return result

    scope['record'] = record
    scope['approved_population'] = lambda: [require(reviewed_text(PACKET / row['path']), row['path']) for row in rows]
    scope['classification_proof'] = dict(original_sha256=ORIGINAL_SHA, receipt_sha256=RECEIPT_SHA,
        review_sha256=REVIEW_SHA, adapted_source_sha256=sha(adapted.encode()),
        transformations=[dict(old=old_path, new=new_path), dict(old=old_suffix, new=new_suffix)],
        root=str(ROOT), original_file_context=str(ORIGINAL), exceptions=rows)
    return scope


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('snapshot', 'check', 'staged'))
    parser.add_argument('--manifest', type=Path, default=HERE / 'final-record-manifest.json')
    args = parser.parse_args()
    scope = load()
    scope['approved_population']()  # All seven remain mandatory even if a file disappears from enumeration.
    scope['execute'](args.mode, args.manifest)
