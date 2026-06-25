// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.Commands.Infrastructure;

/// <summary>
///     Enriched device context available to every <c>yk</c> command after device selection.
///     <para>
///         Unlike the per-applet tools which only know about their own session type,
///         the monolith always opens a <see cref="ManagementSession" /> first to populate
///         <see cref="Info" /> with rich device metadata: part number, firmware, enabled
///         capabilities. Commands can use this for feature gating and display without
///         opening a second management session.
///     </para>
/// </summary>
public sealed class YkDeviceContext
{
    /// <summary>The selected YubiKey device.</summary>
    public required IYubiKey Device { get; init; }

    /// <summary>
    ///     Display metadata (serial, form factor, firmware string, transport) from the
    ///     device selection phase.
    /// </summary>
    public required DeviceSelection Selection { get; init; }

    /// <summary>
    ///     Full device information from <see cref="ManagementSession" />, including part number,
    ///     enabled/supported capabilities, FIPS status, and PIN complexity settings.
    ///     <c>null</c> if the ManagementSession could not be opened (e.g., SmartCard unavailable).
    /// </summary>
    public DeviceInfo? Info { get; init; }

    /// <summary>
    ///     Returns a human-readable device banner line, preferring the part number from
    ///     <see cref="Info" /> over the generic form-factor display name.
    /// </summary>
    public string DisplayBanner =>
        Info?.PartNumber is { Length: > 0 } partNumber
            ? $"{partNumber} — S/N: {Selection.SerialNumber?.ToString() ?? "N/A"} [{Selection.FirmwareVersion}]"
            : Selection.DisplayName;
}
