"""Bounded documentation inventory, shared by preparation and final identity checks."""
from pathlib import Path
import hashlib
import re
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'toolchain.cs').exists())
RUN = HERE.parent.parent
SHARED = ['docs/usage/user-interaction.md', 'docs/migration/v1-to-v2.md',
          'docs/migration/v1-to-v2-gaps.md', 'docs/migration/v1-to-v2-map.yml',
          'docs/architecture/device-discovery-guarantees.md']

def sha(data):
    return hashlib.sha256(data).hexdigest()

def file_record(path):
    raw = path.read_bytes()
    return dict(path=str(path.relative_to(ROOT)), bytes=len(raw), sha256=sha(raw))

def edited_paths():
    text = (RUN / 'edit-report.md').read_text()
    return sorted(set(re.findall(r'^\| `([^`]+)` \| \d+ \| `[a-f0-9]{64}` \|$', text, re.M)))

def scope(path, headings):
    if path.startswith('src/YubiOtp/'):
        return 'module'
    if path == SHARED[0]:
        return 'shared-interaction'
    if path == SHARED[1]:
        return 'otp-migration' if any(h.startswith('YubiOTP:') for h in headings) else 'outside-otp-migration'
    if path == SHARED[2]:
        return 'otp-gaps' if any(h == 'Module: YubiOTP' for h in headings) else 'outside-otp-gaps'
    return 'shared-record-metadata'

def markdown(path):
    text = (ROOT / path).read_bytes().decode('utf-8')
    lines = text.splitlines(keepends=True)
    fences, inline = [], []
    headings = []
    opened = None
    csharp_index = 0
    for number, line in enumerate(lines, 1):
        marker = re.match(r'^\s{0,3}(`{3,}|~{3,})([^\r\n]*)', line)
        if opened is not None:
            if marker and marker[1][0] == opened['marker'][0] and len(marker[1]) >= len(opened['marker']) and not marker[2].strip():
                body = ''.join(lines[opened['line']:number-1])
                entry = dict(path=path, index=len(fences)+1, csharp_index=opened['csharp_index'],
                             language=opened['language'], line=opened['line'], end_line=number,
                             headings=opened['headings'], scope=opened['scope'], body=body,
                             body_sha256=sha(body.encode('utf-8')))
                fences.append(entry)
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
                          headings=list(headings), scope=scope(path, headings))
            continue
        heading = re.match(r'^(#{1,6})\s+(.+?)\s*#*\s*$', line)
        if heading:
            depth = len(heading[1])
            headings = headings[:depth-1] + [heading[2]]
        # Shared migration/gaps text outside the assigned OTP section is not
        # expanded into a new module/example audit. Fence identity still checked.
        current_scope = scope(path, headings)
        if current_scope.startswith('outside-otp-'):
            continue
        for match in re.finditer(r'(?<!`)`([^`\r\n]+)`(?!`)', line):
            body = match[1]
            inline.append(dict(path=path, index=len(inline)+1, line=number,
                               scope=current_scope, headings=list(headings), body=body,
                               body_sha256=sha(body.encode('utf-8'))))
    if opened is not None:
        raise ValueError(f'Unclosed fenced block: {path}:{opened["line"]}')
    return fences, inline

def xml_inventory(paths):
    entries, documents = [], []
    for path in paths:
        raw = (ROOT / path).read_bytes()
        documents.append(file_record(ROOT / path))
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
                                    line=text[:match.start()].count('\n')+1, body=body,
                                    body_sha256=sha(body.encode('utf-8')), attributes=dict(node.attrib)))
    return entries, documents

def scan():
    edited = edited_paths()
    if len(edited) != 50:
        raise ValueError(f'Reconcile edited-path inventory, expected 50, found {len(edited)}')
    documents = sorted(set(str(p.relative_to(ROOT)) for p in (ROOT / 'src/YubiOtp').rglob('*.md')) | set(SHARED))
    fences, inline = [], []
    for path in documents:
        blocks, spans = markdown(path)
        fences.extend(blocks)
        inline.extend(spans)
    source_paths = sorted(str(p.relative_to(ROOT)) for p in (ROOT / 'src/YubiOtp').rglob('*.cs')
                          if not any(part in ('bin','obj','generated') for part in p.relative_to(ROOT / 'src/YubiOtp').parts))
    xml, xml_documents = xml_inventory(source_paths)
    # YAML mappings are symbolic before/after API designators, not executable
    # fenced examples. Do not parse the unrelated pre-existing YAML syntax defect.
    mappings = []
    text = (ROOT / SHARED[3]).read_text()
    for match in re.finditer(r'^  - id: ([^\n]+)\n(.*?)(?=^  - id: |\Z)', text, re.M | re.S):
        if 'yubiotp' in match[1].lower():
            mappings.append(dict(path=SHARED[3], id=match[1], body=match[0], body_sha256=sha(match[0].encode())))
    return dict(documents=[file_record(ROOT / path) for path in documents], fences=fences,
                inline=inline, xml=xml, xml_documents=xml_documents, mappings=mappings,
                edited_paths=edited, edited_xml_paths=[p for p in edited if p.endswith('.cs')])

def identities(inventory):
    """Bodies/additions matter; line shifts and surrounding prose do not."""
    return dict(
        documents=[r['path'] for r in inventory['documents']],
        edited_paths=inventory['edited_paths'],
        fences=[{k:r[k] for k in ('path','index','csharp_index','language','scope','body','body_sha256')} for r in inventory['fences']],
        inline=[{k:r[k] for k in ('path','scope','body','body_sha256')} for r in inventory['inline'] if '(' in r['body'] or '=' in r['body'] or ';' in r['body']],
        xml=[{k:r[k] for k in ('path','tag','index','body','body_sha256','attributes')} for r in inventory['xml']],
        mappings=[dict(path=r['path'], id=r['id'], signatures=re.findall(r'^    v[12]: .+$',r['body'],re.M)) for r in inventory['mappings']])
