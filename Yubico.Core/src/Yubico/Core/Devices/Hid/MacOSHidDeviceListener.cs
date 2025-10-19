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
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using static Yubico.PlatformInterop.NativeMethods;

namespace Yubico.Core.Devices.Hid
{
    /// <summary>
    /// A MacOS implementation of the HID device listener
    /// </summary>
    internal class MacOSHidDeviceListener : HidDeviceListener
    {
        private Thread? _listenerThread;
        private IntPtr? _runLoop;

        private readonly ILogger _log = Logging.Log.GetLogger<MacOSHidDeviceListener>();

        // Start listening as soon as this object is constructed.
        public MacOSHidDeviceListener()
        {
            _log.LogInformation("Creating MacOSHidDeviceListener.");
            StartListening();
        }

        ~MacOSHidDeviceListener()
        {
            Dispose(false);
        }

        private void StartListening()
        {
            _listenerThread = new Thread(ListeningThread)
            {
                IsBackground = true
            };
            _listenerThread.Start();
        }

        private void StopListening()
        {
            if (_runLoop.HasValue && _runLoop != IntPtr.Zero)
            {
                CFRunLoopStop(_runLoop.Value);
            }

            _listenerThread?.Join();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    // No managed resources to dispose.
                }

                try
                {
                    StopListening();
                }
                catch (Exception ex)
                {
                    // CRITICAL: Never throw from Dispose, especially when called from finalizer
                    // Throwing from finalizer will crash the GC thread and terminate the application
                    if (disposing)
                    {
                        _log.LogWarning(ex, "Exception during StopListening in MacOSHidDeviceListener disposal");
                    }
                    // If !disposing (finalizer path), silently ignore to prevent GC thread crash
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void ListeningThread()
        {
            const int runLoopTimeout = 10; // 10 seconds is arbitrary, pulled from Apple sample code
            using IDisposable? logScope = _log.BeginScope("MacOSHidDeviceListener.StartListening()");

            _log.LogInformation("HID listener thread started. ThreadID is {ThreadID}.", Environment.CurrentManagedThreadId);

            IntPtr manager = IntPtr.Zero;
            IntPtr runLoopMode = IntPtr.Zero;

            try
            {
                byte[] cstr = Encoding.UTF8.GetBytes($"default-runloop-{Environment.CurrentManagedThreadId}");
                runLoopMode = CFStringCreateWithCString(IntPtr.Zero, cstr, 0);

                manager = IOHIDManagerCreate(IntPtr.Zero, 0);
                IOHIDManagerSetDeviceMatching(manager, IntPtr.Zero);

                _runLoop = CFRunLoopGetCurrent();
                IOHIDManagerScheduleWithRunLoop(manager, _runLoop.Value, runLoopMode);

                _log.LogInformation(
                    "IOHIDManager {Manager} is scheduled with run loop {Loop} with mode {Mode}",
                    manager,
                    _runLoop,
                    runLoopMode);

                // MacOS returns both present and future device events. We're only interested in the future ones, so let's
                // clear out the ones that are already present.
                _ = CFRunLoopRunInMode(runLoopMode, runLoopTimeout, true);
                _log.LogInformation("Flushed existing devices.");

                IOHIDManagerRegisterDeviceMatchingCallback(manager, ArrivedCallback, IntPtr.Zero);
                IOHIDManagerRegisterDeviceRemovalCallback(manager, RemovedCallback, IntPtr.Zero);

                int runLoopResult = kCFRunLoopRunHandledSource;

                // This is essentially an infinite loop (hence running this on its own thread). This can be broken if
                // an error is encountered, or if someone calls CFRunLoopStop as is done in StopListening/Finalizer.
                _log.LogInformation("Beginning run loop polling.");
                while (runLoopResult == kCFRunLoopRunHandledSource || runLoopResult == kCFRunLoopRunTimedOut)
                {
                    runLoopResult = CFRunLoopRunInMode(runLoopMode, runLoopTimeout, true);
                }
                _log.LogInformation("Run loop exited.");

                if (runLoopResult != kCFRunLoopRunStopped)
                {
                    _log.LogError("The run loop was terminated for an unexpected reason: {Result}", runLoopResult);
                }
            }
            catch (Exception e)
            {
                // We must not let exceptions escape from this callback. There's nowhere for them to go, and
                // it will likely crash the process.
                _log.LogError(e, "Exception in HID listener thread.");
            }
            finally
            {
                if (_runLoop.HasValue)
                {
                    IOHIDManagerUnscheduleFromRunLoop(manager, _runLoop.Value, runLoopMode);
                }

                if (manager != IntPtr.Zero)
                {
                    _log.LogInformation("IOHIDManager released.");
                }

                if (runLoopMode != IntPtr.Zero)
                {
                    CFRelease(runLoopMode);
                }
            }
        }

        private void ArrivedCallback(IntPtr context, int result, IntPtr sender, IntPtr device) =>
            OnArrived(new MacOSHidDevice(MacOSHidDevice.GetEntryId(device)));

        private void RemovedCallback(IntPtr context, int result, IntPtr sender, IntPtr device) =>
            OnRemoved(NullDevice.Instance);
    }
}
