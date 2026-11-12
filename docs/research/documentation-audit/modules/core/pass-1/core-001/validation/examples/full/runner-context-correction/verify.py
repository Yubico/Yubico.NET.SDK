"""Finite xUnit v3 ownership oracle for FULL-061; no SDK or capture phase executes."""
import importlib.util
import hashlib
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

HERE=Path(__file__).resolve().parent
FULL=HERE.parent
ROOT=FULL.parents[9]
PROJECT=ROOT/'src/Management/tests/Yubico.YubiKit.Management.UnitTests/Yubico.YubiKit.Management.UnitTests.csproj'


def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module


def runner_oracles():
    generator=load('runner_owner_generator',FULL/'test-contexts.py')
    assert generator.GROUPS.get('Management.UnitTests')==[61], 'FULL-061 must have genuine Management xUnit v3 ownership'
    assert 61 not in generator.GROUPS['Management.IntegrationTests']
    adapter=load('runner_owner_capture',FULL/'capture.py')
    reporter='/home/rupicola/.nuget/packages/xunit.v3.core/3.0.0/_content/DefaultRunnerReporters.cs'
    assert reporter in adapter.expected_sources(PROJECT,[]), 'New exact v3 context needs existing actual xUnit producer sources'
    return dict(body='FULL-061',context='Management.UnitTests',project=str(PROJECT),hardware_task_remains_compile_only=True)


def identity(path):
    raw=Path(path).read_bytes();return dict(path=str(path),bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest())


def main():
    result=runner_oracles();old=json.loads((FULL/'manifest-contexts-fixed.json').read_bytes());new=json.loads((FULL/'manifest-runner-fixed.json').read_bytes())
    old_rows={row['id']:row for row in old['records']};current={row['id']:row for row in new['records']};assert set(old_rows)==set(current)
    previous=old_rows['FULL-061'];row=current['FULL-061']
    assert {key:value for key,value in previous.items() if key!='compiled'}=={key:value for key,value in row.items() if key!='compiled'}
    assert all(value==current[id] for id,value in old_rows.items() if id!='FULL-061')
    old_source=FULL/previous['compiled']['path'];new_source=FULL/row['compiled']['path']
    expected_prefix=previous['compiled']['prefix'].replace('using Yubico.YubiKit.Tests.Shared;\n','').replace('using Yubico.YubiKit.Tests.Shared.Infrastructure;\n','')
    assert row['compiled']['prefix']==expected_prefix and row['compiled']['suffix']==previous['compiled']['suffix']
    assert new_source.read_bytes()==(expected_prefix+row['decoded_body']+row['compiled']['suffix']).encode()
    assert old_source.read_bytes()==(previous['compiled']['prefix']+previous['decoded_body']+previous['compiled']['suffix']).encode()
    assert identity(new_source)['sha256']==row['compiled']['generated_sha256']
    assert new_source.read_bytes()[len(expected_prefix.encode()):]==old_source.read_bytes()[len(previous['compiled']['prefix'].encode()):]
    plan=json.loads((FULL/'compiled-contexts-runner-fixed.json').read_bytes());old_plan=json.loads((FULL/'compiled-contexts.json').read_bytes())
    assert len(plan['contexts'])==new['counts']['compile_contexts']==11
    assert set(new['counts'])==set(old['counts']) and all(value==new['counts'][key] for key,value in old['counts'].items() if key!='compile_contexts')
    old_contexts={ctx['name']:ctx for ctx in old_plan['contexts']};contexts={ctx['name']:ctx for ctx in plan['contexts']}
    assert set(contexts)-set(old_contexts)=={'Management.UnitTests'}
    for name,value in old_contexts.items():
        expected=dict(value)
        if name=='Management.IntegrationTests':expected=dict(value,injections=[path for path in value['injections'] if path!=previous['compiled']['path']],body_ids=[id for id in value['body_ids'] if id!='FULL-061'])
        assert contexts[name]==expected,name
    assert contexts['Management.UnitTests']==dict(name='Management.UnitTests',project=str(PROJECT.relative_to(ROOT)),injections=[row['compiled']['path']],body_ids=['FULL-061'])
    selected={str(FULL/path) for ctx in plan['contexts'] for path in ctx['injections']}|{str(path) for path in (FULL/'host/src').glob('*.cs')}
    assert len(selected)==300 and str(new_source) in selected and str(old_source) not in selected
    preservation=json.loads((HERE/'preservation.json').read_bytes());saved={value['path']:value for value in preservation['generated_sources']}
    assert selected-set(saved)=={str(new_source)} and set(saved)-selected=={str(old_source)}
    retained=[preservation['predecessor_profile']]+preservation['failed_attempt_records']+preservation['generated_sources']+preservation['prior_correction_records']+[pair['snapshot'] for pair in preservation['participants']]
    accepted=[]
    for checkpoint in preservation['successful_contexts']:
        retained+=checkpoint['records']+checkpoint['retained_outputs'];accepted.append(checkpoint['accepted_compilation']['sha256'])
        assert json.loads(Path(checkpoint['accepted_compilation']['path']).read_bytes())['profile']==checkpoint['profile']
    for value in retained:assert identity(value['path'])==value,value['path']
    generator_scan=load('runner_finite_context_scan',FULL/'compiler-context-correction/verify.py');checked=set()
    for entry in new['records']:
        if entry.get('compilation_status')!='pending-canonical-build':continue
        data=entry.get('context',entry.get('compiled'))
        if data.get('compilation_alias_of') is not None:continue
        path=ROOT/entry['generated_source']['path'] if entry['disposition']=='prepared-consumer-body' else FULL/data.get('generated_path',data.get('path'))
        generator_scan.scan(entry,data,path.read_text());checked.add(str(path))
    assert checked==selected
    test_rows=[]
    for ctx in plan['contexts']:
        if ctx['name']=='consumer':continue
        project=ROOT/ctx['project'];assets=json.loads((project.parent/'obj/project.assets.json').read_bytes())
        v3='xunit.v3.core/3.0.0' in assets['libraries'];v2='xunit.core/2.9.3' in assets['libraries'];assert v3!=v2
        for path in ctx['injections']:
            code=(FULL/path).read_text();mentions=len(re.findall(r'\bTestContext\b',code));assert not mentions or v3,path
            test_rows.append(dict(id=Path(path).stem,context=ctx['name'],runner='v3' if v3 else 'v2',test_context_occurrences=mentions))
    assert len(test_rows)==56 and {entry['id'] for entry in test_rows if entry['test_context_occurrences']}=={'FULL-011','FULL-061'}
    before=json.loads((HERE/'test-runner-scan-before.json').read_bytes());assert before['mismatch_ids']==['FULL-061']
    assert next(entry for entry in before['rows'] if entry['id']=='FULL-061')['runner']=='xunit-v2-2.9.3'
    refs=ET.parse(PROJECT).getroot();assert refs.findtext('PropertyGroup/UseMicrosoftTestingPlatformRunner')=='true'
    assert refs.findtext('PropertyGroup/OutputType')=='Exe' and any(node.attrib.get('Include')=='xunit.v3' for node in refs.findall('.//PackageReference'))
    driver=load('runner_context_closure_oracle',FULL/'run.py');closure=driver.project_closure(PROJECT)
    management=ROOT/'src/Management/src/Yubico.YubiKit.Management.csproj'
    assert management in closure and len(closure)==3
    core_unit=ROOT/'src/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj'
    assert management not in driver.project_closure(core_unit)
    old_union=set().union(*(driver.project_closure(ROOT/ctx['project']) for ctx in old_plan['contexts']))
    new_union=set().union(*(driver.project_closure(ROOT/ctx['project']) for ctx in plan['contexts']))
    assert len(old_union)==25 and len(new_union)==26 and new_union-old_union=={PROJECT}
    evidence=json.loads((HERE/'ownership-evidence.json').read_bytes())
    for name in ('document','selected_project','rejected_core_unit','xunit_v3_symbol_documentation','xunit_v3_compile_assembly'):
        assert identity(evidence[name]['path'])==evidence[name]
    assert evidence['authority_entry']['body']==row['raw_body']
    xml=Path(evidence['xunit_v3_symbol_documentation']['path']).read_text()
    assert all('name="'+symbol+'"' in xml for symbol in evidence['required_symbols'])
    frozen=json.loads((HERE/'management-unit-restored.before.json.snapshot').read_bytes())
    for value in frozen.values():
        raw=value['content'].encode();assert len(raw)==value['identity']['bytes'] and hashlib.sha256(raw).hexdigest()==value['identity']['sha256']
    assets=json.loads(frozen['assets']['content']);assert 'xunit.v3.extensibility.core/3.0.0' in assets['libraries'] and 'Microsoft.NET.Test.Sdk/18.0.1' not in assets['libraries']
    compile_assets=assets['targets']['net10.0']['xunit.v3.extensibility.core/3.0.0']['compile']
    assert 'lib/netstandard2.0/xunit.v3.core.dll' in compile_assets
    admission=json.loads((FULL/'package-entrypoint-admission-runner-fixed.json').read_bytes());old_admission=json.loads((FULL/'package-entrypoint-admission.json').read_bytes())
    assert {key:value for key,value in admission.items() if key!='excluded_contexts'}=={key:value for key,value in old_admission.items() if key!='excluded_contexts'}
    assert admission['excluded_contexts']==dict(old_admission['excluded_contexts'],**{'Management.UnitTests':str(PROJECT)})
    capture=load('runner_package_capture',FULL/'capture.py');assert admission['files']['source']['path'] not in capture.expected_sources(PROJECT,[])
    package_oracle=load('prior_finite_package_oracles',FULL/'package-entrypoint-correction/verify.py');package_oracle.admission_oracles()
    old_frozen=json.loads((FULL/'package-entrypoint-correction/resolved-contexts.before.json.snapshot').read_bytes())
    synthetic=dict(GenerateProgramFile='true',GeneratedProgramFile=admission['files']['source']['path'],NuGetPackageRoot='/home/rupicola/.nuget/packages/',Language='C#')
    for ctx in old_frozen['contexts']:
        if ctx['context'] in admission['contexts']:
            capture.test_sdk_metadata(json.loads(ctx['files']['assets']['content']),ctx['files']['nuget_props']['content'],ctx['files']['nuget_targets']['content'],admission,Path(ctx['project']['path']),synthetic)
    # Exact finite whitelist extension and descriptor selector are the entire capture delta.
    capture_text=(FULL/'capture.py').read_text().replace(", 'Yubico.YubiKit.Management.UnitTests'",'').replace(", 'Management.UnitTests'",'').replace('package-entrypoint-admission-runner-fixed.json','package-entrypoint-admission.json')
    assert capture_text==(HERE/'before/capture.py.snapshot').read_text()
    actual=json.loads((HERE/'actual-diagnostics.json').read_bytes());assert identity(actual['source_log']['path'])==actual['source_log'] and len(actual['unique_diagnostics'])==3
    assert all(value['code']=='CS0103' and "'TestContext'" in value['message'] for value in actual['unique_diagnostics'])
    assert not re.search(r'\b(?:class|struct)\s+TestContext\b',new_source.read_text())
    return dict(scope='Static finite runner-ownership and exact source/membership proof only; no SDK build/test/capture execution.',
                **result,context_count=11,project_closure_count=26,new_project_closure=[str(path) for path in sorted(closure)],
                all_selected_sources=300,selected_test_sources=56,test_runner_classifications=test_rows,
                emitted_diagnostics_generated_context=3,true_body_or_policy_defects=[],body_tokens_and_original_directives_exact=True,
                predecessor_source_retained_not_selected=True,all_300_predecessor_sources_unchanged=True,
                four_accepted_checkpoint_shas=accepted,retained_prior_output_files=sum(len(ctx['retained_outputs']) for ctx in preservation['successful_contexts']),
                package_admission_seven_integration_contexts_unchanged=True,new_unit_uses_existing_xunit_producers=True,
                qualification='Hardware integration semantics remain; new v3 assembly is compile-only, never an injected test execution lane. New context and Management retry still need actual coordinator compiler acceptance.')


if __name__=='__main__':print(json.dumps(main(),indent=2))
