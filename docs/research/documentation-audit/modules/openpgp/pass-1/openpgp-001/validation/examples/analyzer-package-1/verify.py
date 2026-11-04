"""Verify complete final snippet dispositions/current bodies and optional actual compiler evidence."""
from pathlib import Path
import argparse
import ast
import json
import re
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import analyzer_policy as _analyzer_policy
from inventory import HERE, ROOT, scan, identities, sha, unit_id, require, source_paths

_analyzer_policy.receipt()
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('revision')
parser.add_argument('stage', choices=('prepared', 'compiled'))
parser.add_argument('--capture', action='append', default=[], help='Exact context=attempt, once per active context')
parser.add_argument('--live', action='store_true', help='Require current binary/analyzer/assets identities as well as retained evidence')
args = parser.parse_args()
require(bool(re.fullmatch(r'v[1-9][0-9]*', args.revision)), 'Require vN revision')
folder = HERE / args.revision
manifest_path = folder / 'manifest.json'
require(manifest_path.exists(), 'Await completed-editor extraction; host skeletons alone supply no snippet evidence')
manifest = json.loads(manifest_path.read_text())
require(manifest['status'] == 'extracted-final-inputs', 'Final extraction checkpoint missing')


def verify_file(base, row):
    raw = (base / row['path']).read_bytes()
    require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], f'Changed retained/input file: {base / row["path"]}')


current = scan()
require(identities(current) == identities(manifest['inventory']), 'Current fence/inline/XML/mapping bodies or additions/removals changed; prepare a new revision')
for key in ('prepared_files', 'tool_sources', 'approach_provenance', 'dependencies'):
    for row in manifest[key]:
        verify_file(ROOT, row)
all_units = {unit_id(kind, item): item for kind, plural in [('fence', 'fences'), ('inline', 'inline'), ('xml', 'xml'), ('mapping', 'mappings')]
             for item in current[plural]}
dispositions = {row['id']: row for row in manifest['dispositions']}
require(len(dispositions) == len(manifest['dispositions']) and set(dispositions) == set(all_units), 'Require exactly one explicit disposition for every fence/inline/XML/mapping unit')
allowed = {'compile', 'historical', 'noncompiling-by-design', 'symbolic', 'other-applet', 'non-csharp', 'non-executable'}
for row in dispositions.values():
    require(row['status'] in allowed and bool(row['reason'].strip()), 'Require a concrete disposition and reason')
selected = []
for group in manifest['groups']:
    raw = (folder / group['raw']).read_text()
    require(bool(group.get('prerequisites')) and bool(group.get('resource_lifetimes')) and bool(group.get('transform')), 'Describe typed dependencies, resource ownership/lifetimes and whole-body wrapper transformation for every group')
    require(bool(group['originals']) and raw == '\n'.join(item['body'] for item in group['originals']) and sha(raw.encode()) == group['raw_sha256'], f'Original complete bundle changed: {group["name"]}')
    code = (folder / group['context'] / 'src/Examples.cs').read_text()
    require(group['wrapper'] in code and group['type_body'] in code, f'Complete wrapper/type missing: {group["name"]}')
    for original in group['originals']:
        identity = original['unit_id']
        require(identity in all_units and original['body'] == all_units[identity]['body'] and original['body_sha256'] == all_units[identity]['body_sha256'], 'Group original differs from current complete body')
        require(dispositions[identity]['status'] == 'compile' and dispositions[identity]['group'] == group['name'], 'Group/disposition binding differs')
        selected.append(identity)
require(len(selected) == len(set(selected)) and set(selected) == {key for key, row in dispositions.items() if row['status'] == 'compile'}, 'Compiled-group membership has omissions or overlaps')
active = set(manifest['active_contexts'])
require(active <= {'consumer', 'annotations', 'openpgptool'} and {'consumer', 'openpgptool'} <= active, 'Require genuine consumer and complete shipping contexts')
require({group['context'] for group in manifest['groups']} == active - {'openpgptool'}, 'Compile each meaningful context; leave unused skeletons outside the result')
sample = manifest['sample']
require(len(sample['source_paths']) == 19 and sample['source_paths'] == source_paths(ROOT / 'src/OpenPgp/examples/OpenPgpTool'), 'Reconcile all 19 current OpenPgpTool source paths')
for path in HERE.glob('*.py'):
    ast.parse(path.read_text(), filename=str(path))
for pattern in ('*.csproj', '*.props', '*.targets'):
    for path in folder.rglob(pattern):
        ET.parse(path)
for context in active - {'openpgptool'}:
    name = f'Yubico.YubiKit.AuditPgp001.Examples.{args.revision}.{context}'
    selector = f'AuditPgp001.Examples.{args.revision}.{context}'
    matches = sorted(path for path in ROOT.rglob('*.csproj') if selector.lower() in path.stem.lower())
    require(matches == [folder / context / 'src' / (name + '.csproj')], f'Project name collision: {name}')
driver = ROOT / sample['traversal_driver']
name = 'AuditPgp001.Examples.Shipping.OpenPgpTool'
matches = [path for path in ROOT.rglob('*.csproj') if name.lower() in path.stem.lower()]
require(matches == [driver], 'Shipping traversal selector changed/collided')
print(f'PASS: {len(all_units)} explicit unit dispositions, {len(manifest["groups"])} complete groups, 19 shipping source paths; current bodies, typed wrappers and source/configuration identities reconcile.')
if args.stage == 'prepared':
    require(not args.capture and not args.live, 'Prepared-only mode leaves compiler evidence separate')
    print('PENDING: builds, C# binding/analyzers and actual compiler captures. Example/sample/test/hardware execution remains separately scoped.')
else:
    captures = {}
    for value in args.capture:
        context, attempt = value.split('=', 1)
        require(context in active and context not in captures and bool(re.fullmatch(r'[a-z0-9-]+', attempt)), 'Require distinct context=new-attempt capture selectors')
        captures[context] = attempt
    require(set(captures) == active, 'Require every active snippet context and complete actual shipping sample')
    for context, attempt in captures.items():
        evidence = folder / context / 'evidence' / attempt
        data = json.loads((evidence / 'manifest.json').read_text())
        require(data['revision'] == args.revision and data['context'] == context and data['attempt'] == attempt, 'Compiler capture identity mismatch')
        require(data['extraction_manifest_sha256'] == sha(manifest_path.read_bytes()), 'Capture binds another extraction manifest')
        _analyzer_policy.verify_capture(data, evidence, args.live)
        for row in data['retained']:
            verify_file(evidence, row)
        retained = {row['path']: row for row in data['retained']}
        for name in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig'):
            require(sha((ROOT / name).read_bytes()) == retained[name + '.snapshot']['sha256'], f'Captured root policy differs from current input: {name}')
        project = ROOT / sample['project'] if context == 'openpgptool' else folder / context / 'src' / f'Yubico.YubiKit.AuditPgp001.Examples.{args.revision}.{context}.csproj'
        require(sha(project.read_bytes()) == retained['compiled-project.csproj.snapshot']['sha256'], 'Captured project differs from current project')
        if context == 'openpgptool':
            require(data['native_shipping_capture'] is True, 'Require native-output actual shipping capture')
            sdk = data['sdk_optional_hook']
            require(sdk['sdk_version'] == data['policy']['NETCoreSdkVersion'] == '10.0.403' and sdk['target_exists'] is False, 'Require pinned SDK optional-hook evidence')
            for source in sdk['sdk_sources']:
                require(retained['sdk-source/' + Path(source['path']).name + '.snapshot']['sha256'] == source['sha256'], 'SDK source snapshot differs from optional-hook evidence')
            require(data['policy']['MSBuildProjectExtensionsPath'].rstrip('/\\') == str(project.parent / 'obj'), 'Require ordinary shipping obj provenance')
            hook = folder / context / 'OpenPgpTool.Capture.targets'
            require(sha(hook.read_bytes()) == retained['capture-hook.targets.snapshot']['sha256'], 'Active late capture hook changed')
        if args.live:
            require(sha(Path(data['policy']['ProjectAssetsFile']).read_bytes()) == retained['project.assets.json.snapshot']['sha256'], 'Current restore assets differ from capture')
        for row in data['records']:
            if 'retained' in row:
                verify_file(evidence, dict(row, path=row['retained']))
            if args.live:
                verify_file(Path('/'), row)
        required = [ROOT / path for path in sample['source_paths']] if context == 'openpgptool' else [folder / context / 'src/Examples.cs']
        sources = {row['path']: row for row in data['records'] if row['kind'] == 'source'}
        for path in required:
            require(str(path) in sources and sha(path.read_bytes()) == sources[str(path)]['sha256'], f'Actual compiler source differs from final current source: {path}')
        print(f'PASS: exact {context}/{attempt} actual Csc/source/reference/analyzer/config/assets evidence; current complete compiler bodies match.')
    print('PASS: live binary/analyzer/assets identities checked.' if args.live else 'PENDING: live dependency identity recheck; retained identities remain usable without binaries/caches.')
