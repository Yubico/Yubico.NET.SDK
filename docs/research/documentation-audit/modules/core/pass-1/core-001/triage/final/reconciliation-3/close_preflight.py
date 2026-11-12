"""Bounded PF-01/PF-02 record response. No Git, SDK or source-body audit."""
import hashlib
import json
import re
from collections import Counter
from pathlib import Path

OUT = Path(__file__).resolve().parent
FINAL = OUT.parent
RUN = FINAL.parents[1]
ROOT = RUN.parents[6]
PIN = 'fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef'
inputs = {}


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def read(path):
    p = Path(path)
    p = p if p.is_absolute() else RUN / p
    raw = p.read_bytes()
    key = str(p.relative_to(RUN)) if p.is_relative_to(RUN) else str(p)
    inputs[key] = sha(raw)
    return raw.decode('utf-8-sig')


def load(path):
    return json.loads(read(path))


def save(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


history = OUT / 'deferred-findings.reconciliation-2.md.snapshot'
if not history.exists():
    history.write_bytes((RUN / 'deferred-findings.md').read_bytes())
prior_paths = sorted(p for p in (RUN / 'triage').rglob('*') if p.is_file() and not p.is_relative_to(OUT))
prior_paths += [RUN / 'triage-preliminary.md', RUN / 'triage-full-family.md',
                RUN / 'validation/closeout-review/preflight.md', RUN / 'validation/closeout-review/input-identities.json']
prior_hashes = {str(p.relative_to(RUN)): sha(p.read_bytes()) for p in prior_paths}
if (OUT / 'history-preservation.json').exists():
    previous = json.loads((OUT / 'history-preservation.json').read_text())
    assert previous['unchanged_records'] == prior_hashes, 'predecessor/preflight bytes changed'
    assert previous['predecessor_handoff_sha256'] == sha(history.read_bytes())
else:
    save('history-preservation.json', {'unchanged_records': prior_hashes,
        'predecessor_handoff': history.name, 'predecessor_handoff_sha256': sha(history.read_bytes()),
        'rule': 'Prior maps, final records, reconciliation-2 (including Astra addendum) and preflight remain byte-identical. Only the current handoff is an authorized successor.'})
r2_manifest = load('triage/final/reconciliation-2/output-identities.json')
assert r2_manifest['files']['deferred-findings.md'] == sha(history.read_bytes())
preflight = read('validation/closeout-review/preflight.md')
read('triage/final/reconciliation-2/validation-state-addendum.md')
queue_text = read('editing/integration/queued-sampling-corrections.md')
current = load('editing/integration/current-products.json')
products = {x['path']: x for x in current['product_files']}
assert len(products) == len(current['product_files']) == 361
current_checks = {p: {'accepted_sha256': row['sha256'], 'current_sha256': sha((ROOT / p).read_bytes())}
                  for p, row in products.items()}
assert all(x['accepted_sha256'] == x['current_sha256'] for x in current_checks.values())
old = load('triage/final/candidate-reconciliation.json')
inherited = load('triage/final/inherited-reconciliation.json')
protected = load('triage/final/protected-dispositions.json')
assert len(old['candidates']) == 185 and len(inherited['rows']) == 100
assert len(inherited['groups_preserved']) == 70 and len(inherited['pgp_documentation_dependencies_preserved']) == 21
assert protected['hsm_retry_message']['state'] == 'RETRACTED-NOT-AN-OPEN-BUG'
inventory = {x['path']: x for x in load('inventory/files.json')}
units = {x['id']: x for x in load('inventory/semantic-units.json')}

# PF-01: completed independent review supersedes only the editor's earlier PENDING field.
worktree = Path('/home/rupicola/Code/y/yubikit-core-docs-edit-transports')
workrun = worktree / RUN.relative_to(ROOT)
directory = workrun / 'editing/transports/sampling-udev-receive'
review = read(directory / 'review.md')
review_ids = load(directory / 'review-identities.json')
disposition = load(directory / 'file-disposition.json')
source = load(directory / 'source-evidence.json')
patch = directory / 'source.patch'
patch_hash = sha(patch.read_bytes())
expected_patch = '9405abcd2f52d438f281b809ef8d4b3f5fbd010cfb3e9d17a51ddc9b1140be86'
before = '676d31c9d87d67a9b481082caed6cb06a91576f3120aa3c336d2dd9ded1f2f29'
after = '27276323b17745eaddd0b16d1030d01304ae6122d49d516795a945620d040445'
target = 'src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs'
uid = 'core-001:unit:82f001f6db22d507'
assert review_ids['decision'] == 'PASS' and review_ids['remaining_mandatory_corrections'] == []
assert patch_hash == expected_patch == review_ids['accepted_patch_sha256']
assert patch.stat().st_size == review_ids['accepted_patch_bytes'] == 1052
assert before == disposition['before_raw_sha256'] == review_ids['before_current_primary_raw_sha256'] == products[target]['sha256']
assert after == disposition['after_raw_sha256'] == review_ids['accepted_after_raw_sha256']
assert sha((ROOT / target).read_bytes()) == before
assert sha((worktree / target).read_bytes()) == after
for key in ('id', 'path', 'source_sha256', 'range_sha256', 'start_character', 'end_character_exclusive', 'first_line', 'last_line'):
    assert disposition['original_selected_unit'][key] == units[uid][key], ('original selected identity', key)
input_checks = {}
for name, digest in review_ids['accepted_input_records_relative_to_sampling_udev_receive'].items():
    actual = sha((directory / name).read_bytes())
    assert actual == digest, ('reviewed input', name)
    input_checks[name] = {'reviewed_sha256': digest, 'actual_sha256': actual}
source_checks = []
for item in source['evidence']:
    path = ROOT / item['path']
    expected = item.get('sha256', item.get('before_sha256'))
    actual = sha(path.read_bytes())
    assert actual == expected, ('retained source identity', item['path'])
    source_checks.append({'path': item['path'], 'expected_sha256': expected, 'current_sha256': actual,
                          'scope': 'Byte identity only; completed reviewer supplied passage/implementation interpretation.'})
pf01 = {'finding': 'PF-01', 'response_state': 'MISSING-OVERLAY-LINK-ADDRESSED; content-PASS-queued-untransferred',
    'sample_finding': 'SAMPLE-B2-003', 'unit': units[uid], 'candidate_links': [{'id': 'CORE-B2-C002',
        'relation': 'Existing Linux mechanism/comment context only; no new engineering group or unchanged-clean credit.'}],
    'target': target, 'patch': str(patch), 'patch_bytes': 1052, 'patch_sha256': patch_hash,
    'review_path': str(directory / 'review.md'), 'review_sha256': inputs[str(directory / 'review.md')],
    'review_text': review, 'review_identities': review_ids,
    'source_evidence_record': str(directory / 'source-evidence.json'), 'source_evidence': source,
    'accepted_before_sha256': before, 'accepted_after_sha256': after,
    'current_primary_sha256': current_checks[target]['current_sha256'],
    'current_worktree_after_sha256': sha((worktree / target).read_bytes()),
    'before_basis': review_ids['before_basis'], 'reviewed_input_hash_checks': input_checks,
    'source_identity_checks': source_checks, 'final_clean_credit': 0,
    'actual_read_credit_added': 0, 'implementation_or_native_outcome_added': False,
    'claim': 'Remove unsupported receive-null cause-frequency ranking; preserve no-event/error possibilities and conservative rescan rationale. No rescan/wake/cancel/drain/release behavior changed.',
    'remaining': 'Reservation, separate later transfer receipt and predecessor map, source/document/profile impact reconciliation, final preservation and sampling quota decisions remain coordinator-owned.'}
save('pf01-sampling-overlay.json', pf01)
r2_findings = load('triage/final/reconciliation-2/sampling-findings.json')
queue = {key: {'state': row['state'], 'patch_sha256': row['patch_sha256'],
               'review_path': row['review_path'], 'original_record': '../reconciliation-2/sampling-findings.json#findings/' + key,
               'final_clean_credit': 0, 'transfer_state': 'QUEUED-UNTRANSFERRED'}
         for key, row in r2_findings['findings'].items() if key in ('SAMPLE-WA-001', 'SAMPLE-PGP-001')}
queue['SAMPLE-B2-003'] = {'state': 'FOCUSED-CONTENT-PASS; queued and untransferred', 'patch_sha256': patch_hash,
    'review_path': str(directory / 'review.md'), 'full_identity_record': 'pf01-sampling-overlay.json',
    'final_clean_credit': 0, 'transfer_state': 'QUEUED-UNTRANSFERRED'}
assert len(queue) == 3 and all(row['patch_sha256'] in queue_text for row in queue.values())
save('queued-corrections.json', {'corrections': queue, 'source_registry': 'editing/integration/queued-sampling-corrections.md',
    'rule': 'Additive to existing accepted batches, not replacements. No primary transfer, new sample read, clean credit or engineering finding in this response.'})

# PF-02: direct completed editor holds/referrals were outside the first path-table join.
# Carry them through explicitly, with a facet-specific coordinator proposal; retain
# source/hold/read status separately rather than self-approving configuration content.
proposals = load('triage/final/reconciliation-3/pf02-proposals.json')
requested = re.findall(r'^\| (CORE-[A-Z0-9]+-[CX][0-9]+) \| `([^`]+)` \|', preflight, re.M)
assert [(x['candidate'], x['path']) for x in proposals] == requested and len(proposals) == 9
cache = {}


def record_rows(path):
    if path not in cache:
        data = load(path)
        cache[path] = data if isinstance(data, list) else data['files']
    return cache[path]


records = []
for n, proposal in enumerate(proposals):
    path = proposal['path']
    cid = proposal['candidate']
    assert path in old['candidates'][cid]['documentation_paths']
    source_rows = [(i, row) for i, row in enumerate(record_rows(proposal['source_record'])) if row.get('path') == path]
    finished = [(i, row) for i, row in enumerate(record_rows(proposal['finished_decision_record'])) if row.get('path') == path]
    assert source_rows and len(finished) == 1, (path, len(source_rows), len(finished))
    i, row = finished[0]
    assert row['decision'] in ('hold-for-exact-owner', 'HOLD-OUTSIDE-EDIT-SCOPE')
    latest_review = 'editing/SHARED/revision-2/review.md' if proposal['finished_decision_record'].startswith('editing/SHARED/') else 'editing/applet-boundaries/revision-2/review.md'
    assert 'PASS' in read(latest_review)
    raw_hash = sha((ROOT / path).read_bytes())
    assert raw_hash == inventory[path]['sha256'], ('unexpected change of retained surface', path)
    records.append({'response_index': n, 'candidate': cid, 'run_qualified_facet': f'core-001:{cid}#documentation',
        'path': path, 'existing_completed_editor_decision': {'record': proposal['finished_decision_record'],
            'record_sha256': inputs[proposal['finished_decision_record']], 'json_pointer': f'/{i}', 'original_decision': row,
            'accepted_batch_scope_review': latest_review, 'review_sha256': inputs[latest_review],
            'limit': 'Finished outside-owner hold/referral link, not independent configuration/data/API semantic acceptance.'},
        'original_source_dispositions': [{'record': proposal['source_record'], 'record_sha256': inputs[proposal['source_record']],
            'json_pointer': f'/{idx}', 'row': data} for idx, data in source_rows],
        'direct_response_link': f'pf02-dispositions.json#/dispositions/{n}',
        'source_identity': {'inventory_raw_sha256': inventory[path]['sha256'], 'pinned_git_blob_from_inventory': inventory[path]['git_blob'],
                            'current_raw_sha256': raw_hash, 'unchanged_raw_bytes': True},
        'coordinator_retain_hold_referral_proposal': proposal,
        'record_gap_state': 'DIRECT-FINISHED-HOLD-OR-REFERRAL-LINK-PROVIDED; 9-path accounting gap addressed',
        'Astra_adjudication': 'PENDING for the facet-specific coordinator proposal; no authorization inferred',
        'new_product_config_or_API_changes': False, 'new_source_read_or_bug': False})
save('pf02-dispositions.json', {'finding': 'PF-02', 'direct_path_count': len(records), 'dispositions': records,
    'scope': 'Explicit final retain/hold/referral accounting from completed source and editor records. Proposal reasons are facet-specific; source reads and immutable values remain intact.',
    'acceptance_limit': 'This records owner addresses the link/proposal gap. Astra adjudicates coordinator proposals; final evidence reviewer controls final acceptance. No blanket retained-path PASS.'})
table = ['# PF-02 direct path dispositions', '',
    'Each row links the exact completed editor hold/referral and original investigator record. Facet-specific coordinator proposals await Astra adjudication; no value or API change is authorized.', '',
    '| Original candidate / exact surface | Direct completed decision | Facet-specific retention/referral reason | Reopening / held scope |',
    '| --- | --- | --- | --- |']
for entry in records:
    source_link = '../../..' + '/' + entry['existing_completed_editor_decision']['record']
    proposal = entry['coordinator_retain_hold_referral_proposal']
    cells = [f"{entry['candidate']} — `{entry['path']}`", f"[{entry['existing_completed_editor_decision']['original_decision']['decision']}]({source_link}); JSON {entry['existing_completed_editor_decision']['json_pointer']}",
             proposal['facet_reason'], proposal['reopen_when']]
    table.append('| ' + ' | '.join(x.replace('|', '\\|') for x in cells) + ' |')
(OUT / 'pf02-direct-links.md').write_text('\n'.join(table) + '\n')

third_receipt = load('validation/commands-v2/examples-full-consumer-third/receipt.json')
third_diagnostics = load('validation/examples/full/compiler-context-correction/actual-diagnostics.json')
assert third_receipt['status'] == 'failed' and third_receipt['exit_code'] == 1
assert len(third_diagnostics['unique_diagnostics']) == 21
save('validation-state.json', {'latest_completed_full_consumer_attempt': {'label': 'examples-full-consumer-third',
    'receipt': third_receipt, 'diagnostic_record': 'validation/examples/full/compiler-context-correction/actual-diagnostics.json',
    'unique_diagnostics': third_diagnostics['unique_diagnostics'], 'unique_diagnostic_count': 21,
    'codes': dict(Counter(x['code'] for x in third_diagnostics['unique_diagnostics']))},
    'earlier_failed_attempts': 'Preserved in ../reconciliation-2/validation-state.json and original command/attempt directories; neither is a pass.',
    'scope': 'Attempt 3 failed at Csc on remaining type/prerequisite/async/nullability context diagnostics. Validation owner is correcting/classifying generated contexts versus genuine body defects; no full compilation acceptance inferred.',
    'original_300_body_identity_limit': 'Original prepared body bytes remain preserved; no body correction, wrapper fix or subsequent compiler result is accepted by this records-only response.',
    'current_accepted_product_count': 361, 'full_compile_accepted': False, 'queued_correction_count': 3,
    'existing_limits': 'No final sample-quota/current-span PASS, no new SDK/native/device/crypto run, no publication acceptance. Reuse valid qualified checker/logger and preserve earlier profile/seal/checkpoints.'})
links_checked = 0
for file in (RUN / 'deferred-findings.md', OUT / 'response.md', OUT / 'pf02-direct-links.md'):
    for target_link in re.findall(r'\]\(([^)]+)\)', file.read_text()):
        if '://' not in target_link:
            assert (file.parent / target_link.split('#')[0]).exists(), ('local record link', file, target_link)
            links_checked += 1
for path, digest in prior_hashes.items():
    assert sha((RUN / path).read_bytes()) == digest
save('verification.json', {'status': 'PASS bounded PF response identity/link checks; final adjudication/closeout pending',
    'source_baseline': PIN, 'PF01': 'B2 third queued patch fully linked with review/source/unit/before/after identities and zero credit',
    'PF02_direct_finished_decision_links': len(records), 'PF02_facet_specific_proposals': len(proposals), 'Astra_proposal_adjudication': 'PENDING',
    'original_candidates_preserved': 185, 'inherited_rows_preserved': 100, 'inherited_groups_preserved': 70, 'pgp_labels_preserved': 21,
    'current_product_map_sha256': inputs['editing/integration/current-products.json'], 'current_product_hash_matches': 361,
    'queued_untransferred_patches': 3, 'new_clean_credit': 0, 'current_third_attempt': 'FAILED;21 remaining diagnostics',
    'prior_records_preserved_count': len(prior_hashes), 'predecessor_handoff_sha256': sha(history.read_bytes()),
    'local_record_links_checked': links_checked,
    'input_hashes': inputs, 'new_Git_SDK_native_device_crypto_subagent_actions': 0,
    'no_final_Pass1_or_release_verdict': True})
save('output-identities.json', {'state': 'BOUNDED-PREFLIGHT-RESPONSE; Astra proposal adjudication/final acceptance pending',
    'source_baseline': PIN, 'scope': 'Only owned reconciliation-3 records and updated handoff, excluding this manifest itself; not a publication/acceptance seal.',
    'files': {str(p.relative_to(RUN)): sha(p.read_bytes()) for p in
              [RUN / 'deferred-findings.md'] + sorted(x for x in OUT.iterdir() if x.is_file() and x.name != 'output-identities.json')}})
print(json.dumps({k: v for k, v in json.loads((OUT / 'verification.json').read_text()).items() if k != 'input_hashes'}))
