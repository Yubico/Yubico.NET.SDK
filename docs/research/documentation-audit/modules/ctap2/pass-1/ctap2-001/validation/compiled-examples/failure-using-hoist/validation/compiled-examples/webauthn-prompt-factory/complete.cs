using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;

namespace AuditCt001.Prepared.webauthn_prompt_factory;

public static class Example
{
public static async Task ExampleAsync(IYubiKey device, WebAuthnOrigin origin, WebAuthnClientOptions clientOptions)
{
// Alternative factory call: replace the demo's call; do not append another live client.
await using var client = await device.CreateWebAuthnClientAsync(
    origin, clientOptions, sessionOptions: new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() });
}
}

sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
