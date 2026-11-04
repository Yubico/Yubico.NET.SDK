"""Prepare one minimal OtpTool late-capture-only overlay, preserving accepted v2 evidence."""
from pathlib import Path
import ast
import json
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
EXAMPLES = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(EXAMPLES))
from inventory import ROOT, sha, file_record

OVERLAY = EXAMPLES / 'overlays/otptool-native-1'
BASE = EXAMPLES / 'v2'
WORK = '/tmp/opencode/yubiotp-001-examples-v2-otptool-native-1'
if (OVERLAY / 'manifest.json').exists():
    raise SystemExit('Overlay already prepared; refusing to replace original configuration/provenance')

def write_once(path, raw):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('xb') as output:
        output.write(raw)

def substitute(text, before, after):
    if text.count(before) != 1:
        raise ValueError(f'Expected exactly one proven source fragment: {before[:100]}')
    return text.replace(before, after)

sources = [EXAMPLES / name for name in ('capture.py', 'verify.py', 'inventory.py')]
sources += [BASE / 'otptool' / name for name in ('OtpTool.Output.props', 'OtpTool.Capture.targets')]
before = []
for path in sources:
    relative = 'before/' + path.name + '.snapshot'
    write_once(OVERLAY / relative, path.read_bytes())
    before.append(dict(source=file_record(path), snapshot=relative))
old_plan = EXAMPLES.parent.parent / 'snippet-preparation.md'
write_once(OVERLAY / 'before/snippet-preparation.md.snapshot', old_plan.read_bytes())
before.append(dict(source=file_record(old_plan), snapshot='before/snippet-preparation.md.snapshot',
                   role='Historical preparation instructions before the authoritative overlay correction; not a current-input requirement.'))

target = ET.parse(BASE / 'otptool/OtpTool.Capture.targets').getroot()
workspace = target.find('./PropertyGroup/DocumentationWorkspace')
if workspace is None or workspace.text != '/tmp/opencode/yubiotp-001-examples-v2-otptool':
    raise ValueError('Unexpected original late capture hook')
workspace.text = WORK
policy = target.find("./Target[@AfterTargets='CoreCompile']/ItemGroup")
for name in ('BaseIntermediateOutputPath', 'BaseOutputPath', 'IntermediateOutputPath', 'OutputPath',
             '_InitialMSBuildProjectExtensionsPath', 'CustomBeforeMicrosoftCommonProps', 'CustomAfterMicrosoftCommonTargets'):
    ET.SubElement(policy, 'DocumentationPolicy', Include=f'{name}=$({name})')
ET.indent(target, space='  ')
write_once(OVERLAY / 'OtpTool.Capture.targets', (ET.tostring(target, encoding='unicode') + '\n').encode())
property_names = {node.tag for group in target.findall('PropertyGroup') for node in group}
if property_names != {'DocumentationWorkspace', 'ProvideCommandLineArgs'}:
    raise ValueError('Overlay must not set any sample output/intermediate/compiler-policy property')

injected_import = "OVERLAY = Path(__file__).resolve().parent\nsys.path.insert(0, str(OVERLAY.parent.parent))\n"
capture = (EXAMPLES / 'capture.py').read_text()
capture = substitute(capture, 'from inventory import HERE, ROOT, sha, file_record', injected_import + 'from inventory import HERE, ROOT, sha, file_record\nfrom support import validate_overlay\nvalidate_overlay()')
capture = substitute(capture, "sys.argv[2] not in ('consumer','annotations','otptool')", "sys.argv[2] != 'otptool'")
capture = substitute(capture, '<consumer|annotations|otptool>', 'otptool')
capture = substitute(capture, "work=Path(f'/tmp/opencode/yubiotp-001-examples-{revision}-{context}')", f"work=Path('{WORK}')")
capture = substitute(capture, "dest=folder/'evidence'/attempt", "dest=OVERLAY/'evidence'/attempt")
capture = substitute(capture, "revision,context,attempt=sys.argv[1:]", "revision,context,attempt=sys.argv[1:]\nif revision != 'v2':\n    raise SystemExit('This overlay reuses the accepted v2 extraction only')")
capture = substitute(capture, "require(actual_project.resolve()==expected_project.resolve(),'Capture belongs to another project')", """require(actual_project.resolve()==expected_project.resolve(),'Capture belongs to another project')
    def native_path(value):
        return (actual_project.parent/value.replace('\\\\','/')).resolve()
    native_obj=(expected_project.parent/'obj').resolve()
    native_bin=(expected_project.parent/'bin').resolve()
    require(native_path(policy['MSBuildProjectExtensionsPath'])==native_obj,'Sample extensions must use its ordinary obj directory')
    require(native_path(policy['ProjectAssetsFile'])==native_obj/'project.assets.json','Sample assets must come from its ordinary obj directory')
    require(native_path(policy['BaseIntermediateOutputPath'])==native_obj,'Sample base intermediate path was overridden')
    require(native_path(policy['BaseOutputPath'])==native_bin,'Sample base output path was overridden')
    require(native_path(policy['IntermediateOutputPath']).is_relative_to(native_obj),'Sample intermediate output escaped its ordinary obj tree')
    require(native_path(policy['OutputPath']).is_relative_to(native_bin),'Sample output escaped its ordinary bin tree')
    require('OtpTool.Output.props' not in policy['CustomBeforeMicrosoftCommonProps'],'The failed early output hook is still active')
    require(native_path(policy['_InitialMSBuildProjectExtensionsPath'])==native_obj,'Sample extensions differ from their initial imported value')""")
capture = substitute(capture, "retained.append(retain(folder/'OtpTool.Capture.targets','capture-hook.targets.snapshot'))\n        retained.append(retain(folder/'OtpTool.Output.props','capture-output.props.snapshot'))", "retained.append(retain(OVERLAY/'OtpTool.Capture.targets','capture-hook.targets.snapshot'))\n        retained.append(retain(OVERLAY/'manifest.json','overlay-provenance.json.snapshot'))")
capture = substitute(capture, 'result=dict(revision=revision,context=context,attempt=attempt,policy=policy,records=records,retained=retained,', "result=dict(revision=revision,context=context,attempt=attempt,overlay_id='otptool-native-1',policy=policy,records=records,retained=retained,")
ast.parse(capture)
write_once(OVERLAY / 'capture.py', capture.encode())

verify = (EXAMPLES / 'verify.py').read_text()
verify = substitute(verify, 'from inventory import HERE, ROOT, scan, identities, sha', injected_import + 'from inventory import HERE, ROOT, scan, identities, sha\nfrom support import validate_overlay\nOVERLAY_MANIFEST = validate_overlay()')
verify = substitute(verify, "folder=HERE/args.revision", "if args.revision != 'v2':\n    raise SystemExit('This overlay reuses the accepted v2 extraction only')\nfolder=HERE/args.revision")
verify = substitute(verify, "evidence=folder/context/'evidence'/attempt", "evidence=OVERLAY/'evidence'/attempt if context=='otptool' else folder/context/'evidence'/attempt")
verify = substitute(verify, "require(data['revision']==args.revision and data['context']==context and data['attempt']==attempt,'Compiler capture identity mismatch')", """require(data['revision']==args.revision and data['context']==context and data['attempt']==attempt,'Compiler capture identity mismatch')
        if context=='otptool':
            require(data.get('overlay_id')=='otptool-native-1','Require the late-only native-output OtpTool capture, not the failed v2 hook')
            require(data['policy']['MSBuildProjectExtensionsPath'].rstrip('/\\\\')==str(ROOT/'src/YubiOtp/examples/OtpTool/obj'),'Require actual shipping-native obj provenance')
        else:
            require(attempt==OVERLAY_MANIFEST['reused_captures'][context]['attempt'],'Reuse the accepted original first capture; no replacement consumer/annotation compilation is required')""")
verify = substitute(verify, "print('NOT RUN: builds, C# binding/analyzers, examples/sample execution, compiler captures, hardware or formatter.')", "print('NOT RUN by overlay preparation: new OtpTool build/capture or any tests/hardware/formatter. Original consumer/annotations first captures are verified and reused; late-only OtpTool result remains pending.')")
ast.parse(verify)
write_once(OVERLAY / 'verify.py', verify.encode())
ast.parse((OVERLAY / 'support.py').read_text())

after = []
for name in ('capture.py', 'verify.py', 'support.py', 'OtpTool.Capture.targets'):
    path = OVERLAY / name
    relative = 'after/' + name + '.snapshot'
    write_once(OVERLAY / relative, path.read_bytes())
    after.append(dict(source=file_record(path), snapshot=relative))
write_once(OVERLAY / 'after/prepare-otptool-native.py.snapshot', Path(__file__).read_bytes())

run = EXAMPLES.parent.parent
failure_paths = [run / 'validation/examples-v2-otptool-build.log',
                 run / 'validation/examples-v2-otptool-build-20261104-115031-825-build/01-clean-AuditOtp001.Examples.Shipping.OtpTool.log']
sdk = Path('/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.403')
sdk_evidence = []
sdk_paths = [(sdk / 'Sdks/Microsoft.NET.Sdk/Sdk/Sdk.props',44,50),
             (sdk / 'Current/Microsoft.Common.props',45,92),
             (sdk / 'Microsoft.Common.CurrentVersion.targets',866,875)]
for path,first,last in sdk_paths:
    raw = path.read_bytes()
    sdk_evidence.append(dict(path=str(path),bytes=len(raw),sha256=sha(raw),
                             excerpt_lines=[first,last],excerpt='\n'.join(raw.decode('utf-8-sig').splitlines()[first-1:last])))
reused = {}
for context, expected in [('consumer',(4,179,10)), ('annotations',(5,205,12))]:
    folder = BASE / context / 'evidence/first'
    data = json.loads((folder / 'manifest.json').read_text())
    counts = data['counts']
    if (counts['sources'],counts['references'],counts['analyzers']) != expected:
        raise ValueError(f'Unexpected accepted {context} first capture counts')
    reused[context] = dict(directory=str(folder.relative_to(ROOT)),attempt='first',
                           counts=counts,manifest=file_record(folder/'manifest.json'))
preserved = [file_record(path) for version in ('v1','v2') for path in sorted((EXAMPLES/version).rglob('*')) if path.is_file()]
inputs = [file_record(path) for path in sources + [BASE/'manifest.json',Path(__file__)] + failure_paths]
inputs += [binding['manifest'] for binding in reused.values()]
inputs += [{k:row[k] for k in ('path','bytes','sha256')} for row in sdk_evidence]
files = [file_record(path) for path in sorted(OVERLAY.rglob('*')) if path.is_file()]
manifest = dict(id='otptool-native-1',base_revision='v2',status='prepared-not-built-not-captured',
                workspace=WORK,files=files,inputs=inputs,preserved_v1_v2=preserved,
                before=before,after=after,reused_captures=reused,sdk_import_evidence=sdk_evidence,
                root_cause='Installed 10.0.403 Microsoft.Common.props initializes/captures MSBuildProjectExtensionsPath at lines 54-71 and imports CustomBeforeMicrosoftCommonProps only at line 92; the original hook then changed it, and CurrentVersion.targets lines 871-875 emitted MSB3540 during Clean before compilation.',
                correction='Remove the early output hook from the command entirely. Use actual shipping-native bin/obj and only the late observation hook with a distinct temporary capture workspace. No SDK, toolchain, driver or v1/v2 source/capture edits.',
                capture_adaptation='Keep original strict Csc membership/policy/assets checks; route only OtpTool capture to this overlay; retain only the active late hook; assert actual ordinary shipping output paths and record overlay identity.',
                verifier_adaptation='Keep original v2 body/source/config/dependency and compiled-evidence checks; pin/reuse consumer/first and annotations/first; route only OtpTool to overlay evidence; require new overlay/native-output identities.',
                documentation=['https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=visualstudio',
                               'https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-net-builds?view=visualstudio',
                               'https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview'],
                limits='Static preparation only; full-solution sample build success does not replace requested actual compiler provenance. Original failed clean/logs and first compiler identities remain retained; no binary/cache copies.')
write_once(OVERLAY / 'manifest.json', (json.dumps(manifest,indent=2)+'\n').encode())
print(f'Prepared late-only OtpTool native-output overlay; {len(preserved)} v1/v2 file identities pinned, accepted consumer/annotations first captures reused without duplication.')
print('No build, compilation, test, hardware, formatter, SDK/toolchain edit or prior evidence overwrite performed.')
