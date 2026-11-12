"""Last bounded handoff snapshot: records and byte identities only; no Git/SDK."""
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
    inputs[str(p.relative_to(RUN)) if p.is_relative_to(RUN) else str(p)] = sha(raw)
    return raw.decode('utf-8-sig')


def load(path):
    return json.loads(read(path))


def save(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


snapshot = OUT / 'deferred-findings.reconciliation-3.md.snapshot'
if not snapshot.exists():
    snapshot.write_bytes((RUN / 'deferred-findings.md').read_bytes())
prior_paths = sorted(p for p in (RUN / 'triage').rglob('*') if p.is_file() and not p.is_relative_to(OUT))
prior_paths += [RUN / 'triage-preliminary.md', RUN / 'triage-full-family.md',
                RUN / 'validation/closeout-review/preflight.md', RUN / 'validation/closeout-review/input-identities.json']
prior = {str(p.relative_to(RUN)): sha(p.read_bytes()) for p in prior_paths}
if (OUT / 'history-preservation.json').exists():
    previous = json.loads((OUT / 'history-preservation.json').read_text())
    assert previous['unchanged_prior_records'] == prior
    assert previous['predecessor_handoff_sha256'] == sha(snapshot.read_bytes())
else:
    save('history-preservation.json', {'unchanged_prior_records': prior, 'predecessor_handoff': snapshot.name,
        'predecessor_handoff_sha256': sha(snapshot.read_bytes()),
        'rule': 'Prior source/editor/triage/preflight snapshots retain their historical states; only new additive records and the current handoff are writable.'})
old = load('triage/final/candidate-reconciliation.json')
inherited = load('triage/final/inherited-reconciliation.json')
protected = load('triage/final/protected-dispositions.json')
assert len(old['candidates']) == 185 and len(inherited['rows']) == 100
assert len(inherited['groups_preserved']) == 70 and len(inherited['pgp_documentation_dependencies_preserved']) == 21
assert protected['hsm_retry_message']['state'] == 'RETRACTED-NOT-AN-OPEN-BUG'
current = load('editing/integration/current-products.json')
assert inputs['editing/integration/current-products.json'] == '2626e78f146a0dc1d28f8604632e33a58891a98a13a5c1cb2b7d9ed90225b14b'
products = {x['path']: x for x in current['product_files']}
assert len(products) == len(current['product_files']) == 363
current_hashes = {path: {'accepted_sha256': x['sha256'], 'current_sha256': sha((ROOT / path).read_bytes())}
                  for path, x in products.items()}
assert all(x['accepted_sha256'] == x['current_sha256'] for x in current_hashes.values())
initial = load('validation/source-before.json')
assert len(initial['files']) == 1608
source_rows = [dict(x, actual_current_sha256=sha((ROOT / x['path']).read_bytes())) for x in initial['files']]
raw_changed = {x['path'] for x in source_rows if x['actual_current_sha256'] != x['current_sha256']}
assert raw_changed == set(products) and len(raw_changed) == 363
save('final-source-state.json', {'source_baseline': PIN, 'captured_initial_raw_source_record': 'validation/source-before.json',
    'paths': source_rows, 'scoped_paths': 1608, 'changed_since_initial_raw_capture': 363,
    'changed_path_set_equals_current_product_map': True,
    'scope': 'File-byte equality to the initial captured checkout representation, not a fresh Git-clean/index/staged-object check or source-body audit. Recorded original newline qualifications remain.'})
chain = {}
receipts = {}
for name in current['receipt_order']:
    receipt = load('editing/integration/' + name)
    receipts[name] = receipt
    for row in receipt['product_files']:
        if 'previous_sha256' in row and row['path'] in chain:
            assert row['previous_sha256'] == chain[row['path']]['sha256'], ('transfer predecessor', name, row['path'])
        chain[row['path']] = dict(row, accepted_receipt=name)
assert set(chain) == set(products)
assert all(chain[path]['sha256'] == row['sha256'] and chain[path]['accepted_receipt'] == row['accepted_receipt'] for path, row in products.items())
five_names = ['accepted-closeout-WA.json', 'accepted-closeout-PGP.json', 'accepted-closeout-B2-receive.json',
              'accepted-closeout-final-guides.json', 'accepted-closeout-architecture-map.json']
five_paths = [row['path'] for name in five_names for row in receipts[name]['product_files']]
assert len(five_paths) == len(set(five_paths)) == 9
adjudication = read('triage/final/reconciliation-3/coordinator-adjudication.md')
assert 'accept retain/referral decisions for eight surfaces' in adjudication
assert receipts['accepted-closeout-architecture-map.json']['patch_sha256'] in adjudication
map_review = read('editing/SHARED/architecture-map-closeout/review.md')
assert 'PASS' in map_review
save('transfers-and-PF02.json', {'source_baseline': PIN, 'current_products': current,
    'current_raw_hash_checks': current_hashes, 'ordered_receipts': receipts,
    'five_formerly_queued': {name: receipts[name] for name in five_names},
    'five_patch_path_count': 9, 'five_patch_state': 'TRANSFERRED; exact accepted before/after chain retained',
    'PF01': {'coordinator_adjudication': adjudication,
        'current_state': 'Accepted SAMPLE-B2-003 accounting/link resolution preserved; B2 receive correction subsequently transferred under accepted-closeout-B2-receive.json. Original queued/zero-credit checkpoint remains historical; no new sampling credit awarded here.',
        'transfer_receipt': 'editing/integration/accepted-closeout-B2-receive.json'},
    'PF02': {'original_nine_disposition_records': '../reconciliation-3/pf02-dispositions.json',
        'coordinator_adjudication': adjudication,
        'architecture_independent_review': map_review,
        'current_state': '9/9 ADJUDICATED: eight retain/referrals accepted; architecture five-symbol/prose correction independently PASS and actually transferred',
        'architecture_receipt': 'editing/integration/accepted-closeout-architecture-map.json',
        'limit': 'Current review accepts the specified metadata/prose disposition, not general native/runtime/profile or release validation.'}})

wa_review_path = 'editing/FIDO-boundaries/compiler-logging-correction/review.md'
root_review_path = 'editing/SHARED/compiler-logging-access/review.md'
wa_review = read(wa_review_path)
root_review = read(root_review_path)
wa_patch = 'fb137a493fb4eac7b02b6592a9fc9d2abf74e1ef69d23f43015d7f76c5e4ad57'
root_patch = 'b43c9a1da1416b45971f25e81b25bc46705158066e474d214e506f7909b452bd'
assert wa_patch == receipts['accepted-WA-logging.json']['patch_sha256'] and wa_patch in wa_review
assert root_patch == receipts['accepted-root-logging.json']['patch_sha256'] and root_patch in root_review
retraction = read('sampling/coordinator/compiler-WA-logging-retraction.md')
wa_expansion = read('sampling/coordinator/compiler-WA-expansion-verdicts.md')
save('logging-body-retraction.json', {'sample': 'SAMPLE-WA-002 / FULL-032',
    'original_unit': 'core-001:unit:4886f819fa316d28', 'path': 'src/WebAuthn/CLAUDE.md',
    'original_supported_verdict': 'sampling/coordinator/read-boundary-WebAuthn-verdicts.md; preserved, superseded for this unit',
    'coordinator_retraction': retraction, 'effective_credit': 0,
    'source_evidence': 'Completed independent review: Core CreateLogger<T> is internal; Core friend lists omit WebAuthn and its integration tests. A CTAP2 reference supplies no transitive friendship.',
    'WA_patch_sha256': wa_patch, 'WA_content_review': wa_review, 'WA_transfer_receipt': 'editing/integration/accepted-WA-logging.json',
    'root_patch_sha256': root_patch, 'root_content_review': root_review, 'root_transfer_receipt': 'editing/integration/accepted-root-logging.json',
    'before_body_sha256': '84e2288472121c31a22e5277e4b100a74ec61527c11bb24626f28ff8dd762fba',
    'accepted_corrected_body_sha256': '692631d7b90528c64bbbe5d5b8b0f626bdfac9f49a1d3caab29414102fb6b860',
    'state': 'Both documentation patches review PASS and TRANSFERRED; corrected WA body compiled in accepted examples-full-webauthn-second',
    'corrected_body_compilation_record': 'validation/examples/full/attempts/examples-full-webauthn-second/accepted-compilation.json',
    'corrected_body_compilation_sha256': '14bb63b6209d76f081e611fc7ff7df1c3d621ae6408a05c5e66d7d0543605278',
    'runtime_API_IVT_change': False,
    'candidate_links': ['CORE-C-C008', 'CORE-WA-X012'],
    'limit': 'Genuine compiler-body accessibility error, not merely a wrapper defect; not an observed runtime logging failure or new unique Core implementation bug.',
    'five_actual_WA_expansion_reads': wa_expansion})

logical = load('sampling/final/compiler-WA-read-update/summary.json')
transferred = load('sampling/final/transferred-state/summary.json')
sampler_adjudication = load('sampling/final/transferred-state/adjudication.json')
actual = load('sampling/final/transferred-state/actual-current-read-verdicts.json')
assert len(actual) == len({x['id'] for x in actual}) == 1134
assert logical['actual_unique_reads'] == transferred['actual_unique_reads'] == 1134
assert logical['required_target_sum'] == transferred['required_target_sum'] == 1071
assert logical['historical_broad_population'] == transferred['historical_broad_population'] == 9352
assert logical['new_explicit_supported_readings'] == 5
assert sum(bool(x['final_candidate_credit']) for x in actual) == 1071
assert all(x['accepted_clean_credit'] is None for x in actual)
logging_unit = next(x for x in actual if x['id'] == 'core-001:unit:4886f819fa316d28')
assert logging_unit['final_candidate_credit'] == 0 and logging_unit['mandatory_retraction_overlay']
assert transferred['verified_EOF_only_LF_removals'] == 239
acceptance_path = 'sampling/final/coordinator-acceptance.md'
independent_path = 'validation/closeout-review/sampling-final-review.md'
successor_seal_path = 'sampling/final/post-external-EOF/identity-seal.json'
acceptance = read(acceptance_path)
independent_sampling = read(independent_path)
read('validation/closeout-review/sampling-final-review-identities.json')
successor_seal = load(successor_seal_path)
successor_summary = load('sampling/final/post-external-EOF/summary.json')
successor_adjudication = load('sampling/final/post-external-EOF/adjudication.json')
successor_sha = inputs[successor_seal_path]
assert successor_sha == '9ac3da81963e9fdca7561afe8fa36145725f27c1f43ed4b3888e1b27300ce6f9'
assert successor_sha in acceptance and successor_sha in independent_sampling
assert 'PASS' in acceptance and 'PASS' in independent_sampling
assert successor_summary['current_manifest_sha256'] == inputs['editing/integration/current-products.json']
assert successor_summary['actual_unique_reads'] == 1134 and successor_summary['required_target'] == 1071
assert successor_summary['conservative_population'] == 9352 and successor_summary['roles'] == 94 and successor_summary['owners'] == 17
assert successor_summary['eligibility_losses'] == successor_summary['new_content_reads'] == successor_summary['new_credit_awards'] == 0
assert successor_summary['formerly_recorded_EOF_overlap_now_absent_ids'] == ['core-001:unit:fc1fb9627bba154a']
fixed_sampling_hashes = {str(p.relative_to(RUN)): sha(p.read_bytes()) for p in
    [RUN / acceptance_path, RUN / independent_path, RUN / 'validation/closeout-review/sampling-final-review-identities.json',
     *sorted(p for p in (RUN / 'sampling/final/post-external-EOF').iterdir() if p.is_file())]}
save('sampling-state.json', {'logical_summary': logical, 'transferred_identity_candidate': transferred,
    'transferred_candidate_adjudication': sampler_adjudication, 'actual_read_verdicts': actual,
    'current_status': 'PASS: independent and Astra source-content reading/eligibility/quota gate at seal 9ac3 / map 2626. 1,134 reads / 1,071 counted / 63 withheld / 9,352 conservative basis; 94 roles / 17 owners.',
    'additive_final_acceptance': {'scope': 'SOURCE-CONTENT SAMPLING ONLY', 'sampler_PASS': True,
        'accepted_counted_units': 1071, 'accepted_conservative_quota_basis': 9352, 'actual_reads': 1134, 'withheld_reads': 63,
        'roles': 94, 'owners': 17, 'successor_seal_sha256': successor_sha,
        'current_map_sha256': inputs['editing/integration/current-products.json'],
        'coordinator_record': acceptance_path, 'coordinator_text': acceptance,
        'independent_record': independent_path, 'independent_text': independent_sampling,
        'WA4886': 'RETRACTED/zero counted credit',
        'fc1fb9627bba154a': 'No present overlap after EOF; conservatively held/zero new credit; no extra quota required'},
    'fixed_successor_candidate_fields_preserved': {'summary': successor_summary, 'adjudication': successor_adjudication,
        'limit': 'Sealed pending/null fields remain historical and unmodified; additive independent/coordinator records supply the accepted scope.'},
    'fixed_sampling_record_hashes': fixed_sampling_hashes,
    'identity_checkpoint_limit': 'All 363 product hashes and 241 LF removals reconciled by accepted final source sampling. No compiler, full formatter, runtime or whole-publication acceptance follows.',
    'latest_two_transferred_refresh_paths': ['verification/LinuxHidRouteVerification/Evidence.cs', 'verification/LinuxHidRouteVerification/OperatorScenarios.cs'],
    'original_candidates_preserved': 185, 'new_reading_by_this_reconciliation': 0})


def command_record(lane):
    base = 'validation/commands-v2/' + lane
    receipt = load(base + '/receipt.json')
    stdout = read(base + '/stdout.log')
    stderr = read(base + '/stderr.log')
    return {'receipt': receipt, 'stdout_sha256': inputs[base + '/stdout.log'],
            'stderr_sha256': inputs[base + '/stderr.log'], 'stdout_tail': stdout.splitlines()[-25:],
            'stderr_tail': stderr.splitlines()[-16:]}


contexts = []
all_body_ids = []
for path in sorted((RUN / 'validation/examples/full/attempts').glob('*/accepted-compilation.json')):
    relative = str(path.relative_to(RUN))
    accepted = load(relative)
    command = load(accepted['command_receipt']['path'])
    assert sha(Path(accepted['command_receipt']['path']).read_bytes()) == accepted['command_receipt']['sha256']
    assert command['status'] == 'passed' and command['exit_code'] == 0
    result_paths = list((RUN / 'validation/commands-v2' / path.parent.name / 'toolchain').glob('*/result.json'))
    assert len(result_paths) == 1
    result = load(result_paths[0])
    assert result['status'] == 'passed' and result['warningCount'] == 0
    body_ids = accepted['context']['body_ids']
    all_body_ids += body_ids
    contexts.append({'label': path.parent.name, 'accepted_record': relative, 'accepted_compilation': accepted,
        'context': accepted['context']['name'], 'body_count': len(body_ids), 'body_ids': body_ids,
        'original_command_receipt': command, 'toolchain_result': result,
        'scope': 'Actual zero-warning compile checkpoint at retained profile/source/output identities; no DLL/example/test execution or final-tree simultaneous output equality implied.'})
assert len(contexts) == 11 and len(all_body_ids) == len(set(all_body_ids)) == 302
compiler_closeout = {p.name: load(p) for p in sorted((RUN / 'validation/examples/full/closeout').glob('*.json'))}
compiler_acceptance = compiler_closeout['acceptance.json']
assert compiler_acceptance['accepted_contexts'] == compiler_acceptance['zero_warning_contexts'] == 11
assert compiler_acceptance['selected_full_unique_body_ids'] == 302
assert compiler_acceptance['full_unique_preparations'] == 300 and compiler_acceptance['full_alias_count'] == 2
assert compiler_acceptance['pilot_unique_compilations'] == 17 and compiler_acceptance['total_unique_compilations'] == 317
assert compiler_acceptance['current_product_map']['sha256'] == inputs['editing/integration/current-products.json']
wa_accepted_path = 'validation/examples/full/attempts/examples-full-webauthn-second/accepted-compilation.json'
oath_accepted_path = 'validation/examples/full/attempts/examples-full-oath-first/accepted-compilation.json'
assert inputs[wa_accepted_path] == '14bb63b6209d76f081e611fc7ff7df1c3d621ae6408a05c5e66d7d0543605278'
assert inputs[oath_accepted_path] == 'f86fdfc9dbcdc85c3ca0fa6c37b95016f5b04018f881265ca247d51031787c2f'
wa_failed = command_record('examples-full-webauthn-first')
assert wa_failed['receipt']['status'] == 'failed' and wa_failed['receipt']['exit_code'] != 0
assert 'CS1929' in '\n'.join(wa_failed['stdout_tail'])
tokens = command_record('source-tokens-final-after-eof')
apis = command_record('api-final-after-eof')
docs_patch = command_record('docs-patch-final-first')
docs_complexity = command_record('docs-complexity-final')
assert tokens['receipt']['status'] == apis['receipt']['status'] == docs_patch['receipt']['status'] == 'passed'
assert 'files=1320, failures=0' in '\n'.join(tokens['stdout_tail'])
assert 'files=20, failures=0' in '\n'.join(apis['stdout_tail'])
assert 'Ran 6 tests' in '\n'.join(docs_patch['stderr_tail']) and docs_patch['stderr_tail'][-1] == 'OK'
assert docs_complexity['receipt']['status'] == 'passed' and docs_complexity['receipt']['exit_code'] == 0
docs_result_paths = list((RUN / 'validation/commands-v2/docs-complexity-final/toolchain').glob('*/result.json'))
assert len(docs_result_paths) == 1
docs_result = load(docs_result_paths[0])
assert docs_result['status'] == 'passed' and docs_result['warningCount'] == 0
docs_stdout = read('validation/commands-v2/docs-complexity-final/stdout.log')
for passage in ('Validated 74 active', 'Validated 10 package', '6 diagram entries', '7 diagrams present and fresh', 'files        211', 'methods      63', '0 need action (new or worse), 9 are existing debt'):
    assert passage in docs_stdout, passage
profile = load('validation/checker-binding/reviewed-inputs-final-edits.json')
seal_path = 'validation/checker-binding/attempts/checker-bound-build-final-edits/sealed.json'
seal = load(seal_path)
assert seal['profile']['sha256'] == inputs['validation/checker-binding/reviewed-inputs-final-edits.json']
assert inputs['validation/checker-binding/reviewed-inputs-final-edits.json'].startswith('db834') and inputs[seal_path].startswith('b358')
save('compilation-and-checkpoints.json', {'accepted_context_count': 11, 'accepted_additional_body_count': 302,
    'accepted_additional_unique_preparations': 300, 'alias_count': 2, 'pilot_unique_preparations': 17,
    'total_unique_preparations': 317, 'compiler_owner_closeout': compiler_closeout,
    'count_scope': '302 body IDs include two same-context aliases; 300 additional unique preparations +17 pilot =317, not319. Historical nine-context/298-ID checkpoint remains in preserved drafts.',
    'contexts': contexts, 'WA_failed_body_checkpoint': wa_failed,
    'corrected_WA_body': {'state': 'ACTUAL COMPILE PASS, ZERO WARNINGS', 'body': 'FULL-032', 'retraction': 'logging-body-retraction.json',
        'accepted_compilation': wa_accepted_path, 'sha256': inputs[wa_accepted_path], 'sample_credit_remains': 0},
    'OATH_context': {'state': 'ACTUAL COMPILE PASS, ZERO WARNINGS', 'accepted_compilation': oath_accepted_path, 'sha256': inputs[oath_accepted_path]},
    'pilot': '17-body pilot remains accepted at its original retained profile/output checkpoint; no execution or fresh final-tree compiler credit.',
    'latest_preservation': {'tokens1320': tokens, 'API20': apis,
        'scope': 'PASS after all 239+2 EOF removals and both logging transfers, using the same valid db834 profile/b358 seal.',
        'profile_sha256': inputs['validation/checker-binding/reviewed-inputs-final-edits.json'], 'seal_sha256': inputs[seal_path]},
    'docs_patch_six': docs_patch,
    'docs_complexity_final': {'command': docs_complexity, 'toolchain_result': docs_result,
        'docs': {'active': 74, 'package_READMEs': 10, 'map_entries': 6, 'fresh_images': 7},
        'complexity': {'files': 211, 'methods': 63, 'unchanged_debt': 9, 'new_or_worse': 0}, 'warnings': 0},
    'failure_history': 'Original failed restore/generated-import/context/CS1929 attempts and original schemas/types remain at their paths; successful retained contexts supersede only the matching failed lane, never erase it.',
    'full_final_compilation_complete': True,
    'whole_campaign_acceptance': 'Pending fresh final evidence review, freeze and publication; compiler owner rollup is compiler-only and its normal-build pending prose is historical, superseded by coordinator closeout.'})

coordinator_closeout = read('closeout.md')
coordinator_status = read('status.md')
packages_text = read('validation/final-packages.md')
restoration_text = read('validation/normal-output-restoration.md')
assert 'Verification complete; final evidence review, record freeze and GitHub publication pending.' in coordinator_closeout
assert 'authoritative current gate summary' in coordinator_status
build_pack = command_record('build-pack-final')
unit_final = command_record('unit-tests-final-tree')
final_results = {}
for lane in ('build-pack-final', 'unit-tests-final-tree'):
    paths = list((RUN / 'validation/commands-v2' / lane / 'toolchain').glob('*/result.json'))
    assert len(paths) == 1
    result = load(paths[0])
    assert result['status'] == 'passed' and result['warningCount'] == 0
    assert all(x['status'] == 'passed' for x in result['targets'])
    final_results[lane] = result
assert build_pack['receipt']['status'] == unit_final['receipt']['status'] == 'passed'
assert build_pack['receipt']['exit_code'] == unit_final['receipt']['exit_code'] == 0
unit_projects = final_results['unit-tests-final-tree']['projects']
assert len(unit_projects) == 13 and all(x['outcome'] == 'passed' for x in unit_projects)
assert sum(x['executed'] for x in unit_projects) == 4442
assert sum(x['skipped'] for x in unit_projects) == 5 and sum(x['failed'] for x in unit_projects) == 0
assert '--clean' in build_pack['receipt']['command'] and '--include-docs' in build_pack['receipt']['command']
assert '2.0.0-alpha.2' in build_pack['receipt']['command']
restored_dlls = re.findall(r'^\| ([^|]+) \| `([0-9a-f]{64})` \|$', restoration_text, re.M)
assert len(restored_dlls) == 10 and 'positive control' in restoration_text
assert 'were unset' in restoration_text and 'was absent' in restoration_text
package_rows = re.findall(r'^\| (\w+) \| (\d+) \| (\d+) \| `([0-9a-f]{64})` \|$', packages_text, re.M)
assert len(package_rows) == 10
save('final-validation-results.json', {'state': 'VERIFICATION COMPLETE; fresh final evidence review/freeze/publication pending',
    'authority': {'closeout.md': coordinator_closeout, 'status.md': coordinator_status},
    'ordinary_clean_build_pack': {'command': build_pack, 'result': final_results['build-pack-final'],
        'solution_projects': 42, 'warnings': 0, 'errors': 0},
    'normal_output_restoration': {'record': 'validation/normal-output-restoration.md', 'text': restoration_text,
        'test_DLL_hashes_at_check': dict(restored_dlls), 'injected_marker_absent': 10, 'preserved_positive_controls_present': 10,
        'capture_overrides_unset_before_tests': True,
        'scope': 'Coordinator recorded byte checks before ordinary tests, not present-live-DLL equivalence or execution of injected assemblies; no new DLL load or execution by this reconciliation.'},
    'final_SDK_units': {'command': unit_final, 'result': final_results['unit-tests-final-tree'],
        'executed': 4442, 'projects': 13, 'skipped': 5, 'failed': 0, 'warnings': 0},
    'docs_inclusive_packages': {'record': 'validation/final-packages.md', 'text': packages_text,
        'exact_version': '2.0.0-alpha.2', 'exact_version_count': 10, 'directory_count_including_earlier_versions': 20,
        'packages': [{'module': m, 'bytes': int(b), 'XML_members': int(n), 'sha256': h} for m, b, n, h in package_rows],
        'evidence_scope': 'Coordinator checked archive integrity/ID/version/current README bytes/DLL/XML and external hash-verified preservation. No new archive or assembly execution, package signing or registry publication claim here.'},
    'compiler_owner_rollup': 'validation/examples/full/closeout/acceptance.json',
    'limit': 'All actual verification gates are recorded at their qualified scopes. Failed historical formatter receipts and baseline holds remain explicit; fresh whole-campaign acceptance, freeze and publication remain pending.'})

full_fmt = command_record('format-final-source-check')
folder_fmt = command_record('format-whitespace-final-tree')
assert full_fmt['receipt']['status'] == folder_fmt['receipt']['status'] == 'failed'
runtime = Path('/home/rupicola/Code/y/.core-docs-audit-runtime/core-001')
full_report = load(runtime / 'format-final-source-check.json')
folder_report = load(runtime / 'format-whitespace-final-tree.json')
full_changes = [c for row in full_report for c in row['FileChanges']]
folder_changes = [c for row in folder_report for c in row['FileChanges']]
assert len(full_changes) == 249
assert Counter(x['DiagnosticId'] for x in full_changes)['FINALNEWLINE'] == 239
assert len(folder_changes) == 89
assert Counter(x['DiagnosticId'] for x in folder_changes) == {'WHITESPACE': 73, 'FINALNEWLINE': 16}
baseline = load('editing/format-final/baseline-findings.json')
eof = load('editing/format-final/eof-identities.json')
assert len(baseline['findings']) == 10 and eof['count'] == len(eof['files']) == 239
assert len(receipts['accepted-format-EOF.json']['product_files']) == 239
eof_rows = {x['path']: x for x in eof['files']}
for row in receipts['accepted-format-EOF.json']['product_files']:
    proof = eof_rows[row['path']]
    assert proof['removed_hex'] == '0a' and proof['before_bytes'] == proof['after_bytes'] + 1 and proof['all_nontrailing_bytes_identical']
    assert row['previous_sha256'] == proof['before_sha256'] and row['sha256'] == proof['after_sha256']
    assert products[row['path']]['sha256'] == proof['after_sha256']
fmt_review = read('editing/format-final/review.md')
packaging_review = read('editing/format-final/packaging-review.md')
assert 'PASS' in fmt_review and 'PASS' in packaging_review
external_paths = ['verification/LinuxHidRouteVerification/Evidence.cs', 'verification/LinuxHidRouteVerification/OperatorScenarios.cs']
external_eof = load('editing/format-final/external-scope/eof-identities.json')
external_baseline = load('editing/format-final/external-scope/baseline-findings.json')
external_review = read('editing/format-final/external-scope/review.md')
assert 'PASS' in external_review
external_receipt = receipts['accepted-format-external-EOF.json']
assert external_receipt['patch_sha256'] == 'f5c82111cb86e52a8c5a0de620f487efff3f934d28e529389243aa5c5105e818'
assert len(external_receipt['product_files']) == len(external_eof['files']) == 2
assert len(external_baseline['whitespace_diagnostics']) == 73
assert sum(bool(x['baseline_terminal_lf']) for x in external_baseline['eof_diagnostics']) == 14
external_rows = []
for path in external_paths:
    rows = [x for x in folder_report if str(Path(x['FilePath']).relative_to(ROOT)) == path]
    assert any(c['DiagnosticId'] == 'FINALNEWLINE' for x in rows for c in x['FileChanges'])
    proof = next(x for x in external_eof['files'] if x['path'] == path)
    transfer = next(x for x in external_receipt['product_files'] if x['path'] == path)
    assert proof['removed_hex'] == '0a' and proof['all_preceding_bytes_identical'] and proof['before_bytes'] == proof['after_bytes'] + 1
    assert proof['before_sha256'] == transfer['previous_sha256'] and proof['after_sha256'] == transfer['sha256'] == products[path]['sha256']
    external_rows.append({'path': path, 'current_accepted_sha256': products[path]['sha256'], 'byte_proof': proof,
        'transfer_receipt': 'editing/integration/accepted-format-external-EOF.json',
        'formatter_records': rows, 'classification': 'Two introduced terminal LF corrections independently review PASS and actually transferred; every preceding byte preserved.',
        'product_write_authorized_here': False})
save('formatter-state.json', {'full_verify': {'command': full_fmt, 'diagnostic_records': 249,
        '239_introduced_EOF': 'Independently reviewed and actually removed exactly one terminal LF each; accepted-format-EOF.json',
        'ten_baseline_import_declaration': baseline, 'review': fmt_review, 'packaging_review': packaging_review},
    'folder_whitespace': {'command': folder_fmt, 'requested_CSharp_paths': 310,
        'diagnostic_count': 89, 'diagnostic_counts': dict(Counter(x['DiagnosticId'] for x in folder_changes)),
        'coordinator_classification': {'baseline_code_whitespace_held': 73, 'existing_EOF_baseline_held': 14, 'introduced_EOF_reviewed_and_transferred': 2},
        'classification_authority': 'Completed independent external-scope review and retained source-context records; explicit user preservation. No new pinned formatter replay or semantic audit by this ledger.',
        'external_scope_baseline_records': external_baseline, 'external_scope_review': external_review,
        'last_two_applied': external_rows},
    'policy': 'No blanket formatter-clean PASS. Explicit user preservation keeps code whitespace/import/declaration and existing EOF baselines held; no code/import/decl/policy changes or suppressions are authorized here.',
    'membership_scope': 'Original solution-scoped full verify retains its 294-static/16-unmatched qualification. Independent external-scope review confirms all 310 include paths in the folder whitespace population only; not equivalent semantic/analyzer/project coverage. Both retained formatter receipts remain failed.',
    'final_formatter_clean': False, 'new_formatter_or_source_fix_execution': 0})

prior_runtime = load('triage/final/reconciliation-2/sampling-findings.json')
save('deferred-finding-continuity.json', {'original_count': 185,
    'original_candidate_ids': sorted(old['candidates']), 'inherited_rows': 100, 'inherited_groups': 70, 'PGP_labels': 21,
    'runtime_sample_sites_preserved': {k: prior_runtime['findings'][k] for k in ('SAMPLE-A004', 'PIV-pretry-known-qualification', 'OTP-expansion-pretry-qualification')},
    'compiler_documentation_finding': {'sample': 'SAMPLE-WA-002 / FULL-032', 'record': 'logging-body-retraction.json',
        'existing_candidate_facets': ['CORE-C-C008', 'CORE-WA-X012'], 'no_new_unique_Core_implementation_bug': True},
    'PF02_state': '9/9 coordinator-adjudicated; architecture content PASS and transferred; final gate acceptance remains separate',
    'protected_disposition_authority': '../protected-dispositions.json',
    'qualification_authority': '../deferred-facets.json; predecessor handoff source/revision/actor conditions retained',
    'limits': 'Missing deterministic certificate disposal is not permanent leakage or native failure. OTP/PIV pre-try sites retain caller-specific existing groups. HSM retry-message observation stays RETRACTED; OATH resolutions, PIVIMP-005/OpenPGP/HSM uncertainties and all physical/source/producer/Rust gaps remain.'})

gates = {
    'WA_corrected_body_context': {'status': 'ACTUAL PASS ZERO WARNINGS', 'actual_result': {'path': wa_accepted_path, 'sha256': inputs[wa_accepted_path]}},
    'OATH_context': {'status': 'ACTUAL PASS ZERO WARNINGS', 'actual_result': {'path': oath_accepted_path, 'sha256': inputs[oath_accepted_path]}},
    'two_additional_EOF_review_transfer': {'status': 'REVIEW PASS AND TRANSFERRED', 'actual_result': 'editing/integration/accepted-format-external-EOF.json'},
    'final_sampling_hash_range_quota_adjudication': {'status': 'PASS SOURCE-CONTENT ONLY:1134 reads/1071 counted/63 withheld/9352 basis;94 roles/17 owners;241 LF removals/no eligibility loss',
        'actual_result': {'coordinator': acceptance_path, 'independent': independent_path, 'seal_sha256': successor_sha}},
    'ordinary_uninjected_clean_solution_build': {'status': 'ACTUAL PASS 42-PROJECT RELEASE CLEAN BUILD; ZERO WARNINGS/ERRORS', 'actual_result': 'validation/commands-v2/build-pack-final/receipt.json'},
    'normal_output_restoration': {'status': 'ACTUAL RECORDED PASS BEFORE SDK TESTS:10 DLL MARKERS ABSENT/10 POSITIVE CONTROLS PRESENT; CAPTURE OVERRIDES UNSET', 'actual_result': 'validation/normal-output-restoration.md'},
    'final_tree_unit_lane': {'status': 'ACTUAL FINAL-TREE PASS:4442 executed/13 projects/5 skips/0 failures/0 warnings', 'actual_result': 'validation/commands-v2/unit-tests-final-tree/receipt.json'},
    'final_docs_QA_architecture': {'status': 'ACTUAL PASS:74 active docs/10 package README/6 map/7 fresh images; zero warnings', 'actual_result': 'validation/commands-v2/docs-complexity-final/receipt.json'},
    'docs_inclusive_pack': {'status': 'ACTUAL PASS:10 EXACT alpha.2 PACKAGES; README/DLL/XML/ARCHIVE VALIDATED AND EXTERNALLY PRESERVED;20 DIRECTORY TOTAL INCLUDES OLD10', 'actual_result': 'validation/final-packages.md'},
    'final_complexity': {'status': 'ACTUAL PASS:211 files/63 methods/9 unchanged debt/0 new or worse; zero warnings', 'actual_result': 'validation/commands-v2/docs-complexity-final/receipt.json'},
    'final_formatter_disposition': {'status': 'QUALIFIED/FAILED HISTORICAL RECEIPTS;241 introduced LF removals resolved;14 EOF/73 whitespace/10 import-name baseline holds; no blanket clean claim', 'actual_result': 'triage/final/reconciliation-4/formatter-state.json'},
    'final_source_preservation_and_compiler_admission': {'status': 'ACTUAL PASS AT RECORDED SCOPES:1320 token/20 API;11 compiler contexts/300 additional unique preparations+17 pilot; final363 map', 'actual_result': 'triage/final/reconciliation-4/compilation-and-checkpoints.json'},
    'final_preservation_record_admission_evidence_review': {'status': 'VERIFICATION COMPLETE; FRESH WHOLE-CAMPAIGN EVIDENCE REVIEW AND RECORD/STAGED-BYTE FREEZE PENDING', 'actual_result': None},
    'publication_remote_identity_external_core_index': {'status': 'ASTRA AFTER FREEZE ONLY', 'actual_result': None},
}
save('closing-results-placeholders.json', {'state': 'VERIFICATION COMPLETE; only fresh final review/freeze/publication acceptance remains pending',
    'gates': gates, 'rule': 'Fill only through additive actual coordinator result/adjudication records at exact final identities, preserving these drafts and all failures. No invented PASS/counts.',
    'stop': 'Finish Core Pass-1 publication plus deferred handoff, then stop. No Pass-2 plan/project or new implementation tests.'})
for path, digest in prior.items():
    assert sha((RUN / path).read_bytes()) == digest
for path, digest in fixed_sampling_hashes.items():
    assert sha((RUN / path).read_bytes()) == digest
intermediate_snapshot_count = 0
for directory in (OUT / 'before-final-two-EOF', OUT / 'before-sampling-acceptance', OUT / 'before-final-verification'):
    manifest = json.loads((directory / 'identities.json').read_text())
    for row in manifest['files'].values():
        raw = (directory / row['snapshot']).read_bytes()
        assert sha(raw) == row['sha256'] and len(raw) == row['bytes']
        intermediate_snapshot_count += 1
links_checked = 0
for target in re.findall(r'\]\(([^)]+)\)', (RUN / 'deferred-findings.md').read_text()):
    if '://' not in target:
        assert (RUN / target.split('#')[0]).exists(), ('handoff record link', target)
        links_checked += 1
save('verification.json', {'status': 'PASS bounded records-only final reconciliation; verification complete, fresh final review/freeze/publication pending',
    'source_baseline': PIN, 'original_candidates': 185, 'inherited_rows': 100, 'inherited_groups': 70, 'PGP_labels': 21,
    'current_product_paths': 363, 'current_raw_hash_matches': 363, 'former_queued_patch_count': 5,
    'former_queued_path_count': 9, 'former_queue_current_state': 'TRANSFERRED', 'PF02_adjudicated_surfaces': 9,
    'additional_logging_patches': 'WA fb137 and root b43 both content PASS and TRANSFERRED; corrected WA body actual compile PASS, original sample credit remains retracted',
    'actual_reads': 1134, 'logical_candidate_target': 1071, 'conservative_sampling_basis': 9352,
    'final_sampler_PASS': 'PASS SOURCE-CONTENT ONLY', 'counted_sample_units': 1071, 'withheld_reads': 63,
    'sampling_successor_seal_sha256': successor_sha, 'fixed_sampling_records_preserved': len(fixed_sampling_hashes),
    'accepted_compile_contexts': len(contexts), 'accepted_additional_bodies': len(all_body_ids),
    'accepted_additional_unique_preparations': 300, 'body_ID_aliases': 2, 'pilot_unique_preparations': 17, 'total_unique_preparations': 317,
    'uncompiled_WA_bodies': 0, 'unrun_OATH_bodies': 0, 'introduced_EOF_removed': 241,
    'ordinary_clean_solution_build': 'PASS 42 projects/0 warnings/0 errors',
    'normal_output_restoration': '10 test DLL injected markers absent/10 preserved positive controls present; overrides unset before tests',
    'final_tree_unit_lane': {'status': 'PASS', 'executed': 4442, 'projects': 13, 'skipped': 5, 'failed': 0, 'warnings': 0},
    'docs_inclusive_pack': 'PASS 10 exact alpha.2 packages validated/preserved;20 directory total includes earlier10',
    'baseline_formatter_conflicts_held': 10, 'folder_code_whitespace_held': 73, 'folder_existing_EOF_held': 14,
    'folder_introduced_EOF_review_pending': 0, 'formatter_clean_claim': False,
    'current_map_sha256': inputs['editing/integration/current-products.json'], 'final_source_scope': '1608 raw paths /363 changed, exactly current product map',
    'token_API_checkpoint': '1320/20 PASS final-after-eof, same db834 profile/b358 seal',
    'docs_patch_tests': '6 PASS', 'docs_complexity_final': 'PASS zero warnings;74/10/6/7 docs;211/63/9 unchanged/0 new-worse complexity',
    'prior_records_preserved_count': len(prior),
    'intermediate_R4_snapshot_files_preserved': intermediate_snapshot_count, 'handoff_local_links_checked': links_checked,
    'predecessor_handoff_sha256': sha(snapshot.read_bytes()), 'input_hashes': inputs,
    'new_product_Git_SDK_native_device_crypto_subagent_actions': 0, 'verification_complete': True,
    'fresh_final_review_freeze_publication': 'PENDING ASTRA/EXISTING OWNERS', 'Core_Pass1_complete': False})
save('output-identities.json', {'source_baseline': PIN,
    'state': 'VERIFICATION COMPLETE; fresh final review/freeze/publication pending; bounded reconciliation author stops writes after verification and hash return',
    'scope': 'Owned bounded reconciliation-4 records, saved intermediate drafts and updated handoff. Excludes this manifest itself; no publication seal.',
    'files': {str(p.relative_to(RUN)): sha(p.read_bytes()) for p in
              [RUN / 'deferred-findings.md'] + sorted(x for x in OUT.rglob('*') if x.is_file() and x != OUT / 'output-identities.json')}})
print(json.dumps({k: v for k, v in json.loads((OUT / 'verification.json').read_text()).items() if k != 'input_hashes'}))
