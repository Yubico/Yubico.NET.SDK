using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;

namespace AuditCt001.Prepared.shared_intent_task;

public static class Example
{
public static async Task ExampleAsync(WebAuthnClient client, PublicKeyCredentialRequestOptions requestOptions, CancellationToken cancellationToken)
{
using (UserPresenceIntent.BeginScope("approve the transfer of 500 EUR"))
{
    await client.GetAssertionAsync(requestOptions, cancellationToken: cancellationToken);
}
}
}
