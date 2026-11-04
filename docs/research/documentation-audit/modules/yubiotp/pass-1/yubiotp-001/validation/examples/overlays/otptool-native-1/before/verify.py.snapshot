"""Check final block/addition/dependency identities and optional actual compiler evidence."""
from pathlib import Path
import argparse
import ast
import json
import re
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, scan, identities, sha

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('revision')
parser.add_argument('stage',choices=('prepared','compiled'))
parser.add_argument('--capture',action='append',default=[],help='Exact context=attempt, once for each compiler context')
parser.add_argument('--live',action='store_true',help='Also require current binary/analyzer dependency identities, not only retained evidence')
args=parser.parse_args()
if not re.fullmatch(r'v[1-9][0-9]*',args.revision):
    raise SystemExit('Require vN revision')
folder=HERE/args.revision
manifest=json.loads((folder/'manifest.json').read_text())

def require(condition,message):
    if not condition:
        raise SystemExit(message)

def verify_file(base,row):
    raw=(base/row['path']).read_bytes()
    require(len(raw)==row['bytes'] and sha(raw)==row['sha256'],f'Changed retained/input file: {base/row["path"]}')

current=scan()
require(identities(current)==identities(manifest['inventory']),'Current fenced/meaningful-inline/XML/mapping inventory changed, including additions/removals; prepare a new revision, never overwrite old extraction/evidence')
for row in manifest['prepared_files']+manifest['tool_sources']+manifest['approach_provenance']+manifest['dependencies']:
    verify_file(ROOT,row)
for group in manifest['groups']:
    raw=(folder/group['raw']).read_text()
    require(raw=='\n'.join(item['body'] for item in group['originals']) and sha(raw.encode())==group['raw_sha256'],f'Original bundle differs: {group["name"]}')
    code=(folder/group['context']/'src/Examples.cs').read_text()
    require(group['wrapper'] in code and group['type_body'] in code,f'Complete wrapper/type missing: {group["name"]}')
selected={(item['path'],item['csharp_index']) for group in manifest['groups'] for item in group['originals'] if 'csharp_index' in item}
excluded={(entry['block']['path'],entry['block']['csharp_index']) for entry in manifest['exclusions']}
all_blocks={(item['path'],item['csharp_index']) for item in manifest['inventory']['fences'] if item['csharp_index'] is not None}
require(selected.isdisjoint(excluded) and selected|excluded==all_blocks,'Incomplete/overlapping C# fence disposition')
for path in HERE.glob('*.py'):
    ast.parse(path.read_text(),filename=str(path))
for pattern in ('*.csproj','*.props','*.targets'):
    for path in folder.rglob(pattern):
        ET.parse(path)
for context in ('consumer','annotations'):
    name=f'Yubico.YubiKit.AuditOtp001.Examples.{args.revision}.{context}'
    matches=list(ROOT.rglob(name+'.csproj'))
    require(matches==[folder/context/'src'/f'{name}.csproj'],f'Project-name collision: {name}')
sample_matches=[path for path in ROOT.rglob('*.csproj') if '/src/' in str(path) and 'Yubico.YubiKit.' in str(path)
                and '/.claude/' not in str(path) and 'otptool' in path.stem.lower()]
require(sample_matches==[ROOT/manifest['sample']['traversal_driver']],'OtpTool build selector discovery changed/collided')
print(f'PASS: {len(all_blocks)} C# fences: {len(selected)} selected / {len(excluded)} excluded; {len(manifest["groups"])} exact complete groups; body/addition and source/config dependency identities reconciled.')
print(f'PASS: XML documentation scanned in {len(current["xml_documents"])} module C# files, including all {len(current["edited_xml_paths"])} edited C# paths; Python/XML syntax and two unique compiler-host names inspected.')
if args.stage=='prepared':
    require(not args.capture and not args.live,'Prepared-only mode does not imply compiler evidence')
    print('NOT RUN: builds, C# binding/analyzers, examples/sample execution, compiler captures, hardware or formatter.')
else:
    captures={}
    for value in args.capture:
        context,attempt=value.split('=',1)
        require(context in ('consumer','annotations','otptool') and context not in captures and re.fullmatch(r'[a-z0-9-]+',attempt),'Require distinct context=new-attempt capture selectors')
        captures[context]=attempt
    require(set(captures)=={'consumer','annotations','otptool'},'Require all three real compiler contexts, including the actual shipping OtpTool sample')
    for context,attempt in captures.items():
        evidence=folder/context/'evidence'/attempt
        data=json.loads((evidence/'manifest.json').read_text())
        require(data['revision']==args.revision and data['context']==context and data['attempt']==attempt,'Compiler capture identity mismatch')
        for row in data['retained']:
            verify_file(evidence,row)
        retained_by_name={row['path']:row for row in data['retained']}
        for name in ('Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json','.editorconfig'):
            require(sha((ROOT/name).read_bytes())==retained_by_name[name+'.snapshot']['sha256'],f'Captured canonical configuration differs from final current input: {name}')
        project=ROOT/'src/YubiOtp/examples/OtpTool/OtpTool.csproj' if context=='otptool' else folder/context/'src'/f'Yubico.YubiKit.AuditOtp001.Examples.{args.revision}.{context}.csproj'
        require(sha(project.read_bytes())==retained_by_name['compiled-project.csproj.snapshot']['sha256'],'Captured project differs from final current project')
        if args.live:
            require(sha(Path(data['policy']['ProjectAssetsFile']).read_bytes())==retained_by_name['project.assets.json']['sha256'],'Current restore assets differ from captured compiler context')
        for row in data['records']:
            if 'retained' in row:
                retained=dict(row,path=row['retained'])
                verify_file(evidence,retained)
            if args.live:
                verify_file(Path('/'),row)
        # Actual compiler source snapshot must bind to current complete generated
        # host or all 14 actual sample bodies, not just an old successful build.
        required=[ROOT/path for path in manifest['sample']['source_paths']] if context=='otptool' else [folder/context/'src/Examples.cs']
        sources={row['path']:row for row in data['records'] if row['kind']=='source'}
        for path in required:
            require(str(path) in sources and sha(path.read_bytes())==sources[str(path)]['sha256'],f'Captured compiler source differs from final current source: {path}')
        print(f'PASS: exact {context}/{attempt} retained Csc membership/source/reference/analyzer/config/assets evidence; current compiled example/sample bodies match.')
    print('Live binary/analyzer dependency identities checked.' if args.live else 'NOT RUN: live dependency identity recheck; retained identities remain available without keeping binaries/caches. Use --live before final closeout.')
print('Current document hashes (surrounding prose can change independently of compiled bodies):')
for row in current['documents']:
    print(row['sha256'],row['path'])
