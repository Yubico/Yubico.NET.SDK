"""Bind the original immutable preservation algorithm to the verified new root."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]
ORIGINAL = HERE / 'check-preserved.py'
ORIGINAL_SHA = '0581b55098c6c9f3276c9e0766a0bef88a4a3a4380ee7282e2a136f4eac82dfc'
RECEIPT = HERE.parent / 'recovery/relocation-receipt.json'
RECEIPT_SHA = '0da550fd99ca719dc1de1c91ee32bf5569e824cc27905d9f606830fea74e791c'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def verify():
    source = ORIGINAL.read_bytes()
    receipt_raw = RECEIPT.read_bytes()
    if sha(source) != ORIGINAL_SHA or sha(receipt_raw) != RECEIPT_SHA:
        raise ValueError('Original algorithm/relocation receipt identity changed')
    receipt = json.loads(receipt_raw)
    if ROOT != Path(receipt['new_root']) or receipt['old_root'] != '/tmp/opencode/core-upstream-20261111':
        raise ValueError('Run the authoritative relocated checker in the exact new checkout')
    admin = Path(receipt['private_admin'])
    if (admin / 'gitdir').read_bytes() != (str(ROOT / '.git') + '\n').encode():
        raise ValueError('Worktree registration does not point at the relocated checkout')
    old = '(("integration", data["integration_checkout"]), ("primary", data["primary"]))'
    new = '(("integration", RELOCATED_ROOT), ("primary", data["primary"]))'
    text = source.decode()
    if text.count(old) != 1:
        raise ValueError('Reconcile the source-pinned single root-routing transformation')
    adapted = text.replace(old, new)
    scope = dict(__file__=str(ORIGINAL), __name__='preserved_relocated_algorithm', RELOCATED_ROOT=str(ROOT))
    exec(compile(adapted, str(ORIGINAL) + '[verified-relocation]', 'exec'), scope)
    report = scope['verify']()
    if report['checkouts'][0]['root'] != str(ROOT):
        raise ValueError('Checker inspected the inactive old tree')
    report['relocation'] = dict(receipt_sha256=RECEIPT_SHA, original_algorithm_sha256=ORIGINAL_SHA,
        adapted_algorithm_sha256=sha(adapted.encode()), old_expression=old, new_expression=new,
        old_root=receipt['old_root'], checked_integration_root=str(ROOT), original_before_unchanged=True)
    return report, scope


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--historical-binary-manifest', type=Path)
    args = parser.parse_args()
    report, scope = verify()
    if args.historical_binary_manifest:
        report['historical_binaries'] = scope['historical_binaries'](args.historical_binary_manifest)
    print(json.dumps(report, indent=2))
