// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Cli.YkTool.Infrastructure;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.BioEnrollment;
using Yubico.YubiKit.Fido2.Config;
using Yubico.YubiKit.Fido2.CredentialManagement;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Cli.YkTool.Commands.Fido;

// ── Settings ────────────────────────────────────────────────────────────────

public sealed class FidoResetSettings : GlobalSettings
{
    [CommandOption("-f|--force")]
    [Description("Confirm the action without prompting.")]
    public bool Force { get; init; }
}

public sealed class FidoPinSettings : GlobalSettings
{
    [CommandOption("--pin <PIN>")]
    [Description("Current PIN.")]
    public string? Pin { get; set; }

    [CommandOption("--new-pin <PIN>")]
    [Description("New PIN to set.")]
    public string? NewPin { get; set; }
}

public sealed class FidoVerifyPinSettings : GlobalSettings
{
    [CommandOption("--pin <PIN>")]
    [Description("PIN to verify.")]
    public string? Pin { get; set; }
}

public sealed class FidoConfigSettings : GlobalSettings
{
    [CommandOption("--pin <PIN>")]
    [Description("PIN for authentication.")]
    public string? Pin { get; set; }
}

public sealed class FidoCredentialsListSettings : GlobalSettings
{
    [CommandOption("--pin <PIN>")]
    [Description("PIN for authentication.")]
    public string? Pin { get; set; }
}

public sealed class FidoCredentialsDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<CREDENTIAL_ID>")]
    [Description("Credential ID (hex).")]
    public string CredentialId { get; init; } = "";

    [CommandOption("--pin <PIN>")]
    [Description("PIN for authentication.")]
    public string? Pin { get; set; }

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompt.")]
    public bool Force { get; init; }
}

public sealed class FidoFingerprintsListSettings : GlobalSettings
{
    [CommandOption("--pin <PIN>")]
    [Description("PIN for authentication.")]
    public string? Pin { get; set; }
}

public sealed class FidoFingerprintsAddSettings : GlobalSettings
{
    [CommandArgument(0, "[NAME]")]
    [Description("Friendly name for the fingerprint.")]
    public string? Name { get; init; }

    [CommandOption("--pin <PIN>")]
    [Description("PIN for authentication.")]
    public string? Pin { get; set; }
}

public sealed class FidoFingerprintsDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<TEMPLATE_ID>")]
    [Description("Template ID (hex).")]
    public string TemplateId { get; init; } = "";

    [CommandOption("--pin <PIN>")]
    [Description("PIN for authentication.")]
    public string? Pin { get; set; }

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompt.")]
    public bool Force { get; init; }
}

public sealed class FidoFingerprintsRenameSettings : GlobalSettings
{
    [CommandArgument(0, "<TEMPLATE_ID>")]
    [Description("Template ID (hex).")]
    public string TemplateId { get; init; } = "";

    [CommandArgument(1, "<NAME>")]
    [Description("New friendly name.")]
    public string Name { get; init; } = "";

    [CommandOption("--pin <PIN>")]
    [Description("PIN for authentication.")]
    public string? Pin { get; set; }
}

// ── Commands ────────────────────────────────────────────────────────────────

internal sealed class FidoInfoCommand : YkCommandBase<GlobalSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
    {
        OutputHelpers.WriteHeader("FIDO2 Application");

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            var info = await session.GetInfoAsync();

            FidoHelpers.DisplayAuthenticatorInfo(info);
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError($"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})");
            return ExitCode.GenericError;
        }
    }
}

internal sealed class FidoResetCommand : YkCommandBase<FidoResetSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoResetSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDestructive("factory reset the FIDO2 application"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }

            AnsiConsole.MarkupLine("[yellow]To perform the reset:[/]");
            AnsiConsole.MarkupLine("[yellow]  1. Remove your YubiKey[/]");
            AnsiConsole.MarkupLine("[yellow]  2. Re-insert your YubiKey[/]");
            AnsiConsole.MarkupLine("[yellow]  3. Touch your YubiKey when prompted[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Remove your YubiKey now...[/]");
            AnsiConsole.MarkupLine("[grey]Press Enter after re-inserting your YubiKey.[/]");
            AnsiConsole.Console.Input.ReadKey(intercept: true);
        }

        try
        {
            AnsiConsole.MarkupLine("[yellow]Touch your YubiKey to confirm the reset...[/]");

            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            await session.ResetAsync();

            OutputHelpers.WriteSuccess("FIDO2 application has been factory reset.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            var message = ex.Status switch
            {
                CtapStatus.UserActionTimeout =>
                    "Reset timed out. You need to touch your YubiKey to confirm the reset.",
                CtapStatus.NotAllowed =>
                    "Reset not allowed. Reset must be triggered within 5 seconds after the YubiKey is inserted.",
                CtapStatus.OperationDenied =>
                    "Reset was denied. Remove the YubiKey, re-insert it, and try again within 5 seconds.",
                CtapStatus.PinAuthBlocked =>
                    "Reset not allowed. Remove the YubiKey, re-insert it, and try again within 5 seconds.",
                _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
            };
            OutputHelpers.WriteError(message);
            return ExitCode.GenericError;
        }
    }
}

internal sealed class FidoAccessSetPinCommand : YkCommandBase<FidoPinSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoPinSettings settings, YkDeviceContext deviceContext)
    {
        var newPin = settings.NewPin ?? PinPrompt.PromptForPin("New PIN");

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            await clientPin.SetPinAsync(newPin);

            OutputHelpers.WriteSuccess("PIN set successfully.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapPinError(ex));
            return ExitCode.AuthenticationFailed;
        }
    }
}

internal sealed class FidoAccessChangePinCommand : YkCommandBase<FidoPinSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoPinSettings settings, YkDeviceContext deviceContext)
    {
        var pin = settings.Pin ?? PinPrompt.PromptForPin("Current PIN");
        var newPin = settings.NewPin ?? PinPrompt.PromptForPin("New PIN");

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            await clientPin.ChangePinAsync(pin, newPin);

            OutputHelpers.WriteSuccess("PIN changed successfully.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapPinError(ex));
            return ExitCode.AuthenticationFailed;
        }
    }
}

internal sealed class FidoAccessVerifyPinCommand : YkCommandBase<FidoVerifyPinSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoVerifyPinSettings settings, YkDeviceContext deviceContext)
    {
        var pin = settings.Pin ?? PinPrompt.PromptForPin("PIN");
        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinTokenAsync(pin);

            OutputHelpers.WriteSuccess("PIN is correct.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapPinError(ex));
            return ExitCode.AuthenticationFailed;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
}

internal sealed class FidoConfigToggleAlwaysUvCommand : YkCommandBase<FidoConfigSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoConfigSettings settings, YkDeviceContext deviceContext)
    {
        var pin = settings.Pin ?? PinPrompt.PromptForPin("PIN");
        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin, PinUvAuthTokenPermissions.AuthenticatorConfig);

            var config = new AuthenticatorConfig(session, protocol, pinToken);
            await config.ToggleAlwaysUvAsync();

            OutputHelpers.WriteSuccess("Always-UV setting toggled.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapConfigError(ex));
            return ExitCode.GenericError;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
}

internal sealed class FidoConfigEnableEpAttestationCommand : YkCommandBase<FidoConfigSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoConfigSettings settings, YkDeviceContext deviceContext)
    {
        var pin = settings.Pin ?? PinPrompt.PromptForPin("PIN");
        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin, PinUvAuthTokenPermissions.AuthenticatorConfig);

            var config = new AuthenticatorConfig(session, protocol, pinToken);
            await config.EnableEnterpriseAttestationAsync();

            OutputHelpers.WriteSuccess("Enterprise attestation enabled.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapConfigError(ex));
            return ExitCode.GenericError;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
}

internal sealed class FidoCredentialsListCommand : YkCommandBase<FidoCredentialsListSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoCredentialsListSettings settings, YkDeviceContext deviceContext)
    {
        var pin = settings.Pin ?? PinPrompt.PromptForPin("PIN");
        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin, PinUvAuthTokenPermissions.CredentialManagement);

            var credMgmt = new Fido2.CredentialManagement.CredentialManagement(
                session, protocol, pinToken);

            var rps = await credMgmt.EnumerateRelyingPartiesAsync();

            if (rps.Count == 0)
            {
                OutputHelpers.WriteInfo("No credentials stored on this authenticator.");
                return ExitCode.Success;
            }

            OutputHelpers.WriteSuccess($"Found {rps.Count} relying party(ies).");
            AnsiConsole.WriteLine();

            foreach (var rp in rps)
            {
                AnsiConsole.MarkupLine($"  [green bold]{Markup.Escape(rp.RelyingParty.Id)}[/]");
                OutputHelpers.WriteHex("    RP ID Hash", rp.RpIdHash);

                // Get a fresh token for credential enumeration
                byte[]? credToken = null;
                try
                {
                    credToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        pin, PinUvAuthTokenPermissions.CredentialManagement);

                    var innerCredMgmt = new Fido2.CredentialManagement.CredentialManagement(
                        session, protocol, credToken);

                    var creds = await innerCredMgmt.EnumerateCredentialsAsync(rp.RpIdHash);

                    foreach (var cred in creds)
                    {
                        FidoHelpers.DisplayStoredCredential(cred);
                    }
                }
                finally
                {
                    if (credToken is not null)
                    {
                        CryptographicOperations.ZeroMemory(credToken);
                    }
                }

                AnsiConsole.WriteLine();
            }

            return ExitCode.Success;
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
        {
            OutputHelpers.WriteInfo("No credentials stored on this authenticator.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapCredError(ex));
            return ExitCode.GenericError;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
}

internal sealed class FidoCredentialsDeleteCommand : YkCommandBase<FidoCredentialsDeleteSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoCredentialsDeleteSettings settings, YkDeviceContext deviceContext)
    {
        byte[] credentialId;
        try
        {
            credentialId = Convert.FromHexString(settings.CredentialId);
        }
        catch (FormatException)
        {
            OutputHelpers.WriteError("Invalid hex string for credential ID.");
            return ExitCode.GenericError;
        }

        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDangerous("permanently delete this credential"))
            {
                OutputHelpers.WriteInfo("Operation cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        var pin = settings.Pin ?? PinPrompt.PromptForPin("PIN");
        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin, PinUvAuthTokenPermissions.CredentialManagement);

            var credMgmt = new Fido2.CredentialManagement.CredentialManagement(
                session, protocol, pinToken);

            var descriptor = new PublicKeyCredentialDescriptor(credentialId);
            await credMgmt.DeleteCredentialAsync(descriptor);

            OutputHelpers.WriteSuccess("Credential deleted successfully.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapCredError(ex));
            return ExitCode.GenericError;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
}

internal sealed class FidoFingerprintsListCommand : YkCommandBase<FidoFingerprintsListSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoFingerprintsListSettings settings, YkDeviceContext deviceContext)
    {
        var pin = settings.Pin ?? PinPrompt.PromptForPin("PIN");
        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin, PinUvAuthTokenPermissions.BioEnrollment);

            var bio = new FingerprintBioEnrollment(session, protocol, pinToken);
            var templates = await bio.EnumerateEnrollmentsAsync();

            if (templates.Count == 0)
            {
                OutputHelpers.WriteInfo("No fingerprints enrolled.");
                return ExitCode.Success;
            }

            OutputHelpers.WriteSuccess($"Found {templates.Count} enrollment(s).");
            AnsiConsole.WriteLine();

            foreach (var template in templates)
            {
                FidoHelpers.DisplayTemplate(template);
            }

            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapBioError(ex));
            return ExitCode.GenericError;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
}

internal sealed class FidoFingerprintsAddCommand : YkCommandBase<FidoFingerprintsAddSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoFingerprintsAddSettings settings, YkDeviceContext deviceContext)
    {
        var pin = settings.Pin ?? PinPrompt.PromptForPin("PIN");
        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin, PinUvAuthTokenPermissions.BioEnrollment);

            var bio = new FingerprintBioEnrollment(session, protocol, pinToken);

            AnsiConsole.MarkupLine("[yellow]Touch your YubiKey now...[/]");

            var result = await bio.EnrollBeginAsync();
            var templateId = result.TemplateId;
            var sampleNumber = 1;

            FidoHelpers.DisplaySampleStatus(sampleNumber, result.RemainingSamples, result.LastSampleStatus);

            while (!result.IsComplete)
            {
                try
                {
                    result = await bio.EnrollCaptureNextSampleAsync(templateId);
                    sampleNumber++;
                    FidoHelpers.DisplaySampleStatus(sampleNumber, result.RemainingSamples, result.LastSampleStatus);
                }
                catch (CtapException ex) when (ex.Status == CtapStatus.UserActionTimeout)
                {
                    AnsiConsole.MarkupLine("  [grey]No finger detected (timeout). Touch the sensor again...[/]");
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.Name))
            {
                await bio.SetFriendlyNameAsync(templateId, settings.Name);
            }

            OutputHelpers.WriteSuccess("Fingerprint enrolled successfully.");
            OutputHelpers.WriteHex("Template ID", templateId);
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapBioError(ex));
            return ExitCode.GenericError;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
}

internal sealed class FidoFingerprintsDeleteCommand : YkCommandBase<FidoFingerprintsDeleteSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoFingerprintsDeleteSettings settings, YkDeviceContext deviceContext)
    {
        byte[] templateId;
        try
        {
            templateId = Convert.FromHexString(settings.TemplateId);
        }
        catch (FormatException)
        {
            OutputHelpers.WriteError("Invalid hex string for template ID.");
            return ExitCode.GenericError;
        }

        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDangerous("permanently delete this fingerprint enrollment"))
            {
                OutputHelpers.WriteInfo("Operation cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        var pin = settings.Pin ?? PinPrompt.PromptForPin("PIN");
        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin, PinUvAuthTokenPermissions.BioEnrollment);

            var bio = new FingerprintBioEnrollment(session, protocol, pinToken);
            await bio.RemoveEnrollmentAsync(templateId);

            OutputHelpers.WriteSuccess("Fingerprint enrollment removed successfully.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapBioError(ex));
            return ExitCode.GenericError;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
}

internal sealed class FidoFingerprintsRenameCommand : YkCommandBase<FidoFingerprintsRenameSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.HidFido, ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoFingerprintsRenameSettings settings, YkDeviceContext deviceContext)
    {
        byte[] templateId;
        try
        {
            templateId = Convert.FromHexString(settings.TemplateId);
        }
        catch (FormatException)
        {
            OutputHelpers.WriteError("Invalid hex string for template ID.");
            return ExitCode.GenericError;
        }

        var pin = settings.Pin ?? PinPrompt.PromptForPin("PIN");
        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateFidoSessionAsync();
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pin, PinUvAuthTokenPermissions.BioEnrollment);

            var bio = new FingerprintBioEnrollment(session, protocol, pinToken);
            await bio.SetFriendlyNameAsync(templateId, settings.Name);

            OutputHelpers.WriteSuccess("Fingerprint enrollment renamed successfully.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapBioError(ex));
            return ExitCode.GenericError;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
}

// ── Helpers ─────────────────────────────────────────────────────────────────

internal static class FidoHelpers
{
    public static void DisplayAuthenticatorInfo(AuthenticatorInfo info)
    {
        OutputHelpers.WriteKeyValue("CTAP Versions", string.Join(", ", info.Versions));
        OutputHelpers.WriteHex("AAGUID", info.Aaguid);

        if (info.FirmwareVersion is not null)
        {
            OutputHelpers.WriteKeyValue("Firmware Version", info.FirmwareVersion.ToString());
        }

        if (info.Extensions.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [green]Extensions:[/]");
            foreach (var ext in info.Extensions)
            {
                AnsiConsole.MarkupLine($"    - {Markup.Escape(ext)}");
            }
        }

        if (info.Options.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [green]Options:[/]");
            foreach (var (key, value) in info.Options)
            {
                var color = value ? "green" : "grey";
                AnsiConsole.MarkupLine($"    [{color}]{Markup.Escape(key)}: {value}[/]");
            }
        }

        if (info.Algorithms.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [green]Algorithms:[/]");
            foreach (var alg in info.Algorithms)
            {
                AnsiConsole.MarkupLine($"    - {Markup.Escape(alg.Type)} ({alg.Algorithm})");
            }
        }

        if (info.Transports.Count > 0)
        {
            OutputHelpers.WriteKeyValue("Transports", string.Join(", ", info.Transports));
        }

        if (info.PinUvAuthProtocols.Count > 0)
        {
            OutputHelpers.WriteKeyValue("PIN/UV Auth Protocols",
                string.Join(", ", info.PinUvAuthProtocols.Select(p => $"V{p}")));
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [green]Limits:[/]");

        if (info.MaxMsgSize.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max Message Size", $"{info.MaxMsgSize} bytes");
        }

        if (info.MaxCredentialCountInList.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max Credentials in List", info.MaxCredentialCountInList.ToString());
        }

        if (info.MaxCredentialIdLength.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max Credential ID Length", $"{info.MaxCredentialIdLength} bytes");
        }

        if (info.MaxCredBlobLength.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max CredBlob Length", $"{info.MaxCredBlobLength} bytes");
        }

        if (info.MaxSerializedLargeBlobArray.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max Large Blob Array", $"{info.MaxSerializedLargeBlobArray} bytes");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [green]PIN Configuration:[/]");

        if (info.MinPinLength.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Min PIN Length", info.MinPinLength.ToString());
        }

        if (info.MaxPinLength.HasValue)
        {
            OutputHelpers.WriteKeyValue("  Max PIN Length", info.MaxPinLength.ToString());
        }

        if (info.ForcePinChange.HasValue)
        {
            OutputHelpers.WriteBoolValue("  Force PIN Change", info.ForcePinChange.Value);
        }

        if (info.PinComplexityPolicy.HasValue)
        {
            OutputHelpers.WriteBoolValue("  PIN Complexity Policy", info.PinComplexityPolicy.Value);
        }

        if (info.RemainingDiscoverableCredentials.HasValue)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteKeyValue("Remaining Credential Slots",
                info.RemainingDiscoverableCredentials.ToString());
        }

        if (info.AttestationFormats.Count > 0)
        {
            OutputHelpers.WriteKeyValue("Attestation Formats",
                string.Join(", ", info.AttestationFormats));
        }

        if (info.Certifications.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("  [green]Certifications:[/]");
            foreach (var (name, level) in info.Certifications)
            {
                OutputHelpers.WriteKeyValue($"  {name}", $"Level {level}");
            }
        }
    }

    public static void DisplayStoredCredential(StoredCredentialInfo cred)
    {
        AnsiConsole.MarkupLine($"    [blue]User:[/] {Markup.Escape(cred.User.Name ?? "(unknown)")}");
        if (cred.User.DisplayName is not null)
        {
            AnsiConsole.MarkupLine($"    [blue]Display Name:[/] {Markup.Escape(cred.User.DisplayName)}");
        }

        OutputHelpers.WriteHex("    Credential ID", cred.CredentialId.Id);
        OutputHelpers.WriteHex("    User ID", cred.User.Id);
        AnsiConsole.MarkupLine($"    [blue]Type:[/] {Markup.Escape(cred.CredentialId.Type ?? "public-key")}");

        if (cred.CredProtectPolicy.HasValue)
        {
            AnsiConsole.MarkupLine($"    [blue]Cred Protect:[/] {cred.CredProtectPolicy.Value}");
        }

        AnsiConsole.WriteLine();
    }

    public static void DisplayTemplate(TemplateInfo template)
    {
        OutputHelpers.WriteHex("  Template ID", template.TemplateId);
        if (template.FriendlyName is not null)
        {
            AnsiConsole.MarkupLine($"    [blue]Name:[/] {Markup.Escape(template.FriendlyName)}");
        }

        AnsiConsole.WriteLine();
    }

    public static void DisplaySampleStatus(int sampleNumber, int remaining, FingerprintSampleStatus status)
    {
        var statusText = status switch
        {
            FingerprintSampleStatus.Good => "Good sample captured",
            FingerprintSampleStatus.TooHigh => "Finger too high on sensor",
            FingerprintSampleStatus.TooLow => "Finger too low on sensor",
            FingerprintSampleStatus.TooLeft => "Finger too far left",
            FingerprintSampleStatus.TooRight => "Finger too far right",
            FingerprintSampleStatus.TooFast => "Finger moved too fast",
            FingerprintSampleStatus.TooSlow => "Finger moved too slow",
            FingerprintSampleStatus.PoorQuality => "Poor quality sample",
            FingerprintSampleStatus.TooSkewed => "Finger too skewed",
            FingerprintSampleStatus.TooShort => "Touch too short",
            FingerprintSampleStatus.MergeFailure => "Merge failure",
            FingerprintSampleStatus.StorageFull => "Fingerprint storage full",
            FingerprintSampleStatus.NoUserActivity => "No finger detected (timeout)",
            FingerprintSampleStatus.NoUserPresence => "No user presence",
            _ => $"Unknown status: {status}"
        };

        AnsiConsole.MarkupLine($"  Sample {sampleNumber}: {Markup.Escape(statusText)} ({remaining} remaining)");

        if (remaining > 0)
        {
            AnsiConsole.MarkupLine("[yellow]  Touch the sensor again...[/]");
        }
    }

    public static string MapCtapPinError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinAuthBlocked =>
                "PIN authentication is blocked. Remove and re-insert the YubiKey.",
            CtapStatus.PinNotSet => "No PIN is currently set on this authenticator.",
            CtapStatus.PinPolicyViolation =>
                "The PIN does not meet the authenticator's policy requirements.",
            CtapStatus.NotAllowed =>
                "Operation not allowed. A PIN may already be set (use 'change-pin' instead of 'set-pin').",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };

    public static string MapCtapConfigError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinNotSet => "No PIN is set. Set a PIN first.",
            CtapStatus.NotAllowed =>
                "Operation not allowed. Authenticator config may not be supported.",
            CtapStatus.InvalidCommand =>
                "This configuration operation is not supported on this authenticator.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };

    public static string MapCtapCredError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.NoCredentials => "No credentials found on this authenticator.",
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinNotSet => "No PIN is set. Set a PIN first.",
            CtapStatus.NotAllowed =>
                "Operation not allowed. Credential management may not be supported.",
            CtapStatus.KeyStoreFull => "The authenticator's credential storage is full.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };

    public static string MapCtapBioError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.UserActionTimeout =>
                "Operation timed out. Please try again and touch the sensor when prompted.",
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinNotSet => "No PIN is set. Set a PIN first.",
            CtapStatus.NotAllowed =>
                "Operation not allowed. Bio enrollment may not be supported.",
            CtapStatus.InvalidCommand =>
                "Bio enrollment is not supported on this authenticator.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}
