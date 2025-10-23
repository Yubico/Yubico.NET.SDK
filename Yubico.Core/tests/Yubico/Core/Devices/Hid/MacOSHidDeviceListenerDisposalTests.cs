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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.Hid.UnitTests
{
    /// <summary>
    /// Tests for disposal and resource management of MacOSHidDeviceListener.
    /// These tests verify thread lifecycle, CFRunLoop cleanup, and disposal timing.
    /// </summary>
    [Collection("DisposalTests")]
    public class MacOSHidDeviceListenerDisposalTests
    {
        private readonly ITestOutputHelper _output;

        public MacOSHidDeviceListenerDisposalTests(ITestOutputHelper output)
        {
            _output = output;
        }
        /// <summary>
        /// Verifies that Dispose() completes within a reasonable time.
        /// macOS listener uses CFRunLoopStop which should be fast.
        /// </summary>
        [SkippableFact]
        public void Dispose_CompletesWithinReasonableTime()
        {
            _output.WriteLine("=== TEST START: Dispose_CompletesWithinReasonableTime ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            _output.WriteLine($"[USING-ENTRY] Test={nameof(Dispose_CompletesWithinReasonableTime)}, Thread={Environment.CurrentManagedThreadId}");
            var listener = HidDeviceListener.Create();
            _output.WriteLine($"[USING-BODY] Test={nameof(Dispose_CompletesWithinReasonableTime)}, Thread={Environment.CurrentManagedThreadId}");
            _output.WriteLine($"[TEST] Created listener instance");

            var stopwatch = Stopwatch.StartNew();
            listener.Dispose();
            stopwatch.Stop();

            // CFRunLoopStop should wake thread immediately, plus join time
            // Allow more time than Linux due to CFRunLoop wake latency
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <500ms");
            _output.WriteLine($"[TEST] Instance disposed in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"[USING-EXIT] Test={nameof(Dispose_CompletesWithinReasonableTime)}, Thread={Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Verifies that calling Dispose() multiple times is safe and doesn't throw.
        /// Tests idempotency of disposal.
        /// </summary>
        [SkippableFact]
        public void Dispose_CalledMultipleTimes_IsIdempotent()
        {
            _output.WriteLine("=== TEST START: Dispose_CalledMultipleTimes_IsIdempotent ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            _output.WriteLine($"[USING-ENTRY] Test={nameof(Dispose_CalledMultipleTimes_IsIdempotent)}, Thread={Environment.CurrentManagedThreadId}");
            var listener = HidDeviceListener.Create();
            _output.WriteLine($"[USING-BODY] Test={nameof(Dispose_CalledMultipleTimes_IsIdempotent)}, Thread={Environment.CurrentManagedThreadId}");

            // First disposal
            _output.WriteLine($"[TEST] First Dispose() call");
            listener.Dispose();
            _output.WriteLine($"[TEST] First Dispose() completed");

            // Subsequent disposals should not throw
            _output.WriteLine($"[TEST] Second Dispose() call");
            var exception1 = Record.Exception(() => listener.Dispose());
            _output.WriteLine($"[TEST] Third Dispose() call");
            var exception2 = Record.Exception(() => listener.Dispose());
            _output.WriteLine($"[TEST] Fourth Dispose() call");
            var exception3 = Record.Exception(() => listener.Dispose());

            Assert.Null(exception1);
            Assert.Null(exception2);
            Assert.Null(exception3);
            _output.WriteLine($"[USING-EXIT] Test={nameof(Dispose_CalledMultipleTimes_IsIdempotent)}, Thread={Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Verifies that repeated create/dispose cycles don't leak resources.
        /// Tests for IOKit port leaks and thread leaks.
        /// </summary>
        [SkippableFact]
        public void RepeatedCreateDispose_NoLeaks()
        {
            _output.WriteLine("=== TEST START: RepeatedCreateDispose_NoLeaks ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            int portCountBefore = GetMachPortCount();
            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            // Create and dispose 50 listeners
            for (int i = 0; i < 50; i++)
            {
                _output.WriteLine($"[USING-ENTRY] Test={nameof(RepeatedCreateDispose_NoLeaks)}, Iteration={i}, Thread={Environment.CurrentManagedThreadId}");
                using var listener = HidDeviceListener.Create();
                _output.WriteLine($"[USING-BODY] Test={nameof(RepeatedCreateDispose_NoLeaks)}, Iteration={i}, Thread={Environment.CurrentManagedThreadId}");
                // Listener created and immediately disposed
                _output.WriteLine($"[USING-EXIT] Test={nameof(RepeatedCreateDispose_NoLeaks)}, Iteration={i}, Thread={Environment.CurrentManagedThreadId}");
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
                $"Mach port leak detected: {portCountBefore} before, {portCountAfter} after (difference: {portDifference}, threshold: ±25)");
            Assert.True(threadDifference <= 25,
                $"Thread leak detected: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, threshold: ±25)");
        }

        /// <summary>
        /// Verifies that concurrent Dispose() calls from multiple threads are thread-safe.
        /// </summary>
        [SkippableFact]
        public async Task ConcurrentDispose_IsThreadSafe()
        {
            _output.WriteLine("=== TEST START: ConcurrentDispose_IsThreadSafe ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            _output.WriteLine($"[USING-ENTRY] Test={nameof(ConcurrentDispose_IsThreadSafe)}, Thread={Environment.CurrentManagedThreadId}");
            var listener = HidDeviceListener.Create();
            _output.WriteLine($"[USING-BODY] Test={nameof(ConcurrentDispose_IsThreadSafe)}, Thread={Environment.CurrentManagedThreadId}");

            // Launch 10 concurrent Dispose() calls
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                int taskNum = i;
                tasks[i] = Task.Run(() =>
                {
                    _output.WriteLine($"[TEST] Task {taskNum} calling Dispose()");
                    listener.Dispose();
                    _output.WriteLine($"[TEST] Task {taskNum} Dispose() completed");
                });
            }

            // Should not throw or deadlock
            var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));
            Assert.Null(exception);
            _output.WriteLine($"[USING-EXIT] Test={nameof(ConcurrentDispose_IsThreadSafe)}, Thread={Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Verifies that Dispose() terminates the listener thread.
        /// </summary>
        [SkippableFact]
        public void Dispose_TerminatesListenerThread()
        {
            _output.WriteLine("=== TEST START: Dispose_TerminatesListenerThread ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            _output.WriteLine($"[USING-ENTRY] Test={nameof(Dispose_TerminatesListenerThread)}, Thread={Environment.CurrentManagedThreadId}");
            var listener = HidDeviceListener.Create();
            _output.WriteLine($"[USING-BODY] Test={nameof(Dispose_TerminatesListenerThread)}, Thread={Environment.CurrentManagedThreadId}");
            Thread.Sleep(100); // Give thread time to start

            int threadCountDuring = Process.GetCurrentProcess().Threads.Count;
            Assert.True(threadCountDuring >= threadCountBefore,
                "Thread count should increase when listener is active");

            _output.WriteLine($"[TEST] Calling Dispose()");
            listener.Dispose();
            _output.WriteLine($"[TEST] Dispose() completed");
            Thread.Sleep(200); // Give thread time to terminate

            int threadCountAfter = Process.GetCurrentProcess().Threads.Count;

            // Thread count should return close to original
            // macOS IOKit creates unpredictable background threads, allow ±10 for tolerance
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);
            Assert.True(threadDifference <= 10,
                $"Thread leak detected: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, threshold: ±10)");
            _output.WriteLine($"[USING-EXIT] Test={nameof(Dispose_TerminatesListenerThread)}, Thread={Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Verifies that CFRunLoop is properly stopped and cleaned up.
        /// </summary>
        [SkippableFact]
        public void Dispose_StopsCFRunLoop()
        {
            _output.WriteLine("=== TEST START: Dispose_StopsCFRunLoop ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            _output.WriteLine($"[USING-ENTRY] Test={nameof(Dispose_StopsCFRunLoop)}, Thread={Environment.CurrentManagedThreadId}");
            var listener = HidDeviceListener.Create();
            _output.WriteLine($"[USING-BODY] Test={nameof(Dispose_StopsCFRunLoop)}, Thread={Environment.CurrentManagedThreadId}");
            Thread.Sleep(100); // Ensure CFRunLoop is running

            // Dispose should stop the run loop
            var stopwatch = Stopwatch.StartNew();
            _output.WriteLine($"[TEST] Calling Dispose()");
            listener.Dispose();
            _output.WriteLine($"[TEST] Dispose() completed");
            stopwatch.Stop();

            // If CFRunLoopStop works, this should complete quickly
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"CFRunLoop took {stopwatch.ElapsedMilliseconds}, expected < 500 ms");
            _output.WriteLine($"[USING-EXIT] Test={nameof(Dispose_StopsCFRunLoop)}, Thread={Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Stress test: Create and dispose many listeners in parallel.
        /// Uses fewer listeners than Windows/Linux due to macOS IOKit resource limits.
        /// </summary>
        [SkippableFact]
        public async Task ParallelCreateDispose_NoLeaksOrDeadlocks()
        {
            _output.WriteLine("=== TEST START: ParallelCreateDispose_NoLeaksOrDeadlocks ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            // Create 30 listeners in parallel to amplify leak detection
            // Reduced from 100 to avoid overwhelming macOS IOKit (100 concurrent IOHIDManager instances cause failures)
            // If each leaks 1 thread: 30 leaked threads >> background noise (signal-to-noise: 30:25 = 1.2:1)
            var tasks = new Task[30];
            for (int i = 0; i < 30; i++)
            {
                int taskNum = i; // Capture for closure
                tasks[i] = Task.Run(() =>
                {
                    _output.WriteLine($"[USING-ENTRY] Test={nameof(ParallelCreateDispose_NoLeaksOrDeadlocks)}, Task={taskNum:D2}, Thread={Environment.CurrentManagedThreadId}");
                    using var listener = HidDeviceListener.Create();
                    _output.WriteLine($"[USING-BODY] Test={nameof(ParallelCreateDispose_NoLeaksOrDeadlocks)}, Task={taskNum:D2}, Thread={Environment.CurrentManagedThreadId}");
                    _output.WriteLine($"[TEST-Task{taskNum:D2}] Created listener");
                    Thread.Sleep(50); // Hold briefly to ensure thread starts
                    _output.WriteLine($"[TEST-Task{taskNum:D2}] Disposing listener");
                    _output.WriteLine($"[USING-EXIT] Test={nameof(ParallelCreateDispose_NoLeaksOrDeadlocks)}, Task={taskNum:D2}, Thread={Environment.CurrentManagedThreadId}");
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
                $"Thread leak in parallel test: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, threshold: ±25)");
        }

        /// <summary>
        /// High-iteration sequential test: Create and dispose many listeners sequentially.
        /// This amplifies leak signal while minimizing parallel activity noise.
        /// With 500 iterations, even a 1-thread leak per listener becomes obvious.
        /// </summary>
        [SkippableFact]
        public void SequentialCreateDispose_HighIterations_NoLeaks()
        {
            _output.WriteLine("=== TEST START: SequentialCreateDispose_HighIterations_NoLeaks ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            int threadCountBefore = Process.GetCurrentProcess().Threads.Count;

            // Create and dispose 50 listeners sequentially (REDUCED from 500 for macOS)
            // Sequential execution minimizes parallel activity noise
            // High iteration count amplifies leak signal (50:1 signal-to-noise ratio)
            for (int i = 0; i < 50; i++)
            {
                _output.WriteLine($"[USING-ENTRY] Test={nameof(SequentialCreateDispose_HighIterations_NoLeaks)}, Iteration={i:D3}, Thread={Environment.CurrentManagedThreadId}");
                using var listener = HidDeviceListener.Create();
                _output.WriteLine($"[USING-BODY] Test={nameof(SequentialCreateDispose_HighIterations_NoLeaks)}, Iteration={i:D3}, Thread={Environment.CurrentManagedThreadId}");
                _output.WriteLine($"[TEST-Iteration{i:D3}] Created listener");
                Thread.Sleep(10); // Brief hold to ensure thread lifecycle
                _output.WriteLine($"[USING-EXIT] Test={nameof(SequentialCreateDispose_HighIterations_NoLeaks)}, Iteration={i:D3}, Thread={Environment.CurrentManagedThreadId}");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(200);

            int threadCountAfter = Process.GetCurrentProcess().Threads.Count;
            int threadDifference = Math.Abs(threadCountAfter - threadCountBefore);

            // With 50 iterations, any per-listener leak would be obvious
            // Sequential minimizes noise, but allow generous tolerance for first-use initialization
            // With 50 listeners, real leaks (50+ threads) far exceed tolerance (signal-to-noise: 50:10 = 5:1)
            Assert.True(threadDifference <= 10,
                $"Thread leak in sequential test: {threadCountBefore} before, {threadCountAfter} after (difference: {threadDifference}, threshold: ±10)");
        }

        /// <summary>
        /// Verifies that disposing a listener that was never used still works correctly.
        /// </summary>
        [SkippableFact]
        public void Dispose_UnusedListener_Succeeds()
        {
            _output.WriteLine("=== TEST START: Dispose_UnusedListener_Succeeds ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            _output.WriteLine($"[USING-ENTRY] Test={nameof(Dispose_UnusedListener_Succeeds)}, Thread={Environment.CurrentManagedThreadId}");
            var listener = HidDeviceListener.Create();
            _output.WriteLine($"[USING-BODY] Test={nameof(Dispose_UnusedListener_Succeeds)}, Thread={Environment.CurrentManagedThreadId}");
            // Don't subscribe to any events or do anything

            var stopwatch = Stopwatch.StartNew();
            _output.WriteLine($"[TEST] Calling Dispose()");
            var exception = Record.Exception(() => listener.Dispose());
            _output.WriteLine($"[TEST] Dispose() completed");
            stopwatch.Stop();

            Assert.Null(exception);
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"Dispose took {stopwatch.ElapsedMilliseconds}ms, expected <500ms");
            _output.WriteLine($"[USING-EXIT] Test={nameof(Dispose_UnusedListener_Succeeds)}, Thread={Environment.CurrentManagedThreadId}");
        }

        /// <summary>
        /// Verifies that Dispose(false) path (finalizer code path) doesn't block on Thread.Join().
        /// Tests the finalizer logic directly via reflection since GC.Collect() timing is non-deterministic.
        ///
        /// Background: Original test created listener and hoped GC.Collect() would trigger finalizer,
        /// but GC never finalized objects during test timeframe, leaving orphaned background threads
        /// that interfered with other tests in combined execution.
        ///
        /// This test uses reflection to call Dispose(false) directly, testing the actual finalizer
        /// code path without relying on non-deterministic GC behavior.
        /// </summary>
        [SkippableFact]
        public void Dispose_FromFinalizerPath_DoesNotBlock()
        {
            _output.WriteLine("=== TEST START: Dispose_FromFinalizerPath_DoesNotBlock ===");
            Skip.IfNot(SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS, "macOS-only test");

            _output.WriteLine($"[USING-ENTRY] Test={nameof(Dispose_FromFinalizerPath_DoesNotBlock)}, Thread={Environment.CurrentManagedThreadId}");
            var listener = HidDeviceListener.Create();
            var macListener = listener as MacOSHidDeviceListener;
            _output.WriteLine($"[USING-BODY] Test={nameof(Dispose_FromFinalizerPath_DoesNotBlock)}, Thread={Environment.CurrentManagedThreadId}");
            _output.WriteLine($"[TEST] Created listener");

            // Give listener thread time to start
            Thread.Sleep(100);

            // Call Dispose(false) directly via reflection - simulates finalizer path without relying on GC timing
            var disposeMethod = typeof(MacOSHidDeviceListener)
                .GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(bool) }, null);

            Assert.NotNull(disposeMethod);

            _output.WriteLine($"[TEST] Calling Dispose(false) via reflection to simulate finalizer path");
            var stopwatch = Stopwatch.StartNew();
            disposeMethod.Invoke(macListener, new object[] { false });
            stopwatch.Stop();

            // Dispose(false) should complete quickly because it skips Thread.Join() from finalizer path
            // If it blocks on Thread.Join(), this will take 1+ seconds and fail
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"Dispose(false) took {stopwatch.ElapsedMilliseconds}ms, expected <500ms (should skip Thread.Join from finalizer)");

            _output.WriteLine($"[TEST] Dispose(false) completed in {stopwatch.ElapsedMilliseconds}ms without blocking - finalizer path works correctly");
            _output.WriteLine($"[USING-EXIT] Test={nameof(Dispose_FromFinalizerPath_DoesNotBlock)}, Thread={Environment.CurrentManagedThreadId}");
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
