using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
namespace Audit20261111.DocExamples.Test259;
public sealed class DocumentationBody259
{
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
public async Task AutoEject_PersistsAndRestorationIsAttempted(YubiKeyTestState state)
{
    await using var mgmt = await state.Device.CreateManagementSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard });
    var original = await mgmt.GetDeviceInfoAsync();
    Assert.False(original.IsLocked); // Example prerequisite: unlocked dedicated test key.
    Assert.InRange(original.AutoEjectTimeout, (ushort)0, (ushort)3600); // Must be restorable through this SDK.
    ushort changed = original.AutoEjectTimeout == 30 ? (ushort)31 : (ushort)30;
    try
    {
        await mgmt.SetDeviceConfigAsync(new DeviceConfig { AutoEjectTimeout = changed });
        var persisted = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(changed, persisted.AutoEjectTimeout);
    }
    finally
    {
        await mgmt.SetDeviceConfigAsync(new DeviceConfig { AutoEjectTimeout = original.AutoEjectTimeout });
        var restored = await mgmt.GetDeviceInfoAsync();
        Assert.Equal(original.AutoEjectTimeout, restored.AutoEjectTimeout);
    }
}
}
