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
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;
namespace Audit20261111.DocExamples.Test204;
public sealed class DocumentationBody204
{
internal void CompileOnly(PcscProtocol protocol, FirmwareVersion firmwareVersion)
{
// Protocol auto-configures based on firmware version
protocol.Configure(firmwareVersion);

// Force short APDUs (for compatibility testing)
protocol.Configure(firmwareVersion, new ProtocolConfiguration { ForceShortApdus = true });
}
}
