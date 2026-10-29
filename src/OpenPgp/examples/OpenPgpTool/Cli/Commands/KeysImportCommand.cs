// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Formats.Asn1;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
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

        PrivateKey privateKey;
        try
        {
            privateKey = LoadPrivateKey(pem);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to load private key: {ex.Message}");
            return 1;
        }

        using (privateKey)
        {
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
                    await session.PutKeyAsync(keyRef, privateKey);
                });
        }

        OutputHelpers.WriteSuccess(
            $"Private key imported to {FormatKeyRef(keyRef)} slot.");
        return 0;
    }

    /// <summary>
    ///     Reads a PEM private key: PKCS#8 ("PRIVATE KEY", any supported algorithm), PKCS#1 ("RSA PRIVATE KEY")
    ///     or SEC1 ("EC PRIVATE KEY").
    /// </summary>
    private static PrivateKey LoadPrivateKey(string pem)
    {
        if (!PemEncoding.TryFind(pem, out var fields))
        {
            throw new NotSupportedException("No PEM-encoded private key found.");
        }

        var label = pem[fields.Label];
        if (label == "PRIVATE KEY")
        {
            byte[] der = Convert.FromBase64String(pem[fields.Base64Data]);
            try
            {
                return Pkcs8AlgorithmOid(der) switch
                {
                    "1.2.840.113549.1.1.1" => RSAPrivateKey.CreateFromPkcs8(der),
                    "1.2.840.10045.2.1" => ECPrivateKey.CreateFromPkcs8(der),
                    "1.3.101.110" or "1.3.101.112" => Curve25519PrivateKey.CreateFromPkcs8(der),
                    var oid => throw new NotSupportedException($"Unsupported private key algorithm {oid}."),
                };
            }
            finally
            {
                CryptographicOperations.ZeroMemory(der);
            }
        }

        if (label == "RSA PRIVATE KEY")
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return RSAPrivateKey.CreateFromParameters(rsa.ExportParameters(includePrivateParameters: true));
        }

        if (label == "EC PRIVATE KEY")
        {
            using var ec = ECDsa.Create();
            ec.ImportFromPem(pem);
            return ECPrivateKey.CreateFromParameters(ec.ExportParameters(includePrivateParameters: true));
        }

        throw new NotSupportedException($"Unsupported PEM label '{label}'. Expected PRIVATE KEY, RSA PRIVATE KEY or EC PRIVATE KEY.");
    }

    private static string Pkcs8AlgorithmOid(byte[] pkcs8)
    {
        var privateKeyInfo = new AsnReader(pkcs8, AsnEncodingRules.DER).ReadSequence();
        _ = privateKeyInfo.ReadInteger();
        return privateKeyInfo.ReadSequence().ReadObjectIdentifier();
    }
}