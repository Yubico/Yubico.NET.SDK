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

using Yubico.Core.Devices.SmartCard;
using Yubico.YubiKey.DeviceExtensions;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Diagnostics;
using Yubico.Core.Logging;

namespace Yubico.YubiKey
{
    internal static class SmartCardDeviceInfoFactory
    {
        public static YubiKeyDeviceInfo GetDeviceInfo(ISmartCardDevice device)
        {
            if (!device.IsYubicoDevice())
            {
                throw new ArgumentException(ExceptionMessages.InvalidDeviceNotYubico, nameof(device));
            }

            if (!TryGetDeviceInfoFromManagement(device, out YubiKeyDeviceInfo? ykDeviceInfo))
            {
                ykDeviceInfo = new YubiKeyDeviceInfo();
            }

            // Manually fill in gaps, if necessary
            var defaultDeviceInfo = new YubiKeyDeviceInfo();

            // Build from OTP
            if (ykDeviceInfo.SerialNumber == defaultDeviceInfo.SerialNumber
                && TryGetSerialNumberFromOtp(device, out int? serialNumber))
            {
                ykDeviceInfo.SerialNumber = serialNumber;
            }

            if (ykDeviceInfo.FirmwareVersion == defaultDeviceInfo.FirmwareVersion
                && TryGetFirmwareVersionFromOtp(device, out FirmwareVersion? firmwareVersion))
            {
                ykDeviceInfo.FirmwareVersion = firmwareVersion;
            }

            // Build from PIV
            if (ykDeviceInfo.SerialNumber == defaultDeviceInfo.SerialNumber
                && TryGetSerialNumberFromPiv(device, out serialNumber))
            {
                ykDeviceInfo.SerialNumber = serialNumber;
            }

            if (ykDeviceInfo.FirmwareVersion == defaultDeviceInfo.FirmwareVersion
                && TryGetFirmwareVersionFromPiv(device, out firmwareVersion))
            {
                ykDeviceInfo.FirmwareVersion = firmwareVersion;
            }

            return ykDeviceInfo;
        }

        private static bool TryGetDeviceInfoFromManagement(ISmartCardDevice device, [MaybeNullWhen(returnValue: false)] out YubiKeyDeviceInfo yubiKeyDeviceInfo)
        {
            try
            {
                using var smartCardConnection = new CcidConnection(device, YubiKeyApplication.Management);

                Management.Commands.GetDeviceInfoResponse response = smartCardConnection.SendCommand(new Management.Commands.GetDeviceInfoCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    yubiKeyDeviceInfo = response.GetData();
                    return true;
                }
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(e, "An ISO 7816 application has encountered an error when trying to get device info from management.");
            }

            yubiKeyDeviceInfo = null;
            return false;
        }

        private static bool TryGetFirmwareVersionFromOtp(ISmartCardDevice device,
            [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
        {
            try
            {
                using var ccidConnection = new CcidConnection(device, YubiKeyApplication.Otp);

                Otp.Commands.ReadStatusResponse response = ccidConnection.SendCommand(new Otp.Commands.ReadStatusCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    firmwareVersion = response.GetData().FirmwareVersion;
                    return true;
                }
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(e, "An ISO 7816 application has encountered an error when trying to get firmware version from OTP.");
            }
            catch (MalformedYubiKeyResponseException e)
            {
                ErrorHandler(e, "The length of GetSerialNumberResponse.GetData reponse data is invalid.");
            }

            firmwareVersion = null;
            return false;
        }

        private static bool TryGetFirmwareVersionFromPiv(ISmartCardDevice device,
            [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
        {
            try
            {
                using IYubiKeyConnection connection = new CcidConnection(device, YubiKeyApplication.Piv);

                Piv.Commands.VersionResponse response = connection.SendCommand(new Piv.Commands.VersionCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    firmwareVersion = response.GetData();
                    return true;
                }
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(e, "An ISO 7816 application has encountered an error when trying to get firmware version from PIV.");
            }

            firmwareVersion = null;
            return false;
        }

        private static bool TryGetSerialNumberFromOtp(ISmartCardDevice device, out int? serialNumber)
        {
            try
            {
                using var ccidConnection = new CcidConnection(device, YubiKeyApplication.Otp);

                Otp.Commands.GetSerialNumberResponse response = ccidConnection.SendCommand(new Otp.Commands.GetSerialNumberCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    serialNumber = response.GetData();
                    return true;
                }
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(e, "An ISO 7816 application has encountered an error when trying to get serial number from OTP.");
            }
            catch (MalformedYubiKeyResponseException e)
            {
                // GetSerialNumberResponse.GetData, response data length too short
                ErrorHandler(e, "The GetSerialNumberResponse.GetData response data length is too short.");
            }

            serialNumber = null;
            return false;
        }

        private static bool TryGetSerialNumberFromPiv(ISmartCardDevice device, out int? serialNumber)
        {
            try
            {
                using IYubiKeyConnection connection = new CcidConnection(device, YubiKeyApplication.Piv);

                Piv.Commands.GetSerialNumberResponse response = connection.SendCommand(new Piv.Commands.GetSerialNumberCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    serialNumber = response.GetData();
                    return true;
                }
            }
            catch (Core.Iso7816.ApduException e)
            {
                ErrorHandler(e, "An ISO 7816 application has encountered an error when trying to get serial number from PIV.");
            }

            serialNumber = null;
            return false;
        }

        private static void ErrorHandler(Exception exception, string message)
            => Log.GetLogger().LogWarning(exception, message);
    }
}
