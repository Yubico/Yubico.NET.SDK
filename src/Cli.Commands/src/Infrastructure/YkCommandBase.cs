// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Cli.Commands.Infrastructure;

/// <summary>
///     Abstract base class for all <c>yk</c> commands. Handles the common lifecycle:
///     <list type="number">
///         <item>Select device (filtered by <see cref="AppletTransports" /> and optional <c>--serial</c>).</item>
///         <item>Carry selection metadata into <see cref="YkDeviceContext.Info" /> without another live read.</item>
///         <item>Delegate to <see cref="ExecuteCommandAsync" />; commands request live information when required.</item>
///     </list>
/// </summary>
public abstract class YkCommandBase<TSettings> : AsyncCommand<TSettings>
    where TSettings : GlobalSettings
{
    /// <summary>
    ///     Connection types this applet supports, in device-selection preference order.
    ///     With multiple devices in non-interactive mode, prefer a device exposing the first type.
    ///     This does not pin the operation's transport; only an explicit transport option does that.
    /// </summary>
    protected abstract ConnectionType[] AppletTransports { get; }

    // Test seam at the hardware-selection boundary, shared by every actual command endpoint.
    internal Func<ConnectionType[], int?, ConnectionType?, Task<DeviceSelection?>> SelectDeviceAsync { get; init; } =
        static (transports, serial, requested) =>
            new DeviceSelector(transports, NonInteractiveSelection.PreferFirstTransport, serial, requested)
                .SelectDeviceAsync(CancellationToken.None);

    /// <inheritdoc />
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        try
        {
            if (!TryParseTransport(settings.Transport, out var requestedTransport))
            {
                OutputHelpers.WriteError($"Unsupported transport '{settings.Transport}'. Use smartcard, fido, or otp.");
                return ExitCode.GenericError;
            }

            // 1. Device selection
            DeviceSelection? selection;
            try
            {
                selection = await SelectDeviceAsync(AppletTransports, settings.Serial, requestedTransport);
            }
            catch (ArgumentException ex)
            {
                OutputHelpers.WriteError(ex.Message);
                return ExitCode.FeatureUnsupported;
            }

            if (selection is null)
            {
                OutputHelpers.WriteError("No YubiKey detected. Insert a YubiKey and try again.");
                return ExitCode.DeviceNotFound;
            }

            // Reuse selection's display snapshot; commands read live metadata when their task requires it.
            var deviceContext = new YkDeviceContext
            {
                Device = selection.Device,
                Selection = selection,
                Info = selection.Info,
                PreferredConnection = requestedTransport
            };

            OutputHelpers.WriteActiveDevice(deviceContext.DisplayBanner);

            // 2. Execute the applet command
            return await ExecuteCommandAsync(context, settings, deviceContext);
        }
        catch (OperationCanceledException)
        {
            return ExitCode.UserCancelled;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError(ex.Message);
            return ExitCode.GenericError;
        }
    }

    /// <summary>
    ///     Executes command-specific logic with the selected device and its best-effort display metadata.
    /// </summary>
    /// <param name="context">The Spectre.Console.Cli command context.</param>
    /// <param name="settings">Typed settings including all global flags.</param>
    /// <param name="deviceContext">The selected and enriched YubiKey device context.</param>
    /// <returns>Exit code (use <see cref="ExitCode" /> constants).</returns>
    protected abstract Task<int> ExecuteCommandAsync(
        CommandContext context,
        TSettings settings,
        YkDeviceContext deviceContext);

    private static bool TryParseTransport(string? value, out ConnectionType? transport)
    {
        transport = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        transport = value.Trim().ToLowerInvariant() switch
        {
            "smartcard" or "ccid" => ConnectionType.SmartCard,
            "fido" or "hidfido" or "fido-hid" => ConnectionType.HidFido,
            "otp" or "hidotp" or "otp-hid" => ConnectionType.HidOtp,
            _ => null
        };

        return transport is not null;
    }
}