"""Final static successor receipt; never runs either frozen earlier assembler."""
import copy
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
R2 = INITIAL / 'revision-2'
OUT = INITIAL / 'revision-3'
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

def identity(p):
    return dict(path=str(p.relative_to(ROOT)), bytes=p.stat().st_size, sha256=sha(p.read_bytes()))

def verify_outputs(directory):
    entries = json.loads((directory / 'output-identities.json').read_text())
    for entry in entries:
        assert sha((ROOT / entry['path']).read_bytes()) == entry['sha256'], entry['path']
    return [*entries, identity(directory / 'output-identities.json')]

history1 = verify_outputs(INITIAL)
history2 = verify_outputs(R2)
previous = json.loads((R2 / 'full-successor-path-decisions.json').read_text())
rows = {f['path']: copy.deepcopy(f) for f in previous}
assert len(rows) == 164
decisions = {
    'scripts/toolchain/TestTargets.cs': dict(
        ids=['CORE-A-C018', 'FPT-018'],
        reason='EDIT released D facet: describe actual per-term native arguments and same-kind positive flag restriction; remove duplicate/stale wildcard-combination and unqualified runner boolean claims. Pinned repeated-switch execution remains unrun. No filter code, help/error text, expression, assertion or policy changed.',
        source_anchors=['TranslateToMtpFilter at base lines158-256; branches171-187,190-220,224-256',
                        'investigation-A.md:320-331, CORE-A-C018'],
        current_comment='TranslateToMtpFilter explanatory block immediately before the method'),
    'scripts/toolchain/Results.cs': dict(
        ids=['CORE-B4-C008', 'FPT-055'],
        reason='EDIT released D facet: EnsureTestsRan checks classified failed/all-NoTestsExecuted outcomes, not actual nonzero parsed execution counts. Document empty/mixed/unknown-count limits without strengthening the gate or endorsing them as a new policy.',
        source_anchors=['Gates.EnsureTestsRan at base lines97-109; actual predicates103-108',
                        'scripts/toolchain/TestTargets.cs at base lines97-117, classifier',
                        'investigation/B4/file-evidence.md:177-191; investigation-B4.md:131-138'],
        current_comment='Gates.EnsureTestsRan summary and remarks'),
    'scripts/toolchain/Process.cs': dict(
        ids=['CORE-B4-C007', 'CORE-B4-C008', 'FPT-054', 'FPT-055'],
        reason='EDIT released D facet: scope Exec summary to processes dispatched through it and condition full step logging on availability. Mask covers supplied secrets in shown commands, not process output or RunLog.Start stored argv. Dotnet.Capture and RunLog.Git launch directly. No process/masking/logging/drain implementation or credential value changed.',
        source_anchors=['Exec.Run/Mask at base lines29-85,114-115; Dotnet.Capture130-150',
                        'scripts/toolchain/RunLog.cs:55-64,451-467',
                        'investigation/B4/file-evidence.md:177-191; investigation-B4.md:125-138'],
        current_comment='Exec type summary and remarks')
}
targets = set(decisions)
checks = []
comment_delta = []
for path, row in rows.items():
    current = (ROOT / path).read_bytes()
    if path not in targets:
        assert sha(current) == row['current_raw_sha256'], ('unexpected revision3 product change', path)
        continue
    before = git('show', BASE + ':' + path)
    assert sha(current) != row['current_raw_sha256'], ('authorized comment not edited', path)
    def protected(b):
        return [line for line in norm(b).splitlines() if not line.lstrip().startswith('//')]
    assert protected(before) == protected(current), ('noncomment line changed', path)
    count = 0
    for block in re.findall(r'(?:^[ \t]*///[^\n]*\n?)+', norm(current), flags=re.M):
        ET.fromstring('<root>' + re.sub(r'^[ \t]*/// ?', '', block, flags=re.M) + '</root>')
        count += 1
    old_reason = row['reason']
    row.update(decision='EDIT', reason=decisions[path]['reason'],
               revision3_prior_disposition=dict(decision=row['decision'], reason=old_reason,
                   current_raw_sha256=row['current_raw_sha256']),
               revision3_exact_anchors=decisions[path]['source_anchors'],
               current_raw_sha256=sha(current), current_lf_sha256=sha(norm(current).encode()),
               current_final_newline=current.endswith(b'\n'), current_utf8_bom=current.startswith(b'\xef\xbb\xbf'))
    comment_delta.append(dict(path=path, **decisions[path],
        base_git_blob=git('rev-parse', BASE + ':' + path).decode().strip(),
        base_sha256=sha(before), current_raw_sha256=sha(current),
        base_full_line_comments=[dict(line=i,text=line) for i,line in enumerate(norm(before).splitlines(),1) if line.lstrip().startswith('//')],
        current_full_line_comments=[dict(line=i,text=line) for i,line in enumerate(norm(current).splitlines(),1) if line.lstrip().startswith('//')]))
    checks.append(dict(path=path, protected_lines='exactly equal including blanks, inline comments, directives, imports, declarations, string literals, guards and assertions',
        xml_blocks_well_formed=count, semantic_binding='NOT-RUN'))

prior_edits = set(json.loads((R2 / 'static-preservation.json').read_text())['full_successor_product_edits'])
assert len(prior_edits) == 32
changed = git('diff','--name-only',BASE).decode().splitlines()
product = sorted(p for p in changed if not p.startswith(str(RUN) + '/'))
assert set(product) == prior_edits | targets
assert len(product) == len(prior_edits) + len(targets)
assert len(product) == 35
allow = json.loads((R2 / 'expanded-allowlist.json').read_text())
full_allow = sorted(set(allow['full_successor_allowlist']) | targets)
assert set(product) <= set(full_allow)
assert git('rev-parse','HEAD').decode().strip() == BASE
patch = git('diff','--binary','--src-prefix=a/','--dst-prefix=b/',BASE,'--',*product)
delta = git('diff','--src-prefix=a/','--dst-prefix=b/',BASE,'--',*sorted(targets))
(OUT / 'final-successor-source.patch').write_bytes(patch)
(OUT / 'comment-delta.patch').write_bytes(delta)
subprocess.run(['git','--no-optional-locks','diff','--check'],cwd=ROOT,check=True,capture_output=True)
save('history-preservation.json',dict(initial_records=history1,revision2_records=history2,
    initial_product_manifest=identity(INITIAL/'path-decisions.json'),
    revision2_product_manifest=identity(R2/'full-successor-path-decisions.json'),
    prior_32_product_files_unchanged=sorted(prior_edits),
    all_other_prior_manifest_rows_unchanged=len(rows)-len(targets),
    result='Both frozen output sets and all earlier 32 edited product hashes preserved; only three specifically released previously-held comment paths changed.'))
save('allowlist.json',dict(base=BASE,source_pin=PIN,
    revision2_allowlist=identity(R2/'expanded-allowlist.json'),explicit_revision3_paths=sorted(targets),
    full_final_allowlist=full_allow,primary_26_still_derived_from_packets=allow['packet_selections'],
    no_validation_profile_changes=True,write_limit='Actual comments/XML only in the three added script paths; every earlier ownership limit remains.'))
save('path-decisions.json',[rows[p] for p in sorted(rows)])
save('comment-delta.json',comment_delta)
save('candidate-decisions.json',dict(
    inherited_final_revision2=identity(R2/'full-successor-candidate-decisions.json'),
    updates=[dict(path=p,ids=decisions[p]['ids'],documentation='EDIT, authorized D-comment scope hold discharged; independent review pending',
        reason=decisions[p]['reason'],source_anchors=decisions[p]['source_anchors'],
        original_local_proposal_links=[dict(local_id=c['local_id'],proposal_id=c['proposal_id'],facets=c['facets']) for c in rows[p]['candidates']],
        remaining='M/P/E/runtime/producer/physical facets unchanged; no accepted IDs allocated') for p in sorted(targets)]))
examples2=json.loads((R2/'example-bodies.json').read_text())
for path in targets:
    xml_comments='\n'.join(line for line in norm((ROOT/path).read_bytes()).splitlines() if line.lstrip().startswith('///'))
    assert not re.search(r'<code(?:\s[^>]*)?>',xml_comments), path
save('citations-and-examples.json',dict(
    source_pin=PIN,earlier_citation_notes=[identity(INITIAL/'citations-and-handoff.md'),identity(R2/'edit-external-revision-2.md')],
    exact_revision3_anchors={p:d['source_anchors'] for p,d in decisions.items()},
    source_authority='Pinned implementation and completed family reports, not a new normative requirement or independent citation acceptance.',
    runner_qualification='CORE-A-C018/A-E05: exact xUnit3.0.0 same-kind selection remains unrun; no AND/OR result inferred from emitted flags.',
    example_inventory=identity(R2/'example-bodies.json'),
    initial_example_bodies=len(examples2['initial_bodies_preserved']),retained_extra_bodies=len(examples2['added_primary_bodies']),
    new_example_bodies=0,all_example_compilation='NOT-RUN',
    no_new_protocol_citation=True))
referrals=json.loads((R2/'remaining-referrals.json').read_text())
assert {f['path'] for f in referrals['comment_scope_holds']} == targets
referrals['comment_scope_holds']=[]
referrals['discharged_comment_scope_holds']=[dict(path=p,decision=decisions[p]['reason']) for p in sorted(targets)]
referrals['validation_identity_owner']='Coordinator reconciles accepted final source/input identity after independent full-batch review and the current pilot run. No validation tooling/profile was touched here.'
save('remaining-referrals.json',referrals)
save('static-preservation.json',dict(base=BASE,head=BASE,source_pin=PIN,
    actual_previous_product_count=len(prior_edits),actual_revision3_delta_count=len(targets),
    actual_final_product_count=len(product),final_product_paths=product,path_manifest_count=len(rows),
    full_patch_sha256=sha(patch),comment_delta_sha256=sha(delta),checks=checks,
    earlier_32_raw_product_hashes_and_all_history='unchanged',git_diff_check='PASS',
    no_validation_tooling_or_profiles_changed=True,
    limits='Exact non-full-line-comment comparison and XML well-formedness, not a Roslyn semantic binding/build/test/formatter/native/device/crypto/provider run or independent content review. No other worktree product-base comparison.'))
print(json.dumps(dict(final_product_files=len(product),revision3_comment_files=len(targets),
    final_manifest_paths=len(rows),initial_records_preserved=len(history1),revision2_records_preserved=len(history2),
    remaining_comment_scope_holds=0,full_patch_sha256=sha(patch)),indent=2))
