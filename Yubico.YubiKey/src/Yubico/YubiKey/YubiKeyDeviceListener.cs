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
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Yubico.Core.Devices;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;
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
        public static YubiKeyDeviceListener Instance => _lazyInstance ??= new YubiKeyDeviceListener();

        /// <summary>
        /// Disposes and closes the singleton instance of <see cref="YubiKeyDeviceListener"/>.
        /// </summary>
        public static void ResetInstance()
        {
            if (_lazyInstance != null)
            {
                _lazyInstance.Dispose();
                _lazyInstance = null;
            }
        }

        private static YubiKeyDeviceListener? _lazyInstance;

        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private readonly Logger _log = Log.GetLogger<YubiKeyDeviceListener>();
        private readonly Dictionary<IYubiKeyDevice, bool> _internalCache = new Dictionary<IYubiKeyDevice, bool>();
        private readonly HidDeviceListener _hidListener = HidDeviceListener.Create();
        private readonly SmartCardDeviceListener _smartCardListener = SmartCardDeviceListener.Create();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Task _listenTask;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private CancellationToken CancellationToken => _tokenSource.Token;

        private bool _isListening;

        private YubiKeyDeviceListener()
        {
            _log.LogInformation("Creating YubiKeyDeviceListener instance.");

            SetupDeviceListeners();

            _log.LogInformation("Performing initial cache population.");
            Update();

            _listenTask = ListenForChanges();
        }

        internal List<IYubiKeyDevice> GetAll() => _internalCache.Keys.ToList();

        private void ListenerHandler(object? sender, EventArgs e) => _semaphore.Release();

        private void SetupDeviceListeners()
        {
            _smartCardListener.Arrived += ListenerHandler;
            _smartCardListener.Removed += ListenerHandler;
            _hidListener.Arrived += ListenerHandler;
            _hidListener.Removed += ListenerHandler;
        }

        private async Task ListenForChanges()
        {
            _isListening = true;
            while (_isListening)
            {
                try
                {
                    await _semaphore.WaitAsync(CancellationToken).ConfigureAwait(false);
                    // Give events a chance to coalesce.
                    await Task.Delay(200, CancellationToken).ConfigureAwait(false);
                    Update();
                    // Reset any outstanding events.
                    _ = await _semaphore.WaitAsync(0, CancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void Update()
        {
            _rwLock.EnterWriteLock();
            _log.LogInformation("Entering write-lock.");

            ResetCacheMarkers();

            var devicesToProcess = GetDevices();

            _log.LogInformation("Cache currently aware of {Count} YubiKeys.", _internalCache.Count);

            var addedYubiKeys = new List<IYubiKeyDevice>();

            foreach (var device in devicesToProcess)
            {
                _log.LogInformation("Processing device {Device}", device);

                // First check if we've already seen this device (very fast)
                var existingEntry = _internalCache.Keys.FirstOrDefault(k => k.Contains(device));
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
                YubiKeyDevice.YubicoDeviceWithInfo deviceWithInfo;

                // This sort of call can fail for a number of reasons. Probably the most common will be when some other
                // application is using one of the device interfaces exclusively - GPG is an example of this. It tends
                // to take the smart card reader USB interface and not let go of it. So, for those of us that use GPG
                // with YubiKeys for commit signing, the SDK is unlikely to be able to connect. There's not much we can
                // do about that other than skip, and log a message that this has happened.
                try
                {
                    deviceWithInfo = new YubiKeyDevice.YubicoDeviceWithInfo(device);
                }
                catch (Exception ex) when (ex is SCardException || ex is PlatformApiException)
                {
                    _log.LogError("Encountered a YubiKey but was unable to connect to it. This interface will be ignored.");

                    continue;
                }

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
                .Select(e => e.Key)
                .ToList();

            foreach (var removedKey in removedYubiKeys)
            {
                OnDeviceRemoved(new YubiKeyDeviceEventArgs(removedKey));
                _ = _internalCache.Remove(removedKey);
            }

            foreach (var addedKey in addedYubiKeys)
            {
                OnDeviceArrived(new YubiKeyDeviceEventArgs(addedKey));
            }

            _log.LogInformation("Exiting write-lock.");
            _rwLock.ExitWriteLock();
        }

        private List<IDevice> GetDevices()
        {
            var devicesToProcess = new List<IDevice>();

            IList<IDevice> hidKeyboardDevices = GetHidKeyboardDevices().ToList();
            IList<IDevice> smartCardDevices = GetSmartCardDevices().ToList();
            IList<IDevice> hidFidoDevices = new List<IDevice>();

            if (SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows && !SdkPlatformInfo.IsElevated)
            {
                _log.LogWarning(
                    "SDK running in an un-elevated Windows process. " +
                    "Skipping FIDO enumeration as this requires process elevation.");
            }
            else
            {
                hidFidoDevices = GetHidFidoDevices().ToList();
            }

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
            foreach (var cacheDevice in _internalCache.Keys.ToList())
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
            Log
                .GetLogger(typeof(YubiKeyDeviceListener).FullName!)
                .LogWarning($"Exception caught: {exception}");

        private bool _isDisposed;

        /// <summary>
        /// Disposes the objects.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Signal the listen thread that it's time to end.
                    _tokenSource.Cancel();

                    // Shut down the listener handlers.
                    _hidListener.Arrived -= ListenerHandler;
                    _hidListener.Removed -= ListenerHandler;
                    if (_hidListener is IDisposable hidDisp)
                    {
                        hidDisp.Dispose();
                    }

                    _smartCardListener.Arrived -= ListenerHandler;
                    _smartCardListener.Removed -= ListenerHandler;
                    if (_smartCardListener is IDisposable scDisp)
                    {
                        scDisp.Dispose();
                    }

                    // Give the listen thread a moment (likely is already done).
                    _ = !_listenTask.Wait(100);
                    _listenTask.Dispose();

                    // Get rid of synchronization objects.
                    _rwLock.Dispose();
                    _semaphore.Dispose();
                    _tokenSource.Dispose();
                }
                _isDisposed = true;
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
    }
}
