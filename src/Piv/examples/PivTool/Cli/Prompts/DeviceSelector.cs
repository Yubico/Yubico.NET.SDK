// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;

/// <summary>PIV's banner shape omits the transport suffix used by the shared selection result.</summary>
public record DeviceSelection(IYubiKey Device, int? SerialNumber, FormFactor FormFactor, string FirmwareVersion)
{
    /// <summary>Gets the device display name.</summary>
    public string DisplayName => SerialNumber.HasValue
        ? $"YubiKey {FormFactorFormatter.Format(FormFactor)} - S/N: {SerialNumber}"
        : $"YubiKey {FormFactorFormatter.Format(FormFactor)}";
}

/// <summary>SmartCard-only selection for the PIV menus.</summary>
public static class DeviceSelector
{
    private static readonly SharedSelector Selector = new(
        [ConnectionType.SmartCard], NonInteractiveSelection.RequireChoice);

    /// <summary>Finds an eligible YubiKey, requiring an explicit choice when multiple devices are present.</summary>
    public static async Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default)
    {
        var selection = await Selector.SelectDeviceAsync(cancellationToken);
        return selection is null ? null : new DeviceSelection(
            selection.Device, selection.SerialNumber, selection.FormFactor, selection.FirmwareVersion);
    }

    /// <summary>Formats form factor for display.</summary>
    public static string FormatFormFactor(FormFactor formFactor) => FormFactorFormatter.Format(formFactor);
}