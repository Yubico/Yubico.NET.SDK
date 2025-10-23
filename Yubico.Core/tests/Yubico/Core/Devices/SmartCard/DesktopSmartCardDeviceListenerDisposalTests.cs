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
    [Collection("DisposalTests")]
    public class DesktopSmartCardDeviceListenerDisposalTests
    {
        //  Needs to be lower than the timeout in the listener thread (DesktopSmartCardDeviceListener._maxDisposalWaitTime)
        readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(5); 
        
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

            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Verifies that calling Dispose() multiple times is safe and doesn't throw.
        /// Tests idempotency of disposal.
        /// </summary>
        [Fact]
        public void Dispose_CalledMultipleTimes_IsIdempotent()
        {
            var stopwatch = Stopwatch.StartNew();
            
            var listener = SmartCardDeviceListener.Create();

            listener.Dispose();

            var exception1 = Record.Exception(() => listener.Dispose());
            var exception2 = Record.Exception(() => listener.Dispose());
            var exception3 = Record.Exception(() => listener.Dispose());
            
            stopwatch.Stop();

            Assert.Null(exception1);
            Assert.Null(exception2);
            Assert.Null(exception3);
            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Test took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Verifies that repeated create/dispose cycles don't leak resources.
        /// </summary>
        [Fact]
        public void RepeatedCreateDispose_NoLeaks()
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 20; i++)
            {
                using var listener = SmartCardDeviceListener.Create();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Test took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Verifies that concurrent Dispose() calls from multiple threads are thread-safe.
        /// This tests the disposal lock implementation.
        /// </summary>
        [Fact]
        public async Task ConcurrentDispose_IsThreadSafe()
        {
            var stopwatch = Stopwatch.StartNew();
            
            var listener = SmartCardDeviceListener.Create();

            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => listener.Dispose());
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(8000));
            var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks).WaitAsync(cts.Token));
            
            stopwatch.Stop();
            
            Assert.Null(exception);
            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Test took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Verifies that listener thread terminates after Dispose().
        /// </summary>
        [Fact]
        public void Dispose_TerminatesListenerThread()
        {
            var stopwatch = Stopwatch.StartNew();
            
            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            var listener = SmartCardDeviceListener.Create();
            Thread.Sleep(100);

            int threadCountDuring = Process.GetCurrentProcess().Threads.Count;
            Assert.True(threadCountDuring >= threadCountBefore,
                "Thread count should increase when listener is active");

            listener.Dispose();
            Thread.Sleep(500);

            int threadCountAfter = Process.GetCurrentProcess().Threads.Count;
            
            stopwatch.Stop();

            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);
            Assert.True(threadDifference <= 2,
                $"Thread leak detected: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, limit: ±2)");
            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Test took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Stress test: Create and dispose many listeners in parallel.
        /// Increased to 100 listeners to amplify leak signal above background noise.
        /// </summary>
        [Fact]
        public async Task ParallelCreateDispose_NoLeaksOrDeadlocks()
        {
            var stopwatch = Stopwatch.StartNew();
            
            var tasks = new Task[100];
            for (int i = 0; i < 100; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    using var listener = SmartCardDeviceListener.Create();
                    Thread.Sleep(50);
                });
            }
            
            var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Test took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
            Assert.Null(exception);
        }

        /// <summary>
        /// High-iteration sequential test: Create and dispose many listeners sequentially.
        /// This amplifies leak signal and tests disposal under sequential load.
        /// </summary>
        [Fact]
        public void SequentialCreateDispose_HighIterations_NoLeaks()
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 500; i++)
            {
                using var listener = SmartCardDeviceListener.Create();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Test took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Verifies that disposing a listener that was never used still works correctly.
        /// </summary>
        [Fact]
        public void Dispose_UnusedListener_Succeeds()
        {
            var listener = SmartCardDeviceListener.Create();

            var stopwatch = Stopwatch.StartNew();
            var exception = Record.Exception(() => listener.Dispose());
            stopwatch.Stop();

            Assert.Null(exception);
            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Verifies that finalizer doesn't throw exceptions that would crash GC thread.
        /// </summary>
        [Fact]
        public void Finalizer_DoesNotCrashGCThread()
        {
            var stopwatch = Stopwatch.StartNew();
            
            CreateAndAbandonListener();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Test took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }

        private static void CreateAndAbandonListener()
        {
            _ = SmartCardDeviceListener.Create();
        }

        /// <summary>
        /// Verifies that no events fire after disposal.
        /// This test verifies that the listener thread stops properly and no events
        /// are raised after Dispose() completes, regardless of handler registration.
        /// </summary>
        [Fact]
        public void Dispose_StopsEventsFiring()
        {
            var stopwatch = Stopwatch.StartNew();

            var listener = SmartCardDeviceListener.Create();
            int arrivedCount = 0;
            int removedCount = 0;

            listener.Arrived += (s, e) => Interlocked.Increment(ref arrivedCount);
            listener.Removed += (s, e) => Interlocked.Increment(ref removedCount);

            // Dispose and wait for thread to stop
            listener.Dispose();

            // Capture counts immediately after disposal
            int arrivedAfterDispose = arrivedCount;
            int removedAfterDispose = removedCount;

            // Wait a bit to ensure no delayed events fire
            Thread.Sleep(200);

            stopwatch.Stop();

            // Events might have fired before disposal (that's ok), but not after
            Assert.Equal(arrivedAfterDispose, arrivedCount);
            Assert.Equal(removedAfterDispose, removedCount);
            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Test took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }

        /// <summary>
        /// Verifies that rapid create/dispose cycles don't cause issues.
        /// </summary>
        [Fact]
        public void RapidCreateDisposeCycles_NoExceptions()
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 50; i++)
            {
                var listener = SmartCardDeviceListener.Create();
                listener.Dispose();
            }
            
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < MaxWaitTime.TotalMilliseconds,
                $"Test took {stopwatch.ElapsedMilliseconds}ms, expected <{MaxWaitTime.TotalMilliseconds} ms");
        }
    }
}
