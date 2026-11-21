// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Testing;
using System.Text;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Commands;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class HsmAuthToolInputTests
{
    [Theory]
    [InlineData("--derivation-password", "--derive", false)]
    [InlineData("--derivation-password", "--derive", true)]
    [InlineData("--key-enc", "--import-keys", false)]
    [InlineData("--key-enc", "--import-keys", true)]
    [InlineData("--key-mac", "--import-keys", false)]
    [InlineData("--key-mac", "--import-keys", true)]
    public async Task RetiredSecretOptionIsRejectedBeforeAddWorkflow(string option, string replacement, bool combined)
    {
        const string sentinel = "do-not-print-example-value";
        string[] args = combined
            ? ["credentials", "add", "label", replacement, $"{option.ToUpperInvariant()}={sentinel}"]
            : ["credentials", "add", "label", option, sentinel];
        var console = new TestConsole();
        var previous = AnsiConsole.Console;
        AnsiConsole.Console = console;
        try
        {
            int status = await CredentialsCommand.RunAsync(args, TestContext.Current.CancellationToken,
                (_, _) => throw new InvalidOperationException("The workflow with prompts and device discovery must not run."));

            Assert.Equal(1, status);
            Assert.Contains(replacement, console.Output);
            Assert.DoesNotContain(sentinel, console.Output);
        }
        finally
        {
            AnsiConsole.Console = previous;
        }
    }

    [Fact]
    public async Task NoSecretOptionsStillDispatchesTheAddWorkflow()
    {
        string[] args = ["credentials", "add", "label"];
        var cancellationToken = TestContext.Current.CancellationToken;
        var result = await CredentialsCommand.RunAsync(args, cancellationToken, (received, token) =>
        {
            Assert.Same(args, received);
            Assert.Equal(cancellationToken, token);
            return Task.FromResult(42);
        });
        Assert.Equal(42, result);
    }

    // Both the --derive command and the interactive Add Derived Credential menu use this reader.
    [Theory]
    [InlineData("a", 128, true)]
    [InlineData("a", 129, false)]
    [InlineData("a", 180, false)]
    [InlineData("é", 128, true)]
    [InlineData("🔑", 64, true)]
    [InlineData("🔑", 65, false)]
    public void SharedDerivationPromptRejectsOverflowInsteadOfUsingATruncatedPassword(string unit, int count, bool accepted)
    {
        string input = string.Concat(Enumerable.Repeat(unit, count)); // Synthetic console fixture.
        var token = TestContext.Current.CancellationToken;

        if (accepted)
        {
            using var password = OutputHelpers.ReadDerivationPassword(token, Read);
            Assert.NotNull(password);
            Assert.Equal(Encoding.UTF8.GetBytes(input), password.Memory.ToArray());
        }
        else
        {
            var failure = Assert.Throws<ArgumentException>(() => OutputHelpers.ReadDerivationPassword(token, Read));
            Assert.Contains("128 UTF-16 code units", failure.Message);
        }

        DisposableArrayPoolBuffer Read(CredentialReaderOptions options, CancellationToken readToken)
        {
            Assert.Equal(token, readToken);
            // The interactive reader ignores additional input once the supplied capacity is reached.
            var typed = input.AsSpan(0, Math.Min(input.Length, options.MaxLength));
            var buffer = new DisposableArrayPoolBuffer(options.Encoding.GetByteCount(typed));
            options.Encoding.GetBytes(typed, buffer.Memory.Span);
            return buffer;
        }
    }
}