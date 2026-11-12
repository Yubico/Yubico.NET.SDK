"""Fixed accepted A/C pilot extraction. Writes only this new examples packet, once."""
import hashlib
import html
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
RUN = HERE.parents[1]
ROOT = HERE.parents[8]


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def require(value, message):
    if not value:
        raise ValueError(message)


def identity(path):
    raw = path.read_bytes()
    return dict(path=path.relative_to(ROOT).as_posix(), bytes=len(raw), sha256=sha(raw))


def put(relative, raw):
    path = HERE / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('xb') as output:
        output.write(raw)
    return path


def extract(path, ordinal):
    raw = (ROOT / path).read_bytes()
    matches = list(re.finditer(rb'<code(?:\s[^>]*)?>(.*?)</code>', raw, re.S))
    found = matches[ordinal - 1]
    original = found.group(1)
    text = original.decode().replace('\r\n', '\n')
    lines = [re.sub(r'^\s*/// ?', '', line) for line in text.splitlines()]
    while lines and not lines[0].strip():
        lines.pop(0)
    while lines and not lines[-1].strip():
        lines.pop()
    decoded = html.unescape('\n'.join(lines))
    return dict(path=path, ordinal=ordinal, first_line=raw[:found.start()].count(b'\n') + 1,
                last_line=raw[:found.end()].count(b'\n') + 1, raw_start_byte=found.start(1),
                raw_end_byte=found.end(1), raw_sha256=sha(original), source=identity(ROOT / path),
                normalized_editor_body_sha256=sha(text.encode())), original, decoded


def host(row, body):
    common = ('using System;\nusing System.Buffers;\nusing System.Security.Cryptography;\n'
              'using System.Threading;\nusing System.Threading.Tasks;\n'
              'using Yubico.YubiKit.Core.Credentials;\nusing Yubico.YubiKit.Core.Cryptography;\n'
              'using Yubico.YubiKit.Core.Utilities;\n')
    name = 'Example' + row['id'].replace('-', '')
    pre, post = common, '\n'
    if row['id'] == 'A-03':
        match = re.match(r'((?:using [^\n]+;\n)+)\n', body)
        require(match is not None, 'Manager example import boundary changed')
        imports, method = body[:match.end()], body[match.end():]
        pre = imports + 'namespace Audit20261111.DocExamples;\ninternal static class ' + name + '\n{\n'
        body = method
        row['wrapper_note'] = 'Move the unchanged import prefix outside the containing class; unchanged complete method inside it.'
        row['preserved_import_prefix'] = imports
        post = '\n}\n'
    elif row['id'] == 'C-01':
        pre += 'namespace Audit20261111.DocExamples;\n'
        row['wrapper_note'] = 'Add real namespace imports only; the complete DialogPrompt class is unchanged.'
    else:
        pre += 'namespace Audit20261111.DocExamples;\ninternal static class ' + name + '\n{\n'
        if row['id'] == 'C-03':
            pre += ('    internal static async Task AuthenticateAsync(\n'
                    '        Yubico.YubiKit.WebAuthn.Client.WebAuthnClient client,\n'
                    '        Yubico.YubiKit.WebAuthn.Client.Authentication.PublicKeyCredentialRequestOptions requestOptions,\n'
                    '        CancellationToken cancellationToken)\n    {\n')
            post = '\n    }\n}\n'
            row['typed_prerequisites'] = 'Borrowed WebAuthnClient, PublicKeyCredentialRequestOptions and CancellationToken; client lifetime stays with caller.'
        elif row['id'].startswith('C-P'):
            pre += '    internal static void ConfigureOrUseProvider()\n    {\n'
            post = '\n    }\n}\n'
            row['typed_prerequisites'] = 'Actual System.Security.Cryptography provider types and SDK CryptographyProviders; the whole original using scope/factory assignment is preserved.'
        else:
            post = '\n}\n'
        row['wrapper_note'] = 'Add containing static class/imports and, for statement fragments, a typed method; original statements/resource scopes unchanged.'
    row['wrapped_body_sha256'] = sha(body.encode())
    return pre, body, post


def main():
    inputs = [RUN / 'editing/A/revision-2/example-bodies.json', RUN / 'editing/A/revision-2/review.md',
              RUN / 'editing/C/examples.json', RUN / 'editing/C/revision-2/review.md',
              RUN / 'editing/C/revision-2/mapping.json']
    a = json.loads(inputs[0].read_bytes())
    c = json.loads(inputs[2].read_bytes())
    records = []
    for index, editor in enumerate(a, 1):
        path = editor['path']
        ordinal = index if index < 3 else 1
        row, raw, body = extract(path, ordinal)
        require(sha(body.encode()) == editor['body_sha256'], 'Accepted A body changed: ' + path)
        row.update(id=f'A-{index:02}', family='A', classification=editor['classification'],
                   editor_body_sha256=editor['body_sha256'], accepted_inventory=inputs[0].relative_to(ROOT).as_posix(),
                   compilation_status='prepared' if index == 3 else 'qualified-internal-not-selected', execution_status='not-run')
        if index < 3:
            row['qualification'] = 'ConnectionTypeExtensions is internal; the unique consumer host has no existing IVT grant. No implementation copying, assembly-name impersonation or new access grant.'
        records.append((row, raw, body))
    provider = 0
    for editor in c:
        if editor['version'] != 'current':
            continue
        row, raw, body = extract(editor['path'], editor['ordinal'])
        require(row['normalized_editor_body_sha256'] == editor['body_sha256'], 'Accepted C XML region changed: ' + editor['path'])
        historical = editor['path'].endswith('/RsaFormat.cs')
        leaf = Path(editor['path']).stem
        if historical:
            name = f'C-H{editor["ordinal"]:02}'
        elif leaf == 'CryptographyProviders':
            provider += 1
            name = f'C-P{provider:02}'
        else:
            name = {'ICredentialPrompt': 'C-01', 'ISecureCredentialReader': 'C-02',
                    'UserPresenceIntent': 'C-03', 'IPrivateKeyExtensions': 'C-04'}[leaf]
        row.update(id=name, family='C', classification=editor['classification'],
                   accepted_inventory=inputs[2].relative_to(ROOT).as_posix(), editor_body_sha256=editor['body_sha256'],
                   compilation_status='historical-excluded' if historical else 'prepared', execution_status='not-run')
        records.append((row, raw, body))
    require(len(records) == 41 and sum(row['compilation_status'] == 'prepared' for row, _, _ in records) == 17,
            'Reconcile the accepted A/C example population')
    for row, raw, body in records:
        row['raw_file'] = put('raw/' + row['id'] + '.xml.snapshot', raw).relative_to(ROOT).as_posix()
        row['decoded_file'] = put('decoded/' + row['id'] + '.cs.txt', body.encode()).relative_to(ROOT).as_posix()
        row['decoded_sha256'] = sha(body.encode())
        row['extraction'] = 'Exact UTF-8 bytes between XML code tags retained; compile derivative removes only XML /// prefixes, boundary blank lines and XML entity escaping.'
        if row['compilation_status'] == 'prepared':
            pre, content, post = host(row, body)
            row['wrapper_prefix_file'] = put('wrappers/' + row['id'] + '.prefix.txt', pre.encode()).relative_to(ROOT).as_posix()
            row['wrapper_suffix_file'] = put('wrappers/' + row['id'] + '.suffix.txt', post.encode()).relative_to(ROOT).as_posix()
            generated = put('host/src/' + row['id'] + '.cs', (pre + content + post).encode())
            row['generated_host_source'] = identity(generated)
            row['compiled_body_start_byte'] = len(pre.encode())
            row['compiled_body_end_byte'] = len((pre + content).encode())
            row['context'] = 'Yubico.YubiKit.Audit20261111.DocExamples; genuine Core/Piv/WebAuthn project references; compile-only library; no entry point.'
    manifest = dict(schema='core-doc-examples-1', source_baseline='fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef',
                    acceptance_inputs=[identity(path) for path in inputs], examples=[row for row, _, _ in records],
                    counts=dict(active=19, prepared=17, internal_qualified=2, historical_excluded=22),
                    extension_policy='Append only later independently accepted batch bodies and exact review/inventory pins in a fresh manifest; never treat mutable worktrees as accepted truth.')
    put('manifest.json', (json.dumps(manifest, indent=2) + '\n').encode())
    print(json.dumps(manifest['counts']))


if __name__ == '__main__':
    main()
