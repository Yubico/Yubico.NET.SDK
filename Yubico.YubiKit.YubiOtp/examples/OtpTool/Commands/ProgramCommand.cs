// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Security.Cryptography;
using System.Text;
using Spectre.Console;

namespace Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

/// <summary>
/// Programs OTP slots with various configuration types.
/// </summary>
public static class ProgramCommand
{
    public static async Task<int> RunAsync(CliOptions options, CancellationToken ct)
    {
        var slot = options.Slot ?? throw new ArgumentException("--slot is required for program command.");
        var subCommand = options.SubCommand ?? throw new ArgumentException("Specify program type: otp, hmac, static, hotp");

        var accessCode = OutputHelper.ParseHex(options.AccessCode, "access-code") ?? [];
        var newAccessCode = OutputHelper.ParseHex(options.NewAccessCode, "new-access-code") ?? [];

        await using var session = await DeviceHelper.CreateSessionAsync(options.Json, ct);
        if (session is null)
        {
            if (options.Json) OutputHelper.WriteJsonError("No YubiKey found.");
            else OutputHelper.WriteError("No YubiKey found.");
            return 1;
        }

        try
        {
            switch (subCommand)
            {
                case "otp":
                    await ProgramOtpAsync(session, slot, options, accessCode, newAccessCode, ct);
                    break;
                case "hmac":
                    await ProgramHmacAsync(session, slot, options, accessCode, newAccessCode, ct);
                    break;
                case "static":
                    await ProgramStaticAsync(session, slot, options, accessCode, newAccessCode, ct);
                    break;
                case "hotp":
                    await ProgramHotpAsync(session, slot, options, accessCode, newAccessCode, ct);
                    break;
                default:
                    throw new ArgumentException($"Unknown program type: {subCommand}. Use: otp, hmac, static, hotp");
            }

            if (options.Json)
            {
                OutputHelper.WriteJson(new { status = "ok", slot = slot.ToString(), type = subCommand });
            }
            else
            {
                OutputHelper.WriteSuccess($"Slot {slot} programmed with {subCommand} configuration.");
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (options.Json) OutputHelper.WriteJsonError(ex.Message);
            else OutputHelper.WriteError(ex.Message);
            return 1;
        }
    }

    public static async Task RunInteractiveAsync(CancellationToken ct)
    {
        var typeChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select configuration type:")
                .AddChoices(
                [
                    "Yubico OTP",
                    "HMAC-SHA1 Challenge-Response",
                    "Static Password",
                    "HOTP",
                    "Cancel"
                ]));

        if (typeChoice == "Cancel")
        {
            return;
        }

        var slot = OutputHelper.PromptSlot();
        var accessCodeBytes = PromptOptionalAccessCode("Current access code");
        var newAccessCodeBytes = PromptOptionalAccessCode("New access code");

        await using var session = await DeviceHelper.CreateSessionAsync(jsonMode: false, ct);
        if (session is null)
        {
            return;
        }

        try
        {
            switch (typeChoice)
            {
                case "Yubico OTP":
                    await ProgramOtpInteractiveAsync(session, slot, accessCodeBytes, newAccessCodeBytes, ct);
                    break;
                case "HMAC-SHA1 Challenge-Response":
                    await ProgramHmacInteractiveAsync(session, slot, accessCodeBytes, newAccessCodeBytes, ct);
                    break;
                case "Static Password":
                    await ProgramStaticInteractiveAsync(session, slot, accessCodeBytes, newAccessCodeBytes, ct);
                    break;
                case "HOTP":
                    await ProgramHotpInteractiveAsync(session, slot, accessCodeBytes, newAccessCodeBytes, ct);
                    break;
            }

            OutputHelper.WriteSuccess($"Slot {slot} programmed successfully.");
        }
        catch (Exception ex)
        {
            OutputHelper.WriteError(ex.Message);
        }
    }

    private static async Task ProgramOtpAsync(
        IYubiOtpSession session, Slot slot, CliOptions options,
        byte[] accessCode, byte[] newAccessCode, CancellationToken ct)
    {
        byte[] publicId;
        byte[] privateId;
        byte[] aesKey;

        if (options.Generate)
        {
            publicId = RandomNumberGenerator.GetBytes(6);
            privateId = RandomNumberGenerator.GetBytes(6);
            aesKey = RandomNumberGenerator.GetBytes(16);

            if (!options.Json)
            {
                OutputHelper.WriteKeyValue("Generated Public ID", Convert.ToHexString(publicId));
                OutputHelper.WriteKeyValue("Generated Private ID", Convert.ToHexString(privateId));
                OutputHelper.WriteKeyValue("Generated AES Key", Convert.ToHexString(aesKey));
            }
        }
        else
        {
            publicId = OutputHelper.ParseHex(options.PublicId, "public-id")
                       ?? throw new ArgumentException("--public-id or --generate is required for OTP programming.");
            privateId = OutputHelper.ParseHex(options.PrivateId, "private-id")
                        ?? throw new ArgumentException("--private-id is required for OTP programming.");
            aesKey = OutputHelper.ParseHex(options.Key, "key")
                     ?? throw new ArgumentException("--key is required for OTP programming.");
        }

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        if (options.AppendCr) config.AppendCr();

        await session.PutConfigurationAsync(slot, config, newAccessCode, accessCode, ct);

        CryptographicOperations.ZeroMemory(aesKey);
        CryptographicOperations.ZeroMemory(privateId);
    }

    private static async Task ProgramHmacAsync(
        IYubiOtpSession session, Slot slot, CliOptions options,
        byte[] accessCode, byte[] newAccessCode, CancellationToken ct)
    {
        byte[] hmacKey;

        if (options.Generate)
        {
            hmacKey = RandomNumberGenerator.GetBytes(20);

            if (!options.Json)
            {
                OutputHelper.WriteKeyValue("Generated HMAC Key", Convert.ToHexString(hmacKey));
            }
        }
        else
        {
            hmacKey = OutputHelper.ParseHex(options.Key, "key")
                      ?? throw new ArgumentException("--key or --generate is required for HMAC-SHA1 programming.");
        }

        using var config = new HmacSha1SlotConfiguration(hmacKey);
        if (options.RequireTouch) config.RequireTouch();
        if (options.ShortChallenge) config.UseShortChallenge();

        await session.PutConfigurationAsync(slot, config, newAccessCode, accessCode, ct);

        CryptographicOperations.ZeroMemory(hmacKey);
    }

    private static async Task ProgramStaticAsync(
        IYubiOtpSession session, Slot slot, CliOptions options,
        byte[] accessCode, byte[] newAccessCode, CancellationToken ct)
    {
        var password = options.Password
                       ?? throw new ArgumentException("--password is required for static password programming.");

        var scanCodes = Encoding.UTF8.GetBytes(password);

        try
        {
            using var config = new StaticPasswordSlotConfiguration(scanCodes);
            if (options.AppendCr) config.AppendCr();

            await session.PutConfigurationAsync(slot, config, newAccessCode, accessCode, ct);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(scanCodes);
        }
    }

    private static async Task ProgramHotpAsync(
        IYubiOtpSession session, Slot slot, CliOptions options,
        byte[] accessCode, byte[] newAccessCode, CancellationToken ct)
    {
        byte[] hmacKey;

        if (options.Generate)
        {
            hmacKey = RandomNumberGenerator.GetBytes(20);

            if (!options.Json)
            {
                OutputHelper.WriteKeyValue("Generated HMAC Key", Convert.ToHexString(hmacKey));
            }
        }
        else
        {
            hmacKey = OutputHelper.ParseHex(options.Key, "key")
                      ?? throw new ArgumentException("--key or --generate is required for HOTP programming.");
        }

        using var config = new HotpSlotConfiguration(hmacKey, options.Imf ?? 0);
        if (options.Digits == 8) config.Use8Digits();
        if (options.AppendCr) config.AppendCr();

        await session.PutConfigurationAsync(slot, config, newAccessCode, accessCode, ct);

        CryptographicOperations.ZeroMemory(hmacKey);
    }

    private static async Task ProgramOtpInteractiveAsync(
        IYubiOtpSession session, Slot slot,
        byte[] accessCode, byte[] newAccessCode, CancellationToken ct)
    {
        bool generate = AnsiConsole.Confirm("Generate random keys?", defaultValue: true);
        byte[] publicId, privateId, aesKey;

        if (generate)
        {
            publicId = RandomNumberGenerator.GetBytes(6);
            privateId = RandomNumberGenerator.GetBytes(6);
            aesKey = RandomNumberGenerator.GetBytes(16);

            OutputHelper.WriteKeyValue("Public ID", Convert.ToHexString(publicId));
            OutputHelper.WriteKeyValue("Private ID", Convert.ToHexString(privateId));
            OutputHelper.WriteKeyValue("AES Key", Convert.ToHexString(aesKey));
        }
        else
        {
            publicId = OutputHelper.PromptHex("Public ID") ?? throw new InvalidOperationException("Public ID required.");
            privateId = OutputHelper.PromptHex("Private ID") ?? throw new InvalidOperationException("Private ID required.");
            aesKey = OutputHelper.PromptHex("AES Key") ?? throw new InvalidOperationException("AES Key required.");
        }

        bool appendCr = AnsiConsole.Confirm("Append carriage return?", defaultValue: false);

        using var config = new YubiOtpSlotConfiguration(publicId, privateId, aesKey);
        if (appendCr) config.AppendCr();

        await session.PutConfigurationAsync(slot, config, newAccessCode, accessCode, ct);

        CryptographicOperations.ZeroMemory(aesKey);
        CryptographicOperations.ZeroMemory(privateId);
    }

    private static async Task ProgramHmacInteractiveAsync(
        IYubiOtpSession session, Slot slot,
        byte[] accessCode, byte[] newAccessCode, CancellationToken ct)
    {
        bool generate = AnsiConsole.Confirm("Generate random key?", defaultValue: true);
        byte[] hmacKey;

        if (generate)
        {
            hmacKey = RandomNumberGenerator.GetBytes(20);
            OutputHelper.WriteKeyValue("HMAC Key", Convert.ToHexString(hmacKey));
        }
        else
        {
            hmacKey = OutputHelper.PromptHex("HMAC Key (20 bytes)")
                      ?? throw new InvalidOperationException("HMAC Key required.");
        }

        bool requireTouch = AnsiConsole.Confirm("Require touch?", defaultValue: false);

        using var config = new HmacSha1SlotConfiguration(hmacKey);
        if (requireTouch) config.RequireTouch();
        config.UseShortChallenge();

        await session.PutConfigurationAsync(slot, config, newAccessCode, accessCode, ct);

        CryptographicOperations.ZeroMemory(hmacKey);
    }

    private static async Task ProgramStaticInteractiveAsync(
        IYubiOtpSession session, Slot slot,
        byte[] accessCode, byte[] newAccessCode, CancellationToken ct)
    {
        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("[grey]Password:[/]")
                .Secret());

        var scanCodes = Encoding.UTF8.GetBytes(password);
        bool appendCr = AnsiConsole.Confirm("Append carriage return?", defaultValue: true);

        try
        {
            using var config = new StaticPasswordSlotConfiguration(scanCodes);
            if (appendCr) config.AppendCr();

            await session.PutConfigurationAsync(slot, config, newAccessCode, accessCode, ct);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(scanCodes);
        }
    }

    private static async Task ProgramHotpInteractiveAsync(
        IYubiOtpSession session, Slot slot,
        byte[] accessCode, byte[] newAccessCode, CancellationToken ct)
    {
        bool generate = AnsiConsole.Confirm("Generate random key?", defaultValue: true);
        byte[] hmacKey;

        if (generate)
        {
            hmacKey = RandomNumberGenerator.GetBytes(20);
            OutputHelper.WriteKeyValue("HMAC Key", Convert.ToHexString(hmacKey));
        }
        else
        {
            hmacKey = OutputHelper.PromptHex("HMAC Key (20 bytes)")
                      ?? throw new InvalidOperationException("HMAC Key required.");
        }

        var digitChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("OTP digit count:")
                .AddChoices(["6 digits", "8 digits"]));

        bool appendCr = AnsiConsole.Confirm("Append carriage return?", defaultValue: true);

        using var config = new HotpSlotConfiguration(hmacKey);
        if (digitChoice == "8 digits") config.Use8Digits();
        if (appendCr) config.AppendCr();

        await session.PutConfigurationAsync(slot, config, newAccessCode, accessCode, ct);

        CryptographicOperations.ZeroMemory(hmacKey);
    }

    private static byte[] PromptOptionalAccessCode(string label)
    {
        if (!AnsiConsole.Confirm($"Provide {label.ToLowerInvariant()}?", defaultValue: false))
        {
            return [];
        }

        return OutputHelper.PromptHex(label) ?? [];
    }
}
