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
using System.Threading;
using Yubico.YubiKey.Management.Commands;
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.YubiHsmAuth;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    ///     These methods help get the YubiKey under test into a known state
    ///     by enabling capabilities and resetting applications.
    /// </summary>
    public class DeviceReset
    {
        /// <summary>
        ///     Enables all USB and NFC capabilities
        ///     and resets the PIV, OATH, and YubiHSM Auth applications
        ///     to the default state
        /// </summary>
        public static IYubiKeyDevice ResetAll(IYubiKeyDevice key)
        {
            key = EnableAllCapabilities(key);
            key = ResetPiv(key);
            key = ResetOath(key);
            key = ResetYubiHsmAuth(key);

            return key;
        }

        /// <summary>
        ///     Resets the PIV application to the default state.
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
        ///     Resets the OATH application to the default state.
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
        ///     Resets the YubiHSM Auth application to the default state.
        /// </summary>
        public static IYubiKeyDevice ResetYubiHsmAuth(IYubiKeyDevice key)
        {
            using (var yubiHsmAuthSession = new YubiHsmAuthSession(key))
            {
                yubiHsmAuthSession.ResetApplication();
            }

            return key;
        }

        /// <summary>
        ///     Enables all USB and NFC capabilities.
        /// </summary>
        /// <remarks>
        ///     The <paramref name="key" /> must have a serial number. The serial
        ///     number is what is used to find the YubiKey after it resets. The
        ///     "re-found" <c>IYubiKeyDevice</c> is returned by this method.
        /// </remarks>
        public static IYubiKeyDevice EnableAllCapabilities(IYubiKeyDevice key)
        {
            var serialNumber =
                key.SerialNumber ?? throw new InvalidOperationException("Serial number required");

            var setCommand = new SetDeviceInfoCommand
            {
                EnabledUsbCapabilities = key.AvailableUsbCapabilities,
                EnabledNfcCapabilities = key.AvailableNfcCapabilities,
                ResetAfterConfig = true
            };

            var setDeviceInfoResponse = SetDeviceInfo(key, setCommand);

            if (setDeviceInfoResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(setDeviceInfoResponse.StatusMessage);
            }

            // Get the new device handle once the YubiKey reconnects
            Thread.Sleep(millisecondsTimeout: 1000);
            key = TestDeviceSelection.RenewDeviceEnumeration(serialNumber);

            return key;
        }

        /// <summary>
        ///     Sets which USB features are enabled (and disabled).
        /// </summary>
        private static IYubiKeyDevice SetEnabledUsbCapabilities(
            IYubiKeyDevice key,
            YubiKeyCapabilities yubiKeyCapabilities)
        {
            if ((key.AvailableUsbCapabilities & yubiKeyCapabilities) == YubiKeyCapabilities.None)
            {
                throw new InvalidOperationException(ExceptionMessages.MustEnableOneAvailableUsbCapability);
            }

            var setCommand = new SetDeviceInfoCommand
            {
                EnabledUsbCapabilities = yubiKeyCapabilities,
                ResetAfterConfig = true
            };

            var setDeviceInfoResponse = SetDeviceInfo(key, setCommand);

            if (setDeviceInfoResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(setDeviceInfoResponse.StatusMessage);
            }

            return key;
        }

        /// <summary>
        ///     Sets which NFC features are enabled (and disabled).
        /// </summary>
        private static IYubiKeyDevice SetEnabledNfcCapabilities(
            IYubiKeyDevice key,
            YubiKeyCapabilities yubiKeyCapabilities)
        {
            var setCommand = new SetDeviceInfoCommand
            {
                EnabledNfcCapabilities = yubiKeyCapabilities,
                ResetAfterConfig = true
            };

            var setDeviceInfoResponse = SetDeviceInfo(key, setCommand);

            if (setDeviceInfoResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(setDeviceInfoResponse.StatusMessage);
            }

            return key;
        }

        private static IYubiKeyResponse SetDeviceInfo(
            IYubiKeyDevice key,
            SetDeviceInfoBaseCommand baseCommand)
        {
            IYubiKeyCommand<IYubiKeyResponse> command;

            if (key.TryConnect(YubiKeyApplication.Management, out var connection))
            {
                command = new SetDeviceInfoCommand(baseCommand);
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
