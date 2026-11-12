using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Xunit;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;

namespace Audit20261111.DocExamples.Test60;
public class ProtocolConfigurationExamples
{
    [Fact]
    public void Configure_FirmwareBelow400_UsesNeoMaxSize()
    {
        using var connection = new FakeSmartCardConnection { SupportsExtendedApduValue = false };
        using var protocol = new PcscProtocol(connection);

        protocol.Configure(new FirmwareVersion(3, 5, 0));

        Assert.Equal(1390, protocol.MaxApduSize); // Complete-command ceiling, not a short data chunk.
    }
}
