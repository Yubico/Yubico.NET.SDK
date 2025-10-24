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
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey
{
    /// <summary>
    /// Unit tests for YubiKeyDeviceListener crash safety.
    /// These tests verify that background threads don't crash the application.
    /// </summary>
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class YubiKeyDeviceListenerCrashSafetyUnitTests
    {
        /// <summary>
        /// Test #1 (SIMPLEST): Verify that calling Dispose() twice doesn't throw an exception.
        /// This is the most basic safety test - disposal must be idempotent.
        /// </summary>
        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // Arrange
            var listener = YubiKeyDeviceListener.Instance;

            // Act & Assert - First disposal should succeed
            listener.Dispose();

            // Second disposal should not throw
            var exception = Record.Exception(() => listener.Dispose());

            Assert.Null(exception);
        }

        /// <summary>
        /// Test #2: Verify that exceptions in event handlers don't crash the application.
        /// This tests Issue #1 from the investigation - event handler exceptions should be caught.
        /// EXPECTED: This test should FAIL initially (showing bug exists), then PASS after fix.
        /// </summary>
        [Fact]
        public void EventHandler_ThrowsException_DoesNotPropagate()
        {
            // Arrange
            var listener = YubiKeyDeviceListener.Instance;

            // Subscribe an event handler that throws
            EventHandler<YubiKeyDeviceEventArgs> throwingHandler = (sender, args) =>
            {
                throw new InvalidOperationException("Simulated user code exception");
            };

            listener.Arrived += throwingHandler;

            try
            {
                // Act - Trigger the event by calling the internal method directly
                // Pass null device since we're just testing exception handling
                var eventArgs = new YubiKeyDeviceEventArgs(null!);

                // This should NOT throw after the fix - exception should be caught internally
                var exception = Record.Exception(() => listener.OnDeviceArrived(eventArgs));

                // Assert - After fix, no exception should propagate
                Assert.Null(exception);
            }
            finally
            {
                // Cleanup - Remove handler
                listener.Arrived -= throwingHandler;
            }
        }

        /// <summary>
        /// Test #3: Verify that when one event handler throws, other handlers still execute.
        /// This ensures handler isolation - one bad handler shouldn't break others.
        /// EXPECTED: This test should FAIL initially, then PASS after fix.
        /// </summary>
        [Fact]
        public void EventHandler_MultipleHandlers_OneThrows_OthersStillExecute()
        {
            // Arrange
            var listener = YubiKeyDeviceListener.Instance;
            bool handler1Called = false;
            bool handler3Called = false;

            EventHandler<YubiKeyDeviceEventArgs> handler1 = (sender, args) => { handler1Called = true; };
            EventHandler<YubiKeyDeviceEventArgs> handler2 = (sender, args) =>
            {
                throw new InvalidOperationException("Handler 2 throws");
            };
            EventHandler<YubiKeyDeviceEventArgs> handler3 = (sender, args) => { handler3Called = true; };

            listener.Arrived += handler1;
            listener.Arrived += handler2;
            listener.Arrived += handler3;

            try
            {
                // Act
                var eventArgs = new YubiKeyDeviceEventArgs(null!);
                _ = Record.Exception(() => listener.OnDeviceArrived(eventArgs));

                // Assert - After fix, both handler1 and handler3 should execute
                // even though handler2 threw an exception
                Assert.True(handler1Called, "Handler 1 should have been called");
                Assert.True(handler3Called, "Handler 3 should have been called despite handler 2 throwing");
            }
            finally
            {
                // Cleanup
                listener.Arrived -= handler1;
                listener.Arrived -= handler2;
                listener.Arrived -= handler3;
            }
        }
    }
}
