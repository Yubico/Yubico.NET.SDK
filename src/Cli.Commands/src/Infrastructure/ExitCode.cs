// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

namespace Yubico.YubiKit.Cli.Commands.Infrastructure;

/// <summary>
///     Structured exit codes for the <c>yk</c> CLI.
///     Scripts and CI pipelines can use these to distinguish failure modes.
/// </summary>
public static class ExitCode
{
    /// <summary>Operation completed successfully.</summary>
    public const int Success = 0;

    /// <summary>Unspecified error — check stderr for details.</summary>
    public const int GenericError = 1;

    /// <summary>No YubiKey detected, or the targeted device was not found.</summary>
    public const int DeviceNotFound = 3;

    /// <summary>Authentication failed (wrong PIN, blocked PIN, wrong management key, etc.).</summary>
    public const int AuthenticationFailed = 4;

    /// <summary>Operation cancelled by the user (Ctrl+C or declined confirmation prompt).</summary>
    public const int UserCancelled = 5;

    /// <summary>The requested feature is not supported on this device or firmware version.</summary>
    public const int FeatureUnsupported = 7;
}
