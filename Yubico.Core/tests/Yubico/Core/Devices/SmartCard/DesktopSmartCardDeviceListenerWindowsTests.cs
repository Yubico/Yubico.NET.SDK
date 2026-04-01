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

// Track A — Windows integration test for GitHub issue #434.
//
// PURPOSE
// -------
// The Track B mock tests (DesktopSmartCardDeviceListenerSCardErrorTests.cs) prove that the
// managed polling loop is throttled after the fix. They do NOT exercise the actual CPU-intensive
// mechanism reported in the bug: WinSCard.dll internally raising and unwinding a C++ exception
// (CxxThrowException / RtlRaiseException / RtlUnwindEx) for every call made with an invalid
// SCARDCONTEXT handle.
//
// This file contains tests that close that gap by:
//   1. Creating a real listener backed by the production SCardInterop / WinSCard.dll.
//   2. Programmatically invalidating the SCARDCONTEXT handle via SCardReleaseContext, which
//      produces exactly the same invalid-handle condition that an RDS session disconnect creates.
//   3. Measuring real CPU consumption (Process.TotalProcessorTime) to prove the symptom
//      (pegged CPU core) exists before the fix and is eliminated after the fix.
//
// REQUIREMENTS
// ------------
// - Windows host (any edition with Smart Card service running — no physical reader needed).
// - The Smart Card service (SCardSvr) must be in Running state. It is enabled by default on
//   Windows 10/11 and Windows Server. If disabled, SCardEstablishContext will fail and the
//   listener will enter dormant/Error status — the tests will skip gracefully.
// - Run in isolation: the CPU measurement is sensitive to concurrent test thread activity.
//   The [Collection("WindowsOnlyTests")] attribute ensures xUnit serializes these tests.
//
// HOW TO RUN ON YOUR WINDOWS MACHINE
// ------------------------------------
//   dotnet test Yubico.Core/tests/Yubico.Core.UnitTests.csproj
//       --filter "FullyQualifiedName~DesktopSmartCardDeviceListenerWindowsTests"
//       --logger "console;verbosity=detailed"

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard.UnitTests
{
    [Collection("WindowsOnlyTests")]
    public class DesktopSmartCardDeviceListenerWindowsTests
    {
        // ─────────────────────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the SCARDCONTEXT handle held by a running listener via reflection.
        /// </summary>
        private static SCardContext GetListenerContext(SmartCardDeviceListener listener)
        {
            var field = listener.GetType()
                .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException(
                    "_context field not found — listener type may have changed.");

            return (SCardContext)(field.GetValue(listener)
                ?? throw new InvalidOperationException("_context is null."));
        }

        /// <summary>
        /// Invalidates the SCARDCONTEXT handle the listener is actively polling against.
        /// This is exactly what happens when a Windows RDS session is disconnected:
        /// the Smart Card Service invalidates all existing context handles for that session.
        /// </summary>
        private static void InvalidateListenerContext(SmartCardDeviceListener listener)
        {
            SCardContext context = GetListenerContext(listener);
            // SCardReleaseContext with the raw IntPtr tells WinSCard the handle is gone.
            // Subsequent SCardGetStatusChange calls using this handle will fail immediately
            // with SCARD_E_INVALID_HANDLE and trigger WinSCard's internal C++ exception path.
            uint result = NativeMethods.SCardReleaseContext(context.DangerousGetHandle());
            Skip.If(result != ErrorCode.SCARD_S_SUCCESS,
                $"SCardReleaseContext failed with 0x{result:X8}; context may already be invalid or disposed. Skipping test.");
        }

        /// <summary>
        /// Returns true if the listener successfully established a Smart Card context.
        /// If SCardSvr is not running, the listener enters Error/dormant status and
        /// the tests should be skipped rather than fail.
        /// </summary>
        private static bool ListenerIsActive(SmartCardDeviceListener listener) =>
            listener.Status == DeviceListenerStatus.Started;

        // ─────────────────────────────────────────────────────────────────────────────────────
        // Test 1: CPU measurement — the gold standard for issue #434
        //
        // This test FAILS before the fix is applied and PASSES after.
        //
        // Before fix: SCARD_E_INVALID_HANDLE unhandled → loop spins at thousands/sec →
        //   each spin calls WinSCard with invalid handle → WinSCard raises C++ exception
        //   internally → CxxThrowException / RtlUnwindEx machinery runs → CPU pegged.
        //   TotalProcessorTime over 3s: > 2000ms (one core pegged).
        //
        // After fix: SCARD_E_INVALID_HANDLE handled → UpdateCurrentContext() called →
        //   Thread.Sleep(1000) back-off applied → ~1 call/sec → CxxThrowException fires
        //   at most once per second → negligible CPU.
        //   TotalProcessorTime over 3s: < 500ms.
        // ─────────────────────────────────────────────────────────────────────────────────────

        [SkippableFact]
        [Trait("Category", "WindowsOnly")]
        [Trait("Category", "CpuRegression")]
        public void RealWinSCard_WhenHandleInvalidated_CpuDoesNotSpike()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "This test requires WinSCard.dll and is only valid on Windows.");

            using var listener = SmartCardDeviceListener.Create();

            Skip.IfNot(ListenerIsActive(listener),
                "Smart Card service (SCardSvr) is not running on this machine. " +
                "Enable the service and re-run.");

            // Let the listener settle into its normal 100ms polling cadence.
            Thread.Sleep(300);

            // Invalidate the handle — simulates RDS session disconnect.
            InvalidateListenerContext(listener);

            // Measure CPU consumption over the observation window.
            // The process should be otherwise idle during this window.
            var cpuBefore = Process.GetCurrentProcess().TotalProcessorTime;
            const int observationWindowMs = 3000;
            Thread.Sleep(observationWindowMs);
            var cpuAfter = Process.GetCurrentProcess().TotalProcessorTime;

            var cpuConsumedMs = (cpuAfter - cpuBefore).TotalMilliseconds;

            // Threshold: 500ms CPU in 3000ms wall-clock.
            // With fix:    1 retry/sec × (cheap EstablishContext + 1000ms sleep) ≈ 30–100ms
            // Without fix: core pegged ≈ 2500–3000ms
            // Headroom:    10× between expected-good and expected-bad.
            Assert.True(
                cpuConsumedMs < 500,
                $"CPU consumed {cpuConsumedMs:F0}ms in {observationWindowMs}ms wall-clock after " +
                "handle invalidation. Expected < 500ms. " +
                "This is the high-CPU symptom from GitHub issue #434: " +
                "WinSCard raises a C++ exception (CxxThrowException) for every call " +
                "made with an invalid SCARDCONTEXT handle. " +
                "The fix must add a backoff after SCARD_E_INVALID_HANDLE to reduce the call rate.");
        }

        // ─────────────────────────────────────────────────────────────────────────────────────
        // Test 2: Recovery — context re-establishment with real WinSCard
        //
        // After invalidating the handle, the listener must re-establish a fresh SCARDCONTEXT.
        // Verifies that the new handle is different from (and valid, unlike) the old one.
        // This test is complementary to the CPU test: it proves the listener recovers
        // functionally, not just stops spinning.
        // ─────────────────────────────────────────────────────────────────────────────────────

        [SkippableFact]
        [Trait("Category", "WindowsOnly")]
        public void RealWinSCard_WhenHandleInvalidated_NewContextIsEstablished()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "This test requires WinSCard.dll and is only valid on Windows.");

            using var listener = SmartCardDeviceListener.Create();

            Skip.IfNot(ListenerIsActive(listener),
                "Smart Card service (SCardSvr) is not running on this machine.");

            Thread.Sleep(300);

            // Capture handle value before invalidation.
            IntPtr originalHandle = GetListenerContext(listener).DangerousGetHandle();

            // Invalidate.
            InvalidateListenerContext(listener);

            // Give the listener time to detect SCARD_E_INVALID_HANDLE, call
            // UpdateCurrentContext (EstablishContext), sleep 1000ms, and continue.
            Thread.Sleep(2500);

            // The listener should have replaced _context with a new valid handle.
            SCardContext newContext = GetListenerContext(listener);

            Assert.False(
                newContext.IsInvalid,
                "The new SCARDCONTEXT handle is invalid. " +
                "UpdateCurrentContext must have called SCardEstablishContext and stored the result.");

            Assert.NotEqual(
                originalHandle,
                newContext.DangerousGetHandle(),
                "The SCARDCONTEXT handle is unchanged after invalidation. " +
                "Expected a fresh handle from a new SCardEstablishContext call.");

            // Listener must still be polling normally — not in Error state.
            Assert.Equal(DeviceListenerStatus.Started, listener.Status);
        }

        // ─────────────────────────────────────────────────────────────────────────────────────
        // Test 3: Dispose safety after handle invalidation
        //
        // After the handle is invalidated and the recovery path fires, Dispose must still
        // complete cleanly within a reasonable time (SCardCancel on the new context,
        // StopListening, context.Dispose). Regression guard: this was a secondary risk
        // identified in the Opus Engineer review (thread safety race with _context replacement).
        // ─────────────────────────────────────────────────────────────────────────────────────

        [SkippableFact]
        [Trait("Category", "WindowsOnly")]
        public void RealWinSCard_WhenHandleInvalidatedThenDisposed_DisposalCompletesCleanly()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "This test requires WinSCard.dll and is only valid on Windows.");

            var listener = SmartCardDeviceListener.Create();

            try
            {
                Skip.IfNot(ListenerIsActive(listener),
                    "Smart Card service (SCardSvr) is not running on this machine.");

                Thread.Sleep(300);
                InvalidateListenerContext(listener);

                // Let recovery fire once (1000ms sleep inside the listener thread).
                Thread.Sleep(1500);

                // Now dispose — must complete well within 8 seconds.
                var stopwatch = Stopwatch.StartNew();
                var exception = Record.Exception(() => listener.Dispose());
                stopwatch.Stop();

                Assert.Null(exception);
                Assert.True(
                    stopwatch.ElapsedMilliseconds < 5000,
                    $"Dispose took {stopwatch.ElapsedMilliseconds}ms after handle invalidation. " +
                    "Expected < 5000ms. The listener thread may be blocked in the recovery sleep.");
            }
            finally
            {
                listener.Dispose();
            }
        }
    }
}
