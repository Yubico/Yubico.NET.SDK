using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2.Extensions;

namespace AuditCt001.Prepared.xml_output;

public static class Example
{
public static void Run(ReadOnlyMemory<byte> extensionCbor)
{
var output = ExtensionOutput.Decode(extensionCbor);
if (output.TryGetCredProtect(out var policy))
{
    Console.WriteLine($"Credential protection: {policy}");
}
}
}
