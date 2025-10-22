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
        // Apple's standard run loop mode for normal operation
        // https://developer.apple.com/documentation/corefoundation/default-run-loop-mode
        private const string kCFRunLoopDefaultMode = "kCFRunLoopDefaultMode";

        private Thread? _listenerThread;
        private IntPtr? _runLoop;

        // Volatile flag for thread-safe cancellation
        // Using Apple's recommended pattern: while (!_shouldStop) { CFRunLoopRunInMode(...); }
        // This ensures disposal exits quickly without depending on CFRunLoopStop() timing
        private volatile bool _shouldStop;

        // Keep strong references to delegates to prevent garbage collection
        // Native IOHIDManager stores function pointers to these callbacks
        // If delegates are collected, native callbacks will crash with "callback on garbage collected delegate"
        private IOHIDDeviceCallback? _arrivedCallbackDelegate;
        private IOHIDDeviceCallback? _removedCallbackDelegate;

        private bool _isDisposed;
        private readonly object _disposeLock = new object();

        private readonly ILogger _log = Logging.Log.GetLogger<MacOSHidDeviceListener>();

        // Unique instance ID for correlating logs across threads and tests
        #pragma warning disable IDE0032
        private readonly string _instanceId;
        #pragma warning restore IDE0032
        private static int _instanceCounter;

        // Expose instance ID for test correlation
        // Tests can log "Created listener {InstanceId}" to correlate telemetry logs
        internal string InstanceId => _instanceId;

        // Start listening as soon as this object is constructed.
        public MacOSHidDeviceListener()
        {
            _instanceId = $"L{System.Threading.Interlocked.Increment(ref _instanceCounter):D3}";
            _log.LogInformation("[TELEMETRY][{InstanceId}] MacOSHidDeviceListener constructor called on thread {ThreadId}", _instanceId, Environment.CurrentManagedThreadId);
            StartListening();
            _log.LogInformation("[TELEMETRY][{InstanceId}] MacOSHidDeviceListener constructor complete", _instanceId);
        }

        ~MacOSHidDeviceListener()
        {
            _log.LogInformation("[TELEMETRY][{InstanceId}] MacOSHidDeviceListener finalizer called", _instanceId);
            Dispose(false);
        }

        private void StartListening()
        {
            _log.LogInformation("[TELEMETRY][{InstanceId}] StartListening() called, creating background thread", _instanceId);
            _listenerThread = new Thread(ListeningThread)
            {
                IsBackground = true
            };
            _listenerThread.Start();
            _log.LogInformation("[TELEMETRY][{InstanceId}] Background thread started", _instanceId);
        }

        private void StopListening(bool fromFinalizer = false)
        {
            _log.LogInformation("[TELEMETRY][{InstanceId}] StopListening(fromFinalizer={FromFinalizer}) called", _instanceId, fromFinalizer);

            // Use local variables to prevent race condition if multiple threads call StopListening()
            Thread? threadToJoin = _listenerThread;
            IntPtr? runLoopToStop = _runLoop;

            _log.LogInformation("[TELEMETRY][{InstanceId}] Setting _shouldStop flag to true", _instanceId);

            // Set cancellation flag (Apple's recommended pattern)
            // Thread will check this flag and exit on next loop iteration
            _shouldStop = true;

            _log.LogInformation("[TELEMETRY][{InstanceId}] _shouldStop flag set. RunLoop pointer: {RunLoop}", _instanceId, runLoopToStop);

            // Also call CFRunLoopStop() as best-effort to wake the thread immediately
            // This is not strictly necessary (flag will work), but reduces latency
            // If thread hasn't initialized _runLoop yet, that's fine - flag will handle it
            if (runLoopToStop.HasValue && runLoopToStop != IntPtr.Zero)
            {
                _log.LogInformation("[TELEMETRY][{InstanceId}] Calling CFRunLoopStop({RunLoop})", _instanceId, runLoopToStop.Value);
                CFRunLoopStop(runLoopToStop.Value);
                _log.LogInformation("[TELEMETRY][{InstanceId}] CFRunLoopStop() returned", _instanceId);
            }
            else
            {
                _log.LogInformation("[TELEMETRY][{InstanceId}] Skipping CFRunLoopStop (runLoop not initialized)", _instanceId);
            }

            // CRITICAL: Never block on Thread.Join() from finalizer
            // Blocking the GC thread causes test runner hangs and cascading failures
            // From finalizer: Signal cancellation and return immediately, let thread terminate asynchronously
            // From Dispose: Wait briefly to ensure clean shutdown, but don't block forever
            if (!fromFinalizer && threadToJoin != null)
            {
                _log.LogInformation("[TELEMETRY][{InstanceId}] Waiting for thread to exit (1 second timeout)...", _instanceId);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                bool exited = threadToJoin.Join(TimeSpan.FromSeconds(1));
                stopwatch.Stop();

                if (exited)
                {
                    _log.LogInformation("[TELEMETRY][{InstanceId}] Thread exited successfully after {Elapsed}ms", _instanceId, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _log.LogWarning("[TELEMETRY][{InstanceId}] HANG DETECTED! Thread did not exit after {Elapsed}ms", _instanceId, stopwatch.ElapsedMilliseconds);
                }
            }
            else if (fromFinalizer)
            {
                _log.LogInformation("[TELEMETRY][{InstanceId}] Skipping Thread.Join() from finalizer to prevent GC thread blocking", _instanceId);
            }
            else
            {
                _log.LogInformation("[TELEMETRY][{InstanceId}] No thread to join", _instanceId);
            }

            // Clear fields to make StopListening() idempotent
            _runLoop = null;
            _listenerThread = null;

            _log.LogInformation("[TELEMETRY][{InstanceId}] StopListening() complete", _instanceId);
        }

        protected override void Dispose(bool disposing)
        {
            string callStack = new System.Diagnostics.StackTrace(true).ToString();
            _log.LogInformation("[TELEMETRY][{InstanceId}] >>> DISPOSE-ENTRY: disposing={Disposing}, thread={ThreadId}, _isDisposed={IsDisposed}, _shouldStop={ShouldStop}\n  CallStack:\n{CallStack}",
                _instanceId, disposing, Environment.CurrentManagedThreadId, _isDisposed, _shouldStop, callStack);

            lock (_disposeLock)
            {
                if (_isDisposed)
                {
                    _log.LogInformation("[TELEMETRY][{InstanceId}] Already disposed, returning early", _instanceId);
                    return;
                }

                _log.LogInformation("[TELEMETRY][{InstanceId}] Setting _isDisposed = true", _instanceId);
                _isDisposed = true;

                try
                {
                    // Stop the listening thread and wait for it to complete
                    // This ensures ListeningThread's finally block runs and cleans up native resources
                    // Pass fromFinalizer=true when disposing=false to prevent blocking GC thread
                    _log.LogInformation("[TELEMETRY][{InstanceId}] Calling StopListening(fromFinalizer={FromFinalizer})...", _instanceId, !disposing);
                    StopListening(fromFinalizer: !disposing);
                    _log.LogInformation("[TELEMETRY][{InstanceId}] StopListening() returned successfully", _instanceId);

                    // Clear delegate references to allow garbage collection
                    // Must be done AFTER StopListening() to ensure callbacks aren't invoked on null delegates
                    _arrivedCallbackDelegate = null;
                    _removedCallbackDelegate = null;
                }
                catch (Exception ex)
                {
                    // CRITICAL: Never throw from Dispose, especially when called from finalizer
                    // Throwing from finalizer will crash the GC thread and terminate the application
                    _log.LogError(ex, "[TELEMETRY][{InstanceId}] ⚠️ EXCEPTION in Dispose: {Message}\n  StackTrace:\n{StackTrace}",
                        _instanceId, ex.Message, ex.StackTrace);
                    if (disposing)
                    {
                        _log.LogWarning(ex, "[TELEMETRY][{InstanceId}] Exception during MacOSHidDeviceListener disposal", _instanceId);
                    }
                    // If !disposing (finalizer path), silently ignore to prevent GC thread crash
                }
                finally
                {
                    _log.LogInformation("[TELEMETRY][{InstanceId}] Calling base.Dispose({Disposing})", _instanceId, disposing);
                    base.Dispose(disposing);
                    _log.LogInformation("[TELEMETRY][{InstanceId}] <<< DISPOSE-EXIT: Complete", _instanceId);
                }
            }
        }

        private void ListeningThread()
        {
            // CFRunLoopRunInMode timeout in SECONDS (not milliseconds!)
            // Reference: https://developer.apple.com/documentation/corefoundation/1541988-cfrunloopruninmode
            //
            // CFRunLoopRunInMode monitors the run loop for events with a timeout, similar to Linux poll().
            // It blocks the thread for up to 'timeout' seconds waiting for IOHIDManager device events.
            //
            // Return values from CFRunLoopRunInMode:
            // - kCFRunLoopRunStopped (2): Stopped by CFRunLoopStop() - triggers disposal exit
            // - kCFRunLoopRunTimedOut (3): Timeout expired with no events - continue monitoring
            // - kCFRunLoopRunHandledSource (4): Processed a device event - continue monitoring
            //
            // 0.1 second (100ms) timeout provides:
            // - Responsiveness: Thread checks CFRunLoopStop() every 100ms
            // - Efficiency: Only 10 wake-ups per second (minimal CPU/battery impact)
            // - Low latency: Device events processed within 0-100ms
            // - Consistency: Matches Linux poll() timeout for uniform cross-platform behavior
            const double runLoopTimeout = 0.1; // 100 milliseconds (0.1 seconds)
            using IDisposable? logScope = _log.BeginScope("MacOSHidDeviceListener.StartListening()");

            _log.LogInformation("HID listener thread started. ThreadID is {ThreadID}.", Environment.CurrentManagedThreadId);

            IntPtr manager = IntPtr.Zero;
            IntPtr runLoopMode = IntPtr.Zero;

            try
            {
                // Create CFString for kCFRunLoopDefaultMode constant
                byte[] modeBytes = System.Text.Encoding.UTF8.GetBytes(kCFRunLoopDefaultMode + "\0");
                runLoopMode = CFStringCreateWithCString(IntPtr.Zero, modeBytes, 0);

                manager = IOHIDManagerCreate(IntPtr.Zero, 0);
                IOHIDManagerSetDeviceMatching(manager, IntPtr.Zero);

                _runLoop = CFRunLoopGetCurrent();

                IOHIDManagerScheduleWithRunLoop(manager, _runLoop.Value, runLoopMode);

                _log.LogInformation(
                    "IOHIDManager {Manager} is scheduled with run loop {Loop} with mode {Mode}",
                    manager,
                    _runLoop,
                    kCFRunLoopDefaultMode);  // Log the constant string name

                // MacOS returns both present and future device events. We're only interested in the future ones, so let's
                // clear out the ones that are already present.
                _ = CFRunLoopRunInMode(runLoopMode, runLoopTimeout, true);
                _log.LogInformation("Flushed existing devices.");

                // CRITICAL: Store delegates as instance fields to prevent garbage collection
                // If we pass method groups directly, the P/Invoke marshaller creates temporary delegates
                // that can be GC'd while native code still holds function pointers to them
                // This causes: "A callback was made on a garbage collected delegate" crash
                _arrivedCallbackDelegate = ArrivedCallback;
                _removedCallbackDelegate = RemovedCallback;

                IOHIDManagerRegisterDeviceMatchingCallback(manager, _arrivedCallbackDelegate, IntPtr.Zero);
                IOHIDManagerRegisterDeviceRemovalCallback(manager, _removedCallbackDelegate, IntPtr.Zero);

                int runLoopResult = kCFRunLoopRunHandledSource;

                // Apple's recommended pattern: while (!_shouldStop) { CFRunLoopRunInMode(...); }
                // This ensures the thread exits promptly when disposal sets _shouldStop = true
                // We also check return codes to exit on unexpected errors
                _log.LogInformation("[TELEMETRY][{InstanceId}] Entering run loop. _shouldStop={ShouldStop}", _instanceId, _shouldStop);
                int loopIterations = 0;
                while (!_shouldStop && (runLoopResult == kCFRunLoopRunHandledSource || runLoopResult == kCFRunLoopRunTimedOut))
                {
                    loopIterations++;

                    // Log every iteration for first 50, then every 10th to avoid spam
                    bool shouldLog = loopIterations <= 50 || loopIterations % 10 == 0;

                    if (shouldLog)
                    {
                        _log.LogInformation("[TELEMETRY][{InstanceId}] Loop iter {Iteration}: _shouldStop={ShouldStop}, prevResult={Result}, calling CFRunLoopRunInMode...",
                            _instanceId, loopIterations, _shouldStop, runLoopResult);
                    }

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    runLoopResult = CFRunLoopRunInMode(runLoopMode, runLoopTimeout, true);
                    sw.Stop();

                    if (shouldLog)
                    {
                        _log.LogInformation("[TELEMETRY][{InstanceId}] Loop iter {Iteration}: CFRunLoopRunInMode returned {Result} after {Elapsed}ms",
                            _instanceId, loopIterations, runLoopResult, sw.ElapsedMilliseconds);
                    }
                }
                _log.LogInformation("[TELEMETRY][{InstanceId}] Run loop exited after {Iterations} iterations. _shouldStop={ShouldStop}, finalResult={Result}",
                    _instanceId, loopIterations, _shouldStop, runLoopResult);

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
                // Clean up thread-local native resources
                // Wrap in try-catch to prevent P/Invoke exceptions from crashing the background thread
                _log.LogInformation("[TELEMETRY][{InstanceId}] Listener thread finally block - cleaning up native resources", _instanceId);

                try
                {
                    // Unschedule manager from run loop before releasing
                    if (_runLoop.HasValue && manager != IntPtr.Zero && runLoopMode != IntPtr.Zero)
                    {
                        _log.LogInformation("[TELEMETRY][{InstanceId}] Unscheduling IOHIDManager from run loop", _instanceId);
                        IOHIDManagerUnscheduleFromRunLoop(manager, _runLoop.Value, runLoopMode);
                        _log.LogInformation("[TELEMETRY][{InstanceId}] IOHIDManager unscheduled from run loop", _instanceId);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "[TELEMETRY][{InstanceId}] Exception unscheduling IOHIDManager from run loop", _instanceId);
                }

                try
                {
                    // Release the IOHIDManager
                    if (manager != IntPtr.Zero)
                    {
                        _log.LogInformation("[TELEMETRY][{InstanceId}] Releasing IOHIDManager", _instanceId);
                        CFRelease(manager);
                        _log.LogInformation("[TELEMETRY][{InstanceId}] IOHIDManager released", _instanceId);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "[TELEMETRY][{InstanceId}] Exception releasing IOHIDManager", _instanceId);
                }

                try
                {
                    // Release the run loop mode CFString
                    if (runLoopMode != IntPtr.Zero)
                    {
                        _log.LogInformation("[TELEMETRY][{InstanceId}] Releasing run loop mode CFString", _instanceId);
                        CFRelease(runLoopMode);
                        _log.LogInformation("[TELEMETRY][{InstanceId}] Run loop mode CFString released", _instanceId);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "[TELEMETRY][{InstanceId}] Exception releasing run loop mode CFString", _instanceId);
                }

                _log.LogInformation("[TELEMETRY][{InstanceId}] Listener thread cleanup complete - thread exiting", _instanceId);
            }
        }

        private void ArrivedCallback(IntPtr context, int result, IntPtr sender, IntPtr device) =>
            OnArrived(new MacOSHidDevice(MacOSHidDevice.GetEntryId(device)));

        private void RemovedCallback(IntPtr context, int result, IntPtr sender, IntPtr device) =>
            OnRemoved(NullDevice.Instance);
    }
}

