// Copyright 2021 Yubico AB
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

namespace Yubico.Core.Devices.Hid.UnitTests
{
    internal class FakeHidDevice : IHidDevice
    {
        public DateTime LastAccessed { get; } = DateTime.Now;
        public string Path { get; } = string.Empty;
        public string? ParentDeviceId { get; } = null;
        public short VendorId { get; }
        public short ProductId { get; }
        public short Usage { get; }
        public HidUsagePage UsagePage { get; }

        public IHidConnection ConnectToFeatureReports()
        {
            throw new NotImplementedException();
        }

        public IHidConnection ConnectToIOReports()
        {
            throw new NotImplementedException();
        }
    }

    internal class FakeHidListener : HidDeviceListener
    {
        public void FireArrival()
        {
            OnArrived(new FakeHidDevice());
        }

        public void FireRemoval()
        {
            OnRemoved(new FakeHidDevice());
        }

        public void Clear()
        {
            ClearEventHandlers();
        }
    }

    public class HidDeviceListenerTests
    {
        [Fact]
        public void Create_ReturnsInstanceOfListener()
        {
            var listener = HidDeviceListener.Create();
            _ = Assert.IsAssignableFrom<HidDeviceListener>(listener);
        }

        [Fact]
        public void OnArrived_WithNoListeners_NoOps()
        {
            var listener = new FakeHidListener();
            listener.FireArrival();
        }

        [Fact]
        public void OnArrived_WithEventListener_RaisesArrivedEvent()
        {
            var listener = new FakeHidListener();
            _ = Assert.Raises<HidDeviceEventArgs>(
                e => listener.Arrived += e,
                e => listener.Arrived -= e,
                () => listener.FireArrival());
        }

        [Fact]
        public void OnRemoved_WithNoListeners_NoOps()
        {
            var listener = new FakeHidListener();
            listener.FireRemoval();
        }

        [Fact]
        public void OnRemoved_WithEventListener_RaisesRemovedEvent()
        {
            var listener = new FakeHidListener();
            _ = Assert.Raises<HidDeviceEventArgs>(
                e => listener.Removed += e,
                e => listener.Removed -= e,
                () => listener.FireRemoval());
        }

        [Fact]
        public void ClearEventHandlers_WithNoListeners_Succeeds()
        {
            var listener = new FakeHidListener();
            listener.Clear();
        }

        [Fact]
        public void ClearEventHandlers_WithEventListeners_DoesNotRaiseEvent()
        {
            var listener = new FakeHidListener();

            listener.Arrived += (sender, args) => Assert.False(condition: true);
            listener.Removed += (sender, args) => Assert.False(condition: true);

            listener.Clear();

            listener.FireArrival();
            listener.FireRemoval();
        }
    }
}
