"""Retain CTAP documentation compile inputs; no .NET, restore, test or device execution.

Classification is explicit in selection.json. Refuse changed block anchors/counts/hashes.
Default roots are PRIMARY; --guide-root permits the pending F checkout for preparation.
Use --check for a read-only rerun, --patch to emit additions/updates for the patch tool,
or --materialize to generate only the explicitly named run-local extracted artifacts.
Neither mode prepares SDK hosts. Existing generated outputs must be reviewed before patching.
"""

import argparse
import hashlib
import json
from pathlib import Path
import re
import runpy
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
PRIMARY = next(p for p in HERE.parents if (p / 'toolchain.cs').is_file())
RUN = HERE.parent.parent
BASELINE = '5b09a9b6942ded3913b5c9340c3e126617e61a82'


def digest(data):
    return hashlib.sha256(data.encode() if isinstance(data, str) else data).hexdigest()


def fences(root, relative):
    data = (root / relative).read_bytes()
    text = data.decode().replace('\r\n', '\n')
    lines = text.splitlines(keepends=True)
    result, opened, heading = [], None, ''
    for number, line in enumerate(lines, 1):
        if opened is None and line.startswith('#'):
            heading = line.strip()
        marker = re.match(r'^(`{3,}|~{3,})([^\n]*)\n?$', line)
        if not marker:
            continue
        if opened is None:
            opened = (number, marker[1], marker[2].strip(), heading)
        elif marker[1][0] == opened[1][0] and len(marker[1]) >= len(opened[1]) and not marker[2].strip():
            start, _, language, title = opened
            body = ''.join(lines[start:number - 1])
            result.append(dict(id=f'{relative}#f{len(result)+1}', path=relative,
                               kind='fence', ordinal=len(result)+1, language=language,
                               heading=title, opening_line=start, closing_line=number,
                               extracted_text=body, extracted_sha256=digest(body),
                               verbatim_text=''.join(lines[start-1:number]),
                               document_sha256=digest(data), origin_root=str(root)))
            opened = None
    if opened is not None:
        raise ValueError(f'Unclosed fence: {relative}')
    return result


def xml_blocks(root, relative):
    data = (root / relative).read_bytes()
    text = data.decode().replace('\r\n', '\n')
    lines = text.splitlines(keepends=True)
    docs, current = [], []
    for number, line in enumerate(lines, 1):
        match = re.match(r'^\s*/// ?(.*?)(\n?)$', line)
        if match:
            current.append((number, match[1] + match[2], line))
        elif current:
            docs.append(current)
            current = []
    if current:
        docs.append(current)
    result = []
    for doc in docs:
        # XML well-formedness is checked independently of code compilation.
        body = ''.join(item[1] for item in doc)
        ET.fromstring('<doc>' + body + '</doc>')
        for match in re.finditer(r'<code(?:\s[^>]*)?>(.*?)</code>', body, re.S):
            raw = match[1]
            # Current XML contains no escaped C# operators; fail rather than silently rewrite them.
            if '&' in raw:
                raise ValueError(f'XML entity transformation needs review: {relative}')
            code = raw[1:] if raw.startswith('\n') else raw
            if code.endswith('\n'):
                code = code[:-1]
            code += '\n'
            start = doc[0][0] + body[:match.start()].count('\n')
            end = doc[0][0] + body[:match.end()].count('\n')
            result.append(dict(id=f'{relative}#x{len(result)+1}', path=relative, kind='xml-code',
                               ordinal=len(result)+1, language='xml-code', heading='XML <code>',
                               opening_line=start, closing_line=end, extracted_text=code,
                               extracted_sha256=digest(code), verbatim_text=''.join(lines[start-1:end]),
                               document_sha256=digest(data), origin_root=str(root),
                               extraction_transformations=['Remove /// prefix and XML code delimiter only; preserve code indentation/text.']))
    return result


def relocate_imports(text):
    """Move only the initial directive prefix; never regex-rewrite a body/literal."""
    lines = text.splitlines(keepends=True)
    index = 0
    while index < len(lines) and (lines[index].startswith('using ') and lines[index].rstrip().endswith(';') or not lines[index].strip()):
        index += 1
    return ''.join(lines[:index]), ''.join(lines[index:])


def wrapper(group, blocks):
    imports, bodies = [], []
    for key in group['blocks']:
        imported, body = relocate_imports(blocks[key]['extracted_text'])
        imports.append(imported)
        bodies.append(body)
    # Only initial imports are relocated. All statements/attributes/comments remain in order.
    preamble = ''.join(dict.fromkeys(imports))
    existing_imports = set(preamble.splitlines())
    preamble += ''.join(f'using {name};\n' for name in group['imports'] if f'using {name};' not in existing_imports)
    body = '\n'.join(bodies)
    shape = group['shape']
    if shape == 'top-level':
        return preamble + '\n' + body
    namespace = f'namespace AuditCt001.Prepared.{group["name"].replace("-", "_")};\n\n'
    if shape == 'class':
        return preamble + '\n' + namespace + body
    declaration = 'public sealed class Example' if shape in {'test-method', 'class-members'} else 'public static class Example'
    if group.get('implements'):
        declaration += ' : ' + group['implements']
    if shape in {'async-method', 'sync-method'}:
        signature = 'public static async Task ExampleAsync' if shape == 'async-method' else 'public static void Run'
        body = f'{signature}({group["parameters"]})\n{{\n' + body + '}\n'
    elif shape not in {'test-method', 'class-members'}:
        raise ValueError(f'Unapproved wrapper shape: {shape}')
    source = preamble + '\n' + namespace + declaration + '\n{\n' + body + '}\n'
    for key in group.get('class_blocks', []):
        source += '\n' + blocks[key]['extracted_text']
    return source


def generate(root, guide):
    selection = runpy.run_path(str(HERE / 'selection.py'))['selection']()
    blocks, documents = {}, []
    for document in selection['documents']:
        origin = guide if document['origin'] == 'guide' else root
        relative = document['path']
        found = fences(origin, relative) if document['kind'] == 'markdown' else xml_blocks(origin, relative)
        if len(found) != len(document['blocks']):
            raise ValueError(f'Block count changed: {relative}: {len(found)} != {len(document["blocks"])}')
        for block, expected in zip(found, document['blocks'], strict=True):
            if block['extracted_sha256'] != expected['sha256']:
                raise ValueError(f'Block anchor/hash changed: {block["id"]}; review/reclassify before extracting')
            block['classification'] = expected['classification']
            block['reason'] = expected['reason']
            block['verbatim_sha256'] = digest(block['verbatim_text'])
            blocks[block['id']] = block
        documents.append(dict(path=relative, origin_root=str(origin), sha256=digest((origin / relative).read_bytes()),
                              block_count=len(found), scope=document['scope']))
    # Module universe must be exhaustive, not just the hand-picked document list.
    module_docs = {str(p.relative_to(guide)) for p in (guide / 'src/Ctap2').rglob('*.md')
                   if not {'bin', 'obj', 'generated'} & set(p.parts)}
    selected_module_docs = {d['path'] for d in selection['documents'] if d['path'].startswith('src/Ctap2/') and d['kind'] == 'markdown'}
    if module_docs != selected_module_docs:
        raise ValueError(f'Module Markdown universe changed: {module_docs ^ selected_module_docs}')
    current_xml = []
    for p in sorted((root / 'src/Ctap2').rglob('*.cs')):
        if not {'bin', 'obj', 'generated'} & set(p.parts):
            current_xml.extend(xml_blocks(root, str(p.relative_to(root))))
    expected_xml = {key for key in blocks if '#x' in key}
    if {b['id'] for b in current_xml} != expected_xml or len(current_xml) != 5:
        raise ValueError('Module XML universe changed; explicit five-block classification required')
    module_fences = [b for b in blocks.values() if b['kind'] == 'fence' and b['path'].startswith('src/Ctap2/')]
    if len(module_fences) != 39:
        raise ValueError('Module fence count changed; expected 39')
    output, groups, membership = {}, [], {}
    for spec in selection['groups']:
        keys = spec['blocks'] + spec.get('class_blocks', [])
        for key in keys:
            if blocks[key]['classification'] != 'compile':
                raise ValueError(f'Excluded block routed to compilation: {key}')
            membership.setdefault(key, []).append(spec['name'])
        complete = wrapper(spec, blocks)
        raw = ''.join(f'// SOURCE {key}\n' + blocks[key]['extracted_text'] + '\n' for key in keys)
        name = spec['name']
        output[f'{name}/complete.cs'] = complete
        output[f'{name}/readable.raw'] = raw
        extraction = blocks[keys[0]]
        combined = '\n'.join(blocks[key]['extracted_text'] for key in keys)
        groups.append(dict(name=name, context=spec['context'], source=f'{name}/complete.cs',
                           source_sha256=digest(complete), raw_source=f'{name}/readable.raw', raw_sha256=digest(raw),
                           extracted_text=combined, extracted_sha256=digest(combined),
                           extraction={key: extraction[key] for key in ('path', 'document_sha256', 'ordinal', 'opening_line', 'closing_line')},
                           extractions=[{field: blocks[key][field] for field in ('id','path','document_sha256','ordinal','opening_line','closing_line','origin_root','extracted_sha256','verbatim_sha256')} for key in keys],
                           provenance=[f'{key}: {blocks[key]["reason"]}' for key in keys],
                           prerequisites=spec['prerequisites'], modules=spec['modules'], references=spec['references'],
                           shape=spec['shape'], parameters=spec.get('parameters', ''), imports=spec['imports'], implements=spec.get('implements'),
                           transformations=['Relocate only initial using directives to file prefix (initial blank lines preserved).',
                                            'Concatenate complete selected blocks in explicit order; do not flatten alternatives.',
                                            'Add only listed typed method parameters/imports/namespace/class/method boundaries.'],
                           source_api_evidence=spec['source_api_evidence']))
    compiled = {key for key, block in blocks.items() if block['classification'] == 'compile'}
    if compiled != set(membership):
        raise ValueError(f'Compile partition not exhaustive: {compiled ^ set(membership)}')
    for key, names in membership.items():
        blocks[key]['groups'] = names
    exclusions = [b for b in blocks.values() if b['classification'] != 'compile']
    reference_selection = list(selection['reference_inventory'])
    # Every active path in F's external discovery table is a hash-only dependency,
    # not an assertion of complete semantic coverage of its unrelated module.
    evidence = (RUN / 'evidence/f/external-paths.md').read_text()
    seen = {item['path'] for item in reference_selection}
    for line in evidence.splitlines():
        match = re.match(r'^\| ([^|]+?) \| (.+) \|$', line)
        if match and match[1] not in {'Exact path'} and match[1] not in seen:
            relative = match[1]
            if (root / relative).is_file():
                reference_selection.append(dict(path=relative, origin='primary', disposition='F discovery dependency only: ' + match[2]))
                seen.add(relative)
    references = []
    for item in reference_selection:
        origin = guide if item['origin'] == 'guide' else root
        p = origin / item['path']
        references.append(dict(**item, root=str(origin), sha256=digest(p.read_bytes())))
    api_paths = {p for spec in selection['groups'] for p in spec['source_api_evidence']}
    source_evidence = [dict(path=p, sha256=digest((root / p).read_bytes())) for p in sorted(api_paths)]
    config = json.loads((RUN / 'validation/checker-source/context/contexts-manifest.json').read_text())
    names = {g['name'] for g in groups}
    assigned = [n for c in config['contexts'] for n in c['groups']]
    if len(assigned) != len(set(assigned)) or set(assigned) != names or any(g['context'] != c['id'] for c in config['contexts'] for g in groups if g['name'] in c['groups']):
        raise ValueError('SDK context routing must be explicit, disjoint and exhaustive')
    provisional_dlls = set()
    for context in config['contexts']:
        provisional_dlls.update(context['references'])
        provisional_dlls.update(f'src/{m}/src/bin/Release/net10.0/Yubico.YubiKit.{m}.dll' for m in ['Core', 'Ctap2'] + context['modules'])
    dll_inventory = [dict(path=p, exists=(root/p).is_file(), sha256=digest((root/p).read_bytes()) if (root/p).is_file() else None,
                          status='PROVISIONAL ONLY: freshness not established; coordinator must rebuild before context preparation') for p in sorted(provisional_dlls)]
    external_references = []
    for document in documents:
        text = (Path(document['origin_root']) / document['path']).read_text()
        urls = sorted(set(re.findall(r'https?://[^\s<>\]"\)]+', text)))
        external_references.extend(dict(document=document['path'],document_sha256=document['sha256'],url=url,
                                        status='Current literal reference inventory only; no HTTP revalidation/remote-content hash claimed') for url in urls)
    manifest = dict(schema_version=1, baseline=BASELINE, primary_root=str(root), guide_root=str(guide),
                    status=('PREPARATION ONLY; integrated PRIMARY inputs; final boundary review/fresh SDK builds pending; no compile verdict'
                            if root == guide else 'PREPARATION ONLY; F review/integration and fresh SDK builds pending; no compile verdict'),
                    extractor_sha256=digest(Path(__file__).read_bytes()), selection_sha256=digest((HERE/'selection.py').read_bytes()),
                    context_configuration_sha256=digest((RUN/'validation/checker-source/context/contexts-manifest.json').read_bytes()),
                    counts=dict(module_fences=39, module_xml=5, total_blocks=len(blocks), selected_blocks=len(compiled),
                                excluded_blocks=len(exclusions), groups=len(groups)),
                    documents=documents, blocks=list(blocks.values()), groups=groups,
                    exclusions=[{key: b[key] for key in ('id','path','ordinal','classification','reason','extracted_sha256')} for b in exclusions],
                    reference_inventory=references, external_references=external_references,
                    source_api_evidence=source_evidence, provisional_dll_inventory=dll_inventory,
                    strict_raw=dict(status='not-applicable', groups=[], reason='All groups use explicitly reviewed real SDK/MSBuild contexts from the start; optional raw consumer check not selected.'))
    output['manifest.json'] = json.dumps(manifest, indent=2) + '\n'
    output['strict-raw-not-applicable.json'] = json.dumps(manifest['strict_raw'], indent=2) + '\n'
    return output, manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=PRIMARY)
    parser.add_argument('--guide-root', type=Path, default=PRIMARY)
    modes = parser.add_mutually_exclusive_group(required=True)
    modes.add_argument('--patch', action='store_true')
    modes.add_argument('--check', action='store_true')
    modes.add_argument('--materialize', action='store_true')
    args = parser.parse_args()
    output, manifest = generate(args.root.resolve(strict=True), args.guide_root.resolve(strict=True))
    if args.materialize:
        previous_path = HERE / 'manifest.json'
        previous = json.loads(previous_path.read_text()) if previous_path.is_file() else None
        # Never overwrite unfamiliar/user-modified artifacts. Only prior outputs whose
        # retained hash still matches may be refreshed after input integration.
        expected = {}
        if previous:
            for group in previous['groups']:
                expected[group['source']] = group['source_sha256']
                expected[group['raw_source']] = group['raw_sha256']
        for relative, text in output.items():
            path = HERE / relative
            if path.exists() and path.read_bytes() != text.encode() and relative not in {'manifest.json','strict-raw-not-applicable.json'}:
                if relative not in expected or digest(path.read_bytes()) != expected[relative]:
                    raise ValueError(f'Refuse unfamiliar/edited output: {relative}')
        for relative, text in output.items():
            path = HERE / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(text.encode())
        print(json.dumps(dict(status='generated compile INPUTS only; NOT compiled', **manifest['counts']), indent=2))
        return
    if args.check:
        stale = [relative for relative, text in output.items() if not (HERE/relative).is_file() or (HERE/relative).read_bytes() != text.encode()]
        if stale:
            raise ValueError(f'Missing/stale retained extraction: {stale}')
        print(json.dumps(dict(status='static-inputs-match; NOT compiled', **manifest['counts']), indent=2))
        return
    print('*** Begin Patch')
    for relative, text in output.items():
        path = HERE / relative
        if path.exists() and path.read_bytes() == text.encode():
            continue
        if path.exists():
            print(f'*** Update File: {path}')
            print('@@')
            for line in path.read_text().splitlines():
                print('-' + line)
        else:
            print(f'*** Add File: {path}')
        for line in text.splitlines():
            print('+' + line)
    print('*** End Patch')


if __name__ == '__main__':
    main()
