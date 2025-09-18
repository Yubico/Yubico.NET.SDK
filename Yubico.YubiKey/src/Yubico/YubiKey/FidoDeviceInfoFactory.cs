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
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.DeviceExtensions;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey;

internal static class FidoDeviceInfoFactory
{
    private static readonly ILogger Log = Core.Logging.Log.GetLogger(typeof(FidoDeviceInfoFactory).FullName!);

    public static YubiKeyDeviceInfo GetDeviceInfo(IHidDevice device)
    {
        if (!device.IsYubicoDevice())
        {
            throw new ArgumentException(ExceptionMessages.InvalidDeviceNotYubico, nameof(device));
        }

        if (!device.IsFido())
        {
            throw new ArgumentException(ExceptionMessages.InvalidDeviceNotFido, nameof(device));
        }

        Log.LogInformation("Getting device info for FIDO device {Device}", device);

        if (!TryGetDeviceInfoFromFido(device, out var deviceInfo))
        {
            deviceInfo = new YubiKeyDeviceInfo();
        }

        deviceInfo.IsSkySeries |= device.ProductId == ProductIdentifiers.SecurityKey;

        // Manually fill in gaps, if necessary
        var defaultDeviceInfo = new YubiKeyDeviceInfo();

        if (deviceInfo.FirmwareVersion == defaultDeviceInfo.FirmwareVersion &&
            TryGetFirmwareVersionFromFido(device, out var firmwareVersion))
        {
            deviceInfo.FirmwareVersion = firmwareVersion;
        }

        if (deviceInfo.FirmwareVersion < FirmwareVersion.V4_0_0 &&
            deviceInfo.AvailableUsbCapabilities == YubiKeyCapabilities.None)
        {
            deviceInfo.AvailableUsbCapabilities = YubiKeyCapabilities.FidoU2f;
        }

        return deviceInfo;
    }

    private static bool TryGetDeviceInfoFromFido(
        IHidDevice device,
        [MaybeNullWhen(returnValue: false)] out YubiKeyDeviceInfo deviceInfo)
    {
        try
        {
            Log.LogInformation("Attempting to read device info via the FIDO interface management command.");
            using var connection = new FidoConnection(device);

            deviceInfo = GetDeviceInfoHelper.GetDeviceInfo<GetPagedDeviceInfoCommand>(connection);
            if (deviceInfo is not null)
            {
                Log.LogInformation("Successfully read device info via FIDO interface management command.");
                return true;
            }
        }
        catch (NotImplementedException e)
        {
            ErrorHandler(e, "MacOSHidDevice.ConnectToIOReports() was not implemented on MacOS.");
        }
        catch (MalformedYubiKeyResponseException e)
        {
            // FidoTransform.AcquireCtapHidChannel, nonce did not match
            // FidoTransform.ReceiveResponse, response data length too long
            // Response.GetData, response data length too long
            ErrorHandler(e, "Exception encountered when trying to get device info over FIDO.");
        }
        catch (UnauthorizedAccessException e)
        {
            ErrorHandler(e, "Must have elevated privileges in Windows to access FIDO device directly.");
        }

        Log.LogWarning(
            "Failed to read device info through the management interface. This may be expected for older YubiKeys.");

        deviceInfo = null;
        return false;
    }

    private static bool TryGetFirmwareVersionFromFido(
        IHidDevice device,
        [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
    {
        try
        {
            Log.LogInformation("Attempting to read firmware version through FIDO.");
            using var connection = new FidoConnection(device);

            var response = connection.SendCommand(new VersionCommand());
            if (response.Status == ResponseStatus.Success)
            {
                firmwareVersion = response.GetData();
                Log.LogInformation("Firmware version: {Version}", firmwareVersion.ToString());

                return true;
            }

            Log.LogError(
                "Reading firmware version via FIDO failed with: {Error} {Message}", response.StatusWord,
                response.StatusMessage);
        }
        catch (NotImplementedException e)
        {
            ErrorHandler(e, "MacOSHidDevice.ConnectToIOReports() was not implemented on MacOS.");
        }
        catch (MalformedYubiKeyResponseException e)
        {
            // FidoTransform.AcquireCtapHidChannel, nonce did not match
            // FidoTransform.ReceiveResponse, response data length too long
            // Response.GetData, response data length too long
            ErrorHandler(e, "Exception encountered when trying to get device info over FIDO.");
        }
        catch (UnauthorizedAccessException e)
        {
            ErrorHandler(e, "Must have elevated privileges in Windows to access FIDO device directly.");
        }

        Log.LogWarning("Failed to read firmware version through FIDO.");
        firmwareVersion = null;

        return false;
    }

    private static void ErrorHandler(Exception exception, string message) => Log.LogWarning(exception, message);
}
