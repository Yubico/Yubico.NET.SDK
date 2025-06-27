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
    }
}
