#!/usr/bin/env python3
"""Read-only bounded successor checks, then a write-once seal of new records."""
import hashlib
import json
from pathlib import Path

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
FINAL=RUN/'sampling/final';BASE=FINAL/'transferred-state';OUT=FINAL/'post-external-EOF'


def sha(b):return hashlib.sha256(b).hexdigest()


def load(p):return json.loads(Path(p).read_bytes())


if (OUT/'identity-seal.json').exists():
    existing=load(OUT/'identity-seal.json')
    for p,h in existing['verified_new_input_sha256'].items():assert sha(Path(p).read_bytes())==h
    for r in existing['output_identities']:assert sha(Path(r['path']).read_bytes())==r['sha256']
    print('Immutable successor inputs/outputs verified; no files rewritten.');raise SystemExit(0)
v=load(OUT/'verification.json')
for p,h in v['new_input_sha256'].items():assert sha(Path(p).read_bytes())==h
for r in v['output_identities']:assert sha(Path(r['path']).read_bytes())==r['sha256']
composition=load(OUT/'candidate-composition.json');base_seal_bytes=(BASE/'identity-seal.json').read_bytes()
assert sha(base_seal_bytes)==composition['predecessor_seal_sha256']=='b93a2584b10dc6ee35c264417b452cc3c80d93d5b4183451bdc121cc67ab61fb'
for r in json.loads(base_seal_bytes)['output_identities']:assert sha(Path(r['path']).read_bytes())==r['sha256']
oldmap={r['path']:r['sha256'] for r in load(BASE/'current-products-at-final-capture.json')['product_files']}
current_path=RUN/'editing/integration/current-products.json';current=current_path.read_bytes()
assert sha(current)=='2626e78f146a0dc1d28f8604632e33a58891a98a13a5c1cb2b7d9ed90225b14b'
assert current==(OUT/'current-products-at-successor-capture.json').read_bytes()
newmap={r['path']:r['sha256'] for r in json.loads(current)['product_files']}
proofs=load(OUT/'two-file-byte-proofs.json');paths={r['path'] for r in proofs}
assert len(paths)==2 and len(oldmap)==len(newmap)==363 and oldmap.keys()==newmap.keys()
assert {p for p in oldmap if oldmap[p]!=newmap[p]}==paths
snapshots=load(RUN/'editing/format-final/external-scope/source-snapshots.json')
for r in proofs:
    s=next(x for x in snapshots['files'] if x['path']==r['path']);before=s['before']['text'].encode();after=s['after']['text'].encode()
    assert before.endswith(b'\n') and after==before[:-1] and after==Path(r['path']).read_bytes()
    assert sha(before)==r['before_sha256']==oldmap[r['path']] and sha(after)==r['after_sha256']==newmap[r['path']]
    assert r['removed_hex']=='0a' and r['before_bytes']==r['after_bytes']+1
population={r['id']:r for r in load(BASE/'population-current-range-overlay.json')}
affected=load(OUT/'affected-population-range-overlay.json')
assert len(affected)==sum(r['path'] in paths for r in population.values())
for r in affected:
    old=population[r['id']];assert r['final_candidate_credit']==old['final_candidate_credit']
    m=r['current_mapping'];assert m['current_file_sha256']==newmap[r['path']]
    assert m['semantic_interval_match']==old['current_mapping']['semantic_interval_match']
    if m['semantic_interval_match']:
        b=Path(r['path']).read_bytes();a,z=m['current_normalized_character_interval'];x,y=m['current_literal_character_interval']
        text=b.decode('utf-8-sig');unit=text.replace('\r\n','\n')[a:z];literal=text[x:y]
        assert sha(unit.encode())==m['current_normalized_range_sha256'] and sha(literal.encode())==m['current_literal_range_sha256']
        assert literal.replace('\r\n','\n')==unit
    if r['final_candidate_credit']:assert m['semantic_interval_match'] and not m['semantic_original_span_overlap']
actual={r['id']:r for r in load(BASE/'actual-current-read-verdicts.json')};overlays=load(OUT/'affected-actual-read-map-overlay.json')
assert {r['id'] for r in overlays}=={k for k,r in actual.items() if r['path'] in paths}
for r in overlays:
    old=actual[r['id']]
    assert {k:v for k,v in old.items() if k!='current_mapping'}=={k:v for k,v in r.items() if k!='current_mapping'}
    actual[r['id']]=r
assert len(actual)==1134 and sum(r['final_candidate_credit'] for r in actual.values())==1071
assert actual['core-001:unit:4886f819fa316d28']['final_candidate_credit']==0
quotas=load(BASE/'owner-role-quotas.json');owners=load(BASE/'owner-totals.json');assert len(quotas)==94 and len(owners)==17
for q in quotas:
    counted={k for k,r in actual.items() if r['final_candidate_credit'] and (r['owner'],r['stratum'])==(q['owner'],q['stratum'])}
    assert counted==set(q['counted_actual_ids']) and q['actual_current_supported_candidate_count']==len(counted) and q['shortfall']==0
assert sum(q['conservative_original_population'] for q in quotas)==9352 and sum(q['required_target'] for q in quotas)==1071
summary=load(OUT/'summary.json');assert summary['shortfall']==summary['new_content_reads']==summary['new_credit_awards']==summary['eligibility_losses']==0
assert summary['literal_original_unit_range_differences']==len(load(OUT/'literal-unit-range-deltas.json'))
combined=load(OUT/'composed-input-identities.json')['input_sha256']
for p,h in json.loads(base_seal_bytes)['input_sha256'].items():
    if p not in paths and p!=str(current_path):assert combined[p]==h
for p in paths:assert combined[p]==newmap[p]
assert combined[str(current_path)]==sha(current)
for p,h in v['new_input_sha256'].items():assert sha(Path(p).read_bytes())==h
result=dict(result='BOUNDED TWO-FILE SUCCESSOR VERIFIED; READY FOR ASTRA/FRESH INDEPENDENT REVIEW',actual_unique_reads=1134,
    counted_supported_candidate=1071,target=1071,shortfall=0,conservative_population=9352,roles=94,owners=17,
    changed_source_files=2,current_manifest_paths=363,affected_population_records=len(affected),affected_actual_maps=len(overlays),
    original_unit_literal_range_differences=summary['literal_original_unit_range_differences'],eligibility_losses=0,new_reads=0,new_credit_awards=0,
    predecessor_seal_preserved=True,Astra_adjudication='PENDING REVIEW',fresh_independent_adjudication='PENDING REVIEW',sampler_PASS=None,compiler_complete_claim=False)
(OUT/'structural-check.json').write_text(json.dumps(result,indent=2)+'\n')
seal=dict(state='IMMUTABLE TWO-FILE EOF FINAL CANDIDATE SUCCESSOR',predecessor_seal_path=str(BASE/'identity-seal.json'),predecessor_seal_sha256=sha(base_seal_bytes),
    verified_new_input_sha256=v['new_input_sha256'],composed_input_identities_sha256=sha((OUT/'composed-input-identities.json').read_bytes()),
    current_manifest_sha256=sha(current),output_identities=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='identity-seal.json'],
    Astra_adjudication='PENDING REVIEW',fresh_independent_adjudication='PENDING REVIEW',sampler_PASS=None,
    immutability='Append a hash-bound review/adjudication or successor; do not mutate predecessor or successor records.')
(OUT/'identity-seal.json').write_text(json.dumps(seal,indent=2)+'\n')
print(json.dumps(result,indent=2));print('identity_seal_sha256',sha((OUT/'identity-seal.json').read_bytes()))
