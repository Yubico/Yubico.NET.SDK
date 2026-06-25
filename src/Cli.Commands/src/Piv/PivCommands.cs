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
using Yubico.YubiKit.Piv;

namespace Yubico.YubiKit.Cli.Commands.Piv;

// ── Settings ────────────────────────────────────────────────────────────────

public sealed class PivResetSettings : GlobalSettings
{
    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompts.")]
    public bool Force { get; init; }
}

public sealed class PivPinSettings : GlobalSettings
{
    [CommandOption("--pin <PIN>")]
    [Description("Current PIN.")]
    public string? Pin { get; set; }

    [CommandOption("--new-pin <PIN>")]
    [Description("New PIN.")]
    public string? NewPin { get; set; }
}

public sealed class PivPukSettings : GlobalSettings
{
    [CommandOption("--puk <PUK>")]
    [Description("Current PUK.")]
    public string? Puk { get; set; }

    [CommandOption("--new-puk <PUK>")]
    [Description("New PUK.")]
    public string? NewPuk { get; set; }
}

public sealed class PivUnblockPinSettings : GlobalSettings
{
    [CommandOption("--puk <PUK>")]
    [Description("PUK to unblock with.")]
    public string? Puk { get; set; }

    [CommandOption("--new-pin <PIN>")]
    [Description("New PIN after unblock.")]
    public string? NewPin { get; set; }
}

public sealed class PivManagementKeySettings : GlobalSettings
{
    [CommandOption("--management-key <HEX>")]
    [Description("Current management key (hex).")]
    public string? ManagementKey { get; set; }

    [CommandOption("--new-management-key <HEX>")]
    [Description("New management key (hex).")]
    public string? NewManagementKey { get; set; }

    [CommandOption("--algorithm <ALGORITHM>")]
    [Description("Key algorithm: 3des, aes128, aes192, aes256 (default: 3des).")]
    public string? Algorithm { get; set; }

    [CommandOption("--require-touch")]
    [Description("Require touch for management key operations.")]
    public bool RequireTouch { get; init; }
}

public sealed class PivSlotSettings : GlobalSettings
{
    [CommandArgument(0, "<SLOT>")]
    [Description("PIV slot: 9a, 9c, 9d, 9e, or 82-95 (retired).")]
    public string Slot { get; init; } = "";
}

public sealed class PivKeyGenerateSettings : GlobalSettings
{
    [CommandArgument(0, "<SLOT>")]
    [Description("PIV slot: 9a, 9c, 9d, 9e, or 82-95.")]
    public string Slot { get; init; } = "";

    [CommandOption("--algorithm <ALGORITHM>")]
    [Description("Key algorithm: rsa2048, rsa3072, rsa4096, eccp256, eccp384, ed25519, x25519.")]
    public string? Algorithm { get; set; }

    [CommandOption("--management-key <HEX>")]
    [Description("Management key (hex).")]
    public string? ManagementKey { get; set; }

    [CommandOption("--pin-policy <POLICY>")]
    [Description("PIN policy: default, never, once, always.")]
    public string? PinPolicy { get; set; }

    [CommandOption("--touch-policy <POLICY>")]
    [Description("Touch policy: default, never, always, cached.")]
    public string? TouchPolicy { get; set; }
}

public sealed class PivCertExportSettings : GlobalSettings
{
    [CommandArgument(0, "<SLOT>")]
    [Description("PIV slot to export from.")]
    public string Slot { get; init; } = "";

    [CommandOption("--output <PATH>")]
    [Description("Output file path (PEM format).")]
    public string? Output { get; set; }
}

public sealed class PivCertImportSettings : GlobalSettings
{
    [CommandArgument(0, "<SLOT>")]
    [Description("PIV slot to import to.")]
    public string Slot { get; init; } = "";

    [CommandOption("--cert-path <PATH>")]
    [Description("Path to certificate file.")]
    public string? CertPath { get; set; }

    [CommandOption("--management-key <HEX>")]
    [Description("Management key (hex).")]
    public string? ManagementKey { get; set; }
}

public sealed class PivCertDeleteSettings : GlobalSettings
{
    [CommandArgument(0, "<SLOT>")]
    [Description("PIV slot to delete certificate from.")]
    public string Slot { get; init; } = "";

    [CommandOption("--management-key <HEX>")]
    [Description("Management key (hex).")]
    public string? ManagementKey { get; set; }

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompt.")]
    public bool Force { get; init; }
}

// ── Helpers ─────────────────────────────────────────────────────────────────

public static class PivHelpers
{
    public static PivSlot ParseSlot(string slotStr)
    {
        var normalized = slotStr.ToUpperInvariant().TrimStart('0').TrimStart('X');
        return normalized switch
        {
            "9A" => PivSlot.Authentication,
            "9C" => PivSlot.Signature,
            "9D" => PivSlot.KeyManagement,
            "9E" => PivSlot.CardAuthentication,
            _ when byte.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, null, out var b)
                   && b is >= 0x82 and <= 0x95 => (PivSlot)b,
            _ => throw new ArgumentException($"Invalid PIV slot: {slotStr}. Use 9a, 9c, 9d, 9e, or 82-95.")
        };
    }

    public static string FormatSlot(PivSlot slot) => $"0x{(byte)slot:X2}";

    public static PivAlgorithm ParseAlgorithm(string? algorithm)
    {
        if (string.IsNullOrEmpty(algorithm))
        {
            throw new ArgumentException("--algorithm is required.");
        }

        return algorithm.ToLowerInvariant() switch
        {
            "rsa2048" => PivAlgorithm.Rsa2048,
            "rsa3072" => PivAlgorithm.Rsa3072,
            "rsa4096" => PivAlgorithm.Rsa4096,
            "eccp256" or "ecp256" or "p256" => PivAlgorithm.EccP256,
            "eccp384" or "ecp384" or "p384" => PivAlgorithm.EccP384,
            "ed25519" => PivAlgorithm.Ed25519,
            "x25519" => PivAlgorithm.X25519,
            _ => throw new ArgumentException(
                $"Unknown algorithm: {algorithm}. Use rsa2048, rsa3072, rsa4096, eccp256, eccp384, ed25519, x25519.")
        };
    }

    public static PivPinPolicy ParsePinPolicy(string? policy) =>
        policy?.ToLowerInvariant() switch
        {
            null or "default" => PivPinPolicy.Default,
            "never" => PivPinPolicy.Never,
            "once" => PivPinPolicy.Once,
            "always" => PivPinPolicy.Always,
            _ => throw new ArgumentException($"Unknown pin policy: {policy}. Use default, never, once, always.")
        };

    public static PivTouchPolicy ParseTouchPolicy(string? policy) =>
        policy?.ToLowerInvariant() switch
        {
            null or "default" => PivTouchPolicy.Default,
            "never" => PivTouchPolicy.Never,
            "always" => PivTouchPolicy.Always,
            "cached" => PivTouchPolicy.Cached,
            _ => throw new ArgumentException($"Unknown touch policy: {policy}. Use default, never, always, cached.")
        };

    public static PivManagementKeyType ParseManagementKeyType(string? algorithm) =>
        algorithm?.ToLowerInvariant() switch
        {
            null or "3des" => PivManagementKeyType.TripleDes,
            "aes128" => PivManagementKeyType.Aes128,
            "aes192" => PivManagementKeyType.Aes192,
            "aes256" => PivManagementKeyType.Aes256,
            _ => throw new ArgumentException($"Unknown key type: {algorithm}. Use 3des, aes128, aes192, aes256.")
        };

    public static byte[]? ParseHex(string? hex, string name)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return null;
        }

        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            throw new ArgumentException($"Invalid hex format for {name}: {hex}");
        }
    }
}

// ── Commands ────────────────────────────────────────────────────────────────

public sealed class PivInfoCommand : YkCommandBase<GlobalSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreatePivSessionAsync();

        OutputHelpers.WriteHeader("PIV Application");

        // Device info from management session
        if (deviceContext.Info is { } info)
        {
            OutputHelpers.WriteKeyValue("Firmware", info.VersionName);
            OutputHelpers.WriteKeyValue("Serial", info.SerialNumber?.ToString() ?? "N/A");
        }

        OutputHelpers.WriteKeyValue("Management Key Type", session.ManagementKeyType.ToString());

        // PIN retries
        try
        {
            var pinRetries = await session.GetPinAttemptsAsync();
            OutputHelpers.WriteKeyValue("PIN Retries Remaining", pinRetries.ToString());
        }
        catch
        {
            OutputHelpers.WriteKeyValue("PIN Retries Remaining", "N/A");
        }

        // PUK retries
        try
        {
            var pukMeta = await session.GetPukMetadataAsync();
            OutputHelpers.WriteKeyValue("PUK Retries Remaining", pukMeta.RetriesRemaining.ToString());
        }
        catch
        {
            OutputHelpers.WriteKeyValue("PUK Retries Remaining", "N/A");
        }

        // Slot overview
        AnsiConsole.WriteLine();
        OutputHelpers.WriteHeader("Slot Overview");

        PivSlot[] knownSlots = [PivSlot.Authentication, PivSlot.Signature, PivSlot.KeyManagement, PivSlot.CardAuthentication];
        string[] slotNames = ["Authentication (9a)", "Signing (9c)", "Key Management (9d)", "Card Auth (9e)"];

        for (int i = 0; i < knownSlots.Length; i++)
        {
            try
            {
                var metadata = await session.GetSlotMetadataAsync(knownSlots[i]);
                if (metadata is { } m)
                {
                    OutputHelpers.WriteKeyValueMarkup(
                        slotNames[i],
                        $"[green]{m.Algorithm}[/] PIN={m.PinPolicy} Touch={m.TouchPolicy}");
                }
                else
                {
                    OutputHelpers.WriteKeyValueMarkup(slotNames[i], "[grey]Empty[/]");
                }
            }
            catch
            {
                OutputHelpers.WriteKeyValueMarkup(slotNames[i], "[grey]Empty[/]");
            }
        }

        return ExitCode.Success;
    }
}

public sealed class PivResetCommand : YkCommandBase<PivResetSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivResetSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDestructive(
                    "reset the PIV application, deleting ALL keys, certificates, and resetting PIN/PUK"))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreatePivSessionAsync();
        await session.ResetAsync();

        OutputHelpers.WriteSuccess("PIV application has been reset to factory defaults.");
        OutputHelpers.WriteInfo("Default PIN: 123456 | Default PUK: 12345678");
        return ExitCode.Success;
    }
}

public sealed class PivAccessChangePinCommand : YkCommandBase<PivPinSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivPinSettings settings, YkDeviceContext deviceContext)
    {
        var pin = settings.Pin ?? PinPrompt.PromptForPin("Current PIN");
        var newPin = settings.NewPin ?? PinPrompt.PromptForPin("New PIN");

        var pinBytes = Encoding.UTF8.GetBytes(pin);
        var newPinBytes = Encoding.UTF8.GetBytes(newPin);

        try
        {
            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.ChangePinAsync(pinBytes, newPinBytes);

            OutputHelpers.WriteSuccess("PIN changed successfully.");
            return ExitCode.Success;
        }
        catch (InvalidPinException ex)
        {
            OutputHelpers.WriteError($"Invalid current PIN. {ex.RetriesRemaining} retries remaining.");
            return ExitCode.AuthenticationFailed;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pinBytes);
            CryptographicOperations.ZeroMemory(newPinBytes);
        }
    }
}

public sealed class PivAccessChangePukCommand : YkCommandBase<PivPukSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivPukSettings settings, YkDeviceContext deviceContext)
    {
        var puk = settings.Puk ?? PinPrompt.PromptForPin("Current PUK");
        var newPuk = settings.NewPuk ?? PinPrompt.PromptForPin("New PUK");

        var pukBytes = Encoding.UTF8.GetBytes(puk);
        var newPukBytes = Encoding.UTF8.GetBytes(newPuk);

        try
        {
            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.ChangePukAsync(pukBytes, newPukBytes);

            OutputHelpers.WriteSuccess("PUK changed successfully.");
            return ExitCode.Success;
        }
        catch (InvalidPinException ex)
        {
            OutputHelpers.WriteError($"Invalid current PUK. {ex.RetriesRemaining} retries remaining.");
            return ExitCode.AuthenticationFailed;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pukBytes);
            CryptographicOperations.ZeroMemory(newPukBytes);
        }
    }
}

public sealed class PivAccessUnblockPinCommand : YkCommandBase<PivUnblockPinSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivUnblockPinSettings settings, YkDeviceContext deviceContext)
    {
        var puk = settings.Puk ?? PinPrompt.PromptForPin("PUK");
        var newPin = settings.NewPin ?? PinPrompt.PromptForPin("New PIN");

        var pukBytes = Encoding.UTF8.GetBytes(puk);
        var newPinBytes = Encoding.UTF8.GetBytes(newPin);

        try
        {
            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.UnblockPinAsync(pukBytes, newPinBytes);

            OutputHelpers.WriteSuccess("PIN unblocked and changed successfully.");
            return ExitCode.Success;
        }
        catch (InvalidPinException ex)
        {
            OutputHelpers.WriteError($"Invalid PUK. {ex.RetriesRemaining} retries remaining.");
            return ExitCode.AuthenticationFailed;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pukBytes);
            CryptographicOperations.ZeroMemory(newPinBytes);
        }
    }
}

public sealed class PivAccessSetManagementKeyCommand : YkCommandBase<PivManagementKeySettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivManagementKeySettings settings, YkDeviceContext deviceContext)
    {
        var currentKey = PivHelpers.ParseHex(settings.ManagementKey, "management-key");
        if (currentKey is null)
        {
            OutputHelpers.WriteError("--management-key is required.");
            return ExitCode.GenericError;
        }

        var newKey = PivHelpers.ParseHex(settings.NewManagementKey, "new-management-key");
        if (newKey is null)
        {
            OutputHelpers.WriteError("--new-management-key is required.");
            CryptographicOperations.ZeroMemory(currentKey);
            return ExitCode.GenericError;
        }

        var keyType = PivHelpers.ParseManagementKeyType(settings.Algorithm);

        try
        {
            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.AuthenticateAsync(currentKey);
            await session.SetManagementKeyAsync(keyType, newKey, settings.RequireTouch);

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

public sealed class PivKeysGenerateCommand : YkCommandBase<PivKeyGenerateSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivKeyGenerateSettings settings, YkDeviceContext deviceContext)
    {
        var slot = PivHelpers.ParseSlot(settings.Slot);
        var algorithm = PivHelpers.ParseAlgorithm(settings.Algorithm);
        var pinPolicy = PivHelpers.ParsePinPolicy(settings.PinPolicy);
        var touchPolicy = PivHelpers.ParseTouchPolicy(settings.TouchPolicy);

        var mgmtKey = PivHelpers.ParseHex(settings.ManagementKey, "management-key");
        if (mgmtKey is null)
        {
            OutputHelpers.WriteError("--management-key is required for key generation.");
            return ExitCode.GenericError;
        }

        try
        {
            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.AuthenticateAsync(mgmtKey);

            var publicKey = await session.GenerateKeyAsync(
                slot, algorithm, pinPolicy, touchPolicy);

            OutputHelpers.WriteSuccess(
                $"Key pair generated in slot {PivHelpers.FormatSlot(slot)}.");
            OutputHelpers.WriteKeyValue("Algorithm", algorithm.ToString());
            OutputHelpers.WriteKeyValue("PIN Policy", pinPolicy.ToString());
            OutputHelpers.WriteKeyValue("Touch Policy", touchPolicy.ToString());

            var spki = publicKey.ExportSubjectPublicKeyInfo();
            if (spki.Length > 0)
            {
                OutputHelpers.WriteKeyValue("Public Key (Base64)",
                    Convert.ToBase64String(spki));
            }

            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }
}

public sealed class PivKeysAttestCommand : YkCommandBase<PivSlotSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivSlotSettings settings, YkDeviceContext deviceContext)
    {
        var slot = PivHelpers.ParseSlot(settings.Slot);

        await using var session = await deviceContext.Device.CreatePivSessionAsync();
        var cert = await session.AttestKeyAsync(slot);

        OutputHelpers.WriteHeader($"Attestation — Slot {PivHelpers.FormatSlot(slot)}");
        OutputHelpers.WriteKeyValue("Subject", cert.Subject);
        OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
        OutputHelpers.WriteKeyValue("Serial", cert.SerialNumber);
        OutputHelpers.WriteKeyValue("Not Before", cert.NotBefore.ToString("O"));
        OutputHelpers.WriteKeyValue("Not After", cert.NotAfter.ToString("O"));

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine(cert.ExportCertificatePem());

        return ExitCode.Success;
    }
}

public sealed class PivCertificatesExportCommand : YkCommandBase<PivCertExportSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivCertExportSettings settings, YkDeviceContext deviceContext)
    {
        var slot = PivHelpers.ParseSlot(settings.Slot);

        await using var session = await deviceContext.Device.CreatePivSessionAsync();
        var cert = await session.GetCertificateAsync(slot);

        if (cert is null)
        {
            OutputHelpers.WriteError($"No certificate in slot {PivHelpers.FormatSlot(slot)}.");
            return ExitCode.GenericError;
        }

        var pem = cert.ExportCertificatePem();

        if (settings.Output is not null)
        {
            await File.WriteAllTextAsync(settings.Output, pem);
            OutputHelpers.WriteSuccess($"Certificate exported to: {settings.Output}");
        }
        else
        {
            AnsiConsole.WriteLine(pem);
        }

        return ExitCode.Success;
    }
}

public sealed class PivCertificatesImportCommand : YkCommandBase<PivCertImportSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivCertImportSettings settings, YkDeviceContext deviceContext)
    {
        var slot = PivHelpers.ParseSlot(settings.Slot);

        if (string.IsNullOrEmpty(settings.CertPath))
        {
            OutputHelpers.WriteError("--cert-path is required.");
            return ExitCode.GenericError;
        }

        if (!File.Exists(settings.CertPath))
        {
            OutputHelpers.WriteError($"Certificate file not found: {settings.CertPath}");
            return ExitCode.GenericError;
        }

        var mgmtKey = PivHelpers.ParseHex(settings.ManagementKey, "management-key");
        if (mgmtKey is null)
        {
            OutputHelpers.WriteError("--management-key is required.");
            return ExitCode.GenericError;
        }

        try
        {
            var certData = await File.ReadAllBytesAsync(settings.CertPath);
            var cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificate(certData);

            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.AuthenticateAsync(mgmtKey);
            await session.StoreCertificateAsync(slot, cert);

            OutputHelpers.WriteSuccess(
                $"Certificate imported to slot {PivHelpers.FormatSlot(slot)}.");
            OutputHelpers.WriteKeyValue("Subject", cert.Subject);
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }
}

public sealed class PivCertificatesDeleteCommand : YkCommandBase<PivCertDeleteSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivCertDeleteSettings settings, YkDeviceContext deviceContext)
    {
        var slot = PivHelpers.ParseSlot(settings.Slot);

        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDangerous(
                    $"delete certificate from slot {PivHelpers.FormatSlot(slot)}"))
            {
                OutputHelpers.WriteInfo("Delete cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        var mgmtKey = PivHelpers.ParseHex(settings.ManagementKey, "management-key");
        if (mgmtKey is null)
        {
            OutputHelpers.WriteError("--management-key is required.");
            return ExitCode.GenericError;
        }

        try
        {
            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.AuthenticateAsync(mgmtKey);
            await session.DeleteCertificateAsync(slot);

            OutputHelpers.WriteSuccess(
                $"Certificate deleted from slot {PivHelpers.FormatSlot(slot)}.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }
}
