"""Bounded final-document inventory; final reads require an explicit completed-editor checkpoint."""
from pathlib import Path
import hashlib
import json
import re
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent.parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def canonical(relative):
    path = Path(relative)
    require(not path.is_absolute() and '..' not in path.parts, f'Unsafe repository-relative path: {relative}')
    current = ROOT
    for part in path.parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked input: {current}')
    return current


def file_record(path):
    raw = path.read_bytes()
    return dict(path=str(path.relative_to(ROOT)), bytes=len(raw), sha256=sha(raw))


def accepted_scope():
    marker = HERE / 'v4/extraction-scope.json'
    require(marker.exists(), 'Await coordinator editor-complete notification and extraction-scope.json; current documents remain unread.')
    scope = json.loads(marker.read_text())
    require(scope['baseline'] == '727fe43afc430c5d8364d868a6436de50626e476' and scope['editor_complete'] is True and scope['reviewer_complete'] is True and scope['formatter_complete'] is True and bool(scope['notification']), 'Record affirmative coordinator editor-complete notification')
    report = canonical(scope['editor_report']['path'])
    require(report.is_relative_to(RUN) and sha(report.read_bytes()) == scope['editor_report']['sha256'], 'Completed editor-report identity changed')
    require(len(set(scope['edited_paths'])) == len(scope['edited_paths']), 'Duplicate edited paths')
    for relative in scope['shared_paths']:
        require((relative.startswith('docs/') or relative == 'README.md') and Path(relative).suffix in ('.md', '.yml'), 'Shared scope must name explicit documentation paths')
    for key in ('final_review', 'formatter_record'):
        row = scope[key]
        target = canonical(row['path'])
        require(target.is_relative_to(RUN) and sha(target.read_bytes()) == row['sha256'], 'Released record identity changed: ' + key)
    reviewed = json.loads(canonical(scope['final_review']['path']).read_text())
    require(reviewed['baseline'] == scope['baseline'], 'Final review baseline mismatch')
    final = {row['path']: row for row in reviewed['files']}
    require(len(final) == len(reviewed['files']) and bool(final), 'Require nonempty distinct final reviewed paths')
    for path in scope['edited_paths']:
        require(path in final and sha(canonical(path).read_bytes()) == final[path]['final_sha256'], 'Released final input changed: ' + path)
    return scope


def scope_for(path, headings):
    if path.startswith('src/Piv/'):
        return 'module'
    accepted = accepted_scope()
    if path in accepted.get('whole_shared_paths', []):
        return 'shared-piv'
    selectors = accepted.get('shared_sections', {}).get(path, [])
    if any(selector.casefold() in heading.casefold() for selector in selectors for heading in headings):
        return 'shared-piv'
    return 'outside-piv-section'


def markdown(path):
    text = canonical(path).read_bytes().decode('utf-8')
    lines = text.splitlines(keepends=True)
    fences, inline, headings = [], [], []
    opened = None
    csharp_index = 0
    for number, line in enumerate(lines, 1):
        marker = re.match(r'^\s{0,3}(`{3,}|~{3,})([^\r\n]*)', line)
        if opened is not None:
            if marker and marker[1][0] == opened['marker'][0] and len(marker[1]) >= len(opened['marker']) and not marker[2].strip():
                body = ''.join(lines[opened['line']:number - 1])
                fences.append(dict(path=path, index=len(fences) + 1, csharp_index=opened['csharp_index'],
                                   language=opened['language'], line=opened['line'], end_line=number,
                                   headings=opened['headings'], scope=opened['scope'], body=body,
                                   body_sha256=sha(body.encode('utf-8'))))
                opened = None
            continue
        if marker:
            language = marker[2].strip().lower()
            if language in ('csharp', 'cs', 'c#'):
                csharp_index += 1
                index = csharp_index
            else:
                index = None
            opened = dict(marker=marker[1], language=language, line=number, csharp_index=index,
                          headings=list(headings), scope=scope_for(path, headings))
            continue
        heading = re.match(r'^(#{1,6})\s+(.+?)\s*#*\s*$', line)
        if heading:
            depth = len(heading[1])
            headings = headings[:depth - 1] + [heading[2]]
        current_scope = scope_for(path, headings)
        if current_scope.startswith('outside-piv-'):
            continue
        for match in re.finditer(r'(?<!`)`([^`\r\n]+)`(?!`)', line):
            body = match[1]
            inline.append(dict(path=path, index=len(inline) + 1, line=number, scope=current_scope,
                               headings=list(headings), body=body, body_sha256=sha(body.encode('utf-8'))))
    require(opened is None, f'Unclosed fenced block: {path}')
    return fences, inline


def xml_inventory(paths):
    entries, documents = [], []
    for path in paths:
        source = canonical(path)
        raw = source.read_bytes()
        documents.append(file_record(source))
        text = raw.decode('utf-8')
        for chunk_index, match in enumerate(re.finditer(r'(?:^[ \t]*///[^\n]*(?:\n|$))+', text, re.M), 1):
            xml = re.sub(r'^[ \t]*/// ?', '', match[0], flags=re.M)
            try:
                tree = ET.fromstring('<documentation>' + xml + '</documentation>')
            except ET.ParseError as error:
                raise ValueError(f'Malformed XML documentation {path}, chunk {chunk_index}: {error}') from error
            counters = {}
            for node in tree.iter():
                if node.tag not in ('code', 'example', 'c'):
                    continue
                counters[node.tag] = counters.get(node.tag, 0) + 1
                body = ''.join(node.itertext())
                entries.append(dict(path=path, chunk=chunk_index, tag=node.tag, index=counters[node.tag],
                                    line=text[:match.start()].count('\n') + 1, body=body,
                                    body_sha256=sha(body.encode('utf-8')), attributes=dict(node.attrib)))
    return entries, documents


def source_paths(folder):
    return sorted(str(p.relative_to(ROOT)) for p in folder.rglob('*.cs')
                  if not any(part.lower() in ('bin', 'obj', 'generated', 'codebase_ast') for part in p.relative_to(folder).parts))


def scan():
    accepted = accepted_scope()
    edited = sorted(accepted['edited_paths'])
    module = ROOT / 'src/Piv'
    documents = sorted({str(p.relative_to(ROOT)) for p in module.rglob('*.md')
                        if not any(part.lower() in ('bin', 'obj', 'generated') for part in p.relative_to(module).parts)} | set(accepted['shared_paths']))
    fences, inline, mappings = [], [], []
    for path in documents:
        if path.endswith('.yml'):
            text = canonical(path).read_text()
            for match in re.finditer(r'^  - id: ([^\n]+)\n(.*?)(?=^  - id: |\Z)', text, re.M | re.S):
                if 'piv' in match[1].lower():
                    mappings.append(dict(path=path, id=match[1], body=match[0], body_sha256=sha(match[0].encode())))
        else:
            blocks, spans = markdown(path)
            fences.extend(blocks)
            inline.extend(spans)
    xml, xml_documents = xml_inventory(source_paths(module))
    tasks = []
    for item in accepted['task_units']:
        path = item['path']
        require(path.startswith('src/Piv/') or path in accepted['shared_paths'], 'Task unit escaped released scope')
        lines = canonical(path).read_text().splitlines(keepends=True)
        lo, hi = item['start_line'], item['end_line']
        require(1 <= lo <= hi <= len(lines) and bool(item['reason']), 'Require a bounded task unit and rationale')
        body = ''.join(lines[lo - 1:hi])
        tasks.append(dict(path=path, id=item['id'], body=body, body_sha256=sha(body.encode()), reason=item['reason']))
    require(len(tasks) == len({row['id'] for row in tasks}), 'Duplicate task unit identities')
    return dict(documents=[file_record(canonical(path)) for path in documents], fences=fences,
                inline=inline, xml=xml, xml_documents=xml_documents, mappings=mappings, tasks=tasks,
                edited_paths=edited, edited_xml_paths=[path for path in edited if path.endswith('.cs') and path.startswith('src/Piv/')])


def unit_id(kind, item):
    suffix = f"{item['chunk']}:{item['tag']}:{item['index']}" if kind == 'xml' else str(item.get('id', item.get('index')))
    return f"{kind}:{item['path']}:{suffix}"


def legacy_identities(inventory):
    """Detect complete body/addition/removal changes while allowing surrounding prose shifts."""
    return dict(documents=[row['path'] for row in inventory['documents']], edited_paths=inventory['edited_paths'],
                fences=[{key: row[key] for key in ('path', 'index', 'csharp_index', 'language', 'scope', 'body', 'body_sha256')} for row in inventory['fences']],
                inline=[{key: row[key] for key in ('path', 'index', 'scope', 'body', 'body_sha256')} for row in inventory['inline'] if '(' in row['body'] or '=' in row['body'] or ';' in row['body']],
                xml=[{key: row[key] for key in ('path', 'chunk', 'tag', 'index', 'body', 'body_sha256', 'attributes')} for row in inventory['xml']],
                mappings=[dict(path=row['path'], id=row['id'], signatures=re.findall(r'^    v[12]: .+$', row['body'], re.M)) for row in inventory['mappings']])


def identities(inventory):
    """Bind every complete current body, source/document identity and task unit."""
    return inventory
