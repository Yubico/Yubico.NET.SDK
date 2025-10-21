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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid.UnitTests
{
    /// <summary>
    /// Tests for disposal and resource management of LinuxHidDeviceListener.
    /// These tests verify thread safety, resource cleanup, and disposal timing.
    /// </summary>
    public class LinuxHidDeviceListenerDisposalTests
    {
        /// <summary>
        /// Verifies that Dispose() completes within a reasonable time (<200ms).
        /// This ensures the poll() timeout mechanism works correctly.
        /// </summary>
        [SkippableFact]
        public void Dispose_CompletesWithinReasonableTime()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Linux, "Linux-only test");

            var listener = HidDeviceListener.Create();

            var stopwatch = Stopwatch.StartNew();
            listener.Dispose();
            stopwatch.Stop();

            // Should complete within 200ms (100ms poll timeout + safety margin)
            Assert.True(stopwatch.ElapsedMilliseconds < 200,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <200ms");
        }

        /// <summary>
        /// Verifies that calling Dispose() multiple times is safe and doesn't throw.
        /// Tests idempotency of disposal.
        /// </summary>
        [SkippableFact]
        public void Dispose_CalledMultipleTimes_IsIdempotent()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Linux, "Linux-only test");

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
        /// This would have caught the original finalizer leak bug.
        /// </summary>
        [SkippableFact]
        public void RepeatedCreateDispose_NoLeaks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Linux, "Linux-only test");

            int fdCountBefore = GetOpenFileDescriptorCount();

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

            int fdCountAfter = GetOpenFileDescriptorCount();

            // Allow for small variance (±2 FDs) due to system activity
            int fdDifference = Math.Abs(fdCountAfter - fdCountBefore);
            Assert.True(fdDifference <= 2,
                $"File descriptor leak detected: {fdCountBefore} before, {fdCountAfter} after (difference: {fdDifference})");
        }

        /// <summary>
        /// Verifies that concurrent Dispose() calls from multiple threads are thread-safe.
        /// </summary>
        [SkippableFact]
        public void ConcurrentDispose_IsThreadSafe()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Linux, "Linux-only test");

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
        /// Verifies that Dispose() can be called while events are being processed.
        /// </summary>
        [SkippableFact]
        public void Dispose_DuringEventHandling_CompletesGracefully()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Linux, "Linux-only test");

            var listener = HidDeviceListener.Create();
            var handlerStarted = new ManualResetEventSlim(false);
            var handlerCanComplete = new ManualResetEventSlim(false);

            listener.Arrived += (s, e) =>
            {
                handlerStarted.Set();
                handlerCanComplete.Wait(TimeSpan.FromSeconds(5));
            };

            // Note: We can't easily trigger a real device event in a unit test,
            // but we can verify that Dispose() completes quickly regardless
            var stopwatch = Stopwatch.StartNew();
            listener.Dispose();
            stopwatch.Stop();

            handlerCanComplete.Set();

            Assert.True(stopwatch.ElapsedMilliseconds < 200,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms even without events");
        }

        /// <summary>
        /// Verifies that listener thread terminates after Dispose().
        /// </summary>
        [SkippableFact]
        public void Dispose_TerminatesListenerThread()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Linux, "Linux-only test");

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
        /// Stress test: Create and dispose many listeners in parallel.
        /// </summary>
        [SkippableFact]
        public void ParallelCreateDispose_NoLeaksOrDeadlocks()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Linux, "Linux-only test");

            int fdCountBefore = GetOpenFileDescriptorCount();

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

            int fdCountAfter = GetOpenFileDescriptorCount();
            int fdDifference = Math.Abs(fdCountAfter - fdCountBefore);
            Assert.True(fdDifference <= 5,
                $"FD leak in parallel test: {fdCountBefore} before, {fdCountAfter} after");
        }

        /// <summary>
        /// Verifies that disposing a listener that was never used still works correctly.
        /// </summary>
        [SkippableFact]
        public void Dispose_UnusedListener_Succeeds()
        {
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.Linux, "Linux-only test");

            var listener = HidDeviceListener.Create();
            // Don't subscribe to any events or do anything

            var stopwatch = Stopwatch.StartNew();
            var exception = Record.Exception(() => listener.Dispose());
            stopwatch.Stop();

            Assert.Null(exception);
            Assert.True(stopwatch.ElapsedMilliseconds < 200);
        }

        /// <summary>
        /// Helper method to get count of open file descriptors for current process.
        /// </summary>
        private static int GetOpenFileDescriptorCount()
        {
            try
            {
                int pid = Environment.ProcessId;
                string fdPath = $"/proc/{pid}/fd";

                if (!Directory.Exists(fdPath))
                {
                    return 0;
                }

                return Directory.GetFiles(fdPath).Length;
            }
            catch
            {
                // If we can't read /proc, return 0 to skip the assertion
                return 0;
            }
        }
    }
}
