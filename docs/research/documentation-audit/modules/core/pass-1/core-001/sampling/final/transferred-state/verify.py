#!/usr/bin/env python3
"""Verify stable actual bytes/ranges and create a write-once candidate identity seal."""
import ast
import hashlib
import json
from pathlib import Path
import re

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
PREP=RUN/'sampling/preparation';FINAL=RUN/'sampling/final';LOGICAL=FINAL/'compiler-WA-read-update';OUT=FINAL/'transferred-state'
RETRACTED='core-001:unit:4886f819fa316d28'


def sha(b):return hashlib.sha256(b).hexdigest()


def load(p):return json.loads(Path(p).read_bytes())


if (OUT/'identity-seal.json').exists():
    seal=load(OUT/'identity-seal.json')
    for path,digest in seal['input_sha256'].items():assert sha(Path(path).read_bytes())==digest
    for r in seal['output_identities']:assert sha(Path(r['path']).read_bytes())==r['sha256']
    print('Existing immutable identity seal matches all bound inputs and outputs; no files rewritten.')
    raise SystemExit(0)
receipt=load(OUT/'verification.json')
for path,digest in receipt['input_sha256'].items():assert sha(Path(path).read_bytes())==digest,('Input drift',path)
for r in receipt['outputs']:assert sha(Path(r['path']).read_bytes())==r['sha256'],('Output drift',r['path'])
assert receipt['input_sha256']==load(OUT/'input-identities.json')
for path,digest in load(OUT/'predecessor-record-seal.json').items():assert sha(Path(path).read_bytes())==digest
checks=['All predecessor candidates, logical joins and seals remain byte-exact. Every bound input/output and reserved product hash is stable.']
manifest=load(OUT/'current-products-at-final-capture.json');assert manifest==load(RUN/'editing/integration/current-products.json')
current_map={r['path']:r['sha256'] for r in manifest['product_files']};assert len(current_map)==363
for path,digest in current_map.items():assert sha(Path(path).read_bytes())==digest
chain=load(OUT/'accepted-transfer-chain.json');assert len(chain)==8
expected={r['path']:r['sha256'] for r in load(FINAL/'current-products-at-capture.json')['product_files']}
for record in chain:
    assert sha(Path(record['path']).read_bytes())==record['sha256'] and load(record['path'])==record['receipt']
    for product in record['receipt']['product_files']:
        path=product['path'];assert expected.get(path,product['previous_sha256'])==product['previous_sha256'];expected[path]=product['sha256']
assert expected==current_map

tree=ast.parse((FINAL/'prepare.py').read_bytes());ns={'re':re}
for name in ['parse_patch','apply','normalized']:
    node=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(FINAL/'prepare.py'),'exec'),ns)
parse=ns['parse_patch'];apply=ns['apply'];normalized=ns['normalized']
old_packets=load(FINAL/'queued-patch-reconciliation.json');queued={}
for packet in old_packets:
    b=Path(packet['patch']).read_bytes();assert sha(b)==packet['patch_sha256'];queued.update(parse(b))
format_dir=RUN/'editing/format-final';format_receipt=load(RUN/'editing/integration/accepted-format-EOF.json')
snapshot=load(format_dir/'source-snapshots.json');eof={r['path']:r for r in snapshot['files']};assert len(eof)==239
format_patch=(format_dir/'eof-only.patch').read_bytes();assert sha(format_patch)==format_receipt['patch_sha256']
format_hunks=parse(format_patch);assert set(format_hunks)==set(eof)
for row in format_receipt['product_files']:
    p=row['path'];before=eof[p]['before']['text'].encode();after=eof[p]['after']['text'].encode()
    assert before.endswith(b'\n') and not before.endswith(b'\r\n') and after==before[:-1]
    assert sha(before)==row['previous_sha256'] and sha(after)==row['sha256'] and Path(p).read_bytes()==after
    assert apply(normalized(before),format_hunks[p])==normalized(after)
    assert apply(normalized(after),format_hunks[p],True)==normalized(before)
proofs=load(OUT/'EOF-file-byte-proofs.json');assert len(proofs)==239
for p in proofs:
    assert p['removed_hex']=='0a' and p['before_bytes']==p['after_bytes']+1 and p['all_preceding_bytes_exact']
    assert not p['semantic_edit_exclusion'] and not p['whole_file_exclusion']
latest={}
for name in ['accepted-WA-logging.json','accepted-root-logging.json']:
    received=load(RUN/'editing/integration'/name)
    patch=next(r for r in received['retained_records'] if r['sha256']==received['patch_sha256']);b=Path(patch['path']).read_bytes()
    assert sha(b)==received['patch_sha256'];hunks=parse(b)
    review=load(next(r['path'] for r in received['retained_records'] if r['path'].endswith('review-identities.json')))
    assert review.get('outcome',review.get('verdict'))=='PASS'
    for p in received['product_files']:
        after=Path(p['path']).read_bytes();before=apply(normalized(after),hunks[p['path']],True)
        assert sha(after)==p['sha256']
        assert any(sha((bom+before.replace('\n',newline)).encode())==p['previous_sha256'] for bom in ['', '\ufeff'] for newline in ['\n','\r\n'])
        assert apply(before,hunks[p['path']])==normalized(after);latest[p['path']]=hunks[p['path']]
checks.append('All363 final product hashes match the complete8-receipt chain: five prior corrections/nine paths, EOF239 exact one-LF removals, WA body and root bullet. Reviewed patch applications reverse/reconstruct exactly.')

units={r['id']:r for r in load(RUN/'inventory/semantic-units.json')};files={r['path']:r for r in load(RUN/'inventory/files.json')}
pop={r['id']:r for r in load(PREP/'population.json')['units']};broad={k for k,p in pop.items() if p['eligibility']=='candidate-eligible'}
assert len(pop)==14044 and len(broad)==9352
accepted=parse((PREP/'provisional-tally/known-accepted-selected-role-scope.diff').read_bytes());originals={}


def original(path):
    if path not in originals:
        actual=Path(path).read_bytes();before_eof=eof[path]['before']['text'].encode() if path in eof else actual;text=normalized(before_eof)
        if path in latest:text=apply(text,latest[path],True)
        if path in queued:text=apply(text,queued[path],True)
        if path in accepted:text=apply(text,accepted[path],True)
        variants=([actual] if normalized(actual)==text else [])+[(bom+text.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
        raw=next(v for v in variants if sha(v)==files[path]['sha256']);originals[path]=raw.decode('utf-8-sig')
    return originals[path]


rows=load(OUT/'actual-current-read-verdicts.json');byread={r['id']:r for r in rows};prior={r['id']:r for r in load(LOGICAL/'actual-read-join.json')}
assert len(rows)==len(byread)==1134 and byread.keys()==prior.keys()
for key,r in byread.items():
    for field in ['raw_category','raw_verdict_payload','authority','authority_sha256','source_sha256','range_sha256']:
        assert r[field]==prior[key][field]
    assert sha(Path(r['authority']).read_bytes())==r['authority_sha256']
    assert r['final_candidate_credit']<=r['logical_candidate_credit_before_final_range_refresh']
assert byread[RETRACTED]['final_candidate_credit']==0 and byread[RETRACTED]['mandatory_retraction_overlay']
selected={r['id'] for r in load(PREP/'selection.json')};assert len(selected)==983 and selected<=byread.keys()
additional=[r for r in rows if r['id'] not in selected];assert len(additional)==151
assert not [r for r in additional if r['logical_candidate_credit_before_final_range_refresh'] and not r['final_candidate_credit']]
overlay=load(OUT/'population-current-range-overlay.json');byrec={r['id']:r for r in overlay}
assert len(overlay)==len(byrec) and broad<=byrec.keys() and byread.keys()<=byrec.keys() and byrec.keys()<=pop.keys()
for r in overlay:
    u=units[r['id']];o=original(u['path']);body=o[u['start_character']:u['end_character_exclusive']]
    assert sha(body.encode())==u['range_sha256'];m=r['current_mapping'];actual=Path(u['path']).read_bytes();text=normalized(actual)
    assert sha(actual)==m['current_file_sha256']
    if m['semantic_interval_match']:
        a,b=m['current_normalized_character_interval'];literal_start,literal_end=m['current_literal_character_interval']
        actual_unit=text[a:b];expected_unit=body.replace('\r\n','\n')
        if m['literal_original_to_current_terminal_LF_difference']:
            assert u['path'] in eof and expected_unit.endswith('\n') and actual_unit==expected_unit[:-1]
            assert u['end_character_exclusive']==len(o) and b==len(text)
        else:assert actual_unit==expected_unit
        assert sha(expected_unit.encode())==m['original_normalized_range_sha256'] and sha(actual_unit.encode())==m['current_normalized_range_sha256']
        literal=actual.decode('utf-8-sig')[literal_start:literal_end];assert sha(literal.encode())==m['current_literal_range_sha256']
        assert literal.replace('\r\n','\n')==actual_unit
        assert m['current_first_line']==text.count('\n',0,a)+1 and m['current_last_line']==text.count('\n',0,max(a,b-1))+1
    if r['final_candidate_credit']:
        assert r['id'] in broad and r['id'] in byread and r['id']!=RETRACTED
        assert byread[r['id']]['raw_category']=='supported-pending-final-eligibility'
        assert m['semantic_interval_match'] and not m['semantic_original_span_overlap']
    assert r['conservative_denominator_member']==(r['id'] in broad) and r['conservative_population_delta']==0 and not r['whole_file_exclusion']
assert sum(r['final_candidate_credit'] for r in overlay)==1071
assert load(OUT/'EOF-original-unit-range-deltas.json')==[r['current_mapping'] for r in overlay if r['current_mapping'].get('literal_original_to_current_terminal_LF_difference')]
checks.append('All9352 admitted original unit hashes/ranges and all1134 actual read maps verify against exact final current bytes. Reviewed literal EOF differences are separate from semantic edits; no whole-file exclusion or new unit/read. All151 additional records retained, with no newly lost supported reading.')

quotas=load(OUT/'owner-role-quotas.json');logical=load(LOGICAL/'owner-role-quotas.json');assert len(quotas)==len(logical)==94
for q in quotas:
    old=next(r for r in logical if (r['owner'],r['stratum'])==(q['owner'],q['stratum']))
    for field in ['conservative_original_population','original_minimum','authorized_expansion','required_target']:assert q[field]==old[field]
    ids={k for k,r in byrec.items() if r['final_candidate_credit'] and (r['owner'],r['stratum'])==(q['owner'],q['stratum'])}
    assert set(q['counted_actual_ids'])==ids and q['actual_current_supported_candidate_count']==len(ids)
    assert q['shortfall']==0 and len(ids)>=q['required_target']
assert sum(q['conservative_original_population'] for q in quotas)==9352
assert sum(q['original_minimum'] for q in quotas)==983 and sum(q['authorized_expansion'] for q in quotas)==88
assert sum(q['required_target'] for q in quotas)==1071 and sum(q['actual_current_supported_candidate_count'] for q in quotas)==1071
owners=load(OUT/'owner-totals.json');assert len(owners)==17 and sum(r['actual_unique_reads'] for r in owners)==1134
assert sum(r['shortfall'] for r in owners)==0
root=load(OUT/'root-bullet-population-overlay.json');assert [r['id'] for r in root]==['core-001:unit:b05cbf7f92ab5484']
assert not root[0]['conservative_denominator_member'] and root[0]['actual_verdict_authority'] is None
assert root[0]['original_population_state']=='provisionally-excluded-pending-edit-overlap'
prose=load(OUT/'latest-reviewed-prose-byte-proofs.json');assert len(prose['changed_WA_fences'])==1 and prose['changed_WA_fences'][0]['fence']==2
assert not prose['compiler_completion_claim']
checks.append('All94 roles/17 owners meet1071 target on conservative9352 basis. Root bullet hits one already-excluded unsampled unit; no new population shrink. WA fence2 hashes match the accepted one-body correction; logging sample stays retracted.')
summary=load(OUT/'summary.json');assert summary['quota_shortfall']==0 and summary['actual_current_supported_candidate_count']==1071
assert summary['identity_and_unit_range_refresh']=='COMPLETE'
adjudication=load(OUT/'adjudication.json');assert adjudication['Astra']['state']==adjudication['independent']['state']=='PENDING REVIEW'
assert not adjudication['compiler_completion_claim'] and adjudication['sampler_PASS'] is None
assert summary['accepted_clean_count'] is None and summary['accepted_denominator'] is None and summary['sampler_PASS'] is None
for path,digest in receipt['input_sha256'].items():assert sha(Path(path).read_bytes())==digest
checks.append('Stable final capture is ready for sampling adjudication only; no false compiler/formatter/full-validation completion or sampling PASS. Prior artifact qualifications remain preserved.')
result=dict(result='FINAL ACTUAL-STATE STRUCTURAL CHECKS VERIFIED; SAMPLING ADJUDICATION PENDING REVIEW',actual_unique_reads=1134,
    actual_current_supported_candidate=1071,target=1071,shortfall=0,roles=94,owners=17,conservative_population=9352,
    final_product_paths=363,EOF_exact_LF_removals=239,literal_original_unit_terminal_differences=len(load(OUT/'EOF-original-unit-range-deltas.json')),
    additional_actual_units=151,newly_lost_supported_additional_units=0,logging_retracted_credit=0,identity_refresh='COMPLETE',
    Astra_adjudication='PENDING REVIEW',independent_adjudication='PENDING REVIEW',compiler_complete_claim=False,sampler_PASS=None,checks=checks)
(OUT/'structural-check.json').write_text(json.dumps(result,indent=2)+'\n')
seal=dict(state='IMMUTABLE FINAL TRANSFERRED-STATE CANDIDATE INPUT/OUTPUT IDENTITY SEAL',
    input_sha256=receipt['input_sha256'],
    output_identities=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='identity-seal.json'],
    final_product_manifest_sha256=sha((OUT/'current-products-at-final-capture.json').read_bytes()),
    structural_check_sha256=sha((OUT/'structural-check.json').read_bytes()),
    Astra_adjudication='PENDING REVIEW',independent_adjudication='PENDING REVIEW',sampler_PASS=None,
    immutable_policy='Do not mutate sealed files; append a hash-bound adjudication or successor if any input/output changes.')
(OUT/'identity-seal.json').write_text(json.dumps(seal,indent=2)+'\n')
print(json.dumps(result,indent=2));print('identity_seal_sha256',sha((OUT/'identity-seal.json').read_bytes()))
