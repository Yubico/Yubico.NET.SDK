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
namespace Audit20261111.DocExamples.External235;
public sealed class DocumentationBody
{
internal async Task CompileOnly()
{
await using var connection = new RecordingSmartCardConnection([0x90, 0x00]);
byte[] command = [0x00, 0xA4, 0x04, 0x00, 0x00]; // Nonsecret synthetic fixture.
ReadOnlyMemory<byte> response = await connection.TransmitAndReceiveAsync(command);

Assert.Equal(command, Assert.Single(connection.TransmittedCommands));
Assert.Equal(new byte[] { 0x90, 0x00 }, response.ToArray());
}
}
