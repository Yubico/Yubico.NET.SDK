using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Management;


namespace ManagementSnippet_physical_transports;

public static class DocumentationExample
{
    public static async Task Example(IYubiKey device)
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
