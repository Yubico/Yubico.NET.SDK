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


namespace ManagementSnippet_guide_24;

public static class DocumentationExample
{
    public static void Example()
    {
        // Flags enum - use bitwise operations
        var capabilities = DeviceCapabilities.Piv | DeviceCapabilities.Oath;

        // Check if specific capability is set
        bool hasPiv = (capabilities & DeviceCapabilities.Piv) != 0;

        // Add capability
        capabilities |= DeviceCapabilities.Fido2;

        // Remove capability
        capabilities &= ~DeviceCapabilities.Otp;

        // Check multiple
        bool hasPivAndOath = (capabilities & (DeviceCapabilities.Piv | DeviceCapabilities.Oath))
            == (DeviceCapabilities.Piv | DeviceCapabilities.Oath);
    }
}
