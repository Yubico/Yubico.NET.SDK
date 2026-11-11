using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;
namespace AuditIntegration20261111.Example22;
public static class Example
{
public static async Task RunAsync(IYubiKey device)
{

DeviceInfo info = await device.GetDeviceInfoAsync();
int? serial = info.SerialNumber;
FirmwareVersion firmware = info.FirmwareVersion;

}
}
