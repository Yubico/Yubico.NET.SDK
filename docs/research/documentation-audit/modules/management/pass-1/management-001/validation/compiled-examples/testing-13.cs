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


namespace ManagementSnippet_testing_13;

public class DocumentationExample
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
    public async Task GetDeviceInfo_UsesRequestedManagementConnection(YubiKeyTestState state)
    {
        Assert.True(state.ConnectionType is
            ConnectionType.SmartCard or ConnectionType.HidFido or ConnectionType.HidOtp);

        await state.WithManagementAsync(async (mgmt, _) =>
        {
            Assert.Equal(state.ConnectionType, mgmt.ConnectionType);

            var info = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(state.SerialNumber, info.SerialNumber);
        }, preferredConnection: state.ConnectionType);
    }
}
