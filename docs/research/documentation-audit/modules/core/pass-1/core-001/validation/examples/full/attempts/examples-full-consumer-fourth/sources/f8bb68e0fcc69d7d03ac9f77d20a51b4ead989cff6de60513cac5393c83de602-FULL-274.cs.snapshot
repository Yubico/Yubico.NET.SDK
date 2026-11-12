using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Management;

namespace Audit20261111.DocExamples.Full;
internal static class Body274
{
internal static async Task CompileOnly(IYubiKey device)
{
var info = await device.GetDeviceInfoAsync();
var requested = DeviceCapabilities.Piv | DeviceCapabilities.Oath;
if ((info.UsbSupported & requested) != requested)
{
    throw new NotSupportedException("This key does not support both requested USB applications.");
}
var config = new DeviceConfig { UsbEnabled = requested };

await using (var session = await device.CreateManagementSessionAsync())
{
    await session.SetDeviceConfigAsync(config, new SetDeviceConfigOptions { Reboot = true });
}

// The session above is disposed. This pause is illustrative, not an enumeration guarantee.
await Task.Delay(TimeSpan.FromSeconds(3));
var rebooted = await YubiKeyManager.FindAllAsync(); // Every Find performs fresh discovery.
}

}
