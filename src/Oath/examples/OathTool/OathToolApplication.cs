// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Globalization;
using System.Text;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Oath.Examples.OathTool.Commands;

namespace Yubico.YubiKit.Oath.Examples.OathTool;

public static class OathToolApplication
{
    // Help and argument errors must return before the callback starts monitoring or opens a device.
    public static async Task<int> RunAsync(string[] args,
        Func<OathInvocation, CancellationToken, Task<int>> execute,
        CancellationToken cancellationToken = default,
        TextWriter? output = null)
    {
        output ??= Console.Error;
        if (OathInvocation.RequestsHelp(args))
        {
            PrintUsage(output);
            return 0;
        }

        if (!OathInvocation.TryParse(args, out var invocation, out string? error))
        {
            output.WriteLine(error);
            PrintUsage(output);
            return 1;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await execute(invocation, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            output.WriteLine("Operation cancelled.");
            return ExitCode.UserCancelled;
        }
    }

    public static async Task<int> ExecuteAsync(OathInvocation invocation, CancellationToken cancellationToken)
    {
        YubiKeyManager.StartMonitoring();
        try
        {
            using var password = EncodePassword(invocation.Value("--password"));
            using var newPassword = EncodePassword(invocation.Value("--new-password"));
            return invocation.Command switch
            {
                "info" => await InfoCommand.ExecuteAsync(cancellationToken),
                "reset" => await ResetCommand.ExecuteAsync(invocation.Flag("--force"), cancellationToken),
                "access change" => await AccessCommand.ChangeAsync(
                    password, newPassword, invocation.Flag("--clear"), cancellationToken),
                _ => await ExecuteAccountAsync(invocation, password, cancellationToken)
            };
        }
        finally
        {
            await YubiKeyManager.ShutdownAsync();
        }
    }

    private static async Task<int> ExecuteAccountAsync(OathInvocation invocation,
        IMemoryOwner<byte>? password, CancellationToken cancellationToken)
    {
        var positional = invocation.Positionals;
        bool force = invocation.Flag("--force");
        return invocation.Command switch
        {
            "accounts list" => await AccountsCommand.ListAsync(password, cancellationToken),
            "accounts code" => await AccountsCommand.CodeAsync(
                positional.Count == 0 ? null : positional[0], password, cancellationToken),
            "accounts delete" => await AccountsCommand.DeleteAsync(positional[0], force, password, cancellationToken),
            "accounts rename" => await AccountsCommand.RenameAsync(positional[0], positional[1], force, password, cancellationToken),
            "accounts uri" => await AccountsCommand.UriAsync(positional[0], invocation.Flag("--touch"), force, password, cancellationToken),
            "accounts add" => await AccountsCommand.AddAsync(
                positional[0], positional[1], invocation.Value("--issuer"),
                string.Equals(invocation.Value("--oath-type"), "HOTP", StringComparison.OrdinalIgnoreCase) ? OathType.Hotp : OathType.Totp,
                invocation.Number("--digits", 6), ParseAlgorithm(invocation.Value("--algorithm")),
                invocation.Number("--period", 30), invocation.Flag("--touch"), force, password, cancellationToken),
            _ => 1
        };
    }

    private static OathHashAlgorithm ParseAlgorithm(string? value)
    {
        if (string.Equals(value, "SHA256", StringComparison.OrdinalIgnoreCase))
        {
            return OathHashAlgorithm.Sha256;
        }

        return string.Equals(value, "SHA512", StringComparison.OrdinalIgnoreCase)
            ? OathHashAlgorithm.Sha512 : OathHashAlgorithm.Sha1;
    }

    private static IMemoryOwner<byte>? EncodePassword(string? password)
    {
        if (password is null)
        {
            return null;
        }

        var buffer = new DisposableArrayPoolBuffer(Encoding.UTF8.GetByteCount(password));
        try
        {
            Encoding.UTF8.GetBytes(password, buffer.Span);
            return buffer;
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    private static void PrintUsage(TextWriter output)
    {
        output.WriteLine("""
            Usage: oath <command> [options]
              info                      Display OATH application status
              reset [--force|-f]         Erase all OATH accounts and the password
              access change [--password|-p PASSWORD] [--new-password|-n PASSWORD] [--clear|-c]
              accounts list [--password|-p PASSWORD]
              accounts code [QUERY] [--password|-p PASSWORD]
              accounts add NAME SECRET [options]
              accounts delete NAME [--force|-f] [--password|-p PASSWORD]
              accounts rename NAME NEW_NAME [--force|-f] [--password|-p PASSWORD]
              accounts uri URI [--touch] [--force|-f] [--password|-p PASSWORD]

            Add options: --issuer ISSUER, --oath-type TOTP|HOTP, --digits DIGITS,
              --algorithm SHA1|SHA256|SHA512, --period SECONDS, --touch, --force|-f,
              --password|-p PASSWORD. Defaults: TOTP, 6 digits, SHA1, 30 seconds.
            Options may precede, separate, or follow positional arguments after the command.
            Boolean flags take no value. Value options require one following non-option token.
            Use -- to end options for positional arguments beginning with '-'.
            --help or -h displays help before accessing a device, unless after --.
            Bare help requests help only as the command or immediately after accounts/access.
            Option values and positional data named help remain data.
            Genuine help takes precedence over --force and argument errors.
            Ctrl+C requests cancellation; an admitted device exchange may finish.
            Redirected line reads cannot be interrupted; cancellation is checked when they return.
            """);
    }
}

public sealed class OathInvocation
{
    private sealed record CommandShape(string? Group, string Name, string Command, int Min, int Max, string[] Options);

    private static readonly CommandShape[] Shapes =
    [
        new(null, "info", "info", 0, 0, []),
        new(null, "reset", "reset", 0, 0, ["--force"]),
        new("access", "change", "access change", 0, 0, ["--password", "--new-password", "--clear"]),
        new("accounts", "list", "accounts list", 0, 0, ["--password"]),
        new("accounts", "code", "accounts code", 0, 1, ["--password"]),
        new("accounts", "delete", "accounts delete", 1, 1, ["--password", "--force"]),
        new("accounts", "rename", "accounts rename", 2, 2, ["--password", "--force"]),
        new("accounts", "uri", "accounts uri", 1, 1, ["--password", "--force", "--touch"]),
        new("accounts", "add", "accounts add", 2, 2,
            ["--password", "--force", "--touch", "--issuer", "--oath-type", "--digits", "--algorithm", "--period"])
    ];

    // Only recognized option names are canonicalized. Never transform or echo an unclassified token.
    private static readonly Dictionary<string, string> OptionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["--force"] = "--force",
        ["-f"] = "--force",
        ["--password"] = "--password",
        ["-p"] = "--password",
        ["--new-password"] = "--new-password",
        ["-n"] = "--new-password",
        ["--clear"] = "--clear",
        ["-c"] = "--clear",
        ["--touch"] = "--touch",
        ["--issuer"] = "--issuer",
        ["--oath-type"] = "--oath-type",
        ["--digits"] = "--digits",
        ["--algorithm"] = "--algorithm",
        ["--period"] = "--period"
    };

    private readonly List<string> _positionals = [];
    private readonly Dictionary<string, string?> _options = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<string> Positionals => _positionals;
    public string Command { get; private init; } = "";
    public string? Value(string option) => _options.GetValueOrDefault(option);
    public bool Flag(string option) => _options.ContainsKey(option);
    public int Number(string option, int fallback) => Value(option) is { } value
        ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;

    public static bool TryParse(string[] args, out OathInvocation invocation, out string? error)
    {
        var shape = FindCommandShape(args, out int start);
        invocation = new OathInvocation { Command = shape?.Command ?? "" };
        error = "Unknown or missing command.";
        if (shape is null)
        {
            return false;
        }

        if (!invocation.ReadArguments(args, start, shape.Options, out error))
        {
            return false;
        }

        return invocation.ValidateArguments(shape, out error);
    }

    public static bool RequestsHelp(string[] args)
    {
        if (RequestsCommandHelp(args))
        {
            return true;
        }

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--")
            {
                break;
            }

            if (Same(args[i], "--help") || Same(args[i], "-h"))
            {
                return true;
            }

            // Match the parser's value arity. A following option is not a value, so a genuine
            // help flag still wins when a preceding value option is malformed.
            if (OptionNames.TryGetValue(args[i], out string? option) && TakesValue(option) && HasValue(args, i))
            {
                i++;
            }
        }

        return false;
    }

    private static bool RequestsCommandHelp(string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }

        return Same(args[0], "help") ||
            (IsGroup(args[0]) && args.Length > 1 && Same(args[1], "help"));
    }

    private static CommandShape? FindCommandShape(string[] args, out int start)
    {
        start = 1;
        if (args.Length == 0)
        {
            return null;
        }

        if (!IsGroup(args[0]))
        {
            return Shapes.FirstOrDefault(shape => shape.Group is null && Same(shape.Name, args[0]));
        }

        start = 2;
        return args.Length < 2 ? null : Shapes.FirstOrDefault(shape =>
            Same(shape.Group, args[0]) && Same(shape.Name, args[1]));
    }

    private bool ReadArguments(string[] args, int start, string[] allowed, out string? error)
    {
        error = null;
        bool positionalOnly = false;
        for (int i = start; i < args.Length; i++)
        {
            string arg = args[i];
            if (!positionalOnly && arg == "--")
            {
                positionalOnly = true;
                continue;
            }

            if (positionalOnly || !arg.StartsWith('-'))
            {
                _positionals.Add(arg);
                continue;
            }

            if (!ReadOption(args, ref i, allowed, out error))
            {
                return false;
            }
        }

        return true;
    }

    private bool ReadOption(string[] args, ref int index, string[] allowed, out string? error)
    {
        error = "Unknown, unsupported, or duplicate option.";
        if (!OptionNames.TryGetValue(args[index], out string? option) ||
            !allowed.Contains(option) || _options.ContainsKey(option))
        {
            return false;
        }

        string? value = null;
        if (TakesValue(option))
        {
            error = $"Option {option} requires a value.";
            if (!HasValue(args, index))
            {
                return false;
            }

            value = args[++index];
        }

        _options.Add(option, value);
        error = null;
        return true;
    }

    private bool ValidateArguments(CommandShape shape, out string? error)
    {
        error = $"Expected {shape.Min} to {shape.Max} positional arguments for '{shape.Command}'.";
        if (_positionals.Count < shape.Min || _positionals.Count > shape.Max)
        {
            return false;
        }

        error = "Invalid add option value.";
        if (!ValidNumber("--digits") || !ValidNumber("--period") ||
            !ValidChoice("--oath-type", ["TOTP", "HOTP"]) ||
            !ValidChoice("--algorithm", ["SHA1", "SHA256", "SHA512"]))
        {
            return false;
        }

        error = null;
        return true;
    }

    private bool ValidNumber(string option) => Value(option) is not { } value ||
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    private bool ValidChoice(string option, string[] choices) => Value(option) is not { } value ||
        choices.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static bool Same(string? left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    private static bool IsGroup(string arg) => Same(arg, "accounts") || Same(arg, "access");
    private static bool TakesValue(string option) => option is not ("--force" or "--touch" or "--clear");
    private static bool HasValue(string[] args, int index) => index + 1 < args.Length && !args[index + 1].StartsWith('-');
}