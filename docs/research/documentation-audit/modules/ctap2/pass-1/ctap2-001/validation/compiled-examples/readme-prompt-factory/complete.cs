using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Core.Credentials;

namespace AuditCt001.Prepared.readme_prompt_factory;

public static class Example
{
public static async Task ExampleAsync(IYubiKey device)
{
// Replace the original session factory above; do not open a second session while it is live.
await using var session = await device.CreateCtap2SessionAsync(
    new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() });
}
}

sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
