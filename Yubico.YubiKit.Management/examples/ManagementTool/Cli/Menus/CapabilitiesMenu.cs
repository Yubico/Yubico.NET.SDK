// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Output;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;
using Yubico.YubiKit.Management.Examples.ManagementTool.ManagementExamples;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;

/// <summary>
/// CLI menu for configuring USB/NFC capabilities.
/// </summary>
public static class CapabilitiesMenu
{
    public static async Task RunAsync(Transport transport, CancellationToken cancellationToken = default)
    {
        var transportName = transport == Transport.Usb ? "USB" : "NFC";
        OutputHelpers.WriteHeader($"{transportName} Capabilities Configuration");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var connection = await selection.Device.ConnectAsync<ISmartCardConnection>(cancellationToken);
        await using var session = await ManagementSession.CreateAsync(connection, cancellationToken: cancellationToken);

        var infoResult = await DeviceInfoQuery.GetDeviceInfoAsync(session, cancellationToken);
        if (!infoResult.Success || !infoResult.DeviceInfo.HasValue)
        {
            OutputHelpers.WriteError(infoResult.ErrorMessage ?? "Failed to get device info");
            return;
        }

        var info = infoResult.DeviceInfo.Value;

        // Get supported and enabled capabilities for this transport
        var supported = transport == Transport.Usb ? info.UsbSupported : info.NfcSupported;
        var enabled = transport == Transport.Usb ? info.UsbEnabled : info.NfcEnabled;

        if (supported == DeviceCapabilities.None)
        {
            OutputHelpers.WriteError($"This device does not support {transportName}");
            return;
        }

        // Display current state
        AnsiConsole.MarkupLine("[bold]Current Configuration[/]");
        OutputHelpers.WriteCapabilities("Supported", supported);
        OutputHelpers.WriteCapabilities("Enabled", enabled);
        AnsiConsole.WriteLine();

        // Build list of available capabilities
        var availableCapabilities = GetAvailableCapabilities(supported);
        if (availableCapabilities.Count == 0)
        {
            OutputHelpers.WriteError("No capabilities available to configure");
            return;
        }

        // Multi-select prompt for capabilities
        var currentlyEnabled = GetEnabledCapabilityNames(enabled);
        var prompt = new MultiSelectionPrompt<string>()
            .Title($"Select {transportName} capabilities to enable:")
            .PageSize(10)
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
            .AddChoices(availableCapabilities.Keys);

        // Pre-select currently enabled capabilities
        foreach (var item in currentlyEnabled)
        {
            prompt.Select(item);
        }

        var selectedNames = AnsiConsole.Prompt(prompt);

        // Convert selections to capabilities flags
        DeviceCapabilities newCapabilities = DeviceCapabilities.None;
        foreach (var name in selectedNames)
        {
            if (availableCapabilities.TryGetValue(name, out var cap))
            {
                newCapabilities |= cap;
            }
        }

        // Validation: USB must have at least one capability
        if (transport == Transport.Usb && newCapabilities == DeviceCapabilities.None)
        {
            OutputHelpers.WriteError("USB must have at least one capability enabled");
            return;
        }

        // Show diff
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Changes[/]");
        OutputHelpers.WriteKeyValue("Before", FormatCapabilities(enabled));
        OutputHelpers.WriteKeyValue("After", FormatCapabilities(newCapabilities));

        // Show what's being disabled
        var disabled = enabled & ~newCapabilities;
        if (disabled != DeviceCapabilities.None)
        {
            AnsiConsole.WriteLine();
            OutputHelpers.WriteWarning($"The following capabilities will be DISABLED: {FormatCapabilities(disabled)}");
        }

        // Prompt for lock code if device is locked
        byte[]? lockCode = null;
        if (info.IsLocked)
        {
            AnsiConsole.WriteLine();
            lockCode = LockCodePrompt.PromptForLockCode("Device is locked. Enter lock code to proceed:");
            if (lockCode is null)
            {
                return;
            }
        }

        try
        {
            // Confirm with warning
            AnsiConsole.WriteLine();
            if (!OutputHelpers.ConfirmDangerous("change device capabilities (device will reboot)"))
            {
                OutputHelpers.WriteInfo("Operation cancelled");
                return;
            }

            // Apply configuration
            var result = await DeviceConfiguration.SetCapabilitiesAsync(
                session,
                transport,
                newCapabilities,
                lockCode,
                reboot: true,
                cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Capabilities updated successfully");
                if (result.RebootRequired)
                {
                    OutputHelpers.WriteWarning("Device is rebooting. Please wait and reconnect.");
                }
            }
            else
            {
                OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to update capabilities");
            }
        }
        finally
        {
            if (lockCode is not null)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(lockCode);
            }
        }
    }

    private static Dictionary<string, DeviceCapabilities> GetAvailableCapabilities(DeviceCapabilities supported)
    {
        var result = new Dictionary<string, DeviceCapabilities>();

        if (supported.HasFlag(DeviceCapabilities.Otp)) result["OTP"] = DeviceCapabilities.Otp;
        if (supported.HasFlag(DeviceCapabilities.U2f)) result["U2F"] = DeviceCapabilities.U2f;
        if (supported.HasFlag(DeviceCapabilities.Fido2)) result["FIDO2"] = DeviceCapabilities.Fido2;
        if (supported.HasFlag(DeviceCapabilities.Piv)) result["PIV"] = DeviceCapabilities.Piv;
        if (supported.HasFlag(DeviceCapabilities.OpenPgp)) result["OpenPGP"] = DeviceCapabilities.OpenPgp;
        if (supported.HasFlag(DeviceCapabilities.Oath)) result["OATH"] = DeviceCapabilities.Oath;
        if (supported.HasFlag(DeviceCapabilities.HsmAuth)) result["HSM Auth"] = DeviceCapabilities.HsmAuth;

        return result;
    }

    private static List<string> GetEnabledCapabilityNames(DeviceCapabilities enabled)
    {
        var result = new List<string>();

        if (enabled.HasFlag(DeviceCapabilities.Otp)) result.Add("OTP");
        if (enabled.HasFlag(DeviceCapabilities.U2f)) result.Add("U2F");
        if (enabled.HasFlag(DeviceCapabilities.Fido2)) result.Add("FIDO2");
        if (enabled.HasFlag(DeviceCapabilities.Piv)) result.Add("PIV");
        if (enabled.HasFlag(DeviceCapabilities.OpenPgp)) result.Add("OpenPGP");
        if (enabled.HasFlag(DeviceCapabilities.Oath)) result.Add("OATH");
        if (enabled.HasFlag(DeviceCapabilities.HsmAuth)) result.Add("HSM Auth");

        return result;
    }

    private static string FormatCapabilities(DeviceCapabilities caps)
    {
        if (caps == DeviceCapabilities.None)
        {
            return "None";
        }

        var parts = new List<string>();
        if (caps.HasFlag(DeviceCapabilities.Otp)) parts.Add("OTP");
        if (caps.HasFlag(DeviceCapabilities.U2f)) parts.Add("U2F");
        if (caps.HasFlag(DeviceCapabilities.Fido2)) parts.Add("FIDO2");
        if (caps.HasFlag(DeviceCapabilities.Piv)) parts.Add("PIV");
        if (caps.HasFlag(DeviceCapabilities.OpenPgp)) parts.Add("OpenPGP");
        if (caps.HasFlag(DeviceCapabilities.Oath)) parts.Add("OATH");
        if (caps.HasFlag(DeviceCapabilities.HsmAuth)) parts.Add("HSM");

        return string.Join(", ", parts);
    }
}
