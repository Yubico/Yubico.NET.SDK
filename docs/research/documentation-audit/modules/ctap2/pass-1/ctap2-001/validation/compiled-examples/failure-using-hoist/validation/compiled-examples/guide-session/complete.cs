using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2;

namespace AuditCt001.Prepared.guide_session;

public static class Example
{
public static async Task ExampleAsync(IYubiKey yubiKey)
{
// With IYubiKey yubiKey in an async method; this factory owns the opened connection.
await using var session = await yubiKey.CreateCtap2SessionAsync();
}
}
