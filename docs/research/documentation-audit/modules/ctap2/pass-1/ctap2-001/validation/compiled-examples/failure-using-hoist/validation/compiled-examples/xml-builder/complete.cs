using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2.Extensions;

namespace AuditCt001.Prepared.xml_builder;

public static class Example
{
public static void Run(ReadOnlyMemory<byte> myBlobData)
{
var extensions = new ExtensionBuilder()
    .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
    .WithCredBlob(myBlobData)
    .Build();
}
}
