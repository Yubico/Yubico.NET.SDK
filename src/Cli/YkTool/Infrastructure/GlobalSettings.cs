// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Yubico.YubiKit.Cli.YkTool.Infrastructure;

/// <summary>
///     Global settings inherited by every <c>yk</c> command. Defines options that
///     apply across all applets: device targeting and mode selection.
/// </summary>
public class GlobalSettings : CommandSettings
{
    [CommandOption("--serial|-s")]
    [Description("Target a specific YubiKey by serial number. Skips device selection prompt.")]
    public int? Serial { get; set; }

    [CommandOption("--transport")]
    [Description("Prefer a specific transport: smartcard, fido, or otp. Overrides applet default.")]
    public string? Transport { get; set; }

    [CommandOption("-i|--interactive")]
    [Description("Launch the interactive menu for this applet instead of running a command.")]
    public bool Interactive { get; set; }
}
