// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates verifying the User PIN and Admin PIN.
/// </summary>
public static class VerifyPin
{
    public static async Task RunUserAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var pin = OutputHelpers.PromptPin("Enter User PIN");

        try
        {
            await session.VerifyPinAsync(pin, cancellationToken: cancellationToken);
            OutputHelpers.WriteSuccess("User PIN verified successfully");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"PIN verification failed: {ex.Message}");

            var status = await session.GetPinStatusAsync(cancellationToken);
            OutputHelpers.WriteWarning($"Remaining User PIN attempts: {status.AttemptsUser}");
        }
    }

    public static async Task RunAdminAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var pin = OutputHelpers.PromptPin("Enter Admin PIN");

        try
        {
            await session.VerifyAdminAsync(pin, cancellationToken: cancellationToken);
            OutputHelpers.WriteSuccess("Admin PIN verified successfully");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Admin PIN verification failed: {ex.Message}");

            var status = await session.GetPinStatusAsync(cancellationToken);
            OutputHelpers.WriteWarning($"Remaining Admin PIN attempts: {status.AttemptsAdmin}");
        }
    }
}
