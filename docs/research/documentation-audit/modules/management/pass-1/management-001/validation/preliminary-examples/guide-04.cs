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
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;


namespace ManagementSnippet_guide_04;

public static class DocumentationExample
{
    public static void Example(YubiKeyTestState state)
    {
        // Composable fragment inside a test receiving YubiKeyTestState state.
        int? serial = state.SerialNumber;
        FirmwareVersion fw = state.FirmwareVersion;
        FormFactor form = state.FormFactor;
        bool isUsb = state.IsUsbTransport;
        bool isNfc = state.IsNfcTransport;
        IYubiKey device = state.Device;

        // Capability checks
        bool hasPiv = state.HasCapability(DeviceCapabilities.Piv);

        // Firmware version checks
        bool isModern = state.FirmwareVersion.IsAtLeast(5, 3, 0);
    }
}
