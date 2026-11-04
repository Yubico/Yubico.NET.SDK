using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2;
using System.Linq;

namespace AuditCt001.Prepared.xml_info;

public static class Example
{
public static async Task ExampleAsync(IYubiKey yubiKey)
{
var info = await yubiKey.GetCtap2InfoAsync();
Console.WriteLine($"AAGUID: {Convert.ToHexString(info.Aaguid.Span)}");
Console.WriteLine($"Supports CTAP2.1: {info.Versions.Contains("FIDO_2_1")}");
}
}
