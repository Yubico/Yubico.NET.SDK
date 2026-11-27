// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli;

// Local application dependencies; no protocol or SDK behavior belongs here.
public sealed class OathToolServices(
    Func<CancellationToken, Task<IOathSession?>> openSessionAsync,
    ISecureCredentialReader credentialReader,
    Func<string, CancellationToken, ValueTask<bool>> confirmAsync)
{
    public static OathToolServices Default { get; } = new(OpenSessionAsync, new ConsoleCredentialReader(), ReadConfirmationAsync);
    public async Task<IOathSession?> OpenSession(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = await openSessionAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            if (session is not null)
            {
                await session.DisposeAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return session;
    }
    public ISecureCredentialReader CredentialReader { get; } = credentialReader;
    public async ValueTask<bool> Confirm(string prompt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool confirmed = await confirmAsync(prompt, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return confirmed;
    }

    private static async Task<IOathSession?> OpenSessionAsync(CancellationToken cancellationToken)
        => await OpenSelectedSessionAsync(cancellationToken);

    public static async Task<IOathSession?> OpenSelectedSessionAsync(CancellationToken cancellationToken,
        IAnsiConsole? console = null,
        Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>>? discover = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken, console, discover);
        cancellationToken.ThrowIfCancellationRequested();
        return selection is null ? null : await selection.Device.CreateOathSessionAsync(cancellationToken: cancellationToken);
    }

    private static async ValueTask<bool> ReadConfirmationAsync(string prompt, CancellationToken cancellationToken)
    {
        // Console's redirected line read is synchronous and cannot be interrupted. The wrapper
        // checks cancellation after it returns, before any subsequent device operation.
        if (Console.IsInputRedirected)
        {
            Console.Error.Write($"{prompt} [y/N] ");
            return string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase);
        }

        return await new ConfirmationPrompt(Markup.Escape(prompt)) { DefaultValue = false }
            .ShowAsync(AnsiConsole.Console, cancellationToken);
    }
}