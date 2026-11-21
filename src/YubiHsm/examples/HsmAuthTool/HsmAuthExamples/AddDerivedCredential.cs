// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

/// <summary>
///     Derives and stores a symmetric YubiKey credential for evaluation. Enroll the same derived
///     K-ENC/K-MAC pair in the HSM; the derivation password and YubiKey credential password serve separate roles.
/// </summary>
public static class AddDerivedCredential
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        var label = AnsiConsole.Ask<string>("Credential [green]label[/]:");
        using var derivationPassword = OutputHelpers.ReadPassword("Derivation password (PBKDF2)", cancellationToken);
        if (derivationPassword is null)
        {
            OutputHelpers.WriteError("Derivation password is required.");
            return;
        }
        using var credentialPassword = OutputHelpers.ReadPassword("Credential password", cancellationToken);
        if (credentialPassword is null)
        {
            OutputHelpers.WriteError("Credential password is required.");
            return;
        }

        using var password = CredentialPassword.FromUtf8(credentialPassword.Memory.Span);

        using var managementKey = OutputHelpers.ReadKey("Management key (16 bytes, hex)", 16, cancellationToken);
        if (managementKey is null)
            return;

        var touchRequired = AnsiConsole.Confirm("Require touch?", defaultValue: false);

        await session.PutCredentialDerivedAsync(
            managementKey.Memory,
            label,
            derivationPassword.Memory,
            password,
            touchRequired,
            cancellationToken);

        OutputHelpers.WriteSuccess($"Derived credential '{label}' stored successfully.");
        OutputHelpers.WriteInfo("Keys were derived using PBKDF2-HMAC-SHA256 (10,000 iterations, salt='Yubico').");
    }
}