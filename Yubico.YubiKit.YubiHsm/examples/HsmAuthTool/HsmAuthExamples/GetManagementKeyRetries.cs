// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class GetManagementKeyRetries
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        var retries = await session.GetManagementKeyRetriesAsync(cancellationToken);

        OutputHelpers.WriteKeyValue("Management key retries remaining", retries.ToString());

        if (retries <= 3)
        {
            OutputHelpers.WriteWarning("Low retry count! Incorrect attempts will reduce this further.");
        }
    }
}
