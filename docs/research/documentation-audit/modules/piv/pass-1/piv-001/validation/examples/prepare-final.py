"""Inventory released final inputs, then bind explicit complete-body groups and dispositions."""
from pathlib import Path
import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, RUN, canonical, sha, file_record, require, scan, unit_id, source_paths

BASELINE = '727fe43afc430c5d8364d868a6436de50626e476'


def retain(path, raw):
    require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
    require(path.is_relative_to(HERE), 'Output escaped examples ownership')
    current = HERE
    for part in path.relative_to(HERE).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked output: {current}')
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        require(path.read_bytes() == raw, f'Preserve changed prior preparation: {path}')
    else:
        with path.open('xb') as output:
            output.write(raw)


def save(path, value):
    retain(path, (json.dumps(value, indent=2) + '\n').encode())


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('stage', choices=('inventory', 'extract'))
parser.add_argument('revision', help='Fresh vN; v1 is reserved for immutable empty skeletons')
parser.add_argument('--plan', help='Run-local manual complete-body grouping/disposition plan')
args = parser.parse_args()
require(bool(re.fullmatch(r'v[2-9][0-9]*|v1[0-9]+', args.revision)), 'Use a fresh revision v2 or later')
folder = HERE / args.revision
current = scan()  # Reads current bodies only after reviewed/formatted release verification.
inventory_path = folder / 'inventory.json'
save(inventory_path, current)
units = {unit_id(kind, item): item for kind, plural in [('fence', 'fences'), ('inline', 'inline'), ('xml', 'xml'), ('mapping', 'mappings'), ('task', 'tasks')] for item in current[plural]}
if args.stage == 'inventory':
    require(args.plan is None, 'Inventory stage takes the release scope only')
    print(f'Retained {len(units)} complete current fence/inline/XML/mapping/task bodies in {inventory_path.relative_to(ROOT)}; assign every identity an explicit disposition before extraction.')
    raise SystemExit(0)
require(args.plan is not None, 'Extraction requires a manual typed-group/disposition plan')
plan_path = canonical(args.plan)
require(plan_path.is_relative_to(HERE), 'Plan belongs within assigned examples scope')
plan = json.loads(plan_path.read_text())
require(plan['baseline'] == BASELINE and plan['inventory_sha256'] == sha(inventory_path.read_bytes()), 'Grouping plan binds another baseline/inventory')
dispositions = {row['id']: row for row in plan['dispositions']}
require(len(dispositions) == len(plan['dispositions']) and set(dispositions) == set(units), 'Require exactly one disposition for every current body')
allowed = {'compile', 'historical', 'noncompiling-by-design', 'symbolic', 'other-applet', 'non-csharp', 'non-executable'}
require(all(row['status'] in allowed and bool(row['reason'].strip()) for row in dispositions.values()), 'Use explicit dispositions with useful reasons')
require(plan['helpers_reviewed_as_application_code'] is True, 'Review typed prerequisites/helpers as application-owned code')
groups, selected, context_code = [], [], {}
require(len(plan['groups']) == len({row['name'] for row in plan['groups']}), 'Duplicate group names')
for group in plan['groups']:
    name, context = group['name'], group['context']
    require(bool(re.fullmatch(r'[a-z0-9-]+', name)) and context in ('consumer', 'annotations'), 'Require unique safe task name and genuine context')
    require(group['prerequisites'] and group['resource_lifetimes'] and group['transform'], 'Describe typed dependencies, lifetimes and complete-body transformation')
    code = group['template']
    originals = []
    for identity in group['units']:
        require(identity in units and dispositions[identity]['status'] == 'compile' and dispositions[identity]['group'] == name, 'Group/disposition membership mismatch')
        marker = '{{unit:' + identity + '}}'
        require(code.count(marker) == 1, 'Insert each full original body once into its typed wrapper')
        original = dict(units[identity], unit_id=identity)
        body = original['body']
        for change in group.get('body_changes', {}).get(identity, []):
            require(change['reason'] and body.count(change['before']) == change['count'] > 0, 'Record exact bounded body transformation and rationale')
            body = body.replace(change['before'], change['after'])
        code = code.replace(marker, body)
        originals.append(original)
        selected.append(identity)
    require(bool(originals) and '{{unit:' not in code, 'Resolve every complete-body insertion')
    require(not re.search(r'\bnamespace\s+Yubico\b', code), 'Use application-owned helpers with real SDK references')
    raw_relative = 'raw/' + name + '.cs.txt'
    raw = '\n'.join(item['body'] for item in originals)
    retain(folder / raw_relative, raw.encode())
    groups.append(dict(group, raw=raw_relative, originals=originals, raw_sha256=sha(raw.encode()), wrapper=code, type_body=code))
    context_code.setdefault(context, []).append(code)
require(len(selected) == len(set(selected)) and set(selected) == {key for key, row in dispositions.items() if row['status'] == 'compile'}, 'Complete compiled membership requires unique coverage')
require('consumer' in context_code, 'Compile a meaningful consumer context')
active = sorted(set(context_code) | {'pivtool'})
for context in ('consumer', 'annotations'):
    source = HERE / 'v1' / context
    for path in source.rglob('*'):
        if path.is_file():
            relative = path.relative_to(source)
            new_name = str(relative).replace('.v1.', '.' + args.revision + '.')
            text = path.read_text().replace('examples-v1-', 'examples-' + args.revision + '-')
            if path.suffix == '.csproj':
                root = ET.fromstring(text)
                extra = plan.get('additional_project_references', {}).get(context, [])
                require(len(extra) == len(set(extra)), 'Duplicate additional project references')
                items = ET.SubElement(root, 'ItemGroup') if extra else None
                for reference in extra:
                    target = canonical(reference)
                    require(reference.startswith('src/') and target.suffix == '.csproj' and target.is_file(), 'Use a genuine repository project dependency')
                    ET.SubElement(items, 'ProjectReference', Include='$(RepositoryRoot)/' + reference)
                text = ET.tostring(root, encoding='unicode')
            retain(folder / context / new_name, text.encode())
    if context in context_code:
        imports = plan['imports'][context]
        require(all(re.fullmatch(r'(?:global )?using [^\r\n]+;', item) for item in imports), 'Supply explicit import directives separately')
        code = '\n'.join(imports + context_code[context]) + '\n'
        retain(folder / context / 'src/Examples.cs', code.encode())
hook = HERE / 'v1/pivtool/PivTool.Capture.targets'
retain(folder / 'pivtool/PivTool.Capture.targets', hook.read_bytes().replace(b'examples-v1-', ('examples-' + args.revision + '-').encode()))
sample_project = canonical('src/Piv/examples/PivTool/PivTool.csproj')
sample_sources = source_paths(sample_project.parent)
require(len(sample_sources) == 36, 'Reconcile all current 36 PivTool source paths')
dependencies = {row['path']: row for row in current['documents'] + current['xml_documents']}
for relative in current['edited_paths'] + [item['path'] for item in current['tasks']]:
    dependencies[relative] = file_record(canonical(relative))
for relative in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig', str(sample_project.relative_to(ROOT))):
    dependencies[relative] = file_record(canonical(relative))
prepared = [file_record(path) for path in sorted(folder.rglob('*')) if path.is_file()]
tools = [file_record(path) for path in sorted(HERE.glob('*.py'))]
record = dict(baseline=BASELINE, revision=args.revision, status='extracted-final-inputs', inventory=current,
              dispositions=plan['dispositions'], groups=groups, active_contexts=active,
              prepared_files=prepared, tool_sources=tools,
              approach_provenance=[file_record(HERE / 'tooling-provenance.json'), file_record(plan_path), file_record(HERE / 'extraction-scope.json')],
              dependencies=sorted(dependencies.values(), key=lambda row: row['path']),
              sample=dict(project=str(sample_project.relative_to(ROOT)), source_paths=sample_sources,
                          traversal_driver=str((HERE / 'dispatch/src/Yubico.YubiKit.AuditPiv001.Examples.Shipping.PivTool.csproj').relative_to(ROOT))))
save(folder / 'manifest.json', record)
print(f'Prepared {len(groups)} complete task groups/{len(units)} explicit body dispositions and actual 36-source PivTool context; coordinator compilation/capture follows.')
