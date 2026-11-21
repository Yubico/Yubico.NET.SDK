// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Commands.HsmAuth;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Prompts;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Commands;

/// <summary>
/// Implements: hsmauth credentials {list|add|delete|generate}
/// Manages credentials stored in the YubiHSM Auth applet.
/// </summary>
/// <remarks>
///     This diagnostic command prints generated symmetric keys. The operator provisions matching HSM
///     authentication objects separately and protects terminal/process history. Some parsing failures
///     occur before byte-buffer clearing scopes; use total acquisition/cleanup scopes in an application.
/// </remarks>
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
            "asymmetric" => await AddAsymmetricAsync(label, touchRequired, cancellationToken),
            _ => PrintInvalidAlgorithm(algorithmStr)
        };
    }

    private static async Task<int> AddSymmetricAsync(
        string[] args, string label, bool touchRequired, CancellationToken cancellationToken)
    {
        using var mgmtKey = OutputHelpers.ReadKey("Management key (16 bytes, hex)", 16, cancellationToken);
        if (mgmtKey is null)
        {
            return 1;
        }

        var derive = CommandArgs.HasFlag(args, "--derive");
        var import = CommandArgs.HasFlag(args, "--import-keys");
        if (derive && import)
        {
            OutputHelpers.WriteError("Select either --derive or --import-keys.");
            return 1;
        }

        using var credentialPassword = OutputHelpers.ReadPassword("Credential password", cancellationToken);
        if (credentialPassword is null)
            return 1;

        using var password = CredentialPassword.FromUtf8(credentialPassword.Memory.Span);
        using var derivationPassword = derive ? OutputHelpers.ReadPassword("Derivation password (PBKDF2)", cancellationToken) : null;
        if (derive && derivationPassword is null)
            return 1;

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
            return 1;

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(cancellationToken: cancellationToken);
        if (derivationPassword is null)
            return await StoreSymmetricAsync(session, mgmtKey.Memory, label, password, touchRequired, import, cancellationToken);

        await session.PutCredentialDerivedAsync(mgmtKey.Memory, label, derivationPassword.Memory, password, touchRequired, cancellationToken);
        OutputHelpers.WriteSuccess($"Derived credential '{label}' stored successfully.");
        return 0;
    }

    private static async Task<int> StoreSymmetricAsync(
        IHsmAuthSession session, ReadOnlyMemory<byte> managementKey, string label, CredentialPassword password,
        bool touchRequired, bool import, CancellationToken cancellationToken)
    {
        if (import)
        {
            using var enc = OutputHelpers.ReadKey("Encryption key (16 bytes, hex)", 16, cancellationToken);
            using var mac = OutputHelpers.ReadKey("MAC key (16 bytes, hex)", 16, cancellationToken);
            if (enc is null || mac is null)
                return 1;

            await session.PutCredentialSymmetricAsync(managementKey, label, enc.Memory, mac.Memory, password, touchRequired, cancellationToken);
        }
        else
        {
            var keyEnc = RandomNumberGenerator.GetBytes(16);
            var keyMac = RandomNumberGenerator.GetBytes(16);
            try
            {
                await session.PutCredentialSymmetricAsync(managementKey, label, keyEnc, keyMac, password, touchRequired, cancellationToken);
                OutputHelpers.WriteHex("K-ENC (generated, demo only)", keyEnc);
                OutputHelpers.WriteHex("K-MAC (generated, demo only)", keyMac);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyEnc);
                CryptographicOperations.ZeroMemory(keyMac);
            }
        }

        OutputHelpers.WriteSuccess($"Symmetric credential '{label}' stored successfully.");
        return 0;
    }

    private static async Task<int> AddAsymmetricAsync(
        string label, bool touchRequired, CancellationToken cancellationToken)
    {
        using var mgmtKey = OutputHelpers.ReadKey("Management key (16 bytes, hex)", 16, cancellationToken);
        if (mgmtKey is null)
        {
            return 1;
        }

        using var privateKey = OutputHelpers.ReadKey("EC P-256 private key (32 bytes, hex)", 32, cancellationToken);
        if (privateKey is null)
            return 1;

        using var credentialPassword = OutputHelpers.ReadPassword("Credential password", cancellationToken);
        if (credentialPassword is null)
        {
            OutputHelpers.WriteError("Credential password is required.");
            return 1;
        }

        using var password = CredentialPassword.FromUtf8(credentialPassword.Memory.Span);

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);

        await session.PutCredentialAsymmetricAsync(
            mgmtKey.Memory,
            label,
            privateKey.Memory,
            password,
            touchRequired,
            cancellationToken);

        OutputHelpers.WriteSuccess($"Asymmetric credential '{label}' stored successfully.");
        return 0;
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

        using var mgmtKey = OutputHelpers.ReadKey("Management key (16 bytes, hex)", 16, cancellationToken);
        if (mgmtKey is null)
        {
            return 1;
        }

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);

        await session.DeleteCredentialAsync(mgmtKey.Memory, label, cancellationToken);
        OutputHelpers.WriteSuccess($"Credential '{label}' deleted.");
        return 0;
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

        using var mgmtKey = OutputHelpers.ReadKey("Management key (16 bytes, hex)", 16, cancellationToken);
        if (mgmtKey is null)
        {
            return 1;
        }

        using var credentialPassword = OutputHelpers.ReadPassword("Credential password", cancellationToken);
        if (credentialPassword is null)
        {
            OutputHelpers.WriteError("Credential password is required.");
            return 1;
        }

        using var password = CredentialPassword.FromUtf8(credentialPassword.Memory.Span);

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);

        await session.GenerateCredentialAsymmetricAsync(
            mgmtKey.Memory,
            label,
            password,
            touchRequired,
            cancellationToken);

        OutputHelpers.WriteSuccess(
            $"Asymmetric credential '{label}' generated on device.");
        OutputHelpers.WriteInfo("Private key was generated on-device and never leaves the YubiKey.");

        // Export credential PK-OCE for enrollment in the matching HSM authentication object.
        // The HSM's own trusted static public key is obtained separately through HSM provisioning.
        var publicKey = await session.GetPublicKeyAsync(label, cancellationToken);
        OutputHelpers.WriteHex("Public key", publicKey.Span);

        return 0;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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
        AnsiConsole.MarkupLine("Passwords and keys are prompted for, not accepted as arguments.");
        AnsiConsole.MarkupLine("[bold]Add options:[/]");
        AnsiConsole.MarkupLine("  --derive                    Derive symmetric keys via PBKDF2");
        AnsiConsole.MarkupLine("  --import-keys               Prompt for existing symmetric keys instead of generating them");
        AnsiConsole.MarkupLine("  --algorithm <type>          symmetric (default) or asymmetric");
        AnsiConsole.MarkupLine("  --touch                     Require touch for credential use");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Delete options:[/]");
        AnsiConsole.MarkupLine("  -f, --force                 Skip confirmation prompt");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Generate options:[/]");
        AnsiConsole.MarkupLine("  --touch                     Require touch for credential use");
    }
}