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
using Yubico.Core.Logging;

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

        private static readonly ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private readonly Logger _log = Log.GetLogger();
        private readonly Dictionary<IYubiKeyDevice, bool> _internalCache = new Dictionary<IYubiKeyDevice, bool>();
        private readonly HidDeviceListener _hidListener = HidDeviceListener.Create();
        private readonly SmartCardDeviceListener _smartCardListener = SmartCardDeviceListener.Create();

        private readonly Thread? _listenerThread;
        private bool _isListening;

        private YubiKeyDeviceListener()
        {
            _log.LogInformation("Creating YubiKeyDeviceListener instance.");

            _listenerThread = new Thread(ListenForChanges) { IsBackground = true };
            _isListening = true;

            _log.LogInformation("Performing initial cache population.");
            Update();

            _listenerThread.Start();
        }

        internal List<IYubiKeyDevice> GetAll() => _internalCache.Keys.ToList();

        private void ListenForChanges()
        {
            using var updateEvent = new ManualResetEvent(false);

            _log.LogInformation("YubiKey device listener thread started. ThreadID is {ThreadID}.", Environment.CurrentManagedThreadId);

            _smartCardListener.Arrived += (s, e) =>
            {
                _log.LogInformation("Arrival of smart card {SmartCard} is triggering update.", e.Device);
                _ = updateEvent.Set();
            };

            _smartCardListener.Removed += (s, e) =>
            {
                _log.LogInformation("Removal of smart card {SmartCard} is triggering update.", e.Device);
                _ = updateEvent.Set();
            };

            _hidListener.Arrived += (s, e) =>
            {
                _log.LogInformation("Arrival of HID {HidDevice} is triggering update.", e.Device);
                _ = updateEvent.Set();
            };

            _hidListener.Removed += (s, e) =>
            {
                _log.LogInformation("Removal of HID {HidDevice} is triggering update.", e.Device);
                _ = updateEvent.Set();
            };

            while (_isListening)
            {
                _ = updateEvent.WaitOne();
                Thread.Sleep(200); // I really dislike sleeps, but here, it does seem like a good idea to give the
                                   // system some time to quiet down in terms of PnP activity.
                _ = updateEvent.Reset();
                Update();
            }

            // KeepAlive seems to be necessary here as the collector doesn't know that it shouldn't dispose the
            // event until the very end of this function/thread.
            GC.KeepAlive(updateEvent);
        }

        private void Update()
        {
            RwLock.EnterWriteLock();
            _log.LogInformation("Entering write-lock.");

            _log.LogInformation("Cache currently aware of {Count} YubiKeys.", _internalCache.Count);
            List<IYubiKeyDevice> addedDevices = new List<IYubiKeyDevice>();
            List<IYubiKeyDevice> removedDevices = GetAll();

            IEnumerable<IDevice> hidDevices = YubiKeyDevice.GetFilteredHidDevices(Transport.All);
            IEnumerable<IDevice> smartCardDevices = YubiKeyDevice.GetFilteredSmartCardDevices(Transport.All);
            _log.LogInformation(
                "Found {HidCount} HID devices and {SCardCount} Smart Card devices for processing.",
                hidDevices.Count(),
                smartCardDevices.Count());

            IEnumerable<IDevice> allDevices = smartCardDevices.Union(hidDevices);

            foreach (IYubiKeyDevice yubiKey in GetAll())
            {
                _internalCache[yubiKey] = false;
            }

            foreach (IDevice device in allDevices)
            {
                _log.LogInformation("Processing device {Device}.", device);

                IYubiKeyDevice? newYubiKey = _internalCache.Keys.FirstOrDefault(d => d.Contains(device));
                _log.LogInformation("Device was " + (newYubiKey is null ? "not " : "") + "found in the cache.");

                if (newYubiKey != null)
                {
                    _ = removedDevices.Remove(newYubiKey);
                    continue;
                }

                var yubiKeyWithInfo = new YubiKeyDevice.YubicoDeviceWithInfo(device);
                int? serialNumber = yubiKeyWithInfo.Info.SerialNumber;
                _log.LogInformation("Device {Device} is YubiKey with serial number {SerialNumber}.", device, serialNumber);

                YubiKeyDevice? existingYubiKey =
                    _internalCache.Keys.FirstOrDefault(d => d.SerialNumber == serialNumber) as YubiKeyDevice;
                _log.LogInformation(
                    "YubiKey [{SerialNumber}] was " + (existingYubiKey is null ? "not " : "") + "found in the cache.",
                    serialNumber);

                if (existingYubiKey is null)
                {
                    var yubiKey = new YubiKeyDevice(yubiKeyWithInfo.Device, yubiKeyWithInfo.Info);
                    _internalCache[yubiKey] = true;
                }
                else
                {
                    _ = YubiKeyDevice.TryMergeYubiKey(existingYubiKey, yubiKeyWithInfo);
                }
            }

            foreach (IYubiKeyDevice removedDevice in removedDevices)
            {
                _ = _internalCache.Remove(removedDevice);
            }

            foreach (IYubiKeyDevice yubiKeyDevice in GetAll())
            {
                if (_internalCache.ContainsKey(yubiKeyDevice))
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

            RwLock.ExitWriteLock();
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
                    RwLock.Dispose();
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
