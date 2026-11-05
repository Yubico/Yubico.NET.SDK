"""Read-only run text/link checks; optionally verify final reviewed hashes.

External local link targets are checked for existence, never opened. This is a
bounded Markdown link check, not a CommonMark parser or a content-review verdict.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
from urllib.parse import unquote

HERE = Path(__file__).resolve().parent
RUN = HERE.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--reviewed', action='store_true', help='Require RUN/reviewed-files.json and verify final_sha256 for each path')
parser.add_argument('--markdown', action='append', default=[], help='Additional repo-relative record/handoff Markdown to check; not module content')
args = parser.parse_args()

def require(condition, message):
    if not condition:
        raise SystemExit(message)

def canonical(relative):
    path = Path(relative)
    require(not path.is_absolute() and '..' not in path.parts, f'Unsafe repository-relative path: {relative}')
    current = ROOT
    for part in path.parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked path: {current}')
    return current

hashes = 0
if args.reviewed:
    rows = json.loads((RUN / 'reviewed-files.json').read_text())['files']
    require(bool(rows) and len({r['path'] for r in rows}) == len(rows), 'Require nonempty distinct reviewed files')
    for row in rows:
        require(hashlib.sha256(canonical(row['path']).read_bytes()).hexdigest() == row['final_sha256'], f"Reviewed hash mismatch: {row['path']}")
        hashes += 1
files = sorted(RUN.glob('*.md'))
for relative in args.markdown:
    require(relative.startswith('docs/research/documentation-audit/') and relative.endswith('.md'), 'Additional Markdown must be a campaign record/handoff')
    files.append(canonical(relative))
links = 0
for path in dict.fromkeys(files):
    fence = None
    for line in path.read_text().splitlines():
        match = re.match(r'^\s*(`{3,}|~{3,})(.*)$', line)
        if match:
            marker, tail = match.groups()
            if fence is None:
                fence = marker
            elif marker[0] == fence[0] and len(marker) >= len(fence) and not tail.strip():
                fence = None
            continue
        if fence is not None:
            continue
        for target in re.findall(r'\]\(([^)]+)\)', line):
            target = target.strip()
            if re.match(r'^[A-Za-z][A-Za-z0-9+.-]*:', target) or target.startswith(('#', '//')):
                continue
            # Optional titles and nested parentheses need manual review, not a false pass.
            require(not re.search(r'\s', target) or (target.startswith('<') and target.endswith('>')), f'Unsupported link syntax: {path}: {target}')
            target = unquote(target.strip('<>').split('#', 1)[0])
            require(bool(target) and (path.parent / target).exists(), f'Broken local link: {path}: {target}')
            links += 1
    require(fence is None, f'Unclosed fenced block: {path}')
count = 0
primary = {r['path']: r for r in json.loads((HERE / 'primary-binary-sources.json').read_text())}
seen = set()
for path in RUN.rglob('*'):
    require(not path.is_symlink(), f'Symlink in run: {path}')
    if path.is_file():
        raw = path.read_bytes()
        relative = str(path.relative_to(RUN))
        if relative in primary:
            expected = primary[relative]
            require(raw.startswith(b'%PDF-') and len(raw) == expected['bytes'] and hashlib.sha256(raw).hexdigest() == expected['sha256'], f'Primary source changed: {path}')
            seen.add(relative)
        else:
            raw.decode('utf-8')
            require(b'\0' not in raw, f'NUL-containing retained artifact: {path}')
        require(not any(p.lower() in ('bin', 'obj', '__pycache__', '.cache') for p in path.relative_to(RUN).parts), f'Cache directory retained: {path}')
        require(path.suffix.lower() not in ('.dll', '.exe', '.pdb', '.binlog', '.nupkg', '.snupkg', '.pyc', '.cache'), f'Binary/cache artifact retained: {path}')
        count += 1
require(seen == set(primary), 'Registered primary sources missing')
print(f'PASS: {len(set(files))} record Markdown files / {links} local links, balanced fences; {count-len(seen)} UTF-8 artifacts and {len(seen)} pinned primary-source PDFs; {hashes} reviewed hashes checked.')
if not args.reviewed:
    print('PENDING: final reviewed-files hash check; use --reviewed after review. Scope: record text and local-target existence.')
