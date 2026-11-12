#!/usr/bin/env python3
"""Tiny immutable successor: two handed-off EOF files and the current product map only."""
import ast
import copy
import difflib
import hashlib
import json
from pathlib import Path
import re

RUN=Path('docs/research/documentation-audit/modules/core/pass-1/core-001')
FINAL=RUN/'sampling/final';BASE=FINAL/'transferred-state';OUT=FINAL/'post-external-EOF'
BASE_SEAL='b93a2584b10dc6ee35c264417b452cc3c80d93d5b4183451bdc121cc67ab61fb'
PATCH='f5c82111cb86e52a8c5a0de620f487efff3f934d28e529389243aa5c5105e818'
MAP='2626e78f146a0dc1d28f8604632e33a58891a98a13a5c1cb2b7d9ed90225b14b'
PATHS={'verification/LinuxHidRouteVerification/Evidence.cs','verification/LinuxHidRouteVerification/OperatorScenarios.cs'}
assert not (OUT/'identity-seal.json').exists(),'Immutable successor already sealed; do not overwrite.'
inputs={}


def sha(b):return hashlib.sha256(b).hexdigest()


def read(p):
    b=Path(p).read_bytes();inputs[str(p)]=sha(b);return b


def load(p):return json.loads(read(p))


def emit(name,data):(OUT/name).write_text(json.dumps(data,indent=2)+'\n')


read(Path(__file__));read(OUT/'verify.py')
seal_bytes=read(BASE/'identity-seal.json');assert sha(seal_bytes)==BASE_SEAL;predecessor=json.loads(seal_bytes)
# Verify immutable predecessor outputs, not the superseded live-product inputs of its old state.
for r in predecessor['output_identities']:assert sha(read(r['path']))==r['sha256']
oldmanifest=load(BASE/'current-products-at-final-capture.json');oldmap={r['path']:r['sha256'] for r in oldmanifest['product_files']}
manifest_path=RUN/'editing/integration/current-products.json';manifest_bytes=read(manifest_path);assert sha(manifest_bytes)==MAP
manifest=json.loads(manifest_bytes);newmap={r['path']:r['sha256'] for r in manifest['product_files']}
assert len(oldmap)==len(newmap)==363 and oldmap.keys()==newmap.keys()
assert {p for p in oldmap if oldmap[p]!=newmap[p]}==PATHS
rp=RUN/'editing/integration/accepted-format-external-EOF.json';receipt=load(rp)
assert receipt['patch_sha256']==PATCH and {r['path'] for r in receipt['product_files']}==PATHS
folder=RUN/'editing/format-final/external-scope';review=load(folder/'review-identities.json')
assert review['decision'].startswith('PASS') and review['patch_sha256']==PATCH and not review['clean_formatter_claim']
bundle_bytes=read(folder/'source-snapshots.json');assert sha(bundle_bytes)==review['snapshot_bundle_sha256'];bundle=json.loads(bundle_bytes)
eof_ids=load(folder/'eof-identities.json');patch=read(folder/'eof-only.patch');assert sha(patch)==PATCH
read(folder/'review.md')
tree=ast.parse(read(FINAL/'prepare.py'));ns={'re':re}
for name in ['parse_patch','apply','normalized']:
    node=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name==name)
    exec(compile(ast.Module(body=[node],type_ignores=[]),str(FINAL/'prepare.py'),'exec'),ns)
parse=ns['parse_patch'];apply=ns['apply'];normalized=ns['normalized'];hunks=parse(patch);assert set(hunks)==PATHS
accepted=parse(read(RUN/'sampling/preparation/provisional-tally/known-accepted-selected-role-scope.diff'))
file_inventory={r['path']:r for r in load(RUN/'inventory/files.json')}
unit_inventory={r['id']:r for r in load(RUN/'inventory/semantic-units.json')}
sources={};proofs=[]
for r in receipt['product_files']:
    p=r['path'];s=next(x for x in bundle['files'] if x['path']==p)
    before=s['before']['text'].encode();after=s['after']['text'].encode()
    assert before.endswith(b'\n') and not before.endswith(b'\r\n') and after==before[:-1]
    assert sha(before)==r['previous_sha256']==oldmap[p] and sha(after)==r['sha256']==newmap[p]
    assert read(p)==after
    assert apply(normalized(before),hunks[p])==normalized(after) and apply(normalized(after),hunks[p],True)==normalized(before)
    original=apply(normalized(before),accepted[p],True) if p in accepted else normalized(before)
    variants=[(bom+original.replace('\n',newline)).encode() for bom in ['', '\ufeff'] for newline in ['\n','\r\n']]
    exact=next(v for v in variants if sha(v)==file_inventory[p]['sha256'])
    assert not original.endswith('\n') and s['baseline_terminal_lf'] is False
    sources[p]=dict(before=before,after=after,original=exact.decode('utf-8-sig'))
    proofs.append(dict(path=p,before_sha256=sha(before),after_sha256=sha(after),before_bytes=len(before),after_bytes=len(after),
        removed_byte_interval=[len(after),len(before)],removed_hex='0a',all_preceding_bytes_exact=True,recorded_pin_terminal_LF=False,
        reconstructed_original_inventory_sha256=sha(exact),semantic_edit=False,whole_file_exclusion=False))

base_population=load(BASE/'population-current-range-overlay.json');base_actual=load(BASE/'actual-current-read-verdicts.json')
affected=[];literal_deltas=[];cleared_overlap=[]
for old in base_population:
    if old['path'] not in PATHS:continue
    r=copy.deepcopy(old);m=r['current_mapping'];p=r['path'];s=sources[p];u=unit_inventory[r['id']]
    body=s['original'][u['start_character']:u['end_character_exclusive']];assert sha(body.encode())==u['range_sha256']
    before=normalized(s['before']);after=normalized(s['after'])
    old_offsets=[0]
    for line in s['original'].splitlines(keepends=True):old_offsets.append(old_offsets[-1]+len(line))
    semantic_spans=[]
    for tag,i,j,a,b in difflib.SequenceMatcher(a=s['original'].replace('\r\n','\n').splitlines(keepends=True),b=after.splitlines(keepends=True),autojunk=False).get_opcodes():
        if tag!='equal':semantic_spans.append(dict(start_character=old_offsets[i],end_character_exclusive=old_offsets[j],old_first_line=i+1,old_line_count=j-i,added_lines=b-a))
    start,end=u['start_character'],u['end_character_exclusive']
    overlaps=[x for x in semantic_spans if (start<x['end_character_exclusive'] and x['start_character']<end) if x['start_character']!=x['end_character_exclusive']] + [x for x in semantic_spans if x['start_character']==x['end_character_exclusive'] and start<=x['start_character']<=end]
    historical_overlap=m['semantic_original_span_overlap']
    m['semantic_original_span_overlap']=overlaps;m['predecessor_span_overlap_preserved_as_history']=historical_overlap
    if historical_overlap and not overlaps:cleared_overlap.append(r['id'])
    m['current_file_sha256']=newmap[p];m['reviewed_EOF_only_file']=True
    if m['semantic_interval_match']:
        a,b=m['current_normalized_character_interval'];x,y=m['current_literal_character_interval']
        before_unit=before[a:b];current_end=min(b,len(after));literal_end=min(y,len(s['after'].decode('utf-8-sig')))
        current_unit=after[a:current_end];literal_unit=s['after'].decode('utf-8-sig')[x:literal_end]
        assert sha(before_unit.encode())==m['current_normalized_range_sha256']
        assert sha(s['before'].decode('utf-8-sig')[x:y].encode())==m['current_literal_range_sha256']
        if current_unit!=before_unit:
            assert b==len(before) and current_unit==before_unit[:-1] and before_unit.endswith('\n')
            delta=dict(id=r['id'],path=p,before_normalized_range_sha256=sha(before_unit.encode()),after_normalized_range_sha256=sha(current_unit.encode()),
                before_literal_range_sha256=m['current_literal_range_sha256'],after_literal_range_sha256=sha(literal_unit.encode()),literal_removed_hex='0a')
            literal_deltas.append(delta);m['literal_original_to_current_terminal_LF_difference']='REMOVED 0a'
            m['state']='SEMANTIC-INTERVAL-MATCH; LITERAL-TERMINAL-LF-DIFFERENCE'
        m.update(current_normalized_character_interval=[a,current_end],current_literal_character_interval=[x,literal_end],
            current_normalized_range_sha256=sha(current_unit.encode()),current_literal_range_sha256=sha(literal_unit.encode()),
            current_first_line=after.count('\n',0,a)+1,current_last_line=after.count('\n',0,max(a,current_end-1))+1)
        assert current_unit==body.replace('\r\n','\n') or (current_unit==body.replace('\r\n','\n')[:-1] and m['literal_original_to_current_terminal_LF_difference'])
    r['EOF_successor_note']='Exactly one LF removed; predecessor counted-slot allocation/content verdict retained. Former semantic-overlap holds are not automatically awarded new credit.'
    r['predecessor_counted_allocation_retained']=True
    assert r['final_candidate_credit']==old['final_candidate_credit']
    if r['final_candidate_credit']:assert m['semantic_interval_match'] and not overlaps
    affected.append(r)
by_affected={r['id']:r for r in affected};actual_overlay=[]
for old in base_actual:
    if old['path'] not in PATHS:continue
    r=copy.deepcopy(old);r['current_mapping']=by_affected[r['id']]['current_mapping'];actual_overlay.append(r)
assert len(base_actual)==len({r['id'] for r in base_actual})==1134
assert sum(r['final_candidate_credit'] for r in base_actual)==1071
assert next(r for r in base_actual if r['id']=='core-001:unit:4886f819fa316d28')['final_candidate_credit']==0
quotas=load(BASE/'owner-role-quotas.json');owners=load(BASE/'owner-totals.json')
assert len(quotas)==94 and len(owners)==17 and sum(q['conservative_original_population'] for q in quotas)==9352
composed={r['id']:r for r in base_actual};composed.update({r['id']:r for r in actual_overlay})
for q in quotas:
    counted={k for k,r in composed.items() if r['final_candidate_credit'] and (r['owner'],r['stratum'])==(q['owner'],q['stratum'])}
    assert counted==set(q['counted_actual_ids']) and len(counted)==q['actual_current_supported_candidate_count'] and q['shortfall']==0
assert sum(q['required_target'] for q in quotas)==1071
summary=dict(state='FINAL TWO-FILE EOF SUCCESSOR CANDIDATE; READY FOR ASTRA/FRESH INDEPENDENT REVIEW',predecessor_seal_sha256=BASE_SEAL,
    actual_unique_reads=len(composed),actual_current_supported_candidate_count=sum(r['final_candidate_credit'] for r in composed.values()),
    required_target=1071,shortfall=sum(q['shortfall'] for q in quotas),conservative_population=9352,roles=len(quotas),owners=len(owners),
    current_manifest_paths=363,current_manifest_sha256=MAP,changed_product_paths=sorted(PATHS),affected_population_range_records=len(affected),affected_actual_read_maps=len(actual_overlay),
    literal_original_unit_range_differences=len(literal_deltas),new_content_reads=0,new_credit_awards=0,eligibility_losses=0,
    formerly_recorded_EOF_overlap_now_absent_ids=cleared_overlap,
    formerly_held_credit_policy='Prior counted allocations remain unchanged; any restored eligibility/surplus award requires explicit Astra adjudication. No current EOF-only difference is labelled substantive.',
    identity_refresh='COMPLETE: two actual files plus exact current map; other identities inherited unchanged from predecessor seal and identical map entries',
    Astra_adjudication='PENDING REVIEW',fresh_independent_adjudication='PENDING REVIEW',accepted_clean_count=None,accepted_denominator=None,sampler_PASS=None,
    compiler_or_full_formatter_completion_claim=False)
for p,h in inputs.items():assert sha(Path(p).read_bytes())==h,('Stable successor input drift',p)
for r in predecessor['output_identities']:assert sha(Path(r['path']).read_bytes())==r['sha256']
emit('affected-population-range-overlay.json',affected);emit('affected-actual-read-map-overlay.json',actual_overlay)
emit('two-file-byte-proofs.json',proofs);emit('literal-unit-range-deltas.json',literal_deltas);emit('summary.json',summary)
(OUT/'current-products-at-successor-capture.json').write_bytes(manifest_bytes)
emit('candidate-composition.json',dict(predecessor_identity_seal=str(BASE/'identity-seal.json'),predecessor_seal_sha256=BASE_SEAL,
    actual_verdict_base=str(BASE/'actual-current-read-verdicts.json'),population_base=str(BASE/'population-current-range-overlay.json'),
    replacement_rule='Replace rows by exact id from affected overlays; all other rows, verdicts, aliases, counted allocations and qualification histories inherit byte-exact predecessor records.',
    actual_overlay='affected-actual-read-map-overlay.json',population_overlay='affected-population-range-overlay.json',
    quotas=str(BASE/'owner-role-quotas.json'),owners=str(BASE/'owner-totals.json'),current_products='current-products-at-successor-capture.json'))
emit('adjudication.json',dict(Astra='PENDING REVIEW',fresh_independent='PENDING REVIEW',identity_refresh='COMPLETE',
    no_product_changes_remaining_reported=True,accepted_clean_count=None,accepted_denominator=None,sampler_PASS=None,
    review_must_be_additive='Append hash-bound adjudication; do not mutate predecessor or successor sealed files.',compiler_complete_claim=False))
emit('new-input-identities.json',inputs)
updated_inputs=dict(predecessor['input_sha256']);updated_inputs.update(inputs)
emit('composed-input-identities.json',dict(input_sha256=updated_inputs,
    inherited_identity_scope='Unchanged predecessor inputs retained; only two live source files and current map are refreshed. Immutable record inputs verified, not a new whole-product scan.',
    predecessor_sha256=BASE_SEAL))
(OUT/'candidate.md').write_text('\n'.join([
    '# Final two-file EOF successor candidate','',
    '**1,134 actual reads; 1,071 counted supported candidates / target1,071; zero shortfall across94 roles/17 owners. Conservative population9,352 unchanged.**','',
    'Only `Evidence.cs`, `OperatorScenarios.cs` and the current363-path map are refreshed. Exact reviewed patch and before/after identities prove one0a removal per file; all preceding bytes match. No new content read, verdict, unit or credit award. The original WA logging sample remains retracted/zero.',
    f"The two files affect **{len(affected)}** predecessor population range records and **{len(actual_overlay)}** actual-read maps; **{len(literal_deltas)}** original-unit literal range-hash differences. Other maps and all counted allocations inherit the byte-exact predecessor. Exact current semantic spans are updated; historical overlap holds are not silently converted into extra credit.",
    'Use `candidate-composition.json` to combine the immutable predecessor with the small exact-id overlays. `summary.json` discloses any former EOF overlap that no longer exists, with counted allocations retained for Astra adjudication.',
    f"Predecessor seal `{BASE_SEAL}` remains byte-exact. Current map SHA-256 `{MAP}` matches the explicit final handoff; its only two changed entries match actual primary bytes.",
    'Astra and fresh independent review are pending review only for this candidate. Accepted totals/denominator and sampler PASS remain null. No compiler/full formatter/SDK/Git/product-write/subagent claim or execution; inherited failed-gate, baseline/context and source-scope qualifications remain unchanged.',
    'After adjudication, finish Core Pass1 publication and precise deferred handoff, then stop.'
])+'\n')
emit('verification.json',dict(state='SUCCESSOR PREPARED; STRUCTURAL VERIFY BEFORE IMMUTABLE SEAL',new_input_sha256=inputs,
    output_identities=[dict(path=str(p),sha256=sha(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(OUT.iterdir()) if p.is_file() and p.name not in ['identity-seal.json','verification.json','structural-check.json']]))
print(json.dumps(summary,indent=2))
