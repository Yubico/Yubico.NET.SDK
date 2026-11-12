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
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.YubiOtp;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;

namespace Audit20261111.DocExamples.Full;
internal static class Body330
{
internal static async Task CompileOnly(IYubiKey device)
{
byte[] hmacKey = RandomNumberGenerator.GetBytes(20);
try
{
    // Securely enroll hmacKey with your verifier here as part of your application's provisioning.
    await using var session = await device.CreateYubiOtpSessionAsync(
        new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
    using var config = new HmacSha1SlotConfiguration(hmacKey);
    config.RequireTouch();
    config.UseShortChallenge();
    await session.PutConfigurationAsync(Slot.Two, config);
}
finally
{
    CryptographicOperations.ZeroMemory(hmacKey);
}
}

}
