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


namespace ManagementSnippet_testing_11;

public class DocumentationExample
{
    // One device row; state.ConnectionType may contain multiple flags.
    [SkippableTheory]
    [WithYubiKey]
    public async Task DefaultSelection(YubiKeyTestState state)
    {
        var info = await state.Device.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    }

    // One row whose device must expose SmartCard.
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SmartCardOnly(YubiKeyTestState state)
    {
        var info = await state.Device.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    }

    // Three separate rows, one for each requested connection type.
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
    public async Task EachManagementConnection(YubiKeyTestState state)
    {
        // This demonstrates independent device-filter rows, not operation-transport coverage.
        var info = await state.Device.GetDeviceInfoAsync();
        Assert.Equal(state.SerialNumber, info.SerialNumber);
    }
}
