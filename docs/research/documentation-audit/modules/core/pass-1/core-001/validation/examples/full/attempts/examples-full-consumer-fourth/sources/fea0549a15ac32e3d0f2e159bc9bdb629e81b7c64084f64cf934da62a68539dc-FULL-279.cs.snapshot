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
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;

namespace Audit20261111.DocExamples.Full;
internal static class Body279
{
internal static async Task CompileOnly(IManagementSession session, CancellationToken ct)
{
byte[]? lockCode = null;
try
{
    lockCode = LockCodePrompt.PromptForLockCode("Enter lock code");
    if (lockCode is null) return; // User cancelled
    
    // session is an initialized IManagementSession; ct is the caller's CancellationToken.
    await session.SetDeviceConfigAsync(
        new DeviceConfig { ChallengeResponseTimeout = 15 },
        new SetDeviceConfigOptions { CurrentLockCode = lockCode }, ct);
}
finally
{
    if (lockCode is not null)
    {
        CryptographicOperations.ZeroMemory(lockCode);
    }
}
}

}
