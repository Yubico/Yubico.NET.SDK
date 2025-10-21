// Copyright 2025 Yubico AB
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
using Microsoft.Extensions.Logging;
using Yubico.PlatformInterop;
using static Yubico.PlatformInterop.LibcFcntlConstants;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.Hid
{
    internal class LinuxHidDeviceListener : HidDeviceListener
    {
        private bool _isListening;
        private Thread? _listenerThread;
        private readonly object _startStopLock = new object();
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isDisposed;

        private LinuxUdevMonitorSafeHandle _monitorObject;
        private LinuxUdevSafeHandle _udevObject;
        private readonly ILogger _log = Logging.Log.GetLogger<LinuxHidDeviceListener>();

        public LinuxHidDeviceListener()
        {
            _udevObject = udev_new();
            _monitorObject = ThrowIfFailedNull(udev_monitor_new_from_netlink(_udevObject, UdevMonitorName));

            StartListening();
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                if (disposing)
                {
                    StopListening();

                    _monitorObject.Dispose();
                    _udevObject.Dispose();

                    _monitorObject = null!;
                    _udevObject = null!;
                }

                _isDisposed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
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
            lock (_startStopLock)
            {
                if (_isListening)
                {
                    return;
                }

                _ = ThrowIfFailedNegative(udev_monitor_filter_add_match_subsystem_devtype(
                        _monitorObject, UdevSubsystemName, null));

                _ = ThrowIfFailedNegative(udev_monitor_enable_receiving(_monitorObject));

                _cancellationTokenSource = new CancellationTokenSource();
                _listenerThread = new Thread(ListenForReaderChanges)
                {
                    IsBackground = true
                };

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
            lock (_startStopLock)
            {
                if (!_isListening || _listenerThread is null || _cancellationTokenSource is null)
                {
                    return;
                }

                _isListening = false;

                // Signal the thread to stop
                _cancellationTokenSource.Cancel();
            }

            // Wait for thread to exit (outside of lock to avoid blocking other operations)
            // Use a timeout to prevent indefinite blocking
            Thread? threadToJoin = _listenerThread;
            if (threadToJoin != null)
            {
                bool exited = threadToJoin.Join(TimeSpan.FromSeconds(3));
                if (!exited)
                {
                    _log.LogWarning("Listener thread did not exit within timeout. This should not happen with proper cancellation support.");
                }
            }

            lock (_startStopLock)
            {
                _listenerThread = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        
        private void ListenForReaderChanges()
        {
            try
            {
                CancellationToken cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

                while (!cancellationToken.IsCancellationRequested)
                {
                    CheckForUpdates();
                }
            }
            catch (Exception e)
            {
                // We must not let exceptions escape from this callback. There's nowhere for them to go, and
                // it will likely crash the process.
                _log.LogWarning(e, "Exception in ListenForReaderChanges thread.");
            }
        }

        // If there was a relevant update, this method will call the appropriate
        // EventHandler.
        // If there has been no update, or if the update is not one we're
        // concerned with, it simply returns.
        private void CheckForUpdates()
        {
            // First check if there are any events available using poll with a short timeout
            // This allows the thread to remain responsive to cancellation
            if (!HasPendingEvents(timeoutMs: 100))
            {
                // No events available within timeout, return to allow cancellation check
                return;
            }

            // If this call returns NULL, there was no update.
            // Since we're using non-blocking mode, this should return immediately
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


        /// <summary>
        /// Checks if there are any pending udev events using poll with a timeout.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>True if events are available, false if timeout occurred</returns>
        private bool HasPendingEvents(int timeoutMs)
        {
            IntPtr fd = udev_monitor_get_fd(_monitorObject);

            // Use poll to wait for events with timeout
            // POLLIN = 0x0001 (data available to read)
            const short POLLIN = 0x0001;

            var pollFd = new PollFd
            {
                fd = fd,
                events = POLLIN,
                revents = 0
            };

            PollFd[] pollFds = new[] { pollFd };
            int result = poll(pollFds, 1, timeoutMs);

            // result > 0 means events are ready
            // result == 0 means timeout
            // result < 0 means error
            return result > 0 && (pollFds[0].revents & POLLIN) != 0;
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
