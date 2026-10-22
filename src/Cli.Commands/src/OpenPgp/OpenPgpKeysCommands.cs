// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Formats.Asn1;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.OpenPgp;
using static Yubico.YubiKit.Cli.Commands.OpenPgp.OpenPgpHelpers;

namespace Yubico.YubiKit.Cli.Commands.OpenPgp;

// ── Settings ────────────────────────────────────────────────────────────────

public sealed class KeysSetTouchSettings : GlobalSettings
{
    [CommandArgument(0, "<KEY>")]
    [Description("Key slot (sig, dec, aut, att).")]
    public string Key { get; init; } = "";

    [CommandArgument(1, "<POLICY>")]
    [Description("Touch policy (on, off, fixed, cached, cached-fixed).")]
    public string Policy { get; init; } = "";

    [CommandOption("--admin-pin <PIN>")]
    [Description("Admin PIN (prompted if not provided).")]
    public string? AdminPin { get; init; }

    [CommandOption("-f|--force")]
    [Description("Confirm the action without prompting.")]
    public bool Force { get; init; }
}

public sealed class KeysImportSettings : GlobalSettings
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

public sealed class KeysGenerateSettings : GlobalSettings
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

public sealed class KeysAttestSettings : GlobalSettings
{
    [CommandArgument(0, "<KEY>")]
    [Description("Key slot (sig, dec, aut).")]
    public string Key { get; init; } = "";

    [CommandOption("--pin <PIN>")]
    [Description("User PIN (prompted if not provided).")]
    public string? Pin { get; init; }
}

// ── Commands ────────────────────────────────────────────────────────────────

public sealed class OpenPgpKeysSetTouchCommand : YkCommandBase<KeysSetTouchSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, KeysSetTouchSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var keyRef = ParseKeyRef(settings.Key);

        var uif = settings.Policy.ToLowerInvariant() switch
        {
            "on" => Uif.On,
            "off" => Uif.Off,
            "fixed" => Uif.Fixed,
            "cached" => Uif.Cached,
            "cached-fixed" => Uif.CachedFixed,
            _ => throw new ArgumentException(
                $"Invalid policy: {settings.Policy}. Must be on, off, fixed, cached, or cached-fixed.")
        };

        var current = await session.GetUifAsync(keyRef);
        if (current.IsFixed())
        {
            OutputHelpers.WriteError("Current touch policy is fixed and cannot be changed without factory reset.");
            return ExitCode.GenericError;
        }

        if (uif.IsFixed() && !ConfirmAction(
                "set a PERMANENT touch policy that cannot be changed without factory reset", settings.Force))
        {
            OutputHelpers.WriteInfo("Touch policy change cancelled.");
            return ExitCode.UserCancelled;
        }

        using var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        if (adminPin is null)
        {
            OutputHelpers.WriteError("Admin PIN is required.");
            return ExitCode.GenericError;
        }

        await session.VerifyAdminAsync(adminPin.Memory);

        await session.SetUifAsync(keyRef, uif);

        OutputHelpers.WriteSuccess($"Touch policy for {FormatKeyRef(keyRef)} set to {settings.Policy}.");
        return ExitCode.Success;
    }
}

public sealed class OpenPgpKeysImportCommand : YkCommandBase<KeysImportSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, KeysImportSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var keyRef = ParseKeyRef(settings.Key);

        if (!File.Exists(settings.PemFile))
        {
            OutputHelpers.WriteError($"File not found: {settings.PemFile}");
            return ExitCode.GenericError;
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
            return ExitCode.GenericError;
        }

        using (privateKey)
        {
            using var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
            if (adminPin is null)
            {
                OutputHelpers.WriteError("Admin PIN is required.");
                return ExitCode.GenericError;
            }

            await session.VerifyAdminAsync(adminPin.Memory);

            await AnsiConsole.Status()
                .StartAsync("Importing private key...", async _ =>
                {
                    await session.PutKeyAsync(keyRef, privateKey);
                });
        }

        OutputHelpers.WriteSuccess($"Private key imported to {FormatKeyRef(keyRef)} slot.");
        return ExitCode.Success;
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

public sealed class OpenPgpKeysGenerateCommand : YkCommandBase<KeysGenerateSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, KeysGenerateSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var keyRef = ParseKeyRef(settings.Key);
        using var adminPin = GetPin(settings.AdminPin, "Enter Admin PIN");
        if (adminPin is null)
        {
            OutputHelpers.WriteError("Admin PIN is required.");
            return ExitCode.GenericError;
        }

        await session.VerifyAdminAsync(adminPin.Memory);

        var alg = settings.Algorithm.ToUpperInvariant();

        await AnsiConsole.Status()
            .StartAsync($"Generating {alg} key in {FormatKeyRef(keyRef)}...", async _ =>
            {
                switch (alg)
                {
                    case "RSA2048":
                        await session.GenerateKeyAsync(keyRef, RsaAttributes.Create(RsaSize.Rsa2048));
                        break;
                    case "RSA3072":
                        await session.GenerateKeyAsync(keyRef, RsaAttributes.Create(RsaSize.Rsa3072));
                        break;
                    case "RSA4096":
                        await session.GenerateKeyAsync(keyRef, RsaAttributes.Create(RsaSize.Rsa4096));
                        break;
                    case "ECCP256":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, CurveOid.Secp256R1));
                        break;
                    case "ECCP384":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, CurveOid.Secp384R1));
                        break;
                    case "ECCP521":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, CurveOid.Secp521R1));
                        break;
                    case "ED25519":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, CurveOid.Ed25519));
                        break;
                    case "X25519":
                        await session.GenerateKeyAsync(keyRef, EcAttributes.Create(keyRef, CurveOid.X25519));
                        break;
                    default:
                        throw new ArgumentException(
                            $"Unsupported algorithm: {settings.Algorithm}. " +
                            "Use RSA2048, RSA3072, RSA4096, ECCP256, ECCP384, ECCP521, Ed25519, or X25519.");
                }
            });

        OutputHelpers.WriteSuccess($"{alg} key generated in {FormatKeyRef(keyRef)} slot.");
        return ExitCode.Success;
    }
}

public sealed class OpenPgpKeysAttestCommand : YkCommandBase<KeysAttestSettings>
{
    protected override ConnectionType[] AppletTransports => [ConnectionType.SmartCard];

    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, KeysAttestSettings settings, YkDeviceContext deviceContext)
    {
        await using var session = await deviceContext.Device.CreateOpenPgpSessionAsync();

        var keyRef = ParseKeyRef(settings.Key);
        var cert = await session.AttestKeyAsync(keyRef);

        AnsiConsole.MarkupLine("[bold]Attestation Certificate[/]");
        OutputHelpers.WriteKeyValue("Subject", cert.Subject);
        OutputHelpers.WriteKeyValue("Issuer", cert.Issuer);
        OutputHelpers.WriteKeyValue("Serial Number", cert.SerialNumber);
        OutputHelpers.WriteKeyValue("Not Before", cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss"));
        OutputHelpers.WriteKeyValue("Not After", cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss"));
        OutputHelpers.WriteKeyValue("Thumbprint", cert.Thumbprint);

        if (cert.PublicKey is not null)
        {
            OutputHelpers.WriteKeyValue("Public Key Algorithm",
                cert.PublicKey.Oid?.FriendlyName ?? "Unknown");
        }

        AnsiConsole.WriteLine();
        var pem = PemEncoding.Write("CERTIFICATE", cert.RawData);
        AnsiConsole.WriteLine(new string(pem));

        return ExitCode.Success;
    }
}