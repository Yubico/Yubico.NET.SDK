"""One plain full manifest and finite context plan; qualified pilot remains immutable."""
import importlib.util
import json
from pathlib import Path
import hashlib

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[9]
RUN=HERE.parents[2]


def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module


def identity(path):
    raw=path.read_bytes();return dict(path=path.relative_to(ROOT).as_posix(),bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest())


def main():
    inventory=json.loads((HERE/'final-current-inventory.json').read_bytes())
    external=json.loads((HERE/'external-current-inventory.json').read_bytes())
    pilot=json.loads((HERE.parent/'manifest.json').read_bytes())
    decisions=json.loads((HERE/'qualification-decisions.json').read_bytes())['entries']
    builder=load('full_literal_contexts_for_manifest',HERE/'contexts.py')
    source={row['id']:row for row in inventory['entries']}
    tests=json.loads((HERE/'test-context-dispositions-final-draft.json').read_bytes())['contexts']
    extcontexts=json.loads((HERE/'external-context-dispositions.json').read_bytes())
    test_by_id={row['id']:(group,row) for group in tests for row in group['bodies']}
    contexts={'consumer':dict(name='consumer',project=(HERE/'host/src/Yubico.YubiKit.Audit20261111.DocExamples.Full.csproj').relative_to(ROOT).as_posix(),injections=[],body_ids=[])}
    records=[]
    for row in inventory['entries']:
        record=dict(row,execution_status='not-run')
        if row['id'] in decisions:
            record.update(disposition='proposed-qualified-illustration',qualification=decisions[row['id']],compilation_status='not-run-no-counterfeit-context')
        elif row['id'] in test_by_id:
            group,compiled=test_by_id[row['id']]
            record.update(disposition='prepared-real-test-context',compiled=compiled,compilation_status='pending-canonical-build')
            context=contexts.setdefault(group['context'],dict(name=group['context'],project=group['project'],injections=[],body_ids=[]))
            context['body_ids'].append(row['id'])
            if compiled['compilation_alias_of'] is None:context['injections'].append(compiled['path'])
        else:
            chosen=builder.select(dict(row),source)
            if chosen['status']=='prepared-consumer-body':
                path=HERE/'host/src'/(row['id']+'.cs')
                raw=(chosen['prefix']+chosen['body']+chosen['suffix']).encode()
                assert path.read_bytes()==raw,row['id']
                record.update(disposition='prepared-consumer-body',context=chosen,generated_source=identity(path),compilation_status='pending-canonical-build')
                contexts['consumer']['body_ids'].append(row['id'])
            else:
                assert chosen['status'] in ('non-csharp-excluded','non-csharp-layout-excluded','historical-excluded'),(row['id'],chosen['status'])
                record.update(disposition=chosen['status'],qualification=chosen['reason'],compilation_status='not-applicable')
        records.append(record)
    ext_by_id={row['id']:row for row in external['entries']}
    compiled_external={row['id']:row for row in extcontexts['entries']}
    for row in external['entries']:
        record=dict(row)
        if row['id'] in compiled_external:
            compiled=compiled_external[row['id']]
            record.update(disposition='prepared-external-current-body',compiled=compiled,compilation_status='pending-canonical-build')
            name=compiled['context']
            context=contexts['consumer'] if name=='consumer' else contexts.setdefault(name,dict(name=name,project=extcontexts['existing_projects'][name],injections=[],body_ids=[]))
            context['body_ids'].append(row['id'])
            if name!='consumer' and compiled['compilation_alias_of'] is None:context['injections'].append(compiled['generated_path'])
        else:record['disposition']=row['compilation_status']
        records.append(record)
    # Keep historical/base metadata only once per authority record; none is silently promoted to current.
    assert len(records)==662
    prepared=[row for row in records if row.get('compilation_status')=='pending-canonical-build']
    assert len(prepared)==302
    plan=dict(contexts=list(contexts.values()),source_baseline=inventory['source_baseline'],
              policy='Canonical compile only, no test/example execution. Actual test contexts use existing assemblies/fixtures/IVT with run-owned Compile injection; final ordinary clean build restores test outputs before SDK tests.')
    plan_raw=(json.dumps(plan,indent=2)+'\n').encode()
    plan_path=HERE/'compiled-contexts.json'
    if plan_path.exists():assert plan_path.read_bytes()==plan_raw
    else:
        with plan_path.open('xb') as out:out.write(plan_raw)
    result=dict(source_baseline=inventory['source_baseline'],pilot=dict(body_manifest=identity(HERE.parent/'manifest.json'),
                accepted_compilation=identity(HERE.parent/'attempts/examples-ac-first/accepted-compilation.json'),
                mutable_original_preservation=identity(HERE/'pilot-output-preservation.json'),
                compiled_bodies=17,internal_qualified=2,historical_excluded=22,no_bookkeeping_rerun=True),
                records=records,counts=dict(additional_authority_records=662,pilot_records=41,total_authority_records=703,
                additional_prepared_occurrences=302,unique_additional_compilations=300,consumer_bodies=244,
                existing_test_body_occurrences=58,unique_existing_test_bodies=56,proposed_illustration_qualifications=34,
                compile_contexts=len(contexts)),context_plan=identity(HERE/'compiled-contexts.json'),
                qualifications_require_independent_review=True,actual_full_compilation='not-run',
                reuse='Qualified pilot compiler observer and qualified logger/parser remain unchanged. Current source/configuration/output scope is bound separately; no old binary equivalence or six-context proof stack is inferred.')
    with (HERE/'manifest-final.json').open('x') as out:json.dump(result,out,indent=2);out.write('\n')
    print(result['counts'])


if __name__=='__main__':main()
