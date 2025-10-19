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
using Yubico.Core.Iso7816;

namespace Yubico.Core.Devices.SmartCard.UnitTests
{
    class FakeSmartCardDevice : ISmartCardDevice
    {
        public DateTime LastAccessed { get; } = DateTime.Now;
        public string Path { get; } = string.Empty;
        public string? ParentDeviceId { get; } = null;
        public AnswerToReset? Atr { get; }
        public SmartCardConnectionKind Kind { get; }
        public ISmartCardConnection Connect() => throw new NotImplementedException();
    }

    class FakeSmartCardListener : SmartCardDeviceListener
    {
        public void FireArrival() => OnArrived(new FakeSmartCardDevice());
        public void FireRemoval() => OnRemoved(new FakeSmartCardDevice());
        public void Clear() => ClearEventHandlers();
    }

    public class SmartCardDeviceListenerTests
    {
        // [Fact]
        // public void Create_ReturnsInstanceOfListener()
        // {
        //     var listener = SmartCardDeviceListener.Create();
        //     _ = Assert.IsAssignableFrom<SmartCardDeviceListener>(listener);
        // }

        [Fact]
        public void OnArrived_WithNoListeners_NoOps()
        {
            var listener = new FakeSmartCardListener();
            listener.FireArrival();
        }

        [Fact]
        public void OnArrived_WithEventListener_RaisesArrivedEvent()
        {
            var listener = new FakeSmartCardListener();
            _ = Assert.Raises<SmartCardDeviceEventArgs>(
                e => listener.Arrived += e,
                e => listener.Arrived -= e,
                () => listener.FireArrival());
        }

        [Fact]
        public void OnRemoved_WithNoListeners_NoOps()
        {
            var listener = new FakeSmartCardListener();
            listener.FireRemoval();
        }

        [Fact]
        public void OnRemoved_WithEventListener_RaisesRemovedEvent()
        {
            var listener = new FakeSmartCardListener();
            _ = Assert.Raises<SmartCardDeviceEventArgs>(
                e => listener.Removed += e,
                e => listener.Removed -= e,
                () => listener.FireRemoval());
        }

        [Fact]
        public void ClearEventHandlers_WithNoListeners_Succeeds()
        {
            var listener = new FakeSmartCardListener();
            listener.Clear();
        }

        [Fact]
        public void ClearEventHandlers_WithEventListeners_DoesNotRaiseEvent()
        {
            var listener = new FakeSmartCardListener();

            listener.Arrived += (sender, args) => Assert.False(true);
            listener.Removed += (sender, args) => Assert.False(true);

            listener.Clear();

            listener.FireArrival();
            listener.FireRemoval();
        }

        [Fact]
        public void OnArrived_EventHandlerThrows_DoesNotPropagate()
        {
            // Arrange
            var listener = new FakeSmartCardListener();

            // Subscribe an event handler that throws
            listener.Arrived += (sender, args) =>
            {
                throw new InvalidOperationException("Simulated handler exception");
            };

            // Act & Assert - Should NOT throw, exception should be caught internally
            var exception = Record.Exception(() => listener.FireArrival());
            Assert.Null(exception);
        }

        [Fact]
        public void OnArrived_MultipleHandlers_OneThrows_OthersStillExecute()
        {
            // Arrange
            var listener = new FakeSmartCardListener();
            bool handler1Called = false;
            bool handler3Called = false;

            listener.Arrived += (sender, args) => { handler1Called = true; };
            listener.Arrived += (sender, args) =>
            {
                throw new InvalidOperationException("Handler 2 throws");
            };
            listener.Arrived += (sender, args) => { handler3Called = true; };

            // Act
            listener.FireArrival();

            // Assert - Both handler1 and handler3 should execute despite handler2 throwing
            Assert.True(handler1Called, "Handler 1 should have been called");
            Assert.True(handler3Called, "Handler 3 should have been called despite handler 2 throwing");
        }

        [Fact]
        public void OnRemoved_EventHandlerThrows_DoesNotPropagate()
        {
            // Arrange
            var listener = new FakeSmartCardListener();

            // Subscribe an event handler that throws
            listener.Removed += (sender, args) =>
            {
                throw new InvalidOperationException("Simulated handler exception");
            };

            // Act & Assert - Should NOT throw, exception should be caught internally
            var exception = Record.Exception(() => listener.FireRemoval());
            Assert.Null(exception);
        }

        [Fact]
        public void OnRemoved_MultipleHandlers_OneThrows_OthersStillExecute()
        {
            // Arrange
            var listener = new FakeSmartCardListener();
            bool handler1Called = false;
            bool handler3Called = false;

            listener.Removed += (sender, args) => { handler1Called = true; };
            listener.Removed += (sender, args) =>
            {
                throw new InvalidOperationException("Handler 2 throws");
            };
            listener.Removed += (sender, args) => { handler3Called = true; };

            // Act
            listener.FireRemoval();

            // Assert - Both handler1 and handler3 should execute despite handler2 throwing
            Assert.True(handler1Called, "Handler 1 should have been called");
            Assert.True(handler3Called, "Handler 3 should have been called despite handler 2 throwing");
        }
    }
}
