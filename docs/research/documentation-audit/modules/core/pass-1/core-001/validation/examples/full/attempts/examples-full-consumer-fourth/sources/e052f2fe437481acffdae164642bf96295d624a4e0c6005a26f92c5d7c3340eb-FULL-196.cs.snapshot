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
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Oath;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Piv;

namespace Audit20261111.DocExamples.Full;
internal static class Body196
{
internal static async Task CompileOnly(IYubiKey yubiKey, CancellationToken cancellationToken)
{
var options = new SessionCreationOptions
{
    UserPresencePrompt = ConsoleUserPresencePrompt.Instance
};

await using PivSession session = await yubiKey.CreatePivSessionAsync(
    options,
    cancellationToken);
}

}
