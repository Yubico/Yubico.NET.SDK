"""Capture-only SDK-default correction; reuse accepted consumer/first and preserve failed sample/first."""
from pathlib import Path
import json
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, sha, file_record, require

CORRECTION = HERE / 'sdk-default-1'
POST = HERE / 'post-format.py'
OVERLAY = HERE / 'post-format-1'
FIRST = OVERLAY / 'evidence/hsmauthtool/first'
CONSUMER = OVERLAY / 'evidence/consumer/first'
WORK = Path('/tmp/opencode/yubihsm-001-examples-v1-hsmauthtool')
SDK = Path('/home/rupicola/.local/share/mise/dotnet-root/sdk/10.0.403')
DEFAULT = SDK / 'vCurrent/Custom.Before.Microsoft.Common.props'
NAMES = ('compile-inputs.txt', 'resolved-references.txt', 'analyzers.txt', 'analyzer-configs.txt', 'policy-final.txt', 'csc-command-line.txt')
GUARD = "require(not policy['CustomBeforeMicrosoftCommonProps'].strip(), 'Remove early common-props hook from the shipping command')"
DEFAULT_GUARD = "require(not policy['CustomBeforeMicrosoftCommonProps'].strip() or (policy['NETCoreSdkVersion'] == '10.0.403' and policy['CustomBeforeMicrosoftCommonProps'] == " + repr(str(DEFAULT)) + " and not Path(" + repr(str(DEFAULT)) + ").exists() and not Path(" + repr(str(DEFAULT)) + ").is_symlink()), 'Require empty hook or the exact pinned, absent SDK 10.0.403 default; external hooks remain rejected')"
DISPATCH_NEEDLE = "require(adaptation == record['runtime_adaptations'][tool], 'Pinned runtime tool substitution changed')"


def files(folder):
    return [file_record(path) for path in sorted(folder.rglob('*')) if path.is_file()]


def sdk_record(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=sha(raw))


def replace_once(text, before, after):
    require(text.count(before) == 1, 'Expected one pinned correction fragment: ' + before)
    return text.replace(before, after)


def source_evidence():
    common = SDK / 'Current/Microsoft.Common.props'
    outer = SDK / 'Sdks/Microsoft.NET.Sdk/Sdk/Sdk.props'
    tree = ET.parse(common).getroot()
    node = tree.find('.//CustomBeforeMicrosoftCommonProps')
    require(node is not None and node.attrib['Condition'] == "'$(CustomBeforeMicrosoftCommonProps)'==''", 'SDK empty-property default condition changed')
    require(node.text == r'$(MSBuildExtensionsPath)\v$(MSBuildToolsVersion)\Custom.Before.$(MSBuildThisFile)', 'SDK default expression changed')
    imports = [entry for entry in tree.findall('Import') if entry.attrib.get('Project') == '$(CustomBeforeMicrosoftCommonProps)']
    require(len(imports) == 1 and imports[0].attrib['Condition'] == "'$(CustomBeforeMicrosoftCommonProps)' != '' and Exists('$(CustomBeforeMicrosoftCommonProps)')", 'SDK conditional import changed')
    require(r'$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props' in outer.read_text(), 'SDK import context changed')
    expected = node.text.replace('$(MSBuildExtensionsPath)', str(SDK)).replace('$(MSBuildToolsVersion)', 'Current').replace('$(MSBuildThisFile)', 'Microsoft.Common.props').replace('\\', '/')
    policy = dict(line.split('=', 1) for line in (FIRST / 'policy-final.txt').read_text().splitlines())
    require(policy['NETCoreSdkVersion'] == '10.0.403' and policy['CustomBeforeMicrosoftCommonProps'] == expected == str(DEFAULT), 'Captured value differs from verified SDK default')
    require(not DEFAULT.exists() and not DEFAULT.is_symlink() and not DEFAULT.parent.exists(), 'Default target appeared; reopen the imported-file assumption rather than trusting an external hook')
    return [common, outer], dict(sdk_version='10.0.403', sdk_root=str(SDK), default_target=str(DEFAULT), target_exists=False,
                                 default_expression=node.text, import_condition=imports[0].attrib['Condition'],
                                 observed_policy=file_record(FIRST / 'policy-final.txt'),
                                 conclusion='The nonempty property matches the installed SDK default. The target directory/file is absent at inspection; Exists makes this a dormant import path, not imported hook bytes.')


def adaptations():
    post = POST.read_text()
    replacements = {
        'dispatcher': {DISPATCH_NEEDLE: DISPATCH_NEEDLE + '\ntext = _sdk_adjust(tool, text)'},
        'capture_guard': {GUARD: DEFAULT_GUARD},
    }
    virtual = replace_once(post, DISPATCH_NEEDLE, replacements['dispatcher'][DISPATCH_NEEDLE])
    compile(virtual, str(POST) + ':sdk-default-1', 'exec')
    return dict(dispatcher_source=file_record(POST), virtual_dispatcher_sha256=sha(virtual.encode()),
                capture_source=file_record(HERE / 'capture.py'), verifier_source=file_record(HERE / 'verify.py'),
                replacements=replacements)


def identity():
    sources, evidence = source_evidence()
    failure = json.loads((FIRST / 'capture-failure.json').read_text())
    require(failure['error'] == 'Remove early common-props hook from the shipping command', 'Original capture failure classification changed')
    require(not (FIRST / 'manifest.json').exists(), 'Failed sample/first unexpectedly became accepted evidence')
    for name in NAMES:
        require((FIRST / name).read_bytes() == (WORK / name).read_bytes(), 'Actual compiler workspace changed after original capture; retain new build identity before proceeding')
    consumer = json.loads((CONSUMER / 'manifest.json').read_text())
    require(consumer['context'] == 'consumer' and consumer['attempt'] == 'first', 'Accepted consumer identity changed')
    for row in consumer['retained']:
        raw = (CONSUMER / row['path']).read_bytes()
        require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], 'Accepted consumer text changed')
    return dict(id='sdk-default-1', runner=file_record(Path(__file__)), sdk=evidence,
                sdk_sources=[sdk_record(path) for path in sources], failed_first=files(FIRST),
                accepted_consumer_first=files(CONSUMER), workspace=[dict(path=str(WORK / name), sha256=sha((WORK / name).read_bytes())) for name in NAMES],
                immutable_inputs=[file_record(OVERLAY / 'manifest.json'), file_record(OVERLAY / 'transform-binding.json'), file_record(HERE / 'v1/manifest.json')],
                tool_adaptations=adaptations(),
                policy='Capture-only preparation-assumption correction. Pin original tools and reproduce narrow after-program substitutions in memory; preserve consumer/first and failed HsmAuthTool/first. Empty hook or exact absent SDK default only; all native output, compiler membership, analyzer/assets and current-body checks remain active. No rebuild or execution.')


def validate():
    record = json.loads((CORRECTION / 'manifest.json').read_text())
    require(record == identity(), 'SDK correction/source/capture/input identity changed')
    for row in record['sdk_sources']:
        snapshot = CORRECTION / 'sdk-source' / (Path(row['path']).name + '.snapshot')
        raw = snapshot.read_bytes()
        require(len(raw) == row['bytes'] and sha(raw) == row['sha256'], 'SDK source snapshot changed')
    return record


def adjust(tool, text):
    digest = sha((CORRECTION / 'manifest.json').read_bytes())
    if tool == 'capture.py':
        text = replace_once(text, GUARD, DEFAULT_GUARD)
        text = replace_once(text, "native_shipping_capture=context == 'hsmauthtool',", "sdk_default_correction_sha256=" + repr(digest) + ", native_shipping_capture=context == 'hsmauthtool',")
        retained = "retained.append(retain(OVERLAY / 'transform-binding.json', 'post-format-binding.json.snapshot'))"
        text = replace_once(text, retained, retained + "\n    retained.append(retain(Path(" + repr(str(CORRECTION / 'manifest.json')) + "), 'sdk-default-correction.json.snapshot'))")
    else:
        anchor = "require(data['post_format_overlay_sha256'] == overlay_sha and data['transform_binding_sha256'] == binding_sha, 'Post-format overlay/binding capture mismatch')"
        extra = "\n        if context == 'hsmauthtool':\n            require(data.get('sdk_default_correction_sha256') == " + repr(digest) + " and attempt == 'second', 'Require corrected HsmAuthTool/second provenance; failed first remains historical')"
        text = replace_once(text, anchor, anchor + extra)
    compile(text, str(HERE / tool) + ':sdk-default-1', 'exec')
    return text


def layered_programs():
    prefix = POST.read_text().split("require(len(sys.argv) >= 2,", 1)[0]
    scope = dict(__file__=str(POST), __name__='sdk_default_static')
    exec(compile(prefix, str(POST), 'exec'), scope)
    rows = []
    for tool in ('capture.py', 'verify.py'):
        before_text, provenance = scope['adapted'](tool)
        after_text = adjust(tool, before_text)
        rows.append(dict(tool=tool, base_post_format_adaptation=provenance,
                         before_program_sha256=sha(before_text.encode()), after_program_sha256=sha(after_text.encode()),
                         correction_runner=file_record(Path(__file__)),
                         scope='After-program reproduced by the pinned correction runner; exact SDK guard, correction-manifest retention/provenance and HsmAuthTool-only verifier binding. Originals remain referenced, never overwritten.'))
    return rows


require(len(sys.argv) >= 2, 'Usage: sdk-default.py prepare | static | capture hsmauthtool second | verify prepared|compiled [capture selectors]')
action, *arguments = sys.argv[1:]
if action == 'prepare':
    require(not arguments, 'prepare takes no arguments')
    record = identity()
    CORRECTION.mkdir(exist_ok=False)
    (CORRECTION / 'sdk-source').mkdir()
    for row in record['sdk_sources']:
        with (CORRECTION / 'sdk-source' / (Path(row['path']).name + '.snapshot')).open('xb') as output:
            output.write((ROOT / row['path']).read_bytes())
    with (CORRECTION / 'manifest.json').open('x') as output:
        json.dump(record, output, indent=2)
        output.write('\n')
    with (CORRECTION / 'tool-adaptations.json').open('x') as output:
        json.dump(layered_programs(), output, indent=2)
        output.write('\n')
    print('Prepared capture-only correction with exact SDK source snapshots/default absence, original failure, accepted consumer and before/after tool substitution provenance.')
    raise SystemExit(0)
record = validate()
if action == 'static':
    require(not arguments, 'static takes no arguments')
    # Validate both layered after-programs without executing either capture or compiler.
    require(json.loads((CORRECTION / 'tool-adaptations.json').read_text()) == layered_programs(), 'Before/after virtual tool identities changed')
    print('PASS: SDK 10.0.403 default expression/Exists import/absent target and exact source snapshots; original failed first plus accepted consumer/first unchanged; compiler workspace text intact; both narrow layered after-programs parse. No build/capture/device execution.')
    raise SystemExit(0)
require(action in ('capture', 'verify'), 'Unknown correction action')
require(action != 'capture' or arguments == ['hsmauthtool', 'second'], 'Capture only HsmAuthTool/second; preserve first and accepted consumer')
require(action != 'verify' or arguments and arguments[0] in ('prepared', 'compiled'), 'Require verification stage')
post_text = replace_once(POST.read_text(), DISPATCH_NEEDLE, DISPATCH_NEEDLE + '\ntext = _sdk_adjust(tool, text)')
require(sha(post_text.encode()) == record['tool_adaptations']['virtual_dispatcher_sha256'], 'Virtual dispatcher changed')
sys.argv = [str(POST), action, *arguments]
exec(compile(post_text, str(POST) + ':sdk-default-1', 'exec'),
     dict(__name__='__main__', __file__=str(POST), _sdk_adjust=adjust))
