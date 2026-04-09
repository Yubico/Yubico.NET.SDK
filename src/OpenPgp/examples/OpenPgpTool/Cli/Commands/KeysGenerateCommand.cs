// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Generates a key on the YubiKey (openpgp keys generate).
/// </summary>
public sealed class KeysGenerateCommand : OpenPgpCommand<KeysGenerateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Key slot (sig, dec, aut).")]
        public string Key { get; init; } = "";

        [CommandOption("--algorithm <ALG>")]
        [Description("Algorithm (RSA2048, RSA3072, RSA4096, ECCP256, ECCP384, ECCP521, Ed25519, X25519).")]
        [DefaultValue("RSA2048")]
        public string Algorithm { get; init; } = "RSA2048";

        [CommandOption("--admin-pin <PIN>")]
        [Description("Admin PIN (prompted if not provided).")]
        public string? AdminPin { get; init; }
    }

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context,
        Settings settings,
        IOpenPgpSession session)
    {
        var keyRef = ParseKeyRef(settings.Key);

        var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        await session.VerifyAdminAsync(Encoding.UTF8.GetBytes(adminPin));

        var alg = settings.Algorithm.ToUpperInvariant();

        await AnsiConsole.Status()
            .StartAsync($"Generating {alg} key in {FormatKeyRef(keyRef)}...", async _ =>
            {
                switch (alg)
                {
                    case "RSA2048":
                        await session.GenerateRsaKeyAsync(keyRef, RsaSize.Rsa2048);
                        break;
                    case "RSA3072":
                        await session.GenerateRsaKeyAsync(keyRef, RsaSize.Rsa3072);
                        break;
                    case "RSA4096":
                        await session.GenerateRsaKeyAsync(keyRef, RsaSize.Rsa4096);
                        break;
                    case "ECCP256":
                        await session.GenerateEcKeyAsync(keyRef, CurveOid.Secp256R1);
                        break;
                    case "ECCP384":
                        await session.GenerateEcKeyAsync(keyRef, CurveOid.Secp384R1);
                        break;
                    case "ECCP521":
                        await session.GenerateEcKeyAsync(keyRef, CurveOid.Secp521R1);
                        break;
                    case "ED25519":
                        await session.GenerateEcKeyAsync(keyRef, CurveOid.Ed25519);
                        break;
                    case "X25519":
                        await session.GenerateEcKeyAsync(keyRef, CurveOid.X25519);
                        break;
                    default:
                        throw new ArgumentException(
                            $"Unsupported algorithm: {settings.Algorithm}. " +
                            "Use RSA2048, RSA3072, RSA4096, ECCP256, ECCP384, ECCP521, Ed25519, or X25519.");
                }
            });

        OutputHelpers.WriteSuccess(
            $"{alg} key generated in {FormatKeyRef(keyRef)} slot.");
        return 0;
    }
}