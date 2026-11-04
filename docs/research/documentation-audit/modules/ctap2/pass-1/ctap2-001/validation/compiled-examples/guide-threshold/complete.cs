using System;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2;

namespace AuditCt001.Prepared.guide_threshold;

public static class Example
{
public static void Run(FirmwareVersion firmwareVersion)
{
if (!Ctap2Session.FeatureBioEnrollment.IsSupportedByFirmware(firmwareVersion))
{
    throw new NotSupportedException("Bio enrollment requires firmware 5.2+");
}
}
}
