"""Prepare additive graph-corrected tools; preserve every original extraction/tool/failure identity."""
from pathlib import Path
import ast
import hashlib
import json
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
EXAMPLES = HERE.parent
RUN = EXAMPLES.parent.parent
ROOT = next(path for path in HERE.parents if (path / 'toolchain.cs').exists())
HARDWARE = RUN / 'validation/hardware-probe/capture-v3.py'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=sha(raw))


def retain(path, raw):
    require(not (RUN / 'record-manifest.json').exists(), 'Run is frozen')
    require(path.is_relative_to(EXAMPLES), 'Correction output escaped examples ownership')
    current = EXAMPLES
    for part in path.relative_to(EXAMPLES).parts:
        current /= part
        require(not current.is_symlink(), 'Symlinked correction output')
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        require(path.read_bytes() == raw, 'Preserve changed existing correction: ' + str(path))
    else:
        with path.open('xb') as output:
            output.write(raw)


require(not (HERE / 'receipt.json').exists(), 'Correction receipt already exists; preserve its identities')
# Pin all original example artifacts before generating the additive tools or receipt.
derived = {EXAMPLES / name for name in ('capture-v3.py', 'verify-v3.py', 'project_graph_v3.py')}
preserved_paths = {path for path in EXAMPLES.rglob('*') if path.is_file() and not path.is_relative_to(HERE) and path not in derived}
preserved_paths |= {RUN / 'validation/examples-v2-consumer-build.log', RUN / 'validation/examples-v2-consumer-capture.log'}
preserved = [identity(path) for path in sorted(preserved_paths)]
original = HARDWARE.read_text()
tree = ast.parse(original)
function = next(node for node in tree.body if isinstance(node, ast.FunctionDef) and node.name == 'project_closure_proof')
function_text = ast.get_source_segment(original, function)
require(function_text is not None, 'Working hardware graph helper absent')
old_expected = '''    expected = direct | {ROOT / path for path in (
        'src/Cli.Commands/src/Yubico.YubiKit.Cli.Commands.csproj', 'src/Cli.Shared/src/Yubico.YubiKit.Cli.Shared.csproj',
        'src/Ctap2/src/Yubico.YubiKit.Ctap2.csproj', 'src/Management/src/Yubico.YubiKit.Management.csproj',
        'src/Oath/src/Yubico.YubiKit.Oath.csproj', 'src/OpenPgp/src/Yubico.YubiKit.OpenPgp.csproj',
        'src/YubiHsm/src/Yubico.YubiKit.YubiHsm.csproj', 'src/YubiOtp/src/Yubico.YubiKit.YubiOtp.csproj')}
    require(len(closure) == 12 and closure == expected == set(nodes), 'Require exactly the known twelve-project transitive closure')'''
graph_changes = {
    'def project_closure_proof(project, direct, references, policy):': 'def project_closure_proof(project, direct, references, policy, evidence, expected):',
    "'Restored direct project graph differs from the declared four'": "'Restored direct project graph differs from the exact declared context set'",
    old_expected: "    require(closure == expected == set(nodes), 'Require the exact accepted context-specific transitive closure')",
    "DEST / 'project-references.txt'": "evidence / 'project-references.txt'",
    "DEST / 'resolved-project-references.txt'": "evidence / 'resolved-project-references.txt'",
    "        output, source = line.split('|', 1)": "        fields = line.split('|')\n        require(len(fields) in (2, 3) and bool(fields[1]), 'Require resolved output and MSBuildSourceProjectFile columns')\n        output, source = fields[:2]",
}
operations = []
for before, after in graph_changes.items():
    count = function_text.count(before)
    require(count == 1, 'Expected one bounded hardware graph adaptation: ' + before)
    function_text = function_text.replace(before, after)
    operations.append(dict(before=before, after=after, count=count))
header = '"""Versioned actual project closure and independent host/SDK-version proof."""\nfrom pathlib import Path\nimport json\nimport xml.etree.ElementTree as ET\nfrom inventory import HERE, ROOT, sha, require\n\n'
runtime = (HERE / 'runtime.py').read_bytes()
graph_raw = header.encode() + function_text.encode() + b'\n\n' + runtime
ast.parse(graph_raw.decode())
retain(EXAMPLES / 'project_graph_v3.py', graph_raw)
transforms = []
for name in ('capture', 'verify'):
    source = EXAMPLES / (name + '.py')
    text = source.read_text()
    changes = {
        'from provenance import project_reference_proof': 'from project_graph_v3 import project_reference_proof, acceptance_receipt',
        'from provenance import verify_project_proof': 'from project_graph_v3 import verify_project_proof, acceptance_receipt',
    }
    if name == 'capture':
        changes.update({
            "extraction = json.loads(manifest_path.read_text())": "extraction = json.loads(manifest_path.read_text())\ncorrection = acceptance_receipt()",
            'result = dict(project_reference_proof=project_proof,': "result = dict(project_graph_correction_sha256=sha((HERE / 'project-graph-1/receipt.json').read_bytes()), project_reference_proof=project_proof,",
        })
    else:
        changes.update({
            'manifest = json.loads(manifest_path.read_text())': 'manifest = json.loads(manifest_path.read_text())\ncorrection = acceptance_receipt()',
            "        _analyzer_policy.verify_capture(data, evidence, args.live)": "        require(data['project_graph_correction_sha256'] == sha((HERE / 'project-graph-1/receipt.json').read_bytes()), 'Capture binds another versioned graph correction')\n        _analyzer_policy.verify_capture(data, evidence, args.live)",
        })
    substitutions = []
    for before, after in changes.items():
        count = text.count(before)
        if not count and before.startswith('from provenance'):
            continue
        require(count == 1, 'Expected one bounded example-tool adaptation: ' + before)
        text = text.replace(before, after)
        substitutions.append(dict(before=before, after=after, count=count))
    ast.parse(text)
    target = EXAMPLES / (name + '-v3.py')
    retain(target, text.encode())
    transforms.append(dict(source=identity(source), target=identity(target), substitutions=substitutions))


def declared(path):
    direct = set()
    def walk(node, conditional=False):
        conditional = conditional or bool(node.attrib.get('Condition'))
        if node.tag.rsplit('}', 1)[-1] == 'ProjectReference':
            require(not conditional and set(node.attrib) == {'Include'} and not list(node), 'Adjudicate a conditional/metadata direct edge explicitly')
            value = node.attrib['Include'].replace('$(RepositoryRoot)', str(ROOT)).replace('\\', '/')
            require(not any(token in value for token in ('$(', '@(', '*', '?', ';')), 'Adjudicate computed project edges explicitly')
            direct.add((path.parent / value).resolve())
        for child in node:
            walk(child, conditional)
    walk(ET.parse(path).getroot())
    return direct


contexts = []
for name, path in (
    ('consumer', EXAMPLES / 'v2/consumer/src/Yubico.YubiKit.AuditPiv001.Examples.v2.consumer.csproj'),
    ('annotations', EXAMPLES / 'v2/annotations/src/Yubico.YubiKit.AuditPiv001.Examples.v2.annotations.csproj'),
    ('pivtool', ROOT / 'src/Piv/examples/PivTool/PivTool.csproj'),
):
    direct = declared(path)
    closure, pending = set(), list(direct)
    while pending:
        child = pending.pop()
        if child in closure:
            continue
        require(child.is_relative_to(ROOT / 'src') and child.is_file(), 'Use genuine source project graph nodes')
        closure.add(child)
        pending.extend(declared(child))
    contexts.append(dict(name=name, project=identity(path), direct=[str(p) for p in sorted(direct)],
                         closure=[str(p) for p in sorted(closure)], graph_projects=[identity(p) for p in sorted(closure)]))
require([(row['name'], len(row['direct'])) for row in contexts] == [('consumer', 5), ('annotations', 2), ('pivtool', 5)], 'Exact declared context metadata changed; adjudicate before capture')
work = Path('/tmp/opencode/piv-001-examples-v2-consumer')
workspace_paths = [work / name for name in ('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'policy-final.txt', 'csc-command-line.txt', 'imported-projects.txt', 'project-references.txt', 'resolved-project-references.txt')]
workspace_paths += [work / 'obj/project.assets.json']
tools = [identity(path) for path in (EXAMPLES / 'capture-v3.py', EXAMPLES / 'verify-v3.py', EXAMPLES / 'project_graph_v3.py', HERE / 'prepare.py', HERE / 'runtime.py', HERE / 'verify.py')]
record = dict(id='project-graph-1', baseline='727fe43afc430c5d8364d868a6436de50626e476',
              acceptance='Coordinator requested an additive evidence-tool correction after a zero-warning consumer build and the original direct-versus-late-transitive membership failure. Existing final examples, projects and source bodies remain accepted unchanged.',
              parent_extraction=identity(EXAMPLES / 'v2/manifest.json'), parent_source_context=identity(EXAMPLES / 'v2/source-context.json'),
              hardware_source=identity(HARDWARE),
              graph_reuse=dict(function='project_closure_proof', original_function_sha256=sha(ast.get_source_segment(original, function).encode()),
                               header=header, substitutions=operations, runtime=identity(HERE / 'runtime.py'), target=identity(EXAMPLES / 'project_graph_v3.py')),
              transformations=transforms, contexts=contexts, preserved=preserved,
              preparation_failures=[identity(HERE / 'preparation-first-failure.json')],
              consumer_workspace=[identity(path) for path in workspace_paths], tools=tools,
              policy='Original v2 manifest/tool/source/failure identities remain byte-identical. Additive graph-corrected capture/verify tools bind this explicit acceptance receipt; no new extraction, build, capture or hardware result is inferred. Actual late ProjectReference and resolved outputs match each full context closure while exact declared direct sets remain distinct. All original compiler/analyzer/source/NoSkip/WAE proofs remain active.')
retain(HERE / 'receipt.json', (json.dumps(record, indent=2) + '\n').encode())
counts = ', '.join(f"{row['name']} {len(row['direct'])}/{len(row['closure'])}" for row in contexts)
print('Prepared additive project-graph-1 tools from the working hardware closure algorithm; exact declared/recursive XML contexts: ' + counts + '. Original consumer compiler workspace and failed capture remain preserved; capture execution stays coordinator-owned.')
