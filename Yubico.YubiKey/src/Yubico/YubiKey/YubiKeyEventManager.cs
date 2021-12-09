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
using System.Linq;
using Yubico.Core.Devices.SmartCard;
using Yubico.PlatformInterop;
using Yubico.YubiKey.DeviceExtensions;

namespace Yubico.YubiKey
{
    /// <summary>
    /// This class provides events for YubiKeyDevice arrival and removal.
    /// </summary>
    internal class YubiKeyEventManager : IDisposable
    {
        private readonly Action<YubiKeyDeviceEventArgs> _arrivalAction;
        private readonly Action<YubiKeyDeviceEventArgs> _removalAction;

        /// <summary>
        /// Listener for smart card related events.
        /// </summary>
        private readonly SCardListener _sCardListener;

        /// <summary>
        /// Constructs a <see cref="YubiKeyEventManager"/>.
        /// </summary>
        public YubiKeyEventManager(Action<YubiKeyDeviceEventArgs> arrivalAction, Action<YubiKeyDeviceEventArgs> removalAction)
        {
            _arrivalAction = arrivalAction;
            _removalAction = removalAction;

            var yubiKeyDevices = YubiKeyDevice.FindAll().ToList(); // todo: avoid enumerating yubikeys that already have a connection. 

            _sCardListener = new SCardListener();

            _sCardListener.CardArrival += (s, e) =>
            {
                ISmartCardDevice newSmartCardDevice = SmartCardDevice.Create(e.ReaderName, e.Atr);

                if (newSmartCardDevice.IsYubicoDevice()
                    && (newSmartCardDevice.IsNfcTransport() || newSmartCardDevice.IsUsbTransport()))
                {
                    if (!yubiKeyDevices.Any(d => d.Contains(newSmartCardDevice)))
                    {
                        YubiKeyDeviceInfo deviceInfo = SmartCardDeviceInfoFactory.GetDeviceInfo(newSmartCardDevice);
                        var device = new YubiKeyDevice(newSmartCardDevice, null, null, deviceInfo);
                        var deviceEventArgs = new YubiKeyDeviceEventArgs(device);
                        OnDeviceArrived(deviceEventArgs);
                        yubiKeyDevices = YubiKeyDevice.FindAll().ToList();
                    }
                }
            };

            _sCardListener.CardRemoval += (s, e) =>
            {
                ISmartCardDevice newSmartCardDevice = SmartCardDevice.Create(e.ReaderName, e.Atr);

                if (ProductAtrs.AllYubiKeys.Contains(e.Atr)
                    && (newSmartCardDevice.IsNfcTransport() || newSmartCardDevice.IsUsbTransport()))
                {
                    IYubiKeyDevice? device = yubiKeyDevices.FirstOrDefault(d => d.Contains(newSmartCardDevice));

                    if (device != null)
                    {
                        var deviceEventArgs = new YubiKeyDeviceEventArgs(device);
                        OnDeviceRemoved(deviceEventArgs);
                        yubiKeyDevices = YubiKeyDevice.FindAll().ToList();
                    }
                }
            };
        }

        /// <summary>
        /// Raises event on device arrival.
        /// </summary>
        private void OnDeviceArrived(YubiKeyDeviceEventArgs e) => _arrivalAction(e);

        /// <summary>
        /// Raises event on device removal.
        /// </summary>
        private void OnDeviceRemoved(YubiKeyDeviceEventArgs e) => _removalAction(e);

        #region IDisposable Support

        private bool _disposedValue;

        /// <summary>
        /// Disposes the objects.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _sCardListener.Dispose();
                }
                _disposedValue = true;
            }
        }

        ~YubiKeyEventManager()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        /// <summary>
        /// Calls Dispose(true).
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
