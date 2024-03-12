// Copyright 2021 Yubico AB
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Logging;
using Yubico.YubiKey.DeviceExtensions;

namespace Yubico.YubiKey
{
    internal static class FidoDeviceInfoFactory
    {
        public static YubiKeyDeviceInfo GetDeviceInfo(IHidDevice device)
        {
            Logger log = Log.GetLogger();

            if (!device.IsYubicoDevice())
            {
                throw new ArgumentException(ExceptionMessages.InvalidDeviceNotYubico, nameof(device));
            }

            if (!device.IsFido())
            {
                throw new ArgumentException(ExceptionMessages.InvalidDeviceNotFido, nameof(device));
            }

            log.LogInformation("Getting device info for FIDO device {Device}", device);

            if (!TryGetDeviceInfoFromFido(device, out YubiKeyDeviceInfo? ykDeviceInfo))
            {
                ykDeviceInfo = new YubiKeyDeviceInfo();
            }

            ykDeviceInfo.IsSkySeries |= device.ProductId == ProductIdentifiers.SecurityKey;

            // Manually fill in gaps, if necessary
            var defaultDeviceInfo = new YubiKeyDeviceInfo();

            if (ykDeviceInfo.FirmwareVersion == defaultDeviceInfo.FirmwareVersion
                && TryGetFirmwareVersionFromFido(device, out FirmwareVersion? firmwareVersion))
            {
                ykDeviceInfo.FirmwareVersion = firmwareVersion;
            }

            if (ykDeviceInfo.FirmwareVersion < FirmwareVersion.V4_0_0 && ykDeviceInfo.AvailableUsbCapabilities == YubiKeyCapabilities.None)
            {
                ykDeviceInfo.AvailableUsbCapabilities = YubiKeyCapabilities.FidoU2f;
            }

            return ykDeviceInfo;
        }

        private static bool TryGetDeviceInfoFromFido(IHidDevice device, [MaybeNullWhen(returnValue: false)] out YubiKeyDeviceInfo yubiKeyDeviceInfo)
        {
            Logger log = Log.GetLogger();

            try
            {
                log.LogInformation("Attempting to read device info via the FIDO interface management command.");
                using var FidoConnection = new FidoConnection(device);

                U2f.Commands.GetDeviceInfoResponse response = FidoConnection.SendCommand(new U2f.Commands.GetDeviceInfoCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    yubiKeyDeviceInfo = response.GetData();
                    log.LogInformation("Successfully read device info via FIDO interface management command.");
                    return true;
                }

                log.LogError("Failed to get device info from management application: {Error} {Message}", response.StatusWord, response.StatusMessage);
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

            log.LogWarning("Failed to read device info through the management interface. This may be expected for older YubiKeys.");
            yubiKeyDeviceInfo = null;
            return false;
        }

        private static bool TryGetFirmwareVersionFromFido(IHidDevice device, [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
        {
            Logger log = Log.GetLogger();

            try
            {
                log.LogInformation("Attempting to read firmware version through FIDO.");
                using var FidoConnection = new FidoConnection(device);

                Fido2.Commands.VersionResponse response = FidoConnection.SendCommand(new Fido2.Commands.VersionCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    firmwareVersion = response.GetData();
                    log.LogInformation("Firmware version: {Version}", firmwareVersion.ToString());
                    return true;
                }
                log.LogError("Reading firmware version via FIDO failed with: {Error} {Message}", response.StatusWord, response.StatusMessage);
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

            log.LogWarning("Failed to read firmware version through FIDO.");
            firmwareVersion = null;
            return false;
        }

        private static void ErrorHandler(Exception exception, string message)
            => Log.GetLogger().LogWarning(exception, message);
    }
}
