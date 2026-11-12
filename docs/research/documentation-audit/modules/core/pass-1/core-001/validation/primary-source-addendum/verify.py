"""Read-only exact primary-PDF admission, including content and source-role negatives."""
import copy
import importlib.util
import json
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
V = HERE.parent
sys.path.insert(0, str(V))


def main():
    spec = importlib.util.spec_from_file_location('primary_record_admission', V / 'check-records.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    registered = module.primary_sources()
    assert len(registered) == 4
    relative, row = next((key, row) for key, row in registered.items() if row['kind'] == 'original-specification-pdf')
    path = V.parent / relative
    raw = path.read_bytes()
    assert module.source_kind(relative, raw, registered) == 'original-specification-pdf'
    result = module.load()['record'](path)
    assert result['kind'] == 'original-specification-pdf' and result['raw_preserved_in_git'] is True
    assert result['raw_sha256'] == result['staged_sha256'] == row['sha256']
    rejected = []
    cases = [('changed-bytes', relative, raw[:-1] + bytes([raw[-1] ^ 1]), registered),
             ('changed-length', relative, raw[:-1], registered),
             ('unregistered-path', 'sources/unregistered.pdf', raw, registered)]
    for label, key, payload, rows in cases:
        try:
            kind = module.source_kind(key, payload, rows)
            if kind is None:
                module.classify(key, payload)
        except ValueError:
            rejected.append(label)
        else:
            raise AssertionError('Accepted unreviewed PDF identity: ' + label)
    for label, field, value in [('wrong-kind', 'kind', 'runtime'), ('wrong-url', 'url', 'file:///runtime'),
                                 ('wrong-hash', 'sha256', '0' * 64), ('wrong-size', 'bytes', 1)]:
        changed = copy.deepcopy(registered)
        changed[relative][field] = value
        try:
            module.source_kind(relative, raw, changed)
        except ValueError:
            rejected.append(label)
        else:
            raise AssertionError('Accepted invalid source registration: ' + label)
    for label, payload in [('MZ', b'MZ' + raw[2:]), ('ELF', b'\x7fELF' + raw[4:]), ('bad-magic', b'other' + raw[5:])]:
        changed = copy.deepcopy(registered)
        changed[relative]['sha256'] = module.sha(payload)
        try:
            module.source_kind(relative, payload, changed)
        except ValueError:
            rejected.append(label)
        else:
            raise AssertionError('Accepted binary payload with source role: ' + label)
    for relative_path in ('../outside.pdf', '/outside.pdf'):
        try:
            module.safe_source(relative_path)
        except ValueError:
            rejected.append('unsafe-path:' + relative_path)
        else:
            raise AssertionError('Accepted escaping primary source')
    saved_link = Path.is_symlink
    Path.is_symlink = lambda candidate: candidate == path or saved_link(candidate)
    try:
        try:
            module.primary_sources()
        except ValueError:
            rejected.append('symlinked-registered-source')
        else:
            raise AssertionError('Accepted a symlinked registered source')
    finally:
        Path.is_symlink = saved_link
    saved_read = Path.read_bytes
    def missing(candidate):
        if candidate == path:
            raise FileNotFoundError(str(candidate))
        return saved_read(candidate)
    Path.read_bytes = missing
    try:
        try:
            module.primary_sources()
        except FileNotFoundError:
            rejected.append('missing-registered-source')
        else:
            raise AssertionError('Omitted a missing registered source')
    finally:
        Path.read_bytes = saved_read
    html_key, html_row = next((key, value) for key, value in registered.items() if value['kind'] == 'original-source-html')
    html = (V.parent / html_key).read_bytes()
    for label, field, value in [('html-wrong-encoding', 'encoding', 'utf-8'), ('html-no-declaration-evidence', 'encoding_evidence', '')]:
        changed = copy.deepcopy(registered)
        changed[html_key][field] = value
        try:
            module.source_kind(html_key, html, changed)
        except ValueError:
            rejected.append(label)
        else:
            raise AssertionError('Accepted unreviewed encoding: ' + label)
    try:
        module.classify('sources/unregistered.html', html)
    except ValueError:
        rejected.append('unregistered-non-utf8-html')
    else:
        raise AssertionError('Accepted arbitrary unregistered Latin-1')
    authority_path = V.parent / html_row['authority']
    original_authority = json.loads(authority_path.read_bytes())
    changed_authority = copy.deepcopy(original_authority)
    next(item for item in changed_authority if item.get('file') == Path(html_key).name)['content_type'] = 'application/octet-stream'
    def wrong_media(candidate):
        return json.dumps(changed_authority).encode() if candidate == authority_path else saved_read(candidate)
    Path.read_bytes = wrong_media
    try:
        try:
            module.primary_sources()
        except ValueError:
            rejected.append('html-wrong-original-response-media-type')
        else:
            raise AssertionError('Accepted an unqualified non-UTF8 response')
    finally:
        Path.read_bytes = saved_read
    accepted = [module.load()['record'](V.parent / key) for key in registered]
    assert all(record['raw_preserved_in_git'] and record['raw_sha256'] == record['staged_sha256'] == registered[key]['sha256']
               for key, record in zip(registered, accepted))
    return dict(scope='Read-only exact original PDF/encoding-qualified HTML and actual Git-clean proof; no compiler/checker/native/device execution.',
                registered_sources=len(registered), accepted_records=accepted, rejected=rejected)


if __name__ == '__main__':
    print(json.dumps(main(), indent=2))
