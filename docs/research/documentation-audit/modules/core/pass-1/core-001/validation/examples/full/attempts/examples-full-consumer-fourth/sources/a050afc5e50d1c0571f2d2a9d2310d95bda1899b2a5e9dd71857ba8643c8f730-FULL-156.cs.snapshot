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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Oath;

namespace Audit20261111.DocExamples.Full;
internal static class Body156
{
internal static async Task CompileOnly(IYubiKey device)
{
// Default order (no override): Management prefers SmartCard.
await using (var mgmt = await device.CreateManagementSessionAsync())
{
    var info = await mgmt.GetDeviceInfoAsync();
    Console.WriteLine(info.FirmwareVersion);
}

// Explicit override: force HID OTP for this session.
await using (var otpMgmt = await device.CreateManagementSessionAsync(
    new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp }))
{
    var info = await otpMgmt.GetDeviceInfoAsync();
    Console.WriteLine(info.FirmwareVersion);
}
}

}
