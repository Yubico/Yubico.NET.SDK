// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class ResetHsmAuth
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        if (!OutputHelpers.ConfirmDestructive(
                "factory reset the YubiHSM Auth applet, deleting ALL stored credentials and resetting the management key"))
        {
            OutputHelpers.WriteInfo("Factory reset cancelled.");
            return;
        }

        await session.ResetAsync(cancellationToken);

        OutputHelpers.WriteSuccess("YubiHSM Auth applet reset to factory defaults.");
        OutputHelpers.WriteInfo("Management key has been reset to the default (all zeros).");
        OutputHelpers.WriteInfo("All credentials have been deleted.");
    }
}
