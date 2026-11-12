#!/usr/bin/env python3
"""Verify logical records only. EOF probes are synthetic, never actual formatter-file evidence."""
import hashlib
import json
from pathlib import Path
import re

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
FINAL=RUN/'sampling/final';PREVIOUS=FINAL/'compiler-WA-addendum';OUT=FINAL/'compiler-WA-read-update'
RETRACTED='core-001:unit:4886f819fa316d28'


def sha(b):return hashlib.sha256(b).hexdigest()


def load(p):return json.loads(Path(p).read_bytes())


receipt=load(OUT/'verification.json')
for path,digest in receipt['input_sha256'].items():
    assert Path(path).absolute().is_relative_to((RUN/'sampling').absolute()),('Unexpected non-sampling input',path)
    assert sha(Path(path).read_bytes())==digest
for r in receipt['outputs']:assert sha(Path(r['path']).read_bytes())==r['sha256']
for path,digest in load(OUT/'predecessor-record-seal.json').items():assert sha(Path(path).read_bytes())==digest
checks=['All sealed inputs are sampling/actual-verdict/predecessor records only; no live product, integration manifest or formatter-owner files read or repinned. Predecessors remain exact.']
old=load(FINAL/'actual-read-verdicts.json');oldby={r['id']:r for r in old};assert len(oldby)==1129
rows=load(OUT/'actual-read-join.json');byid={r['id']:r for r in rows};assert len(rows)==len(byid)==1134
new=load(OUT/'new-five-actual-verdicts.json');newids={r['id'] for r in new}
assert len(new)==len(newids)==5 and not oldby.keys()&newids and byid.keys()==oldby.keys()|newids
proposals=load(PREVIOUS/'new-proposals.json');assert newids=={r['id'] for r in proposals}
authority=(RUN/'sampling/coordinator/compiler-WA-expansion-verdicts.md').read_bytes()
table=[m.groups() for line in authority.decode().splitlines() if (m:=re.fullmatch(r'\| `([0-9a-f]{16})` \| (.*) \|',line))]
assert len(table)==5
for suffix,verdict in table:
    r=byid['core-001:unit:'+suffix]
    assert r['raw_verdict_payload']==dict(suffix=suffix,verdict_text=verdict) and r['authority_sha256']==sha(authority)
    assert r['logical_candidate_credit_before_final_range_refresh']==1
for key,prior in oldby.items():
    r=byid[key]
    for field in ['raw_category','raw_verdict_payload','authority','authority_sha256','source_sha256','range_sha256']:
        assert r[field]==prior[field],('Changed historical actual verdict',key,field)
assert byid[RETRACTED]['logical_candidate_credit_before_final_range_refresh']==0
assert byid[RETRACTED]['mandatory_retraction_overlay']==str(PREVIOUS/'retraction-overlay.json')
assert load(OUT/'retraction-overlay-retained.json')==load(PREVIOUS/'retraction-overlay.json')
assert load(OUT/'retraction-overlay-retained.json')['candidate_credit']==0
ctap=byid['core-001:unit:6afae0cb3a2a58c7']['raw_verdict_payload']['verdict_text']
assert '18973–18993' in ctap and 'SHOULD NOT' in ctap and "SDK's missing enforcement" in ctap
originalids={r['id'] for r in load(RUN/'sampling/preparation/selection.json')}
assert len(originalids)==983 and originalids<=byid.keys() and len(byid.keys()-originalids)==151
checks.append('Exact1134-ID union = 983+151; five explicit supported rows match prior proposals1:1, raw scoped verdicts and CTAP2.3 passage limits preserved, original logging retraction remains zero.')
quotas=load(OUT/'owner-role-quotas.json');previous=load(PREVIOUS/'role-quota-status.json')
assert len(quotas)==len(previous)==94
all_counted=set()
for q in quotas:
    p=next(r for r in previous if (r['owner'],r['stratum'])==(q['owner'],q['stratum']))
    added={k for k in newids if (byid[k]['owner'],byid[k]['stratum'])==(q['owner'],q['stratum'])}
    ids=set(p['candidate_counted_ids'])|added
    assert q['required_target']==p['required_target'] and q['conservative_original_population']==p['conservative_original_population']
    assert q['original_minimum']==p['original_minimum'] and q['authorized_expansion']==p['previous_authorized_expansion']+p['new_authorized_expansion']
    assert len(q['logical_counted_ids'])==len(ids) and set(q['logical_counted_ids'])==ids
    assert q['logical_source_supported_count_before_final_range_refresh']==len(ids)
    assert q['logical_shortfall']==0 and q['unread_proposals_remaining']==[] and q['final_accepted_clean_count'] is None
    assert not all_counted&ids;all_counted|=ids
assert len(all_counted)==1071 and RETRACTED not in all_counted
assert all_counted=={k for k,r in byid.items() if r['logical_candidate_credit_before_final_range_refresh']}
assert sum(q['conservative_original_population'] for q in quotas)==9352
assert sum(q['original_minimum'] for q in quotas)==983 and sum(q['authorized_expansion'] for q in quotas)==88
assert sum(q['required_target'] for q in quotas)==1071
wa=next(q for q in quotas if (q['owner'],q['stratum'])==('WebAuthn','narrative-examples-and-maps'))
assert wa['required_target']==wa['logical_source_supported_count_before_final_range_refresh']==8
owners=load(OUT/'owner-totals.json');assert len(owners)==17 and sum(r['actual_unique_reads'] for r in owners)==1134
assert sum(r['logical_supported_count'] for r in owners)==1071
checks.append('All94 roles/17 owners meet1071 logical target; WA narrative8/8. Conservative9352, minima983 and expansions88 unchanged; no borrowing, new proposals or denominator shrink.')

# Pure comparator probes on synthetic byte strings. Do not locate or read formatter products.
ns={};exec(compile((OUT/'eof_interval_policy.py').read_bytes(),str(OUT/'eof_interval_policy.py'),'exec'),ns)
compare=ns['compare_eof_append'];probes=[]
for label,before,suffix,start,end in [
    ('terminal-LF',b'alpha\nomega',b'\n',6,11),
    ('interior-unit',b'alpha\nomega',b'\n',0,5),
    ('terminal-CRLF-BOM',b'\xef\xbb\xbfalpha\r\nomega',b'\r\n',7,12),
]:
    r=compare(before,before+suffix,start,end)
    assert r['state']=='STRICT-EOF-ONLY-APPEND' and r['literal_append_hex']==suffix.hex()
    assert r['original_interval_bytes_unchanged'] and r['original_interval_sha256']==r['same_interval_successor_sha256']
    assert r['semantic_edit_exclusion'] is False and r['whole_file_exclusion'] is False and r['content_or_sample_credit_granted'] is False
    if r['original_unit_ended_at_EOF']:assert r['successor_unit_including_EOF_sha256']!=r['original_interval_sha256']
    else:assert r['successor_unit_including_EOF_sha256']==r['original_interval_sha256']
    probes.append(dict(synthetic_probe=label,result=r))
for label,before,after in [
    ('semantic-body-change',b'alpha',b'Alpha\n'),('two-newlines',b'alpha',b'alpha\n\n'),
    ('already-terminated',b'alpha\n',b'alpha\n\n'),('whitespace-before-EOF',b'alpha',b'alpha \n'),
    ('normalize-other-lines',b'alpha\r\nomega',b'alpha\nomega\n')]:
    r=compare(before,after,0,len(before.decode()));assert r['state']=='NOT-STRICT-EOF-ONLY-APPEND';probes.append(dict(synthetic_probe=label,result=r))
checks.append('Synthetic EOF probes preserve exact original intervals, report literal LF/CRLF and terminal-unit range-hash differences, and reject substantive/whitespace/repeated-terminator changes. Actual formatter files verified:0.')
summary=load(OUT/'summary.json');assert summary['actual_unique_reads']==1134 and summary['logical_source_supported_candidate_count_before_final_range_refresh']==1071
assert summary['current_product_hashes_read_or_refreshed_this_update']==0 and summary['actual_EOF_changes_verified']==0 and not summary['formatter_owner_polled']
for field in ['final_product_hash_seal','final_accepted_denominator','final_accepted_clean_count','sampler_PASS']:assert summary[field] is None
assert all(r['final_accepted_clean_credit'] is None for r in rows)
pending=load(OUT/'pending-final-transfers.json');assert len(pending)==3 and pending[2]['reported_file_count']==239
assert pending[0]['handoff_reference']=='fb137...' and pending[1]['handoff_reference']=='b43...'
assert pending[0]['reference_is_abbreviated_not_a_verified_full_hash'] and pending[1]['reference_is_abbreviated_not_a_verified_full_hash']
for path,digest in receipt['input_sha256'].items():assert sha(Path(path).read_bytes())==digest
checks.append('No final product hash seal or current-product refresh performed; WA/root substantive patches and EOF239 report await explicit Astra handoff. Final clean count/denominator/sampler PASS remain null.')
result=dict(result='RECORDS-ONLY LOGICAL JOIN VERIFIED; NO FINAL PRODUCT SEAL OR SAMPLER PASS',actual_unique_reads=1134,
    logical_supported_candidate=1071,required_target=1071,logical_shortfall=0,original_logging_credit=0,
    actual_formatter_files_inspected=0,live_product_files_read=0,final_product_hash_seal=None,final_accepted_clean_count=None,sampler_PASS=None,
    checks=checks,synthetic_EOF_probes=probes,
    sealed_sampling_records_only=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='structural-check.json'])
(OUT/'structural-check.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ['sealed_sampling_records_only','synthetic_EOF_probes']},indent=2))
