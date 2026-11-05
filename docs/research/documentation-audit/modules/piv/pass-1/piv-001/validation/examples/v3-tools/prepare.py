"""Prepare additive v3 inputs after the independently accepted Markdown annotation correction."""
from pathlib import Path
import argparse
import ast
import copy
import hashlib
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
TOOLS = Path(__file__).resolve().parent
HERE = TOOLS.parent
RUN = HERE.parent.parent
ROOT = next(path for path in HERE.parents if (path / 'toolchain.cs').exists())
FOLDER = HERE / 'v3'
BASELINE = '727fe43afc430c5d8364d868a6436de50626e476'
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('stage', choices=('tools', 'prepare'))
parser.add_argument('--acceptance-sha256', help='Explicit coordinator-released latest post-format acceptance identity')
args = parser.parse_args()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=sha(raw))


def repo_record(path):
    return dict(identity(path), path=str(path.relative_to(ROOT)))


def retain(path, raw):
    require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
    require(path.is_relative_to(HERE), 'Output escaped examples ownership')
    current = HERE
    for part in path.relative_to(HERE).parts:
        current /= part
        require(not current.is_symlink(), 'Symlinked extraction output')
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        require(path.read_bytes() == raw, 'Preserve changed existing v3 preparation: ' + str(path))
    else:
        with path.open('xb') as output:
            output.write(raw)


def save(path, value):
    retain(path, (json.dumps(value, indent=2) + '\n').encode())


require(not (FOLDER / 'tooling-provenance.json').exists(), 'Preserve the existing v3 checkpoint')
preserved_paths = {path for path in HERE.rglob('*') if path.is_file() and not path.is_relative_to(TOOLS) and not path.is_relative_to(FOLDER)}
preserved_paths |= set((RUN / 'validation/reconciliation-v1').rglob('*'))
preserved_paths |= {RUN / 'validation' / name for name in ('examples-v2-consumer-build.log', 'examples-v2-consumer-capture.log', 'examples-v2-consumer-capture-second.log', 'examples-v2-annotations-build.log')}
preserved_paths |= {RUN / 'evidence' / name for name in ('review-accepted.json', 'review-accepted-v2.json', 'review-accepted-v3.json')}
preserved = [identity(path) for path in sorted(preserved_paths) if path.is_file()]
changes = []


def derive(source_name, target_name, substitutions):
    source = HERE / source_name
    text = source.read_text()
    operations = []
    for before, after in substitutions.items():
        count = text.count(before)
        require(count > 0, 'Required v3 source adaptation absent: ' + source_name + ': ' + before)
        text = text.replace(before, after)
        operations.append(dict(before=before, after=after, count=count))
    ast.parse(text, filename=target_name)
    target = TOOLS / target_name
    retain(target, text.encode())
    changes.append(dict(source=identity(source), target=identity(target), substitutions=operations))


derive('inventory.py', 'inventory.py', {
    'HERE = Path(__file__).resolve().parent': 'HERE = Path(__file__).resolve().parent.parent',
    "HERE / 'extraction-scope.json'": "HERE / 'v3/extraction-scope.json'",
})
derive('compiler_policy.py', 'compiler_policy.py', {
    'HERE = Path(__file__).resolve().parent': 'HERE = Path(__file__).resolve().parent.parent',
    "HERE / 'tooling-provenance.json'": "HERE / 'v3/tooling-provenance.json'",
})
derive('sdk-policy.py', 'sdk-policy.py', {})
derive('project_graph_v3.py', 'project_graph_v3.py', {
    "HERE / 'project-graph-1/receipt.json'": "HERE / 'v3/project-graph-acceptance.json'",
})
derive('capture-v3.py', 'capture-v3.py', {
    "HERE / 'sdk-policy.py'": "Path(__file__).resolve().parent / 'sdk-policy.py'",
    "HERE / 'tooling-provenance.json'": "HERE / 'v3/tooling-provenance.json'",
    "HERE / 'project-graph-1/receipt.json'": "HERE / 'v3/project-graph-acceptance.json'",
})
derive('verify-v3.py', 'verify-v3.py', {
    "HERE / 'project-graph-1/receipt.json'": "HERE / 'v3/project-graph-acceptance.json'",
})
derive('prepare-final.py', 'prepare-final.py', {
    "code = '\\n'.join(imports + context_code[context]) + '\\n'":
        "code = '\\n'.join(imports) + f'\\nnamespace AuditPiv001.Examples.V3.{context.title()};\\n\\n' + '\\n'.join(context_code[context]) + '\\n'",
    "sorted(HERE.glob('*.py'))": "sorted(Path(__file__).resolve().parent.glob('*.py'))",
    "HERE / 'tooling-provenance.json'": "HERE / 'v3/tooling-provenance.json'",
    "HERE / 'extraction-scope.json'": "HERE / 'v3/extraction-scope.json'",
})
derive('bind-source-context-v2.py', 'bind-source-context-v3.py', {
    "folder = HERE / 'v2'": "folder = HERE / 'v3'",
    "revision='v2'": "revision='v3'",
    "HERE / 'extraction-scope.json'": "HERE / 'v3/extraction-scope.json'",
    "sorted(HERE.glob('*.py'))": "sorted(Path(__file__).resolve().parent.glob('*.py'))",
    'final v2 verification': 'final v3 verification',
})
derive('verify-v2.py', 'verify-source-context-v3.py', {
    "folder = HERE / 'v2'": "folder = HERE / 'v3'",
    "HERE / 'extraction-scope.json'": "HERE / 'v3/extraction-scope.json'",
    "HERE.glob('*.py')": "Path(__file__).resolve().parent.glob('*.py')",
    "print('PASS: 61 baseline originals": "print(f'PASS: 61 baseline originals",
    '730 explicit dispositions': '{len(manifest["dispositions"])} explicit dispositions',
})
tool_draft = dict(status='source-only-tools-awaiting-final-release', transformations=changes,
                  tools=[identity(path) for path in sorted(TOOLS.glob('*.py'))],
                  release_requirement='Latest independently accepted review-accepted-v3.json plus matching coordinator reviewed-files/format-transform identities; README inline selector changes and the annotation fence are reconciled separately.')
save(TOOLS / 'tool-transformations.json', tool_draft)
if args.stage == 'tools':
    print('Prepared nine versioned v3 source-tool adaptations; final product-body extraction and manifest remain gated on the latest post-format acceptance/reconciliation release.')
    raise SystemExit(0)
require(args.acceptance_sha256 is not None and len(args.acceptance_sha256) == 64, 'Supply the explicit latest coordinator-released acceptance SHA-256')
accepted_path = RUN / 'evidence/review-accepted-v3.json'
require(sha(accepted_path.read_bytes()) == args.acceptance_sha256, 'Post-format acceptance identity changed')
accepted = json.loads(accepted_path.read_text())
reviewed_path = RUN / 'reviewed-files.json'
reviewed = json.loads(reviewed_path.read_text())
format_path = RUN / 'validation/format-transform.json'
formatted = json.loads(format_path.read_text())
require(accepted['baseline'] == reviewed['baseline'] == formatted['baseline'] == BASELINE, 'Release baseline mismatch')
old_review_path = RUN / 'validation/reconciliation-v1/reviewed-files.json'
old_review = json.loads(old_review_path.read_text())
previous = {row['path']: row for row in old_review['files']}
final = {row['path']: row for row in reviewed['files']}
accepted_map = {row['path']: row for row in accepted['files']}
require(len(final) == len(previous) == len(accepted_map) == 57 and set(final) == set(previous) == set(accepted_map), 'Accepted final path set changed')
changed = []
for path, row in final.items():
    current = sha((ROOT / path).read_bytes())
    require(current == row['final_sha256'] == accepted_map[path]['sha256'], 'Current post-format accepted file changed: ' + path)
    if current != previous[path]['final_sha256']:
        changed.append(path)
require(set(changed) == {'src/Piv/CLAUDE.md', 'src/Piv/README.md'}, 'Require the two independently accepted Markdown paths')
current_claude = (ROOT / 'src/Piv/CLAUDE.md').read_bytes()
require(sha(current_claude) == 'f97b64c75b941c90e339f9b9b9e6b51a269f4bafeaae3d58e3a5bc2d22578d65', 'Corrected CLAUDE identity changed')
require(sha((ROOT / 'src/Piv/README.md').read_bytes()) == '1725cae8c82139750743613680a256160f5691955473bef2bee02a49a54599b8', 'Released qualified-selector README identity changed')
reverse = current_claude.replace(b', MinFirmware = "5.3.0"', b'').replace(b'Assert.InRange(metadata.RetriesRemaining, 0, metadata.TotalRetries)', b'Assert.NotNull(metadata)')
old_claude_path = RUN / 'validation/reconciliation-v1/CLAUDE.md.snapshot'
require(reverse == old_claude_path.read_bytes() and sha(reverse) == previous['src/Piv/CLAUDE.md']['final_sha256'], 'Markdown annotation change exceeds the exact two reviewed lines')
require(formatted['post_format_review_sha256'] == sha(accepted_path.read_bytes()) and set(formatted['late_markdown_changes']) == set(changed) and len(formatted['files']) == 48, 'Formatter/post-format acceptance chain changed')
old_format = json.loads((RUN / 'validation/reconciliation-v1/validation--format-transform.json').read_text())
require(formatted['files'] == old_format['files'] and all(row['whitespace_only'] and row['imports_identical'] for row in formatted['files']), 'Preserve all original 48 C# formatter proofs')
scope = json.loads((HERE / 'extraction-scope.json').read_text())
scope.update(notification='Coordinator released v3 final inputs after independent acceptance of the Markdown annotation correction and qualified canonical README project selectors. Fresh v3 consumer, annotation and actual-shipping contexts require their own compilation/captures.',
             final_review=dict(path=str(reviewed_path.relative_to(ROOT)), sha256=sha(reviewed_path.read_bytes())),
             formatter_record=dict(path=str(format_path.relative_to(ROOT)), sha256=sha(format_path.read_bytes())),
             accepted_review=dict(path=str(accepted_path.relative_to(ROOT)), sha256=sha(accepted_path.read_bytes())),
             annotation_correction_review=repo_record(RUN / 'review-example-correction.md'),
             selector_correction_review=repo_record(RUN / 'review-selector-correction.md'))
save(FOLDER / 'extraction-scope.json', scope)
tools = [identity(path) for path in sorted(TOOLS.glob('*.py'))]
bootstrap = dict(baseline=BASELINE, revision='v3', parent_extraction=identity(HERE / 'v2/manifest.json'),
                 parent_graph_acceptance=identity(HERE / 'project-graph-1/receipt.json'),
                 accepted_review=identity(accepted_path), reviewed_files=identity(reviewed_path), formatter=identity(format_path),
                 correction_review=identity(RUN / 'review-example-correction.md'),
                 selector_correction_review=identity(RUN / 'review-selector-correction.md'),
                 release_gate_history=[identity(TOOLS / name) for name in ('preparation.log', 'release-gate-hold.json')],
                 changed_product_paths=changed, exact_markdown_reversal=True,
                 annotation_body_sha256='4464f14bda4ed114d1cbbb0c3994bc0af74fab338095a7eb6ee9ba5a756d213e',
                 preserved=preserved, transformations=changes, tools=tools,
                 policy='New v3 extraction and unique hosts; prior v2 builds/captures and failures remain immutable history. Exact project-closure/analyzer/source/NoSkip/WAE algorithms retained. Every v3 context requires a fresh build and capture; no historical compilation result is relabeled.')
save(FOLDER / 'tooling-provenance.json', bootstrap)


def run_static(*args):
    subprocess.run([sys.executable, *args], cwd=ROOT, check=True)


run_static(str(TOOLS / 'prepare-final.py'), 'inventory', 'v3')
current = json.loads((FOLDER / 'inventory.json').read_text())
prior = json.loads((HERE / 'v2/inventory.json').read_text())
require(current['xml_documents'] == prior['xml_documents'], 'All 111 module C# inputs remain unchanged')
require(current['xml'] == prior['xml'] and current['mappings'] == prior['mappings'], 'XML/mapping body set changed')
old_inline = {(row['path'], row['index']): row for row in prior['inline']}
new_inline = {(row['path'], row['index']): row for row in current['inline']}
inline_changes = []
for key in sorted(set(old_inline) | set(new_inline)):
    old, new = old_inline.get(key), new_inline.get(key)
    if old != new:
        # A nearby added sentence can move line numbers; compare actual token bodies separately.
        if old is not None and new is not None and old['body'] == new['body']:
            continue
        require(key[0] == 'src/Piv/README.md', 'Only qualified README inline selector references may change')
        inline_changes.append(dict(path=key[0], index=key[1], before=old, after=new))
body_changes = []
for old, new in zip(prior['fences'], current['fences'], strict=True):
    if old['body'] != new['body'] or old['body_sha256'] != new['body_sha256']:
        require(old['path'] == new['path'] == 'src/Piv/CLAUDE.md' and old['index'] == new['index'] == 6, 'Only the reviewed annotation fence may change')
        body_changes.append(dict(path=new['path'], index=6, before_sha256=old['body_sha256'], after_sha256=new['body_sha256']))
require(len(body_changes) == 1 and body_changes[0]['after_sha256'] == bootstrap['annotation_body_sha256'], 'Require the one exact accepted annotation body change')
plan = copy.deepcopy(json.loads((HERE / 'final-plan-v2.json').read_text()))
plan['inventory_sha256'] = sha((FOLDER / 'inventory.json').read_bytes())
old_dispositions = {row['id']: row for row in plan['dispositions']}
dispositions = []
for kind, plural in [('fence', 'fences'), ('inline', 'inline'), ('xml', 'xml'), ('mapping', 'mappings'), ('task', 'tasks')]:
    for row in current[plural]:
        suffix = f"{row['chunk']}:{row['tag']}:{row['index']}" if kind == 'xml' else str(row.get('id', row.get('index')))
        key = f"{kind}:{row['path']}:{suffix}"
        if key in old_dispositions:
            dispositions.append(old_dispositions[key])
        else:
            require(kind == 'inline' and row['path'] == 'src/Piv/README.md', 'Adjudicate new executable/body units explicitly')
            dispositions.append(dict(id=key, status='symbolic', reason='Qualified canonical toolchain selector/reference in the independently accepted README prose; this inline command is retained separately from the unchanged C# example bodies.'))
plan['dispositions'] = dispositions
group = next(row for row in plan['groups'] if row['name'] == 'metadata-test-annotation')
group['prerequisites'] = ['Actual xUnit v2/skippable theory and Tests.Shared context; WithYubiKey requires PIV and firmware 5.3.0. Assert.InRange checks 0 <= RetriesRemaining <= TotalRetries using the actual PinMetadata record-struct fields.']
save(FOLDER / 'final-plan.json', plan)
save(FOLDER / 'body-change.json', dict(baseline=BASELINE, parent_inventory=identity(HERE / 'v2/inventory.json'),
                                     current_inventory=identity(FOLDER / 'inventory.json'), changes=body_changes,
                                     inline_selector_changes=inline_changes,
                                     unchanged_current_bodies=59, unchanged_consumer_groups=44,
                                     prior_compilation_scope='The accepted v2 consumer capture stays attached to v2; all new v3 hosts await fresh compilation.'))
run_static(str(TOOLS / 'prepare-final.py'), 'extract', 'v3', '--plan', str((FOLDER / 'final-plan.json').relative_to(ROOT)))
run_static(str(TOOLS / 'bind-source-context-v3.py'))
old_graph = json.loads((HERE / 'project-graph-1/receipt.json').read_text())
contexts = copy.deepcopy(old_graph['contexts'])
for context in contexts:
    if context['name'] != 'pivtool':
        path = FOLDER / context['name'] / 'src' / f'Yubico.YubiKit.AuditPiv001.Examples.v3.{context["name"]}.csproj'
        context['project'] = identity(path)
    else:
        require(identity(Path(context['project']['path'])) == context['project'], 'Actual shipping project changed')
    for row in context['graph_projects']:
        require(identity(Path(row['path'])) == row, 'Context graph dependency metadata changed')
    project = Path(context['project']['path'])
    direct = {str((project.parent / node.attrib['Include'].replace('$(RepositoryRoot)', str(ROOT)).replace('\\', '/')).resolve()) for node in ET.parse(project).iter('ProjectReference')}
    require(direct == set(context['direct']), 'Preserve exact context-specific direct graph sets')
graph_acceptance = dict(id='v3-project-graph-acceptance', baseline=BASELINE, revision='v3',
                        parent_extraction=identity(FOLDER / 'manifest.json'), parent_source_context=identity(FOLDER / 'source-context.json'),
                        prior_graph_acceptance=identity(HERE / 'project-graph-1/receipt.json'),
                        v3_tooling_provenance=identity(FOLDER / 'tooling-provenance.json'),
                        hardware_source=old_graph['hardware_source'], contexts=contexts,
                        preserved=preserved, tools=tools, consumer_workspace=[],
                        acceptance='Coordinator released the independently reviewed annotation correction for fresh v3 extraction and all three new compiler contexts. Preserve exact direct/closure sets 5/12, 2/4 and 5/10 and all canonical compiler/analyzer guards. Old v2 compilation and capture evidence remains historical.')
save(FOLDER / 'project-graph-acceptance.json', graph_acceptance)
print('Prepared fresh v3: one accepted annotation body change plus separate README inline-selector dispositions, unchanged 111 C# sources and 44 consumer groups; unique v3 hosts, full actual shipping project and versioned graph/canonical-analyzer tools. Builds/captures remain coordinator-owned.')
