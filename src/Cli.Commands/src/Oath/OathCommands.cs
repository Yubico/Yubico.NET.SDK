// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Oath;
using static Yubico.YubiKit.Cli.Commands.Oath.OathHelpers;

namespace Yubico.YubiKit.Cli.Commands.Oath;

// ── Settings ────────────────────────────────────────────────────────────────

public sealed class OathPasswordSettings : GlobalSettings
{
    [CommandOption("--password|-p <PASSWORD>")]
    [Description("OATH access password (if set).")]
    public string? Password { get; set; }
}

public sealed class OathResetSettings : GlobalSettings
{
    [CommandOption("-f|--force")]
    [Description("Confirm the action without prompting.")]
    public bool Force { get; init; }
}

public sealed class OathAccessChangePasswordSettings : GlobalSettings
{
    [CommandOption("--password|-p <PASSWORD>")]
    [Description("Current OATH password (if set).")]
    public string? Password { get; set; }

    [CommandOption("--new-password|-n <PASSWORD>")]
    [Description("New OATH password.")]
    public string? NewPassword { get; set; }

    [CommandOption("--clear|-c")]
    [Description("Remove password protection.")]
    public bool Clear { get; init; }
}

public sealed class OathAccountsListSettings : GlobalSettings
{
    [CommandOption("--password|-p <PASSWORD>")]
    [Description("OATH access password (if set).")]
    public string? Password { get; set; }
}

public sealed class OathAccountsAddSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Account name.")]
    public string Name { get; init; } = "";

    [CommandArgument(1, "<SECRET>")]
    [Description("Base32-encoded secret key.")]
    public string Secret { get; init; } = "";

    [CommandOption("--issuer <ISSUER>")]
    [Description("Credential issuer.")]
    public string? Issuer { get; set; }

    [CommandOption("--oath-type <TYPE>")]
    [Description("OATH type: TOTP or HOTP (default: TOTP).")]
    [DefaultValue("TOTP")]
    public string OathTypeStr { get; init; } = "TOTP";

    [CommandOption("--digits <DIGITS>")]
    [Description("Number of digits: 6 or 8 (default: 6).")]
    [DefaultValue(6)]
    public int Digits { get; init; } = 6;

    [CommandOption("--algorithm <ALG>")]
    [Description("Hash algorithm: SHA1, SHA256, SHA512 (default: SHA1).")]
    [DefaultValue("SHA1")]
    public string AlgorithmStr { get; init; } = "SHA1";

    [CommandOption("--period <SECONDS>")]
    [Description("TOTP period in seconds (default: 30).")]
    [DefaultValue(30)]
    public int Period { get; init; } = 30;

    [CommandOption("--touch")]
    [Description("Require touch.")]
    public bool Touch { get; init; }

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompt.")]
    public bool Force { get; init; }

    [CommandOption("--password|-p <PASSWORD>")]
    [Description("OATH access password (if set).")]
    public string? Password { get; set; }
}

public sealed class OathAccountsCodeSettings : GlobalSettings
{
    [CommandArgument(0, "[QUERY]")]
    [Description("Filter credentials by name.")]
    public string? Query { get; init; }

    [CommandOption("--password|-p <PASSWORD>")]
    [Description("OATH access password (if set).")]
    public string? Password { get; set; }
}

public sealed class OathAccountsDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Credential name to delete.")]
    public string Name { get; init; } = "";

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompt.")]
    public bool Force { get; init; }

    [CommandOption("--password|-p <PASSWORD>")]
    [Description("OATH access password (if set).")]
    public string? Password { get; set; }
}

public sealed class OathAccountsRenameSettings : GlobalSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Current credential name.")]
    public string Name { get; init; } = "";

    [CommandArgument(1, "<NEW_NAME>")]
    [Description("New credential name (issuer:name format).")]
    public string NewName { get; init; } = "";

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompt.")]
    public bool Force { get; init; }

    [CommandOption("--password|-p <PASSWORD>")]
    [Description("OATH access password (if set).")]
    public string? Password { get; set; }
}

public sealed class OathAccountsUriSettings : GlobalSettings
{
    [CommandArgument(0, "<URI>")]
    [Description("otpauth:// URI.")]
    public string Uri { get; init; } = "";

    [CommandOption("--touch")]
    [Description("Require touch.")]
    public bool Touch { get; init; }

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompt.")]
    public bool Force { get; init; }

    [CommandOption("--password|-p <PASSWORD>")]
    [Description("OATH access password (if set).")]
    public string? Password { get; set; }
}

// ── Commands ────────────────────────────────────────────────────────────────

public sealed class OathInfoCommand : YkCommandBase<OathPasswordSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathPasswordSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOathSessionAsync();

        OutputHelpers.WriteHeader("OATH Application");
        OutputHelpers.WriteKeyValue("OATH Version", session.FirmwareVersion.ToString());
        OutputHelpers.WriteBoolValue("Password Protected", session.IsLocked);
        OutputHelpers.WriteKeyValue("Device ID", session.DeviceId);

        return ExitCode.Success;
    }
}

public sealed class OathResetCommand : YkCommandBase<OathResetSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathResetSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDestructive(
                    "reset the OATH application, destroying all credentials and the access password"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreateOathSessionAsync();
        await session.ResetAsync();

        OutputHelpers.WriteSuccess("OATH application reset.");
        return ExitCode.Success;
    }
}

public sealed class OathAccessChangePasswordCommand : YkCommandBase<OathAccessChangePasswordSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathAccessChangePasswordSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOathSessionAsync();

        if (session.IsLocked)
        {
            if (!await UnlockIfNeededAsync(session, settings.Password))
            {
                return ExitCode.AuthenticationFailed;
            }
        }

        if (settings.Clear)
        {
            await session.UnsetKeyAsync();
            OutputHelpers.WriteSuccess("OATH access password cleared.");
            return ExitCode.Success;
        }

        var newPassword = settings.NewPassword;
        if (string.IsNullOrEmpty(newPassword))
        {
            newPassword = PinPrompt.PromptForPin("Enter new OATH password");
            var confirm = PinPrompt.PromptForPin("Confirm new OATH password");
            if (!string.Equals(newPassword, confirm, StringComparison.Ordinal))
            {
                OutputHelpers.WriteError("Passwords do not match.");
                return ExitCode.GenericError;
            }
        }

        if (string.IsNullOrEmpty(newPassword))
        {
            OutputHelpers.WriteError("New password cannot be empty. Use --clear to remove password protection.");
            return ExitCode.GenericError;
        }

        byte[]? key = null;
        try
        {
            key = session.DeriveKey(Encoding.UTF8.GetBytes(newPassword));
            await session.SetKeyAsync(key);
            OutputHelpers.WriteSuccess("OATH access password set.");
            return ExitCode.Success;
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }
}

public sealed class OathAccountsListCommand : YkCommandBase<OathAccountsListSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathAccountsListSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOathSessionAsync();

        if (!await UnlockIfNeededAsync(session, settings.Password))
        {
            return ExitCode.AuthenticationFailed;
        }

        var credentials = await session.ListCredentialsAsync();

        if (credentials.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored on this device.");
            return ExitCode.Success;
        }

        foreach (var cred in credentials.OrderBy(c => FormatCredentialName(c.Issuer, c.Name)))
        {
            AnsiConsole.WriteLine(FormatCredentialName(cred.Issuer, cred.Name));
        }

        return ExitCode.Success;
    }
}

public sealed class OathAccountsAddCommand : YkCommandBase<OathAccountsAddSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathAccountsAddSettings settings, YkDeviceContext deviceContext)
    {
        byte[] secretBytes;
        try
        {
            secretBytes = Base32Decode(settings.Secret);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Invalid Base32 secret: {ex.Message}");
            return ExitCode.GenericError;
        }

        var oathType = settings.OathTypeStr.ToUpperInvariant() switch
        {
            "HOTP" => OathType.Hotp,
            "TOTP" => OathType.Totp,
            _ => throw new ArgumentException($"Invalid OATH type: '{settings.OathTypeStr}'.")
        };

        var algorithm = settings.AlgorithmStr.ToUpperInvariant() switch
        {
            "SHA256" => OathHashAlgorithm.Sha256,
            "SHA512" => OathHashAlgorithm.Sha512,
            "SHA1" => OathHashAlgorithm.Sha1,
            _ => throw new ArgumentException($"Invalid algorithm: '{settings.AlgorithmStr}'.")
        };

        using var credentialData = new CredentialData
        {
            Name = settings.Name,
            OathType = oathType,
            HashAlgorithm = algorithm,
            Secret = secretBytes,
            Digits = settings.Digits,
            Period = settings.Period,
            Issuer = settings.Issuer
        };

        var displayName = FormatCredentialName(credentialData.Issuer, credentialData.Name);

        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDangerous($"add credential {displayName}"))
            {
                OutputHelpers.WriteInfo("Add cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreateOathSessionAsync();

        if (!await UnlockIfNeededAsync(session, settings.Password))
        {
            return ExitCode.AuthenticationFailed;
        }

        await session.PutCredentialAsync(credentialData, settings.Touch);
        OutputHelpers.WriteSuccess($"Credential added: {displayName}");
        return ExitCode.Success;
    }
}

public sealed class OathAccountsCodeCommand : YkCommandBase<OathAccountsCodeSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathAccountsCodeSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOathSessionAsync();

        if (!await UnlockIfNeededAsync(session, settings.Password))
        {
            return ExitCode.AuthenticationFailed;
        }

        if (settings.Query is not null)
        {
            var credential = await FindCredentialAsync(session, settings.Query);
            if (credential is null)
            {
                return ExitCode.GenericError;
            }

            var code = await session.CalculateCodeAsync(credential);
            var displayName = FormatCredentialName(credential.Issuer, credential.Name);
            AnsiConsole.WriteLine($"{displayName}  {code.Value}");
            return ExitCode.Success;
        }

        var results = await session.CalculateAllAsync();

        if (results.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored on this device.");
            return ExitCode.Success;
        }

        foreach (var (credential, code) in results.OrderBy(r =>
                     FormatCredentialName(r.Key.Issuer, r.Key.Name)))
        {
            var displayName = FormatCredentialName(credential.Issuer, credential.Name);
            string codeValue = code switch
            {
                not null => code.Value,
                _ when credential.TouchRequired == true => "[Touch credential]",
                _ => "[HOTP credential]"
            };
            AnsiConsole.WriteLine($"{displayName}  {codeValue}");
        }

        return ExitCode.Success;
    }
}

public sealed class OathAccountsDeleteCommand : YkCommandBase<OathAccountsDeleteSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathAccountsDeleteSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOathSessionAsync();

        if (!await UnlockIfNeededAsync(session, settings.Password))
        {
            return ExitCode.AuthenticationFailed;
        }

        var credential = await FindCredentialAsync(session, settings.Name);
        if (credential is null)
        {
            return ExitCode.GenericError;
        }

        var displayName = FormatCredentialName(credential.Issuer, credential.Name);

        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDangerous($"delete credential {displayName}"))
            {
                OutputHelpers.WriteInfo("Delete cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await session.DeleteCredentialAsync(credential);
        OutputHelpers.WriteSuccess($"Credential deleted: {displayName}");
        return ExitCode.Success;
    }
}

public sealed class OathAccountsRenameCommand : YkCommandBase<OathAccountsRenameSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathAccountsRenameSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOathSessionAsync();

        if (!await UnlockIfNeededAsync(session, settings.Password))
        {
            return ExitCode.AuthenticationFailed;
        }

        var credential = await FindCredentialAsync(session, settings.Name);
        if (credential is null)
        {
            return ExitCode.GenericError;
        }

        var displayName = FormatCredentialName(credential.Issuer, credential.Name);

        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDangerous($"rename {displayName} to {settings.NewName}"))
            {
                OutputHelpers.WriteInfo("Rename cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        string? newIssuer = null;
        string newAccountName = settings.NewName;
        if (settings.NewName.Contains(':'))
        {
            int colonIndex = settings.NewName.IndexOf(':');
            newIssuer = settings.NewName[..colonIndex];
            newAccountName = settings.NewName[(colonIndex + 1)..];
        }

        var renamed = await session.RenameCredentialAsync(credential, newIssuer, newAccountName);
        var renamedDisplay = FormatCredentialName(renamed.Issuer, renamed.Name);
        OutputHelpers.WriteSuccess($"Renamed: {displayName} -> {renamedDisplay}");
        return ExitCode.Success;
    }
}

public sealed class OathAccountsUriCommand : YkCommandBase<OathAccountsUriSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OathAccountsUriSettings settings, YkDeviceContext deviceContext)
    {
        CredentialData credentialData;
        try
        {
            credentialData = CredentialData.ParseUri(settings.Uri);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Invalid otpauth URI: {ex.Message}");
            return ExitCode.GenericError;
        }

        using (credentialData)
        {
            var displayName = FormatCredentialName(credentialData.Issuer, credentialData.Name);

            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous($"add credential {displayName} ({credentialData.OathType})"))
                {
                    OutputHelpers.WriteInfo("Add cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateOathSessionAsync();

            if (!await UnlockIfNeededAsync(session, settings.Password))
            {
                return ExitCode.AuthenticationFailed;
            }

            await session.PutCredentialAsync(credentialData, settings.Touch);
            OutputHelpers.WriteSuccess($"Credential added: {displayName}");
            return ExitCode.Success;
        }
    }
}
