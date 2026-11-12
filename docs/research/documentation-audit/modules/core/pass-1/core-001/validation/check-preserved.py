"""Verify Core's current baseline receipts, frozen archives and original primary user files."""
import argparse
import json
from pathlib import Path
from foundation import ROOT, RUN, BASELINE, baseline, frozen, git, pinned, require, sha


def safe(root, relative):
    path = root / relative
    require(not Path(relative).is_absolute() and '..' not in Path(relative).parts, f'Unsafe path: {relative}')
    require(path.resolve().is_relative_to(root.resolve()) and not path.is_symlink(), f'Redirected input: {path}')
    return path


def verify_bytes(root, row, digest='sha256'):
    path = safe(root, row['path'])
    raw = path.read_bytes()
    require(sha(raw) == row[digest] and ('bytes' not in row or len(raw) == row['bytes']), f'Protected bytes changed: {path}')
    return path, raw


def clean_oid(relative, raw):
    return git('hash-object', '--path=' + relative, '--stdin', raw=raw).decode().strip()


def verify():
    data = baseline()
    git('merge-base', '--is-ancestor', BASELINE, 'HEAD')
    # Source remains pinned; a later metadata/phase commit is a separate observation.
    for ancestor in ('f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa', '5746e72ed9a848d08b5782ddde025aa4a4b522ef'):
        git('merge-base', '--is-ancestor', ancestor, BASELINE)
    artifacts = 0
    nontext = 0
    protected = set()
    baseline_objects = {}
    for entry in git('ls-tree', '-r', '-z', BASELINE).split(b'\0'):
        if entry:
            metadata, path = entry.split(b'\t', 1)
            baseline_objects[path.decode()] = metadata.decode().split()[2]
    for archive in data['frozen_manifests']:
        path, raw = verify_bytes(ROOT, archive)
        rows = json.loads(raw)['files']
        require(len(rows) == archive['artifacts'] and len({row['path'] for row in rows}) == len(rows), 'Archive membership changed')
        base = path.parent / 'records' if path == ROOT / 'docs/research/documentation-audit/record-manifest.json' else path.parent
        for row in rows:
            artifact, content = verify_bytes(base, row)
            relative = artifact.relative_to(ROOT).as_posix()
            require(clean_oid(relative, content) == baseline_objects.get(relative), f'Frozen actual Git-clean identity changed: {relative}')
            protected.add(relative)
            try:
                content.decode('utf-8-sig')
                nontext += int(b'\0' in content)
            except UnicodeError:
                nontext += 1
        protected.add(path.relative_to(ROOT).as_posix())
        artifacts += len(rows)
    require(artifacts == data['frozen_artifact_count'], 'Frozen artifact total changed')
    integration = data['integration_manifest']
    manifest_path, manifest_raw = verify_bytes(ROOT, {key: value for key, value in integration.items() if key != 'bytes'})
    rows = json.loads(manifest_raw)['files']
    require(len(rows) == integration['records'] and len({row['path'] for row in rows}) == len(rows), 'Integration membership changed')
    total = 0
    for row in rows:
        path, raw = verify_bytes(ROOT, row, 'raw_sha256')
        relative = path.relative_to(ROOT).as_posix()
        oid = clean_oid(relative, raw)
        require(oid == row['staged_oid'], f'Actual Git-clean identity changed: {relative}')
        # Object identity proves actual clean conversion without assuming LF policy for PDFs.
        clean = git('cat-file', 'blob', oid)
        require(sha(clean) == row['staged_sha256'], f'Git-clean byte identity changed: {relative}')
        protected.add(relative)
        total += len(raw)
    require(total == integration['bytes'], 'Integration payload byte total changed')
    protected.add(manifest_path.relative_to(ROOT).as_posix())
    prior = data['prior_piv_integration']['files']
    require(len({row['path'] for row in prior}) == len(prior), 'Duplicate PIV record')
    for row in prior:
        path, _ = verify_bytes(ROOT, row)
        protected.add(path.relative_to(ROOT).as_posix())
    users = json.loads(pinned(RUN / 'user-work-baseline.json', '8515cd743225e2c74b1249d0b7dc41226575f8a4d006835bafab3c25b0c95dc8'))
    for row in users:
        verify_bytes(ROOT, row)
    # Check actual expected-clean state against both index and HEAD. Other worktrees retain their owners.
    dirty = set(git('diff', '--name-only', '-z', 'HEAD', '--').decode().split('\0')) - {''}
    require(not protected.intersection(dirty), 'Frozen tracked records have index/worktree changes')
    unmerged = git('ls-files', '--unmerged')
    require(not unmerged, 'Resolve the unmerged index before claiming preservation')
    indexed = {}
    for entry in git('ls-files', '--stage', '-z').split(b'\0'):
        if entry:
            metadata, path = entry.split(b'\t', 1)
            indexed[path.decode()] = metadata.decode().split()[1]
    require(all(indexed.get(path) == baseline_objects.get(path) and path in baseline_objects for path in protected),
            'Protected index membership/object differs from the original Core baseline')
    return dict(source_baseline=BASELINE, metadata_head=git('rev-parse', 'HEAD').decode().strip(),
                primary=str(ROOT), frozen_manifests=len(data['frozen_manifests']), frozen_artifacts=artifacts,
                frozen_nontext_artifacts=nontext, integration_records=len(rows), integration_bytes=total,
                prior_piv_files=len(prior), original_user_files=len(users), protected_git_clean=True,
                other_worktrees='outside this phase; no unchanged-HEAD requirement')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--historical-binary-manifest', type=Path)
    args = parser.parse_args()
    report = verify()
    if args.historical_binary_manifest:
        report['historical_binaries'] = frozen('check-preserved.py').historical_binaries(args.historical_binary_manifest)
    print(json.dumps(report, indent=2))
