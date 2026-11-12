"""Revision-2 static receipt. Never imports/runs the frozen initial receipt generator."""
import ast
import collections
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path('/home/rupicola/Code/y/yubikit-core-docs-edit-external')
RUN = Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
INITIAL = ROOT / RUN / 'editing/external'
OUT = INITIAL / 'revision-2'
BASE = 'da66f5d01ba594993974ddd26723e8c300c0a454'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
os.environ['GIT_OPTIONAL_LOCKS'] = '0'

def sha(b):
    return hashlib.sha256(b).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT, stderr=subprocess.PIPE)

def norm(b):
    return b.decode('utf-8-sig').replace('\r\n', '\n')

def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

initial = json.loads((INITIAL / 'path-decisions.json').read_text())
initial_changed = {f['path'] for f in initial if f['decision'] == 'EDIT'}
assert len(initial_changed) == 19
for f in initial:
    if f['path'] in initial_changed:
        assert sha((ROOT / f['path']).read_bytes()) == f['current_raw_sha256'], f['path']
history = json.loads((INITIAL / 'output-identities.json').read_text())
for entry in history:
    assert sha((ROOT / entry['path']).read_bytes()) == entry['sha256'], entry['path']
history.append(dict(path=str((INITIAL / 'output-identities.json').relative_to(ROOT)),
    bytes=(INITIAL / 'output-identities.json').stat().st_size,
    sha256=sha((INITIAL / 'output-identities.json').read_bytes())))

packets = {}
primary = set()
support = set()
support_extensions = {'.py', '.sh', '.yml', '.yaml', '.csproj', '.props', '.targets'}
for name in ['B2-linux-lifetimes', 'B3-macos-lifetimes', 'B4-windows-support']:
    path = ROOT / RUN / 'inventory/packets' / (name + '.json')
    packet = json.loads(path.read_text())
    hits = {p for p in packet['paths'] if p.startswith(('verification/', 'benchmarks/'))
            and Path(p).suffix in {'.cs', '.md'}}
    primary.update(hits)
    support.update(p for p in packet['paths'] if p.startswith(('verification/', 'benchmarks/'))
                   and Path(p).suffix in support_extensions)
    packets[name] = dict(path=str(path.relative_to(ROOT)), sha256=sha(path.read_bytes()),
                         count=len(hits), selected_paths=sorted(hits))
assert len(primary) == 26, len(primary)
previous_37 = {f['path'] for f in initial if f['decision'] == 'HOLD-READONLY-FORMAT-OR-SCRIPT'}
assert len(previous_37) == 37
support.update(p for p in previous_37 if Path(p).suffix in support_extensions)
new_allowlist = primary | support
initial_allowlist = set(json.loads((INITIAL / 'allowlist.json').read_text())['active_product_allowlist'])
expanded_allowlist = initial_allowlist | new_allowlist
notes = json.loads((OUT / 'semantic-decisions.json').read_text())
assert set(notes) == primary | support | previous_37, (set(notes) ^ (primary | support | previous_37))
mapping = json.loads((ROOT / RUN / 'triage/full-family/map.json').read_text())
associations = collections.defaultdict(list)
for group in mapping['groups']:
    for local in group['local_records']:
        anchors = [dict(unit_id=ref['unit_id'], **mapping['evidence_units'][ref['unit_id']])
                   for ref in local['evidence_unit_refs']]
        paths = set(local['documentation_surface_refs']) | {a['path'] for a in anchors}
        for path in paths & set(notes):
            associations[path].append(dict(local_id=local['local_id'], proposal_id=group['proposal_id'],
                facets=group['facets'], proposal=group['proposal_and_rationale'],
                reopening=group['implementation_reopen_trigger_or_needed_probe'],
                exact_source_anchors=anchors, original_definition=local['original_definition']))

files = []
checks = []
for path in sorted(notes):
    before = git('show', BASE + ':' + path)
    current = (ROOT / path).read_bytes()
    changed = norm(before) != norm(current)
    assert not changed or path in new_allowlist, path
    reason = notes[path]
    files.append(dict(path=path, population='PRIMARY-26' if path in primary else 'INITIAL-37' if path in previous_37 else 'PRIMARY-SUPPORT',
        decision='EDIT' if changed else 'RETAIN' if not reason.startswith('HOLD') else 'HOLD',
        reason=reason, candidates=associations[path], baseline_commit=BASE, source_pin=PIN,
        source_anchor=dict(path=path, first_line=1, last_line=len(norm(before).splitlines()),
            git_blob=git('rev-parse', PIN + ':' + path).decode().strip(), sha256=sha(git('show', PIN + ':' + path))),
        base_git_blob=git('rev-parse', BASE + ':' + path).decode().strip(),
        base_git_sha256=sha(before), current_raw_sha256=sha(current),
        base_lf_sha256=sha(norm(before).encode()), current_lf_sha256=sha(norm(current).encode()),
        base_final_newline=before.endswith(b'\n'), current_final_newline=current.endswith(b'\n'),
        base_utf8_bom=before.startswith(b'\xef\xbb\xbf'), current_utf8_bom=current.startswith(b'\xef\xbb\xbf')))
    if not changed:
        # A retained raw checkout may differ from Git only through its established line endings.
        checks.append(dict(path=path, check='base/current LF-normalized bytes equal; no revision-2 semantic change'))
        continue
    if path.endswith('.cs'):
        def noncomments(content):
            return [line for line in content.splitlines() if not line.lstrip().startswith('//')]
        assert noncomments(norm(before)) == noncomments(norm(current)), ('non-comment change', path)
        count = 0
        for block in re.findall(r'(?:^[ \t]*///[^\n]*\n?)+', norm(current), flags=re.M):
            ET.fromstring('<root>' + re.sub(r'^[ \t]*/// ?', '', block, flags=re.M) + '</root>')
            count += 1
        checks.append(dict(path=path, check='exact non-full-line-comment lines including blanks equal', xml_blocks=count))
    elif Path(path).suffix in {'.csproj', '.props', '.targets'}:
        a = re.sub(r'<!--.*?-->', '', norm(before), flags=re.S)
        b = re.sub(r'<!--.*?-->', '', norm(current), flags=re.S)
        assert a == b, ('outside XML comment changed', path)
        assert ET.tostring(ET.fromstring(before)) == ET.tostring(ET.fromstring(current)), path
        checks.append(dict(path=path, check='exact text outside XML comments and parsed element/attribute/text structure equal'))
    elif path.endswith('.py'):
        assert ast.dump(ast.parse(norm(before)), include_attributes=False) == ast.dump(ast.parse(norm(current)), include_attributes=False)
        def protected(content):
            return [l for l in content.splitlines() if not (l.lstrip().startswith('#') and not l.startswith('#!'))]
        assert protected(norm(before)) == protected(norm(current)), path
        checks.append(dict(path=path, check='AST including docstrings, shebang and non-comment lines equal'))
    else:
        # No shell/YAML edits selected. Do not claim a parser check for a path never changed.
        raise AssertionError(('unexpected additional format edit', path))

changed = git('diff', '--name-only', BASE).decode().splitlines()
product_changed = sorted(p for p in changed if not p.startswith(str(RUN) + '/'))
assert set(product_changed) <= expanded_allowlist, set(product_changed) - expanded_allowlist
assert set(product_changed) - initial_changed <= new_allowlist
assert git('rev-parse', 'HEAD').decode().strip() == BASE
patch = git('diff', '--binary', '--src-prefix=a/', '--dst-prefix=b/', BASE, '--', *product_changed)
(OUT / 'successor-source.patch').write_bytes(patch)
subprocess.run(['git', '--no-optional-locks', 'diff', '--check'], cwd=ROOT, check=True, capture_output=True)
save('history-preservation.json', dict(initial_records=history,
    initial_19_product_hashes=[f for f in initial if f['path'] in initial_changed],
    result='All initial output record hashes and all initial 19 raw product hashes unchanged. Initial generator not run.'))
save('expanded-allowlist.json', dict(base=BASE, source_pin=PIN, packet_selections=packets,
    primary_union_count=len(primary), primary_paths=sorted(primary),
    additional_comment_formats=sorted(support_extensions), additional_support_paths=sorted(support),
    previous_37=sorted(previous_37), added_allowlist=sorted(new_allowlist),
    full_successor_allowlist=sorted(expanded_allowlist),
    scope_note='Only allowed comments/XML guidance; scripts/toolchain/*.cs retain explicit D facets pending format-scope clarification. No initial guide/module ownership expanded.'))
save('path-decisions.json', files)
merged = {f['path']: f for f in initial}
merged.update({f['path']: f for f in files})
for path, row in merged.items():
    assert sha((ROOT / path).read_bytes()) == row['current_raw_sha256'], ('stale successor hash', path)
save('full-successor-path-decisions.json', [merged[p] for p in sorted(merged)])
save('candidate-decisions.json', [dict(path=f['path'], decision=f['decision'], reason=f['reason'],
    candidates=f['candidates']) for f in files if f['candidates']])
save('full-successor-candidate-decisions.json', dict(
    initial_local_records=json.loads((INITIAL / 'candidate-decisions.json').read_text()),
    initial_supplemental_dependencies=json.loads((INITIAL / 'supplemental-dependencies.json').read_text()),
    revision2_path_facets=[dict(path=f['path'], decision=f['decision'], reason=f['reason'], candidates=f['candidates'])
        for f in files if f['candidates']],
    rule='Exact initial local/proposal identities preserved; successor path facets extend editing scope without accepted-ID allocation or runtime closure.'))
save('remaining-referrals.json', dict(
    comment_scope_holds=[f for f in files if f['reason'].startswith('HOLD-D-COMMENT-SCOPE')],
    literal_data_holds=[dict(path=f['path'], reason=f['reason'], candidates=f['candidates'])
        for f in files if f['reason'].startswith(('HOLD-LITERAL-DATA', 'HOLD-CONFIGURATION', 'HOLD-JSON-DATA'))],
    resolved_ownership_gap='All 26 primary paths have explicit D edit/retain/historical decisions. They are no longer outside-owner referrals solely because they were absent from shared-sections.',
    retained_qualifications=[
        'Initial complete-argv capture/redaction, test-gate/selection and harness changes remain M/P/E, not silently fixed.',
        'SCP/APDU phase/profile/hidden-copy qualification remains with completed B5/B6 and applet-specific records; no new wire result.',
        'Source package/deployed-file hashes do not identify installed native producer, loaded path or headers; no all-platform/native acceptance.',
        'Windows CCID/HID future delta remains UNREADY/UNREVIEWED; no unfinished worktree was inspected.',
        'PIV physical touch/hotplug/touch-dependent cancellation remains user-deferred.',
        'Historical serial9681620 cause remains unexplained; serial125 setup6A88 precedes EXTERNAL AUTHENTICATE/RESET.',
        'Initial protected CLI help/output mismatches and YAML DOC-002 parse issue remain literal/runtime work.']))
input_paths = [ROOT / 'CLAUDE.md', ROOT / RUN / 'triage/editing-release.md',
               ROOT / RUN / 'triage/full-family/map.json']
primary_root = Path('/home/rupicola/Code/y/Yubico.Yubikit.NET')
for family in ['B2','B3','B4']:
    input_paths.extend([primary_root / RUN / f'investigation-{family}.md',
        primary_root / RUN / 'investigation' / family / 'candidates.md',
        primary_root / RUN / 'investigation' / family / 'file-evidence.md'])
for worktree, relative in [('transports','editing/transports/followups.json'),
                          ('transports','edit-transports.md'),
                          ('protocols','editing/protocols/outside-owner-handoffs.json')]:
    input_paths.append(ROOT.parent / ('yubikit-core-docs-edit-' + worktree) / RUN / relative)
save('input-identities.json', [dict(path=str(p), bytes=p.stat().st_size, sha256=sha(p.read_bytes()),
    scope='Read-only completed report/referral/instruction, not product-base comparison or independent acceptance') for p in input_paths])
initial_examples = json.loads((INITIAL / 'example-bodies.json').read_text())
extra_examples = []
for path in sorted(primary):
    for version, data in [('base', git('show', BASE + ':' + path)), ('current', (ROOT / path).read_bytes())]:
        content = norm(data)
        if path.endswith('.md'):
            for match in re.finditer(r'^```([^\n]*)\n(.*?)^```\s*$', content, flags=re.M | re.S):
                extra_examples.append(dict(path=path, version=version, language=match[1], body=match[2],
                    first_line=content.count('\n',0,match.start())+1, body_sha256=sha(match[2].encode()),
                    classification='Retained historical macOS compatibility command recipe with exact artifact prerequisites, not current all-platform qualification',
                    compiler_status='NOT-RUN'))
        else:
            assert not re.search(r'<code(?:\s[^>]*)?>', content), ('classify additional XML body', path)
save('example-bodies.json', dict(initial_history_reference='../example-bodies.json',
    initial_bodies_preserved=initial_examples, added_primary_bodies=extra_examples,
    new_consumer_bodies_introduced=0, compiler_status='No compilation or runtime execution'))
save('static-preservation.json', dict(base=BASE, head=BASE, source_pin=PIN,
    primary_26_dispositions=dict(collections.Counter(f['decision'] for f in files if f['path'] in primary)),
    previous_37_dispositions=dict(collections.Counter(f['decision'] for f in files if f['path'] in previous_37)),
    revision_2_edits=sorted(set(product_changed)-initial_changed), full_successor_product_edits=product_changed,
    full_successor_patch_sha256=sha(patch), checks=checks, git_diff_check='PASS',
    initial_history_and_product_preserved=True,
    limits='Exact comment-only/normalized-text/XML structure checks; no SDK build/test/formatter/native/device/crypto/provider execution, Roslyn semantic binding, or independent review. Other worktrees never compared to raw product base.'))
print(json.dumps(dict(primary_count=len(primary), primary_decisions=collections.Counter(f['decision'] for f in files if f['path'] in primary),
    reconciled_37=len(previous_37), support_count=len(support), revision2_edits=len(set(product_changed)-initial_changed),
    full_successor_product_edits=len(product_changed), patch_sha256=sha(patch)), indent=2))
