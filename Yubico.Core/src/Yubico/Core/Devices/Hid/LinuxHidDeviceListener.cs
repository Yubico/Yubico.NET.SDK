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
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Yubico.PlatformInterop;

using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.Hid
{
    internal class LinuxHidDeviceListener : HidDeviceListener
    {
        // Note that both the main thread and the created thread (sub-thread)
        // will have access to _isListening. That seems like we will have race
        // conditions and hence we need a lock.
        // However, only the main thread will change this value. The sub-thread
        // will only read it. The only race condition is when the StopListening
        // method is called. In that case, the main thread will change it from
        // true to false. Once the sub-thread sees it is false, it will stop.
        // Suppose by chance, that, at the exact same time, the main thread wants
        // to stop (change _isListening to false) and the sub-thread wants to
        // read it to determine if it should continue. This is the race
        // condition.
        // If the main thread is able to change the value first, then the sub
        // thread will see it is false and stop running, which is what we want.
        // If the sub-thread reads it first, it will start another iteration, the
        // main thread will change the value and wait for the sub-thread to stop.
        // The sub-thread will finish its current iteration and then the next
        // iteration will see that the value is false and will stop.
        // This is exactly what would happen if we had a lock.
        private bool _isListening;
        private Thread? _listenerThread;

        private readonly LinuxUdevMonitorSafeHandle _monitorObject;
        private readonly LinuxUdevSafeHandle _udevObject;

        public LinuxHidDeviceListener()
        {
            _udevObject = udev_new();
            _monitorObject = ThrowIfFailedNull(udev_monitor_new_from_netlink(_udevObject, UdevMonitorName));

            StartListening();
        }

        // We're declaring an explicit finalizer because there are resources that need to be cleaned.
        // I'd like to avoid forcing this class to be disposable, based on its expected usage. None
        // of the resources acquired by this class have any critical time constraints that would
        // necessitate deterministic cleanup (Dispose). Letting the garbage collector clean up this
        // class should be fine, so long as this finalizer is called.
        ~LinuxHidDeviceListener()
        {
            StopListening();
            _monitorObject.Dispose();
            _udevObject.Dispose();
        }

        /// <summary>
        /// Call this method after you have added the EventHandlers.
        /// </summary>
        /// <remarks>
        /// If you call this method and the object is already listening, it will
        /// do nothing, the object will simply continue its previously started
        /// listening session.
        /// <para>
        /// If the object has stopped listening, this method will start listening
        /// again.
        /// </para>
        /// </remarks>
        private void StartListening()
        {
            _ = ThrowIfFailedNegative(udev_monitor_filter_add_match_subsystem_devtype(
                    _monitorObject, UdevSubsystemName, null));

            _ = ThrowIfFailedNegative(udev_monitor_enable_receiving(_monitorObject));

            // We don't need to lock because if there is a separate thread that
            // has access to _isListening, it does not change it.
            // If there is a sub-thread running, _isListening will be true and we
            // don't want to do anything.
            // If there is no sub-thread, _isListening will be false and we will
            // create a new thread and start it. Just make sure _isListening is
            // set to true before starting the thread.
            if (!_isListening)
            {
                _listenerThread = new Thread(ListenForReaderChanges);
                _isListening = true;
                _listenerThread.Start();
            }
        }

        /// <summary>
        /// Let the object know it should stop listening.
        /// </summary>
        /// <remarks>
        /// If the object has already stopped listening, this method will do
        /// nothing.
        /// </remarks>
        private void StopListening()
        {
            // If someone creates an instance of this class and then immediately
            // calls Stop, we don't want to do anything. Hence, check for null
            // (we declared this field nullable).
            // If there is a _listenerThread, then it is either active or not.
            // It can be inactive if the caller called Stop two times in a row.
            // If it is inactive, _isListening will be false already, and it
            // doesn't matter that we're setting it to false again. The Join will
            // do nothing and we exit.
            // If the sub-thread is active, then _isListening is true, so we're
            // setting it to false so the sub-thread knows to quit. The
            // sub-thread will complete the iteration it has most recently
            // started, see _isListening is false and quit. At that point, the
            // Join will will be able to complete and this method will exit.
            if (!(_listenerThread is null))
            {
                _isListening = false;
                _listenerThread.Join();
            }
        }

        // This method is the delegate sent to the new Thread.
        // Once the new Thread is started, this method will execute. As long as
        // the _isListening field is true, it will keep checking for updates.
        // Once _isListening is false, it will quit the loop and return, which
        // will terminate the thread.
        private void ListenForReaderChanges()
        {
            while (_isListening)
            {
                CheckForUpdates();
            }
        }

        // If there was a relevant update, this method will call the appropriate
        // EventHandler.
        // If there has been no update, or if the update is not one we're
        // concerned with, it simply returns.
        private void CheckForUpdates()
        {
            // If this call returns NULL, there was no update.
            using LinuxUdevDeviceSafeHandle udevDevice = udev_monitor_receive_device(_monitorObject);
            if (udevDevice.IsInvalid)
            {
                return;
            }

            var device = new LinuxHidDevice(udevDevice);

            // Was this an add or remove? If so, call the appropriate handler. If
            // not, ignore this change.
            IntPtr actionPtr = udev_device_get_action(udevDevice);
            string action = Marshal.PtrToStringAnsi(actionPtr) ?? string.Empty;
            if (string.Equals(action, "add", StringComparison.Ordinal))
            {
                OnArrived(device);
            }
            else if (string.Equals(action, "remove", StringComparison.Ordinal))
            {
                OnRemoved(device);
            }
        }

        // Throw the PlatformApiException(LinuxUdevError) if the value is NULL.
        // Otherwise, just return value.
        private static T ThrowIfFailedNull<T>(T value) where T : SafeHandle
        {
            if (!value.IsInvalid)
            {
                return value;
            }

            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.LinuxUdevError));
        }


        // Throw the PlatformApiException(LinuxUdevError) if the value is < 0.
        // Otherwise, just return.
        private static int ThrowIfFailedNegative(int value)
        {
            if (value >= 0)
            {
                return value;
            }

            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.LinuxUdevError));
        }
    }
}
