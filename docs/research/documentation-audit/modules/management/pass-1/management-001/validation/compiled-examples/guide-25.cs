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


namespace ManagementSnippet_guide_25;

public static class DocumentationExample
{
    public static async Task Example(ManagementSession mgmt)
    {
        var info = await mgmt.GetDeviceInfoAsync();

        // Different version representations
        FirmwareVersion fw = info.FirmwareVersion;           // e.g., 5.7.2
        VersionQualifier qualifier = info.VersionQualifier;  // e.g., 5.7.2.beta.1
        string versionName = info.VersionName;               // Display string

        // VersionQualifierType enum values:
        // - Final: Production release (5.7.2)
        // - Alpha: Alpha prerelease (5.7.2.alpha.1)
        // - Beta: Beta prerelease (5.7.2.beta.1)
    }
}
