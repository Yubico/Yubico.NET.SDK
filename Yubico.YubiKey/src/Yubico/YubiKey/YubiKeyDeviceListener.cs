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
using Yubico.YubiKey.DeviceExtensions;

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
        private readonly bool _isListening;

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

            ResetCacheMarkers();

            List<IDevice> devicesToProcess = GetDevices();

            _log.LogInformation("Cache currently aware of {Count} YubiKeys.", _internalCache.Count);

            var addedYubiKeys = new List<IYubiKeyDevice>();

            foreach (IDevice device in devicesToProcess)
            {
                _log.LogInformation("Processing device {Device}", device);

                // First check if we've already seen this device (very fast)
                IYubiKeyDevice? existingEntry = _internalCache.Keys.FirstOrDefault(k => k.Contains(device));

                if (existingEntry != null)
                {
                    MarkExistingYubiKey(existingEntry);

                    continue;
                }

                // Next, see if the device has any information about its parent, and if we can match that way (fast)
                existingEntry = _internalCache.Keys.FirstOrDefault(k => k.HasSameParentDevice(device));

                if (existingEntry is YubiKeyDevice parentDevice)
                {
                    MergeAndMarkExistingYubiKey(parentDevice, device);

                    continue;
                }

                // Lastly, let's talk to the YubiKey to get its device info and see if we match via serial number (slow)
                var deviceWithInfo = new YubiKeyDevice.YubicoDeviceWithInfo(device);

                if (deviceWithInfo.Info.SerialNumber is null)
                {
                    CreateAndMarkNewYubiKey(deviceWithInfo, addedYubiKeys);

                    continue;
                }

                existingEntry =
                    _internalCache.Keys.FirstOrDefault(k => k.SerialNumber == deviceWithInfo.Info.SerialNumber);

                if (existingEntry is YubiKeyDevice mergeTarget)
                {
                    MergeAndMarkExistingYubiKey(mergeTarget, deviceWithInfo);

                    continue;
                }

                CreateAndMarkNewYubiKey(deviceWithInfo, addedYubiKeys);
            }

            IEnumerable<IYubiKeyDevice> removedYubiKeys = _internalCache
                .Where(e => e.Value == false)
                .Select(e => e.Key);

            foreach (IYubiKeyDevice removedKey in removedYubiKeys)
            {
                OnDeviceRemoved(new YubiKeyDeviceEventArgs(removedKey));
                _ = _internalCache.Remove(removedKey);
            }

            foreach (IYubiKeyDevice addedKey in addedYubiKeys)
            {
                OnDeviceArrived(new YubiKeyDeviceEventArgs(addedKey));
            }

            RwLock.ExitWriteLock();
        }

        private List<IDevice> GetDevices()
        {
            var devicesToProcess = new List<IDevice>();

            IList<IDevice> hidKeyboardDevices = new List<IDevice>();// GetHidKeyboardDevices().ToList();
            IList<IDevice> smartCardDevices = GetSmartCardDevices().ToList();
            IList<IDevice> hidFidoDevices = GetHidFidoDevices().ToList();

            _log.LogInformation(
                "Found {HidCount} HID Keyboard devices, {FidoCount} HID FIDO devices, and {SCardCount} Smart Card devices for processing.",
                hidKeyboardDevices.Count,
                hidFidoDevices.Count,
                smartCardDevices.Count);

            devicesToProcess.AddRange(hidKeyboardDevices);
            devicesToProcess.AddRange(smartCardDevices);
            devicesToProcess.AddRange(hidFidoDevices);

            return devicesToProcess;
        }

        private void ResetCacheMarkers()
        {
            // Copy the list of keys as changing a dictionary's value will invalidate any enumerators (i.e. the loop).
            foreach (IYubiKeyDevice cacheDevice in _internalCache.Keys.ToList())
            {
                _internalCache[cacheDevice] = false;
            }
        }

        private void MergeAndMarkExistingYubiKey(YubiKeyDevice mergeTarget, YubiKeyDevice.YubicoDeviceWithInfo deviceWithInfo)
        {
            _log.LogInformation(
                "Device was not found in the cache, but appears to be YubiKey {Serial}. Merging devices.",
                mergeTarget.SerialNumber);

            mergeTarget.Merge(deviceWithInfo.Device, deviceWithInfo.Info);
            _internalCache[mergeTarget] = true;
        }

        private void MergeAndMarkExistingYubiKey(YubiKeyDevice mergeTarget, IDevice newChildDevice)
        {
            _log.LogInformation(
                "Device was not found in the cache, but appears to share the same composite device as YubiKey {Serial}."
                + " Merging devices.",
                mergeTarget.SerialNumber);

            mergeTarget.Merge(newChildDevice);
            _internalCache[mergeTarget] = true;
        }

        private void MarkExistingYubiKey(IYubiKeyDevice existingEntry)
        {
            _log.LogInformation(
                "Device was found in the cache and appears to be YubiKey {Serial}.",
                existingEntry.SerialNumber);

            _internalCache[existingEntry] = true;
        }

        private void CreateAndMarkNewYubiKey(YubiKeyDevice.YubicoDeviceWithInfo deviceWithInfo, List<IYubiKeyDevice> addedYubiKeys)
        {
            _log.LogInformation(
                "Device appears to be a brand new YubiKey with serial {Serial}",
                deviceWithInfo.Info.SerialNumber
                );

            var newYubiKey = new YubiKeyDevice(deviceWithInfo.Device, deviceWithInfo.Info);
            addedYubiKeys.Add(newYubiKey);
            _internalCache[newYubiKey] = true;
        }

        /// <summary>
        /// Raises event on device arrival.
        /// </summary>
        private void OnDeviceArrived(YubiKeyDeviceEventArgs e) => Arrived?.Invoke(typeof(YubiKeyDevice), e);

        /// <summary>
        /// Raises event on device removal.
        /// </summary>
        private void OnDeviceRemoved(YubiKeyDeviceEventArgs e) => Removed?.Invoke(typeof(YubiKeyDevice), e);

        private static IEnumerable<IDevice> GetHidFidoDevices()
        {
            try
            {
                return HidDevice
                    .GetHidDevices()
                    .Where(d => d.IsYubicoDevice() && d.IsFido());
            }
            catch (PlatformInterop.PlatformApiException e) { ErrorHandler(e); }

            return Enumerable.Empty<IDevice>();
        }

        private static IEnumerable<IDevice> GetHidKeyboardDevices()
        {
            try
            {
                return HidDevice
                    .GetHidDevices()
                    .Where(d => d.IsYubicoDevice() && d.IsKeyboard());
            }
            catch (PlatformInterop.PlatformApiException e) { ErrorHandler(e); }

            return Enumerable.Empty<IDevice>();
        }

        private static IEnumerable<IDevice> GetSmartCardDevices()
        {
            try
            {
                return SmartCardDevice
                        .GetSmartCardDevices()
                        .Where(d => d.IsYubicoDevice());
            }
            catch (PlatformInterop.SCardException e) { ErrorHandler(e); }

            return Enumerable.Empty<IDevice>();
        }

        private static void ErrorHandler(Exception exception) =>
            Log.GetLogger().LogWarning($"Exception caught: {exception}");

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
