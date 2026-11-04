"""Extract complete accepted documentation groups; compile/run remain coordinator-owned."""
from pathlib import Path
import json
import re
import sys

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, RUN, sha, file_record, require, scan, unit_id, source_paths

SHARED = ['docs/usage/user-interaction.md', 'docs/migration/v1-to-v2.md',
          'docs/migration/v1-to-v2-gaps.md', 'docs/migration/v1-to-v2-map.yml',
          'docs/v1-to-v2-comparison.md', 'docs/v2-highlights.md']


def retain(path, raw):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        require(path.read_bytes() == raw, f'Refusing changed existing preparation: {path}')
    else:
        with path.open('xb') as output:
            output.write(raw)


def checkpoint():
    report = RUN / 'edit-report.md'
    text = report.read_text()
    rows = re.findall(r'^([a-f0-9]{64}) (.+)$', text, re.M)
    require(len(rows) == 35, 'Reconcile completed editor product path list')
    identities = [dict(path=path, reported_sha256=digest, current_sha256=sha((ROOT / path).read_bytes())) for digest, path in rows]
    record = dict(editor_complete=True,
                  notification='Coordinator explicitly released final extraction after editor completion: 35 product files/26 C#; use complete final current docs and six scoped shared sections, retain HSM-IMP-008 qualification; hardware remains coordinator-owned.',
                  editor_session='ses_e599c7023ffetNl1WFvH5NVqnX',
                  editor_report=dict(path=str(report.relative_to(ROOT)), sha256=sha(report.read_bytes())),
                  edited_paths=sorted(path for digest, path in rows), shared_paths=SHARED,
                  handoff_reconciliation=identities,
                  reconciliation_policy='Six README body hashes are checked independently below; whole-file advances are pinned as current inputs and reported for coordinator final-review reconciliation.')
    retain(HERE / 'extraction-scope.json', (json.dumps(record, indent=2) + '\n').encode())
    return record


def preview(inventory):
    for kind, plural in [('fence', 'fences'), ('xml', 'xml')]:
        for item in inventory[plural]:
            print(unit_id(kind, item), 'scope=' + item.get('scope', 'module-xml'), 'tag/lang=' + item.get('tag', item.get('language', '')))
            print(item['body'] if kind == 'xml' else item['body'][:260])
    print('MEANINGFUL INLINE:')
    for item in inventory['inline']:
        if '(' in item['body'] or '=' in item['body'] or ';' in item['body'] or '[Trait' in item['body']:
            print(unit_id('inline', item), item['scope'], repr(item['body']))
    print('TOTALS', {key: len(inventory[key]) for key in ('documents', 'fences', 'inline', 'xml', 'xml_documents', 'mappings')})


require(sys.argv[1:] in (['preview'], ['prepare']), 'Usage: prepare.py preview|prepare (after explicit editor-complete notification)')
checkpoint_record = checkpoint()
inventory = scan()
if sys.argv[1] == 'preview':
    preview(inventory)
    raise SystemExit(0)

revision = 'v1'
folder = HERE / revision
require(not (folder / 'manifest.json').exists(), 'Extraction already prepared; preserve it and create a fresh revision for corrections')
tooling = json.loads((HERE / 'tooling-provenance.json').read_text())
for row in tooling['copies']:
    original = (ROOT / row['source']).read_bytes()
    target = HERE / row['target']
    require(sha(original) == row['source_sha256'] and sha(target.read_bytes()) == row['target_sha256'], 'Template source/target provenance changed')
    text = original.decode('utf-8')
    for before, after in row['substitutions'].items():
        require(text.count(before) == row['substitution_counts'][before], 'Template substitution count changed')
        text = text.replace(before, after)
    require(text.encode() == target.read_bytes(), 'Template has unrecorded changes')
for row in tooling['adaptations']:
    require(sha((ROOT / row['source']).read_bytes()) == row['source_sha256'] and sha((HERE / row['target']).read_bytes()) == row['target_sha256'], 'Adapted tool provenance changed')
for row in tooling['tools']:
    require(sha((HERE / row['path']).read_bytes()) == row['sha256'], 'Prepared tool changed')

readme = 'src/YubiHsm/README.md'
interaction = 'docs/usage/user-interaction.md'
fences = {(row['path'], row['index']): row for row in inventory['fences']}


def block(path, index):
    return fences[(path, index)]


expected = re.findall(r'\| README #\d+ / \d+ \|.*?\| `([a-f0-9]{64})` \|', (RUN / 'edit-report.md').read_text())
actual = [row['body_sha256'] for row in inventory['fences'] if row['path'] == readme and row['csharp_index'] is not None]
require(len(actual) == 6 and actual == expected, 'Six complete README bodies differ from the editor checkpoint')
groups = []


def group(name, originals, wrapper='', type_body='', prerequisites=None, transform='Complete original member/body retained; enclosing class supplies declaration context.'):
    bound = [dict(item, unit_id=unit_id(kind, item)) for kind, item in originals]
    raw = '\n'.join(item['body'] for item in bound)
    relative = 'raw/' + name + '.cs.txt'
    retain(folder / relative, raw.encode())
    groups.append(dict(name=name, context='consumer', originals=bound, raw=relative, raw_sha256=sha(raw.encode()),
                       wrapper=wrapper, type_body=type_body, prerequisites=prerequisites or [], transform=transform))


listing = block(readme, 2)['body']
imports = re.findall(r'^using [A-Za-z0-9_.]+;\n', listing, re.M)
listing_body = re.sub(r'^using [A-Za-z0-9_.]+;\n', '', listing, flags=re.M)
group('Listing', [('fence', block(readme, 2))],
      wrapper='public static async Task ListingAsync()\n{\n' + listing_body + '}\n',
      prerequisites=['Actual device discovery and enabled SmartCard applet; complete loop and owned session disposal retained.'],
      transform='Separate only namespace imports into file scope; retain complete discovery/session/list body inside an async method.')
group('Enrollment', [('fence', block(readme, 3))], wrapper=block(readme, 3)['body'],
      prerequisites=['Selected real IYubiKey; borrowed management/password buffers; HSM object ID/label mapping; supplied typed provisioning delegate that completes enrollment/recovery storage before returning.'])
group('SymmetricHandshake', [('fence', block(readme, 4))], wrapper=block(readme, 4)['body'],
      prerequisites=['Real selected device and enrolled HSM key mapping; borrowed credential password; owned ISymmetricHsmExchange factory with partial-construction cleanup; complete disposable adapter contract and method retained.',
                     'HSM-IMP-008: supplied-cryptogram 56-byte parser compatibility remains unresolved; compile-only sketch establishes no authenticated interoperability.'])
group('ManagementRotation', [('fence', block(readme, 5))], wrapper=block(readme, 5)['body'],
      prerequisites=['Selected real device; borrowed current management key; supplied typed durable-recovery storage delegate; persist-before-PUT order and replacement clearing retained.'])
group('PromptSession', [('fence', block(readme, 6)), ('fence', block(readme, 7))],
      wrapper='public static async Task PromptSessionAsync(IYubiKey device)\n{\n' + block(readme, 7)['body'] + '}\n',
      type_body=block(readme, 6)['body'], prerequisites=['Real caller-selected IYubiKey and complete original TouchPrompt implementation; method owns/disposes the opened session.'],
      transform='Place complete prompt declaration at namespace scope; place complete options/await-using fragment in an async method with a real typed device parameter.')
wording = block(interaction, 2)['body']
wording_body = re.sub(r'^using [A-Za-z0-9_.]+;\n', '', wording, flags=re.M)
group('SharedOperationWording', [('fence', block(interaction, 2))],
      wrapper='public static string OperationMessage(UserPresenceContext context)\n{\n' + wording_body + '\nreturn message;\n}\n',
      prerequisites=['Real UserPresenceContext from a callback and public operation/application constants through the genuine YubiHsm/Core references.'],
      transform='Separate namespace import; retain complete switch/default; return its original message result from a typed wrapper.')
group('SharedIntentPrompt', [('fence', block(interaction, 4))],
      type_body='public sealed class SharedIntentPrompt : IUserPresencePrompt\n{\n' + block(interaction, 4)['body'] + '}\n',
      prerequisites=['Real IUserPresencePrompt callback and UserPresenceContext.Intent; original asynchronous callback signature/body retained.'],
      transform='Place the complete callback as the actual interface implementation; supply a containing sealed type without fake SDK/client methods.')
one_shot = [row for row in inventory['inline'] if row['path'] == readme and row['body'] == 'device.ListHsmAuthCredentialsAsync()']
require(len(one_shot) == 1, 'Reconcile current one-shot inline example')
group('InlineOneShotList', [('inline', one_shot[0])],
      wrapper='public static Task<IReadOnlyList<HsmAuthCredential>> OneShotListAsync(IYubiKey device)\n{\nreturn ' + one_shot[0]['body'] + ';\n}\n',
      prerequisites=['Actual typed IYubiKey; returned Task/IReadOnlyList<HsmAuthCredential> bind the genuine one-shot extension and automatic session disposal.'],
      transform='Keep the exact inline invocation with a real typed receiver; return its genuine asynchronous result.')

code = ''.join(imports) + '\nnamespace Yubico.YubiKit.AuditHsm001.Examples.V1.Consumer;\n\n'
code += 'public static class ConsumerExamples\n{\n' + '\n'.join(item['wrapper'] for item in groups if item['wrapper']) + '}\n\n'
code += '\n'.join(item['type_body'] for item in groups if item['type_body'])
retain(folder / 'consumer/src/Examples.cs', code.encode())
selected = {item['unit_id']: item_group['name'] for item_group in groups for item in item_group['originals']}
dispositions = []
for kind, plural in [('fence', 'fences'), ('inline', 'inline'), ('xml', 'xml'), ('mapping', 'mappings')]:
    for item in inventory[plural]:
        identity = unit_id(kind, item)
        row = dict(id=identity, path=item['path'], body_sha256=item['body_sha256'])
        if identity in selected:
            row.update(status='compile', group=selected[identity], reason='Complete current body belongs to the named real typed consumer group.')
        elif kind == 'fence' and item['csharp_index'] is None:
            row.update(status='non-csharp', reason='Shell installation/sample-launch recipe; recorded as a command rather than C# compilation or execution.')
        elif kind == 'fence' and item['scope'].startswith('outside-hsm-'):
            row.update(status='other-applet', reason='Shared migration recipe outside the assigned YubiHSM section, under ' + ' / '.join(item['headings']) + '; preserve historical/current status there without expanding this module compilation.')
        elif kind == 'fence' and item['path'] == interaction and item['index'] == 1:
            row.update(status='other-applet', reason='Explicit PivSession/CreatePivSessionAsync example with Cli.Shared ConsoleUserPresencePrompt; outside YubiHSM compilation.')
        elif kind == 'fence' and item['path'] == interaction and item['index'] == 3:
            row.update(status='other-applet', reason='Implicit client is WebAuthnClient with PublicKeyCredentialRequestOptions per enclosing prose; GetAssertionAsync is a WebAuthn ceremony, so no stub client is invented.')
        elif kind == 'fence':
            raise ValueError(f'Unclassified fenced example: {identity}')
        elif kind == 'mapping':
            row.update(status='symbolic', reason='Migration before/after API designators and historical/current annotations; symbolic mapping rather than a complete C# body.')
        elif kind == 'xml':
            row.update(status='symbolic', reason='XML c-span literal, identifier, parameter/signature or slice/argument notation; all 25 spans are inline notation and there are no XML code/example bodies to compile.')
        elif item['scope'] == 'hsm-gaps' and item['body'] == 'GetApplicationVersion()':
            row.update(status='historical', reason='Historical v1 application-version API named as a missing public v2 method; compilation would invent an absent API.')
        elif item['body'] == 'SessionCreationOptions { PreferredConnectionType = ... }':
            row.update(status='noncompiling-by-design', reason='Prose configuration shape intentionally contains an ellipsis for a rejected non-SmartCard transport; retain as explanatory notation, without replacing the placeholder with a fake successful recipe.')
        elif item['path'] == 'docs/v1-to-v2-comparison.md' and item['body'] == 'PivSession.VerifyPinAsync(pinUtf8)':
            row.update(status='other-applet', reason='Explicit PIV verification designator in comparison prose; outside YubiHSM compilation.')
        elif '(' in item['body'] and item['path'] == 'src/YubiHsm/CLAUDE.md':
            row.update(status='symbolic', reason='Internal helper signature/designator rather than a callable public example: ' + item['body'] + '. Preserve descriptive private-helper ownership instead of fabricating access or parameters.')
        elif '=' in item['body']:
            row.update(status='symbolic', reason='Policy/property value or MSBuild setting notation rather than a complete C# assignment recipe: ' + item['body'])
        else:
            row.update(status='symbolic', reason='Inline identifier, literal, API designator or command/property notation; complete actionable bodies are inventoried separately.')
        dispositions.append(row)

require(not any(row['tag'] in ('code', 'example') for row in inventory['xml']), 'New XML body requires an explicit full group/disposition')
require(len(inventory['xml']) == 25 and len(inventory['xml_documents']) == 44, 'Reconcile XML coverage counts')
sample_sources = source_paths(ROOT / 'src/YubiHsm/examples/HsmAuthTool')
require(len(sample_sources) == 22, 'Reconcile complete 22-source HsmAuthTool')
prepared_paths = sorted(path for path in folder.rglob('*') if path.is_file())
prepared_paths.append(HERE / 'dispatch/src/Yubico.YubiKit.AuditHsm001.Examples.Shipping.HsmAuthTool.csproj')
tools = sorted(HERE.glob('*.py'))
dependencies = [ROOT / name for name in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', '.editorconfig',
                'src/YubiHsm/src/Yubico.YubiKit.YubiHsm.csproj', 'src/YubiHsm/examples/HsmAuthTool/HsmAuthTool.csproj')]
dependencies += [ROOT / row['path'] for row in inventory['xml_documents']]
record = dict(revision=revision, status='extracted-final-inputs', baseline='a74792facb657413e98223e059876d8b213ffff3',
              active_contexts=['consumer', 'hsmauthtool'], inventory=inventory, groups=groups, dispositions=dispositions,
              prepared_files=[file_record(path) for path in prepared_paths], tool_sources=[file_record(path) for path in tools],
              approach_provenance=[file_record(HERE / 'tooling-provenance.json'), file_record(HERE / 'extraction-scope.json'), file_record(RUN / 'edit-report.md')],
              dependencies=[file_record(path) for path in dependencies],
              sample=dict(project='src/YubiHsm/examples/HsmAuthTool/HsmAuthTool.csproj', source_paths=sample_sources,
                          traversal_driver=str(prepared_paths[-1].relative_to(ROOT))),
              reconciliation=dict(editor_report_sha256=checkpoint_record['editor_report']['sha256'],
                                  whole_file_advances=[row for row in checkpoint_record['handoff_reconciliation'] if row['reported_sha256'] != row['current_sha256']],
                                  six_readme_body_hashes_match=True),
              limits='Preparation and exact input provenance only. All groups use real SDK types/delegates/interfaces. HSM-IMP-008 supplied-cryptogram compatibility remains qualified; actual Csc/build results and runtime/HSM/device execution stay separate.')
with (folder / 'manifest.json').open('x') as output:
    json.dump(record, output, indent=2)
    output.write('\n')
print(f'Prepared {len(groups)} complete consumer groups from eight current fences and one meaningful inline call; all {len(dispositions)} units classified, 44 XML sources scanned and all 22 shipping source paths bound. Builds/captures remain coordinator-owned.')
