"""Additive closeout ledger snapshot. File/record reads only; no Git or SDK calls."""
import hashlib
import json
import re
from collections import Counter, defaultdict
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


# Preserve the first final draft before its authorized replacement. Prior records
# stay at their original paths; their old manifest's handoff hash binds this snapshot.
history = OUT / 'deferred-findings.reconciliation-1.md.snapshot'
if not history.exists():
    history.write_bytes((RUN / 'deferred-findings.md').read_bytes())
old_files = sorted(p for p in FINAL.iterdir() if p.is_file())
old_files += [RUN / 'triage-preliminary.md', RUN / 'triage-full-family.md']
for folder in ('preliminary', 'full-family'):
    old_files += sorted(p for p in (RUN / 'triage' / folder).iterdir() if p.is_file())
old_hashes = {str(p.relative_to(RUN)): sha(p.read_bytes()) for p in old_files}
if (OUT / 'history-preservation.json').exists():
    saved = json.loads((OUT / 'history-preservation.json').read_text())
    assert saved['unchanged_prior_records'] == old_hashes
    assert saved['prior_handoff_snapshot_sha256'] == sha(history.read_bytes())
else:
    save('history-preservation.json', {'unchanged_prior_records': old_hashes,
        'prior_handoff_snapshot': history.name, 'prior_handoff_snapshot_sha256': sha(history.read_bytes()),
        'rule': 'First-final records remain historical. Their handoff hash now resolves to this byte-identical snapshot, not an assertion that the current handoff is unchanged.'})
old_manifest = load('triage/final/output-identities.json')
assert old_manifest['files']['deferred-findings.md'] == sha(history.read_bytes())
old = load('triage/final/candidate-reconciliation.json')
inherited = load('triage/final/inherited-reconciliation.json')
protected = load('triage/final/protected-dispositions.json')
registry = load('inherited-findings/registry.json')
inventory = {x['id']: x for x in load('inventory/semantic-units.json')}
assert len(old['candidates']) == 185
assert [x['original_registry_row'] for x in inherited['rows']] == registry['rows']
assert len(registry['rows']) == 100 and len(registry['groups']) == 70
assert len(registry['openpgp_documentation_dependencies']) == 21
assert protected['hsm_retry_message']['state'] == 'RETRACTED-NOT-AN-OPEN-BUG'

batch_config = {
    'A': ('editing/A/revision-2', 'path-dispositions.json', 'accepted-A-C.json'),
    'C': ('editing/C', 'path-decisions.json', 'accepted-A-C.json'),
    'transports': ('editing/transports/revision-2', 'file-dispositions.json', 'accepted-transports.json'),
    'protocols': ('editing/protocols/revision-2', 'path-decisions.json', 'accepted-protocols.json'),
    'FIDO-boundaries': ('editing/FIDO-boundaries/revision-2', 'path-decisions.json', 'accepted-FIDO.json'),
    'SHARED': ('editing/SHARED/revision-2', 'path-dispositions.json', 'accepted-SHARED.json'),
    'applet-boundaries': ('editing/applet-boundaries/revision-2', 'path-decisions.json', 'accepted-applets.json'),
    'PGP-boundary': ('editing/PGP-boundary/revision-2', 'path-decisions.json', 'accepted-PGP-PIV.json'),
    'PIV-boundary': ('editing/PIV-boundary/revision-2', 'path-decisions.json', 'accepted-PGP-PIV.json'),
    'external': ('editing/external/revision-4', 'path-decisions.json', 'accepted-external.json'),
}
current = load('editing/integration/current-products.json')
products = {x['path']: dict(x, current_primary_sha256=sha((ROOT / x['path']).read_bytes())) for x in current['product_files']}
assert len(products) == len(current['product_files']) == 361
assert all(x['sha256'] == x['current_primary_sha256'] for x in products.values())
receipts = {name: load('editing/integration/' + name) for name in current['receipt_order']}
replayed = {}
for name in current['receipt_order']:
    for row in receipts[name]['product_files']:
        replayed[row['path']] = dict(row, accepted_receipt=name)
assert set(replayed) == set(products)
assert all(replayed[p]['sha256'] == products[p]['sha256'] and replayed[p]['accepted_receipt'] == products[p]['accepted_receipt'] for p in products)
editor_rows = {}
candidate_refs = defaultdict(set)
batches = {}
for batch, (directory, decisions, receipt) in batch_config.items():
    review_path = directory + '/review.md' if batch != 'C' else 'editing/C/revision-2/review.md'
    review = read(review_path)
    assert 'PASS' in review
    data = load(directory + '/' + decisions)
    rows = data.get('files', []) if isinstance(data, dict) else data
    batches[batch] = {'review': review_path, 'review_sha256': inputs[review_path], 'review_text': review,
        'decision_record': directory + '/' + decisions, 'integration_receipt': receipt,
        'state': 'CONTENT-CITATION-PASS-AT-BOUND-IDENTITIES; main batch integrated',
        'returned_path_rows': len(rows), 'compiler_gate': 'Full consumer/test-context and final combined validation pending; see validation-state.json'}
    for n, row in enumerate(rows):
        path = row['path']
        key = f'{batch}:{n}:{path}'
        editor_rows[key] = {'batch': batch, 'path': path, 'original_return_row': row,
            'review': review_path, 'current_integrated_product': products.get(path),
            'limit': 'A path can be retained or edited for another facet. Content PASS grants no engineering, device, final compiler or whole-file clean-sample acceptance.'}
        ids = set()
        for field in ('candidate_ids', 'local_candidate_ids', 'local_ids', 'candidates', 'candidate_ids_from_original_file_ledger'):
            for value in row.get(field, []):
                if isinstance(value, str):
                    ids.add(value)
                elif isinstance(value, dict):
                    ids.update(value[k] for k in ('local_id', 'local', 'id', 'candidate') if isinstance(value.get(k), str))
        ids.update(x['local_id'] for x in row.get('candidate_associations', []))
        if isinstance(row.get('candidate'), str):
            ids.add(row['candidate'])
        for cid in ids:
            if cid in old['candidates']:
                candidate_refs[cid].add(key)
save('editor-integration.json', {'source_baseline': PIN, 'main_batches': batches,
    'path_decisions': editor_rows, 'current_products_record': current,
    'current_products_hash_check': products, 'ordered_integration_receipts': receipts,
    'unique_integrated_paths': len(products),
    'rule': 'All main batches content PASS; later accepted or in-progress sampling corrections have separate integration states.'})

# Actual coordinator verdicts only. Selection/preparation files never create reads.
selection = load('sampling/preparation/selection.json')
selected = {x['id']: x for x in selection}
assert len(selected) == len(selection) == 983
sample_rows = {}
verdict_texts = {}
for path in sorted((RUN / 'sampling/coordinator').glob('*verdicts.json')):
    relative = str(path.relative_to(RUN))
    data = load(relative)
    for row in data['units']:
        uid = row['id']
        assert uid in selected and uid not in sample_rows, (relative, uid)
        assert row['range_sha256'] == selected[uid]['range_sha256'] == inventory[uid]['range_sha256']
        sample_rows[uid] = dict(row, verdict_record=relative, reader='Astra', read_basis='Actual complete coordinator read',
            final_clean_credit=None if row['verdict'].startswith('SOURCE-CONTENT-SUPPORTED') else 0)
for path in sorted((RUN / 'sampling/coordinator').glob('read-*verdicts.md')):
    relative = str(path.relative_to(RUN))
    text = read(relative)
    verdict_texts[relative] = text
    for suffix, verdict in re.findall(r'^\| `([a-f0-9]{16})` \| (.+?) \|\s*$', text, re.M):
        uid = 'core-001:unit:' + suffix
        assert uid in selected and uid not in sample_rows, (relative, uid)
        state = 'SOURCE-CONTENT-SUPPORTED-PENDING-FINAL-ELIGIBILITY'
        if 'SAMPLE-' in verdict:
            state = 'DOCUMENTATION-MISS'
        elif 'Known deferred qualification' in verdict:
            state = 'KNOWN-DEFERRED-QUALIFICATION'
        elif 'Edited overlap' in verdict:
            state = 'INELIGIBLE-EDITED-OVERLAP'
        elif 'Administrative' in verdict or 'Ineligible preamble' in verdict:
            state = 'INELIGIBLE-ADMINISTRATIVE'
        sample_rows[uid] = dict(selected[uid], verdict=state, note=verdict,
            verdict_record=relative, reader='Astra', read_basis='Actual complete coordinator read',
            final_clean_credit=None if state.startswith('SOURCE-CONTENT-SUPPORTED') else 0)
# The first batch's three misses precede its supported-unit table.
for suffix, finding in [('711e47d18bab0742', 'SAMPLE-A001'), ('c085f1cd3632e7c2', 'SAMPLE-A002'), ('076b4d4328825902', 'SAMPLE-A002')]:
    uid = 'core-001:unit:' + suffix
    assert uid in selected and uid not in sample_rows
    sample_rows[uid] = dict(selected[uid], verdict='DOCUMENTATION-MISS', note=finding,
        verdict_record='sampling/coordinator/read-001-verdicts.md', reader='Astra',
        read_basis='Actual complete coordinator read; miss paragraph', final_clean_credit=0)
assert set(sample_rows) == set(selected), ('unmapped verdicts', set(selected) - set(sample_rows))
read('sampling/coordinator/admission.md')
read('sampling/coordinator/additional-observations.md')
for uid, row in sample_rows.items():
    links = []
    for cid, candidate in old['candidates'].items():
        if uid in candidate['source_unit_refs']:
            links.append({'candidate': cid, 'relation': 'original evidence/context unit association; no new defect inferred'})
        elif row['path'] in candidate['documentation_paths']:
            links.append({'candidate': cid, 'relation': 'same documentation path only; not a same-defect finding'})
    row['original_candidate_links'] = links
    row['final_eligibility'] = 'PENDING-CURRENT-MAPPING-EDITED-EXCLUSION-QUOTA-RECONCILIATION'
    row['read_status'] = 'READ-BY-COORDINATOR'
    row.pop('review_verdict', None)
save('sampling-verdicts.json', {'source_baseline': PIN, 'reader': 'Astra', 'original_sample_units_read': len(sample_rows),
    'verdict_counts': dict(Counter(x['verdict'] for x in sample_rows.values())), 'units': sample_rows,
    'original_markdown_verdicts': verdict_texts,
    'rule': '983 reads, not 983 final clean passes. Qualified, administrative, dormant, reference-data, protected-output, miss and edited-overlap outcomes retain zero or pending clean credit.'})

# Add completed expansion/replacement verdicts separately from the original 983.
# No original-unit bodies are reread and repeated IDs gain no second read credit.
expansion_rows = {}
expansion_records = {}
for name in ('expansion-A-verdicts.md', 'expansion-B2-B3-B4-verdicts.md', 'family-and-SD-replacement-verdicts.md'):
    relative = 'sampling/coordinator/' + name
    text = read(relative)
    expansion_records[relative] = text
    local_verdicts = {}
    for suffix in re.findall(r'`([a-f0-9]{16})`', text):
        uid = 'core-001:unit:' + suffix
        local_verdicts[uid] = 'SOURCE-CONTENT-SUPPORTED-PENDING-FINAL-ELIGIBILITY'
    notes = {}
    for suffix, note in re.findall(r'^\| `([a-f0-9]{16})` \| (.+?) \|\s*$', text, re.M):
        uid = 'core-001:unit:' + suffix
        notes[uid] = note
    for suffix, note in re.findall(r'^- `([a-f0-9]{16})`: (.+?)(?=\n(?:- |## |All )|\Z)', text, re.M | re.S):
        notes['core-001:unit:' + suffix] = note.strip()
    for uid, note in notes.items():
        lower = note.lower()
        if 'sample-a004' in lower:
            state = 'DEFERRED-SOURCE-OBSERVATION'
        elif 'source-only cleanup' in lower:
            state = 'KNOWN-DEFERRED-PRETRY-CLEANUP-QUALIFICATION'
        elif 'edited overlap' in lower:
            state = 'INELIGIBLE-EDITED-OVERLAP'
        elif 'historical' in lower and ('zero clean' in lower or 'qualified evidence' in lower or 'qualified context' in lower):
            state = 'HISTORICAL-EVIDENCE-QUALIFIED'
        elif 'administrative' in lower:
            state = 'INELIGIBLE-ADMINISTRATIVE'
        elif 'source-qualified' in lower or 'protected-output' in lower or 'no clean credit for unresolved applicability' in lower:
            state = 'SOURCE-QUALIFIED-PENDING-REPLACEMENT'
        else:
            state = local_verdicts[uid]
        local_verdicts[uid] = state
    for uid, verdict in local_verdicts.items():
        assert uid in inventory
        if uid not in expansion_rows:
            expansion_rows[uid] = {'id': uid, 'path': inventory[uid]['path'],
                'first_line': inventory[uid]['first_line'], 'last_line': inventory[uid]['last_line'],
                'source_sha256': inventory[uid]['source_sha256'], 'range_sha256': inventory[uid]['range_sha256'],
                'verdict': verdict, 'note': notes.get(uid, 'Supported within the source scope stated in the completed coordinator expansion verdict.'),
                'verdict_record': relative, 'reader': 'Astra', 'actual_read': True,
                'final_clean_credit': None if verdict.startswith('SOURCE-CONTENT-SUPPORTED') else 0,
                'read_count_limit': 'Actual expansion/replacement read; duplicates with any other record count once. Final mapping and quota acceptance pending.'}
for uid, row in expansion_rows.items():
    row['original_candidate_links'] = [
        {'candidate': cid, 'relation': 'original evidence/context unit' if uid in candidate['source_unit_refs'] else 'same path only, not a same-defect finding'}
        for cid, candidate in old['candidates'].items()
        if uid in candidate['source_unit_refs'] or row['path'] in candidate['documentation_paths']]
save('sampling-expansion-verdicts.json', {'units': expansion_rows, 'completed_records': expansion_records,
    'distinct_expansion_replacement_ids': len(expansion_rows),
    'new_ids_beyond_original_983': len(set(expansion_rows) - set(sample_rows)),
    'verdict_counts': dict(Counter(x['verdict'] for x in expansion_rows.values())),
    'final_clean_pass': False, 'rule': 'Finished actual reads are credited only as reads; explicit qualification/exclusion and remaining quota replacements stay open.'})

# Read prepared expansion/replacement records, never their original source bodies.
pending_packets = {}
for directory in sorted((RUN / 'sampling/preparation').iterdir()):
    if not directory.is_dir() or directory.name == 'history':
        continue
    source = directory / 'revision-2' if directory.name == 'family-replacements' else directory
    records = {}
    for name in ('additional-selection.json', 'replacement-selection.json', 'quotas.json', 'quota.json', 'verification.json', 'read-plan.md'):
        path = source / name
        if path.exists():
            relative = str(path.relative_to(RUN))
            records[name] = load(relative) if path.suffix == '.json' else read(relative)
    if records:
        proposed_ids = {row['id'] for name, data in records.items() if 'selection' in name and isinstance(data, list) for row in data}
        actual_ids = proposed_ids & (set(expansion_rows) | set(sample_rows))
        pending_packets[str(source.relative_to(RUN))] = {'records': records,
            'read_state': 'Use actual-read ID overlay below; remaining proposed IDs have no actual read credit.',
            'actual_coordinator_read_ids': sorted(actual_ids), 'proposed_ids_without_consumed_actual_read': sorted(proposed_ids - actual_ids),
            'credit': 'No clean credit from preparation, selection size or helper demonstration.'}
save('sampling-pending-packets.json', {'packets': pending_packets,
    'rule': 'Selection and validation are preparation only. Actual reads and final per-owner/per-role quota acceptance remain separate.'})

wa_review_path = Path('/home/rupicola/Code/y/yubikit-core-docs-edit-fido-boundaries') / RUN.relative_to(ROOT) / 'editing/FIDO-boundaries/sampling-selection-oracle/review.md'
wa_review = read(wa_review_path)
wa_path = 'src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientGetAssertionTests.cs'
wa_current_hash = sha((ROOT / wa_path).read_bytes())
wa_before = '9f12c9ace45d8fe8e534a700557c353a67fd4707a257cfd73def6bec0abd84d5'
wa_after = '9223a2299f63cd2f8ceb5479cf5cc56ed5fb99463c084db60590cf3123f4cff1'
assert wa_current_hash == wa_before, 'queued WA patch integration state changed; coordinator reconciliation needed'
pgp_review_path = Path('/home/rupicola/Code/y/yubikit-core-docs-edit-pgp-piv') / RUN.relative_to(ROOT) / 'editing/PGP-boundary/sampling-UIF-fallback/review.md'
pgp_review = read(pgp_review_path)
assert '9d5433109986b2037f47654d912970b7618cc84c336873950a5d1fecba33e2c5' in pgp_review
findings = {
    'SAMPLE-A004': {'state': 'SOURCE-ONLY-DEFERRED; no permanent leak/native failure demonstrated',
        'units': ['core-001:unit:10653b759a2209f8', 'core-001:unit:3ccf06e816f277d2'],
        'paths': ['src/Cli.Commands/src/Piv/PivCommands.cs', 'src/Cli.Commands/src/OpenPgp/OpenPgpCertificatesCommands.cs'],
        'observed': 'PIV CLI attestation receives separately returned X509Certificate2; OpenPGP certificate caller loads a certificate that survives output/PIN/import paths. Both lack deterministic certificate disposal, with separate caller-specific anchors.',
        'candidate_links': [{'id': 'CORE-PIV-X006', 'relation': 'related sample/resource-lifetime family; unified CLI site remains explicitly distinct from standalone tool callers'},
                            {'id': 'CORE-PGP-X008', 'relation': 'same deterministic-certificate ownership class; retain distinct OpenPGP CLI caller anchor, not a second permanent-leak assertion'}],
        'owner': 'External CLI/result owner; retain SAMPLE-A004 site evidence and exact existing aliases before any later engineering grouping',
        'reopen': 'Returned certificate deterministic lifetime/output behavior or resource guidance changes; no new implementation/test work authorized.'},
    'SAMPLE-PGP-001': {'state': 'FOCUSED-CONTENT-PASS; queued and untransferred',
        'units': ['core-001:unit:8b2a95f01db9bf2c', 'core-001:unit:710814862611f595'],
        'paths': [selected['core-001:unit:' + x]['path'] for x in ('8b2a95f01db9bf2c', '710814862611f595')],
        'observed': 'UIF failed-read fallback logs before selecting PolicyMayRequire; a throwing provider can interrupt that fallback.',
        'candidate_links': [{'id': 'CORE-C-C008', 'relation': 'existing logger/callback-exception mechanism'}, {'id': 'CORE-PGP-X004', 'relation': 'actual advisory presence fallback caller'}],
        'patch_sha256': '9d5433109986b2037f47654d912970b7618cc84c336873950a5d1fecba33e2c5',
        'review_path': str(pgp_review_path), 'review_text': pgp_review,
        'owner': 'Coordinator transfer after reserved compilation cycle; no main runtime change',
        'reopen': 'Qualified fallback comment/guide and corresponding logging/provider failure policy; zero clean credit, implementation/narrative expansions pending.'},
    'SAMPLE-WA-001': {'state': 'FOCUSED-CONTENT-PASS; queued and untransferred',
        'units': ['core-001:unit:e29522f04d40349a'], 'paths': [wa_path],
        'observed': 'Comment overstates instance identity/all-fields equality; actual assertions compare CredentialId/Signature bytes and one backend call.',
        'candidate_links': [{'id': 'CORE-WA-X012', 'relation': 'existing test-oracle/provenance facet; no new engineering finding'}],
        'patch_sha256': 'd70a50aaf297cc62fd6c7fa93f13e8cfb1acb44fe975279dc9fb60fc30e5b05e',
        'review_path': str(wa_review_path), 'review_text': wa_review,
        'current_primary_sha256': wa_current_hash, 'accepted_after_sha256': wa_after,
        'owner': 'Coordinator transfer only after reserved compilation cycle; test/helper expansion and eligibility repairs remain sampler-owned',
        'reopen': 'Transfer exact focused patch, preserve original assertions, then current-source mapping and actual expansion/replacement reads; no present clean credit.'},
    'PIV-pretry-known-qualification': {'state': 'ALREADY-DEFERRED; no duplicate engineering finding',
        'units': ['core-001:unit:a579b6a5cf4242e4', 'core-001:unit:8fc815177a95b970'],
        'paths': [selected['core-001:unit:' + x]['path'] for x in ('a579b6a5cf4242e4', '8fc815177a95b970')],
        'observed': 'PIN and management-key allocations precede awaited discovery outside the clearing scope.',
        'candidate_links': [{'id': 'CORE-PIV-X006', 'relation': 'same preserved pre-try sample cleanup facet'}, {'id': 'piv-001:PIVIMP-006', 'relation': 'original engineering identity'}],
        'owner': 'Existing PIV sample owner; runtime unchanged',
        'reopen': 'Same original resource/discovery cleanup condition; zero clean credit and quota replacements, without another runtime group.'},
    'OTP-expansion-pretry-qualification': {'state': 'SOURCE-ONLY-KNOWN-CLEANUP-CLASS; M facet retained; no new runtime proof',
        'units': ['core-001:unit:4089aedab94d4e23'], 'paths': ['src/Cli.Commands/src/Otp/OtpCommands.cs'],
        'observed': 'Acquired HMAC key precedes generated-key output and access-code parsing outside the clearing finally.',
        'candidate_links': [{'id': 'CORE-OTP-X005', 'relation': 'existing pre-protected-scope cleanup responsibility, caller-specific HMAC-key anchor'}, {'id': 'CORE-C-C013', 'relation': 'related source/acquisition/intermediate ownership class, not an identical helper'}],
        'owner': 'Existing OTP/CLI cleanup owner; no code change or new engineering group authorized',
        'reopen': 'Same acquired-owner/protected-scope/output/parse lifetime changes; zero final clean credit.'},
    'OpenPGP-expansion-applicability-output': {'state': 'PROTECTED-OUTPUT-AND-APPLICABILITY-QUALIFIED; no duplicate finding',
        'units': ['core-001:unit:297e68004991d4a8', 'core-001:unit:cf3538431462d4ab'],
        'paths': [inventory['core-001:unit:' + x]['path'] for x in ('297e68004991d4a8', 'cf3538431462d4ab')],
        'observed': 'Info infers key presence from fingerprint and emits PIN-policy text; keygen requests slot/algorithm combinations subject to existing OpenPGP applicability limits.',
        'candidate_links': [{'id': 'CORE-PGP-X008', 'relation': 'existing protected sample output/oracle qualification'}, {'id': 'CORE-PGP-X007', 'relation': 'existing key/slot/profile applicability responsibility'}],
        'owner': 'Existing OpenPGP sample/profile owners; no runtime string or behavior changed',
        'reopen': 'Existing metadata/output or key/slot applicability conditions; zero final clean credit.'},
}
correction_receipts = {'SAMPLE-A001': 'accepted-sampling-A.json', 'SAMPLE-A002': 'accepted-sampling-A.json',
    'SAMPLE-A003': 'accepted-sampling-A.json', 'SAMPLE-B2-001': 'accepted-sampling-B2.json',
    'SAMPLE-B2-002': 'accepted-sampling-B2.json', 'SAMPLE-B3-001': 'accepted-sampling-B3.json',
    'SAMPLE-B4-001': 'accepted-sampling-B4.json', 'SAMPLE-B4-002': 'accepted-sampling-B4.json',
    'SAMPLE-SD-001': 'accepted-sampling-SD.json'}
for name, receipt in correction_receipts.items():
    affected = [uid for uid, row in sample_rows.items() if name in row.get('note', '') or name.removeprefix('SAMPLE-') in row.get('note', '')]
    assert affected, name
    findings[name] = {'state': 'DOCUMENTATION-CORRECTION-CONTENT-ACCEPTED-AND-INTEGRATED; expansion/clean-quota gate pending',
        'units': affected, 'integration_receipt': receipt,
        'paths': [x['path'] for x in receipts[receipt]['product_files']],
        'source_verdicts': [sample_rows[uid]['verdict_record'] for uid in affected],
        'candidate_links': 'Use exact-unit/same-path candidate associations in sampling-verdicts.json; a documentation miss adds no independently demonstrated runtime defect.',
        'reopen': 'Corrected/edited overlaps receive zero unchanged clean credit; actual expanded strata and replacements require coordinator verdicts.'}
for finding in findings.values():
    finding['original_unit_anchors'] = [{k: inventory[uid][k] for k in ('id', 'path', 'first_line', 'last_line', 'source_sha256', 'range_sha256')}
                                        for uid in finding['units']]
save('sampling-findings.json', {'findings': findings, 'original_candidate_count_preserved': 185,
    'rule': 'Sampling labels supplement original evidence; they do not renumber original candidates or manufacture accepted engineering groups. HSM retry-message observation remains retracted.'})

lanes = ('examples-ac-first', 'unit-tests-integrated-docs', 'checker-bound-build-final-edits',
         'source-tokens-final-profile-first', 'api-final-profile-first', 'examples-full-consumer-first', 'examples-full-consumer-second')
commands = {}
for lane in lanes:
    base = 'validation/commands-v2/' + lane
    receipt = load(base + '/receipt.json')
    stdout = read(base + '/stdout.log')
    commands[lane] = {'receipt': receipt, 'stdout_sha256': inputs[base + '/stdout.log'],
        'outcome': receipt['status'], 'exit_code': receipt['exit_code'],
        'result_tail': stdout.splitlines()[-30:],
        'scope': 'Retained coordinator run at its actual input/output checkpoint; no execution by this reconciliation.'}
assert commands['examples-full-consumer-first']['exit_code'] != 0 and commands['examples-full-consumer-second']['exit_code'] != 0
profile_path = 'validation/checker-binding/reviewed-inputs-final-edits.json'
seal_path = 'validation/checker-binding/attempts/checker-bound-build-final-edits/sealed.json'
profile = load(profile_path)
seal = load(seal_path)
assert inputs[profile_path] == seal['profile']['sha256']
assert inputs[profile_path].startswith('db834') and inputs[seal_path].startswith('b358')
diagnostics_path = 'validation/examples/full/compiler-import-correction/actual-diagnostics.json'
diagnostics = load(diagnostics_path)
restore = read('validation/examples/full/restore-pruning-correction/handoff.md')
unit_result_path = 'validation/commands-v2/unit-tests-integrated-docs/toolchain/20261112-023026-756-test/result.json'
unit_result = load(unit_result_path)
unit_counts = {k: sum(x.get(k, 0) for x in unit_result['projects']) for k in ('executed', 'failed', 'skipped')}
assert len(unit_result['projects']) == 13 and unit_counts == {'executed': 4442, 'failed': 0, 'skipped': 5}
pilot_manifest = load('validation/examples/manifest.json')
assert pilot_manifest['counts']['prepared'] == 17
pilot_compilation = load('validation/examples/attempts/examples-ac-first/accepted-compilation.json')
pilot_result_path = 'validation/commands-v2/examples-ac-first/toolchain/20261112-023423-403-build/result.json'
pilot_result = load(pilot_result_path)
assert pilot_result['status'] == 'passed' and pilot_result['warningCount'] == 0
validation = {'commands': commands,
    'DocCheck': {'profile': profile_path, 'profile_sha256': inputs[profile_path], 'seal': seal_path, 'seal_sha256': inputs[seal_path],
        'profile_matches_seal': True, 'qualified_scope': 'Valid final DocCheck profile and bound token/API checkpoint before queued sampling patches; no final new product run here.'},
    'retained_successes': {'pilot': '17 bodies, zero warnings at examples-ac-first retained checkpoint',
        'unit_tests': '4,442 passed, 13 projects, zero failures, five skips at unit-tests-integrated-docs retained checkpoint',
        'pilot_manifest': 'validation/examples/manifest.json', 'pilot_result': pilot_result_path,
        'accepted_pilot_compilation': pilot_compilation,
        'unit_result': unit_result_path, 'unit_counts': unit_counts, 'unit_project_count': len(unit_result['projects'])},
    'full_consumer': {'attempt_1': 'Failed in restore (NU1510), before Csc; no full compilation acceptance',
        'attempt_2': 'Reached Csc; failed generated imports/duplicates; wrapper correction in progress by validation owner',
        'diagnostic_record': diagnostics_path, 'diagnostic_code_counts': dict(Counter(x['code'] for x in diagnostics['unique_diagnostics'])),
        'restore_correction_handoff': restore, 'final_full_compile_acceptance': False,
        'queued_patch_rule': 'Current primary reserved for compiler cycle. Independently accepted WA and PGP patches remain queued/untransferred; no main runtime change.',
        'all_300_prepared_body_bytes': 'Unchanged; validation owner is correcting generated wrapper/import context only. No full compilation acceptance inferred.'},
    'readiness': 'Not ready to assert final compiler/sampling/Pass-1 completion. Fresh final evidence review follows remaining gates.',
    'new_sdk_native_device_crypto_executions': 0}
save('validation-state.json', validation)

overlay = {}
all_actual_sample_rows = dict(sample_rows, **expansion_rows)
for cid, original in old['candidates'].items():
    returned = sorted(candidate_refs[cid])
    samples = [uid for uid, row in all_actual_sample_rows.items() if any(x['candidate'] == cid for x in row['original_candidate_links'])]
    finding_refs = [name for name, finding in findings.items() if isinstance(finding.get('candidate_links'), list)
                    and any(x['id'] == cid for x in finding['candidate_links'])]
    overlay[cid] = {'run_qualified_id': original['run_qualified_id'],
        'original_source_facets': '../candidate-reconciliation.json#' + cid,
        'inherited_association_keys_preserved': original['inherited_association_keys'],
        'accepted_main_editor_decision_refs': returned, 'sampling_unit_refs': samples,
        'original_983_sample_refs': [uid for uid in samples if uid in sample_rows],
        'actual_expansion_replacement_refs': [uid for uid in samples if uid in expansion_rows],
        'explicit_sampling_finding_refs': finding_refs,
        'documentation': 'All main batches content PASS; credit exact returned facet/path only, with held/no-D/referral distinctions preserved.',
        'original_documentation_facet': original['facets']['documentation'],
        'current_documentation_facet': {
            'main_batch_review': 'All main batches content/citation PASS at accepted hashes',
            'candidate_scope': original['facets']['documentation']['status'] if original['facets']['documentation']['status'].startswith(('HELD', 'NO-RELEASED', 'RETAIN-REFER'))
                               else 'Credit only exact returned D edit/retain facets and accepted current receipt. No final compiler/clean-quota/publication or engineering acceptance.',
            'returned_path_decision_refs': returned,
            'sampling_findings': finding_refs},
        'non_documentation_facets_preserved': {k: original['facets'][k] for k in ('engineering', 'decision', 'qualification')},
        'pending': 'Queued sampling corrections, final current mapping/quotas/remaining replacement or expansion reads, full contexts and final evidence/publication remain separate.',
        'limit': 'A same-path sample association is context only; it is not a new defect or automatic same-root-cause merge.'}
assert set(overlay) == set(old['candidates'])
save('candidate-editor-sampling.json', {'state': 'DRAFT-RECONCILIATION-2; final closeout pending', 'source_baseline': PIN,
    'original_candidates': overlay, 'original_count': len(overlay),
    'inherited_authority': '../inherited-reconciliation.json', 'protected_dispositions': '../protected-dispositions.json',
    'rule': '185 originals / 100 inherited rows / 70 existing groups / 21 PGP labels unchanged. No new canonical accepted engineering/doc namespace allocated.'})
table = ['# Candidate/editor/sampling mapping — reconciliation 2', '',
    'All main editor batches are content PASS at their bound identities. The counts below are links, not accepted bugs, whole-file approvals, final clean samples or compilation.', '',
    '| Original candidate | Accepted main batches with returned decisions | Actual original-sample links | Explicit sampling finding |', '| --- | --- | ---: | --- |']
for cid, row in overlay.items():
    batch_names = sorted({editor_rows[x]['batch'] for x in row['accepted_main_editor_decision_refs']})
    table.append('| ' + ' | '.join([cid, ', '.join(batch_names) or 'Held/referral; see original facet and current owner',
        str(len(row['sampling_unit_refs'])), ', '.join(row['explicit_sampling_finding_refs']) or '—']) + ' |')
(OUT / 'candidate-map.md').write_text('\n'.join(table) + '\n')
assert any(row['batch'] == 'external' for cid in overlay.values() for ref in cid['accepted_main_editor_decision_refs'] for row in [editor_rows[ref]])
for cid, item in overlay.items():
    assert item['run_qualified_id'] == old['candidates'][cid]['run_qualified_id']
    assert item['inherited_association_keys_preserved'] == old['candidates'][cid]['inherited_association_keys']
    assert all(ref in editor_rows for ref in item['accepted_main_editor_decision_refs'])
    assert all(uid in all_actual_sample_rows for uid in item['sampling_unit_refs'])
links_checked = 0
for file in (RUN / 'deferred-findings.md', OUT / 'candidate-map.md'):
    for target in re.findall(r'\]\(([^)]+)\)', file.read_text()):
        if '://' not in target:
            assert (file.parent / target.split('#')[0]).exists(), ('local record link', file, target)
            links_checked += 1
for path, digest in old_hashes.items():
    assert sha((RUN / path).read_bytes()) == digest
save('verification.json', {'status': 'PASS records-only identity/mapping checks; closeout pending', 'source_baseline': PIN,
    'original_candidates': len(overlay), 'inherited_rows': 100, 'inherited_groups': 70, 'pgp_labels': 21,
    'accepted_main_batches': len(batches), 'current_integrated_paths': len(products),
    'current_product_hash_matches': sum(x['sha256'] == x['current_primary_sha256'] for x in products.values()),
    'original_samples_with_actual_coordinator_verdict': len(sample_rows),
    'actual_distinct_expansion_replacement_ids': len(expansion_rows),
    'distinct_original_plus_expansion_read_ids': len(set(sample_rows) | set(expansion_rows)),
    'sampling_verdict_counts': dict(Counter(x['verdict'] for x in sample_rows.values())),
    'prepared_pending_packet_sets': len(pending_packets), 'final_clean_sample_pass': False, 'full_compile_accepted': False,
    'WA_patch_state': 'CONTENT-PASS-QUEUED-UNTRANSFERRED', 'PGP_sampling_correction_state': 'CONTENT-PASS-QUEUED-UNTRANSFERRED',
    'DocCheck_profile_sha256': inputs[profile_path], 'DocCheck_seal_sha256': inputs[seal_path],
    'preserved_prior_record_count': len(old_hashes), 'prior_handoff_snapshot_sha256': sha(history.read_bytes()),
    'local_record_links_checked': links_checked,
    'input_hashes': inputs, 'new_Git_SDK_native_device_crypto_subagent_actions': 0})
save('output-identities.json', {'state': 'DRAFT-RECONCILIATION-2; final compiler/sampling/evidence/publication gates pending',
    'source_baseline': PIN, 'scope': 'Only owned additive reconciliation records and updated handoff; excludes this manifest itself. No publication/acceptance seal.',
    'files': {str(p.relative_to(RUN)): sha(p.read_bytes()) for p in
              [RUN / 'deferred-findings.md'] + sorted(x for x in OUT.iterdir() if x.is_file() and x.name != 'output-identities.json')}})
print(json.dumps({k: v for k, v in json.loads((OUT / 'verification.json').read_text()).items() if k != 'input_hashes'}))
