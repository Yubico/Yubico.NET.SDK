"""Read-only actual-source admission and process-local author-metadata mutations."""
import copy
import importlib.util
import json
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
V = HERE.parent
sys.path.insert(0, str(V))


def load():
    spec = importlib.util.spec_from_file_location('corrected_author_record_checker', V / 'check-records-v2.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    adapter = load()
    original = adapter.original()
    try:
        original.primary_sources()
    except ValueError as error:
        retained_failure = str(error)
    else:
        raise AssertionError('The demonstrated original schema failure disappeared')
    corrected = adapter.load()
    registered = corrected['registered_sources']()
    assert len(registered) == 8
    accepted = [corrected['record'](original.RUN / key) for key in registered]
    assert sum(row['kind'] == 'original-specification-pdf' for row in accepted) == 7
    assert sum(row['kind'] == 'original-source-html' for row in accepted) == 1
    assert all(row['raw_preserved_in_git'] and row['raw_sha256'] == row['staged_sha256'] for row in accepted)
    rejected = []
    for relative, row in registered.items():
        authority = original.RUN / row['authority']
        manifest = json.loads(authority.read_bytes())
        author = adapter.match_author(original, row, authority, manifest)
        for field in ('url', 'access_utc', 'bytes', 'sha256'):
            changed = copy.deepcopy(row)
            changed[field] = 1 if field == 'bytes' else 'changed'
            try:
                adapter.validate_author(original, changed, author)
            except ValueError:
                rejected.append(relative + ':' + field)
            else:
                raise AssertionError('Mismatched author identity accepted: ' + field)
    target = next(row for row in registered.values() if '/B6/' in row['path'])
    authority = original.RUN / target['authority']
    manifest = json.loads(authority.read_bytes())
    selected = next(index for index, row in enumerate(manifest['sources']) if row.get('name') == Path(target['path']).name)
    cases = [('status404', 'status', 404), ('string200', 'status', '200'), ('bool200', 'status', True),
             ('missing-retained', 'retained_path', None), ('wrong-retained', 'retained_path', 'docs/other.pdf'),
             ('escape-retained', 'retained_path', '../outside.pdf'), ('absolute-retained', 'retained_path', '/outside.pdf'),
             ('escape-name', 'name', '../outside.pdf'), ('failed-fetch', 'error', 'HTTP Error'),
             ('not-original', 'byte_exact_original', False), ('wrong-media-type', 'content_type', 'text/html')]
    for label, field, value in cases:
        changed = copy.deepcopy(manifest)
        changed['sources'][selected][field] = value
        try:
            normalized = adapter.match_author(original, target, authority, changed)
            adapter.validate_author(original, target, normalized)
        except ValueError:
            rejected.append(label)
        else:
            raise AssertionError('Invalid B6 authority accepted: ' + label)
    changed = copy.deepcopy(manifest)
    changed['sources'].append(copy.deepcopy(changed['sources'][selected]))
    try:
        adapter.match_author(original, target, authority, changed)
    except ValueError:
        rejected.append('ambiguous-author')
    else:
        raise AssertionError('Ambiguous authority accepted')
    b4 = next(row for row in registered.values() if '/B4/' in row['path'])
    b4_authority = original.RUN / b4['authority']
    b4_manifest = json.loads(b4_authority.read_bytes())
    index = next(index for index, row in enumerate(b4_manifest) if row.get('name') == Path(b4['path']).name)
    for label, field, value in [('b4-status404', 'status', 404), ('b4-string200', 'status', '200'),
                                 ('b4-failed-fetch', 'fetch_error', 'failed'), ('b4-byte-type', 'bytes', str(b4['bytes'])),
                                 ('b4-name-mismatch', 'name', 'another.pdf')]:
        changed = copy.deepcopy(b4_manifest)
        changed[index][field] = value
        try:
            author = adapter.match_author(original, b4, b4_authority, changed)
            adapter.validate_author(original, b4, author)
        except ValueError:
            rejected.append(label)
        else:
            raise AssertionError('Invalid B4 authority accepted: ' + label)
    unknown_authority = b4_authority.with_name('unregistered-schema.json')
    try:
        adapter.match_author(original, b4, unknown_authority, b4_manifest)
    except ValueError:
        rejected.append('name-schema-at-unrecognized-authority')
    else:
        raise AssertionError('Name/status schema accepted at arbitrary authority')
    saved_read, saved_link = Path.read_bytes, Path.is_symlink
    path = original.RUN / target['path']
    for label in ('missing', 'changed', 'symlink'):
        def fake_read(candidate):
            if candidate == path:
                if label == 'missing':
                    raise FileNotFoundError(str(path))
                if label == 'changed':
                    return b'%PDF-changed'
            return saved_read(candidate)
        Path.read_bytes = fake_read
        Path.is_symlink = lambda candidate: candidate == path if label == 'symlink' else saved_link(candidate)
        try:
            try:
                adapter.primary_sources(original)
            except (ValueError, FileNotFoundError):
                rejected.append('registered-source-' + label)
            else:
                raise AssertionError('Invalid registered source accepted: ' + label)
        finally:
            Path.read_bytes, Path.is_symlink = saved_read, saved_link
    for relative, raw in [('unregistered.pdf', b'%PDF-fake'), ('runtime.dll', b'MZpayload'), ('nonutf8.html', b'\xff')]:
        try:
            original.classify(relative, raw)
        except ValueError:
            rejected.append(relative)
        else:
            raise AssertionError('Ordinary binary/non-UTF8 input accepted')
    return dict(scope='Read-only exact original source admission and in-memory negative mutations; no archive freeze/staged check.',
                original_failure=retained_failure, registered_sources=len(registered), accepted_records=accepted,
                negative_cases=len(rejected), rejected=rejected)


if __name__ == '__main__':
    print(json.dumps(main(), indent=2))
