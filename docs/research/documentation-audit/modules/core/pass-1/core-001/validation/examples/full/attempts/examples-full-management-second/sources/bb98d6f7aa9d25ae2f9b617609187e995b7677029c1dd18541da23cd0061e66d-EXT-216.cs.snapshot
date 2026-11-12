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
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Xunit;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.Management;
namespace Audit20261111.DocExamples.External216;
public sealed class DocumentationBody
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
