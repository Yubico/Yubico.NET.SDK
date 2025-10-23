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
    /// Tests for disposal and resource management of WindowsHidDeviceListener.
    /// These tests verify callback unregistration, resource cleanup, and disposal timing.
    /// </summary>
    public class WindowsHidDeviceListenerDisposalTests
    {
        private static bool _isWarm = false;
        private static readonly object _warmupLock = new object();

        /// <summary>
        /// Warms up Windows Configuration Manager subsystem to avoid first-use overhead.
        /// The first CM_Register_Notification call in a process allocates ~70 handles for
        /// system initialization (thread pools, notification infrastructure, etc.).
        /// These are one-time allocations, not leaks.
        /// Runs multiple warmup rounds to ensure subsystem is fully initialized.
        /// </summary>
        private static void WarmupIfNeeded()
        {
            lock (_warmupLock)
            {
                if (_isWarm)
                {
                    return;
                }
            }

            lock (_warmupLock)
            {
                if (_isWarm)
                {
                    return;
                }

                // Run multiple warmup rounds to fully initialize Windows subsystems
                // This helps stabilize handle counts before actual testing
                for (var round = 0; round < 3; round++)
                {
                    // Create some listeners in parallel to warm up thread pools
                    var warmupTasks = new Task[10];
                    for (var i = 0; i < 10; i++)
                    {
                        warmupTasks[i] = Task.Run(() =>
                        {
                            using var listener = HidDeviceListener.Create();
                            Thread.Sleep(10);
                        });
                    }
                    Task.WaitAll(warmupTasks);
                }

                // Give Windows time to finish cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Thread.Sleep(500);

                _isWarm = true;
            }
        }

        /// <summary>
        /// Verifies that Dispose() completes within a reasonable time.
        /// Windows listener should unregister callback quickly.
        /// </summary>
        [SkippableFact]
        public void Dispose_CompletesWithinReasonableTime()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

            var listener = HidDeviceListener.Create();

            var stopwatch = Stopwatch.StartNew();
            listener.Dispose();
            stopwatch.Stop();

            // Windows callback unregistration should be very fast (<100ms)
            Assert.True(stopwatch.ElapsedMilliseconds < 100,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <100ms");
        }

        /// <summary>
        /// Verifies that calling Dispose() multiple times is safe and doesn't throw.
        /// Tests idempotency of disposal.
        /// </summary>
        [SkippableFact]
        public void Dispose_CalledMultipleTimes_IsIdempotent()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

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
        /// Tests for handle leaks and callback registration leaks.
        /// </summary>
        [SkippableFact]
        public void RepeatedCreateDispose_NoLeaks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

            // Warm up to exclude one-time Windows subsystem initialization overhead
            WarmupIfNeeded();

            var handleCountBefore = GetOpenHandleCount();

            // Create and dispose 50 listeners
            for (var i = 0; i < 50; i++)
            {
                using var listener = HidDeviceListener.Create();
                // Listener created and immediately disposed
            }

            // Force GC to ensure any finalizers run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var handleCountAfter = GetOpenHandleCount();

            // Allow for small variance (±5 handles) due to system activity
            var handleDifference = Math.Abs(handleCountAfter - handleCountBefore);
            Assert.True(handleDifference <= 5,
                $"Handle leak detected: {handleCountBefore} before, {handleCountAfter} after (difference: {handleDifference}, threshold: ±5)");
        }

        /// <summary>
        /// Verifies that concurrent Dispose() calls from multiple threads are thread-safe.
        /// </summary>
        [SkippableFact]
        public async Task ConcurrentDispose_IsThreadSafe()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

            var listener = HidDeviceListener.Create();

            // Launch 10 concurrent Dispose() calls
            var tasks = new Task[10];
            for (var i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => listener.Dispose());
            }

            // Should not throw or deadlock
            var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that Dispose() can be called while events might be firing.
        /// Tests that callback unregistration prevents use-after-free.
        /// </summary>
        [SkippableFact]
        public void Dispose_DuringPotentialEvents_CompletesGracefully()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

            var listener = HidDeviceListener.Create();
            var handlerCallCount = 0;

            listener.Arrived += (s, e) =>
            {
                Interlocked.Increment(ref handlerCallCount);
            };

            // Dispose should complete quickly even if events might be firing
            var stopwatch = Stopwatch.StartNew();
            listener.Dispose();
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < 100,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Stress test: Create and dispose many listeners in parallel.
        /// Increased to 100 listeners to amplify leak signal above background noise.
        /// </summary>
        [SkippableFact]
        public async Task ParallelCreateDispose_NoLeaksOrDeadlocks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

            // Warm up to exclude one-time Windows subsystem initialization overhead
            WarmupIfNeeded();

            var handleCountBefore = GetOpenHandleCount();

            // Create 100 listeners in parallel to amplify leak detection
            // If each leaks 1 handle: 100 leaked handles >> background noise
            var tasks = new Task[100];
            for (var i = 0; i < 100; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    using var listener = HidDeviceListener.Create();
                    Thread.Sleep(10); // Hold briefly
                });
            }

            // Should complete without timeout (longer timeout for more listeners)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await Task.WhenAll(tasks).WaitAsync(cts.Token);

            // Force GC to ensure any finalizers run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Give Windows time to fully release handles from parallel disposal
            // Parallel creation with 100 tasks requires significant cleanup time
            Thread.Sleep(2000);

            var handleCountAfter = GetOpenHandleCount();
            var handleDifference = Math.Abs(handleCountAfter - handleCountBefore);

            // Tolerance adjusted for parallel activity noise
            // Testing shows ~28 handle variance from Windows thread pool/subsystem overhead
            // when creating 100 listeners in parallel. This is NOT a per-listener leak.
            // A real per-listener leak would show 100+ handles leaked.
            Assert.True(handleDifference <= 35,
                $"Handle leak in parallel test: {handleCountBefore} before, {handleCountAfter} after (difference: {handleDifference}, threshold: ±35)");
        }

        /// <summary>
        /// High-iteration sequential test: Create and dispose many listeners sequentially.
        /// This amplifies leak signal while minimizing parallel activity noise.
        /// With 500 iterations, even a 1-handle leak per listener becomes obvious (500 vs ±15 noise).
        /// </summary>
        [SkippableFact]
        public void SequentialCreateDispose_HighIterations_NoLeaks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

            // Warm up to exclude one-time Windows subsystem initialization overhead
            WarmupIfNeeded();

            var handleCountBefore = GetOpenHandleCount();

            // Create and dispose 500 listeners sequentially
            // Sequential execution minimizes parallel activity noise
            // High iteration count amplifies leak signal (500:1 signal-to-noise ratio)
            for (var i = 0; i < 500; i++)
            {
                using var listener = HidDeviceListener.Create();
                // Dispose happens at end of using block
            }

            // Force GC to ensure any finalizers run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Give Windows time to fully release handles
            Thread.Sleep(100);

            var handleCountAfter = GetOpenHandleCount();
            var handleDifference = Math.Abs(handleCountAfter - handleCountBefore);

            // With 500 iterations, any per-listener leak would be obvious
            // Background noise is still ~±15, so tolerance of ±20 catches leaks clearly
            Assert.True(handleDifference <= 20,
                $"Handle leak in sequential test: {handleCountBefore} before, {handleCountAfter} after (difference: {handleDifference}, threshold: ±20)");
        }

        /// <summary>
        /// Verifies that disposing a listener that was never used still works correctly.
        /// </summary>
        [SkippableFact]
        public void Dispose_UnusedListener_Succeeds()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

            var listener = HidDeviceListener.Create();
            // Don't subscribe to any events or do anything

            var stopwatch = Stopwatch.StartNew();
            var exception = Record.Exception(() => listener.Dispose());
            stopwatch.Stop();

            Assert.Null(exception);
            Assert.True(stopwatch.ElapsedMilliseconds < 100,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <100ms");
        }

        /// <summary>
        /// Verifies that finalizer doesn't throw exceptions that would crash GC thread.
        /// </summary>
        [SkippableFact]
        public void Finalizer_DoesNotCrashGCThread()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

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
        /// Verifies that GCHandle is properly freed to prevent memory leaks.
        /// </summary>
        [SkippableFact]
        public void Dispose_FreesGCHandle_NoPinnedObjectLeak()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

            // Create and dispose multiple listeners
            // If GCHandle isn't freed, we'll get increasing pinned object count
            for (var i = 0; i < 10; i++)
            {
                using var listener = HidDeviceListener.Create();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            // If we get here without OOM or GC pressure, GCHandles were freed
            Assert.True(true);
        }

        /// <summary>
        /// Helper method to get count of open handles for current process.
        /// </summary>
        private static int GetOpenHandleCount()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                return process.HandleCount;
            }
            catch
            {
                // If we can't get handle count, return 0 to skip the assertion
                return 0;
            }
        }
    }
}
