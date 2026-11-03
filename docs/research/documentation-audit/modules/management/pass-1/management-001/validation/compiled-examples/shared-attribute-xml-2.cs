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


namespace ManagementSnippet_shared_attribute_xml_2;

public class DocumentationExample
{
    public class ProductionKeysOnly : IYubiKeyFilter
    {
        public bool Matches(YubiKeyTestState device) => device.SerialNumber > 10_000_000;
        public string GetDescription() => "Production keys (SN > 10000000)";
    }

    [SkippableTheory]
    [WithYubiKey(CustomFilter = typeof(ProductionKeysOnly))]
    public async Task ReadSelectedRange(YubiKeyTestState device)
    {
        var info = await device.Device.GetDeviceInfoAsync();
        Assert.Equal(device.SerialNumber, info.SerialNumber);
    }
}
