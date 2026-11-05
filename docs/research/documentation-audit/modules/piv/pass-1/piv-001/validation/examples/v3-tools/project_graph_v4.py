"""Versioned actual project closure and independent host/SDK-version proof."""
from pathlib import Path
import json
import xml.etree.ElementTree as ET
from inventory import HERE, ROOT, sha, require

def project_closure_proof(project, direct, references, policy, evidence, expected):
    assets_path = Path(policy['ProjectAssetsFile'])
    assets = json.loads(assets_path.read_text())
    require(Path(assets['project']['restore']['projectPath']).resolve() == project, 'Restored graph belongs to another host')
    restored_direct = {Path(p).resolve() for p in
        assets['project']['restore']['frameworks']['net10.0']['projectReferences']}
    require(restored_direct == direct, 'Restored direct project graph differs from the exact declared context set')
    libraries = {key: value for key, value in assets['libraries'].items() if value['type'] == 'project'}
    target = assets['targets']['net10.0']
    by_name, nodes = {}, {}
    for key, library in libraries.items():
        path = (project.parent / library['msbuildProject'].replace('\\', '/')).resolve()
        require(path.is_file() and path.is_relative_to(ROOT / 'src'), 'Graph project must be a real repository project')
        require(path not in nodes, 'Duplicate restored project identity')
        require(target[key]['type'] == 'project' and target[key]['framework'] == '.NETCoreApp,Version=v10.0', 'Graph target framework changed')
        nodes[path] = dict(project_id=key, target=target[key], library=library)
        name = key.rsplit('/', 1)[0]
        require(name not in by_name, 'Ambiguous restored project name')
        by_name[name] = path

    def xml_references(path):
        literal, deferred = set(), []
        def walk(node, conditions=()):
            conditions += (node.attrib['Condition'],) if node.attrib.get('Condition') else ()
            if node.tag.rsplit('}', 1)[-1] == 'ProjectReference':
                include = node.attrib.get('Include', '')
                value = include.replace('$(RepositoryRoot)', str(ROOT)).replace('\\', '/')
                if conditions or set(node.attrib) != {'Include'} or list(node) or any(t in value for t in ('$(', '@(', '*', '?', ';')):
                    deferred.append(dict(include=include, conditions=list(conditions), authority='restored net10.0 target graph'))
                else:
                    require(bool(value), 'ProjectReference Include missing')
                    literal.add((path.parent / value).resolve())
            for child in node:
                walk(child, conditions)
        walk(ET.parse(path).getroot())
        return literal, deferred

    graph, closure, pending = [], set(), list(direct)
    while pending:
        path = pending.pop()
        if path in closure:
            continue
        require(path in nodes, 'XML dependency missing from restored project graph')
        evaluated = {by_name[name] for name in nodes[path]['target'].get('dependencies', {}) if name in by_name}
        literal, deferred = xml_references(path)
        require(literal <= evaluated, 'Unconditional XML edge missing from restored target graph: ' + str(path))
        require(deferred or literal == evaluated, 'Unconditional XML project edges differ from restored target graph: ' + str(path))
        selected = evaluated if deferred else literal
        graph.append(dict(project=identity(path), project_id=nodes[path]['project_id'],
            dependencies=[str(p) for p in sorted(selected)], xml_literal_edges=[str(p) for p in sorted(literal)],
            deferred_xml_items=deferred, edge_authority='restored target graph' if deferred else 'recursive XML and restored target graph agree'))
        closure.add(path)
        pending.extend(selected)
    require(closure == expected == set(nodes), 'Require the exact accepted context-specific transitive closure')
    actual = {Path(p).resolve() for p in lines(evidence / 'project-references.txt')}
    require(actual == closure, 'Actual late ProjectReference item set differs from exact graph closure')
    resolved = {}
    for line in lines(evidence / 'resolved-project-references.txt'):
        fields = line.split('|')
        require(len(fields) in (2, 3) and bool(fields[1]), 'Require resolved output and MSBuildSourceProjectFile columns')
        output, source = fields[:2]
        path = Path(source).resolve()
        require(path not in resolved, 'Duplicate resolved project output')
        resolved[path] = Path(output).resolve()
    require(set(resolved) == closure, 'Resolved project identities differ from graph closure')
    bindings = []
    for path in sorted(closure):
        compile_names = [Path(p).name for p in nodes[path]['target'].get('compile', {}) if p.endswith('.dll')]
        require(len(compile_names) == 1, 'Restored project compile identity must select one DLL')
        name = compile_names[0]
        matches = [Path(p) for p in references if Path(p).name == name]
        require(len(matches) == 1 and matches[0].is_relative_to(path.parent / 'obj'), 'Csc project DLL must bind its own obj/project identity: ' + str(path))
        require(resolved[path].name == name and resolved[path].is_relative_to(path.parent / 'bin'), 'Resolved project output identity mismatch')
        bindings.append(dict(project=identity(path), project_id=nodes[path]['project_id'],
            actual_csc_reference=identity(matches[0]), resolved_output=identity(resolved[path])))
    sdk = Path('/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.403/Sdks/Microsoft.NET.Sdk/targets/Microsoft.PackageDependencyResolution.targets')
    require('<ProjectReference Include="@(_TransitiveProjectReferences)" />' in sdk.read_text(), 'Pinned SDK transitive-reference inclusion changed')
    return closure, dict(direct=[str(p) for p in sorted(direct)], closure=[str(p) for p in sorted(closure)],
        graph=graph, bindings=bindings, restored_assets=identity(assets_path), sdk_transitive_reference_source=identity(sdk))

"""Runtime adapter appended to the source-pinned hardware project-closure proof."""


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=sha(raw))


def lines(path):
    return [line.strip() for line in path.read_text(encoding='utf-8-sig').splitlines() if line.strip()]


def acceptance_receipt():
    path = HERE / 'v3/project-graph-acceptance.json'
    record = json.loads(path.read_text())
    require(record['baseline'] == '727fe43afc430c5d8364d868a6436de50626e476', 'Project graph correction baseline changed')
    for row in record['preserved'] + record['tools'] + [record['hardware_source']]:
        require(identity(Path(row['path'])) == row, 'Preserved/versioned source identity changed: ' + row['path'])
    # These are original consumer compiler text inputs, rather than a new capture result.
    for row in record['consumer_workspace']:
        require(identity(Path(row['path'])) == row, 'Original consumer compiler workspace changed: ' + row['path'])
    return record


def context_for(project, record):
    matches = [row for row in record['contexts'] if Path(row['project']['path']) == project.resolve()]
    require(len(matches) == 1, 'Require an exact accepted consumer/annotation/actual-shipping project')
    context = matches[0]
    require(identity(project) == context['project'], 'Accepted context project metadata changed')
    for row in context['graph_projects']:
        require(identity(Path(row['path'])) == row, 'Accepted recursive graph project metadata changed')
    return context


def project_reference_proof(project, policy, references, evidence):
    record = acceptance_receipt()
    context = context_for(project, record)

    def resolve(value):
        value = value.replace('$(RepositoryRoot)', str(ROOT)).replace('\\', '/')
        require('$(' not in value, 'Resolve additional direct-project expressions explicitly')
        return (project.parent / value).resolve()

    declared = {resolve(node.attrib['Include']) for node in ET.parse(project).iter('ProjectReference')}
    expected_direct = {Path(value) for value in context['direct']}
    expected_closure = {Path(value) for value in context['closure']}
    require(bool(declared) and declared == expected_direct, 'Exact context-specific declared project set changed')
    closure, graph = project_closure_proof(project, declared, references, policy, evidence, expected_closure)
    require(closure == expected_closure, 'Context closure changed')
    snapshots = []
    for source in [Path(row['project']['path']) for row in graph['graph']] + [Path(graph['sdk_transitive_reference_source']['path'])]:
        row = identity(source)
        relative = 'project-graph/' + row['sha256'] + '-' + source.name + '.snapshot'
        target = evidence / relative
        require(not source.is_symlink() and not target.exists(), 'Preserve unique actual project graph text inputs')
        target.parent.mkdir(parents=True, exist_ok=True)
        with target.open('xb') as output:
            output.write(source.read_bytes())
        snapshots.append(dict(path=relative, bytes=row['bytes'], sha256=row['sha256']))
    require(sha((evidence / 'project.assets.json.snapshot').read_bytes()) == graph['restored_assets']['sha256'], 'Graph proof binds another retained restore asset set')
    direct_projects = [dict(project=row['project'], assembly=Path(row['actual_csc_reference']['path']).stem,
                            csc_reference=row['actual_csc_reference'])
                       for row in graph['bindings'] if Path(row['project']['path']) in declared]
    sdk = [identity(Path(value)) for value in sorted(references) if Path(value).name.startswith('Yubico.YubiKit.')]
    require({'Yubico.YubiKit.Piv.dll', 'Yubico.YubiKit.Core.dll'} <= {Path(row['path']).name for row in sdk}, 'Actual Csc requires PIV and Core references')
    versions = [node.text for node in ET.parse(ROOT / 'Directory.Packages.props').iter('YubiKitVersion')]
    require(versions == [policy['YubiKitVersion']], 'Recorded SDK package version differs from canonical metadata')
    require(policy['TargetFramework'] == 'net10.0' and policy['Configuration'] == 'Release' and policy['LangVersion'] in ('14', '14.0'), 'Actual SDK compilation configuration changed')
    return dict(context=context['name'], project=identity(project), direct_projects=direct_projects,
                project_graph=graph, sdk_csc_references=sdk, retained=snapshots,
                project_graph_receipt_sha256=identity(HERE / 'v3/project-graph-acceptance.json')['sha256'],
                resolved_project_items=(evidence / 'resolved-project-references.txt').read_text(),
                host_version=policy['Version'], host_assembly_version=policy['AssemblyVersion'],
                sdk_package_version=policy['YubiKitVersion'],
                rationale='Declared references match the exact accepted direct set; recursive XML and restored target nodes establish the late transitive item set and each actual obj/bin DLL binding. Host and SDK versions remain independent recorded properties.')


def canonical_graph_nodes(value):
    """Return a shallow comparison copy with unique graph nodes ordered by project path."""
    nodes = value['graph']
    paths = [node['project']['path'] for node in nodes]
    project_ids = [node['project_id'] for node in nodes]
    require(len(paths) == len(set(paths)) and len(project_ids) == len(set(project_ids)),
            'Graph comparison requires unique project paths and project IDs')
    return dict(value, graph=sorted(nodes, key=lambda node: node['project']['path']))


def verify_project_proof(proof, evidence, live):
    record = acceptance_receipt()
    require(proof['project_graph_receipt_sha256'] == identity(HERE / 'v3/project-graph-acceptance.json')['sha256'], 'Capture binds another graph correction acceptance')
    project = Path(proof['project']['path'])
    context = context_for(project, record)
    require(proof['context'] == context['name'], 'Captured graph context changed')
    graph = proof['project_graph']
    require(set(graph['direct']) == set(context['direct']) and set(graph['closure']) == set(context['closure']), 'Accepted direct/closure identity changed')
    require({row['project']['path'] for row in graph['graph']} == set(context['closure']) == {row['project']['path'] for row in graph['bindings']}, 'Graph nodes/bindings require the complete exact closure')
    for row in proof['retained']:
        raw = (evidence / row['path']).read_bytes()
        require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], 'Retained graph metadata changed')
    captured = {row['sha256'] for row in proof['retained']}
    require({row['project']['sha256'] for row in graph['graph']} | {graph['sdk_transitive_reference_source']['sha256']} <= captured, 'Complete graph/SDK source snapshots required')
    require(sha((evidence / 'project.assets.json.snapshot').read_bytes()) == graph['restored_assets']['sha256'], 'Retained graph assets changed')
    require((evidence / 'resolved-project-references.txt').read_text() == proof['resolved_project_items'], 'Resolved project items changed')
    for row in graph['bindings']:
        source = Path(row['project']['path'])
        reference, output = Path(row['actual_csc_reference']['path']), Path(row['resolved_output']['path'])
        require(reference.is_relative_to(source.parent / 'obj') and output.is_relative_to(source.parent / 'bin') and reference.name == output.name, 'Captured project output/reference must match its own obj/bin identity')
    if live:
        policy = dict(line.split('=', 1) for line in lines(evidence / 'policy-final.txt'))
        references = {str(Path(line.split('|', 1)[0]).resolve()) for line in lines(evidence / 'resolved-references.txt')}
        closure, current = project_closure_proof(project, {Path(value) for value in context['direct']}, references, policy, evidence, {Path(value) for value in context['closure']})
        require(canonical_graph_nodes(current) == canonical_graph_nodes(graph), 'Live project/restore/Csc/bin graph identities changed')
        for row in proof['sdk_csc_references']:
            require(identity(Path(row['path'])) == row, 'Live actual SDK Csc reference changed')
