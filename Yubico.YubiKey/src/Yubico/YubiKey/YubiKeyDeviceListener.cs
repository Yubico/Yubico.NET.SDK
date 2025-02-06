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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        /// <remarks>
        /// <para>
        /// Enumerating YubiKeys is actually done via a cache. As such, this cache must be maintained
        /// and kept up-to-date. This is done by starting several listeners that run in the background.
        /// These listen for the relevant OS device arrival and removal events.
        /// </para>
        /// <para>
        /// Normally, these background listeners will run starting with the first enumeration call to the
        /// SDK and remain active until the process shuts down. But there are cases where you may not want
        /// the overhead of these listeners running all the time. While they do their best to not consume
        /// excessive resources, they can sometimes generate log noise, exceptions, etc.
        /// </para>
        /// <para>
        /// This method allows you to stop these
        /// background listeners and reclaim resources, as possible. This will not invalidate any existing
        /// IYubiKeyDevice instances, however you will not receive any additional events regarding that device.
        /// Any subsequent calls to <see cref="YubiKeyDevice.FindAll"/>, <see cref="YubiKeyDevice.FindByTransport"/>,
        /// or <see cref="Instance"/> will restart the listeners.
        /// </para>
        /// </remarks>
        public static void StopListening()
        {
            if (_lazyInstance == null)
            {
                return;
            }

            _lazyInstance.Dispose();
            _lazyInstance = null;
        }

        internal static bool IsListenerRunning => !(_lazyInstance is null);
        internal List<IYubiKeyDevice> GetAll() => _internalCache.Keys.ToList();

        private static YubiKeyDeviceListener? _lazyInstance;

        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private readonly ILogger _log = Log.GetLogger<YubiKeyDeviceListener>();
        private readonly Dictionary<IYubiKeyDevice, bool> _internalCache = new Dictionary<IYubiKeyDevice, bool>();
        private readonly HidDeviceListener _hidListener = HidDeviceListener.Create();
        private readonly SmartCardDeviceListener _smartCardListener = SmartCardDeviceListener.Create();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Task _listenTask;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private CancellationToken CancellationToken => _tokenSource.Token;

        private bool _isDisposed;
        private bool _isListening;

        private YubiKeyDeviceListener()
        {
            _log.LogInformation($"Creating {nameof(YubiKeyDeviceListener)} instance.");

            SetupDeviceListeners();

            _log.LogInformation("Performing initial cache population.");
            Update();

            _listenTask = ListenForChanges();
        }


        private void ArriveHandler(object? _, IDeviceEventArgs<IDevice> e) => ListenerHandler("Arrival", e);

        private void RemoveHandler(object? _, IDeviceEventArgs<IDevice> e) => ListenerHandler("Removal", e);

        private void ListenerHandler(string eventType, IDeviceEventArgs<IDevice> e)
        {
            LogEvent(eventType, e);
            _ = _semaphore.Release();
        }

        private void SetupDeviceListeners()
        {
            _smartCardListener.Arrived += ArriveHandler;
            _smartCardListener.Removed += RemoveHandler;
            _hidListener.Arrived += ArriveHandler;
            _hidListener.Removed += RemoveHandler;
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

        private void LogEvent(string eventType, IDeviceEventArgs<IDevice> e)
        {
            var device = e.Device;
            string deviceTypeText = device switch
            {
                ISmartCardDevice _ => "SMART CARD",
                IHidDevice _ => "HID",
                _ => "UNKNOWN"
            };

            _log.LogInformation(
                "{EventType} of {DeviceType} {Device} is triggering update.",
                eventType,
                deviceTypeText,
                device);
        }

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

        private static void ErrorHandler(Exception exception) => Log
                .GetLogger(typeof(YubiKeyDeviceListener).FullName!)
                .LogWarning($"Exception caught: {exception}");

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
                    _hidListener.Arrived -= ArriveHandler;
                    _hidListener.Removed -= RemoveHandler;
                    if (_hidListener is IDisposable hidDisp)
                    {
                        hidDisp.Dispose();
                    }

                    _smartCardListener.Arrived -= ArriveHandler;
                    _smartCardListener.Removed -= RemoveHandler;
                    if (_smartCardListener is IDisposable scDisp)
                    {
                        scDisp.Dispose();
                    }

                    // Give the listen task a moment (likely is already done).
                    _ = !_listenTask.Wait(100);
                    _listenTask.Dispose();

                    // Get rid of synchronization objects.
                    _rwLock.Dispose();
                    _semaphore.Dispose();
                    _tokenSource.Dispose();

                    if (ReferenceEquals(_lazyInstance, this))
                    {
                        _lazyInstance = null;
                    }
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
