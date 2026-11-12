"""Finite final-source/body admission checks; unchanged capture/kernel, no SDK execution."""
import hashlib
import html
import importlib.util
import json
from pathlib import Path
import re

HERE=Path(__file__).resolve().parent
FULL=HERE.parent
ROOT=FULL.parents[9]


def sha(raw):return hashlib.sha256(raw).hexdigest()


def identity(path):
    raw=Path(path).read_bytes();return dict(path=str(path),bytes=len(raw),sha256=sha(raw))


def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module


def main():
    # Pending/candidate records remain historical. Only the explicit completed receipt set is admitted.
    archival=load('preserved_nine_checkpoints',FULL/'webauthn-body-transition/verify.py').main()
    assert archival['accepted_contexts']==9 and archival['retained_local_files']==169
    transitions=json.loads((HERE/'known-transitions.json').read_bytes())
    assert transitions['transition_step_count']==252 and transitions['unique_transition_paths']==250
    assert len(transitions['receipt_inputs'])==9
    old_map=json.loads(Path(transitions['predecessor_map']['path']).read_bytes())
    assert identity(transitions['predecessor_map']['path'])==transitions['predecessor_map']
    assert identity(transitions['pre_final_two_map']['path'])==transitions['pre_final_two_map']
    assert identity(transitions['current_map']['path'])==transitions['current_map']
    assert transitions['current_map']['sha256']=='2626e78f146a0dc1d28f8604632e33a58891a98a13a5c1cb2b7d9ed90225b14b'
    for row in transitions['receipt_inputs']:assert identity(row['identity']['path'])==row['identity']
    for row in transitions['retained_receipt_evidence']:assert identity(row['path'])==row
    products={row['path']:row['sha256'] for row in old_map['product_files']}
    for row in transitions['steps']:
        if row['path'] in products:assert products[row['path']]==row['previous_sha256']
        products[row['path']]=row['sha256']
    current=json.loads(Path(transitions['current_map']['path']).read_bytes())
    assert products=={row['path']:row['sha256'] for row in current['product_files']} and len(products)==363
    for path,digest in products.items():assert sha((ROOT/path).read_bytes())==digest,path
    eof=json.loads((HERE/'eof-239-proof.json').read_bytes());bundle=json.loads(Path(eof['text_bundle']['path']).read_bytes())
    assert identity(eof['text_bundle']['path'])==eof['text_bundle']
    assert eof['paths']==239 and len(bundle['files'])==239 and eof['entries']==478
    assert not eof['archive_in_fixed_profile'] and not eof['binary_archive_in_run']
    assert identity(eof['external_archive_identity']['path'])==eof['external_archive_identity']
    for row in bundle['files']:
        before=row['before']['text'].encode();after=row['after']['text'].encode()
        assert sha(before)==row['before']['sha256'] and sha(after)==row['after']['sha256']
        assert before.endswith(b'\n') and after==before[:-1] and (ROOT/row['path']).read_bytes()==after
        spans=list(re.finditer(rb'<code(?:\s[^>]*)?>(.*?)</code>',before,re.S))
        assert all(not(m.start(1)<=len(before)-1<m.end(1)) for m in spans)
    extra=json.loads((HERE/'eof-external-2-proof.json').read_bytes());two=json.loads(Path(extra['text_bundle']['path']).read_bytes())
    for name in ('receipt','review','review_identities','text_bundle','patch'):assert identity(extra[name]['path'])==extra[name]
    assert extra['paths']==2 and two['entries']==4 and len(two['files'])==2
    assert {row['path'] for row in two['files']}=={'verification/LinuxHidRouteVerification/Evidence.cs','verification/LinuxHidRouteVerification/OperatorScenarios.cs'}
    for row in two['files']:
        before=row['before']['text'].encode();after=row['after']['text'].encode()
        assert sha(before)==row['before']['sha256'] and sha(after)==row['after']['sha256']
        assert before.endswith(b'\n') and after==before[:-1] and (ROOT/row['path']).read_bytes()==after
    # Recheck exact reviewed before/after payloads, including the three comment-only C# closeouts.
    pool={}
    for data in (bundle,two):
        for row in data['files']:
            for stage in ('before','after'):
                raw=row[stage]['text'].encode();pool[sha(raw)]=raw
    for row in transitions['retained_receipt_evidence']:
        raw=Path(row['path']).read_bytes();pool[sha(raw)]=raw
        if Path(row['path']).name=='inputs.json':
            data=json.loads(raw)
            if 'before_source_text' in data:
                source=data['before_source_text'].encode();pool[sha(source)]=source
    for path in products:
        raw=(ROOT/path).read_bytes();pool[sha(raw)]=raw
    for row in transitions['steps']:
        before=pool[row['previous_sha256']];after=pool[row['sha256']]
        if row['receipt'] in ('accepted-format-EOF.json','accepted-format-external-EOF.json'):assert after==before[:-1]
        elif row['path'].endswith('.cs'):
            before_code=[line for line in before.decode().splitlines() if not re.match(r'^\s*//',line)]
            after_code=[line for line in after.decode().splitlines() if not re.match(r'^\s*//',line)]
            assert before_code==after_code,row['path']
    old=json.loads((HERE/'before/manifest-helper-imports-fixed.json.snapshot').read_bytes())
    candidate=json.loads((HERE/'manifest.json').read_bytes())
    baseline={row['id']:row for row in old['records']};current_rows={row['id']:row for row in candidate['records']}
    assert set(baseline)==set(current_rows) and candidate['counts']==old['counts'] and candidate['context_plan']==old['context_plan']
    source_changes=[];raw_changes=[]
    for row in candidate['records']:
        previous=baseline[row['id']]
        if 'raw_body' not in row:assert row==previous;continue
        raw=(ROOT/row['path']).read_bytes();md=row['path'].endswith('.md')
        pattern=rb'^[ \t]*```([^\r\n]*)\r?\n(.*?)^[ \t]*```[^\r\n]*' if md else rb'<code(?:\s[^>]*)?>(.*?)</code>'
        match=list(re.finditer(pattern,raw,re.M|re.S))[row['ordinal']-1];body=match.group(2 if md else 1)
        assert body.decode()==row['raw_body'] and sha(body)==row['raw_sha256']
        assert row['raw_start_byte']==match.start(2 if md else 1) and row['raw_end_byte']==match.end(2 if md else 1)
        assert row['document']==dict(path=row['path'],bytes=len(raw),sha256=sha(raw))
        if row['raw_sha256']!=previous['raw_sha256']:raw_changes.append(row['id'])
        if row['id']!='FULL-032':
            assert row['raw_body']==previous['raw_body'] and row['decoded_body']==previous['decoded_body']
            assert row['authority']==previous['authority'] and row['review']==previous['review']
        else:
            assert row['raw_sha256']=='692631d7b90528c64bbbe5d5b8b0f626bdfac9f49a1d3caab29414102fb6b860'
            assert 'YubiKitLogging.LoggerFactory.CreateLogger<PreviewSignProcessor>()' in row['decoded_body']
            assert row['decoded_body']==html.unescape(body.decode()).strip('\n')
            assert (ROOT/row['authority']['path']).is_file() and (ROOT/row['review']['path']).is_file()
            assert row['predecessor_authority']==previous['authority'] and row['predecessor_review']==previous['review']
            assert sha((ROOT/row['authority']['path']).read_bytes())==row['authority']['sha256']
            assert sha((ROOT/row['review']['path']).read_bytes())==row['review']['sha256']
        if row.get('compilation_status')!='pending-canonical-build':continue
        consumer=row['disposition']=='prepared-consumer-body';data=row['context'] if consumer else row['compiled']
        if data.get('compilation_alias_of') is not None:continue
        path=ROOT/row['generated_source']['path'] if consumer else FULL/data.get('generated_path',data.get('path'))
        code=path.read_bytes();old_data=previous.get('context',previous.get('compiled'))
        expected_digest=previous['generated_source']['sha256'] if consumer else old_data['generated_sha256']
        if sha(code)!=expected_digest:source_changes.append(row['id'])
        if row['id']=='FULL-032':
            assert data['path']==old_data['path'] and data['suffix']==old_data['suffix']
            assert data['prefix']==old_data['prefix'].replace('YubiKitLogging.CreateLogger<PreviewSignProcessor>()','YubiKitLogging.LoggerFactory.CreateLogger<PreviewSignProcessor>()',1)
            assert sha(code)==data['generated_sha256']
            old_decl=previous['decoded_body'].split('\n\n',1)[0];new_decl,statement=row['decoded_body'].split('\n\n',1)
            assert statement==previous['decoded_body'].split('\n\n',1)[1]
            original=(HERE/'before/injections/WebAuthn.IntegrationTests/FULL-032.cs.snapshot').read_bytes()
            assert code==original.replace(old_decl.encode(),new_decl.encode(),1)
        else:assert sha(code)==expected_digest
    assert raw_changes==source_changes==['FULL-032']
    match=json.loads((HERE/'body-checkpoint-match.json').read_bytes())
    assert match['unchanged_bodies_and_generated_sources']==299 and match['original_accepted_distinct_compilations']==297
    pilot=json.loads((HERE/'pilot-source-match.json').read_bytes());assert len(pilot['rows'])==41
    for row in pilot['rows']:
        raw=(ROOT/row['path']).read_bytes();assert sha(raw[row['raw_start_byte']:row['raw_end_byte']])==row['original_raw_body_sha256']
    before=json.loads((HERE/'preservation.json').read_bytes())
    for row in before['before']:
        assert identity(row['snapshot']['path'])==row['snapshot']
        if row['original']['path'].endswith(('capture.py','capture.targets','run.py','compiled-contexts-runner-fixed.json','package-entrypoint-admission-runner-fixed.json')):
            assert identity(row['original']['path'])==row['original']
    old_profile=json.loads((HERE/'before/reviewed-inputs-helper-imports-fixed.json.snapshot').read_bytes());fixed={row['path']:row for row in old_profile['files']}
    for relative in ('scripts/toolchain/Process.cs','scripts/toolchain/Results.cs','scripts/toolchain/TestTargets.cs','src/Core/src/Yubico.YubiKit.Core.csproj','src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj'):
        assert identity(ROOT/relative)==fixed[str(ROOT/relative)],relative
    wa_rows=[row for row in candidate['records'] if row['path']=='src/WebAuthn/CLAUDE.md'];assert len(wa_rows)==8
    assert all(row['document']['sha256']=='ae35d3238b647fef94c050537f7fb8a2f17e816a4a49eccdd78a5089068a838b' for row in wa_rows)
    assert all(row['compiled']['source_document']==row['document'] for row in wa_rows if 'compiled' in row)
    shared=old['records'][0]['authority']['path']
    assert [row['id'] for row in candidate['records'] if row.get('authority',{}).get('path')==shared]==[row['id'] for row in old['records'] if row.get('authority',{}).get('path')==shared and row['id']!='FULL-032']
    return dict(status='FINAL_SOURCE_STATIC_ADMISSION_READY_FOR_FOCUSED_REVIEW_NOT_COMPILER_ACCEPTANCE',completed_receipts=9,whole_file_transition_steps=252,
                unique_transition_paths=250,accepted_product_paths=363,eof_paths_verified=241,source_body_rows_reextracted=538,
                raw_and_generated_body_changes=['FULL-032'],unchanged_canonical_body_source_pairs=299,pilot_bodies_unchanged=41,
                accepted_contexts_preserved=9,accepted_body_occurrences=298,retained_local_files=169,
                source_alias_rows_updated=len([row for row in candidate['records'] if row.get('document')!=baseline[row['id']].get('document')]),
                qualified_toolchain_and_friendship_inputs_unchanged=True,existing_capture_runtime_entrypoint_fixes_unchanged=True,
                scope='Static final source/body/known-receipt reconciliation only; no build/capture/test/native/device/Git/subagent action.',
                qualification='Nine original checkpoints remain original exact evidence. Fresh actual WA then Oath Csc proofs remain coordinator-owned; no old-binary/current-source equivalence claimed.')


if __name__=='__main__':print(json.dumps(main(),indent=2))
