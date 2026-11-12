#!/usr/bin/env python3
"""Logical actual-verdict join only; deliberately no live product or formatter-owner access."""
import collections
import hashlib
import json
from pathlib import Path
import re

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
FINAL=RUN/'sampling/final';PREVIOUS=FINAL/'compiler-WA-addendum';OUT=FINAL/'compiler-WA-read-update'
RETRACTED='core-001:unit:4886f819fa316d28';ROLE=('WebAuthn','narrative-examples-and-maps')
inputs={}


def sha(b):return hashlib.sha256(b).hexdigest()


def read(p):
    b=Path(p).read_bytes();inputs[str(p)]=sha(b);return b


def load(p):return json.loads(read(p))


def emit(name,data):(OUT/name).write_text(json.dumps(data,indent=2)+'\n')


read(Path(__file__));read(OUT/'eof_interval_policy.py')
prior=load(PREVIOUS/'summary.json');assert prior['actual_unique_reads']==1129 and prior['supported_candidate_after_retraction']==1066
assert prior['new_target_sum']==1071 and prior['new_unread_proposals']==5
# Seal only predecessor sampling records. Do not follow old receipts into live product paths.
predecessor_seal={}
for base in [FINAL,PREVIOUS]:
    for p in sorted(base.iterdir()):
        if p.is_file():predecessor_seal[str(p)]=sha(read(p))
for r in load(PREVIOUS/'structural-check.json')['sealed_outputs']:assert sha(read(r['path']))==r['sha256']
oldactual=load(FINAL/'actual-read-verdicts.json');assert len(oldactual)==1129
maps=load(PREVIOUS/'refreshed-current-read-mapping.json');credit={r['id']:r['candidate_credit'] for r in maps}
assert len(credit)==1129 and credit[RETRACTED]==0 and sum(credit.values())==1066
overlay=load(PREVIOUS/'retraction-overlay.json');assert overlay['candidate_credit']==0
proposals=load(PREVIOUS/'new-proposals.json');proposed_ids={r['id'] for r in proposals};assert len(proposed_ids)==5
path=RUN/'sampling/coordinator/compiler-WA-expansion-verdicts.md';raw=read(path);text=raw.decode()
verdicts=[dict(suffix=m[1],verdict_text=m[2]) for line in text.splitlines() if (m:=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line))]
assert len(verdicts)==5 and len({r['suffix'] for r in verdicts})==5
assert {'core-001:unit:'+r['suffix'] for r in verdicts}==proposed_ids
assert all(r['verdict_text'].startswith('Supported') for r in verdicts)
joined=[]
for old in oldactual:
    authority=read(old['authority']);assert sha(authority)==old['authority_sha256']
    payload=old['raw_verdict_payload'];authority_text=authority.decode();suffix=old['id'].split(':')[-1]
    if old['authority'].endswith('.json'):assert next(r for r in json.loads(authority_text)['units'] if r['id']==old['id'])==payload
    elif 'coordinator_list' in payload:assert payload['coordinator_list'] in authority_text and '`'+suffix+'`' in payload['coordinator_list']
    elif 'scope' in payload:assert payload['verdict_text'] in authority_text and '`'+suffix+'`' in payload['verdict_text']
    else:assert '| `'+suffix+'` | '+payload['verdict_text']+' |' in authority_text or '- `'+suffix+'`: '+payload['verdict_text'] in authority_text
    joined.append(dict(id=old['id'],owner=old['owner'],stratum=old['stratum'],path=old['path'],
        source_sha256=old['source_sha256'],range_sha256=old['range_sha256'],raw_category=old['raw_category'],
        raw_verdict_payload=payload,authority=old['authority'],authority_sha256=old['authority_sha256'],actually_read_by='Astra',
        prior_actual_record=str(FINAL/'actual-read-verdicts.json')+'#'+old['id'],
        logical_candidate_credit_before_final_range_refresh=credit[old['id']],
        mandatory_retraction_overlay=str(PREVIOUS/'retraction-overlay.json') if old['id']==RETRACTED else None,
        final_accepted_clean_credit=None,preparer_content_verdict=None))
existing_ids={r['id'] for r in joined};assert not existing_ids&proposed_ids
newreads=[]
for verdict in verdicts:
    key='core-001:unit:'+verdict['suffix'];p=next(r for r in proposals if r['id']==key)
    row=dict(id=key,owner=p['owner'],stratum=p['stratum'],path=p['path'],source_sha256=p['source_sha256'],range_sha256=p['range_sha256'],
        raw_category='supported-pending-final-eligibility',raw_verdict_payload=verdict,authority=str(path),authority_sha256=sha(raw),actually_read_by='Astra',
        original_proposal=str(PREVIOUS/'new-proposals.json')+'#'+key,original_rank=p['original_rank'],
        logical_candidate_credit_before_final_range_refresh=1,final_accepted_clean_credit=None,preparer_content_verdict=None,
        identity_state='Exact prior proposal/range identities retained; final transferred-product/hash/range reconciliation still pending')
    joined.append(row);newreads.append(row)
assert len(joined)==len({r['id'] for r in joined})==1134
quotas=[]
for q in load(PREVIOUS/'role-quota-status.json'):
    added=[r['id'] for r in newreads if (r['owner'],r['stratum'])==(q['owner'],q['stratum'])]
    ids=q['candidate_counted_ids']+added;assert len(ids)==len(set(ids))
    quotas.append(dict(owner=q['owner'],stratum=q['stratum'],conservative_original_population=q['conservative_original_population'],
        original_minimum=q['original_minimum'],authorized_expansion=q['previous_authorized_expansion']+q['new_authorized_expansion'],
        required_target=q['required_target'],logical_source_supported_count_before_final_range_refresh=len(ids),logical_counted_ids=ids,
        logical_shortfall=max(0,q['required_target']-len(ids)),actually_read_new_ids=added,unread_proposals_remaining=[],
        post_transfer_identity_and_range_refresh='PENDING EXPLICIT ASTRA FINAL TRANSFER HANDOFF',final_accepted_clean_count=None,no_cross_role_borrowing=True))
assert len(quotas)==94 and sum(q['conservative_original_population'] for q in quotas)==9352
assert sum(q['required_target'] for q in quotas)==1071 and sum(q['authorized_expansion'] for q in quotas)==88
assert sum(q['logical_source_supported_count_before_final_range_refresh'] for q in quotas)==1071
assert sum(q['logical_shortfall'] for q in quotas)==0
assert sum(r['logical_candidate_credit_before_final_range_refresh'] for r in joined)==1071
assert next(r for r in joined if r['id']==RETRACTED)['logical_candidate_credit_before_final_range_refresh']==0
original_ids={r['id'] for r in load(RUN/'sampling/preparation/selection.json')};assert len(original_ids)==983 and original_ids<={r['id'] for r in joined}
owners=[]
for owner in sorted({q['owner'] for q in quotas}):
    qs=[q for q in quotas if q['owner']==owner]
    owners.append(dict(owner=owner,actual_unique_reads=sum(r['owner']==owner for r in joined),required_target=sum(q['required_target'] for q in qs),
        logical_supported_count=sum(q['logical_source_supported_count_before_final_range_refresh'] for q in qs),logical_shortfall=sum(q['logical_shortfall'] for q in qs),
        final_accepted_clean_count=None,identity_refresh='PENDING'))
pending=[dict(scope='SAMPLE-WA-002/FULL-032 WebAuthn guide body',handoff_reference='fb137...',
    reference_is_abbreviated_not_a_verified_full_hash=True,status='User reports accepted guide-body patch changes one fence; final transfer/identity handoff pending',
    product_path='src/WebAuthn/CLAUDE.md',already_in_last_verified_363_path_map=True,corrected_original_unit_credit=0),
    dict(scope='Root logging bullet',handoff_reference='b43...',reference_is_abbreviated_not_a_verified_full_hash=True,
        status='User reports pending review; exact final patch/transfer identities not inspected or inferred',product_path='CLAUDE.md'),
    dict(scope='Dedicated format-owner final-newline changes',reported_file_count=239,
        status='Owner preparing EOF-only C# changes; await explicit final transfer handoff, do not poll owner or scan/re-pin unknown product files',
        actual_EOF_changed_files_verified_in_this_update=0,automatic_semantic_or_whole_file_exclusion=False)]
eof_policy=dict(scope='Future explicit-handoff comparison policy only; no actual formatter files inspected',
    automatic_semantic_exclusion_for_EOF_only=False,automatic_whole_file_exclusion=False,
    required_checks=['Bind explicit accepted before/after hashes and actual transfer receipt; do not infer file list or repin unknown bytes.',
        'Require exact raw prefix identity plus exactly one LF or CRLF appended to a previously unterminated file; preserve BOM and all preceding bytes.',
        'Verify exact original decoded-character interval bytes/range hash and normalized existing mapped interval independently of whole-file hash.',
        'If a successor terminal unit includes the new terminator, record that literal interval/range hash difference explicitly, not as a semantic edit.',
        'Record append byte offset, hex bytes, source/successor lengths and unit intervals; no broad rstrip, whitespace normalization or content-verdict inference.',
        'Any other edit uses ordinary exact changed-span reconciliation; root bullet and WA body remain substantive edits.'],
    comparator='eof_interval_policy.py:compare_eof_append',actual_product_comparisons=0,
    final_product_hash_seal='NOT CREATED; wait explicit Astra final transfer handoff')
summary=dict(state='UPDATED LOGICAL SAMPLING CANDIDATE ONLY; FINAL IDENTITY/RANGE REFRESH PENDING',actual_unique_reads=1134,
    original_actual_reads=983,additional_actual_reads=151,new_explicit_supported_readings=5,original_logging_unit_credit=0,
    historical_broad_population=9352,original_role_minimum_sum=983,authorized_expansion_sum=88,required_target_sum=1071,
    logical_source_supported_candidate_count_before_final_range_refresh=1071,logical_role_shortfall=0,owner_roles=94,owners=17,
    next_proposals=[],preparer_content_verdicts=0,last_previously_verified_primary_path_count=363,
    current_product_hashes_read_or_refreshed_this_update=0,formatter_owner_polled=False,actual_EOF_changes_verified=0,
    final_product_hash_seal=None,final_accepted_denominator=None,final_accepted_clean_count=None,sampler_PASS=None,
    post_transfer_refresh='PENDING EXPLICIT ASTRA FINAL TRANSFER HANDOFF: WA one-fence body, root bullet, EOF-only formatting',
    CTAP23_evidence='Preserve exact Astra verdict for 6afae0cb3a2a58c7, including retained lines18973–18993 and distinction between normative platform SHOULD NOT and SDK missing enforcement; no new preparer source read.')
for p,h in inputs.items():assert sha(Path(p).read_bytes())==h,('Bound record changed',p)
for p,h in predecessor_seal.items():assert sha(Path(p).read_bytes())==h
emit('actual-read-join.json',joined);emit('new-five-actual-verdicts.json',newreads);emit('owner-role-quotas.json',quotas);emit('owner-totals.json',owners)
emit('summary.json',summary);emit('pending-final-transfers.json',pending);emit('EOF-interval-policy.json',eof_policy)
emit('retraction-overlay-retained.json',overlay);emit('predecessor-record-seal.json',predecessor_seal)
report=['# Updated logical sampling candidate — five actual WA readings','','**For Astra and independent review. No final product hash seal or sampler PASS.**','',
    '**1,134 distinct actual Astra reads = 983 original + 151 additions/replacements.** The five explicit supported table rows join exactly to the five prior proposals, with raw scoped verdicts preserved. The CTAP2.3 normative passage reading and its SDK-enforcement distinction remain exactly Astra’s recorded scope, not a new source judgment.',
    '**Logical supported candidate: 1,071 against unchanged target sum 1,071; all 94 owner/roles have zero logical shortfall.** WA narrative is now 8/8. Broad conservative population remains 9,352, original minima total983 and authorized expansions88. No further proposal, category expansion, borrowing or denominator shrink.',
    '`4886f819fa316d28` remains **retracted / zero credit** under SAMPLE-WA-002 / FULL-032. Its earlier manual supported row remains historical; the overlay is mandatory. The predecessor1,067-unit candidate is not revived as final acceptance.',
    '## Identity gates deliberately deferred',
    'The last verified363-path primary map covered the five already-transferred corrections. This update reads only predecessor/actual-verdict records: it does **not** scan live product files, inspect formatter-owner progress or generate an AFTER-product hash seal.',
    'Await Astra’s explicit final handoff for the accepted WA guide-body patch (`fb137...`, one fence), root logging bullet (`b43...`, pending review) and the dedicated owner’s reported239 EOF-only C# changes. These abbreviated references are not treated as verified full hashes. No file list or final bytes are guessed or repinned.',
    '## EOF-only range rule',
    'A changed whole-file hash from an exact terminal LF/CRLF append must not by itself exclude an original semantic unit or whole file. Check original interval bytes/range hash and normalized mapped slice; record the literal appended bytes, offsets and any extended terminal-unit range hash separately. No rstrip or general whitespace exemption. Substantive guide/root edits still use exact changed-span rules.',
    '`EOF-interval-policy.json` and the pure `eof_interval_policy.py` describe this comparison for a future explicit handoff. **Zero actual formatter files were inspected or verified here.**',
    '## Records',
    '- `actual-read-join.json`: all1,134 explicit actual IDs, raw verdicts and logical credit under the retained retraction overlay.',
    '- `owner-role-quotas.json` / `owner-totals.json`: derived1071/1071 logical counts, pending final range acceptance.',
    '- `pending-final-transfers.json`: exact pending scopes and acknowledged unverified abbreviated references.',
    '- `predecessor-record-seal.json`: preservation of prior sampling records, not a final product seal.',
    'Final denominator, accepted clean count, final product hash seal and sampler PASS stay null. After the explicit final transfer handoff, refresh exact identities/ranges and reconcile substantive versus literal-EOF deltas, then obtain final Astra/independent acceptance and existing compiler/capture/publication gates. No product/Git/SDK/subagent work by this preparer. Finish Core Pass1, then stop.']
(OUT/'candidate.md').write_text('\n'.join(report)+'\n')
emit('verification.json',dict(result='RECORDS-ONLY LOGICAL JOIN PREPARED; NO FINAL PRODUCT SEAL',seal_scope='SAMPLING/VERDICT/PREDECESSOR RECORDS ONLY',
    input_sha256=inputs,outputs=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name not in ['verification.json','preparation.stdout.json','structural-check.json']],
    final_product_hash_seal=None,sampler_PASS=None))
print(json.dumps(summary,indent=2))
