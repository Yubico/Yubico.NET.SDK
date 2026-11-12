"""Finite Test.Sdk package-source admission oracles; never invoke capture phases or builds."""
import importlib.util
import ast
import base64
import copy
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET
import zipfile

HERE=Path(__file__).resolve().parent
FULL=HERE.parent
ROOT=FULL.parents[9]


def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module


def admission_oracles():
    adapter=load('package_source_admission_adapter',FULL/'capture.py')
    admission=json.loads((FULL/'package-entrypoint-admission.json').read_bytes())
    source=admission['files']['source']['path']
    failures=[]
    for name,project in admission['contexts'].items():
        if source not in adapter.expected_sources(Path(project),[]):failures.append(name)
    assert not failures,'Exact package source missing from reviewed expected population: '+', '.join(failures)
    for name,project in admission['excluded_contexts'].items():
        assert source not in adapter.expected_sources(Path(project),[]),name
    return dict(included_contexts=sorted(admission['contexts']),excluded_contexts=sorted(admission['excluded_contexts']))


def identity(path):
    raw=Path(path).read_bytes();return dict(path=str(path),bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest())


def main():
    result=admission_oracles();adapter=load('finite_package_metadata_oracle',FULL/'capture.py')
    admission=json.loads((FULL/'package-entrypoint-admission.json').read_bytes())
    assert admission['package_key']=='Microsoft.NET.Test.Sdk/18.0.1'
    assert set(admission['contexts'])=={name for name in admission['contexts'] if name.endswith('.IntegrationTests')} and len(admission['contexts'])==7
    assert admission['files']['source']['path']=='/home/rupicola/.nuget/packages/microsoft.net.test.sdk/18.0.1/build/net8.0/Microsoft.NET.Test.Sdk.Program.cs'
    for row in admission['files'].values():assert identity(row['path'])==row
    archive=Path(admission['files']['archive']['path'])
    digest=base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode()
    assert digest==admission['archive_sha512']==Path(admission['files']['archive_sha512']['path']).read_text().strip()
    assert json.loads(Path(admission['files']['metadata']['path']).read_bytes())['contentHash']==admission['package_content_hash']
    with zipfile.ZipFile(archive) as package:
        for role,member in admission['archive_members'].items():assert package.read(member)==Path(admission['files'][role]['path']).read_bytes()
    producer=ET.parse(admission['files']['producer']['path']).getroot();ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
    assert producer.attrib['InitialTargets']=='GenerateProgramFile'
    target=producer.find("m:Target[@Name='GenerateProgramFile']",ns)
    assert target.attrib['Condition']=="'$(GenerateProgramFile)' == 'true'"
    assert target.find('m:ItemGroup/m:Compile[@Include]',ns).attrib['Include']=='$(GeneratedProgramFile)'
    frozen=json.loads((HERE/'resolved-contexts.before.json.snapshot').read_bytes());positive=[];first=None
    synthetic_policy=dict(GenerateProgramFile='true',GeneratedProgramFile=admission['files']['source']['path'],NuGetPackageRoot='/home/rupicola/.nuget/packages/',Language='C#')
    for context in frozen['contexts']:
        for row in context['files'].values():
            raw=row['content'].encode();assert len(raw)==row['identity']['bytes'] and hashlib.sha256(raw).hexdigest()==row['identity']['sha256']
        assets=json.loads(context['files']['assets']['content']);props=context['files']['nuget_props']['content'];targets=context['files']['nuget_targets']['content'];project=Path(context['project']['path'])
        if context['context'] in admission['contexts']:
            adapter.test_sdk_metadata(assets,props,targets,admission,project,synthetic_policy)
            positive.append(context['context'])
            if first is None:first=(assets,props,targets,project)
        else:
            assert admission['package_key'] not in assets['libraries'] and 'microsoft.net.test.sdk/' not in targets
    assert sorted(positive)==result['included_contexts']
    assets,props,targets,project=first;negative=[]
    cases=[]
    for name,value in [('GenerateProgramFile','false'),('GeneratedProgramFile','/tmp/other/Microsoft.NET.Test.Sdk.Program.cs'),('NuGetPackageRoot','/tmp/packages/'),('Language','VB')]:
        policy=dict(synthetic_policy);policy[name]=value;cases.append((name,copy.deepcopy(assets),props,targets,project,policy))
    wrong=copy.deepcopy(assets);wrong['libraries'][admission['package_key']]['sha512']='foreign';cases.append(('wrong-package-content-hash',wrong,props,targets,project,synthetic_policy))
    wrong=copy.deepcopy(assets);wrong['libraries'][admission['package_key']]['path']='microsoft.net.test.sdk/18.0.2';cases.append(('wrong-package-path',wrong,props,targets,project,synthetic_policy))
    wrong=copy.deepcopy(assets);wrong['libraries'][admission['package_key']]['files'].remove('build/net8.0/Microsoft.NET.Test.Sdk.Program.cs');cases.append(('source-absent-from-assets',wrong,props,targets,project,synthetic_policy))
    wrong=copy.deepcopy(assets);wrong['targets']['net10.0'][admission['package_key']]['build'].pop('build/net8.0/Microsoft.NET.Test.Sdk.targets');cases.append(('producer-absent-from-assets',wrong,props,targets,project,synthetic_policy))
    wrong=copy.deepcopy(assets);wrong['project']['restore']['projectPath']='/tmp/foreign.csproj';cases.append(('foreign-project-assets',wrong,props,targets,project,synthetic_policy))
    cases.append(('producer-import-wrong-version',assets,props,targets.replace('microsoft.net.test.sdk/18.0.1/','microsoft.net.test.sdk/18.0.2/'),project,synthetic_policy))
    cases.append(('producer-import-condition-weakened',assets,props,targets.replace(" '$(ExcludeRestorePackageImports)' != 'true' ",'true'),project,synthetic_policy))
    cases.append(('excluded-project',assets,props,targets,Path(admission['excluded_contexts']['Core.UnitTests']),synthetic_policy))
    for name,a,p,t,selected,policy in cases:
        try:adapter.test_sdk_metadata(a,p,t,admission,selected,policy)
        except (ValueError,KeyError):negative.append(name)
        else:raise AssertionError('Invalid package source/producer admitted: '+name)
    before=(HERE/'before/capture.py.snapshot').read_text();current=(FULL/'capture.py').read_text()
    old_module=ast.parse(before);new_module=ast.parse(current)
    for name in ('kernel','full_descriptors','finish'):
        old=next(node for node in old_module.body if isinstance(node,ast.FunctionDef) and node.name==name)
        new=next(node for node in new_module.body if isinstance(node,ast.FunctionDef) and node.name==name)
        assert ast.dump(old)==ast.dump(new),name
    # The existing unknown-source guard and qualified compiler phase are not broadened.
    guard=before[before.index('    obj = RUNTIME'):before.index("    require(any(Path(path).name == 'Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll'")]
    assert guard in current and "k.compiler(directory)  # Proven observer" in current
    old=ET.fromstring((HERE/'before/capture.targets.snapshot').read_bytes());new=ET.parse(FULL/'capture.targets').getroot()
    policy=new.find("Target[@Name='DocExamplesFullBefore']/WriteLinesToFile[@File='$(DocExamplesCapture)/policy.txt']")
    old_policy=old.find("Target[@Name='DocExamplesFullBefore']/WriteLinesToFile[@File='$(DocExamplesCapture)/policy.txt']")
    addition=';GenerateProgramFile=$(GenerateProgramFile);GeneratedProgramFile=$(GeneratedProgramFile);NuGetPackageRoot=$(NuGetPackageRoot);Language=$(Language)'
    assert policy.attrib['Lines']==old_policy.attrib['Lines']+addition
    policy.attrib['Lines']=old_policy.attrib['Lines'];assert ET.tostring(new)==ET.tostring(old)
    preservation=json.loads((HERE/'preservation.json').read_bytes())
    identities=[preservation['predecessor_profile'],preservation['manifest']]+preservation['failed_attempt_records']+preservation['generated_sources']+[pair['snapshot'] for pair in preservation['participants']]
    accepted=[]
    for context in preservation['successful_contexts']:
        identities+=context['records']+context['retained_outputs'];accepted.append(context['accepted_compilation']['sha256'])
        assert json.loads(Path(context['accepted_compilation']['path']).read_bytes())['profile']==preservation['predecessor_profile']
    for row in identities:assert identity(row['path'])==row,row['path']
    assert len(preservation['generated_sources'])==300
    failed=FULL/'attempts/examples-full-ctap-integration-first'
    assert not any((failed/name).exists() for name in ('before.json','compiler.json','finish.json','accepted-compilation.json'))
    observed=(failed/'sources.txt').read_text().splitlines();source=admission['files']['source']['path'];assert observed.count(source)==1
    kernel=adapter.kernel(project)
    assert hashlib.sha256((FULL.parent/'capture.py').read_bytes()).hexdigest()==adapter.PILOT_SHA
    # The qualified observer already enforces equal before/post items and exact executed Csc sources.
    assert 'sorted(sources) == actual[\'sources\']' in (FULL.parent/'capture.py').read_text()
    return dict(scope='Static exact package/archive/assets/import/source-admission proof; no capture phase, build or test execution.',
                **result,negative_cases_rejected=negative,source_sha256=admission['files']['source']['sha256'],
                producer_sha256=admission['files']['producer']['sha256'],archive_sha512=digest,
                package_metadata_content_hash=admission['package_content_hash'],hash_roles_not_conflated=True,
                package_source_observed_in_failed_before_items=True,qualified_kernel_parser_and_exact_csc_membership_unchanged=True,
                generated_sources_and_manifest_unchanged=300,prior_accepted_compilation_shas=accepted,
                failed_attempt_identities_preserved=len(preservation['failed_attempt_records']),
                effective_policy_qualification='Positive metadata cases use explicit synthetic expected policy; actual effective GenerateProgramFile/GeneratedProgramFile/NuGetPackageRoot/Language are newly observed and required at coordinator retry.',
                actual_retry='Pending focused review and examples-full-ctap-integration-second; no new Csc acceptance or runtime claim.')


if __name__=='__main__':print(json.dumps(main(),indent=2))
