"""Retain actual SDK compiler inputs/provenance after a host build; no code is executed."""
from pathlib import Path
import hashlib
import json
import re
import shutil
import sys

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p/'toolchain.cs').exists())
revision, context = sys.argv[1:]
assert re.fullmatch(r'v[1-9][0-9]*', revision) and context in {'consumer','hardware'}
folder=HERE/revision/context
work=Path(f'/tmp/opencode/security-domain-001-examples-{revision}-{context}')
dest=folder/'evidence'
if dest.exists():
    raise SystemExit(f'Refusing to replace captured evidence: {dest}')
dest.mkdir()

def sha(data): return hashlib.sha256(data).hexdigest()
def lines(path): return [x.strip() for x in path.read_text().splitlines() if x.strip()]
def resolve(text): return str((folder/'src'/text.strip('"')).resolve())

captures=['compile-inputs.txt','resolved-references.txt','analyzers.txt','analyzer-configs.txt','policy-final.txt','csc-command-line.txt']
for name in captures:
    shutil.copyfile(work/name,dest/name)
args=lines(dest/'csc-command-line.txt')
policy=dict(row.split('=',1) for row in lines(dest/'policy-final.txt'))
assert policy['TreatWarningsAsErrors']=='true'
assert policy['RunAnalyzersDuringBuild']=='true' and policy['SkipAnalyzers'].lower()!='true'
assert policy['SkipCompilerExecution'].lower()!='true'
assert policy['AnalysisMode']==('Recommended' if context=='hardware' else 'AllEnabledByDefault')
assert '/warnaserror+' in args and '/warn:9999' in args
assert not any('/skipanalyzers' in a.lower() for a in args)

source_args={resolve(a) for a in args if a.endswith('.cs') and not re.match(r'^/[A-Za-z]+:',a)}
sources={str(Path(p).resolve()) for p in lines(dest/'compile-inputs.txt')}
assert source_args==sources,(source_args-sources,sources-source_args)
def arguments(prefix): return {resolve(a[len(prefix):]) for a in args if a.startswith(prefix)}
refs={str(Path(p.split('|',1)[0]).resolve()) for p in lines(dest/'resolved-references.txt')}
analyzers={str(Path(p).resolve()) for p in lines(dest/'analyzers.txt')}
assert arguments('/reference:')==refs
assert arguments('/analyzer:')==analyzers
assert any('Microsoft.CodeAnalysis.NetAnalyzers.dll' in p for p in analyzers)
if context=='hardware': assert any('xunit.analyzers' in p.lower() for p in analyzers)
configs=arguments('/analyzerconfig:')
records=[]
for kind, paths in [('source',sources),('reference',refs),('analyzer',analyzers),('analyzer-config',configs)]:
    for name in sorted(paths):
        p=Path(name); data=p.read_bytes()
        row=dict(kind=kind,path=name,bytes=len(data),sha256=sha(data))
        if kind in {'source','analyzer-config'}:
            retained=f'{kind}/{sha(data)[:12]}-{p.name}.snapshot'
            target=dest/retained; target.parent.mkdir(exist_ok=True)
            target.write_bytes(data); row['retained']=retained
        else:
            row['retention']='Binary dependency not copied; pinned identity/path/hash and project.assets.json retained'
        records.append(row)
assets=work/'obj/project.assets.json'
shutil.copyfile(assets,dest/'project.assets.json')
for name in ['Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json','.editorconfig']:
    shutil.copyfile(ROOT/name,dest/(name+'.snapshot'))
original='src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Yubico.YubiKit.SecurityDomain.IntegrationTests.csproj' if context=='hardware' else 'src/SecurityDomain/src/Yubico.YubiKit.SecurityDomain.csproj'
shutil.copyfile(ROOT/original,dest/'source-context.csproj.snapshot')
retained=[dict(path=str(p.relative_to(dest)),bytes=p.stat().st_size,sha256=sha(p.read_bytes())) for p in sorted(dest.rglob('*')) if p.is_file()]
(dest/'manifest.json').write_text(json.dumps(dict(context=context,revision=revision,policy=policy,records=records,retained=retained,
    counts=dict(sources=len(sources),references=len(refs),analyzers=len(analyzers),configs=len(configs)),
    limits='Actual build-only compiler source/reference/analyzer membership checked; no host/test/hardware execution. SDK EnableNETAnalyzers=false is duplicate-package selection: actual pinned NuGet analyzers verified in Csc command.'),indent=2)+'\n')
print(f'PASS {context}: {len(sources)} actual compiler sources, {len(refs)} references, {len(analyzers)} analyzers; preserved text and binary identities.')
