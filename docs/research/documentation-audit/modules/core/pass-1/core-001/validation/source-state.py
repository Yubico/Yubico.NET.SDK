"""Hash complete current source/docs, including HTML, against the fixed Core source baseline."""
import argparse
import json
from pathlib import Path
from foundation import HERE, ROOT, RUN, BASELINE, baseline, git, require, sha


def selected(path):
    # Frozen evidence has its separate preservation gate; audit tooling has its own version pins.
    users = {row['path'] for row in json.loads((RUN / 'user-work-baseline.json').read_bytes())}
    return path not in users and not path.startswith(('docs/research/', 'docs/archive/', 'docs/completed/', '.claude/'))


def capture():
    baseline()
    listing = git('ls-tree', '-r', '-z', BASELINE).split(b'\0')
    rows = []
    for entry in listing:
        if not entry:
            continue
        metadata, raw_path = entry.split(b'\t', 1)
        path = raw_path.decode()
        if not selected(path):
            continue
        mode, kind, oid = metadata.decode().split()
        require(kind == 'blob' and mode != '120000', f'Review source symlink/submodule: {path}')
        before = git('show', BASELINE + ':' + path)
        current = ROOT / path
        require(current.is_file() and not current.is_symlink(), f'Source path removed/redirected: {path}')
        after = current.read_bytes()
        clean_oid = git('hash-object', '--path=' + path, '--stdin', raw=after).decode().strip()
        rows.append(dict(path=path, baseline_oid=oid, baseline_sha256=sha(before), current_sha256=sha(after),
                         current_git_clean_oid=clean_oid, baseline_bytes=len(before), current_bytes=len(after),
                         changed=clean_oid != oid, raw_changed=before != after,
                         newline_only=before != after and before.replace(b'\r\n', b'\n') == after.replace(b'\r\n', b'\n')))
    current_paths = set(git('ls-files', '--cached', '--others', '--exclude-standard', '-z').decode().split('\0')) - {''}
    additions = sorted(path for path in current_paths if selected(path) and path not in {row['path'] for row in rows})
    require(not additions, f'Review new source/document membership before checkpoint: {additions}')
    return dict(source_baseline=BASELINE, metadata_head=git('rev-parse', 'HEAD').decode().strip(), files=rows,
                scope_sha256=sha(json.dumps(rows, sort_keys=True, separators=(',', ':')).encode()),
                changed_paths=[row['path'] for row in rows if row['changed']],
                raw_changed_paths=[row['path'] for row in rows if row['raw_changed']],
                newline_only_paths=[row['path'] for row in rows if row['newline_only']])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    require(output.parent == HERE and not output.exists(), 'Choose a fresh source-state file directly under validation/')
    report = capture()
    with output.open('x') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print(f"Full source/docs: {len(report['files'])} paths, {len(report['changed_paths'])} changed; {output}")
