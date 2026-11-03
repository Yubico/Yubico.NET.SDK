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


namespace ManagementSnippet_guide_05;

public static class IYubiKeyExtensions
{
    // C# 14 syntax: extension(Type param) defines extensions for Type
    extension(IYubiKey yubiKey)
    {
        // Methods here extend IYubiKey
        public async Task<DeviceInfo> GetDeviceInfoAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // 'yubiKey' parameter is implicitly the extension target
            await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(options, cancellationToken);
            return await mgmtSession.GetDeviceInfoAsync(cancellationToken);
        }
    }
}
