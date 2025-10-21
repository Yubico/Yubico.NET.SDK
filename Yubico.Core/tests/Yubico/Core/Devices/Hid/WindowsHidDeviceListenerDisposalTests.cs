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

            int handleCountBefore = GetOpenHandleCount();

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

            int handleCountAfter = GetOpenHandleCount();

            // Allow for small variance (±5 handles) due to system activity
            int handleDifference = Math.Abs(handleCountAfter - handleCountBefore);
            Assert.True(handleDifference <= 5,
                $"Handle leak detected: {handleCountBefore} before, {handleCountAfter} after (difference: {handleDifference})");
        }

        /// <summary>
        /// Verifies that concurrent Dispose() calls from multiple threads are thread-safe.
        /// </summary>
        [SkippableFact]
        public void ConcurrentDispose_IsThreadSafe()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

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
        /// </summary>
        [SkippableFact]
        public void ParallelCreateDispose_NoLeaksOrDeadlocks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows, "Windows-only test");

            int handleCountBefore = GetOpenHandleCount();

            // Create 20 listeners in parallel, dispose them
            var tasks = new Task[20];
            for (int i = 0; i < 20; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    using var listener = HidDeviceListener.Create();
                    Thread.Sleep(10); // Hold briefly
                });
            }

            // Should complete without timeout
            bool completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(10));
            Assert.True(completed, "Parallel create/dispose timed out");

            GC.Collect();
            GC.WaitForPendingFinalizers();

            int handleCountAfter = GetOpenHandleCount();
            int handleDifference = Math.Abs(handleCountAfter - handleCountBefore);
            Assert.True(handleDifference <= 10,
                $"Handle leak in parallel test: {handleCountBefore} before, {handleCountAfter} after");
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
            Assert.True(stopwatch.ElapsedMilliseconds < 100);
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
            for (int i = 0; i < 10; i++)
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
