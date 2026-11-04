using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

namespace AuditCt001.Prepared.xml_session_alternatives;

public static class Example
{
public static async Task ExampleAsync(IYubiKey yubiKey)
{
// Ordinary session: dispose before opening another connection on this physical key.
{
    await using var session = await yubiKey.CreateCtap2SessionAsync();
    var info = await session.GetInfoAsync();
}

// Alternative: SCP03 over SmartCard on a device provisioned for these keys.
using var scpKeys = Scp03KeyParameters.Default;
await using var secureSession = await yubiKey.CreateCtap2SessionAsync(
    new SessionCreationOptions
    {
        ScpKeyParameters = scpKeys,
        PreferredConnectionType = ConnectionType.SmartCard
    });
}
}
