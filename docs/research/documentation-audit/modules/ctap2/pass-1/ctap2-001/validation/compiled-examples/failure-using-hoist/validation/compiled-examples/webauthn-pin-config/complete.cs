using System;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.WebAuthn.Client;

namespace AuditCt001.Prepared.webauthn_pin_config;

public static class Example
{
public static void Run(WebAuthnClientOptions clientOptions, Func<CredentialPromptContext, CancellationToken, Task<byte[]>> ReadPinAsync)
{
// Alternative configuration: insert before the demo's single CreateWebAuthnClientAsync call.
clientOptions = clientOptions with { CredentialPrompt = new PinPrompt(ReadPinAsync) };
}
}

internal sealed class PinPrompt(Func<CredentialPromptContext, CancellationToken, Task<byte[]>> readPin) : ICredentialPrompt
{
    public async ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
        CredentialPromptContext context, CancellationToken cancellationToken)
    {
        byte[] pin = await readPin(context, cancellationToken);
        try { return DisposableArrayPoolBuffer.CreateFromSpan(pin); }   // exactly sized; ownership transfers
        finally { CryptographicOperations.ZeroMemory(pin); }
    }
}
