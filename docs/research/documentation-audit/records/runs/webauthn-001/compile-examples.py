from pathlib import Path
import re
import subprocess

repo = Path('/home/rupicola/Code/y/Yubico.Yubikit.NET')
out = Path(__file__).parent / 'compiled-examples'
out.mkdir(exist_ok=True)
checker = Path(__file__).parent / 'validation/checker/src/bin/Release/net10.0/Yubico.YubiKit.WebAuthn.DocCheck.dll'

def blocks(path):
    return re.findall(r'```csharp\n(.*?)```', (repo / path).read_text(), re.S)

readme = blocks('src/WebAuthn/README.md')
globals = '\n'.join('global using ' + ns + ';' for ns in [
    'System', 'System.Collections.Generic', 'System.Linq', 'System.Threading',
    'System.Threading.Tasks', 'System.IO', 'System.Net.Http']) + '\n'
demo = readme[0]
usings = '\n'.join(line for line in demo.splitlines() if line.startswith('using ')) + '\n'
original_factory = 'await using var client = await device.CreateWebAuthnClientAsync(origin, clientOptions);'
assert original_factory in demo
prompted = demo.replace(original_factory, readme[2] + readme[4])
prompted = prompted.replace('pinBytes: demoPin', 'pinBytes: null')
stub = '\nstatic Task<byte[]> ReadPinAsync(CredentialPromptContext context, CancellationToken cancellationToken) => throw new NotImplementedException("Application-supplied input; compile only.");\n'
sources = {
    'README-demo.cs': globals + demo,
    'README-prompt-and-touch.cs': globals + prompted + stub + readme[1] + readme[3],
}

intent = next(block for block in blocks('docs/usage/user-interaction.md') if 'await client.GetAssertionAsync' in block)
xml = (repo / 'src/Core/src/Credentials/UserPresenceIntent.cs').read_text()
xml_code = re.search(r'///\s*<code>(.*?)///\s*</code>', xml, re.S).group(1)
xml_code = re.sub(r'^\s*///\s?', '', xml_code, flags=re.M)
sign = next(block for block in blocks('src/WebAuthn/CLAUDE.md') if 'var registrationExtensions' in block)
sources['configuration-and-shared-examples.cs'] = globals + usings + '''
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
internal interface IRpValidation
{
    bool IsRpValidForOrigin(WebAuthnOrigin origin, string rpId);
}
internal static class DocumentationFragments
{
    internal static async Task Intent(WebAuthnClient client, PublicKeyCredentialRequestOptions requestOptions, CancellationToken cancellationToken)
    {
''' + intent + '''
    }
    internal static async Task XmlIntent(WebAuthnClient client, PublicKeyCredentialRequestOptions requestOptions, CancellationToken cancellationToken)
    {
''' + xml_code + '''
    }
    internal static WebAuthnClientOptions Allowlist()
    {
''' + readme[5] + '''
        return allowlistOptions;
    }
    internal static WebAuthnClientOptions Service(IRpValidation rpValidationService)
    {
''' + readme[6] + '''
        return serviceOptions;
    }
    internal static void Signing(GeneratedSigningKey generatedKey, ReadOnlyMemory<byte> credentialId, ReadOnlyMemory<byte> toBeSigned, ReadOnlyMemory<byte>? algorithmSpecificAdditionalArgs)
    {
''' + sign + '''
    }
}
'''

for name, source in sources.items():
    path = out / name
    path.write_text(source)
    subprocess.run(['dotnet', str(checker), 'compile', str(path), str(repo)], check=True)
print('PASS: three compile-only source groups, extracted from current documentation; application prerequisites supplied only as typed stubs/parameters. No demo, authenticator, or RP operation executed.')
