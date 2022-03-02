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

using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.DeviceExtensions;
using System.Diagnostics;
using System;
using System.Diagnostics.CodeAnalysis;
using Yubico.Core.Logging;

namespace Yubico.YubiKey
{
    internal static class KeyboardDeviceInfoFactory
    {
        public static YubiKeyDeviceInfo GetDeviceInfo(IHidDevice device)
        {
            if (!device.IsYubicoDevice())
            {
                throw new ArgumentException(ExceptionMessages.InvalidDeviceNotYubico, nameof(device));
            }

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

            return ykDeviceInfo;
        }

        private static bool TryGetDeviceInfoFromKeyboard(IHidDevice device, [MaybeNullWhen(returnValue: false)] out YubiKeyDeviceInfo yubiKeyDeviceInfo)
        {
            try
            {
                using var KeyboardConnection = new KeyboardConnection(device);

                Otp.Commands.GetDeviceInfoResponse response = KeyboardConnection.SendCommand(new Otp.Commands.GetDeviceInfoCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    yubiKeyDeviceInfo = response.GetData();
                    return true;
                }
            }
            catch (NotImplementedException e)
            {
                ErrorHandler(e, "The MacOSHidDevice.ConnectToFeatureReport was not implemented on MacOS.");
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

            yubiKeyDeviceInfo = null;
            return false;
        }

        private static bool TryGetSerialNumberFromKeyboard(IHidDevice device, out int? serialNumber)
        {
            try
            {
                using var KeyboardConnection = new KeyboardConnection(device);

                Otp.Commands.GetSerialNumberResponse response = KeyboardConnection.SendCommand(new Otp.Commands.GetSerialNumberCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    serialNumber = response.GetData();
                    return true;
                }
            }
            catch (NotImplementedException e)
            {
                ErrorHandler(e, "MacOSHidDevice.ConnectToFeatureReports was not implemented on MacOS.");
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

            serialNumber = null;
            return false;
        }

        private static bool TryGetFirmwareVersionFromKeyboard(IHidDevice device, [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
        {
            try
            {
                using var KeyboardConnection = new KeyboardConnection(device);

                Otp.Commands.ReadStatusResponse response = KeyboardConnection.SendCommand(new Otp.Commands.ReadStatusCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    firmwareVersion = response.GetData().FirmwareVersion;
                    return true;
                }
            }
            catch (NotImplementedException e)
            {
                ErrorHandler(e, "MacOSHidDevice.ConnectToFeatureReports was not implemented on MacOS.");
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
                ErrorHandler(e, "The length of GetSerialNumberResponse.GetData response data is invalid.");
            }

            firmwareVersion = null;
            return false;
        }

        private static void ErrorHandler(Exception exception, string message)
            => Log.GetLogger().LogWarning(exception, message);
    }
}
