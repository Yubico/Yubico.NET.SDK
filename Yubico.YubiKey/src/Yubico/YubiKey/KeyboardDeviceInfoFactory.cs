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
using Yubico.Core.Devices.Hid;
using Yubico.Core.Logging;
using Yubico.YubiKey.DeviceExtensions;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey
{
    internal static class KeyboardDeviceInfoFactory
    {
        public static YubiKeyDeviceInfo GetDeviceInfo(IHidDevice device)
        {
            Logger log = Log.GetLogger();

            if (!device.IsYubicoDevice())
            {
                throw new ArgumentException(ExceptionMessages.InvalidDeviceNotYubico, nameof(device));
            }

            log.LogInformation("Getting device info for keyboard device {Device}.", device);

            if (!device.IsKeyboard())
            {
                throw new ArgumentException(ExceptionMessages.InvalidDeviceNotKeyboard, nameof(device));
            }

            if (!TryGetDeviceInfoFromKeyboard(device, out YubiKeyDeviceInfo? ykDeviceInfo))
            {
                ykDeviceInfo = new YubiKeyDeviceInfo();
            }

            // Manually fill in gaps, if necessary
            var defaultDeviceInfo = new YubiKeyDeviceInfo();

            if (ykDeviceInfo.SerialNumber == defaultDeviceInfo.SerialNumber
                && TryGetSerialNumberFromKeyboard(device, out int? serialNumber))
            {
                ykDeviceInfo.SerialNumber = serialNumber;
            }

            if (ykDeviceInfo.FirmwareVersion == defaultDeviceInfo.FirmwareVersion
                && TryGetFirmwareVersionFromKeyboard(device, out FirmwareVersion? firmwareVersion))
            {
                ykDeviceInfo.FirmwareVersion = firmwareVersion;
            }

            if (ykDeviceInfo.FirmwareVersion < FirmwareVersion.V4_0_0 && ykDeviceInfo.AvailableUsbCapabilities == YubiKeyCapabilities.None)
            {
                ykDeviceInfo.AvailableUsbCapabilities = YubiKeyCapabilities.Otp;
            }

            return ykDeviceInfo;
        }

        private static bool TryGetDeviceInfoFromKeyboard(IHidDevice device, [MaybeNullWhen(returnValue: false)] out YubiKeyDeviceInfo yubiKeyDeviceInfo)
        {
            Logger log = Log.GetLogger();

            try
            {
                log.LogInformation("Attempting to read device info via the management command over the keyboard interface.");
                using var connection = new KeyboardConnection(device);

                yubiKeyDeviceInfo = DeviceInfoHelper.GetDeviceInfo<GetPagedDeviceInfoCommand>(connection);
                if (yubiKeyDeviceInfo is {})
                {
                    log.LogInformation("Successfully read device info via the keyboard management command.");
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
            
            log.LogWarning("Failed to read device info through the keyboard management command. This may be expected for older YubiKeys.");
            yubiKeyDeviceInfo = null;
            return false;
        }

        private static bool TryGetSerialNumberFromKeyboard(IHidDevice device, out int? serialNumber)
        {
            Logger log = Log.GetLogger();

            try
            {
                log.LogInformation("Attempting to read serial number through the keybaord interface.");
                using var keyboardConnection = new KeyboardConnection(device);

                Otp.Commands.GetSerialNumberResponse response = keyboardConnection.SendCommand(new Otp.Commands.GetSerialNumberCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    serialNumber = response.GetData();
                    log.LogInformation("Serial number: {Serial}", serialNumber);
                    return true;
                }
                log.LogError("Reading serial number via the keyboard interface failed with: {Error} {Message}", response.StatusWord, response.StatusMessage);
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

            log.LogWarning("Failed to read serial through the keyboard interface.");
            serialNumber = null;
            return false;
        }

        private static bool TryGetFirmwareVersionFromKeyboard(IHidDevice device, [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
        {
            Logger log = Log.GetLogger();

            try
            {
                log.LogInformation("Attempting to read firmware version through the keyboard interface.");
                using var keyboardConnection = new KeyboardConnection(device);

                Otp.Commands.ReadStatusResponse response = keyboardConnection.SendCommand(new Otp.Commands.ReadStatusCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    firmwareVersion = response.GetData().FirmwareVersion;
                    log.LogInformation("Firmware version: {Version}", firmwareVersion.ToString());
                    return true;
                }

                log.LogError("Reading firmware version via keyboard failed with: {Error} {Message}", response.StatusWord, response.StatusMessage);
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

            log.LogWarning("Failed to read firmware version through the keyboard interface.");
            firmwareVersion = null;
            return false;
        }

        private static void ErrorHandler(Exception exception, string message)
            => Log.GetLogger().LogWarning(exception, message);
    }
}
