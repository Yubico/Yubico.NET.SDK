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
using Yubico.YubiKit.YubiOtp;

namespace Yubico.YubiKit.Cli.Commands.Otp;

// ── Settings ────────────────────────────────────────────────────────────────

public class OtpSlotSettings : GlobalSettings
{
    [CommandArgument(0, "<SLOT>")]
    [Description("Slot number (1 or 2).")]
    public int Slot { get; init; }

    [CommandOption("--access-code <HEX>")]
    [Description("6-byte access code for protected slot operations (hex).")]
    public string? AccessCode { get; set; }

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompts.")]
    public bool Force { get; init; }
}

public sealed class OtpSwapSettings : GlobalSettings
{
    [CommandOption("--access-code <HEX>")]
    [Description("6-byte access code for protected slot operations (hex).")]
    public string? AccessCode { get; set; }

    [CommandOption("-f|--force")]
    [Description("Skip confirmation prompts.")]
    public bool Force { get; init; }
}

public sealed class OtpChalRespSettings : OtpSlotSettings
{
    [CommandOption("--key <HEX>")]
    [Description("20-byte HMAC-SHA1 key (hex).")]
    public string? Key { get; set; }

    [CommandOption("--generate")]
    [Description("Generate a random HMAC key.")]
    public bool Generate { get; init; }

    [CommandOption("--require-touch")]
    [Description("Require physical touch for challenge-response.")]
    public bool RequireTouch { get; init; }

    [CommandOption("--totp")]
    [Description("TOTP mode (implies touch + short challenge).")]
    public bool Totp { get; init; }
}

public sealed class OtpHotpSettings : OtpSlotSettings
{
    [CommandOption("--key <HEX>")]
    [Description("20-byte HMAC key (hex).")]
    public string? Key { get; set; }

    [CommandOption("--generate")]
    [Description("Generate a random HMAC key.")]
    public bool Generate { get; init; }

    [CommandOption("--digits <DIGITS>")]
    [Description("OTP digit count: 6 or 8 (default: 6).")]
    public int? Digits { get; set; }

    [CommandOption("--imf <VALUE>")]
    [Description("Initial moving factor.")]
    public int? Imf { get; set; }
}

public sealed class OtpStaticSettings : OtpSlotSettings
{
    [CommandOption("--password <TEXT>")]
    [Description("Password text for static password.")]
    public string? Password { get; set; }

    [CommandOption("--generate")]
    [Description("Generate a random static password.")]
    public bool Generate { get; init; }

    [CommandOption("--length <N>")]
    [Description("Length of generated password (1-38).")]
    public int? Length { get; set; }

    [CommandOption("--keyboard-layout <LAYOUT>")]
    [Description("Keyboard layout (default: US).")]
    public string? KeyboardLayout { get; set; }
}

public sealed class OtpYubiOtpSettings : OtpSlotSettings
{
    [CommandOption("--public-id <HEX>")]
    [Description("Public ID (hex, up to 16 bytes).")]
    public string? PublicId { get; set; }

    [CommandOption("--serial-public-id")]
    [Description("Use device serial number as public ID prefix.")]
    public bool SerialPublicId { get; init; }

    [CommandOption("--private-id <HEX>")]
    [Description("Private ID (hex, 6 bytes).")]
    public string? PrivateId { get; set; }

    [CommandOption("--generate-private-id")]
    [Description("Generate random private ID.")]
    public bool GeneratePrivateId { get; init; }

    [CommandOption("--key <HEX>")]
    [Description("AES key (hex, 16 bytes).")]
    public string? Key { get; set; }

    [CommandOption("--generate-key")]
    [Description("Generate random AES key.")]
    public bool GenerateKey { get; init; }
}

public sealed class OtpCalculateSettings : OtpSlotSettings
{
    [CommandArgument(1, "[CHALLENGE]")]
    [Description("Hex-encoded challenge for HMAC-SHA1 calculation.")]
    public string? Challenge { get; set; }
}

public sealed class OtpNdefSettings : OtpSlotSettings
{
    [CommandOption("--prefix <URI_PREFIX>")]
    [Description("URI prefix for NDEF configuration.")]
    public string? Prefix { get; set; }

    [CommandOption("--ndef-type <TYPE>")]
    [Description("NDEF record type: URI or TEXT (default: URI).")]
    public string? NdefType { get; set; }
}

public sealed class OtpSettingsSettings : OtpSlotSettings
{
    [CommandOption("--pacing <MS>")]
    [Description("Keystroke pacing delay: 10 or 20 ms.")]
    public int? Pacing { get; set; }

    [CommandOption("--no-enter")]
    [Description("Disable carriage return after output.")]
    public bool NoEnter { get; init; }
}

// ── Helpers ─────────────────────────────────────────────────────────────────

public static class OtpHelpers
{
    private const string ModHexChars = "cbdefghijklnrtuv";

    public static Slot ParseSlot(int slot) =>
        slot switch
        {
            1 => Slot.One,
            2 => Slot.Two,
            _ => throw new ArgumentException($"Invalid slot: {slot}. Must be 1 or 2.")
        };

    public static string FormatSlot(Slot slot) =>
        slot == Slot.One ? "1" : "2";

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

    public static byte[] RequireHex(string? hex, string name)
    {
        if (string.IsNullOrEmpty(hex))
        {
            throw new ArgumentException($"--{name} is required.");
        }

        return ParseHex(hex, name)!;
    }

    public static string GenerateStaticPassword(int length)
    {
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        var sb = new StringBuilder(length);
        foreach (byte b in randomBytes)
        {
            sb.Append(ModHexChars[b % ModHexChars.Length]);
        }

        CryptographicOperations.ZeroMemory(randomBytes);
        return sb.ToString();
    }

    public static byte[] EncodeSerialAsPublicId(int serial)
    {
        byte[] publicId = new byte[6];
        publicId[2] = (byte)((serial >> 24) & 0xFF);
        publicId[3] = (byte)((serial >> 16) & 0xFF);
        publicId[4] = (byte)((serial >> 8) & 0xFF);
        publicId[5] = (byte)(serial & 0xFF);
        return publicId;
    }
}

// ── Commands ────────────────────────────────────────────────────────────────

public sealed class OtpInfoCommand : YkCommandBase<GlobalSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, GlobalSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();

        var state = session.GetConfigState();

        OutputHelpers.WriteHeader("OTP Slot Status");
        OutputHelpers.WriteKeyValue("Firmware", state.FirmwareVersion.ToString());

        AnsiConsole.WriteLine();

        var table = OutputHelpers.CreateTable("Property", "Slot 1", "Slot 2");

        try
        {
            table.AddRow(
                "Configured",
                FormatBool(state.IsConfigured(Slot.One)),
                FormatBool(state.IsConfigured(Slot.Two)));
        }
        catch (InvalidOperationException)
        {
            table.AddRow("Configured", "[grey]N/A[/]", "[grey]N/A[/]");
        }

        try
        {
            table.AddRow(
                "Touch Triggered",
                FormatBool(state.IsTouchTriggered(Slot.One)),
                FormatBool(state.IsTouchTriggered(Slot.Two)));
        }
        catch (InvalidOperationException)
        {
            table.AddRow("Touch Triggered", "[grey]N/A[/]", "[grey]N/A[/]");
        }

        table.AddRow("LED Inverted", FormatBool(state.IsLedInverted()), "");

        AnsiConsole.Write(table);
        return ExitCode.Success;
    }

    private static string FormatBool(bool value) =>
        value ? "[green]Yes[/]" : "[grey]No[/]";
}

public sealed class OtpSwapCommand : YkCommandBase<OtpSwapSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpSwapSettings settings, YkDeviceContext deviceContext)
    {
        if (!settings.Force)
        {
            if (!ConfirmationPrompts.ConfirmDangerous("swap slot 1 and slot 2 configurations"))
            {
                OutputHelpers.WriteInfo("Swap cancelled.");
                return ExitCode.UserCancelled;
            }
        }

        await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();
        await session.SwapSlotsAsync();

        OutputHelpers.WriteSuccess("Slot 1 and slot 2 configurations swapped.");
        return ExitCode.Success;
    }
}

public sealed class OtpDeleteCommand : YkCommandBase<OtpSlotSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpSlotSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);
        byte[] accessCode = OtpHelpers.ParseHex(settings.AccessCode, "access-code") ?? [];

        try
        {
            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous($"delete slot {OtpHelpers.FormatSlot(slot)} configuration"))
                {
                    OutputHelpers.WriteInfo("Delete cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();
            await session.DeleteSlotAsync(slot, accessCode);

            OutputHelpers.WriteSuccess($"Slot {OtpHelpers.FormatSlot(slot)} configuration deleted.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }
}

public sealed class OtpChalRespCommand : YkCommandBase<OtpChalRespSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpChalRespSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);
        byte[] hmacKey;

        if (settings.Generate)
        {
            hmacKey = RandomNumberGenerator.GetBytes(20);
            OutputHelpers.WriteHex("Generated HMAC key", hmacKey);
        }
        else
        {
            hmacKey = OtpHelpers.RequireHex(settings.Key, "key");
        }

        byte[] accessCode = OtpHelpers.ParseHex(settings.AccessCode, "access-code") ?? [];

        try
        {
            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous(
                        $"program slot {OtpHelpers.FormatSlot(slot)} with HMAC-SHA1 challenge-response"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();

            using var config = new HmacSha1SlotConfiguration(hmacKey);

            if (settings.RequireTouch || settings.Totp)
            {
                config.RequireTouch();
            }

            if (settings.Totp)
            {
                config.UseShortChallenge();
            }

            await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode);

            OutputHelpers.WriteSuccess(
                $"Slot {OtpHelpers.FormatSlot(slot)} programmed for HMAC-SHA1 challenge-response.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hmacKey);
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }
}

public sealed class OtpHotpCommand : YkCommandBase<OtpHotpSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpHotpSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);
        byte[] hmacKey;

        if (settings.Generate)
        {
            hmacKey = RandomNumberGenerator.GetBytes(20);
            OutputHelpers.WriteHex("Generated HMAC key", hmacKey);
        }
        else
        {
            hmacKey = OtpHelpers.RequireHex(settings.Key, "key");
        }

        byte[] accessCode = OtpHelpers.ParseHex(settings.AccessCode, "access-code") ?? [];

        try
        {
            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous(
                        $"program slot {OtpHelpers.FormatSlot(slot)} with HOTP"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();

            using var config = new HotpSlotConfiguration(hmacKey, settings.Imf ?? 0);

            if (settings.Digits == 8)
            {
                config.Use8Digits();
            }

            await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode);

            OutputHelpers.WriteSuccess($"Slot {OtpHelpers.FormatSlot(slot)} programmed for HOTP.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hmacKey);
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }
}

public sealed class OtpStaticCommand : YkCommandBase<OtpStaticSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpStaticSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);
        byte[] scanCodes;

        if (settings.Generate)
        {
            int length = settings.Length ?? 38;
            string generated = OtpHelpers.GenerateStaticPassword(length);
            OutputHelpers.WriteKeyValue("Generated password", generated);
            scanCodes = Encoding.UTF8.GetBytes(generated);
        }
        else
        {
            if (string.IsNullOrEmpty(settings.Password))
            {
                OutputHelpers.WriteError("--password or --generate is required.");
                return ExitCode.GenericError;
            }

            scanCodes = Encoding.UTF8.GetBytes(settings.Password);
        }

        byte[] accessCode = OtpHelpers.ParseHex(settings.AccessCode, "access-code") ?? [];

        try
        {
            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous(
                        $"program slot {OtpHelpers.FormatSlot(slot)} with static password"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();

            using var config = new StaticPasswordSlotConfiguration(scanCodes);

            await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode);

            OutputHelpers.WriteSuccess(
                $"Slot {OtpHelpers.FormatSlot(slot)} programmed with static password.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(scanCodes);
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }
}

public sealed class OtpYubiOtpCommand : YkCommandBase<OtpYubiOtpSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpYubiOtpSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);
        byte[]? publicId = null;
        byte[]? privateId = null;
        byte[]? aesKey = null;
        byte[] accessCode = OtpHelpers.ParseHex(settings.AccessCode, "access-code") ?? [];

        try
        {
            // Public ID
            if (!settings.SerialPublicId)
            {
                publicId = OtpHelpers.ParseHex(settings.PublicId, "public-id");
                if (publicId is null)
                {
                    OutputHelpers.WriteError("--public-id or --serial-public-id is required.");
                    return ExitCode.GenericError;
                }
            }

            // Private ID
            if (settings.GeneratePrivateId)
            {
                privateId = RandomNumberGenerator.GetBytes(6);
                OutputHelpers.WriteHex("Generated private ID", privateId);
            }
            else
            {
                privateId = OtpHelpers.ParseHex(settings.PrivateId, "private-id");
                if (privateId is null)
                {
                    OutputHelpers.WriteError("--private-id or --generate-private-id is required.");
                    return ExitCode.GenericError;
                }
            }

            // AES Key
            if (settings.GenerateKey)
            {
                aesKey = RandomNumberGenerator.GetBytes(16);
                OutputHelpers.WriteHex("Generated AES key", aesKey);
            }
            else
            {
                aesKey = OtpHelpers.ParseHex(settings.Key, "key");
                if (aesKey is null)
                {
                    OutputHelpers.WriteError("--key or --generate-key is required.");
                    return ExitCode.GenericError;
                }
            }

            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous(
                        $"program slot {OtpHelpers.FormatSlot(slot)} with Yubico OTP"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();

            // Resolve serial-based public ID
            if (settings.SerialPublicId)
            {
                int serial = await session.GetSerialAsync();
                publicId = OtpHelpers.EncodeSerialAsPublicId(serial);
                OutputHelpers.WriteHex("Serial-based public ID", publicId);
            }

            using var config = new YubiOtpSlotConfiguration(publicId!, privateId, aesKey);

            await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode);

            OutputHelpers.WriteSuccess(
                $"Slot {OtpHelpers.FormatSlot(slot)} programmed with Yubico OTP.");
            return ExitCode.Success;
        }
        finally
        {
            if (aesKey is not null) CryptographicOperations.ZeroMemory(aesKey);
            if (privateId is not null) CryptographicOperations.ZeroMemory(privateId);
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }
}

public sealed class OtpCalculateCommand : YkCommandBase<OtpCalculateSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpCalculateSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);

        if (string.IsNullOrEmpty(settings.Challenge))
        {
            OutputHelpers.WriteError("CHALLENGE argument is required (hex-encoded).");
            return ExitCode.GenericError;
        }

        byte[] challenge = OtpHelpers.RequireHex(settings.Challenge, "CHALLENGE");

        try
        {
            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();

            var response = await session.CalculateHmacSha1Async(slot, challenge);

            OutputHelpers.WriteHeader("HMAC-SHA1 Challenge-Response");
            OutputHelpers.WriteKeyValue("Slot", OtpHelpers.FormatSlot(slot));
            OutputHelpers.WriteHex("Challenge", challenge);
            OutputHelpers.WriteHex("Response", response.Span);

            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(challenge);
        }
    }
}

public sealed class OtpNdefCommand : YkCommandBase<OtpNdefSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpNdefSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);
        byte[] accessCode = OtpHelpers.ParseHex(settings.AccessCode, "access-code") ?? [];

        NdefType ndefType = settings.NdefType?.ToUpperInvariant() switch
        {
            null or "URI" => NdefType.Uri,
            "TEXT" => NdefType.Text,
            _ => throw new ArgumentException($"Invalid --ndef-type: {settings.NdefType}. Must be URI or TEXT.")
        };

        try
        {
            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous(
                        $"configure NDEF on slot {OtpHelpers.FormatSlot(slot)}"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();

            await session.SetNdefConfigurationAsync(slot, settings.Prefix, accessCode, ndefType);

            OutputHelpers.WriteSuccess($"Slot {OtpHelpers.FormatSlot(slot)} NDEF configured.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }
}

public sealed class OtpSettingsCommand : YkCommandBase<OtpSettingsSettings>
{
    protected override ConnectionType[] AppletTransports =>
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpSettingsSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);
        byte[] accessCode = OtpHelpers.ParseHex(settings.AccessCode, "access-code") ?? [];

        try
        {
            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous(
                        $"update settings on slot {OtpHelpers.FormatSlot(slot)}"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync();

            using var config = new UpdateConfiguration();

            if (settings.Pacing == 10)
            {
                config.PacingChar10();
            }
            else if (settings.Pacing == 20)
            {
                config.PacingChar20();
            }

            if (settings.NoEnter)
            {
                config.AppendCr(false);
            }

            await session.UpdateConfigurationAsync(slot, config, currentAccessCode: accessCode);

            OutputHelpers.WriteSuccess($"Slot {OtpHelpers.FormatSlot(slot)} settings updated.");
            return ExitCode.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }
}
