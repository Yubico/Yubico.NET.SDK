"""Prepare complete current OTP documentation bodies in genuine compiler contexts; never execute."""
from pathlib import Path
import json
import re
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, RUN, scan, sha, file_record

if len(sys.argv) != 2 or not re.fullmatch(r'v[1-9][0-9]*', sys.argv[1]):
    raise SystemExit('Usage: prepare.py <new-vN>')
revision = sys.argv[1]
DEST = HERE / revision
if DEST.exists():
    raise SystemExit(f'Refusing to overwrite an extraction or evidence: {DEST}')
inventory = scan()
readme, guide = 'src/YubiOtp/README.md', 'src/YubiOtp/CLAUDE.md'
interaction, migration = 'docs/usage/user-interaction.md', 'docs/migration/v1-to-v2.md'
blocks = [b for b in inventory['fences'] if b['csharp_index'] is not None]
def select(path, index):
    return next(b for b in blocks if b['path'] == path and b['csharp_index'] == index)

def require(condition, message):
    if not condition:
        raise SystemExit(message)

require(sum(b['path'] == readme for b in blocks) == 9, 'Reconcile README C# block additions/removals')
require(sum(b['path'] == guide for b in blocks) == 3, 'Reconcile CLAUDE C# block additions/removals')
require(sum(b['path'] == interaction for b in blocks) == 4, 'Reconcile all shared interaction examples, including implicit client calls')
otp_migration = [b for b in blocks if b['path'] == migration and b['scope'] == 'otp-migration']
require(len(otp_migration) == 2, 'Reconcile historical/current OTP migration blocks')
require('Yubico.YubiKey.Otp' in otp_migration[0]['body'] and 'Yubico.YubiKit.YubiOtp' in otp_migration[1]['body'], 'Historical/current migration classification changed')
require(sum(r['tag'] == 'code' for r in inventory['xml']) == 1 and not any(r['tag'] == 'example' for r in inventory['xml']), 'New XML code/example requires semantic classification')
require('Offset  Size  Field' in next(r['body'] for r in inventory['xml'] if r['tag']=='code'), 'XML code block is no longer the known non-executable wire table')
DEST.mkdir()
groups = []
selected = set()

def split_imports(body):
    lines = body.splitlines(keepends=True)
    predicate = lambda line: re.fullmatch(r'using [A-Za-z0-9_.]+;\r?\n', line) is not None
    return ''.join(line for line in lines if predicate(line)), ''.join(line for line in lines if not predicate(line))

imports, entry = split_imports(select(readme,1)['body'])
require(bool(imports) and 'IYubiKey device = await YubiKeyManager.FindFirstAsync();' in entry, 'Reconcile entry-point imports/prerequisites')
imports += 'using Yubico.YubiKit.Core.Credentials;\n'

def group(name, originals, body, parameters='', async_method=True, return_type=None,
          prerequisites=None, type_body='', context='consumer', transform='Typed wrapper only; complete body/resources retained verbatim.'):
    raw = '\n'.join(item['body'] for item in originals)
    raw_path = DEST / 'raw' / f'{name}.cs.txt'
    raw_path.parent.mkdir(exist_ok=True)
    raw_path.write_text(raw)
    for item in originals:
        if 'csharp_index' in item:
            selected.add((item['path'],item['csharp_index']))
    modifier = 'async ' if async_method else ''
    result = return_type or ('Task' if async_method else 'void')
    wrapper = f'public static {modifier}{result} {name}({parameters})\n{{\n{body}\n}}\n'
    if not body and type_body:
        wrapper = ''
    if context == 'annotations':
        wrapper = body
    groups.append(dict(name=name, context=context, originals=originals,
                       raw=str(raw_path.relative_to(DEST)), raw_sha256=sha(raw.encode()),
                       wrapper=wrapper, type_body=type_body, prerequisites=prerequisites or [], transform=transform))

group('Discovery', [select(readme,1)], entry, prerequisites=['Real device discovery if executed; initial session ends inside its original braces.'], transform='Only namespace imports separated; declaration and complete session/resource scope retained.')
for name,index,parameters,conditions in [
    ('UpdateRetainCode',2,'IYubiKey device, byte[] accessCode',['Caller-owned exact six-byte current/new code; authorized existing updatable keyboard slot Two.']),
    ('ProvisionHmac',3,'IYubiKey device',['Authorized slot Two replacement, verifier enrollment and later physical touch if executed.']),
    ('CalculateHmac',4,'IYubiKey device',['Provisioned short-mode HMAC slot; no live competing connection; verifier consumes before finally.']),
    ('AesTicket',5,'IYubiKey device',['Authorized replacement; verifier enrollment supplied by application; no verifier stub.']),
    ('StaticPassword',6,'IYubiKey device',['Authorized replacement; matching en_US layout; immutable literal tradeoff remains.']),
    ('SwapDelete',7,'IYubiKey device',['Destructive authorized unprotected slots; both slots affected if executed.'])]:
    group(name,[select(readme,index)],select(readme,index)['body'],parameters,prerequisites=conditions)
group('PromptSession',[select(readme,8),select(readme,9)],select(readme,9)['body'],'IYubiKey device',
      type_body=select(readme,8)['body'], prerequisites=['Real TouchPrompt type and explicitly HID session; recipe intentionally contains no calculation.'],
      transform='Prompt declaration retained verbatim at namespace scope; complete session body retained in typed method. No interface stub or added calculation.')
group('BorrowedConnectionProvision',[select(guide,1)],select(guide,1)['body'],'IConnection connection, byte[] key',
      prerequisites=['Caller-owned open real SmartCard/OTP HID IConnection; caller-owned exact 20-byte key; caller disposes connection after session.'])
group('ExistingSessionChallenge',[select(guide,2)],select(guide,2)['body'],'YubiOtpSession session, byte[] challenge',
      prerequisites=['Borrowed live provisioned session; task-owned challenge; caller owns enclosing session/connection.'])
group('CachedState',[select(guide,3)],select(guide,3)['body'],'YubiOtpSession session',async_method=False,
      prerequisites=['Borrowed live session; configured-status-capable firmware. This is not a fresh read.'])
migration_imports, migration_body = split_imports(otp_migration[1]['body'])
imports += migration_imports
group('MigrationCurrentV2',[otp_migration[1]],migration_body,
      'IYubiKey device, byte[] hmacKey, byte[] challenge, CancellationToken cancellationToken',
      prerequisites=['Authorized replacement; caller-owned exact 20-byte key and 1-63-byte challenge; enrolled verifier; no other open connection.'],
      transform='Only namespace imports separated; all original using/await/finally/input/output clearing retained unchanged.')
_, wording = split_imports(select(interaction,2)['body'])
group('SharedOperationWording',[select(interaction,2)],wording+'\nreturn message;','UserPresenceContext context',async_method=False,return_type='string',
      prerequisites=['Generic context supplied by application; Core operation constants require no other applet source/reference.'],
      transform='Namespace import separated; original message switch retained; wrapper returns its result to avoid discarded-example output.')
group('SharedIntentPrompt',[select(interaction,4)],'',async_method=False,
      type_body='public sealed class IntentPrompt : IUserPresencePrompt\n{\n'+select(interaction,4)['body']+'\n}\n',
      prerequisites=['Real callback implementation; no WebAuthn/PIV client, mock or terminal-resolution stub.'],
      transform='Entire original callback method retained in a real IUserPresencePrompt type; no callable placeholder or extra operation invented.')

# Meaningful inline expressions are compiled with real typed receivers. Symbolic
# signatures, ellipses and internal implementation references are inventoried separately.
expressions = {
    'state.IsConfigured(Slot.One)': ('InlineConfigured','ConfigState state','bool','state.IsConfigured(Slot.One)'),
    'device.GetConfigStateAsync()': ('InlineReadState','IYubiKey device','Task<ConfigState>','device.GetConfigStateAsync()'),
    'UseShortChallenge(false)': ('InlineFixedChallenge','HmacSha1SlotConfiguration config','HmacSha1SlotConfiguration','config.UseShortChallenge(false)'),
    'new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp }': ('InlineHidOptions','','SessionCreationOptions','new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp }'),
}
inline_dispositions=[]
for entry in inventory['inline']:
    value=entry['body']
    if value in expressions:
        name,parameters,result,expression=expressions[value]
        existing=next((g for g in groups if g['name']==name),None)
        if existing is None:
            group(name,[entry],'return '+expression+';',parameters,async_method=False,return_type=result,
                  prerequisites=['Actual typed API inputs; compile only. Borrowed objects remain caller-owned.'],
                  transform='Original inline expression retained, with a typed receiver where the text is an unqualified instance-method designator.')
        else:
            existing['originals'].append(entry)
        disposition='compiled:'+name
    elif value.startswith('[Trait('):
        require(value=='[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]','Reconcile changed inline trait')
        group('InlineTrait',[entry],value+'\npublic sealed class AnnotationExample\n{\n}\n',async_method=False,context='annotations',
              prerequisites=['Real xUnit-v2 Trait and real Tests.Shared category constants; compile-only annotation, not a runnable placeholder test.'],
              transform='Original attribute placed on a concrete declaration, with real canonical test references.')
        disposition='compiled:InlineTrait'
    elif '(' in value or '=' in value or ';' in value:
        if entry['scope']=='otp-gaps':
            disposition='excluded: historical/missing-feature API catalog designator, not a current executable recipe; restored static constructor covered by StaticPassword'
        elif entry['scope']=='shared-record-metadata':
            disposition='excluded: shared architecture/mapping designator outside an OTP executable task; no other-module qualification'
        elif '...' in value or value=='StaticPasswordSlotConfiguration(string, KeyboardLayout)':
            disposition='excluded: symbolic signature/ellipsis, not a complete example; concrete API calls covered in complete task groups'
        elif value in ('GetConfigState()','SlotConfiguration.GetConfig()','ReadStatusAsync()'):
            disposition='excluded: API/implementation method designator, not a standalone call; GetConfigState covered by CachedState, no internal backend host invented'
        else:
            raise SystemExit(f'Reconcile new meaningful inline code: {entry["path"]}:{entry["line"]}: {value}')
    else:
        disposition='excluded: identifier, type/signature label, data value, option, path or prose token, not an executable fragment'
    inline_dispositions.append(dict(entry=entry,disposition=disposition))
xml_dispositions=[]
for entry in inventory['xml']:
    if entry['tag']=='c' and entry['body']=='UseShortChallenge(false)':
        next(g for g in groups if g['name']=='InlineFixedChallenge')['originals'].append(entry)
        disposition='compiled:InlineFixedChallenge (typed receiver added)'
    elif entry['tag']=='code':
        disposition='excluded: complete non-executable 52-byte wire-layout table, not C#'
    else:
        disposition='excluded: inline XML identifier/range/literal or explanatory markup, not an executable task'
    xml_dispositions.append(dict(entry=entry,disposition=disposition))

# Inline occurrences may share one typed binding group. Preserve every original
# occurrence in that group's raw bundle, not just the first equivalent expression.
for item in groups:
    raw='\n'.join(original['body'] for original in item['originals'])
    (DEST/item['raw']).write_text(raw)
    item['raw_sha256']=sha(raw.encode())

exclusions=[]
for item in blocks:
    if (item['path'],item['csharp_index']) in selected:
        continue
    if item is otp_migration[0]:
        reason='Historical v1 before block; not current v2 compilation or legacy-source qualification.'
    elif item['path']==interaction and item['csharp_index']==1:
        reason='Unchanged explicit PIV session/CLI prompt example, outside OTP assignment; original body inventoried, no PIV/CLI source read.'
    elif item['path']==interaction and item['csharp_index']==3:
        reason='Unchanged implicit client.GetAssertionAsync WebAuthn example with actual surrounding typed prerequisites, outside OTP assignment; no client stub/other-module audit.'
    elif item['scope'].startswith('outside-otp-'):
        reason='Shared migration example outside the assigned YubiOTP section; inventory identity only, no other-module qualification.'
    else:
        raise SystemExit(f'Unclassified C# fence: {item["path"]}:{item["line"]}')
    exclusions.append(dict(block=item,reason=reason))

template=ROOT/'docs/research/documentation-audit/modules/security-domain/pass-1/security-domain-001/validation/examples/v1'
provenance=[]
for context in ('consumer','annotations'):
    source_context='consumer' if context=='consumer' else 'hardware'
    folder=DEST/context
    (folder/'src').mkdir(parents=True)
    for name in ('Directory.Build.props','Directory.Build.targets'):
        source=template/source_context/name
        substitutions={f'/tmp/opencode/security-domain-001-examples-v1-{source_context}':f'/tmp/opencode/yubiotp-001-examples-{revision}-{context}',
                       'SecurityDomainDocumentation':'AuditOtp001Documentation'}
        text=source.read_text()
        for before,after in substitutions.items():
            text=text.replace(before,after)
        if name=='Directory.Build.targets':
            tree=ET.fromstring(text)
            policy=tree.find("./Target[@AfterTargets='CoreCompile']/ItemGroup")
            for key in ('MSBuildProjectFullPath','MSBuildProjectExtensionsPath','ProjectAssetsFile','IsTestProject','OutputType','Configuration'):
                ET.SubElement(policy,'DocumentationPolicy',Include=f'{key}=$({key})')
            ET.indent(tree,space='  ')
            text=ET.tostring(tree,encoding='unicode')+'\n'
        target=folder/name
        target.write_text(text)
        provenance.append(dict(source=file_record(source),target=file_record(target),substitutions=substitutions))
    project=ET.Element('Project',Sdk='Microsoft.NET.Sdk')
    properties=ET.SubElement(project,'PropertyGroup')
    for name,value in {'TargetFramework':'net10.0','OutputType':'Library','IsPackable':'false',
                       'ProvideCommandLineArgs':'true','EnableDefaultCompileItems':'false'}.items():
        ET.SubElement(properties,name).text=value
    items=ET.SubElement(project,'ItemGroup')
    original='src/YubiOtp/src/Yubico.YubiKit.YubiOtp.csproj'
    ET.SubElement(items,'ProjectReference',Include='$(RepositoryRoot)/'+original)
    if context=='annotations':
        original='src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/Yubico.YubiKit.YubiOtp.IntegrationTests.csproj'
        ET.SubElement(items,'ProjectReference',Include='$(RepositoryRoot)/src/Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj')
        for item in ET.parse(ROOT/original).findall('.//PackageReference'):
            items.append(ET.fromstring(ET.tostring(item)))
    ET.SubElement(items,'Compile',Include='Examples.cs')
    ET.indent(project,space='  ')
    project_path=folder/'src'/f'Yubico.YubiKit.AuditOtp001.Examples.{revision}.{context}.csproj'
    project_path.write_text(ET.tostring(project,encoding='unicode')+'\n')
    original_record=file_record(ROOT/original)
    provenance.append(dict(source=original_record,target=file_record(project_path),
                           transformation='Real YubiOtp reference; canonical integration PackageReference elements copied verbatim only for annotations; no diagnostics disabled.'))
    if context=='consumer':
        using_lines=list(dict.fromkeys(imports.splitlines()))
        usings='\n'.join(using_lines)+'\n'
    else:
        usings='using Xunit;\nusing Yubico.YubiKit.Tests.Shared.Infrastructure;\n'
    code=usings+'\nnamespace AuditOtp001.Examples;\n\n'
    current=[g for g in groups if g['context']==context]
    if context=='consumer':
        code+='public static class ConsumerExamples\n{\n'+''.join(g['wrapper'] for g in current)+'}\n'
        code+='\n'.join(g['type_body'] for g in current if g['type_body'])
    else:
        code+='\n'.join(g['wrapper'] for g in current)
    (folder/'src/Examples.cs').write_text(code)
    print(f'Prepared {context}: {len(current)} complete groups, {project_path.name}; no build or execution.')

# The shipping sample is never replaced by a reduced host. A process-scoped
# MSBuild extension hook adds observation only for MSBuildProjectName=OtpTool.
sample_folder=DEST/'otptool'
sample_folder.mkdir()
early=ET.Element('Project')
early_properties=ET.SubElement(early,'PropertyGroup',Condition="'$(MSBuildProjectName)' == 'OtpTool'")
for name,value in {
    'DocumentationWorkspace':f'/tmp/opencode/yubiotp-001-examples-{revision}-otptool',
    'BaseIntermediateOutputPath':'$(DocumentationWorkspace)/obj/',
    'MSBuildProjectExtensionsPath':'$(DocumentationWorkspace)/obj/',
    'BaseOutputPath':'$(DocumentationWorkspace)/bin/'}.items():
    ET.SubElement(early_properties,name).text=value
ET.indent(early,space='  ')
early_path=sample_folder/'OtpTool.Output.props'
early_path.write_text(ET.tostring(early,encoding='unicode')+'\n')
provenance.append(dict(source=file_record(template/'consumer/Directory.Build.props'),target=file_record(early_path),
                       transformation='Reuse proven isolated-output properties in an exact-OtpTool early extension hook; leave shipping project canonical props/targets imports intact. No compiler/reference/analyzer policy changes.'))
source=template/'consumer/Directory.Build.targets'
hook=ET.fromstring(source.read_text())
for item in list(hook.findall('Import')):
    hook.remove(item)
condition="'$(MSBuildProjectName)' == 'OtpTool'"
properties=ET.Element('PropertyGroup',Condition=condition)
ET.SubElement(properties,'DocumentationWorkspace').text=f'/tmp/opencode/yubiotp-001-examples-{revision}-otptool'
ET.SubElement(properties,'ProvideCommandLineArgs').text='true'
hook.insert(0,properties)
for target in hook.findall('Target'):
    target.set('Name',target.get('Name').replace('SecurityDomainDocumentation','AuditOtp001SampleDocumentation'))
    target.set('Condition',condition)
policy=hook.find("./Target[@AfterTargets='CoreCompile']/ItemGroup")
for key in ('MSBuildProjectFullPath','MSBuildProjectExtensionsPath','ProjectAssetsFile','IsTestProject','OutputType','Configuration'):
    ET.SubElement(policy,'DocumentationPolicy',Include=f'{key}=$({key})')
ET.indent(hook,space='  ')
hook_path=sample_folder/'OtpTool.Capture.targets'
hook_path.write_text(ET.tostring(hook,encoding='unicode')+'\n')
provenance.append(dict(source=file_record(source),target=file_record(hook_path),
                       transformation='Observation-only external hook: remove redundant canonical import, condition all targets/properties on exact OtpTool project, unique target names/workspace, enable command-line capture and record actual evaluated project/assets paths. Shipping project and diagnostics unchanged.'))

dependencies=[ROOT/name for name in ('Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json','.editorconfig')]
library=ROOT/'src/YubiOtp/src'
library_sources=[p for p in library.rglob('*.cs') if not any(x in ('bin','obj','generated') for x in p.relative_to(library).parts)]
require(len(library_sources)==23,'Reconcile actual YubiOtp library source dependency inventory')
dependencies += list(library.glob('*.csproj'))+library_sources+list(library.glob('PublicAPI.*.txt'))
sample=ROOT/'src/YubiOtp/examples/OtpTool'
sample_sources=sorted(p for p in sample.rglob('*.cs') if not any(x in ('bin','obj') for x in p.relative_to(sample).parts))
require(len(sample_sources)==14,'Reconcile actual OtpTool compiler source inventory')
dependencies += sample_sources+[sample/'OtpTool.csproj']
dependencies += [ROOT/row['path'] for row in inventory['xml_documents']]
dependencies += [ROOT/'src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/Yubico.YubiKit.YubiOtp.IntegrationTests.csproj']
driver=HERE/'dispatch/src/Yubico.YubiKit.AuditOtp001.Examples.Shipping.OtpTool.csproj'
ET.parse(driver)
discovered=[p for p in ROOT.rglob('*.csproj') if '/src/' in str(p) and 'Yubico.YubiKit.' in str(p) and '/.claude/' not in str(p)]
matches=[p for p in discovered if 'otptool' in p.stem.lower()]
require(matches==[driver],'The plain OtpTool selector must discover exactly the one real-project traversal driver; reconcile any collision')
dependencies += [driver]+[ROOT/f'scripts/toolchain/{name}.cs' for name in ('Context','Discovery','Conventions','BuildTargets')]
pdfs=[file_record(path) for path in sorted((RUN/'evidence').glob('*.pdf'))]
require(len(pdfs)==3,'Reconcile retained original PDF source exceptions')
manifest=dict(revision=revision,baseline='95e5ccaeda33954c21f2a38058d4f66b5f208787',inventory=inventory,
              groups=groups,exclusions=exclusions,inline_dispositions=inline_dispositions,xml_dispositions=xml_dispositions,
              template_provenance=provenance,dependencies=[file_record(p) for p in sorted(set(dependencies))],
              sample=dict(project='src/YubiOtp/examples/OtpTool/OtpTool.csproj',source_paths=[str(p.relative_to(ROOT)) for p in sample_sources],
                          traversal_driver=str(driver.relative_to(ROOT)),
                          discovery='Shipping OtpTool.csproj lacks the Yubico.YubiKit. path prefix required by current build discovery. Exactly one prefix-qualified traversal driver makes selector OtpTool delegate Restore/Clean/Build/Rebuild to the untouched shipping project; this is not a reduced C# host.',
                          policy='Actual shipping project built separately, never executed; capture actual compiler context through the scoped external hook.'),
              pdf_source_exceptions=pdfs,
              limits='Preparation only; no stubs/dynamic/assertion removal/diagnostic disabling; no Core/other-module implementation read; no old result bodies copied.')
manifest['tool_sources']=[file_record(p) for p in sorted(HERE.glob('*.py'))]
manifest['approach_provenance']=[file_record(template.parent/name) for name in ('prepare.py','verify.py','capture.py')]
manifest['editor_corrections_record']=file_record(RUN/'edit-corrections.md')
tool_archive=DEST/'tool-source'
tool_archive.mkdir()
for path in sorted(HERE.glob('*.py')):
    (tool_archive/(path.name+'.snapshot')).write_bytes(path.read_bytes())
manifest['prepared_files']=[file_record(p) for p in sorted(DEST.rglob('*')) if p.is_file()]
(DEST/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(f'Inventoried {len(inventory["documents"])} documents, {len(blocks)} C# fences across shared/module scope, {len(inventory["edited_xml_paths"])} affected and {len(inventory["xml_documents"])} total module C# XML-documentation files; {len(groups)} compile groups; original PDFs listed for coordinator exception handling.')
