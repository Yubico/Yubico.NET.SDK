"""Retain actual Csc membership/policy/assets after coordinator build; no code execution.

Adapted from frozen Security Domain examples/capture.py. Unique attempt directories
preserve failures. No binary/reference cache is copied; source/config text is retained.
"""
from pathlib import Path
import json
import re
import sys

sys.dont_write_bytecode = True
OVERLAY = Path(__file__).resolve().parent
sys.path.insert(0, str(OVERLAY.parent.parent))
from inventory import HERE, ROOT, sha, file_record
from support import validate_overlay
validate_overlay()

if len(sys.argv)!=4 or not re.fullmatch(r'v[1-9][0-9]*',sys.argv[1]) or sys.argv[2] != 'otptool' or not re.fullmatch(r'[a-z0-9-]+',sys.argv[3]):
    raise SystemExit('Usage: capture.py <vN> otptool <new-attempt-name>')
revision,context,attempt=sys.argv[1:]
if revision != 'v2':
    raise SystemExit('This overlay reuses the accepted v2 extraction only')
folder=HERE/revision/context
work=Path('/tmp/opencode/yubiotp-001-examples-v2-otptool-native-1')
dest=OVERLAY/'evidence'/attempt
dest.mkdir(parents=True,exist_ok=False)

def require(condition,message):
    if not condition:
        raise ValueError(message)

def lines(path):
    return [line.strip() for line in path.read_text().splitlines() if line.strip()]

def retain(source,relative):
    raw=source.read_bytes()
    raw.decode('utf-8')
    require(b'\0' not in raw,f'Non-text capture: {source}')
    target=dest/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    with target.open('xb') as output:
        output.write(raw)
    return dict(path=str(relative),bytes=len(raw),sha256=sha(raw))

try:
    names=('compile-inputs.txt','resolved-references.txt','analyzers.txt','analyzer-configs.txt','policy-final.txt','csc-command-line.txt')
    retained=[retain(work/name,name) for name in names]
    args=lines(dest/'csc-command-line.txt')
    policy=dict(row.split('=',1) for row in lines(dest/'policy-final.txt'))
    require(policy['TreatWarningsAsErrors']=='true','Warnings-as-errors not effective')
    require(policy['RunAnalyzersDuringBuild']=='true' and policy['SkipAnalyzers'].lower()!='true','Analyzers skipped')
    require(policy['SkipCompilerExecution'].lower()!='true','Compiler execution skipped')
    require(policy['AnalysisMode']==('Recommended' if context=='annotations' else 'AllEnabledByDefault'),'Unexpected canonical analysis mode')
    require('/warnaserror+' in args and '/warn:9999' in args,'Compiler warning policy mismatch')
    require(not any('/skipanalyzers' in argument.lower() for argument in args),'Csc skipped analyzers')
    actual_project=Path(policy['MSBuildProjectFullPath'])
    if context=='otptool':
        expected_project=ROOT/'src/YubiOtp/examples/OtpTool/OtpTool.csproj'
    else:
        expected_project=folder/'src'/f'Yubico.YubiKit.AuditOtp001.Examples.{revision}.{context}.csproj'
    require(actual_project.resolve()==expected_project.resolve(),'Capture belongs to another project')
    def native_path(value):
        return (actual_project.parent/value.replace('\\','/')).resolve()
    native_obj=(expected_project.parent/'obj').resolve()
    native_bin=(expected_project.parent/'bin').resolve()
    require(native_path(policy['MSBuildProjectExtensionsPath'])==native_obj,'Sample extensions must use its ordinary obj directory')
    require(native_path(policy['ProjectAssetsFile'])==native_obj/'project.assets.json','Sample assets must come from its ordinary obj directory')
    require(native_path(policy['BaseIntermediateOutputPath'])==native_obj,'Sample base intermediate path was overridden')
    require(native_path(policy['BaseOutputPath'])==native_bin,'Sample base output path was overridden')
    require(native_path(policy['IntermediateOutputPath']).is_relative_to(native_obj),'Sample intermediate output escaped its ordinary obj tree')
    require(native_path(policy['OutputPath']).is_relative_to(native_bin),'Sample output escaped its ordinary bin tree')
    require('OtpTool.Output.props' not in policy['CustomBeforeMicrosoftCommonProps'],'The failed early output hook is still active')
    require(native_path(policy['_InitialMSBuildProjectExtensionsPath'])==native_obj,'Sample extensions differ from their initial imported value')
    def resolve(text):
        return str((actual_project.parent/text.strip('"')).resolve())
    def arguments(prefix):
        return {resolve(argument[len(prefix):]) for argument in args if argument.startswith(prefix)}
    source_args={resolve(argument) for argument in args if argument.endswith('.cs') and not re.match(r'^/[A-Za-z]+:',argument)}
    sources={str(Path(path).resolve()) for path in lines(dest/'compile-inputs.txt')}
    references={str(Path(path.split('|',1)[0]).resolve()) for path in lines(dest/'resolved-references.txt')}
    analyzers={str(Path(path).resolve()) for path in lines(dest/'analyzers.txt')}
    require(source_args==sources,'Actual Csc source arguments differ from Compile items')
    require(arguments('/reference:')==references,'Actual Csc reference arguments differ from resolved references')
    require(arguments('/analyzer:')==analyzers,'Actual Csc analyzer arguments differ from Analyzer items')
    require(any('Microsoft.CodeAnalysis.NetAnalyzers.dll' in name for name in analyzers),'Canonical .NET analyzers missing')
    if context=='annotations':
        require(any('xunit.analyzers' in name.lower() for name in analyzers),'Real xUnit analyzers missing')
    if context=='otptool':
        manifest=json.loads((HERE/revision/'manifest.json').read_text())
        require({str((ROOT/name).resolve()) for name in manifest['sample']['source_paths']} <= sources,'Actual sample compilation omitted one of its 14 original source files')
    else:
        require(str((folder/'src/Examples.cs').resolve()) in sources,'Complete generated example source missing from Csc')
    configs=arguments('/analyzerconfig:')
    records=[]
    for kind,paths in [('source',sources),('reference',references),('analyzer',analyzers),('analyzer-config',configs)]:
        for name in sorted(paths):
            path=Path(name)
            data=path.read_bytes()
            row=dict(kind=kind,path=name,bytes=len(data),sha256=sha(data))
            if kind in ('source','analyzer-config'):
                # Only this host or the actual OTP sample appears in the source
                # list; references to other modules are hashed binary metadata.
                relative=f'{kind}/{sha(data)[:16]}-{path.name}.snapshot'
                retained.append(retain(path,relative))
                row['retained']=relative
            else:
                row['retention']='Binary dependency not copied; exact path/size/hash and actual assets retained.'
            records.append(row)
    assets=Path(policy['ProjectAssetsFile'])
    retained.append(retain(assets,'project.assets.json'))
    for name in ('Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json','.editorconfig'):
        retained.append(retain(ROOT/name,name+'.snapshot'))
    retained.append(retain(actual_project,'compiled-project.csproj.snapshot'))
    original='src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/Yubico.YubiKit.YubiOtp.IntegrationTests.csproj' if context=='annotations' else 'src/YubiOtp/src/Yubico.YubiKit.YubiOtp.csproj'
    retained.append(retain(ROOT/original,'source-context.csproj.snapshot'))
    if context=='otptool':
        retained.append(retain(OVERLAY/'OtpTool.Capture.targets','capture-hook.targets.snapshot'))
        retained.append(retain(OVERLAY/'manifest.json','overlay-provenance.json.snapshot'))
    result=dict(revision=revision,context=context,attempt=attempt,overlay_id='otptool-native-1',policy=policy,records=records,retained=retained,
                assets_identity=file_record(assets) if assets.is_relative_to(ROOT) else dict(path=str(assets),sha256=sha(assets.read_bytes())),
                counts=dict(sources=len(sources),references=len(references),analyzers=len(analyzers),configs=len(configs)),
                limits='Actual Csc source/reference/analyzer membership reconciled; text sources/config/assets retained; binary/cache identities only. No example/sample/test/hardware execution. Compiler success/warnings/raw diagnostics remain in the separately retained toolchain build log.')
    with (dest/'manifest.json').open('x') as output:
        json.dump(result,output,indent=2)
        output.write('\n')
    print(f'PASS: {context}/{attempt}: {len(sources)} actual compiler sources, {len(references)} references, {len(analyzers)} analyzers; no binaries copied.')
except Exception as error:
    with (dest/'capture-failure.json').open('x') as output:
        json.dump(dict(type=type(error).__name__,error=str(error),scope='Capture/provenance failure, not a compiler or example result; earlier text captures preserved.'),output,indent=2)
        output.write('\n')
    raise
