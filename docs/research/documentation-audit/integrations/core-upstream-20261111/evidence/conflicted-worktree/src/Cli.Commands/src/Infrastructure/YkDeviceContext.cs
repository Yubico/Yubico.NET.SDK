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
<<<<<<< HEAD
///         After selection, a best-effort one-shot Management query populates <see cref="Info" />.
///         Its temporary session and connection are closed before command execution; this context
///         holds a metadata snapshot, not an open <see cref="ManagementSession" />.
///         Selection may already have queried metadata.
=======
///         Selection metadata is last-known and suitable for display. Commands requiring authoritative
///         current device state explicitly read Management information rather than relying on <see cref="Info" />.
>>>>>>> 5746e72ed9a848d08b5782ddde025aa4a4b522ef
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
<<<<<<< HEAD
    ///     Device-information snapshot, or <c>null</c> when enrichment fails, for example because the
    ///     selected default transport is busy/unavailable or the query fails. Management also supports HID;
    ///     absent SmartCard alone does not imply failure. Current Core FIPS masks have a mapping issue and
    ///     flagged form factors can miss named enum values; do not infer approval or family eligibility from them.
=======
    ///     Last-known discovery metadata or selection's best-effort live fallback; null if unavailable.
    ///     Not guaranteed current. Do not use this snapshot for configuration or reset prerequisites.
>>>>>>> 5746e72ed9a848d08b5782ddde025aa4a4b522ef
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
    /// Reads authoritative current Management information over the requested session transport.
    /// A failed read returns null, never the selection snapshot; cancellation propagates.
    /// </summary>
    public async Task<DeviceInfo?> ReadLiveInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Device.GetDeviceInfoAsync(SessionOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OutputHelpers.WriteInfo($"Could not retrieve full device info: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Returns a human-readable device banner line, preferring the part number from
    ///     <see cref="Info" /> over the generic form-factor display name.
    /// </summary>
    public string DisplayBanner =>
        Info?.PartNumber is { Length: > 0 } partNumber
            ? $"{partNumber} — S/N: {Selection.SerialNumber?.ToString() ?? "N/A"} [{Selection.FirmwareVersion}]"
            : Selection.DisplayName;
}