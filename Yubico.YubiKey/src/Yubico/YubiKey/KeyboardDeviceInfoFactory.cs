﻿// Copyright 2021 Yubico AB
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
using Yubico.Core.Logging;
using Yubico.YubiKey.DeviceExtensions;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey
{
    internal static class KeyboardDeviceInfoFactory
    {
        private static readonly ILogger Logger = Log.GetLogger(typeof(KeyboardDeviceInfoFactory).FullName!);

        public static YubiKeyDeviceInfo GetDeviceInfo(IHidDevice device)
        {
            if (!device.IsYubicoDevice())
            {
                throw new ArgumentException(ExceptionMessages.InvalidDeviceNotYubico, nameof(device));
            }

            Logger.LogInformation("Getting device info for keyboard device {Device}.", device);

            if (!device.IsKeyboard())
            {
                throw new ArgumentException(ExceptionMessages.InvalidDeviceNotKeyboard, nameof(device));
            }

            if (!TryGetDeviceInfoFromKeyboard(device, out var deviceInfo))
            {
                deviceInfo = new YubiKeyDeviceInfo();
            }

            // Manually fill in gaps, if necessary
            var defaultDeviceInfo = new YubiKeyDeviceInfo();

            if (deviceInfo.SerialNumber == defaultDeviceInfo.SerialNumber
                && TryGetSerialNumberFromKeyboard(device, out int? serialNumber))
            {
                deviceInfo.SerialNumber = serialNumber;
            }

            if (deviceInfo.FirmwareVersion == defaultDeviceInfo.FirmwareVersion && 
                TryGetFirmwareVersionFromKeyboard(device, out var firmwareVersion))
            {
                deviceInfo.FirmwareVersion = firmwareVersion;
            }

            if (deviceInfo.FirmwareVersion < FirmwareVersion.V4_0_0 && deviceInfo.AvailableUsbCapabilities == YubiKeyCapabilities.None)
            {
                deviceInfo.AvailableUsbCapabilities = YubiKeyCapabilities.Otp;
            }

            return deviceInfo;
        }

        private static bool TryGetDeviceInfoFromKeyboard(IHidDevice device, [MaybeNullWhen(returnValue: false)] out YubiKeyDeviceInfo yubiKeyDeviceInfo)
        {
            try
            {
                Logger.LogInformation("Attempting to read device info via the management command over the keyboard interface.");
                using var connection = new KeyboardConnection(device);

                yubiKeyDeviceInfo = GetDeviceInfoHelper.GetDeviceInfo<GetPagedDeviceInfoCommand>(connection);
                if (yubiKeyDeviceInfo is { })
                {
                    Logger.LogInformation("Successfully read device info via the keyboard management command.");
                    return true;
                }

            }
            catch (KeyboardConnectionException e)
            {
                // KeyboardTransform.HandleSlotRequestInstruction, unexpected no reply after writing report
                // KeyboardTransform.HandleSlotRequestInstruction, expected to read next report but reached unexpected end of buffer
                // KeyboardTransform.WaitFor, timeout waiting for YubiKey to ack a write to the interface
                // KeyboardTransform.WaitFor, timeout waiting for user interaction
                ErrorHandler(e, "Exception encountered when trying to get device info from keyboard.");
            }
            catch (MalformedYubiKeyResponseException e)
            {
                ErrorHandler(e, "The KeyboardTransform.HandleStatusInstruction has invalid StatusReport format " +
                    "or The GetDeviceInfoResponse.GetData response data length is too long.");
            }

            Logger.LogWarning("Failed to read device info through the keyboard management command. This may be expected for older YubiKeys.");
            yubiKeyDeviceInfo = null;
            return false;
        }

        private static bool TryGetSerialNumberFromKeyboard(IHidDevice device, out int? serialNumber)
        {
            try
            {
                Logger.LogInformation("Attempting to read serial number through the keybaord interface.");
                using var keyboardConnection = new KeyboardConnection(device);

                var response = keyboardConnection.SendCommand(new GetSerialNumberCommand());
                if (response.Status == ResponseStatus.Success)
                {
                    serialNumber = response.GetData();
                    Logger.LogInformation("Serial number: {Serial}", serialNumber);
                    return true;
                }
                Logger.LogError("Reading serial number via the keyboard interface failed with: {Error} {Message}", response.StatusWord, response.StatusMessage);
            }
            catch (KeyboardConnectionException e)
            {
                // KeyboardTransform.HandleStatusInstruction, failed to read keyboard status report
                // KeyboardTransform.HandleSlotRequestInstruction, unexpected no reply after writing report
                // KeyboardTransform.HandleSlotRequestInstruction, expected to read next report but reached unexpected end of buffer
                // KeyboardTransform.WaitFor, timeout waiting for YubiKey to ack a write to the interface
                // KeyboardTransform.WaitFor, timeout waiting for user interaction
                ErrorHandler(e, "Exception encountered when trying to get serial number from keyboard.");
            }
            catch (MalformedYubiKeyResponseException e)
            {
                ErrorHandler(e, "The GetSerialNumberResponse.GetData response data length is too short.");
            }

            Logger.LogWarning("Failed to read serial through the keyboard interface.");
            serialNumber = null;
            return false;
        }

        private static bool TryGetFirmwareVersionFromKeyboard(IHidDevice device, [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
        {
            try
            {
                Logger.LogInformation("Attempting to read firmware version through the keyboard interface.");
                using var keyboardConnection = new KeyboardConnection(device);

                var response = keyboardConnection.SendCommand(new ReadStatusCommand());
                if (response.Status == ResponseStatus.Success)
                {
                    firmwareVersion = response.GetData().FirmwareVersion;
                    Logger.LogInformation("Firmware version: {Version}", firmwareVersion.ToString());
                    return true;
                }

                Logger.LogError("Reading firmware version via keyboard failed with: {Error} {Message}", response.StatusWord, response.StatusMessage);
            }
            catch (KeyboardConnectionException e)
            {
                // KeyboardTransform.HandleStatusInstruction, failed to read keyboard status report
                // KeyboardTransform.HandleSlotRequestInstruction, unexpected no reply after writing report
                // KeyboardTransform.HandleSlotRequestInstruction, expected to read next report but reached unexpected end of buffer
                // KeyboardTransform.WaitFor, timeout waiting for YubiKey to ack a write to the interface
                // KeyboardTransform.WaitFor, timeout waiting for user interaction
                ErrorHandler(e, "Exception encountered when trying to get firmware version from keyboard.");
            }
            catch (MalformedYubiKeyResponseException e)
            {
                ErrorHandler(e, "The length of ReadStatusCommand.GetData response data is invalid.");
            }

            Logger.LogWarning("Failed to read firmware version through the keyboard interface.");
            firmwareVersion = null;
            return false;
        }

        private static void ErrorHandler(Exception exception, string message)
            => Logger.LogWarning(exception, message);
    }
}
