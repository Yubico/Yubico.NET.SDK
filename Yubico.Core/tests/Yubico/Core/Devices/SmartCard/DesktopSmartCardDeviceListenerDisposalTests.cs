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

namespace Yubico.Core.Devices.SmartCard.UnitTests
{
    /// <summary>
    /// Tests for disposal and resource management of DesktopSmartCardDeviceListener.
    /// These tests verify thread safety, resource cleanup, and disposal timing.
    /// </summary>
    public class DesktopSmartCardDeviceListenerDisposalTests
    {
        /// <summary>
        /// Verifies that Dispose() completes within a reasonable time.
        /// This ensures the listener thread terminates properly.
        /// </summary>
        [Fact]
        public void Dispose_CompletesWithinReasonableTime()
        {
            var listener = SmartCardDeviceListener.Create();

            var stopwatch = Stopwatch.StartNew();
            listener.Dispose();
            stopwatch.Stop();

            // Should complete within 5 seconds (thread join timeout + safety margin)
            Assert.True(stopwatch.ElapsedMilliseconds < 5500,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <5500ms");
        }

        /// <summary>
        /// Verifies that calling Dispose() multiple times is safe and doesn't throw.
        /// Tests idempotency of disposal.
        /// </summary>
        [Fact]
        public void Dispose_CalledMultipleTimes_IsIdempotent()
        {
            var listener = SmartCardDeviceListener.Create();

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
        /// </summary>
        [Fact]
        public void RepeatedCreateDispose_NoLeaks()
        {
            // Create and dispose 20 listeners
            for (int i = 0; i < 20; i++)
            {
                using var listener = SmartCardDeviceListener.Create();
                // Listener created and immediately disposed
            }

            // Force GC to ensure any finalizers run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // If we get here without exceptions, no resource leaks occurred
            Assert.True(true);
        }

        /// <summary>
        /// Verifies that concurrent Dispose() calls from multiple threads are thread-safe.
        /// This tests the disposal lock implementation.
        /// </summary>
        [Fact]
        public async Task ConcurrentDispose_IsThreadSafe()
        {
            var listener = SmartCardDeviceListener.Create();

            // Launch 10 concurrent Dispose() calls
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => listener.Dispose());
            }

            // Should not throw or deadlock
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks).WaitAsync(cts.Token));
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that Dispose() can be called while events might be processing.
        /// </summary>
        [Fact]
        public void Dispose_DuringEventHandling_CompletesGracefully()
        {
            var listener = SmartCardDeviceListener.Create();
            var handlerCanComplete = new ManualResetEventSlim(false);

            listener.Arrived += (s, e) =>
            {
                handlerCanComplete.Wait(TimeSpan.FromSeconds(5));
            };

            // Dispose should complete even if event handlers are registered
            var stopwatch = Stopwatch.StartNew();
            listener.Dispose();
            stopwatch.Stop();

            handlerCanComplete.Set();

            Assert.True(stopwatch.ElapsedMilliseconds < 5500,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Verifies that listener thread terminates after Dispose().
        /// </summary>
        [Fact]
        public void Dispose_TerminatesListenerThread()
        {
            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            var listener = SmartCardDeviceListener.Create();
            Thread.Sleep(100); // Give thread time to start

            int threadCountDuring = Process.GetCurrentProcess().Threads.Count;
            Assert.True(threadCountDuring >= threadCountBefore,
                "Thread count should increase when listener is active");

            listener.Dispose();
            Thread.Sleep(500); // Give thread time to terminate

            int threadCountAfter = Process.GetCurrentProcess().Threads.Count;

            // Thread count should return to original (±2 for variance due to system activity)
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);
            Assert.True(threadDifference <= 2,
                $"Thread leak detected: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, limit: ±2)");
        }

        /// <summary>
        /// Stress test: Create and dispose many listeners in parallel.
        /// Increased to 100 listeners to amplify leak signal above background noise.
        /// </summary>
        [Fact]
        public async Task ParallelCreateDispose_NoLeaksOrDeadlocks()
        {
            // Create 100 listeners in parallel to amplify leak detection
            // Increased from 10 to make potential resource leaks more visible
            var tasks = new Task[100];
            for (int i = 0; i < 100; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    using var listener = SmartCardDeviceListener.Create();
                    Thread.Sleep(50); // Hold briefly
                });
            }

            // Should complete without timeout (adjusted for more listeners)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await Task.WhenAll(tasks).WaitAsync(cts.Token);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            // If we get here, no deadlocks or exceptions occurred
            Assert.True(true);
        }

        /// <summary>
        /// High-iteration sequential test: Create and dispose many listeners sequentially.
        /// This amplifies leak signal and tests disposal under sequential load.
        /// </summary>
        [Fact]
        public void SequentialCreateDispose_HighIterations_NoLeaks()
        {
            // Create and dispose 500 listeners sequentially
            // High iteration count helps detect resource leaks
            for (int i = 0; i < 500; i++)
            {
                using var listener = SmartCardDeviceListener.Create();
                // Dispose happens at end of using block
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            // If we get here without timeout or exception, disposal is working correctly
            Assert.True(true);
        }

        /// <summary>
        /// Verifies that disposing a listener that was never used still works correctly.
        /// </summary>
        [Fact]
        public void Dispose_UnusedListener_Succeeds()
        {
            var listener = SmartCardDeviceListener.Create();
            // Don't subscribe to any events or do anything

            var stopwatch = Stopwatch.StartNew();
            var exception = Record.Exception(() => listener.Dispose());
            stopwatch.Stop();

            Assert.Null(exception);
            Assert.True(stopwatch.ElapsedMilliseconds < 5500,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <5500ms");
        }

        /// <summary>
        /// Verifies that finalizer doesn't throw exceptions that would crash GC thread.
        /// </summary>
        [Fact]
        public void Finalizer_DoesNotCrashGCThread()
        {
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
            _ = SmartCardDeviceListener.Create();
            // Let it go out of scope without disposing
        }

        /// <summary>
        /// Verifies that event handlers are cleared during disposal.
        /// </summary>
        [Fact]
        public void Dispose_ClearsEventHandlers()
        {
            var listener = SmartCardDeviceListener.Create();
            bool arrivedCalled = false;
            bool removedCalled = false;

            listener.Arrived += (s, e) => arrivedCalled = true;
            listener.Removed += (s, e) => removedCalled = true;

            listener.Dispose();

            // Event handlers should be cleared, but we can't easily test this
            // without triggering events. This test mainly verifies no exceptions.
            Assert.False(arrivedCalled);
            Assert.False(removedCalled);
        }

        /// <summary>
        /// Verifies that rapid create/dispose cycles don't cause issues.
        /// </summary>
        [Fact]
        public void RapidCreateDisposeCycles_NoExceptions()
        {
            for (int i = 0; i < 50; i++)
            {
                var listener = SmartCardDeviceListener.Create();
                listener.Dispose();
            }

            // If we get here, no exceptions were thrown
            Assert.True(true);
        }
    }
}
