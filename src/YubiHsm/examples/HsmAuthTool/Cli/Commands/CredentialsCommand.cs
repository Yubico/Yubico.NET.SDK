// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Commands.HsmAuth;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Prompts;
using PinPrompt = Yubico.YubiKit.Cli.Shared.Output.PinPrompt;
using SecureCredential = Yubico.YubiKit.Cli.Shared.Output.SecureCredential;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Commands;

/// <summary>
/// Implements: hsmauth credentials {list|add|delete|generate}
/// Manages credentials stored in the YubiHSM Auth applet.
/// </summary>
internal static class CredentialsCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return 1;
        }

        return args[1].ToLowerInvariant() switch
        {
            "list" => await ListAsync(args, cancellationToken),
            "add" => await AddAsync(args, cancellationToken),
            "delete" => await DeleteAsync(args, cancellationToken),
            "generate" => await GenerateAsync(args, cancellationToken),
            _ => PrintUsageAndReturn()
        };
    }

    // ── credentials list ─────────────────────────────────────────────────────

    private static async Task<int> ListAsync(string[] args, CancellationToken cancellationToken)
    {
        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);

        var credentials = await session.ListCredentialsAsync(cancellationToken);

        if (credentials.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored.");
            return 0;
        }

        var table = HsmAuthHelpers.BuildCredentialsTable(credentials);

        AnsiConsole.Write(table);
        OutputHelpers.WriteInfo($"{credentials.Count} credential(s) found.");
        return 0;
    }

    // ── credentials add LABEL ────────────────────────────────────────────────

    private static async Task<int> AddAsync(string[] args, CancellationToken cancellationToken)
    {
        var label = CommandArgs.GetPositional(args, subcommandCount: 2);
        if (label is null)
        {
            OutputHelpers.WriteError("Missing required argument: LABEL");
            AnsiConsole.MarkupLine("[bold]Usage:[/] HsmAuthTool credentials add LABEL [[options]]");
            return 1;
        }

        var algorithmStr = CommandArgs.GetOption(args, "--algorithm") ?? "symmetric";
        var touchRequired = CommandArgs.HasFlag(args, "--touch");

        return algorithmStr.ToLowerInvariant() switch
        {
            "symmetric" => await AddSymmetricAsync(args, label, touchRequired, cancellationToken),
            "asymmetric" => await AddAsymmetricAsync(args, label, touchRequired, cancellationToken),
            _ => PrintInvalidAlgorithm(algorithmStr)
        };
    }

    private static async Task<int> AddSymmetricAsync(
        string[] args, string label, bool touchRequired, CancellationToken cancellationToken)
    {
        var mgmtKey = ResolveManagementKey(args);
        if (mgmtKey is null)
        {
            return 1;
        }

        // Check for --derivation-password for PBKDF2-derived mode
        var derivationPasswordOption = CommandArgs.GetOption(args, "--derivation-password");
        var keyEncHex = CommandArgs.GetOption(args, "--key-enc");
        var keyMacHex = CommandArgs.GetOption(args, "--key-mac");

        try
        {
            // Both secrets are resolved inside the try so that an aborted prompt, a declined
            // prompt, or a rejected encoding still leaves mgmtKey zeroed by the finally below.
            using var credentialPassword = PinPrompt.Resolve(
                CommandArgs.GetOption(args, "--credential-password"), "Credential password");
            using var derivationPassword = derivationPasswordOption is null
                ? null
                : SecureCredential.FromUtf8String(derivationPasswordOption);
            if (credentialPassword is null)
            {
                OutputHelpers.WriteError("Credential password is required.");
                return 1;
            }

            var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
            if (selection is null)
            {
                return 1;
            }

            await using var session = await selection.Device.CreateHsmAuthSessionAsync(
                cancellationToken: cancellationToken);

            if (derivationPassword is not null)
            {
                await session.PutCredentialDerivedAsync(
                    mgmtKey,
                    label,
                    derivationPassword.Memory,
                    credentialPassword.Memory,
                    touchRequired,
                    cancellationToken);

                OutputHelpers.WriteSuccess($"Derived credential '{label}' stored successfully.");
                OutputHelpers.WriteInfo(
                    "Keys derived using PBKDF2-HMAC-SHA256 (10,000 iterations, salt='Yubico').");
            }
            else
            {
                byte[] keyEnc;
                byte[] keyMac;
                bool generated = false;

                if (keyEncHex is not null && keyMacHex is not null)
                {
                    keyEnc = Convert.FromHexString(keyEncHex);
                    keyMac = Convert.FromHexString(keyMacHex);
                }
                else
                {
                    keyEnc = RandomNumberGenerator.GetBytes(16);
                    keyMac = RandomNumberGenerator.GetBytes(16);
                    generated = true;
                }

                try
                {
                    await session.PutCredentialSymmetricAsync(
                        mgmtKey,
                        label,
                        keyEnc,
                        keyMac,
                        credentialPassword.Memory,
                        touchRequired,
                        cancellationToken);

                    OutputHelpers.WriteSuccess($"Symmetric credential '{label}' stored successfully.");

                    if (generated)
                    {
                        OutputHelpers.WriteHex("K-ENC (generated)", keyEnc);
                        OutputHelpers.WriteHex("K-MAC (generated)", keyMac);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyEnc);
                    CryptographicOperations.ZeroMemory(keyMac);
                }
            }

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }

    private static async Task<int> AddAsymmetricAsync(
        string[] args, string label, bool touchRequired, CancellationToken cancellationToken)
    {
        var mgmtKey = ResolveManagementKey(args);
        if (mgmtKey is null)
        {
            return 1;
        }

        var privateKeyHex = CommandArgs.GetOption(args, "--private-key");
        if (privateKeyHex is null)
        {
            OutputHelpers.WriteError(
                "Missing --private-key for asymmetric add. Use 'credentials generate' for on-device key generation.");
            CryptographicOperations.ZeroMemory(mgmtKey);
            return 1;
        }

        var privateKey = CommandArgs.ParseHex(privateKeyHex);
        if (privateKey is null || privateKey.Length != 32)
        {
            OutputHelpers.WriteError("Invalid private key. Must be 32 bytes (64 hex characters).");
            CryptographicOperations.ZeroMemory(mgmtKey);
            return 1;
        }

        try
        {
            // Resolved inside the try so an aborted or rejected prompt still zeros mgmtKey.
            using var credentialPassword = PinPrompt.Resolve(
                CommandArgs.GetOption(args, "--credential-password"), "Credential password");
            if (credentialPassword is null)
            {
                OutputHelpers.WriteError("Credential password is required.");
                return 1;
            }

            var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
            if (selection is null)
            {
                return 1;
            }

            await using var session = await selection.Device.CreateHsmAuthSessionAsync(
                cancellationToken: cancellationToken);

            await session.PutCredentialAsymmetricAsync(
                mgmtKey,
                label,
                privateKey,
                credentialPassword.Memory,
                touchRequired,
                cancellationToken);

            OutputHelpers.WriteSuccess($"Asymmetric credential '{label}' stored successfully.");
            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    // ── credentials delete LABEL ─────────────────────────────────────────────

    private static async Task<int> DeleteAsync(string[] args, CancellationToken cancellationToken)
    {
        var label = CommandArgs.GetPositional(args, subcommandCount: 2);
        if (label is null)
        {
            OutputHelpers.WriteError("Missing required argument: LABEL");
            AnsiConsole.MarkupLine("[bold]Usage:[/] HsmAuthTool credentials delete LABEL [[options]]");
            return 1;
        }

        var force = CommandArgs.HasFlag(args, "-f", "--force");

        if (!force)
        {
            if (!AnsiConsole.Confirm($"Delete credential '{label}'?", defaultValue: false))
            {
                AnsiConsole.MarkupLine("[grey]Aborted.[/]");
                return 1;
            }
        }

        var mgmtKey = ResolveManagementKey(args);
        if (mgmtKey is null)
        {
            return 1;
        }

        try
        {
            var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
            if (selection is null)
            {
                return 1;
            }

            await using var session = await selection.Device.CreateHsmAuthSessionAsync(
                cancellationToken: cancellationToken);

            await session.DeleteCredentialAsync(mgmtKey, label, cancellationToken);
            OutputHelpers.WriteSuccess($"Credential '{label}' deleted.");
            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }

    // ── credentials generate LABEL ───────────────────────────────────────────

    private static async Task<int> GenerateAsync(string[] args, CancellationToken cancellationToken)
    {
        var label = CommandArgs.GetPositional(args, subcommandCount: 2);
        if (label is null)
        {
            OutputHelpers.WriteError("Missing required argument: LABEL");
            AnsiConsole.MarkupLine(
                "[bold]Usage:[/] HsmAuthTool credentials generate LABEL [[options]]");
            return 1;
        }

        var touchRequired = CommandArgs.HasFlag(args, "--touch");

        var mgmtKey = ResolveManagementKey(args);
        if (mgmtKey is null)
        {
            return 1;
        }

        try
        {
            // Resolved inside the try so an aborted or rejected prompt still zeros mgmtKey.
            using var credentialPassword = PinPrompt.Resolve(
                CommandArgs.GetOption(args, "--credential-password"), "Credential password");
            if (credentialPassword is null)
            {
                OutputHelpers.WriteError("Credential password is required.");
                return 1;
            }

            var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
            if (selection is null)
            {
                return 1;
            }

            await using var session = await selection.Device.CreateHsmAuthSessionAsync(
                cancellationToken: cancellationToken);

            await session.GenerateCredentialAsymmetricAsync(
                mgmtKey,
                label,
                credentialPassword.Memory,
                touchRequired,
                cancellationToken);

            OutputHelpers.WriteSuccess(
                $"Asymmetric credential '{label}' generated on device.");
            OutputHelpers.WriteInfo("Private key was generated on-device and never leaves the YubiKey.");

            // Retrieve and display the public key
            var publicKey = await session.GetPublicKeyAsync(label, cancellationToken);
            OutputHelpers.WriteHex("Public key", publicKey.Span);

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the management key from --management-key option or prompts.
    /// Returns null if the key is invalid.
    /// </summary>
    private static byte[]? ResolveManagementKey(string[] args)
    {
        var mgmtKeyHex = CommandArgs.GetOption(args, "--management-key");

        if (mgmtKeyHex is null)
        {
            mgmtKeyHex = AnsiConsole.Prompt(
                new TextPrompt<string>("Management key (hex, 16 bytes):")
                    .DefaultValue("00000000000000000000000000000000"));
        }

        var mgmtKey = CommandArgs.ParseHex(mgmtKeyHex);
        if (mgmtKey is null || mgmtKey.Length != 16)
        {
            OutputHelpers.WriteError("Invalid management key. Must be 16 bytes (32 hex characters).");
            return null;
        }

        return mgmtKey;
    }

    private static int PrintInvalidAlgorithm(string algorithm)
    {
        OutputHelpers.WriteError($"Unknown algorithm: '{algorithm}'. Use 'symmetric' or 'asymmetric'.");
        return 1;
    }

    private static int PrintUsageAndReturn()
    {
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        AnsiConsole.MarkupLine("[bold]Usage:[/] HsmAuthTool credentials <command> [[options]]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Commands:[/]");
        AnsiConsole.MarkupLine("  list                        List all stored credentials");
        AnsiConsole.MarkupLine("  add LABEL                   Add a credential");
        AnsiConsole.MarkupLine("  delete LABEL                Delete a credential");
        AnsiConsole.MarkupLine("  generate LABEL              Generate asymmetric credential on device");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Add options:[/]");
        AnsiConsole.MarkupLine("  --algorithm <type>          symmetric (default) or asymmetric");
        AnsiConsole.MarkupLine("  --credential-password PWD   Credential password");
        AnsiConsole.MarkupLine("  --management-key HEX        Management key (hex, 16 bytes)");
        AnsiConsole.MarkupLine("  --touch                     Require touch for credential use");
        AnsiConsole.MarkupLine("  --derivation-password PWD   Derive keys via PBKDF2 (symmetric only)");
        AnsiConsole.MarkupLine("  --key-enc HEX               Encryption key (symmetric only, hex)");
        AnsiConsole.MarkupLine("  --key-mac HEX               MAC key (symmetric only, hex)");
        AnsiConsole.MarkupLine("  --private-key HEX           EC P256 private key (asymmetric only)");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Delete options:[/]");
        AnsiConsole.MarkupLine("  -f, --force                 Skip confirmation prompt");
        AnsiConsole.MarkupLine("  --management-key HEX        Management key (hex, 16 bytes)");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Generate options:[/]");
        AnsiConsole.MarkupLine("  --credential-password PWD   Credential password");
        AnsiConsole.MarkupLine("  --management-key HEX        Management key (hex, 16 bytes)");
        AnsiConsole.MarkupLine("  --touch                     Require touch for credential use");
    }
}