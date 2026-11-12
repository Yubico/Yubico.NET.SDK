"""Reconcile only accepted current inventories; no compiler, build, test or body execution."""
import hashlib
import html
import json
from pathlib import Path
import re

HERE = Path(__file__).resolve().parent
RUN = HERE.parents[2]
ROOT = HERE.parents[9]
EDIT = RUN / 'editing'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def identity(path):
    raw = path.read_bytes()
    return dict(path=path.relative_to(ROOT).as_posix(), bytes=len(raw), sha256=sha(raw))


def extract(packet, row):
    path = row['path']
    original = (ROOT / path).read_bytes()
    text = original.decode('utf-8-sig').replace('\r\n', '\n')
    markdown = path.endswith('.md')
    if markdown:
        blocks = list(re.finditer(r'^[ \t]*```([^\n]*)\n(.*?)^[ \t]*```[^\n]*$', text, re.M | re.S))
    else:
        blocks = list(re.finditer(r'<code(?:\s[^>]*)?>(.*?)</code>', text, re.S))
    if packet in ('PGP', 'PIV'):
        index = row['body_index_in_file']
    elif packet == 'FIDO':
        index = row['fence'] - 1
    elif packet == 'APP':
        index = row['ordinal_all_fences_or_code_blocks'] - 1
    elif packet == 'SHARED':
        index = row['current_file_ordinal'] - 1
    else:
        index = row['ordinal'] - 1
    if index >= len(blocks):
        raise ValueError(f'Inventory coordinate requires schema check: {packet} {path} index {index}, blocks {len(blocks)}')
    match = blocks[index]
    raw = match.group(2 if markdown else 1)
    language = match.group(1).strip() if markdown else 'csharp'
    stripped = html.unescape(re.sub(r'(?m)^[ \t]*/// ?', '', raw))
    # SHARED's reviewed_body fields preserve the predecessor; body/body_sha256 are its successor.
    expected = row.get('body_sha256', row.get('body_sha256_utf8'))
    # These are the exact documented editor extraction formulas, not source equivalence rules.
    forms = [('literal-xml-or-fence', raw), ('rendered-prefixes-entities', stripped),
             ('rendered-rstrip', stripped.rstrip()), ('rendered-boundary-lines', stripped.strip('\n')),
             ('rendered-initial-newline-final-whitespace', stripped.lstrip('\n').rstrip())]
    if 'body' in row and sha(row['body'].encode()) == expected:
        forms += [('editor-body-with-rendered-boundary-whitespace', stripped.strip())]
    accepted = [(formula, value) for formula, value in forms if sha(value.encode()) == expected]
    if not accepted:
        raise ValueError(f'Accepted current body mismatch: {packet} {path} index {index}; expected {expected}')
    formula, editor_body = accepted[0]
    start = len(text[:match.start(2 if markdown else 1)].encode())
    # Retain exact original byte coordinates even for CRLF checkouts.
    pattern = rb'^[ \t]*```([^\r\n]*)\r?\n(.*?)^[ \t]*```[^\r\n]*' if markdown else rb'<code(?:\s[^>]*)?>(.*?)</code>'
    actual = list(re.finditer(pattern, original, re.M | re.S))[index]
    raw_bytes = actual.group(2 if markdown else 1)
    decoded = html.unescape(re.sub(r'(?m)^[ \t]*/// ?', '', raw_bytes.decode().replace('\r\n', '\n'))).strip('\n')
    return dict(packet=packet, path=path, ordinal=index + 1, language=language,
                first_line=original[:actual.start()].count(b'\n') + 1,
                last_line=original[:actual.end()].count(b'\n') + 1,
                raw_start_byte=actual.start(2 if markdown else 1), raw_end_byte=actual.end(2 if markdown else 1),
                raw_sha256=sha(raw_bytes), raw_body=raw_bytes.decode(), decoded_body=decoded,
                decoded_sha256=sha(decoded.encode()), editor_body_sha256=expected, editor_formula=formula,
                editor_classification=row.get('classification', ''), document=identity(ROOT / path))


def collect():
    entries, authorities = [], []
    def add(packet, relative, rows):
        inventory = EDIT / relative
        folder = relative.split('/')[0]
        review = EDIT / folder / 'revision-2/review.md'
        assert review.is_file() and 'PASS' in review.read_text()[:1000]
        authorities.extend([identity(inventory), identity(review)])
        counts = {}
        for row in rows:
            if packet == 'SHARED':
                counts[row['path']] = counts.get(row['path'], 0) + 1
                row = dict(row, current_file_ordinal=counts[row['path']])
            item = extract(packet, row)
            item.update(authority=identity(inventory), review=identity(review))
            entries.append(item)
    for packet, relative in [('FIDO', 'FIDO-boundaries/revision-2/examples.json'),
                             ('SHARED', 'SHARED/revision-2/example-bodies.json'),
                             ('APP', 'applet-boundaries/examples.json'),
                             ('PGP', 'PGP-boundary/revision-2/example-inventory.json'),
                             ('PIV', 'PIV-boundary/revision-2/example-inventory.json')]:
        add(packet, relative, json.loads((EDIT / relative).read_bytes()))
    protocol = json.loads((EDIT / 'protocols/examples.json').read_bytes())
    add('PROTO', 'protocols/examples.json', [row for row in protocol['xml_code_bodies'] if row['version'] == 'current'])
    transports = json.loads((EDIT / 'transports/revision-2/example-bodies.json').read_bytes())
    rows = []
    for source in transports['bodies']:
        for ordinal, body in enumerate(source['current_bodies'], 1):
            rows.append(dict(path=source['path'], ordinal=ordinal, body_sha256=body['body_sha256'],
                             body=body['body'], classification=source['classification']))
    add('TRANS', 'transports/revision-2/example-bodies.json', rows)
    for index, row in enumerate(entries, 1):
        row['id'] = f'FULL-{index:03}'
    return dict(source_baseline='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef',
                authorities=authorities, entries=entries,
                pilot='17 already compiled bodies remain historical accepted pilot evidence; no bookkeeping rerun.',
                external='Pending transfer/acceptance; final toolchain participant descriptor remains unsealed.')


if __name__ == '__main__':
    result = collect()
    with (HERE / 'current-inventory.json').open('x') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    from collections import Counter
    print('Current reconciled entries:', len(result['entries']), dict(Counter(row['packet'] for row in result['entries'])))
