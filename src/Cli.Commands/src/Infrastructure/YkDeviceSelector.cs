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
public sealed class YkDeviceSelector : DeviceSelectorBase
{
    private readonly ConnectionType[] _supportedTypes;

    /// <param name="supportedTypes">
    ///     Connection types this applet supports, in preference order
    ///     (first is auto-selected in non-interactive mode if available).
    /// </param>
    public YkDeviceSelector(ConnectionType[] supportedTypes)
    {
        _supportedTypes = supportedTypes;
    }

    protected override ConnectionType[] SupportedConnectionTypes => _supportedTypes;

    protected override string SupportedTransportsDescription =>
        string.Join(", ", _supportedTypes.Select(ConnectionTypeFormatter.Format));

    /// <summary>
    ///     Auto-selects the first device whose transport matches the first preferred type.
    ///     Falls back to the first device if none match.
    /// </summary>
    protected override IYubiKey? AutoSelectDevice(IReadOnlyList<IYubiKey> devices) =>
        devices.FirstOrDefault(d => d.ConnectionType == _supportedTypes[0])
        ?? devices[0];
}
