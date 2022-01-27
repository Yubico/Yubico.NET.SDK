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
using System.Linq;
using System.Threading;
using Yubico.Core.Devices;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Devices.SmartCard;

namespace Yubico.YubiKey
{
    /// <summary>
    /// This class provides events for YubiKeyDevice arrival and removal.
    /// </summary>
    public class YubiKeyDeviceListener : IDisposable
    {
        /// <summary>
        /// Subscribe to receive an event whenever a YubiKey is added to the computer.
        /// </summary>
        public event EventHandler<YubiKeyDeviceEventArgs>? Arrived;

        /// <summary>
        /// Subscribe to receive an event whenever a YubiKey is removed from the computer.
        /// </summary>
        public event EventHandler<YubiKeyDeviceEventArgs>? Removed;

        /// <summary>
        /// An instance of a <see cref="YubiKeyDeviceListener"/>.
        /// </summary>
        public static YubiKeyDeviceListener Instance => _lazyInstance.Value;

        private static readonly Lazy<YubiKeyDeviceListener> _lazyInstance =
            new Lazy<YubiKeyDeviceListener>(() => new YubiKeyDeviceListener());

        private static ReaderWriterLockSlim _rw = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private Dictionary<IYubiKeyDevice, bool> internalCache = new Dictionary<IYubiKeyDevice, bool>();

        internal List<IYubiKeyDevice> GetAll() => internalCache.Keys.ToList();

        private YubiKeyDeviceListener()
        {
            var hidDeviceListener = HidDeviceListener.Create();
            var smartCardListener = SmartCardDeviceListener.Create();

            smartCardListener.Arrived += (s, e) => Update();
            smartCardListener.Removed += (s, e) => Update();

            hidDeviceListener.Arrived += (s, e) => Update();
            hidDeviceListener.Removed += (s, e) => Update();

            Update();
        }

        private void Update()
        {
            _rw.EnterWriteLock();

            List<IYubiKeyDevice> addedDevices = new List<IYubiKeyDevice>();
            List<IYubiKeyDevice> removedDevices = GetAll();

            IEnumerable<IDevice>? allDevices = YubiKeyDevice.GetFilteredHidDevices(Transport.All)
                .Union(YubiKeyDevice.GetFilteredSmartCardDevices(Transport.All));

            foreach (IYubiKeyDevice yubiKey in GetAll())
            {
                internalCache[yubiKey] = false;
            }

            foreach (IDevice device in allDevices)
            {
                IYubiKeyDevice? newYubiKey = internalCache.Keys.FirstOrDefault(d => d.Contains(device));

                if (newYubiKey != null)
                {
                    _ = removedDevices.Remove(newYubiKey);
                    continue;
                }

                var yubiKeyWithInfo = new YubiKeyDevice.YubicoDeviceWithInfo(device);
                int? serialNumber = yubiKeyWithInfo.Info.SerialNumber;

                YubiKeyDevice? existingYubiKey =
                    internalCache.Keys.FirstOrDefault(d => d.SerialNumber == serialNumber) as YubiKeyDevice;

                if (existingYubiKey is null)
                {
                    if (YubiKeyDevice.TryBuildYubiKey(
                        new List<YubiKeyDevice.YubicoDeviceWithInfo>() { yubiKeyWithInfo },
                        out YubiKeyDevice? yubiKey))
                    {
                        internalCache[yubiKey] = true;
                    }
                }
                else
                {
                    _ = internalCache.Remove(existingYubiKey);
                    if (YubiKeyDevice.TryMergeYubiKey(existingYubiKey, yubiKeyWithInfo))
                    {
                        internalCache[existingYubiKey] = true;
                    }
                }
            }

            foreach (IYubiKeyDevice removedDevice in removedDevices)
            {
                _ = internalCache.Remove(removedDevice);
            }

            foreach (IYubiKeyDevice yubiKeyDevice in GetAll())
            {
                if (internalCache[yubiKeyDevice])
                {
                    addedDevices.Add(yubiKeyDevice);
                }
            }

            foreach (IYubiKeyDevice removedDevice in removedDevices)
            {
                OnDeviceRemoved(new YubiKeyDeviceEventArgs(removedDevice));
            }

            foreach (IYubiKeyDevice addedDevice in addedDevices)
            {
                OnDeviceArrived(new YubiKeyDeviceEventArgs(addedDevice));
            }

            _rw.ExitWriteLock();
        }

        /// <summary>
        /// Raises event on device arrival.
        /// </summary>
        private void OnDeviceArrived(YubiKeyDeviceEventArgs e) => Arrived?.Invoke(typeof(YubiKeyDevice), e);

        /// <summary>
        /// Raises event on device removal.
        /// </summary>
        private void OnDeviceRemoved(YubiKeyDeviceEventArgs e) => Removed?.Invoke(typeof(YubiKeyDevice), e);

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
                    _rw.Dispose();
                }
                _disposedValue = true;
            }
        }

        ~YubiKeyDeviceListener()
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
