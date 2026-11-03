using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features.Results;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;


namespace ManagementSnippet_tool_lock_prompt;

public static class DocumentationExample
{
    public static async Task Example(IManagementSession session, CancellationToken ct)
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
