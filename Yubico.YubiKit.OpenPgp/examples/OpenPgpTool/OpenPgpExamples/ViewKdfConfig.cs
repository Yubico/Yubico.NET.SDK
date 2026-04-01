// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates reading the KDF (Key Derivation Function) configuration.
/// </summary>
public static class ViewKdfConfig
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        try
        {
            var kdf = await session.GetKdfAsync(cancellationToken);

            AnsiConsole.MarkupLine("[bold]KDF Configuration[/]");
            AnsiConsole.WriteLine();

            switch (kdf)
            {
                case KdfNone:
                    OutputHelpers.WriteKeyValue("KDF Type", "None (PINs sent in plaintext)");
                    OutputHelpers.WriteInfo("KDF is not configured. PINs are sent directly to the card.");
                    break;

                case KdfIterSaltedS2k s2k:
                    OutputHelpers.WriteKeyValue("KDF Type", "Iterated-Salted-S2K");
                    OutputHelpers.WriteKeyValue("Hash Algorithm", s2k.HashAlgorithm.ToString());
                    OutputHelpers.WriteKeyValue("Iteration Count", s2k.IterationCount.ToString("N0"));
                    OutputHelpers.WriteHex("User Salt", s2k.SaltUser.Span);

                    if (s2k.SaltAdmin is { } adminSalt)
                    {
                        OutputHelpers.WriteHex("Admin Salt", adminSalt.Span);
                    }

                    if (s2k.SaltReset is { } resetSalt)
                    {
                        OutputHelpers.WriteHex("Reset Salt", resetSalt.Span);
                    }

                    if (s2k.InitialHashUser is { } hashUser)
                    {
                        OutputHelpers.WriteHex("Initial Hash User", hashUser.Span);
                    }

                    if (s2k.InitialHashAdmin is { } hashAdmin)
                    {
                        OutputHelpers.WriteHex("Initial Hash Admin", hashAdmin.Span);
                    }

                    break;

                default:
                    OutputHelpers.WriteKeyValue("KDF Type", kdf.GetType().Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to read KDF configuration: {ex.Message}");
        }
    }

}
