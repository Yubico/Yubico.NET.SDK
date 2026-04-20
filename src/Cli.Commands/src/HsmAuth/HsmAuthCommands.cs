// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.YubiHsm;

namespace Yubico.YubiKit.Cli.Commands.HsmAuth;

// ── Settings ────────────────────────────────────────────────────────────────

public sealed class HsmAuthResetSettings : GlobalSettings
{
    [CommandOption("-f|--force")]
    [Description("Confirm the action without prompting.")]
    public bool Force { get; init; }
}

public sealed class HsmAuthChangeManagementKeySettings : GlobalSettings
{
    [CommandOption("--management-key <HEX>")]
    [Description("Current management key (hex, 16 bytes). Default: all zeros.")]
    public string? ManagementKey { get; set; }

    [CommandOption("--new-management-key <HEX>")]
    [Description("New management key (hex, 16 bytes).")]
    public string? NewManagementKey { get; set; }
}

public sealed class HsmAuthCredentialsListSettings : GlobalSettings;

public sealed class HsmAuthCredentialsAddSettings : GlobalSettings
{
    [CommandArgument(0, "<LABEL>")]
    [Description("Credential label.")]
    public string Label { get; init; } = "";

    [CommandOption("--management-key <HEX>")]
    [Description("Management key (hex, 16 bytes).")]
    public string? ManagementKey { get; set; }

    [CommandOption("--credential-password <PASSWORD>")]
    [Description("Credential password.")]
    public string? CredentialPassword { get; set; }

    [CommandOption("--touch")]
    [Description("Require touch for credential use.")]
    public bool Touch { get; init; }

    [CommandOption("--derivation-password <PASSWORD>")]
    [Description("Derive symmetric keys via PBKDF2 instead of random generation.")]
    public string? DerivationPassword { get; set; }
}

public sealed class HsmAuthCredentialsDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<LABEL>")]
    [Description("Credential label to delete.")]
    public string Label { get; init; } = "";

    [CommandOption("--management-key <HEX>")]
    [Description("Management key (hex, 16 bytes).")]
    public string? ManagementKey { get; set; }

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompt.")]
    public bool Force { get; init; }
}

public sealed class HsmAuthCredentialsGenerateSettings : GlobalSettings
{
    [CommandArgument(0, "<LABEL>")]
    [Description("Credential label.")]
    public string Label { get; init; } = "";

    [CommandOption("--management-key <HEX>")]
    [Description("Management key (hex, 16 bytes).")]
    public string? ManagementKey { get; set; }

    [CommandOption("--credential-password <PASSWORD>")]
    [Description("Credential password.")]
    public string? CredentialPassword { get; set; }

    [CommandOption("--touch")]
    [Description("Require touch for credential use.")]
    public bool Touch { get; init; }
}

// ── Helper ──────────────────────────────────────────────────────────────────

public static class HsmAuthHelpers
{
    public static byte[]? ResolveManagementKey(string? hex)
    {
        hex ??= "00000000000000000000000000000000";

        try
        {
            var key = Convert.FromHexString(hex);
            if (key.Length != 16)
            {
                OutputHelpers.WriteError("Invalid management key. Must be 16 bytes (32 hex characters).");
                return null;
            }

            return key;
        }
        catch (FormatException)
        {
            OutputHelpers.WriteError("Invalid management key hex format.");
            return null;
        }
    }
}

// ── Commands ────────────────────────────────────────────────────────────────

public sealed class HsmAuthInfoCommand : YkCommandBase<GlobalSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateHsmAuthSessionAsync();

        var retries = await session.GetManagementKeyRetriesAsync();
        var credentials = await session.ListCredentialsAsync();

        OutputHelpers.WriteHeader("YubiHSM Auth");
        OutputHelpers.WriteKeyValue("Version", session.FirmwareVersion.ToString());
        OutputHelpers.WriteKeyValue("Management Key Retries", retries.ToString());
        OutputHelpers.WriteKeyValue("Stored Credentials", credentials.Count.ToString());

        if (retries <= 3)
        {
            OutputHelpers.WriteWarning("Low retry count! Incorrect attempts will reduce this further.");
        }

        return ExitCode.Success;
    }
}

public sealed class HsmAuthResetCommand : YkCommandBase<HsmAuthResetSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, HsmAuthResetSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDestructive(
                    "reset the YubiHSM Auth applet, deleting ALL credentials and resetting the management key"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreateHsmAuthSessionAsync();
        await session.ResetAsync();

        OutputHelpers.WriteSuccess("YubiHSM Auth applet has been reset.");
        return ExitCode.Success;
    }
}

public sealed class HsmAuthAccessChangeManagementKeyCommand : YkCommandBase<HsmAuthChangeManagementKeySettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, HsmAuthChangeManagementKeySettings settings, YkDeviceContext deviceContext)
    {
        var currentKey = HsmAuthHelpers.ResolveManagementKey(settings.ManagementKey);
        if (currentKey is null)
        {
            return ExitCode.GenericError;
        }

        if (string.IsNullOrEmpty(settings.NewManagementKey))
        {
            OutputHelpers.WriteError("--new-management-key is required.");
            CryptographicOperations.ZeroMemory(currentKey);
            return ExitCode.GenericError;
        }

        var newKey = HsmAuthHelpers.ResolveManagementKey(settings.NewManagementKey);
        if (newKey is null)
        {
            CryptographicOperations.ZeroMemory(currentKey);
            return ExitCode.GenericError;
        }

        try
        {
            await using var session = await deviceContext.Device.CreateHsmAuthSessionAsync();
            await session.PutManagementKeyAsync(currentKey, newKey);

            OutputHelpers.WriteSuccess("Management key changed successfully.");
            OutputHelpers.WriteWarning("Store the new key securely. Losing it will require a factory reset.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(currentKey);
            CryptographicOperations.ZeroMemory(newKey);
        }
    }
}

public sealed class HsmAuthCredentialsListCommand : YkCommandBase<HsmAuthCredentialsListSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, HsmAuthCredentialsListSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateHsmAuthSessionAsync();
        var credentials = await session.ListCredentialsAsync();

        if (credentials.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored.");
            return ExitCode.Success;
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
                Markup.Escape(cred.Label), algorithm, touch, cred.Counter.ToString());
        }

        AnsiConsole.Write(table);
        OutputHelpers.WriteInfo($"{credentials.Count} credential(s) found.");
        return ExitCode.Success;
    }
}

public sealed class HsmAuthCredentialsAddCommand : YkCommandBase<HsmAuthCredentialsAddSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, HsmAuthCredentialsAddSettings settings, YkDeviceContext deviceContext)
    {
        var mgmtKey = HsmAuthHelpers.ResolveManagementKey(settings.ManagementKey);
        if (mgmtKey is null)
        {
            return ExitCode.GenericError;
        }

        var credentialPassword = settings.CredentialPassword
                                 ?? PinPrompt.PromptForPin("Credential password");

        try
        {
            await using var session = await deviceContext.Device.CreateHsmAuthSessionAsync();

            if (settings.DerivationPassword is not null)
            {
                await session.PutCredentialDerivedAsync(
                    mgmtKey, settings.Label, settings.DerivationPassword,
                    credentialPassword, settings.Touch);

                OutputHelpers.WriteSuccess($"Derived credential '{settings.Label}' stored successfully.");
            }
            else
            {
                byte[] keyEnc = RandomNumberGenerator.GetBytes(16);
                byte[] keyMac = RandomNumberGenerator.GetBytes(16);

                try
                {
                    await session.PutCredentialSymmetricAsync(
                        mgmtKey, settings.Label, keyEnc, keyMac,
                        credentialPassword, settings.Touch);

                    OutputHelpers.WriteSuccess($"Symmetric credential '{settings.Label}' stored.");
                    OutputHelpers.WriteHex("K-ENC (generated)", keyEnc);
                    OutputHelpers.WriteHex("K-MAC (generated)", keyMac);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyEnc);
                    CryptographicOperations.ZeroMemory(keyMac);
                }
            }

            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }
}

public sealed class HsmAuthCredentialsDeleteCommand : YkCommandBase<HsmAuthCredentialsDeleteSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, HsmAuthCredentialsDeleteSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDangerous($"delete credential '{settings.Label}'"))
            {
                OutputHelpers.WriteInfo("Delete cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        var mgmtKey = HsmAuthHelpers.ResolveManagementKey(settings.ManagementKey);
        if (mgmtKey is null)
        {
            return ExitCode.GenericError;
        }

        try
        {
            await using var session = await deviceContext.Device.CreateHsmAuthSessionAsync();
            await session.DeleteCredentialAsync(mgmtKey, settings.Label);

            OutputHelpers.WriteSuccess($"Credential '{settings.Label}' deleted.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }
}

public sealed class HsmAuthCredentialsGenerateCommand : YkCommandBase<HsmAuthCredentialsGenerateSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, HsmAuthCredentialsGenerateSettings settings, YkDeviceContext deviceContext)
    {
        var mgmtKey = HsmAuthHelpers.ResolveManagementKey(settings.ManagementKey);
        if (mgmtKey is null)
        {
            return ExitCode.GenericError;
        }

        var credentialPassword = settings.CredentialPassword
                                 ?? PinPrompt.PromptForPin("Credential password");

        try
        {
            await using var session = await deviceContext.Device.CreateHsmAuthSessionAsync();

            await session.GenerateCredentialAsymmetricAsync(
                mgmtKey, settings.Label, credentialPassword, settings.Touch);

            OutputHelpers.WriteSuccess($"Asymmetric credential '{settings.Label}' generated on device.");
            OutputHelpers.WriteInfo("Private key was generated on-device and never leaves the YubiKey.");

            var publicKey = await session.GetPublicKeyAsync(settings.Label);
            OutputHelpers.WriteHex("Public key", publicKey.Span);

            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }
}
