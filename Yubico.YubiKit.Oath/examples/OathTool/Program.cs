// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Oath;
using Yubico.YubiKit.Oath.Examples.OathTool.Commands;

YubiKeyManager.StartMonitoring();

try
{
    return await DispatchAsync(args);
}
finally
{
    await YubiKeyManager.ShutdownAsync();
}

// --- Command dispatch ---

static async Task<int> DispatchAsync(string[] args)
{
    if (args.Length == 0)
    {
        PrintUsage();
        return 1;
    }

    var command = args[0].ToLowerInvariant();

    return command switch
    {
        "info" => await InfoCommand.ExecuteAsync(),
        "reset" => await DispatchResetAsync(args[1..]),
        "access" => await DispatchAccessAsync(args[1..]),
        "accounts" => await DispatchAccountsAsync(args[1..]),
        "-h" or "--help" or "help" => PrintUsageAndReturn(),
        _ => PrintUnknownCommand(command)
    };
}

// --- oath reset ---

static async Task<int> DispatchResetAsync(string[] args)
{
    bool force = HasFlag(args, "--force", "-f");
    return await ResetCommand.ExecuteAsync(force);
}

// --- oath access ---

static async Task<int> DispatchAccessAsync(string[] args)
{
    if (args.Length == 0)
    {
        PrintAccessUsage();
        return 1;
    }

    var sub = args[0].ToLowerInvariant();
    var subArgs = args[1..];

    return sub switch
    {
        "change" => await DispatchAccessChangeAsync(subArgs),
        "-h" or "--help" or "help" => PrintAccessUsageAndReturn(),
        _ => PrintUnknownSubcommand("access", sub)
    };
}

static async Task<int> DispatchAccessChangeAsync(string[] args)
{
    string? password = GetArgValue(args, "--password", "-p");
    string? newPassword = GetArgValue(args, "--new-password", "-n");
    bool clear = HasFlag(args, "--clear", "-c");

    return await AccessCommand.ChangeAsync(password, newPassword, clear);
}

// --- oath accounts ---

static async Task<int> DispatchAccountsAsync(string[] args)
{
    if (args.Length == 0)
    {
        PrintAccountsUsage();
        return 1;
    }

    var sub = args[0].ToLowerInvariant();
    var subArgs = args[1..];

    return sub switch
    {
        "list" => await DispatchAccountsListAsync(subArgs),
        "add" => await DispatchAccountsAddAsync(subArgs),
        "code" => await DispatchAccountsCodeAsync(subArgs),
        "delete" => await DispatchAccountsDeleteAsync(subArgs),
        "rename" => await DispatchAccountsRenameAsync(subArgs),
        "uri" => await DispatchAccountsUriAsync(subArgs),
        "-h" or "--help" or "help" => PrintAccountsUsageAndReturn(),
        _ => PrintUnknownSubcommand("accounts", sub)
    };
}

static async Task<int> DispatchAccountsListAsync(string[] args)
{
    string? password = GetArgValue(args, "--password", "-p");
    return await AccountsCommand.ListAsync(password);
}

static async Task<int> DispatchAccountsAddAsync(string[] args)
{
    // First positional arg is NAME, second is SECRET
    var positional = GetPositionalArgs(args);
    if (positional.Count < 2)
    {
        Console.Error.WriteLine("Usage: oath accounts add NAME SECRET [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  --issuer ISSUER          Credential issuer");
        Console.Error.WriteLine("  --oath-type TOTP|HOTP    OATH type (default: TOTP)");
        Console.Error.WriteLine("  --digits 6|8             Number of digits (default: 6)");
        Console.Error.WriteLine("  --algorithm SHA1|SHA256|SHA512  Hash algorithm (default: SHA1)");
        Console.Error.WriteLine("  --period SECONDS         TOTP period (default: 30)");
        Console.Error.WriteLine("  --touch                  Require touch");
        Console.Error.WriteLine("  --force, -f              Skip confirmation prompt");
        Console.Error.WriteLine("  --password, -p PASSWORD  OATH access password");
        return 1;
    }

    string name = positional[0];
    string secret = positional[1];
    string? issuer = GetArgValue(args, "--issuer");
    bool touch = HasFlag(args, "--touch");
    bool force = HasFlag(args, "--force", "-f");
    string? password = GetArgValue(args, "--password", "-p");

    string? oathTypeStr = GetArgValue(args, "--oath-type");
    OathType oathType = ParseOathType(oathTypeStr);

    string? digitsStr = GetArgValue(args, "--digits");
    int digits = digitsStr is not null ? int.Parse(digitsStr) : 6;

    string? algorithmStr = GetArgValue(args, "--algorithm");
    OathHashAlgorithm algorithm = ParseAlgorithm(algorithmStr);

    string? periodStr = GetArgValue(args, "--period");
    int period = periodStr is not null ? int.Parse(periodStr) : 30;

    return await AccountsCommand.AddAsync(
        name, secret, issuer, oathType, digits, algorithm, period, touch, force, password);
}

static async Task<int> DispatchAccountsCodeAsync(string[] args)
{
    var positional = GetPositionalArgs(args);
    string? query = positional.Count > 0 ? positional[0] : null;
    string? password = GetArgValue(args, "--password", "-p");

    return await AccountsCommand.CodeAsync(query, password);
}

static async Task<int> DispatchAccountsDeleteAsync(string[] args)
{
    var positional = GetPositionalArgs(args);
    if (positional.Count < 1)
    {
        Console.Error.WriteLine("Usage: oath accounts delete NAME [--force] [--password PW]");
        return 1;
    }

    string name = positional[0];
    bool force = HasFlag(args, "--force", "-f");
    string? password = GetArgValue(args, "--password", "-p");

    return await AccountsCommand.DeleteAsync(name, force, password);
}

static async Task<int> DispatchAccountsRenameAsync(string[] args)
{
    var positional = GetPositionalArgs(args);
    if (positional.Count < 2)
    {
        Console.Error.WriteLine("Usage: oath accounts rename NAME NEW_NAME [--force] [--password PW]");
        return 1;
    }

    string name = positional[0];
    string newName = positional[1];
    bool force = HasFlag(args, "--force", "-f");
    string? password = GetArgValue(args, "--password", "-p");

    return await AccountsCommand.RenameAsync(name, newName, force, password);
}

static async Task<int> DispatchAccountsUriAsync(string[] args)
{
    var positional = GetPositionalArgs(args);
    if (positional.Count < 1)
    {
        Console.Error.WriteLine("Usage: oath accounts uri URI [--touch] [--force] [--password PW]");
        return 1;
    }

    string uri = positional[0];
    bool touch = HasFlag(args, "--touch");
    bool force = HasFlag(args, "--force", "-f");
    string? password = GetArgValue(args, "--password", "-p");

    return await AccountsCommand.UriAsync(uri, touch, force, password);
}

// --- Argument parsing helpers ---

static string? GetArgValue(string[] args, string flag, string? shortFlag = null)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase) ||
            (shortFlag is not null && string.Equals(args[i], shortFlag, StringComparison.OrdinalIgnoreCase)))
        {
            return args[i + 1];
        }
    }

    return null;
}

static bool HasFlag(string[] args, string flag, string? shortFlag = null)
{
    foreach (var arg in args)
    {
        if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase) ||
            (shortFlag is not null && string.Equals(arg, shortFlag, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
    }

    return false;
}

static List<string> GetPositionalArgs(string[] args)
{
    var positional = new List<string>();
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].StartsWith('-'))
        {
            // Skip flag and its value (if it has one)
            if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
            {
                i++; // skip the value
            }

            continue;
        }

        positional.Add(args[i]);
    }

    return positional;
}

static OathType ParseOathType(string? value) =>
    value?.ToUpperInvariant() switch
    {
        "HOTP" => OathType.Hotp,
        "TOTP" or null => OathType.Totp,
        _ => throw new ArgumentException($"Invalid OATH type: '{value}'. Use TOTP or HOTP.")
    };

static OathHashAlgorithm ParseAlgorithm(string? value) =>
    value?.ToUpperInvariant() switch
    {
        "SHA256" => OathHashAlgorithm.Sha256,
        "SHA512" => OathHashAlgorithm.Sha512,
        "SHA1" or null => OathHashAlgorithm.Sha1,
        _ => throw new ArgumentException($"Invalid algorithm: '{value}'. Use SHA1, SHA256, or SHA512.")
    };

// --- Usage strings ---

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: oath <command> [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  info        Display OATH application status");
    Console.Error.WriteLine("  reset       Reset all OATH data");
    Console.Error.WriteLine("  access      Manage password protection");
    Console.Error.WriteLine("  accounts    Manage OATH accounts");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Run 'oath <command> --help' for more information on a command.");
}

static int PrintUsageAndReturn()
{
    PrintUsage();
    return 0;
}

static int PrintUnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Console.Error.WriteLine();
    PrintUsage();
    return 1;
}

static int PrintUnknownSubcommand(string parent, string sub)
{
    Console.Error.WriteLine($"Unknown {parent} subcommand: {sub}");
    return 1;
}

static void PrintAccessUsage()
{
    Console.Error.WriteLine("Usage: oath access <subcommand> [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Subcommands:");
    Console.Error.WriteLine("  change      Set, change, or clear the OATH access password");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options for 'change':");
    Console.Error.WriteLine("  --password, -p PASSWORD       Current password (if set)");
    Console.Error.WriteLine("  --new-password, -n PASSWORD   New password to set");
    Console.Error.WriteLine("  --clear, -c                   Remove password protection");
}

static int PrintAccessUsageAndReturn()
{
    PrintAccessUsage();
    return 0;
}

static void PrintAccountsUsage()
{
    Console.Error.WriteLine("Usage: oath accounts <subcommand> [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Subcommands:");
    Console.Error.WriteLine("  list                  List all accounts");
    Console.Error.WriteLine("  add NAME SECRET       Add an account");
    Console.Error.WriteLine("  code [QUERY]          Generate OTP codes");
    Console.Error.WriteLine("  delete NAME           Delete an account");
    Console.Error.WriteLine("  rename NAME NEW_NAME  Rename an account");
    Console.Error.WriteLine("  uri URI               Add account from otpauth:// URI");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Common options:");
    Console.Error.WriteLine("  --force, -f              Skip confirmation prompts");
    Console.Error.WriteLine("  --password, -p PASSWORD  OATH access password");
}

static int PrintAccountsUsageAndReturn()
{
    PrintAccountsUsage();
    return 0;
}