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

using System.Collections.Generic;
using System.Linq;
using Yubico.YubiKey.DeviceExtensions;
using Yubico.Core.Devices;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Devices.SmartCard;
using System;
using Yubico.Core.Logging;

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
        /// and <see cref="Transport.HidFido"/> will result in exceptions being thrown.
        /// <see cref="FindAll"/> is a convenience function to find <see cref="Transport.All"/>.
        /// </param>
        /// <returns>
        /// A collection of YubiKeys that were found, as <see cref="IYubiKeyDevice"/>s.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="transport"/> is <see cref="Transport.None"/>.
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// Thrown when <paramref name="transport"/> contains the flag <see cref="Transport.HidFido"/>.
        /// </exception>
        public static IEnumerable<IYubiKeyDevice> FindByTransport(Transport transport = Transport.All)
        {
            if (transport == Transport.None)
            {
                throw new ArgumentException(ExceptionMessages.InvalidConnectionTypeNone, nameof(transport));
            }

            // FIDO enumeration suppressed until we can provide a non-throwing
            // code path for common situations such as UnauthorizedAccessException.
            if (transport.HasFlag(Transport.HidFido))
            {
                throw new NotImplementedException(ExceptionMessages.NotImplementedHidFidoEnumeration);
            }

            return YubiKeyDeviceListener.Instance.GetAll();
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
            yubiKey = FindAll()
                .FirstOrDefault(k => k.SerialNumber == serialNumber);
            return yubiKey != default;
        }

        internal static IEnumerable<IDevice> GetFilteredHidDevices(Transport transport)
        {
            IEnumerable<IDevice> yubicoHidDevices = Enumerable.Empty<IDevice>();

            bool fidoFlag = transport.HasFlag(Transport.HidFido);
            bool keyboardFlag = transport.HasFlag(Transport.HidKeyboard);

            if (fidoFlag || keyboardFlag)
            {
                try
                {
                    yubicoHidDevices = HidDevice.GetHidDevices()
                        .Where(d => d.IsYubicoDevice())
                        .Where(d => (fidoFlag && d.IsFido()) || (keyboardFlag && d.IsKeyboard()));
                }
                catch (PlatformInterop.PlatformApiException e) { ErrorHandler(e); }
                catch (NotImplementedException e) { ErrorHandler(e); }
            }

            return yubicoHidDevices;
        }

        internal static IEnumerable<IDevice> GetFilteredSmartCardDevices(Transport transport)
        {
            IEnumerable<IDevice> yubicoSmartCardDevices = Enumerable.Empty<IDevice>();

            bool usbSmartCardFlag = transport.HasFlag(Transport.UsbSmartCard);
            bool nfcSmartCardFlag = transport.HasFlag(Transport.NfcSmartCard);

            if (usbSmartCardFlag || nfcSmartCardFlag)
            {
                try
                {
                    yubicoSmartCardDevices = SmartCardDevice.GetSmartCardDevices()
                        .Where(d => d.IsYubicoDevice())
                        .Where(d => (usbSmartCardFlag && d.IsUsbTransport())
                            || (nfcSmartCardFlag && d.IsNfcTransport()));
                }
                catch (PlatformInterop.SCardException e) { ErrorHandler(e); }
            }

            return yubicoSmartCardDevices;
        }

        internal static bool TryMergeYubiKey(
            YubiKeyDevice originalDevice,
            YubicoDeviceWithInfo newDevice)
        {
            if (!IsValidYubiKeyDevice(newDevice))
            {
                return false;
            }

            originalDevice.Merge(newDevice.Device, newDevice.Info);

            return true;
        }

        private static bool IsValidYubiKeyDevice(YubicoDeviceWithInfo device) =>
            device.Device is SmartCardDevice ||
            (device.Device is HidDevice hidDevice && (hidDevice.IsKeyboard() || hidDevice.IsFido()));

        private static bool IsValidYubiKeyDeviceGroup(ICollection<YubicoDeviceWithInfo> devices) =>
            devices.Count > 0 && devices.Count <= 3
            && devices.Count(d => d.Device is SmartCardDevice) <= 1
            && devices.Count(d => d.Device is HidDevice hd && hd.IsKeyboard()) <= 1
            && devices.Count(d => d.Device is HidDevice hd && hd.IsFido()) <= 1;

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

            public override bool Equals(object obj)
            {
                // Check for null and compare run-time types
                if ((obj == null) || !GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    int? thisSerialNumber = Info.SerialNumber;

                    var objDeviceWithInfo = (YubicoDeviceWithInfo)obj;
                    int? objSerialNumber = objDeviceWithInfo.Info.SerialNumber;

                    return thisSerialNumber.HasValue
                        && objSerialNumber.HasValue
                        && thisSerialNumber.Value == objSerialNumber.Value;
                }
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

        private static void ErrorHandler(Exception exception) =>
            Log.GetLogger().LogWarning($"Exception caught: {exception}");
    }
}
