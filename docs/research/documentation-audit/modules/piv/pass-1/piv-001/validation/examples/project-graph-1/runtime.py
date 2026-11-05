"""Runtime adapter appended to the source-pinned hardware project-closure proof."""


def identity(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=sha(raw))


def lines(path):
    return [line.strip() for line in path.read_text(encoding='utf-8-sig').splitlines() if line.strip()]


def acceptance_receipt():
    path = HERE / 'project-graph-1/receipt.json'
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
                project_graph_receipt_sha256=identity(HERE / 'project-graph-1/receipt.json')['sha256'],
                resolved_project_items=(evidence / 'resolved-project-references.txt').read_text(),
                host_version=policy['Version'], host_assembly_version=policy['AssemblyVersion'],
                sdk_package_version=policy['YubiKitVersion'],
                rationale='Declared references match the exact accepted direct set; recursive XML and restored target nodes establish the late transitive item set and each actual obj/bin DLL binding. Host and SDK versions remain independent recorded properties.')


def verify_project_proof(proof, evidence, live):
    record = acceptance_receipt()
    require(proof['project_graph_receipt_sha256'] == identity(HERE / 'project-graph-1/receipt.json')['sha256'], 'Capture binds another graph correction acceptance')
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
        require(current == graph, 'Live project/restore/Csc/bin graph identities changed')
        for row in proof['sdk_csc_references']:
            require(identity(Path(row['path'])) == row, 'Live actual SDK Csc reference changed')
