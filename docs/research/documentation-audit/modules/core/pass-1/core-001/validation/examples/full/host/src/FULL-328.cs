using System.Buffers;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;
using Yubico.YubiKit.YubiOtp;

namespace Audit20261111.DocExamples.Full;
internal static class Body328
{
internal static async Task CompileOnly()
{
IYubiKey device = await YubiKeyManager.FindFirstAsync();
{
    await using var session = await device.CreateYubiOtpSessionAsync();
    ConfigState state = session.GetConfigState();
    Console.WriteLine($"OTP applet firmware {state.FirmwareVersion}");
}
}

}
