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
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.Oath;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Audit20261111.DocExamples.Full;
internal static class Body153
{
internal static async Task CompileOnly(IYubiKey device)
{
await using (var smartCard = await device.ConnectAsync<ISmartCardConnection>())
{
    // Use SmartCard, then dispose it before opening another interface.
}

await using (var fido = await device.ConnectAsync<IFidoHidConnection>())
{
    // Use FIDO HID, then dispose it before opening another interface.
}

await using (var otp = await device.ConnectAsync<IOtpHidConnection>())
{
    // Use OTP HID.
}
}

}
