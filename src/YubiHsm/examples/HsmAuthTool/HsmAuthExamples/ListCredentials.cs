// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
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

        var table = OutputHelpers.CreateTable("Label", "Algorithm", "Touch", "Counter");

        foreach (var cred in credentials.OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase))
        {
            var algorithm = cred.Algorithm switch
            {
                HsmAuthAlgorithm.Aes128YubicoAuthentication => "AES-128",
                HsmAuthAlgorithm.EcP256YubicoAuthentication => "EC P256",
                _ => cred.Algorithm.ToString()
            };

            var touch = cred.TouchRequired switch
            {
                true => "[yellow]Required[/]",
                false => "[grey]No[/]",
                null => "[grey]Unknown[/]"
            };

            table.AddRow(
                Markup.Escape(cred.Label),
                algorithm,
                touch,
                cred.Counter.ToString());
        }

        AnsiConsole.Write(table);
        OutputHelpers.WriteInfo($"{credentials.Count} credential(s) found.");
    }
}
