// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.Buffers;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
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

    [CommandOption("--key-type <KEY_TYPE>")]
    [Description("Key algorithm: 3des, aes128, aes192, aes256 (default: 3des).")]
    public string? KeyType { get; set; }

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

    [CommandOption("--key-type <KEY_TYPE>")]
    [Description("Key algorithm: rsa2048, rsa3072, rsa4096, eccp256, eccp384, ed25519, x25519.")]
    public string? KeyType { get; set; }

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
    public static Slot ParseSlot(string slotStr)
    {
        var normalized = slotStr.ToUpperInvariant().TrimStart('0').TrimStart('X');
        return normalized switch
        {
            "9A" => Slot.Authentication,
            "9C" => Slot.Signature,
            "9D" => Slot.KeyManagement,
            "9E" => Slot.CardAuthentication,
            _ when byte.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, null, out var b)
                   && b is >= 0x82 and <= 0x95 => (Slot)b,
            _ => throw new ArgumentException($"Invalid PIV slot: {slotStr}. Use 9a, 9c, 9d, 9e, or 82-95.")
        };
    }

    public static string FormatSlot(Slot slot) => $"0x{(byte)slot:X2}";

    private static readonly FrozenDictionary<string, KeyType> KeyTypeNames =
        new Dictionary<string, KeyType>
        {
            ["rsa2048"] = KeyType.Rsa2048,
            ["rsa3072"] = KeyType.Rsa3072,
            ["rsa4096"] = KeyType.Rsa4096,
            ["eccp256"] = KeyType.P256,
            ["ecp256"] = KeyType.P256,
            ["p256"] = KeyType.P256,
            ["eccp384"] = KeyType.P384,
            ["ecp384"] = KeyType.P384,
            ["p384"] = KeyType.P384,
            ["ed25519"] = KeyType.Ed25519,
            ["x25519"] = KeyType.X25519
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static KeyType ParseKeyType(string? keyType)
    {
        if (string.IsNullOrEmpty(keyType))
            throw new ArgumentException("--key-type is required.");
        return KeyTypeNames.TryGetValue(keyType, out var selected)
            ? selected
            : throw new ArgumentException($"Unknown key type: {keyType}. Use rsa2048, rsa3072, rsa4096, eccp256, eccp384, ed25519, x25519.");
    }

    public static PinPolicy ParsePinPolicy(string? policy) =>
        policy?.ToLowerInvariant() switch
        {
            null or "default" => PinPolicy.Default,
            "never" => PinPolicy.Never,
            "once" => PinPolicy.Once,
            "always" => PinPolicy.Always,
            _ => throw new ArgumentException($"Unknown pin policy: {policy}. Use default, never, once, always.")
        };

    public static TouchPolicy ParseTouchPolicy(string? policy) =>
        policy?.ToLowerInvariant() switch
        {
            null or "default" => TouchPolicy.Default,
            "never" => TouchPolicy.Never,
            "always" => TouchPolicy.Always,
            "cached" => TouchPolicy.Cached,
            _ => throw new ArgumentException($"Unknown touch policy: {policy}. Use default, never, always, cached.")
        };

    public static KeyType ParseManagementKeyType(string? keyType) =>
        keyType?.ToLowerInvariant() switch
        {
            null or "3des" => KeyType.TripleDes,
            "aes128" => KeyType.Aes128,
            "aes192" => KeyType.Aes192,
            "aes256" => KeyType.Aes256,
            _ => throw new ArgumentException($"Unknown key type: {keyType}. Use 3des, aes128, aes192, aes256.")
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

    public static IMemoryOwner<byte>? GetCredential(string? provided, string promptLabel) =>
        PinPrompt.Resolve(provided, promptLabel);
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

        Slot[] knownSlots = [Slot.Authentication, Slot.Signature, Slot.KeyManagement, Slot.CardAuthentication];
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
                        $"[green]{m.KeyType}[/] PIN={m.PinPolicy} Touch={m.TouchPolicy}");
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
        using var pin = PivHelpers.GetCredential(settings.Pin, "Current PIN");
        using var newPin = PivHelpers.GetCredential(settings.NewPin, "New PIN");

        if (pin is null || newPin is null)
        {
            OutputHelpers.WriteError("Current and new PINs are required.");
            return ExitCode.GenericError;
        }

        try
        {
            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.ChangePinAsync(pin.Memory, newPin.Memory);

            OutputHelpers.WriteSuccess("PIN changed successfully.");
            return ExitCode.Success;
        }
        catch (InvalidPinException ex)
        {
            OutputHelpers.WriteError($"Invalid current PIN. {ex.RetriesRemaining} retries remaining.");
            return ExitCode.AuthenticationFailed;
        }
    }
}

public sealed class PivAccessChangePukCommand : YkCommandBase<PivPukSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivPukSettings settings, YkDeviceContext deviceContext)
    {
        using var puk = PivHelpers.GetCredential(settings.Puk, "Current PUK");
        using var newPuk = PivHelpers.GetCredential(settings.NewPuk, "New PUK");

        if (puk is null || newPuk is null)
        {
            OutputHelpers.WriteError("Current and new PUKs are required.");
            return ExitCode.GenericError;
        }

        try
        {
            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.ChangePukAsync(puk.Memory, newPuk.Memory);

            OutputHelpers.WriteSuccess("PUK changed successfully.");
            return ExitCode.Success;
        }
        catch (InvalidPinException ex)
        {
            OutputHelpers.WriteError($"Invalid current PUK. {ex.RetriesRemaining} retries remaining.");
            return ExitCode.AuthenticationFailed;
        }
    }
}

public sealed class PivAccessUnblockPinCommand : YkCommandBase<PivUnblockPinSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, PivUnblockPinSettings settings, YkDeviceContext deviceContext)
    {
        using var puk = PivHelpers.GetCredential(settings.Puk, "PUK");
        using var newPin = PivHelpers.GetCredential(settings.NewPin, "New PIN");

        if (puk is null || newPin is null)
        {
            OutputHelpers.WriteError("PUK and new PIN are required.");
            return ExitCode.GenericError;
        }

        try
        {
            await using var session = await deviceContext.Device.CreatePivSessionAsync();
            await session.UnblockPinAsync(puk.Memory, newPin.Memory);

            OutputHelpers.WriteSuccess("PIN unblocked and changed successfully.");
            return ExitCode.Success;
        }
        catch (InvalidPinException ex)
        {
            OutputHelpers.WriteError($"Invalid PUK. {ex.RetriesRemaining} retries remaining.");
            return ExitCode.AuthenticationFailed;
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

        var keyType = PivHelpers.ParseManagementKeyType(settings.KeyType);

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
        var keyType = PivHelpers.ParseKeyType(settings.KeyType);
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
                slot,
                keyType,
                new KeyCreationOptions
                {
                    PinPolicy = pinPolicy,
                    TouchPolicy = touchPolicy
                });

            OutputHelpers.WriteSuccess(
                $"Key pair generated in slot {PivHelpers.FormatSlot(slot)}.");
            OutputHelpers.WriteKeyValue("Key type", keyType.ToString());
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