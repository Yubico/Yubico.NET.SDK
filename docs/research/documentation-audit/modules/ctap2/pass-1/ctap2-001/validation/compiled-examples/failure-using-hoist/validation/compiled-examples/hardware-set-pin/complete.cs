using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Pin;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;

namespace AuditCt001.Prepared.hardware_set_pin;

public static class Example
{
public static async Task ExampleAsync(ICtap2Session session)
{
// With an existing ICtap2Session session; only on a human-approved test device.
using var protocol = new PinUvAuthProtocolV2();
using var clientPin = new ClientPin(session, protocol);
byte[] pin = Encoding.UTF8.GetBytes(Ctap2TestStateExtensions.KnownTestPinString);
try
{
    await clientPin.SetPinAsync(pin);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}
}
}
