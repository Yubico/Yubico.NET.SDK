"""Record this bounded documentation patch; no SDK, provider or device execution."""
import difflib
import hashlib
import json
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[9]
PRIMARY = Path('/home/rupicola/Code/y/Yubico.Yubikit.NET')
RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
INPUT = PRIMARY / RUN / 'boundaries/OpenPgp'
OUT = ROOT / RUN / 'editing/PGP-boundary'

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def sha(data):
    return hashlib.sha256(data).hexdigest()

def normalized(data):
    return data.decode('utf-8-sig').replace('\r\n', '\n')

def load(name):
    return json.loads((INPUT / name).read_text())

def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + '\n')

assert git('rev-parse', 'HEAD').decode().strip() == 'da66f5d01ba594993974ddd26723e8c300c0a454'
changed = git('diff', '--name-only', 'HEAD').decode().splitlines()
product = [p for p in changed if p.startswith('src/')]
assert all(p.startswith('src/OpenPgp/') and Path(p).suffix in ('.md', '.cs') for p in product)
associated = {r['path']: r for r in load('unchanged-paths.json')}
files = load('file-dispositions.json')
assert set(product) <= set(associated)
manifest = []
xml_count = 0
for path in product:
    before = (PRIMARY / path).read_bytes()
    assert normalized(before) == normalized(git('show', 'HEAD:' + path)), path
    after = (ROOT / path).read_bytes()
    a, b = before.decode('utf-8-sig').splitlines(), after.decode('utf-8-sig').splitlines()
    ranges = []
    for tag, i, j, k, l in difflib.SequenceMatcher(a=a, b=b, autojunk=False).get_opcodes():
        if tag != 'equal':
            ranges.append({'before_start': i + 1, 'before_count': j - i,
                           'after_start': k + 1, 'after_count': l - k})
            if path.endswith('.cs'):
                assert all(line.lstrip().startswith('//') for line in a[i:j] + b[k:l]), (path, ranges[-1])
    if path.endswith('.cs'):
        # All changed source lines must be standalone line comments. Comparing the remaining
        # full lines also preserves strings, imports, directives, declarations and assertions.
        assert [x for x in a if not x.lstrip().startswith('//')] == [x for x in b if not x.lstrip().startswith('//')]
        for block in re.findall(r'(?:^[ \t]*///[^\n]*(?:\n|$))+', after.decode('utf-8-sig'), re.M):
            ET.fromstring('<root>' + '\n'.join(re.sub(r'^\s*/// ?', '', x) for x in block.splitlines()) + '</root>')
            xml_count += 1
    manifest.append({'path': path, 'before_sha256': sha(before), 'after_sha256': sha(after),
                     'before_ends_in_newline': before.endswith(b'\n'), 'after_ends_in_newline': after.endswith(b'\n'),
                     'changed_ranges': ranges, 'candidates': associated[path].get('candidates', [])})
save('changed-files.json', manifest)

decisions = []
for row in files:
    path = row['path']
    before = (PRIMARY / path).read_bytes()
    assert sha(before) == row['source_sha256'], path
    assert normalized(before) == normalized(git('show', 'HEAD:' + path)), path
    after = (ROOT / path).read_bytes()
    assert (path in product) == (normalized(before) != normalized(after)), path
    input_source_sha256 = sha(before)
    if path not in product:
        # Git checkout policy may choose different BOM/CRLF bytes in an otherwise unchanged worktree.
        # These retained paths were never written; record their local raw identity separately.
        before = after
    decisions.append({'path': path, 'candidates': row.get('candidates', []),
        'eligible': Path(path).suffix in ('.md', '.cs'), 'decision': 'EDIT' if path in product else 'RETAIN',
        'reason': ('Source-supported documentation facet only; see changed ranges and candidate-decisions.md.'
                   if path in product else
                   'Retain the existing contract/context; no executable, configuration or snapshot changes authorized. '
                   + row.get('observation', row.get('unchanged_reason', 'No candidate-associated correction.'))),
        'before_sha256': sha(before), 'after_sha256': sha(after),
        'primary_input_source_sha256': input_source_sha256,
        'input_file_disposition': str(RUN / 'boundaries/OpenPgp/file-dispositions.json')})
save('path-decisions.json', decisions)

handoffs = []
for path, row in associated.items():
    if path.startswith('src/OpenPgp/'):
        continue
    if path.startswith('docs/migration/'):
        owner = 'Coordinator: assign migration guide owner; outside current SHARED path grant'
    elif path.endswith('.md'):
        owner = 'SHARED narrative editor: /home/rupicola/Code/y/yubikit-core-docs-edit-guides'
    elif '/Credentials/' in path or '/Utilities/Tlv' in path:
        owner = 'Core C source editor; C001-C006/C017 as applicable'
    elif '/Devices/Feature' in path or '/Devices/FirmwareVersion' in path:
        owner = 'Core A metadata source owner, coordinated with B5 effective-version owner'
    elif path.startswith('src/Tests.Shared/'):
        owner = 'Coordinator: Tests.Shared source owner; B6/B1 recorder-example referral'
    elif '/Transports/' in path:
        owner = 'B1/B4 transport editor: /home/rupicola/Code/y/yubikit-core-docs-edit-transports'
    else:
        owner = 'B5/B6 session/protocol editor: /home/rupicola/Code/y/yubikit-core-docs-edit-protocols'
    before = (ROOT / path).read_bytes()
    assert normalized(before) == normalized(git('show', 'HEAD:' + path)), path
    handoffs.append({'path': path, 'decision': 'REFER-OUTSIDE-OWNER; READ-ONLY', 'owner': owner,
                     'candidates': row.get('candidates', []), 'reopening_trigger': row['reopening_trigger'],
                     'applicability': row['reason'], 'unchanged_sha256': sha(before)})
save('outside-owner-handoffs.json', handoffs)

examples = []
for row in load('example-dispositions.json'):
    path = row['path']
    before = git('show', 'HEAD:' + path).decode('utf-8-sig')
    after = (ROOT / path).read_text(encoding='utf-8-sig')
    fences = lambda text: list(re.finditer(r'^```([^\n]*)\n(.*?)^```\s*$', text, re.M | re.S))
    old_fences, new_fences = fences(before), fences(after)
    assert len(old_fences) == len(new_fences)
    old = next(m for m in old_fences if before[:m.start()].count('\n') + 1 == row['first_line'])
    new = new_fences[old_fences.index(old)]
    assert old.group(2) == new.group(2)
    examples.append({'path': path, 'before_first_line': row['first_line'],
        'after_first_line': after[:new.start()].count('\n') + 1,
        'classification': row['classification'], 'body_sha256': sha(new.group(2).encode()),
        'decision': 'RETAIN-EXACT-BODY', 'scope': row['resource_scope'],
        'compilation': 'NOT RUN; historical results retain their original scope', 'runtime': 'NOT RUN'})
# XML protocol fragments are outside the packet's 17 Markdown fences; account for them explicitly.
for path in ['src/OpenPgp/src/Kdf.cs', 'src/OpenPgp/src/PrivateKeyTemplate.cs']:
    before = git('show', 'HEAD:' + path).decode('utf-8-sig')
    after = (ROOT / path).read_text(encoding='utf-8-sig')
    old, new = re.findall(r'<code>(.*?)</code>', before, re.S), re.findall(r'<code>(.*?)</code>', after, re.S)
    assert old == new
    for body in new:
        examples.append({'path': path, 'classification': 'ILLUSTRATIVE-PROTOCOL-FRAGMENT; NOT C#',
            'body_sha256': sha(body.encode()), 'decision': 'RETAIN-EXACT-BODY', 'compilation': 'NOT APPLICABLE'})
save('example-inventory.json', examples)

links = []
for path in product:
    if not path.endswith('.md'):
        continue
    for target in re.findall(r'\]\(([^)]+)\)', (ROOT / path).read_text()):
        if '://' in target or target.startswith('#'):
            continue
        local = target.split('#')[0]
        exists = (ROOT / path).parent.joinpath(local).exists()
        assert exists, (path, target)
        links.append({'path': path, 'target': target, 'exists': exists})
save('local-links.json', links)

inputs = [INPUT / p for p in ['candidate-index.json', 'file-dispositions.json', 'unchanged-paths.json',
    'example-dispositions.json', 'source-identities.json', 'sources.md', 'flow-and-verification.md',
    'inherited-dispositions.json', 'documentation-dependencies.json', 'applet-findings-dispositions.json']]
inputs += [PRIMARY / RUN / p for p in ['boundary-OpenPgp.md', 'triage/editing-release.md', 'triage-full-family.md']]
save('input-identities.json', [{'path': str(p), 'sha256': sha(p.read_bytes())} for p in inputs])
save('preserved-provenance.json', {
    'inherited_keys': [r['key'] for r in load('inherited-dispositions.json')],
    'documentation_dependencies_not_new_Core_bugs': [r['key'] for r in load('documentation-dependencies.json')],
    'deferred_engineering_groups': [r['key'] for r in load('applet-findings-dispositions.json')],
    'status': 'Original identities/statuses unchanged; documentation patch closes no engineering or policy facet'})
(OUT / 'product.patch').write_bytes(git('diff', '--binary', 'HEAD', '--', 'src/OpenPgp'))
check = subprocess.run(['git', 'diff', '--check'], cwd=ROOT, capture_output=True, text=True)
assert check.returncode == 0, check.stdout + check.stderr
save('static-verification.json', {'product_files_changed': len(product), 'file_dispositions': len(decisions),
    'outside_owner_referrals': len(handoffs), 'Markdown_example_bodies_preserved': len(examples) - 2,
    'XML_protocol_fragments_preserved': 2, 'XML_blocks_well_formed': xml_count,
    'local_links_resolve': len(links), 'cs_changed_lines_only_standalone_comments': True,
    'cs_noncomment_lines_exactly_preserved_ignoring_final_newline': True,
    'git_diff_check': 'PASS', 'build_test_formatter_crypto_native_device': 'NOT RUN',
    'independent_review_compiler_binding_runtime_qualification': 'PENDING; no credit claimed'})
print(json.dumps({'changed': len(product), 'dispositions': len(decisions), 'referrals': len(handoffs),
                  'examples': len(examples), 'xml_blocks': xml_count, 'links': len(links)}))
