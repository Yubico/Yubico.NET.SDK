// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.Commands.Infrastructure;

/// <summary>
///     Abstract base class for all <c>yk</c> commands. Handles the common lifecycle:
///     <list type="number">
///         <item>Start YubiKeyManager monitoring.</item>
///         <item>Select device (filtered by <see cref="AppletTransports" /> and optional <c>--serial</c>).</item>
///         <item>Enrich context via ManagementSession (<see cref="YkDeviceContext.Info" />).</item>
///         <item>Delegate to <see cref="ExecuteCommandAsync" /> with the enriched context.</item>
///         <item>Shut down YubiKeyManager on exit.</item>
///     </list>
/// </summary>
public abstract class YkCommandBase<TSettings> : AsyncCommand<TSettings>
    where TSettings : GlobalSettings
{
    /// <summary>
    ///     Connection types this applet uses, in preference order. The first type
    ///     is auto-selected in non-interactive mode when multiple devices are present.
    /// </summary>
    protected abstract ConnectionType[] AppletTransports { get; }

    /// <inheritdoc />
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        YubiKeyManager.StartMonitoring();

        try
        {
            if (!TryParseTransport(settings.Transport, out var requestedTransport))
            {
                OutputHelpers.WriteError($"Unsupported transport '{settings.Transport}'. Use smartcard, fido, or otp.");
                return ExitCode.GenericError;
            }

            // 1. Device selection
            YkDeviceSelector selector;
            try
            {
                selector = new YkDeviceSelector(AppletTransports, settings.Serial, requestedTransport);
            }
            catch (ArgumentException ex)
            {
                OutputHelpers.WriteError(ex.Message);
                return ExitCode.FeatureUnsupported;
            }

            var selection = await selector.SelectDeviceAsync(CancellationToken.None);

            if (selection is null)
            {
                OutputHelpers.WriteError("No YubiKey detected. Insert a YubiKey and try again.");
                return ExitCode.DeviceNotFound;
            }

            // 2. Enrich with ManagementSession data (part number, capabilities, etc.)
            DeviceInfo? deviceInfo = null;
            try
            {
                deviceInfo = await selection.Device.GetDeviceInfoAsync();
            }
            catch (Exception ex)
            {
                // Non-fatal: log a debug warning and proceed without enriched info.
                // This can happen if SmartCard transport is unavailable for this connection.
                OutputHelpers.WriteInfo($"Could not retrieve full device info: {ex.Message}");
            }

            var deviceContext = new YkDeviceContext
            {
                Device = selection.Device,
                Selection = selection,
                Info = deviceInfo,
                PreferredConnection = requestedTransport
            };

            OutputHelpers.WriteActiveDevice(deviceContext.DisplayBanner);

            // 3. Execute the applet command
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
        finally
        {
            await YubiKeyManager.ShutdownAsync();
        }
    }

    /// <summary>
    ///     Executes the command-specific logic with a fully-enriched device context.
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