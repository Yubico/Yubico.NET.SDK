"""Minimal post-format input overlay; reuse v1 bodies/hosts and pinned tools by reference."""
from collections import Counter
from pathlib import Path
import json
import re
import shlex
import sys

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, RUN, sha, file_record, require, scan, identities

OVERLAY = HERE / 'post-format-1'
BASE = HERE / 'v1/manifest.json'
BEFORE = RUN / 'validation/pre-format-content.json'
REVIEW = RUN / 'evidence/review-path-manifest.json'
FORMAT_LOG = RUN / 'validation/format-apply.log'
ALLOW_REMOVALS = {
    'src/YubiHsm/src/IYubiKeyExtensions.cs': ['Yubico.YubiKit.Core.Protocols.SmartCard.Apdu', 'Yubico.YubiKit.Core.Protocols.SmartCard.Scp'],
    'src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/HsmAuthSessionTests.cs': ['Yubico.YubiKit.Core.Protocols.SmartCard.Apdu'],
    'src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthSessionByteLevelTests.cs': ['Yubico.YubiKit.Core.Protocols.SmartCard.Apdu'],
}
ORDER_PATH = 'src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthRetryExceptionTests.cs'
VERIFY_CHECK = "require(data['extraction_manifest_sha256'] == sha(manifest_path.read_bytes()), 'Capture binds another extraction manifest')"
VERIFY_PATCHES = {
    "manifest = json.loads(manifest_path.read_text())": "manifest = json.loads(manifest_path.read_text())\nmanifest['dependencies'] = [updated_dependencies.get(row['path'], row) for row in manifest['dependencies']]",
    "evidence = folder / context / 'evidence' / attempt": "evidence = OVERLAY / 'evidence' / context / attempt",
    VERIFY_CHECK: VERIFY_CHECK + "\n        require(data['post_format_overlay_sha256'] == overlay_sha and data['transform_binding_sha256'] == binding_sha, 'Post-format overlay/binding capture mismatch')",
}
CAPTURE_PATCHES = {
    "dest = folder / 'evidence' / attempt": "dest = OVERLAY / 'evidence' / context / attempt",
    "retained = [retain(work / name, name) for name in names]": "retained = [retain(work / name, name) for name in names]\n    retained.append(retain(OVERLAY / 'manifest.json', 'post-format-overlay.json.snapshot'))\n    retained.append(retain(OVERLAY / 'transform-binding.json', 'post-format-binding.json.snapshot'))",
    "native_shipping_capture=context == 'hsmauthtool',": "post_format_overlay_sha256=overlay_sha, transform_binding_sha256=binding_sha, native_shipping_capture=context == 'hsmauthtool',",
}


def header(text):
    """Normalize only ordinary imports and blank lines in the initial header; preserve all other bytes."""
    imports, opaque = [], []
    lines = text.splitlines(keepends=True)
    index = 0
    for line in lines:
        match = re.fullmatch(r'using ([A-Za-z_][A-Za-z_0-9]*(?:\.[A-Za-z_][A-Za-z_0-9]*)*);\n', line)
        if match:
            imports.append(match[1])
        elif line.startswith('//') or re.fullmatch(r'using [^\n]+;\n', line):
            opaque.append(line)
        elif not re.fullmatch(r'[ \t]*\n', line):
            break
        index += 1
    return imports, ''.join(opaque), ''.join(lines[index:]), sum(bool(re.fullmatch(r'[ \t]*\n', line)) for line in lines[:index])


def adapted(tool):
    source = HERE / tool
    text = source.read_text()
    patches = VERIFY_PATCHES if tool == 'verify.py' else CAPTURE_PATCHES
    for before, after in patches.items():
        require(text.count(before) == 1, f'Expected one pinned tool fragment: {tool}: {before}')
        text = text.replace(before, after)
    compile(text, str(source) + ':post-format-1', 'exec')
    return text, dict(source=file_record(source), substitutions=patches, virtual_target_sha256=sha(text.encode()))


def proof():
    base = json.loads(BASE.read_text())
    before = json.loads(BEFORE.read_text())
    review = json.loads(REVIEW.read_text())
    require(before['baseline'] == base['baseline'] == review['baseline'], 'Baseline mismatch')
    accepted = {row['path']: row for row in review['accepted']}
    captures = {row['path']: row for row in before['files']}
    require(len(accepted) == 35 and len(captures) == 26 and set(captures) == {path for path in accepted if path.endswith('.cs')}, 'Require accepted 35 paths and exactly their 26 C# pre-format captures')
    dependencies = {row['path']: row for row in base['dependencies']}
    command = FORMAT_LOG.read_text().splitlines()
    argv = shlex.split(command[0].removeprefix('$ '))
    require(argv[:2] == ['dotnet', 'format'] and '--include' in argv and set(captures) <= set(argv) and command[-1] == 'EXIT=0', 'Require actual successful scoped formatter command')
    rows = []
    for path, capture in captures.items():
        original = capture['content'].encode('utf-8')
        require(sha(original) == capture['sha256'] == accepted[path]['sha256'] == dependencies[path]['sha256'], f'Pre-format content must bind accepted review and v1 input: {path}')
        current = (ROOT / path).read_bytes()
        old, new = original.decode('utf-8'), current.decode('utf-8')
        require(old.endswith('\n') and not new.endswith('\n'), f'Expected exactly the recorded terminal LF removal: {path}')
        old_imports, old_opaque, old_body, old_blanks = header(old[:-1])
        new_imports, new_opaque, new_body, new_blanks = header(new)
        removals = ALLOW_REMOVALS.get(path, [])
        require(all(old_imports.count(name) == 1 and name not in new_imports for name in removals), f'Removal differs from named ordinary import: {path}')
        require(Counter(new_imports) == Counter(old_imports) - Counter(removals), f'Import addition/unknown removal: {path}')
        require(old_opaque == new_opaque and old_body == new_body, f'Non-import/comment/directive/body bytes changed beyond terminal LF: {path}')
        surviving = [name for name in old_imports if name not in removals]
        reordered = surviving != new_imports
        require(reordered == (path == ORDER_PATH), f'Unexpected import-order change: {path}')
        require(old_blanks == new_blanks or path == ORDER_PATH, f'Unexpected header blank-line change: {path}')
        if removals or reordered:
            require(not re.search(r'(?m)^\s*#', old + '\n' + new), f'Import changes require directive-free source: {path}')
        rows.append(dict(path=path, before_sha256=sha(original), after_sha256=sha(current), bytes=len(current),
                         removed_namespaces=removals, using_order_changed=reordered,
                         header_blank_lines_before=old_blanks, header_blank_lines_after=new_blanks,
                         terminal_lf_removed=True, other_header_and_body_bytes_identical=True))
    for path, row in accepted.items():
        if path not in captures:
            require(sha((ROOT / path).read_bytes()) == row['sha256'], f'Accepted documentation changed: {path}')
    for row in base['inventory']['documents']:
        require(sha((ROOT / row['path']).read_bytes()) == row['sha256'], f'Complete inventoried document changed: {row["path"]}')
    require(identities(scan()) == identities(base['inventory']), 'Fence/inline/XML/mapping body/addition/removal changed')
    for key in ('prepared_files', 'tool_sources', 'approach_provenance', 'dependencies'):
        for row in base[key]:
            raw = (ROOT / row['path']).read_bytes()
            if row['path'] not in captures:
                require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], f'Unlisted v1 dependency/configuration/tool changed: {row["path"]}')
    return dict(id='post-format-1', base_extraction=file_record(BASE), pre_format=file_record(BEFORE),
                accepted_review=file_record(REVIEW), formatter_command=file_record(FORMAT_LOG),
                runner=file_record(Path(__file__)), files=rows,
                runtime_adaptations={tool: adapted(tool)[1] for tool in ('verify.py', 'capture.py')},
                policy='Reuse immutable v1 bodies/hosts/tools by reference; update only 26 proven source input hashes. Exact non-import/header/body bytes survive, except one final LF; four named ordinary imports removed and one directive-free import/header group reordered. Coordinator Roslyn proof remains separate.')


def validate():
    record = json.loads((OVERLAY / 'manifest.json').read_text())
    require(record == proof(), 'Post-format overlay/input/proof identity changed; preserve it and create a fresh scoped record')
    return record


def binding(record):
    path = OVERLAY / 'transform-binding.json'
    require(path.exists(), 'Bind the coordinator format-transform.json before compiling/capturing accepted post-format inputs')
    result = json.loads(path.read_text())
    transform = ROOT / result['transform']['path']
    require(file_record(transform) == result['transform'], 'Coordinator transform record changed')
    require(result['overlay_sha256'] == sha((OVERLAY / 'manifest.json').read_bytes()), 'Transform binding belongs to another overlay')
    check_transform(transform, record)
    return sha(path.read_bytes())


def check_transform(path, record):
    require(path.is_relative_to(RUN / 'validation'), 'Coordinator transform must live under RUN/validation')
    transform = json.loads(path.read_text())
    require(transform['baseline'] == 'a74792facb657413e98223e059876d8b213ffff3' and transform['formatter_exit'] == 0, 'Coordinator transform baseline/formatter failed')
    rows = {row['path']: row for row in transform['files']}
    require(len(rows) == len(transform['files']) == 26 and set(rows) == {row['path'] for row in record['files']}, 'Coordinator transform path set mismatch')
    for row in record['files']:
        actual = rows[row['path']]
        require(actual['before_sha256'] == row['before_sha256'] and actual['after_sha256'] == row['after_sha256'], 'Coordinator transform source hashes differ')
        require(sorted(actual['removed_namespaces']) == sorted(row['removed_namespaces']) and actual['added_namespaces'] == [] and actual['non_using_non_whitespace_identical'] is True, 'Coordinator import/body accounting differs')


require(len(sys.argv) >= 2, 'Usage: post-format.py reconcile | bind-transform <repo-relative-json> | verify prepared|compiled [capture args] | capture <context> <attempt>')
action, *arguments = sys.argv[1:]
if action == 'reconcile':
    require(not arguments, 'reconcile takes no arguments')
    record = proof()
    OVERLAY.mkdir(exist_ok=False)
    with (OVERLAY / 'manifest.json').open('x') as output:
        json.dump(record, output, indent=2)
        output.write('\n')
    print('PASS: 26 sources bind accepted 35-path review, v1 inputs and pre-format capture; exact EOF/import-only proof; v1 artifacts remain unchanged. Coordinator transform binding/Roslyn proof remain separate.')
    raise SystemExit(0)
record = validate()
if action == 'bind-transform':
    require(len(arguments) == 1, 'bind-transform requires the coordinator repo-relative transform JSON')
    path = (ROOT / arguments[0]).resolve()
    check_transform(path, record)
    value = dict(overlay_sha256=sha((OVERLAY / 'manifest.json').read_bytes()), transform=file_record(path),
                 scope='Coordinator exact formatter transform agrees with independent byte-preserving proof; separate Roslyn token/API evidence retains its own gate.')
    with (OVERLAY / 'transform-binding.json').open('x') as output:
        json.dump(value, output, indent=2)
        output.write('\n')
    print('PASS: coordinator formatter transform bound exactly; post-format compilation/capture input identity ready.')
    raise SystemExit(0)
require(action in ('verify', 'capture'), 'Unknown post-format action')
require(arguments and (action != 'verify' or arguments[0] in ('prepared', 'compiled')), 'Require prepared|compiled verification stage')
binding_sha = binding(record) if action == 'capture' or arguments[0] == 'compiled' else None
tool = action + '.py'
text, adaptation = adapted(tool)
require(adaptation == record['runtime_adaptations'][tool], 'Pinned runtime tool substitution changed')
updated = {row['path']: dict(path=row['path'], bytes=row['bytes'], sha256=row['after_sha256']) for row in record['files']}
sys.argv = [str(HERE / tool), 'v1', *arguments]
exec(compile(text, str(HERE / tool) + ':post-format-1', 'exec'),
     dict(__name__='__main__', __file__=str(HERE / tool), OVERLAY=OVERLAY,
          updated_dependencies=updated, overlay_sha=sha((OVERLAY / 'manifest.json').read_bytes()), binding_sha=binding_sha))
