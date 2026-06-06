// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Cli.Commands.Infrastructure;

/// <summary>
///     Device selector for the unified <c>yk</c> CLI. Accepts a dynamic set of
///     supported connection types so a single selector class serves all applets —
///     each applet branch configures the transports it requires.
/// </summary>
public class YkDeviceSelector : DeviceSelectorBase
{
    private readonly ConnectionType[] _effectiveTypes;
    private readonly int? _serial;

    /// <param name="supportedTypes">
    ///     Connection types this applet supports, in preference order
    ///     (first is auto-selected in non-interactive mode if available).
    /// </param>
    /// <param name="serial">Optional target serial number from the global <c>--serial</c> option.</param>
    /// <param name="requestedTransport">Optional target transport from the global <c>--transport</c> option.</param>
    public YkDeviceSelector(
        ConnectionType[] supportedTypes,
        int? serial = null,
        ConnectionType? requestedTransport = null)
    {
        ArgumentNullException.ThrowIfNull(supportedTypes);
        if (supportedTypes.Length == 0)
        {
            throw new ArgumentException("At least one supported transport is required.", nameof(supportedTypes));
        }

        if (requestedTransport is { } transport && !supportedTypes.Contains(transport))
        {
            throw new ArgumentException(
                $"Transport {ConnectionTypeFormatter.Format(transport)} is not supported by this command.",
                nameof(requestedTransport));
        }

        _effectiveTypes = requestedTransport is { } requested
            ? [requested]
            : [.. supportedTypes];
        _serial = serial;
    }

    protected override ConnectionType[] SupportedConnectionTypes => _effectiveTypes;

    protected override int? TargetSerialNumber => _serial;

    protected override string SupportedTransportsDescription =>
        string.Join(", ", _effectiveTypes.Select(ConnectionTypeFormatter.Format));

    /// <summary>
    ///     Auto-selects the first device whose transport matches the first preferred type.
    ///     Falls back to the first device if none match.
    /// </summary>
    protected override IYubiKey? AutoSelectDevice(IReadOnlyList<IYubiKey> devices) =>
        devices.FirstOrDefault(d => d.ConnectionType == _effectiveTypes[0])
        ?? devices[0];
}