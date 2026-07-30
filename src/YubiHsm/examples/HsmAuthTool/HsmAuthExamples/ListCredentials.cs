// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Cli.Commands.HsmAuth;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class ListCredentials
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        var credentials = await session.ListCredentialsAsync(cancellationToken);

        if (credentials.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored.");
            return;
        }

        var table = HsmAuthHelpers.BuildCredentialsTable(credentials);

        AnsiConsole.Write(table);
        OutputHelpers.WriteInfo($"{credentials.Count} credential(s) found.");
    }
}