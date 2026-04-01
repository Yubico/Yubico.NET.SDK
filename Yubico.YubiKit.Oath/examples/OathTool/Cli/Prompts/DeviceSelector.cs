// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

/// <summary>
/// Represents a selected YubiKey device with its identifying information.
/// </summary>
/// <param name="Device">The selected YubiKey device.</param>
/// <param name="SerialNumber">The device serial number, if available.</param>
/// <param name="FormFactor">The device form factor.</param>
/// <param name="FirmwareVersion">The firmware version string.</param>
public record DeviceSelection(
    IYubiKey Device,
    int? SerialNumber,
    FormFactor FormFactor,
    string FirmwareVersion)
{
    /// <summary>
    /// Gets a display string for this device.
    /// </summary>
    public string DisplayName =>
        SerialNumber.HasValue
            ? $"YubiKey {FormatFormFactor(FormFactor)} (Serial: {SerialNumber})"
            : $"YubiKey {FormatFormFactor(FormFactor)}";

    private static string FormatFormFactor(FormFactor formFactor) =>
        DeviceSelector.FormatFormFactor(formFactor);
}

/// <summary>
/// Handles YubiKey device discovery and selection for OATH operations.
/// Auto-selects when only one device is connected. Fails with clear error
/// when multiple devices are found (no interactive prompt in CLI mode).
/// </summary>
public static class DeviceSelector
{
    /// <summary>
    /// Finds a single connected YubiKey via SmartCard.
    /// Auto-selects when exactly one device is found.
    /// Fails with an error when zero or multiple devices are connected.
    /// </summary>
    public static async Task<DeviceSelection?> SelectDeviceAsync(
        CancellationToken cancellationToken = default)
    {
        var allDevices = await YubiKeyManager.FindAllAsync(
            ConnectionType.All, cancellationToken: cancellationToken);

        var devices = allDevices
            .Where(d => d.ConnectionType == ConnectionType.SmartCard)
            .ToList();

        if (devices.Count == 0)
        {
            OutputHelpers.WriteError("No YubiKey detected via SmartCard. Insert a YubiKey and try again.");
            return null;
        }

        if (devices.Count > 1)
        {
            OutputHelpers.WriteError(
                $"Multiple YubiKeys detected ({devices.Count}). " +
                "Remove extra devices so only one is connected.");
            return null;
        }

        var device = devices[0];
        var info = await GetDeviceInfoAsync(device, cancellationToken);
        return new DeviceSelection(
            device,
            info?.SerialNumber,
            info?.FormFactor ?? FormFactor.Unknown,
            info?.FirmwareVersion.ToString() ?? "Unknown");
    }

    /// <summary>
    /// Gets device info for a single device.
    /// </summary>
    private static async Task<DeviceInfo?> GetDeviceInfoAsync(
        IYubiKey device,
        CancellationToken cancellationToken)
    {
        try
        {
            return await device.GetDeviceInfoAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Formats form factor for display.
    /// </summary>
    public static string FormatFormFactor(FormFactor formFactor) =>
        formFactor switch
        {
            FormFactor.UsbAKeychain => "5A",
            FormFactor.UsbANano => "5 Nano",
            FormFactor.UsbCKeychain => "5C",
            FormFactor.UsbCNano => "5C Nano",
            FormFactor.UsbCLightning => "5Ci",
            FormFactor.UsbABiometricKeychain => "Bio",
            FormFactor.UsbCBiometricKeychain => "Bio (USB-C)",
            _ => "Unknown"
        };
}