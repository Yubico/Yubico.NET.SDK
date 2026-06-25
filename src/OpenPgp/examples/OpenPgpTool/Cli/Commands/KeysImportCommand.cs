// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Imports a private key from a PEM file (openpgp keys import).
/// </summary>
public sealed class KeysImportCommand : OpenPgpCommand<KeysImportCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<KEY>")]
        [Description("Key slot (sig, dec, aut).")]
        public string Key { get; init; } = "";

        [CommandArgument(1, "<PEM_FILE>")]
        [Description("Path to PEM-encoded private key file.")]
        public string PemFile { get; init; } = "";

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

        if (!File.Exists(settings.PemFile))
        {
            OutputHelpers.WriteError($"File not found: {settings.PemFile}");
            return 1;
        }

        var pem = await File.ReadAllTextAsync(settings.PemFile);

        // Determine key type and create appropriate template
        PrivateKeyTemplate template;
        AlgorithmAttributes attributes;

        try
        {
            (template, attributes) = LoadPrivateKey(pem, keyRef);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to load private key: {ex.Message}");
            return 1;
        }

        using var adminPin = GetAdminPin(settings.AdminPin);
        if (adminPin is null)
        {
            OutputHelpers.WriteError("Admin PIN is required.");
            return 1;
        }

        await session.VerifyAdminAsync(adminPin.Memory);

        await AnsiConsole.Status()
            .StartAsync("Importing private key...", async _ =>
            {
                await session.PutKeyAsync(keyRef, template, attributes);
            });

        OutputHelpers.WriteSuccess(
            $"Private key imported to {FormatKeyRef(keyRef)} slot.");
        return 0;
    }

    private static (PrivateKeyTemplate Template, AlgorithmAttributes Attributes) LoadPrivateKey(
        string pem, KeyRef keyRef)
    {
        // Try RSA first
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            var parameters = rsa.ExportParameters(includePrivateParameters: true);

            var modulusLenBits = parameters.Modulus!.Length * 8;
            var size = modulusLenBits switch
            {
                2048 => RsaSize.Rsa2048,
                3072 => RsaSize.Rsa3072,
                4096 => RsaSize.Rsa4096,
                _ => throw new NotSupportedException(
                    $"Unsupported RSA key size: {modulusLenBits}")
            };

            var template = new RsaCrtKeyTemplate(
                keyRef,
                parameters.Exponent!,
                parameters.P!,
                parameters.Q!,
                parameters.InverseQ!,
                parameters.DP!,
                parameters.DQ!,
                parameters.Modulus!);

            var attributes = RsaAttributes.Create(size, RsaImportFormat.Crt);
            return (template, attributes);
        }
        catch (CryptographicException)
        {
            // Not RSA, try EC
        }

        try
        {
            using var ec = ECDsa.Create();
            ec.ImportFromPem(pem);
            var parameters = ec.ExportParameters(includePrivateParameters: true);

            var curve = parameters.Curve.Oid?.Value switch
            {
                "1.2.840.10045.3.1.7" => CurveOid.Secp256R1,
                "1.3.132.0.34" => CurveOid.Secp384R1,
                "1.3.132.0.35" => CurveOid.Secp521R1,
                _ => throw new NotSupportedException(
                    $"Unsupported curve: {parameters.Curve.Oid?.FriendlyName ?? "unknown"}")
            };

            var template = new EcKeyTemplate(keyRef, parameters.D!);
            var attributes = EcAttributes.Create(keyRef, curve);
            return (template, attributes);
        }
        catch (CryptographicException)
        {
            // Not ECDSA either
        }

        throw new NotSupportedException(
            "Unsupported key format. Expected RSA or ECDSA PEM-encoded private key.");
    }
}