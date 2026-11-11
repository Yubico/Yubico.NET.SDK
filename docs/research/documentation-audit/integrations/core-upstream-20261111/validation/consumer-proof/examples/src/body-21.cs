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
namespace AuditIntegration20261111.Example21;
public static class Example
{
public static void Run(IYubiKey device)
{
DeviceInfo? lastKnown = device.CachedDeviceInfo; // No I/O or presence check.

}
}
