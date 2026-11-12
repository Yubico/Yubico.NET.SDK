#!/usr/bin/env python3
"""Capture this bounded edit's identities/joins. No SDK code or source generator executes.

Reuses the inventory's Pygments lexer approach, plus the stronger exact non-comment-line
comparison available because every product edit is a standalone // or /// comment line.
Only generated JSON receipts in this directory are written; product edits use patch.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET
from pygments.lexers import CSharpLexer
from pygments.token import Comment

ROOT = Path.cwd()
RUN = ROOT / 'docs/research/documentation-audit/modules/core/pass-1/core-001'
OUT = RUN / 'editing/C'
BASE = 'da66f5d01ba594993974ddd26723e8c300c0a454'
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
ENV = dict(os.environ, GIT_OPTIONAL_LOCKS='0')

def git(*args):
    return subprocess.check_output(['git', *args], env=ENV)

def load(p):
    return json.loads((RUN / p).read_text())

def sha(b):
    return hashlib.sha256(b).hexdigest()

def emit(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

def text(b):
    return b.decode('utf-8-sig').replace('\r\n', '\n')

def noncomments(t):
    return [l for l in t.splitlines() if not l.lstrip().startswith('//')]

def tokens(t):
    return [(str(k), v) for _, k, v in CSharpLexer().get_tokens_unprocessed(t)
            if k not in Comment and not v.isspace()]

decisions = load('editing/C/decisions.json')
packet = set(load('inventory/packets/C.json')['paths'])
old_rows = load('investigation/C/unchanged-paths.json')
groups = load('triage/full-family/map.json')['groups']
proposal = {local: g for g in groups for local in g['local_ids'] if local.startswith('CORE-C-')}
assert git('rev-parse', 'HEAD').decode().strip() == BASE
changed = set(git('diff', '--name-only', BASE).decode().splitlines())
untracked = set(git('ls-files', '--others', '--exclude-standard').decode().splitlines())
product = {p for p in changed if p.endswith('.cs')}
assert product <= packet
assert all(p in packet or p.startswith(str(OUT.relative_to(ROOT)) + '/') or
           p == str((RUN / 'edit-C.md').relative_to(ROOT)) for p in changed)
assert all(p.startswith(str(OUT.relative_to(ROOT)) + '/') or
           p == str((RUN / 'edit-C.md').relative_to(ROOT)) for p in untracked)
assert not git('diff', '--cached', '--name-only')
associations = {}
for row in old_rows:
    associations.setdefault(row['path'], set()).add(row['candidate_id'])
for p, ids in decisions['additional_primary_associations'].items():
    associations.setdefault(p, set()).update(ids)

file_rows = []
examples = []
for p in sorted(packet):
    before = git('show', BASE + ':' + p)
    pinned = git('show', PIN + ':' + p)
    after = (ROOT / p).read_bytes()
    a, b = text(before), text(after)
    assert text(pinned) == a, p
    assert noncomments(a) == noncomments(b), p
    assert tokens(a) == tokens(b), p
    for m in re.finditer(r'(?m)(?:^[ \t]*///[^\n]*\n?)+', b):
        xml = '\n'.join(re.sub(r'^\s*/// ?', '', l) for l in m[0].splitlines())
        ET.fromstring('<root>' + xml + '</root>')
    ids = sorted(associations.get(p, set()))
    reason = decisions['retained_primary_reasons'].get(p)
    if p in product:
        action = 'EDIT'
        reason = 'Documentation-only edits for ' + ', '.join(ids) + '; see candidate_actions and exact diff. Other correct local context retained.'
    else:
        action = 'RETAIN'
        reason = reason or 'No released local correction requires changing this primary file; retain correct current documentation and executable source without style-only churn.'
    diff = git('diff', '--unified=0', BASE, '--', p).decode()
    ranges = [dict(base_start=int(m[1]), base_count=int(m[2] or 1),
                   current_start=int(m[3]), current_count=int(m[4] or 1))
              for m in re.finditer(r'^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@', diff, re.M)]
    file_rows.append(dict(path=p, action=action, reason=reason, candidates=ids,
                          proposals=[proposal[i]['proposal_id'] for i in ids],
                          base_sha256=sha(before), current_sha256=sha(after),
                          product_pin_sha256=sha(pinned), normalized_pin_matches=True,
                          changed_documentation_ranges=ranges,
                          exact_noncomment_lines_preserved=True, lexical_tokens_preserved=True))
    for version, t in [('base', a), ('current', b)]:
        for n, m in enumerate(re.finditer(r'<code(?:\s[^>]*)?>(.*?)</code>', t, re.S), 1):
            body = m.group(1)
            first = t.count('\n', 0, m.start()) + 1
            last = t.count('\n', 0, m.end()) + 1
            if p.endswith('RsaFormat.cs'):
                kind = 'Historical commented-out RsaFormat illustration; excluded from active SDK/example compilation; retained without correction or runtime claim'
            elif p.endswith('ICredentialPrompt.cs'):
                kind = 'Complete example class; application-supplied async acquisition delegate; transferred byte array cleared in finally; result owner transfers on success'
            elif p.endswith('ISecureCredentialReader.cs'):
                kind = 'Complete method example; borrowed open IPivSession and token; owned credential using encloses awaited VerifyPinAsync; caller retains session disposal'
            elif p.endswith('IPrivateKeyExtensions.cs'):
                kind = 'Complete method example; borrowed encodedKey; concrete RSAPrivateKey owner using scope; IPrivateKey and cast result are aliases, not disposal owners'
            elif p.endswith('UserPresenceIntent.cs'):
                kind = 'Context fragment; borrowed WebAuthnClient, PublicKeyCredentialRequestOptions and token; complete owned intent using scope; client lifetime stays with caller'
            elif p.endswith('CryptographyProviders.cs'):
                kind = ('Resource-lifetime fragment with complete provider using scope' if 'using (' in body
                        else 'Startup configuration fragment selecting a factory; returned-object ownership is documented separately')
            else:
                kind = 'Context illustration; review before reuse'
            examples.append(dict(path=p, version=version, ordinal=n, first_line=first, last_line=last,
                                 start_character=m.start(1), end_character=m.end(1),
                                 body_sha256=sha(body.encode()), classification=kind if version == 'current' else 'Historical original body; preserved identity only',
                                 compiler_status='NOT-RUN', execution_status='NOT-RUN'))

path_rows = []
for row in old_rows:
    p, local = row['path'], row['candidate_id']
    before = git('show', BASE + ':' + p)
    after = (ROOT / p).read_bytes()
    primary = p in packet
    if not primary:
        assert before == after, p
    retained_facet = decisions['retained_facet_reasons'].get(local + '|' + p)
    if local in ['CORE-C-C019', 'CORE-C-C020']:
        action = 'HOLD-FACET' if local.endswith('019') else 'RETAIN-REFER-OWNER'
    elif retained_facet:
        action = 'RETAIN-FACET'
    else:
        action = 'EDIT' if p in product else 'RETAIN' if primary else 'RETAIN-REFER-OWNER'
    path_rows.append(dict(candidate=local, proposal=proposal[local]['proposal_id'], path=p,
                          owner=row['editor_owner'], action=action, primary=primary,
                          base_sha256=sha(before), current_sha256=sha(after),
                          other_released_facets_edited=p in product,
                          reason=retained_facet or (decisions['candidate_actions'][local] if p in product else
                          decisions['retained_primary_reasons'].get(p, decisions['candidate_actions'][local] +
                          (' Assigned owner must review the affected local passage; this file is read-only here.' if not primary else ' Correct local context is retained.')))))

user_rows = []
for row in load('user-work-baseline.json'):
    p = ROOT / row['path']
    absent = not p.exists()
    if absent:
        p = Path('/home/rupicola/Code/y/Yubico.Yubikit.NET') / row['path']
    b = p.read_bytes()
    assert len(b) == row['bytes'] and sha(b) == row['sha256'], p
    user_rows.append(dict(path=row['path'], isolated_checkout_absent=absent,
                          read_path=str(p), baseline_matches=True, current_sha256=sha(b)))

emit('files.json', file_rows)
emit('path-decisions.json', path_rows)
emit('examples.json', examples)
emit('candidate-map.json', [dict(local=local, proposal=proposal[local]['proposal_id'],
     released_facets=proposal[local]['facets'], action=action,
     primary_paths=[row['path'] for row in file_rows if local in row['candidates']],
     path_decision_rows=[i for i, row in enumerate(path_rows) if row['candidate'] == local],
     inherited_aliases=next(c.get('inherited_aliases', []) for c in load('investigation/C/findings.json')['candidates'] if c['id'] == local))
     for local, action in decisions['candidate_actions'].items()])
emit('static-verification.json', dict(base_head=BASE, product_pin=PIN, primary_files=len(packet),
     changed_primary_files=len(product), retained_primary_files=len(packet-product),
     all_primary_pin_normalized_match=True, noncomment_lines_and_tokens_preserved=True,
     all_primary_xml_blocks_well_formed=True, candidate_path_rows=len(path_rows),
     candidate_paths=len({r['path'] for r in path_rows}), examples=len(examples),
     protected_candidate_dependencies_unchanged=True, staged_changes=False, user_files=user_rows,
     tracked_and_untracked_paths_within_allowlist=True,
     record_inputs=[dict(path=p, sha256=sha((RUN / p).read_bytes())) for p in
        ['triage/editing-release.md', 'triage/full-family/map.json', 'investigation/C/findings.json',
         'investigation/C/triage-clarifications.md', 'investigation/C/sources/final-source-register.md',
         'investigation/C/inherited-dispositions.json', 'investigation/C/unchanged-paths.json',
         'editing/C/decisions.json']],
     archive_check=dict(command='python3 -B docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/check-preserved.py',
        archive_counts=[784,402,534,164,433,477], archive_hashes='PASS', overall_exit=1,
        qualification='Existing checker completed six archive populations, then failed because original user files are absent in the isolated worktree. Separate read-only primary-location checks above match all four Core user baselines.'),
     limits=['Static lexical/line/hash/XML checks are not a C# compiler, API semantic validator, independent review, runtime test or native/provider/device attestation.',
             'No SDK compilation, formatter, tests, crypto/native/device execution, index/ref updates, commit, push or subagents.',
             'Generated receipts only; no central inventory, registry, reports, archives, guides or other module edits.']))
emit('output-identities.json', [dict(path=str(p.relative_to(ROOT)), sha256=sha(p.read_bytes()))
    for p in sorted([*OUT.glob('*.json'), *OUT.glob('*.md'), *OUT.glob('*.py'), RUN / 'edit-C.md'])
    if p.exists() and p.name != 'output-identities.json'])
print(json.dumps(dict(primary_files=len(packet), edited=len(product), retained=len(packet-product),
                     candidate_path_rows=len(path_rows), example_records=len(examples), static_checks='PASS'), indent=2))
