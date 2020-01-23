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

namespace Yubico.YubiKey
{
    internal static class FidoDeviceInfoFactory
    {
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

            if (!TryGetDeviceInfoFromFido(device, out YubiKeyDeviceInfo? ykDeviceInfo))
            {
                ykDeviceInfo = new YubiKeyDeviceInfo();
            }

            // Manually fill in gaps, if necessary
            var defaultDeviceInfo = new YubiKeyDeviceInfo();

            if (ykDeviceInfo.FirmwareVersion == defaultDeviceInfo.FirmwareVersion
                && TryGetFirmwareVersionFromFido(device, out FirmwareVersion? firmwareVersion))
            {
                ykDeviceInfo.FirmwareVersion = firmwareVersion;
            }

            return ykDeviceInfo;
        }

        private static bool TryGetDeviceInfoFromFido(IHidDevice device, [MaybeNullWhen(returnValue: false)] out YubiKeyDeviceInfo yubiKeyDeviceInfo)
        {
            try
            {
                using var FidoConnection = new FidoConnection(device);

                U2f.Commands.GetDeviceInfoResponse response = FidoConnection.SendCommand(new U2f.Commands.GetDeviceInfoCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    yubiKeyDeviceInfo = response.GetData();
                    return true;
                }
            }
            catch (NotImplementedException e)
            {
                // MacOSHidDevice.ConnectToIOReports(), not implemented on MacOS
                ErrorHandler(e);
            }
            catch (MalformedYubiKeyResponseException e)
            {
                // FidoTransform.AcquireCtapHidChannel, nonce did not match
                // FidoTransform.ReceiveResponse, response data length too long
                // Response.GetData, response data length too long
                ErrorHandler(e);
            }
            catch (UnauthorizedAccessException e)
            {
                // Must have elevated privileges in Windows to access FIDO device directly
                ErrorHandler(e);
            }

            yubiKeyDeviceInfo = null;
            return false;
        }

        private static bool TryGetFirmwareVersionFromFido(IHidDevice device, [MaybeNullWhen(returnValue: false)] out FirmwareVersion firmwareVersion)
        {
            try
            {
                using var FidoConnection = new FidoConnection(device);

                Fido2.Commands.VersionResponse response = FidoConnection.SendCommand(new Fido2.Commands.VersionCommand());

                if (response.Status == ResponseStatus.Success)
                {
                    firmwareVersion = response.GetData();
                    return true;
                }
            }
            catch (NotImplementedException e)
            {
                // MacOSHidDevice.ConnectToIOReports(), not implemented on MacOS
                ErrorHandler(e);
            }
            catch (MalformedYubiKeyResponseException e)
            {
                // FidoTransform.AcquireCtapHidChannel, nonce did not match
                // FidoTransform.ReceiveResponse, response data length too long
                // Response.GetData, response data invalid length
                ErrorHandler(e);
            }
            catch (UnauthorizedAccessException e)
            {
                // Must have elevated privileges in Windows to access FIDO device directly
                ErrorHandler(e);
            }

            firmwareVersion = null;
            return false;
        }

        private static void ErrorHandler(Exception exception) => Debug.WriteLine($"Exception caught: {exception}\n");
    }
}
