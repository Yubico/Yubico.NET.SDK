"""Finite owning-project helper/import checks for the selected 56 test preparations; no execution."""
import importlib.util
import copy
import hashlib
import json
from pathlib import Path
import re
import sys

HERE=Path(__file__).resolve().parent
FULL=HERE.parent
ROOT=FULL.parents[9]
NAMESPACE='Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions'
IDS={'FULL-302','FULL-303','FULL-306'}
sys.path.insert(0,str(FULL))
from compiler_imports import key


def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module


def scan(manifest):
    providers=json.loads((HERE/'helper-evidence.json').read_bytes())['providers']
    records={row['id']:row for row in manifest['records']};rows=[]
    for ctx in json.loads((FULL/'compiled-contexts-runner-fixed.json').read_bytes())['contexts']:
        if ctx['name']=='consumer':continue
        for path in ctx['injections']:
            id=Path(path).stem;row=records[id];data=row['compiled']
            body=re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"','',row['decoded_body'])
            imports=set(re.findall(r'^[ \t]*using[ \t]+([A-Za-z0-9_.]+);',data['prefix'],re.M));requirements=[]
            for provider in providers:
                if provider['context']!=ctx['name']:continue
                if provider['kind']=='extension':symbols=[name for name in provider['methods'] if re.search(r'\.\s*'+re.escape(name)+r'\s*\(',body)]
                else:symbols=[name for name in provider['classes'] if re.search(r'\b'+re.escape(name)+r'\b',body)]
                if symbols:requirements.append(dict(provider_path=provider['source']['path'],namespace=provider['namespace'],symbols=symbols,import_present=provider['namespace'] in imports,kind=provider['kind']))
            rows.append(dict(id=id,context=ctx['name'],provider_requirements=requirements,missing_namespaces=[item['namespace'] for item in requirements if not item['import_present']]))
    assert len(rows)==56
    return rows


def import_oracles():
    path=FULL/'manifest-helper-imports-fixed.json'
    if not path.exists():path=FULL/'manifest-runner-fixed.json'
    rows=scan(json.loads(path.read_bytes()));missing=[row['id'] for row in rows if row['missing_namespaces']]
    assert not missing,'Missing actual owning-project helper namespace: '+', '.join(missing)
    return rows


def identity(path):
    raw=Path(path).read_bytes();return dict(path=str(path),bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest())


def main():
    rows=import_oracles();old=json.loads((FULL/'manifest-runner-fixed.json').read_bytes());new=json.loads((FULL/'manifest-helper-imports-fixed.json').read_bytes())
    old_rows={row['id']:row for row in old['records']};current={row['id']:row for row in new['records']};assert set(old_rows)==set(current)
    assert {k:v for k,v in old.items() if k not in ('records','actual_full_compilation')}=={k:v for k,v in new.items() if k not in ('records','actual_full_compilation','helper_import_correction')}
    preservation=json.loads((HERE/'preservation.json').read_bytes());snapshot=json.loads((HERE/'generated-sources.before.json.snapshot').read_bytes());saved={row['path']:row for row in snapshot['files']};assert len(saved)==300
    namespaces={match[1] for path in (ROOT/'src').rglob('*.cs') if not {'bin','obj'}.intersection(path.parts) for match in re.finditer(r'^\s*namespace\s+([A-Za-z0-9_.]+)\s*[;{]',path.read_text(),re.M)}
    fault_scan=load('current_finite_body_fault_scan',FULL/'compiler-context-correction/verify.py');checked=set();changed=[]
    for row in new['records']:
        prior=old_rows[row['id']]
        if row['id'] not in IDS:assert row==prior
        else:
            assert {k:v for k,v in row.items() if k!='compiled'}=={k:v for k,v in prior.items() if k!='compiled'}
            assert {k:v for k,v in row['compiled'].items() if k not in ('prefix','generated_sha256')}=={k:v for k,v in prior['compiled'].items() if k not in ('prefix','generated_sha256')}
        if row.get('compilation_status')!='pending-canonical-build':continue
        consumer=row['disposition']=='prepared-consumer-body';data=row['context'] if consumer else row['compiled'];previous=prior['context'] if consumer else prior['compiled']
        if data.get('compilation_alias_of') is not None:continue
        path=ROOT/row['generated_source']['path'] if consumer else FULL/data.get('generated_path',data.get('path'));baseline=saved[str(path)];before=baseline['content'].encode();assert hashlib.sha256(before).hexdigest()==baseline['sha256'];code=path.read_bytes()
        if row['id'] in IDS:
            boundary=previous['prefix'].index('namespace Audit20261111.')
            expected=previous['prefix'][:boundary]+'using '+NAMESPACE+';\n'+previous['prefix'][boundary:]
            assert data['prefix']==expected and code==expected.encode()+before[len(previous['prefix'].encode()):]
            assert code[len(expected.encode()):]==before[len(previous['prefix'].encode()):]
            assert data['generated_sha256']==identity(path)['sha256'];changed.append(row['id'])
        else:assert code==before
        original=previous.get('original_import_prefix',previous.get('preserved_import_prefix',''))
        if original:assert original in data['prefix']
        directives=[key(line) for line in code.decode().splitlines() if key(line)];assert len(directives)==len(set(directives)),row['id']
        assert all(static or not name.startswith('Yubico.YubiKit.') or name in namespaces for static,name in directives),row['id']
        fault_scan.scan(row,data,code.decode());checked.add(str(path))
    assert checked==set(saved) and set(changed)==IDS
    inventory=json.loads((HERE/'helper-evidence.json').read_bytes());profile=json.loads((FULL/'reviewed-inputs-runner-fixed.json').read_bytes());fixed={row['path']:row for row in profile['files']}
    capture=load('unchanged_current_helper_capture',FULL/'capture.py')
    for provider in inventory['providers']:
        assert identity(provider['source']['path'])==provider['source']==fixed[provider['source']['path']]
        assert Path(provider['source']['path']).read_text()==provider['source_text']
        assert 'namespace '+provider['namespace']+';' in provider['source_text']
        assert identity(provider['project']['path'])==provider['project']
        assert provider['source']['path'] in capture.expected_sources(Path(provider['project']['path']),[])
    assert len(inventory['providers'])==6 and len(inventory['projects'])==10
    before=json.loads((HERE/'helper-scan-before.json').read_bytes());assert set(before['missing_ids'])==IDS
    negative=[]
    for label,replacement in [('removed-import',''),('ancestor-namespace','using Yubico.YubiKit.SecurityDomain.IntegrationTests;\n'),('wrong-module-namespace','using Yubico.YubiKit.Oath.IntegrationTests.TestExtensions;\n')]:
        candidate=copy.deepcopy(new)
        for row in candidate['records']:
            if row['id'] in IDS:row['compiled']['prefix']=row['compiled']['prefix'].replace('using '+NAMESPACE+';\n',replacement)
        assert {row['id'] for row in scan(candidate) if row['missing_namespaces']}==IDS;negative.append(label)
    generator=(FULL/'test-contexts.py').read_text();addition="            if group == 'SecurityDomain.IntegrationTests':\n                imports += 'using "+NAMESPACE+";\\n'\n"
    assert generator.count(addition)==1 and generator.replace(addition,'')==(HERE/'before/test-contexts.py.snapshot').read_text()
    retained=[preservation['predecessor_profile'],preservation['generated_sources_snapshot'],preservation['context_plan'],preservation['capture'],preservation['capture_targets'],preservation['run_driver']]+preservation['failed_attempt_records']+preservation['prior_correction_records']+[pair['snapshot'] for pair in preservation['participants']]
    accepted=[]
    for checkpoint in preservation['successful_contexts']:
        retained+=checkpoint['records']+checkpoint['retained_outputs'];accepted.append(checkpoint['accepted_compilation']['sha256'])
        assert json.loads(Path(checkpoint['accepted_compilation']['path']).read_bytes())['profile']==checkpoint['profile']
    for value in retained:assert identity(value['path'])==value,value['path']
    plan=json.loads((FULL/'compiled-contexts-runner-fixed.json').read_bytes());runners=[]
    for ctx in plan['contexts']:
        if ctx['name']=='consumer':continue
        assets=json.loads((ROOT/ctx['project']).parent.joinpath('obj/project.assets.json').read_bytes());v3='xunit.v3.core/3.0.0' in assets['libraries'];assert v3!=('xunit.core/2.9.3' in assets['libraries'])
        for path in ctx['injections']:
            uses=bool(re.search(r'\bTestContext\b',(FULL/path).read_text()));assert not uses or v3,path
            runners.append(dict(id=Path(path).stem,context=ctx['name'],runner='v3' if v3 else 'v2',uses_test_context=uses))
    assert len(runners)==56
    runner=load('preserved_finite_runner_oracles',FULL/'runner-context-correction/verify.py');runner.runner_oracles()
    package=load('preserved_finite_package_oracles',FULL/'package-entrypoint-correction/verify.py');package.admission_oracles()
    actual=json.loads((HERE/'actual-diagnostics.json').read_bytes());assert identity(actual['source_log']['path'])==actual['source_log']
    pattern=re.compile(r'(?P<path>[^\r\n]+?\.cs)\((?P<line>\d+),(?P<column>\d+)\): error (?P<code>\w+): (?P<message>.*?) \[')
    emitted={tuple(match.groupdict()[k].strip() for k in ('path','line','column','code','message')) for match in pattern.finditer(Path(actual['source_log']['path']).read_text())}
    assert len(emitted)==5 and emitted=={tuple(row[k] for k in ('path','line','column','code','message')) for row in actual['unique_diagnostics']}
    triage=json.loads((HERE/'diagnostic-triage.json').read_bytes());assert not triage['true_body_or_api_defects'] and len(triage['diagnostics'])==5
    assert emitted=={tuple(row[k] for k in ('path','line','column','code','message')) for row in triage['diagnostics']}
    helper_path=ROOT/'src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs'
    observed=(FULL/'attempts/examples-full-security-domain-first/sources.txt').read_text().splitlines();assert str(helper_path) in observed
    for diagnostic in triage['diagnostics']:
        assert diagnostic['code']=='CS1061' and diagnostic['classification']=='generated-missing-helper-namespace' and not diagnostic['accepted_body_defect']
        assert saved[diagnostic['path']]['content'].splitlines()[int(diagnostic['line'])-1]==diagnostic['generated_line_text']
        assert identity(diagnostic['helper_source']['path'])==diagnostic['helper_source']
        assert 'public Task '+diagnostic['method']+'(' in helper_path.read_text()
    for source in triage['namespace_authority']:
        assert identity(source['identity']['path'])==source['identity']
        assert Path(source['identity']['path']).read_text().splitlines()[source['first_line']-1:source['last_line']]==source['lines']
    return dict(scope='Static finite owning-project helper/import and exact all-source preservation only; no SDK build/test/capture execution.',
                changed_source_ids=sorted(changed),generated_prefix_directives_added=3,selected_sources=300,test_sources_checked=56,
                all_accepted_bodies_directives_and_nonprefix_bytes_exact=True,all_context_and_project_membership_unchanged=True,
                helper_import_classifications=rows,test_runner_classifications=runners,negative_cases_rejected=negative,
                actual_helper_source_already_in_failed_compile_items=True,all_five_diagnostics_generated_context=True,true_body_or_api_defects=[],
                accepted_checkpoint_shas=accepted,accepted_body_occurrences=sum(ctx['body_occurrences'] for ctx in preservation['successful_contexts']),
                retained_prior_output_files=sum(len(ctx['retained_outputs']) for ctx in preservation['successful_contexts']),
                failed_attempt_identities_preserved=len(preservation['failed_attempt_records']),
                qualification='Finite helper-family scan is not general C# binding. Six accepted checkpoints stay on original profiles; new SD compiler proof and four later contexts remain coordinator-owned.')


if __name__=='__main__':print(json.dumps(main(),indent=2))
