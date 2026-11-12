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
internal static class Body251
{
internal static async Task CompileOnly(IYubiKey yubiKey, CancellationToken cancellationToken)
{
await using var mgmt = await yubiKey.CreateManagementSessionAsync(cancellationToken: cancellationToken);
var info = await mgmt.GetDeviceInfoAsync(cancellationToken);
var remaining = info.UsbEnabled & ~DeviceCapabilities.Otp;
if ((remaining & info.UsbSupported) == DeviceCapabilities.None)
{
    throw new InvalidOperationException("Preserve a supported USB application.");
}
await mgmt.SetDeviceConfigAsync(
    new DeviceConfig { UsbEnabled = remaining },
    new SetDeviceConfigOptions { Reboot = true },
    cancellationToken);
// End this scope; rediscover the intended key and open a fresh connection after reboot.
}

}
