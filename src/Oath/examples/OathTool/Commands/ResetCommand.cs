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

        var session = await services.OpenSession(cancellationToken);
        if (session is null)
        {
            return 1;
        }

        Exception? disposalFailure = null;
        try
        {
            await session.ResetAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            OutputHelpers.WriteInfo("Reset cancelled. Device data may already have been erased; close/reopen the connection and create a fresh session to inspect its state.");
            throw;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Reset failed: {ex.Message}. Device data may already have been erased.");
            return 1;
        }
        finally
        {
            // Close after reset or failure without masking the original cancellation/error.
            try { await session.DisposeAsync(); }
            catch (Exception ex) { disposalFailure = ex; }
        }

        if (disposalFailure is not null)
        {
            OutputHelpers.WriteError($"OATH application reset, but closing the session failed: {disposalFailure.Message}");
            return 1;
        }

        OutputHelpers.WriteSuccess("OATH application reset.");
        return 0;
    }
}