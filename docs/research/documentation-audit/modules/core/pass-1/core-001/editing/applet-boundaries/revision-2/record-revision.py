"""Bounded R1/R2 successor records. Never execute the first editor's writing collector."""
from pathlib import Path
import difflib
import hashlib
import html
import json
import re
import subprocess
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
OLD = HERE.parent
RUN = OLD.parent.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
PRIMARY = Path('/home/rupicola/Code/y/Yubico.Yubikit.NET')
PRIMARY_RUN = PRIMARY / RUN.relative_to(ROOT)
TARGETS = {
    'src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03Tests.cs',
    'src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/SecurityDomainSession_Scp03KeyLifecycleTests.cs'}

def sha(raw):
    return hashlib.sha256(raw).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def save(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

def ranges(before, after):
    return [{'kind': kind, 'before': [a + 1, b] if b > a else None,
             'after': [c + 1, d] if d > c else None,
             'before_line_count': b - a, 'after_line_count': d - c,
             'insertion_or_deletion_anchor_before_line': a + 1}
            for kind, a, b, c, d in difflib.SequenceMatcher(
                a=before.splitlines(), b=after.splitlines(), autojunk=False).get_opcodes() if kind != 'equal']

def noncomments(text):
    return '\n'.join(line for line in text.splitlines() if not line.lstrip().startswith('//'))

history_paths = [RUN / 'edit-applet-boundaries.md', *sorted(p for p in OLD.iterdir() if p.is_file())]
history = {str(p.relative_to(ROOT)): sha(p.read_bytes()) for p in history_paths}
review = json.loads((OLD / 'review-identities.json').read_text())
assert sha((OLD / 'product.diff').read_bytes()) == review['reviewed_product_diff_sha256'] == \
    '6c3797572fc9dcc7cb7fff70f4f1122f2736101462a43289ff95f7085a71a568'
assert sha((OLD / 'review.md').read_bytes()) == review['review_report']['sha256']
for row in review['editor_artifacts_relative_to_run']:
    assert sha((RUN / row['path']).read_bytes()) == row['sha256'], row['path']
for row in json.loads((OLD / 'owned-output-identities.json').read_text()):
    assert sha((ROOT / row['path']).read_bytes()) == row['after_sha256'], row['path']

rows = json.loads((OLD / 'path-decisions.json').read_text())
before_rows = {row['path']: row for row in rows}
assert len(rows) == len(before_rows) == 233
changed_from_review = []
checks = []
for row in rows:
    path = row['path']
    old_hash = row['after_worktree_sha256']
    raw = (ROOT / path).read_bytes()
    if path not in TARGETS:
        assert sha(raw) == old_hash, (path, 'accepted content changed outside R2')
        continue
    baseline = git('show', 'HEAD:' + path)
    assert sha(baseline) == old_hash, (path, 'R2 file was not retained at review')
    before, after = baseline.decode('utf-8-sig'), raw.decode('utf-8-sig')
    assert noncomments(before) == noncomments(after), (path, 'non-comment content changed')
    assert before != after
    for block in re.findall(r'(?m)(?:^[ \t]*///[^\n]*(?:\n|$))+', after):
        ET.fromstring('<doc>' + re.sub(r'(?m)^[ \t]*/// ?', '', block) + '</doc>')
    row.update(decision='EDIT', reason='R2 / CORE-SD-X008 (related X004): describe an attempted '
               'default-key precondition, not asserted factory restoration. Rotation replaces the '
               'earlier custom set. Calls, assertions, tokens and independent Track cleanup unchanged.',
               previous_review_decision='RETAIN', previous_review_after_sha256=old_hash,
               after_worktree_sha256=sha(raw), changed_ranges_1_based_inclusive=ranges(before, after),
               terminal_newline_after=raw.endswith(b'\n'))
    changed_from_review.append(path)
    checks.append({'path': path, 'all_non_full_line_comment_lines_exact': True,
                   'XML_fragments_parse': True, 'before_sha256': old_hash, 'after_sha256': sha(raw)})
assert set(changed_from_review) == TARGETS
assert sum(row['decision'] == 'EDIT' for row in rows) == 39
save('path-decisions.json', rows)
save('R2-path-decisions.json', [row for row in rows if row['path'] in TARGETS])
(HERE / 'R2-comments.diff').write_bytes(git('diff', '--no-ext-diff', '--binary',
    '--src-prefix=c/', '--dst-prefix=w/', 'HEAD', '--', *sorted(TARGETS)))

examples = json.loads((OLD / 'examples.json').read_text())
assert len(examples) == 107 and not any(row['path'] in TARGETS for row in examples)
witnesses = []
for row in examples:
    text = (ROOT / row['path']).read_text()
    md = row['path'].endswith('.md')
    pattern = r'(?m)^```([^\n]*)\n(.*?)\n```' if md else r'<code>\s*\n(.*?)</code>'
    block = list(re.finditer(pattern, text, re.S))[row['ordinal_all_fences_or_code_blocks'] - 1]
    body = html.unescape(re.sub(r'(?m)^[ \t]*/// ?', '', block[2] if md else block[1])).rstrip()
    assert body == row['body'] and sha(body.encode()) == row['body_sha256_utf8']
    assert (text[:block.start()].count('\n') + 1, text[:block.end()].count('\n') + 1) == \
        (row['first_line'], row['last_line'])
    witnesses.append({key: row[key] for key in ['path', 'ordinal_all_fences_or_code_blocks',
                                              'first_line', 'last_line', 'body_sha256_utf8']})
save('example-preservation.json', {'original_inventory_sha256': sha((OLD / 'examples.json').read_bytes()),
     'body_range_hash_matches': 107, 'new_or_changed_in_revision_2': 0, 'witnesses': witnesses,
     'compilation': 'NOT-RUN; original classifications and VALID-NOT-CHECKED states unchanged'})

anchors = []
for name, start, end in [('scp03-1.1.2.txt', 1177, 1194), ('scp03-1.1.2.txt', 1048, 1055),
                         ('scp03-1.1.2.txt', 1084, 1086), ('scp03-1.1.2.txt', 1158, 1164),
                         ('scp11-public-review-1.3.0.13.txt', 1108, 1134)]:
    path = PRIMARY_RUN / 'investigation/B6/sources' / name
    raw = path.read_bytes()
    expected = next(row['sha256'] for row in review['primary_originals_relative_to_run']
                    if row['path'] == 'investigation/B6/sources/' + name)
    assert sha(raw) == expected
    text = raw.decode()
    pieces = text.splitlines(keepends=True)
    a, b = sum(map(len, pieces[:start - 1])), sum(map(len, pieces[:end]))
    anchors.append({'primary_path': str(path.relative_to(PRIMARY)), 'source_sha256': sha(raw),
                    'line_convention': 'Python str.splitlines(), including form-feed boundaries',
                    'lines': [start, end], 'LF_only_lines': [text[:a].count('\n') + 1,
                                                          text[:b].rstrip('\r\n').count('\n') + 1],
                    'UTF8_byte_range_half_open': [len(text[:a].encode()), len(text[:b].encode())],
                    'excerpt': text[a:b]})
save('source-anchors.json', anchors)

citations = (OLD / 'citations.md').read_text()
assert citations.count('scp03-1.1.2.txt:1149–1173') == citations.count('scp11-public-review-1.3.0.13.txt:1085–1119') == 1
citations = citations.replace('scp03-1.1.2.txt:1149–1173`, §6.2.6, reread. With C-DECRYPTION/C-MAC selected,',
    'scp03-1.1.2.txt:1177–1194`, page 29/§6.2.6, reread. With both C-DECRYPTION and C-MAC selected,')
citations = citations.replace('§6.2.5/6.2.7 response/error scopes remain in B6\'s register',
    'Selected-R-MAC qualification is retained: §6.2.5 describes response R-MAC generation/verification '
    'when that protection is selected, not a universal response requirement; preceding context at '
    '1158–1164 explicitly makes R-MAC optional. Error-status and EXTERNAL AUTHENTICATE exceptions remain '
    'at 1048–1055; host verification is at 1084–1086. No broader response-encryption claim is added')
citations = citations.replace('scp11-public-review-1.3.0.13.txt:1085–1119`, §4.3, reread:',
    'scp11-public-review-1.3.0.13.txt:1108–1134`, page 24/§4.3, reread:')
citations = citations.replace('with correct MAC; b authenticates SD to OCE only.',
    'with correct MAC (1111–1115); the SD authenticates to the OCE with its CA-KLCC-signed certificate '
    'and receipt at the end of key establishment (1123–1126); a/c are mutual, while b authenticates '
    'SD to OCE only (1127–1128).')
citations = citations.replace('[input-identities.json](input-identities.json)', '[input-identities.json](../input-identities.json)')
citations = citations.replace('(../../boundaries/', '(../../../boundaries/')
citations += ('\n## Revision-2 locator convention\n\n'
    'R1 supersedes only the SCP03/SCP11 locator/actor-scope rows above. Ranges use Python '
    '`str.splitlines()`, which counts form-feed page breaks. The same bytes place SCP03 §6.2.6 at '
    'LF-only 1149–1166 and SCP11 §4.3 at LF-only 1085–1111. The earlier record did not name its '
    'LF-only convention; no original bytes or substantive protocol claim were contradicted. '
    'Exact excerpts, both coordinate systems and UTF-8 byte offsets are in '
    '[source-anchors.json](source-anchors.json). Old citation/register/review bytes remain unchanged.\n')
(HERE / 'citations.md').write_text(citations)

edited = sorted(row['path'] for row in rows if row['decision'] == 'EDIT')
diff = git('diff', '--no-ext-diff', '--binary', '--src-prefix=c/', '--dst-prefix=w/', 'HEAD', '--', *edited)
(HERE / 'product.diff').write_bytes(diff)
git('diff', '--check')
assert all(sha((ROOT / path).read_bytes()) == digest for path, digest in history.items())
save('history-preservation.json', {'old_product_diff_sha256': review['reviewed_product_diff_sha256'],
     'unchanged_editor_report_and_record_files': history, 'all_37_reviewed_product_paths_unchanged': True,
     'note': 'The two added R2 paths were retained, not among the 37 first-edit paths. Original collector not run.'})
save('static-checks.json', {'eligible_paths': 233, 'edited_paths': 39, 'retained_paths': 194,
     'revision_2_changed_source_paths': sorted(TARGETS), 'R2_CSharp_checks': checks,
     'other_231_path_hashes_match_reviewed_233_path_ledger': True, 'all_107_examples_unchanged': True,
     'old_history_files_preserved': len(history), 'git_diff_check': 'PASS',
     'full_successor_diff_sha256': sha(diff), 'independent_R1_R2_rereview': 'PENDING',
     'build_tests_formatter_crypto_native_device_connector_git_mutations_subagents': 'NOT-RUN'})
owned = sorted(p for p in HERE.iterdir() if p.is_file() and p.name != 'output-identities.json')
save('output-identities.json', [{'path': str(p.relative_to(ROOT)), 'sha256': sha(p.read_bytes())} for p in owned])
print(json.dumps({'eligible_paths': 233, 'edited_paths': 39, 'retained_paths': 194,
      'revision_2_CSharp_changes': len(checks), 'unchanged_examples': 107,
      'old_history_files_preserved': len(history), 'full_successor_diff_sha256': sha(diff)}, indent=2))
