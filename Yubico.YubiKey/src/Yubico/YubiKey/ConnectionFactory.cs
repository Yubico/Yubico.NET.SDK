// Copyright 2025 Yubico AB
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Devices.SmartCard;
using Yubico.YubiKey.Scp;
#pragma warning disable CS0618 // Type or member is obsolete
using StaticKeys = Yubico.YubiKey.Scp03.StaticKeys;
#pragma warning restore CS0618 // Type or member is obsolete

namespace Yubico.YubiKey;

/// <summary>
///     Factory class responsible for creating connections to YubiKey devices.
/// </summary>
/// <remarks>
///     The ConnectionFactory manages the creation of different types of connections to a YubiKey device,
///     including SmartCard and HID interfaces. It handles both secure channel protocol (SCP) and standard
///     connections based on the application requirements.
/// </remarks>
internal class ConnectionFactory
{
    private readonly YubiKeyDevice _device;
    private readonly IHidDevice? _hidFidoDevice;
    private readonly IHidDevice? _hidKeyboardDevice;
    private readonly ILogger _log;
    private readonly ISmartCardDevice? _smartCardDevice;

    /// <summary>
    ///     Initializes a new instance of the ConnectionFactory class.
    /// </summary>
    /// <param name="log">Logger instance for recording events and diagnostics.</param>
    /// <param name="device">The YubiKey device to create connections for.</param>
    /// <param name="smartCardDevice">The SmartCard interface of the YubiKey, if available.</param>
    /// <param name="hidKeyboardDevice">The HID keyboard interface of the YubiKey, if available.</param>
    /// <param name="hidFidoDevice">The HID FIDO interface of the YubiKey, if available.</param>
    public ConnectionFactory(
        ILogger log,
        YubiKeyDevice device,
        ISmartCardDevice? smartCardDevice,
        IHidDevice? hidKeyboardDevice,
        IHidDevice? hidFidoDevice)
    {
        _log = log;
        _device = device;
        _smartCardDevice = smartCardDevice;
        _hidKeyboardDevice = hidKeyboardDevice;
        _hidFidoDevice = hidFidoDevice;
    }

    [Obsolete("Obsolete")]
    internal IScp03YubiKeyConnection CreateScpConnection(YubiKeyApplication application, StaticKeys scp03Keys)
    {
        if (_smartCardDevice is null)
        {
            throw new InvalidOperationException(
                "No smart card interface present. Unable to establish SCP connection to YubiKey.");
        }

        _log.LogDebug("Connecting via the SmartCard interface using SCP03.");
        WaitForReclaimTimeout(Transport.SmartCard);

        return new Scp03Connection(_smartCardDevice, application, scp03Keys);
    }

    /// <summary>
    ///     Creates a secure channel protocol (SCP) connection to a specific YubiKey application.
    /// </summary>
    /// <param name="application">The YubiKey application to connect to.</param>
    /// <param name="keyParameters">The security parameters for establishing the SCP connection.</param>
    /// <returns>A secure connection to the specified YubiKey application.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the SmartCard interface is not available on the YubiKey.</exception>
    /// <remarks>
    ///     This method establishes a secure channel to the YubiKey using the SmartCard interface. The connection
    ///     is protected using the Secure Channel Protocol (SCP) with the provided key parameters.
    /// </remarks>
    public IScpYubiKeyConnection CreateScpConnection(YubiKeyApplication application, ScpKeyParameters keyParameters)
    {
        LogConnectionAttempt(application, keyParameters);

        if (_smartCardDevice is null)
        {
            throw new InvalidOperationException(
                "No smart card interface present. Unable to establish SCP connection to YubiKey.");
        }

        _log.LogDebug("Connecting via the SmartCard interface using SCP.");
        WaitForReclaimTimeout(Transport.SmartCard);

        return new ScpConnection(_smartCardDevice, application, keyParameters);
    }

    /// <summary>
    ///     Creates a standard (non-SCP) connection to a specific YubiKey application.
    /// </summary>
    /// <param name="application">The YubiKey application to connect to.</param>
    /// <returns>A connection to the specified YubiKey application.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no suitable interface is available for the requested
    ///     application.
    /// </exception>
    /// <remarks>
    ///     This method creates a connection using the most appropriate interface available for the specified application.
    ///     It first attempts to use the SmartCard interface, then falls back to HID interfaces if necessary.
    /// </remarks>
    public IYubiKeyConnection CreateConnection(YubiKeyApplication application)
    {
        if (_hidKeyboardDevice != null && application == YubiKeyApplication.Otp)
        {
            _log.LogDebug("Connecting via the Keyboard interface.");

            WaitForReclaimTimeout(Transport.HidKeyboard);
            return new KeyboardConnection(_hidKeyboardDevice);
        }

        bool isFidoApplication = application is YubiKeyApplication.Fido2 or YubiKeyApplication.FidoU2f;
        if (_hidFidoDevice != null && isFidoApplication)
        {
            _log.LogDebug("Connecting via the FIDO interface.");

            WaitForReclaimTimeout(Transport.HidFido);
            return new FidoConnection(_hidFidoDevice);
        }

        if (_smartCardDevice != null)
        {
            _log.LogDebug("Connecting via the SmartCard interface.");

            WaitForReclaimTimeout(Transport.SmartCard);
            return new SmartCardConnection(_smartCardDevice, application);
        }

        throw new InvalidOperationException(
            "No suitable interface present. Unable to establish connection to YubiKey.");
    }

    // This function handles waiting for the reclaim timeout on the YubiKey to elapse. The reclaim timeout requires
    // the SDK to wait 3 seconds since the last USB message to an interface before switching to a different interface.
    // Failure to wait can result in very strange behavior from the USB devices ultimately resulting in communication
    // failures (i.e. exceptions).
    private void WaitForReclaimTimeout(Transport newTransport)
    {
        // Newer YubiKeys are able to switch interfaces much, much faster. Maybe this is being paranoid, but we
        // should still probably wait a few milliseconds for things to stabilize. But definitely not the full
        // three seconds! For older keys, we use a value of 3.01 seconds to give us a little wiggle room as the
        // YubiKey's measurement for the reclaim timeout is likely not as accurate as our system clock.
        var reclaimTimeout = CanFastReclaim()
            ? TimeSpan.FromMilliseconds(100)
            : TimeSpan.FromSeconds(3.01);

        // We're only affected by the reclaim timeout if we're switching USB transports.
        if (_device.LastActiveTransport == newTransport)
        {
            _log.LogDebug(
                "{Transport} transport is already active. No need to wait for reclaim.",
                _device.LastActiveTransport);

            return;
        }

        _log.LogDebug(
            "Switching USB transports from {OldTransport} to {NewTransport}.",
            _device.LastActiveTransport,
            newTransport);

        var timeSinceLastActivation = DateTime.Now - GetLastActiveTime();

        // If we haven't already waited the duration of the reclaim timeout, we need to do so.
        // Otherwise, we've already waited and can immediately switch the transport.
        if (timeSinceLastActivation < reclaimTimeout)
        {
            var waitNeeded = reclaimTimeout - timeSinceLastActivation;

            _log.LogDebug(
                "Reclaim timeout still active. Need to wait {TimeMS} milliseconds.",
                waitNeeded.TotalMilliseconds);

            Thread.Sleep(waitNeeded);
        }

        _device.LastActiveTransport = newTransport;

        _log.LogDebug("Reclaim timeout has lapsed. It is safe to switch USB transports.");
    }

    private bool CanFastReclaim()
    {
        if (AppContext.TryGetSwitch(YubiKeyCompatSwitches.UseOldReclaimTimeoutBehavior, out bool useOldBehavior) &&
            useOldBehavior)
        {
            return false;
        }

        return _device.HasFeature(YubiKeyFeature.FastUsbReclaim);
    }

    private DateTime GetLastActiveTime() =>
        _device.LastActiveTransport switch
        {
            Transport.SmartCard when _smartCardDevice is not null => _smartCardDevice.LastAccessed,
            Transport.HidFido when _hidFidoDevice is not null => _hidFidoDevice.LastAccessed,
            Transport.HidKeyboard when _hidKeyboardDevice is not null => _hidKeyboardDevice.LastAccessed,
            Transport.None => DateTime.Now,
            _ => throw new InvalidOperationException(ExceptionMessages.DeviceTypeNotRecognized)
        };

    private void LogConnectionAttempt(
        YubiKeyApplication application,
        ScpKeyParameters keyParameters)
    {
        string applicationName = GetApplicationName(application);
        string scpInfo = keyParameters switch
        {
            Scp03KeyParameters scp03KeyParameters => $"SCP03 ({scp03KeyParameters.KeyReference})",
            Scp11KeyParameters scp11KeyParameters => $"SCP11 ({scp11KeyParameters.KeyReference})",
            _ => "Unknown"
        };

        _log.LogDebug("YubiKey connecting to {Application} application over {ScpInfo}", applicationName, scpInfo);
    }

    private static string GetApplicationName(YubiKeyApplication application) =>
        Enum.GetName(typeof(YubiKeyApplication), application) ?? "Unknown";
}
