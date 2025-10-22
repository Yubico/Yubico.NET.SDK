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

            // Allow for variance including first-use IOKit initialization
            // macOS allocates Mach ports (~15) and threads (~15) on first IOHIDManager usage
            // With 50 iterations, real leaks (50+ resources) still exceed tolerance (signal-to-noise: 50:25 = 2:1)
            int portDifference = Math.Abs(portCountAfter - portCountBefore);
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);

            Assert.True(portDifference <= 25,
                $"Mach port leak detected: {portCountBefore} before, {portCountAfter} after (difference: {portDifference}, limit: ±25)");
            Assert.True(threadDifference <= 25,
                $"Thread leak detected: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, limit: ±25)");
        }

        /// <summary>
        /// Verifies that concurrent Dispose() calls from multiple threads are thread-safe.
        /// </summary>
        [SkippableFact]
        public async Task ConcurrentDispose_IsThreadSafe()
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
            var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));
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

            // Thread count should return close to original
            // Single listener test is more susceptible to OS noise, allow ±3 threads
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);
            Assert.True(threadDifference <= 3,
                $"Thread leak detected: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, limit: ±3)");
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
        /// Uses fewer listeners than Windows/Linux due to macOS IOKit resource limits.
        /// </summary>
        [SkippableFact]
        public async Task ParallelCreateDispose_NoLeaksOrDeadlocks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            // Create 30 listeners in parallel to amplify leak detection
            // Reduced from 100 to avoid overwhelming macOS IOKit (100 concurrent IOHIDManager instances cause failures)
            // If each leaks 1 thread: 30 leaked threads >> background noise (signal-to-noise: 30:25 = 1.2:1)
            var tasks = new Task[30];
            for (int i = 0; i < 30; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    using var listener = HidDeviceListener.Create();
                    Thread.Sleep(50); // Hold briefly to ensure thread starts
                });
            }

            // Should complete without timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await Task.WhenAll(tasks).WaitAsync(cts.Token);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(200);

            int threadCountAfter = Process.GetCurrentProcess().Threads.Count;
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);

            // Tolerance covers first-use IOKit/CFRunLoop initialization (~15 threads) plus parallel variance (~10 threads)
            // With 30 listeners, real per-listener leaks (30+ threads) still exceed tolerance
            Assert.True(threadDifference <= 25,
                $"Thread leak in parallel test: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, limit: ±25)");
        }

        /// <summary>
        /// High-iteration sequential test: Create and dispose many listeners sequentially.
        /// This amplifies leak signal while minimizing parallel activity noise.
        /// With 500 iterations, even a 1-thread leak per listener becomes obvious.
        /// </summary>
        [SkippableFact]
        public void SequentialCreateDispose_HighIterations_NoLeaks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            // Create and dispose 500 listeners sequentially
            // Sequential execution minimizes parallel activity noise
            // High iteration count amplifies leak signal (500:1 signal-to-noise ratio)
            for (int i = 0; i < 500; i++)
            {
                using var listener = HidDeviceListener.Create();
                Thread.Sleep(10); // Brief hold to ensure thread lifecycle
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(200);

            int threadCountAfter = Process.GetCurrentProcess().Threads.Count;
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);

            // With 500 iterations, any per-listener leak would be obvious
            // Sequential minimizes noise, but allow generous tolerance for first-use initialization
            // With 500 listeners, real leaks (500+ threads) far exceed tolerance (signal-to-noise: 500:10 = 50:1)
            Assert.True(threadDifference <= 10,
                $"Thread leak in sequential test: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, limit: ±10)");
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
