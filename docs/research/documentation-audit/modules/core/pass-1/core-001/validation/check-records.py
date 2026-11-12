"""Core text/registered-original records, actual Git-clean policy and exact index bytes."""
import argparse
import json
import re
from pathlib import Path
from urllib.parse import urlsplit
from foundation import HERE, ROOT, RUN, PINS, INTEGRATION, pinned, require, sha


def safe_source(relative):
    parts = Path(relative).parts
    require(not Path(relative).is_absolute() and '..' not in parts and parts, 'Unsafe primary-source path')
    require(not {'bin', 'obj', 'node_modules', '__pycache__', '.git'}.intersection(parts), 'Primary source in generated directory')
    path = RUN / relative
    require(path.resolve().is_relative_to(RUN) and not any((RUN / Path(*parts[:i])).is_symlink()
            for i in range(1, len(parts) + 1)), 'Symlinked/escaping primary source')
    return path


def source_kind(relative, raw, registered):
    row = registered.get(relative)
    if row is None:
        return None
    url = urlsplit(row['url'])
    require(url.scheme == 'https' and url.netloc and not url.username and not url.password,
            'Require an explicit original-source HTTPS URL registration')
    require(type(row['bytes']) is int and len(raw) == row['bytes'] and sha(raw) == row['sha256'], 'Registered source identity changed')
    if row['kind'] == 'original-specification-pdf':
        require(Path(relative).suffix.lower() == '.pdf' and raw.startswith(b'%PDF-'), 'Registered source has no PDF suffix/magic')
    elif row['kind'] == 'original-source-html':
        require(Path(relative).suffix.lower() in ('.html', '.htm') and row.get('encoding') == 'iso-8859-1',
                'Registered HTML requires the explicit supported original encoding')
        declaration = re.search(rb'<meta\s+[^>]*http-equiv\s*=\s*["\']content-type["\'][^>]*content\s*=\s*["\']text/html;\s*charset=iso-8859-1["\'][^>]*>', raw[:8192], re.IGNORECASE)
        require(declaration and declaration.group(0).decode('ascii') in row.get('encoding_evidence', '') and
                b'<html' in raw[:8192].lower() and b'\0' not in raw and not raw.startswith((b'MZ', b'\x7fELF', b'PK\x03\x04')),
                'Original HTML encoding declaration/format evidence differs')
        raw.decode('iso-8859-1')  # Validate the declared interpretation; original bytes remain the only payload.
    else:
        raise ValueError('Unreviewed primary source kind')
    return row['kind']


def primary_sources():
    registry = safe_source('sources/primary-binaries.json')
    rows = json.loads(registry.read_bytes())['files']
    require(rows and len({row['path'] for row in rows}) == len(rows), 'Empty/duplicate primary source registry')
    result = {}
    for row in rows:
        path = safe_source(row['path'])
        authority = safe_source(row['authority'])
        manifest = json.loads(authority.read_bytes())
        population = manifest if isinstance(manifest, list) else manifest['files']
        matches = [item for item in population if item.get('path', item.get('file')) and
                   (authority.parent / item.get('path', item.get('file'))).resolve() == path.resolve()]
        require(len(matches) == 1, 'Primary source has no unique author-manifest entry')
        author = matches[0]
        require(all(author[key] == row[key] for key in ('sha256', 'bytes', 'url')) and
                author.get('access_utc', author.get('requested_at_utc')) == row['access_utc'], 'Primary source provenance differs')
        expected_content = 'text/html' if row['kind'] == 'original-source-html' else 'application/pdf'
        if row['kind'] == 'original-source-html':
            require(author.get('content_type', '').split(';', 1)[0].strip().lower() == 'text/html',
                    'Encoded HTML needs its original response media type')
        require(author.get('role', '').startswith('original-source') or author.get('content_type') == expected_content or
                author.get('status', '').startswith('FETCHED'), 'Primary source has no original-source/successful-fetch evidence')
        require('error' not in author and source_kind(row['path'], path.read_bytes(), {row['path']: row}) is not None,
                'Primary source registration rejected')
        result[row['path']] = row
    return result


def classify(relative, raw):
    path = Path(relative)
    require(not {'bin', 'obj', 'node_modules', '__pycache__', '.git'}.intersection(path.parts),
            f'Generated payload directory: {relative}')
    # A compound metadata name is a text candidate; a final binary extension is a payload.
    require(path.suffix.lower() not in {'.dll', '.exe', '.pdb', '.so', '.a', '.o', '.dylib', '.pyc', '.nupkg', '.zip'},
            f'Binary/cache payload name: {relative}')
    require(path.suffix.lower() != '.pdf' and not raw.startswith(b'%PDF-'), 'Register exact original PDF before record admission')
    text = raw.decode('utf-8-sig')  # Invalid UTF-8 is a failed record, never an omitted input.
    require(b'\0' not in raw and not raw.startswith((b'MZ', b'\x7fELF', b'PK\x03\x04', b'!<arch>\n')),
            f'Binary payload content: {relative}')
    if path.suffix == '.json' or path.name.endswith('.json.snapshot'):
        json.loads(text)
        return 'utf8-json'
    return 'utf8-text'


def load():
    original = INTEGRATION / 'validation/check-records.py'
    source = pinned(original, PINS['check-records.py']).decode()
    old = '''    forbidden = {"bin", "obj", "node_modules", "__pycache__", ".git"}
    require(not forbidden.intersection(path.relative_to(PACKET).parts), f"Generated directory: {path}")
    require(not any(suffix.lower() in {".dll", ".exe", ".pdb", ".so", ".a", ".o", ".dylib", ".pyc", ".nupkg", ".zip"}
                    for suffix in path.suffixes), f"Binary/cache artifact: {path}")
'''
    require(source.count(old) == 1, 'Reconcile the pinned classification site')
    registered = primary_sources()  # Every listed original remains mandatory, even if enumeration misses it.
    def is_registered_source(path, raw):
        return source_kind(path.relative_to(RUN).as_posix(), raw, registered) is not None
    text_guard = '''    raw.decode("utf-8-sig")
    require(b"\\0" not in raw and not raw.startswith((b"MZ", b"\\x7fELF")), f"Runtime/binary content: {path}")
'''
    require(source.count(text_guard) == 1, 'Reconcile the pinned text-content guard')
    adapted = source.replace(old, '').replace(text_guard, '    if not is_registered_source(path, raw):\n' +
                                              ''.join('    ' + line for line in text_guard.splitlines(keepends=True)))
    scope = dict(__file__=str(original), __name__='core_record_algorithm', is_registered_source=is_registered_source)
    exec(compile(adapted, str(original) + '[Core-content-kinds-and-registered-primary-sources]', 'exec'), scope)
    scope.update(HERE=HERE, ROOT=ROOT, PACKET=RUN, PREFIX=RUN.relative_to(ROOT).as_posix() + '/')
    original_record = scope['record']

    def record(path):
        require(not path.is_symlink() and path.resolve().is_relative_to(RUN), f'Escaping/symlinked record: {path}')
        relative = path.relative_to(RUN).as_posix()
        if relative in registered:
            safe_source(relative)
        raw = path.read_bytes()
        kind = source_kind(relative, raw, registered) or classify(relative, raw)
        result = original_record(path)
        require(result['raw_sha256'] == sha(raw), 'Record changed between classification and capture')
        if relative in registered:
            require(result['attributes']['text'] == 'unset' and result['raw_preserved_in_git'], 'Original source needs byte-exact Git attributes')
            return dict(result, kind=kind, normalized_sha256=result['raw_sha256'], normalization='byte-exact-original-source',
                        source_registration=registered[relative])
        return dict(result, kind=kind)

    scope['record'] = record
    return scope


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('snapshot', 'check', 'staged'))
    parser.add_argument('--manifest', type=Path, required=True, help='Fresh manifest directly under validation/')
    args = parser.parse_args()
    load()['execute'](args.mode, args.manifest)
