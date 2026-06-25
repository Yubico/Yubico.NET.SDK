// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;

namespace Yubico.YubiKit.Cli.Commands.Infrastructure;

/// <summary>
///     Pre-execution interceptor for all <c>yk</c> commands. Runs before any command's
///     <c>ExecuteAsync</c> is called, allowing cross-cutting concerns (future: telemetry,
///     global validation, audit logging) without modifying individual commands.
/// </summary>
public sealed class YkCommandInterceptor : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        // Reserved for future global-option handling:
        //   - Store GlobalSettings in context.Data for downstream access
        //   - Validate --serial format
        //   - Set up structured logging level from --verbose
        //
        // Currently a no-op; individual commands read GlobalSettings directly
        // from their TSettings (which inherits GlobalSettings).
    }
}
