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
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Oath;

namespace Yubico.YubiKey.TestUtilities
{
    public class DeviceReset
    {
        /// <summary>
        /// Enables all USB and NFC capabilities
        /// and resets the PIV and OATH applications to the default state
        /// </summary>
        public static IYubiKeyDevice ResetAll(IYubiKeyDevice key)
        {
            key = ResetPiv(key);
            key = ResetOath(key);
            key = EnableAllCapabilities(key);

            return key;
        }

        /// <summary>
        /// Resets the PIV application to the default state.
        /// </summary>
        public static IYubiKeyDevice ResetPiv(IYubiKeyDevice key)
        {
            using (var pivSession = new PivSession(key))
            {
                pivSession.ResetApplication();
            }

            return key;
        }

        /// <summary>
        /// Resets the OATH application to the default state.
        /// </summary>
        public static IYubiKeyDevice ResetOath(IYubiKeyDevice key)
        {
            using (var oathSession = new OathSession(key))
            {
                oathSession.ResetApplication();
            }

            return key;
        }   
        
        /// <summary>
        /// Enables all USB and NFC capabilities.
        /// </summary>
        public static IYubiKeyDevice EnableAllCapabilities(IYubiKeyDevice key)
        {
            var setCommand = new Management.Commands.SetDeviceInfoCommand
            {
                EnabledUsbCapabilities = key.AvailableUsbCapabilities,
                EnabledNfcCapabilities = key.AvailableNfcCapabilities,
                ResetAfterConfig = true,
            };

            IYubiKeyResponse setDeviceInfoResponse = SetDeviceInfo(key, setCommand);

            if (setDeviceInfoResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(setDeviceInfoResponse.StatusMessage);
            }

            return key;
        }

        /// <summary>
        /// Sets which USB features are enabled (and disabled).
        /// </summary>
        private static IYubiKeyDevice SetEnabledUsbCapabilities(
            IYubiKeyDevice key,
            YubiKeyCapabilities yubiKeyCapabilities)
        {
            if ((key.AvailableUsbCapabilities & yubiKeyCapabilities) == YubiKeyCapabilities.None)
            {
                throw new InvalidOperationException(ExceptionMessages.MustEnableOneAvailableUsbCapability);
            }

            var setCommand = new Management.Commands.SetDeviceInfoCommand
            {
                EnabledUsbCapabilities = yubiKeyCapabilities,
                ResetAfterConfig = true,
            };

            IYubiKeyResponse setDeviceInfoResponse = SetDeviceInfo(key,setCommand);

            if (setDeviceInfoResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(setDeviceInfoResponse.StatusMessage);
            }

            return key;
        }

        /// <summary>
        /// Sets which NFC features are enabled (and disabled).
        /// </summary>
        private static IYubiKeyDevice SetEnabledNfcCapabilities(
            IYubiKeyDevice key,
            YubiKeyCapabilities yubiKeyCapabilities)
        {
            var setCommand = new Management.Commands.SetDeviceInfoCommand
            {
                EnabledNfcCapabilities = yubiKeyCapabilities,
                ResetAfterConfig = true,
            };

            IYubiKeyResponse setDeviceInfoResponse = SetDeviceInfo(key, setCommand);

            if (setDeviceInfoResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(setDeviceInfoResponse.StatusMessage);
            }

            return key;
        }

        private static IYubiKeyResponse SetDeviceInfo(
            IYubiKeyDevice key,
            Management.Commands.SetDeviceInfoBaseCommand baseCommand)
        {
            IYubiKeyCommand<IYubiKeyResponse> command;

            if (key.TryConnect(YubiKeyApplication.Management, out IYubiKeyConnection? connection))
            {
                command = new Management.Commands.SetDeviceInfoCommand(baseCommand);
            }
            else if (key.TryConnect(YubiKeyApplication.Otp, out connection))
            {
                command = new Otp.Commands.SetDeviceInfoCommand(baseCommand);
            }
            else
            {
                throw new NotSupportedException(ExceptionMessages.NoInterfaceAvailable);
            }

            using (connection)
            {
                return connection.SendCommand(command);
            }
        }
    }
}
