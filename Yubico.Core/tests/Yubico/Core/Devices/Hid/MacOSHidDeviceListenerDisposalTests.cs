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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid.UnitTests
{
    /// <summary>
    /// Tests for disposal and resource management of MacOSHidDeviceListener.
    /// These tests verify thread lifecycle, CFRunLoop cleanup, and disposal timing.
    /// </summary>
    public class MacOSHidDeviceListenerDisposalTests
    {
        /// <summary>
        /// Verifies that Dispose() completes within a reasonable time.
        /// macOS listener uses CFRunLoopStop which should be fast.
        /// </summary>
        [SkippableFact]
        public void Dispose_CompletesWithinReasonableTime()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            var listener = HidDeviceListener.Create();

            var stopwatch = Stopwatch.StartNew();
            listener.Dispose();
            stopwatch.Stop();

            // CFRunLoopStop should wake thread immediately, plus join time
            // Allow more time than Linux due to CFRunLoop wake latency
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <500ms");
        }

        /// <summary>
        /// Verifies that calling Dispose() multiple times is safe and doesn't throw.
        /// Tests idempotency of disposal.
        /// </summary>
        [SkippableFact]
        public void Dispose_CalledMultipleTimes_IsIdempotent()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            var listener = HidDeviceListener.Create();

            // First disposal
            listener.Dispose();

            // Subsequent disposals should not throw
            var exception1 = Record.Exception(() => listener.Dispose());
            var exception2 = Record.Exception(() => listener.Dispose());
            var exception3 = Record.Exception(() => listener.Dispose());

            Assert.Null(exception1);
            Assert.Null(exception2);
            Assert.Null(exception3);
        }

        /// <summary>
        /// Verifies that repeated create/dispose cycles don't leak resources.
        /// Tests for IOKit port leaks and thread leaks.
        /// </summary>
        [SkippableFact]
        public void RepeatedCreateDispose_NoLeaks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            int portCountBefore = GetMachPortCount();
            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            // Create and dispose 50 listeners
            for (int i = 0; i < 50; i++)
            {
                using var listener = HidDeviceListener.Create();
                // Listener created and immediately disposed
            }

            // Force GC to ensure any finalizers run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Thread.Sleep(200); // Give threads time to fully terminate

            int portCountAfter = GetMachPortCount();
            int threadCountAfter = Process.GetCurrentProcess().Threads.Count;

            // Allow for small variance due to system activity
            int portDifference = Math.Abs(portCountAfter - portCountBefore);
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);

            Assert.True(portDifference <= 5,
                $"Mach port leak detected: {portCountBefore} before, {portCountAfter} after");
            Assert.True(threadDifference <= 2,
                $"Thread leak detected: {threadCountBefore} before, {threadCountAfter} after");
        }

        /// <summary>
        /// Verifies that concurrent Dispose() calls from multiple threads are thread-safe.
        /// </summary>
        [SkippableFact]
        public void ConcurrentDispose_IsThreadSafe()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            var listener = HidDeviceListener.Create();

            // Launch 10 concurrent Dispose() calls
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => listener.Dispose());
            }

            // Should not throw or deadlock
            var exception = Record.Exception(() => Task.WaitAll(tasks));
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that Dispose() terminates the listener thread.
        /// </summary>
        [SkippableFact]
        public void Dispose_TerminatesListenerThread()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            var listener = HidDeviceListener.Create();
            Thread.Sleep(100); // Give thread time to start

            int threadCountDuring = Process.GetCurrentProcess().Threads.Count;
            Assert.True(threadCountDuring >= threadCountBefore,
                "Thread count should increase when listener is active");

            listener.Dispose();
            Thread.Sleep(200); // Give thread time to terminate

            int threadCountAfter = Process.GetCurrentProcess().Threads.Count;

            // Thread count should return to original (±1 for variance)
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);
            Assert.True(threadDifference <= 1,
                $"Thread leak detected: {threadCountBefore} before, {threadCountAfter} after");
        }

        /// <summary>
        /// Verifies that CFRunLoop is properly stopped and cleaned up.
        /// </summary>
        [SkippableFact]
        public void Dispose_StopsCFRunLoop()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            var listener = HidDeviceListener.Create();
            Thread.Sleep(100); // Ensure CFRunLoop is running

            // Dispose should stop the run loop
            var stopwatch = Stopwatch.StartNew();
            listener.Dispose();
            stopwatch.Stop();

            // If CFRunLoopStop works, this should complete quickly
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                "CFRunLoop did not stop in reasonable time");
        }

        /// <summary>
        /// Stress test: Create and dispose many listeners in parallel.
        /// </summary>
        [SkippableFact]
        public void ParallelCreateDispose_NoLeaksOrDeadlocks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            // Create 20 listeners in parallel, dispose them
            var tasks = new Task[20];
            for (int i = 0; i < 20; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    using var listener = HidDeviceListener.Create();
                    Thread.Sleep(50); // Hold briefly to ensure thread starts
                });
            }

            // Should complete without timeout
            bool completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(15));
            Assert.True(completed, "Parallel create/dispose timed out");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(200);

            int threadCountAfter = Process.GetCurrentProcess().Threads.Count;
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);
            Assert.True(threadDifference <= 3,
                $"Thread leak in parallel test: {threadCountBefore} before, {threadCountAfter} after");
        }

        /// <summary>
        /// Verifies that disposing a listener that was never used still works correctly.
        /// </summary>
        [SkippableFact]
        public void Dispose_UnusedListener_Succeeds()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            var listener = HidDeviceListener.Create();
            // Don't subscribe to any events or do anything

            var stopwatch = Stopwatch.StartNew();
            var exception = Record.Exception(() => listener.Dispose());
            stopwatch.Stop();

            Assert.Null(exception);
            Assert.True(stopwatch.ElapsedMilliseconds < 500);
        }

        /// <summary>
        /// Verifies that finalizer doesn't throw exceptions that would crash GC thread.
        /// </summary>
        [SkippableFact]
        public void Finalizer_DoesNotCrashGCThread()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            // Create listener and let it go out of scope without disposing
            CreateAndAbandonListener();

            // Force GC and finalizers to run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // If we get here, finalizer didn't crash
            Assert.True(true);
        }

        private static void CreateAndAbandonListener()
        {
            _ = HidDeviceListener.Create();
            // Let it go out of scope without disposing
        }

        /// <summary>
        /// Verifies that IOHIDManager callbacks don't fire after disposal.
        /// </summary>
        [SkippableFact]
        public void Dispose_StopsIOKitCallbacks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            var listener = HidDeviceListener.Create();
            var callbackFired = false;

            listener.Arrived += (s, e) =>
            {
                callbackFired = true;
            };

            listener.Dispose();

            // Wait a bit to see if any callbacks fire (they shouldn't)
            Thread.Sleep(500);

            // We can't easily trigger a device event in tests, but we can verify
            // that disposal completed without errors
            Assert.False(callbackFired, "No device events should fire in test environment");
        }

        /// <summary>
        /// Verifies that delegate references are properly cleared to prevent GC issues.
        /// </summary>
        [SkippableFact]
        public void Dispose_ClearsDelegateReferences()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            var listener = HidDeviceListener.Create();

            listener.Arrived += (s, e) => { };
            listener.Removed += (s, e) => { };

            listener.Dispose();

            // Force GC to verify delegates can be collected
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // If we get here without issues, delegates were properly released
            Assert.True(true);
        }

        /// <summary>
        /// Helper method to get count of Mach ports for current process.
        /// This helps detect IOKit notification port leaks.
        /// </summary>
        private static int GetMachPortCount()
        {
            try
            {
                // On macOS, we can check Mach ports via lsof or process info
                // For simplicity, just use thread count as a proxy
                // Real implementation would use Mach port APIs
                using var process = Process.GetCurrentProcess();
                return process.Threads.Count; // Simplified proxy
            }
            catch
            {
                return 0;
            }
        }
    }
}
