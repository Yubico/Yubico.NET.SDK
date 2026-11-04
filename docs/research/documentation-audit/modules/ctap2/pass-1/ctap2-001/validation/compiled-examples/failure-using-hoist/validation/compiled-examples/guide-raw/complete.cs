using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Pin;
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.Cbor;

namespace AuditCt001.Prepared.guide_raw;

public static class Example
{
public static async Task ExampleAsync(ICtap2Session session, IPinUvAuthProtocol protocol)
{
var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
    .WithInt(1, protocol.Version)
    .WithInt(2, ClientPinSubCommand.GetRetries)
    .Build();

var response = await session.SendCborRequestAsync(request);
}
}
