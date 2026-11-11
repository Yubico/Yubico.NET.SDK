// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.Commands.Infrastructure;

/// <summary>
///     Enriched device context available to every <c>yk</c> command after device selection.
///     <para>
///         After selection, a best-effort one-shot Management query populates <see cref="Info" />.
///         Its temporary session and connection are closed before command execution; this context
///         holds a metadata snapshot, not an open <see cref="ManagementSession" />.
///         Selection may already have queried metadata.
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
    ///     Device-information snapshot, or <c>null</c> when enrichment fails, for example because the
    ///     selected default transport is busy/unavailable or the query fails. Management also supports HID;
    ///     absent SmartCard alone does not imply failure. Current Core FIPS masks have a mapping issue and
    ///     flagged form factors can miss named enum values; do not infer approval or family eligibility from them.
    /// </summary>
    public DeviceInfo? Info { get; init; }

    /// <summary>
    ///     The concrete transport requested by the global <c>--transport</c> option, or <see langword="null" />
    ///     to use each applet's default transport order.
    /// </summary>
    public ConnectionType? PreferredConnection { get; init; }

    /// <summary>
    ///     Gets fresh session creation options carrying the selected transport and shared terminal prompt.
    /// </summary>
    public SessionCreationOptions SessionOptions => new()
    {
        PreferredConnectionType = PreferredConnection,
        UserPresencePrompt = ConsoleUserPresencePrompt.Instance
    };

    /// <summary>
    ///     Returns a human-readable device banner line, preferring the part number from
    ///     <see cref="Info" /> over the generic form-factor display name.
    /// </summary>
    public string DisplayBanner =>
        Info?.PartNumber is { Length: > 0 } partNumber
            ? $"{partNumber} — S/N: {Selection.SerialNumber?.ToString() ?? "N/A"} [{Selection.FirmwareVersion}]"
            : Selection.DisplayName;
}