"""Static, bounded R1–R6 successor capture; never imports or runs the original capture tool."""
import copy
import difflib
import hashlib
import json
import pathlib
import re
import subprocess
import urllib.parse

OUT = pathlib.Path(__file__).resolve().parent
OLD = OUT.parent
RUN = OLD.parent.parent
ROOT = RUN.parents[6]
BASE = 'da66f5d01ba594993974ddd26723e8c300c0a454'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
SUBJECT = '3267ab5d547b281f19c28b3b709ef509a7cd54ee82439b6386b33a842030b937'

def sha(b):
    return hashlib.sha256(b).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def read(name):
    return json.loads((OLD / name).read_text())

def put(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

assert git('rev-parse', 'HEAD').decode().strip() == BASE
assert not git('diff', '--cached', '--name-only').strip()
original_patch = (OLD / 'product.patch').read_bytes()
assert sha(original_patch) == SUBJECT
review = read('review-identities.json')
for path, digest in review['editor_inputs_relative_to_run'].items():
    assert sha((RUN / path).read_bytes()) == digest, path
preserved = [{'path': str(p.relative_to(RUN)), 'sha256': sha(p.read_bytes())}
             for p in sorted(OLD.iterdir()) if p.is_file()]
preserved.append({'path': 'edit-SHARED.md', 'sha256': sha((RUN / 'edit-SHARED.md').read_bytes())})

# Reconstruct exact reviewed files in memory from the preserved full patch and immutable base.
patch_lines = original_patch.decode().splitlines(keepends=True)
reviewed_text = {}
path = None
pieces = []
source = []
cursor = 0
i = 0
while i < len(patch_lines):
    line = patch_lines[i]
    if line.startswith('diff --git '):
        if path is not None:
            reviewed_text[path] = ''.join(pieces + source[cursor:])
        path = line.split(' b/', 1)[1].rstrip('\n')
        source = git('show', f'{BASE}:{path}').decode().splitlines(keepends=True)
        pieces, cursor = [], 0
    elif line.startswith('@@ '):
        start = int(re.match(r'@@ -(\d+)', line)[1]) - 1
        pieces.extend(source[cursor:start])
        cursor = start
        records = []
        i += 1
        while i < len(patch_lines) and not patch_lines[i].startswith(('@@ ', 'diff --git ')):
            item = patch_lines[i]
            if item.startswith('\\ No newline'):
                op, text = records[-1]
                records[-1] = (op, text.removesuffix('\n'))
            elif item[0] in ' +-':
                records.append((item[0], item[1:]))
            else:
                raise AssertionError(item)
            i += 1
        for op, text in records:
            if op in ' -':
                assert source[cursor] == text, (path, cursor)
                cursor += 1
            if op in ' +':
                pieces.append(text)
        continue
    i += 1
if path is not None:
    reviewed_text[path] = ''.join(pieces + source[cursor:])

files = read('path-dispositions.json')
blocked = {p['path'] for p in review['product_paths'] if p['status'] == 'correction-required'}
assert len(blocked) == 7
current_text = {}
delta_paths = []
for row in files:
    p = row['path']
    before = reviewed_text.get(p, git('show', f'{BASE}:{p}').decode())
    assert sha(before.encode()) == row['current_sha256'], p
    current = (ROOT / p).read_bytes()
    current_text[p] = current.decode()
    if current != before.encode():
        delta_paths.append(p)
    if p not in blocked:
        assert current == before.encode(), p
    row['reviewed_subject_sha256'] = row['current_sha256']
    row['current_sha256'] = sha(current)
    row['revision_2'] = ('R1–R5 correction; focused review pending' if p in blocked else
                         'Product bytes retained from reviewed subject; record-only R6 if applicable')
assert set(delta_paths) == blocked
edited = [p['path'] for p in files if p['decision'] == 'edit']
assert set(git('diff', '--name-only', BASE).decode().splitlines()) == set(edited)
successor = git('diff', '--binary', '--full-index', '--src-prefix=a/', '--dst-prefix=b/', BASE, '--', *edited)
(OUT / 'product.patch').write_bytes(successor)
delta = ''.join(''.join(difflib.unified_diff(reviewed_text[p].splitlines(keepends=True),
                   current_text[p].splitlines(keepends=True), fromfile='a/'+p, tofile='b/'+p))
                for p in sorted(blocked))
(OUT / 'corrections.patch').write_text(delta)
put('path-dispositions.json', files)
put('changed-path-sha256.json', [p for p in files if p['decision'] == 'edit'])
put('correction-path-sha256.json', [p for p in files if p['path'] in blocked])

old_examples = read('example-bodies.json')
new_by_path = {}
unclosed = []
for p, text in current_text.items():
    bodies = []
    lines = text.splitlines(keepends=True)
    opening = None
    for n, line in enumerate(lines):
        match = re.match(r'^\s*(`{3,}|~{3,})([^\r\n]*)', line)
        if not match:
            continue
        if opening is None:
            opening, mark, language = n, match[1][0], match[2].strip()
        elif match[1][0] == mark:
            body = ''.join(lines[opening+1:n])
            bodies.append(dict(kind='fence', opening_line=opening+1, closing_line=n+1,
                body_first_line=opening+2, body_last_line=n, language=language,
                body=body, body_sha256=sha(body.encode())))
            opening = None
    if opening is not None:
        unclosed.append((p, opening+1))
    for match in re.finditer(r'<code(?:\s[^>]*)?>(.*?)</code>', text, re.S):
        body = match[1]
        bodies.append(dict(kind='xml-code', body=body, body_sha256=sha(body.encode()),
            body_first_line=text.count('\n', 0, match.start(1))+1,
            body_last_line=text.count('\n', 0, match.end(1))+1))
    new_by_path[p] = bodies
examples = []
for number, old in enumerate(old_examples, 1):
    fresh = new_by_path[old['path']].pop(0)
    assert fresh['kind'] == old['kind']
    row = copy.deepcopy(old)
    row.update(fresh)
    row.update(reviewed_example_number=number, reviewed_body=old['body'],
               reviewed_body_sha256=old['body_sha256'], reviewed_coordinates={k:v for k,v in old.items() if 'line' in k})
    if number in (13, 53, 65, 95):
        row['classification'] = 'compilable-test-class-unchecked' if number == 13 else 'compilable-method-unchecked'
        row['reason'] = {
            13: 'Complete current test class in Core.UnitTests internal/fake/xUnit context; scopes fake and borrowed protocol, asserts the 1390 complete-command ceiling. Compilation NOT RUN.',
            53: 'Complete typed method borrowing application-owned ILoggerFactory; caller lifetime covers late work and captured static loggers. Global restoration is not provider drain. Compilation NOT RUN.',
            65: 'Complete typed method borrowing an open RawSmartCardSession, owning request/response cleanup. Command bytes are illustrative; caller must select a valid applet/command. Compilation NOT RUN.',
            95: 'Complete illustrative helper borrowing an IYubiKey fake configured for SmartCard, owning returned connection and checking actual Type. Requires xUnit/Core references; no device-health claim. Compilation NOT RUN.',
        }[number]
    if number == 118:
        assert row['body_sha256'] == old['body_sha256']
        row['classification'] = 'mixed-historical-before-current-after'
        row['reason'] = 'Retain the complete mixed body. Only obsolete Before is historical/excluded; current After is eligible for context-specific compilation with borrowed connection, AID and token.'
        split = row['body'].index('// After\n')
        row['segments'] = []
        for label, a, b, eligible in [('Before', 0, split, False), ('After', split, len(row['body']), True)]:
            body = row['body'][a:b]
            row['segments'].append(dict(label=label, body=body, body_sha256=sha(body.encode()),
                first_line=row['body_first_line']+row['body'][:a].count('\n'),
                last_line=row['body_first_line']+row['body'][:b].count('\n')-1,
                compilation_eligible=eligible, classification='current-composable-fragment' if eligible else 'historical-excluded',
                compiler_state='NOT RUN', execution_state='NOT RUN'))
    if number == 65:
        assert row['body_sha256'] == old['body_sha256']
    examples.append(row)
assert len(examples) == 193 and not any(new_by_path.values())
put('example-bodies.json', examples)
put('example-decisions.json', [x for x in examples if x['reviewed_example_number'] in (13,53,65,95,118)])

refs = {
    'R1': ['CORE-B6-C001','CORE-C-C017','CORE-B4-C010'],
    'R2': ['CORE-B1-C012','CORE-B1-C005','CORE-B4-C001'],
    'R3': ['CORE-C-C015'],
    'R4': ['CORE-A-C008','CORE-B1-C002','CORE-C-C008','CORE-B6-C014'],
    'R5': ['CORE-B5-C002'],
    'R6': ['CORE-C-C017','CORE-B6-C015','CORE-B5-C009'],
}
candidates = read('candidate-map.json')
for row in candidates:
    row['revision_2_review_corrections'] = [r for r, ids in refs.items() if row['local_id'] in ids]
    row['revision_2_source_report'] = 'source-corrections.md'
    row['revision_2_runtime_policy_qualification'] = 'Unchanged; all original holds preserved.'
put('candidate-map.json', candidates)
put('candidate-decisions.json', {'review_refs': refs,
    'separate_boundary_refs': ['CORE-CTAP-X006', 'ctap2-001:CTAP-IMP-007'],
    'changes': 'Documentation/record corrections only; original local/proposal IDs and holds unchanged.'})

source_ranges = {
    'src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs': [(44,70),(162,195)],
    'src/Core/src/Protocols/SmartCard/Apdu/SmartCardMaxApduSizes.cs': [(17,34)],
    'src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Apdu/Fakes/FakeSmartCardConnection.cs': [(24,72)],
    'src/Core/src/Transports/SmartCard/ISmartCardConnection.cs': [(19,72)],
    'src/Core/src/Abstractions/IConnection.cs': [(26,29)],
    'src/Core/src/Devices/DeviceConnectionRegistry.cs': [(94,124),(193,245)],
    'src/Ctap2/src/Pin/PinUvAuthHelpers.cs': [(58,117)],
    'src/Core/src/Devices/FindYubiKeys.cs': [(43,43),(230,271)],
    'src/Core/src/YubiKitLogging.cs': [(38,39),(52,66)],
    'src/Core/src/Sessions/RawCtapHidSession.cs': [(64,74)],
    'src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs': [(290,310),(384,410)],
}
source_rows = []
for path, ranges in source_ranges.items():
    current = (ROOT/path).read_bytes()
    pinned = git('show', f'{PIN}:{path}')
    lines = current.decode('utf-8-sig').splitlines(keepends=True)
    source_rows.append(dict(path=path, current_sha256=sha(current), source_pin_sha256=sha(pinned),
        source_pin_blob=git('rev-parse', f'{PIN}:{path}').decode().strip(),
        ranges=[dict(first_line=a,last_line=b,text=''.join(lines[a-1:b]),
                     range_sha256=sha(''.join(lines[a-1:b]).encode())) for a,b in ranges]))
for name, ranges, authority in [
    ('ctap21.txt',[(8227,8230),(8272,8292),(8321,8325)],
     'CTAP 2.1 PS 2021-06-15 / errata 2022-06-21, §§6.5.6–6.5.7; ecdh/getPublicKey requirements and protocol-two inheritance, not SDK enforcement.'),
    ('ctap23.txt',[(13960,13969)],
     'CTAP 2.3 PS 2026-02-26, §11.2.9.1.5; authenticator MUST cancel cancelable outstanding work and MUST NOT reply to CANCEL; client MAY send during CBOR. Not the SDK raced-success classification.')]:
    path = RUN/'investigation/B5/sources'/name
    lines = path.read_text().splitlines(keepends=True)
    source_rows.append(dict(path=str(path.relative_to(ROOT)), sha256=sha(path.read_bytes()), authority=authority,
        ranges=[dict(first_line=a,last_line=b,text=''.join(lines[a-1:b]),
                     range_sha256=sha(''.join(lines[a-1:b]).encode())) for a,b in ranges]))
put('source-evidence.json', source_rows)

def missing(text, p):
    result = set()
    fence = False
    for line in text.splitlines():
        if re.match(r'^\s*(```+|~~~+)', line):
            fence = not fence
        if fence:
            continue
        for href in re.findall(r'(?<!!)\[[^\]]+\]\(([^\s)]+)(?:\s+"[^"]*")?\)', line):
            url = urllib.parse.urlsplit(href)
            if url.scheme or not url.path:
                continue
            if not ((ROOT/p).parent/urllib.parse.unquote(url.path)).exists():
                result.add(href)
    return result

new_missing = []
for p in edited:
    new_missing += [(p, h) for h in missing(current_text[p],p)-missing(reviewed_text[p],p)]
checks = {}
for name, args in {'diff': ['diff','--check'],
    'successor_patch': ['apply','--reverse','--check',str(OUT/'product.patch')],
    'correction_patch': ['apply','--reverse','--check',str(OUT/'corrections.patch')]}.items():
    proc = subprocess.run(['git',*args],cwd=ROOT,capture_output=True,text=True)
    checks[name] = dict(exit=proc.returncode, output=proc.stdout+proc.stderr)
    assert proc.returncode == 0, checks[name]
for item in preserved:
    assert sha((RUN/item['path']).read_bytes()) == item['sha256'], item['path']
assert not new_missing and not unclosed
put('original-record-preservation.json', preserved)
put('verification.json', dict(base=BASE, runtime_pin=PIN, original_patch_sha256=SUBJECT,
    full_successor_patch_sha256=sha(successor), corrections_patch_sha256=sha(delta.encode()),
    corrected_product_paths=sorted(blocked), stable_content_reviewed_paths=19,
    edited_paths=26, retained_paths=7, complete_example_bodies=193,
    unchanged_body_65=True, unchanged_full_mixed_body_118=True,
    new_missing_local_targets=new_missing, unclosed_fences=unclosed, static_checks=checks,
    original_records_preserved=len(preserved), compilation_execution='NOT RUN',
    focused_independent_review='pending', index_unchanged=True))
put('output-identities.json', [{'path': p.name, 'sha256':sha(p.read_bytes())}
                             for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='output-identities.json'])
print((OUT/'verification.json').read_text())
