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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Yubico.Core.Devices;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;
using Yubico.YubiKey.DeviceExtensions;

namespace Yubico.YubiKey
{
    /// <summary>
    /// Provides device and enumeration capabilities.
    /// </summary>
    public partial class YubiKeyDevice
    {
        /// <summary>
        /// Enumerate all YubiKeys on the system over all available transports.
        /// </summary>
        /// <remarks><inheritdoc cref="FindByTransport(Transport)"/></remarks>
        /// <returns><inheritdoc cref="FindByTransport(Transport)"/></returns>
        public static IEnumerable<IYubiKeyDevice> FindAll() => FindByTransport(Transport.All);

        /// <summary>
        /// Enumerate YubiKeys over the given transports.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method will exclude any connection (SmartCard, HidFido, HidKeyboard) that did
        /// not successfully respond to a request for its Firmware Version.
        /// This means that there may be fewer IYubiKeys returned than expected,
        /// or that some IYubiKeys are missing an expected connection.
        /// </para>
        /// <para>
        /// To the host device, a single YubiKey can appear as multiple devices. This method
        /// will attempt to match these devices back together into a single
        /// <see cref="IYubiKeyDevice"/> using their serial number. If they cannot be matched,
        /// each connection will be returned as a separate <see cref="IYubiKeyDevice"/>.
        /// </para>
        /// </remarks>
        /// <param name="transport">
        /// Argument controls which devices are searched for. Values <see cref="Transport.None"/>
        /// will result in exceptions being thrown. <see cref="FindAll"/> is a
        /// convenience function to find <see cref="Transport.All"/>.
        /// </param>
        /// <returns>
        /// A collection of YubiKeys that were found, as <see cref="IYubiKeyDevice"/>s.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="transport"/> is <see cref="Transport.None"/>.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when attempting to find YubiKeys for the transport
        /// <c>HidFido</c> on Windows, and the application is not running in an
        /// elevated state (e.g. "Run as administrator").
        /// </exception>
        public static IEnumerable<IYubiKeyDevice> FindByTransport(Transport transport = Transport.All)
        {
            Logger log = Log.GetLogger();

            log.LogInformation("FindByTransport {Transport}", transport);

            if (transport == Transport.None)
            {
                throw new ArgumentException(ExceptionMessages.InvalidConnectionTypeNone, nameof(transport));
            }

            // If the caller is looking only for HidFido, and this is Windows,
            // and the process is not running elevated, we can't use the YubiKey,
            // so throw an exception.
            if (transport == Transport.HidFido &&
                SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows &&
                !SdkPlatformInfo.IsElevated)
            {
                throw new UnauthorizedAccessException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.HidFidoWindowsNotElevated));
            }

            // Return any key that has at least one overlapping available transport with the requested transports.
            return YubiKeyDeviceListener
                .Instance
                .GetAll()
                .Where(k => (k.AvailableTransports & transport) != 0);
        }

        /// <summary>
        /// Get info on a specific YubiKey by serial number.
        /// </summary>
        /// <remarks>
        /// This method will only be successful if the YubiKey was programmed with the flag
        /// <c>SerialNumberUsbVisible</c>.
        /// </remarks>
        /// <param name="serialNumber">Integer representation of the YubiKey serial number.</param>
        /// <param name="yubiKey">Out parameter that returns an <see cref="IYubiKeyDevice"/> instance.</param>
        /// <returns>A bool indicating whether the YubiKey was found.</returns>
        public static bool TryGetYubiKey(int serialNumber, out IYubiKeyDevice yubiKey)
        {
            yubiKey = FindAll().FirstOrDefault(k => k.SerialNumber == serialNumber);
            return yubiKey != null;
        }

        internal class YubicoDeviceWithInfo
        {
            /// <summary>
            /// Device information synthesized from various commands.
            /// </summary>
            public YubiKeyDeviceInfo Info { get; }
            public IDevice Device { get; }

            // Assumes that `device` is a YubiKey
            public YubicoDeviceWithInfo(IDevice device)
            {
                Device = device;
                Info = GetDeviceInfo();
            }

            public override bool Equals(object? obj)
            {
                // Check for null and compare run-time types
                if (obj == null || !GetType().Equals(obj.GetType()))
                {
                    return false;
                }

                var objDeviceWithInfo = (YubicoDeviceWithInfo)obj;
                int? objSerialNumber = objDeviceWithInfo.Info.SerialNumber;

                int? thisSerialNumber = Info.SerialNumber;
                return thisSerialNumber.HasValue
                    && objSerialNumber.HasValue
                    && thisSerialNumber.Value == objSerialNumber.Value;
            }

            public override int GetHashCode() => Info.SerialNumber.GetHashCode();

            private YubiKeyDeviceInfo GetDeviceInfo() =>
                Device switch
                {
                    ISmartCardDevice scDevice => SmartCardDeviceInfoFactory.GetDeviceInfo(scDevice),
                    IHidDevice keyboardDevice when keyboardDevice.IsKeyboard() =>
                        KeyboardDeviceInfoFactory.GetDeviceInfo(keyboardDevice),
                    IHidDevice fidoDevice when fidoDevice.IsFido() =>
                        FidoDeviceInfoFactory.GetDeviceInfo(fidoDevice),
                    _ => new YubiKeyDeviceInfo(),
                };
        }
    }
}
