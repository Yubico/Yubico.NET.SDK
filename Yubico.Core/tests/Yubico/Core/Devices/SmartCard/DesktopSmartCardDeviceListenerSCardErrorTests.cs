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

// Reproduces GitHub issue #434:
// High idle CPU cost of enumerating devices in terminal server environments.
//
// Root cause: When an RDS session is disconnected, the Windows Smart Card Service invalidates
// existing SCARDCONTEXT handles. DesktopSmartCardDeviceListener continued to call
// SCardGetStatusChange with the stale handle, which internally raises and unwinds a C++
// exception thousands of times per second, pegging a CPU core.
//
// Reproduction mechanism: FakeSCardInterop returns SCARD_E_INVALID_HANDLE from GetStatusChange
// to simulate what WinSCard returns after an RDS handle invalidation. The fake does not require
// Windows or a real smart card reader, and runs on all CI platforms.

using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard.UnitTests
{
    [Collection("SCardErrorTests")]
    public class DesktopSmartCardDeviceListenerSCardErrorTests
    {
        // -----------------------------------------------------------------------------------------
        // Issue #434 — SCARD_E_INVALID_HANDLE causes tight loop and high CPU
        //
        // This test FAILS before the fix and PASSES after.
        // Before fix: SCARD_E_INVALID_HANDLE is not handled by UpdateContextIfNonCritical, so
        //             the listener logs the error and immediately retries, spinning at full speed.
        // After fix:  SCARD_E_INVALID_HANDLE triggers UpdateCurrentContext (re-establishes the
        //             SCARDCONTEXT) followed by Thread.Sleep(1000) to back off.
        // -----------------------------------------------------------------------------------------

        [Fact]
        public void WhenGetStatusChangeReturnsInvalidHandle_ContextIsReestablished()
            => AssertErrorTriggersContextReestablishment(
                ErrorCode.SCARD_E_INVALID_HANDLE,
                "SCARD_E_INVALID_HANDLE");

        // -----------------------------------------------------------------------------------------
        // Issue #434 — Proof that SCARD_E_INVALID_HANDLE causes a tight polling loop (high CPU)
        //
        // This test quantifies the spin rate. When SCARD_E_INVALID_HANDLE is returned on every
        // GetStatusChange call (simulating persistent handle invalidation as in RDS), the loop
        // must NOT spin freely. The Thread.Sleep(1000) backoff introduced by the fix limits the
        // rate to ~1 iteration per second.
        //
        // This test FAILS before the fix (spin -> hundreds of calls) and PASSES after.
        // -----------------------------------------------------------------------------------------

        [Fact]
        public void WhenGetStatusChangeAlwaysReturnsInvalidHandle_LoopDoesNotSpin()
        {
            // Arrange: all GetStatusChange calls (after probe) return SCARD_E_INVALID_HANDLE.
            // This simulates the worst case: handle remains invalid after each recovery attempt.
            var fake = new FakeSCardInterop(
                probeResult: ErrorCode.SCARD_E_TIMEOUT,
                defaultResult: ErrorCode.SCARD_E_INVALID_HANDLE);

            using var listener = new DesktopSmartCardDeviceListener(fake);

            // Act: observe for 600ms.
            // Without fix: INVALID_HANDLE is ignored, loop spins at max speed —
            //   expect hundreds of GetStatusChange calls in 600ms.
            // With fix: INVALID_HANDLE triggers recovery + Thread.Sleep(1000) —
            //   only 1–2 main poll calls fit in 600ms (probe + first main poll, then sleeping).
            Thread.Sleep(600);

            int callCount = fake.GetStatusChangeCallCount;

            // Assert: fewer than 15 calls in 600ms proves no tight loop.
            // With fix: expect ~2 (probe + first INVALID_HANDLE poll, then 1000ms sleep begins).
            // Without fix: expect hundreds (unthrottled spin).
            Assert.True(
                callCount < 15,
                $"GetStatusChange was called {callCount} times in ~600ms. " +
                "Expected < 15: SCARD_E_INVALID_HANDLE must not cause an unthrottled polling loop. " +
                "This is the high-CPU symptom reported in GitHub issue #434.");
        }

        // -----------------------------------------------------------------------------------------
        // Issue #434 — SCARD_E_SYSTEM_CANCELLED (RDS session disconnect/logoff) also recovers
        // -----------------------------------------------------------------------------------------

        [Fact]
        public void WhenGetStatusChangeReturnsSystemCancelled_ContextIsReestablished()
            => AssertErrorTriggersContextReestablishment(
                ErrorCode.SCARD_E_SYSTEM_CANCELLED,
                "SCARD_E_SYSTEM_CANCELLED (RDS logoff/disconnect)");

        // -----------------------------------------------------------------------------------------
        // Issue #434 — ERROR_BROKEN_PIPE (RDS smart card redirection not supported) also recovers
        // -----------------------------------------------------------------------------------------

        [Fact]
        public void WhenGetStatusChangeReturnsBrokenPipe_ContextIsReestablished()
            => AssertErrorTriggersContextReestablishment(
                ErrorCode.ERROR_BROKEN_PIPE,
                "ERROR_BROKEN_PIPE (RDS smart card redirection error)");

        /// <summary>
        /// Verifies that a given non-critical SCard error triggers context re-establishment.
        /// The error is scheduled as the first GetStatusChange result after the PnP probe.
        /// </summary>
        private static void AssertErrorTriggersContextReestablishment(uint errorCode, string errorName)
        {
            var fake = new FakeSCardInterop(
                probeResult: ErrorCode.SCARD_E_TIMEOUT,
                scheduledResults: new[] { errorCode });

            using var listener = new DesktopSmartCardDeviceListener(fake);
            Thread.Sleep(2500);

            Assert.True(
                fake.EstablishContextCallCount >= 2,
                $"EstablishContext was called {fake.EstablishContextCallCount} time(s). " +
                $"Expected >= 2: {errorName} must trigger context re-establishment.");
        }

        // -----------------------------------------------------------------------------------------
        // ISC-D — When context re-establishment itself fails, listener continues without crashing
        //
        // If SCardEstablishContext fails during recovery (Smart Card Service still unavailable),
        // the listener must not crash, must not replace _context with a failed handle, and
        // must continue attempting recovery (bounded by the 1000ms sleep between retries).
        // -----------------------------------------------------------------------------------------

        [Fact]
        public void WhenContextReestablishmentFails_ListenerContinuesWithoutCrashing()
        {
            // Arrange: first EstablishContext (construction) succeeds,
            // subsequent EstablishContext calls (recovery) fail.
            // GetStatusChange returns INVALID_HANDLE to trigger recovery.
            var fake = new FakeSCardInterop(
                probeResult: ErrorCode.SCARD_E_TIMEOUT,
                defaultResult: ErrorCode.SCARD_E_INVALID_HANDLE,
                establishContextFailAfterFirstCall: true);

            var exception = Record.Exception(() =>
            {
                using var listener = new DesktopSmartCardDeviceListener(fake);
                Thread.Sleep(2500);
                // Listener should still be alive (Status is not Error due to exception)
                Assert.NotEqual(DeviceListenerStatus.Error, listener.Status);
            });

            Assert.Null(exception);
        }

        // -----------------------------------------------------------------------------------------
        // Follow-up step 1 — Status resets to Started after a recovered managed exception
        //
        // ListenForReaderChanges sets Status = Error in its catch (Exception) block but never
        // resets it. After the listener recovers (next poll succeeds), Status must reflect live
        // health (Started), not the stale Error value.
        // -----------------------------------------------------------------------------------------

        [Fact]
        public void WhenPollSucceedsAfterManagedException_StatusResetsToStarted()
        {
            // Arrange: probe -> TIMEOUT (no PnP workaround). First post-probe poll throws,
            // flipping Status to Error in ListenForReaderChanges' catch block. Subsequent
            // polls return TIMEOUT (success path that reaches the end of CheckForUpdates).
            var fake = new FakeSCardInterop(
                probeResult: ErrorCode.SCARD_E_TIMEOUT,
                defaultResult: ErrorCode.SCARD_E_TIMEOUT,
                throwOnGetStatusChangeAfterProbe: true);

            using var listener = new DesktopSmartCardDeviceListener(fake);

            // Wait long enough for: probe + throw (Status=Error) + 1000ms sleep + several successful polls.
            Thread.Sleep(1500);

            Assert.Equal(DeviceListenerStatus.Started, listener.Status);
        }

        // -----------------------------------------------------------------------------------------
        // Follow-up step 2 — ListenForReaderChanges catch block is throttled
        //
        // An unexpected managed exception from CheckForUpdates re-enters the while (_isListening)
        // loop with no delay (pre-Step-2). This can cause a tight spin if the same exception
        // recurs. Step 2 adds Thread.Sleep(RecoveryBackoffDelay) to throttle the retry.
        // -----------------------------------------------------------------------------------------

        [Fact]
        public void WhenCatchBlockTriggers_LoopThrottlesBeforeRetry()
        {
            // Arrange: probe -> TIMEOUT, then every poll throws.
            var fake = new FakeSCardInterop(
                probeResult: ErrorCode.SCARD_E_TIMEOUT,
                defaultResult: ErrorCode.SCARD_E_TIMEOUT,
                throwOnGetStatusChangeAfterProbe: true);

            using var listener = new DesktopSmartCardDeviceListener(fake);

            // Act: observe for ~600ms. Without throttle in the catch block, hundreds of
            // GetStatusChange calls would occur. With throttle, only 1–2 fit in 600ms.
            Thread.Sleep(600);

            int callCount = fake.GetStatusChangeCallCount;

            // Assert: fewer than 5 calls proves throttling is working.
            // Expected: probe + 1 throw (~0ms) + 1000ms sleep → only ~2 calls total in 600ms.
            Assert.True(
                callCount < 5,
                $"GetStatusChange was called {callCount} times in ~600ms. " +
                "Expected < 5: catch block must throttle before retry.");
        }

        // -----------------------------------------------------------------------------------------
        // Follow-up step 3 — Dispose unblocks immediately when listener is in recovery wait
        //
        // Thread.Sleep(RecoveryBackoffDelay) blocks Dispose for up to 1 second per active wait
        // site. _scard.Cancel(_context) only wakes a blocked syscall, not a sleeping thread.
        // Step 3 replaces Thread.Sleep with ManualResetEventSlim.Wait(timeout) so StopListening
        // can signal the wait and Dispose returns immediately.
        // -----------------------------------------------------------------------------------------

        [Fact]
        public void WhenDisposeCalledDuringRecoveryWait_DisposeReturnsQuickly()
        {
            // Arrange: schedule INVALID_HANDLE on every poll so the listener enters recovery wait.
            var fake = new FakeSCardInterop(
                probeResult: ErrorCode.SCARD_E_TIMEOUT,
                defaultResult: ErrorCode.SCARD_E_INVALID_HANDLE);

            var listener = new DesktopSmartCardDeviceListener(fake);

            // Give the listener time to enter the recovery wait (probe + first INVALID_HANDLE poll
            // + start of 1000ms wait).
            Thread.Sleep(50);

            // Act: measure Dispose duration.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            listener.Dispose();
            sw.Stop();

            // Assert: Dispose must return in under 200ms.
            // Pre-Step-3: Dispose would block on the full 1000ms Thread.Sleep.
            // Post-Step-3: _stopRequested.Set() wakes the wait immediately.
            Assert.True(
                sw.ElapsedMilliseconds < 200,
                $"Dispose took {sw.ElapsedMilliseconds}ms. Expected < 200ms: " +
                "recovery waits must be cancellation-aware so Dispose unblocks immediately.");
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // Test double
        // ─────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A deterministic fake of <see cref="ISCardInterop"/> that lets tests control which
        /// error codes <c>GetStatusChange</c> returns and count calls to each method.
        /// Thread-safe: counters use <c>volatile</c> reads/writes from the listener thread.
        /// </summary>
        private sealed class FakeSCardInterop : ISCardInterop
        {
            private readonly uint _probeResult;
            private readonly uint _defaultResult;
            private readonly Queue<uint> _scheduledResults;
            private readonly bool _establishContextFailAfterFirstCall;
            private readonly bool _throwOnGetStatusChangeAfterProbe;

            private int _establishContextCallCount;
            private int _getStatusChangeCallCount;
            private int _hasThrownOnce;

            /// <summary>Total calls to EstablishContext. Safe to read from test thread after Thread.Sleep.</summary>
            public int EstablishContextCallCount => Volatile.Read(ref _establishContextCallCount);

            /// <summary>Total calls to GetStatusChange (includes the UsePnpWorkaround probe).</summary>
            public int GetStatusChangeCallCount => Volatile.Read(ref _getStatusChangeCallCount);

            /// <param name="probeResult">
            ///   Return value for the very first GetStatusChange call (UsePnpWorkaround probe).
            ///   Defaults to SCARD_E_TIMEOUT so the probe indicates no PnP workaround needed.
            /// </param>
            /// <param name="defaultResult">
            ///   Return value for all GetStatusChange calls once <paramref name="scheduledResults"/>
            ///   is exhausted. Defaults to SCARD_E_TIMEOUT (normal polling).
            /// </param>
            /// <param name="scheduledResults">
            ///   Ordered sequence of return values for GetStatusChange calls after the probe.
            ///   Values are consumed in order; after the queue is empty, <paramref name="defaultResult"/> is used.
            /// </param>
            /// <param name="establishContextFailAfterFirstCall">
            ///   When true, the second and subsequent calls to EstablishContext return
            ///   SCARD_E_NO_SERVICE to simulate the Smart Card Service being unavailable during recovery.
            /// </param>
            /// <param name="throwOnGetStatusChangeAfterProbe">
            ///   When true, the first GetStatusChange call after the probe throws
            ///   InvalidOperationException to simulate a managed exception escaping into
            ///   ListenForReaderChanges' catch block. Subsequent calls behave normally.
            /// </param>
            public FakeSCardInterop(
                uint probeResult = ErrorCode.SCARD_E_TIMEOUT,
                uint defaultResult = ErrorCode.SCARD_E_TIMEOUT,
                uint[]? scheduledResults = null,
                bool establishContextFailAfterFirstCall = false,
                bool throwOnGetStatusChangeAfterProbe = false)
            {
                _probeResult = probeResult;
                _defaultResult = defaultResult;
                _scheduledResults = scheduledResults is null
                    ? new Queue<uint>()
                    : new Queue<uint>(scheduledResults);
                _establishContextFailAfterFirstCall = establishContextFailAfterFirstCall;
                _throwOnGetStatusChangeAfterProbe = throwOnGetStatusChangeAfterProbe;
            }

            public uint EstablishContext(SCARD_SCOPE scope, out SCardContext context)
            {
                int callNum = Interlocked.Increment(ref _establishContextCallCount);

                if (_establishContextFailAfterFirstCall && callNum > 1)
                {
                    context = new SCardContext(IntPtr.Zero);
                    return ErrorCode.SCARD_E_NO_SERVICE;
                }

                // Return a distinct non-zero handle on success, matching real WinSCard behavior.
                context = new SCardContext(new IntPtr(callNum));
                return ErrorCode.SCARD_S_SUCCESS;
            }

            public uint GetStatusChange(SCardContext context, int timeout, SCARD_READER_STATE[] states, int count)
            {
                int callNum = Interlocked.Increment(ref _getStatusChangeCallCount);

                // Call #1 is always the UsePnpWorkaround probe (timeout=0).
                if (callNum == 1)
                {
                    return _probeResult;
                }

                if (_throwOnGetStatusChangeAfterProbe
                    && Interlocked.Exchange(ref _hasThrownOnce, 1) == 0)
                {
                    throw new InvalidOperationException("Simulated managed exception in GetStatusChange.");
                }

                lock (_scheduledResults)
                {
                    if (_scheduledResults.Count > 0)
                    {
                        return _scheduledResults.Dequeue();
                    }
                }

                return _defaultResult;
            }

            public uint ListReaders(SCardContext context, string[]? groups, out string[] readerNames)
            {
                // Return empty reader list — no readers is valid and avoids allocating real state.
                readerNames = Array.Empty<string>();
                return ErrorCode.SCARD_E_NO_READERS_AVAILABLE;
            }

            public uint Cancel(SCardContext context) => ErrorCode.SCARD_S_SUCCESS;
        }
    }
}
