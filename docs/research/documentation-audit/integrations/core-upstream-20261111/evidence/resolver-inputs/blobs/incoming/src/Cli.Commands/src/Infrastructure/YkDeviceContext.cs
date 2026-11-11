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
///         Selection metadata is last-known and suitable for display. Commands requiring authoritative
///         current device state explicitly read Management information rather than relying on <see cref="Info" />.
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
    ///     Last-known discovery metadata or selection's best-effort live fallback; null if unavailable.
    ///     Not guaranteed current. Do not use this snapshot for configuration or reset prerequisites.
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