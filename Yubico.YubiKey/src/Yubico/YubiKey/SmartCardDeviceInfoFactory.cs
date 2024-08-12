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
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Logging;
using Yubico.YubiKey.DeviceExtensions;
using Yubico.YubiKey.Management.Commands;

namespace Yubico.YubiKey
{
    internal static class SmartCardDeviceInfoFactory
    {
        public static YubiKeyDeviceInfo GetDeviceInfo(
            ISmartCardDevice device)
        {
            ILogger logger = Log.GetLogger(typeof(SmartCardDeviceInfoFactory).FullName!);

            if (!device.IsYubicoDevice())
            {
                throw new ArgumentException(ExceptionMessages.InvalidDeviceNotYubico, nameof(device));
            }

            logger.LogInformation("Getting device info for smart card {Device}.", device);

            if (!TryGetDeviceInfoFromManagement(device, out YubiKeyDeviceInfo? deviceInfo))
            {
                deviceInfo = new YubiKeyDeviceInfo();
            }

            // Manually fill in gaps, if necessary
            var defaultDeviceInfo = new YubiKeyDeviceInfo();

            // Build from OTP
            if (deviceInfo.SerialNumber == defaultDeviceInfo.SerialNumber
                && TryGetSerialNumberFromOtp(device, out int? serialNumber))
            {
                deviceInfo.SerialNumber = serialNumber;
            }

            if (deviceInfo.FirmwareVersion == defaultDeviceInfo.FirmwareVersion
                && TryGetFirmwareVersionFromOtp(device, out FirmwareVersion? firmwareVersion))
            {
                deviceInfo.FirmwareVersion = firmwareVersion;
            }

            // Build from PIV
            if (deviceInfo.SerialNumber == defaultDeviceInfo.SerialNumber
                && TryGetSerialNumberFromPiv(device, out serialNumber))
            {
                deviceInfo.SerialNumber = serialNumber;
            }

            if (deviceInfo.FirmwareVersion == defaultDeviceInfo.FirmwareVersion
                && TryGetFirmwareVersionFromPiv(device, out firmwareVersion))
            {
                deviceInfo.FirmwareVersion = firmwareVersion;
            }

            if (deviceInfo.FirmwareVersion < FirmwareVersion.V4_0_0 &&
                deviceInfo.AvailableUsbCapabilities == YubiKeyCapabilities.None)
            {
                deviceInfo.AvailableUsbCapabilities = YubiKeyCapabilities.Oath |
                    YubiKeyCapabilities.OpenPgp |
                    YubiKeyCapabilities.Piv |
                    YubiKeyCapabilities.Ccid;
            }

            return deviceInfo;
        }

        private static bool TryGetDeviceInfoFromManagement(
            ISmartCardDevice device,
            [MaybeNullWhen(returnValue: false)] out YubiKeyDeviceInfo deviceInfo)
        {
            ILogger logger = Log.GetLogger(typeof(SmartCardDeviceInfoFactory).FullName!);

            try
            {
                logger.LogInformation("Attempting to read device info via the management application.");
                using var connection = new SmartCardConnection(device, YubiKeyApplication.Management);

                deviceInfo = GetDeviceInfoHelper.GetDeviceInfo<GetPagedDeviceInfoCommand>(connection);

                if (deviceInfo is { })
                {
                    logger.LogInformation("Successfully read device info via management application.");

                    return true;
                }
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(
                    e,
                    "An ISO 7816 application has encountered an error when trying to get device info from management.");
            }

            logger.LogWarning(
                "Failed to read device info through the management interface. This may be expected for older YubiKeys.");

            deviceInfo = null;

            return false;
        }

        private static bool TryGetFirmwareVersionFromOtp(
            ISmartCardDevice device,
            [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
        {
            ILogger logger = Log.GetLogger(typeof(SmartCardDeviceInfoFactory).FullName!);

            try
            {
                logger.LogInformation("Attempting to read firmware version through OTP.");
                using var connection = new SmartCardConnection(device, YubiKeyApplication.Otp);

                Otp.Commands.ReadStatusResponse response =
                    connection.SendCommand(new Otp.Commands.ReadStatusCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    firmwareVersion = response.GetData().FirmwareVersion;
                    logger.LogInformation("Firmware version: {Version}", firmwareVersion.ToString());

                    return true;
                }

                logger.LogError(
                    "Reading firmware version via OTP failed with: {Error} {Message}", response.StatusWord,
                    response.StatusMessage);
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(
                    e,
                    "An ISO 7816 application has encountered an error when trying to get firmware version from OTP.");
            }
            catch (MalformedYubiKeyResponseException e)
            {
                ErrorHandler(e, "The length of GetSerialNumberResponse.GetData response data is invalid.");
            }

            logger.LogWarning("Failed to read firmware version through OTP. This may be expected over USB.");
            firmwareVersion = null;

            return false;
        }

        private static bool TryGetFirmwareVersionFromPiv(
            ISmartCardDevice device,
            [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
        {
            ILogger logger = Log.GetLogger(typeof(SmartCardDeviceInfoFactory).FullName!);

            try
            {
                logger.LogInformation("Attempting to read firmware version through the PIV application.");
                using var connection = new SmartCardConnection(device, YubiKeyApplication.Piv);

                Piv.Commands.VersionResponse response = connection.SendCommand(new Piv.Commands.VersionCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    firmwareVersion = response.GetData();
                    logger.LogInformation("Firmware version: {Version}", firmwareVersion.ToString());

                    return true;
                }

                logger.LogError(
                    "Reading firmware version via PIV failed with: {Error} {Message}", response.StatusWord,
                    response.StatusMessage);
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(
                    e,
                    "An ISO 7816 application has encountered an error when trying to get firmware version from PIV.");
            }

            logger.LogWarning("Failed to read firmware version through PIV.");
            firmwareVersion = null;

            return false;
        }

        private static bool TryGetSerialNumberFromOtp(
            ISmartCardDevice device,
            out int? serialNumber)
        {
            ILogger logger = Log.GetLogger(typeof(SmartCardDeviceInfoFactory).FullName!);

            try
            {
                logger.LogInformation("Attempting to read serial number through the OTP application.");
                using var connection = new SmartCardConnection(device, YubiKeyApplication.Otp);

                Otp.Commands.GetSerialNumberResponse response =
                    connection.SendCommand(new Otp.Commands.GetSerialNumberCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    serialNumber = response.GetData();
                    logger.LogInformation("Serial number: {SerialNumber}", serialNumber);

                    return true;
                }

                logger.LogError(
                    "Reading serial number via OTP failed with: {Error} {Message}", response.StatusWord,
                    response.StatusMessage);
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(
                    e,
                    "An ISO 7816 application has encountered an error when trying to get serial number from OTP.");
            }
            catch (MalformedYubiKeyResponseException e)
            {
                // GetSerialNumberResponse.GetData, response data length too short
                ErrorHandler(e, "The GetSerialNumberResponse.GetData response data length is too short.");
            }

            logger.LogWarning("Failed to read serial number through OTP.");
            serialNumber = null;

            return false;
        }

        private static bool TryGetSerialNumberFromPiv(
            ISmartCardDevice device,
            out int? serialNumber)
        {
            ILogger logger = Log.GetLogger(typeof(SmartCardDeviceInfoFactory).FullName!);

            try
            {
                logger.LogInformation("Attempting to read serial number through the PIV application.");
                using var connection = new SmartCardConnection(device, YubiKeyApplication.Piv);

                Piv.Commands.GetSerialNumberResponse response =
                    connection.SendCommand(new Piv.Commands.GetSerialNumberCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    serialNumber = response.GetData();
                    logger.LogInformation("Serial number: {SerialNumber}", serialNumber);

                    return true;
                }

                logger.LogError(
                    "Reading serial number via PIV failed with: {Error} {Message}", response.StatusWord,
                    response.StatusMessage);
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(
                    e,
                    "An ISO 7816 application has encountered an error when trying to get serial number from PIV.");
            }

            logger.LogWarning("Failed to read serial number through PIV.");
            serialNumber = null;

            return false;
        }

        private static void ErrorHandler(
            Exception exception,
            string message) =>
            Log
                .GetLogger(typeof(SmartCardDeviceInfoFactory).FullName!)
                .LogWarning(exception, message);
    }
}
