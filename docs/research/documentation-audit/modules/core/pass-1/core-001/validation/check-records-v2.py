"""Additive known-author-schema adapter; original format/Git-clean/index algorithms stay exact."""
import argparse
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from foundation import ROOT, RUN, require, pinned

ORIGINAL_SHA = 'e115552ca79243091ad160bddb0667e4bb1dc9d2d1cd86800b4152ac8ab2f552'
B4 = 'investigation/B4/sources/captures.json'
B6 = 'investigation/B6/sources/final-manifest.json'


def original():
    path = HERE / 'check-records.py'
    raw = pinned(path, ORIGINAL_SHA)
    spec = importlib.util.spec_from_file_location('preserved_core_record_checker', path)
    module = importlib.util.module_from_spec(spec)
    exec(compile(raw, str(path), 'exec'), module.__dict__)
    require(module.HERE == HERE and module.ROOT == ROOT and module.RUN == RUN, 'Original checker context differs')
    return module


def relative_path(base, value, boundary):
    require(isinstance(value, str) and value and not Path(value).is_absolute() and '..' not in Path(value).parts,
            'Require a safe relative author path')
    parts = Path(value).parts
    require(not any((base / Path(*parts[:index])).is_symlink() for index in range(1, len(parts) + 1)), 'Symlinked author path')
    path = base / value
    require(path.resolve().is_relative_to(boundary), 'Author path escapes its declared source scope')
    return path.resolve()


def match_author(module, row, authority, manifest):
    location = authority.relative_to(RUN).as_posix()
    if location == B6:
        require(isinstance(manifest, dict) and manifest.get('state') == 'FINAL-BOUNDED-SOURCE-REGISTER' and
                isinstance(manifest.get('sources'), list), 'Unsupported B6 final source manifest')
        population, schema = manifest['sources'], 'B6-final'
    else:
        require(isinstance(manifest, list) or isinstance(manifest, dict) and isinstance(manifest.get('files'), list),
                'Unsupported original author manifest')
        population = manifest if isinstance(manifest, list) else manifest['files']
        schema = 'B4-captures' if location == B4 else 'legacy'
    target = module.safe_source(row['path']).resolve()
    matches = []
    for item in population:
        require(isinstance(item, dict), 'Author manifest contains a non-object entry')
        fields = ('name',) if schema != 'legacy' else ('path', 'file')
        aliases = [relative_path(authority.parent, item[field], RUN) for field in fields if item.get(field) is not None]
        retained = item.get('retained_path')
        retained_match = False
        if retained is not None:
            retained_match = relative_path(ROOT, retained, RUN) == target
        if target not in aliases and not retained_match:
            continue
        require(aliases and all(path == target for path in aliases), 'Author name/path aliases disagree with registered source')
        if schema == 'B6-final':
            require(retained is not None and retained_match and item.get('byte_exact_original') is True,
                    'B6 retained_path/name/original-byte identity differs')
        elif retained is not None:
            require(retained_match, 'Optional retained_path disagrees with author name')
        normalized = dict(item)
        normalized['_schema'] = schema
        timestamp = 'accessed_utc' if schema == 'B4-captures' else 'requested_utc' if schema == 'B6-final' else None
        normalized['access_utc'] = item.get(timestamp) if timestamp else item.get('access_utc', item.get('requested_at_utc'))
        matches.append(normalized)
    require(len(matches) == 1, 'Primary source has no unique normalized author-manifest entry')
    return matches[0]


def validate_author(module, row, author):
    require(type(row['bytes']) is int and type(author.get('bytes')) is int and row['bytes'] > 0 and
            all(author.get(key) == row[key] for key in ('sha256', 'bytes', 'url', 'access_utc')) and
            all(isinstance(author.get(key), str) and author[key] for key in ('sha256', 'url', 'access_utc')),
            'Primary source author URL/timestamp/hash/byte identity differs')
    require('error' not in author and 'fetch_error' not in author, 'Failed author fetch supplies no retained-original proof')
    status = author.get('status')
    if author['_schema'] in ('B4-captures', 'B6-final'):
        require(type(status) is int and status == 200, 'Known HTTP author schema requires integer status 200')
    elif 'status' in author:
        require(isinstance(status, str) and status.startswith('FETCHED'), 'Legacy fetch status does not establish success')
    expected_content = 'text/html' if row['kind'] == 'original-source-html' else 'application/pdf'
    if row['kind'] == 'original-source-html':
        require(author.get('content_type', '').split(';', 1)[0].strip().lower() == 'text/html', 'Encoded HTML needs original response media type')
    if author['_schema'] == 'legacy':
        require((isinstance(author.get('role', ''), str) and author.get('role', '').startswith('original-source')) or
                author.get('content_type') == expected_content or (isinstance(status, str) and status.startswith('FETCHED')),
                'Legacy author has no original-source/successful-fetch evidence')
    if 'content_type' in author:
        require(isinstance(author['content_type'], str) and author['content_type'].split(';', 1)[0].strip().lower() == expected_content,
                'Author response format disagrees with registered source kind')


def primary_sources(module):
    rows = json.loads(module.safe_source('sources/primary-binaries.json').read_bytes())['files']
    require(rows and len({row['path'] for row in rows}) == len(rows), 'Empty/duplicate primary source registry')
    result = {}
    for row in rows:
        path, authority = module.safe_source(row['path']), module.safe_source(row['authority'])
        author = match_author(module, row, authority, json.loads(authority.read_bytes()))
        validate_author(module, row, author)
        require(module.source_kind(row['path'], path.read_bytes(), {row['path']: row}) is not None, 'Original source kind/format/bytes rejected')
        result[row['path']] = row
    return result


def load():
    module = original()
    module.primary_sources = lambda: primary_sources(module)
    scope = module.load()
    scope['registered_sources'] = module.primary_sources
    return scope


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('snapshot', 'check', 'staged'))
    parser.add_argument('--manifest', type=Path, required=True)
    args = parser.parse_args()
    load()['execute'](args.mode, args.manifest)
