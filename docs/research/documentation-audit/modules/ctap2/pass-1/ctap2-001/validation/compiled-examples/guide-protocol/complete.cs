using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Ctap2.Pin;

namespace AuditCt001.Prepared.guide_protocol;

public static class Example
{
public static void Run()
{
using var protocol = new PinUvAuthProtocolV2();
// Dispose() only marks the instance disposed; it holds no key material to release.
}
}
