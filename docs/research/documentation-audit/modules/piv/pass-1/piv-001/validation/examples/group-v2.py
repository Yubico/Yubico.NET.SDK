"""Prepare explicit typed tasks from released complete bodies; leave compilation to the coordinator."""
from pathlib import Path
import json
import re
import sys

sys.dont_write_bytecode = True
from inventory import HERE, ROOT, sha, require, unit_id

inventory_path = HERE / 'v2/inventory.json'
data = json.loads(inventory_path.read_text())
units = {unit_id(kind, item): (kind, item) for kind, plural in [('fence', 'fences'), ('inline', 'inline'), ('xml', 'xml'), ('mapping', 'mappings'), ('task', 'tasks')] for item in data[plural]}
groups = []
selected = {}
imports = [
    'using System;', 'using System.Buffers;', 'using System.IO;', 'using System.Threading;',
    'using System.Threading.Tasks;', 'using System.Security.Cryptography;',
    'using System.Security.Cryptography.X509Certificates;', 'using System.Text;',
    'using Yubico.YubiKit.Core.Abstractions;', 'using Yubico.YubiKit.Core.Credentials;',
    'using Yubico.YubiKit.Core.Cryptography;', 'using Yubico.YubiKit.Core.Devices;',
    'using Yubico.YubiKit.Core.Sessions;', 'using Yubico.YubiKit.Core.Transports.SmartCard;',
    'using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;',
    'using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;',
    'using Yubico.YubiKit.Piv;', 'using Yubico.YubiKit.Management;',
    'using Yubico.YubiKit.Cli.Shared.Output;',
    'using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;',
    'using Certificates = Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Certificates;',
]


def fence(path, index):
    return next(key for key, (kind, item) in units.items() if kind == 'fence' and item['path'] == path and item['index'] == index)


def xml(path, ordinal):
    candidates = [key for key, (kind, item) in units.items() if kind == 'xml' and item['path'] == path and item['tag'] == 'code']
    return candidates[ordinal - 1]


def marker(identity):
    return '{{unit:' + identity + '}}'


def add(name, identities, template, prerequisites, lifetimes, context='consumer', changes=None):
    require(len(identities) == len(set(identities)) and not any(identity in selected for identity in identities), 'Unique complete-body group membership')
    body_changes = changes or {}
    # Hoist every literal using directive to the genuine host import list.
    for identity in identities:
        body = units[identity][1]['body']
        for directive in re.findall(r'^using (?:[A-Za-z_][\w.]*|[A-Za-z_]\w*\s*=\s*[A-Za-z_][\w.]*)[ \t]*;\n', body, re.M):
            if directive.strip() not in imports:
                imports.append(directive.strip())
            body_changes.setdefault(identity, []).append(dict(before=directive, after='', count=body.count(directive), reason='Hoist the complete original import directive to compilation-unit scope while retaining the complete original body.'))
    groups.append(dict(name=name, context=context, units=identities, template=template,
                       prerequisites=prerequisites, resource_lifetimes=lifetimes,
                       transform='Insert complete original bodies in a typed application-owned task; hoist imports and record each bounded adaptation. Real SDK/sample helpers remain referenced project APIs.',
                       body_changes=body_changes))
    selected.update({identity: name for identity in identities})


def method(name, identity, parameters, prefix='', suffix='', secrets=(), prerequisites=None, lifetimes=None, return_type='Task'):
    class_name = ''.join(word.title() for word in name.split('-'))
    content = prefix + '\n' + marker(identity) + '\n' + suffix
    if secrets:
        content = 'try\n{\n' + content + '\n}\nfinally\n{\n' + '\n'.join('    CryptographicOperations.ZeroMemory(' + secret + ');' for secret in secrets) + '\n}'
    template = 'public static class ' + class_name + '\n{\npublic static async ' + return_type + ' RunAsync(' + parameters + ')\n{\n' + content + '\n}\n}\n'
    add(name, [identity], template, prerequisites or ['Caller supplies the real device/session and task-specific provisioning described beside this body.'],
        lifetimes or ['Await all operations; preserve original session/certificate scopes. The wrapper clears explicitly supplied caller-owned secret arrays in finally; other inputs stay borrowed.'])


README = 'src/Piv/README.md'
CLAUDE = 'src/Piv/CLAUDE.md'
method('readme-discovery', fence(README, 2), '')
method('readme-generation', fence(README, 3), 'IYubiKey device', prerequisites=['Authorized provisioned device; the installed 24-byte Triple-DES/AES-192 key equals the explicitly shown public evaluation value.'], lifetimes=['The original task owns/clears managementKey and disposes its session. Generated IPublicKey is public metadata without IDisposable.'])
method('readme-certificate', fence(README, 4), 'IYubiKey device, byte[] managementKey, CancellationToken cancellationToken', prefix='await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);\nawait session.AuthenticateAsync(managementKey, cancellationToken);', secrets=('managementKey',), prerequisites=['Authorized slot and matching issuer-provided authentication.cer; caller supplies the installed management key.'])
method('readme-host-signature', fence(README, 5), 'IYubiKey device', prerequisites=['Provisioned P-256 Authentication slot, current illustrative PIN, metadata-capable firmware.'], lifetimes=['Original session/verifier scopes and PIN/digest/owned mutable signature finally cleanup are preserved.'])
method('readme-known-plaintext', fence(README, 6), 'IYubiKey device', prerequisites=['Provisioned matching RSA Key Management certificate/key, metadata-capable firmware, current illustrative PIN.'], lifetimes=['Original certificate/RSA/session scopes and PIN/known-plaintext/owned mutable plaintext finally cleanup are preserved.'])

def protected(name, identity, method_name):
    template = '''public static class CLASS
{
public static async Task RunAsync(IYubiKey device, byte[] pin, byte[] activeManagementKey, KeyType activeManagementKeyType, CancellationToken cancellationToken)
{
    try
    {
        if (activeManagementKey.Length != 24 || activeManagementKeyType is not (KeyType.TripleDes or KeyType.Aes192))
            throw new ArgumentException("Supply the installed 24-byte Triple-DES or AES-192 key and its known algorithm.", nameof(activeManagementKey));
        await METHOD(device, pin, activeManagementKey, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(pin);
        CryptographicOperations.ZeroMemory(activeManagementKey);
    }
}
public BODY
}
'''.replace('CLASS', ''.join(word.title() for word in name.split('-'))).replace('METHOD', method_name).replace('BODY', marker(identity))
    add(name, [identity], template,
        ['Authorized dedicated device; current PIN and already installed 24-byte active key with explicitly known Triple-DES/AES-192 algorithm; independent secure backup retained through recovery.'],
        ['The wrapper calls the full composable method, keeps input arrays alive through all awaits and clears caller-owned PIN/key arrays in finally. Setup/reopened sessions retain original disposal order; partial device mutations remain possible.'])

protected('readme-protected-roundtrip', fence(README, 7), 'ProtectedAdministrationRoundTripAsync')
touch_class, touch_use = fence(README, 8), fence(README, 9)
add('readme-touch-callback', [touch_class, touch_use], 'public static class ReadmeTouchCallback\n{\n' + marker(touch_class) + '\npublic static async Task RunAsync(IYubiKey device)\n{\n' + marker(touch_use) + '\n}\n}\n',
    ['Real PIV device; complete original IUserPresencePrompt implementation and its session-registration body are grouped together.'],
    ['The application owns the retained TouchPrompt; original await-using closes the session before the enclosing task exits.'])
method('factory-owned-connection', fence(CLAUDE, 1), 'IYubiKey device, CancellationToken cancellationToken')
method('factory-borrowed-connection', fence(CLAUDE, 2), 'IYubiKey device, CancellationToken cancellationToken', lifetimes=['The original task opens/disposes the caller-owned connection and its borrowing session in reverse declaration order.'])
store = fence(CLAUDE, 4)
add('store-authentication-callback', [store], 'public static class StoreAuthenticationCallback\n{\npublic ' + marker(store) + '\n}\n',
    ['Real open IPivSession; caller supplies Action<Memory<byte>> that fills all 24 bytes from the real active Triple-DES/AES-192 credential store.'],
    ['Preserve full callback method and pooled buffer finally cleanup; callback/session are borrowed, and the task owns its rented storage.'])
annotation = fence(CLAUDE, 6)
add('metadata-test-annotation', [annotation], 'public sealed class MetadataTestAnnotation\n{\n' + marker(annotation) + '\n}\n',
    ['Actual Tests.Shared WithYubiKey attribute, YubiKeyTestState, DeviceCapabilities and xUnit v2/skippable theory packages.'],
    ['Original annotated method owns its created session; compilation leaves test/device execution separate.'], context='annotations')

sample = 'src/Piv/examples/PivTool/PivExamples/'
for ordinal, label in ((1, 'slot-attestation'), (2, 'f9-retrieval')):
    prefix = 'await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);' if ordinal == 2 else ''
    method('tool-' + label, xml(sample + 'Attestation.cs', ordinal), 'IYubiKey device, CancellationToken ct', prefix=prefix,
           prerequisites=['Actual public PivTool Attestation helper; authorized generated slot for slot attestation or retained F9 certificate for retrieval. F9 result uses AttestationCertificate; JSON field is attestationCertificate and intermediateCertificate stays null.'],
           lifetimes=['Original returned certificate property using declarations each retain one owner. Retrieval establishes result shape, with trust and key-binding decisions left to the application.'])
for ordinal, label in enumerate(('certificate-read', 'certificate-import', 'certificate-export', 'certificate-delete', 'certificate-self-sign', 'certificate-csr'), 1):
    secrets = ('managementKey', 'pin') if ordinal == 5 else ('managementKey',) if ordinal in (2, 4) else ('pin',) if ordinal == 6 else ()
    extra = ''.join(', byte[] ' + name for name in secrets)
    method('tool-' + label, xml(sample + 'Certificates.cs', ordinal), 'IYubiKey device' + extra + ', CancellationToken ct', secrets=secrets,
           prerequisites=['Actual public Certificates helper from the real PivTool project, matching provisioned slot/certificate inputs and current credentials; self-sign/CSR delegates to the original device-backed X509SignatureGenerator implementations.'],
           lifetimes=['Preserve the complete original task and returned Certificate using scope; supplied caller-owned credential arrays are cleared after all awaited work. Internal signature generators compile in the complete actual shipping project.'])
method('tool-pin-verify', xml(sample + 'PinManagement.cs', 1), 'IYubiKey device, CancellationToken ct')
for ordinal, label in ((2, 'pin-change'), (3, 'pin-unblock'), (4, 'management-authenticate')):
    method('tool-' + label, xml(sample + 'PinManagement.cs', ordinal), 'IYubiKey device, CancellationToken ct', prefix='await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);',
           prerequisites=['Authorized device; supplied illustrative PIN/PUK values are current, or the shown 24-byte management default is installed with Triple-DES/AES-192 as locally qualified.'],
           lifetimes=['Keep original PIN/PUK/key finally cleanup and wrapper-owned session disposal.'])
method('tool-management-rotation', xml(sample + 'PinManagement.cs', 5), 'IYubiKey device, byte[] currentKey, byte[] newKey, CancellationToken ct',
       prefix='await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);\nif (currentKey.Length != 24 || newKey.Length != 24)\n    throw new ArgumentException("Supply the active and backed-up replacement 24-byte Triple-DES keys.", nameof(newKey));', secrets=('currentKey', 'newKey'),
       prerequisites=['Current Triple-DES management key and backed-up 24-byte replacement; authorized persistent key rotation; application retains an independent backup through fresh-session authentication.'])
method('tool-retry-metadata', xml(sample + 'DeviceInfo.cs', 1), 'IYubiKey device, CancellationToken ct')
method('tool-key-generation', xml(sample + 'KeyGeneration.cs', 1), 'IYubiKey device, byte[] managementKey, CancellationToken ct', secrets=('managementKey',))
method('tool-reset', xml(sample + 'Reset.cs', 1), 'IYubiKey device, bool userConfirmedReset, CancellationToken ct', prerequisites=['Application has authorized destructive reset on this dedicated device; the explicit confirmation boolean is supplied by the caller. Normal reset retains F9.'])
method('tool-signing', xml(sample + 'Signing.cs', 1), 'IYubiKey device, IUserPresencePrompt userPresencePrompt, byte[] pin, ReadOnlyMemory<byte> dataBytes, CancellationToken ct', secrets=('pin',),
       prerequisites=['Provisioned P-256/P-384 Signature slot; application-owned presence prompt, current caller-owned PIN and borrowed input data. Actual generic helper is limited to this ECDSA task.'], lifetimes=['Session and PIN scopes enclose all awaited helper work. Returned read-only sample Signature is inspected without writable casting; underlying sample ownership gaps remain qualified.'])
method('tool-decryption-inspection', xml(sample + 'Decryption.cs', 1), 'IYubiKey device, IUserPresencePrompt userPresencePrompt, byte[] pin, ReadOnlyMemory<byte> encryptedBytes, CancellationToken ct', secrets=('pin',),
       prerequisites=['Provisioned RSA Key Management slot, metadata-capable firmware, configured prompt and matching ciphertext; actual helper chooses its documented padding.'], lifetimes=['The wrapper owns the credential cleanup; DecryptionResult exposes read-only memory. Preserve that existing limitation without writable casts; the README direct-SDK task separately owns and clears mutable plaintext.'])
method('tool-slot-query', xml(sample + 'SlotInfo.cs', 1), 'IYubiKey device, CancellationToken ct', lifetimes=['Original finally disposes every returned slot certificate, including unsuccessful result processing; session closes afterward.'])
method('tool-host-verification', xml(sample + 'Verification.cs', 1), 'IYubiKey device, ReadOnlyMemory<byte> originalData, ReadOnlyMemory<byte> signatureBytes, CancellationToken ct', prerequisites=['Stored certificate already bound to the provisioned Signature key, original data/signature and SHA-256 pairing; inspect both result.Success and result.IsValid.'])
for ordinal, label in ((1, 'rsa-extractor'), (2, 'ecdsa-extractor')):
    method(label, xml('src/Piv/src/SlotMetadataExtensions.cs', ordinal), 'IYubiKey device, CancellationToken cancellationToken', prefix='await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);',
           prerequisites=['Provisioned matching RSA or P-256/P-384 Authentication key on metadata-capable firmware.'], lifetimes=['Original nullable metadata guard, disposable public host key and owned RSA plaintext finally cleanup are retained.'])
method('session-contention-guard', xml('src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSessionContentionTests.cs', 1), 'IYubiKey key, byte[] pin, byte[] digest, CancellationToken ct', secrets=('pin', 'digest'), prerequisites=['Provisioned P-256 Authentication key and matching 32-byte digest/current PIN; actual Management extension and ConnectionInUseException are referenced.'], lifetimes=['Retain original connection-exclusion/signature body and clear its mutable signature; wrapper clears supplied PIN/digest after the original session closes.'])

MIGRATION = 'docs/migration/v1-to-v2.md'
method('migration-factory', fence(MIGRATION, 10), 'IYubiKey device, CancellationToken cancellationToken')
method('migration-generation', fence(MIGRATION, 12), 'IYubiKey device, CancellationToken cancellationToken', prerequisites=['Installed public evaluation management value and authorized P-256 slot; the full current-v2 body binds to current KeyType/KeyCreationOptions.'])
protected('migration-protected-enable', fence(MIGRATION, 22), 'EnableProtectedAdministrationAsync')
RAW = 'docs/architecture/raw-access-tiers.md'
method('shared-piv-tier', fence(RAW, 1), 'IYubiKey yubiKey, CancellationToken cancellationToken', suffix='Console.WriteLine(metadata.RetriesRemaining);')
raw_parameters = 'IYubiKey yubiKey, FirmwareVersion firmwareVersion, ReadOnlyMemory<byte> applicationId, int cla, int ins, int p1, int p2, ReadOnlyMemory<byte> commandData, CancellationToken cancellationToken'
method('raw-smartcard-owned', fence(RAW, 2), raw_parameters, suffix='try\n{\n    Console.WriteLine($"APDU status: {response.SW1:X2}{response.SW2:X2}");\n}\nfinally\n{\n    response.Clear();\n}', lifetimes=['Original raw session owns its hidden connection; wrapper clears the returned response. Caller retains commandData storage until completion and clears sensitive source data in its own enclosing scope.'])
method('raw-smartcard-borrowed', fence(RAW, 3), 'IYubiKey yubiKey, FirmwareVersion firmwareVersion, ReadOnlyMemory<byte> applicationId, ApduCommand command, CancellationToken cancellationToken', lifetimes=['Original caller-owned connection spans sequential borrowing raw/PIV sessions. Caller retains and clears sensitive ApduCommand.Data after awaited completion.'])
raw_scp = fence(RAW, 7)
method('raw-scp-parameters', raw_scp, 'IYubiKey yubiKey, FirmwareVersion firmwareVersion, ReadOnlyMemory<byte> applicationId, ApduCommand command, Func<ScpKeyParameters> loadScpParameters, CancellationToken cancellationToken', prefix='ScpKeyParameters LoadScpParametersFromSecureStorage() => loadScpParameters();', suffix='try\n{\n    Console.WriteLine($"APDU status: {response.SW1:X2}{response.SW2:X2}");\n}\nfinally\n{\n    response.Clear();\n}',
       prerequisites=['Caller supplies a real secure-store delegate producing owned configured ScpKeyParameters matched to the device; firmware/application/command parameters are typed actual SDK inputs.'], lifetimes=['The original using disposes the owned parameters after raw session disposal. The caller retains command input memory; response cleanup is explicit.'])
raw_migration = fence(RAW, 8)
old = units[raw_migration][1]['body'].split('// After\n')[0]
template = 'public static class RawMigrationCurrent\n{\npublic static async Task RunAsync(IYubiKey device, ReadOnlyMemory<byte> applicationId, CancellationToken cancellationToken)\n{\nawait using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);\n' + marker(raw_migration) + '\n}\n}\n'
add('raw-migration-current', [raw_migration], template, ['Current raw-session migration half with an actual caller-owned SmartCard connection.'], ['Preserve the complete historical/current original; compile its current After half. Caller-owned connection encloses the borrowing raw session.'], changes={raw_migration: [dict(before=old, after='', count=1, reason='The Before half documents the removed legacy protocol API; retain it raw and compile the complete current After half only.')]})

INTERACTION = 'docs/usage/user-interaction.md'
method('shared-console-prompt', fence(INTERACTION, 1), 'IYubiKey yubiKey, CancellationToken cancellationToken', lifetimes=['Actual ConsoleUserPresencePrompt singleton remains application/shared owned; original session scope is retained.'])
wording, intent_callback = fence(INTERACTION, 2), fence(INTERACTION, 4)
add('shared-prompt-callbacks', [wording, intent_callback], 'public sealed class SharedPromptCallbacks : IUserPresencePrompt\n{\npublic static string Describe(UserPresenceContext context)\n{\n' + marker(wording) + '\nreturn message;\n}\n' + marker(intent_callback) + '\n}\n',
    ['Actual UserPresenceContext/operation constants, complete shared request callback and display-message switch; no applet keyword filter.'], ['Prompt remains caller owned; ValueTask callback returns promptly with original operation/intent wording.'])
method('shared-intent-await', fence(INTERACTION, 3), 'Yubico.YubiKit.WebAuthn.Client.WebAuthnClient client, Yubico.YubiKit.WebAuthn.Client.Authentication.PublicKeyCredentialRequestOptions requestOptions, CancellationToken cancellationToken',
       prerequisites=['Existing real WebAuthnClient and relying-party request options supplied by the application; retained as shared intent/callback guidance, with actual WebAuthn project reference.'], lifetimes=['Client/request are borrowed, the actual awaited SDK operation stays inside the original using intent scope. Compilation adds no WebAuthn or PIV hardware execution claim.'])

dispositions = []
for identity, (kind, item) in units.items():
    if identity in selected:
        dispositions.append(dict(id=identity, status='compile', group=selected[identity], reason='Complete current body compiled in the named typed task with original text/hash retained.'))
    elif kind == 'fence':
        if item['path'] == MIGRATION and item['index'] in (9, 11, 21):
            status, reason = 'historical', 'Historical v1 half paired with the current PIV migration recipe; original body remains retained and outside current-v2 compilation.'
        elif item['path'] == RAW and item['index'] in (4, 5, 6):
            status, reason = 'other-applet', 'FIDO/OTP command and CRC guidance belongs to other applets; preserve the complete shared body outside PIV task compilation.'
        elif item['path'].startswith('src/Piv/') and item['language'] not in ('cs', 'c#', 'csharp'):
            status, reason = ('non-executable', 'Protocol-flow diagram retained as explanatory structure.') if item['language'] == 'text' else ('non-csharp', 'Complete shell command example retained; command/device execution stays separately authorized.')
        else:
            status, reason = 'other-applet', 'Shared migration/reference fence outside the six assigned PIV recipes; retain without expanding the PIV module audit.'
        dispositions.append(dict(id=identity, status=status, reason=reason))
    elif kind == 'xml' and item['tag'] == 'example':
        dispositions.append(dict(id=identity, status='non-executable', reason='XML example container; its complete nested code body has its own compile disposition, preserving a single task denominator.'))
    elif kind in ('inline', 'xml'):
        dispositions.append(dict(id=identity, status='symbolic', reason='Inline API/type/value/contract expression in its surrounding prose; meaningful executable fences/XML code are separately compiled in complete typed tasks.'))
    else:
        dispositions.append(dict(id=identity, status='symbolic', reason='Declarative migration signature/path mapping; current executable counterpart is retained as a complete migration task.'))
plan = dict(baseline=data.get('baseline', '727fe43afc430c5d8364d868a6436de50626e476'), inventory_sha256=sha(inventory_path.read_bytes()),
            helpers_reviewed_as_application_code=True,
            imports=dict(consumer=imports, annotations=['using System.Threading.Tasks;', 'using Yubico.YubiKit.Core.Devices;', 'using Yubico.YubiKit.Piv;', 'using Xunit;', 'using Yubico.YubiKit.Tests.Shared;', 'using Yubico.YubiKit.Tests.Shared.Infrastructure;']),
            additional_project_references=dict(consumer=['src/Piv/examples/PivTool/PivTool.csproj', 'src/Management/src/Yubico.YubiKit.Management.csproj', 'src/Cli.Shared/src/Yubico.YubiKit.Cli.Shared.csproj', 'src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj'], annotations=[]),
            groups=groups, dispositions=dispositions)
target = HERE / 'final-plan-v2.json'
raw = (json.dumps(plan, indent=2) + '\n').encode()
require(not (HERE.parent.parent / 'record-manifest.json').exists(), 'Run is frozen')
if target.exists():
    require(target.read_bytes() == raw, 'Preserve changed existing final plan')
else:
    with target.open('xb') as output:
        output.write(raw)
print(f'Prepared {len(groups)} explicit typed groups covering {len(selected)} complete current bodies; {len(dispositions)} total current-body/token/mapping dispositions. Compilation remains coordinator work.')
