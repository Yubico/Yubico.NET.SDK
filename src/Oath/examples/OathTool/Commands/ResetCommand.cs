// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Commands;

/// <summary>
/// Implements 'oath reset' - factory resets the OATH application.
/// </summary>
public static class ResetCommand
{
    public static async Task<int> ExecuteAsync(
        bool force = false,
        CancellationToken cancellationToken = default,
        OathToolServices? services = null)
    {
        services ??= OathToolServices.Default;
        cancellationToken.ThrowIfCancellationRequested();
        if (!force)
        {
            Console.Error.WriteLine(
                "WARNING: This will destroy all OATH credentials and the access password on the YubiKey.");
            if (!await services.Confirm("Proceed?", cancellationToken))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await services.OpenSession(cancellationToken);
        if (session is null)
        {
            return 1;
        }

        try
        {
            await session.ResetAsync(cancellationToken);
            OutputHelpers.WriteSuccess("OATH application reset.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            OutputHelpers.WriteInfo("Reset cancelled. Device data may already have been erased; reopen the session to inspect its state.");
            throw;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Reset failed: {ex.Message}. Device data may already have been erased.");
            return 1;
        }
    }
}