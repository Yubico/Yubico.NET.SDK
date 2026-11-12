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
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Audit20261111.DocExamples.Full;
internal static class Body414
{
internal static async Task CompileOnly()
{
// Explicit compatibility opt-in, before any discovery or monitoring:
SmartCardConnectionFactory.DefaultSharing = SmartCardSharing.ExclusiveWithSharedFallback;
var devices = await YubiKeyManager.FindAllAsync();
}

}
