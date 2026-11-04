"""Bind released final identities and extract complete groups; compilation stays coordinator-owned."""
from pathlib import Path
import importlib.util
import json
import re
import sys

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, RUN, canonical, sha, file_record, require, scan, unit_id, source_paths

BASELINE = 'fe180282a2d77b15eaaa5868d6c8b00e251d1f05'


def retain(path, raw):
    current = HERE
    require(path.is_relative_to(HERE), 'Preparation output escaped assigned examples directory')
    for part in path.relative_to(HERE).parts:
        current /= part
        require(not current.is_symlink(), f'Symlinked output: {current}')
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        require(path.read_bytes() == raw, f'Preserve changed existing preparation: {path}')
    else:
        with path.open('xb') as output:
            output.write(raw)


def record_json(path, value):
    retain(path, (json.dumps(value, indent=2) + '\n').encode())


def checkpoint():
    reviewed = json.loads((RUN / 'reviewed-files.json').read_text())
    accepted_path = RUN / 'evidence/review-accepted.json'
    accepted = json.loads(accepted_path.read_text())
    transform = json.loads((RUN / 'validation/format-transform.json').read_text())
    require(reviewed['baseline'] == accepted['baseline'] == transform['baseline'] == BASELINE,
            'Final checkpoint baseline mismatch')
    require(accepted['verdict'] == 'PASS' and accepted['product_path_count'] == 59 and accepted['csharp_files'] == 49,
            'Require final independent 59-path acceptance')
    require(accepted['required_findings_resolved'] == [f'PGP-REV-{number:03d}' for number in range(1, 6)],
            'Required correction acceptance changed')
    require(sha((RUN / 'edit-corrections.md').read_bytes()) == accepted['edit_corrections_sha256'],
            'Authoritative correction report changed')
    require(transform['accepted_manifest_sha256'] == sha(accepted_path.read_bytes()),
            'Formatter transform binds another accepted review')
    before = {row['path']: row for row in accepted['files']}
    final = {row['path']: row for row in reviewed['files']}
    changes = {row['path']: row for row in transform['files']}
    require(len(before) == len(final) == len(accepted['files']) == len(reviewed['files']) == 59 and
            set(before) == set(final), 'Final path set differs from accepted review')
    require(len(changes) == len(transform['files']) == 49 and
            set(changes) == {path for path in final if path.endswith('.cs')}, 'Formatter C# path set changed')
    for path, row in final.items():
        require(sha(canonical(path).read_bytes()) == row['final_sha256'], f'Final reviewed source changed: {path}')
        require(row['reviewed_sha256'] == before[path]['sha256'], f'Pre-format review identity differs: {path}')
        if path in changes:
            change = changes[path]
            require(change['before_sha256'] == row['reviewed_sha256'] and
                    change['after_sha256'] == row['final_sha256'] and
                    change['whitespace_only'] is True and change['imports_identical'] is True,
                    f'Formatter change exceeds released whitespace-only boundary: {path}')
        else:
            require(row['final_sha256'] == row['reviewed_sha256'], f'Unrecorded non-C# change: {path}')
    package = json.loads((RUN / 'validation/package-description.json').read_text())
    require(package['all_other_xml_identical'] is True and
            package['final_sha256'] == final[package['path']]['final_sha256'], 'Package Description identity changed')
    shared = sorted(path for path in final if not path.startswith('src/OpenPgp/'))
    require(len(shared) == 7, 'Reconcile seven assigned shared documents')
    record = dict(editor_complete=True, formatter_complete=True, reviewer_complete=True,
                  notification='Coordinator released final post-editor/reviewer/formatter extraction: 59 accepted paths, 49 whitespace-only C# transforms with unchanged imports; twelve complete README bodies remain unchanged. Compilation and execution gates remain separate.',
                  editor_report=file_record(RUN / 'edit-corrections.md'), edited_paths=sorted(final), shared_paths=shared,
                  whole_shared_paths=['docs/usage/user-interaction.md'],
                  shared_sections={'README.md': ['Packages'], 'docs/migration/v1-to-v2.md': ['OpenPGP'],
                                   'docs/migration/v1-to-v2-gaps.md': ['Cross-cutting Developer Experience'],
                                   'docs/v1-to-v2-comparison.md': ['New in v2'], 'docs/v2-highlights.md': ['OpenPGP']},
                  shared_policy='Assigned OpenPGP sections plus generic user-presence callback/wording dependencies; explicit PIV/WebAuthn ceremonies are excluded. Other shared fences receive outside-applet dispositions; no other-applet implementation investigation.',
                  final_review=file_record(RUN / 'reviewed-files.json'), accepted_review=file_record(accepted_path),
                  formatter_transform=file_record(RUN / 'validation/format-transform.json'),
                  original_editor_report=file_record(RUN / 'edit-report.md'),
                  correction_acceptance=file_record(RUN / 'review-corrections.md'),
                  package_description=file_record(RUN / 'validation/package-description.json'))
    record_json(HERE / 'extraction-scope.json', record)
    return record


def preview(inventory):
    print('TOTALS', {key: len(inventory[key]) for key in ('documents', 'fences', 'inline', 'xml', 'xml_documents', 'mappings')})
    for kind, plural in [('fence', 'fences'), ('xml', 'xml')]:
        for item in inventory[plural]:
            print(unit_id(kind, item), item.get('scope', 'module-xml'), item.get('tag', item.get('language', '')),
                  repr(item['body']) if kind == 'xml' else ' / '.join(item.get('headings', [])))
    print('MEANINGFUL INLINE:')
    for item in inventory['inline']:
        if '(' in item['body'] or '=' in item['body'] or ';' in item['body'] or '[Trait' in item['body']:
            print(unit_id('inline', item), item['scope'], repr(item['body']))


require(sys.argv[1:] in (['preview'], ['prepare']), 'Usage: prepare-final.py preview|prepare')
checkpoint_record = checkpoint()
inventory = scan()
if sys.argv[1:] == ['preview']:
    preview(inventory)
    raise SystemExit(0)

folder = HERE / 'v1'
require(not (folder / 'manifest.json').exists(), 'Preserve the accepted extraction; use a fresh revision for new inputs')
tooling_path = HERE / 'tooling-provenance.json'
tooling = json.loads(tooling_path.read_text())
source_archive = tooling['source_manifest']
source_manifest = ROOT / source_archive['manifest']
require(sha(source_manifest.read_bytes()) == source_archive['manifest_sha256'], 'Frozen tooling source manifest changed')
frozen_rows = json.loads(source_manifest.read_text())['files']
require(len(frozen_rows) == source_archive['checked'] == 477, 'Frozen tooling archive count changed')
frozen = {row['path']: row for row in frozen_rows}
for row in tooling['copies']:
    source, target = ROOT / row['source'], HERE / row['target']
    original = source.read_bytes()
    expected = frozen[str(source.relative_to(source_manifest.parent))]
    require(len(original) == expected['bytes'] and sha(original) == expected['sha256'] == row['source_sha256'],
            'Frozen tooling source changed')
    text = original.decode()
    for before, after in row['substitutions'].items():
        require(text.count(before) == row['substitution_counts'][before], 'Tooling substitution count changed')
        text = text.replace(before, after)
    require(target.read_bytes() == text.encode() and sha(target.read_bytes()) == row['target_sha256'],
            'Integrated compiler/inventory/capture tooling changed')
for row in tooling['tools']:
    require(file_record(HERE / row['path'])['sha256'] == row['sha256'], 'Initial prepared tool changed')
for row in tooling['lesson_sources']:
    source = ROOT / row['source']
    expected = frozen[str(source.relative_to(source_manifest.parent))]
    require(sha(source.read_bytes()) == row['sha256'] == expected['sha256'], 'Frozen lesson source changed')

spec = importlib.util.spec_from_file_location('audit_pgp_sdk_policy', HERE / 'sdk-policy.py')
sdk = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sdk)
sdk_default = sdk.inspect_default()
sdk.validate(dict(NETCoreSdkVersion=sdk.SDK_VERSION, CustomBeforeMicrosoftCommonProps=''))
sdk.validate(dict(NETCoreSdkVersion=sdk.SDK_VERSION, CustomBeforeMicrosoftCommonProps=str(sdk.DEFAULT)))
record_json(folder / 'sdk-default-preparation.json', sdk_default)
for row in sdk_default['sdk_sources']:
    retain(folder / 'sdk-source' / (Path(row['path']).name + '.snapshot'), Path(row['path']).read_bytes())

coverage = json.loads((RUN / 'coverage-final.json').read_text())
require(coverage['baseline'] == BASELINE and coverage['module_paths'] == len(coverage['module']) == 96,
        'Final module coverage path set changed')
module_rows = {row['path']: row for row in coverage['module']}
for path, row in module_rows.items():
    require(sha(canonical(path).read_bytes()) == row['final_sha256'], f'Final module input changed: {path}')
source_set = source_paths(ROOT / 'src/OpenPgp')
require(len(inventory['xml_documents']) == len(source_set) == 84 and
        [row['path'] for row in inventory['xml_documents']] == source_set, 'Reconcile all 84 module C# XML inputs')
source_categories = {
    'library': sum(path.startswith('src/OpenPgp/src/') for path in source_set),
    'tool': sum(path.startswith('src/OpenPgp/examples/') for path in source_set),
    'tests': sum(path.startswith('src/OpenPgp/tests/') for path in source_set),
}
require(source_categories == dict(library=44, tool=19, tests=21), 'Module C# category set changed')

readme = 'src/OpenPgp/README.md'
interaction = 'docs/usage/user-interaction.md'
readme_blocks = [row for row in inventory['fences'] if row['path'] == readme and row['csharp_index'] is not None]
corrections = (RUN / 'edit-corrections.md').read_text()
expected = re.findall(r'^\| \d+ \| \d+ \| \d+ \| ([a-f0-9]{64}) \|$', corrections, re.M)
require([row['body_sha256'] for row in readme_blocks] == expected and
        sum(len(row['body'].splitlines()) for row in readme_blocks) == 197,
        'Complete README bodies differ from final independently reviewed correction report')
require(sha(canonical(readme).read_bytes()) == 'db23caf2e117418d76c0c3bbc5c2e14ff84bf267d6c1e0dcd7276dccbfb4e811',
        'Final corrected README identity changed')
groups = []


def group(name, originals, *, wrapper='', type_body='', prerequisites, resource_lifetimes, transform,
          source_segments=None):
    bound = [dict(item, unit_id=unit_id(kind, item), source=file_record(canonical(item['path'])))
             for kind, item in originals]
    raw = '\n'.join(item['body'] for item in bound)
    relative = 'raw/' + name + '.cs.txt'
    retain(folder / relative, raw.encode())
    groups.append(dict(name=name, context='consumer', originals=bound, raw=relative,
                       raw_sha256=sha(raw.encode()), wrapper=wrapper, type_body=type_body,
                       prerequisites=prerequisites, resource_lifetimes=resource_lifetimes,
                       transform=transform, source_segments=source_segments or []))


first = readme_blocks[0]
match = re.match(r'(?:using [A-Za-z0-9_.]+;\n)+\n', first['body'])
require(match is not None, 'Read-only entry-point directives changed')
directives, entry = match[0], first['body'][match.end():]
group('ReadOnlySession', [('fence', first)],
      wrapper='public static async Task InspectAsync()\n{\n' + entry + '}\n',
      prerequisites=['Actual discoverable IYubiKey and enabled OpenPGP applet; no placeholder device/session definitions.'],
      resource_lifetimes=['The complete original await-using declaration owns the opened session through metadata read and the entire key-information loop. No borrowed secret buffers.'],
      transform='Move the exact initial namespace directives to file scope; retain the complete remaining entry-point body inside an async method.',
      source_segments=[dict(text=directives, role='file-scope directives'), dict(text=entry, role='complete original entry-point statements')])
details = [
    ('ChangeCredentials',
     ['Real IYubiKey; five borrowed credential memories and caller cancellation token; current credentials must authorize the actual device.'],
     ['Keep all five inputs valid until completion; caller clears them. Original await-using owns the session across every persistent change and authentication. No rollback is introduced.']),
    ('RecoverUserPin',
     ['Real IYubiKey and caller-owned recovery material; the two complete methods retain alternative Reset Code and Admin-authenticated recovery routes.'],
     ['Each original method owns its own await-using session. Caller owns/reset-code/Admin/new-PIN buffers and clears them after the chosen operation completes.']),
    ('ConfigureInitialKdf',
     ['Authorized empty KDF-capable YubiKey; current Admin and default User/Admin byte buffers supplied by caller; ordinary verification route only.'],
     ['Keep original derivation/configured KDF declarations and aliasing of salt/hash arrays. Provisioning disposes before fresh verification; configured/derivation scopes enclose session use. Verification disposes before outer KDF teardown. Original finally clears the owned hashes and three salts, including exceptional paths; caller clears borrowed PINs.']),
    ('GenerateP256SigningKey',
     ['Real IYubiKey, borrowed valid Admin PIN and cancellation token; supported P-256 signing-slot profile.'],
     ['Original await-using owns the session through Admin verification, destructive generation and key-status readback. Caller keeps/clears the borrowed PIN.']),
    ('ImportRsaSigningKey',
     ['Real IYubiKey; borrowed Admin PIN and byte[] holding actual supported RSA PKCS#8 DER; genuine Core RSAPrivateKey.CreateFromPkcs8(ReadOnlyMemory<byte>) API.'],
     ['The complete using declaration owns the imported private-key object. Session disposes before that key object; source DER and PIN remain borrowed and caller-cleared.']),
    ('SignAndVerifyP256',
     ['Real IYubiKey with provisioned P-256 signing key; borrowed User PIN/message; complete original DecodeP256PublicKey helper and actual Core TLV APIs.'],
     ['Keep complete TLV decode helper, host ECDsa using scope and session await-using. Public coordinates escape the TLV scope. Original finally clears actual returned signature backing storage; caller owns PIN/message and any separate trust decision.']),
    ('DecipherAndConsume',
     ['Real IYubiKey, qualified extended-mode PIN/KDF configuration and algorithm-appropriate raw input; actual Func<ReadOnlyMemory<byte>, CancellationToken, Task> consumer supplied by application.'],
     ['Original awaited callback borrows the result only until it returns. Original finally clears actual result backing storage on success/failure; any callback copy needs its own clearing scope. Session teardown and borrowed input ownership remain unchanged.']),
    ('StoreAndReadCertificate',
     ['Real selected slot/device, valid borrowed Admin PIN and certificate DER; actual X509CertificateLoader API.'],
     ['Original loaded-certificate using, session await-using and read-back certificate using scopes all remain complete. Return public DER; caller owns inputs and trust/binding decisions.']),
    ('AttestGeneratedKey',
     ['Actual generated ordinary target key, borrowed User PIN and device/cancellation token; caller authorizes certificate overwrite and supplies later host trust verification.'],
     ['Original session and returned attestation using scopes remain intact. Exported DER survives certificate disposal; borrowed PIN belongs to caller.']),
    ('ConfigureSigningPolicy',
     ['Actual selected IYubiKey and authorized borrowed Admin PIN; supported ordinary Sig UIF/PIN-policy configuration.'],
     ['Original await-using owns session across both policy mutations and UIF readback; caller retains PIN until completion.']),
    ('ResetAndInspect',
     ['Application-authorized loss of ordinary material on the selected IYubiKey; full independent reset verification remains the application responsibility.'],
     ['Original await-using encloses attempted reset and partial metadata readback; returned model survives session disposal. ATT exceptions and partial-state caveats remain in surrounding source prose.']),
]
require(len(details) == len(readme_blocks) - 1, 'Assign every complete README method group')
for item, (name, prerequisites, lifetimes) in zip(readme_blocks[1:], details, strict=True):
    group(name, [('fence', item)], wrapper=item['body'], prerequisites=prerequisites,
          resource_lifetimes=lifetimes,
          transform='Keep the complete original method body/declarations byte-for-byte, including all helpers, using scopes, awaited calls and finally blocks; supply only the containing static class and common original directives.',
          source_segments=[dict(text=item['body'], role='complete original methods and helpers')])

shared_blocks = {row['index']: row for row in inventory['fences'] if row['path'] == interaction}
wording = shared_blocks[2]
match = re.match(r'using Yubico.YubiKit.Core.Credentials;\n\n', wording['body'])
require(match is not None, 'Shared wording import context changed')
wording_body = wording['body'][match.end():]
group('SharedOperationWording', [('fence', wording)],
      wrapper='public static string OperationMessage(UserPresenceContext context)\n{\n' + wording_body + '\nreturn message;\n}\n',
      prerequisites=['Genuine Core UserPresenceContext callback parameter and actual operation/application constants; default branch handles OpenPGP/future operations.'],
      resource_lifetimes=['Borrow the callback context for the synchronous formatting call. Return only the original display message; the complete switch/default remains.'],
      transform='Move the exact Credentials import to file scope. Keep every original statement and switch arm in a typed method and return its original message result.',
      source_segments=[dict(text=match[0], role='shared file-scope import'), dict(text=wording_body, role='complete original switch/body')])
callback = shared_blocks[4]
resolution = '''public ValueTask OnUserPresenceResolvedAsync(
    UserPresenceContext context, UserPresenceOutcome outcome, CancellationToken cancellationToken)
    => resolve(context, outcome, cancellationToken);
'''
prompt_type = '''public sealed class SharedIntentPrompt(
    Func<UserPresenceContext, UserPresenceOutcome, CancellationToken, ValueTask> resolve) : IUserPresencePrompt
{
''' + callback['body'] + '\n' + resolution + '}\n'
group('SharedIntentPrompt', [('fence', callback)], type_body=prompt_type,
      prerequisites=['Actual Core IUserPresencePrompt/UserPresenceContext/UserPresenceOutcome types. Application supplies a real typed resolution delegate; the wrapper implements the genuine full interface without a no-op SDK/client stub.'],
      resource_lifetimes=['Prompt and resolution delegate are borrowed services retained by caller through all sessions using them. Original request callback returns promptly; supplied resolution delegate owns the application cleanup/UI behavior and token handling.'],
      transform='Keep the complete original request callback byte-for-byte in an actual IUserPresencePrompt implementation; add only its containing type and genuine typed delegation for the required resolution method.',
      source_segments=[dict(text=callback['body'], role='complete original request callback')])


def inline(body):
    matches = [row for row in inventory['inline'] if row['path'] == readme and row['body'] == body]
    require(len(matches) == 1, 'Reconcile meaningful inline task fragment: ' + body)
    return matches[0]


item = inline('RsaAttributes.Create(RsaSize.Rsa2048)')
group('InlineRsaAttributes', [('inline', item)],
      wrapper='public static RsaAttributes Rsa2048Attributes()\n{\nreturn ' + item['body'] + ';\n}\n',
      prerequisites=['Actual public RsaAttributes/RsaSize factory and model; runtime ROCA/capability checks remain the caller provisioning responsibility.'],
      resource_lifetimes=['Factory returns an attribute model; no secret input, connection or disposable resource is invented.'],
      transform='Keep the exact executable factory expression and return its genuine model type from a containing method.',
      source_segments=[dict(text=item['body'], role='complete original expression')])
item = inline('GetPinStatusAsync().AttemptsUser')
group('InlineFreshUserAttempts', [('inline', item)],
      wrapper='public static async Task<int> ReadUserAttemptsAsync(IOpenPgpSession session)\n{\nreturn (await session.GetPinStatusAsync()).AttemptsUser;\n}\n',
      prerequisites=['Caller supplies a real open IOpenPgpSession. Public API confirms Task<PwStatus> and int AttemptsUser; the prose chain means awaiting fresh status before reading its property.'],
      resource_lifetimes=['Borrow the caller-owned live session through completion; the helper leaves its disposal/credential policy with the caller. No extra VERIFY consumes a retry.'],
      transform='Compile the entire prose task chain with the required real session receiver and await grouping: GetPinStatusAsync().AttemptsUser becomes (await session.GetPinStatusAsync()).AttemptsUser. Preserve the named query/property and add no fake result/helper definition.')
item = inline('DeleteCertificateAsync(slot)')
group('InlineDeleteCertificate', [('inline', item)],
      wrapper='public static Task DeleteCertificateInAuthorizedSessionAsync(IOpenPgpSession session, KeyRef slot)\n{\nreturn session.' + item['body'] + ';\n}\n',
      prerequisites=['Caller supplies a real open Admin-authorized IOpenPgpSession and the actual KeyRef slot; genuine Task-returning public DeleteCertificateAsync signature.'],
      resource_lifetimes=['Borrow the live authorized session through returned Task completion. Caller owns session teardown and the choice to mutate persistent certificate state.'],
      transform='Keep the complete invocation and slot argument; supply only its actual typed session receiver and return the genuine Task.',
      source_segments=[dict(text=item['body'], role='complete original invocation')])

code = directives + 'using Yubico.YubiKit.Core.Credentials;\n\n'
code += 'namespace Yubico.YubiKit.AuditPgp001.Examples.V1.Consumer;\n\n'
code += 'public static class ConsumerExamples\n{\n' + '\n'.join(row['wrapper'] for row in groups if row['wrapper']) + '}\n\n'
code += '\n'.join(row['type_body'] for row in groups if row['type_body'])
for row in groups:
    require(row['wrapper'] in code and row['type_body'] in code, 'Complete wrapper/type missing')
    for segment in row['source_segments']:
        require(segment['text'] in code, 'Complete original source segment omitted')
retain(folder / 'consumer/src/Examples.cs', code.encode())

selected = {original['unit_id']: row['name'] for row in groups for original in row['originals']}
require(len(selected) == sum(len(row['originals']) for row in groups), 'Original group membership overlaps')
dispositions = []
for kind, plural in [('fence', 'fences'), ('inline', 'inline'), ('xml', 'xml'), ('mapping', 'mappings')]:
    for item in inventory[plural]:
        identity = unit_id(kind, item)
        row = dict(id=identity, path=item['path'], body_sha256=item['body_sha256'])
        if identity in selected:
            row.update(status='compile', group=selected[identity], reason='Complete current original belongs to its named genuine typed consumer group with dependency/resource scopes.')
        elif kind == 'fence' and item['scope'].startswith('outside-openpgp-'):
            row.update(status='other-applet', reason='Outside the assigned OpenPGP shared-document section: ' + ' / '.join(item['headings']) + '. Historical/current source body is retained for identity, with no other-applet compilation claim.')
        elif kind == 'fence' and item['csharp_index'] is None:
            row.update(status='non-csharp', reason='Original shell installation/tool-launch recipe or explicitly unlabeled S2K pseudocode; retained as source notation rather than C# compilation/execution.')
        elif kind == 'fence' and item['path'] == interaction and item['index'] == 1:
            row.update(status='other-applet', reason='Complete explicit PivSession/CreatePivSessionAsync with Cli.Shared terminal prompt; outside assigned OpenPGP callback compilation.')
        elif kind == 'fence' and item['path'] == interaction and item['index'] == 3:
            row.update(status='other-applet', reason='Enclosing prose identifies implicit client as WebAuthnClient and requestOptions as PublicKeyCredentialRequestOptions. This is a WebAuthn ceremony; no fake generic client is substituted.')
        elif kind == 'fence':
            raise ValueError('Unclassified fenced example: ' + identity)
        elif kind == 'xml' and item['tag'] == 'code':
            row.update(status='non-executable', reason='Complete card byte-layout/serialization diagram or explicitly described S2K pseudocode, rather than C# source. Preserve actual byte labels/algorithm notation without inventing code.')
        elif kind == 'xml':
            row.update(status='symbolic', reason='XML c-span literal, member identifier or partial-file name; no callable C# example body is present. Original source/chunk/hash is retained.')
        elif kind == 'mapping':
            row.update(status='symbolic', reason='OpenPGP migration entry with historical/current API designators; symbolic migration metadata rather than an executable body. Chronology qualifications remain unchanged.')
        elif item['path'] == 'docs/migration/v1-to-v2-gaps.md' and item['body'] == 'UseTemporary(ILoggerFactory)':
            row.update(status='other-applet', reason='Cross-cutting logging helper designator outside the assigned current OpenPGP exception qualification; no logging implementation or fake logger is introduced.')
        elif item['body'] == 'OpenPgpSession.CreateAsync(connection, ...)':
            row.update(status='noncompiling-by-design', reason='Intentional ellipsis designates the connection-factory overload in ownership prose; the complete current device-factory path is compiled in ReadOnlySession. No undefined connection/options stub is invented.')
        elif '(' in item['body'] or '=' in item['body'] or ';' in item['body']:
            row.update(status='symbolic', reason='Contextual method signature, omitted-argument designator, tuple/byte-field notation or framework-setting list: ' + item['body'] + '. Meaningful complete task expressions are separately bound to typed groups.')
        else:
            row.update(status='symbolic', reason='Inline identifier, literal, property/type/API designator or command notation; actionable whole bodies are inventoried and grouped separately.')
        dispositions.append(row)

core_api = ROOT / 'src/Core/src/PublicAPI.Unshipped.txt'
signature_lines = [
    'static Yubico.YubiKit.Core.Cryptography.RSAPrivateKey.CreateFromPkcs8(System.ReadOnlyMemory<byte> encodedKey) -> Yubico.YubiKit.Core.Cryptography.RSAPrivateKey!',
    'Yubico.YubiKit.Core.Credentials.IUserPresencePrompt.OnUserPresenceRequestedAsync(Yubico.YubiKit.Core.Credentials.UserPresenceContext! context, System.Threading.CancellationToken cancellationToken) -> System.Threading.Tasks.ValueTask',
    'Yubico.YubiKit.Core.Credentials.IUserPresencePrompt.OnUserPresenceResolvedAsync(Yubico.YubiKit.Core.Credentials.UserPresenceContext! context, Yubico.YubiKit.Core.Credentials.UserPresenceOutcome outcome, System.Threading.CancellationToken cancellationToken) -> System.Threading.Tasks.ValueTask',
]
require(all(line in core_api.read_text().splitlines() for line in signature_lines), 'Supplied Core public signature context changed')
proof = dict(baseline=BASELINE, release=checkpoint_record, source_tooling=file_record(tooling_path),
             source_archive=source_archive, sdk_optional_default=file_record(folder / 'sdk-default-preparation.json'),
             core_public_signatures=dict(source=file_record(core_api), signatures=signature_lines,
                                         boundary='Public signature context only; no Core implementation investigation.'),
             source_categories=source_categories, readme_full_bodies=len(readme_blocks), readme_body_lines=197,
             groups=len(groups), selected_originals=len(selected), total_dispositions=len(dispositions),
             policy='Final post-formatter current input identities; complete originals separated from wrappers, all 84 module XML inputs and seven assigned shared documents reconciled. Initial edit report is historical; correction report and exact accepted/final hashes govern. No compiler/hardware result is synthesized.')
record_json(folder / 'preparation-provenance.json', proof)
sample_sources = source_paths(ROOT / 'src/OpenPgp/examples/OpenPgpTool')
require(len(sample_sources) == 19, 'Reconcile complete real OpenPgpTool')
driver = HERE / 'dispatch/src/Yubico.YubiKit.AuditPgp001.Examples.Shipping.OpenPgpTool.csproj'
prepared = sorted(path for path in folder.rglob('*') if path.is_file()) + [driver]
dependencies = set(module_rows) | set(checkpoint_record['edited_paths']) | {
    'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig',
    'src/Core/src/PublicAPI.Shipped.txt', 'src/Core/src/PublicAPI.Unshipped.txt',
}
approach = [tooling_path, HERE / 'extraction-scope.json', RUN / 'edit-report.md', RUN / 'edit-corrections.md',
            RUN / 'review-corrections.md', RUN / 'reviewed-files.json', RUN / 'evidence/review-accepted.json',
            RUN / 'validation/format-transform.json', RUN / 'validation/package-description.json',
            RUN / 'coverage-final.json', RUN / 'sampling-final.json']
record = dict(revision='v1', status='extracted-final-inputs', baseline=BASELINE,
              active_contexts=['consumer', 'openpgptool'], inventory=inventory, groups=groups,
              dispositions=dispositions, prepared_files=[file_record(path) for path in prepared],
              tool_sources=[file_record(path) for path in sorted(HERE.glob('*.py'))],
              approach_provenance=[file_record(path) for path in approach],
              dependencies=[file_record(canonical(path)) for path in sorted(dependencies)],
              sample=dict(project='src/OpenPgp/examples/OpenPgpTool/OpenPgpTool.csproj',
                          source_paths=sample_sources, traversal_driver=str(driver.relative_to(ROOT))),
              reconciliation=dict(final_review_paths=59, formatted_csharp_paths=49,
                                  module_paths=len(module_rows), shared_documents=len(checkpoint_record['shared_paths']),
                                  xml_inputs=len(source_set), readme_bodies_match_independent_review=True),
              limits='Preparation and exact input provenance only. Actual compiler binding/analyzers/Csc evidence remain coordinator gates. Wrapper changes retain complete originals and explicit typed dependencies. Deferred runtime contracts and recorded hardware failures remain independent; compilation establishes no device behavior or conformance.')
record_json(folder / 'manifest.json', record)
print(f'Prepared {len(groups)} complete consumer groups / {len(selected)} original units, including {len(readme_blocks)} complete README bodies / 197 lines; {len(dispositions)} explicit dispositions, 84 C# XML inputs, seven shared documents and all 19 real tool sources. Correct SDK optional default checked. Compilation/capture remain coordinator-owned.')
